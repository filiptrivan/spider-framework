using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.Text;
using Soft.SourceGenerator.NgTable.Helpers;
using System.Collections.Immutable;
using System.Linq;
using System.IO;
using Soft.SourceGenerators.Helpers;
using System.Diagnostics;
using Soft.SourceGenerator.NgTable.Angular;
using Soft.SourceGenerators.Models;
using Soft.SourceGenerators.Enums;

namespace Soft.SourceGenerator.NgTable.Net
{
    [Generator]
    public class EntitiesToDTOGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            //#if DEBUG
            //            if (!Debugger.IsAttached)
            //            {
            //                Debugger.Launch();
            //            }
            //#endif
            IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = Helper.GetClassInrementalValuesProvider(context.SyntaxProvider, new List<NamespaceExtensionCodes>
                {
                    NamespaceExtensionCodes.Entities,
                });

            IncrementalValueProvider<List<SoftClass>> referencedProjectClasses = Helper.GetIncrementalValueProviderClassesFromReferencedAssemblies(context,
                new List<NamespaceExtensionCodes>
                {
                    NamespaceExtensionCodes.Entities,
                });

            var allClasses = classDeclarations.Collect()
                .Combine(referencedProjectClasses);

            context.RegisterImplementationSourceOutput(allClasses, static (spc, source) => Execute(source.Left, source.Right, spc));

