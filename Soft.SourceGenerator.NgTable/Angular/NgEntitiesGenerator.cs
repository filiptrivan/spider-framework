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
using static System.Net.WebRequestMethods;

namespace Soft.SourceGenerator.NgTable.Angular
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
            if (classes.Count <= 1) 
                return; // FT: one because of config settings

            string outputPath = Helper.GetGeneratorOutputPath(nameof(NgEntitiesGenerator), classes);

            if (outputPath == null)
                return;

            List<SoftClass> DTOClasses = Helper.GetDTOClasses(Helper.GetSoftClasses(classes));

            string[] namespacePartsWithoutLastElement = Helper.GetNamespacePartsWithoutLastElement(classes[0]);
            string projectName = namespacePartsWithoutLastElement.LastOrDefault() ?? "ERROR"; // eg. Security

            StringBuilder sb = new StringBuilder();
            StringBuilder sbImports = new StringBuilder();
            sbImports.Append($$"""
import { BaseEntity } from "../../../core/entities/base-entity";
import { TableFilter } from "src/app/core/entities/table-filter";
import { TableFilterContext } from "src/app/core/entities/table-filter-context";
import { TableFilterSortMeta } from "src/app/core/entities/table-filter-sort-meta";
import { MimeTypes } from "src/app/core/entities/mime-type";
{{string.Join("\n", GetEnumPropertyImports(DTOClasses, projectName))}}

""");

            foreach (IGrouping<string, SoftClass> DTOClassGroup in DTOClasses.GroupBy(x => x.Name)) // Grouping because UserDTO.generated and UserDTO
            {
                List<SoftProperty> DTOProperties = new List<SoftProperty>();

                foreach (SoftClass DTOClass in DTOClassGroup) // It can only be 2 here
                    DTOProperties.AddRange(DTOClass.Properties);

                List<string> angularPropertyDefinitions = GetAllAngularPropertyDefinitions(DTOProperties); // FT: If, in some moment, we want to make another aproach set this to false, now it doesn't matter
                string angularClassIdentifier = DTOClassGroup.Key.Replace("DTO", "");

                sbImports.Append(string.Join("\n", Helper.GetAngularImports(DTOProperties, projectName)));

                sb.AppendLine($$"""


export class {{angularClassIdentifier}} extends BaseEntity
{
    {{string.Join("\n\t", angularPropertyDefinitions)}}

    constructor(
    {
        {{string.Join(",\n\t\t", DTOProperties.Select(x => x.IdentifierText.FirstCharToLower()))}}
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

            Helper.WriteToTheFile(sbImports.ToString(), $@"{outputPath}\{projectName.FromPascalToKebabCase()}-entities.generated.ts");
        }

        private static List<string> GetAllAngularPropertyDefinitions(List<SoftProperty> DTOProperties)
        {
            List<string> result = new List<string>();
            foreach (SoftProperty DTOProp in DTOProperties)
            {
                string DTOPropLowerCase = DTOProp.IdentifierText.FirstCharToLower();

                string angularDataType = Helper.GetAngularType(DTOProp.Type);
                result.Add($"{DTOPropLowerCase}?: {angularDataType};");
            }

            return result;
        }

        private static List<string> GetAngularPropertyAssignments(List<SoftProperty> DTOProperties)
        {
            List<string> result = new List<string>();
            foreach (SoftProperty DTOProp in DTOProperties)
            {
                string DTOPropLowerCase = DTOProp.IdentifierText.FirstCharToLower();
                result.Add($"this.{DTOPropLowerCase} = {DTOPropLowerCase};");
            }

            return result;
        }

        private static List<string> GetEnumPropertyImports(List<SoftClass> DTOClasses, string projectName)
        {
            List<string> result = new List<string>();

            foreach (IGrouping<string, SoftClass> DTOClassGroup in DTOClasses.GroupBy(x => x.Name)) // Grouping because UserDTO.generated and UserDTO
            {
                List<SoftProperty> DTOProperties = new List<SoftProperty>();

                foreach (SoftClass DTOClass in DTOClassGroup) // It can only be 2 here
                    DTOProperties.AddRange(DTOClass.Properties);

                foreach (SoftProperty property in DTOProperties)
                {
                    if (property.Type.IsEnum() == false)
                        continue;

                    if (result.Contains(property.IdentifierText) == false)
                    {
                        result.Add($$"""
import { {{property.IdentifierText}} } from "../../enums/generated/{{projectName.FromPascalToKebabCase()}}-enums.generated";
""");
                    }
                }
            }

            return result;
        }


    }
}
