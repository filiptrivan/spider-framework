using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Soft.SourceGenerator.NgTable.Angular;
using Soft.SourceGenerators.Enums;
using Soft.SourceGenerators.Helpers;
using Soft.SourceGenerators.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.IO;
using System.Linq;
using System.Resources.NetStandard;
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
        };

        #region Source Generator

        /// <summary>
        /// Getting all properties of the single class <paramref name="c"/>, including inherited ones.
        /// The inherited properties doesn't have any attributes
        /// </summary>
        public static List<SoftProperty> GetAllPropertiesOfTheClass(ClassDeclarationSyntax c, IList<ClassDeclarationSyntax> currentProjectClasses, List<SoftClass> referencedProjectsClasses)
        {
            TypeSyntax baseType = c.BaseList?.Types.FirstOrDefault()?.Type; //BaseClass<long>
            ClassDeclarationSyntax baseClass = GetClass(baseType, currentProjectClasses);

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
                    {
                        properties.AddRange(GetRoleProperties());
                    }
                    else if (baseType.ToString() == "RoleDTO")
                    {
                        properties.AddRange(GetRoleDTOProperties());
                    }
                    else
                    {
                        SoftClass softBaseClass = referencedProjectsClasses.Where(x => x.Name == c.Identifier.Text).SingleOrDefault();

                        if (softBaseClass != null)
                            properties.AddRange(softBaseClass.Properties);
                    }

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

        public static List<SoftAttribute> GetAllAttributesOfTheClass(ClassDeclarationSyntax c, IList<ClassDeclarationSyntax> currentProjectClasses, List<SoftClass> allClasses)
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
                
                cHelper = currentProjectClasses.Where(x => x.Identifier.Text == baseType?.ToString()).SingleOrDefault();

                if (baseType != null && cHelper == null)
                {
                    SoftClass softBaseClass = allClasses.Where(x => x.Name == c.Identifier.Text || $"{x.Name}DTO" == c.Identifier.Text).SingleOrDefault();

                    if (softBaseClass != null)
                        softAttributes.AddRange(softBaseClass.Attributes);

                    break;
                }

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
        public static List<SoftProperty> GetPropsOfCurrentClass(ClassDeclarationSyntax c)
        {
            List<SoftProperty> properties = c.Members.OfType<PropertyDeclarationSyntax>()
                .Select(prop => new SoftProperty()
                {
                    Type = prop.Type.ToString(),
                    Name = prop.Identifier.Text,
                    EntityName = c.Identifier.Text,
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
                    Body = method.Body?.ToString(), // FT: CreateHostBuilder method inside Program.cs has no body
                    Parameters = method.ParameterList.Parameters.Select(x => new SoftParameter { Name = x.Identifier.Text, Type = x.Type.ToString() }).ToList(),
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

        public static string GetDisplayNamePropForClass(ClassDeclarationSyntax c, IList<ClassDeclarationSyntax> classes)
        {
            List<SoftProperty> props = GetAllPropertiesOfTheClass(c, classes, new List<SoftClass>());
            SoftProperty displayNamePropForClass = props.Where(x => x.Attributes.Any(x => x.Name == DisplayNameAttribute)).SingleOrDefault();

            if (displayNamePropForClass == null)
                return $"Id.ToString()";

            if (displayNamePropForClass.Type != "string")
                return $"{displayNamePropForClass.Name}.ToString()";

            return displayNamePropForClass.Name;
        }

        public static string GetDisplayNameProperty(SoftClass entity)
        {
            SoftAttribute entityDisplayNameAttribute = entity.Attributes.Where(x => x.Name == "SoftDisplayName").SingleOrDefault();

            if (entityDisplayNameAttribute != null)
                return entityDisplayNameAttribute.Value;

            List<SoftProperty> props = entity.Properties;
            SoftProperty displayNamePropForClass = props.Where(x => x.Attributes.Any(x => x.Name == DisplayNameAttribute)).SingleOrDefault();

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
                            DTOClassProperties.Add(new SoftProperty { Name = propNameInsideBrackets });
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
                .Where(prop => prop.Type.IsManyToOneType() &&
                               prop.Attributes.Any(x => x.Name == "ManyToOneRequired") &&
                               prop.Type == nameOfTheEntityClass)
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

            string pattern = @"nameof\((?:[^.]*\.)?([^.)]*)\)"; // nameof(abc.def.ghi) => ghi
            value = value != null ? Regex.Replace(value, pattern, "$1") : null;

            return value;
        }

        private static SoftProperty GetPropWithModifiedT(PropertyDeclarationSyntax prop, TypeSyntax typeGeneric, ClassDeclarationSyntax baseClass)
        {
            List<SoftAttribute> attributes = GetAllAttributesOfTheMember(prop);
            SoftProperty newProp = new SoftProperty() { Type = prop.Type.ToString(), Name = prop.Identifier.Text, Attributes = attributes, EntityName = baseClass.Identifier.Text };

            if (prop.Type.ToString() == "T") // If some property has type of T, we change it to long for example
            {
                newProp.Type = typeGeneric.ToString();
                return newProp;
            }

            return newProp;
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

        public static IncrementalValueProvider<List<SoftClass>> GetIncrementalValueProviderClassesFromReferencedAssemblies(IncrementalGeneratorInitializationContext context, List<NamespaceExtensionCodes> namespaceExtensions)
        {
            return context.CompilationProvider
                .Select((compilation, _) =>
                {
                    List<SoftClass> classes = new List<SoftClass>();

                    foreach (IAssemblySymbol referencedAssembly in compilation.SourceModule.ReferencedAssemblySymbols
                             .Where(a => a.Name.Contains("Soft") || a.Name.Contains("Playerty")))
                    {
                        classes.AddRange(GetClassesFromReferencedAssemblies(referencedAssembly.GlobalNamespace, namespaceExtensions));
                    }

                    return classes;
                });
        }

        private static List<SoftClass> GetClassesFromReferencedAssemblies(INamespaceSymbol namespaceSymbol, List<NamespaceExtensionCodes> namespaceExtensions)
        {
            List<SoftClass> classes = new List<SoftClass>();

            List<INamedTypeSymbol> types = namespaceSymbol.GetTypeMembers()
                .Where(type => type.TypeKind == TypeKind.Class &&
                       namespaceExtensions.Any(namespaceExtension => GetFullNamespace(type).EndsWith($".{namespaceExtension}")))
                .ToList();

            // Add all the type members (classes, structs, etc.) in this namespace
            foreach (INamedTypeSymbol type in types)
            {
                List<SoftAttribute> attributes = GetAttributesFromReferencedAssemblies(type);

                SoftClass softClass = new SoftClass
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

                classes.Add(softClass);
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
                            Name = member.Name,
                            EntityName = type.Name,
                            Type = property.Type.TypeToDisplayString(),
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

                SoftAttribute softAttribute = new SoftAttribute
                {
                    Name = attributeName,
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

        public static List<string> GetEntityClassesUsings(List<SoftClass> referencedProjectEntityClasses)
        {
            List<string> namespaces = referencedProjectEntityClasses
                .Where(x => x.Namespace.EndsWith(".Entities"))
                .Select(x => $"using {x.Namespace};")
                .Distinct()
                .ToList();

            return namespaces;
        }

        public static List<string> GetDTOClassesUsings(List<SoftClass> referencedProjectEntityClasses)
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

            List<SoftProperty> properties = GetAllPropertiesOfTheClass(settingsClass, classes, new List<SoftClass>());
            SoftProperty p = properties?.Where(x => x.Name == generatorName)?.SingleOrDefault();
            string outputPath = p?.Attributes?.Where(x => x.Name == "Output")?.SingleOrDefault()?.Value;
            return outputPath;
        }

        public static bool ShouldStartGenerator(string generatorName, IList<ClassDeclarationSyntax> classes)
        {
            ClassDeclarationSyntax settingsClass = GetSettingsClass(classes);

            if (settingsClass == null)
                return false;

            List<SoftProperty> properties = GetAllPropertiesOfTheClass(settingsClass, classes, new List<SoftClass>());
            SoftProperty p = properties?.Where(x => x.Name == generatorName)?.SingleOrDefault();

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

        public static List<SoftClass> GetSoftEntityClasses(IList<ClassDeclarationSyntax> currentProjectClasses, List<SoftClass> referencedProjectsClasses)
        {
            return GetSoftClasses(currentProjectClasses, referencedProjectsClasses)
                .Where(x => x.Namespace.EndsWith(".Entities"))
                .ToList();
        }

        public static List<SoftClass> GetDTOClasses(List<SoftClass> classes)
        {
            return classes
                .Where(x => x.Namespace.EndsWith($".{EntitiesNamespaceEnding}") || x.Namespace.EndsWith($".{DTONamespaceEnding}"))
                .SelectMany(x =>
                {
                    if (x.Name.EndsWith("DTO") || x.Namespace.EndsWith(".DTO"))
                    {
                        return new List<SoftClass>
                        {
                            new SoftClass
                            {
                                Name = x.Name,
                                Properties = x.Properties,
                                Attributes = x.Attributes,
                                BaseType = x.BaseType,
                                IsAbstract = x.IsAbstract,
                                Methods = x.Methods,
                                Namespace = x.Namespace,
                                IsGenerated = false,
                            }
                        };
                    }
                    else // Entity
                    {
                        return new List<SoftClass>
                        {
                            new SoftClass
                            {
                                Name = $"{x.Name}DTO",
                                Properties = GetDTOSoftProps(x, classes),
                                Namespace = x.Namespace.Replace(".Entities", ".DTO"),
                                IsGenerated = true
                            },
                            new SoftClass
                            {
                                Name = $"{x.Name}SaveBodyDTO",
                                Properties = GetSaveBodyDTOProperties(x, classes),
                                Namespace = x.Namespace.Replace(".Entities", ".DTO"),
                                IsGenerated = true
                            },
                        };
                    }
                })
                .ToList();
        }

        private static List<SoftProperty> GetSaveBodyDTOProperties(SoftClass entity, List<SoftClass> entities)
        {
            List<SoftProperty> result = new List<SoftProperty>();
            result.Add(new SoftProperty { Name = $"{entity.Name}DTO", Type = $"{entity.Name}DTO" });

            foreach (SoftProperty property in entity.Properties)
            {
                SoftClass extractedEntity = entities.Where(x => x.Name == ExtractTypeFromGenericType(property.Type)).SingleOrDefault();
                string extractedEntityIdType = entity.GetIdType(entities);

                if (property.HasOrderedOneToManyAttribute())
                {
                    result.Add(new SoftProperty { Name = $"{property.Name}DTO", Type = $"List<{extractedEntity.Name}>" });
                }
                else if (
                    property.IsMultiSelectControlType() ||
                    property.IsMultiAutocompleteControlType())
                {
                    result.Add(new SoftProperty { Name = $"Selected{property.Name}Ids", Type = $"List<{extractedEntityIdType}>" });
                }
                else if (property.HasSimpleManyToManyTableLazyLoadAttribute())
                {
                    result.Add(new SoftProperty { Name = $"Selected{property.Name}Ids", Type = $"List<{extractedEntityIdType}>" });
                    result.Add(new SoftProperty { Name = $"Unselected{property.Name}Ids", Type = $"List<{extractedEntityIdType}>" });
                    result.Add(new SoftProperty { Name = $"AreAll{property.Name}Selected", Type = "bool?" });
                    result.Add(new SoftProperty { Name = $"{property.Name}TableFilter", Type = "TableFilterDTO" });
                }
            }

            return result;
        }

        public static List<SoftClass> GetSoftClasses(IList<ClassDeclarationSyntax> currentProjectClasses, List<SoftClass> referencedProjectsClasses)
        {
            return currentProjectClasses
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
                        Properties = GetAllPropertiesOfTheClass(x, currentProjectClasses, referencedProjectsClasses),
                        Attributes = GetAllAttributesOfTheClass(x, currentProjectClasses, referencedProjectsClasses),
                        Methods = GetMethodsOfCurrentClass(x),
                    };
                })
                .OrderBy(x => x.Name)
                .ToList();
        }

        public static string GetTypeForTheClassAndPropName(SoftClass c, string propName)
        {
            return c.Properties.Where(x => x.Name == propName).Select(x => x.Type).Single();
        }

        public static List<SoftProperty> GetDTOSoftProps(SoftClass entityClass, List<SoftClass> entityClasses)
        {
            List<SoftProperty> props = new List<SoftProperty>(); // public string Email { get; set; }
            List<SoftProperty> properties = entityClass.Properties;

            foreach (SoftProperty prop in properties)
            {
                if (prop.SkipPropertyInDTO())
                    continue;

                string propType = prop.Type;
                string propName = prop.Name;
                // FT: Not adding attributes because they are not the same

                if (propType.IsManyToOneType())
                {
                    props.Add(new SoftProperty { Name = $"{propName}DisplayName", Type = "string" });
                    SoftClass manyToOneClass = entityClasses.Where(x => x.Name == propType).SingleOrDefault();
                    props.Add(new SoftProperty { Name = $"{propName}Id", Type = $"{manyToOneClass.GetIdType(entityClasses)}?" });
                    continue;
                }
                else if (propType.IsEnumerable() && prop.Attributes.Any(x => x.Name == "GenerateCommaSeparatedDisplayName"))
                {
                    props.Add(new SoftProperty { Name = $"{propName}CommaSeparated", Type = "string" });
                    continue;
                }
                else if (propType == "byte[]")
                {
                    props.Add(new SoftProperty { Name = propName, Type = "string" });
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
                    props.Add(new SoftProperty { Name = $"{propName}Data", Type = "string" });
                }
                else if (propType != "string")
                {
                    propType = "UNSUPPORTED TYPE";
                }

                props.Add(new SoftProperty { Name = propName, Type = propType });
            }

            return props;
        }

        public static bool SkipPropertyInDTO(this SoftProperty property)
        {
            return property.Attributes.Any(x => x.Name == "IgnorePropertyInDTO" || x.Name == "M2MMaintanceEntityKey" || x.Name == "M2MExtendEntityKey");
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

        public static List<SoftProperty> GetUIOrderedOneToManyProperties(SoftClass entity)
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
            else if (parts[parts.Length - 1].IsBaseType())
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

                    List<SoftAttribute> classAttributes = GetAllAttributesOfTheClass(x, classes, new List<SoftClass>());

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

        #region Populate hacks

        // FT HACK, FT TODO: Make this with all project references
        private static List<SoftProperty> GetRoleProperties()
        {
            List<SoftProperty> properties = new List<SoftProperty>
            {
                new SoftProperty
                {
                    Name="Name", Type="string", Attributes=new List<SoftAttribute>
                    {
                        new SoftAttribute { Name="SoftDisplayName" },
                        new SoftAttribute { Name="Required" },
                        new SoftAttribute { Name="StringLength", Value="255, MinimumLength = 1" },
                    }
                },
                new SoftProperty
                {
                    Name="Description", Type="string", Attributes=new List<SoftAttribute>
                    {
                        new SoftAttribute { Name="StringLength", Value="400, MinimumLength = 1" },
                    }
                },
                new SoftProperty
                {
                    Name="Permissions", Type="List<Permission>"
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
                    Name="Name", Type="string",
                },
                new SoftProperty
                {
                    Name="Description", Type="string"
                },
                new SoftProperty
                {
                    Name="Permissions", Type="List<PermissionDTO>"
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
                        new SoftProperty{ Type = "int?", Name = "Version" },
                        new SoftProperty{ Type = idType, Name = "Id" },
                        new SoftProperty{ Type = "DateTime?", Name = "CreatedAt" },
                        new SoftProperty{ Type = "DateTime?", Name = "ModifiedAt" },
                    };
                }
                else
                {
                    return new List<SoftProperty>()
                    {
                        new SoftProperty{ Type = "int", Name = "Version" },
                        new SoftProperty{ Type = idType, Name = "Id" },
                        new SoftProperty{ Type = "DateTime", Name = "CreatedAt" },
                        new SoftProperty{ Type = "DateTime", Name = "ModifiedAt" },
                    };
                }
            }
            else if (typeName.StartsWith($"{ReadonlyObject}"))
            {
                if (typeName.Contains("DTO"))
                {
                    return new List<SoftProperty>()
                    {
                        new SoftProperty { Type = idType, Name = "Id" },
                        //new SoftProperty { Type = "DateTime?", IdentifierText = "CreatedAt" },
                    };
                }
                else
                {
                    return new List<SoftProperty>()
                    {
                        new SoftProperty { Type = idType, Name = "Id" },
                        //new SoftProperty { Type = "DateTime", IdentifierText = "CreatedAt" },
                    };
                }
            }
            else if (typeName.StartsWith($"LazyTableSelectionDTO")) // TODO FT: Put inside variable
            {
                return new List<SoftProperty>()
                {
                    new SoftProperty { Type = $"TableFilterDTO", Name = "TableFilter" },
                    new SoftProperty { Type = $"List<{idType}>", Name = "SelectedIds" },
                    new SoftProperty { Type = $"List<{idType}>", Name = "UnselectedIds" },
                    new SoftProperty { Type = "bool?", Name = "AreAllSelected" },
                };
            }
            else
            {
                return new List<SoftProperty>() { };
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

        #endregion
    }
}


