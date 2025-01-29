using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.Text;
using Spider.SourceGenerators.Shared;
using System.Linq;
using Spider.SourceGenerators.Models;
using Spider.SourceGenerators.Enums;

namespace Spider.SourceGenerators.Net
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
            IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = Helpers.GetClassInrementalValuesProvider(context.SyntaxProvider, new List<NamespaceExtensionCodes>
                {
                    NamespaceExtensionCodes.Entities,
                });

            IncrementalValueProvider<List<SpiderClass>> referencedProjectClasses = Helpers.GetIncrementalValueProviderClassesFromReferencedAssemblies(context,
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

        private static void Execute(IList<ClassDeclarationSyntax> classes, List<SpiderClass> referencedProjectEntityClasses, SourceProductionContext context)
        {
            if (classes.Count <= 1)
                return;

            List<SpiderClass> entities = Helpers.GetSpiderEntities(classes, referencedProjectEntityClasses);
            List<SpiderClass> allEntities = entities.Concat(referencedProjectEntityClasses).ToList();

            string[] namespacePartsWithoutLastElement = Helpers.GetNamespacePartsWithoutLastElement(entities[0].Namespace);
            string basePartOfNamespace = string.Join(".", namespacePartsWithoutLastElement); // eg. Spider.Security
            string projectName = namespacePartsWithoutLastElement[namespacePartsWithoutLastElement.Length - 1]; // eg. Security

            List<string> DTOClasses = new List<string>();

            foreach (SpiderClass entity in entities)
            {
                AddDTOClass(entity, DTOClasses, allEntities);
            }

            string result = $$"""
{{GetUsings()}}

namespace {{basePartOfNamespace}}.DTO
{
{{string.Join("\n\n", DTOClasses)}}
}
""";

            context.AddSource($"{projectName}DTOList.generated", SourceText.From(result, Encoding.UTF8));
        }

        #region Data Fill

        #region Entities

        private static void AddDTOClass(SpiderClass entity, List<string> DTOClasses, List<SpiderClass> allEntities)
        {
            string DTObaseType = entity.GetDTOBaseType();

            List<string> DTOPropertiesWithoutBaseType = new List<string>();
            List<string> orderedOneToManyProperties = new List<string>();
            List<string> manyToManyMultiControlTypeProperties = new List<string>();
            List<string> simpleManyToManyTableLazyLoadProperties = new List<string>();

            foreach (SpiderProperty property in entity.Properties)
            {
                AddDTOPropertiesWithoutBaseType(property, DTOPropertiesWithoutBaseType, entity, allEntities);
                AddOrderedOneToManyProperties(property, orderedOneToManyProperties, allEntities);
                AddManyToManyMultiControlTypeProperties(property, manyToManyMultiControlTypeProperties, allEntities);
                AddSimpleManyToManyTableLazyLoadProperties(property, simpleManyToManyTableLazyLoadProperties, allEntities);
            }

            DTOClasses.Add($$"""
    public partial class {{entity.Name}}DTO {{GetDTOBaseTypeExtension(DTObaseType)}}
    {
{{string.Join("\n", DTOPropertiesWithoutBaseType)}}
    }

    public partial class {{entity.Name}}SaveBodyDTO
    {
        public {{entity.Name}}DTO {{entity.Name}}DTO { get; set; }
{{string.Join("\n", orderedOneToManyProperties)}}
{{string.Join("\n", manyToManyMultiControlTypeProperties)}}
{{string.Join("\n", simpleManyToManyTableLazyLoadProperties)}}
    }
""");
        }

        #endregion

        #region Properties

        /// <summary>
        /// Getting the properties of the DTO based on the entity class, we don't include base type properties because of the inheritance
        /// </summary>
        private static void AddDTOPropertiesWithoutBaseType(SpiderProperty property, List<string> DTOPropertiesWithoutBaseType, SpiderClass entity, List<SpiderClass> allClasses)
        {
            if (property.EntityName != entity.Name) // FT: Skipping the base properties from other classes
                return;

            if (property.ShouldSkipPropertyInDTO())
                return;

            string propType = property.Type;
            string propName = property.Name;

            if (propType.IsManyToOneType())
            {
                DTOPropertiesWithoutBaseType.Add($$"""
        public string {{propName}}DisplayName { get; set; }
""");
                SpiderClass manyToOneClass = allClasses.Where(x => x.Name == propType).Single();
                DTOPropertiesWithoutBaseType.Add($$"""
        public {{manyToOneClass.GetIdType(allClasses)}}? {{propName}}Id { get; set; }
""");
                return;
            }
            else if (propType.IsEnumerable() && property.HasGenerateCommaSeparatedDisplayNameAttribute())
            {
                DTOPropertiesWithoutBaseType.Add($$"""
        public string {{propName}}CommaSeparated { get; set; }
""");
                return;
            }
            else if (propType == "byte[]")
            {
                DTOPropertiesWithoutBaseType.Add($$"""
        public string {{propName}} { get; set; }
""");
                return;
            }
            else if (propType.IsEnumerable() && property.HasIncludeInDTOAttribute())
            {
                string DTOListPropType = propType.Replace(">", "DTO>");
                DTOPropertiesWithoutBaseType.Add($$"""
        /// <summary>
        /// Made only for manual mapping, it's not included in the mapping library.
        /// </summary>
        public {{DTOListPropType}} {{propName}}DTOList { get; set; }
""");
                return;
            }
            else if (propType.IsEnumerable())
            {
                return;
            }
            else if (propType.IsBaseType() && propType != "string")
            {
                propType = $"{property.Type}?".Replace("??", "?");
            }
            else if (property.Attributes.Any(x => x.Name == "BlobName"))
            {
                DTOPropertiesWithoutBaseType.Add($$"""
        public string {{propName}}Data { get; set; }
""");
            }
            else if (propType != "string")
            {
                propType = "UNSUPPORTED TYPE";
            }

            // string data type
            DTOPropertiesWithoutBaseType.Add($$"""
        public {{propType}} {{propName}} { get; set; }
""");


        }

        private static void AddOrderedOneToManyProperties(SpiderProperty property, List<string> orderedOneToManyProperties, List<SpiderClass> entities)
        {
            if (property.HasOrderedOneToManyAttribute() == false)
                return;

            SpiderClass extractedEntity = entities.Where(x => x.Name == Helpers.ExtractTypeFromGenericType(property.Type)).SingleOrDefault();

            orderedOneToManyProperties.Add($$"""
        public List<{{extractedEntity.Name}}DTO> {{property.Name}}DTO { get; set; }
""");
        }

        private static void AddManyToManyMultiControlTypeProperties(SpiderProperty property, List<string> manyToManyMultiControlTypeProperties, List<SpiderClass> entities)
        {
            if (property.IsMultiSelectControlType() == false &&
                property.IsMultiAutocompleteControlType() == false)
            {
                return;
            }

            SpiderClass extractedEntity = entities.Where(x => x.Name == Helpers.ExtractTypeFromGenericType(property.Type)).SingleOrDefault();

            manyToManyMultiControlTypeProperties.Add($$"""
        public List<{{extractedEntity.GetIdType(entities)}}> Selected{{property.Name}}Ids { get; set; }
""");

        }

        private static void AddSimpleManyToManyTableLazyLoadProperties(SpiderProperty property, List<string> simpleManyToManyTableLazyLoadProperties, List<SpiderClass> entities)
        {
            if (property.HasSimpleManyToManyTableLazyLoadAttribute() == false)
                return;

            SpiderClass extractedEntity = entities.Where(x => x.Name == Helpers.ExtractTypeFromGenericType(property.Type)).SingleOrDefault();
            string extractedEntityIdType = extractedEntity.GetIdType(entities);

            simpleManyToManyTableLazyLoadProperties.Add($$"""
        public List<{{extractedEntityIdType}}> Selected{{property.Name}}Ids { get; set; }
        public List<{{extractedEntityIdType}}> Unselected{{property.Name}}Ids { get; set; }
        public bool? AreAll{{property.Name}}Selected { get; set; }
        public TableFilterDTO {{property.Name}}TableFilter { get; set; }
""");
        }

        #endregion

        #endregion

        #region Helpers

        private static string GetDTOBaseTypeExtension(string DTObaseType)
        {
            return DTObaseType == null ? "" : $": {DTObaseType}";
        }

        private static string GetUsings()
        {
            return $$"""
using Microsoft.AspNetCore.Http;
using Spider.Shared.DTO;
using Spider.Security.DTO;
using Spider.Shared.Helpers;
""";
        }

        #endregion
    }
}
