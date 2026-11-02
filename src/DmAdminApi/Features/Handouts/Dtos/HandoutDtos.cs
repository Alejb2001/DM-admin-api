namespace DmAdminApi.Features.Handouts.Dtos;

public record HandoutDto(Guid Id, string Title, string Type, string Content, bool IsRevealed,
                         string CreatedByName, DateTimeOffset CreatedAt);
public record CreateHandoutDto(string Title, string Type, string Content);
public record UpdateHandoutDto(string Title, string Type, string Content);
