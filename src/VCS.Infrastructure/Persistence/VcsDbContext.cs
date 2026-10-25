using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VCS.Domain.Advancement;
using VCS.Domain.Advantages;
using VCS.Domain.Characters;
using VCS.Domain.Predators;
using VCS.Domain.Skills;
using VCS.Infrastructure.Catalogs.Seed;

namespace VCS.Infrastructure.Persistence;

public sealed class VcsDbContext(DbContextOptions<VcsDbContext> options) : DbContext(options)
{
    public DbSet<Character> Characters => Set<Character>();
    public DbSet<AdvantageDefinition> AdvantageCatalog => Set<AdvantageDefinition>();
    public DbSet<SkillDefinition> SkillCatalog => Set<SkillDefinition>();
    public DbSet<PredatorCatalogRecord> PredatorCatalog => Set<PredatorCatalogRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var predators = modelBuilder.Entity<PredatorCatalogRecord>();
        predators.ToTable("PredatorCatalog");
        predators.HasKey(x => x.Id);
        predators.Property(x => x.Name).HasMaxLength(80).IsRequired();
        predators.Property(x => x.Definition).IsRequired();
        predators.HasData(Domain.Predators.PredatorCatalog.All.Select(x => new PredatorCatalogRecord
        {
            Id = x.Id, Name = x.Name, Definition = JsonSerializer.Serialize(x)
        }));
        var skills = modelBuilder.Entity<SkillDefinition>();
        skills.ToTable("SkillCatalog");
        skills.HasKey(x => x.Id);
        skills.Property(x => x.Name).HasMaxLength(80).IsRequired();
        skills.Property(x => x.Group).HasMaxLength(80).IsRequired();
        skills.Property(x => x.Description).IsRequired();
        skills.HasData(ProvidedSkills.All);
        var catalog = modelBuilder.Entity<AdvantageDefinition>();
        catalog.ToTable("AdvantageCatalog", table => table.HasCheckConstraint("CK_AdvantageCatalog_Points",
            "\"Minimum\" >= 1 AND \"Maximum\" >= \"Minimum\" AND \"Maximum\" <= 5"));
        catalog.HasKey(x => x.Id);
        catalog.Property(x => x.Id).HasMaxLength(80);
        catalog.Property(x => x.Name).HasMaxLength(80).IsRequired();
        catalog.Property(x => x.Group).HasMaxLength(80).IsRequired();
        catalog.Property(x => x.Description).IsRequired();
        catalog.Property(x => x.LoresheetCollection).HasMaxLength(80).IsRequired();
        catalog.Property(x => x.PointsLabel).IsRequired();
        catalog.HasData(ProvidedAdvantages.All);
        catalog.HasData(ProvidedLoresheets.All);
        var character = modelBuilder.Entity<Character>();
        character.ToTable("Characters");
        character.HasKey(x => x.Id);
        character.Property(x => x.Revision).IsConcurrencyToken();
        character.Property(x => x.CreatedUtc).HasConversion(
            x => x, x => DateTime.SpecifyKind(x, DateTimeKind.Utc));
        character.Property(x => x.UpdatedUtc).HasConversion(
            x => x, x => DateTime.SpecifyKind(x, DateTimeKind.Utc));
        character.Ignore(x => x.HealthCapacity);
        character.Ignore(x => x.WillpowerCapacity);
        character.OwnsOne(x => x.Details, details =>
        {
            details.Property(x => x.PredatorSelection).HasConversion(
                x => JsonSerializer.Serialize(x, (JsonSerializerOptions?)null),
                x => JsonSerializer.Deserialize<PredatorSelection>(x, (JsonSerializerOptions?)null));
            details.Property(x => x.Advancement).HasConversion(
                x => JsonSerializer.Serialize(x, (JsonSerializerOptions?)null),
                x => JsonSerializer.Deserialize<AdvancementState>(x, (JsonSerializerOptions?)null));
            details.Property(x => x.NpcAttacks).HasConversion(
                x => JsonSerializer.Serialize(x, (JsonSerializerOptions?)null),
                x => new ValueList<NpcAttack>(JsonSerializer.Deserialize<NpcAttack[]>(x, (JsonSerializerOptions?)null) ?? Array.Empty<NpcAttack>()),
                new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<IReadOnlyList<NpcAttack>>(
                    (a, b) => a == null ? b == null : b != null && a.SequenceEqual(b),
                    x => x.Aggregate(0, (hash, attack) => HashCode.Combine(hash, attack)),
                    x => new ValueList<NpcAttack>(x)))
                .HasDefaultValue(ValueList<NpcAttack>.Empty).IsRequired();
            details.Property(x => x.NoteEntries).HasConversion(
                x => JsonSerializer.Serialize(x, (JsonSerializerOptions?)null),
                x => new ValueList<CharacterNote>(JsonSerializer.Deserialize<CharacterNote[]>(x, (JsonSerializerOptions?)null) ?? Array.Empty<CharacterNote>()),
                new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<IReadOnlyList<CharacterNote>>(
                    (a, b) => a == null ? b == null : b != null && a.SequenceEqual(b),
                    x => x.Aggregate(0, (hash, note) => HashCode.Combine(hash, note)),
                    x => new ValueList<CharacterNote>(x)))
                .HasDefaultValue(ValueList<CharacterNote>.Empty).IsRequired();
            details.Property(x => x.Name).HasColumnName("Name").HasMaxLength(120).IsRequired();
            details.Property(x => x.Clan).HasColumnName("Clan").HasMaxLength(80).IsRequired();
            details.Property(x => x.Chronicle).HasColumnName("Chronicle").HasMaxLength(120).IsRequired();
            details.HasIndex(x => x.Name);
        });
        character.Navigation(x => x.Details).IsRequired();
        character.HasMany(x => x.Traits).WithOne().HasForeignKey("CharacterId").IsRequired().OnDelete(DeleteBehavior.Cascade);
        character.Navigation(x => x.Traits).UsePropertyAccessMode(PropertyAccessMode.Field);

        var trait = modelBuilder.Entity<CharacterTrait>();
        trait.ToTable("CharacterTraits");
        trait.HasKey(x => x.Id);
        trait.Property(x => x.Key).HasMaxLength(80).IsRequired();
        trait.Property(x => x.Name).HasMaxLength(80).IsRequired();
        trait.Property(x => x.Specialization).HasMaxLength(500).IsRequired();
        trait.Property(x => x.Loresheet).HasConversion(
            x => JsonSerializer.Serialize(x, (JsonSerializerOptions?)null),
            x => JsonSerializer.Deserialize<LoresheetInfo>(x, (JsonSerializerOptions?)null));
        trait.Property(x => x.Merit).HasConversion(
            x => JsonSerializer.Serialize(x, (JsonSerializerOptions?)null),
            x => JsonSerializer.Deserialize<MeritInfo>(x, (JsonSerializerOptions?)null));
        trait.Property(x => x.Skill).HasConversion(
            x => JsonSerializer.Serialize(x, (JsonSerializerOptions?)null),
            x => JsonSerializer.Deserialize<SkillInfo>(x, (JsonSerializerOptions?)null));
        trait.Property(x => x.Power).HasConversion(
            x => JsonSerializer.Serialize(x, (JsonSerializerOptions?)null),
            x => JsonSerializer.Deserialize<PowerInfo>(x, (JsonSerializerOptions?)null));
        trait.HasIndex("CharacterId", nameof(CharacterTrait.Category), nameof(CharacterTrait.Key)).IsUnique();
    }
}
