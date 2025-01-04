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
                    predicate: static (s, _) => Helper.IsSyntaxTargetForGenerationAllReferenced(s),
                    transform: static (ctx, _) => Helper.GetSemanticTargetForGenerationAllReferenced(ctx))
                .Where(static c => c is not null);

            IncrementalValueProvider<List<SoftClass>> referencedProjectClasses = Helper.GetIncrementalValueProviderClassesFromReferencedAssemblies(context,
                new List<NamespaceExtensionCodes>
                {
                    NamespaceExtensionCodes.DTO
                });

            var allClasses = classDeclarations.Collect()
                .Combine(referencedProjectClasses);

            context.RegisterImplementationSourceOutput(allClasses, static (spc, source) => Execute(source.Left, source.Right, spc));
        }

        private static void Execute(IList<ClassDeclarationSyntax> classes, List<SoftClass> referencedClassesDTO, SourceProductionContext context)
        {
            if (classes.Count <= 1) 
                return;

            string outputPath = Helper.GetGeneratorOutputPath(nameof(NgTranslatesGenerator), classes);
            //List<SoftClass> DTOClasses = Helper.GetDTOClasses(Helper.GetSoftClasses(classes));

            if (outputPath == null)
                return;

            string[] namespacePartsWithoutLastElement = Helper.GetNamespacePartsWithoutLastElement(classes[0]);
            //string projectName = namespacePartsWithoutLastElement.LastOrDefault() ?? "ERROR"; // eg. Security

            StringBuilder sbClassNames = new StringBuilder();
            StringBuilder sbLabels = new StringBuilder();
            List<SoftProperty> DTOProperties = new List<SoftProperty>();

            foreach (SoftClass DTOClass in referencedClassesDTO)
                DTOProperties.AddRange(DTOClass.Properties);

            sbClassNames.AppendLine($$"""
import { Injectable } from '@angular/core';
import { TranslocoService } from '@jsverse/transloco';

@Injectable({
  providedIn: 'root',
})
export class TranslateClassNamesGeneratedService {

    constructor(
    private translocoService: TranslocoService
    ) {
    }

    translate(name: string): string
    {
        switch(name) 
        {
{{string.Join("\n", GetCasesForClassNameTranslate(referencedClassesDTO))}}
            default:
                return null;
        }
    }
}
""");

            sbLabels.AppendLine($$"""
import { Injectable } from '@angular/core';
import { TranslocoService } from '@jsverse/transloco';

@Injectable({
  providedIn: 'root',
})
export class TranslateLabelsGeneratedService {

    constructor(
        private translocoService: TranslocoService
    ) {
    }

    translate(name: string): string
    {
        switch(name) 
        {
{{string.Join("\n", GetCasesForLabelTranslate(DTOProperties))}}
            default:
                return null;
        }
    }
}
""");
            
            Helper.WriteToTheFile(sbClassNames.ToString(), $@"{outputPath}\class-names.generated.ts");
            Helper.WriteToTheFile(sbLabels.ToString(), $@"{outputPath}\labels.generated.ts");
        }

        private static List<string> GetCasesForLabelTranslate(List<SoftProperty> DTOProperties)
        {
            List<string> result = new List<string>();
            
            foreach (SoftProperty DTOProperty in DTOProperties.DistinctBy(x => x.IdentifierText))
            {
                string propName = DTOProperty.IdentifierText;

                if (propName.EndsWith("Id") && propName != "Id")
                    propName = propName.Substring(0, propName.Length - 2);

                if (propName.EndsWith("DisplayName") && propName != "DisplayName")
                    continue;

                if (propName.EndsWith("CommaSeparated") && propName != "CommaSeparated")
                    propName = propName.Replace("CommaSeparated", "");

                result.Add($$""""
            case '{{propName.FirstCharToLower()}}':
                return this.translocoService.translate('{{propName}}');
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
                return this.translocoService.translate('{{className}}');
"""");
            }

            return result;
        }

    }
}
