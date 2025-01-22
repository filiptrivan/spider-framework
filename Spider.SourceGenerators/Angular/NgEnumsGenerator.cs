using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;
using System.Diagnostics;
using Spider.SourceGenerators.Shared;
using Spider.SourceGenerators.Models;
using Spider.SourceGenerators.Enums;

namespace Spider.SourceGenerators.Angular
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
                    predicate: static (s, _) => Helpers.IsEnumSyntaxTargetForGeneration(s),
                    transform: static (ctx, _) => Helpers.GetEnumSemanticTargetForGeneration(ctx))
                .Where(static c => c is not null);

            IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = Helpers.GetClassInrementalValuesProvider(context.SyntaxProvider, new List<NamespaceExtensionCodes>
                {
                    NamespaceExtensionCodes.Entities,
                });

            IncrementalValueProvider<string> callingProjectDirectory = context.GetCallingPath();

            var combined = enumDeclarations.Collect()
                .Combine(classDeclarations.Collect())
                .Combine(callingProjectDirectory);

            context.RegisterImplementationSourceOutput(combined, static (spc, source) =>
            {
                var (enumsAndClasses, callingPath) = source;
                var (enums, classDeclarations) = enumsAndClasses;

                Execute(enums, classDeclarations, callingPath, spc);
            });
        }

        private static void Execute(IList<EnumDeclarationSyntax> enums, IList<ClassDeclarationSyntax> classes, string callingProjectDirectory, SourceProductionContext context)
        {
            if (enums.Count == 0) 
                return;

            string[] namespacePartsWithoutLastElement = Helpers.GetNamespacePartsWithoutLastElement(classes[0]);
            string projectName = namespacePartsWithoutLastElement.LastOrDefault() ?? "ERROR"; // eg. Security

            // ...\API\PlayertyLoyals.Business -> ...\Angular\src\app\business\enums\{projectName}-enums.ts
            string outputPath = callingProjectDirectory.ReplaceEverythingAfter(@"\API\", $@"\Angular\src\app\business\enums\{projectName.FromPascalToKebabCase()}-enums.generated.ts");

            List<ClassDeclarationSyntax> entityClasses = Helpers.GetEntityClasses(classes);

            StringBuilder sb = new StringBuilder();

            foreach (EnumDeclarationSyntax enume in enums.OrderBy(x => x.Identifier.Text).ToList())
            {
                string enumName = enume.Identifier.Text;
                List<SpiderEnum> enumMembers = Helpers.GetEnumMembers(enume);
                List<string> angularEnumMemberValuePairs = GetAngularEnumMemberValuePairs(enumMembers);

                List<string> entityPermissionCodes = new List<string>();
                if (enumName == "PermissionCodes")
                    entityPermissionCodes = Helpers.GetPermissionCodesForEntites(entityClasses);

                sb.AppendLine($$"""
export enum {{enumName}}
{
    {{string.Join("\n\t", angularEnumMemberValuePairs)}}
    {{string.Join(",\n\t", entityPermissionCodes)}}
}

""");
            }

            Helpers.WriteToTheFile(sb.ToString(), outputPath);
        }

        private static List<string> GetAngularEnumMemberValuePairs(List<SpiderEnum> enumMembers)
        {
            List<string> result = new List<string>();

            foreach (SpiderEnum enume in enumMembers)
            {
                if(enume.Value != null)
                    result.Add($"{enume.Name} = {enume.Value},");
                else
                    result.Add($"{enume.Name},");
            }

            return result;
        }

    }
}
