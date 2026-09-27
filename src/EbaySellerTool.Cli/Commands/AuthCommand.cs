using System.CommandLine;
using System.Diagnostics;
using System.Security.Cryptography;
using EbaySellerTool.Core.Configuration;
using EbaySellerTool.Core.Ebay;
using EbaySellerTool.Core.Ebay.Auth;
using Microsoft.Extensions.Options;
using Spectre.Console;

namespace EbaySellerTool.Cli.Commands;

internal sealed class AuthCommand(
    IEbayOAuthClient oauthClient,
    ITokenStore tokenStore,
    IOptions<EbayOptions> ebayOptions) : ICliCommand
{
    private readonly EbayOptions _ebay = ebayOptions.Value;

    public Command Build()
    {
        var command = new Command("auth", "Sign in to eBay and let the tool manage your listings. Needed once every 18 months.");
        command.SetAction((_, cancellationToken) => ExecuteAsync(cancellationToken));

        return command;
    }

    private async Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        var missingSettings = _ebay.ActiveCredentials.GetMissingSettings(_ebay.Environment);

        if (missingSettings.Count > 0)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]Missing eBay app settings:[/] {string.Join(", ", missingSettings)}");
            AnsiConsole.MarkupLine("Add them with [bold]dotnet user-secrets[/] (see the README).");
            return ExitCodes.InvalidInput;
        }

        var state = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        var consentUrl = oauthClient.BuildConsentUrl(state);

        ShowInstructions(consentUrl);
        OpenInBrowser(consentUrl);

        var redirect = AskForRedirect(state);
        var token = await ExchangeCodeAsync(redirect, cancellationToken);

        if (token is null)
        {
            return ExitCodes.InvalidInput;
        }

        await tokenStore.SaveAsync(_ebay.Environment, token, cancellationToken);
        AnsiConsole.MarkupLineInterpolated(
            $"[green]Signed in to eBay {_ebay.Environment}.[/] The sign-in lasts until {token.RefreshTokenExpiresAt.LocalDateTime:d MMM yyyy}.");

        return ExitCodes.Success;
    }

    private void ShowInstructions(Uri consentUrl)
    {
        AnsiConsole.MarkupLineInterpolated($"Signing in to eBay [bold]{_ebay.Environment}[/].");

        if (_ebay.Environment == EbayEnvironment.Sandbox)
        {
            AnsiConsole.MarkupLine("[yellow]Use your Sandbox test user, not your real eBay account.[/]");
        }

        AnsiConsole.MarkupLine("1. Sign in on the page that opens in your browser and click [bold]Agree[/].");
        AnsiConsole.MarkupLine("2. Copy the full address of the page you land on and paste it below.");
        AnsiConsole.MarkupLine("If the browser doesn't open, visit:");
        AnsiConsole.WriteLine(consentUrl.AbsoluteUri);
    }

    private static void OpenInBrowser(Uri url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url.AbsoluteUri) { UseShellExecute = true });
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            AnsiConsole.MarkupLine("[yellow]Couldn't open a browser automatically; use the link above.[/]");
        }
    }

    private static AuthorizationRedirect AskForRedirect(string expectedState)
    {
        while (true)
        {
            var pastedUrl = AnsiConsole.Ask<string>("Address of the page after clicking Agree:");

            if (!AuthorizationRedirect.TryParse(pastedUrl, out var redirect))
            {
                AnsiConsole.MarkupLine("[red]That address doesn't contain an eBay authorization code.[/] Copy the whole address bar and try again.");
            }
            else if (redirect!.State is not null && redirect.State != expectedState)
            {
                AnsiConsole.MarkupLine("[red]That address is from a different sign-in attempt.[/] Use the page opened by this command.");
            }
            else
            {
                return redirect;
            }
        }
    }

    private async Task<EbayToken?> ExchangeCodeAsync(AuthorizationRedirect redirect, CancellationToken cancellationToken)
    {
        try
        {
            return await oauthClient.ExchangeAuthorizationCodeAsync(redirect.Code, cancellationToken);
        }
        catch (EbayApiException exception)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]{exception.Message}[/]");
            AnsiConsole.MarkupLine("The code is only valid for about 5 minutes. Run [bold]ebaytool auth[/] again if it expired.");
            return null;
        }
    }
}
