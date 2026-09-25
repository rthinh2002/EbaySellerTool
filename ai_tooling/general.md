# EbaySellerTool — Project Overview

> **For AI agents:** read this first. Keep it current: when you add a feature, change the architecture, or make a notable decision, update the relevant section and add an entry to the [Changelog](#changelog).

## Purpose

eBay's bulk-listing UI is slow and awkward. This tool lets the owner list trading cards in bulk from an Excel sheet using the eBay Sell APIs. It runs as a console app and reports every listing that succeeded or failed.

## Business context

| Topic | Decision |
|---|---|
| Marketplace | **eBay Australia** (`EBAY_AU`), currency **AUD** |
| Products | TCG single cards, mostly **Yu-Gi-Oh!** and **Riftbound**, plus other games |
| Condition | All cards are **raw / ungraded** (Ungraded condition plus the *Card Condition* descriptor) |
| Images | **Local files** on the owner's PC, uploaded via the Media API before listing |
| Seller account | Business policies are on; an eBay Store with store categories exists |
| Listing format | Fixed price |

## Architecture

```
EbaySellerTool.slnx
src/
  EbaySellerTool.Core/     Domain models, Excel import/export, validation, eBay API clients, listing workflow
  EbaySellerTool.Cli/      Console host (assembly name: ebaytool). Argument parsing and output only.
tests/
  EbaySellerTool.Tests/    xUnit tests
ai_tooling/                Guidance for AI agents (this folder)
```

**Rule:** all business logic lives in **Core** and knows nothing about the console. A future **ASP.NET Core API and Angular UI** will reuse Core as it is, so Core must stay UI-agnostic.

### Tech stack

- .NET 10, C#, nullable reference types enabled
- Microsoft.Extensions.Hosting / DependencyInjection / Configuration / Options
- `IHttpClientFactory` + `Microsoft.Extensions.Http.Resilience` for eBay calls (retries on 429/5xx)
- ClosedXML for Excel
- Spectre.Console for console output
- xUnit for tests
- Secrets stored in `dotnet user-secrets` (never committed)

## eBay integration

**Listing pipeline** (Inventory API, max 25 items per bulk call):

1. **Read and validate** the Excel sheet: required fields, price, item specifics, image files exist.
2. **Upload images**: Media API `createImageFromFile` returns eBay-hosted URLs. Cached by file hash so re-runs don't upload again.
3. **Inventory items**: `bulkCreateOrReplaceInventoryItem`, keyed by SKU (so re-runs are safe).
4. **Offers**: `bulkCreateOffer` sets price, category, store category, business policy IDs and `merchantLocationKey`.
5. **Publish**: `bulkPublishOffer` returns a `listingId` or errors for each offer.
6. **Report**: console summary plus `results_<timestamp>.xlsx` next to the input file.

**Supporting APIs**

- **OAuth**: authorization-code grant, refresh token stored locally. Scopes: `sell.inventory`, `sell.account`, plus the Media API scope.
- **Account API**: reads business policy IDs, creates the inventory location.
- **Taxonomy API**: fetches the required item specifics for a category (cached).
- **Stores**: store categories are set via `storeCategoryNames` on the offer.

**Known caveats**

- Listings created through the Inventory API should be revised through the API, not Seller Hub.
- Riftbound is a newer game, so check whether eBay AU has a `Game` aspect value for it.

**Values to verify against the live API** (from eBay docs, not yet confirmed):

- Raw cards use condition `USED_VERY_GOOD` (Ungraded, ID 4000) plus condition descriptor `40001` (Card Condition), with values `400010` NM, `400011` LP, `400012` MP, `400013` HP. See `Ebay/Inventory/EbayCardConditions.cs`.
- Aspect names `Game`, `Card Name`, `Set`, `Card Number`, `Rarity`, `Language` (`CardAspectNames.cs`). The Taxonomy API will confirm them.
- Default category `183454` (CCG Individual Cards) on EBAY_AU.

## Configuration

`src/EbaySellerTool.Cli/appsettings.json` (copied to the build output), overridden by user-secrets:

- `Ebay`: `Environment` (Sandbox/Production), `MarketplaceId` (EBAY_AU), `Currency` (AUD), `Locale` (en_AU). Bound to `EbayOptions`.
- `ListingDefaults`: `CategoryId`, `MerchantLocationKey`, `FulfillmentPolicyId`, `PaymentPolicyId`, `ReturnPolicyId`, `DescriptionTemplatePath`. Bound to `ListingDefaultsOptions`. All except the template are required for live listing; `GetMissingRequiredSettings()` reports the gaps. The `setup` command will fill these in.

## Listing pipeline (Core/Listing)

- `ListingService` splits the cards into batches of 25 and runs the `IListingStep`s in order on the still-active `ListingJob`s: `ImageUploadStep` → `InventoryItemStep` → `OfferStep` → `PublishStep`.
- Each `ListingJob` tracks one card: image URLs, offer ID, listing ID, errors, warnings and final status (`Listed`, `Revised`, `Failed`). Rows that failed validation become `Invalid`, and dry runs are `DryRun`.
- **Re-runs:** if creating an offer fails, `OfferStep` looks up an existing offer for the SKU and updates it. If that offer is already live, the update revises the listing (`Revised`); otherwise it goes on to publish.
- An `EbayApiException` (the whole request failed) fails only the jobs in that batch; later batches still run.
- eBay access goes through `IEbayInventoryClient` and `IImageUploader` (Media API). **Neither has an HTTP implementation yet**, so `ListingService` and its steps aren't registered in DI. Register them in pipeline order once the clients exist.
- `ListingRequestMapper` turns a `CardListing` into eBay `InventoryItemRequest` / `OfferRequest` DTOs (`Ebay/Inventory/Models`, serialised with `EbayJson.Options`).
- `ListingDescriptionBuilder` fills an HTML template (the embedded `Descriptions/DefaultDescriptionTemplate.html`, or `DescriptionTemplatePath`) with `{{Title}}`, `{{Game}}`, `{{CardName}}`, `{{SetName}}`, `{{CardNumber}}`, `{{Rarity}}`, `{{Language}}`, `{{Condition}}`, `{{Details}}` (list of filled-in fields) and `{{Description}}` (the row's own text). Values are HTML-encoded.
- `DryRunPlanner` builds the exact bulk requests without calling eBay. Local `file:///` URIs stand in for image URLs.
- `ListingReportWriter` writes `results_<sheet>_<timestamp>.xlsx`: Row, SKU, Title, Status (colour-coded), Listing ID, Listing URL (hyperlink), Offer ID, Errors, Warnings.

## Scan splitting (Core/Scanning)

The owner scans card **fronts** on an A4 flatbed (HP printer via HP Smart), up to 9 cards in a 3×3 grid on a **white** background. Back scanning is intentionally out of scope for now and may be added later (front and back paired by grid position).

- `CardDetector` (OpenCvSharp) builds a foreground mask: pixels that are darker than the background **or** saturated (to catch pale coloured borders). It then applies a morphological close to fill small gaps and an **open** to remove the thin shadow lines scanners leave along the glass edges; both kernel sizes scale with the image. External contours are kept when they're big enough, close to the 63×88 mm card ratio (±0.2) and rectangular enough (≥0.85).
- `CardCropper` warps each `RotatedRect` to an upright portrait image, which straightens slightly rotated cards.
- `CardReadingOrder` numbers cards row by row, left to right, tolerating uneven rows.
- `ScanSplitter` saves `<scan>_card01.jpg`, `<scan>_card02.jpg`, … (JPEG quality 95).
- `ListingSheetAppender` adds one row per image to the Excel sheet with only `Images` filled in (as a path relative to the sheet), skipping images already listed.
- **Packages:** `OpenCvSharp4` (managed) is in Core, and the native `OpenCvSharp4.runtime.win` is in Cli and Tests. A Linux host (the future web API) would need the Linux runtime package instead.
- The regression test uses a real scan at `tests/EbaySellerTool.Tests/TestData/riftbound_scan.jpg` (9 Riftbound cards, two touching the scanner's edge shadows).
- **Scanning tips for users:** 600 DPI (the CLI warns under 1600 px on the longest side), a gap between cards, and about 1 cm from the glass edges.

## CLI commands

```
ebaytool template <file.xlsx> [--force]  Generate a blank input sheet                          (done)
ebaytool validate <file.xlsx>            Check a sheet; no changes on eBay                    (done)
ebaytool split <scan|folder> [--output images] [--sheet file.xlsx]
                                         Cut scans into one image per card; add rows to sheet (done)
ebaytool list <file.xlsx> --dry-run      Write dryrun_*.json (eBay requests) + results_*.xlsx (done)
ebaytool list <file.xlsx>                Live listing                (needs the eBay HTTP clients)
ebaytool auth                            One-time OAuth login                                 (planned)
ebaytool setup                           Fetch policies, create location, cache category aspects (planned)
```

Exit codes: `0` success, `1` validation errors or failed listings, `2` invalid input (missing file, wrong extension), `3` feature not available yet.

## Excel input format

- Sheet **`Cards`** (the first sheet is used if there's no `Cards` sheet). Row 1 holds the headers; each row after it is one listing. Blank rows are skipped.
- Header matching ignores case, spaces and underscores (`Card Name` = `CardName`).
- Column definitions (the single source of truth) live in `Core/Excel/ListingColumns.cs`. The template's `Instructions` sheet is generated from them.
- **Required:** `Game`, `CardName`, `CardCondition`, `Price`, `Images`.
- **Optional:** `SKU`, `Title`, `SetName`, `CardNumber`, `Rarity`, `Language`, `Quantity` (default 1), `StoreCategory`, `CategoryId`, `Description`.
- **Extra item specifics:** any `Aspect:<Name>` column (for example `Aspect:Edition`).
- **CardCondition:** `Near Mint or Better`, `Lightly Played (Excellent)`, `Moderately Played (Very Good)`, `Heavily Played (Poor)`, or the codes `NM` / `LP` / `MP` / `HP`. The template shows these as a dropdown.
- **Images:** local paths separated by `|` (max 24). Relative paths are resolved from the Excel file's folder, and quotes from Explorer's "Copy as path" are stripped.
- **Generated values:** a blank `SKU` becomes `CARDNUMBER-RARITY-CONDITION` (deterministic, so re-runs are safe; needs `CardNumber`). A blank `Title` is built from CardName, CardNumber, Rarity, SetName, Game and the condition code, skipping any part that would push it past 80 characters.

## Import pipeline (Core)

`ListingImportService` → `IListingSheetReader` (Excel → `ListingRow`) → `ICardListingParser` (row → `CardListing`, reporting type and required-field errors) → `ICardListingValidator` (runs every `IListingRule` and `IListingBatchRule` registered in DI).

- Every problem on a row is reported together, and one bad row never stops the others.
- **To add a validation rule:** implement `IListingRule` (per listing) or `IListingBatchRule` (across listings, such as duplicate SKUs), then register it in `ServiceCollectionExtensions.AddListingValidation`.

## Features

| Feature | Status |
|---|---|
| Solution structure (Core / Cli / Tests) | Done |
| Excel template, parser and validation (`template`, `validate`) | Done |
| eBay request mapping, description template, settings | Done |
| Listing pipeline (batching, steps, per-item results, re-run handling) | Done (tested with fakes) |
| Results report (`results_*.xlsx`) and `list --dry-run` | Done |
| Scan splitting: one image per card, rows added to the sheet (`split`) | Done (fronts only) |
| Back scans paired with fronts | Future |
| HTTP clients for Inventory and Media APIs | Planned (needs developer keys) |
| OAuth and token storage | Planned |
| Setup command (policies, location, aspects) | Planned |
| Image upload via Media API | Planned |
| Bulk list pipeline and results report | Planned |
| ASP.NET Core API + Angular UI | Future |

## Changelog

- **2026-09-25**: Initial solution (Core, Cli, Tests), README, public GitHub repo `rthinh2002/EbaySellerTool`. Added `ai_tooling/` agent guidance.
- **2026-09-25**: Excel template generation, sheet reader, row parser, validation rules (title, SKU, price, quantity, images, category ID, duplicate SKUs), and the `template` / `validate` CLI commands (System.CommandLine + Spectre.Console). Auto-generated SKU and title. 43 unit tests.
- **2026-09-25**: eBay Inventory API request/response models and `ListingRequestMapper` (Ungraded condition + Card Condition descriptor, item specifics, AUD pricing, store category, policies). HTML description templates. `appsettings.json` settings. Listing pipeline (`ListingService` + image/inventory/offer/publish steps) with re-run handling, tested against a fake eBay client. Colour-coded `results_*.xlsx` report. `list --dry-run` command. 73 unit tests.
- **2026-09-25**: Visual Studio launch profiles (`samples/` working folder) and a sample sheet. `split` command: OpenCV card detection on white-background flatbed scans, straightening and cropping, reading-order numbering, and adding rows to the sheet. 83 unit tests.
