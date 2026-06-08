using ViralImpact.Api.Dtos;
using ViralImpact.Api.Entities;

namespace ViralImpact.Api.Mapping;

public static class UserMapping
{
    public static User ToEntity(this UserDto dto)
    {
        return new User
        {
            Id = dto.Id,
            UserName = dto.Username,
            Name = dto.Name,
            GroupId = dto.GroupId
        };
    }

    public static void UpdateFromDto(this User entity, UserDto dto)
    {
        entity.Id = dto.Id;
        entity.UserName = dto.Username;
        entity.Name = dto.Name;
        entity.GroupId = dto.GroupId;
    }

    public static UserDto ToDto(this User entity)
    {
        return new UserDto(
            entity.Id,
            entity.UserName ?? "",
            entity.Name,
            entity.GroupId
        );
    }
}
