using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Memory;

namespace SmartSolutionsLab.Roomy.Gateway.Authentication;

// Server-side store for the cookie auth ticket (ADR-0013). With SaveTokens the ticket holds the
// access/refresh/id tokens — several KB. Keeping that in the cookie makes it huge, and because cookies
// are not isolated by port, the gateway's __Host-roomy.bff cookie is also sent to Keycloak on the same
// localhost in dev, overflowing its header limit (431 Request Header Fields Too Large). Storing the
// ticket here leaves only a small session key in the cookie. In-memory is fine for local dev (a single
// gateway instance; sessions reset on restart); a distributed store (e.g. Redis) is the production swap.
internal sealed class MemoryTicketStore(IMemoryCache cache) : ITicketStore
{
    private const string KeyPrefix = "roomy-bff-ticket:";
    private static readonly TimeSpan fallbackLifetime = TimeSpan.FromHours(8);

    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var key = KeyPrefix + Guid.NewGuid().ToString("N");
        await RenewAsync(key, ticket).ConfigureAwait(false);
        return key;
    }

    public Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        var options = new MemoryCacheEntryOptions();
        if (ticket.Properties.ExpiresUtc is { } expiresUtc)
        {
            options.SetAbsoluteExpiration(expiresUtc);
        }
        else
        {
            options.SetSlidingExpiration(fallbackLifetime);
        }

        cache.Set(key, ticket, options);
        return Task.CompletedTask;
    }

    public Task<AuthenticationTicket?> RetrieveAsync(string key) =>
        Task.FromResult(cache.Get<AuthenticationTicket>(key));

    public Task RemoveAsync(string key)
    {
        cache.Remove(key);
        return Task.CompletedTask;
    }
}
