using DmAdminApi.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DmAdminApi.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<CampaignRole> CampaignRoles => Set<CampaignRole>();
    public DbSet<CampaignMember> CampaignMembers => Set<CampaignMember>();
    public DbSet<CampaignInvitation> CampaignInvitations => Set<CampaignInvitation>();
    public DbSet<EntityType> EntityTypes => Set<EntityType>();
    public DbSet<EntityTypeField> EntityTypeFields => Set<EntityTypeField>();
    public DbSet<WorldEntity> WorldEntities => Set<WorldEntity>();
    public DbSet<EntityPermission> EntityPermissions => Set<EntityPermission>();
    public DbSet<RelationshipType> RelationshipTypes => Set<RelationshipType>();
    public DbSet<EntityRelationship> EntityRelationships => Set<EntityRelationship>();
    public DbSet<EntityChangeLog> EntityChangeLogs => Set<EntityChangeLog>();
    public DbSet<GameSession> GameSessions => Set<GameSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<SessionScene> SessionScenes => Set<SessionScene>();
    public DbSet<MapToken> MapTokens => Set<MapToken>();
    public DbSet<CharacterResource> CharacterResources => Set<CharacterResource>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        // ── Users ────────────────────────────────────────────────────────────
        m.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Email).IsRequired().HasMaxLength(256);
            e.Property(u => u.PasswordHash).IsRequired();
            e.Property(u => u.DisplayName).IsRequired().HasMaxLength(100);
            e.Property(u => u.AvatarUrl).HasMaxLength(500);
            e.Property(u => u.SubscriptionTier).IsRequired().HasMaxLength(20).HasDefaultValue("free");
            e.Property(u => u.StripeCustomerId).HasMaxLength(100);
            e.Property(u => u.CreatedAt).HasDefaultValueSql("now()");
        });

        // ── RefreshTokens ─────────────────────────────────────────────────────
        m.Entity<RefreshToken>(e =>
        {
            e.HasKey(rt => rt.Id);
            e.Property(rt => rt.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(rt => rt.Token).IsRequired().HasMaxLength(512);
            e.HasIndex(rt => rt.Token).IsUnique();
            e.Property(rt => rt.CreatedAt).HasDefaultValueSql("now()");
            e.HasOne(rt => rt.User)
             .WithMany(u => u.RefreshTokens)
             .HasForeignKey(rt => rt.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Campaigns ─────────────────────────────────────────────────────────
        m.Entity<Campaign>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(c => c.Name).IsRequired().HasMaxLength(200);
            e.Property(c => c.JoinCode).IsRequired().HasMaxLength(8)
             .HasDefaultValueSql("upper(substring(md5(random()::text), 1, 8))");
            e.HasIndex(c => c.JoinCode).IsUnique();
            e.Property(c => c.CreatedAt).HasDefaultValueSql("now()");
            e.HasOne(c => c.Owner)
             .WithMany()
             .HasForeignKey(c => c.OwnerId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── CampaignRoles ─────────────────────────────────────────────────────
        m.Entity<CampaignRole>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(r => r.Name).IsRequired().HasMaxLength(100);
            e.HasOne(r => r.Campaign)
             .WithMany(c => c.Roles)
             .HasForeignKey(r => r.CampaignId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── CampaignMembers ───────────────────────────────────────────────────
        m.Entity<CampaignMember>(e =>
        {
            e.HasKey(cm => cm.Id);
            e.Property(cm => cm.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(cm => new { cm.CampaignId, cm.UserId }).IsUnique();
            e.Property(cm => cm.JoinedAt).HasDefaultValueSql("now()");
            e.HasOne(cm => cm.Campaign)
             .WithMany(c => c.Members)
             .HasForeignKey(cm => cm.CampaignId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(cm => cm.User)
             .WithMany()
             .HasForeignKey(cm => cm.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(cm => cm.Role)
             .WithMany(r => r.Members)
             .HasForeignKey(cm => cm.RoleId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── CampaignInvitations ───────────────────────────────────────────────
        m.Entity<CampaignInvitation>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(i => i.Token).IsUnique();
            e.Property(i => i.Token).IsRequired().HasMaxLength(100);
            e.Property(i => i.CreatedAt).HasDefaultValueSql("now()");
            e.HasOne(i => i.Campaign)
             .WithMany(c => c.Invitations)
             .HasForeignKey(i => i.CampaignId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(i => i.Role)
             .WithMany()
             .HasForeignKey(i => i.RoleId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── EntityTypes ───────────────────────────────────────────────────────
        m.Entity<EntityType>(e =>
        {
            e.HasKey(et => et.Id);
            e.Property(et => et.Name).IsRequired().HasMaxLength(100);
            e.Property(et => et.Icon).HasMaxLength(50);
            e.Property(et => et.Color).HasMaxLength(20);
            e.HasOne(et => et.Campaign)
             .WithMany()
             .HasForeignKey(et => et.CampaignId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Cascade);

            // Seed the 5 global system entity types
            e.HasData(
                new EntityType { Id = SystemEntityTypes.PersonajeId, Name = "Personaje", Icon = "person",     Color = "#3F51B5", IsSystemDefault = true },
                new EntityType { Id = SystemEntityTypes.LugarId,     Name = "Lugar",     Icon = "place",      Color = "#4CAF50", IsSystemDefault = true },
                new EntityType { Id = SystemEntityTypes.FaccionId,   Name = "Facción",   Icon = "groups",     Color = "#FF9800", IsSystemDefault = true },
                new EntityType { Id = SystemEntityTypes.ObjetoId,    Name = "Objeto",    Icon = "inventory_2",Color = "#9C27B0", IsSystemDefault = true },
                new EntityType { Id = SystemEntityTypes.EventoId,    Name = "Evento",    Icon = "event",      Color = "#F44336", IsSystemDefault = true }
            );
        });

        // ── EntityTypeFields ──────────────────────────────────────────────────
        m.Entity<EntityTypeField>(e =>
        {
            e.HasKey(f => f.Id);
            e.Property(f => f.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(f => f.Name).IsRequired().HasMaxLength(100);
            e.Property(f => f.FieldType).IsRequired().HasMaxLength(20);
            e.HasOne(f => f.EntityType)
             .WithMany(et => et.Fields)
             .HasForeignKey(f => f.EntityTypeId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── WorldEntities ─────────────────────────────────────────────────────
        m.Entity<WorldEntity>(e =>
        {
            e.HasKey(we => we.Id);
            e.Property(we => we.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(we => we.Name).IsRequired().HasMaxLength(200);
            e.Property(we => we.Slug).IsRequired().HasMaxLength(200);
            e.HasIndex(we => new { we.CampaignId, we.Slug }).IsUnique();
            e.Property(we => we.CustomFields).HasColumnType("jsonb");
            e.Property(we => we.CreatedAt).HasDefaultValueSql("now()");
            e.Property(we => we.UpdatedAt).HasDefaultValueSql("now()");
            e.HasOne(we => we.Campaign)
             .WithMany(c => c.Entities)
             .HasForeignKey(we => we.CampaignId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(we => we.EntityType)
             .WithMany(et => et.Entities)
             .HasForeignKey(we => we.EntityTypeId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(we => we.Creator)
             .WithMany()
             .HasForeignKey(we => we.CreatedBy)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── EntityPermissions ─────────────────────────────────────────────────
        m.Entity<EntityPermission>(e =>
        {
            e.HasKey(ep => ep.Id);
            e.Property(ep => ep.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(ep => new { ep.EntityId, ep.RoleId }).IsUnique();
            e.HasOne(ep => ep.Entity)
             .WithMany(we => we.Permissions)
             .HasForeignKey(ep => ep.EntityId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(ep => ep.Role)
             .WithMany(r => r.EntityPermissions)
             .HasForeignKey(ep => ep.RoleId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── RelationshipTypes ─────────────────────────────────────────────────
        m.Entity<RelationshipType>(e =>
        {
            e.HasKey(rt => rt.Id);
            e.Property(rt => rt.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(rt => rt.LabelForward).IsRequired().HasMaxLength(100);
            e.Property(rt => rt.LabelInverse).IsRequired().HasMaxLength(100);
            e.HasOne(rt => rt.Campaign)
             .WithMany()
             .HasForeignKey(rt => rt.CampaignId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(rt => rt.SourceType)
             .WithMany()
             .HasForeignKey(rt => rt.SourceTypeId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(rt => rt.TargetType)
             .WithMany()
             .HasForeignKey(rt => rt.TargetTypeId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── EntityChangeLogs ──────────────────────────────────────────────────
        m.Entity<EntityChangeLog>(e =>
        {
            e.HasKey(cl => cl.Id);
            e.Property(cl => cl.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(cl => cl.UserDisplayName).IsRequired().HasMaxLength(100);
            e.Property(cl => cl.ChangedAt).HasDefaultValueSql("now()");
            e.Property(cl => cl.FieldChanged).HasMaxLength(50);
            e.HasOne(cl => cl.Entity)
             .WithMany()
             .HasForeignKey(cl => cl.EntityId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(cl => cl.ChangedBy)
             .WithMany()
             .HasForeignKey(cl => cl.UserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── EntityRelationships ───────────────────────────────────────────────
        m.Entity<EntityRelationship>(e =>
        {
            e.HasKey(er => er.Id);
            e.Property(er => er.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasOne(er => er.SourceEntity)
             .WithMany()
             .HasForeignKey(er => er.SourceEntityId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(er => er.TargetEntity)
             .WithMany()
             .HasForeignKey(er => er.TargetEntityId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(er => er.RelationshipType)
             .WithMany(rt => rt.Relationships)
             .HasForeignKey(er => er.RelationshipTypeId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── GameSessions ──────────────────────────────────────────────────────
        m.Entity<GameSession>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(s => s.Name).IsRequired().HasMaxLength(200);
            e.Property(s => s.Status).IsRequired().HasMaxLength(20).HasDefaultValue("active");
            e.Property(s => s.StartedAt).HasDefaultValueSql("now()");
            e.HasIndex(s => new { s.CampaignId, s.StartedAt });
            e.HasOne(s => s.Campaign)
             .WithMany()
             .HasForeignKey(s => s.CampaignId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(s => s.CreatedBy)
             .WithMany()
             .HasForeignKey(s => s.CreatedById)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── ChatMessages ──────────────────────────────────────────────────────
        m.Entity<ChatMessage>(e =>
        {
            e.HasKey(cm => cm.Id);
            e.Property(cm => cm.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(cm => cm.Type).IsRequired().HasMaxLength(20);
            e.Property(cm => cm.Content).IsRequired();
            e.Property(cm => cm.DiceResultJson).HasColumnName("dice_result").HasColumnType("jsonb");
            e.HasIndex(cm => new { cm.SessionId, cm.CreatedAt });
            e.HasOne(cm => cm.Session)
             .WithMany(s => s.Messages)
             .HasForeignKey(cm => cm.SessionId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(cm => cm.User)
             .WithMany()
             .HasForeignKey(cm => cm.UserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── SessionScenes ─────────────────────────────────────────────────────
        m.Entity<SessionScene>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(s => s.Name).IsRequired().HasMaxLength(200);
            e.Property(s => s.GridSize).HasDefaultValue(50);
            e.Property(s => s.GridEnabled).HasDefaultValue(true);
            e.Property(s => s.IsActive).HasDefaultValue(false);
            e.Property(s => s.CreatedAt).HasDefaultValueSql("now()");
            e.HasIndex(s => s.SessionId);
            e.HasOne(s => s.Session)
             .WithMany()
             .HasForeignKey(s => s.SessionId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── CharacterResources ────────────────────────────────────────────────
        m.Entity<CharacterResource>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(r => r.Name).IsRequired().HasMaxLength(100);
            e.Property(r => r.Color).IsRequired().HasMaxLength(20).HasDefaultValue("#e53935");
            e.Property(r => r.UpdatedAt).HasDefaultValueSql("now()");
            e.HasIndex(r => r.EntityId);
            e.HasOne(r => r.Entity)
             .WithMany()
             .HasForeignKey(r => r.EntityId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── MapTokens ─────────────────────────────────────────────────────────
        m.Entity<MapToken>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(t => t.Label).IsRequired().HasMaxLength(100);
            e.Property(t => t.Color).IsRequired().HasMaxLength(20).HasDefaultValue("#7E57C2");
            e.Property(t => t.Width).HasDefaultValue(1);
            e.Property(t => t.Height).HasDefaultValue(1);
            e.Property(t => t.IsVisible).HasDefaultValue(true);
            e.Property(t => t.UpdatedAt).HasDefaultValueSql("now()");
            e.HasIndex(t => t.SceneId);
            e.HasOne(t => t.Scene)
             .WithMany(s => s.Tokens)
             .HasForeignKey(t => t.SceneId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(t => t.Entity)
             .WithMany()
             .HasForeignKey(t => t.EntityId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(t => t.Controller)
             .WithMany()
             .HasForeignKey(t => t.ControlledBy)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
