using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Spider.SourceGenerators.Angular;
using Spider.SourceGenerators.Enums;
using Spider.SourceGenerators.Models;
using Spider.SourceGenerators.Shared;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Resources.NetStandard;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Spider.SourceGenerators.Shared
{
    public static class Helpers
    {
        public static readonly string DisplayNameAttribute = "DisplayName";
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
            "LazyTableSelection",
            "Namebook",
            "Codebook",
            "SimpleSaveResult",
            "BusinessObject",
            "ReadonlyObject",
            "ExcelReportOptions",
            "UserRole",
            "PaginationResult",
            "TableFilterContext",
            "TableFilterSortMeta",
            "LazyLoadSelectedIdsResult",
            "BusinessObjectCodebook", // Nucleus
            "BusinessObjectNamebook", // Nucleus
        };

        #region Source Generator

        /// <summary>
        /// Getting all properties of the single class <paramref name="c"/>, including inherited ones.
        /// The inherited properties doesn't have any attributes
        /// </summary>
        public static List<SpiderProperty> GetAllPropertiesOfTheClass(ClassDeclarationSyntax c, IList<ClassDeclarationSyntax> currentProjectClasses, List<SpiderClass> referencedProjectsClasses)
        {
            TypeSyntax baseType = c.BaseList?.Types.FirstOrDefault()?.Type; //BaseClass<long>
            ClassDeclarationSyntax baseClass = GetClass(baseType, currentProjectClasses);

            List<SpiderProperty> properties = GetPropsOfCurrentClass(c);

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
                    SpiderClass spiderBaseClass = referencedProjectsClasses.Where(x => x.Name == c.Identifier.Text).SingleOrDefault();

                    if (spiderBaseClass != null)
                        properties.AddRange(spiderBaseClass.Properties);

                    break;
                }
                else
                {
                    foreach (PropertyDeclarationSyntax prop in baseClass.Members.OfType<PropertyDeclarationSyntax>())
                    {
                        properties.Add(GetPropWithModifiedT(prop, typeGeneric, baseClass));
                    }
                }

                baseType = baseClass.BaseList?.Types.FirstOrDefault()?.Type;
                baseClass = GetClass(baseType, currentProjectClasses);
            }

            return properties;
        }

        public static List<SpiderAttribute> GetAllAttributesOfTheClass(ClassDeclarationSyntax c, IList<ClassDeclarationSyntax> currentProjectClasses, List<SpiderClass> allClasses)
        {
            if (c == null) return null;

            ClassDeclarationSyntax cHelper = SyntaxFactory.ClassDeclaration(c.Identifier).WithBaseList(c.BaseList).WithAttributeLists(c.AttributeLists); // FT: Doing this because of reference type, we don't want to change c
            List<SpiderAttribute> attributes = new List<SpiderAttribute>();

            TypeSyntax baseType = cHelper.BaseList?.Types.FirstOrDefault()?.Type; // BaseClass
            // FT: Getting the attributes for all base classes also
            do
            {
                attributes.AddRange(cHelper.AttributeLists.SelectMany(x => x.Attributes).Select(x =>
                {
                    return GetSpiderAttribute(x);
                })
                .ToList());

                cHelper = currentProjectClasses.Where(x => x.Identifier.Text == baseType?.ToString()).SingleOrDefault();

                if (baseType != null && cHelper == null)
                {
                    SpiderClass baseClass = allClasses.Where(x => x.Name == c.Identifier.Text || $"{x.Name}DTO" == c.Identifier.Text).SingleOrDefault();

                    if (baseClass != null)
                        attributes.AddRange(baseClass.Attributes);

                    break;
                }

                baseType = cHelper?.BaseList?.Types.FirstOrDefault()?.Type;
            }
            while (baseType != null);

            return attributes;
        }

        /// <summary>
        /// Using this method only when getting all properties of the class, for other situations, we should search SpiderClass.
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

        public static string GetIdType(ClassDeclarationSyntax c, IList<ClassDeclarationSyntax> classes)
        {
            if (c == null)
                return "GetIdType.TheClassDoesNotExist";

            TypeSyntax baseType = c.BaseList?.Types.FirstOrDefault()?.Type; //BaseClass<long>

            while (baseType is not GenericNameSyntax && baseType != null)
            {
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

        /// <summary>
        /// FT: Without inherited
        /// </summary>
        public static List<SpiderProperty> GetPropsOfCurrentClass(ClassDeclarationSyntax c)
        {
            List<SpiderProperty> properties = c.Members.OfType<PropertyDeclarationSyntax>()
                .Select(prop => new SpiderProperty()
                {
                    Type = prop.Type.ToString(),
                    Name = prop.Identifier.Text,
                    EntityName = c.Identifier.Text,
                    Attributes = prop.AttributeLists.SelectMany(x => x.Attributes).Select(x =>
                    {
                        return GetSpiderAttribute(x);
                    })
                    .ToList()
                })
                .ToList();

            return properties;
        }

        public static List<SpiderMethod> GetMethodsOfCurrentClass(ClassDeclarationSyntax c)
        {
            List<SpiderMethod> methods = c.Members.OfType<MethodDeclarationSyntax>()
                .Select(method => new SpiderMethod()
                {
                    Name = method.Identifier.Text,
                    ReturnType = method.ReturnType.ToString(),
                    Body = method.Body?.ToString(), // FT: CreateHostBuilder method inside Program.cs has no body
                    Parameters = method.ParameterList.Parameters
                        .Select(parameter => new SpiderParameter
                        {
                            Name = parameter.Identifier.Text,
                            Type = parameter.Type.ToString(),
                            Attributes = parameter.AttributeLists.SelectMany(x => x.Attributes).Select(x => GetSpiderAttribute(x)).ToList()
                        })
                        .ToList(),
                    DescendantNodes = method.DescendantNodes(),
                    Attributes = method.AttributeLists.SelectMany(x => x.Attributes).Select(x => GetSpiderAttribute(x)).ToList()
                })
                .ToList();

            return methods;
        }

        public static string GetDisplayNamePropForClass(ClassDeclarationSyntax c, IList<ClassDeclarationSyntax> classes)
        {
            List<SpiderProperty> props = GetAllPropertiesOfTheClass(c, classes, new List<SpiderClass>());
            SpiderProperty displayNamePropForClass = props.Where(x => x.Attributes.Any(x => x.Name == DisplayNameAttribute)).SingleOrDefault();

            if (displayNamePropForClass == null)
                return $"Id.ToString()";

            if (displayNamePropForClass.Type != "string")
                return $"{displayNamePropForClass.Name}.ToString()";

            return displayNamePropForClass.Name;
        }

        public static string GetDisplayNameProperty(SpiderClass entity)
        {
            SpiderAttribute entityDisplayNameAttribute = entity.Attributes.Where(x => x.Name == "DisplayName").SingleOrDefault();

            if (entityDisplayNameAttribute != null)
                return entityDisplayNameAttribute.Value;

            List<SpiderProperty> props = entity.Properties;
            SpiderProperty displayNamePropForClass = props.Where(x => x.Attributes.Any(x => x.Name == DisplayNameAttribute)).SingleOrDefault();

            if (displayNamePropForClass == null)
                return $"Id.ToString()";

            if (displayNamePropForClass.Type != "string")
                return $"{displayNamePropForClass.Name}.ToString()";

            return displayNamePropForClass.Name;
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

            return namespacePartsWithoutLastElement; // eg. Spider, Generator, Security
        }

        public static string[] GetNamespacePartsWithoutLastElement(string namespaceValue)
        {
            string[] namespaceParts = namespaceValue.Split('.');
            string[] namespacePartsWithoutLastElement = namespaceParts.Take(namespaceParts.Length - 1).ToArray();

            return namespacePartsWithoutLastElement; // eg. Spider, Generator, Security
        }

        public static string GetBasePartOfNamespace(string namespaceValue)
        {
            return string.Join(".", GetNamespacePartsWithoutLastElement(namespaceValue));  // eg. Spider.Security
        }

        public static string GetProjectName(string namespaceValue)
        {
            string[] namespacePartsWithoutLastElement = GetNamespacePartsWithoutLastElement(namespaceValue);

            return namespacePartsWithoutLastElement[namespacePartsWithoutLastElement.Length - 1]; // eg. Security
        }

        public static List<SpiderProperty> GetManyToOneRequiredProperties(string entityName, List<SpiderClass> entities)
        {
            return entities
                .SelectMany(x => x.Properties)
                .Where(prop => prop.Type.IsManyToOneType() &&
                               prop.Attributes.Any(x => x.Name == "ManyToOneRequired") &&
                               prop.Type == entityName)
                .ToList();
        }

        /// <summary>
        /// List<long> -> long
        /// </summary>
        public static string ExtractTypeFromGenericType(string input)
        {
            if (input == null)
                return null;

            string[] parts = input.Split('<'); // List, long>
            string result = parts.Last().Replace(">", "");

            return result;
        }

        private static SpiderAttribute GetSpiderAttribute(AttributeSyntax a)
        {
            string argumentValue = a?.ArgumentList?.Arguments != null && a.ArgumentList.Arguments.Any()
                    ? string.Join(", ", a.ArgumentList.Arguments.Select(arg => arg?.ToString()))
                    : null; ; // FT: Doing this because of Range(0, 5) (long tail because of null pointer exception)

            argumentValue = GetFormatedAttributeValue(argumentValue);

            return new SpiderAttribute
            {
                Name = a.Name.ToString(),
                Value = argumentValue,
            };
        }

        private static string GetFormatedAttributeValue(string value)
        {
            value = value?.Replace("\"", "").Replace("@", "");

            string pattern = @"nameof\((?:[^.]*\.)?([^.)]*)\)"; // nameof(abc.def.ghi) => ghi
            value = value != null ? Regex.Replace(value, pattern, "$1") : null;

            return value;
        }

        private static SpiderProperty GetPropWithModifiedT(PropertyDeclarationSyntax prop, TypeSyntax typeGeneric, ClassDeclarationSyntax baseClass)
        {
            List<SpiderAttribute> attributes = GetAllAttributesOfTheMember(prop);
            SpiderProperty newProp = new SpiderProperty() { Type = prop.Type.ToString(), Name = prop.Identifier.Text, Attributes = attributes, EntityName = baseClass.Identifier.Text };

            if (prop.Type.ToString() == "T") // If some property has type of T, we change it to long for example
            {
                newProp.Type = typeGeneric.ToString();
                return newProp;
            }

            return newProp;
        }

        /// <summary>
        /// </summary>
        /// <param name="entity">Main entity from which we get one to many property</param>
        public static SpiderClass GetManyToManyEntityWithAttributeValue(string attributeValue, SpiderClass entity, List<SpiderClass> entities)
        {
            return entities
                .Where(x => x.BaseType == null && x.Properties
                    .Any(x => x.Type == entity.Name && x.Attributes
                        .Any(x => (x.Name == "M2MEntity" || x.Name == "M2MMaintanceEntity") && x.Value == attributeValue)))
                .SingleOrDefault();
        }

        public static SpiderProperty GetManyToManyPropertyWithAttributeValue(SpiderClass manyToManyEntity, string attributeValue)
        {
            if (manyToManyEntity == null)
                return null;

            return manyToManyEntity.Properties
                .Where(x => x.Attributes
                    .Any(x => (x.Name == "M2MEntity" || x.Name == "M2MMaintanceEntity") && x.Value == attributeValue))
                .SingleOrDefault();
        }

        public static SpiderProperty GetOppositeManyToManyProperty(SpiderProperty oneToManyProperty, SpiderClass extractedPropertyEntity, SpiderClass entity, List<SpiderClass> entities)
        {
            SpiderClass manyToManyEntity = GetManyToManyEntityWithAttributeValue(oneToManyProperty.Name, entity, entities);

            if (manyToManyEntity == null)
                return null;

            SpiderProperty manyToManyProperty = GetManyToManyPropertyWithAttributeValue(manyToManyEntity, oneToManyProperty.Name);
            SpiderProperty oppositeManyToManyProperty = null;

            if (manyToManyProperty.HasM2MMaintanceEntityAttribute())
            {
                oppositeManyToManyProperty = manyToManyEntity.Properties.Where(x => x.HasM2MEntityAttribute()).Single();
            }
            else if (manyToManyProperty.HasM2MEntityAttribute())
            {
                oppositeManyToManyProperty = manyToManyEntity.Properties.Where(x => x.HasM2MMaintanceEntityAttribute()).Single();
            }

            string propertyName = oppositeManyToManyProperty.Attributes.Where(x => x.Name == "M2MMaintanceEntity" || x.Name == "M2MEntity").Select(x => x.Value).SingleOrDefault();

            return extractedPropertyEntity.Properties.Where(x => x.Name == propertyName).SingleOrDefault();
        }

        #endregion

        #region Syntax and Semantic targets

        public static IncrementalValuesProvider<ClassDeclarationSyntax> GetClassInrementalValuesProvider(SyntaxValueProvider syntaxValueProvider, List<NamespaceExtensionCodes> namespaceExtensions)
        {
            return syntaxValueProvider
                .CreateSyntaxProvider(
                   predicate: (s, _) => IsClassSyntaxTargetForGeneration(s, namespaceExtensions),
                   transform: (ctx, _) => GetClassSemanticTargetForGeneration(ctx, namespaceExtensions))
                .Where(static c => c is not null);
        }

        public static bool IsClassSyntaxTargetForGeneration(SyntaxNode node, List<NamespaceExtensionCodes> namespaceExtensions)
        {
            if (node is ClassDeclarationSyntax classDeclaration)
            {
                string namespaceName = classDeclaration.GetNamespace();

                if (namespaceName != null && (namespaceExtensions.Any(namespaceExtension => namespaceName.EndsWith($".{namespaceExtension}")) || namespaceName.EndsWith($".GeneratorSettings")))
                    return true;
            }

            return false;
        }

        public static ClassDeclarationSyntax GetClassSemanticTargetForGeneration(GeneratorSyntaxContext context, List<NamespaceExtensionCodes> namespaceExtensions)
        {
            ClassDeclarationSyntax classDeclaration = (ClassDeclarationSyntax)context.Node;

            string namespaceName = classDeclaration.GetNamespace();

            if (namespaceName != null && (namespaceExtensions.Any(namespaceExtension => namespaceName.EndsWith($".{namespaceExtension}")) || namespaceName.EndsWith($".GeneratorSettings")))
                return classDeclaration;

            return null;
        }

        public static bool IsEnumSyntaxTargetForGeneration(SyntaxNode node)
        {
            if (node is EnumDeclarationSyntax enumDeclaration)
            {
                string namespaceName = enumDeclaration.GetNamespace();

                if (namespaceName != null && (namespaceName.EndsWith(".Enums") || namespaceName.EndsWith(".GeneratorSettings")))
                    return true;
            }

            return false;
        }

        public static EnumDeclarationSyntax GetEnumSemanticTargetForGeneration(GeneratorSyntaxContext context)
        {
            EnumDeclarationSyntax enumDeclaration = (EnumDeclarationSyntax)context.Node;

            string namespaceName = enumDeclaration
               .Ancestors()
               .OfType<NamespaceDeclarationSyntax>()
               .Select(ns => ns.Name.ToString())
               .FirstOrDefault();

            if (namespaceName != null && (namespaceName.EndsWith(".Enums") || namespaceName.EndsWith(".GeneratorSettings")))
                return enumDeclaration;

            return null;
        }

        public static bool IsSyntaxTargetForGenerationEveryClass(SyntaxNode node)
        {
            if (node is ClassDeclarationSyntax classDeclaration)
            {
                string namespaceName = classDeclaration.GetNamespace();

                if (namespaceName != null)
                    return true;
            }

            return false;
        }

        public static ClassDeclarationSyntax GetSemanticTargetForGenerationEveryClass(GeneratorSyntaxContext context)
        {
            ClassDeclarationSyntax classDeclaration = (ClassDeclarationSyntax)context.Node;

            string namespaceName = classDeclaration.GetNamespace();

            if (namespaceName != null)
                return classDeclaration;

            return null;
        }

        #endregion

        #region Referenced Assemblies

        public static IncrementalValueProvider<List<SpiderClass>> GetIncrementalValueProviderClassesFromReferencedAssemblies(IncrementalGeneratorInitializationContext context, List<NamespaceExtensionCodes> namespaceExtensions)
        {
            return context.CompilationProvider
                .Select((compilation, _) =>
                {
                    List<SpiderClass> classes = new List<SpiderClass>();

                    foreach (IAssemblySymbol referencedAssembly in compilation.SourceModule.ReferencedAssemblySymbols)
                    {
                        classes.AddRange(GetClassesFromReferencedAssemblies(referencedAssembly.GlobalNamespace, namespaceExtensions));
                    }

                    return classes;
                });
        }

        private static List<SpiderClass> GetClassesFromReferencedAssemblies(INamespaceSymbol namespaceSymbol, List<NamespaceExtensionCodes> namespaceExtensions)
        {
            List<SpiderClass> classes = new List<SpiderClass>();

            List<INamedTypeSymbol> types = namespaceSymbol.GetTypeMembers()
                .Where(type => type.TypeKind == TypeKind.Class &&
                       namespaceExtensions.Any(namespaceExtension => GetFullNamespace(type).EndsWith($".{namespaceExtension}")))
                .ToList();

            // Add all the type members (classes, structs, etc.) in this namespace
            foreach (INamedTypeSymbol type in types)
            {
                List<SpiderAttribute> attributes = GetAttributesFromReferencedAssemblies(type);

                SpiderClass spiderClass = new SpiderClass
                {
                    Name = type.Name,
                    Namespace = GetFullNamespace(type),
                    BaseType = type.BaseType?.TypeToDisplayString() == "object" ? null : type.BaseType?.TypeToDisplayString(),
                    IsAbstract = type.IsAbstract,
                    ControllerName = attributes.Where(x => x.Name == "Controller").Select(x => x.Value).SingleOrDefault() ?? type.Name,
                    Properties = GetPropertiesFromReferencedAssemblies(type),
                    Attributes = attributes,
                    Methods = GetMethodsOfCurrentClassFromReferencedAssemblies(type),
                };

                classes.Add(spiderClass);
            }

            // Recursively gather classes from nested namespaces
            foreach (INamespaceSymbol nestedNamespace in namespaceSymbol.GetNamespaceMembers())
            {
                classes.AddRange(GetClassesFromReferencedAssemblies(nestedNamespace, namespaceExtensions));
            }

            return classes;
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

        private static List<SpiderProperty> GetPropertiesFromReferencedAssemblies(INamedTypeSymbol type)
        {
            List<SpiderProperty> properties = new List<SpiderProperty>();

            while (type != null)
            {
                foreach (ISymbol member in type.GetMembers())
                {
                    if (member is IPropertySymbol propertySymbol)
                    {
                        SpiderProperty property = new SpiderProperty
                        {
                            Name = member.Name,
                            EntityName = type.Name,
                            Type = propertySymbol.Type.TypeToDisplayString(),
                            Attributes = GetAttributesFromReferencedAssemblies(member)
                        };

                        properties.Add(property);
                    }
                }

                type = type.BaseType;
            }

            return properties;
        }

        private static List<SpiderAttribute> GetAttributesFromReferencedAssemblies(ISymbol symbol)
        {
            List<SpiderAttribute> attributes = new List<SpiderAttribute>();

            foreach (AttributeData attribute in symbol.GetAttributes())
            {
                string attributeName = attribute.AttributeClass.Name?.Replace("Attribute", "");

                string argumentValue = null;

                if (attribute.ConstructorArguments.Length > 0)
                {
                    if (attributeName == "StringLength")
                    {
                        List<string> parts = new List<string>
                        {
                            attribute.ConstructorArguments[0].Value?.ToString() // Max length
                        };

                        var minLengthArg = attribute.NamedArguments.FirstOrDefault(na => na.Key == "MinimumLength");

                        if (minLengthArg.Key != null)
                        {
                            parts.Add($"MinimumLength={minLengthArg.Value.Value}");
                        }

                        argumentValue = string.Join(", ", parts);
                    }
                    else
                    {
                        argumentValue = attribute.ConstructorArguments.Length > 0
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
                    }
                }

                argumentValue = GetFormatedAttributeValue(argumentValue);

                SpiderAttribute spiderAttribute = new SpiderAttribute
                {
                    Name = attributeName,
                    Value = argumentValue
                };

                attributes.Add(spiderAttribute);
            }

            return attributes;
        }

        /// <summary>
        /// Cant get method Body and method DescendantNodes from referenced assemblies
        /// </summary>
        private static List<SpiderMethod> GetMethodsOfCurrentClassFromReferencedAssemblies(INamedTypeSymbol type)
        {
            List<SpiderMethod> methods = new List<SpiderMethod>();

            foreach (ISymbol member in type.GetMembers())
            {
                if (member is IMethodSymbol methodSymbol)
                {
                    SpiderMethod method = new SpiderMethod
                    {
                        Name = member.Name,
                        ReturnType = methodSymbol.ReturnType.ToString(),
                        Attributes = GetAttributesFromReferencedAssemblies(member),
                    };

                    methods.Add(method);
                }
            }

            return methods;
        }

        public static List<string> GetEntityClassesUsings(List<SpiderClass> referencedProjectEntityClasses)
        {
            List<string> namespaces = referencedProjectEntityClasses
                .Where(x => x.Namespace.EndsWith(".Entities"))
                .Select(x => $"using {x.Namespace};")
                .Distinct()
                .ToList();

            return namespaces;
        }

        public static List<string> GetDTOClassesUsings(List<SpiderClass> referencedProjectEntityClasses)
        {
            List<string> namespaces = referencedProjectEntityClasses
                .Where(x => x.Namespace.EndsWith(".Entities"))
                .Select(x => $"using {x.Namespace.Replace(".Entities", ".DTO")};")
                .Distinct()
                .ToList();

            return namespaces;
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

            if (settingsClass == null)
                return null;

            List<SpiderProperty> properties = GetAllPropertiesOfTheClass(settingsClass, classes, new List<SpiderClass>());
            SpiderProperty p = properties?.Where(x => x.Name == generatorName)?.SingleOrDefault();
            string outputPath = p?.Attributes?.Where(x => x.Name == "Output")?.SingleOrDefault()?.Value;
            return outputPath;
        }

        public static bool ShouldStartGenerator(string generatorName, IList<ClassDeclarationSyntax> classes)
        {
            ClassDeclarationSyntax settingsClass = GetSettingsClass(classes);

            if (settingsClass == null)
                return false;

            List<SpiderProperty> properties = GetAllPropertiesOfTheClass(settingsClass, classes, new List<SpiderClass>());
            SpiderProperty p = properties?.Where(x => x.Name == generatorName)?.SingleOrDefault();

            bool.TryParse(p?.Attributes?.Where(x => x.Name == "Output")?.SingleOrDefault()?.Value, out bool shouldStart);

            return shouldStart;
        }

        public static List<ClassDeclarationSyntax> GetEntityClasses(IList<ClassDeclarationSyntax> classes)
        {
            return classes
                .Where(x => x.Ancestors()
                    .OfType<NamespaceDeclarationSyntax>()
                    .Select(ns => ns.Name.ToString())
                    .Any(ns => ns.EndsWith($".{EntitiesNamespaceEnding}")))
                .OrderBy(x => x.Identifier.Text)
                .ToList();
        }

        public static List<SpiderClass> GetSpiderEntities(IList<ClassDeclarationSyntax> currentProjectClasses, List<SpiderClass> referencedProjectsClasses)
        {
            return GetSpiderClasses(currentProjectClasses, referencedProjectsClasses)
                .Where(x => x.Namespace.EndsWith(".Entities"))
                .ToList();
        }

        public static List<SpiderClass> GetDTOClasses(List<SpiderClass> currentProjectClasses, List<SpiderClass> allClasses)
        {
            List<SpiderClass> DTOList = new();

            foreach (var x in currentProjectClasses
                .Where(x => x.Namespace.EndsWith($".{EntitiesNamespaceEnding}") || x.Namespace.EndsWith($".{DTONamespaceEnding}"))
            )
            {
                if (x.Name.EndsWith("DTO") || x.Namespace.EndsWith(".DTO"))
                {
                    DTOList.Add(new SpiderClass
                    {
                        Name = x.Name,
                        Properties = x.Properties,
                        Attributes = x.Attributes,
                        BaseType = x.BaseType,
                        IsAbstract = x.IsAbstract,
                        Methods = x.Methods,
                        Namespace = x.Namespace,
                        IsGenerated = false,
                    });
                }
                else // Entity
                {
                    DTOList.Add(new SpiderClass
                    {
                        Name = $"{x.Name}DTO",
                        BaseType = x.GetDTOBaseType(),
                        Properties = GetSpiderDTOProperties(x, allClasses),
                        Namespace = x.Namespace.Replace(".Entities", ".DTO"),
                        IsGenerated = true
                    });
                    DTOList.Add(new SpiderClass
                    {
                        Name = $"{x.Name}SaveBodyDTO",
                        Properties = GetSaveBodyDTOProperties(x, allClasses),
                        Namespace = x.Namespace.Replace(".Entities", ".DTO"),
                        IsGenerated = true
                    });
                }
            }

            return DTOList;
        }

        private static List<SpiderProperty> GetSaveBodyDTOProperties(SpiderClass entity, List<SpiderClass> entities)
        {
            List<SpiderProperty> result = new List<SpiderProperty>();
            result.Add(new SpiderProperty { Name = $"{entity.Name}DTO", Type = $"{entity.Name}DTO", EntityName = $"{entity.Name}SaveBodyDTO" });

            foreach (SpiderProperty property in entity.Properties)
            {
                SpiderClass extractedEntity = entities.Where(x => x.Name == ExtractTypeFromGenericType(property.Type)).SingleOrDefault();
                string extractedEntityIdType = extractedEntity.GetIdType(entities);

                if (property.HasOrderedOneToManyAttribute())
                {
                    result.Add(new SpiderProperty { Name = $"{property.Name}DTO", Type = $"List<{extractedEntity.Name}DTO>", EntityName = $"{entity.Name}SaveBodyDTO" });
                }
                else if (
                    property.IsMultiSelectControlType() ||
                    property.IsMultiAutocompleteControlType())
                {
                    result.Add(new SpiderProperty { Name = $"Selected{property.Name}Ids", Type = $"List<{extractedEntityIdType}>", EntityName = $"{entity.Name}SaveBodyDTO" });
                }
                else if (property.HasSimpleManyToManyTableLazyLoadAttribute())
                {
                    result.Add(new SpiderProperty { Name = $"Selected{property.Name}Ids", Type = $"List<{extractedEntityIdType}>", EntityName = $"{entity.Name}SaveBodyDTO" });
                    result.Add(new SpiderProperty { Name = $"Unselected{property.Name}Ids", Type = $"List<{extractedEntityIdType}>", EntityName = $"{entity.Name}SaveBodyDTO" });
                    result.Add(new SpiderProperty { Name = $"AreAll{property.Name}Selected", Type = "bool?", EntityName = $"{entity.Name}SaveBodyDTO" });
                    result.Add(new SpiderProperty { Name = $"{property.Name}TableFilter", Type = "TableFilterDTO", EntityName = $"{entity.Name}SaveBodyDTO" });
                }
            }

            return result;
        }

        public static List<SpiderClass> GetSpiderClasses(IList<ClassDeclarationSyntax> currentProjectClasses, List<SpiderClass> referencedProjectsClasses)
        {
            return currentProjectClasses
                .Select(x =>
                {
                    return new SpiderClass
                    {
                        Name = x.Identifier.Text,
                        Namespace = x.Ancestors()
                         .OfType<NamespaceDeclarationSyntax>()
                         .FirstOrDefault()?.Name.ToString(),
                        BaseType = x.GetBaseType(),
                        IsAbstract = x.IsAbstract(),
                        Properties = GetAllPropertiesOfTheClass(x, currentProjectClasses, referencedProjectsClasses),
                        Attributes = GetAllAttributesOfTheClass(x, currentProjectClasses, referencedProjectsClasses),
                        Methods = GetMethodsOfCurrentClass(x),
                    };
                })
                .OrderBy(x => x.Name)
                .ToList();
        }

        public static string GetTypeForTheClassAndPropName(SpiderClass c, string propName)
        {
            return c.Properties.Where(x => x.Name == propName).Select(x => x.Type).Single();
        }

        public static List<SpiderProperty> GetSpiderDTOProperties(SpiderClass entity, List<SpiderClass> entities)
        {
            List<SpiderProperty> DTOProperties = new List<SpiderProperty>(); // public string Email { get; set; }

            foreach (SpiderProperty property in entity.Properties)
            {
                if (property.ShouldSkipPropertyInDTO())
                    continue;

                if (property.Type.IsManyToOneType())
                {
                    SpiderClass manyToOneClass = entities.Where(x => x.Name == property.Type).SingleOrDefault();

                    DTOProperties.Add(new SpiderProperty { Name = $"{property.Name}DisplayName", Type = "string", EntityName = $"{property.EntityName}DTO" });
                    DTOProperties.Add(new SpiderProperty { Name = $"{property.Name}Id", Type = $"{manyToOneClass.GetIdType(entities)}?", EntityName = $"{property.EntityName}DTO" });
                }
                else if (property.Type.IsOneToManyType() && property.HasGenerateCommaSeparatedDisplayNameAttribute())
                {
                    DTOProperties.Add(new SpiderProperty { Name = $"{property.Name}CommaSeparated", Type = "string", EntityName = $"{property.EntityName}DTO" });
                }
                else if (property.Type.IsOneToManyType() && property.HasIncludeInDTOAttribute())
                {
                    DTOProperties.Add(new SpiderProperty { Name = $"{property.Name}DTOList", Type = property.Type.Replace(">", "DTO>"), EntityName = $"{property.EntityName}DTO" });
                }
                else if (property.Type == "byte[]")
                {
                    DTOProperties.Add(new SpiderProperty { Name = property.Name, Type = "string", EntityName = $"{property.EntityName}DTO" });
                }
                else if (property.IsBlob())
                {
                    DTOProperties.Add(new SpiderProperty { Name = $"{property.Name}Data", Type = "string", EntityName = $"{property.EntityName}DTO" });
                    DTOProperties.Add(new SpiderProperty { Name = property.Name, Type = "string", EntityName = $"{property.EntityName}DTO" });
                }
                else
                {
                    DTOProperties.Add(new SpiderProperty { Name = property.Name, Type = GetFormatedDTOPropertyType(property.Type), EntityName = $"{property.EntityName}DTO" });
                }
            }

            return DTOProperties;
        }

        public static string GetFormatedDTOPropertyType(string propertyType)
        {
            if (propertyType != "string" && propertyType.IsBaseDataType())
                return $"{propertyType}?".Replace("??", "?");

            return propertyType;
        }

        #endregion

        #region Angular

        /// <summary>
        /// Pass the properties with the C# data types
        /// </summary>
        public static List<string> GetAngularImports(List<SpiderProperty> properties, string projectName = null, bool generateClassImports = false, string importPath = null)
        {
            List<string> result = new List<string>();

            foreach (SpiderProperty prop in properties)
            {
                string cSharpDataType = prop.Type;
                if (cSharpDataType.IsBaseDataType() == false)
                {
                    string angularDataType = GetAngularDataTypeForImport(cSharpDataType);

                    if (generateClassImports && cSharpDataType.Contains($"{DTONamespaceEnding}"))
                    {
                        result.Add($"import {{ {angularDataType} }} from \"./{importPath}{projectName.FromPascalToKebabCase()}-entities.generated\";");
                    }
                    else if (generateClassImports && cSharpDataType.IsEnum())
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

            if (cSharpType.IsEnum())
                return cSharpType;

            if (cSharpType.EndsWith("MimeTypes") || cSharpType.EndsWith("MimeTypes>"))
                return cSharpType;

            if (cSharpType.Contains(DTONamespaceEnding) || (cSharpType.Contains("Task<") && cSharpType.Contains("ActionResult") == false)) // FT: We don't want to handle "ActionResult"
                return ExtractAngularTypeFromGenericCSharpType(cSharpType); // ManyToOne

            return "any"; // eg. "ActionResult", "Task"...
        }

        public static List<SpiderProperty> GetUIOrderedOneToManyProperties(SpiderClass entity)
        {
            return entity.Properties.Where(x => x.Attributes.Any(x => x.Name == "UIOrderedOneToMany")).ToList();
        }

        #region Helpers

        private static string GetAngularDataTypeForImport(string CSharpDataType)
        {
            //if (ExtractAngularTypeFromGenericCSharpType(CSharpDataType).IsBaseType()) // TODO FT: We were checking for the C# type, which wasn't correct, but add correct code here if we need in the future
            //    return null;

            if (ExtractAngularTypeFromGenericCSharpType(CSharpDataType).IsEnum())
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

            parts[parts.Length - 1] = parts[parts.Length - 1].Replace(">", ""); // long

            if (cSharpType.Contains("TableResponseDTO"))
            {
                result = $"TableResponse<{parts[parts.Length - 1].Replace("DTO", "")}>";
            }
            else if (cSharpType.Contains("LazyLoadSelectedIdsResultDTO"))
            {
                result = "LazyLoadSelectedIdsResult";
            }
            else if (cSharpType.Contains("NamebookDTO"))
            {
                result = "Namebook";
            }
            else if (cSharpType.Contains("CodebookDTO"))
            {
                result = "Codebook";
            }
            else if (parts[parts.Length - 1].IsBaseDataType())
            {
                result = GetAngularType(parts[parts.Length - 1]); // List<long>
            }
            else
            {
                result = parts[parts.Length - 1]; // List<UserDTO>
            }

            return result.Replace(DTONamespaceEnding, "").Replace("[]", "");
        }

        #endregion

        #endregion

        #region Permissions

        public static List<SpiderEnum> GetEnumMembers(EnumDeclarationSyntax enume)
        {
            List<SpiderEnum> enumMembers = new List<SpiderEnum>();
            foreach (EnumMemberDeclarationSyntax member in enume.Members)
            {
                string name = member.Identifier.Text;
                string value = member.EqualsValue != null ? member.EqualsValue.Value.ToString() : null;
                enumMembers.Add(new SpiderEnum { Name = name, Value = value });
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

                    List<SpiderAttribute> classAttributes = GetAllAttributesOfTheClass(x, classes, new List<SpiderClass>());

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

        public static List<SpiderProperty> GetBlobProperties(SpiderClass c)
        {
            return c.Properties.Where(x => x.Attributes.Any(x => x.Name == "BlobName")).ToList();
        }

        public static List<SpiderProperty> GetBlobProperties(List<SpiderProperty> properties)
        {
            return properties.Where(x => x.Attributes.Any(x => x.Name == "BlobName")).ToList();
        }

        #endregion

        #region Populate hacks

        private static List<SpiderAttribute> GetAllAttributesOfTheMember(MemberDeclarationSyntax prop)
        {
            List<SpiderAttribute> attributes = new List<SpiderAttribute>();
            attributes = prop.AttributeLists.SelectMany(x => x.Attributes).Select(x =>
            {
                return GetSpiderAttribute(x);
            })
            .ToList();
            return attributes;
        }

        // FT: Maybe ill need it in the future, for now im using only for the current class
        //private static List<SpiderMethod> GetAllMethodsOfTheClass(ClassDeclarationSyntax c, IEnumerable<ClassDeclarationSyntax> allClasses,)
        //{
        //    TypeSyntax baseType = c.BaseList?.Types.FirstOrDefault()?.Type; //BaseClass<long>
        //    ClassDeclarationSyntax baseClass = GetClass(baseType, allClasses);

        //    string s = c.Identifier.Text;

        //    List<SpiderMethod> properties = GetMethodsOfCurrentClass(c);

        //    TypeSyntax typeGeneric = null;

        //    while (baseType != null)
        //    {
        //        baseType = baseClass.BaseList?.Types.FirstOrDefault()?.Type;
        //        baseClass = GetClass(baseType, allClasses);
        //    }

        //    return properties;
        //}

        private static List<SpiderProperty> GetPropertiesForBaseClasses(string typeName, string idType)
        {
            if (typeName.StartsWith($"{BusinessObject}"))
            {
                if (typeName.Contains("DTO"))
                {
                    return new List<SpiderProperty>()
                    {
                        new SpiderProperty{ Type = "int?", Name = "Version" },
                        new SpiderProperty{ Type = idType, Name = "Id" },
                        new SpiderProperty{ Type = "DateTime?", Name = "CreatedAt" },
                        new SpiderProperty{ Type = "DateTime?", Name = "ModifiedAt" },
                    };
                }
                else
                {
                    return new List<SpiderProperty>()
                    {
                        new SpiderProperty{ Type = "int", Name = "Version" },
                        new SpiderProperty{ Type = idType, Name = "Id" },
                        new SpiderProperty{ Type = "DateTime", Name = "CreatedAt" },
                        new SpiderProperty{ Type = "DateTime", Name = "ModifiedAt" },
                    };
                }
            }
            else if (typeName.StartsWith($"{ReadonlyObject}"))
            {
                if (typeName.Contains("DTO"))
                {
                    return new List<SpiderProperty>()
                    {
                        new SpiderProperty { Type = idType, Name = "Id" },
                        //new SpiderProperty { Type = "DateTime?", IdentifierText = "CreatedAt" },
                    };
                }
                else
                {
                    return new List<SpiderProperty>()
                    {
                        new SpiderProperty { Type = idType, Name = "Id" },
                        //new SpiderProperty { Type = "DateTime", IdentifierText = "CreatedAt" },
                    };
                }
            }
            else
            {
                return new List<SpiderProperty>() { };
            }
        }

        #endregion

        #region Helpers

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
                StreamWriter sw = new StreamWriter(path, false);
                sw.WriteLine(data);
                sw.Close();
            }
        }

        public static void WriteToTheFile(StringBuilder data, string path)
        {
            if (data != null)
            {
                StreamWriter sw = new StreamWriter(path, false);
                sw.WriteLine(data);
                sw.Close();
            }
        }

        public static void UpdateResourceFile(Dictionary<string, string> data, string path)
        {
            Dictionary<string, string> resourceEntries = new Dictionary<string, string>();

            if (File.Exists(path))
            {
                //Get existing resources
                ResXResourceReader reader = new ResXResourceReader(path);
                resourceEntries = reader.Cast<DictionaryEntry>().ToDictionary(d => d.Key.ToString(), d => d.Value?.ToString() ?? "");
                reader.Close();
            }
            else
            {
                return;
            }

            foreach (KeyValuePair<string, string> entry in data)
            {
                if (!resourceEntries.ContainsKey(entry.Key))
                {
                    if (!resourceEntries.ContainsValue(entry.Value))
                    {
                        resourceEntries.Add(entry.Key, entry.Value);
                    }
                }
            }

            string directoryPath = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            //Write the combined resource file
            ResXResourceWriter resourceWriter = new ResXResourceWriter(path);

            foreach (KeyValuePair<string, string> entry in resourceEntries)
            {
                resourceWriter.AddResource(entry.Key, resourceEntries[entry.Key]);
            }

            resourceWriter.Generate();

            resourceWriter.Close();
        }

        public static void WriteResourceFile(Dictionary<string, string> data, string path)
        {
            if (File.Exists(path) == false)
                return;

            Dictionary<string, string> resourceEntries = new Dictionary<string, string>();

            foreach (KeyValuePair<string, string> entry in data)
            {
                if (resourceEntries.ContainsKey(entry.Key) == false)
                    resourceEntries.Add(entry.Key, entry.Value);
            }

            ResXResourceWriter resourceWriter = new ResXResourceWriter(path);

            foreach (KeyValuePair<string, string> entry in resourceEntries)
                resourceWriter.AddResource(entry.Key, entry.Value ?? "");

            resourceWriter.Generate();

            resourceWriter.Close();
        }

        public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> source)
        {
            using (var e = source.GetEnumerator())
            {
                if (e.MoveNext())
                {
                    for (var value = e.Current; e.MoveNext(); value = e.Current)
                    {
                        yield return value;
                    }
                }
            }
        }

        #endregion
    }
}


