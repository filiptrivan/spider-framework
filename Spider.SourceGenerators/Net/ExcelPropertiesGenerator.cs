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
            IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = Helpers.GetClassInrementalValuesProvider(context.SyntaxProvider, new List<NamespaceExtensionCodes>
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
            if (classes.Count <= 1) return;

            List<SpiderClass> currentProjectClasses = Helpers.GetSpiderClasses(classes, referencedProjectClasses);
            List<SpiderClass> allClasses = currentProjectClasses.Concat(referencedProjectClasses).ToList();
            List<SpiderClass> currentProjectDTOClasses = Helpers.GetDTOClasses(currentProjectClasses, allClasses);

            ClassDeclarationSyntax mapperClass = Helpers.GetManualyWrittenMapperClass(classes);

            StringBuilder sb = new StringBuilder();

            string[] namespacePartsWithoutLastElement = Helpers.GetNamespacePartsWithoutLastElement(classes[0]);

            string basePartOfNamespace = string.Join(".", namespacePartsWithoutLastElement); // eg. Spider.Security
            string projectName = namespacePartsWithoutLastElement[namespacePartsWithoutLastElement.Length - 1]; // eg. Security

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
                IList<string> propertyNames = new List<string>();

                foreach (SpiderProperty prop in GetPropsToExcludeFromExcelExport(DTOClassGroup.Key, currentProjectDTOClasses, mapperClass))
                {
                    propertyNames.Add($"\"{prop.Name}\"");
                }

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

        private static List<SpiderProperty> GetPropsToExcludeFromExcelExport(string className, IList<SpiderClass> DTOClasses, ClassDeclarationSyntax mapperClass)
        {
            List<SpiderProperty> DTOClassProperties = new List<SpiderProperty>();

            // FT: I dont know why did i add here this, if im overriding it down.
            //List<SpiderClass> pairDTOClasses = DTOClasses.Where(x => x.Name == className).ToList(); // There will be 2, partial generated and partial manual
            //foreach (SpiderClass classDTO in pairDTOClasses) // It's only two here
            //    DTOClassProperties.AddRange(classDTO.Properties);

            MethodDeclarationSyntax excelMethod = mapperClass?.Members.OfType<MethodDeclarationSyntax>()
               .Where(x => x.ReturnType.ToString() == className && x.Identifier.ToString() == $"{Helpers.MethodNameForExcelExportMapping}")
               .SingleOrDefault();

            IList<SpiderAttribute> excludePropAttributes = new List<SpiderAttribute>();

            DTOClassProperties = DTOClassProperties // excluding enumerables from the excel
                .Where(prop => prop.Type.IsEnumerable())
                .ToList();

            // ubacivanje atributa gde vidimo koje propertije treba da preskocimo u Excelu
            if (excelMethod != null)
            {
                foreach (AttributeListSyntax item in excelMethod.AttributeLists)
                {
                    foreach (AttributeSyntax attribute in item.Attributes)
                    {
                        string attributeName = attribute.Name.ToString();
                        if (attributeName != null && attributeName == $"{Helpers.MapperlyIgnoreAttribute}")
                        {
                            string propNameInsideBrackets = attribute.ArgumentList.Arguments.FirstOrDefault().ToString().Split('.').Last().Replace(")", "").Replace("\"", "");
                            //excludePropAttributes.Add(new SpiderAttribute() { Name = attribute.Name.ToString(), PropNameInsideBrackets = propNameInsideBrackets }); // FT: i don't need this if i don't know which prop type it is
                            DTOClassProperties.Add(new SpiderProperty { Name = propNameInsideBrackets });
                        }
                    }
                }
            }

            return DTOClassProperties;
        }
    }
}