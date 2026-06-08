namespace ViralImpact.Api.Entities;

public class GameStats
{
    public int Id { get; set; } 
    public string UserId { get; set; } = null!;
    public int monstersKilled { get; set; }
    public int deaths { get; set; }
    public int levelsCompleted { get; set; }
    public int conversationsCompleted { get; set; }

    public User User { get; set; } = null!;
}