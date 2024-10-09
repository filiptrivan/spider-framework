using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Soft.SourceGenerator.NgTable.Helpers;
using Soft.SourceGenerators.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Soft.SourceGenerator.NgTable.Net
{
    [Generator]
    public class MapperlyGenerator : IIncrementalGenerator
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
                    predicate: static (s, _) => Helper.IsSyntaxTargetForGenerationEntitiesAndDataMappers(s),
                    transform: static (ctx, _) => Helper.GetSemanticTargetForGenerationEntitiesAndDataMapper(ctx))
                .Where(static c => c is not null);

            context.RegisterImplementationSourceOutput(classDeclarations.Collect(),
                static (spc, source) => Execute(source, spc));
        }

        /// <summary>
        /// </summary>
        /// <param name="classes">Only EF classes</param>
        /// <param name="context"></param>
        private static void Execute(IList<ClassDeclarationSyntax> classes, SourceProductionContext context)
        {
            if (classes.Count <= 1) return;

            string outputPath = Helper.GetGeneratorOutputPath(nameof(MapperlyGenerator), classes);
            if (outputPath == null) return;

            List<ClassDeclarationSyntax> entityClassesHelper = Helper.GetEntityClasses(classes);
            List<ClassDeclarationSyntax> entityClasses = Helper.GetUninheritedClasses(entityClassesHelper);

            ClassDeclarationSyntax mapperClass = Helper.GetManualyWrittenMapperClass(classes);

            StringBuilder sb = new StringBuilder();

            string[] namespacePartsWithoutLastElement = Helper.GetNamespacePartsWithoutLastElement(classes[0]);

            string basePartOfNamespace = string.Join(".", namespacePartsWithoutLastElement); // eg. Soft.Generator.Security
            string projectName = namespacePartsWithoutLastElement[namespacePartsWithoutLastElement.Length - 1]; // eg. Security
            string[] namespacePartsWithoutTwoLastElements = namespacePartsWithoutLastElement.Take(namespacePartsWithoutLastElement.Length - 1).ToArray();
            string wholeProjectBasePartOfNamespace = string.Join(".", namespacePartsWithoutTwoLastElements); // eg. Soft.Generator

            sb.AppendLine($$"""
using Mapster;
using {{basePartOfNamespace}}.DTO;
using {{basePartOfNamespace}}.Entities;

namespace {{basePartOfNamespace}}.DataMappers
{
    public static partial class Mapper
    {
""");
            foreach (ClassDeclarationSyntax c in entityClasses)
            {
                //string baseClass = c.GetBaseType();
                //if (baseClass == null)
                //    continue;

        //{{(c.IsAbstract() ? "" : GetMapper("ToEntityConfig", c.Identifier.Text, mapperClass))}} // FT: I think we don't need this anymore
        //{{GetMapperDTO($"ExcelProjectToConfig", mapperClass, c, entityClasses)}} // FT: Excel map or project to, you don't need both

                sb.AppendLine($$"""

        #region {{c.Identifier.Text}}

        {{GetMapperDTO($"{c.Identifier.Text}ToDTOConfig", mapperClass, c, entityClasses)}}

        {{GetMapperDTO($"{c.Identifier.Text}ProjectToConfig", mapperClass, c, entityClasses)}}

        {{GetMapperDTO($"{c.Identifier.Text}ExcelProjectToConfig", mapperClass, c, entityClasses)}}

        #endregion

""");
            }

            sb.AppendLine($$"""
    }
}
""");

            context.AddSource($"{projectName}Mapper.generated", SourceText.From(sb.ToString(), Encoding.UTF8));
        }

        [Obsolete]
        public static string GetMapper(string methodName, string entityClassName, ClassDeclarationSyntax mapperClass)
        {
            if (mapperClass == null)
                return "You didn't define DataMappers";

            if (HasNonGeneratedPair(mapperClass, methodName))
                return "";

            string result = $$"""
.
""";

            return result;
        }

        private static bool HasNonGeneratedPair(ClassDeclarationSyntax mapperClass, string methodName)
        {
            List<MethodDeclarationSyntax> nonGeneratedMethods = mapperClass?.Members.OfType<MethodDeclarationSyntax>().ToList();

            if (nonGeneratedMethods.Any(x => x.Identifier.Text == methodName))
                return true;

            return false;
        }

        public static string GetMapperDTO(string methodName, ClassDeclarationSyntax mapperClass, ClassDeclarationSyntax c, IList<ClassDeclarationSyntax> entityClasses)
        {
            if (mapperClass == null)
                return "You didn't define DataMappers";

            if (HasNonGeneratedPair(mapperClass, methodName))
                return "";

            List<string> manyToOneAttributeMappers = GetAttributesForManyToOneClass(c, entityClasses);

            return $$"""
        public static TypeAdapterConfig {{methodName}}()
        {
            TypeAdapterConfig config = new TypeAdapterConfig();

            config
                .NewConfig<{{c.Identifier.Text}}, {{c.Identifier.Text}}DTO>()
{{string.Join("\t\t\t\t\n", manyToOneAttributeMappers)}}
                ;

            return config;
        }
""";
        }

        public static List<string> GetAttributesForManyToOneClass(ClassDeclarationSyntax c, IList<ClassDeclarationSyntax> entityClasses)
        {
            // po svakom propertiju iz currentClass idem, ako je ManyToOne objekat onda mu postavljam DisplayName iz Entiteta
            List<PropertyDeclarationSyntax> properties = c.Members.OfType<PropertyDeclarationSyntax>().ToList();
            List<string> manyToOneAttributeMappers = new List<string>();
            foreach (PropertyDeclarationSyntax prop in properties)
            {
                string propType = prop.Type.ToString(); // User
                string propName = prop.Identifier.Text;

                if (propType.PropTypeIsManyToOne())
                {
                    ClassDeclarationSyntax entityClass = entityClasses
                        .Where(x => x.Identifier.Text == propType)
                        .SingleOrDefault();

                    if (entityClass == null)
                        continue;

                    string displayNamePropOfManyToOne = Helper.GetDisplayNamePropForClass(entityClass, entityClasses);
                    displayNamePropOfManyToOne = displayNamePropOfManyToOne.Replace(".ToString()", "");

                    manyToOneAttributeMappers.Add($".Map(dest => dest.{propName}Id, src => src.{propName}.Id)"); // "dest.TierId", "src.Tier.Id"
                    manyToOneAttributeMappers.Add($".Map(dest => dest.{propName}DisplayName, src => src.{propName}.{displayNamePropOfManyToOne})"); // "dest.TierDisplayName", "src.Tier.Name"
                }
            }

            return manyToOneAttributeMappers;
        }

    }
}
