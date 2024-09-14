using Microsoft.EntityFrameworkCore;
using Soft.Generator.Shared.BaseEntities;
using Soft.Generator.Security.Entities;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Soft.Generator.Shared.Interfaces;
using System.Reflection;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using Soft.Generator.Shared.Helpers;
using Soft.Generator.Shared.Enums;

namespace Soft.Generator.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        private readonly SoftSecuritySettings _softSecuritySettings;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, SoftSecuritySettings softSecuritySettings)
                : base(options)
        {
            _softSecuritySettings = softSecuritySettings;
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            if(_softSecuritySettings.UseGoogleAsExternalProvider == false)
            {
                modelBuilder.Entity<User>().Ignore(u => u.HasLoggedInWithExternalProvider);
            }
        }

        public DbSet<TEntity> DbSet<TEntity>() where TEntity : class
        {
            return Set<TEntity>();
        }

        public override int SaveChanges()
        {
            foreach (EntityEntry changedEntity in ChangeTracker.Entries())
            {
                HandleObjectChanges(changedEntity);
            }

            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            foreach (EntityEntry changedEntity in ChangeTracker.Entries())
            {
                HandleObjectChanges(changedEntity);
            }

            return base.SaveChangesAsync(cancellationToken);
        }

        void HandleObjectChanges(EntityEntry changedEntity)
        {
            switch (changedEntity.Entity)
            {
                case BusinessObject<long> businessObjectLong:
                    HandleBusinessObjectChanges(businessObjectLong, changedEntity);
                    break;

                case BusinessObject<int> businessObjectInt:
                    HandleBusinessObjectChanges(businessObjectInt, changedEntity);
                    break;

                case BusinessObject<byte> businessObjectByte:
                    HandleBusinessObjectChanges(businessObjectByte, changedEntity);
                    break;

                case ReadonlyObject<long> readonlyObjectLong:
                    HandleReadonlyObjectChanges(readonlyObjectLong, changedEntity);
                    break;

                case ReadonlyObject<int> readonlyObjectInt:
                    HandleReadonlyObjectChanges(readonlyObjectInt, changedEntity);
                    break;

                case ReadonlyObject<byte> readonlyObjectByte:
                    HandleReadonlyObjectChanges(readonlyObjectByte, changedEntity);
                    break;
            }
        }

        void HandleBusinessObjectChanges<T>(BusinessObject<T> businessObject, EntityEntry changedEntity)
        {
            DateTime now = DateTime.Now;

            switch (changedEntity.State)
            {
                case EntityState.Added:
                    businessObject.SetCreatedAt(now);
                    businessObject.SetModifiedAt(now);
                    businessObject.SetVersion(1);
                    break;

                case EntityState.Modified:
                    Entry(businessObject).Property(x => x.CreatedAt).IsModified = false;
                    businessObject.SetModifiedAt(now);
                    businessObject.SetVersion(businessObject.Version + 1);
                    break;
            }
        }

        void HandleReadonlyObjectChanges<T>(ReadonlyObject<T> readOnlyObject, EntityEntry changedEntity)
        {
            DateTime now = DateTime.Now;

            switch (changedEntity.State)
            {
                case EntityState.Added:
                    readOnlyObject.SetCreatedAt(now);
                    break;

                case EntityState.Modified:
                    Entry(readOnlyObject).Property(x => x.CreatedAt).IsModified = false;
                    break;
            }
        }

    }

}
