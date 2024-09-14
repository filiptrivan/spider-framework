using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Soft.SourceGenerator.NgTable.Models;
using Soft.SourceGenerators.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Soft.SourceGenerator.NgTable.Helpers
{
    public static class Helper
    {
        public static readonly string DisplayNameAttribute = "SoftDisplayName";
        public static readonly string MethodNameForExcelExportMapping = "ExcelMap"; // Change to ExcelProjectTo
        public static readonly string MapperlyIgnoreAttribute = "MapperIgnoreTarget";

        public static readonly string BusinessObject = "BusinessObject";
        public static readonly string ReadonlyObject = "ReadonlyObject";

        public static readonly string EntitiesNamespaceEnding = "Entities";
        public static readonly string DTONamespaceEnding = "DTO";
        public static readonly string MapperNamespaceEnding = "DataMappers";

        #region Syntax and Semantic targets

        public static bool IsSyntaxTargetForGenerationEntities(SyntaxNode node)
        {
            if (node is ClassDeclarationSyntax classDeclaration)
            {
                string namespaceName = classDeclaration
                   .Ancestors()
                   .OfType<NamespaceDeclarationSyntax>()
                   .Select(ns => ns.Name.ToString())
                   .FirstOrDefault();

                if (namespaceName != null && namespaceName.EndsWith($".{EntitiesNamespaceEnding}"))
                    return true;
            }

            return false;
        }

        public static ClassDeclarationSyntax GetSemanticTargetForGenerationEntities(GeneratorSyntaxContext context)
        {
            ClassDeclarationSyntax classDeclaration = (ClassDeclarationSyntax)context.Node;

            string namespaceName = classDeclaration
               .Ancestors()
               .OfType<NamespaceDeclarationSyntax>()
               .Select(ns => ns.Name.ToString())
               .FirstOrDefault();

            if (namespaceName != null && namespaceName.EndsWith($".{EntitiesNamespaceEnding}"))
            {
                return classDeclaration;
            }

            return null;
        }

        public static bool IsSyntaxTargetForGenerationDTO(SyntaxNode node)
        {
            if (node is ClassDeclarationSyntax classDeclaration)
            {
                string namespaceName = classDeclaration
                   .Ancestors()
                   .OfType<NamespaceDeclarationSyntax>()
                   .Select(ns => ns.Name.ToString())
                   .FirstOrDefault();

                if (namespaceName != null && namespaceName.EndsWith($".{DTONamespaceEnding}"))
                    return true;
            }

            return false;
        }

        public static ClassDeclarationSyntax GetSemanticTargetForGenerationDTO(GeneratorSyntaxContext context)
        {
            ClassDeclarationSyntax classDeclaration = (ClassDeclarationSyntax)context.Node;

            string namespaceName = classDeclaration
               .Ancestors()
               .OfType<NamespaceDeclarationSyntax>()
               .Select(ns => ns.Name.ToString())
               .FirstOrDefault();

            if (namespaceName != null && namespaceName.EndsWith($".{DTONamespaceEnding}"))
            {
                return classDeclaration;
            }

            return null;
        }

        public static bool IsSyntaxTargetForGenerationEntitiesAndDTO(SyntaxNode node)
        {
            if (node is ClassDeclarationSyntax classDeclaration)
            {
                string namespaceName = classDeclaration
                   .Ancestors()
                   .OfType<NamespaceDeclarationSyntax>()
                   .Select(ns => ns.Name.ToString())
                   .FirstOrDefault();

                if (namespaceName != null && (namespaceName.EndsWith($".{DTONamespaceEnding}") || namespaceName.EndsWith($".{EntitiesNamespaceEnding}")))
                    return true;
            }

            return false;
        }

        public static ClassDeclarationSyntax GetSemanticTargetForGenerationEntitiesAndDTO(GeneratorSyntaxContext context)
        {
            ClassDeclarationSyntax classDeclaration = (ClassDeclarationSyntax)context.Node;

            string namespaceName = classDeclaration
               .Ancestors()
               .OfType<NamespaceDeclarationSyntax>()
               .Select(ns => ns.Name.ToString())
               .FirstOrDefault();

            if (namespaceName != null && (namespaceName.EndsWith($".{DTONamespaceEnding}") || namespaceName.EndsWith($".{EntitiesNamespaceEnding}")))
            {
                return classDeclaration;
            }

            return null;
        }

        public static bool IsSyntaxTargetForGenerationDTOAndDataMappers(SyntaxNode node)
        {
            if (node is ClassDeclarationSyntax classDeclaration)
            {
                string namespaceName = classDeclaration
                   .Ancestors()
                   .OfType<NamespaceDeclarationSyntax>()
                   .Select(ns => ns.Name.ToString())
                   .FirstOrDefault();

                if (namespaceName != null)
                {
                    if (namespaceName.EndsWith($".{DTONamespaceEnding}") || namespaceName.EndsWith(".DataMappers"))
                        return true;
                }
            }

            return false;
        }

        public static ClassDeclarationSyntax GetSemanticTargetForGenerationDTOAndDataMappers(GeneratorSyntaxContext context)
        {
            ClassDeclarationSyntax classDeclaration = (ClassDeclarationSyntax)context.Node;

            string namespaceName = classDeclaration
               .Ancestors()
               .OfType<NamespaceDeclarationSyntax>()
               .Select(ns => ns.Name.ToString())
               .FirstOrDefault();

            if (namespaceName != null)
            {
                if (namespaceName.EndsWith($".{DTONamespaceEnding}") || namespaceName.EndsWith(".DataMappers"))
                {
                    return classDeclaration;
                }
            }

            return null;
        }

        public static bool IsSyntaxTargetForGenerationEntitiesAndDataMappers(SyntaxNode node)
        {
            if (node is ClassDeclarationSyntax classDeclaration)
            {
                string namespaceName = classDeclaration
                   .Ancestors()
                   .OfType<NamespaceDeclarationSyntax>()
                   .Select(ns => ns.Name.ToString())
                   .FirstOrDefault();
                if (namespaceName != null)
                {
                    if (namespaceName.EndsWith($".{EntitiesNamespaceEnding}") || namespaceName.EndsWith(".DataMappers"))
                        return true;
                }
            }

            return false;
        }

        public static ClassDeclarationSyntax GetSemanticTargetForGenerationEntitiesAndDataMapper(GeneratorSyntaxContext context)
        {
            ClassDeclarationSyntax classDeclaration = (ClassDeclarationSyntax)context.Node;

            string namespaceName = classDeclaration
               .Ancestors()
               .OfType<NamespaceDeclarationSyntax>()
               .Select(ns => ns.Name.ToString())
               .FirstOrDefault();

            if (namespaceName != null)
            {
                if (namespaceName.EndsWith($".{EntitiesNamespaceEnding}") || namespaceName.EndsWith(".DataMappers"))
                {
                    return classDeclaration;
                }
            }

            return null;
        }

        public static bool IsSyntaxTargetForGenerationDTODataMappersAndEntities(SyntaxNode node)
        {
            if (node is ClassDeclarationSyntax classDeclaration)
            {
                string namespaceName = classDeclaration
                   .Ancestors()
                   .OfType<NamespaceDeclarationSyntax>()
                   .Select(ns => ns.Name.ToString())
                   .FirstOrDefault();
                if (namespaceName != null)
                {
                    if (namespaceName.EndsWith($".{DTONamespaceEnding}") || namespaceName.EndsWith(".DataMappers") || namespaceName.EndsWith($".{EntitiesNamespaceEnding}"))
                        return true;
                }
            }

            return false;
        }

        public static ClassDeclarationSyntax GetSemanticTargetForGenerationDTODataMappersAndEntities(GeneratorSyntaxContext context)
        {
            ClassDeclarationSyntax classDeclaration = (ClassDeclarationSyntax)context.Node;

            string namespaceName = classDeclaration
               .Ancestors()
               .OfType<NamespaceDeclarationSyntax>()
               .Select(ns => ns.Name.ToString())
               .FirstOrDefault();

            if (namespaceName != null)
            {
                if (namespaceName.EndsWith($".{DTONamespaceEnding}") || namespaceName.EndsWith(".DataMappers") || namespaceName.EndsWith($".{EntitiesNamespaceEnding}"))
                {
                    return classDeclaration;
                }
            }

            return null;
        }

        public static bool IsSyntaxTargetForGenerationControllers(SyntaxNode node)
        {
            if (node is ClassDeclarationSyntax classDeclaration)
            {
                string namespaceName = classDeclaration
                   .Ancestors()
                   .OfType<NamespaceDeclarationSyntax>()
                   .Select(ns => ns.Name.ToString())
                   .FirstOrDefault();

                if (namespaceName != null)
                {
                    if (namespaceName.EndsWith(".Controllers"))
                        return true;
                }
            }

            return false;
        }

        public static ClassDeclarationSyntax GetSemanticTargetForGenerationControllers(GeneratorSyntaxContext context)
        {
            ClassDeclarationSyntax classDeclaration = (ClassDeclarationSyntax)context.Node;

            string namespaceName = classDeclaration
               .Ancestors()
               .OfType<NamespaceDeclarationSyntax>()
               .Select(ns => ns.Name.ToString())
               .FirstOrDefault();

            if (namespaceName != null)
            {
                if (namespaceName.EndsWith(".Controllers"))
                {
                    return classDeclaration;
                }
            }

            return null;
        }

        public static bool IsSyntaxTargetForGenerationValidationRules(SyntaxNode node)
        {
            if (node is ClassDeclarationSyntax classDeclaration)
            {
                string namespaceName = classDeclaration
                   .Ancestors()
                   .OfType<NamespaceDeclarationSyntax>()
                   .Select(ns => ns.Name.ToString())
                   .FirstOrDefault();

                if (namespaceName != null && namespaceName.EndsWith(".ValidationRules"))
                    return true;
            }

            return false;
        }

        public static ClassDeclarationSyntax GetSemanticTargetForGenerationValidationRules(GeneratorSyntaxContext context)
        {
            ClassDeclarationSyntax classDeclaration = (ClassDeclarationSyntax)context.Node;

            string namespaceName = classDeclaration
               .Ancestors()
               .OfType<NamespaceDeclarationSyntax>()
               .Select(ns => ns.Name.ToString())
               .FirstOrDefault();

            if (namespaceName != null && namespaceName.EndsWith(".ValidationRules"))
            {
                return classDeclaration;
            }

            return null;
        }

        public static bool IsSyntaxTargetForGenerationEnums(SyntaxNode node)
        {
            if (node is EnumDeclarationSyntax enumDeclaration)
            {
                string namespaceName = enumDeclaration
                   .Ancestors()
                   .OfType<NamespaceDeclarationSyntax>()
                   .Select(ns => ns.Name.ToString())
                   .FirstOrDefault();

                if (namespaceName != null && namespaceName.EndsWith(".Enums"))
                    return true;
            }

            return false;
        }

        public static EnumDeclarationSyntax GetSemanticTargetForGenerationEnums(GeneratorSyntaxContext context)
        {
            EnumDeclarationSyntax enumDeclaration = (EnumDeclarationSyntax)context.Node;

            string namespaceName = enumDeclaration
               .Ancestors()
               .OfType<NamespaceDeclarationSyntax>()
               .Select(ns => ns.Name.ToString())
               .FirstOrDefault();

            if (namespaceName != null && namespaceName.EndsWith(".Enums"))
            {
                return enumDeclaration;
            }

            return null;
        }

        #endregion

        /// <summary>
        /// Getting all properties of the single class <paramref name="c"/>, including inherited ones.
        /// The inherited properties doesn't have any attributes
        /// </summary>
        public static List<Prop> GetAllPropertiesOfTheClass(ClassDeclarationSyntax c, IEnumerable<ClassDeclarationSyntax> allClasses, bool getEnumerableProperties = false)
        {

            TypeSyntax baseType = c.BaseList?.Types.FirstOrDefault()?.Type; //BaseClass<long>
            ClassDeclarationSyntax baseClass = GetClass(baseType, allClasses);

            string s = c.Identifier.Text;

            List<Prop> properties = GetPropsOfCurrentClass(c);

            TypeSyntax typeGeneric = null;

            while (baseType != null)
            {
                if (baseType is GenericNameSyntax genericNameSyntax && baseClass == null)
                {
                    typeGeneric = genericNameSyntax.TypeArgumentList.Arguments.FirstOrDefault(); // long
                    properties.AddRange(GetPropertiesForBaseClasses(baseType, typeGeneric));
                    break;
                }
                else
                {
                    foreach (PropertyDeclarationSyntax prop in baseClass.Members.OfType<PropertyDeclarationSyntax>())
                    {
                        properties.Add(GetPropWithModifiedT(prop, typeGeneric));
                    }
                }

                baseType = baseClass.BaseList?.Types.FirstOrDefault()?.Type;
                baseClass = GetClass(baseType, allClasses);
            }

            if (getEnumerableProperties == false)
            {
                properties = properties
                    .Where(prop => prop.Type.IsEnumerable() == false)
                    .ToList();
            }

            return properties;
        }

        public static List<SoftAttribute> GetAllAttributesOfTheProperty(PropertyDeclarationSyntax prop)
        {
            List<SoftAttribute> softAttributes = new List<SoftAttribute>();
            softAttributes = prop.AttributeLists.SelectMany(x => x.Attributes).Select(x => new SoftAttribute
            {
                Name = x.Name.ToString(),
                Value = x?.ArgumentList?.Arguments.FirstOrDefault()?.ToString()?.Replace("\"", "")
            })
            .ToList();
            return softAttributes;
        }

        public static List<SoftAttribute> GetAllAttributesOfTheClass(ClassDeclarationSyntax c, IList<ClassDeclarationSyntax> classes)
        {
            ClassDeclarationSyntax cHelper = SyntaxFactory.ClassDeclaration(c.Identifier).WithBaseList(c.BaseList).WithAttributeLists(c.AttributeLists); // FT: Doing this because of reference type, we don't want to change c
            List<SoftAttribute> softAttributes = new List<SoftAttribute>();

            TypeSyntax baseType = cHelper.BaseList?.Types.FirstOrDefault()?.Type; // BaseClass
            // FT: Getting the attributes for all base classes also
            do
            {
                softAttributes.AddRange(cHelper.AttributeLists.SelectMany(x => x.Attributes).Select(x => new SoftAttribute
                {
                    Name = x.Name.ToString(),
                    Value = x?.ArgumentList?.Arguments.FirstOrDefault()?.ToString()?.Replace("\"", "")
                })
                .ToList());
                cHelper = classes.Where(x => x.Identifier.Text == baseType?.ToString()).FirstOrDefault();
                baseType = cHelper?.BaseList?.Types.FirstOrDefault()?.Type;
            }
            while (baseType != null);

            return softAttributes;
        }

        public static List<Prop> GetPropsToExcludeFromExcelExport(string className, IList<ClassDeclarationSyntax> DTOClasses, ClassDeclarationSyntax mapperClass)
        {
            List<Prop> DTOClassProperties = new List<Prop>();

            List<ClassDeclarationSyntax> pairDTOClasses = DTOClasses.Where(x => x.Identifier.Text == className).ToList(); // There will be 2, partial generated and partial manual
            foreach (ClassDeclarationSyntax classDTO in pairDTOClasses)
            {
                DTOClassProperties.AddRange(GetAllPropertiesOfTheClass(classDTO, DTOClasses));
            }

            MethodDeclarationSyntax excelMethod = mapperClass?.Members.OfType<MethodDeclarationSyntax>()
               .Where(x => x.ReturnType.ToString() == className && x.Identifier.ToString() == $"{MethodNameForExcelExportMapping}")
               .SingleOrDefault();

            IList<SoftAttribute> excludePropAttributes = new List<SoftAttribute>();

            DTOClassProperties = DTOClassProperties
                .Where(prop => prop.Type.IsEnumerable())
                .ToList();

            // ubacivanje atributa gde vidimo koje propertije treba da preskocimo u Excelu
            if (excelMethod != null)
            {
                foreach (AttributeListSyntax item in excelMethod.AttributeLists)
                {
                    foreach (AttributeSyntax attribute in item.Attributes)
                    {
                        string attributeName = attribute.Name.ToString();
                        if (attributeName != null && attributeName == $"{MapperlyIgnoreAttribute}")
                        {
                            string propNameInsideBrackets = attribute.ArgumentList.Arguments.FirstOrDefault().ToString().Split('.').Last().Replace(")", "");
                            //excludePropAttributes.Add(new SoftAttribute() { Name = attribute.Name.ToString(), PropNameInsideBrackets = propNameInsideBrackets }); // FT: i don't need this if i don't know which prop type it is
                            DTOClassProperties.Add(new Prop { IdentifierText = propNameInsideBrackets });
                        }
                    }
                }
            }

            return DTOClassProperties;
        }

        /// <summary>
        /// Getting non generated partial mapper class.
        /// </summary>
        public static ClassDeclarationSyntax GetManualyWrittenMapperClass(IList<ClassDeclarationSyntax> classes)
        {
            ClassDeclarationSyntax mapperClass = classes
                .Where(x =>
                {
                    string namespaceName = x.Ancestors().OfType<NamespaceDeclarationSyntax>()
                        .Select(ns => ns.Name.ToString())
                        .FirstOrDefault(ns => ns.EndsWith($".{MapperNamespaceEnding}"));
                    if (namespaceName != null && x.AttributeLists.Count == 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                })
                .Single(); // FT: It should allways be only one

            return mapperClass;
        }

        /// <summary>
        /// Getting generated mapper class.
        /// </summary>
        public static ClassDeclarationSyntax GetGeneratedMapperClass(IList<ClassDeclarationSyntax> classes)
        {
            ClassDeclarationSyntax mapperClass = classes
                .Where(x =>
                {
                    string namespaceName = x.Ancestors().OfType<NamespaceDeclarationSyntax>()
                        .Select(ns => ns.Name.ToString())
                        .FirstOrDefault(ns => ns.EndsWith($".{MapperNamespaceEnding}"));
                    if (namespaceName != null && x.AttributeLists.Count == 1)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                })
                .Single(); // FT: It should allways be only one

            return mapperClass;
        }

        /// <summary>
        /// Getting generated mapper class and partial non generated mapper class, there should be always be two.
        /// </summary>
        public static List<ClassDeclarationSyntax> GetAllMapperClassesFromAssembly(IList<ClassDeclarationSyntax> classes)
        {
            List<ClassDeclarationSyntax> mapperClasses = classes
                .Where(x =>
                {
                    string namespaceName = x.Ancestors().OfType<NamespaceDeclarationSyntax>()
                        .Select(ns => ns.Name.ToString())
                        .FirstOrDefault(ns => ns.EndsWith($".{MapperNamespaceEnding}"));
                    if (namespaceName != null)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                })
                .ToList();

            return mapperClasses;
        }

        /// <summary>
        /// From all passed classes, filtering only those that are entities
        /// </summary>
        public static List<ClassDeclarationSyntax> GetEntityClasses(IList<ClassDeclarationSyntax> classes)
        {
            List<ClassDeclarationSyntax> entityClasses = classes
                .Where(x =>
                {
                    string namespaceName = x.Ancestors().OfType<NamespaceDeclarationSyntax>()
                        .Select(ns => ns.Name.ToString())
                        .FirstOrDefault(ns => ns.EndsWith($".{EntitiesNamespaceEnding}"));

                    if (namespaceName != null)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                })
                .ToList();

            return entityClasses;
        }

        /// <summary>
        /// From all passed classes, filtering only those that are DTOs and
        /// </summary>
        public static List<ClassDeclarationSyntax> GetDTOClasses(IList<ClassDeclarationSyntax> classes)
        {
            List<ClassDeclarationSyntax> DTOClasses = classes
               .Where(x =>
               {
                   string namespaceName = x.Ancestors().OfType<NamespaceDeclarationSyntax>()
                       .Select(ns => ns.Name.ToString())
                       .FirstOrDefault(ns => ns.EndsWith($".{DTONamespaceEnding}"));

                   if (namespaceName != null) // Here, the count would be 2 - "public class MyClass : MyBaseClass, IMyInterface { }"
                   {
                       return true;
                   }
                   else
                   {
                       return false;
                   }
               }).ToList();

            return DTOClasses;
        }

        /// <summary>
        /// FT: Without inherited
        /// </summary>
        public static List<Prop> GetPropsOfCurrentClass(ClassDeclarationSyntax c)
        {

            List<Prop> properties = c.Members.OfType<PropertyDeclarationSyntax>()
                .Select(prop => new Prop()
                {
                    Type = prop.Type.ToString(),
                    IdentifierText = prop.Identifier.Text,
                    Attributes = prop.AttributeLists.SelectMany(x => x.Attributes).Select(x => new SoftAttribute
                    {
                        Name = x.Name.ToString(),
                        Value = x?.ArgumentList?.Arguments.FirstOrDefault()?.ToString()?.Replace("\"", "")
                    })
                    .ToList()
                })
                .ToList();

            return properties;
        }

        public static List<EnumMember> GetEnumMembers(EnumDeclarationSyntax enume)
        {
            List<EnumMember> enumMembers = new List<EnumMember>();
            foreach (EnumMemberDeclarationSyntax member in enume.Members)
            {
                string name = member.Identifier.Text;
                string value = member.EqualsValue != null ? member.EqualsValue.Value.ToString() : "Auto-Generated";
                enumMembers.Add(new EnumMember { Name = name, Value = value });
            }
            return enumMembers;
        }

        public static ClassDeclarationSyntax GetClass(TypeSyntax type, IEnumerable<ClassDeclarationSyntax> classes)
        {
            string typeName = "";
            if (type is GenericNameSyntax genericNameSyntax)
            {
                typeName = genericNameSyntax.Identifier.Text; // BaseClass<T>
            }
            else if (type is NameSyntax nameSyntax)
            {
                typeName = nameSyntax.ToString();
            }
            return classes.Where(x => x.Identifier.Text == typeName).FirstOrDefault();
        }

        public static Prop GetPropWithModifiedT(PropertyDeclarationSyntax prop, TypeSyntax typeGeneric)
        {
            List<SoftAttribute> attributes = GetAllAttributesOfTheProperty(prop);
            Prop newProp = new Prop() { Type = prop.Type.ToString(), IdentifierText = prop.Identifier.Text, Attributes = attributes };

            if (prop.Type.ToString() == "T") // If some property has type of T, we change it to long for example
            {
                newProp.Type = typeGeneric.ToString();
                return newProp;
            }

            return newProp;
        }

        public static List<Prop> GetPropertiesForBaseClasses(TypeSyntax type, TypeSyntax typeGeneric)
        {
            string typeName = type.ToString();
            if (typeName.StartsWith($"{BusinessObject}"))
            {
                if (typeName.Contains("DTO"))
                {
                    return new List<Prop>()
                    {
                        new Prop{ Type = "int?", IdentifierText = "Version" },
                        new Prop{ Type = typeGeneric.ToString(), IdentifierText = "Id" },
                        new Prop{ Type = "DateTime?", IdentifierText = "CreatedAt" },
                        new Prop{ Type = "DateTime?", IdentifierText = "ModifiedAt" },
                    };
                }
                else
                {
                    return new List<Prop>()
                    {
                        new Prop{ Type = "int", IdentifierText = "Version" },
                        new Prop{ Type = typeGeneric.ToString(), IdentifierText = "Id" },
                        new Prop{ Type = "DateTime", IdentifierText = "CreatedAt" },
                        new Prop{ Type = "DateTime", IdentifierText = "ModifiedAt" },
                    };
                }
            }
            else if (typeName.StartsWith($"{ReadonlyObject}"))
            {
                if (typeName.Contains("DTO"))
                {
                    return new List<Prop>()
                    {
                        new Prop { Type = $"{typeGeneric}", IdentifierText = "Id" },
                        new Prop { Type = "DateTime?", IdentifierText = "CreatedAt" },
                    };
                }
                else
                {
                    return new List<Prop>()
                    {
                        new Prop { Type = typeGeneric.ToString(), IdentifierText = "Id" },
                        new Prop { Type = "DateTime", IdentifierText = "CreatedAt" },
                    };
                }
            }
            else
            {
                return new List<Prop>() { };
            }
        }

        /// <summary>
        /// Pass the properties with the C# data types
        /// </summary>
        public static List<string> GetAngularImports(List<Prop> properties, string importPath = null)
        {
            List<string> result = new List<string>();
            foreach (Prop prop in properties)
            {
                string cSharpDataType = prop.Type;
                if (cSharpDataType.IsBaseType() == false)
                {
                    string angularDataType = GetAngularDataTypeForImport(cSharpDataType);
                    if (cSharpDataType.Contains($"{DTONamespaceEnding}"))
                    {
                        result.Add($"import {{ {angularDataType} }} from \"./{importPath}{angularDataType.FromPascalToKebabCase()}.generated\";");
                    }
                    else if (cSharpDataType.Contains($"Codes"))
                    {
                        result.Add($"import {{ {angularDataType} }} from \"../../enums/generated/{importPath}{angularDataType.FromPascalToKebabCase()}.generated\";"); // TODO FT: When you need, implement so you can also send enums from the controller
                    }
                }
            }

            return result.Distinct().ToList();
        }

        public static string GetAngularDataType(string CSharpDataType)
        {
            switch (CSharpDataType)
            {
                case "string":
                    return "string";
                case "bool":
                case "bool?":
                    return "boolean";
                case "DateTime":
                case "DateTime?":
                    return "Date";
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
                    return "number";
                default:
                    break;
            }

            if (CSharpDataType.IsEnumerable())
                return $"{ExtractAngularClassNameFromGenericType(CSharpDataType)}[]";

            if (CSharpDataType.Contains(DTONamespaceEnding)) // FT: We don't want to handle "ActionResult" for example
                return ExtractAngularClassNameFromGenericType(CSharpDataType); // ManyToOne

            if (CSharpDataType.EndsWith("Codes")) // Enum
                return CSharpDataType;

            return "any"; // eg. "ActionResult", "Task"...
        }

        public static string GetAngularDataTypeForImport(string CSharpDataType)
        {
            if (ExtractAngularClassNameFromGenericType(CSharpDataType).IsBaseType())
                return null;

            if (ExtractAngularClassNameFromGenericType(CSharpDataType).EndsWith("Codes"))
                return CSharpDataType;

            return ExtractAngularClassNameFromGenericType(CSharpDataType);
        }

        public static string GetGenericIdType(ClassDeclarationSyntax c, IList<ClassDeclarationSyntax> classes)
        {
            TypeSyntax baseType = c.BaseList?.Types.FirstOrDefault()?.Type; //BaseClass<long>

            while (baseType is not GenericNameSyntax && baseType != null)
            {
                ClassDeclarationSyntax baseC = classes.Where(x => x.Identifier.Text == baseType.ToString()).FirstOrDefault();
                baseType = baseC.BaseList?.Types.FirstOrDefault()?.Type; //BaseClass<long>
            }

            if (baseType != null && baseType is GenericNameSyntax genericNameSyntax)
                return genericNameSyntax.TypeArgumentList.Arguments.FirstOrDefault().ToString(); // long
            else
                return "Every entity class needs to have the base class";
        }



        public static string GetGenericBaseType(ClassDeclarationSyntax c)
        {
            TypeSyntax baseType = c.BaseList?.Types.Where(x => x.Type is GenericNameSyntax).FirstOrDefault()?.Type; //BaseClass<long>
            if (baseType != null)
                return baseType.ToString();
            else
                return "Every entity class needs to have the base class";
        }

        public static string GetBaseType(ClassDeclarationSyntax c)
        {
            TypeSyntax baseType = c.BaseList?.Types.FirstOrDefault()?.Type; //BaseClass<long>
            if (baseType != null)
                return baseType.ToString();
            else
                return "Every entity class needs to have the base class";
        }

        public static List<string> GetBaseTypeNames(ClassDeclarationSyntax c)
        {
            List<string> baseTypeNames = c.BaseList?.Types.Select(x => x.Type.ToString()).ToList();
            return baseTypeNames;
        }

        /// <summary>
        /// eg. if we have List2:List1, List1:List0 we will return List2
        /// </summary>
        public static List<ClassDeclarationSyntax> GetUninheritedClasses(IList<ClassDeclarationSyntax> classes)
        {
            List<string> baseTypeNames = classes.SelectMany(x => GetBaseTypeNames(x)).ToList();
            List<ClassDeclarationSyntax> helper = new List<ClassDeclarationSyntax>();
            foreach (ClassDeclarationSyntax entityClass in classes)
            {
                if (baseTypeNames.Contains(entityClass.Identifier.Text) == false) // FT: Ako se neki entity ne nalazi nigde kao base type onda ga koristimo
                    helper.Add(entityClass);
            }
            return helper;
        }

        public static string GetDisplayNamePropForClass(ClassDeclarationSyntax c, IList<ClassDeclarationSyntax> classes)
        {
            List<Prop> props = GetAllPropertiesOfTheClass(c, classes);
            Prop displayNamePropForClass = props.Where(x => x.Attributes.Any(x => x.Name == DisplayNameAttribute)).SingleOrDefault();
            if (displayNamePropForClass == null)
                return "YOU DON'T HAVE DISPLAYNAME PROP, OR YOU HAVE MORE THEN ONE";

            return displayNamePropForClass.IdentifierText;
        }

        public static string[] GetNamespacePartsWithoutLastElement(BaseTypeDeclarationSyntax b)
        {
            string domainModelNamespace = b
                .Ancestors()
                .OfType<NamespaceDeclarationSyntax>()
                .Select(ns => ns.Name.ToString())
                .FirstOrDefault();

            string[] namespaceParts = domainModelNamespace.Split('.');
            string[] namespacePartsWithoutLastElement = namespaceParts.Take(namespaceParts.Length - 1).ToArray();

            return namespacePartsWithoutLastElement; // eg. Soft, Generator, Security
        }

        public static string ExtractAngularClassNameFromGenericType(string input)
        {
            string result;

            string[] parts = input.Split('<');
            parts[parts.Length-1] = parts[parts.Length-1].Replace(">", "");
            if (parts[parts.Length-1].IsBaseType() && parts[parts.Length-2].IsEnumerable() == false)
                result = parts[parts.Length-2];
            else
                result = parts[parts.Length-1];

            return result.Replace(DTONamespaceEnding, "").Replace("[]", "");
        }

        public static void WriteToTheFile(string data, string path)
        {
            if (data != null)
            {
                StreamWriter sw = new StreamWriter(path);
                sw.WriteLine(data);
                sw.Close();
            }
        }
    }
}


public class Prop
{
    public string Type { get; set; }
    public string IdentifierText { get; set; }

    public List<SoftAttribute> Attributes = new List<SoftAttribute>();
}

public class SoftAttribute
{
    public string Name { get; set; }
    public string PropNameInsideBrackets { get; set; }

    /// <summary>
    /// Doesn't handle if more values are in the prenteces, eg. [Attribute("First", "Second")]
    /// </summary>
    public string Value { get; set; }
}

public class EnumMember
{
    public string Name { get; set; }
    public string Value { get; set; }
}

