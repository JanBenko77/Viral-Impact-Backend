using Microsoft.AspNetCore.Identity;

namespace ViralImpact.Api.Entities;

public class User : IdentityUser
{
    public string Name { get; set; } = null!;
    public string GroupId { get; set; } = null!; // The group the user belongs to, which can be used for caretakers to check if their patients
    public ICollection<GameStats> GameStats { get; set; } = new List<GameStats>();
    public ICollection<ConversationSession> Conversations { get; set; } = new List<ConversationSession>();
}
