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

## Planned CLI commands

```
ebaytool auth                  One-time OAuth login
ebaytool setup                 Fetch policies, create location, cache category aspects
ebaytool template <file.xlsx>  Generate a blank input sheet
ebaytool validate <file.xlsx>  Dry run with no changes on eBay
ebaytool list <file.xlsx> [--sandbox]
```

## Features

| Feature | Status |
|---|---|
| Solution structure (Core / Cli / Tests) | Done |
| Excel template, parser and validation | Planned |
| OAuth and token storage | Planned |
| Setup command (policies, location, aspects) | Planned |
| Image upload via Media API | Planned |
| Bulk list pipeline and results report | Planned |
| ASP.NET Core API + Angular UI | Future |

## Changelog

- **2026-09-25**: Initial solution (Core, Cli, Tests), README, public GitHub repo `rthinh2002/EbaySellerTool`. Added `ai_tooling/` agent guidance.
