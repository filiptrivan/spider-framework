using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using Soft.SourceGenerator.NgTable.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Soft.SourceGenerators.Helpers;
using Soft.SourceGenerators.Models;
using Soft.SourceGenerator.NgTable.Net;

namespace Soft.SourceGenerator.NgTable.Angular
{
    [Generator]
    public class NgValidatorsGenerator : IIncrementalGenerator
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

            List<SoftClass> entityClasses = Helper.GetSoftEntityClasses(classes);
            List<SoftClass> DTOClasses = Helper.GetDTOClasses(Helper.GetSoftClasses(classes));

            string outputPath = Helper.GetGeneratorOutputPath(nameof(NgValidatorsGenerator), classes);

            string[] namespacePartsWithoutLastElement = Helper.GetNamespacePartsWithoutLastElement(classes[0]);
            string projectName = namespacePartsWithoutLastElement.LastOrDefault() ?? "ERROR"; // eg. Security

            StringBuilder sb = new StringBuilder();
            StringBuilder sbMethods = new StringBuilder();

            sb.AppendLine($$"""
import { ValidationErrors } from '@angular/forms';
import { SoftFormControl, SoftValidatorFn } from 'src/app/core/components/soft-form-control/soft-form-control';
import { validatePrecisionScale } from '../../../../core/services/helper-functions';
import { TranslocoService } from '@jsverse/transloco';
import { Injectable } from '@angular/core';

@Injectable({
    providedIn: 'root',
})
export class Validator{{projectName}}Service {

    constructor(
        private translocoService: TranslocoService
    ) {
    }

    getValidator(formControl: SoftFormControl, className: string): SoftValidatorFn {
        switch(formControl.label + className){
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

                SoftClass entityClass = entityClasses.Where(x => DTOClassGroup.Key.Replace("DTO", "") == x.Name).SingleOrDefault(); // If it is null then we only made DTO, without entity class

                string validationClassConstructorBody = GetValidationClassConstructorBody(DTOProperties, DTOAttributes, entityClass, entityClasses);

                sb.AppendLine(GetAngularValidationCases(DTOClassGroup.Key, validationClassConstructorBody));
                sbMethods.AppendLine(GenerateAngularValidationMethods(DTOClassGroup.Key, validationClassConstructorBody, DTOProperties));
            }
            sb.AppendLine($$"""
            default:
                return null;
        }
    }

{{sbMethods}}
}
""");
            //sb.AppendLine(sbMethods.ToString());

            Helper.WriteToTheFile(sb.ToString(), $@"{outputPath}\{projectName.FromPascalToKebabCase()}-validation-rules.generated.ts");
        }

        public static string GenerateAngularValidationMethods(string DTOClassName, string validationClassConstructorBody, List<SoftProperty> DTOProperties)
        {
            string validationClassName = DTOClassName.Replace("DTO", "");

            List<string> validationRulePropNames = ParseValidationParameters(validationClassConstructorBody);

            StringBuilder sb = new StringBuilder();

            foreach (string validationRulePropName in validationRulePropNames)
                sb.AppendLine(GenerateAngularValidationMethod(validationRulePropName, validationClassName, validationClassConstructorBody, DTOProperties));

            return sb.ToString();
        }

        public static string GenerateAngularValidationMethod(string validationRulePropName, string classNameForValidation, string input, List<SoftProperty> DTOProperties)
        {
            string validationRulePropNameFirstLower = AdjustManyToOnePropertyNameForValidation(validationRulePropName);

            string pattern = $@"RuleFor\(x => x\.{validationRulePropName}\)(.*?);";
            Match match = Regex.Match(input, pattern, RegexOptions.Singleline);

            if (!match.Success)
            {
                return string.Empty;
            }

            string rules = match.Groups[1].Value.Trim(); // .NotEmpty().Length(0,45)

            List<string> ruleStatements = new List<string>(); // eg. const {ruleName}: boolean = typeof value !== 'undefined' && value !== '';
            List<string> validationMessages = new List<string>(); // eg. must have a minimum of {min} and a maximum of {max} characters
            List<string> translocoVariables = new List<string>(); // eg. [max, min]
            List<string> translationTags = new List<string>(); // eg. Length, IsEmpty
            List<string> ruleNames = new List<string>(); // eg. notEmptyRule

            PopulateListOfStrings(rules, DTOProperties, validationRulePropName, ruleStatements, validationMessages, translocoVariables, ruleNames, translationTags);

            string allRules = string.Join(" && ", ruleNames);

            string result = $@"
    {validationRulePropNameFirstLower}{classNameForValidation}Validator(control: SoftFormControl): SoftValidatorFn {{
        const validator: SoftValidatorFn = (): ValidationErrors | null => {{
            const value = control.value;

    {string.Join("\n", ruleStatements)}

            const {validationRulePropNameFirstLower}Valid = {allRules};

            return {validationRulePropNameFirstLower}Valid ? null : {{ _ : this.translocoService.translate('{string.Join("", translationTags)}', {{{string.Join(", ", translocoVariables)}}}) }};
        }};
        {(ruleNames.Any(x => x == "notEmptyRule") ? "validator.hasNotEmptyRule = true;" : "")}
        return validator;
    }}";

            return result;
        }

        public static void PopulateListOfStrings(string rules, List<SoftProperty> DTOProperties, string validationRulePropName, List<string> ruleStatements, List<string> validationMessages, List<string> translocoVariables, List<string> ruleNames, List<string> translationTags)
        {
            SoftProperty property = DTOProperties.Where(x => x.IdentifierText == validationRulePropName).Single();

            if (rules.Contains("NotEmpty") || property.Type == "int" || property.Type == "long" || property.Type == "byte")
            {
                string ruleName = "notEmptyRule";
                ruleStatements.Add($$"""
        const {{ruleName}} = typeof value !== 'undefined' && value !== null && value !== '';
