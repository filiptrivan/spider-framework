using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore;
using Spider.Shared.Attributes.EF;
using System.Reflection;
using Spider.Shared.Helpers;
using System.ComponentModel.DataAnnotations;

namespace Spider.Infrastructure
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
                    SetNullAttribute setNullAttribute = property.GetCustomAttribute<SetNullAttribute>();

                    if (setNullAttribute == null)
                        continue;

                    WithManyAttribute withManyAttribute = property.GetCustomAttribute<WithManyAttribute>();

                    modelBuilder.Entity(clrType)
                        .HasOne(property.PropertyType, property.Name)
                        .WithMany(withManyAttribute.WithMany)
                        .OnDelete(DeleteBehavior.SetNull)
                        .IsRequired(false)
                        .HasForeignKey($"{property.Name}Id");
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

                var m2mEntity = properties
                    .Where(x => x != null && x.GetCustomAttribute<M2MEntityAttribute>() != null)
                    .Select(x => new { Property = x, Attribute = x.GetCustomAttribute<M2MEntityAttribute>() })
                    .SingleOrDefault();

                if (m2mMaintanceEntity == null || m2mEntity == null)
                    continue;

                PropertyInfo m2mMaintanceEntityWithManyProperty = mutableEntityTypes
                    .Where(x => x.Name == m2mMaintanceEntity.Property.PropertyType.FullName)
                    .SelectMany(x => x.ClrType.GetProperties())
                    .Where(x => x.Name == m2mMaintanceEntity.Attribute.WithManyProperty)
                    .SingleOrDefault();
                PropertyInfo m2mEntityWithManyProperty = mutableEntityTypes
                    .Where(x => x.Name == m2mEntity.Property.PropertyType.FullName)
                    .SelectMany(x => x.ClrType.GetProperties())
                    .Where(x => x.Name == m2mEntity.Attribute.WithManyProperty)
                    .SingleOrDefault();

                if (m2mMaintanceEntityWithManyProperty == null || m2mEntityWithManyProperty == null)
                    throw new Exception($"Bad WithManyProperty definitions for {clrType.Name}.");

                List<string> primaryKeys = [$"{m2mMaintanceEntity.Property.Name}Id", $"{m2mEntity.Property.Name}Id"];

                foreach (PropertyInfo property in properties.Where(x => x != null && x.GetCustomAttribute<KeyAttribute>() != null))
                    primaryKeys.Add($"{property.Name}Id");

                modelBuilder.Entity(clrType)
                    .HasKey(primaryKeys.ToArray());

                if (properties.Count == 2 || (m2mMaintanceEntityWithManyProperty.PropertyType.ToString() != m2mEntityWithManyProperty.PropertyType.ToString()))
                {
                    modelBuilder.Entity(m2mMaintanceEntity.Property.PropertyType) 
                        .HasMany(m2mMaintanceEntity.Attribute.WithManyProperty)
                        .WithMany(m2mEntity.Attribute.WithManyProperty)
                        .UsingEntity(
                            clrType,
                            j => j.HasOne(m2mEntity.Property.Name)
                                  .WithMany()
                                  .HasForeignKey($"{m2mEntity.Property.Name}Id"),
                            j => j.HasOne(m2mMaintanceEntity.Property.Name)
                                  .WithMany()
                                  .HasForeignKey($"{m2mMaintanceEntity.Property.Name}Id")
                        );
                }
                else
                {
                    modelBuilder.Entity(m2mMaintanceEntity.Property.PropertyType)
                        .HasMany(clrType, m2mMaintanceEntityWithManyProperty.Name)
                        .WithOne(m2mMaintanceEntity.Property.Name)
                        .HasForeignKey($"{m2mMaintanceEntity.Property.Name}Id");

                    modelBuilder.Entity(m2mEntity.Property.PropertyType)
                        .HasMany(clrType, m2mEntityWithManyProperty.Name)
                        .WithOne(m2mEntity.Property.Name)
                        .HasForeignKey($"{m2mEntity.Property.Name}Id");
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
                    ManyToOneRequiredAttribute manyToOneRequiredAttribute = property.GetCustomAttribute<ManyToOneRequiredAttribute>();

                    if (manyToOneRequiredAttribute == null)
                        continue;

                    WithManyAttribute withManyAttribute = property.GetCustomAttribute<WithManyAttribute>();

                    modelBuilder.Entity(clrType)
                        .HasOne(property.PropertyType, property.Name)
                        .WithMany(withManyAttribute.WithMany)
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired(true)
                        .HasForeignKey($"{property.Name}Id");
                }
            }
        }


    }
}
