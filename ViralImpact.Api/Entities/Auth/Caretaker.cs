using Microsoft.AspNetCore.Identity;

namespace ViralImpact.Api.Entities;

public class Caretaker : IdentityUser
{
    public string Name { get; set; } = null!;
    public string? GroupId { get; set; }   // null for Admin and Scientist roles
    public string StaffRole { get; set; } = "Caretaker"; // "Admin", "Scientist", "Caretaker"
}
