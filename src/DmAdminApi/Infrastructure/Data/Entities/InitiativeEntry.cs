namespace DmAdminApi.Infrastructure.Data.Entities;

public class InitiativeEntry
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public Guid? TokenId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Initiative { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = false;
    public DateTimeOffset CreatedAt { get; set; }

    public GameSession Session { get; set; } = null!;
    public MapToken? Token { get; set; }
}
