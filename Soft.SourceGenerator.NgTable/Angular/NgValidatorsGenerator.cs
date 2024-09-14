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
                    predicate: static (s, _) => Helper.IsSyntaxTargetForGenerationValidationRules(s),
                    transform: static (ctx, _) => Helper.GetSemanticTargetForGenerationValidationRules(ctx))
                .Where(static c => c is not null);

            context.RegisterImplementationSourceOutput(classDeclarations.Collect(),
            static (spc, source) => Execute(source, spc));

        }
        private static void Execute(IList<ClassDeclarationSyntax> validationClasses, SourceProductionContext context)
        {
            if (validationClasses.Count == 0) return;
            string[] namespacePartsWithoutLastElement = Helper.GetNamespacePartsWithoutLastElement(validationClasses[0]);
            string[] namespacePartsWithoutTwoLastElements = namespacePartsWithoutLastElement.Take(namespacePartsWithoutLastElement.Length - 1).ToArray();

            string projectName = namespacePartsWithoutLastElement.LastOrDefault() ?? "ERROR"; // eg. Security
            string wholeProjectBasePartOfNamespace = string.Join(".", namespacePartsWithoutTwoLastElements); // eg. Soft.Generator

            StringBuilder sb = new StringBuilder();
            StringBuilder sbMethods = new StringBuilder();

            sb.AppendLine($$"""
import { ValidationErrors } from '@angular/forms';
import { SoftFormControl, SoftValidatorFn } from 'src/app/core/components/soft-form-control/soft-form-control';
import { validatePrecisionScale } from '../../../../core/services/helper-functions';

export function getValidator{{projectName}}(formControl: SoftFormControl, className: string): SoftValidatorFn {
    switch(formControl.label + className){
""");
            foreach (ClassDeclarationSyntax validationClass in validationClasses)
            {
                sb.AppendLine(GetAngularValidationCases(validationClass));
                sbMethods.AppendLine(GenerateAngularValidationMethods(validationClass));
            }
            sb.AppendLine($$"""
        default:
            return null;
    }
}
""");
            sb.AppendLine(sbMethods.ToString());

            Helper.WriteToTheFile(sb.ToString(), $@"E:\Projects\{wholeProjectBasePartOfNamespace}\Source\{wholeProjectBasePartOfNamespace}.SPA\src\app\business\services\validation\generated\{projectName.FromPascalToKebabCase()}-validation-rules.generated.ts");
        }

        public static string GenerateAngularValidationMethods(ClassDeclarationSyntax validationClass)
        {
            string validationClassConstructorBody = GetValidationClassConstructorBody(validationClass);
            List<string> validationRulePropNames = ParseValidationParameters(validationClassConstructorBody);
            StringBuilder sb = new StringBuilder();

            foreach (string validationRulePropName in validationRulePropNames)
            {
                sb.AppendLine(GenerateAngularValidationMethod(validationRulePropName, GetClassNameForValidation(validationClass), validationClassConstructorBody));
            }

            return sb.ToString();
        }

        public static string GenerateAngularValidationMethod(string validationRulePropName, string classNameForValidation, string input)
        {
            string parameterFirstLower = validationRulePropName.FirstCharToLower();
            string pattern = $@"RuleFor\(x => x\.{validationRulePropName}\)(.*?);";
            Match match = Regex.Match(input, pattern, RegexOptions.Singleline);

            if (!match.Success)
            {
                return string.Empty;
            }

            string rules = match.Groups[1].Value.Trim(); // .NotEmpty().Length(0,45)

            List<string> ruleStatements = new List<string>(); // eg. const {ruleName}: boolean = typeof value !== 'undefined' && value !== '';
            List<string> validationMessages = new List<string>(); // eg. must have a minimum of {min} and a maximum of {max} characters
            List<string> translationTags = new List<string>(); // eg. Length, IsEmpty
            List<string> ruleNames = new List<string>(); // eg. notEmptyRule

            PopulateListOfStrings(rules, ruleStatements, validationMessages, ruleNames, translationTags);

            string allRules = string.Join(" && ", ruleNames);

            string result = $@"
export function {parameterFirstLower}{classNameForValidation}Validator(control: SoftFormControl): SoftValidatorFn {{
    const validator: SoftValidatorFn = (): ValidationErrors | null => {{
        const value = control.value;

{string.Join("\n", ruleStatements)}

        const {parameterFirstLower}Valid = {allRules};

        return {parameterFirstLower}Valid ? null : {{ _ : $localize`:@@{string.Join("", translationTags)}:The field {validationMessages.ToCommaSeparatedString()}.` }};
    }};
    {(ruleNames.Any(x => x == "notEmptyRule") ? "validator.hasNotEmptyRule = true;" : "")}
    return validator;
}}";

            return result;
        }

        public static void PopulateListOfStrings(string rules, List<string> ruleStatements, List<string> validationMessages, List<string> ruleNames, List<string> translationTags)
        {
            if (rules.Contains("NotEmpty"))
            {
                string ruleName = "notEmptyRule";
                ruleStatements.Add($$"""
        const {{ruleName}} = typeof value !== 'undefined' && value !== '';
""");
                ruleNames.Add(ruleName);
                validationMessages.Add("is mandatory");
                translationTags.Add("NotEmpty");
            }

            if (rules.Contains("Length"))
            {
                Match lengthMatch = Regex.Match(rules, @"Length\((\d+),\s*(\d+)\)");
                if (lengthMatch.Success)
                {
                    string ruleName = "stringLengthRule";
                    string min = lengthMatch.Groups[1].Value;
                    string max = lengthMatch.Groups[2].Value;
                    ruleStatements.Add($$"""
        const min = {{min}};
        const max = {{max}};
        const {{ruleName}} = value?.length >= min && value?.length <= max;
""");
                    ruleNames.Add(ruleName);
                    validationMessages.Add($"must have a minimum of ${{min}} and a maximum of ${{max}} characters");
                    translationTags.Add("Length");
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
        const {{ruleName}} = validatePrecisionScale(value, precision, scale, ignoreTrailingZeros);
""");
                    ruleNames.Add(ruleName);
                    validationMessages.Add($"must have a total number of ${{precision}} digits, and the number of digits after the decimal point must not exceed ${{scale}}");
                    translationTags.Add("PrecisionScale");
                }
            }
        }

        public static string GetAngularValidationCases(ClassDeclarationSyntax validationClass)
        {
            string validationClassName = GetClassNameForValidation(validationClass);
            StringBuilder validationCases = new StringBuilder();

            List<string> validationRulePropNames = ParseValidationParameters(GetValidationClassConstructorBody(validationClass));

            foreach (string propName in validationRulePropNames)
            {
                validationCases.AppendLine($$"""
        case '{{propName.FirstCharToLower()}}{{validationClassName}}':
            return {{propName.FirstCharToLower()}}{{validationClassName}}Validator(formControl);
""");
            }

            return validationCases.ToString();
        }

        /// <summary>
        /// eg. UserDTOValidationRules -> User
        /// </summary>
        private static string GetClassNameForValidation(ClassDeclarationSyntax validationClass)
        {
            string validationClassName = validationClass.Identifier.Text;
            int index = validationClassName.IndexOf("DTO");

            if (index >= 0)
            {
                return validationClassName.Substring(0, index);
            }

            return validationClassName; // FT: If "DTO" is not found, return the original input
        }

        private static string GetValidationClassConstructorBody(ClassDeclarationSyntax validationClass)
        {
            ConstructorDeclarationSyntax validationConstructor = validationClass.Members.OfType<ConstructorDeclarationSyntax>().Single();
            return validationConstructor.Body.ToString();
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
