using FSEdu.Shared.Contracts.Auth;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace FSEdu.Web.Services;

public sealed class AuthState
{
    private readonly ProtectedSessionStorage _storage;
    private readonly ApiClient _api;

    private const string TokenKey = "fsedu.access_token";
    private const string RefreshKey = "fsedu.refresh_token";
    private const string UserKey = "fsedu.user";

    public AuthState(ProtectedSessionStorage storage, ApiClient api)
    {
        _storage = storage;
        _api = api;
    }

    public UserSummary? CurrentUser { get; private set; }
    public string? AccessToken { get; private set; }

    public bool IsAuthenticated => !string.IsNullOrEmpty(AccessToken);

    public bool IsInRole(string role) =>
        CurrentUser?.Roles.Contains(role) ?? false;

    public event Action? OnChange;

    public async Task SaveAsync(AuthResponse resp)
    {
        AccessToken = resp.AccessToken;
        CurrentUser = resp.User;
        _api.SetAuthToken(resp.AccessToken);

        await _storage.SetAsync(TokenKey, resp.AccessToken);
        await _storage.SetAsync(RefreshKey, resp.RefreshToken);
        await _storage.SetAsync(UserKey, resp.User);

        OnChange?.Invoke();
    }

    public async Task RestoreAsync()
    {
        try
        {
            var tokenResult = await _storage.GetAsync<string>(TokenKey);
            var userResult = await _storage.GetAsync<UserSummary>(UserKey);

            if (tokenResult.Success && userResult.Success)
            {
                AccessToken = tokenResult.Value;
                CurrentUser = userResult.Value;
                _api.SetAuthToken(AccessToken);
                OnChange?.Invoke();
            }
        }
        catch { /* session expired or unavailable */ }
    }

    public async Task UpdateAvatarLocallyAsync(string? avatarUrl)
    {
        if (CurrentUser is null) return;
        CurrentUser = CurrentUser with { AvatarUrl = avatarUrl };
        await _storage.SetAsync(UserKey, CurrentUser);
        OnChange?.Invoke();
    }

    public async Task LogoutAsync()
    {
        AccessToken = null;
        CurrentUser = null;
        _api.SetAuthToken(null);

        await _storage.DeleteAsync(TokenKey);
        await _storage.DeleteAsync(RefreshKey);
        await _storage.DeleteAsync(UserKey);

        OnChange?.Invoke();
    }
}
