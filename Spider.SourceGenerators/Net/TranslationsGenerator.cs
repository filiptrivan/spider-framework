using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Spider.SourceGenerators.Shared;
using Spider.SourceGenerators.Enums;
using Spider.SourceGenerators.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Spider.SourceGenerators.Net
{
    [Generator]
    public class TranslationsGenerator : IIncrementalGenerator
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

        private static void Execute(IList<ClassDeclarationSyntax> classes, List<SpiderClass> referencedProjectEntityClasses, string callingProjectDirectory, SourceProductionContext context)
        {
            if (classes.Count < 1)
                return;

            bool shouldGenerate = Helpers.ShouldStartGenerator(nameof(TranslationsGenerator), classes);

            if (shouldGenerate == false)
                return;

            referencedProjectEntityClasses = referencedProjectEntityClasses.OrderBy(x => x.Name).ToList();

            Dictionary<string, string> dataEn = referencedProjectEntityClasses
                .Select(entity => GetTranslationData(entity, LanguageCodes.En))
                .PrepareForTranslation();

            Dictionary<string, string> dataSrLatnRS = referencedProjectEntityClasses
                .Select(entity => GetTranslationData(entity, LanguageCodes.SrLatnRS))
                .PrepareForTranslation();

            string sharedBusinessProjectPath = callingProjectDirectory.Replace(".WebAPI", ".Shared");

            Helpers.WriteResourceFile(dataEn, Path.Combine(sharedBusinessProjectPath, GetTermsFilePath(LanguageCodes.En)));
            Helpers.WriteResourceFile(dataSrLatnRS, Path.Combine(sharedBusinessProjectPath, GetTermsFilePath(LanguageCodes.SrLatnRS)));

            // E:\Projects\PlayertyLoyals\API\PlayertyLoyals.Shared -> E:\Projects\PlayertyLoyals\Angular\src\assets\i18n
            string angulari18nFolderPath = callingProjectDirectory.ReplaceEverythingAfter(@"\API\", @"\Angular\src\assets\i18n");

            Helpers.WriteToTheFile(GetJsonFromDictionaryList(dataEn), Path.Combine(angulari18nFolderPath, GetAngulari18nFilePath(LanguageCodes.En)));
            Helpers.WriteToTheFile(GetJsonFromDictionaryList(dataSrLatnRS), Path.Combine(angulari18nFolderPath, GetAngulari18nFilePath(LanguageCodes.SrLatnRS)));
        }

        private static Dictionary<string, string> GetTranslationData(SpiderClass entity, LanguageCodes language)
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>
            {
                { $"{entity.Name}", entity.Translate(TranslationCodes.Singular, language) },
                { $"{entity.Name}List", entity.Translate(TranslationCodes.Plural, language) },
                { $"{entity.Name}ExcelExportName", entity.Translate(TranslationCodes.Excel, language) ?? entity.Translate(TranslationCodes.Plural, language).ToTrainCase() },
            };

            foreach (SpiderProperty property in entity.Properties)
                dictionary.Add(property.Name, property.Translate(language));

            return dictionary;
        }

        private static string GetTermsFilePath(LanguageCodes language)
        {
            return @$"Terms\TermsGenerated{GetTermsFileExtension(language)}.resx"; ;
        }

        private static string GetTermsFileExtension(LanguageCodes language)
        {
            string extension = null;

            if (language == LanguageCodes.En)
            {
                extension = null;
            }
            else if (language == LanguageCodes.SrLatnRS)
            {
                extension = ".sr-Latn-RS";
            }

            return extension;
        }

        private static string GetJsonFromDictionaryList(Dictionary<string, string> data)
        {
            return $$"""
{
{{string.Join(",\n", GetJsonElementsFromDictionaryList(data))}}
}
""";
        }

        private static List<string> GetJsonElementsFromDictionaryList(Dictionary<string, string> data)
        {
            List<string> result = new();
            Dictionary<string, string> alreadyAddedKeyValuePairs = new Dictionary<string, string>();

            foreach (KeyValuePair<string, string> keyValuePair in data)
            {
                KeyValuePair<string, string> alreadyAddedKeyValuePair = alreadyAddedKeyValuePairs.Where(x => x.Key == keyValuePair.Key).SingleOrDefault();

                if (alreadyAddedKeyValuePair.Key == null)
                {
                    alreadyAddedKeyValuePairs.Add(keyValuePair.Key, keyValuePair.Value);

                    string row = $$"""
    "{{keyValuePair.Key}}": "{{keyValuePair.Value}}"
""";

                    result.Add(row);
                }
            }

            return result;
        }

        private static string GetAngulari18nFilePath(LanguageCodes language)
        {
            string filePath = null;

            if (language == LanguageCodes.En)
            {
                filePath = "en.generated.json";
            }
            else if (language == LanguageCodes.SrLatnRS)
            {
                filePath = "sr-Latn-RS.generated.json";
            }

            return filePath;
        }

    }
}
