using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Soft.SourceGenerator.NgTable.Helpers;
using Soft.SourceGenerators.Helpers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Soft.SourceGenerator.NgTable.NgTable
{
    [Generator]
    public class TableFilterQueryGenerator : IIncrementalGenerator
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
                    predicate: static (s, _) => Helper.IsSyntaxTargetForGenerationDTODataMappersAndEntities(s),
                    transform: static (ctx, _) => Helper.GetSemanticTargetForGenerationDTODataMappersAndEntities(ctx))
                .Where(static c => c is not null);

            context.RegisterImplementationSourceOutput(classDeclarations.Collect(),
                static (spc, source) => Execute(source, spc));
        }

        private static void Execute(ImmutableArray<ClassDeclarationSyntax> classes, SourceProductionContext context)
        {
            if (classes.Count() == 0) return;
            IList<ClassDeclarationSyntax> entityFrameworkClasses = Helper.GetEntityClasses(classes);

            StringBuilder sb = new StringBuilder();
            List<string> usings = new List<string>();
            StringBuilder sbUsings = new StringBuilder();

            sb.AppendLine($$"""
using Soft.NgTable.Models;
using LinqKit;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Soft.SourceGenerator.NgTable
{
    public static class TableFilterQueryable
    {
""");
            foreach (ClassDeclarationSyntax entityClass in entityFrameworkClasses)
            {
                if (entityClass.BaseList?.Types == null) // FT: maybe don't need to do this because i don't make ManyToMany files anymore
                {
                    continue;
                }
                usings.Add(entityClass.
                    Ancestors()
                   .OfType<NamespaceDeclarationSyntax>()
                   .Select(ns => ns.Name.ToString())
                   .FirstOrDefault());

                sb.AppendLine($$"""
        public static async Task<BasePaginationResult<{{entityClass.Identifier.Text}}>> Build(IQueryable<{{entityClass.Identifier.Text}}> query, TableFilterDTO tableFilterPayload)
        {
            Expression<Func<{{entityClass.Identifier.Text}}, bool>> predicate = PredicateBuilder.New<{{entityClass.Identifier.Text}}>(true);

            foreach (KeyValuePair<string, List<TableFilterContext>> item in tableFilterPayload.Filters)
            {
                foreach (TableFilterContext filter in item.Value)
                {
                    if (filter.Value != null)
                    {
                        Expression<Func<{{entityClass.Identifier.Text}}, bool>> condition;

                        switch (item.Key)
                        {
""");
                // FT: idem po svim DTO propertijima, ako naletim na neki koji ne postoji u ef klasi, trazim resenje u maperima, ako ne postoji upisujem odgovarajucu gresku
                List<ClassDeclarationSyntax> pairDTOClasses = classes.Where(x => x.Identifier.Text == $"{entityClass.Identifier.Text}DTO").DistinctBy(x => x.Identifier.Text).ToList(); // FT: Getting the pair DTO classes of entity class
                List<Prop> efClassProps = Helper.GetAllPropertiesOfTheClass(entityClass, classes);
                foreach (ClassDeclarationSyntax pairDTOClass in pairDTOClasses)
                {
                    foreach (Prop DTOprop in Helper.GetAllPropertiesOfTheClass(pairDTOClass, classes))
                    {
                        string entityDotNotation = DTOprop.IdentifierText; // RoleDisplayName
                        string propType = DTOprop.Type;

                        if (efClassProps.Where(x => x.IdentifierText == DTOprop.IdentifierText).Any() == false) // FT: ako property u DTO ne postoji u ef klasi (RoleDisplayName ne postoji)
                        {
                            entityDotNotation = GetDotNotatioOfEntityFromMappers(classes, pairDTOClass, entityDotNotation); // "Role.Id"
                            if (entityDotNotation == null)
                                continue;

                            propType = GetPropTypeOfEntityDotNotationProperty(entityDotNotation, entityClass, classes);
                        }

                        switch (propType)
                        {
                            case "string":
                                sb.AppendLine(GetCaseForString(DTOprop.IdentifierText, entityDotNotation));
                                break;
                            case "bool":
                            case "bool?":
                                sb.AppendLine(GetCaseForBool(DTOprop.IdentifierText, entityDotNotation));
                                break;
                            case "DateTime":
                            case "DateTime?":
                                sb.AppendLine(GetCaseForDateTime(DTOprop.IdentifierText, entityDotNotation));
                                break;
                            case "long":
                            case "long?":
                            case "int":
                            case "int?":
                            case "decimal":
                            case "decimal?":
                            case "float":
                            case "float?":
                            case "double":
                            case "double?":
                            case "byte":
                            case "byte?":
                                sb.AppendLine(GetCaseForNumber(DTOprop.IdentifierText, entityDotNotation, propType));
                                break;
                            default:
                                //sb.AppendLine(GetCaseForManyToOneFromMapping(prop, c, classes)); // FT: it's already done in other cases
                                break;
                        }

                    }
                }
                sb.AppendLine($$"""
                            default:
                                break;
                        }
                    }
                }
            }
            query = query.Where(predicate).OrderBy(x => x.Id);
            return new BasePaginationResult<{{entityClass.Identifier.Text}}>()
            {
                TotalRecords = await query.CountAsync(),
                Query = query
            };
        }
""");
            }
            sb.AppendLine($$"""
    }
}
""");
            foreach (string item in usings.Distinct())
            {
                sbUsings.AppendLine($$"""
using {{item}};
""");
            }

            sbUsings.AppendLine(sb.ToString());
            context.AddSource("TableFilterQueryable.generated", SourceText.From(sbUsings.ToString(), Encoding.UTF8));
        }



        private static string GetCaseForString(string DTOIdentifier, string entityDotNotation)
        {
            return $$"""
                        case "{{DTOIdentifier.FirstCharToLower()}}":
                            switch (filter.MatchMode)
                            {
                                case "startsWith":
                                    condition = x => x.{{entityDotNotation}}.StartsWith(filter.Value.ToString());
                                    break;
                
                                case "contains":
                                    condition = x => x.{{entityDotNotation}}.Contains(filter.Value.ToString());
                                    break;
                
                                case "equals":
                                    condition = x => x.{{entityDotNotation}}.Equals(filter.Value.ToString());
                                    break;
                
                                default:
                                    throw new ArgumentException("Invalid Match mode!");
                            }
                            predicate = predicate.And(condition);
                            break;
""";
        }

        private static string GetCaseForBool(string DTOIdentifier, string entityDotNotation)
        {
            return $$"""
                        case "{{DTOIdentifier.FirstCharToLower()}}":
                            switch (filter.MatchMode)
                            {
                                case "equals":
                                    condition = x => x.{{entityDotNotation}}.Equals(Convert.ToBoolean(filter.Value.ToString()));
                                    break;
                
                                default:
                                    throw new ArgumentException("Invalid Match mode!");
                            }
                            predicate = predicate.And(condition);
                            break;
""";
        }

        private static string GetCaseForDateTime(string DTOIdentifier, string entityDotNotation)
        {
            return $$"""
                        case "{{DTOIdentifier.FirstCharToLower()}}":
                            switch (filter.MatchMode)
                            {
                                case "dateBefore":
                                    condition = x => x.{{entityDotNotation}} <= Convert.ToDateTime(filter.Value.ToString());
                                    break;
                
                                case "dateAfter":
                                    condition = x => x.{{entityDotNotation}} >= Convert.ToDateTime(filter.Value.ToString());
                                    break;
                
                                default:
                                    throw new ArgumentException("Invalid Match mode!");
                            }
                            predicate = predicate.And(condition);
                            break;
""";
        }

        private static string GetCaseForNumber(string DTOIdentifier, string entityDotNotation, string numberType)
        {
            string numberTypeWithoutQuestion = numberType.Replace("?", "");

            return $$"""
                        case "{{DTOIdentifier.FirstCharToLower()}}":
                            switch (filter.MatchMode)
                            {
                                case "equals":
                                    condition = x => x.{{entityDotNotation}} == {{numberTypeWithoutQuestion}}.Parse(filter.Value.ToString());
                                    break;

                                case "lte":
                                    condition = x => x.{{entityDotNotation}} <= {{numberTypeWithoutQuestion}}.Parse(filter.Value.ToString());
                                    break;

                                case "gte":
                                    condition = x => x.{{entityDotNotation}} <= {{numberTypeWithoutQuestion}}.Parse(filter.Value.ToString());
                                    break;
                
                                default:
                                    throw new ArgumentException("Invalid Match mode!");
                            }
                            predicate = predicate.And(condition);
                            break;
""";
        }

        /// <summary>
        /// </summary>
        /// <param name="DTOClass">UserDTO</param>
        /// <param name="DTOClassProp">RoleDisplayName</param>
        /// <returns>Role.Id</returns>
        private static string GetDotNotatioOfEntityFromMappers(IList<ClassDeclarationSyntax> allClasses, ClassDeclarationSyntax DTOClass, string DTOClassProp)
        {
            List<ClassDeclarationSyntax> bothMapperClasses = Helper.GetMapperClasses(allClasses); // Generated and manualy written

            foreach (ClassDeclarationSyntax mapperClass in bothMapperClasses)
            {
                MethodDeclarationSyntax mapMethod = mapperClass?.Members.OfType<MethodDeclarationSyntax>()
                    .Where(x => x.ReturnType.ToString() == DTOClass.Identifier.Text && x.Identifier.ToString() == "Map") // TODO FT: put this into appsettings
                    .SingleOrDefault(); // It's single because if we added it manualy we don't generate it

                if (mapMethod != null)
                    return GetFirstAttributeParamFromMapper(mapMethod, DTOClassProp); // "Role.Id"
            }

            return null;
        }

        /// <summary>
        /// </summary>
        /// <param name="mapMethod">public static partial UserDTO Map(User poco);</param>
        /// <param name="DTOClassProp">RoleDisplayName</param>
        /// <returns></returns>
        public static string GetFirstAttributeParamFromMapper(MethodDeclarationSyntax mapMethod, string DTOClassProp)
        {
            foreach (var attributeList in mapMethod.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    SeparatedSyntaxList<AttributeArgumentSyntax> arguments = attribute.ArgumentList.Arguments; // "Role.Id", "RoleDisplayName"
                    if (arguments.Count > 1) // Doing this because of MapperIgnoreTarget
                    {
                        if (arguments[1].ToString().Split('.').First().Trim('"') == DTOClassProp)
                        {
                            return arguments[0].ToString().Trim('"');
                        }
                    }
                }
            }

            return null;
        }

        public static string GetPropTypeOfEntityDotNotationProperty(string entityDotNotation, ClassDeclarationSyntax entityClass, IList<ClassDeclarationSyntax> allClasses)
        {
            // User
            // Role.Permission.Id
            // Role.Id
            string propName = entityDotNotation.Split('.')[0]; // Role
            List<Prop> entityClassProperties = Helper.GetAllPropertiesOfTheClass(entityClass, allClasses);
            Prop prop = entityClassProperties.Where(x => x.IdentifierText == propName).Single(); // Role

            int i = 1;
            while (prop.Type.IsBaseType() == false)
            {
                ClassDeclarationSyntax helperClass = allClasses.Where(x => x.Identifier.Text == propName).SingleOrDefault(); // Role
                if (helperClass == null)
                    break;
                List<Prop> helperProps = Helper.GetAllPropertiesOfTheClass(helperClass, allClasses);
                propName = entityDotNotation.Split('.')[i]; // Id
                prop = helperProps.Where(x => x.IdentifierText == propName).Single(); // Id
                i++;
            }

            return prop.Type;
        }

    }
}
