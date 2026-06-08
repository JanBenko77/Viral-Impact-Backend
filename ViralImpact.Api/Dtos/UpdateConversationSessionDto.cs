namespace ViralImpact.Api.Dtos;

public record class UpdateConversationSessionDto(
    string GroupId,
    string NPCName,
    string StartAt,
    string EndAt,
    bool Outcome,
    List<CreateConversationTurnDto> Turns
);
