namespace DashboardApp.Models;

public record class ConversationSessionDto(
    int Id,
    string? GroupId,
    string NPCName,
    DateTime StartAt,
    DateTime EndAt,
    bool Outcome,
    List<ConversationTurnDto> Turns
);
