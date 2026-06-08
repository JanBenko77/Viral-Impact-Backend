using System.Net.Http.Json;
using DashboardApp.Models;
using Microsoft.JSInterop;

namespace DashboardApp.Services;

public class AuthService
{
    private readonly HttpClient _http;
    private readonly IJSRuntime _js;
    private readonly CustomAuthStateProvider _authStateProvider;

    public AuthService(HttpClient http, IJSRuntime js, CustomAuthStateProvider authStateProvider)
    {
        _http = http;
        _js = js;
        _authStateProvider = authStateProvider;
    }

    public async Task<(bool Success, string? Error)> LoginAsync(string username, string password)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("staff/login", new { Username = username, Password = password });
            if (!response.IsSuccessStatusCode)
                return (false, "Invalid username or password.");

            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
            if (result?.AccessToken == null)
                return (false, "Invalid response from server.");

            await _js.InvokeVoidAsync("localStorage.setItem", "authToken", result.AccessToken);
            _authStateProvider.NotifyAuthStateChanged();
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"Connection error: {ex.Message}");
        }
    }

    public async Task LogoutAsync()
    {
        await _js.InvokeVoidAsync("localStorage.removeItem", "authToken");
        _http.DefaultRequestHeaders.Authorization = null;
        _authStateProvider.NotifyAuthStateChanged();
    }
}
