using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using Spider.SourceGenerators.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Spider.SourceGenerators.Models;
using Spider.SourceGenerators.Net;
using Spider.SourceGenerators.Enums;

namespace Spider.SourceGenerators.Angular
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
                    predicate: static (s, _) => Helpers.IsSyntaxTargetForGenerationEveryClass(s),
                    transform: static (ctx, _) => Helpers.GetSemanticTargetForGenerationEveryClass(ctx))
                .Where(static c => c is not null);

            IncrementalValueProvider<List<SpiderClass>> referencedProjectClasses = Helpers.GetIncrementalValueProviderClassesFromReferencedAssemblies(context,
                new List<NamespaceExtensionCodes>
                {
                    NamespaceExtensionCodes.Entities,
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

        private static void Execute(IList<ClassDeclarationSyntax> classes, List<SpiderClass> referencedProjectClasses, string callingProjectDirectory, SourceProductionContext context)
        {
            if (classes.Count <= 1) return;

            if (callingProjectDirectory.Contains(".WebAPI") == false)
                return;

            // ...\API\PlayertyLoyals.Business -> ...\Angular\src\app\business\services\validators
            string outputPath = callingProjectDirectory.ReplaceEverythingAfter(@"\API\", @"\Angular\src\app\business\services\validators");

            referencedProjectClasses = referencedProjectClasses.OrderBy(x => x.Name).ToList();

            List<SpiderClass> entities = referencedProjectClasses.Where(x => x.Namespace.EndsWith(".Entities")).ToList();
            List<SpiderClass> DTOClasses = referencedProjectClasses.Where(x => x.Namespace.EndsWith(".DTO")).ToList();

            List<string> switchCases = new();
            List<string> validationMethods = new();

            foreach (SpiderClass DTOClass in DTOClasses)
            {
                SpiderClass entityClass = entities.Where(x => DTOClass.Name.Replace("DTO", "") == x.Name).SingleOrDefault(); // If it is null then we only made DTO, without entity class

                List<SpiderValidationRule> rules = Helpers.GetValidationRules(DTOClass.Properties, DTOClass.Attributes, entityClass);

                string angularValidationSwitchCases = GetAngularValidationSwitchCases(DTOClass, rules);
                if (string.IsNullOrEmpty(angularValidationSwitchCases) == false)
                    switchCases.Add(angularValidationSwitchCases);

                string angularValidationMethods = GenerateAngularValidationMethods(DTOClass, rules);
                if (string.IsNullOrEmpty(angularValidationMethods) == false)
                    validationMethods.Add(angularValidationMethods);
            }

            string result = $$"""
import { ValidationErrors } from '@angular/forms';
import { TranslocoService } from '@jsverse/transloco';
import { Injectable } from '@angular/core';
import { SpiderFormControl, SpiderValidatorFn, validatePrecisionScale } from '@playerty/spider';

@Injectable({
    providedIn: 'root',
})
export class ValidatorServiceGenerated {

    constructor(
        protected translocoService: TranslocoService
    ) {
    }

    setValidator = (formControl: SpiderFormControl, className: string): SpiderValidatorFn => {
        switch(formControl.label + className){
{{string.Join("\n", switchCases)}}
            default:
                return null;
        }
    }

{{string.Join("\n", validationMethods)}}

}
""";

            Helpers.WriteToTheFile(result, Path.Combine(outputPath, "validators.generated.ts"));
        }

        #region Switch Cases

        public static string GetAngularValidationSwitchCases(SpiderClass DTOClass, List<SpiderValidationRule> rules)
        {
            string validationClassName = DTOClass.Name.Replace("DTO", "");

            StringBuilder validationCases = new();

            foreach (SpiderValidationRule rule in rules)
            {
                validationCases.AppendLine($$"""
            case '{{rule.Property.Name.FirstCharToLower()}}{{validationClassName}}':
                return this.{{rule.Property.Name.FirstCharToLower()}}{{validationClassName}}Validator(formControl);
""");
            }

            return validationCases.ToString();
        }

        #endregion

        #region Validation Methods

        public static string GenerateAngularValidationMethods(SpiderClass DTOClass, List<SpiderValidationRule> rules)
        {
            string validationClassName = DTOClass.Name.Replace("DTO", "");

            StringBuilder sb = new();

            foreach (SpiderValidationRule rule in rules)
                sb.AppendLine(GenerateAngularValidationMethod(rule, validationClassName, DTOClass.Properties));

            return sb.ToString();
        }

        public static string GenerateAngularValidationMethod(SpiderValidationRule rule, string validationClassName, List<SpiderProperty> DTOProperties)
        {
            List<string> ruleStatements = new(); // eg. const {ruleName}: boolean = typeof value !== 'undefined' && value !== '';
            List<string> validationMessages = new(); // eg. must have a minimum of {min} and a maximum of {max} characters
            List<string> translocoVariables = new(); // eg. [max, min]
            List<string> translationTags = new(); // eg. Length, IsEmpty
            List<string> ruleNames = new(); // eg. notEmptyRule

            PopulateAngularValidationData(rule, DTOProperties, ruleStatements, validationMessages, translocoVariables, ruleNames, translationTags);

            string allAngularRules = string.Join(" && ", ruleNames);

            string result = $$"""
    {{rule.Property.Name.FirstCharToLower()}}{{validationClassName}}Validator = (control: SpiderFormControl): SpiderValidatorFn => {
        const validator: SpiderValidatorFn = (): ValidationErrors | null => {
            const value = control.value;

{{string.Join("\n", ruleStatements)}}

            const valid = {{allAngularRules}};

            return valid ? null : { _ : this.translocoService.translate('{{string.Join("", translationTags)}}', {{{string.Join(", ", translocoVariables)}}}) };
        };
{{GetNonEmptyControlData(ruleNames)}}
        control.validator = validator;
{{GetUpdateValidationAndValidityData(rule.Property)}}
        return validator;
    }

""";

            return result;
        }

        private static string GetUpdateValidationAndValidityData(SpiderProperty ruleProperty)
        {
            if (ruleProperty.Type == "DateTime" || ruleProperty.Type == "DateTime?")
            {
                return $$"""
        control.updateValueAndValidity(); // FT: It's necessary only for Date Angular type
""";
            }

            return null;
        }

        private static string GetNonEmptyControlData(List<string> ruleNames)
        {
            if (ruleNames.Any(x => x == "notEmptyRule"))
            {
                return $$"""
        validator.hasNotEmptyRule = true;
        control.required = true;
""";
            }

            return null;
        }

        public static void PopulateAngularValidationData(
            SpiderValidationRule rule,
            List<SpiderProperty> DTOProperties,
            List<string> ruleStatements,
            List<string> validationMessages,
            List<string> translocoVariables,
            List<string> ruleNames,
            List<string> translationTags
        )
        {
            if (rule.ValidationRuleParts.Any(x => x.Name == "NotEmpty"))
            {
                string ruleName = "notEmptyRule";

                ruleStatements.Add($$"""
            const {{ruleName}} = {{GetRequiredControlCheckInTypeScript(rule.Property)}};
""");
                ruleNames.Add(ruleName);
                validationMessages.Add("is mandatory");
                translationTags.Add("NotEmpty");
            }

            if (rule.ValidationRuleParts.Any(x => x.Name == "Length"))
            {
                SpiderValidationRulePart rulePart = rule.ValidationRuleParts.SingleOrDefault(x => x.Name == "Length");
                Match lengthMatch = Regex.Match(rulePart.MethodParametersBody, @"(\d+),\s*(\d+)");
                Match singleLengthMatch = Regex.Match(rulePart.MethodParametersBody, @"(\d+)");

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

            if (rule.ValidationRuleParts.Any(x => x.Name == "LessThanOrEqualTo"))
            {
                SpiderValidationRulePart rulePart = rule.ValidationRuleParts.SingleOrDefault(x => x.Name == "LessThanOrEqualTo");

                string ruleName = "numberMaxRangeRule";
                string max = rulePart.MethodParametersBody;
                ruleStatements.Add($$"""
            const max = {{max}};
            const {{ruleName}} = (value <= max) || (typeof value === 'undefined' || value === null || value === '');
""");
                ruleNames.Add(ruleName);
                validationMessages.Add($"must be less or equal to ${{max}}");
                translocoVariables.AddRange(["max"]);
                translationTags.Add("NumberRangeMax");
            }

            if (rule.ValidationRuleParts.Any(x => x.Name == "GreaterThanOrEqualTo"))
            {
                SpiderValidationRulePart rulePart = rule.ValidationRuleParts.SingleOrDefault(x => x.Name == "GreaterThanOrEqualTo");

                string ruleName = "numberMinRangeRule";
                string min = rulePart.MethodParametersBody;
                ruleStatements.Add($$"""
            const min = {{min}};
            const {{ruleName}} = (value >= min) || (typeof value === 'undefined' || value === null || value === '');
""");
                ruleNames.Add(ruleName);
                validationMessages.Add($"must be greater or equal to ${{min}}");
                translocoVariables.AddRange(["min"]);
                translationTags.Add("NumberRangeMin");
            }

            if (rule.ValidationRuleParts.Any(x => x.Name == "NotHaveWhiteSpace"))
            {
                string ruleName = "notHaveWhiteSpaceRule";
                ruleStatements.Add($$"""
            const {{ruleName}} = !/\\s/.test(value);
""");
                ruleNames.Add(ruleName);
                validationMessages.Add("must not contain whitespace");
                translationTags.Add("NotHaveWhiteSpace");
            }

            if (rule.ValidationRuleParts.Any(x => x.Name == "EmailAddress"))
            {
                string ruleName = "emailAddressRule";
                ruleStatements.Add($$"""
            const {{ruleName}} = /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
""");
                ruleNames.Add(ruleName);
                validationMessages.Add("must be a valid email address");
                translationTags.Add("EmailAddress");
            }

            if (rule.ValidationRuleParts.Any(x => x.Name == "PrecisionScale"))
            {
                SpiderValidationRulePart rulePart = rule.ValidationRuleParts.SingleOrDefault(x => x.Name == "PrecisionScale");
                Match precisionScaleMatch = Regex.Match(rulePart.MethodParametersBody, @"(\d+),\s*(\d+),\s*(true|false)");

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

        private static string GetRequiredControlCheckInTypeScript(SpiderProperty property)
        {
            if (property.IsEditorControlType())
            {
                return "typeof value !== 'undefined' && value !== null && value !== '' && value !== '<p></p>'";
            }
            else
            {
                return "typeof value !== 'undefined' && value !== null && value !== ''";
            }
        }

        #endregion

    }
}
