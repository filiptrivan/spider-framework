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

            List<SpiderClass> softClasses = Helpers.GetSpiderClasses(classes, referencedProjectClasses);
            List<SpiderClass> allClasses = softClasses.Concat(referencedProjectClasses).ToList();

            List<SpiderClass> currentProjectDTOClasses = Helpers.GetDTOClasses(softClasses, allClasses);
            List<SpiderClass> currentProjectEntities = softClasses.Where(x => x.Namespace.EndsWith(".Entities")).ToList();
            List<SpiderClass> allEntityClasses = allClasses.Where(x => x.Namespace.EndsWith(".Entities")).ToList();

            StringBuilder sb = new StringBuilder();
            List<string> usings = new List<string>();
            StringBuilder sbUsings = new StringBuilder();

            string[] namespacePartsWithoutLastElement = Helpers.GetNamespacePartsWithoutLastElement(currentProjectEntities[0].Namespace);

            string basePartOfNamespace = string.Join(".", namespacePartsWithoutLastElement); // eg. Spider.Security
            string projectName = namespacePartsWithoutLastElement[namespacePartsWithoutLastElement.Length - 1]; // eg. Security

            sb.AppendLine($$"""
using LinqKit;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Spider.Shared.DTO;
using System.Text.Json;
using {{basePartOfNamespace}}.Entities;

namespace {{basePartOfNamespace}}.TableFiltering
{
    public static class TableFilterQueryable
    {
""");
            foreach (SpiderClass entity in currentProjectEntities)
            {
                string baseType = entity.BaseType;

                if (baseType == null)
                    continue;

                sb.AppendLine($$"""
        public static async Task<PaginationResult<{{entity.Name}}>> Build(IQueryable<{{entity.Name}}> query, TableFilterDTO tableFilterPayload)
        {
            Expression<Func<{{entity.Name}}, bool>> predicate = PredicateBuilder.New<{{entity.Name}}>(true);

            foreach (KeyValuePair<string, List<TableFilterContext>> item in tableFilterPayload.Filters)
            {
                foreach (TableFilterContext filter in item.Value)
                {
                    if (filter.Value != null)
                    {
                        Expression<Func<{{entity.Name}}, bool>> condition;

                        switch (item.Key)
                        {
""");
                // FT: idem po svim DTO propertijima, ako naletim na neki koji ne postoji u ef klasi, trazim resenje u maperima, ako ne postoji upisujem odgovarajucu gresku
                List<SpiderClass> pairDTOClasses = currentProjectDTOClasses.Where(x => x.Name == $"{entity.Name}DTO").ToList(); // FT: Getting the pair DTO classes of entity class
                List<SpiderProperty> efClassProps = entity.Properties;

                foreach (SpiderClass pairDTOClass in pairDTOClasses)
                {
                    foreach (SpiderProperty DTOprop in pairDTOClass.Properties)
                    {
                        string entityDotNotation = DTOprop.Name; // RoleDisplayName
                        string DTOpropType = DTOprop.Type;

                        if (efClassProps.Where(x => x.Name == DTOprop.Name).Any() == false) // FT: ako property u DTO ne postoji u ef klasi (RoleDisplayName ne postoji)
                        {
                            if (entityDotNotation.EndsWith("CommaSeparated") && pairDTOClass.IsGenerated == true)
                            {
                                string entityPropName = entityDotNotation.Replace("CommaSeparated", ""); // "SegmentationItems"

                                sb.AppendLine(GetCaseForEnumerable(DTOprop.Name, entityPropName, entity.GetIdType(currentProjectEntities)));

                                continue;
                            }
                            else
                            {
                                entityDotNotation = GetDotNotatioOfEntityFromMappers(allEntityClasses, entity, pairDTOClass, entityDotNotation); // "Role.Id"

                                if (entityDotNotation == null)
                                    continue;

                                DTOpropType = GetPropTypeOfEntityDotNotationProperty(entityDotNotation, entity, allEntityClasses);
                            }
                        }

                        switch (DTOpropType)
                        {
                            case "string":
                                sb.AppendLine(GetCaseForString(DTOprop.Name, entityDotNotation));
                                break;
                            case "bool":
                            case "bool?":
                                sb.AppendLine(GetCaseForBool(DTOprop.Name, entityDotNotation));
                                break;
                            case "DateTime":
                            case "DateTime?":
                                sb.AppendLine(GetCaseForDateTime(DTOprop.Name, entityDotNotation));
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
                                sb.AppendLine(GetCaseForNumber(DTOprop.Name, entityDotNotation, DTOpropType));
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

            query = query.Where(predicate);

            return new PaginationResult<{{entity.Name}}>()
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

                                case "in":
                                    {{numberType}}[] values = JsonSerializer.Deserialize<{{numberType}}[]>(filter.Value.ToString());
                                    condition = x => values.Contains(x.{{entityDotNotation}});
                                    break;
                
                                default:
                                    throw new ArgumentException("Invalid Match mode!");
                            }
                            predicate = predicate.And(condition);
                            break;
""";
        }

        private static string GetCaseForEnumerable(string DTOIdentifier, string entityDotNotation, string idType)
        {
            return $$"""
                        case "{{DTOIdentifier.FirstCharToLower()}}":
                            switch (filter.MatchMode)
                            {
                                case "in":
                                    {{idType}}[] values = JsonSerializer.Deserialize<{{idType}}[]>(filter.Value.ToString());
                                    condition = x => x.{{entityDotNotation}}.Any(x => values.Contains(x.Id));
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
        private static string GetDotNotatioOfEntityFromMappers(List<SpiderClass> allClasses, SpiderClass entity, SpiderClass DTOClass, string DTOClassProp)
        {
            if (DTOClassProp.EndsWith("DisplayName") && DTOClass.IsGenerated == true) // FT: Doing this thing with the IsGenerated so we can make prop in non generated DTO with "DisplayName" or "Id" sufix 
            {
                string baseClassInDotNotation = DTOClassProp.Replace("DisplayName", ""); // "Rolinho"
                SpiderProperty propertyInEntityClass = entity.Properties.Where(x => x.Name == baseClassInDotNotation).Single();
                string typeOfThePropertyInEntityClass = propertyInEntityClass.Type; // "Role"
                SpiderClass entityClassWhichWeAreSearchingDisplayNameFor = allClasses.Where(x => x.Name == typeOfThePropertyInEntityClass).Single();
                string displayName = Helpers.GetDisplayNameProperty(entityClassWhichWeAreSearchingDisplayNameFor); // Name
                displayName = displayName.Replace(".ToString()", "");
                return $"{baseClassInDotNotation}.{displayName}"; // FT: It's okay to do it like this, because when we generating DisplayNames for DTO, we are doing it just for the first level.
            }
            if (DTOClassProp.EndsWith("Id") && DTOClassProp.Length > 2 && DTOClass.IsGenerated == true)
            {
                string baseClassInDotNotation = DTOClassProp.Replace("Id", ""); // "Rolinho"
                return $"{baseClassInDotNotation}.Id";
            }

            SpiderClass nonGeneratedMapperClass = allClasses.Where(x => x.Namespace.EndsWith(".DataMappers")).SingleOrDefault(); // FT: Can be null if the user still didn't made DataMappers partial class

            List<SpiderMethod> methodsOfTheNonGeneratedMapperClass = nonGeneratedMapperClass?.Methods; // FT: Classes from referenced assemblies won't have method body, but here it's not important.

            return GetEntityDotNotationForDTO(methodsOfTheNonGeneratedMapperClass, DTOClass.Name, entity.Name, DTOClassProp);

            //if (mapMethod != null)
            //    return GetFirstAttributeParamFromMapper(mapMethod, DTOClassProp); // "Role.Id"
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

        public static string GetPropTypeOfEntityDotNotationProperty(string entityDotNotation, SpiderClass entityClass, List<SpiderClass> allClasses)
        {
            // Rolinho.Permission.Id
            string propName = entityDotNotation.Split('.')[0]; // Rolinho
            List<SpiderProperty> entityClassProperties = entityClass.Properties;
            SpiderProperty prop = entityClassProperties.Where(x => x.Name == propName).Single(); // Role

            int i = 1;
            while (prop.Type.IsBaseType() == false)
            {
                SpiderClass helperClass = allClasses.Where(x => x.Name == prop.Type).Single(); // Role

                List<SpiderProperty> helperProps = helperClass.Properties;

                propName = entityDotNotation.Split('.')[i]; // Id
                prop = helperProps.Where(x => x.Name == propName).Single(); // Id
                i++;
            }

            return prop.Type;
        }

        public static string GetEntityDotNotationForDTO(List<SpiderMethod> methodsOfTheNonGeneratedMapperClass, string destinationDTOClass, string sourceEntityClass, string DTOProp)
        {
            if (methodsOfTheNonGeneratedMapperClass == null)
                return null;

            List<SpiderMethod> methodsWithTableFiltersAttribute = methodsOfTheNonGeneratedMapperClass.Where(x => x.Attributes.Any(x => x.Name == "TableFiltersListener")).ToList();

            SpiderMethod currentConfigMethod = methodsWithTableFiltersAttribute.Where(x => x.Body.Contains($".NewConfig<{sourceEntityClass}, {destinationDTOClass}>()")).SingleOrDefault();

            if (currentConfigMethod == null)
                return null;

            IEnumerable<InvocationExpressionSyntax> newConfigCalls = currentConfigMethod.DescendantNodes
                .OfType<InvocationExpressionSyntax>()
                .Where(invocation => invocation.Expression.ToString().Contains("NewConfig"));

            foreach (InvocationExpressionSyntax invocation in newConfigCalls)
            {
                List<string> linqRows = invocation.ArgumentList?.Arguments.Select(x => x.ToString()).ToList();

                if (linqRows.Where(x => x.Split('.').LastOrDefault() == DTOProp).Count() == 1) // dest.TestDisplayName -> TestDisplayName
                {
                    string src = linqRows.Where(x => x.Split('.').LastOrDefault() != DTOProp).Single(); // src => src.Gender.Name
                    List<string> parts = src.Split('.').ToList(); // src => src ; Gender ; Name
                    List<string> partsToJoin = parts.GetRange(1, parts.Count - 1).ToList(); // Gender ; Name
                    return string.Join(".", partsToJoin);
                }
            }

            return null;
        }



    }
}
