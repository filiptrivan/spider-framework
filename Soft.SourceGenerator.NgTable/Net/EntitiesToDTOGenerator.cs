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
            IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => Helper.IsSyntaxTargetForGenerationEntities(s),
                    transform: static (ctx, _) => Helper.GetSemanticTargetForGenerationEntities(ctx))
                .Where(static c => c is not null);

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

            List<SoftClass> entityClasses = Helper.GetSoftEntityClasses(classes);
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

        private static List<string> GetDTOClasses(List<SoftClass> entityClasses, List<SoftClass> allClasses)
        {
            List<string> result = new List<string>();

            foreach (SoftClass entityClass in entityClasses)
            {
                string DTObaseType = entityClass.GetDTOBaseType();

                // Add table selection base class to SaveBodyDTO if there is some attribute on the class
                result.Add($$"""
    public partial class {{entityClass.Name}}DTO {{(DTObaseType == null ? "" : $": {DTObaseType}")}}
    {
{{string.Join("\n", GetDTOPropertiesWithoutBaseType(entityClass, allClasses))}}
    }

    public partial class {{entityClass.Name}}SaveBodyDTO
    {
        public {{entityClass.Name}}DTO {{entityClass.Name}}DTO { get; set; }
    }
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

            List<SoftProperty> propertiesOfTheCurrentClass = entityClass.Properties.Where(x => x.ClassName == entityClass.Name).ToList();

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
