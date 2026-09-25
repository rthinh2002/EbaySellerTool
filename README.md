# EbaySellerTool

A console tool for bulk-listing raw TCG single cards (Yu-Gi-Oh!, Riftbound, and more) on **eBay Australia (`EBAY_AU`)**. It reads an Excel sheet and publishes listings through the eBay Sell APIs.

## Projects

| Project | Purpose |
|---|---|
| `src/EbaySellerTool.Core` | Models, Excel import, validation, eBay API clients, listing workflow. UI-agnostic, so a future ASP.NET Core API / Angular app can reuse it. |
| `src/EbaySellerTool.Cli` | Console host (`ebaytool`). |
| `tests/EbaySellerTool.Tests` | xUnit tests. |

## How listing works

1. Read and validate the Excel sheet (required fields, price, item specifics, local image files exist).
2. Upload local images through the **Media API** (`createImageFromFile`) to get eBay-hosted image URLs.
3. `bulkCreateOrReplaceInventoryItem` (25 per call, keyed by SKU).
4. `bulkCreateOffer`: price, category, store category, business policies, location.
5. `bulkPublishOffer`: returns a listing ID or error for each offer.
6. Print a success/failure summary and write `results_<timestamp>.xlsx` next to the input file.

## Usage

```
dotnet run --project src/EbaySellerTool.Cli -- template cards.xlsx   # create a blank input sheet
dotnet run --project src/EbaySellerTool.Cli -- validate cards.xlsx   # check the sheet for errors
dotnet run --project src/EbaySellerTool.Cli -- list cards.xlsx --dry-run   # preview the eBay requests
```

`list --dry-run` writes two files next to the sheet:

- `dryrun_<sheet>_<timestamp>.json`: the exact eBay requests that would be sent (inventory items and offers, in batches of 25)
- `results_<sheet>_<timestamp>.xlsx`: one row per card with its status and any errors

### Settings

Listing defaults live in `src/EbaySellerTool.Cli/appsettings.json`: marketplace (`EBAY_AU`), currency, default category, business policy IDs, inventory location and an optional HTML description template (`ListingDefaults:DescriptionTemplatePath`). A template can use `{{Title}}`, `{{CardName}}`, `{{Game}}`, `{{SetName}}`, `{{CardNumber}}`, `{{Rarity}}`, `{{Language}}`, `{{Condition}}`, `{{Details}}` and `{{Description}}`.

The template has a **Cards** sheet (required headers highlighted in orange; hover a header for help) and an **Instructions** sheet describing every column. Image paths can be relative to the Excel file's folder, and multiple images are separated with `|`.

### Planned commands

```
ebaytool auth                 # one-time OAuth login (stores refresh token locally)
ebaytool setup                # fetch business policy IDs, create inventory location, cache category aspects
ebaytool list <file.xlsx>     # live listing, once the eBay API clients are connected
```

## Getting eBay developer credentials

1. Sign up at <https://developer.ebay.com> with your eBay account and wait for approval (usually about a day).
2. Under **Application Keys**, create a keyset for **Sandbox** and one for **Production** (App ID / Client ID, Cert ID / Client Secret).
3. Complete the **Marketplace Account Deletion** notification requirement for the production keyset (or apply for the exemption, since this tool doesn't store other users' data).
4. Under **User Tokens → Get a Token from eBay via Your Application**, create a **RuName** (redirect URL name). Note it down.
5. Store the secrets with user-secrets. They must never be committed:

   ```
   dotnet user-secrets --project src/EbaySellerTool.Cli set "Ebay:ClientId" "<id>"
   dotnet user-secrets --project src/EbaySellerTool.Cli set "Ebay:ClientSecret" "<secret>"
   dotnet user-secrets --project src/EbaySellerTool.Cli set "Ebay:RuName" "<runame>"
   ```

## Status

Early setup. See the roadmap above.
