using System.CommandLine;
using EbaySellerTool.Cli.Commands;
using EbaySellerTool.Cli.Rendering;
using EbaySellerTool.Cli.Storage;
using EbaySellerTool.Core.Configuration;
using EbaySellerTool.Core.DependencyInjection;
using EbaySellerTool.Core.Ebay.Auth;
using EbaySellerTool.Core.Ebay.Media;
using EbaySellerTool.Core.Ebay.Stores;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { ContentRootPath = AppContext.BaseDirectory });
builder.Configuration.AddUserSecrets<Program>(optional: true);

var environment = builder.Configuration.GetValue($"{EbayOptions.SectionName}:{nameof(EbayOptions.Environment)}", EbayEnvironment.Sandbox);
builder.Configuration.AddJsonFile(UserSettingsFile.PathFor(environment), optional: true);

builder.Logging.SetMinimumLevel(LogLevel.Warning);
builder.Logging.AddFilter("Polly", LogLevel.Error);

builder.Services.AddEbaySellerToolCore(builder.Configuration);
builder.Services.AddSingleton<ITokenStore, ProtectedFileTokenStore>();
builder.Services.AddSingleton<IImageUrlCache, JsonFileImageUrlCache>();
builder.Services.AddSingleton<IStoreCategoryCache, JsonFileStoreCategoryCache>();
builder.Services.AddSingleton<ImportResultRenderer>();
builder.Services.AddSingleton<ListingRunRenderer>();
builder.Services.AddSingleton<ScanSplitRenderer>();
builder.Services.AddSingleton<ICliCommand, AuthCommand>();
builder.Services.AddSingleton<ICliCommand, SetupCommand>();
builder.Services.AddSingleton<ICliCommand, StoreCategoriesCommand>();
builder.Services.AddSingleton<ICliCommand, TemplateCommand>();
builder.Services.AddSingleton<ICliCommand, ValidateCommand>();
builder.Services.AddSingleton<ICliCommand, ListCommand>();
builder.Services.AddSingleton<ICliCommand, SplitCommand>();

using var host = builder.Build();

var rootCommand = new RootCommand("Bulk-list TCG cards on eBay from an Excel sheet.");
foreach (var command in host.Services.GetServices<ICliCommand>())
{
    rootCommand.Subcommands.Add(command.Build());
}

return await rootCommand.Parse(args).InvokeAsync();
