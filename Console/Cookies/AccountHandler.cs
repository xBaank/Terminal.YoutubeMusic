namespace Console.Cookies;

internal class AccountHandler(int? accountIndex) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        if (accountIndex is not null)
        {
            request.Headers.Add("X-Goog-AuthUser", accountIndex.ToString());
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
