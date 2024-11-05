using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Soft.SourceGenerator.NgTable.Angular;
using Soft.SourceGenerator.NgTable.Helpers;
using Soft.SourceGenerators.Helpers;
using Soft.SourceGenerators.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Soft.SourceGenerator.NgTable.Net
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
            IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => Helper.IsSyntaxTargetForGenerationEntitiesAndDTO(s),
                    transform: static (ctx, _) => Helper.GetSemanticTargetForGenerationEntitiesAndDTO(ctx))
                .Where(static c => c is not null);

            context.RegisterImplementationSourceOutput(classDeclarations.Collect(),
                static (spc, source) => Execute(source, spc));
        }

        private static void Execute(IList<ClassDeclarationSyntax> classes, SourceProductionContext context)
        {
            if (classes.Count <= 1) return;

            List<ClassDeclarationSyntax> entityClasses = Helper.GetEntityClasses(classes);
            List<SoftClass> DTOClasses = Helper.GetDTOClasses(classes);

            StringBuilder sb = new StringBuilder();

            string[] namespacePartsWithoutLastElement = Helper.GetNamespacePartsWithoutLastElement(entityClasses[0]);
            string basePartOfNamespace = string.Join(".", namespacePartsWithoutLastElement); // eg. Soft.Generator.Security
            string projectName = namespacePartsWithoutLastElement[namespacePartsWithoutLastElement.Length - 1]; // eg. Security

            sb.AppendLine($$"""
using FluentValidation;
using {{basePartOfNamespace}}.DTO;
using Soft.Generator.Shared.SoftFluentValidation;

namespace {{basePartOfNamespace}}.ValidationRules
{
""");
            foreach (IGrouping<string, SoftClass> DTOClassGroup in DTOClasses.GroupBy(x => x.Name)) // Grouping because UserDTO.generated and UserDTO
            {
                List<SoftProperty> DTOProperties = new List<SoftProperty>();
                List<SoftAttribute> DTOAttributes = new List<SoftAttribute>();

                ClassDeclarationSyntax nonGeneratedDTOClass = classes.Where(x => x.Identifier.Text == DTOClassGroup.Key).SingleOrDefault();
                List<SoftAttribute> softAttributes = Helper.GetAllAttributesOfTheClass(nonGeneratedDTOClass, classes);

                if (softAttributes != null)
                    DTOAttributes.AddRange(softAttributes); // FT: Its okay to add only for non generated because we will not have any attributes on the generated DTOs

                foreach (SoftClass DTOClass in DTOClassGroup)
                    DTOProperties.AddRange(DTOClass.Properties);

                ClassDeclarationSyntax entityClass = entityClasses.Where(x => DTOClassGroup.Key.Replace("DTO", "") == x.Identifier.Text).SingleOrDefault(); // If it is null then we only made DTO, without entity class

                sb.AppendLine($$"""
    public class {{DTOClassGroup.Key}}ValidationRules : AbstractValidator<{{DTOClassGroup.Key}}>
    {
        public {{DTOClassGroup.Key}}ValidationRules()
        {
            {{string.Join("\n\t\t\t", GetValidationRules(DTOProperties, DTOAttributes, entityClass, entityClasses))}}
        }
    }
""");
            }

            sb.AppendLine($$"""
}
""");

            context.AddSource($"{projectName}ValidationRules.generated", SourceText.From(sb.ToString(), Encoding.UTF8));
        }

        /// <summary>
        /// Getting the validation rules for the single object (DTO + Entity)
        /// </summary>
        /// <param name="DTOProperties">Including the attributes</param>
        /// <param name="entityClass">User</param>
        /// <returns>List of rules: eg. [RuleFor(x => x.Username).Length(0, 70), RuleFor(x => x.Email).Length(0, 70)]</returns>
        public static List<string> GetValidationRules(List<SoftProperty> DTOProperties, List<SoftAttribute> DTOAttributes, ClassDeclarationSyntax entityClass, IList<ClassDeclarationSyntax> entityClasses)
        {
            // [RuleFor(x => x.Username).Length(0, 70);, RuleFor(x => x.Email).Length(0, 70);]
            List<string> validationRulesOnDTO = new List<string>(); // priority - 1.
            //List<string> validationRulesOnDTOProperties = new List<string>(); // priority - 2.
            List<string> validationRulesOnEntity = new List<string>(); // priority - 3.
            List<string> validationRulesOnEntityProperties = new List<string>(); // priority - 4.

            foreach (SoftAttribute attribute in DTOAttributes)
            {
                if (attribute.Name == "CustomValidator")
                    validationRulesOnDTO.Add(attribute.Value);
            }

            //foreach (Prop prop in DTOProperties) // FT: Add if you would need to specify rules on the property level of DTO
            //{
            //    string rule = GetRuleForProp(prop);
            //    if (rule != null)
            //        validationRulesOnDTOProperties.Add(rule);
            //}

            if (entityClass != null) // If it is null then we only made DTO, without entity class
            {
                validationRulesOnEntity.AddRange(GetRulesOnEntity(entityClass, entityClasses));

                List<SoftProperty> entityProperties = Helper.GetAllPropertiesOfTheClass(entityClass, entityClasses);
                foreach (SoftProperty prop in entityProperties)
                {
                    string rule = GetRuleForProp(prop);
                    if (rule != null)
                        validationRulesOnEntityProperties.Add(rule);
                }
            }

            List<string> mergedValidationRules = GetMergedValidationRules(validationRulesOnDTO, validationRulesOnEntity, validationRulesOnEntityProperties);

            return mergedValidationRules;
        }

        static string GetRuleForProp(SoftProperty prop)
        {
            List<string> singleRulesOnProperty = GetSingleRulesForProp(prop); // NotEmpty(), Length(0, 70);
            string propName = GetPropNameForRule(prop, singleRulesOnProperty);

            if (propName == null)
                return null;

            if (prop.Attributes.Any(x => x.Name == "Required") == false) // FT: If there is no required attribute, we should let user save null to database
            {
                if (prop.Type == "string")
                {
                    singleRulesOnProperty.Add($"Unless(i => string.IsNullOrEmpty(i.{propName}))");
                }
                else
                {
                    singleRulesOnProperty.Add($"Unless(i => i.{propName} == null)");
                }
            }

            return $"RuleFor(x => x.{propName}).{string.Join(".", singleRulesOnProperty)};";
        }

        private static string GetPropNameForRule(SoftProperty prop, List<string> singleRulesOnProperty)
        {
            string propName = prop.IdentifierText;

            if (singleRulesOnProperty.Count == 0 || prop.Type.IsEnumerable())
                return null;

            if (prop.Type.IsBaseType() == false)  // FT: if it is not base type and not enumerable than it's many to one for sure, and the validation can only be for id to be required
            {
                propName = $"{prop.IdentifierText}Id";
                if (singleRulesOnProperty.Count > 1)
                    propName = "YOU CAN'T DEFINE ANYTHING THEN REQUIRED VALIDATION FOR MANY TO ONE PROPERTY";
            }

            return propName;
        }

        static List<string> GetSingleRulesForProp(SoftProperty prop)
        {
            List<string> singleRules = new List<string>();

            foreach (SoftAttribute attribute in prop.Attributes)
            {
                string attributeName = attribute.Name;
                if (attributeName == null)
                    continue;

                switch (attributeName)
                {
                    case "Required":
                        singleRules.Add("NotEmpty()");
                        break;
                    case "Column":
                        if (attribute.Value.Contains("VARCHAR"))
                            singleRules.Add($"Length(0, {FindNumberBetweenVarcharParentheses(attribute.Value)})");
                        break;
                    case "StringLength":
                        string minValue = FindMinValueForStringLength(attribute.Value);
                        if (minValue == null)
                            singleRules.Add($"Length({FindMaxValueForStringLength(attribute.Value)})");
                        else
                            singleRules.Add($"Length({minValue}, {FindMaxValueForStringLength(attribute.Value)})");
                        break;
                    case "Precision":
                        singleRules.Add($"PrecisionScale({attribute.Value}, false)"); // FT: only here the attribute.Value should be two values eg. 6, 7
                        break;
                    case "Range":
                        singleRules.Add($"GreaterThanOrEqualTo({attribute.Value.Split(',')[0].Trim()})");
                        singleRules.Add($"LessThanOrEqualTo({attribute.Value.Split(',')[1].Trim()})");
                        break;
                    case "GreaterThanOrEqualTo":
                        singleRules.Add($"GreaterThanOrEqualTo({attribute.Value})");
                        break;
                    case "CustomValidator":
                        singleRules.Add(attribute.Value);
                        break;
                    default:
                        break;
                }
            }

            return singleRules;
        }

        /// <summary>
        /// Looking for attributes on the class not on the properties
        /// </summary>
        /// <param name="entityClass"></param>
        /// <returns></returns>
        static List<string> GetRulesOnEntity(ClassDeclarationSyntax entityClass, IList<ClassDeclarationSyntax> entityClasses)
        {
            List<string> ruleFors = new List<string>();
            List<SoftAttribute> entityAttributes = Helper.GetAllAttributesOfTheClass(entityClass, entityClasses);

            foreach (SoftAttribute attribute in entityAttributes)
            {
                string attributeName = attribute.Name;
                if (attributeName == null)
                    continue;

                string attributeValue = attribute.Value;

                switch (attributeName)
                {
                    case "CustomValidator":
                        ruleFors.Add(attributeValue);
                        break;
                    default:
                        break;
                }
            }

            return ruleFors;
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

        /// <summary>
        /// Getting merged validation rules for the single object (DTO + Entity)
        /// </summary>
        /// <returns></returns>
        private static List<string> GetMergedValidationRules(List<string> validationRulesOnDTOProperties, List<string> validationRulesOnEntity, List<string> validationRulesOnEntityProperties)
        {
            List<string> mergedValidationRules = new List<string>();

            foreach (string ruleOnDTOProperties in validationRulesOnDTOProperties) // RuleFor(x => x.Username).Length(0, 70).Required();
            {
                mergedValidationRules.Add(ruleOnDTOProperties);
                string identifierPart = ruleOnDTOProperties.Split(')').First(); // RuleFor(x => x.Username

                validationRulesOnEntity.RemoveAll(x => x.Split(')').First() == identifierPart);
                validationRulesOnEntityProperties.RemoveAll(x => x.Split(')').First() == identifierPart);
            }
            foreach (string ruleOnEntity in validationRulesOnEntity) // RuleFor(x => x.Name).Required();
            {
                mergedValidationRules.Add(ruleOnEntity);
                string identifierPart = ruleOnEntity.Split(')').First(); // RuleFor(x => x.Name

                validationRulesOnEntityProperties.RemoveAll(x => x.Split(')').First() == identifierPart);
            }
            foreach (string ruleOnEntityProperties in validationRulesOnEntityProperties) // RuleFor(x => x.Password).Length(0, 20);
            {
                mergedValidationRules.Add(ruleOnEntityProperties);
            }

            return mergedValidationRules;
        }

        /// <summary>
        /// Getting merged validation rules for the single object (DTO + Entity)
        /// </summary>
        /// <returns></returns>
        private static List<string> GetMergedValidationRulesObsolete(List<string> validationRulesOnDTOProperties, List<string> validationRulesOnEntity, List<string> validationRulesOnEntityProperties)
        {
            List<string> mergedValidationRules = new List<string>();

            foreach (string ruleOnDTOProperties in validationRulesOnDTOProperties) // RuleFor(x => x.Username).Length(0, 70).Required();
            {
                List<string> mergedSingleRules = new List<string>();

                List<string> sr1 = GetSingleRulesWithValues(ruleOnDTOProperties); // .Length(0, 70), .Required()
                string identifierPart = ruleOnDTOProperties.Split(')').First(); // RuleFor(x => x.Username
                mergedSingleRules = sr1;

                string ruleOnEntity = validationRulesOnEntity.Select(x => x.Split(')').First()).Where(x => x == identifierPart).FirstOrDefault(); // RuleFor(x => x.Username).Length(0, 20).Other();
                List<string> sr2 = GetSingleRulesWithValues(ruleOnEntity); // .Length(0, 20), .Other()
                List<string> sr22 = sr2.Select(x => x.Split('(').First()).ToList(); // .Length, .Other

                string ruleOnEntityProperties = validationRulesOnEntityProperties.Select(x => x.Split(')').First()).Where(x => x == identifierPart).FirstOrDefault(); // RuleFor(x => x.Username).Length(0, 20).OtherOther(0, 20);
                List<string> sr3 = GetSingleRulesWithValues(ruleOnEntityProperties); // .Length(0, 20), .OtherOther()
                List<string> sr33 = sr2.Select(x => x.Split('(').First()).ToList(); // .Length, .OtherOther

                foreach (string singleRule in sr1) // .Length(0, 70), .Required()
                {
                    mergedSingleRules.Add(singleRule);
                    string singleRuleIdentifierPart = singleRule.Split('(').First(); // .Length from .Length(0, 70)

                    if (sr22.Contains(singleRuleIdentifierPart) == true)
                    {
                        sr2.RemoveAt(sr2.FindIndex(x => x.StartsWith(singleRuleIdentifierPart)));
                        sr22.RemoveAt(sr2.FindIndex(x => x == singleRuleIdentifierPart));
                    }

                    if (sr33.Contains(singleRuleIdentifierPart) == true)
                    {
                        sr3.RemoveAt(sr3.FindIndex(x => x.StartsWith(singleRuleIdentifierPart)));
                        sr33.RemoveAt(sr3.FindIndex(x => x == singleRuleIdentifierPart));
                    }
                }

                foreach (string singleRule2 in sr2)
                {
                    string singleRule2IdentifierPart = singleRule2.Split('(').First(); // .Length from .Length(0, 20)

                    if (sr33.Contains(singleRule2IdentifierPart) == true)
                    {
                        sr3.RemoveAt(sr3.FindIndex(x => x.StartsWith(singleRule2IdentifierPart)));
                        sr33.RemoveAt(sr3.FindIndex(x => x == singleRule2IdentifierPart));
                    }
                }

                mergedSingleRules.AddRange(sr2);
                mergedSingleRules.AddRange(sr3);
                // mergedSingleRules - .Length(0, 70), .Required(), Other(), OtherOther()
                mergedValidationRules.Add($"{identifierPart}){string.Join("", mergedSingleRules)};");
            }

            return mergedValidationRules;
        }

        ///// <summary>
        ///// </summary>
        ///// <param name="rule">RuleFor(x => x.Username).Length(0, 70).Required();</param>
        ///// <returns>.Length, .Required</returns>
        //private static List<string> GetSingleRulesWithoutValues(string rule)
        //{
        //    List<string> helper = rule.Split('(').ToList(); // "x => x.Username).Length", "0, 70).Required"
        //    List<string> singleRulesWithoutValues = helper.Select(x => x.Substring(0, x.LastIndexOf(')')+1)).ToList(); // .Length, .Required
        //    return singleRulesWithoutValues;
        //}

        /// <summary>
        /// </summary>
        /// <param name="rule">RuleFor(x => x.Username).Length(0, 70).Required();</param>
        /// <returns>.Length(0, 70), .Required()</returns>
        private static List<string> GetSingleRulesWithValues(string rule)
        {
            List<string> helper = rule.Split(')').Skip(1).Select(x => $"{x})").ToList(); // ".Length(0, 70)", ".Required()"
            return helper;
        }

    }
}