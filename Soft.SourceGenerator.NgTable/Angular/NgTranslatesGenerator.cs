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
using CodegenCS;
using Soft.SourceGenerators.Helpers;
using Soft.SourceGenerators.Models;

namespace Soft.SourceGenerator.NgTable.Angular
{
    [Generator]
    public class NgTranslatesGenerator : IIncrementalGenerator
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
                    predicate: static (s, _) => Helper.IsSyntaxTargetForGenerationDTO(s),
                    transform: static (ctx, _) => Helper.GetSemanticTargetForGenerationDTO(ctx))
                .Where(static c => c is not null);

            context.RegisterImplementationSourceOutput(classDeclarations.Collect(),
            static (spc, source) => Execute(source, spc));
        }

        private static void Execute(IList<ClassDeclarationSyntax> classes, SourceProductionContext context)
        {
            if (classes.Count <= 1) return;

            string outputPath = Helper.GetGeneratorOutputPath(nameof(NgTranslatesGenerator), classes);
            List<SoftClass> DTOClasses = Helper.GetDTOClasses(classes);

            string[] namespacePartsWithoutLastElement = Helper.GetNamespacePartsWithoutLastElement(classes[0]);
            string projectName = namespacePartsWithoutLastElement.LastOrDefault() ?? "ERROR"; // eg. Security

            StringBuilder sbClassNames = new StringBuilder();
            StringBuilder sbLabels = new StringBuilder();
            List<SoftProperty> DTOProperties = new List<SoftProperty>();

            foreach (SoftClass DTOClass in DTOClasses)
                DTOProperties.AddRange(DTOClass.Properties);

            sbClassNames.AppendLine($$"""
export function getTranslatedClassName{{projectName}}(name: string): string
{
    switch(name) 
    {
{{string.Join("\n", GetCasesForClassNameTranslate(DTOClasses))}}
        default:
            return null;
    }
}
""");

            sbLabels.AppendLine($$"""
export function getTranslatedLabel{{projectName}}(name: string): string
{
    switch(name) 
    {
{{string.Join("\n", GetCasesForLabelTranslate(DTOProperties))}}
        default:
            return null;
    }
}
""");
            
            Helper.WriteToTheFile(sbClassNames.ToString(), $@"{outputPath}\{projectName.FromPascalToKebabCase()}-class-names.generated.ts");
            Helper.WriteToTheFile(sbLabels.ToString(), $@"{outputPath}\{projectName.FromPascalToKebabCase()}-labels.generated.ts");
        }

        private static List<string> GetCasesForLabelTranslate(List<SoftProperty> DTOProperties)
        {
            List<string> result = new List<string>();
            
            foreach (SoftProperty DTOProperty in DTOProperties.DistinctBy(x => x.IdentifierText))
            {
                if (DTOProperty.IdentifierText.EndsWith("Id") && DTOProperty.IdentifierText != "Id")
                    DTOProperty.IdentifierText = DTOProperty.IdentifierText.Substring(0, DTOProperty.IdentifierText.Length - 2);

                if (DTOProperty.IdentifierText.EndsWith("DisplayName") && DTOProperty.IdentifierText != "DisplayName")
                    continue;

                result.Add($$""""
        case '{{DTOProperty.IdentifierText.FirstCharToLower()}}':
            return $localize`:@@{{DTOProperty.IdentifierText}}:{{DTOProperty.IdentifierText}}`;
"""");
            }

            return result;
        }

        private static List<string> GetCasesForClassNameTranslate(IList<SoftClass> DTOclasses)
        {
            List<string> result = new List<string>();

            foreach (string className in DTOclasses.DistinctBy(x => x.Name).Select(x => x.Name.Replace("DTO", "")))
            {
                result.Add($$""""
        case '{{className}}':
            return $localize`:@@{{className}}:{{className}}`;
"""");
            }

            return result;
        }

    }
}
