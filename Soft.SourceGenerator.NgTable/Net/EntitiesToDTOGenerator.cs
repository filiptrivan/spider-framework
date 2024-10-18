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
            List<ClassDeclarationSyntax> uninheritedEntityClasses = Helper.GetUninheritedClasses(entityClasses);

            string outputPath = Helper.GetGeneratorOutputPath(nameof(EntitiesToDTOGenerator), classes);

            StringBuilder sb = new StringBuilder();

            string[] namespacePartsWithoutLastElement = Helper.GetNamespacePartsWithoutLastElement(entityClasses[0]);

            string basePartOfNamespace = string.Join(".", namespacePartsWithoutLastElement); // eg. Soft.Generator.Security
            string projectName = namespacePartsWithoutLastElement[namespacePartsWithoutLastElement.Length - 1]; // eg. Security

            sb.AppendLine($$"""
using Microsoft.AspNetCore.Http;
using Soft.Generator.Shared.DTO;
using Soft.Generator.Security.DTO;
using Soft.Generator.Shared.Helpers;

namespace {{basePartOfNamespace}}.DTO // FT: Don't change namespace in generator, it's mandatory for partial classes
{
""");
            foreach (ClassDeclarationSyntax c in entityClasses)
            {
                string baseClass = c.GetDTOBaseType();

                sb.AppendLine($$"""
    public partial class {{c.Identifier.Text}}DTO {{(baseClass == null ? "" : $": {baseClass}")}}
    {
        {{string.Join("\n\t\t", GetDTOWithoutBaseProps(c, entityClasses))}}
    }
""");
            }

            sb.AppendLine($$"""
}
""");

            context.AddSource($"{projectName}DTOList.generated", SourceText.From(sb.ToString(), Encoding.UTF8));
        }

        /// <summary>
        /// </summary>
        /// <param name="c"></param>
        /// <param name="classes">Passing this just to pass it further to the GetGenericIdType method</param>
        /// <returns></returns>
        private static List<string> GetDTOWithoutBaseProps(ClassDeclarationSyntax entityClass, IList<ClassDeclarationSyntax> entityClasses)
        {
            List<string> props = new List<string>(); // public string Email { get; set; }
            List<SoftProperty> properties = Helper.GetPropsOfCurrentClass(entityClass);

            foreach (SoftProperty prop in properties)
            {
                string propType = prop.Type;
                string propName = prop.IdentifierText;

                if (propType.PropTypeIsManyToOne())
                {
                    props.Add($"public string {propName}DisplayName {{ get; set; }}");
                    ClassDeclarationSyntax manyToOneClass = entityClasses.Where(x => x.Identifier.Text == propType).Single();
                    props.Add($"public {Helper.GetGenericIdType(manyToOneClass, entityClasses)}? {propName}Id {{ get; set; }}");
                    continue;
                }
                else if (propType.IsEnumerable() && prop.Attributes.Any(x => x.Name == "GenerateCommaSeparatedDisplayName"))
                {
                    props.Add($"public string {propName}CommaSeparated {{ get; set; }}");
                    continue;
                }
                else if (propType == "byte[]")
                {
                    props.Add($"public string {propName} {{ get; set; }}");
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
                    props.Add($"public string {propName}Data {{ get; set; }}");
                    props.Add($"public MimeTypes {propName}MimeType {{ get; set; }}");
                }
                else if (propType != "string")
                {
                    propType = "UNSUPPORTED TYPE";
                }

                props.Add($"public {propType} {propName} {{ get; set; }}");
            }

            return props;
        }

    }
}
