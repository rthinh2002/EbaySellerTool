# Code Style Guide

> **For AI agents:** follow this guide for all code in this repository. Aim for code that is **readable, reusable and easy to extend**. Code that is simple for the next person to change beats code that is clever.

## Core principles

- **Clean Code:** names show intent, functions are small, each unit does one thing.
- **SOLID**
  - *Single Responsibility:* a class has one reason to change. For example, the Excel parser doesn't call eBay.
  - *Open/Closed:* extend through new implementations, not by editing `switch` statements scattered around the code.
  - *Liskov Substitution:* implementations must honour their interface's contract.
  - *Interface Segregation:* prefer small, focused interfaces (`IImageUploader`, not `IEbayEverything`).
  - *Dependency Inversion:* depend on abstractions and inject them. Never `new` up services inside logic.
- **DRY:** extract repeated logic, but only once there is real duplication. Don't abstract early.
- **KISS:** choose the simplest design that works.
- **YAGNI:** don't build features or extension points nobody needs yet. Keep the design *open* to extension without *building* the extension.
- **Composition over inheritance.**

## Comments

Good code explains itself. Comments should be rare.

- **Don't** comment *what* the code does. Rename or extract a method instead.
  ```csharp
  // Bad
  // check if card has images
  if (card.ImagePaths.Count > 0) { ... }

  // Good
  if (card.HasImages) { ... }
  ```
- **Do** comment *why* when the reason isn't obvious: eBay API quirks, workarounds, business rules, and non-obvious algorithms.
  ```csharp
  // eBay rejects bulk requests with more than 25 items.
  private const int MaxBulkBatchSize = 25;
  ```
- XML doc comments (`///`) only on public APIs in Core whose behaviour isn't obvious from the signature.
- No commented-out code, no change-log comments, no `// TODO` without a clear follow-up.

## Naming (C# conventions)

| Element | Convention | Example |
|---|---|---|
| Classes, records, methods, properties | PascalCase | `ListingService`, `PublishOffersAsync` |
| Interfaces | `I` + PascalCase | `IInventoryClient` |
| Local variables, parameters | camelCase | `offerId` |
| Private fields | `_camelCase` | `_httpClient` |
| Constants | PascalCase | `MaxBulkBatchSize` |
| Async methods | `Async` suffix | `UploadImageAsync` |

- Use full, meaningful words: `listingResult`, not `lr` or `res`.
- Booleans read as questions: `IsValid`, `HasImages`, `CanPublish`.
- Methods are verbs (`BuildOffer`); classes are nouns (`OfferBuilder`).
- Use domain language consistently: *Card*, *Listing*, *Offer*, *InventoryItem*, *SKU*, *Aspect*.

## Functions and classes

- Keep methods short, ideally under about 20 lines. Extract well-named helpers.
- Use **guard clauses** and early returns instead of deep nesting.
- Take at most about 3–4 parameters. Beyond that, introduce a parameter object or record.
- Avoid boolean flag parameters that switch behaviour. Write two methods instead.
- Replace magic numbers and strings with named constants, enums or configuration.
- Keep classes cohesive. If a class name needs "And" or "Manager", split it.

## C# / .NET specifics

- File-scoped namespaces; one public type per file; the file name matches the type.
- Nullable reference types are on. Don't suppress warnings with `!` without a clear reason.
- Use `record` for immutable data and DTOs; `required` and `init` for mandatory properties.
- Prefer `IReadOnlyList<T>` / `IReadOnlyCollection<T>` for exposed collections.
- Use `var` when the type is obvious from the right-hand side.
- Async all the way: `async`/`await`, no `.Result` or `.Wait()`. Pass a `CancellationToken` through every async call chain.
- Use dependency injection through `Microsoft.Extensions.DependencyInjection`. Register services in one extension method per project (for example `services.AddEbaySellerToolCore(configuration)`).
- Use the **Options pattern** (`IOptions<T>`) for configuration. Never read `IConfiguration` inside business logic.
- Use `IHttpClientFactory` or typed clients for HTTP. Never create `new HttpClient()` per call.
- Log through `ILogger<T>` with structured templates: `_logger.LogInformation("Published {Sku} as {ListingId}", sku, listingId)`.
- Use `System.Text.Json`, with `[JsonPropertyName]` where eBay's naming differs.

## Error handling

- **Expected failures** (a bad Excel row, a rejected offer) are *data*, not exceptions. Return result types (for example `ValidationResult`, `ListingResult`) so one failed card never stops the batch.
- **Unexpected failures** (network down, auth expired) throw exceptions. Catch them only where you can add context or recover. Never swallow exceptions silently.
- Error messages are specific and actionable. Include the SKU or row number and eBay's error message.

## Architecture rules

- **Core** has no dependency on console, Spectre.Console or any UI. It must work under a future ASP.NET Core API.
- **Cli** is thin: parse arguments, call Core services, render results.
- Separate concerns into folders or namespaces by feature, for example `Excel/`, `Validation/`, `Ebay/Inventory/`, `Ebay/Media/`, `Listing/`.
- Keep eBay API DTOs apart from domain models. Map between them in dedicated mappers so API changes don't leak into the domain.
- Never hardcode secrets. Use user-secrets or environment variables.

## Testing

- xUnit. Test names follow `MethodName_Scenario_ExpectedResult`.
- Arrange / Act / Assert structure, one behaviour per test.
- Test Core logic (parsing, validation, mapping, batching) without real HTTP. Fake the interfaces.
- New features and bug fixes come with tests.

## Before finishing a change

- [ ] `dotnet build` has no warnings or errors, and `dotnet test` passes.
- [ ] Names are clear; no dead code; comments only where they explain *why*.
- [ ] Core stays UI-agnostic.
- [ ] `ai_tooling/general.md` is updated if features or architecture changed.
