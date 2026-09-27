using System.Net;
using EbaySellerTool.Core.Ebay.Account;
using EbaySellerTool.Core.Ebay.Inventory;
using EbaySellerTool.Tests.TestSupport;

namespace EbaySellerTool.Tests.Ebay;

public class EbayAccountAndLocationClientTests
{
    private readonly EbayClientTestContext _context = new();

    [Fact]
    public async Task GetPoliciesAsync_ReadsPoliciesOfRequestedType()
    {
        _context.Http.RespondWith(HttpStatusCode.OK, """
            {"total":2,"fulfillmentPolicies":[{"fulfillmentPolicyId":"1","name":"Standard"},{"fulfillmentPolicyId":"2","name":"Express"}]}
            """);

        var policies = await CreateAccountClient().GetPoliciesAsync(BusinessPolicyType.Fulfillment, CancellationToken.None);

        Assert.Equal(["Standard", "Express"], policies.Select(policy => policy.Name));
        Assert.EndsWith("sell/account/v1/fulfillment_policy?marketplace_id=EBAY_AU", _context.Http.Requests.Single().Request.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task GetPoliciesAsync_NotOptedIn_ThrowsBusinessPoliciesNotEnabled()
    {
        _context.Http.RespondWith(HttpStatusCode.BadRequest, """
            {"errors":[{"errorId":20403,"message":"Invalid .","longMessage":"User is not eligible for Business Policy."}]}
            """);

        await Assert.ThrowsAsync<BusinessPoliciesNotEnabledException>(
            () => CreateAccountClient().GetPoliciesAsync(BusinessPolicyType.Payment, CancellationToken.None));
    }

    [Fact]
    public async Task CreatePolicyAsync_SandboxTestPolicy_ReturnsCreatedPolicy()
    {
        _context.Http.RespondWith(HttpStatusCode.Created, """{"name":"EbaySellerTool test return","returnPolicyId":"99"}""");
        var body = SandboxTestPolicies.Create(BusinessPolicyType.Return, _context.Options.Value);

        var policy = await CreateAccountClient().CreatePolicyAsync(BusinessPolicyType.Return, body, CancellationToken.None);

        Assert.Equal(new BusinessPolicy(BusinessPolicyType.Return, "99", "EbaySellerTool test return"), policy);
        Assert.Contains("\"returnsAccepted\":true", _context.Http.Requests.Single().Body);
    }

    [Fact]
    public async Task GetLocationsAsync_ReadsLocations()
    {
        _context.Http.RespondWith(HttpStatusCode.OK, """
            {"total":1,"locations":[{"merchantLocationKey":"home","name":"Home","location":{"address":{"city":"Sydney","stateOrProvince":"NSW","postalCode":"2000","country":"AU"}}}]}
            """);

        var location = Assert.Single(await new EbayLocationClient(_context.RestClient).GetLocationsAsync(CancellationToken.None));

        Assert.Equal(new InventoryLocation("home", "Home", "Sydney", "NSW", "2000", "AU"), location);
    }

    [Fact]
    public async Task GetLocationsAsync_SandboxSystemErrorForNoLocations_ReturnsEmpty()
    {
        _context.Http.RespondWith(HttpStatusCode.InternalServerError, """{"errors":[{"errorId":25001,"message":"System error."}]}""");

        Assert.Empty(await new EbayLocationClient(_context.RestClient).GetLocationsAsync(CancellationToken.None));
    }

    [Fact]
    public async Task CreateLocationAsync_PostsWarehouseAddressUnderKey()
    {
        _context.Http.RespondWith(HttpStatusCode.NoContent, string.Empty);

        await new EbayLocationClient(_context.RestClient).CreateLocationAsync(
            new InventoryLocation("home", "Home", "Sydney", "NSW", "2000", "AU"), CancellationToken.None);

        var (request, body) = _context.Http.Requests.Single();
        Assert.EndsWith("sell/inventory/v1/location/home", request.RequestUri!.AbsoluteUri);
        Assert.Equal(
            """{"location":{"address":{"city":"Sydney","stateOrProvince":"NSW","postalCode":"2000","country":"AU"}},"locationTypes":["WAREHOUSE"],"name":"Home","merchantLocationStatus":"ENABLED"}""",
            body);
    }

    private EbayAccountClient CreateAccountClient() => new(_context.RestClient, _context.Options);
}
