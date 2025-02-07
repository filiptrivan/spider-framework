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
            IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = Helpers.GetClassInrementalValuesProvider(context.SyntaxProvider, new List<NamespaceExtensionCodes>
                {
                    NamespaceExtensionCodes.Entities,
                    NamespaceExtensionCodes.DataMappers,
                });

            context.RegisterImplementationSourceOutput(classDeclarations.Collect(),
                static (spc, source) => Execute(source, spc));
        }

        private static void Execute(IList<ClassDeclarationSyntax> classes, SourceProductionContext context)
        {
            if (classes.Count <= 1) return;

            List<ClassDeclarationSyntax> entityClasses = Helpers.GetEntityClasses(classes);

            ClassDeclarationSyntax mapperClass = Helpers.GetManualyWrittenMapperClass(classes);

            StringBuilder sb = new StringBuilder();

            string[] namespacePartsWithoutLastElement = Helpers.GetNamespacePartsWithoutLastElement(classes[0]);

            string basePartOfNamespace = string.Join(".", namespacePartsWithoutLastElement); // eg. Spider.Security
            string projectName = namespacePartsWithoutLastElement[namespacePartsWithoutLastElement.Length - 1]; // eg. Security

            sb.AppendLine($$"""
using Mapster;
using Microsoft.AspNetCore.Http;
using {{basePartOfNamespace}}.DTO;
using {{basePartOfNamespace}}.Entities;

namespace {{basePartOfNamespace}}.DataMappers
{
    public static partial class Mapper
    {
""");
            foreach (ClassDeclarationSyntax entityClass in entityClasses)
            {
                sb.AppendLine($$"""

        #region {{entityClass.Identifier.Text}}

{{(entityClass.IsAbstract() ? "" : GetMapperToEntity($"{entityClass.Identifier.Text}DTOToEntityConfig", mapperClass, entityClass, entityClasses))}}

{{GetMapperToDTO($"{entityClass.Identifier.Text}ToDTOConfig", mapperClass, entityClass, entityClasses)}}

{{GetMapperToDTO($"{entityClass.Identifier.Text}ProjectToConfig", mapperClass, entityClass, entityClasses)}}

{{GetMapperToDTO($"{entityClass.Identifier.Text}ExcelProjectToConfig", mapperClass, entityClass, entityClasses)}}

        #endregion

""");
            }

            sb.AppendLine($$"""
    }
}
""");

            context.AddSource($"{projectName}Mapper.generated", SourceText.From(sb.ToString(), Encoding.UTF8));
        }

        #region To Entity

        public static string GetMapperToEntity(string methodName, ClassDeclarationSyntax mapperClass, ClassDeclarationSyntax entityClass, IList<ClassDeclarationSyntax> entityClasses)
        {
            if (mapperClass == null)
                return "You didn't define DataMappers";

            if (HasNonGeneratedPair(mapperClass, methodName))
                return "";

            List<string> mappers = GetFromDTOToEntityConfig(entityClass, entityClasses);

            string result = $$"""
        public static TypeAdapterConfig {{methodName}}()
        {
            TypeAdapterConfig config = new TypeAdapterConfig();

            config
                .NewConfig<{{entityClass.Identifier.Text}}DTO, {{entityClass.Identifier.Text}}>()
                {{string.Join("\n\t\t\t\t", mappers)}}
                ;

            return config;
        }
""";

            return result;
        }

        private static List<string> GetFromDTOToEntityConfig(ClassDeclarationSyntax entityClass, IList<ClassDeclarationSyntax> entityClasses)
        {
            List<string> result = new List<string>();

            List<SpiderProperty> entityProperties = Helpers.GetAllPropertiesOfTheClass(entityClass, entityClasses, new List<SpiderClass>());

            foreach (SpiderProperty entityProp in entityProperties)
            {
                string entityPropType = entityProp.Type;
                string entityPropName = entityProp.Name;

                if (entityPropType == "byte[]")
                    result.Add($".Map(dest => dest.{entityPropName}, src => src.{entityPropName} == null ? null : Convert.FromBase64String(src.{entityPropName}))");

                //if (Helper.GetGenericBaseType(entityClass) == null && entityPropName.EndsWith("Id")) // FT: I don't know why i did this for M2M
                //    result.Add($".Ignore(dest => dest.{entityPropName})");
            }

            return result;
        }

        #endregion

        #region To DTO

        public static string GetMapperToDTO(string methodName, ClassDeclarationSyntax mapperClass, ClassDeclarationSyntax entityClass, IList<ClassDeclarationSyntax> entityClasses)
        {
            if (mapperClass == null)
                return "You didn't define DataMappers";

            if (HasNonGeneratedPair(mapperClass, methodName))
                return "";

            List<string> manyToOneMappers = GetConfigForManyToOneClass(entityClass, entityClasses);

            return $$"""
        public static TypeAdapterConfig {{methodName}}()
        {
            TypeAdapterConfig config = new TypeAdapterConfig();

            config
                .NewConfig<{{entityClass.Identifier.Text}}, {{entityClass.Identifier.Text}}DTO>()
                {{string.Join("\n\t\t\t\t", manyToOneMappers)}}
                ;

            return config;
        }
""";
        }

        public static List<string> GetConfigForManyToOneClass(ClassDeclarationSyntax entityClass, IList<ClassDeclarationSyntax> entityClasses)
        {
            List<SpiderProperty> entityProperties = Helpers.GetAllPropertiesOfTheClass(entityClass, entityClasses, new List<SpiderClass>());

            List<string> manyToOneAttributeMappers = new List<string>();

            foreach (SpiderProperty entityProp in entityProperties)
            {
                string entityPropType = entityProp.Type;
                string entityPropName = entityProp.Name;

                if (entityPropType.IsManyToOneType())
                {
                    ClassDeclarationSyntax manyToOneEntityClass = entityClasses
                        .Where(x => x.Identifier.Text == entityPropType)
                        .SingleOrDefault();

                    if (manyToOneEntityClass == null)
                        continue;

                    string displayNamePropOfManyToOne = Helpers.GetDisplayNamePropForClass(manyToOneEntityClass, entityClasses);
                    displayNamePropOfManyToOne = displayNamePropOfManyToOne.Replace(".ToString()", "");

                    manyToOneAttributeMappers.Add($".Map(dest => dest.{entityPropName}Id, src => src.{entityPropName}.Id)"); // "dest.TierId", "src.Tier.Id"
                    manyToOneAttributeMappers.Add($".Map(dest => dest.{entityPropName}DisplayName, src => src.{entityPropName}.{displayNamePropOfManyToOne})"); // "dest.TierDisplayName", "src.Tier.Name"
                }

                if (entityPropType.IsOneToManyType())
                {
                    string entityPropTypeInsideListBrackets = Helpers.ExtractTypeFromGenericType(entityPropType);

                    ClassDeclarationSyntax enumerableEntityClass = entityClasses
                        .Where(x => x.Identifier.Text == entityPropTypeInsideListBrackets)
                        .SingleOrDefault();

                    if (enumerableEntityClass == null)
                        continue;

                    string displayNamePropOfEnumerable = Helpers.GetDisplayNamePropForClass(enumerableEntityClass, entityClasses);
                    displayNamePropOfEnumerable = displayNamePropOfEnumerable.Replace(".ToString()", "");

                    if (entityProp.Attributes.Any(x => x.Name == "GenerateCommaSeparatedDisplayName"))
                    {
                        // FT: eg.                      ".Map(dest => dest.SegmentationItemsCommaSeparated, src => string.Join(", ", src.CheckedSegmentationItems.Select(x => x.Name)))"
                        manyToOneAttributeMappers.Add($".Map(dest => dest.{entityPropName}CommaSeparated, src => string.Join(\", \", src.{entityPropName}.Select(x => x.{displayNamePropOfEnumerable})))");
                    }

                    //if (entityProp.Attributes.Any(x => x.Name == "IncludeInDTO"))
                    //{
                    //    manyToOneAttributeMappers.Add($".Map(dest => dest.{entityPropName}, src => src.{entityPropName}.Adapt<List<{entityPropTypeInsideListBrackets}>>())");
                    //}
                }

                if (entityPropType == "byte[]")
                {
                    manyToOneAttributeMappers.Add($".Map(dest => dest.{entityPropName}, src => src.{entityPropName} == null ? null : Convert.ToBase64String(src.{entityPropName}))");
                }
            }

            return manyToOneAttributeMappers;
        }

        #endregion

        #region Helpers

        private static bool HasNonGeneratedPair(ClassDeclarationSyntax mapperClass, string methodName)
        {
            List<MethodDeclarationSyntax> nonGeneratedMethods = mapperClass?.Members.OfType<MethodDeclarationSyntax>().ToList();

            if (nonGeneratedMethods.Any(x => x.Identifier.Text == methodName))
                return true;

            return false;
        }

        #endregion
    }
}
