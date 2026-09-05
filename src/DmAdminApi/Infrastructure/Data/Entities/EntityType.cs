namespace DmAdminApi.Infrastructure.Data.Entities;

public class EntityType
{
    public Guid Id { get; set; }
    public Guid? CampaignId { get; set; }   // null = sistema global
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public bool IsSystemDefault { get; set; }

    public Campaign? Campaign { get; set; }
    public ICollection<EntityTypeField> Fields { get; set; } = [];
    public ICollection<WorldEntity> Entities { get; set; } = [];
}

public static class SystemEntityTypes
{
    public static readonly Guid PersonajeId = new("11111111-0000-0000-0000-000000000001");
    public static readonly Guid LugarId     = new("11111111-0000-0000-0000-000000000002");
    public static readonly Guid FaccionId   = new("11111111-0000-0000-0000-000000000003");
    public static readonly Guid ObjetoId    = new("11111111-0000-0000-0000-000000000004");
    public static readonly Guid EventoId    = new("11111111-0000-0000-0000-000000000005");
}