""");
                ruleNames.Add(ruleName);
                validationMessages.Add("is mandatory");
                translationTags.Add("NotEmpty");
            }

            if (rules.Contains("Length"))
            {
                Match lengthMatch = Regex.Match(rules, @"Length\((\d+),\s*(\d+)\)");
                Match singleLengthMatch = Regex.Match(rules, @"Length\((\d+)\)");

                if (lengthMatch.Success)
                {
                    string ruleName = "stringLengthRule";
                    string min = lengthMatch.Groups[1].Value;
                    string max = lengthMatch.Groups[2].Value;
                    ruleStatements.Add($$"""
        const min = {{min}};
        const max = {{max}};
        const {{ruleName}} = (value?.length >= min && value?.length <= max) || (typeof value === 'undefined' || value === null || value === '');
""");
                    ruleNames.Add(ruleName);
                    validationMessages.Add($"must have a minimum of ${{min}} and a maximum of ${{max}} characters");
                    translocoVariables.AddRange(["min", "max"]);
                    translationTags.Add("Length");
                }
                else if (singleLengthMatch.Success)
                {
                    string ruleName = "stringSingleLengthRule";
                    string length = singleLengthMatch.Groups[1].Value;
                    ruleStatements.Add($$"""
        const length = {{length}};
        const {{ruleName}} = (value?.length == length) || (typeof value === 'undefined' || value === null || value === '');
""");
                    ruleNames.Add(ruleName);
                    validationMessages.Add($"must be ${{length}} character long");
                    translocoVariables.AddRange(["length"]);
                    translationTags.Add("SingleLength");
                }
            }

            if (rules.Contains("LessThanOrEqualTo"))
            {
                Match rangeMatch = Regex.Match(rules, @"LessThanOrEqualTo\((\d+)\)");

                if (rangeMatch.Success)
                {
                    string ruleName = "numberMaxRangeRule";
                    string max = rangeMatch.Groups[1].Value;
                    ruleStatements.Add($$"""
        const max = {{max}};
        const {{ruleName}} = (value <= max) || (typeof value === 'undefined' || value === null || value === '');
""");
                    ruleNames.Add(ruleName);
                    validationMessages.Add($"must be less or equal to ${{max}}");
                    translocoVariables.AddRange(["max"]);
                    translationTags.Add("NumberRangeMax");
                }
            }

            if (rules.Contains("GreaterThanOrEqualTo"))
            {
                Match rangeMatch = Regex.Match(rules, @"GreaterThanOrEqualTo\((\d+)\)");

                if (rangeMatch.Success)
                {
                    string ruleName = "numberMinRangeRule";
                    string min = rangeMatch.Groups[1].Value;
                    ruleStatements.Add($$"""
        const min = {{min}};
        const {{ruleName}} = (value >= min) || (typeof value === 'undefined' || value === null || value === '');
