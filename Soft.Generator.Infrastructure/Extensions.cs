using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore;
using Soft.Generator.Shared.Attributes.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.Infrastructure
{
    public static class Extensions
    {
        public static void ConfigureReferenceTypesSetNull(this ModelBuilder modelBuilder)
        {
            foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
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

        public static void ConfigureManyToManyRelationships(this ModelBuilder modelBuilder)
        {
            foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
            {
                Type clrType = entityType.ClrType;
                List<PropertyInfo> properties = clrType.GetProperties().ToList();

                var m2mMaintanceKey = properties
                    .Select(p => new { Property = p, Attribute = p.GetCustomAttribute<M2MMaintanceEntityKeyAttribute>() })
                    .FirstOrDefault(x => x.Attribute != null);

                var m2mExtendKey = properties
                    .Select(p => new { Property = p, Attribute = p.GetCustomAttribute<M2MExtendEntityKeyAttribute>() })
                    .FirstOrDefault(x => x.Attribute != null);

                if (m2mMaintanceKey == null || m2mExtendKey == null)
                    continue;

                PropertyInfo maintanceNavigation = clrType.GetProperty(m2mMaintanceKey.Attribute.NavigationPropertyName);
                PropertyInfo extendNavigation = clrType.GetProperty(m2mExtendKey.Attribute.NavigationPropertyName);

                if (maintanceNavigation == null || extendNavigation == null)
                    throw new InvalidOperationException($"Navigation properties not found on type {clrType.Name}");

                modelBuilder.Entity(clrType)
                    .HasKey([m2mMaintanceKey.Property.Name, m2mExtendKey.Property.Name]);

                modelBuilder.Entity(clrType)
                    .HasOne(maintanceNavigation.PropertyType, m2mMaintanceKey.Attribute.NavigationPropertyName)
                    .WithMany()
                    .HasForeignKey(m2mMaintanceKey.Property.Name);

                modelBuilder.Entity(clrType)
                    .HasOne(extendNavigation.PropertyType, m2mExtendKey.Attribute.NavigationPropertyName)
                    .WithMany()
                    .HasForeignKey(m2mExtendKey.Property.Name);
            }
        }

        public static void ConfigureManyToOneRequired(this ModelBuilder modelBuilder)
        {
            foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
            {
                Type clrType = entityType.ClrType;

                foreach (PropertyInfo property in clrType.GetProperties())
                {
                    MemberInfo member = property;

                    if (member == null)
                        continue;

                    ManyToOneRequiredAttribute attributeValue = Attribute.GetCustomAttribute(member, typeof(ManyToOneRequiredAttribute)) as ManyToOneRequiredAttribute;

                    if (attributeValue == null)
                        continue;

                    modelBuilder.Entity(clrType)
                        .HasOne(property.PropertyType, property.Name)
                        .WithMany()
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired(true);
                }
            }
        }


    }
}
