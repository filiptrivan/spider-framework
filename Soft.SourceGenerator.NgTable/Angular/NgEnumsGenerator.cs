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

            context.RegisterImplementationSourceOutput(enumDeclarations.Collect(),
            static (spc, source) => Execute(source, spc));

        }
        private static void Execute(IList<EnumDeclarationSyntax> enums, SourceProductionContext context)
        {
            if (enums.Count == 0) return;
            string[] namespacePartsWithoutLastElement = Helper.GetNamespacePartsWithoutLastElement(enums[0]);
            string[] namespacePartsWithoutTwoLastElements = namespacePartsWithoutLastElement.Take(namespacePartsWithoutLastElement.Length - 1).ToArray();

            //string projectBasePartOfNamespace = string.Join(".", namespacePartsWithoutLastElement); // eg. Soft.Generator.Security
            string wholeProjectBasePartOfNamespace = string.Join(".", namespacePartsWithoutTwoLastElements); // eg. Soft.Generator

            foreach (EnumDeclarationSyntax enume in enums)
            {
                StringBuilder sb = new StringBuilder();
                string enumName = enume.Identifier.Text;
                List<EnumMember> enumMembers = Helper.GetEnumMembers(enume);
                List<string> angularEnumMemberValuePairs = GetAngularEnumMemberValuePairs(enumMembers);

                sb.AppendLine($$"""
export enum {{enumName}}
{
    {{string.Join("\n\t", angularEnumMemberValuePairs)}}
}
""");
                
                Helper.WriteToTheFile(sb.ToString(), $@"E:\Projects\{wholeProjectBasePartOfNamespace}\Source\{wholeProjectBasePartOfNamespace}.SPA\src\app\business\enums\generated\{enumName.FromPascalToKebabCase()}.generated.ts");
            }
        }

        private static List<string> GetAngularEnumMemberValuePairs(List<EnumMember> enumMembers)
        {
            List<string> result = new List<string>();

            foreach (EnumMember enume in enumMembers)
                result.Add($"{enume.Name} = {enume.Value},");

            return result;
        }

    }
}
