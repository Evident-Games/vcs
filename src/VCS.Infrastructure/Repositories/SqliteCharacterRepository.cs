using Microsoft.EntityFrameworkCore;
using VCS.Application.Abstractions;
using VCS.Domain.Characters;
using VCS.Infrastructure.Persistence;

namespace VCS.Infrastructure.Repositories;

public sealed class SqliteCharacterRepository(DatabaseAccess database) : ICharacterRepository
{
    public async Task<IReadOnlyList<Character>> ListAsync(CancellationToken cancellationToken = default)
    {
        await database.Gate.WaitAsync(cancellationToken);
        try
        {
            await using var db = database.CreateContext();
            return await db.Characters.AsNoTracking().ToListAsync(cancellationToken);
        }
        finally { database.Gate.Release(); }
    }

    public async Task<Character?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await database.Gate.WaitAsync(cancellationToken);
        try
        {
            await using var db = database.CreateContext();
            return await db.Characters.AsNoTracking().Include(x => x.Traits)
                .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        }
        finally { database.Gate.Release(); }
    }

    public async Task SaveAsync(Character character, long? expectedRevision, CancellationToken cancellationToken = default)
    {
        await database.Gate.WaitAsync(cancellationToken);
        try
        {
            await using var db = database.CreateContext();
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            if (expectedRevision is null)
            {
                db.Characters.Add(character);
            }
            else
            {
                await db.Set<CharacterTrait>().Where(x => EF.Property<Guid>(x, "CharacterId") == character.Id)
                    .ExecuteDeleteAsync(cancellationToken);
                db.Attach(character);
                db.Entry(character).State = EntityState.Modified;
                db.Entry(character).Property(x => x.Revision).OriginalValue = expectedRevision.Value;
                db.Entry(character.Details).State = EntityState.Modified;
                db.Set<CharacterTrait>().AddRange(character.Traits);
            }
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new CharacterConflictException();
        }
        finally { database.Gate.Release(); }
    }

    public async Task DeleteAsync(Guid id, long expectedRevision, CancellationToken cancellationToken = default)
    {
        await database.Gate.WaitAsync(cancellationToken);
        try
        {
            await using var db = database.CreateContext();
            var deleted = await db.Characters.Where(x => x.Id == id && x.Revision == expectedRevision)
                .ExecuteDeleteAsync(cancellationToken);
            if (deleted == 0)
                throw new CharacterConflictException();
        }
        finally { database.Gate.Release(); }
    }
}
