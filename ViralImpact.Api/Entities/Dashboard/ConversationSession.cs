namespace ViralImpact.Api.Entities;

public class ConversationSession
{
    public int Id { get; set; } //Guid
    public string UserId { get; set; } = null!;
    public string GroupId { get; set; } = null!;
    public string NPCName { get; set; } = null!;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public bool Outcome { get; set; } //win or lose
    public required List<ConversationTurn> Turns { get; set; }

    public User User { get; set; } = null!;
}

//filter SQLite by linking tables and querying ConversationTurn for 
//PlayerStartKey/PlayerEndKey/NPCLineKey/NPCReactionKey/AllPlayerChoicesKeys,
//then get ConversationSessionId to retrieve ConversationSession data