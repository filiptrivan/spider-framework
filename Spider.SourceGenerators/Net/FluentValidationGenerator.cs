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
{{string.Join("\n", GetDTOValidationRules(currentProjectDTOClasses, currentProjectEntities))}}
}
""";

            context.AddSource($"{projectName}ValidationRules.generated", SourceText.From(result, Encoding.UTF8));
        }

        private static List<string> GetDTOValidationRules(List<SpiderClass> currentProjectDTOClasses, List<SpiderClass> currentProjectEntities)
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

                result.Add($$"""
    public class {{DTOClassGroup.Key}}ValidationRules : AbstractValidator<{{DTOClassGroup.Key}}>
    {
        public {{DTOClassGroup.Key}}ValidationRules()
        {
            {{string.Join("\n\t\t\t", GetValidationRules(DTOProperties, DTOAttributes, entity, currentProjectEntities))}}
        }
    }
""");
            }

            return result;
        }

        /// <summary>
        /// Getting the validation rules for the single object (DTO + Entity)
        /// </summary>
        /// <param name="DTOProperties">Including the attributes</param>
        /// <param name="entity">User</param>
        /// <returns>List of rules: eg. [RuleFor(x => x.Username).Length(0, 70), RuleFor(x => x.Email).Length(0, 70)]</returns>
        public static List<string> GetValidationRules(List<SpiderProperty> DTOProperties, List<SpiderAttribute> DTOAttributes, SpiderClass entity, List<SpiderClass> currentProjectEntities)
        {
            // [RuleFor(x => x.Username).Length(0, 70);, RuleFor(x => x.Email).Length(0, 70);]
            List<string> rulesOnDTO = new(); // priority - 1.
            List<string> rulesOnDTOProperties = new(); // priority - 2.
            List<string> rulesOnEntity = new(); // priority - 3.
            List<string> rulesOnEntityProperties = new(); // priority - 4.

            rulesOnDTO.AddRange(GetRulesForAttributes(DTOAttributes));

            foreach (SpiderProperty DTOproperty in DTOProperties)
            {
                string rule = GetRuleForProperty(DTOproperty);

                if (rule != null)
                    rulesOnDTOProperties.Add(rule);
            }

            if (entity != null) // FT: If it is null then we only made DTO, without entity class
            {
                rulesOnEntity.AddRange(GetRulesForAttributes(entity.Attributes));

                foreach (SpiderProperty property in entity.Properties)
                {
                    string rule = GetRuleForProperty(property);

                    if (rule != null)
                        rulesOnEntityProperties.Add(rule);
                }
            }

            List<string> mergedValidationRules = GetMergedValidationRules(rulesOnDTO, rulesOnDTOProperties, rulesOnEntity, rulesOnEntityProperties);

            return mergedValidationRules;
        }

        private static List<string> GetRulesForAttributes(List<SpiderAttribute> attributes)
        {
            List<string> rules = new();

            foreach (SpiderAttribute attribute in attributes)
            {
                if (attribute.Name == "CustomValidator")
                    rules.Add(attribute.Value);
            }

            return rules;
        }

        static string GetRuleForProperty(SpiderProperty property)
        {
            if (property.Type.IsEnumerable())
                return null;

            string rulePropertyName = GetPropertyNameForRule(property);
            List<string> singleRulesOnProperty = GetRulePartsForProperty(property, rulePropertyName); // NotEmpty(), Length(0, 70);

            if (singleRulesOnProperty.Count == 0)
                return null;

            return $"RuleFor(x => x.{rulePropertyName}).{string.Join(".", singleRulesOnProperty)};";
        }

        static List<string> GetRulePartsForProperty(SpiderProperty property, string rulePropertyName)
        {
            List<string> ruleParts = new List<string>();

            foreach (SpiderAttribute attribute in property.Attributes)
            {
                switch (attribute.Name)
                {
                    case "Required":
                        ruleParts.Add("NotEmpty()");
                        break;
                    case "ManyToOneRequired":
                        ruleParts.Add("NotEmpty()");
                        break;
                    case "Column":
                        if (attribute.Value.Contains("VARCHAR"))
                            ruleParts.Add($"Length(0, {FindNumberBetweenVarcharParentheses(attribute.Value)})");
                        break;
                    case "StringLength":
                        string minValue = FindMinValueForStringLength(attribute.Value);
                        if (minValue == null)
                            ruleParts.Add($"Length({FindMaxValueForStringLength(attribute.Value)})");
                        else
                            ruleParts.Add($"Length({minValue}, {FindMaxValueForStringLength(attribute.Value)})");
                        break;
                    case "Precision":
                        ruleParts.Add($"PrecisionScale({attribute.Value}, false)"); // FT: only here the attribute.Value should be two values eg. 6, 7
                        break;
                    case "Range":
                        ruleParts.Add($"GreaterThanOrEqualTo({attribute.Value.Split(',')[0].Trim()})");
                        ruleParts.Add($"LessThanOrEqualTo({attribute.Value.Split(',')[1].Trim()})");
                        break;
                    case "GreaterThanOrEqualTo":
                        ruleParts.Add($"GreaterThanOrEqualTo({attribute.Value})");
                        break;
                    case "CustomValidator":
                        ruleParts.Add(attribute.Value);
                        break;
                    default:
                        break;
                }
            }

            // FT: If there is no Required nor ManyToOneRequired attribute, we should let user save null to database
            if (ruleParts.Count > 0 && property.Attributes.Any(x => x.Name == "Required" || x.Name == "ManyToOneRequired") == false)
            {
                if (property.Type == "string")
                    ruleParts.Add($"Unless(i => string.IsNullOrEmpty(i.{rulePropertyName}))");
                else
                    ruleParts.Add($"Unless(i => i.{rulePropertyName} == null)");
            }

            return ruleParts;
        }

        /// <summary>
        /// Getting merged validation rules for the single object (DTO + Entity)
        /// </summary>
        /// <returns></returns>
        private static List<string> GetMergedValidationRules(List<string> rulesOnDTO, List<string> rulesOnDTOProperties, List<string> rulesOnEntity, List<string> rulesOnEntityProperties)
        {
            List<string> mergedRules = new();

            foreach (var group in rulesOnDTO.Concat(rulesOnDTOProperties).Concat(rulesOnEntity).Concat(rulesOnEntityProperties).GroupBy(x => GetRuleIdentifierPart(x)))
            {
                List<string> rulePartsOnDTO = GetRuleParts(rulesOnDTO, group.Key);
                List<string> rulePartsOnDTOProperties = GetRuleParts(rulesOnDTOProperties, group.Key);
                List<string> rulePartsOnEntity = GetRuleParts(rulesOnEntity, group.Key);
                List<string> rulePartsOnEntityProperties = GetRuleParts(rulesOnEntityProperties, group.Key);

                RemoveDuplicateRuleParts([rulePartsOnDTOProperties, rulePartsOnEntity, rulePartsOnEntityProperties], rulePartsOnDTO);
                RemoveDuplicateRuleParts([rulePartsOnEntity, rulePartsOnEntityProperties], rulePartsOnDTOProperties);
                RemoveDuplicateRuleParts([rulePartsOnEntityProperties], rulePartsOnEntity);

                List<string> mergedRuleParts = rulePartsOnDTO.Concat(rulePartsOnDTOProperties).Concat(rulePartsOnEntity).Concat(rulePartsOnEntityProperties).ToList();

                mergedRules.Add($"{group.Key}){string.Join("", mergedRuleParts)};");
            }

            return mergedRules;
        }

        private static void RemoveDuplicateRuleParts(List<List<string>> rulePartsToRemove, List<string> priorRuleParts)
        {
            for (int i = 0; i < rulePartsToRemove.Count; i++)
            {
                for (int j = 0; j < rulePartsToRemove[i].Count; j++)
                {
                    foreach (string priorRulePart in priorRuleParts)
                    {
                        string rulePartName = GetRulePartName(priorRulePart); // .Length(
                        if (rulePartsToRemove[i][j].StartsWith(rulePartName))
                        {
                            rulePartsToRemove[i].RemoveAt(j);
                        }
                    }
                }
            }
        }

        ///// <param name="rule">RuleFor(x => x.Username).Length(0, 70).NotEmpty();</param>
        ///// <returns>.Length, .NotEmpty</returns>
        //private static List<string> GetRulePartNames(string rule)
        //{
        //    List<string> helper = rule.Split('(').ToList(); // "x => x.Username).Length", "0, 70).NotEmpty"
        //    List<string> rulePartNames = helper.Select(x => x.Substring(0, x.LastIndexOf(')') + 1)).ToList(); // .Length, .NotEmpty
        //    return rulePartNames;
        //}

        /// <param name="rule">RuleFor(x => x.Username).Length(0, 70).Required();</param>
        /// <returns>.Length(0, 70), .Required()</returns>
        private static List<string> GetRuleParts(List<string> rules, string ruleIdentifierPart)
        {
            string rule = rules.Where(x => GetRuleIdentifierPart(x) == ruleIdentifierPart).SingleOrDefault();

            if (rule == null)
                return new List<string>();

            return rule.Split(')').Skip(1).Select(x => $"{x})").SkipLast().ToList(); // ".Length(0, 70)", ".Required()"
        }

        /// <summary>
        /// .Length(0, 60) -> .Length(
        /// </summary>
        private static string GetRulePartName(string rulePart)
        {
            return rulePart.ReplaceEverythingAfter("(", "(");
        }

        #region Helpers

        private static string GetPropertyNameForRule(SpiderProperty property)
        {
            if (property.Type.IsManyToOneType())  // FT: if it is not base type and not enumerable than it's many to one for sure, and the validation can only be for id to be required
                return $"{property.Name}Id";

            return property.Name;
        }

        /// <returns>RuleFor(x => x.Username</returns>
        private static string GetRuleIdentifierPart(string rule)
        {
            return rule.Split(')').First();
        }

        static string FindNumberBetweenVarcharParentheses(string input)
        {
            int startIndex = input.IndexOf('(');
            int endIndex = input.IndexOf(')');

            if (startIndex != -1 && endIndex != -1)
            {
                string numberStr = input.Substring(startIndex + 1, endIndex - startIndex - 1);

                return numberStr;
            }
            else
            {
                return "0";
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="input">"70, MinimumLength = 5"</param>
        /// <returns></returns>
        static string FindMinValueForStringLength(string input)
        {
            string pattern = @"MinimumLength\s*=\s*(\d+)";

            Match match = Regex.Match(input, pattern);

            if (match.Success)
                return match.Groups[1].Value;
            else
                return null;
        }

        /// <summary>
        /// </summary>
        /// <param name="input">"70, MinimumLength = 5"</param>
        /// <returns></returns>
        static string FindMaxValueForStringLength(string input)
        {
            return input.Split(',').First().Replace(" ", "");
        }

        #endregion

    }
}