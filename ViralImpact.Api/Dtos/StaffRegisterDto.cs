namespace ViralImpact.Api.Dtos;

public record class StaffRegisterDto(
    string Username,
    string Password,
    string Name,
    string StaffRole,   // "Admin", "Scientist", "Caretaker"
    string? GroupId     // required when StaffRole is "Caretaker", null otherwise
);
