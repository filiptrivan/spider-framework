using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;
using Spider.SourceGenerators.Shared;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;
using System.Diagnostics;
using CodegenCS;
using Spider.SourceGenerators.Enums;
using Spider.SourceGenerators.Models;

namespace Spider.SourceGenerators.Angular
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
                    predicate: static (s, _) => Helpers.IsSyntaxTargetForGenerationEveryClass(s),
                    transform: static (ctx, _) => Helpers.GetSemanticTargetForGenerationEveryClass(ctx))
                .Where(static c => c is not null);

            IncrementalValueProvider<List<SpiderClass>> referencedProjectClasses = Helpers.GetIncrementalValueProviderClassesFromReferencedAssemblies(context,
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

        private static void Execute(IList<ClassDeclarationSyntax> classes, List<SpiderClass> referencedClassesDTO, string callingProjectDirectory, SourceProductionContext context)
        {
            if (classes.Count <= 1) 
                return;

            // ...\API\PlayertyLoyals.Business -> ...\Angular\src\app\business\services\translates
            string outputPath = callingProjectDirectory.ReplaceEverythingAfter(@"\API\", @"\Angular\src\app\business\services\translates");

            StringBuilder sbClassNames = new StringBuilder();
            StringBuilder sbLabels = new StringBuilder();
            List<SpiderProperty> DTOProperties = new List<SpiderProperty>();

            referencedClassesDTO = referencedClassesDTO.OrderBy(x => x.Name).ToList();

            foreach (SpiderClass DTOClass in referencedClassesDTO)
            {
                DTOProperties.AddRange(DTOClass.Properties);
            }

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
            
            Helpers.WriteToTheFile(sbClassNames.ToString(), Path.Combine(outputPath, "class-names.generated.ts"));
            Helpers.WriteToTheFile(sbLabels.ToString(), Path.Combine(outputPath, "labels.generated.ts"));
        }

        private static List<string> GetCasesForLabelTranslate(List<SpiderProperty> DTOProperties)
        {
            List<string> result = new List<string>();
            
            foreach (SpiderProperty DTOProperty in DTOProperties)
            {
                string propName = DTOProperty.Name;

                if (propName.EndsWith("Id") && propName != "Id")
                    propName = propName.Substring(0, propName.Length - 2);

                if (propName.EndsWith("DisplayName") && propName != "DisplayName")
                    continue;

                if (propName.EndsWith("CommaSeparated") && propName != "CommaSeparated")
                    propName = propName.Replace("CommaSeparated", "");

                string singleCase = $$"""
            case '{{propName.FirstCharToLower()}}':
                return this.translocoService.translate('{{propName}}');
""";

                if (result.Contains(singleCase) == false)
                    result.Add(singleCase);
            }

            return result;
        }

        private static List<string> GetCasesForClassNameTranslate(IList<SpiderClass> DTOclasses)
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
