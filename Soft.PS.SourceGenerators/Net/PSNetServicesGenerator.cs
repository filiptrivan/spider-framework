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
using System.Linq;
using System.Text;

namespace Soft.SourceGenerator.NgTable.Net
{
    [Generator]
    public class PSNetServicesGenerator : IIncrementalGenerator
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
                    predicate: static (s, _) => Helper.IsSyntaxTargetForGenerationEntities(s),
                    transform: static (ctx, _) => Helper.GetSemanticTargetForGenerationEntities(ctx))
                .Where(static c => c is not null);

            IncrementalValueProvider<IEnumerable<INamedTypeSymbol>> referencedProjectClasses = Helper.GetReferencedProjectsSymbolsEntities(context);

            var allClasses = classDeclarations.Collect()
                .Combine(referencedProjectClasses);

            context.RegisterImplementationSourceOutput(allClasses, static (spc, source) => Execute(source.Left, source.Right, spc));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="classes">Only EF classes</param>
        /// <param name="context"></param>
        private static void Execute(IList<ClassDeclarationSyntax> classes, IEnumerable<INamedTypeSymbol> referencedClassesEntities, SourceProductionContext context)
        {
            if (classes.Count <= 1) return;
            List<ClassDeclarationSyntax> entityClasses = Helper.GetEntityClasses(classes);

            StringBuilder sb = new StringBuilder();

            string[] namespacePartsWithoutLastElement = Helper.GetNamespacePartsWithoutLastElement(entityClasses[0]);

            string basePartOfNamespace = string.Join(".", namespacePartsWithoutLastElement); // eg. Soft.Generator.Security
            string projectName = namespacePartsWithoutLastElement[namespacePartsWithoutLastElement.Length - 1]; // eg. Security

            bool generateAuthorizationMethods = projectName != "Security";

            sb.AppendLine($$"""
usings...

namespace {{basePartOfNamespace}}.Services
{
    public class {{projectName}}BusinessServiceGenerated : BusinessServiceBase
    {
        private readonly SqlConnection _connection;

        public {{projectName}}BusinessServiceGenerated(SqlConnection connection)
        : base(connection)
        {
            _connection = connection
        }
""");
            foreach (ClassDeclarationSyntax entityClass in entityClasses)
            {
                string baseType = entityClass.GetBaseType();

                if (baseType == null) // FT: Handling many to many, maybe you should do something else in the future
                    continue;

                string nameOfTheEntityClass = entityClass.Identifier.Text;
                string nameOfTheEntityClassFirstLower = entityClass.Identifier.Text.FirstCharToLower();
                string idTypeOfTheEntityClass = Helper.GetGenericIdType(entityClass, entityClasses);
                string displayNameProperty = Helper.GetDisplayNamePropForClass(entityClass, entityClasses);

                List<SoftProperty> entityProperties = Helper.GetAllPropertiesOfTheClass(entityClass, entityClasses, true);

                sb.AppendLine($$"""
        public {{nameOfTheEntityClass}} Get{{nameOfTheEntityClass}}({{idTypeOfTheEntityClass}} id)
        {
            List<{{nameOfTheEntityClass}}> {{nameOfTheEntityClassFirstLower}}List = new List<{{nameOfTheEntityClass}}>();
            Dictionary<{{idTypeOfTheEntityClass}}, {{nameOfTheEntityClass}}> {{nameOfTheEntityClassFirstLower}}Dict = new Dictionary<{{idTypeOfTheEntityClass}}, {{nameOfTheEntityClass}}>();

            string query = @$"
SELECT 
{{string.Join(", ", GetAllSelectProperties(entityClass, entityClasses, new HashSet<string>()))}}
FROM {{nameOfTheEntityClass}} AS {{nameOfTheEntityClassFirstLower}}
{{string.Join(", ", GetAllJoins(entityClass, entityClasses, new HashSet<string>()))}}
WHERE {{nameOfTheEntityClassFirstLower}}.Id = @id
";

            _connection.WithTransaction(() =>
            {
                using (SqlCommand cmd = new SqlCommand(query, _connection))
                {
                    cmd.Parameters.AddWithValue("@id", id);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            {{idTypeOfTheEntityClass}} {{nameOfTheEntityClassFirstLower}}Id = reader.{{GetReaderParseMethodForIdType(idTypeOfTheEntityClass)}}(reader.GetOrdinal("{{nameOfTheEntityClass}}Id"));

                            if (!{{nameOfTheEntityClassFirstLower}}Dict.TryGetValue({{nameOfTheEntityClassFirstLower}}Id, out {{nameOfTheEntityClass}} {{nameOfTheEntityClassFirstLower}}))
                            {
                                {{nameOfTheEntityClassFirstLower}} = new {{nameOfTheEntityClass}}
                                {
                                    {{}}
                                    Id = {{nameOfTheEntityClassFirstLower}}Id,
                                    Name = reader.GetString(reader.GetOrdinal("PermissionName")),
                                    Code = reader.GetString(reader.GetOrdinal("PermissionCode")),
                                    Companies = new List<Company>(),
                                };

                                {{nameOfTheEntityClassFirstLower}}Dict[{{nameOfTheEntityClassFirstLower}}Id] = {{nameOfTheEntityClassFirstLower}};
                                {{nameOfTheEntityClassFirstLower}}List.Add({{nameOfTheEntityClassFirstLower}});
                            }

                            if (!reader.IsDBNull(reader.GetOrdinal("CompanyId")))
                            {
                                Company company = new Company
                                {
                                    Id = reader.{{GetReaderParseMethodForIdType(idTypeOfTheEntityClass)}}(reader.GetOrdinal("CompanyId")),
                                    Name = reader.GetString(reader.GetOrdinal("CompanyName"))
                                };

                                {{nameOfTheEntityClassFirstLower}}.Companies.Add(company);
                            }
                        }
                    }
                }
            });

            Permission {{nameOfTheEntityClassFirstLower}} = {{nameOfTheEntityClassFirstLower}}List.SingleOrDefault();

            if ({{nameOfTheEntityClassFirstLower}} == null)
                throw new Exception("Objekat ne postoji u bazi podataka.");

            return {{nameOfTheEntityClassFirstLower}};
        }
""");

                context.AddSource($"{projectName}BusinessService.generated", SourceText.From(sb.ToString(), Encoding.UTF8));
            }

        }

        private static List<string> GetAllSelectProperties(ClassDeclarationSyntax entityClass, IList<ClassDeclarationSyntax> entityClasses, HashSet<string> processedClasses)
        {
            string nameOfTheEntityClass = entityClass.Identifier.Text;
            string nameOfTheEntityClassFirstLower = entityClass.Identifier.Text.FirstCharToLower();

            if (processedClasses.Contains(nameOfTheEntityClass))
                return new List<string>();

            processedClasses.Add(nameOfTheEntityClass);

            List<string> result = new List<string>();

            List<SoftProperty> entityProperties = Helper.GetAllPropertiesOfTheClass(entityClass, entityClasses, true);

            foreach (SoftProperty property in entityProperties)
            {
                // TODO FT: Add the logic for the different names of the association
                if (property.Type.IsEnumerable())
                {
                    ClassDeclarationSyntax extractedEnumerableEntity = Helper.ExtractEntityFromList(property.Type, entityClasses);
                    result.AddRange(GetAllSelectProperties(extractedEnumerableEntity, entityClasses, processedClasses));
                }
                if (property.Type.PropTypeIsManyToOne())
                {
                    ClassDeclarationSyntax manyToOneEntity = Helper.GetClass(property.Type, entityClasses);
                    result.AddRange(GetAllSelectProperties(manyToOneEntity, entityClasses, processedClasses));
                }

                result.Add($"{nameOfTheEntityClassFirstLower}.{property.IdentifierText} AS {nameOfTheEntityClass}{property.IdentifierText}");
            }

            return result;
        }

        private static List<string> GetAllJoins(ClassDeclarationSyntax entityClass, IList<ClassDeclarationSyntax> entityClasses, HashSet<string> processedClasses)
        {
            string nameOfTheEntityClass = entityClass.Identifier.Text;
            string nameOfTheEntityClassFirstLower = entityClass.Identifier.Text.FirstCharToLower();

            if (processedClasses.Contains(nameOfTheEntityClass))
                return new List<string>();

            processedClasses.Add(nameOfTheEntityClass);

            List<string> result = new List<string>();

            List<SoftProperty> entityProperties = Helper.GetAllPropertiesOfTheClass(entityClass, entityClasses, true);

            foreach (SoftProperty property in entityProperties)
            {
                // TODO FT: Add the logic for the different names of the association
                if (property.Type.IsEnumerable())
                {
                    ClassDeclarationSyntax extractedEnumerableEntity = Helper.ExtractEntityFromList(property.Type, entityClasses);
                    result.AddRange(GetAllSelectProperties(extractedEnumerableEntity, entityClasses, processedClasses));

                    string manyToManyName = property.Attributes.Where(x => x.Name == "ManyToMany").Select(x => x.Value).SingleOrDefault();
                    if (manyToManyName == null)
                    {
                        result.Add($$"""
LEFT JOIN {{extractedEnumerableEntity.Identifier.Text}} AS {{extractedEnumerableEntity.Identifier.Text.FirstCharToLower()}} ON {{extractedEnumerableEntity.Identifier.Text.FirstCharToLower()}}.{{nameOfTheEntityClass}}Id = {{nameOfTheEntityClass}}.Id
""");
                    }
                    else
                    {
                        result.Add($$"""
LEFT JOIN {{manyToManyName}} AS {{manyToManyName.FirstCharToLower()}} ON {{nameOfTheEntityClassFirstLower}}.Id = {{manyToManyName.FirstCharToLower()}}.{{nameOfTheEntityClass}}Id
LEFT JOIN {{extractedEnumerableEntity.Identifier.Text}} AS {{extractedEnumerableEntity.Identifier.Text.FirstCharToLower()}} ON {{extractedEnumerableEntity.Identifier.Text.FirstCharToLower()}}.Id = {{manyToManyName.FirstCharToLower()}}.{{extractedEnumerableEntity.Identifier.Text}}Id
""");
                    }
                }
                if (property.Type.PropTypeIsManyToOne())
                {
                    ClassDeclarationSyntax manyToOneEntity = Helper.GetClass(property.Type, entityClasses);
                    result.AddRange(GetAllSelectProperties(manyToOneEntity, entityClasses, processedClasses));

                    result.Add($$"""
LEFT JOIN {{manyToOneEntity.Identifier.Text}} AS {{manyToOneEntity.Identifier.Text.FirstCharToLower()}} ON {{manyToOneEntity.Identifier.Text.FirstCharToLower()}}.{{nameOfTheEntityClass}}Id = {{nameOfTheEntityClass}}.Id
""");
                }
            }

            return result;
        }

        private static string GetReaderParseMethodForIdType(string idType)
        {
            if (idType == "byte")
                return "GetInt16";
            else if (idType == "int")
                return "GetInt32";
            else if (idType == "long")
                return "GetInt64";

            return "NotSupportedIdType";
        }
    }
}