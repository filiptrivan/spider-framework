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
using Soft.SourceGenerators.Enums;

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
                    predicate: static (s, _) => Helper.IsSyntaxTargetForGenerationEveryClass(s),
                    transform: static (ctx, _) => Helper.GetSemanticTargetForGenerationEveryClass(ctx))
                .Where(static c => c is not null);

            IncrementalValueProvider<List<SoftClass>> referencedProjectClasses = Helper.GetIncrementalValueProviderClassesFromReferencedAssemblies(context,
                new List<NamespaceExtensionCodes>
                {
                    NamespaceExtensionCodes.DTO
                });

            IncrementalValueProvider<string> callingProjectDirectory = context.GetCallingPath();

            var combined = classDeclarations.Collect()
                .Combine(referencedProjectClasses)
                .Combine(callingProjectDirectory);

            context.RegisterImplementationSourceOutput(combined, static (spc, source) =>
            {
                var (classesAndEntities, callingPath) = source;
                var (classes, referencedClasses) = classesAndEntities;

                Execute(classes, referencedClasses, callingPath, spc);
            });
        }

        private static void Execute(IList<ClassDeclarationSyntax> classes, List<SoftClass> referencedClassesDTO, string callingProjectDirectory, SourceProductionContext context)
        {
            if (classes.Count <= 1) 
                return;

            // ...\API\PlayertyLoyals.Business -> ...\Angular\src\app\business\services\translates
            string outputPath = callingProjectDirectory.ReplaceEverythingAfter(@"\API\", @"\Angular\src\app\business\services\translates");

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
            
            Helper.WriteToTheFile(sbClassNames.ToString(), Path.Combine(outputPath, "class-names.generated.ts"));
            Helper.WriteToTheFile(sbLabels.ToString(), Path.Combine(outputPath, "labels.generated.ts"));
        }

        private static List<string> GetCasesForLabelTranslate(List<SoftProperty> DTOProperties)
        {
            List<string> result = new List<string>();
            
            foreach (SoftProperty DTOProperty in DTOProperties.DistinctBy(x => x.Name))
            {
                string propName = DTOProperty.Name;

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
