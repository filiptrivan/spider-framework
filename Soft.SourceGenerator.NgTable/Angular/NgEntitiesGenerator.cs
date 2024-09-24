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
            if (classes.Count <= 1) return; // FT: one because of config settings

            string outputPath = Helper.GetGeneratorOutputPath(nameof(NgEntitiesGenerator), classes);
            List<ClassDeclarationSyntax> DTOClasses = Helper.GetDTOClasses(classes);

            string[] namespacePartsWithoutLastElement = Helper.GetNamespacePartsWithoutLastElement(DTOClasses[0]);
            string projectName = namespacePartsWithoutLastElement.LastOrDefault() ?? "ERROR"; // eg. Security

            StringBuilder sb = new StringBuilder();
            StringBuilder sbImports = new StringBuilder();
            sbImports.Append($$"""
import { BaseEntity } from "../../../core/entities/base-entity";
import { TableFilterContext } from "src/app/core/entities/table-filter-context";
import { TableFilterSortMeta } from "src/app/core/entities/table-filter-sort-meta";

""");

            foreach (IGrouping<string, ClassDeclarationSyntax> DTOClassGroup in DTOClasses.GroupBy(x => x.Identifier.Text)) // Grouping because UserDTO.generated and UserDTO
            {
                List<Prop> DTOProperties = new List<Prop>();
                foreach (ClassDeclarationSyntax DTOClass in DTOClassGroup)
                {
                    DTOProperties.AddRange(Helper.GetAllPropertiesOfTheClass(DTOClass, DTOClasses, true));
                }
                if (DTOProperties.Count == 12)
                {

                }
                List<string> angularPropertyDefinitions = GetAllAngularPropertyDefinitions(DTOProperties, true); // FT: If, in some moment, we want to make another aproach set this to false, now it doesn't matter
                List<string> nullableAngularPropertyDefinitions = GetAllAngularPropertyDefinitions(DTOProperties, true);
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
        {{string.Join("\n\t\t", nullableAngularPropertyDefinitions)}}     
    } = {}
    ) {
        super('{{angularClassIdentifier}}'); 

        {{string.Join("\n\t\t", GetAngularPropertyAssignments(DTOProperties))}}
    }
}
""");

            }

            sb.AppendLine(GetAdditionalEntities());

            sbImports.Append(sb);

            Helper.WriteToTheFile(sbImports.ToString(), $@"{outputPath}\{projectName.FromPascalToKebabCase()}-entities.generated.ts");
        }

        private static List<string> GetAllAngularPropertyDefinitions(List<Prop> DTOProperties, bool alwaysNullable = false)
        {
            List<string> result = new List<string>();
            foreach (Prop DTOProp in DTOProperties.Distinct()) // FT: Trying to solve constant generating duplicate properties in angular with distinct
            {
                string DTOPropLowerCase = DTOProp.IdentifierText.FirstCharToLower();
                string angularIdentifierText;
                if (DTOProp.Type.IsTypeNullable() || alwaysNullable == true)
                    angularIdentifierText = $"{DTOPropLowerCase}?";
                else
                    angularIdentifierText = DTOPropLowerCase;

                string angularDataType = Helper.GetAngularDataType(DTOProp.Type);
                result.Add($"{angularIdentifierText}: {angularDataType};");
            }

            return result;
        }

        private static List<string> GetAngularPropertyAssignments(List<Prop> DTOProperties)
        {
            List<string> result = new List<string>();
            foreach (Prop DTOProp in DTOProperties)
            {
                string DTOPropLowerCase = DTOProp.IdentifierText.FirstCharToLower();
                result.Add($"this.{DTOPropLowerCase} = {DTOPropLowerCase};");
            }

            return result;
        }

        private static string GetAdditionalEntities()
        {
            string additionalEntities = $$"""


// FT HACK: Fake generated class, because of api imports
export class Namebook extends BaseEntity
{
    id?: number;
    displayName?: string;

    constructor(
    {
        id,
        displayName,
    }:{
        id?: number;
        displayName?: string;
    } = {}
    ) {
        super('Namebook');

        this.id = id;
        this.displayName = displayName;
    }
}


// FT HACK: Fake generated class, because of api imports
export class TableFilter extends BaseEntity
{
    filters?: Map<string, TableFilterContext[]>;
    first?: number;
    rows?: number;
    sortField?: string;
    sortOrder?: number;
    multiSortMeta?: TableFilterSortMeta[];

    constructor(
    {
        filters,
        first,
        rows,
        sortField,
        sortOrder,
        multiSortMeta
    }:{
        filters?: Map<string, TableFilterContext[]>;
        first?: number;
        rows?: number;
        sortField?: string;
        sortOrder?: number;
        multiSortMeta?: TableFilterSortMeta[];
    } = {}
    ) {
        super('TableFilter');

        this.filters = filters;
        this.first = first;
        this.rows = rows;
        this.sortField = sortField;
        this.sortOrder = sortOrder;
        this.multiSortMeta = multiSortMeta;
    }
}
""";

            return additionalEntities;
        }
    }
}
