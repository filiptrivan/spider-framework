using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Spider.SourceGenerators.Shared;
using Spider.SourceGenerators.Enums;
using Spider.SourceGenerators.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spider.SourceGenerators.Net
{
    [Generator]
    public class ExcelPropertiesGenerator : IIncrementalGenerator
    {

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            //#if DEBUG
            //            if (!Debugger.IsAttached)
            //            {
            //                Debugger.Launch();
            //            }
            //#endif
            IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = Helpers.GetClassIncrementalValuesProvider(context.SyntaxProvider, new List<NamespaceExtensionCodes>
                {
                    NamespaceExtensionCodes.Entities,
                    NamespaceExtensionCodes.DTO,
                    NamespaceExtensionCodes.DataMappers,
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
            if (classes.Count <= 1) 
                return;

            List<SpiderClass> currentProjectClasses = Helpers.GetSpiderClasses(classes, referencedProjectClasses);
            List<SpiderClass> allClasses = currentProjectClasses.Concat(referencedProjectClasses).ToList();
            List<SpiderClass> currentProjectDTOClasses = Helpers.GetDTOClasses(currentProjectClasses, allClasses);

            SpiderClass customMapperClass = Helpers.GetManualyWrittenMapperClass(currentProjectClasses);

            StringBuilder sb = new();

            string namespaceValue = currentProjectClasses[0].Namespace;
            string basePartOfNamespace = Helpers.GetBasePartOfNamespace(namespaceValue);
            string projectName = Helpers.GetProjectName(namespaceValue);

            sb.AppendLine($$"""
using Spider.Shared.Excel.DTO;
using {{basePartOfNamespace}}.DTO;

namespace {{basePartOfNamespace}}.ExcelProperties
{
    public static class ExcelPropertiesToExclude
    {
""");
            foreach (IGrouping<string, SpiderClass> DTOClassGroup in currentProjectDTOClasses.GroupBy(x => x.Name))
            {
                sb.AppendLine($$"""
        public static string[] GetHeadersToExclude({{DTOClassGroup.Key}} _)
        {
""");

                List<SpiderProperty> DTOProperties = new();
                foreach (SpiderClass DTOClass in DTOClassGroup)
                    DTOProperties.AddRange(DTOClass.Properties);

                List<string> propertyNames = new();

                foreach (string propertyName in GetPropertiesToExcludeFromExcelExport(DTOClassGroup.Key, DTOProperties, customMapperClass))
                    propertyNames.Add($"\"{propertyName}\"");

                sb.AppendLine($$"""
            return new string[] { {{string.Join(", ", propertyNames)}} };
        }
""");
            }
            sb.AppendLine($$"""
    }
}
""");

            context.AddSource("ExcelPropertiesToExclude.generated", SourceText.From(sb.ToString(), Encoding.UTF8));
        }

        private static List<string> GetPropertiesToExcludeFromExcelExport(string DTOClassName, List<SpiderProperty> DTOProperties, SpiderClass customMapperClass)
        {
            List<string> DTOClassPropertiesToExclude = new();

            SpiderMethod excelMethod = customMapperClass.Methods
                .Where(x => x.ReturnType == DTOClassName && x.Name == "ExcelProjectTo")
                .SingleOrDefault();

            List<SpiderAttribute> excludePropertyAttributes = new();

            DTOClassPropertiesToExclude = DTOProperties // FT: Excluding Enumerables from the excel
                .Where(prop => prop.Type.IsEnumerable())
                .Select(x => x.Name)
                .ToList();

            if (excelMethod != null)
            {
                foreach (SpiderAttribute attribute in excelMethod.Attributes)
                {
                    if (attribute.Name == "MapperIgnoreTarget")
                    {
                        DTOClassPropertiesToExclude.Add(attribute.Value);
                    }
                }
            }

            return DTOClassPropertiesToExclude;
        }
    }
}