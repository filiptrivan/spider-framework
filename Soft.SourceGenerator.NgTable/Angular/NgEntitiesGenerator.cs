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
        private static void Execute(IList<ClassDeclarationSyntax> DTOClasses, SourceProductionContext context)
        {
            if (DTOClasses.Count == 0) return;
            string[] namespacePartsWithoutLastElement = Helper.GetNamespacePartsWithoutLastElement(DTOClasses[0]);
            string[] namespacePartsWithoutTwoLastElements = namespacePartsWithoutLastElement.Take(namespacePartsWithoutLastElement.Length - 1).ToArray();

            //string projectBasePartOfNamespace = string.Join(".", namespacePartsWithoutLastElement); // eg. Soft.Generator.Security
            string wholeProjectBasePartOfNamespace = string.Join(".", namespacePartsWithoutTwoLastElements); // eg. Soft.Generator

            foreach (IGrouping<string, ClassDeclarationSyntax> DTOClassGroup in DTOClasses.GroupBy(x => x.Identifier.Text)) // Grouping because UserDTO.generated and UserDTO
            {
                StringBuilder sb = new StringBuilder();
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

                sb.AppendLine($$"""
import { BaseEntity } from "../../../core/entities/base-entity";
{{string.Join("\n", Helper.GetAngularImports(DTOProperties))}}

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
                
                Helper.WriteToTheFile(sb.ToString(), $@"E:\Projects\{wholeProjectBasePartOfNamespace}\Source\{wholeProjectBasePartOfNamespace}.SPA\src\app\business\entities\generated\{angularClassIdentifier.FromPascalToKebabCase()}.generated.ts");
            }
        }

        private static List<string> GetAllAngularPropertyDefinitions(List<Prop> DTOProperties, bool alwaysNullable = false)
        {
            List<string> result = new List<string>();
            foreach (Prop DTOProp in DTOProperties)
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
    }
}
