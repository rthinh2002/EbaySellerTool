using EbaySellerTool.Core.Ebay.Http;

namespace EbaySellerTool.Core.Ebay.Inventory;

public sealed class EbayLocationClient(EbayRestClient restClient) : IEbayLocationClient
{
    private const string WarehouseLocationType = "WAREHOUSE";
    private const string EnabledStatus = "ENABLED";
    private const int SystemErrorId = 25001;

    public async Task<IReadOnlyList<InventoryLocation>> GetLocationsAsync(CancellationToken cancellationToken)
    {
        var response = await restClient.GetAsync<LocationsResponse>(restClient.ApiUrl("sell/inventory/v1/location"), cancellationToken);

        // The Sandbox answers "System error" (25001) instead of an empty list while an account has no locations.
        // Treating it as empty is safe: a genuine failure resurfaces when the location is then created.
        if (response.HasErrorId(SystemErrorId))
        {
            return [];
        }

        response.EnsureSuccess("Reading inventory locations");

        return [.. (response.Value?.Locations ?? []).Select(ToInventoryLocation)];
    }

    public async Task CreateLocationAsync(InventoryLocation location, CancellationToken cancellationToken)
    {
        var request = new
        {
            Location = new { Address = new LocationAddress(location.City, location.StateOrProvince, location.PostalCode, location.Country) },
            LocationTypes = new[] { WarehouseLocationType },
            location.Name,
            MerchantLocationStatus = EnabledStatus
        };

        var url = restClient.ApiUrl($"sell/inventory/v1/location/{Uri.EscapeDataString(location.MerchantLocationKey)}");
        var response = await restClient.SendJsonAsync<EmptyResponse>(HttpMethod.Post, url, request, cancellationToken);

        response.EnsureSuccess($"Creating inventory location '{location.MerchantLocationKey}'");
    }

    private static InventoryLocation ToInventoryLocation(LocationDetails details)
    {
        var address = details.Location?.Address;

        return new InventoryLocation(
            details.MerchantLocationKey,
            details.Name ?? string.Empty,
            address?.City ?? string.Empty,
            address?.StateOrProvince ?? string.Empty,
            address?.PostalCode ?? string.Empty,
            address?.Country ?? string.Empty);
    }

    private sealed record LocationsResponse(IReadOnlyList<LocationDetails>? Locations);

    private sealed record LocationDetails(string MerchantLocationKey, string? Name, LocationBody? Location);

    private sealed record LocationBody(LocationAddress? Address);

    private sealed record LocationAddress(string? City, string? StateOrProvince, string? PostalCode, string? Country);
}
