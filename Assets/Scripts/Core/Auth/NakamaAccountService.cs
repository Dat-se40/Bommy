using Nakama;
using System;
using System.Threading.Tasks;

internal sealed class NakamaAccountService
{
    readonly IClient client;

    public IApiAccount Account { get; private set; }

    public NakamaAccountService(IClient client)
    {
        this.client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public async Task<IApiAccount> RefreshAsync(ISession session)
    {
        Account = await client.GetAccountAsync(RequireSession(session));
        return Account;
    }

    public async Task<IApiAccount> UpdateUsernameAsync(ISession session, string username)
    {
        await client.UpdateAccountAsync(RequireSession(session), username);
        return await RefreshAsync(session);
    }

    public async Task<IApiAccount> UpdateDisplayNameAsync(ISession session, string displayName)
    {
        session = RequireSession(session);
        string username = Account?.User?.Username ?? session.Username;
        await client.UpdateAccountAsync(session, username, displayName);
        return await RefreshAsync(session);
    }

    public string GetDisplayName(ISession session)
    {
        string displayName = Account?.User?.DisplayName;

        if (!string.IsNullOrWhiteSpace(displayName))
            return displayName.Trim();

        string username = Account?.User?.Username ?? session?.Username;
        return string.IsNullOrWhiteSpace(username) ? General.AUTH_DEFAULT_DISPLAY_NAME : username;
    }

    public void Clear()
    {
        Account = null;
    }

    static ISession RequireSession(ISession session)
    {
        return session ?? throw new InvalidOperationException("Nakama authentication is not available.");
    }
}
