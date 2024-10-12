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

            context.RegisterImplementationSourceOutput(classDeclarations.Collect(),
                static (spc, source) => Execute(source, spc));
        }

        private static void Execute(IList<ClassDeclarationSyntax> classes, SourceProductionContext context)
        {
            if (classes.Count() == 0) return;
            IList<ClassDeclarationSyntax> entityClasses = Helper.GetEntityClasses(classes);
            List<SoftClass> DTOClasses = Helper.GetDTOClasses(classes);

            StringBuilder sb = new StringBuilder();
            List<string> usings = new List<string>();
            StringBuilder sbUsings = new StringBuilder();

            string[] namespacePartsWithoutLastElement = Helper.GetNamespacePartsWithoutLastElement(entityClasses[0]);

            string basePartOfNamespace = string.Join(".", namespacePartsWithoutLastElement); // eg. Soft.Generator.Security
            string projectName = namespacePartsWithoutLastElement[namespacePartsWithoutLastElement.Length - 1]; // eg. Security

            sb.AppendLine($$"""
using LinqKit;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Soft.Generator.Shared.DTO;
using System.Text.Json;

namespace {{basePartOfNamespace}}.TableFiltering
{
    public static class TableFilterQueryable
    {
""");
            foreach (ClassDeclarationSyntax entityClass in entityClasses)
            {
                if (entityClass.BaseList?.Types == null)
                {
                    continue;
                }

                SoftClass entitySoftClass = entityClass.ToSoftClass(classes);

                usings.Add(entityClass.
                    Ancestors()
                   .OfType<NamespaceDeclarationSyntax>()
                   .Select(ns => ns.Name.ToString())
                   .FirstOrDefault());

                sb.AppendLine($$"""
        public static async Task<PaginationResult<{{entityClass.Identifier.Text}}>> Build(IQueryable<{{entityClass.Identifier.Text}}> query, TableFilterDTO tableFilterPayload)
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
                List<SoftClass> pairDTOClasses = DTOClasses.Where(x => x.Name == $"{entityClass.Identifier.Text}DTO").ToList(); // FT: Getting the pair DTO classes of entity class
                List<SoftProperty> efClassProps = Helper.GetAllPropertiesOfTheClass(entityClass, classes);
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
                                entityDotNotation = GetDotNotatioOfEntityFromMappers(classes, entitySoftClass, pairDTOClass, entityDotNotation); // "Role.Id"

                                if (entityDotNotation == null)
                                    continue;

                                DTOpropType = GetPropTypeOfEntityDotNotationProperty(entityDotNotation, entityClass, classes);
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
            return new PaginationResult<{{entityClass.Identifier.Text}}>()
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
                                    {{numberTypeWithoutQuestion}}[] values = JsonSerializer.Deserialize<{{numberTypeWithoutQuestion}}[]>(filter.Value.ToString());
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
        private static string GetDotNotatioOfEntityFromMappers(IList<ClassDeclarationSyntax> allClasses, SoftClass entitySoftClass, SoftClass DTOClass, string DTOClassProp)
        {
            if (DTOClassProp.EndsWith("DisplayName") && DTOClass.IsGenerated == true) // FT: Doing this thing with the IsGenerated so we can make prop in non generated DTO with "DisplayName" or "Id" sufix 
            {
                string baseClassInDotNotation = DTOClassProp.Replace("DisplayName", ""); // "Rolinho"
                SoftProperty propertyInEntityClass = entitySoftClass.Properties.Where(x => x.IdentifierText == baseClassInDotNotation).Single();
                string typeOfThePropertyInEntityClass = propertyInEntityClass.Type; // "Role"
                ClassDeclarationSyntax entityClassWhichWeAreSearchingDisplayNameFor = Helper.GetClass(typeOfThePropertyInEntityClass, allClasses);
                string displayName = Helper.GetDisplayNamePropForClass(entityClassWhichWeAreSearchingDisplayNameFor, allClasses); // Name
                displayName = displayName.Replace(".ToString()", "");
                return $"{baseClassInDotNotation}.{displayName}"; // FT: It's okay to do it like this, because when we generating DisplayNames for DTO, we are doing it just for the first level.
            }
            if (DTOClassProp.EndsWith("Id") && DTOClassProp.Length > 2 && DTOClass.IsGenerated == true)
            {
                string baseClassInDotNotation = DTOClassProp.Replace("Id", ""); // "Rolinho"
                return $"{baseClassInDotNotation}.Id";
            }

            ClassDeclarationSyntax nonGeneratedMapperClass = Helper.GetNonGeneratedMapperClass(allClasses);

            List<SoftMethod> methodsOfTheNonGeneratedMapperClass = Helper.GetMethodsOfCurrentClass(nonGeneratedMapperClass);

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

        public static string GetPropTypeOfEntityDotNotationProperty(string entityDotNotation, ClassDeclarationSyntax entityClass, IList<ClassDeclarationSyntax> allClasses)
        {
            // Rolinho.Permission.Id
            string propName = entityDotNotation.Split('.')[0]; // Rolinho
            List<SoftProperty> entityClassProperties = Helper.GetAllPropertiesOfTheClass(entityClass, allClasses);
            SoftProperty prop = entityClassProperties.Where(x => x.IdentifierText == propName).Single(); // Role

            int i = 1;
            while (prop.Type.IsBaseType() == false)
            {
                ClassDeclarationSyntax helperClass = allClasses.Where(x => x.Identifier.Text == prop.Type).SingleOrDefault(); // Role

                if (helperClass == null)
                    break;

                List<SoftProperty> helperProps = Helper.GetAllPropertiesOfTheClass(helperClass, allClasses);

                propName = entityDotNotation.Split('.')[i]; // Id
                prop = helperProps.Where(x => x.IdentifierText == propName).Single(); // Id
                i++;
            }

            return prop.Type;
        }

        public static string GetEntityDotNotationForDTO(List<SoftMethod> methodsOfTheNonGeneratedMapperClass, string destinationDTOClass, string sourceEntityClass, string DTOProp)
        {
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
