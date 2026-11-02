namespace DmAdminApi.Features.Tables.Dtos;

public record TableEntryDto(Guid Id, string Text, int Weight);
public record RollableTableDto(Guid Id, string Name, string Description,
                               string CreatedByName, DateTimeOffset CreatedAt,
                               List<TableEntryDto> Entries);
public record CreateTableDto(string Name, string Description);
public record UpdateTableDto(string Name, string Description);
public record CreateEntryDto(string Text, int Weight);
public record UpdateEntryDto(string Text, int Weight);
public record TableRollResultDto(Guid TableId, string TableName, TableEntryDto Entry);
