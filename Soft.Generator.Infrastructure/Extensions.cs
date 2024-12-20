using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore;
using Soft.Generator.Shared.Attributes.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Soft.Generator.Shared.Helpers;

namespace Soft.Generator.Infrastructure
{
    public static class Extensions
    {
        public static void ConfigureReferenceTypesSetNull(this List<IMutableEntityType> mutableEntityTypes, ModelBuilder modelBuilder)
        {
            foreach (IMutableEntityType entityType in mutableEntityTypes)
            {
                Type clrType = entityType.ClrType;

                foreach (PropertyInfo property in clrType.GetProperties())
                {
                    if (property.GetCustomAttribute<SetNullAttribute>() != null)
                    {
                        if (property.GetCustomAttribute<SetNullAttribute>() == null)
                            throw new Exception("The property set null attribute can not be null.");

                        IMutableNavigation navigation = entityType.FindNavigation(property.Name);

                        if (navigation != null)
                            navigation.ForeignKey.DeleteBehavior = DeleteBehavior.NoAction;
                    }
                }
            }
        }

        public static void ConfigureManyToManyRelationships(this List<IMutableEntityType> mutableEntityTypes, ModelBuilder modelBuilder)
        {
            foreach (IMutableEntityType entityType in mutableEntityTypes)
            {
                Type clrType = entityType.ClrType;
                List<PropertyInfo> properties = clrType.GetProperties().ToList();

                var m2mMaintanceEntity = properties
                    .Where(x => x != null && x.GetCustomAttribute<M2MMaintanceEntityAttribute>() != null)
                    .Select(x => new { Property = x, Attribute = x.GetCustomAttribute<M2MMaintanceEntityAttribute>() })
                    .SingleOrDefault();

                var m2mExtendEntity = properties
                    .Where(x => x != null && x.GetCustomAttribute<M2MExtendEntityAttribute>() != null)
                    .Select(x => new { Property = x, Attribute = x.GetCustomAttribute<M2MExtendEntityAttribute>() })
                    .SingleOrDefault();

                if (m2mMaintanceEntity == null || m2mExtendEntity == null)
                    continue;

                string m2mMaintanceEntityWithManyProperty = m2mMaintanceEntity.Attribute?.WithManyProperty;
                string m2mExtendEntityWithManyProperty = m2mExtendEntity.Attribute?.WithManyProperty;

                if (m2mMaintanceEntityWithManyProperty == null || m2mExtendEntityWithManyProperty == null)
                    throw new Exception($"Bad WithManyProperty definitions for {clrType.Name}.");

                modelBuilder.Entity(clrType)
                    .HasKey(new[] { $"{m2mMaintanceEntity.Property.Name}Id", $"{m2mExtendEntity.Property.Name}Id" });

                if (properties.Count == 2 || (m2mMaintanceEntityWithManyProperty != m2mExtendEntityWithManyProperty)) // FT HACK, FT TODO: For now, when we migrate UserNotification and PartnerUserPartnerNotification, we should change this.
                {
                    modelBuilder.Entity(m2mMaintanceEntity.Property.PropertyType) 
                        .HasMany(m2mMaintanceEntity.Attribute.WithManyProperty)
                        .WithMany(m2mExtendEntity.Attribute.WithManyProperty)
                        .UsingEntity(
                            clrType,
                            j => j.HasOne(m2mExtendEntity.Property.Name)
                                  .WithMany()
                                  .HasForeignKey($"{m2mExtendEntity.Property.Name}Id"),
                            j => j.HasOne(m2mMaintanceEntity.Property.Name)
                                  .WithMany()
                                  .HasForeignKey($"{m2mMaintanceEntity.Property.Name}Id")
                        );
                }
                else
                {
                    modelBuilder.Entity(m2mMaintanceEntity.Property.PropertyType)
                        .HasMany(clrType, m2mMaintanceEntityWithManyProperty)
                        .WithOne(m2mMaintanceEntity.Property.Name)
                        .HasForeignKey($"{m2mMaintanceEntity.Property.Name}Id");

                    modelBuilder.Entity(m2mExtendEntity.Property.PropertyType)
                        .HasMany(clrType, m2mExtendEntityWithManyProperty)
                        .WithOne(m2mExtendEntity.Property.Name)
                        .HasForeignKey($"{m2mExtendEntity.Property.Name}Id");
                }

            }
        }

        public static void ConfigureManyToOneRequired(this List<IMutableEntityType> mutableEntityTypes, ModelBuilder modelBuilder)
        {
            foreach (IMutableEntityType entityType in mutableEntityTypes)
            {
                Type clrType = entityType.ClrType;

                foreach (PropertyInfo property in clrType.GetProperties())
                {
                    ManyToOneRequiredAttribute attributeValue = property.GetCustomAttribute<ManyToOneRequiredAttribute>();

                    if (attributeValue == null)
                        continue;

                    modelBuilder.Entity(clrType)
                        .HasOne(property.PropertyType, property.Name)
                        .WithMany(attributeValue.WithManyProperty)
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired(true)
                        .HasForeignKey($"{property.Name}Id");
                }
            }
        }


    }
}
