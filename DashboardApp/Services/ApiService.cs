using System.Net.Http.Json;
using DashboardApp.Models;

namespace DashboardApp.Services;

public class ApiService
{
    private readonly HttpClient _http;

    public ApiService(HttpClient http) => _http = http;

    public Task<List<UserDto>?> GetUsersAsync() =>
        _http.GetFromJsonAsync<List<UserDto>>("dashboard/users");

    public Task<UserDto?> GetUserAsync(string id) =>
        _http.GetFromJsonAsync<UserDto>($"dashboard/users/{id}");

    public Task<List<GameStatsDto>?> GetUserStatsAsync(string id) =>
        _http.GetFromJsonAsync<List<GameStatsDto>>($"dashboard/users/{id}/stats");

    public Task<List<ConversationSessionDto>?> GetUserConversationsAsync(string id) =>
        _http.GetFromJsonAsync<List<ConversationSessionDto>>($"dashboard/users/{id}/conversations");

    public Task<List<string>?> GetGroupsAsync() =>
        _http.GetFromJsonAsync<List<string>>("dashboard/groups");

    public Task<Dictionary<string, string>?> GetLocalizationAsync(string language) =>
        _http.GetFromJsonAsync<Dictionary<string, string>>($"localization/{language}");
}
