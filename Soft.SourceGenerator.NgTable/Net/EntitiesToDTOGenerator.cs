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

            context.RegisterImplementationSourceOutput(classDeclarations.Collect(),
                static (spc, source) => Execute(source, spc));
        }

        private static void Execute(IList<ClassDeclarationSyntax> classes, SourceProductionContext context)
        {
            if (classes.Count <= 1) return;

            List<ClassDeclarationSyntax> entityClasses = Helper.GetEntityClasses(classes);

            StringBuilder sb = new StringBuilder();

            string[] namespacePartsWithoutLastElement = Helper.GetNamespacePartsWithoutLastElement(entityClasses[0]);

            string basePartOfNamespace = string.Join(".", namespacePartsWithoutLastElement); // eg. Soft.Generator.Security
            string projectName = namespacePartsWithoutLastElement[namespacePartsWithoutLastElement.Length - 1]; // eg. Security

            sb.AppendLine($$"""
{{GetUsings()}}

namespace {{basePartOfNamespace}}.DTO
{
""");
            foreach (ClassDeclarationSyntax entityClass in entityClasses)
            {
                string baseClass = entityClass.GetDTOBaseType();

                sb.AppendLine($$"""
    public partial class {{entityClass.Identifier.Text}}DTO {{(baseClass == null ? "" : $": {baseClass}")}}
    {
        {{string.Join("\n\t\t", GetDTOPropertiesWithoutBaseType(entityClass, entityClasses))}}
    }
""");
            }

            sb.AppendLine($$"""
}
""");

            context.AddSource($"{projectName}DTOList.generated", SourceText.From(sb.ToString(), Encoding.UTF8));
        }

        /// <summary>
        /// Getting the properties of the DTO based on the entity class, we don't include base type properties because of the inheritance
        /// </summary>
        private static List<string> GetDTOPropertiesWithoutBaseType(ClassDeclarationSyntax entityClass, IList<ClassDeclarationSyntax> entityClasses)
        {
            List<string> DTOproperties = new List<string>(); // public string Email { get; set; }

            List<SoftProperty> propertiesEntityClass = Helper.GetPropsOfCurrentClass(entityClass);

            foreach (SoftProperty prop in propertiesEntityClass)
            {
                string propType = prop.Type;
                string propName = prop.IdentifierText;

                if (propType.PropTypeIsManyToOne())
                {
                    DTOproperties.Add($"public string {propName}DisplayName {{ get; set; }}");
                    ClassDeclarationSyntax manyToOneClass = entityClasses.Where(x => x.Identifier.Text == propType).Single();
                    DTOproperties.Add($"public {Helper.GetGenericIdType(manyToOneClass, entityClasses)}? {propName}Id {{ get; set; }}");
                    continue;
                }
                else if (propType.IsEnumerable() && prop.Attributes.Any(x => x.Name == "GenerateCommaSeparatedDisplayName"))
                {
                    DTOproperties.Add($"public string {propName}CommaSeparated {{ get; set; }}");
                    continue;
                }
                else if (propType == "byte[]")
                {
                    DTOproperties.Add($"public string {propName} {{ get; set; }}");
                    continue;
                }
                else if (propType.IsEnumerable() && prop.Attributes.Any(x => x.Name == "Map"))
                {
                    string DTOListPropType = propType.Replace(">", "DTO>");
                    DTOproperties.Add($"public {DTOListPropType} {propName} {{ get; set; }}");
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
                    DTOproperties.Add($"public string {propName}Data {{ get; set; }}");
                }
                else if (propType != "string")
                {
                    propType = "UNSUPPORTED TYPE";
                }

                DTOproperties.Add($"public {propType} {propName} {{ get; set; }}"); // string
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
