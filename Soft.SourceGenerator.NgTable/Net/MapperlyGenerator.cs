using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Soft.SourceGenerator.NgTable.Helpers;
using Soft.SourceGenerators.Helpers;
using Soft.SourceGenerators.Models;
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
            foreach (ClassDeclarationSyntax entityClass in entityClasses)
            {
                //string baseClass = c.GetBaseType();
                //if (baseClass == null)
                //    continue;

        //{{(c.IsAbstract() ? "" : GetMapper("ToEntityConfig", c.Identifier.Text, mapperClass))}} // FT: I think we don't need this anymore
        //{{GetMapperDTO($"ExcelProjectToConfig", mapperClass, c, entityClasses)}} // FT: Excel map or project to, you don't need both

                sb.AppendLine($$"""

        #region {{entityClass.Identifier.Text}}

        {{GetMapperDTO($"{entityClass.Identifier.Text}ToDTOConfig", mapperClass, entityClass, entityClasses)}}

        {{GetMapperDTO($"{entityClass.Identifier.Text}ProjectToConfig", mapperClass, entityClass, entityClasses)}}

        {{GetMapperDTO($"{entityClass.Identifier.Text}ExcelProjectToConfig", mapperClass, entityClass, entityClasses)}}

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

        public static string GetMapperDTO(string methodName, ClassDeclarationSyntax mapperClass, ClassDeclarationSyntax entityClass, IList<ClassDeclarationSyntax> entityClasses)
        {
            if (mapperClass == null)
                return "You didn't define DataMappers";

            if (HasNonGeneratedPair(mapperClass, methodName))
                return "";

            List<string> manyToOneAttributeMappers = GetAttributesForManyToOneClass(entityClass, entityClasses);

            return $$"""
        public static TypeAdapterConfig {{methodName}}()
        {
            TypeAdapterConfig config = new TypeAdapterConfig();

            config
                .NewConfig<{{entityClass.Identifier.Text}}, {{entityClass.Identifier.Text}}DTO>()
{{string.Join("\t\t\t\t\n", manyToOneAttributeMappers)}}
                ;

            return config;
        }
""";
        }

        public static List<string> GetAttributesForManyToOneClass(ClassDeclarationSyntax entityClass, IList<ClassDeclarationSyntax> entityClasses)
        {
            List<SoftProperty> entityProperties = Helper.GetAllPropertiesOfTheClass(entityClass, entityClasses, true);

            List<string> manyToOneAttributeMappers = new List<string>();

            foreach (SoftProperty entityProp in entityProperties)
            {
                string entityPropType = entityProp.Type;
                string entityPropName = entityProp.IdentifierText;

                if (entityPropType.PropTypeIsManyToOne())
                {
                    ClassDeclarationSyntax manyToOneEntityClass = entityClasses
                        .Where(x => x.Identifier.Text == entityPropType)
                        .SingleOrDefault();

                    if (manyToOneEntityClass == null)
                        continue;

                    string displayNamePropOfManyToOne = Helper.GetDisplayNamePropForClass(manyToOneEntityClass, entityClasses);
                    displayNamePropOfManyToOne = displayNamePropOfManyToOne.Replace(".ToString()", "");

                    manyToOneAttributeMappers.Add($".Map(dest => dest.{entityPropName}Id, src => src.{entityPropName}.Id)"); // "dest.TierId", "src.Tier.Id"
                    manyToOneAttributeMappers.Add($".Map(dest => dest.{entityPropName}DisplayName, src => src.{entityPropName}.{displayNamePropOfManyToOne})"); // "dest.TierDisplayName", "src.Tier.Name"
                }

                if (entityPropType.IsEnumerable() && entityProp.Attributes.Any(x => x.Name == "GenerateCommaSeparatedDisplayName"))
                {
                    string entityPropTypeInsideListBrackets = Helper.ExtractTypeFromGenericType(entityPropType);

                    ClassDeclarationSyntax enumerableEntityClass = entityClasses
                        .Where(x => x.Identifier.Text == entityPropTypeInsideListBrackets)
                        .SingleOrDefault();

                    if (enumerableEntityClass == null)
                        continue;

                    string displayNamePropOEnumerable = Helper.GetDisplayNamePropForClass(enumerableEntityClass, entityClasses);
                    displayNamePropOEnumerable = displayNamePropOEnumerable.Replace(".ToString()", "");

                    // FT: eg.                      ".Map(dest => dest.SegmentationItemsCommaSeparated, src => string.Join(", ", src.CheckedSegmentationItems.Select(x => x.Name)))"
                    manyToOneAttributeMappers.Add($".Map(dest => dest.{entityPropName}CommaSeparated, src => string.Join(\", \", src.{entityPropName}.Select(x => x.{displayNamePropOEnumerable})))");
                }
            }

            return manyToOneAttributeMappers;
        }

    }
}
