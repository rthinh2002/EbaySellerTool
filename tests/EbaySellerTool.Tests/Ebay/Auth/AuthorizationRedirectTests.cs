using EbaySellerTool.Core.Ebay.Auth;

namespace EbaySellerTool.Tests.Ebay.Auth;

public class AuthorizationRedirectTests
{
    [Fact]
    public void TryParse_AcceptUrl_DecodesCodeAndState()
    {
        const string url = "https://auth2.ebay.com/oauth2/ThirdPartyAuthSucessFailure?isAuthSuccessful=true&state=abc&code=v%5E1.1%23i%5E1%23f%3D%3D&expires_in=299";

        Assert.True(AuthorizationRedirect.TryParse(url, out var redirect));
        Assert.Equal("v^1.1#i^1#f==", redirect!.Code);
        Assert.Equal("abc", redirect.State);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a url")]
    [InlineData("https://www.ebay.com.au/?isAuthSuccessful=false")]
    public void TryParse_NoCode_ReturnsFalse(string url)
    {
        Assert.False(AuthorizationRedirect.TryParse(url, out _));
    }
}
