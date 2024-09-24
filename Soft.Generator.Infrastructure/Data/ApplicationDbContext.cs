using Microsoft.EntityFrameworkCore;
using Soft.Generator.Shared.BaseEntities;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Soft.Generator.Shared.Interfaces;
using System.Reflection;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using Soft.Generator.Shared.Helpers;
using Soft.Generator.Shared.Enums;
using Soft.Generator.Security.Interface;
using Soft.Generator.Security.Entities;

namespace Soft.Generator.Infrastructure.Data
{
    public class ApplicationDbContext<TUser> : DbContext, IApplicationDbContext 
        where TUser : class, IUser, new() 
    {

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext<TUser>> options)
                : base(options)
        {
        }

        protected ApplicationDbContext(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<TUser> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<RoleUser> RoleUser { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<NotificationUser> NotificationUser { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<RoleUser>()
                .HasKey(ru => new { ru.RolesId, ru.UsersId });

            modelBuilder.Entity<TUser>()
                .HasMany(e => e.Roles)
                .WithMany()
                .UsingEntity<RoleUser>(
                    j => j.HasOne<Role>().WithMany().HasForeignKey(ru => ru.RolesId),
                    j => j.HasOne<TUser>().WithMany().HasForeignKey(ru => ru.UsersId)
                );

            modelBuilder.Entity<NotificationUser>()
                .HasKey(ru => new { ru.NotificationsId, ru.UsersId });

            modelBuilder.Entity<TUser>()
                .HasMany(e => e.Notifications)
                .WithMany()
                .UsingEntity<NotificationUser>(
                    j => j.HasOne<Notification>()
                          .WithMany()
                          .HasForeignKey(ru => ru.NotificationsId),
                    j => j.HasOne<TUser>()
                          .WithMany()
                          .HasForeignKey(ru => ru.UsersId)
                );

            if (SettingsProvider.Current.UseGoogleAsExternalProvider == false)
            {
                modelBuilder.Entity<TUser>().Ignore(x => x.HasLoggedInWithExternalProvider);
            }

            if (SettingsProvider.Current.AppHasLatinTranslation == false)
            {
                modelBuilder.Entity<Permission>().Ignore(x => x.NameLatin);
                modelBuilder.Entity<Permission>().Ignore(x => x.DescriptionLatin);
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

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            foreach (EntityEntry changedEntity in ChangeTracker.Entries())
            {
                HandleObjectChanges(changedEntity);
            }

            return await base.SaveChangesAsync(cancellationToken);
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
