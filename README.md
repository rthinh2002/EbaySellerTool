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

### Scanning cards

Scan up to 9 card fronts at once on an A4 flatbed (3×3 grid), then split the scan into one image per card:

```
dotnet run --project src/EbaySellerTool.Cli -- split scans --output images --sheet cards.xlsx
```

`split` takes a scan or a folder of scans, straightens and crops each card to `images/<scan>_card01.jpg`, … (numbered left to right, top to bottom), and adds a row per card to the sheet with **Images** already filled in. You only type the card details.

For best results:
- Scan at **600 DPI**.
- Leave a gap between cards.
- Keep cards about **1 cm from the glass edges**, where scanners leave shadow lines.
- Plain white backgrounds work for dark-bordered cards.

In Visual Studio, pick a launch profile (Validate, List (dry run), Split scans, Template) next to the ▶ button. They run against the `samples/` folder.

### Dry run

`list --dry-run` writes two files next to the sheet:

- `dryrun_<sheet>_<timestamp>.json`: the exact eBay requests that would be sent (inventory items and offers, in batches of 25)
- `results_<sheet>_<timestamp>.xlsx`: one row per card with its status and any errors

### Settings

Listing defaults live in `src/EbaySellerTool.Cli/appsettings.json`: marketplace (`EBAY_AU`), currency, default category, business policy IDs, inventory location and an optional HTML description template (`ListingDefaults:DescriptionTemplatePath`). A template can use `{{Title}}`, `{{CardName}}`, `{{Game}}`, `{{SetName}}`, `{{CardNumber}}`, `{{Rarity}}`, `{{Language}}`, `{{Condition}}`, `{{Details}}` and `{{Description}}`.

The template has a **Cards** sheet (required headers highlighted in orange; hover a header for help) and an **Instructions** sheet describing every column. Image paths can be relative to the Excel file's folder, and multiple images are separated with `|`.

### Listing on eBay

```
dotnet run --project src/EbaySellerTool.Cli -- setup              # once per environment: pick business policies + inventory location
dotnet run --project src/EbaySellerTool.Cli -- list cards.xlsx    # list every valid row; writes results_*.xlsx
```

Re-running `list` on the same sheet revises existing listings (matched by SKU) instead of duplicating them. In the **Sandbox**, eBay's image upload isn't available, so every listing uses a placeholder image (`Ebay:SandboxPlaceholderImageUrl`). Real photos are uploaded in Production.

## Getting eBay developer credentials

1. Sign up at <https://developer.ebay.com> with your eBay account and wait for approval (usually about a day).
2. Under **Application Keys**, create a keyset for **Sandbox** and one for **Production** (App ID / Client ID, Cert ID / Client Secret).
3. Complete the **Marketplace Account Deletion** notification requirement for the production keyset (or apply for the exemption, since this tool doesn't store other users' data).
4. Under **User Tokens → Get a Token from eBay via Your Application**, create a **RuName** (redirect URL name) and tick **OAuth Enabled**. The *Auth Accepted URL* can stay as eBay's default page.
5. For the Sandbox, create a **Sandbox test user** (Developer portal → Sandbox → Users). You sign in with that user, not your real account.
6. Store the credentials with user-secrets, per environment. They must never be committed:

   ```
   dotnet user-secrets --project src/EbaySellerTool.Cli set "Ebay:Sandbox:ClientId" "<App ID>"
   dotnet user-secrets --project src/EbaySellerTool.Cli set "Ebay:Sandbox:ClientSecret" "<Cert ID>"
   dotnet user-secrets --project src/EbaySellerTool.Cli set "Ebay:Sandbox:RuName" "<RuName>"
   ```

   Use `Ebay:Production:...` for the production keyset, and switch environments with `Ebay:Environment` in `appsettings.json`.

## Signing in

```
dotnet run --project src/EbaySellerTool.Cli -- auth
```

This opens eBay's sign-in page. Sign in, click **Agree**, then copy the full address of the page you land on and paste it into the console. The tool stores the sign-in encrypted (Windows DPAPI) in `%LOCALAPPDATA%\EbaySellerTool\token.<environment>.bin` and refreshes it automatically. It lasts about 18 months.

## Status

Working end to end in the eBay Sandbox. Production listing is next.