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
using System.Reflection.Metadata;
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
            if (classes.Count == 0) return; // We don't need config
            List<ClassDeclarationSyntax> entityClasses = Helper.GetEntityClasses(classes);

            StringBuilder sb = new StringBuilder();

            string[] namespacePartsWithoutLastElement = Helper.GetNamespacePartsWithoutLastElement(entityClasses[0]);

            string basePartOfNamespace = string.Join(".", namespacePartsWithoutLastElement); // eg. Soft.Generator.Security
            string projectName = namespacePartsWithoutLastElement[namespacePartsWithoutLastElement.Length - 1]; // eg. Security

            bool generateAuthorizationMethods = projectName != "Security";

            sb.AppendLine($$"""
using Microsoft.Data.SqlClient;
using {{basePartOfNamespace}}.Entities;
using {{basePartOfNamespace}}.Extensions;
using {{basePartOfNamespace}}.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace {{basePartOfNamespace}}.Services
{
    /// <summary>
    /// Every get method is returning only flat data without any related data, because of performance
    /// When inserting data with a foreign key, only the Id field in related data is mandatory. Additionally, the Id must correspond to an existing record in the database.
    /// </summary>
    public class {{projectName}}BusinessServiceGenerated : BusinessServiceBase
    {
        private readonly SqlConnection _connection;

        public {{projectName}}BusinessServiceGenerated(SqlConnection connection)
        : base(connection)
        {
            _connection = connection;
        }

""");
            foreach (ClassDeclarationSyntax entityClass in entityClasses)
            {
                //string baseType = entityClass.GetBaseType();

                //if (baseType == null) // FT: Handling many to many, maybe you should do something else in the future
                //    continue;

                if (Helper.GetAllAttributesOfTheClass(entityClass, entityClasses).Any(x => x.Name == "ManyToMany"))
                    continue;

                string nameOfTheEntityClass = entityClass.Identifier.Text;
                string nameOfTheEntityClassFirstLower = entityClass.Identifier.Text.FirstCharToLower();
                SoftProperty idPropertyOfTheEntityClass = GetIdentifierProperty(entityClass, entityClasses);
                string idTypeOfTheEntityClass = idPropertyOfTheEntityClass.Type;

                List<SoftProperty> entityPropertiesWithoutEnumerable = Helper.GetAllPropertiesOfTheClass(entityClass, entityClasses, false);

                List<SoftProperty> entityPropertiesWithoutEnumerableAndId = entityPropertiesWithoutEnumerable
                    .Where(x => x.Attributes.Any(x => x.Name == "Identifier") == false)
                    .ToList();

                List<string> entityPropertiesForInsertAndUpdate = entityPropertiesWithoutEnumerableAndId
                    .Select(x => x.Type.PropTypeIsManyToOne() ? $"{x.IdentifierText}Id" : $"{x.IdentifierText}")
                    .ToList();

                string insertColumnNames = string.Join(", ", entityPropertiesForInsertAndUpdate.Select(x => $"[{x}]"));
                string insertParameterNames = string.Join(", ", entityPropertiesForInsertAndUpdate.Select(x => $"@{x}"));

                string updateParameterNames = string.Join(", ", entityPropertiesForInsertAndUpdate.Select(x => $"[{x}] = @{x}"));

                sb.AppendLine($$"""
        #region {{nameOfTheEntityClass}}

        public {{nameOfTheEntityClass}} Get{{nameOfTheEntityClass}}({{idTypeOfTheEntityClass}} id)
        {
            List<{{nameOfTheEntityClass}}> {{nameOfTheEntityClassFirstLower}}List = new List<{{nameOfTheEntityClass}}>();
            Dictionary<{{idTypeOfTheEntityClass}}, {{nameOfTheEntityClass}}> {{nameOfTheEntityClassFirstLower}}Dict = new Dictionary<{{idTypeOfTheEntityClass}}, {{nameOfTheEntityClass}}>();

            string query = @$"
SELECT 
{{string.Join(", ", GetAllPropertiesForSqlSelect(entityClass, entityClasses))}}
FROM [{{nameOfTheEntityClass}}] AS [{{nameOfTheEntityClassFirstLower}}]
{{string.Join("\n", GetManyToOneFirstLevelSqlJoins(entityClass, entityClasses))}}
WHERE [{{nameOfTheEntityClassFirstLower}}].[{{idPropertyOfTheEntityClass.IdentifierText}}] = @id
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
{{FillData(entityClass, entityClasses)}}
                        }
                    }
                }
            });

            {{nameOfTheEntityClass}} {{nameOfTheEntityClassFirstLower}} = {{nameOfTheEntityClassFirstLower}}List.SingleOrDefault();

            if ({{nameOfTheEntityClassFirstLower}} == null)
                throw new Exception("Objekat ne postoji u bazi podataka.");

            return {{nameOfTheEntityClassFirstLower}};
        }

        public List<{{nameOfTheEntityClass}}> Get{{nameOfTheEntityClass}}List()
        {
            List<{{nameOfTheEntityClass}}> {{nameOfTheEntityClassFirstLower}}List = new List<{{nameOfTheEntityClass}}>();
            Dictionary<{{idTypeOfTheEntityClass}}, {{nameOfTheEntityClass}}> {{nameOfTheEntityClassFirstLower}}Dict = new Dictionary<{{idTypeOfTheEntityClass}}, {{nameOfTheEntityClass}}>();

            string query = @$"
SELECT 
{{string.Join(", ", GetAllPropertiesForSqlSelect(entityClass, entityClasses))}}
FROM [{{nameOfTheEntityClass}}] AS [{{nameOfTheEntityClassFirstLower}}]
{{string.Join("\n", GetManyToOneFirstLevelSqlJoins(entityClass, entityClasses))}}
";

            _connection.WithTransaction(() =>
            {
                using (SqlCommand cmd = new SqlCommand(query, _connection))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
{{FillData(entityClass, entityClasses)}}
                        }
                    }
                }
            });

            return {{nameOfTheEntityClassFirstLower}}List;
        }

{{string.Join("\n\n", GetListMethodsWithFilters(entityClass, entityClasses))}}

        private {{nameOfTheEntityClass}} Insert{{nameOfTheEntityClass}}({{nameOfTheEntityClass}} entity)
        {
            if (entity == null)
                throw new Exception("Ne možete da ubacite prazan objekat.");

            // FT: Not validating here property by property, because sql server will throw exception, we should already validate object on the form.

            string query = $"INSERT INTO [{{nameOfTheEntityClass}}] ({{insertColumnNames}}) OUTPUT INSERTED.[{{idPropertyOfTheEntityClass.IdentifierText}}] VALUES ({{insertParameterNames}});";

            _connection.WithTransaction(() =>
            {
                using (SqlCommand cmd = new SqlCommand(query, _connection))
                {
                    {{string.Join("\n\t\t\t\t\t", entityPropertiesWithoutEnumerableAndId
                        .Select(x => x.Type.PropTypeIsManyToOne() ?
                            $"cmd.Parameters.AddWithValue(\"@{x.IdentifierText}Id\", entity.{x.IdentifierText}.{GetIdentifierProperty(x.Type, entityClasses).IdentifierText});" :
                            $"cmd.Parameters.AddWithValue(\"@{x.IdentifierText}\", entity.{x.IdentifierText});"))}}

                    {{idTypeOfTheEntityClass}} newId = ({{idTypeOfTheEntityClass}})cmd.ExecuteScalar();
                    entity.{{idPropertyOfTheEntityClass.IdentifierText}} = newId;
                }
            });

            return entity;
        }

        private {{nameOfTheEntityClass}} Update{{nameOfTheEntityClass}}({{nameOfTheEntityClass}} entity)
        {
            if (entity == null)
                throw new Exception("Ne možete da ažurirate prazan objekat.");

            // FT: Not validating here property by property, because sql server will throw exception, we should already validate object on the form.

            string query = $"UPDATE [{{nameOfTheEntityClass}}] SET {{updateParameterNames}} where [{{idPropertyOfTheEntityClass.IdentifierText}}] = @{{idPropertyOfTheEntityClass.IdentifierText}};";

            _connection.WithTransaction(() =>
            {
                using (SqlCommand cmd = new SqlCommand(query, _connection))
                {
                    cmd.Parameters.AddWithValue("@{{idPropertyOfTheEntityClass.IdentifierText}}", entity.{{idPropertyOfTheEntityClass.IdentifierText}});
                    {{string.Join("\n\t\t\t\t\t", entityPropertiesWithoutEnumerableAndId.Select(x => x.Type.PropTypeIsManyToOne() ? $"cmd.Parameters.AddWithValue(\"@{x.IdentifierText}Id\", entity.{x.IdentifierText}.{GetIdentifierProperty(x.Type, entityClasses).IdentifierText});" : $"cmd.Parameters.AddWithValue(\"@{x.IdentifierText}\", entity.{x.IdentifierText});"))}}

                    cmd.ExecuteNonQuery();
                }
            });

            return entity;
        }

        public {{nameOfTheEntityClass}} Save{{nameOfTheEntityClass}}({{nameOfTheEntityClass}} entity)
        {
            if (entity == null)
                throw new Exception("Ne možete da sačuvate prazan objekat.");

            if (entity.Id > 0)
            {
                return Update{{nameOfTheEntityClass}}(entity);
            }
            else if (entity.Id == 0)
            {
                return Insert{{nameOfTheEntityClass}}(entity);
            }
            else
            {
                throw new InvalidOperationException("Vrednost identifikatora ne sme biti negativna.");
            }
        }

        public void Delete{{nameOfTheEntityClass}}({{idTypeOfTheEntityClass}} id)
        {
            _connection.WithTransaction(() =>
            {
{{string.Join("\n", GetManyToOneDeleteQueries(entityClass, entityClasses, null, 0))}}

                DeleteEntity<{{nameOfTheEntityClass}}, {{idTypeOfTheEntityClass}}>(id);
            });
        }

        #endregion

""");
            }

            sb.Append($$"""
    }
}
""");

            context.AddSource($"{projectName}BusinessService.generated", SourceText.From(sb.ToString(), Encoding.UTF8));
        }

        private static List<string> GetListMethodsWithFilters(ClassDeclarationSyntax entityClass, IList<ClassDeclarationSyntax> entityClasses)
        {
            List<string> result = new List<string>();

            string nameOfTheEntityClass = entityClass.Identifier.Text;
            string nameOfTheEntityClassFirstLower = entityClass.Identifier.Text.FirstCharToLower();
            string idTypeOfTheEntityClass = GetIdType(entityClass, entityClasses);

            List<SoftProperty> entityProperties = Helper.GetAllPropertiesOfTheClass(entityClass, entityClasses, true);

            foreach (SoftProperty entityProperty in entityProperties)
            {
                if (entityProperty.Type.IsEnumerable())
                {
                    ClassDeclarationSyntax extractedEntityClass = Helper.ExtractEntityFromList(entityProperty.Type, entityClasses);
                    SoftProperty extractedEntityIdProperty = GetIdentifierProperty(extractedEntityClass, entityClasses);

                    result.Add($$"""
        public List<{{nameOfTheEntityClass}}> Get{{nameOfTheEntityClass}}ListFor{{extractedEntityClass.Identifier.Text}}List(List<{{extractedEntityIdProperty.Type}}> ids)
        {
            List<{{nameOfTheEntityClass}}> {{nameOfTheEntityClassFirstLower}}List = new List<{{nameOfTheEntityClass}}>();
            Dictionary<{{idTypeOfTheEntityClass}}, {{nameOfTheEntityClass}}> {{nameOfTheEntityClassFirstLower}}Dict = new Dictionary<{{idTypeOfTheEntityClass}}, {{nameOfTheEntityClass}}>();

            if (ids == null || ids.Count == 0)
                throw new ArgumentException("Lista po kojoj želite da filtrirate ne može da bude prazna.");

            List<string> parameters = new List<string>();
            for (int i = 0; i < ids.Count; i++)
            {
                parameters.Add($"@id{i}");
            }

            string query = @$"
SELECT DISTINCT
{{string.Join(", ", GetAllPropertiesForSqlSelect(entityClass, entityClasses))}}
FROM [{{nameOfTheEntityClass}}] AS [{{nameOfTheEntityClassFirstLower}}]
{{GetJoinBasedOnPropertyListType(entityProperty, entityClass, entityClasses)}}
WHERE [{{extractedEntityClass.Identifier.Text.FirstCharToLower()}}].[{{extractedEntityIdProperty.IdentifierText}}] IN ({string.Join(", ", parameters)});
";

            _connection.WithTransaction(() =>
            {
                using (SqlCommand cmd = new SqlCommand(query, _connection))
                {
                    for (int i = 0; i < ids.Count; i++)
                    {
                        cmd.Parameters.AddWithValue($"@id{i}", ids[i]);
                    }

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
{{FillData(entityClass, entityClasses)}}
                        }
                    }
                }
            });

            return {{nameOfTheEntityClassFirstLower}}List;
        }
""");
                }
                else if (entityProperty.Type.PropTypeIsManyToOne())
                {
                    ClassDeclarationSyntax extractedEntityClass = Helper.GetClass(entityProperty.Type, entityClasses);
                    string extractedEntityIdType = GetIdType(extractedEntityClass, entityClasses);

                    result.Add($$"""
        public List<{{nameOfTheEntityClass}}> Get{{nameOfTheEntityClass}}ListFor{{extractedEntityClass.Identifier.Text}}({{extractedEntityIdType}} id)
        {
            List<{{nameOfTheEntityClass}}> {{nameOfTheEntityClassFirstLower}}List = new List<{{nameOfTheEntityClass}}>();
            Dictionary<{{idTypeOfTheEntityClass}}, {{nameOfTheEntityClass}}> {{nameOfTheEntityClassFirstLower}}Dict = new Dictionary<{{idTypeOfTheEntityClass}}, {{nameOfTheEntityClass}}>();

            string query = @$"
SELECT
{{string.Join(", ", GetAllPropertiesForSqlSelect(entityClass, entityClasses))}}
FROM [{{nameOfTheEntityClass}}] AS [{{nameOfTheEntityClassFirstLower}}]
{{string.Join("\n", GetManyToOneFirstLevelSqlJoins(entityClass, entityClasses))}}
WHERE [{{nameOfTheEntityClassFirstLower}}].[{{extractedEntityClass.Identifier.Text}}Id] = @id;
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
{{FillData(entityClass, entityClasses)}}
                        }
                    }
                }
            });

            return {{nameOfTheEntityClassFirstLower}}List;
        }
""");
                }
            }

            return result;
        }

        private static string FillData(ClassDeclarationSyntax entityClass, IList<ClassDeclarationSyntax> entityClasses)
        {
            string nameOfTheEntityClass = entityClass.Identifier.Text;
            string nameOfTheEntityClassFirstLower = entityClass.Identifier.Text.FirstCharToLower();
            string idTypeOfTheEntityClass = GetIdType(entityClass, entityClasses);

            StringBuilder sb = new StringBuilder();

            sb.Append($$"""
                            {{idTypeOfTheEntityClass}} {{nameOfTheEntityClassFirstLower}}Id = reader.{{GetReaderParseMethodForType(idTypeOfTheEntityClass)}}(reader.GetOrdinal("{{nameOfTheEntityClass}}Id"));
                            bool {{nameOfTheEntityClassFirstLower}}AlreadyAdded = {{nameOfTheEntityClassFirstLower}}Dict.TryGetValue({{nameOfTheEntityClassFirstLower}}Id, out {{nameOfTheEntityClass}} {{nameOfTheEntityClassFirstLower}});
                            if (!{{nameOfTheEntityClassFirstLower}}AlreadyAdded)
                            {
                                {{nameOfTheEntityClassFirstLower}} = new {{nameOfTheEntityClass}}
                                {
                                    {{string.Join(",\n\t\t\t\t\t\t\t\t\t", GetClassInitialization(entityClass, entityClasses, null, null))}}
                                };

                                {{nameOfTheEntityClassFirstLower}}Dict[{{nameOfTheEntityClassFirstLower}}Id] = {{nameOfTheEntityClassFirstLower}};

{{string.Join("\n\n", FillManyToOneFirstLevelData(entityClass, entityClasses))}}

                                {{nameOfTheEntityClassFirstLower}}List.Add({{nameOfTheEntityClassFirstLower}});
                            }
""");

            return sb.ToString();
        }

        private static List<string> FillManyToOneFirstLevelData(ClassDeclarationSyntax entityClass, IList<ClassDeclarationSyntax> entityClasses)
        {
            List<string> result = new List<string>();

            string nameOfTheEntityClass = entityClass.Identifier.Text;
            string nameOfTheEntityClassFirstLower = entityClass.Identifier.Text.FirstCharToLower();

            List<SoftProperty> entityPropertiesForTheJoin = Helper.GetAllPropertiesOfTheClass(entityClass, entityClasses, true).Where(x => x.Type.PropTypeIsManyToOne()).ToList();

            foreach (SoftProperty property in entityPropertiesForTheJoin)
            {
                ClassDeclarationSyntax manyToOneEntity = Helper.GetClass(property.Type, entityClasses);

                result.Add($$"""
                            if (!reader.IsDBNull(reader.GetOrdinal("{{nameOfTheEntityClass}}{{property.IdentifierText}}Id")))
                            {
                                {{nameOfTheEntityClassFirstLower}}.{{property.IdentifierText}} = new {{property.Type}}
                                {
                                    {{string.Join(",\n\t\t\t\t\t\t\t\t\t", GetClassInitialization(manyToOneEntity, entityClasses, property, nameOfTheEntityClass))}}
                                };
                            }
""");
            }

            return result;
        }

        private static List<string> GetClassInitialization(ClassDeclarationSyntax entityClass, IList<ClassDeclarationSyntax> entityClasses, SoftProperty manyToOneProperty, string parentClassName)
        {
            List<string> result = new List<string>();

            string nameOfTheEntityClass = entityClass.Identifier.Text;
            string nameOfTheEntityClassFirstLower = entityClass.Identifier.Text.FirstCharToLower();
            string idTypeOfTheEntityClass = GetIdType(entityClass, entityClasses);

            List<SoftProperty> entityProperties = Helper.GetAllPropertiesOfTheClass(entityClass, entityClasses, false);

            foreach (SoftProperty entityProperty in entityProperties)
            {
                if (entityProperty.IsIdentifier())
                {
                    if (manyToOneProperty == null && parentClassName == null)
                        result.Add($"{entityProperty.IdentifierText} = {nameOfTheEntityClassFirstLower}Id");
                    else
                        result.Add($"{entityProperty.IdentifierText} = reader.{GetReaderParseMethodForType(idTypeOfTheEntityClass)}(reader.GetOrdinal(\"{parentClassName}{manyToOneProperty.IdentifierText}Id\"))");
                }
                else if (entityProperty.Type.IsEnumerable() || entityProperty.Type.PropTypeIsManyToOne())
                {
                    continue;
                    //result.Add($"{entityProperty.IdentifierText} = new {entityProperty.Type}()");
                }
                else
                {
                    if (manyToOneProperty == null && parentClassName == null)
                        result.Add($"{entityProperty.IdentifierText} = reader.{GetReaderParseMethodForType(entityProperty.Type)}(reader.GetOrdinal(\"{nameOfTheEntityClass}{entityProperty.IdentifierText}\"))");
                    else
                        result.Add($"{entityProperty.IdentifierText} = reader.{GetReaderParseMethodForType(entityProperty.Type)}(reader.GetOrdinal(\"{parentClassName}{manyToOneProperty.IdentifierText}{entityProperty.IdentifierText}\"))");
                }
            }

            return result;
        }

        private static List<string> GetAllPropertiesForSqlSelect(ClassDeclarationSyntax entityClass, IList<ClassDeclarationSyntax> entityClasses)
        {
            string nameOfTheEntityClass = entityClass.Identifier.Text;

            string nameOfTheEntityClassFirstLower = entityClass.Identifier.Text.FirstCharToLower();

            List<string> result = new List<string>();

            List<SoftProperty> entityProperties = Helper.GetAllPropertiesOfTheClass(entityClass, entityClasses, false);

            foreach (SoftProperty property in entityProperties)
            {
                if (property.Type.PropTypeIsManyToOne())
                {
                    ClassDeclarationSyntax manyToOneEntity = Helper.GetClass(property.Type, entityClasses);
                    List<SoftProperty> manyToOneEntityProperties = Helper.GetAllPropertiesOfTheClass(manyToOneEntity, entityClasses, false);

                    foreach (SoftProperty manyToOneProperty in manyToOneEntityProperties)
                    {
                        if (manyToOneProperty.IsIdentifier())
                        {
                            result.Add($"[{nameOfTheEntityClassFirstLower}].[{property.IdentifierText}Id] AS [{nameOfTheEntityClass}{property.IdentifierText}Id]");
                        }
                        else if (manyToOneProperty.Type.PropTypeIsManyToOne())
                        {
                            continue;
                        }
                        else
                        {
                            result.Add($"[{nameOfTheEntityClassFirstLower}{manyToOneEntity.Identifier.Text}].[{manyToOneProperty.IdentifierText}] AS [{nameOfTheEntityClass}{manyToOneProperty.IdentifierText}]");
                        }
                    }
                }
                else
                {
                    result.Add($"[{nameOfTheEntityClassFirstLower}].[{property.IdentifierText}] AS [{nameOfTheEntityClass}{property.IdentifierText}]");
                }
            }

            return result;
        }

        private static List<string> GetManyToOneFirstLevelSqlJoins(ClassDeclarationSyntax entityClass, IList<ClassDeclarationSyntax> entityClasses)
        {
            string nameOfTheEntityClass = entityClass.Identifier.Text;
            string nameOfTheEntityClassFirstLower = entityClass.Identifier.Text.FirstCharToLower();
            SoftProperty identifierPropertyOfTheEntityClass = GetIdentifierProperty(entityClass, entityClasses);

            List<string> result = new List<string>();

            List<SoftProperty> entityProperties = Helper.GetAllPropertiesOfTheClass(entityClass, entityClasses, true);

            foreach (SoftProperty property in entityProperties)
            {
                // TODO FT: Add the logic for the different names of the association
                if (property.Type.PropTypeIsManyToOne())
                {
                    ClassDeclarationSyntax manyToOneEntity = Helper.GetClass(property.Type, entityClasses);

                    result.Add($$"""
LEFT JOIN [{{manyToOneEntity.Identifier.Text}}] AS [{{nameOfTheEntityClassFirstLower}}{{property.IdentifierText}}] ON [{{nameOfTheEntityClassFirstLower}}{{property.IdentifierText}}].[{{identifierPropertyOfTheEntityClass.IdentifierText}}] = [{{nameOfTheEntityClass}}].[{{property.IdentifierText}}Id]
""");
                }
            }

            return result;
        }

        private static string GetJoinBasedOnPropertyListType(SoftProperty entityProperty, ClassDeclarationSyntax entityClass, IList<ClassDeclarationSyntax> entityClasses)
        {
            ClassDeclarationSyntax extractedEntityClass = Helper.ExtractEntityFromList(entityProperty.Type, entityClasses);
            string nameOfTheEntityClass = entityClass.Identifier.Text;
            string nameOfTheEntityClassFirstLower = entityClass.Identifier.Text.FirstCharToLower();
            SoftProperty identifierPropertyOfTheEntityClass = GetIdentifierProperty(entityClass, entityClasses);

            //string extractedEntityIdType = GetIdType(extractedEntityClass, entityClasses);
            SoftProperty extractedEntityIdentifierProperty = GetIdentifierProperty(extractedEntityClass, entityClasses);

            string manyToManyValue = entityProperty.Attributes.Where(x => x.Name == "ManyToMany").Select(x => x.Value).SingleOrDefault();

            if (manyToManyValue == null)
            {
                return $$"""
LEFT JOIN [{{extractedEntityClass.Identifier.Text}}] AS [{{extractedEntityClass.Identifier.Text.FirstCharToLower()}}] on [{{extractedEntityClass.Identifier.Text.FirstCharToLower()}}].[{{nameOfTheEntityClass}}Id] = [{{nameOfTheEntityClassFirstLower}}].[{{identifierPropertyOfTheEntityClass.IdentifierText}}]
""";
            }
            else
            {
                return $$"""
LEFT JOIN [{{manyToManyValue}}] AS [{{manyToManyValue.FirstCharToLower()}}] on [{{manyToManyValue.FirstCharToLower()}}].[{{nameOfTheEntityClass}}Id] = [{{nameOfTheEntityClassFirstLower}}].[{{identifierPropertyOfTheEntityClass.IdentifierText}}]
LEFT JOIN [{{extractedEntityClass.Identifier.Text}}] AS [{{extractedEntityClass.Identifier.Text.FirstCharToLower()}}] on [{{extractedEntityClass.Identifier.Text.FirstCharToLower()}}].[{{extractedEntityIdentifierProperty.IdentifierText}}] = [{{manyToManyValue.FirstCharToLower()}}].[{{extractedEntityClass.Identifier.Text}}Id]
""";
            }
        }

        private static List<string> GetManyToOneDeleteQueries(ClassDeclarationSyntax entityClass, IList<ClassDeclarationSyntax> entityClasses, string parentNameOfTheEntityClass, int recursiveIteration)
        {
            if (recursiveIteration > 5000)
            {
                GetManyToOneDeleteQueries(null, null, null, int.MaxValue);
                return new List<string> { "You made cascade delete infinite loop." };
            }

            List<string> result = new List<string>();

            string nameOfTheEntityClass = entityClass.Identifier.Text;
            string nameOfTheEntityClassFirstLower = nameOfTheEntityClass.FirstCharToLower();
            SoftProperty identifierPropertyOfTheEntityClass = GetIdentifierProperty(entityClass, entityClasses);

            List<SoftClass> softEntityClasses = Helper.GetSoftEntityClasses(entityClasses);

            List<SoftProperty> manyToOneRequiredProperties = Helper.GetManyToOneRequiredProperties(nameOfTheEntityClass, softEntityClasses);

            foreach (SoftProperty prop in manyToOneRequiredProperties)
            {
                ClassDeclarationSyntax nestedEntityClass = Helper.GetClass(prop.ClassIdentifierText, entityClasses);
                string nestedEntityClassName = nestedEntityClass.Identifier.Text;
                string nestedEntityClassNameLowerCase = nestedEntityClassName.FirstCharToLower();
                SoftProperty nestedEntityClassIdentifierProperty = GetIdentifierProperty(nestedEntityClass, entityClasses);

                if (recursiveIteration == 0)
                {
                    result.Add($$"""
                List<{{nestedEntityClassIdentifierProperty.Type}}> {{nameOfTheEntityClassFirstLower}}{{nestedEntityClassName}}ListToDelete = Get{{nestedEntityClassName}}ListFor{{nameOfTheEntityClass}}(id).Select(x => x.{{identifierPropertyOfTheEntityClass.IdentifierText}}).ToList();
""");
                }
                else
                {
                    SoftProperty parentEntityClassIdentifierProperty = GetIdentifierProperty(parentNameOfTheEntityClass, entityClasses);

                    result.Add($$"""
                List<{{nestedEntityClassIdentifierProperty.Type}}> {{nameOfTheEntityClassFirstLower}}{{nestedEntityClassName}}ListToDelete = Get{{nestedEntityClassName}}List().Where(x => {{parentNameOfTheEntityClass.FirstCharToLower()}}{{nameOfTheEntityClass}}ListToDelete.Contains(x.{{prop.IdentifierText}}.{{nestedEntityClassIdentifierProperty.IdentifierText}})).Select(x => x.{{parentEntityClassIdentifierProperty.IdentifierText}}).ToList();
""");
                }

                result.AddRange(GetManyToOneDeleteQueries(nestedEntityClass, entityClasses, nameOfTheEntityClass, recursiveIteration + 1));

                result.Add($$"""
                DeleteEntities<{{nestedEntityClassName}}, {{nestedEntityClassIdentifierProperty.Type}}>({{nameOfTheEntityClassFirstLower}}{{nestedEntityClassName}}ListToDelete);
""");
            }

            return result;
        }

        private static string GetReaderParseMethodForType(string type)
        {
            if (type == "byte" || type == "byte?")
                return "GetInt16";
            else if (type == "int" || type == "int?")
                return "GetInt32";
            else if (type == "long" || type == "long?")
                return "GetInt64";
            else if (type == "string")
                return "GetString";
            else if (type == "bool" || type == "bool?")
                return "GetBoolean";

            return "NotSupportedType";
        }

        public static string GetIdType(ClassDeclarationSyntax entityClass, IList<ClassDeclarationSyntax> entityClasses)
        {
            List<SoftProperty> entityProperties = Helper.GetAllPropertiesOfTheClass(entityClass, entityClasses, true);

            string idType = entityProperties.Where(x => x.IdentifierText == "Id").Select(x => x.Type).SingleOrDefault();

            if (idType == null)
                return "YouNeedToSpecifyIdInsideEntity";

            return idType;
        }

        public static SoftProperty GetIdentifierProperty(ClassDeclarationSyntax entityClass, IList<ClassDeclarationSyntax> entityClasses)
        {
            List<SoftProperty> entityProperties = Helper.GetAllPropertiesOfTheClass(entityClass, entityClasses, true);

            SoftProperty idProperty = entityProperties.Where(x => x.Attributes.Any(x => x.Name == "Identifier")).SingleOrDefault();

            if (idProperty == null)
            {
                return new SoftProperty
                {
                    IdentifierText = "YouNeedToSpecifyIdentifierInsideEntity",
                    Type = "NoType"
                };
            }

            return idProperty;
        }

        public static SoftProperty GetIdentifierProperty(string nameOfTheEntityClass, IList<ClassDeclarationSyntax> entityClasses)
        {
            ClassDeclarationSyntax entityClass = Helper.GetClass(nameOfTheEntityClass, entityClasses);
            List<SoftProperty> entityProperties = Helper.GetAllPropertiesOfTheClass(entityClass, entityClasses, true);

            SoftProperty idProperty = entityProperties.Where(x => x.Attributes.Any(x => x.Name == "Identifier")).SingleOrDefault();

            if (idProperty == null)
            {
                return new SoftProperty
                {
                    IdentifierText = "YouNeedToSpecifyIdentifierInsideEntity",
                    Type = "NoType"
                };
            }

            return idProperty;
        }
    }
}