using Microsoft.CodeAnalysis.CSharp.Syntax;
using Soft.SourceGenerator.NgTable.Helpers;
using Soft.SourceGenerators.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Soft.SourceGenerators.Helpers
{
    public static class Extension
    {
        /// <summary>
        /// There is more performant way but this is NET2
        /// </summary>
        public static string FirstCharToUpper(this string input)
        {
            switch (input)
            {
                case null: throw new ArgumentNullException(nameof(input));
                case "": throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input));
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
                case null: throw new ArgumentNullException(nameof(input));
                case "": throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input));
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

        /// <summary>
        /// User -> true
        /// string -> false
        /// List<User> -> false
        /// </summary>
        public static bool PropTypeIsManyToOne(this string propType)
        {
            if (propType.IsEnumerable())
                return false;
            if (propType.IsBaseType())
                return false;

            return true;
        }

        public static bool IsAbstract(this ClassDeclarationSyntax c)
        {
            return c.Modifiers.Any(x => x.Text == "abstract");
        }

        /// <summary>
        /// User : BusinessObject<long> -> true
        /// User : ReadonlyObject<long> -> false
        /// </summary>
        public static bool IsEntityBusinessObject(this ClassDeclarationSyntax c)
        {
            return c.BaseList?.Types.FirstOrDefault()?.Type?.ToString()?.Contains($"{Helper.BusinessObject}<") == true;
        }

        public static bool IsEntityBusinessObject(this SoftClass c)
        {
            return c.BaseType?.Contains($"{Helper.BusinessObject}<") == true;
        }

        public static bool IsEntityReadonlyObject(this ClassDeclarationSyntax c)
        {
            return c.BaseList?.Types.FirstOrDefault()?.Type?.ToString()?.Contains($"{Helper.ReadonlyObject}<") == true;
        }

        public static bool IsEntityReadonlyObject(this SoftClass c)
        {
            return c.BaseType?.Contains($"{Helper.ReadonlyObject}<") == true;
        }

        public static bool IsEnumerable(this string type)
        {
            return type.Contains("List") || type.Contains("IList") || type.Contains("[]");
        }

        public static bool IsEnum(this string type)
        {
            return type.EndsWith("Codes") || type.EndsWith("Codes>");
        }

        public static bool IsBaseType(this string propType)
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

        public static bool HasBlobProperty(this SoftClass c)
        {
            return c.Properties.SelectMany(x => x.Attributes).Any(x => x.Name == "BlobName");
        }

        public static bool HasBlobProperty(this List<SoftProperty> properties)
        {
            return properties.SelectMany(x => x.Attributes).Any(x => x.Name == "BlobName");
        }

        /// <summary>
        /// The same method is in the .NET8 linq, but source generator is .NET2
        /// </summary>
        public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> items, Func<T, TKey> property)
        {
            return items.GroupBy(property).Select(x => x.First());
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

        public static bool IsTypeNullable(this string dataType)
        {
            if (dataType.Contains("?"))
                return true;
            else
                return false;
        }

        public static string GetDTOBaseType(this SoftClass c)
        {
            string baseClass = c.BaseType;
            if (baseClass == null)
                return null;
            else if (baseClass.Contains("<"))
                return baseClass.Replace("<", "DTO<");
            else
                return $"{baseClass}DTO";
        }

        public static string GetBaseType(this ClassDeclarationSyntax c)
        {
            TypeSyntax baseType = c.BaseList?.Types.FirstOrDefault()?.Type; //BaseClass<long>

            if (baseType != null)
                return baseType.ToString();

            return null; // FT: many to many doesn't have base class
        }

        public static SoftClass ToSoftClass(this ClassDeclarationSyntax c, IList<ClassDeclarationSyntax> classes)
        {
            return new SoftClass
            {
                Name = c.Identifier.Text,
                Properties = Helper.GetAllPropertiesOfTheClass(c, classes)
            };
        }
    }
}
