namespace ViralImpact.Api.Dtos;

public record class AuthResponseDto(
    string UserId,
    string Username,
    string Name,
    string AccessToken
);