            //context.RegisterImplementationSourceOutput(classDeclarations.Collect(),
            //    static (spc, source) => Execute(source, spc));
        }

        private static void Execute(IList<ClassDeclarationSyntax> classes, List<SoftClass> referencedProjectEntityClasses, SourceProductionContext context)
        {
            if (classes.Count <= 1) 
                return;

            List<SoftClass> entityClasses = Helper.GetSoftEntityClasses(classes, referencedProjectEntityClasses);
            List<SoftClass> allClasses = entityClasses.Concat(referencedProjectEntityClasses).ToList();

            string[] namespacePartsWithoutLastElement = Helper.GetNamespacePartsWithoutLastElement(entityClasses[0].Namespace);

            string basePartOfNamespace = string.Join(".", namespacePartsWithoutLastElement); // eg. Soft.Generator.Security
            string projectName = namespacePartsWithoutLastElement[namespacePartsWithoutLastElement.Length - 1]; // eg. Security

            string result = $$"""
{{GetUsings()}}

namespace {{basePartOfNamespace}}.DTO
{
{{string.Join("\n\n", GetDTOClasses(entityClasses, allClasses))}}
}
""";

            context.AddSource($"{projectName}DTOList.generated", SourceText.From(result, Encoding.UTF8));
        }

        private static List<string> GetDTOClasses(List<SoftClass> entities, List<SoftClass> allClasses)
        {
            List<string> result = new List<string>();

            foreach (SoftClass entity in entities)
            {
                string DTObaseType = entity.GetDTOBaseType();

                // Add table selection base class to SaveBodyDTO if there is some attribute on the class
                result.Add($$"""
    public partial class {{entity.Name}}DTO {{(DTObaseType == null ? "" : $": {DTObaseType}")}}
    {
{{string.Join("\n", GetDTOPropertiesWithoutBaseType(entity, allClasses))}}
    }

    public partial class {{entity.Name}}SaveBodyDTO
    {
        public {{entity.Name}}DTO {{entity.Name}}DTO { get; set; }
{{string.Join("\n", GetOrderedOneToManyProperties(entity, entities))}}
{{string.Join("\n", GetManyToManyMultiControlTypeProperties(entity, entities))}}
{{string.Join("\n", GetSimpleManyToManyTableLazyLoadProperties(entity, entities))}}
    }
""");
            }

            return result;
        }

        private static List<string> GetSimpleManyToManyTableLazyLoadProperties(SoftClass entity, List<SoftClass> entities)
        {
            List<string> result = new List<string>();

            foreach (SoftProperty property in entity.Properties.Where(x => x.HasSimpleManyToManyTableLazyLoadAttribute()))
            {
                SoftClass extractedEntity = entities.Where(x => x.Name == Helper.ExtractTypeFromGenericType(property.Type)).SingleOrDefault();
                string extractedEntityIdType = Helper.GetIdType(entity, entities);

                result.Add($$"""
        public List<{{extractedEntityIdType}}> Selected{{property.Name}}Ids { get; set; }
        public List<{{extractedEntityIdType}}> Unselected{{property.Name}}Ids { get; set; }
        public bool? AreAll{{property.Name}}Selected { get; set; }
        public TableFilterDTO {{property.Name}}TableFilter { get; set; }
""");
            }

            return result;
        }

        private static List<string> GetOrderedOneToManyProperties(SoftClass entity, List<SoftClass> entities)
        {
            List<string> result = new List<string>();

            foreach (SoftProperty property in entity.GetOrderedOneToManyProperties())
            {
                SoftClass extractedEntity = entities.Where(x => x.Name == Helper.ExtractTypeFromGenericType(property.Type)).SingleOrDefault();

                result.Add($$"""
        public List<{{extractedEntity.Name}}DTO> {{property.Name}}DTO { get; set; }
""");
            }

            return result;
        }

        private static List<string> GetManyToManyMultiControlTypeProperties(SoftClass entity, List<SoftClass> entities)
        {
            List<string> result = new List<string>();

            foreach (SoftProperty property in entity.Properties
                .Where(x =>
                    x.IsMultiSelectControlType() ||
                    x.IsMultiAutocompleteControlType()))
                    
            {
                SoftClass extractedEntity = entities.Where(x => x.Name == Helper.ExtractTypeFromGenericType(property.Type)).SingleOrDefault();

                result.Add($$"""
        public List<{{Helper.GetIdType(extractedEntity, entities)}}> Selected{{property.Name}}Ids { get; set; }
""");
            }

            return result;
        }

        /// <summary>
        /// Getting the properties of the DTO based on the entity class, we don't include base type properties because of the inheritance
        /// </summary>
        private static List<string> GetDTOPropertiesWithoutBaseType(SoftClass entityClass, List<SoftClass> allClasses)
        {
            List<string> DTOproperties = new List<string>(); // public string Email { get; set; }

            List<SoftProperty> propertiesOfTheCurrentClass = entityClass.Properties.Where(x => x.EntityName == entityClass.Name).ToList();

            foreach (SoftProperty prop in propertiesOfTheCurrentClass)
            {
                if (prop.SkipPropertyInDTO())
                    continue;

                string propType = prop.Type;
                string propName = prop.Name;

                if (propType.IsManyToOneType())
                {
                    DTOproperties.Add($$"""
        public string {{propName}}DisplayName { get; set; }
""");
                    SoftClass manyToOneClass = allClasses.Where(x => x.Name == propType).Single();
                    DTOproperties.Add($$"""
        public {{Helper.GetIdType(manyToOneClass, allClasses)}}? {{propName}}Id { get; set; }
""");
                    continue;
                }
                else if (propType.IsEnumerable() && prop.Attributes.Any(x => x.Name == "GenerateCommaSeparatedDisplayName"))
                {
                    DTOproperties.Add($$"""
        public string {{propName}}CommaSeparated { get; set; }
""");
                    continue;
                }
                else if (propType == "byte[]")
                {
                    DTOproperties.Add($$"""
        public string {{propName}} { get; set; }
""");
                    continue;
                }
                else if (propType.IsEnumerable() && prop.Attributes.Any(x => x.Name == "Map"))
                {
                    string DTOListPropType = propType.Replace(">", "DTO>");
                    DTOproperties.Add($$"""
        /// <summary>
        /// Made only for manual mapping, it's not included in the mapping library.
        /// </summary>
        public {{DTOListPropType}} {{propName}}DTOList { get; set; }
""");
                    continue;
                }
                else if (propType.IsEnumerable())
                {
                    continue;
                }
                else if (propType.IsBaseType() && propType != "string")
                {
                    propType = $"{prop.Type}?".Replace("??", "?");
                }
                else if (prop.Attributes.Any(x => x.Name == "BlobName"))
                {
                    DTOproperties.Add($$"""
        public string {{propName}}Data { get; set; }
""");
                }
                else if (propType != "string")
                {
                    propType = "UNSUPPORTED TYPE";
                }

                // string
                DTOproperties.Add($$"""
        public {{propType}} {{propName}} { get; set; }
""");
            }

            return DTOproperties;
        }

        private static string GetUsings()
        {
            return $$"""
using Microsoft.AspNetCore.Http;
using Soft.Generator.Shared.DTO;
using Soft.Generator.Security.DTO;
using Soft.Generator.Shared.Helpers;
""";
        }
    }
}
