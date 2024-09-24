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
using Riok.Mapperly.Abstractions;
using {{basePartOfNamespace}}.DTO;
using {{basePartOfNamespace}}.Entities;

namespace {{basePartOfNamespace}}.DataMappers
{
    [Mapper(EnabledConversions = MappingConversionType.All)]
    public static partial class Mapper
    {
""");
            foreach (ClassDeclarationSyntax c in entityClasses)
            {
                string baseClass = c.GetBaseType();
                if (baseClass == null)
                    continue;

                sb.AppendLine($$"""

        #region {{c.Identifier.Text}}

        {{(c.IsAbstract() ? "" : GetMapper($"public static partial {c.Identifier.Text} Map({c.Identifier.Text}DTO dto);", mapperClass))}}

        {{GetMapperDTO($"public static partial {c.Identifier.Text}DTO Map({c.Identifier.Text} poco);", mapperClass, c, entityClasses)}}

        {{GetMapperDTO($"public static partial {c.Identifier.Text}DTO ExcelMap({c.Identifier.Text} poco);", mapperClass, c, entityClasses)}}

        {{GetMapperDTO($"public static partial IQueryable<{c.Identifier.Text}DTO> ProjectTo(this IQueryable<{c.Identifier.Text}> poco);", mapperClass, c, entityClasses)}}

        {{GetMapperDTO($"public static partial IQueryable<{c.Identifier.Text}DTO> ExcelProjectTo(this IQueryable<{c.Identifier.Text}> poco);", mapperClass, c, entityClasses)}}

        {{GetMapper($"public static partial void MergeMap({c.Identifier.Text}DTO dto, {c.Identifier.Text} poco);", mapperClass)}}

        #endregion

""");
            }

            sb.AppendLine($$"""
    }
}
""");

            Helper.WriteToTheFile(sb.ToString(), $@"{outputPath}");
            // FT: does not generating because we make file on the disk
            //context.AddSource($"{projectName}Mapper.generated", SourceText.From(sb.ToString(), Encoding.UTF8));
        }

        public static string GetMapper(string input, ClassDeclarationSyntax mapperClass)
        {
            if (mapperClass == null)
                return "You didn't define DataMappers";
            List<MethodDeclarationSyntax> methods = mapperClass?.Members.OfType<MethodDeclarationSyntax>().ToList();
            foreach (MethodDeclarationSyntax method in methods)
            {
                string uniquePartOfTheMethod = $"public static partial {method.ReturnType} {method.Identifier}";
                if (input.StartsWith(uniquePartOfTheMethod))
                {
                    return "";
                }
            }

            return input;
        }

        public static string GetMapperDTO(string input, ClassDeclarationSyntax mapperClass, ClassDeclarationSyntax c, IList<ClassDeclarationSyntax> entityClasses)
        {
            string mapper = GetMapper(input, mapperClass);
            if (mapper == "")
                return "";

            List<string> manyToOneAttributeMappers = GetAttributesForManyToOneClass(c, entityClasses);

            return $$"""
        {{string.Join("\n", manyToOneAttributeMappers)}}
        {{input}}
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

                    manyToOneAttributeMappers.Add($"[MapProperty(\"{propName}.Id\", \"{propName}Id\")]"); // [MapProperty("User.Id", "UserId")]
                    manyToOneAttributeMappers.Add($"[MapProperty(\"{propName}.{displayNamePropOfManyToOne}\", \"{propName}DisplayName\")]"); // [MapProperty("User.Name", "UserDisplayName")]
                }
            }

            return manyToOneAttributeMappers;
        }

    }
}
