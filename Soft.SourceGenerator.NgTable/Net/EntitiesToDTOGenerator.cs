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

        private static void Execute(IList<ClassDeclarationSyntax> entityClasses, SourceProductionContext context)
        {
            if (entityClasses.Count() == 0) return;
            List<ClassDeclarationSyntax> uninheritedEntityClasses = Helper.GetUninheritedClasses(entityClasses.ToList());

            StringBuilder sb = new StringBuilder();

            string[] namespacePartsWithoutLastElement = Helper.GetNamespacePartsWithoutLastElement(entityClasses[0]);

            string basePartOfNamespace = string.Join(".", namespacePartsWithoutLastElement); // eg. Soft.Generator.Security
            string projectName = namespacePartsWithoutLastElement[namespacePartsWithoutLastElement.Length - 1]; // eg. Security
            string[] namespacePartsWithoutTwoLastElements = namespacePartsWithoutLastElement.Take(namespacePartsWithoutLastElement.Length - 1).ToArray();
            string wholeProjectBasePartOfNamespace = string.Join(".", namespacePartsWithoutTwoLastElements); // eg. Soft.Generator

            sb.AppendLine($$"""
using Soft.Generator.Shared.DTO;

namespace {{basePartOfNamespace}}.DTO // FT: Don't change namespace in generator, it's mandatory for partial classes
{
""");
            foreach (ClassDeclarationSyntax c in entityClasses)
            {
                string baseClass = GetDTOBaseType(c);

                sb.AppendLine($$"""
    public partial class {{c.Identifier.Text}}DTO : {{baseClass}}
    {
        {{string.Join("\n\t\t", GetDTOProps(c, entityClasses))}}
    }
""");
            }

            sb.AppendLine($$"""
}
""");

            Helper.WriteToTheFile(sb.ToString(), $@"E:\Projects\{wholeProjectBasePartOfNamespace}\Source\{basePartOfNamespace}\DTO\Generated\{projectName}DTOList.generated.cs");

            // FT: does not generating because we make file on the disk, because mapping can't figure out something inside analyzers
            //context.AddSource($"{projectName}DTOList.generated", SourceText.From(sb.ToString(), Encoding.UTF8));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="c"></param>
        /// <param name="classes">Passing this just to pass it further to the GetGenericIdType method</param>
        /// <returns></returns>
        static List<string> GetDTOProps(ClassDeclarationSyntax c, IList<ClassDeclarationSyntax> classes)
        {
            List<string> props = new List<string>(); // public string Email { get; set; }
            List<PropertyDeclarationSyntax> properties = c.Members.OfType<PropertyDeclarationSyntax>().ToList();

            foreach (PropertyDeclarationSyntax prop in properties)
            {
                string propType = prop.Type.ToString();

                if (propType.PropTypeIsManyToOne())
                {
                    props.Add($"public string {propType}DisplayName {{ get; set; }}");
                    ClassDeclarationSyntax manyToOneClass = classes.Where(x => x.Identifier.Text == propType).Single();
                    props.Add($"public {Helper.GetGenericIdType(manyToOneClass, classes)}? {propType}Id {{ get; set; }}");
                    continue;
                }
                else if (propType.IsEnumerable())
                {
                    continue;
                }
                else if (propType.IsBaseType() && propType != "string")
                {
                    propType = $"{prop.Type}?";
                }
                else if (propType != "string")
                {
                    propType = "UNSUPPORTED TYPE";
                }


                props.Add($"public {propType} {prop.Identifier.Text} {{ get; set; }}");
            }

            return props;
        }

        static string GetDTOBaseType(ClassDeclarationSyntax c)
        {
            string baseClass = Helper.GetBaseType(c);
            if(baseClass.Contains("<"))
                return baseClass.Replace("<", "DTO<");
            else
                return $"{baseClass}DTO";
        }
    }
}
