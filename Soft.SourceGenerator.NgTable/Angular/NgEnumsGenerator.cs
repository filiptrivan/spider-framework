using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;
using Soft.SourceGenerator.NgTable.Helpers;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;
using System.Diagnostics;
using Soft.SourceGenerators.Helpers;
using Soft.SourceGenerators.Models;

namespace Soft.SourceGenerator.NgTable.Angular
{
    [Generator]
    public class NgEnumsGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
//#if DEBUG
//            if (!Debugger.IsAttached)
//            {
//                Debugger.Launch();
//            }
//#endif
            IncrementalValuesProvider<EnumDeclarationSyntax> enumDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => Helper.IsSyntaxTargetForGenerationEnums(s),
                    transform: static (ctx, _) => Helper.GetSemanticTargetForGenerationEnums(ctx))
                .Where(static c => c is not null);

            IncrementalValuesProvider<ClassDeclarationSyntax> settingsDeclaration = context.SyntaxProvider
               .CreateSyntaxProvider(
                   predicate: static (s, _) => Helper.IsSyntaxTargetForGenerationSettings(s),
                   transform: static (ctx, _) => Helper.GetSemanticTargetForGenerationSettings(ctx))
               .Where(static c => c is not null);

            var combinedDeclarations = enumDeclarations.Collect().Combine(settingsDeclaration.Collect());

            context.RegisterImplementationSourceOutput(combinedDeclarations,
            static (spc, source) => Execute(source.Left, source.Right, spc));

        }
        private static void Execute(IList<EnumDeclarationSyntax> enums, IList<ClassDeclarationSyntax> settings, SourceProductionContext context)
        {
            if (enums.Count == 0) return;

            string outputPath = Helper.GetGeneratorOutputPath(nameof(NgEnumsGenerator), settings);
            if (outputPath == null) return;

            string[] namespacePartsWithoutLastElement = Helper.GetNamespacePartsWithoutLastElement(enums[0]);
            string projectName = namespacePartsWithoutLastElement.LastOrDefault() ?? "ERROR"; // eg. Security

            StringBuilder sb = new StringBuilder();

            foreach (EnumDeclarationSyntax enume in enums)
            {
                string enumName = enume.Identifier.Text;
                List<SoftEnum> enumMembers = Helper.GetEnumMembers(enume);
                List<string> angularEnumMemberValuePairs = GetAngularEnumMemberValuePairs(enumMembers);

                sb.AppendLine($$"""
export enum {{enumName}}
{
    {{string.Join("\n\t", angularEnumMemberValuePairs)}}
}

""");
            }

            Helper.WriteToTheFile(sb.ToString(), $@"{outputPath}\{projectName.FromPascalToKebabCase()}-enums.generated.ts");
        }

        private static List<string> GetAngularEnumMemberValuePairs(List<SoftEnum> enumMembers)
        {
            List<string> result = new List<string>();

            foreach (SoftEnum enume in enumMembers)
                result.Add($"{enume.Name} = {enume.Value},");

            return result;
        }

    }
}
