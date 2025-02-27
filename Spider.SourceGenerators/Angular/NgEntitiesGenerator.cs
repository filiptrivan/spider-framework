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
using Spider.SourceGenerators.Models;
using Spider.SourceGenerators.Enums;

namespace Spider.SourceGenerators.Angular
{
    [Generator]
    public class NgEntitiesGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            //#if DEBUG
            //            if (!Debugger.IsAttached)
            //            {
            //                Debugger.Launch();
            //            }
            //#endif
            IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = Helpers.GetClassIncrementalValuesProvider(context.SyntaxProvider, new List<NamespaceExtensionCodes>
                {
                    NamespaceExtensionCodes.Entities,
                    NamespaceExtensionCodes.DTO,
                });


            IncrementalValueProvider<List<SpiderClass>> referencedProjectClasses = Helpers.GetIncrementalValueProviderClassesFromReferencedAssemblies(context,
                new List<NamespaceExtensionCodes>
                {
                    NamespaceExtensionCodes.Entities,
                    NamespaceExtensionCodes.DTO,
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

        private static void Execute(IList<ClassDeclarationSyntax> classes, List<SpiderClass> referencedProjectClasses, string callingProjectDirectory, SourceProductionContext context)
        {
            if (classes.Count <= 1)
                return; // FT: one because of config settings

            List<SpiderClass> currentProjectClasses = Helpers.GetSpiderClasses(classes, referencedProjectClasses);
            List<SpiderClass> allClasses = currentProjectClasses.Concat(referencedProjectClasses).ToList();
            List<SpiderClass> currentProjectDTOClasses = Helpers.GetDTOClasses(currentProjectClasses, allClasses);

            string namespaceValue = currentProjectClasses[0].Namespace;
            string projectName = Helpers.GetProjectName(namespaceValue);

            // ...\API\PlayertyLoyals.Business -> ...\Angular\src\app\business\entities\{projectName}-entities.ts
            string outputPath = callingProjectDirectory.ReplaceEverythingAfter(@"\API\", $@"\Angular\src\app\business\entities\{projectName.FromPascalToKebabCase()}-entities.generated.ts");

            StringBuilder sb = new();
            StringBuilder sbImports = new();
            sbImports.Append($$"""
import { BaseEntity, TableFilter, TableFilterContext, TableFilterSortMeta, MimeTypes, Namebook } from '@playerty/spider';
{{string.Join("\n", GetEnumPropertyImports(currentProjectDTOClasses, projectName))}}

""");

            foreach (IGrouping<string, SpiderClass> DTOClassGroup in currentProjectDTOClasses.GroupBy(x => x.Name)) // Grouping because UserDTO.generated and UserDTO
            {
                List<SpiderProperty> DTOProperties = new();

                foreach (SpiderClass DTOClass in DTOClassGroup) // It can only be 2 here
                    DTOProperties.AddRange(DTOClass.Properties);

                List<string> angularPropertyDefinitions = GetAllAngularPropertyDefinitions(DTOProperties); // FT: If, in some moment, we want to make another aproach set this to false, now it doesn't matter
                string angularClassIdentifier = DTOClassGroup.Key.Replace("DTO", "");

                sbImports.Append(string.Join("\n", Helpers.GetAngularImports(DTOProperties, projectName)));

                sb.AppendLine($$"""


export class {{angularClassIdentifier}} extends BaseEntity
{
    {{string.Join("\n\t", angularPropertyDefinitions)}}

    constructor(
    {
        {{string.Join(",\n\t\t", DTOProperties.Select(x => x.Name.FirstCharToLower()))}}
    }:{
        {{string.Join("\n\t\t", angularPropertyDefinitions)}}     
    } = {}
    ) {
        super('{{angularClassIdentifier}}'); 

        {{string.Join("\n\t\t", GetAngularPropertyAssignments(DTOProperties))}}
    }
}
""");

            }

            sbImports.Append(sb);

            Helpers.WriteToTheFile(sbImports.ToString(), outputPath);
        }

        private static List<string> GetAllAngularPropertyDefinitions(List<SpiderProperty> DTOProperties)
        {
            List<string> result = new();

            foreach (SpiderProperty DTOProp in DTOProperties)
            {
                string DTOPropLowerCase = DTOProp.Name.FirstCharToLower();

                string angularDataType = Helpers.GetAngularType(DTOProp.Type);
                result.Add($"{DTOPropLowerCase}?: {angularDataType};");
            }

            return result;
        }

        private static List<string> GetAngularPropertyAssignments(List<SpiderProperty> DTOProperties)
        {
            List<string> result = new();

            foreach (SpiderProperty DTOProp in DTOProperties)
            {
                string DTOPropLowerCase = DTOProp.Name.FirstCharToLower();
                result.Add($"this.{DTOPropLowerCase} = {DTOPropLowerCase};");
            }

            return result;
        }

        private static List<string> GetEnumPropertyImports(List<SpiderClass> DTOClasses, string projectName)
        {
            List<string> result = new();

            foreach (IGrouping<string, SpiderClass> DTOClassGroup in DTOClasses.GroupBy(x => x.Name)) // Grouping because UserDTO.generated and UserDTO
            {
                List<SpiderProperty> DTOProperties = new List<SpiderProperty>();

                foreach (SpiderClass DTOClass in DTOClassGroup) // It can only be 2 here
                    DTOProperties.AddRange(DTOClass.Properties);

                foreach (SpiderProperty property in DTOProperties.Where(x => x.Type.IsEnum()))
                {
                    if (result.Contains(property.Name) == false)
                    {
                        result.Add($$"""
import { {{property.Type}} } from "../enums/{{projectName.FromPascalToKebabCase()}}-enums.generated";
""");
                    }
                }
            }

            return result;
        }


    }
}
