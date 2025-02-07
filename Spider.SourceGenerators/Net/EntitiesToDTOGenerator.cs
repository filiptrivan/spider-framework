using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.Text;
using Spider.SourceGenerators.Shared;
using System.Linq;
using Spider.SourceGenerators.Models;
using Spider.SourceGenerators.Enums;
using System.Diagnostics;
using System;

namespace Spider.SourceGenerators.Net
{
    [Generator]
    public class EntitiesToDTOGenerator : IIncrementalGenerator
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
                });

            IncrementalValueProvider<List<SpiderClass>> referencedProjectClasses = Helpers.GetIncrementalValueProviderClassesFromReferencedAssemblies(context,
                new List<NamespaceExtensionCodes>
                {
                    NamespaceExtensionCodes.Entities,
                });

            var allClasses = classDeclarations.Collect()
                .Combine(referencedProjectClasses);

            context.RegisterImplementationSourceOutput(allClasses, static (spc, source) => Execute(source.Left, source.Right, spc));

            //context.RegisterImplementationSourceOutput(classDeclarations.Collect(),
            //    static (spc, source) => Execute(source, spc));
        }

        private static void Execute(IList<ClassDeclarationSyntax> classes, List<SpiderClass> referencedProjectEntityClasses, SourceProductionContext context)
        {
            if (classes.Count <= 1)
                return;

            List<SpiderClass> currentProjectEntities = Helpers.GetSpiderClasses(classes, referencedProjectEntityClasses);
            List<SpiderClass> allEntities = currentProjectEntities.Concat(referencedProjectEntityClasses).ToList();
            List<SpiderClass> currentProjectDTOClasses = Helpers.GetDTOClasses(currentProjectEntities, allEntities);

            string[] namespacePartsWithoutLastElement = Helpers.GetNamespacePartsWithoutLastElement(currentProjectEntities[0].Namespace);
            string basePartOfNamespace = string.Join(".", namespacePartsWithoutLastElement); // eg. Spider.Security
            string projectName = namespacePartsWithoutLastElement[namespacePartsWithoutLastElement.Length - 1]; // eg. Security

            string result = $$"""
{{GetUsings()}}

namespace {{basePartOfNamespace}}.DTO
{
{{GetDTOClasses(currentProjectDTOClasses, currentProjectEntities, allEntities)}}
}
""";

            context.AddSource($"{projectName}DTOList.generated", SourceText.From(result, Encoding.UTF8));
        }

        private static string GetDTOClasses(List<SpiderClass> currentProjectDTOClasses, List<SpiderClass> currentProjectEntities, List<SpiderClass> allEntities)
        {
            List<string> result = new();

            foreach (SpiderClass currentProjectDTOClass in currentProjectDTOClasses)
            {
                result.Add($$"""
    public partial class {{currentProjectDTOClass.Name}} {{GetDTOBaseTypeExtension(currentProjectDTOClass.BaseType)}}
    {
{{GetDTOProperties(currentProjectDTOClass)}}
    }
""");
            }

            return string.Join("\n\n", result);
        }

        /// <summary>
        /// Getting the properties of the DTO based on the entity class, we don't include base type properties because of the inheritance
        /// </summary>
        private static string GetDTOProperties(SpiderClass currentProjectDTOClass)
        {
            List<string> result = new();

            foreach (SpiderProperty property in currentProjectDTOClass.Properties)
            {
                if (property.EntityName != currentProjectDTOClass.Name)
                    continue;

                result.Add($$"""
        public {{property.Type}} {{property.Name}} { get; set; }
""");
            }

            return string.Join("\n", result);
        }

        #region Helpers

        private static string GetDTOBaseTypeExtension(string DTObaseType)
        {
            return DTObaseType == null ? "" : $": {DTObaseType}";
        }

        private static string GetUsings()
        {
            return $$"""
using Microsoft.AspNetCore.Http;
using Spider.Shared.DTO;
using Spider.Security.DTO;
using Spider.Shared.Helpers;
""";
        }

        #endregion
    }
}
