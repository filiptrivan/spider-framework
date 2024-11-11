using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Soft.SourceGenerators.Helpers;
using Soft.SourceGenerators.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
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
        public static readonly string ValidationNamespaceEnding = "ValidationRules";
        public static readonly string MapperNamespaceEnding = "DataMappers";

        public static readonly List<string> BaseTypePropertiies = new List<string> { "Id", "Version", "CreatedAt", "ModifiedAt" };
        public static readonly List<string> BaseClassNames = new List<string> { "TableFilter", "TableResponse", "TableSelection", "Namebook", "Codebook", "SimpleSaveResult", "BusinessObject", "ReadonlyObject", "ExcelReportOptions", "RoleUser" };

        #region Syntax and Semantic targets

        public static IncrementalValueProvider<IEnumerable<INamedTypeSymbol>> GetReferencedProjectsSymbolsDTO(IncrementalGeneratorInitializationContext context)
        {
            return context.CompilationProvider
                .Select(static (compilation, _) =>
                {
                    var classSymbols = new List<INamedTypeSymbol>();
                    foreach (var referencedAssembly in compilation.SourceModule.ReferencedAssemblySymbols
                             .Where(a => a.Name.Contains("Soft") || a.Name.Contains("Playerty")))
                    {
                        GetClassesFromDTO(referencedAssembly.GlobalNamespace, classSymbols);
                    }
                    return classSymbols.AsEnumerable();
                });
        }

        public static IncrementalValueProvider<IEnumerable<INamedTypeSymbol>> GetReferencedProjectsSymbolsEntities(IncrementalGeneratorInitializationContext context)
        {
            return context.CompilationProvider
                .Select(static (compilation, _) =>
                {
                    var classSymbols = new List<INamedTypeSymbol>();
                    foreach (var referencedAssembly in compilation.SourceModule.ReferencedAssemblySymbols
                             .Where(a => a.Name.Contains("Soft") || a.Name.Contains("Playerty")))
                    {
                        GetClassesFromEntities(referencedAssembly.GlobalNamespace, classSymbols);
                    }
                    return classSymbols.AsEnumerable();
                });
        }

        public static bool IsSyntaxTargetForGenerationEntities(SyntaxNode node)
        {
            if (node is ClassDeclarationSyntax classDeclaration)
            {
                string namespaceName = classDeclaration
                   .Ancestors()
                   .OfType<NamespaceDeclarationSyntax>()
                   .Select(ns => ns.Name.ToString())
                   .FirstOrDefault();

                if (namespaceName != null && (namespaceName.EndsWith($".{EntitiesNamespaceEnding}") || namespaceName.EndsWith($".GeneratorSettings")))
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

            if (namespaceName != null && (namespaceName.EndsWith($".{EntitiesNamespaceEnding}") || namespaceName.EndsWith($".GeneratorSettings")))
                return classDeclaration;

            return null;
        }

        public static void GetClassesFromEntities(INamespaceSymbol namespaceSymbol, List<INamedTypeSymbol> classSymbols)
        {
            // Add all the type members (classes, structs, etc.) in this namespace
            foreach (INamedTypeSymbol type in namespaceSymbol.GetTypeMembers())
            {
                if (type.TypeKind == TypeKind.Class && type.Name.EndsWith("Entities"))
                {
                    classSymbols.Add(type);
                }
            }

            // Recursively gather classes from nested namespaces
            foreach (var nestedNamespace in namespaceSymbol.GetNamespaceMembers())
            {
                GetClassesFromEntities(nestedNamespace, classSymbols);
            }
        }

        public static void GetClassesFromDTO(INamespaceSymbol namespaceSymbol, List<INamedTypeSymbol> classSymbols)
        {
            // Add all the type members (classes, structs, etc.) in this namespace
            foreach (INamedTypeSymbol type in namespaceSymbol.GetTypeMembers())
            {
                if (type.TypeKind == TypeKind.Class && type.Name.EndsWith("DTO"))
                {
                    classSymbols.Add(type);
                }
            }

            // Recursively gather classes from nested namespaces
            foreach (var nestedNamespace in namespaceSymbol.GetNamespaceMembers())
            {
                GetClassesFromDTO(nestedNamespace, classSymbols);
            }
        }

        #endregion

        #region Class list filters

        public static List<ClassDeclarationSyntax> GetEntityClasses(IList<ClassDeclarationSyntax> classes)
        {
            return classes
                .Where(x => x.Ancestors()
                    .OfType<NamespaceDeclarationSyntax>()
                    .Select(ns => ns.Name.ToString())
                    .Any(ns => ns.EndsWith($".{EntitiesNamespaceEnding}")))
                .ToList();
        }

        public static List<SoftClass> GetSoftEntityClasses(IList<ClassDeclarationSyntax> classes)
        {
            return classes
                .Where(x => x.Ancestors()
                    .OfType<NamespaceDeclarationSyntax>()
                    .Select(ns => ns.Name.ToString())
                    .Any(ns => ns.EndsWith($".{EntitiesNamespaceEnding}")))
                .Select(x =>
                {
                    return new SoftClass
                    {
                        Name = x.Identifier.Text,
                        Properties = GetAllPropertiesOfTheClass(x, classes, true),
                        Attributes = GetAllAttributesOfTheClass(x, classes)
                    };
                })
                .ToList();
        }

        #endregion

        /// <summary>
        /// Getting all properties of the single class <paramref name="c"/>, including inherited ones.
        /// The inherited properties doesn't have any attributes
        /// </summary>
        public static List<SoftProperty> GetAllPropertiesOfTheClass(ClassDeclarationSyntax c, IEnumerable<ClassDeclarationSyntax> allClasses, bool getEnumerableProperties = false)
        {
            TypeSyntax baseType = c.BaseList?.Types.FirstOrDefault()?.Type; //BaseClass<long>
            ClassDeclarationSyntax baseClass = GetClass(baseType, allClasses);

            string s = c.Identifier.Text;

            List<SoftProperty> properties = GetPropsOfCurrentClass(c);

            TypeSyntax typeGeneric = null;

            while (baseType != null)
            {
                if (baseType is GenericNameSyntax genericNameSyntax && baseClass == null)
                {
                    typeGeneric = genericNameSyntax.TypeArgumentList.Arguments.FirstOrDefault(); // long
                    properties.AddRange(GetPropertiesForBaseClasses(baseType.ToString(), typeGeneric.ToString()));
                    break;
                }
                else if (baseClass == null)
                {
                    if (baseType.ToString() == "Role")
                        properties.AddRange(GetRoleProperties());

                    if (baseType.ToString() == "RoleDTO")
                        properties.AddRange(GetRoleDTOProperties());

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

        // FT HACK, FT TODO: Make this with all project references
        public static List<SoftProperty> GetRoleProperties()
        {
            List<SoftProperty> properties = new List<SoftProperty>
            {
                new SoftProperty
                {
                    IdentifierText="Name", Type="string", Attributes=new List<SoftAttribute>
                    {
                        new SoftAttribute { Name="SoftDisplayName" },
                        new SoftAttribute { Name="Required" },
                        new SoftAttribute { Name="StringLength", Value="255, MinimumLength = 1" },
                    }
                },
                new SoftProperty 
                {
                    IdentifierText="Description", Type="string", Attributes=new List<SoftAttribute>
                    {
                        new SoftAttribute { Name="StringLength", Value="400, MinimumLength = 1" },
                    }
                },
                new SoftProperty 
                {
                    IdentifierText="Permissions", Type="List<Permission>"
                }
            };

            properties.AddRange(GetPropertiesForBaseClasses(BusinessObject, "int"));

            return properties;
        }

        public static List<SoftProperty> GetRoleDTOProperties()
        {
            List<SoftProperty> properties = new List<SoftProperty>
            {
                new SoftProperty
                {
                    IdentifierText="Name", Type="string",
                },
                new SoftProperty
                {
                    IdentifierText="Description", Type="string"
                },
                new SoftProperty
                {
                    IdentifierText="Permissions", Type="List<PermissionDTO>"
                }
            };

            properties.AddRange(GetPropertiesForBaseClasses($"{BusinessObject}DTO", "int"));

            return properties;
        }

        public static List<SoftAttribute> GetAllAttributesOfTheMember(MemberDeclarationSyntax prop)
        {
            List<SoftAttribute> softAttributes = new List<SoftAttribute>();
            softAttributes = prop.AttributeLists.SelectMany(x => x.Attributes).Select(x =>
            {
                return GetSoftAttribute(x);
            })
            .ToList();
            return softAttributes;
        }

        public static SoftAttribute GetSoftAttribute(AttributeSyntax a)
        {
            string argumentValue = a?.ArgumentList?.Arguments != null && a.ArgumentList.Arguments.Any()
                    ? string.Join(", ", a.ArgumentList.Arguments.Select(arg => arg?.ToString()))
                    : null; ; // FT: Doing this because of Range(0, 5) (long tail because of null pointer exception)
            return new SoftAttribute
            {
                Name = a.Name.ToString(),
                Value = argumentValue?.Replace("\"", "").Replace("@", "")
            };
        }
    
        public static List<SoftAttribute> GetAllAttributesOfTheClass(ClassDeclarationSyntax c, IList<ClassDeclarationSyntax> classes)
        {
            if (c == null) return null;

            ClassDeclarationSyntax cHelper = SyntaxFactory.ClassDeclaration(c.Identifier).WithBaseList(c.BaseList).WithAttributeLists(c.AttributeLists); // FT: Doing this because of reference type, we don't want to change c
            List<SoftAttribute> softAttributes = new List<SoftAttribute>();

            TypeSyntax baseType = cHelper.BaseList?.Types.FirstOrDefault()?.Type; // BaseClass
            // FT: Getting the attributes for all base classes also
            do
            {
                softAttributes.AddRange(cHelper.AttributeLists.SelectMany(x => x.Attributes).Select(x =>
                {
                    return GetSoftAttribute(x);
                })
                .ToList());
                cHelper = classes.Where(x => x.Identifier.Text == baseType?.ToString()).FirstOrDefault();
                baseType = cHelper?.BaseList?.Types.FirstOrDefault()?.Type;
            }
            while (baseType != null);

            return softAttributes;
        }

        public static List<SoftProperty> GetManyToOneRequiredProperties(string nameOfTheEntityClass, List<SoftClass> softEntityClasses)
        {
            return softEntityClasses
                .SelectMany(x => x.Properties)
                .Where(prop => prop.Type.PropTypeIsManyToOne() &&
                               prop.Attributes.Any(x => x.Name == "ManyToOneRequired") &&
                               prop.Type == nameOfTheEntityClass)
                .ToList();
        }

        /// <summary>
        /// FT: Without inherited
        /// </summary>
        public static List<SoftProperty> GetPropsOfCurrentClass(ClassDeclarationSyntax c)
        {
            List<SoftProperty> properties = c.Members.OfType<PropertyDeclarationSyntax>()
                .Select(prop => new SoftProperty()
                {
                    Type = prop.Type.ToString(),
                    IdentifierText = prop.Identifier.Text,
                    ClassIdentifierText = c.Identifier.Text,
                    Attributes = prop.AttributeLists.SelectMany(x => x.Attributes).Select(x =>
                    {
                        return GetSoftAttribute(x);
                    })
                    .ToList()
                })
                .ToList();

            return properties;
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
            return classes.Where(x => x.Identifier.Text == typeName).SingleOrDefault();
        }

        public static ClassDeclarationSyntax GetClass(string type, IEnumerable<ClassDeclarationSyntax> classes)
        {
            return classes.Where(x => x.Identifier.Text == type).SingleOrDefault();
        }

        public static SoftProperty GetPropWithModifiedT(PropertyDeclarationSyntax prop, TypeSyntax typeGeneric)
        {
            List<SoftAttribute> attributes = GetAllAttributesOfTheMember(prop);
            SoftProperty newProp = new SoftProperty() { Type = prop.Type.ToString(), IdentifierText = prop.Identifier.Text, Attributes = attributes };

            if (prop.Type.ToString() == "T") // If some property has type of T, we change it to long for example
            {
                newProp.Type = typeGeneric.ToString();
                return newProp;
            }

            return newProp;
        }

        public static List<SoftProperty> GetPropertiesForBaseClasses(string typeName, string idType)
        {
            if (typeName.StartsWith($"{BusinessObject}"))
            {
                if (typeName.Contains("DTO"))
                {
                    return new List<SoftProperty>()
                    {
                        new SoftProperty{ Type = "int?", IdentifierText = "Version" },
                        new SoftProperty{ Type = idType, IdentifierText = "Id" },
                        new SoftProperty{ Type = "DateTime?", IdentifierText = "CreatedAt" },
                        new SoftProperty{ Type = "DateTime?", IdentifierText = "ModifiedAt" },
                    };
                }
                else
                {
                    return new List<SoftProperty>()
                    {
                        new SoftProperty{ Type = "int", IdentifierText = "Version" },
                        new SoftProperty{ Type = idType, IdentifierText = "Id" },
                        new SoftProperty{ Type = "DateTime", IdentifierText = "CreatedAt" },
                        new SoftProperty{ Type = "DateTime", IdentifierText = "ModifiedAt" },
                    };
                }
            }
            else if (typeName.StartsWith($"{ReadonlyObject}"))
            {
                if (typeName.Contains("DTO"))
                {
                    return new List<SoftProperty>()
                    {
                        new SoftProperty { Type = idType, IdentifierText = "Id" },
                        //new SoftProperty { Type = "DateTime?", IdentifierText = "CreatedAt" },
                    };
                }
                else
                {
                    return new List<SoftProperty>()
                    {
                        new SoftProperty { Type = idType, IdentifierText = "Id" },
                        //new SoftProperty { Type = "DateTime", IdentifierText = "CreatedAt" },
                    };
                }
            }
            else if (typeName.StartsWith($"TableSelectionDTO")) // TODO FT: Put inside variable
            {
                return new List<SoftProperty>()
                {
                    new SoftProperty { Type = $"TableFilterDTO", IdentifierText = "TableFilter" },
                    new SoftProperty { Type = $"List<{idType}>", IdentifierText = "SelectedIds" },
                    new SoftProperty { Type = $"List<{idType}>", IdentifierText = "UnselectedIds" },
                    new SoftProperty { Type = "bool?", IdentifierText = "IsAllSelected" },
                };
            }
            else
            {
                return new List<SoftProperty>() { };
            }
        }

        public static List<string> GetBaseTypeNames(ClassDeclarationSyntax c)
        {
            List<string> baseTypeNames = c.BaseList?.Types.Select(x => x.Type.ToString()).ToList();

            if (baseTypeNames == null)
                return new List<string>();
            else
                return baseTypeNames;
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


        public static ClassDeclarationSyntax ExtractEntityFromList(string input, IList<ClassDeclarationSyntax> entityClasses)
        {
            string[] parts = input.Split('<'); // List, Role>
            string entityClassName = parts[1].Replace(">", "");

            ClassDeclarationSyntax entityClass = GetClass(entityClassName, entityClasses);

            return entityClass;
        }

        /// <summary>
        /// DeSerializes an object from JSON
        /// </summary>
        public static T DeserializeJson<T>(string json) where T : class
        {
            using (var stream = new MemoryStream(Encoding.Default.GetBytes(json)))
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                return serializer.ReadObject(stream) as T;
            }
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


