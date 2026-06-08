using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace DashboardApp.Services;

public static class JwtParser
{
    private static readonly Dictionary<string, string> ClaimTypeMap = new()
    {
        ["nameid"] = ClaimTypes.NameIdentifier,
        ["sub"] = ClaimTypes.NameIdentifier,
        ["unique_name"] = ClaimTypes.Name,
        ["role"] = ClaimTypes.Role,
    };

    public static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var padded = (payload.Length % 4) switch
        {
            2 => payload + "==",
            3 => payload + "=",
            _ => payload
        };
        var bytes = Convert.FromBase64String(padded.Replace('-', '+').Replace('_', '/'));
        var json = Encoding.UTF8.GetString(bytes);
        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;

        var claims = new List<Claim>();
        foreach (var (key, value) in dict)
        {
            var claimType = ClaimTypeMap.TryGetValue(key, out var mapped) ? mapped : key;
            if (value.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in value.EnumerateArray())
                    claims.Add(new Claim(claimType, item.GetString() ?? ""));
            }
            else
            {
                claims.Add(new Claim(claimType, value.ToString()));
            }
        }
        return claims;
    }
}
