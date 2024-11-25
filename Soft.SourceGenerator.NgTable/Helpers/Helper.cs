using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Soft.SourceGenerator.NgTable.Angular;
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
        public static readonly List<string> BaseClassNames = new List<string> 
        { 
            "TableFilter", 
            "TableResponse", 
            "TableSelection", 
            "Namebook", 
            "Codebook", 
            "SimpleSaveResult", 
            "BusinessObject", 
            "ReadonlyObject", 
            "ExcelReportOptions", 
            "RoleUser", 
            "PaginationResult", 
            "TableFilterContext", 
            "TableFilterSortMeta" 
        };

        #region Syntax and Semantic targets

        public static bool IsSyntaxTargetForGenerationSettings(SyntaxNode node)
        {
            if (node is ClassDeclarationSyntax classDeclaration)
            {
                string namespaceName = classDeclaration
                   .Ancestors()
                   .OfType<NamespaceDeclarationSyntax>()
                   .Select(ns => ns.Name.ToString())
                   .FirstOrDefault();

                if (namespaceName != null && namespaceName.EndsWith($".GeneratorSettings"))
                    return true;
            }

            return false;
        }

        public static ClassDeclarationSyntax GetSemanticTargetForGenerationSettings(GeneratorSyntaxContext context)
        {
            ClassDeclarationSyntax classDeclaration = (ClassDeclarationSyntax)context.Node;

            string namespaceName = classDeclaration
               .Ancestors()
               .OfType<NamespaceDeclarationSyntax>()
               .Select(ns => ns.Name.ToString())
               .FirstOrDefault();

            if (namespaceName != null && namespaceName.EndsWith($".GeneratorSettings"))
            {
                return classDeclaration;
            }

            return null;
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

        public static bool IsSyntaxTargetForGenerationDTO(SyntaxNode node)
        {
            if (node is ClassDeclarationSyntax classDeclaration)
            {
                string namespaceName = classDeclaration
                   .Ancestors()
                   .OfType<NamespaceDeclarationSyntax>()
                   .Select(ns => ns.Name.ToString())
                   .FirstOrDefault();

                if (namespaceName != null && (namespaceName.EndsWith($".{DTONamespaceEnding}") || namespaceName.EndsWith($".{EntitiesNamespaceEnding}") || namespaceName.EndsWith(".GeneratorSettings")))
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

            if (namespaceName != null && (namespaceName.EndsWith($".{DTONamespaceEnding}") || namespaceName.EndsWith($".{EntitiesNamespaceEnding}") || namespaceName.EndsWith(".GeneratorSettings")))
                return classDeclaration;

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

                if (namespaceName != null && (namespaceName.EndsWith($".{DTONamespaceEnding}") || namespaceName.EndsWith($".{EntitiesNamespaceEnding}") || namespaceName.EndsWith(".GeneratorSettings")))
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

            if (namespaceName != null && (namespaceName.EndsWith($".{DTONamespaceEnding}") || namespaceName.EndsWith($".{EntitiesNamespaceEnding}") || namespaceName.EndsWith(".GeneratorSettings")))
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
                    if (namespaceName.EndsWith($".{DTONamespaceEnding}") || namespaceName.EndsWith($".{EntitiesNamespaceEnding}") || namespaceName.EndsWith(".DataMappers"))
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
                if (namespaceName.EndsWith($".{DTONamespaceEnding}") || namespaceName.EndsWith($".{EntitiesNamespaceEnding}") || namespaceName.EndsWith(".DataMappers"))
                    return classDeclaration;
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
                    if (namespaceName.EndsWith($".{EntitiesNamespaceEnding}") || namespaceName.EndsWith(".DataMappers") || namespaceName.EndsWith(".GeneratorSettings"))
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
                if (namespaceName.EndsWith($".{EntitiesNamespaceEnding}") || namespaceName.EndsWith(".DataMappers") || namespaceName.EndsWith(".GeneratorSettings"))
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
                    if (namespaceName.EndsWith(".Controllers") || namespaceName.EndsWith(".GeneratorSettings"))
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
                if (namespaceName.EndsWith(".Controllers") || namespaceName.EndsWith(".GeneratorSettings"))
                {
                    return classDeclaration;
                }
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

                if (namespaceName != null && (namespaceName.EndsWith(".Enums") || namespaceName.EndsWith(".GeneratorSettings")))
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

            if (namespaceName != null && (namespaceName.EndsWith(".Enums") || namespaceName.EndsWith(".GeneratorSettings")))
            {
                return enumDeclaration;
            }

            return null;
        }

        #endregion

        #region Referenced Assemblies

        public static IncrementalValueProvider<List<SoftClass>> GetEntityClassesFromReferencedAssemblies(IncrementalGeneratorInitializationContext context)
        {
            return context.CompilationProvider
                .Select(static (compilation, _) =>
                {
                    List<SoftClass> classes = new List<SoftClass>();

                    foreach (IAssemblySymbol referencedAssembly in compilation.SourceModule.ReferencedAssemblySymbols
                             .Where(a => a.Name.Contains("Soft") || a.Name.Contains("Playerty")))
                    {
                        classes.AddRange(GetEntityClassesFromReferencedAssemblies(referencedAssembly.GlobalNamespace));
                    }

                    return classes;
                });
        }

        public static IncrementalValueProvider<List<SoftClass>> GetDTOClassesFromReferencedAssemblies(IncrementalGeneratorInitializationContext context)
        {
            return context.CompilationProvider
                .Select(static (compilation, _) =>
                {
                    List<SoftClass> classes = new List<SoftClass>();

                    foreach (IAssemblySymbol referencedAssembly in compilation.SourceModule.ReferencedAssemblySymbols
                             .Where(a => a.Name.Contains("Soft") || a.Name.Contains("Playerty")))
                    {
                        classes.AddRange(GetDTOClassesFromReferencedAssemblies(referencedAssembly.GlobalNamespace));
                    }

                    return classes;
                });
        }

        private static List<SoftClass> GetEntityClassesFromReferencedAssemblies(INamespaceSymbol namespaceSymbol)
        {
            List<SoftClass> classes = new List<SoftClass>();
            FillClassesFromReferencedAssemblies(namespaceSymbol, classes);
            return classes.Where(x => x.Namespace.EndsWith(".Entities")).ToList();
        }

        private static List<SoftClass> GetDTOClassesFromReferencedAssemblies(INamespaceSymbol namespaceSymbol)
        {
            List<SoftClass> classes = new List<SoftClass>();
            FillClassesFromReferencedAssemblies(namespaceSymbol, classes);
            return classes.Where(x => x.Namespace.EndsWith(".DTO")).ToList();
        }

        private static void FillClassesFromReferencedAssemblies(INamespaceSymbol namespaceSymbol, List<SoftClass> classes)
        {
            // Add all the type members (classes, structs, etc.) in this namespace
            foreach (INamedTypeSymbol type in namespaceSymbol.GetTypeMembers())
            {
                if (type.TypeKind == TypeKind.Class)
                {
                    SoftClass softClass = new SoftClass
                    {
                        Name = type.Name,
                        Namespace = GetFullNamespace(type),
                        BaseType = type.BaseType?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                        IsAbstract = type.IsAbstract,
                        Properties = GetPropertiesFromReferencedAssemblies(type),
                        Attributes = GetAttributesFromReferencedAssemblies(type),
                        Methods = GetMethodsOfCurrentClassFromReferencedAssemblies(type),
                    };

                    classes.Add(softClass);
                }
            }

            // Recursively gather classes from nested namespaces
            foreach (INamespaceSymbol nestedNamespace in namespaceSymbol.GetNamespaceMembers())
            {
                FillClassesFromReferencedAssemblies(nestedNamespace, classes);
            }
        }

        private static string GetFullNamespace(INamedTypeSymbol symbol)
        {
            Stack<string> namespaces = new Stack<string>();
            INamespaceSymbol currentNamespace = symbol.ContainingNamespace;

            while (currentNamespace != null && !currentNamespace.IsGlobalNamespace)
            {
                namespaces.Push(currentNamespace.Name);
                currentNamespace = currentNamespace.ContainingNamespace;
            }

            return string.Join(".", namespaces);
        }

        private static List<SoftProperty> GetPropertiesFromReferencedAssemblies(INamedTypeSymbol type)
        {
            List<SoftProperty> properties = new List<SoftProperty>();

            while (type != null)
            {
                foreach (ISymbol member in type.GetMembers())
                {
                    if (member is IPropertySymbol property)
                    {
                        SoftProperty softProperty = new SoftProperty
                        {
                            IdentifierText = member.Name,
                            ClassIdentifierText = type.Name,
                            Type = property.Type.ToString(),
                            Attributes = GetAttributesFromReferencedAssemblies(member)
                        };

                        properties.Add(softProperty);
                    }
                }

                type = type.BaseType;
            }

            return properties;
        }

        private static List<SoftAttribute> GetAttributesFromReferencedAssemblies(ISymbol symbol)
        {
            List<SoftAttribute> attributes = new List<SoftAttribute>();

            foreach (AttributeData attribute in symbol.GetAttributes())
            {
                string argumentValue = attribute.ConstructorArguments.Length > 0
                    ? 
                    string.Join(", ", attribute.ConstructorArguments.Select(arg =>
                    {
                        try
                        {
                            return arg.Value?.ToString();
                        }
                        catch (Exception)
                        {
                            return arg.Values.FirstOrDefault().Value?.ToString();
                        }
                    }))
                    : null; // FT: Doing this because of Range(0, 5) (long tail because of null pointer exception)

                argumentValue = GetFormatedAttributeValue(argumentValue);

                SoftAttribute softAttribute = new SoftAttribute
                {
                    Name = attribute.AttributeClass.Name?.Replace("Attribute", ""),
                    Value = argumentValue
                };

                attributes.Add(softAttribute);
            }

            return attributes;
        }

        /// <summary>
        /// Cant get method Body and method DescendantNodes from referenced assemblies
        /// </summary>
        private static List<SoftMethod> GetMethodsOfCurrentClassFromReferencedAssemblies(INamedTypeSymbol type)
        {
            List<SoftMethod> methods = new List<SoftMethod>();

            foreach (ISymbol member in type.GetMembers())
            {
                if (member is IMethodSymbol method)
                {
                    SoftMethod softMethod = new SoftMethod
                    {
                        Name = member.Name,
                        ReturnType = method.ReturnType.ToString(),
                        Attributes = GetAttributesFromReferencedAssemblies(member),
                    };

                    methods.Add(softMethod);
                }
            }

            return methods;
        }

        #endregion

        #region Class list filters

        public static ClassDeclarationSyntax GetSettingsClass(IList<ClassDeclarationSyntax> classes)
        {
            return classes
                .Where(x => x.Ancestors()
                    .OfType<NamespaceDeclarationSyntax>()
                    .Select(ns => ns.Name.ToString())
                    .Any(ns => ns.EndsWith($".GeneratorSettings")))
                .SingleOrDefault();
        }

        public static string GetGeneratorOutputPath(string generatorName, IList<ClassDeclarationSyntax> classes)
        {
            ClassDeclarationSyntax settingsClass = GetSettingsClass(classes);
            if (settingsClass == null) return null;
            List<SoftProperty> properties = GetAllPropertiesOfTheClass(settingsClass, classes);
            SoftProperty p = properties?.Where(x => x.IdentifierText == generatorName)?.SingleOrDefault();
            string outputPath = p?.Attributes?.Where(x => x.Name == "Output")?.SingleOrDefault()?.Value;
            return outputPath;
        }

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
            return GetSoftClasses(classes)
                .Where(x => x.Namespace.EndsWith(".Entities"))
                .ToList();
        }

        public static List<SoftClass> GetDTOClasses(List<SoftClass> classes)
        {
            return classes
                .Where(x => x.Namespace.EndsWith($".{EntitiesNamespaceEnding}") || x.Namespace.EndsWith($".{DTONamespaceEnding}"))
                .Select(x =>
                {
                    if (x.Name.EndsWith("DTO"))
                    {
                        return new SoftClass
                        {
                            Name = x.Name,
                            Properties = x.Properties
                        };
                    }
                    else // Entity
                    {
                        return new SoftClass
                        {
                            Name = $"{x.Name}DTO",
                            Properties = GetDTOSoftProps(x, classes),
                            IsGenerated = true
                        };
                    }
                })
                .ToList();
        }

        public static List<SoftClass> GetSoftClasses(IList<ClassDeclarationSyntax> classes)
        {
            return classes
                .Select(x =>
                {
                    return new SoftClass
                    {
                        Name = x.Identifier.Text,
                        Namespace = x.Ancestors()
                         .OfType<NamespaceDeclarationSyntax>()
                         .FirstOrDefault()?.Name.ToString(),
                        BaseType = x.GetBaseType(),
                        IsAbstract = x.IsAbstract(),
                        Properties = GetAllPropertiesOfTheClass(x, classes, true),
                        Attributes = GetAllAttributesOfTheClass(x, classes),
                        Methods = GetMethodsOfCurrentClass(x)
                    };
                })
                .ToList();
        }

        public static string GetTypeForTheClassAndPropName(SoftClass c, string propName)
        {
            return c.Properties.Where(x => x.IdentifierText == propName).Select(x => x.Type).Single();
        }

        public static List<SoftProperty> GetDTOSoftProps(SoftClass entityClass, List<SoftClass> entityClasses)
        {
            List<SoftProperty> props = new List<SoftProperty>(); // public string Email { get; set; }
            List<SoftProperty> properties = entityClass.Properties;

            foreach (SoftProperty prop in properties)
            {
                if (prop.Attributes.Any(x => x.Name == "IgnorePropertyInDTO"))
                    continue;

                string propType = prop.Type;
                string propName = prop.IdentifierText;
                // FT: Not adding attributes because they are not the same

                if (propType.PropTypeIsManyToOne())
                {
                    props.Add(new SoftProperty { IdentifierText = $"{propName}DisplayName", Type = "string" });
                    SoftClass manyToOneClass = entityClasses.Where(x => x.Name == propType).SingleOrDefault();
                    props.Add(new SoftProperty { IdentifierText = $"{propName}Id", Type = $"{GetGenericIdType(manyToOneClass, entityClasses)}?" });
                    continue;
                }
                else if (propType.IsEnumerable() && prop.Attributes.Any(x => x.Name == "GenerateCommaSeparatedDisplayName"))
                {
                    props.Add(new SoftProperty { IdentifierText = $"{propName}CommaSeparated", Type = "string" });
                    continue;
                }
                else if (propType == "byte[]")
                {
                    props.Add(new SoftProperty { IdentifierText = propName, Type = "string" });
                    continue;
                }
                else if (propType.IsEnumerable())
                {
                    continue;
                }
                else if (propType.IsBaseType() && propType != "string")
                {
                    propType = $"{prop.Type}?".Replace("??", "?");
                }
                else if (prop.Attributes.Any(x => x.Name == "BlobName"))
                {
                    props.Add(new SoftProperty { IdentifierText = $"{propName}Data", Type = "string" });
                }
                else if (propType != "string")
                {
                    propType = "UNSUPPORTED TYPE";
                }

                props.Add(new SoftProperty { IdentifierText = propName, Type = propType });
            }

            return props;
        }

        public static List<ClassDeclarationSyntax> GetValidationClasses(IList<ClassDeclarationSyntax> classes)
        {
            return classes
                .Where(x => x.Ancestors()
                    .OfType<NamespaceDeclarationSyntax>()
                    .Select(ns => ns.Name.ToString())
                    .Any(ns => ns.EndsWith($".{ValidationNamespaceEnding}")))
                .ToList();
        }

        public static List<ClassDeclarationSyntax> GetControllerClasses(IList<ClassDeclarationSyntax> classes)
        {
            return classes
                .Where(x => x.Ancestors()
                    .OfType<NamespaceDeclarationSyntax>()
                    .Select(ns => ns.Name.ToString())
                    .Any(ns => ns.EndsWith($".Controllers")))
                .ToList();
        }

        #endregion

        #region Angular

        /// <summary>
        /// Pass the properties with the C# data types
        /// </summary>
        public static List<string> GetAngularImports(List<SoftProperty> properties, string projectName = null, bool generateClassImports = false, string importPath = null)
        {
            List<string> result = new List<string>();

            foreach (SoftProperty prop in properties)
            {
                string cSharpDataType = prop.Type;
                if (cSharpDataType.IsBaseType() == false)
                {
                    string angularDataType = GetAngularDataTypeForImport(cSharpDataType);

                    if (generateClassImports && cSharpDataType.Contains($"{DTONamespaceEnding}"))
                    {
                        result.Add($"import {{ {angularDataType} }} from \"./{importPath}{projectName.FromPascalToKebabCase()}-entities.generated\";");
                    }
                    else if (generateClassImports && cSharpDataType.Contains($"Codes"))
                    {
                        result.Add($"import {{ {angularDataType} }} from \"../../enums/generated/{importPath}{projectName.FromPascalToKebabCase()}-enums.generated\";"); // TODO FT: When you need, implement so you can also send enums from the controller
                    }
                }
            }

            return result.Distinct().ToList();
        }

        public static string GetAngularType(string cSharpType)
        {
            switch (cSharpType)
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

            if (cSharpType.IsEnumerable())
                return $"{ExtractAngularTypeFromGenericCSharpType(cSharpType)}[]";

            if (cSharpType.EndsWith("Codes") || cSharpType.EndsWith("Codes>")) // Enum
                return cSharpType;

            if (cSharpType.EndsWith("MimeTypes") || cSharpType.EndsWith("MimeTypes>"))
                return cSharpType;

            if (cSharpType.Contains(DTONamespaceEnding) || (cSharpType.Contains("Task<") && cSharpType.Contains("ActionResult") == false)) // FT: We don't want to handle "ActionResult"
                return ExtractAngularTypeFromGenericCSharpType(cSharpType); // ManyToOne

            return "any"; // eg. "ActionResult", "Task"...
        }

        #region Helpers

        private static string GetAngularDataTypeForImport(string CSharpDataType)
        {
            //if (ExtractAngularTypeFromGenericCSharpType(CSharpDataType).IsBaseType()) // TODO FT: We were checking for the C# type, which wasn't correct, but add correct code here if we need in the future
            //    return null;

            if (ExtractAngularTypeFromGenericCSharpType(CSharpDataType).EndsWith("Codes"))
                return CSharpDataType;

            return ExtractAngularTypeFromGenericCSharpType(CSharpDataType);
        }

        /// <summary>
        /// cSharp type could be enumerable or class
        /// List<long> -> number
        /// </summary>
        private static string ExtractAngularTypeFromGenericCSharpType(string cSharpType)
        {
            string result;

            string[] parts = cSharpType.Split('<'); // List, long>

            parts[parts.Length-1] = parts[parts.Length-1].Replace(">", ""); // long

            if (cSharpType.Contains("TableResponseDTO"))
            {
                result = "TableResponse";
            }
            else if (cSharpType.Contains("NamebookDTO"))
            {
                result = "Namebook";
            }
            else if (cSharpType.Contains("CodebookDTO"))
            {
                result = "Codebook";
            }
            else if (parts[parts.Length-1].IsBaseType())
            {
                result = GetAngularType(parts[parts.Length-1]); // List<long>
            }
            else
            {
                result = parts[parts.Length-1]; // List<UserDTO>
            }

            return result.Replace(DTONamespaceEnding, "").Replace("[]", "");
        }

        #endregion

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

        /// <summary>
        /// Using this method only when getting all properties of the class, for other situations, we should search SoftClass.
        /// </summary>
        private static ClassDeclarationSyntax GetClass(TypeSyntax type, IEnumerable<ClassDeclarationSyntax> classes)
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

        public static string GetGenericIdType(ClassDeclarationSyntax c, IList<ClassDeclarationSyntax> classes)
        {
            TypeSyntax baseType = c.BaseList?.Types.FirstOrDefault()?.Type; //BaseClass<long>

            while (baseType is not GenericNameSyntax && baseType != null)
            {
                if (baseType.ToString() == "Role")
                    return "int";

                ClassDeclarationSyntax baseClass = classes.Where(x => x.Identifier.Text == baseType.ToString()).SingleOrDefault();

                if (baseClass == null)
                    return null;

                baseType = baseClass.BaseList?.Types.FirstOrDefault()?.Type; //BaseClass<long>
            }

            if (baseType != null && baseType is GenericNameSyntax genericNameSyntax)
                return genericNameSyntax.TypeArgumentList.Arguments.FirstOrDefault().ToString(); // long
            else
                return null; // FT: It doesn't, many to many doesn't
                             //return "Every entity class needs to have the base class";
        }

        public static string GetGenericIdType(SoftClass c, List<SoftClass> classes)
        {
            if (c == null)
                return "TheClassDoesNotExist";

            string baseType = c.BaseType; //BaseClass<long>

            while (baseType != null && baseType.Contains("<") == false)
            {
                if (baseType.ToString() == "Role")
                    return "int";

                SoftClass baseClass = classes.Where(x => x.Name == baseType).SingleOrDefault();

                if (baseClass == null)
                    return null;

                baseType = baseClass.BaseType; //BaseClass<long>
            }

            if (baseType != null && baseType.Contains("<"))
                return baseType.Split('<')[1].Replace(">", ""); // long
            else
                return null; // FT: It doesn't, many to many doesn't
                             //return "Every entity class needs to have the base class";
        }

        public static string GetGenericBaseType(ClassDeclarationSyntax c)
        {
            TypeSyntax baseType = c.BaseList?.Types.Where(x => x.Type is GenericNameSyntax).FirstOrDefault()?.Type; //BaseClass<long>
            if (baseType != null)
                return baseType.ToString();
            else
                return null; // FT: It doesn't, many to many doesn't
                             //return "Every entity class needs to have the base class";
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

        public static List<SoftMethod> GetMethodsOfCurrentClass(ClassDeclarationSyntax c)
        {
            List<SoftMethod> methods = c.Members.OfType<MethodDeclarationSyntax>()
                .Select(method => new SoftMethod()
                {
                    Name = method.Identifier.Text,
                    ReturnType = method.ReturnType.ToString(),
                    Body = method.Body.ToString(),
                    DescendantNodes = method.DescendantNodes(),
                    Attributes = method.AttributeLists.SelectMany(x => x.Attributes).Select(x =>
                    {
                        return GetSoftAttribute(x);
                    })
                    .ToList()
                })
                .ToList();

            return methods;
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

        public static List<string> GetBaseTypeNames(ClassDeclarationSyntax c)
        {
            List<string> baseTypeNames = c.BaseList?.Types.Select(x => x.Type.ToString()).ToList();

            if (baseTypeNames == null)
                return new List<string>();
            else
                return baseTypeNames;
        }

        public static string GetDisplayNamePropForClass(ClassDeclarationSyntax c, IList<ClassDeclarationSyntax> classes)
        {
            List<SoftProperty> props = GetAllPropertiesOfTheClass(c, classes);
            SoftProperty displayNamePropForClass = props.Where(x => x.Attributes.Any(x => x.Name == DisplayNameAttribute)).SingleOrDefault();

            if (displayNamePropForClass == null)
                return $"Id.ToString()";

            if (displayNamePropForClass.Type != "string")
                return $"{displayNamePropForClass.IdentifierText}.ToString()";

            return displayNamePropForClass.IdentifierText;
        }

        public static string GetDisplayNamePropForClass(SoftClass c)
        {
            List<SoftProperty> props = c.Properties;
            SoftProperty displayNamePropForClass = props.Where(x => x.Attributes.Any(x => x.Name == DisplayNameAttribute)).SingleOrDefault();

            if (displayNamePropForClass == null)
                return $"Id.ToString()";

            if (displayNamePropForClass.Type != "string")
                return $"{displayNamePropForClass.IdentifierText}.ToString()";

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

        public static string[] GetNamespacePartsWithoutLastElement(string namespaceValue)
        {
            string[] namespaceParts = namespaceValue.Split('.');
            string[] namespacePartsWithoutLastElement = namespaceParts.Take(namespaceParts.Length - 1).ToArray();

            return namespacePartsWithoutLastElement; // eg. Soft, Generator, Security
        }

        public static List<SoftProperty> GetPropsToExcludeFromExcelExport(string className, IList<SoftClass> DTOClasses, ClassDeclarationSyntax mapperClass)
        {
            List<SoftProperty> DTOClassProperties = new List<SoftProperty>();

            // FT: I dont know why did i add here this, if im overriding it down.
            //List<SoftClass> pairDTOClasses = DTOClasses.Where(x => x.Name == className).ToList(); // There will be 2, partial generated and partial manual
            //foreach (SoftClass classDTO in pairDTOClasses) // It's only two here
            //    DTOClassProperties.AddRange(classDTO.Properties);

            MethodDeclarationSyntax excelMethod = mapperClass?.Members.OfType<MethodDeclarationSyntax>()
               .Where(x => x.ReturnType.ToString() == className && x.Identifier.ToString() == $"{MethodNameForExcelExportMapping}")
               .SingleOrDefault();

            IList<SoftAttribute> excludePropAttributes = new List<SoftAttribute>();

            DTOClassProperties = DTOClassProperties // excluding enumerables from the excel
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
                            string propNameInsideBrackets = attribute.ArgumentList.Arguments.FirstOrDefault().ToString().Split('.').Last().Replace(")", "").Replace("\"", "");
                            //excludePropAttributes.Add(new SoftAttribute() { Name = attribute.Name.ToString(), PropNameInsideBrackets = propNameInsideBrackets }); // FT: i don't need this if i don't know which prop type it is
                            DTOClassProperties.Add(new SoftProperty { IdentifierText = propNameInsideBrackets });
                        }
                    }
                }
            }

            return DTOClassProperties;
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
        /// List<long> -> long
        /// </summary>
        public static string ExtractTypeFromGenericType(string input)
        {
            string[] parts = input.Split('<'); // List, long>
            string result = parts[1].Replace(">", "");

            return result;
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

        #region Permissions

        public static List<SoftEnum> GetEnumMembers(EnumDeclarationSyntax enume)
        {
            List<SoftEnum> enumMembers = new List<SoftEnum>();
            foreach (EnumMemberDeclarationSyntax member in enume.Members)
            {
                string name = member.Identifier.Text;
                string value = member.EqualsValue != null ? member.EqualsValue.Value.ToString() : null;
                enumMembers.Add(new SoftEnum { Name = name, Value = value });
            }
            return enumMembers;
        }

        public static List<string> GetPermissionCodesForEntites(List<ClassDeclarationSyntax> entityClasses)
        {
            List<string> result = new List<string>();

            foreach (ClassDeclarationSyntax c in entityClasses)
            {
                // FT: Maybe continue on readonly properties
                string className = c.Identifier.Text;
                result.Add($"Read{className}");
                result.Add($"Edit{className}");
                result.Add($"Insert{className}");
                result.Add($"Delete{className}");
            }

            if (entityClasses.Select(x => x.Identifier.Text).Contains("UserExtended") == false) // FT: Hack for security project
            {
                result.Add($"ReadUserExtended");
                result.Add($"EditUserExtended");
                result.Add($"InsertUserExtended");
                result.Add($"DeleteUserExtended");
            }

            if (entityClasses.Select(x => x.Identifier.Text).Contains("Role") == false) // FT: Hack for other projects
            {
                result.Add($"ReadRole");
                result.Add($"EditRole");
                result.Add($"InsertRole");
                result.Add($"DeleteRole");
            }

            //if (entityClasses.Select(x => x.Identifier.Text).Contains("Notification") == false) // FT: Hack for other projects
            //{
            //    result.Add($"ReadNotification");
            //    result.Add($"EditNotification");
            //    result.Add($"InsertNotification");
            //    result.Add($"DeleteNotification");
            //}

            return result;
        }

        #endregion

        #region Mapper

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

                    List<SoftAttribute> classAttributes = GetAllAttributesOfTheClass(x, classes);

                    bool hasCustomMapperAttribute = classAttributes.Any(x => x.Name == "CustomMapper");

                    if (namespaceName != null && hasCustomMapperAttribute)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                })
                .SingleOrDefault(); // FT: It should allways be only one

            return mapperClass;
        }

        #endregion

        #region Blobs

        public static List<SoftProperty> GetBlobProperties(SoftClass c)
        {
            return c.Properties.Where(x => x.Attributes.Any(x => x.Name == "BlobName")).ToList();
        }

        public static List<SoftProperty> GetBlobProperties(List<SoftProperty> properties)
        {
            return properties.Where(x => x.Attributes.Any(x => x.Name == "BlobName")).ToList();
        }

        #endregion

        #region Helpers

        private static SoftAttribute GetSoftAttribute(AttributeSyntax a)
        {
            string argumentValue = a?.ArgumentList?.Arguments != null && a.ArgumentList.Arguments.Any()
                    ? string.Join(", ", a.ArgumentList.Arguments.Select(arg => arg?.ToString()))
                    : null; ; // FT: Doing this because of Range(0, 5) (long tail because of null pointer exception)

            argumentValue = GetFormatedAttributeValue(argumentValue);

            return new SoftAttribute
            {
                Name = a.Name.ToString(),
                Value = argumentValue,
            };
        }

        private static string GetFormatedAttributeValue(string value)
        {
            value = value?.Replace("\"", "").Replace("@", "");

            string pattern = @"nameof\(([^)]*)\)";
            value = value != null ? Regex.Replace(value, pattern, "$1") : null;

            return value;
        }

        private static SoftProperty GetPropWithModifiedT(PropertyDeclarationSyntax prop, TypeSyntax typeGeneric)
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

        #endregion

        #region Populate hacks

        // FT HACK, FT TODO: Make this with all project references
        private static List<SoftProperty> GetRoleProperties()
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

        private static List<SoftProperty> GetRoleDTOProperties()
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

        private static List<SoftAttribute> GetAllAttributesOfTheMember(MemberDeclarationSyntax prop)
        {
            List<SoftAttribute> softAttributes = new List<SoftAttribute>();
            softAttributes = prop.AttributeLists.SelectMany(x => x.Attributes).Select(x =>
            {
                return GetSoftAttribute(x);
            })
            .ToList();
            return softAttributes;
        }

        // FT: Maybe ill need it in the future, for now im using only for the current class
        //private static List<SoftMethod> GetAllMethodsOfTheClass(ClassDeclarationSyntax c, IEnumerable<ClassDeclarationSyntax> allClasses,)
        //{
        //    TypeSyntax baseType = c.BaseList?.Types.FirstOrDefault()?.Type; //BaseClass<long>
        //    ClassDeclarationSyntax baseClass = GetClass(baseType, allClasses);

        //    string s = c.Identifier.Text;

        //    List<SoftMethod> properties = GetMethodsOfCurrentClass(c);

        //    TypeSyntax typeGeneric = null;

        //    while (baseType != null)
        //    {
        //        baseType = baseClass.BaseList?.Types.FirstOrDefault()?.Type;
        //        baseClass = GetClass(baseType, allClasses);
        //    }

        //    return properties;
        //}

        private static List<SoftProperty> GetPropertiesForBaseClasses(string typeName, string idType)
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

        #endregion
    }
}


