using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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

            IncrementalValueProvider<List<SoftClass>> referencedProjectEntityClasses = Helper.GetEntityClassesFromReferencedAssemblies(context);

            var allClasses = classDeclarations.Collect()
                .Combine(referencedProjectEntityClasses);

            context.RegisterImplementationSourceOutput(allClasses, static (spc, source) => Execute(source.Left, source.Right, spc));
        }

        private static void Execute(IList<ClassDeclarationSyntax> classes, List<SoftClass> referencedProjectEntityClasses, SourceProductionContext context)
        {
            if (classes.Count <= 1) return;

            List<SoftClass> entityClasses = Helper.GetSoftEntityClasses(classes);
            List<SoftClass> allEntityClasses = entityClasses.Concat(referencedProjectEntityClasses).ToList();
            List<SoftClass> DTOClasses = Helper.GetDTOClasses(Helper.GetSoftClasses(classes));

            StringBuilder sb = new StringBuilder();
            List<string> usings = new List<string>();
            StringBuilder sbUsings = new StringBuilder();

            string[] namespacePartsWithoutLastElement = Helper.GetNamespacePartsWithoutLastElement(entityClasses[0].Namespace);

            string basePartOfNamespace = string.Join(".", namespacePartsWithoutLastElement); // eg. Soft.Generator.Security
            string projectName = namespacePartsWithoutLastElement[namespacePartsWithoutLastElement.Length - 1]; // eg. Security

            sb.AppendLine($$"""
using LinqKit;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Soft.Generator.Shared.DTO;
using System.Text.Json;
using {{basePartOfNamespace}}.Entities;

namespace {{basePartOfNamespace}}.TableFiltering
{
    public static class TableFilterQueryable
    {
""");
            foreach (SoftClass entityClass in entityClasses)
            {
                string baseType = entityClass.BaseType;

                if (baseType == null)
                    continue;

                sb.AppendLine($$"""
        public static async Task<PaginationResult<{{entityClass.Name}}>> Build(IQueryable<{{entityClass.Name}}> query, TableFilterDTO tableFilterPayload)
        {
            Expression<Func<{{entityClass.Name}}, bool>> predicate = PredicateBuilder.New<{{entityClass.Name}}>(true);

            foreach (KeyValuePair<string, List<TableFilterContext>> item in tableFilterPayload.Filters)
            {
                foreach (TableFilterContext filter in item.Value)
                {
                    if (filter.Value != null)
                    {
                        Expression<Func<{{entityClass.Name}}, bool>> condition;

                        switch (item.Key)
                        {
""");
                // FT: idem po svim DTO propertijima, ako naletim na neki koji ne postoji u ef klasi, trazim resenje u maperima, ako ne postoji upisujem odgovarajucu gresku
                List<SoftClass> pairDTOClasses = DTOClasses.Where(x => x.Name == $"{entityClass.Name}DTO").ToList(); // FT: Getting the pair DTO classes of entity class
                List<SoftProperty> efClassProps = entityClass.Properties;

                foreach (SoftClass pairDTOClass in pairDTOClasses)
                {
                    foreach (SoftProperty DTOprop in pairDTOClass.Properties)
                    {
                        string entityDotNotation = DTOprop.IdentifierText; // RoleDisplayName
                        string DTOpropType = DTOprop.Type;

                        if (efClassProps.Where(x => x.IdentifierText == DTOprop.IdentifierText).Any() == false) // FT: ako property u DTO ne postoji u ef klasi (RoleDisplayName ne postoji)
                        {
                            if (entityDotNotation.EndsWith("CommaSeparated") && pairDTOClass.IsGenerated == true)
                            {
                                string entityPropName = entityDotNotation.Replace("CommaSeparated", ""); // "SegmentationItems"
                                string idType = Helper.GetGenericIdType(entityClass, entityClasses); // FT: Id type of SegmentationItem class

                                sb.AppendLine(GetCaseForEnumerable(DTOprop.IdentifierText, entityPropName, idType));

                                continue;
                            }
                            else
                            {
                                entityDotNotation = GetDotNotatioOfEntityFromMappers(allEntityClasses, entityClass, pairDTOClass, entityDotNotation); // "Role.Id"

                                if (entityDotNotation == null)
                                    continue;

                                DTOpropType = GetPropTypeOfEntityDotNotationProperty(entityDotNotation, entityClass, allEntityClasses);
                            }
                        }

                        switch (DTOpropType)
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
                                sb.AppendLine(GetCaseForNumber(DTOprop.IdentifierText, entityDotNotation, DTOpropType));
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
            return new PaginationResult<{{entityClass.Name}}>()
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
        private static string GetDotNotatioOfEntityFromMappers(List<SoftClass> allClasses, SoftClass entitySoftClass, SoftClass DTOClass, string DTOClassProp)
        {
            if (DTOClassProp.EndsWith("DisplayName") && DTOClass.IsGenerated == true) // FT: Doing this thing with the IsGenerated so we can make prop in non generated DTO with "DisplayName" or "Id" sufix 
            {
                string baseClassInDotNotation = DTOClassProp.Replace("DisplayName", ""); // "Rolinho"
                SoftProperty propertyInEntityClass = entitySoftClass.Properties.Where(x => x.IdentifierText == baseClassInDotNotation).Single();
                string typeOfThePropertyInEntityClass = propertyInEntityClass.Type; // "Role"
                SoftClass entityClassWhichWeAreSearchingDisplayNameFor = allClasses.Where(x => x.Name == typeOfThePropertyInEntityClass).Single();
                string displayName = Helper.GetDisplayNamePropForClass(entityClassWhichWeAreSearchingDisplayNameFor); // Name
                displayName = displayName.Replace(".ToString()", "");
                return $"{baseClassInDotNotation}.{displayName}"; // FT: It's okay to do it like this, because when we generating DisplayNames for DTO, we are doing it just for the first level.
            }
            if (DTOClassProp.EndsWith("Id") && DTOClassProp.Length > 2 && DTOClass.IsGenerated == true)
            {
                string baseClassInDotNotation = DTOClassProp.Replace("Id", ""); // "Rolinho"
                return $"{baseClassInDotNotation}.Id";
            }

            SoftClass nonGeneratedMapperClass = allClasses.Where(x => x.Namespace.EndsWith(".DataMappers")).SingleOrDefault(); // FT: Can be null if the user still didn't made DataMappers partial class

            List<SoftMethod> methodsOfTheNonGeneratedMapperClass = nonGeneratedMapperClass?.Methods; // FT: Classes from referenced assemblies won't have method body, but here it's not important.

            return GetEntityDotNotationForDTO(methodsOfTheNonGeneratedMapperClass, DTOClass.Name, entitySoftClass.Name, DTOClassProp);

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

        public static string GetPropTypeOfEntityDotNotationProperty(string entityDotNotation, SoftClass entityClass, List<SoftClass> allClasses)
        {
            // Rolinho.Permission.Id
            string propName = entityDotNotation.Split('.')[0]; // Rolinho
            List<SoftProperty> entityClassProperties = entityClass.Properties;
            SoftProperty prop = entityClassProperties.Where(x => x.IdentifierText == propName).Single(); // Role

            int i = 1;
            while (prop.Type.IsBaseType() == false)
            {
                SoftClass helperClass = allClasses.Where(x => x.Name == prop.Type).Single(); // Role

                List<SoftProperty> helperProps = helperClass.Properties;

                propName = entityDotNotation.Split('.')[i]; // Id
                prop = helperProps.Where(x => x.IdentifierText == propName).Single(); // Id
                i++;
            }

            return prop.Type;
        }

        public static string GetEntityDotNotationForDTO(List<SoftMethod> methodsOfTheNonGeneratedMapperClass, string destinationDTOClass, string sourceEntityClass, string DTOProp)
        {
            if (methodsOfTheNonGeneratedMapperClass == null)
                return null;

            List<SoftMethod> methodsWithTableFiltersAttribute = methodsOfTheNonGeneratedMapperClass.Where(x => x.Attributes.Any(x => x.Name == "TableFiltersListener")).ToList();

            SoftMethod currentConfigMethod = methodsWithTableFiltersAttribute.Where(x => x.Body.Contains($".NewConfig<{sourceEntityClass}, {destinationDTOClass}>()")).SingleOrDefault();

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
