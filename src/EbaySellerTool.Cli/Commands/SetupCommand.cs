using System.CommandLine;
using EbaySellerTool.Cli.Storage;
using EbaySellerTool.Core.Configuration;
using EbaySellerTool.Core.Ebay;
using EbaySellerTool.Core.Ebay.Account;
using EbaySellerTool.Core.Ebay.Auth;
using EbaySellerTool.Core.Ebay.Inventory;
using Microsoft.Extensions.Options;
using Spectre.Console;

namespace EbaySellerTool.Cli.Commands;

internal sealed class SetupCommand(
    IEbayAccountClient accountClient,
    IEbayLocationClient locationClient,
    IOptions<EbayOptions> ebayOptions) : ICliCommand
{
    private const string DefaultLocationKey = "home";
    private const string DefaultLocationName = "Home";

    private readonly EbayOptions _ebay = ebayOptions.Value;

    public Command Build()
    {
        var yesOption = new Option<bool>("--yes", "-y") { Description = "Don't ask questions: turn on business policies and pick the first policy/location." };
        var postcodeOption = new Option<string?>("--postcode") { Description = "Postcode of the new inventory location." };
        var suburbOption = new Option<string?>("--suburb") { Description = "Suburb of the new inventory location." };
        var stateOption = new Option<string?>("--state") { Description = "State of the new inventory location, e.g. NSW." };

        var command = new Command("setup", "Choose your business policies and inventory location for listings.")
        {
            yesOption, postcodeOption, suburbOption, stateOption
        };
        command.SetAction((parseResult, cancellationToken) => ExecuteAsync(
            new SetupAnswers(
                parseResult.GetValue(yesOption),
                AnsiConsole.Profile.Capabilities.Interactive,
                parseResult.GetValue(postcodeOption),
                parseResult.GetValue(suburbOption),
                parseResult.GetValue(stateOption)),
            cancellationToken));

        return command;
    }

    private async Task<int> ExecuteAsync(SetupAnswers answers, CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLineInterpolated($"Setting up eBay [bold]{_ebay.Environment}[/] ({_ebay.MarketplaceId}).");

        try
        {
            var policies = await ChoosePoliciesAsync(answers, cancellationToken);
            var location = policies is null ? null : await ChooseLocationAsync(answers, cancellationToken);

            if (policies is null || location is null)
            {
                return ExitCodes.ValidationFailed;
            }

            SaveSettings(policies, location);
            return ExitCodes.Success;
        }
        catch (EbayNotSignedInException exception)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]{exception.Message}[/]");
            return ExitCodes.InvalidInput;
        }
        catch (EbayApiException exception)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]{exception.Message}[/]");
            return ExitCodes.ValidationFailed;
        }
    }

    private async Task<Dictionary<BusinessPolicyType, BusinessPolicy>?> ChoosePoliciesAsync(SetupAnswers answers, CancellationToken cancellationToken)
    {
        var policiesByType = await LoadPoliciesWithOptInAsync(answers, cancellationToken);

        if (policiesByType is null)
        {
            return null;
        }

        var missingTypes = policiesByType.Where(entry => entry.Value.Count == 0).Select(entry => entry.Key).ToList();

        foreach (var type in missingTypes)
        {
            AnsiConsole.MarkupLineInterpolated($"[yellow]No {type.ToString().ToLowerInvariant()} policy found for {_ebay.MarketplaceId}.[/]");
        }

        if (missingTypes.Count > 0 && !await TryCreateSandboxPoliciesAsync(missingTypes, policiesByType, answers, cancellationToken))
        {
            AnsiConsole.MarkupLine("Create the missing policies in Seller Hub (Account → Business policies), then run setup again.");
            return null;
        }

        return policiesByType.ToDictionary(
            entry => entry.Key,
            entry => Choose($"{entry.Key} policy", entry.Value, policy => policy.Name, answers));
    }

    private async Task<bool> TryCreateSandboxPoliciesAsync(
        IReadOnlyList<BusinessPolicyType> missingTypes,
        Dictionary<BusinessPolicyType, IReadOnlyList<BusinessPolicy>> policiesByType,
        SetupAnswers answers,
        CancellationToken cancellationToken)
    {
        if (_ebay.Environment != EbayEnvironment.Sandbox || !Confirm("Create basic test policies for this Sandbox account?", answers))
        {
            return false;
        }

        foreach (var type in missingTypes)
        {
            var policy = await accountClient.CreatePolicyAsync(type, SandboxTestPolicies.Create(type, _ebay), cancellationToken);
            policiesByType[type] = [policy];
            AnsiConsole.MarkupLineInterpolated($"[green]Created {type.ToString().ToLowerInvariant()} policy[/] {policy.Name}");
        }

        return true;
    }

    private async Task<Dictionary<BusinessPolicyType, IReadOnlyList<BusinessPolicy>>?> LoadPoliciesWithOptInAsync(
        SetupAnswers answers, CancellationToken cancellationToken)
    {
        try
        {
            return await LoadPoliciesAsync(cancellationToken);
        }
        catch (BusinessPoliciesNotEnabledException)
        {
            AnsiConsole.MarkupLine("[yellow]Business policies aren't turned on for this eBay account.[/]");

            if (!Confirm("Turn them on now?", answers))
            {
                return null;
            }

            await accountClient.OptInToBusinessPoliciesAsync(cancellationToken);
            AnsiConsole.MarkupLine("[green]Business policies turned on.[/] eBay can take a few minutes to finish; if the next step fails, run setup again shortly.");
        }

        try
        {
            return await LoadPoliciesAsync(cancellationToken);
        }
        catch (BusinessPoliciesNotEnabledException)
        {
            AnsiConsole.MarkupLine("[yellow]eBay is still turning on business policies. Run setup again in a few minutes.[/]");
            return null;
        }
    }

    private async Task<Dictionary<BusinessPolicyType, IReadOnlyList<BusinessPolicy>>> LoadPoliciesAsync(CancellationToken cancellationToken)
    {
        var policiesByType = new Dictionary<BusinessPolicyType, IReadOnlyList<BusinessPolicy>>();

        foreach (var type in Enum.GetValues<BusinessPolicyType>())
        {
            policiesByType[type] = await accountClient.GetPoliciesAsync(type, cancellationToken);
        }

        return policiesByType;
    }

    private async Task<InventoryLocation?> ChooseLocationAsync(SetupAnswers answers, CancellationToken cancellationToken)
    {
        var locations = await locationClient.GetLocationsAsync(cancellationToken);

        if (locations.Count > 0)
        {
            return Choose("inventory location", locations, DescribeLocation, answers);
        }

        AnsiConsole.MarkupLine("No inventory location yet. eBay needs one to know where cards ship from.");
        var location = AskForNewLocation(answers);

        if (location is null)
        {
            return null;
        }

        await locationClient.CreateLocationAsync(location, cancellationToken);
        AnsiConsole.MarkupLineInterpolated($"[green]Created inventory location[/] {DescribeLocation(location)}");

        return location;
    }

    private InventoryLocation? AskForNewLocation(SetupAnswers answers)
    {
        var postcode = answers.Postcode ?? AskIfInteractive("Postcode:", answers);
        var suburb = answers.Suburb ?? AskIfInteractive("Suburb:", answers);
        var state = answers.State ?? AskIfInteractive("State (e.g. NSW):", answers);

        if (postcode is null || suburb is null || state is null)
        {
            AnsiConsole.MarkupLine("[red]Pass --postcode, --suburb and --state to create the inventory location.[/]");
            return null;
        }

        return new InventoryLocation(DefaultLocationKey, DefaultLocationName, suburb, state.ToUpperInvariant(), postcode, MarketplaceCountryCode());
    }

    private static bool Confirm(string question, SetupAnswers answers)
    {
        if (answers.AssumeYes)
        {
            return true;
        }

        if (!answers.CanPrompt)
        {
            AnsiConsole.MarkupLineInterpolated($"{question} Run setup again with --yes to agree.");
            return false;
        }

        return AnsiConsole.Confirm(question);
    }

    private static string? AskIfInteractive(string question, SetupAnswers answers) =>
        answers.CanPrompt ? AnsiConsole.Ask<string>(question) : null;

    private static T Choose<T>(string description, IReadOnlyList<T> options, Func<T, string> describe, SetupAnswers answers)
        where T : notnull
    {
        var chosen = options.Count == 1 || answers.AssumeYes || !answers.CanPrompt
            ? options[0]
            : AnsiConsole.Prompt(new SelectionPrompt<T>().Title($"Which {description}?").AddChoices(options).UseConverter(describe));

        AnsiConsole.MarkupLineInterpolated($"Using {description}: [bold]{describe(chosen)}[/]");
        return chosen;
    }

    private void SaveSettings(Dictionary<BusinessPolicyType, BusinessPolicy> policies, InventoryLocation location)
    {
        var settings = new Dictionary<string, string>
        {
            [nameof(ListingDefaultsOptions.FulfillmentPolicyId)] = policies[BusinessPolicyType.Fulfillment].Id,
            [nameof(ListingDefaultsOptions.PaymentPolicyId)] = policies[BusinessPolicyType.Payment].Id,
            [nameof(ListingDefaultsOptions.ReturnPolicyId)] = policies[BusinessPolicyType.Return].Id,
            [nameof(ListingDefaultsOptions.MerchantLocationKey)] = location.MerchantLocationKey
        };

        UserSettingsFile.SaveListingDefaults(_ebay.Environment, settings);
        AnsiConsole.MarkupLineInterpolated($"[green]Setup saved[/] to {UserSettingsFile.PathFor(_ebay.Environment)}");
    }

    // Marketplace IDs are "EBAY_" followed by the country code, e.g. EBAY_AU.
    private string MarketplaceCountryCode() => _ebay.MarketplaceId.Split('_').Last();

    private static string DescribeLocation(InventoryLocation location) =>
        $"{location.Name} ({location.MerchantLocationKey}) {location.City} {location.StateOrProvince} {location.PostalCode}".Trim();

    private sealed record SetupAnswers(bool AssumeYes, bool CanPrompt, string? Postcode, string? Suburb, string? State);
}
