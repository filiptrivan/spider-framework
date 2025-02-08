using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Spider.SourceGenerators.Shared;
using Spider.SourceGenerators.Enums;
using Spider.SourceGenerators.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System;
using System.Data;
using System.Diagnostics;

namespace Spider.SourceGenerators.Net
{
    [Generator]
    public class FluentValidationGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
//#if DEBUG
//            if (!Debugger.IsAttached)
//            {
//                Debugger.Launch();
//            }
//#endif
            IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = Helpers.GetClassInrementalValuesProvider(context.SyntaxProvider, new List<NamespaceExtensionCodes>
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

            var allClasses = classDeclarations.Collect()
                .Combine(referencedProjectClasses);

            context.RegisterImplementationSourceOutput(allClasses, static (spc, source) => Execute(source.Left, source.Right, spc));
        }

        private static void Execute(IList<ClassDeclarationSyntax> classes, List<SpiderClass> referencedProjectClasses, SourceProductionContext context)
        {
            if (classes.Count <= 1) return;

            List<SpiderClass> currentProjectClasses = Helpers.GetSpiderClasses(classes, referencedProjectClasses);
            List<SpiderClass> allClasses = currentProjectClasses.Concat(referencedProjectClasses).ToList();

            List<SpiderClass> currentProjectDTOClasses = Helpers.GetDTOClasses(currentProjectClasses, allClasses);
            List<SpiderClass> currentProjectEntities = currentProjectClasses.Where(x => x.Namespace.EndsWith(".Entities")).ToList();

            string namespaceValue = currentProjectEntities[0].Namespace;
            string basePartOfNamespace = Helpers.GetBasePartOfNamespace(namespaceValue);
            string projectName = Helpers.GetProjectName(namespaceValue);

            string result = $$"""
using FluentValidation;
using {{basePartOfNamespace}}.DTO;
using Spider.Shared.FluentValidation;

namespace {{basePartOfNamespace}}.ValidationRules
{
{{string.Join("\n", GetDTOClassesValidationRules(currentProjectDTOClasses, currentProjectEntities))}}
}
""";

            context.AddSource($"{projectName}ValidationRules.generated", SourceText.From(result, Encoding.UTF8));
        }

        private static List<string> GetDTOClassesValidationRules(List<SpiderClass> currentProjectDTOClasses, List<SpiderClass> currentProjectEntities)
        {
            List<string> result = new();

            foreach (IGrouping<string, SpiderClass> DTOClassGroup in currentProjectDTOClasses.GroupBy(x => x.Name)) // Grouping because UserDTO.generated and UserDTO
            {
                List<SpiderProperty> DTOProperties = new();
                List<SpiderAttribute> DTOAttributes = new();

                foreach (SpiderClass DTOClass in DTOClassGroup)
                {
                    DTOAttributes.AddRange(DTOClass.Attributes);
                    DTOProperties.AddRange(DTOClass.Properties);
                }

                SpiderClass entity = currentProjectEntities.Where(x => DTOClassGroup.Key.Replace("DTO", "") == x.Name).SingleOrDefault(); // If it is null then we only made DTO, without entity class

                List<SpiderValidationRule> rules = Helpers.GetValidationRules(DTOProperties, DTOAttributes, entity);

                result.Add($$"""
    public class {{DTOClassGroup.Key}}ValidationRules : AbstractValidator<{{DTOClassGroup.Key}}>
    {
        public {{DTOClassGroup.Key}}ValidationRules()
        {
{{GetDTOValidationRules(rules)}}
        }
    }
""");
            }

            return result;
        }

        private static string GetDTOValidationRules(List<SpiderValidationRule> rules)
        {
            List<string> result = new();

            foreach (SpiderValidationRule rule in rules)
            {
                result.Add($$"""
            RuleFor(x => x.{{rule.Property.Name}}){{string.Join("", rule.ValidationRuleParts.Select(x => $".{x.Name}({x.MethodParametersBody})"))}};
""");
            }

            return string.Join("\n", result);
        }
    }
}