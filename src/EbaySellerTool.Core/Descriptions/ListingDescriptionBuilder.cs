using System.Net;
using EbaySellerTool.Core.Cards;
using EbaySellerTool.Core.Configuration;
using Microsoft.Extensions.Options;

namespace EbaySellerTool.Core.Descriptions;

/// <summary>
/// Fills an HTML template's <c>{{Placeholder}}</c> tokens with the card's details.
/// Placeholders: Title, Game, CardName, SetName, CardNumber, Rarity, Language, Condition, Details, Description.
/// </summary>
public sealed class ListingDescriptionBuilder(IOptions<ListingDefaultsOptions> listingDefaults) : IListingDescriptionBuilder
{
    private const string DefaultTemplateResourceName = "EbaySellerTool.Core.Descriptions.DefaultDescriptionTemplate.html";
    private const string HtmlLineBreak = "\n";

    private readonly Lazy<string> _template = new(() => LoadTemplate(listingDefaults.Value.DescriptionTemplatePath));

    public string Build(CardListing listing)
    {
        var placeholders = new Dictionary<string, string>
        {
            ["Title"] = Encode(listing.Title),
            ["Game"] = Encode(listing.Game),
            ["CardName"] = Encode(listing.CardName),
            ["SetName"] = Encode(listing.SetName),
            ["CardNumber"] = Encode(listing.CardNumber),
            ["Rarity"] = Encode(listing.Rarity),
            ["Language"] = Encode(listing.Language),
            ["Condition"] = Encode(listing.Condition.ToDisplayName()),
            ["Details"] = BuildDetailsList(listing),
            ["Description"] = BuildCustomDescription(listing.Description)
        };

        return placeholders.Aggregate(
            _template.Value,
            (html, placeholder) => html.Replace($"{{{{{placeholder.Key}}}}}", placeholder.Value, StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildDetailsList(CardListing listing)
    {
        (string Label, string? Value)[] details =
        [
            ("Game", listing.Game),
            ("Card Name", listing.CardName),
            ("Set", listing.SetName),
            ("Card Number", listing.CardNumber),
            ("Rarity", listing.Rarity),
            ("Language", listing.Language),
            ("Condition", listing.Condition.ToDisplayName())
        ];

        return string.Join(
            HtmlLineBreak,
            details
                .Where(detail => !string.IsNullOrWhiteSpace(detail.Value))
                .Select(detail => $"<li><strong>{Encode(detail.Label)}:</strong> {Encode(detail.Value)}</li>"));
    }

    private static string BuildCustomDescription(string? description) =>
        string.IsNullOrWhiteSpace(description)
            ? string.Empty
            : $"<p>{Encode(description).ReplaceLineEndings("<br>")}</p>";

    private static string Encode(string? text) => WebUtility.HtmlEncode(text ?? string.Empty);

    private static string LoadTemplate(string templatePath) =>
        string.IsNullOrWhiteSpace(templatePath)
            ? ReadDefaultTemplate()
            : File.ReadAllText(Path.GetFullPath(templatePath));

    private static string ReadDefaultTemplate()
    {
        using var stream = typeof(ListingDescriptionBuilder).Assembly.GetManifestResourceStream(DefaultTemplateResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{DefaultTemplateResourceName}' is missing.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
