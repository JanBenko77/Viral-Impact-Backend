namespace ViralImpact.Api.Dtos;

public record class CreateConversationSessionDto(
    string UserId,
    string GroupId,
    string NPCName,
    string StartAt,
    string EndAt,
    bool Outcome,
    List<CreateConversationTurnDto> Turns
);