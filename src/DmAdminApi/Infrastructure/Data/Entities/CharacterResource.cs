namespace DmAdminApi.Infrastructure.Data.Entities;

public class CharacterResource
{
    public Guid Id { get; set; }
    public Guid EntityId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Current { get; set; }
    public int Max { get; set; }
    public string Color { get; set; } = "#e53935";
    public int SortOrder { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public WorldEntity Entity { get; set; } = null!;
}
