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
#if DEBUG
            if (!Debugger.IsAttached)
            {
                Debugger.Launch();
            }
#endif
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                //string baseType = entityClass.GetBaseType();

                //if (baseType == null) // FT: Handling many to many, maybe you should do something else in the future
                //    continue;

                if (Helper.GetAllAttributesOfTheClass(entityClass, entityClasses).Any(x => x.Name == "ManyToMany"))
                    continue;

                string nameOfTheEntityClass = entityClass.Identifier.Text;
                string nameOfTheEntityClassFirstLower = entityClass.Identifier.Text.FirstCharToLower();
                string idTypeOfTheEntityClass = GetIdType(entityClass, entityClasses);
                string displayNameProperty = Helper.GetDisplayNamePropForClass(entityClass, entityClasses);

                List<SoftProperty> entityProperties = Helper.GetAllPropertiesOfTheClass(entityClass, entityClasses, true);

                sb.AppendLine($$"""
        public {{nameOfTheEntityClass}} Get{{nameOfTheEntityClass}}({{idTypeOfTheEntityClass}} id)
        {
            List<{{nameOfTheEntityClass}}> {{nameOfTheEntityClassFirstLower}}List = new List<{{nameOfTheEntityClass}}>();
            Dictionary<{{idTypeOfTheEntityClass}}, {{nameOfTheEntityClass}}> {{nameOfTheEntityClassFirstLower}}Dict = new Dictionary<{{idTypeOfTheEntityClass}}, {{nameOfTheEntityClass}}>();
            {{string.Join("\n\t\t\t", GetAllDicts(entityClass, entityClasses, new HashSet<string>(), null))}}

            string query = @$"
SELECT 
{{string.Join(", ", GetAllSelectProperties(entityClass, entityClasses, new HashSet<string>(), null))}}
FROM {{nameOfTheEntityClass}} AS {{nameOfTheEntityClassFirstLower}}
{{string.Join(", ", GetAllJoins(entityClass, entityClasses, new HashSet<string>(), null))}}
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
{{FillData(entityClass, entityClasses, new HashSet<string>(), null)}}
                        }
                    }
                }
            });

            {{nameOfTheEntityClass}} {{nameOfTheEntityClassFirstLower}} = {{nameOfTheEntityClassFirstLower}}List.SingleOrDefault();

            if ({{nameOfTheEntityClassFirstLower}} == null)
                throw new Exception("Objekat ne postoji u bazi podataka.");

            return {{nameOfTheEntityClassFirstLower}};
        }
""");

            }

            context.AddSource($"{projectName}BusinessService.generated", SourceText.From(sb.ToString(), Encoding.UTF8));
        }

        private static string FillData(ClassDeclarationSyntax entityClass, IList<ClassDeclarationSyntax> entityClasses, HashSet<string> processedClasses, string parentEntityClassName)
        {
            string nameOfTheEntityClass = entityClass.Identifier.Text;

            if (processedClasses.Contains(nameOfTheEntityClass))
                return null;

            processedClasses.Add(nameOfTheEntityClass);

            string nameOfTheEntityClassFirstLower = entityClass.Identifier.Text.FirstCharToLower();
            string idTypeOfTheEntityClass = GetIdType(entityClass, entityClasses);
            List<SoftProperty> entityPropertiesForTheJoin = Helper.GetAllPropertiesOfTheClass(entityClass, entityClasses, true).Where(x => x.Type.IsEnumerable() || x.Type.PropTypeIsManyToOne()).ToList();

            StringBuilder sb = new StringBuilder();

            sb.Append($$"""
                            if (reader.IsDBNull(reader.GetOrdinal("{{nameOfTheEntityClass}}Id")))
                            {
                                {{idTypeOfTheEntityClass}} {{nameOfTheEntityClassFirstLower}}Id = reader.{{GetReaderParseMethodForType(idTypeOfTheEntityClass)}}(reader.GetOrdinal("{{nameOfTheEntityClass}}Id"));
                                bool {{nameOfTheEntityClassFirstLower}}AlreadyAdded = {{nameOfTheEntityClassFirstLower}}Dict.TryGetValue({{nameOfTheEntityClassFirstLower}}Id, out {{nameOfTheEntityClass}} {{nameOfTheEntityClassFirstLower}});
                                if (!{{nameOfTheEntityClassFirstLower}}AlreadyAdded)
                                {
                                    {{nameOfTheEntityClassFirstLower}} = new {{nameOfTheEntityClass}}
                                    {
                                        {{string.Join(",\n\t\t\t\t\t\t\t\t\t\t", GetClassInitialization(entityClass, entityClasses))}}
                                    };

{{(parentEntityClassName != null ? $$"""
                                    {{parentEntityClassName.FirstCharToLower()}}{{nameOfTheEntityClass}}Dict[{{nameOfTheEntityClassFirstLower}}Id] = {{nameOfTheEntityClassFirstLower}};

""" : $$"""
                                    {{nameOfTheEntityClassFirstLower}}Dict[{{nameOfTheEntityClassFirstLower}}Id] = {{nameOfTheEntityClassFirstLower}};

""")}}

""");

            foreach (SoftProperty entityPropertyForTheJoin in entityPropertiesForTheJoin)
            {
                // TODO FT: Add the logic for the different names of the association
                if (entityPropertyForTheJoin.Type.IsEnumerable())
                {
                    ClassDeclarationSyntax extractedEnumerableEntity = Helper.ExtractEntityFromList(entityPropertyForTheJoin.Type, entityClasses);
                    string helper = FillData(extractedEnumerableEntity, entityClasses, processedClasses, nameOfTheEntityClass);

                    if (helper != null)
                    {
                        sb.Append(helper);
                        sb.Append($$"""
                                    {{nameOfTheEntityClassFirstLower}}.{{entityPropertyForTheJoin.IdentifierText}}.Add({{entityPropertyForTheJoin.IdentifierText}});

""");
                    }
                }
                if (entityPropertyForTheJoin.Type.PropTypeIsManyToOne())
                {
                    ClassDeclarationSyntax manyToOneEntity = Helper.GetClass(entityPropertyForTheJoin.Type, entityClasses);
                    string helper = FillData(manyToOneEntity, entityClasses, processedClasses, nameOfTheEntityClass);

                    if (helper != null)
                    {
                        sb.Append(helper);
                        sb.Append($$"""
                                    {{nameOfTheEntityClassFirstLower}}.{{entityPropertyForTheJoin.IdentifierText}} = {{entityPropertyForTheJoin.IdentifierText}};

""");
                    }
                }
            }

            sb.Append($$"""
                                    {{(parentEntityClassName == null ? $"{nameOfTheEntityClassFirstLower}List.Add({nameOfTheEntityClassFirstLower});" : "\n")}}
                                }
                            }

""");

            return sb.ToString();
        }

        private static List<string> GetClassInitialization(ClassDeclarationSyntax entityClass, IList<ClassDeclarationSyntax> entityClasses)
        {
            List<string> result = new List<string>();

            string nameOfTheEntityClass = entityClass.Identifier.Text;
            string nameOfTheEntityClassFirstLower = entityClass.Identifier.Text.FirstCharToLower();

            List<SoftProperty> entityProperties = Helper.GetAllPropertiesOfTheClass(entityClass, entityClasses, true);

            foreach (SoftProperty entityProperty in entityProperties)
            {
                if (entityProperty.IdentifierText == "Id")
                {
                    result.Add($"{entityProperty.IdentifierText} = {nameOfTheEntityClassFirstLower}Id");
                }
                else if (entityProperty.Type.IsEnumerable() || entityProperty.Type.PropTypeIsManyToOne())
                {
                    result.Add($"{entityProperty.IdentifierText} = new {entityProperty.Type}()");
                }
                else
                {
                    result.Add($"{entityProperty.IdentifierText} = reader.{GetReaderParseMethodForType(entityProperty.Type)}(reader.GetOrdinal(\"{nameOfTheEntityClass}{entityProperty.IdentifierText}\"))");
                }
            }

            return result;
        }

        private static List<string> GetAllSelectProperties(ClassDeclarationSyntax entityClass, IList<ClassDeclarationSyntax> entityClasses, HashSet<string> processedClasses, string parentEntityClassName)
        {
            string nameOfTheEntityClass = entityClass.Identifier.Text;

            if (processedClasses.Contains(nameOfTheEntityClass))
                return new List<string>();

            processedClasses.Add(nameOfTheEntityClass);

            string nameOfTheEntityClassFirstLower = entityClass.Identifier.Text.FirstCharToLower();

            List<string> result = new List<string>();

            List<SoftProperty> entityProperties = Helper.GetAllPropertiesOfTheClass(entityClass, entityClasses, true);

            foreach (SoftProperty property in entityProperties)
            {
                // TODO FT: Add the logic for the different names of the association
                if (property.Type.IsEnumerable())
                {
                    ClassDeclarationSyntax extractedEnumerableEntity = Helper.ExtractEntityFromList(property.Type, entityClasses);
                    result.AddRange(GetAllSelectProperties(extractedEnumerableEntity, entityClasses, processedClasses, nameOfTheEntityClass));
                }
                else if (property.Type.PropTypeIsManyToOne())
                {
                    ClassDeclarationSyntax manyToOneEntity = Helper.GetClass(property.Type, entityClasses);
                    result.AddRange(GetAllSelectProperties(manyToOneEntity, entityClasses, processedClasses, nameOfTheEntityClass));
                }
                else
                {
                    if (parentEntityClassName == null)
                    {
                        result.Add($"{nameOfTheEntityClassFirstLower}.{property.IdentifierText} AS {nameOfTheEntityClass}{property.IdentifierText}");
                    }
                    else
                    {
                        result.Add($"{parentEntityClassName.FirstCharToLower()}{nameOfTheEntityClass}.{property.IdentifierText} AS {parentEntityClassName}{nameOfTheEntityClass}{property.IdentifierText}");
                    }
                }
            }

            return result;
        }

        private static List<string> GetAllJoins(ClassDeclarationSyntax entityClass, IList<ClassDeclarationSyntax> entityClasses, HashSet<string> processedClasses, string parentEntityClassName)
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
                    result.AddRange(GetAllJoins(extractedEnumerableEntity, entityClasses, processedClasses, nameOfTheEntityClass));

                    string manyToManyName = property.Attributes.Where(x => x.Name == "ManyToMany").Select(x => x.Value).SingleOrDefault();
                    if (manyToManyName == null)
                    {
                        if (parentEntityClassName == null)
                        {
                        result.Add($$"""
RIGHT JOIN {{extractedEnumerableEntity.Identifier.Text}} AS {{nameOfTheEntityClassFirstLower}}{{extractedEnumerableEntity.Identifier.Text}} ON {{nameOfTheEntityClassFirstLower}}{{extractedEnumerableEntity.Identifier.Text}}.{{nameOfTheEntityClass}}Id = {{nameOfTheEntityClass}}.Id

""");
                        }
                        else
                        {
                            result.Add($$"""
RIGHT JOIN {{extractedEnumerableEntity.Identifier.Text}} AS {{nameOfTheEntityClassFirstLower}}{{extractedEnumerableEntity.Identifier.Text}} ON {{nameOfTheEntityClassFirstLower}}{{extractedEnumerableEntity.Identifier.Text}}.{{nameOfTheEntityClass}}Id = {{parentEntityClassName.FirstCharToLower()}}{{nameOfTheEntityClass}}.Id

""");
                        }
                    }
                    else
                    {
                        //                            result.Add($$"""
                        //LEFT JOIN {{manyToManyName}} AS {{manyToManyName.FirstCharToLower()}} ON {{nameOfTheEntityClassFirstLower}}.Id = {{manyToManyName.FirstCharToLower()}}.{{nameOfTheEntityClass}}Id
                        //LEFT JOIN {{extractedEnumerableEntity.Identifier.Text}} AS {{extractedEnumerableEntity.Identifier.Text.FirstCharToLower()}} ON {{extractedEnumerableEntity.Identifier.Text.FirstCharToLower()}}.Id = {{manyToManyName.FirstCharToLower()}}.{{extractedEnumerableEntity.Identifier.Text}}Id
                        //""");
                    }
                }
                if (property.Type.PropTypeIsManyToOne())
                {
                    ClassDeclarationSyntax manyToOneEntity = Helper.GetClass(property.Type, entityClasses);
                    result.AddRange(GetAllJoins(manyToOneEntity, entityClasses, processedClasses, nameOfTheEntityClass));

                    if (parentEntityClassName == null)
                    {
                        result.Add($$"""
RIGHT JOIN {{manyToOneEntity.Identifier.Text}} AS {{nameOfTheEntityClassFirstLower}}{{manyToOneEntity.Identifier.Text}} ON {{nameOfTheEntityClassFirstLower}}{{manyToOneEntity.Identifier.Text}}.Id = {{nameOfTheEntityClass}}.{{manyToOneEntity.Identifier.Text}}Id

""");
                    }
                    else
                    {
                    result.Add($$"""
RIGHT JOIN {{manyToOneEntity.Identifier.Text}} AS {{nameOfTheEntityClassFirstLower}}{{manyToOneEntity.Identifier.Text}} ON {{nameOfTheEntityClassFirstLower}}{{manyToOneEntity.Identifier.Text}}.Id = {{parentEntityClassName.FirstCharToLower()}}{{nameOfTheEntityClass}}.{{manyToOneEntity.Identifier.Text}}Id

""");
                    }
                }
            }

            return result;
        }

        private static List<string> GetAllDicts(ClassDeclarationSyntax entityClass, IList<ClassDeclarationSyntax> entityClasses, HashSet<string> processedClasses, string parentEntityClassName)
        {
            string nameOfTheEntityClass = entityClass.Identifier.Text;
            string nameOfTheEntityClassFirstLower = nameOfTheEntityClass.FirstCharToLower();

            if (processedClasses.Contains(nameOfTheEntityClass))
                return new List<string>();

            processedClasses.Add(nameOfTheEntityClass);

            List<string> result = new List<string>();

            List<SoftProperty> entityProperties = Helper.GetAllPropertiesOfTheClass(entityClass, entityClasses, true);

            foreach (SoftProperty property in entityProperties)
            {
                if (property.Type.IsEnumerable())
                {
                    ClassDeclarationSyntax extractedEnumerableEntity = Helper.ExtractEntityFromList(property.Type, entityClasses);
                    result.AddRange(GetAllDicts(extractedEnumerableEntity, entityClasses, processedClasses, nameOfTheEntityClass));
                    string idType = GetIdType(extractedEnumerableEntity, entityClasses);

                    //if (parentEntityClassName != null)
                    //{
                    result.Add($$"""
Dictionary<{{idType}}, {{extractedEnumerableEntity.Identifier.Text}}> {{nameOfTheEntityClassFirstLower}}{{extractedEnumerableEntity.Identifier.Text}}Dict = new Dictionary<{{idType}}, {{extractedEnumerableEntity.Identifier.Text}}>();
""");
                    //}
                }
                if (property.Type.PropTypeIsManyToOne())
                {
                    ClassDeclarationSyntax manyToOneEntity = Helper.GetClass(property.Type, entityClasses);
                    result.AddRange(GetAllDicts(manyToOneEntity, entityClasses, processedClasses, nameOfTheEntityClass));
                    string idType = GetIdType(manyToOneEntity, entityClasses);

                    //if (parentEntityClassName != null)
                    //{
                    result.Add($$"""
Dictionary<{{idType}}, {{manyToOneEntity.Identifier.Text}}> {{nameOfTheEntityClassFirstLower}}{{manyToOneEntity.Identifier.Text}}Dict = new Dictionary<{{idType}}, {{manyToOneEntity.Identifier.Text}}>();
""");
                    //}
                }
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
    }
}