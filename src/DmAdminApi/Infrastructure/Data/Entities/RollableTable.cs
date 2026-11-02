namespace DmAdminApi.Infrastructure.Data.Entities;

public class RollableTable
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Campaign Campaign { get; set; } = null!;
    public User Creator { get; set; } = null!;
    public ICollection<TableEntry> Entries { get; set; } = [];
}

public class TableEntry
{
    public Guid Id { get; set; }
    public Guid TableId { get; set; }
    public string Text { get; set; } = string.Empty;
    public int Weight { get; set; } = 1;

    public RollableTable Table { get; set; } = null!;
}
