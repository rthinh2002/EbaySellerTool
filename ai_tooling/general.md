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

## CLI commands

```
ebaytool template <file.xlsx> [--force]  Generate a blank input sheet           (done)
ebaytool validate <file.xlsx>            Check a sheet; no changes on eBay     (done)
ebaytool auth                            One-time OAuth login                  (planned)
ebaytool setup                           Fetch policies, create location, cache category aspects (planned)
ebaytool list <file.xlsx> [--sandbox]                                          (planned)
```

Exit codes: `0` success, `1` validation errors found, `2` invalid input (missing file, wrong extension).

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
| OAuth and token storage | Planned |
| Setup command (policies, location, aspects) | Planned |
| Image upload via Media API | Planned |
| Bulk list pipeline and results report | Planned |
| ASP.NET Core API + Angular UI | Future |

## Changelog

- **2026-09-25**: Initial solution (Core, Cli, Tests), README, public GitHub repo `rthinh2002/EbaySellerTool`. Added `ai_tooling/` agent guidance.
- **2026-09-25**: Excel template generation, sheet reader, row parser, validation rules (title, SKU, price, quantity, images, category ID, duplicate SKUs), and the `template` / `validate` CLI commands (System.CommandLine + Spectre.Console). Auto-generated SKU and title. 43 unit tests.
