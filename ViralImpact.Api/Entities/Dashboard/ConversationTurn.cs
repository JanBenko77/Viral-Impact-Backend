namespace ViralImpact.Api.Entities;

public class ConversationTurn
{
    public int Id { get; set; } 
    public int TurnIndex { get; set; }
    public int ConversationSessionId { get; set; } // Guid
    public string PlayerStartKey { get; set; } = null!;
    public string PlayerEndKey { get; set; } = null!;
    public string NPCLineKey { get; set; } = null!;
    public string NPCReactionKey { get; set; } = null!;
    public List<string> AllPlayerChoicesKeys { get; set; } = new();
    public int ResponseTimeSecs { get; set; }
    public ConversationSession ConversationSession { get; set; } = null!;
}