""");
                    ruleNames.Add(ruleName);
                    validationMessages.Add($"must be greater or equal to ${{min}}");
                    translocoVariables.AddRange(["min"]);
                    translationTags.Add("NumberRangeMin");
                }
            }

            if (rules.Contains("NotHaveWhiteSpace"))
            {
                string ruleName = "notHaveWhiteSpaceRule";
                ruleStatements.Add($$"""
        const {{ruleName}} = !/\\s/.test(value);
""");
                ruleNames.Add(ruleName);
                validationMessages.Add("must not contain whitespace");
                translationTags.Add("NotHaveWhiteSpace");
            }

            if (rules.Contains("EmailAddress"))
            {
                string ruleName = "emailAddressRule";
                ruleStatements.Add($$"""
        const {{ruleName}} = /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
""");
                ruleNames.Add(ruleName);
                validationMessages.Add("must be a valid email address");
                translationTags.Add("EmailAddress");
            }

            if (rules.Contains("PrecisionScale"))
            {
                Match precisionScaleMatch = Regex.Match(rules, @"PrecisionScale\((\d+),\s*(\d+),\s*(true|false)\)");
                if (precisionScaleMatch.Success)
                {
                    string ruleName = "precisionScaleRule";
                    string precision = precisionScaleMatch.Groups[1].Value;
                    string scale = precisionScaleMatch.Groups[2].Value;
                    string ignoreTrailingZeros = precisionScaleMatch.Groups[3].Value;

                    ruleStatements.Add($$"""
        const precision = {{precision}};
        const scale = {{scale}};
        const ignoreTrailingZeros = {{ignoreTrailingZeros}};
        const {{ruleName}} = validatePrecisionScale(value, precision, scale, ignoreTrailingZeros) || (typeof value === 'undefined' || value === null || value === '');
""");
                    ruleNames.Add(ruleName);
                    validationMessages.Add($"must have a total number of ${{precision}} digits, and the number of digits after the decimal point must not exceed ${{scale}}");
                    translocoVariables.AddRange(["precision", "scale"]);
                    translationTags.Add("PrecisionScale");
                }
            }
        }

        public static string GetAngularValidationCases(string DTOClassName, string validationClassConstructorBody)
        {
            string validationClassName = DTOClassName.Replace("DTO", "");

            List<string> validationRulePropNames = ParseValidationParameters(validationClassConstructorBody);

            StringBuilder validationCases = new StringBuilder();

            foreach (string validationRulePropName in validationRulePropNames)
            {
                string validationRulePropNameFirstLower = AdjustManyToOnePropertyNameForValidation(validationRulePropName);

                validationCases.AppendLine($$"""
        case '{{validationRulePropNameFirstLower}}{{validationClassName}}':
            return this.{{validationRulePropNameFirstLower}}{{validationClassName}}Validator(formControl);
""");
            }

            return validationCases.ToString();
        }

        private static string AdjustManyToOnePropertyNameForValidation(string validationRulePropName)
        {
            string validationRulePropNameFirstLower = validationRulePropName.FirstCharToLower();

            //if (validationRulePropNameFirstLower.EndsWith("Id") && validationRulePropNameFirstLower.Length > 2)
            //{
            //    validationRulePropNameFirstLower = validationRulePropNameFirstLower.Substring(0, validationRulePropNameFirstLower.Length - 2);
            //}
            //else if (validationRulePropNameFirstLower.EndsWith("DisplayName"))
            //{
            //    validationRulePropNameFirstLower = validationRulePropNameFirstLower.Replace("DisplayName", "");
            //}

            return validationRulePropNameFirstLower;
        }

        private static string GetValidationClassConstructorBody(List<SoftProperty> DTOProperties, List<SoftAttribute> DTOAttributes, SoftClass entityClass, List<SoftClass> entityClasses)
        {
            return $"{string.Join("\n\t\t\t", FluentValidationGenerator.GetValidationRules(DTOProperties, DTOAttributes, entityClass, entityClasses))}";
        }

        /// <summary>
        /// RuleFor(x => x.Username).....; -> Username
        /// </summary>
        /// <param name="input">Body of the fluent validation DTO constructor</param>
        /// <returns></returns>
        static List<string> ParseValidationParameters(string body)
        {
            List<string> parameters = new List<string>();
            string pattern = @"x\.([a-zA-Z0-9_]+)";

            foreach (Match match in Regex.Matches(body, pattern))
            {
                parameters.Add(match.Groups[1].Value);
            }

            return parameters;
        }

    }
}
