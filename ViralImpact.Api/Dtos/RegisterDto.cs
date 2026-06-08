namespace ViralImpact.Api.Dtos;

public record class RegisterDto(
    string Username,
    string Password,
    string Name,
    string GroupId
);
