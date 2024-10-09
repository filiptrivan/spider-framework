using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Nucleus.Core.BusinessObject;

namespace Soft.Generator.Shared.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<TEntity> DbSet<TEntity>() where TEntity : class;

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken));

        DatabaseFacade Database { get; }
    }
}
