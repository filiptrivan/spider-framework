using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Spider.SourceGenerators.Enums;
using Spider.SourceGenerators.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Spider.SourceGenerators.Shared
{
    public static class Extensions
    {
        #region Case

        /// <summary>
        /// There is more performant way but this is NET2
        /// </summary>
        public static string FirstCharToUpper(this string input)
        {
            switch (input)
            {
                case null: return null;
                case "": return null;
                default: return input.First().ToString().ToUpper() + input.Substring(1);
            }
        }

        /// <summary>
        /// There is more performant way but this is NET2
        /// </summary>
        public static string FirstCharToLower(this string input)
        {
            switch (input)
            {
                case null: return null;
                case "": return null;
                default: return input.First().ToString().ToLower() + input.Substring(1);
            }
        }

        public static string Pluralize(this string word)
        {
            if (string.IsNullOrEmpty(word))
                return word;

            Dictionary<string, string> irregulars = new Dictionary<string, string>
            {
                { "Child", "Children" },
                { "Person", "People" },
                { "Man", "Men" },
                { "Woman", "Women" },
                { "Mouse", "Mice" },
                { "Foot", "Feet" },
                { "Tooth", "Teeth" },
                { "Goose", "Geese" },
                { "Cactus", "Cacti" },
                { "Focus", "Foci" },
                { "Phenomenon", "Phenomena" },
                { "Analysis", "Analyses" },
                { "Thesis", "Theses" }
            };

            if (irregulars.TryGetValue(word, out string plural))
                return plural;

            HashSet<string> uncountableNouns = new HashSet<string>
            {
                "Fish", "Sheep", "Deer", "Species", "Aircraft", "Moose", "Series"
            };

            if (uncountableNouns.Contains(word))
                return word;

            if (word.EndsWith("y", StringComparison.OrdinalIgnoreCase) && !IsVowel(word[word.Length - 2]))
                return word.Substring(0, word.Length - 1) + "ies";

            if (word.EndsWith("s", StringComparison.OrdinalIgnoreCase) ||
                word.EndsWith("x", StringComparison.OrdinalIgnoreCase) ||
                word.EndsWith("z", StringComparison.OrdinalIgnoreCase) ||
                word.EndsWith("ch", StringComparison.OrdinalIgnoreCase) ||
                word.EndsWith("sh", StringComparison.OrdinalIgnoreCase))
                return word + "es";

            if (word.EndsWith("f", StringComparison.OrdinalIgnoreCase) && !word.EndsWith("ff", StringComparison.OrdinalIgnoreCase))
                return word.Substring(0, word.Length - 1) + "ves";

            if (word.EndsWith("fe", StringComparison.OrdinalIgnoreCase) && !word.EndsWith("ffe", StringComparison.OrdinalIgnoreCase))
                return word.Substring(0, word.Length - 2) + "ves";

            return word + "s";
        }

        private static bool IsVowel(char c)
        {
            return "aeiou".IndexOf(char.ToLower(c)) >= 0;
        }

        public static string ToCommaSeparatedString<T>(this List<T> input)
        {
            List<string> stringList = input.Select(item => item?.ToString() ?? string.Empty).ToList();

            if (stringList.Count > 1)
                return $"{string.Join(", ", stringList.Take(stringList.Count - 1))} and {stringList.Last()}";
            else
                return stringList.FirstOrDefault();
        }

        public static string FromPascalToKebabCase(this string pascalCaseString)
        {
            if (string.IsNullOrEmpty(pascalCaseString))
            {
                return string.Empty;
            }

            string kebabCaseString = Regex.Replace(pascalCaseString, "([a-z])([A-Z])", "$1-$2");
            kebabCaseString = kebabCaseString.ToLower();

            return kebabCaseString;
        }

        public static string ToTrainCase(this string text)
        {
            if (text == null)
                return null;

            return text.ToPascalCase().SplitCamelCase("_").FirstCharToUpper().Replace("--", "_");
        }

        /// <summary>
        /// Converts the specified string to PascalCase.
        /// </summary>
        /// <param name="text">The string to convert.</param>
        /// <returns>The PascalCase version of the string.</returns>
        public static string ToPascalCase(this string text)
        {
            // Create a StringBuilder object to store the result.
            StringBuilder result = new();

            // Get the TextInfo object for the current culture.
            TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;

            // Flag to track if we are at the beginning of a new word.
            bool newWord = true;

            // Iterate over each character in the string.
            for (int i = 0; i < text.Length; i++)
            {
                char currentChar = text[i];

                // If the current character is a letter or digit.
                if (char.IsLetterOrDigit(currentChar))
                {
                    // If we are at the beginning of a new word, convert the character to uppercase.
                    if (newWord)
                    {
                        result.Append(textInfo.ToUpper(currentChar));
                        newWord = false;
                    }
                    // Otherwise, add the character as is for uppercase or convert to lowercase for other characters.
                    else
                    {
                        result.Append(i < text.Length - 1 && char.IsUpper(currentChar) && char.IsLower(text[i + 1]) ? currentChar : char.ToLowerInvariant(currentChar));
                    }
                }
                // If the current character is not a letter or digit, we are at the beginning of a new word.
                else
                {
                    newWord = true;
                }

                // If the current character is a lowercase letter and the next character is an uppercase letter,
                // we are at the beginning of a new word.
                if (i < text.Length - 1 && char.IsLower(text[i]) && char.IsUpper(text[i + 1]))
                {
                    newWord = true;
                }
            }

            // Return the result as a string.
            return result.ToString();
        }

        /// <summary>
        /// Splits a given camel case string into separate words using the specified separator.
        /// </summary>
        /// <param name="input">The camel case string to be split.</param>
        /// <param name="splitWith">The separator to be used. By default, a single space is used.</param>
        /// <returns>The resulting string with words separated by the specified separator.</returns>
        public static string SplitCamelCase(this string input, string splitWith = " ")
        {
            if (string.IsNullOrEmpty(input)) return input; // if input is null or empty, return it as it is.

            // Create a new instance of StringBuilder to store the output string
            StringBuilder result = new();
            // Flag to keep track of whether the previous character was an uppercase letter or not
            bool isPrevUpper = false;

            // Iterate over each character in the input string
            for (int i = 0; i < input.Length; i++)
            {
                // Get the current character
                char currentChar = input[i];

                // If the current character is uppercase and not the first character
                if (i > 0 && char.IsUpper(currentChar))
                {
                    // If the previous character was not uppercase or the next character is not uppercase
                    if (!isPrevUpper || (i < input.Length - 1 && !char.IsUpper(input[i + 1])))
                    {
                        // Append the separator to the output string
                        result.Append(splitWith);
                    }
                }

                // Append the current character to the output string
                result.Append(currentChar);
                // Update the flag to reflect whether the current character is uppercase or not
                isPrevUpper = char.IsUpper(currentChar);
            }

            // Return the resulting string with words separated by the specified separator
            return result.ToString();
        }

        #endregion

        #region IsType

        /// <summary>
        /// User -> true
        /// string -> false
        /// List<User> -> false
        /// </summary>
        public static bool IsManyToOneType(this string type)
        {
            if (type.IsEnumerable())
                return false;
            if (type.IsBaseDataType())
                return false;

            return true;
        }

        public static bool IsManyToMany(this SpiderClass c)
        {
            if (c.BaseType == null)
                return true;

            return false;
        }

        public static bool IsAbstract(this ClassDeclarationSyntax c)
        {
            return c.Modifiers.Any(x => x.Text == "abstract");
        }

        /// <summary>
        /// User : BusinessObject<long> -> true
        /// User : ReadonlyObject<long> -> false
        /// </summary>
        public static bool IsBusinessObject(this SpiderClass c)
        {
            return c.BaseType?.Contains($"{Helpers.BusinessObject}<") == true;
        }

        public static bool IsReadonlyObject(this SpiderClass c)
        {
            return c.BaseType?.Contains($"{Helpers.ReadonlyObject}<") == true;
        }

        public static bool IsEnumerable(this string type)
        {
            return type.Contains("List") || type.Contains("IList") || type.Contains("[]");
        }

        public static bool IsOneToManyType(this string type)
        {
            return type.Contains("List");
        }

        public static bool IsEnum(this string type)
        {
            return type.EndsWith("Codes") || type.EndsWith("Codes>");
        }

        public static bool IsBaseDataType(this string propType)
        {
            return
                propType == "string" ||
                propType == "bool" ||
                propType == "bool?" ||
                propType == "DateTime" ||
                propType == "DateTime?" ||
                propType == "System.DateTime" ||
                propType == "System.DateTime?" ||
                propType == "long" ||
                propType == "long?" ||
                propType == "int" ||
                propType == "int?" ||
                propType == "decimal" ||
                propType == "decimal?" ||
                propType == "float" ||
                propType == "float?" ||
                propType == "double" ||
                propType == "double?" ||
                propType == "byte" ||
                propType == "byte?" ||
                propType == "System.Guid" ||
                propType == "System.Guid?" ||
                propType == "Guid" ||
                propType == "Guid?";
        }

        public static bool IsBlob(this SpiderProperty property)
        {
            SpiderAttribute blobNameAttribute = property.Attributes.Where(x => x.Name == "BlobName").SingleOrDefault();

            if (blobNameAttribute == null)
                return false;

            return true;
        }

        public static bool IsTypeNullable(this string dataType)
        {
            if (dataType.Contains("?"))
                return true;
            else
                return false;
        }

        /// <summary>
        /// Should use this method for the referenced project types
        /// </summary>
        public static string TypeToDisplayString(this object type)
        {
            string bigType = type.ToString();

            string splitType = bigType.Split('.').Last().Replace(">", "");

            if (bigType.IsOneToManyType())
            {
                string enumerableType = bigType.Split('<').First().Split('.').Last();

                return $"{enumerableType}<{splitType}>";
            }

            return splitType;
        }

        public static List<UITableColumn> GetUITableColumns(this SpiderProperty property)
        {
            List<UITableColumn> result = new List<UITableColumn>();

            foreach (SpiderAttribute attribute in property.Attributes)
            {
                if (attribute.Name == "UITableColumn")
                {
                    List<string> attributeValues = attribute.Value.Split(',').Select(v => v.Trim()).ToList();
                    string field = attributeValues[0];
                    string translationKey = attributeValues.Count > 1 ? attributeValues[1] : null;

                    result.Add(new UITableColumn
                    {
                        Field = field,
                        TranslationKey = translationKey ?? field.Replace("DisplayName", ""),
                    });
                }
            }

            return result;
        }

        public static bool ShouldGenerateDropdownControllerMethod(this SpiderProperty property)
        {
            return property.IsDropdownControlType() || property.IsMultiSelectControlType() || property.HasGenerateCommaSeparatedDisplayNameAttribute();
        }

        public static bool ShouldGenerateAutocompleteControllerMethod(this SpiderProperty property)
        {
            return property.IsAutocompleteControlType() || property.IsMultiAutocompleteControlType();
        }

        #region Has Attribute

        public static bool HasUIDoNotGenerateAttribute(this SpiderClass entity)
        {
            return entity.Attributes.Any(x => x.Name == "UIDoNotGenerate");
        }

        public static bool HasDoNotAuthorizeAttribute(this SpiderClass entity)
        {
            return entity.Attributes.Any(x => x.Name == "DoNotAuthorize");
        }

        public static bool HasBlobNameAttribute(this List<SpiderProperty> properties)
        {
            return properties.SelectMany(x => x.Attributes).Any(x => x.Name == "BlobName");
        }

        public static bool HasExcludeFromDTOAttribute(this SpiderProperty property)
        {
            return property.Attributes.Any(x => x.Name == "ExcludeFromDTO");
        }

        public static bool HasRequiredAttribute(this SpiderProperty property)
        {
            return property.Attributes.Any(x => x.Name == "Required");
        }

        public static bool HasUIOrderedOneToManyAttribute(this SpiderProperty property)
        {
            return property.Attributes.Any(x => x.Name == "UIOrderedOneToMany");
        }

        public static bool HasSimpleManyToManyTableLazyLoadAttribute(this SpiderProperty property)
        {
            return property.Attributes.Any(x => x.Name == "SimpleManyToManyTableLazyLoad");
        }

        public static bool HasGenerateCommaSeparatedDisplayNameAttribute(this SpiderProperty property)
        {
            return property.Attributes.Any(x => x.Name == "GenerateCommaSeparatedDisplayName");
        }

        public static bool HasIncludeInDTOAttribute(this SpiderProperty property)
        {
            return property.Attributes.Any(x => x.Name == "IncludeInDTO");
        }

        public static bool HasExcludeServiceMethodsFromGenerationAttribute(this SpiderProperty property)
        {
            return property.Attributes.Any(x => x.Name == "ExcludeServiceMethodsFromGeneration");
        }

        public static bool HasFromFormAttribute(this SpiderParameter parameter)
        {
            return parameter.Attributes.Any(x => x.Name == "FromForm");
        }

        public static bool HasM2MMaintanceEntityAttribute(this SpiderProperty property)
        {
            return property.Attributes.Any(x => x.Name == "M2MMaintanceEntity");
        }

        public static bool HasM2MEntityAttribute(this SpiderProperty property)
        {
            return property.Attributes.Any(x => x.Name == "M2MEntity");
        }

        public static bool HasUIDoNotGenerateAttribute(this SpiderMethod method)
        {
            return method.Attributes.Any(x => x.Name == "UIDoNotGenerate");
        }

        public static bool HasUIDoNotGenerateAttribute(this SpiderProperty property)
        {
            return property.Attributes.Any(x => x.Name == "UIDoNotGenerate");
        }

        #endregion

        #region IsControlType

        public static bool IsColorControlType(this SpiderProperty property)
        {
            return property.Attributes.Any(x => x.Name == "UIControlType" && x.Value == UIControlTypeCodes.ColorPick.ToString());
        }

        public static bool IsMultiSelectControlType(this SpiderProperty property)
        {
            return property.Attributes.Any(x => x.Name == "UIControlType" && x.Value == UIControlTypeCodes.MultiSelect.ToString());
        }

        public static bool IsMultiAutocompleteControlType(this SpiderProperty property)
        {
            return property.Attributes.Any(x => x.Name == "UIControlType" && x.Value == UIControlTypeCodes.MultiAutocomplete.ToString());
        }

        public static bool IsTableControlType(this SpiderProperty property)
        {
            return property.Attributes.Any(x => x.Name == "UIControlType" && x.Value == UIControlTypeCodes.Table.ToString());
        }

        public static bool IsDropdownControlType(this SpiderProperty property)
        {
            return property.Attributes.Any(x => x.Name == "UIControlType" && x.Value == UIControlTypeCodes.Dropdown.ToString());
        }

        public static bool IsAutocompleteControlType(this SpiderProperty property)
        {
            return property.Attributes.Any(x => x.Name == "UIControlType" && x.Value == UIControlTypeCodes.Autocomplete.ToString());
        }

        public static bool IsEditorControlType(this SpiderProperty property)
        {
            return property.Attributes.Any(x => x.Name == "UIControlType" && x.Value == UIControlTypeCodes.Editor.ToString());
        }

        #endregion

        #endregion

        #region Source Generator

        /// <summary>
        /// The same method is built in .NET8 linq, but source generator is .NET2
        /// </summary>
        public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> items, Func<T, TKey> property)
        {
            return items.GroupBy(property).Select(x => x.First());
        }

        public static string GetDTOBaseType(this SpiderClass c)
        {
            string baseClass = c.BaseType;
            if (baseClass == null)
                return null;
            else if (baseClass.Contains("<"))
                return baseClass.Replace("<", "DTO<");
            else
                return $"{baseClass}DTO";
        }

        public static string GetTableFilterAdditionalFilterPropertyName(this string idType)
        {
            if (idType == "int" || idType == "int?")
            {
                return "AdditionalFilterIdInt";
            }
            else if (idType == "long" || idType == "long?")
            {
                return "AdditionalFilterIdLong";
            }

            return null;
        }

        public static string GetBaseType(this ClassDeclarationSyntax c)
        {
            TypeSyntax baseType = c.BaseList?.Types.FirstOrDefault()?.Type; //BaseClass<long>

            if (baseType != null)
                return baseType.ToString();

            return null; // FT: many to many doesn't have base class
        }

        public static string GetNamespace(this BaseTypeDeclarationSyntax baseTypeDeclarationSyntax)
        {
            return baseTypeDeclarationSyntax
                .Ancestors()
                .OfType<NamespaceDeclarationSyntax>()
                .Select(ns => ns.Name.ToString())
                .FirstOrDefault();
        }

        /// <summary>Gets the file path the source generator was called from.</summary>
        /// <param name="context">The context of the Generator's Execute method.</param>
        /// <returns>The file path the generator was called from.</returns>
        public static IncrementalValueProvider<string> GetCallingPath(this IncrementalGeneratorInitializationContext context)
        {
            return context.AnalyzerConfigOptionsProvider
                .Select((provider, _) =>
                {
                    return provider.GlobalOptions.TryGetValue("build_property.projectdir", out var result)
                        ? result
                        : null;
                });
        }

        public static string Translate(this SpiderClass entity, TranslationCodes translation, LanguageCodes language)
        {
            return entity.Attributes.Where(x => x.Name == $"Translate{translation}{language}").Select(x => x.Value).SingleOrDefault();
        }

        public static string Translate(this SpiderProperty property, LanguageCodes language)
        {
            return property.Attributes.Where(x => x.Name == $"TranslateSingular{language}").Select(x => x.Value).SingleOrDefault();
        }

        public static string GetDecimalScale(this SpiderProperty property)
        {
            SpiderAttribute precissionAttribute = property.Attributes.Where(x => x.Name == "Precision").SingleOrDefault();

            if (precissionAttribute == null)
                return null;

            return precissionAttribute.Value.Split(',').Last();
        }

        public static string WithMany(this SpiderProperty property)
        {
            return property.Attributes.Where(x => x.Name == "WithMany").Select(x => x.Value).SingleOrDefault();
        }

        public static SpiderProperty GetManyToOnePropertyWithManyAttribute(this SpiderClass entity, string manyToOneType, string withMany)
        {
            return entity.Properties.Where(x => x.Type == manyToOneType && x.WithMany() == withMany).SingleOrDefault();
        }

        public static List<SpiderProperty> GetOrderedOneToManyProperties(this SpiderClass entity)
        {
            return entity.Properties.Where(x => x.HasUIOrderedOneToManyAttribute()).ToList();
        }

        public static string GetIdType(this SpiderClass c, List<SpiderClass> classes)
        {
            if (c == null)
                return "GetIdType.TheClassDoesNotExist";

            string baseType = c.BaseType; //BaseClass<long>

            while (baseType != null && baseType.Contains("<") == false)
            {
                SpiderClass baseClass = classes.Where(x => x.Name == baseType).SingleOrDefault();

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

        public static bool ShouldSkipPropertyInDTO(this SpiderProperty property)
        {
            if (property.Attributes.Any(x => x.Name == "ExcludeFromDTO" || x.Name == "M2MMaintanceEntityKey" || x.Name == "M2MEntityKey") ||
               (property.Type.IsOneToManyType() && !property.HasGenerateCommaSeparatedDisplayNameAttribute() && !property.HasIncludeInDTOAttribute())
            )
            {
                return true;
            }

            return false;
        }

        #endregion

        #region Helpers

        public static string ReplaceEverythingAfter(this string source, string keyForReplace, string valueToInsert)
        {
            if (string.IsNullOrEmpty(source))
                return null;

            int index = source.IndexOf(keyForReplace, StringComparison.Ordinal);

            if (index == -1)
                throw new InvalidOperationException();

            // Get the part before the key and append the new value.
            return $"{source.Substring(0, index)}{valueToInsert}";
        }

        public static string ReplaceEverythingAfterLast(this string source, string keyForReplace, string valueToInsert)
        {
            if (string.IsNullOrEmpty(source))
                return null;

            int index = source.LastIndexOf(keyForReplace, StringComparison.Ordinal);

            if (index == -1)
                return source;

            // Get the part before the key and append the new value.
            return $"{source.Substring(0, index)}{valueToInsert}";
        }

        public static string ReplaceEverythingAfterTo(this string source, string keyForReplace, string to, string valueToInsert)
        {
            if (string.IsNullOrEmpty(source))
                return null;

            int keyIndex = source.IndexOf(keyForReplace, StringComparison.Ordinal);
            if (keyIndex == -1)
                return source;

            int searchStartIndex = keyIndex + keyForReplace.Length;

            int toIndex = source.IndexOf(to, searchStartIndex, StringComparison.Ordinal);
            if (toIndex == -1)
                return source;

            int secondToIndex = source.IndexOf(to, toIndex, StringComparison.Ordinal);
            if (secondToIndex != -1)
                toIndex = secondToIndex; // FT: If there are "))" in the source i want to get index of the second

            // Construct the new string:
            // 1. Everything before keyForReplace remains unchanged.
            // 2. Insert the new value.
            // 3. Append everything from 'to' onward.
            string beforeKey = source.Substring(0, keyIndex);
            string afterTo = source.Substring(toIndex);
            return $"{beforeKey}{valueToInsert}{afterTo}";
        }

        public static Dictionary<string, string> PrepareForTranslation(this IEnumerable<Dictionary<string, string>> data)
        {
            Dictionary<string, string> alreadyAddedKeyValuePairs = new Dictionary<string, string>();

            foreach (var dictionary in data)
            {
                foreach (var keyValuePair in dictionary)
                {
                    if (!alreadyAddedKeyValuePairs.ContainsKey(keyValuePair.Key))
                    {
                        // Add the new key-value pair if it doesn't already exist.
                        alreadyAddedKeyValuePairs[keyValuePair.Key] = keyValuePair.Value;
                    }
                    else if (alreadyAddedKeyValuePairs[keyValuePair.Key] == null)
                    {
                        // Update the value if it exists but is null.
                        alreadyAddedKeyValuePairs[keyValuePair.Key] = keyValuePair.Value;
                    }
                }
            }

            return alreadyAddedKeyValuePairs;
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

        public static List<string> Split(this string input, string splitter)
        {
            return input.Split([splitter], StringSplitOptions.None).ToList();
        }

        #endregion
    }
}
