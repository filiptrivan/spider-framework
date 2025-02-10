using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Spider.SourceGenerators.Enums;
using Spider.SourceGenerators.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources.NetStandard;
using System.Text;
using System.Text.RegularExpressions;

namespace Spider.SourceGenerators.Shared
{
    public static class Helpers
    {
        public static string DisplayNameAttribute { get; set; } = "DisplayName";
        public static string BusinessObject { get; set; } = "BusinessObject";
        public static string ReadonlyObject { get; set; } = "ReadonlyObject";
        public static string EntitiesNamespaceEnding { get; set; } = "Entities";
        public static string DTONamespaceEnding { get; set; } = "DTO";

        public static List<string> BaseClassNames { get; set; } = new()
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
                attributes.AddRange(cHelper.AttributeLists
                    .SelectMany(x => x.Attributes)
                    .Select(GetSpiderAttribute)
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
                    Attributes = prop.AttributeLists
                        .SelectMany(x => x.Attributes)
                        .Select(x =>
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
            SpiderProperty newProp = new SpiderProperty 
            { 
                Type = prop.Type.ToString(), 
                Name = prop.Identifier.Text,
                EntityName = baseClass.Identifier.Text,
                Attributes = attributes, 
            };

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

        public static IncrementalValuesProvider<ClassDeclarationSyntax> GetClassIncrementalValuesProvider(SyntaxValueProvider syntaxValueProvider, List<NamespaceExtensionCodes> namespaceExtensions)
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
                            Type = propertySymbol.Type.TypeToDisplayString(),
                            Name = member.Name,
                            EntityName = type.Name,
                            Attributes = GetAttributesFromReferencedAssemblies(member),
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

        public static List<string> GetEntityClassesUsings(List<SpiderClass> referencedProjectEntities)
        {
            List<string> namespaces = referencedProjectEntities
                .Where(x => x.Namespace.EndsWith(".Entities"))
                .Select(x => $"using {x.Namespace};")
                .Distinct()
                .ToList();

            return namespaces;
        }

        public static List<string> GetDTOClassesUsings(List<SpiderClass> referencedProjectEntities)
        {
            List<string> namespaces = referencedProjectEntities
                .Where(x => x.Namespace.EndsWith(".Entities"))
                .Select(x => $"using {x.Namespace.Replace(".Entities", ".DTO")};")
                .Distinct()
                .ToList();

            return namespaces;
        }

        #endregion

        #region Class list filters

        public static string GetGeneratorOutputPath(string generatorName, List<SpiderClass> currentProjectClasses)
        {
            SpiderClass settingsClass = GetSettingsClass(currentProjectClasses);

            if (settingsClass == null)
                return null;

            SpiderProperty property = settingsClass.Properties.Where(x => x.Name == generatorName).SingleOrDefault();

            if (property == null)
                return null;

            return property.Attributes
                .Where(x => x.Name == "Output")
                .Select(x => x.Value)
                .SingleOrDefault();
        }

        public static SpiderClass GetSettingsClass(List<SpiderClass> classes)
        {
            return classes
                .Where(x => x.Namespace.EndsWith($".GeneratorSettings"))
                .SingleOrDefault();
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

        #region DTO

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
            List<SpiderProperty> result = new();

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

        #region Validation Rules

        public static List<SpiderValidationRule> GetValidationRules(List<SpiderProperty> DTOProperties, List<SpiderAttribute> DTOAttributes, SpiderClass entity)
        {
            List<SpiderValidationRule> rulesOnDTO = new(); // priority - 1.
            List<SpiderValidationRule> rulesOnDTOProperties = new(); // priority - 2.
            List<SpiderValidationRule> rulesOnEntity = new(); // priority - 3.
            List<SpiderValidationRule> rulesOnEntityProperties = new(); // priority - 4.

            rulesOnDTO.AddRange(GetRulesForAttributes(DTOProperties, DTOAttributes, DTOProperties));

            foreach (SpiderProperty DTOproperty in DTOProperties)
            {
                SpiderValidationRule rule = GetRuleForProperty(DTOproperty, DTOProperties);

                if (rule != null)
                    rulesOnDTOProperties.Add(rule);
            }

            if (entity != null) // FT: If it is null then we only made DTO, without entity class
            {
                rulesOnEntity.AddRange(GetRulesForAttributes(entity.Properties, entity.Attributes, DTOProperties));

                foreach (SpiderProperty property in entity.Properties)
                {
                    SpiderValidationRule rule = GetRuleForProperty(property, DTOProperties);

                    if (rule != null)
                        rulesOnEntityProperties.Add(rule);
                }
            }

            List<SpiderValidationRule> mergedValidationRules = GetMergedValidationRules(rulesOnDTO, rulesOnDTOProperties, rulesOnEntity, rulesOnEntityProperties, DTOProperties);

            return mergedValidationRules;
        }

        /// <summary>
        /// Passing <paramref name="DTOProperties"/> because we are always validating only DTO with FluentValidation
        /// </summary>
        private static List<SpiderValidationRule> GetRulesForAttributes(List<SpiderProperty> properties, List<SpiderAttribute> attributes, List<SpiderProperty> DTOProperties)
        {
            List<SpiderValidationRule> rules = new();

            foreach (SpiderAttribute attribute in attributes)
            {
                if (attribute.Name == "CustomValidator")
                {
                    string rulePropertyName = ParsePropertyNameFromCustomClassValidator(attribute.Value);

                    rules.Add(new SpiderValidationRule
                    {
                        Property = DTOProperties.Where(x => x.Name == rulePropertyName).Single(),
                        ValidationRuleParts = GetValidationRulePartsForCustomClassValidator(attribute.Value),
                    });
                }
            }

            return rules;
        }

        /// <summary>
        /// RuleFor(x => x.GetTransactionsEndpoint).Length(1, 1000).Unless(i => string.IsNullOrEmpty(i.GetTransactionsEndpoint)); -> GetTransactionsEndpoint
        /// </summary>
        private static string ParsePropertyNameFromCustomClassValidator(string rule)
        {
            int dotIndex = rule.IndexOf(".");
            int parenIndex = rule.IndexOf(")", dotIndex);

            return rule.Substring(dotIndex + 1, parenIndex - dotIndex - 1);
        }

        private static List<SpiderValidationRulePart> GetValidationRulePartsForCustomClassValidator(string rule)
        {
            List<string> rulePartsWithValues = rule.Split(").").Skip(1).SkipLast().ToList();
            string lastRulePart = rule.Split(").").Last().Replace(");", "");
            rulePartsWithValues.Add(lastRulePart);

            return rulePartsWithValues
                .Select(rulePart => new SpiderValidationRulePart
                {
                    Name = GetRulePartName(rulePart),
                    MethodParametersBody = GetRulePartMethodParametersBody(rulePart),
                })
                .ToList();
        }

        private static SpiderValidationRule GetRuleForProperty(SpiderProperty property, List<SpiderProperty> DTOProperties)
        {
            if (property.Type.IsEnumerable())
                return null;

            string rulePropertyName = GetManyToOnePropertyNameForRule(property);
            List<SpiderValidationRulePart> ruleParts = GetRulePartsForProperty(property, rulePropertyName); // NotEmpty(), Length(0, 70);

            if (ruleParts.Count == 0)
                return null;

            return new SpiderValidationRule
            {
                Property = DTOProperties.Where(x => x.Name == rulePropertyName).Single(),
                ValidationRuleParts = ruleParts
            };
        }

        private static string GetManyToOnePropertyNameForRule(SpiderProperty property)
        {
            if (property.Type.IsManyToOneType())  // FT: if it is not base type and not enumerable than it's many to one for sure, and the validation can only be for id to be required
                return $"{property.Name}Id";

            return property.Name;
        }

        private static List<SpiderValidationRulePart> GetRulePartsForProperty(SpiderProperty property, string rulePropertyName)
        {
            List<SpiderValidationRulePart> ruleParts = new();

            foreach (SpiderAttribute attribute in property.Attributes)
            {
                switch (attribute.Name)
                {
                    case "Required":
                    case "ManyToOneRequired":
                        ruleParts.Add(new SpiderValidationRulePart
                        {
                            Name = "NotEmpty",
                            MethodParametersBody = ""
                        });
                        break;
                    case "StringLength":
                        string minValue = FindMinValueForStringLength(attribute.Value);
                        ruleParts.Add(new SpiderValidationRulePart
                        {
                            Name = "Length",
                            MethodParametersBody = minValue == null ? $"{FindMaxValueForStringLength(attribute.Value)}" : $"{minValue}, {FindMaxValueForStringLength(attribute.Value)}"
                        });
                        break;
                    case "Precision":
                        ruleParts.Add(new SpiderValidationRulePart
                        {
                            Name = "PrecisionScale",
                            MethodParametersBody = $"{attribute.Value}, false" // FT: only here the attribute.Value should be two values eg. 6, 7
                        });
                        break;
                    case "Range":
                        ruleParts.Add(new SpiderValidationRulePart
                        {
                            Name = "GreaterThanOrEqualTo",
                            MethodParametersBody = $"{attribute.Value.Split(',')[0].Trim()}"
                        });
                        ruleParts.Add(new SpiderValidationRulePart
                        {
                            Name = "LessThanOrEqualTo",
                            MethodParametersBody = $"{attribute.Value.Split(',')[1].Trim()}"
                        });
                        break;
                    case "GreaterThanOrEqualTo":
                        ruleParts.Add(new SpiderValidationRulePart
                        {
                            Name = "GreaterThanOrEqualTo",
                            MethodParametersBody = attribute.Value
                        });
                        break;
                    case "CustomValidator":
                        ruleParts.Add(new SpiderValidationRulePart
                        {
                            Name = GetRulePartName(attribute.Value),
                            MethodParametersBody = GetRulePartMethodParametersBody(attribute.Value)
                        });
                        break;
                    default:
                        break;
                }
            }

            // FT: If there is no Required nor ManyToOneRequired attribute, we should let user save null to database
            if (ruleParts.Count > 0 && property.Attributes.Any(x => x.Name == "Required" || x.Name == "ManyToOneRequired") == false)
            {
                if (property.Type == "string")
                {
                    ruleParts.Add(new SpiderValidationRulePart
                    {
                        Name = "Unless",
                        MethodParametersBody = $"i => string.IsNullOrEmpty(i.{rulePropertyName})"
                    });
                }
                else
                {
                    ruleParts.Add(new SpiderValidationRulePart
                    {
                        Name = "Unless",
                        MethodParametersBody = $"i => i.{rulePropertyName} == null"
                    });
                }
            }

            return ruleParts;
        }

        private static string GetRulePartName(string rulePart)
        {
            return rulePart.Substring(0, rulePart.IndexOf("("));
        }

        private static string GetRulePartMethodParametersBody(string rulePartWithoutLastParen)
        {
            if (rulePartWithoutLastParen.Length > 0 && rulePartWithoutLastParen[rulePartWithoutLastParen.Length - 1] == ')')
                rulePartWithoutLastParen = rulePartWithoutLastParen.Substring(0, rulePartWithoutLastParen.Length - 1);

            return rulePartWithoutLastParen.Substring(rulePartWithoutLastParen.IndexOf("(") + 1);
        }

        /// <summary>
        /// Getting merged validation rules for the single object (DTO + Entity)
        /// </summary>
        /// <returns></returns>
        private static List<SpiderValidationRule> GetMergedValidationRules(
            List<SpiderValidationRule> rulesOnDTO,
            List<SpiderValidationRule> rulesOnDTOProperties,
            List<SpiderValidationRule> rulesOnEntity,
            List<SpiderValidationRule> rulesOnEntityProperties,
            List<SpiderProperty> DTOProperties
        )
        {
            List<SpiderValidationRule> mergedRules = new();

            foreach (IGrouping<string, SpiderValidationRule> ruleGroup in rulesOnDTO.Concat(rulesOnDTOProperties).Concat(rulesOnEntity).Concat(rulesOnEntityProperties).GroupBy(x => x.Property.Name))
            {
                List<SpiderValidationRulePart> rulePartsOnDTO = rulesOnDTO.Where(x => x.Property.Name == ruleGroup.Key).SelectMany(x => x.ValidationRuleParts).ToList();
                List<SpiderValidationRulePart> rulePartsOnDTOProperties = rulesOnDTOProperties.Where(x => x.Property.Name == ruleGroup.Key).SelectMany(x => x.ValidationRuleParts).ToList();
                List<SpiderValidationRulePart> rulePartsOnEntity = rulesOnEntity.Where(x => x.Property.Name == ruleGroup.Key).SelectMany(x => x.ValidationRuleParts).ToList();
                List<SpiderValidationRulePart> rulePartsOnEntityProperties = rulesOnEntityProperties.Where(x => x.Property.Name == ruleGroup.Key).SelectMany(x => x.ValidationRuleParts).ToList();

                RemoveDuplicateRuleParts([rulePartsOnDTOProperties, rulePartsOnEntity, rulePartsOnEntityProperties], rulePartsOnDTO);
                RemoveDuplicateRuleParts([rulePartsOnEntity, rulePartsOnEntityProperties], rulePartsOnDTOProperties);
                RemoveDuplicateRuleParts([rulePartsOnEntityProperties], rulePartsOnEntity);

                List<SpiderValidationRulePart> mergedRuleParts = rulePartsOnDTO.Concat(rulePartsOnDTOProperties).Concat(rulePartsOnEntity).Concat(rulePartsOnEntityProperties).ToList();

                mergedRules.Add(new SpiderValidationRule
                {
                    Property = DTOProperties.Where(x => x.Name == ruleGroup.Key).Single(),
                    ValidationRuleParts = mergedRuleParts
                });
            }

            return mergedRules;
        }

        private static void RemoveDuplicateRuleParts(List<List<SpiderValidationRulePart>> rulePartsToRemove, List<SpiderValidationRulePart> priorRuleParts)
        {
            List<string> priorRulePartNames = priorRuleParts.Select(x => x.Name).ToList();

            foreach (List<SpiderValidationRulePart> ruleParts in rulePartsToRemove)
                ruleParts.RemoveAll(part => priorRulePartNames.Any(name => part.Name == name));
        }

        /// <summary>
        /// </summary>
        /// <param name="input">"70, MinimumLength = 5"</param>
        /// <returns></returns>
        private static string FindMinValueForStringLength(string input)
        {
            string pattern = @"MinimumLength\s*=\s*(\d+)";

            Match match = Regex.Match(input, pattern);

            if (match.Success)
                return match.Groups[1].Value;
            else
                return null;
        }

        /// <summary>
        /// </summary>
        /// <param name="input">"70, MinimumLength = 5"</param>
        /// <returns></returns>
        private static string FindMaxValueForStringLength(string input)
        {
            return input.Split(',').First().Replace(" ", "");
        }

        #endregion

        #endregion

        #region Angular

        /// <summary>
        /// Pass the properties with the C# data types
        /// </summary>
        public static List<string> GetAngularImports(List<SpiderProperty> properties, string projectName = null, bool generateClassImports = false, string importPath = null)
        {
            List<string> result = new();

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

        public static List<SpiderEnumItem> GetEnumItems(EnumDeclarationSyntax enume)
        {
            List<SpiderEnumItem> enumMembers = new();

            foreach (EnumMemberDeclarationSyntax member in enume.Members)
            {
                string name = member.Identifier.Text;
                string value = member.EqualsValue != null ? member.EqualsValue.Value.ToString() : null;
                enumMembers.Add(new SpiderEnumItem { Name = name, Value = value });
            }

            return enumMembers;
        }

        public static List<string> GetPermissionCodesForEntites(List<SpiderClass> entities)
        {
            List<string> result = new();

            foreach (SpiderClass entity in entities)
            {
                result.Add($"Read{entity.Name}");
                result.Add($"Update{entity.Name}");
                result.Add($"Insert{entity.Name}");
                result.Add($"Delete{entity.Name}");
            }

            return result;
        }

        public static string GetAuthorizeEntityMethodName(string entityName, CrudCodes crudCode)
        {
            return $"Authorize{entityName}{crudCode}AndThrow";
        }

        public static string GetAuthorizeEntityListMethodName(string entityName, CrudCodes crudCode)
        {
            return $"Authorize{entityName}List{crudCode}AndThrow";
        }

        #endregion

        #region Mapper

        /// <summary>
        /// Getting non generated partial mapper class.
        /// </summary>
        public static SpiderClass GetManualyWrittenMapperClass(List<SpiderClass> classes)
        {
            return classes
                .Where(x => x.Namespace.EndsWith(".DataMappers") && x.Attributes.Any(x => x.Name == "CustomMapper"))
                .SingleOrDefault(); // FT: It should allways be only one or none
        }

        #endregion

        #region Blobs

        public static List<SpiderProperty> GetBlobProperties(List<SpiderProperty> properties)
        {
            return properties.Where(x => x.Attributes.Any(x => x.Name == "BlobName")).ToList();
        }

        #endregion

        #region Populate hacks

        private static List<SpiderAttribute> GetAllAttributesOfTheMember(MemberDeclarationSyntax prop)
        {
            List<SpiderAttribute> attributes = new List<SpiderAttribute>();
            attributes = prop.AttributeLists
                .SelectMany(x => x.Attributes)
                .Select(GetSpiderAttribute)
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
            Dictionary<string, string> resourceEntries = new();

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

            Dictionary<string, string> resourceEntries = new();

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

        #endregion
    }
}


