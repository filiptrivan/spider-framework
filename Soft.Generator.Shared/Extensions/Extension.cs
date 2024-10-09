using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Soft.Generator.Shared.Extensions
{
    public static class Extension
    {
        public static string FirstCharToUpper(this string input) =>
            input switch
            {
                null => throw new ArgumentNullException(nameof(input)),
                "" => throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input)),
                _ => string.Concat(input[0].ToString().ToUpper(), input.AsSpan(1))
            };

        public static string FirstCharToLower(this string input) =>
            input switch
            {
                null => throw new ArgumentNullException(nameof(input)),
                "" => throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input)),
                _ => string.Concat(input[0].ToString().ToLower(), input.AsSpan(1))
            };

        public static string ToCommaSeparatedString<T>(this List<T> input)
        {
            List<string> stringList = input.Select(item => item?.ToString() ?? string.Empty).ToList();

            if (stringList.Count > 1)
                return $"{string.Join(", ", stringList.Take(stringList.Count - 1))} and {stringList.Last()}"; // TODO FT: Add to the resources
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

        /// <summary>
        /// Searching for the base type in TPH inheritance
        /// </summary>
        public static IQueryable<T> OfType<T>(this IQueryable<T> source, Type type)
        {
            var parameter = Expression.Parameter(typeof(T), "e");
            var body = Expression.TypeIs(parameter, type);
            var predicate = Expression.Lambda<Func<T, bool>>(body, parameter);
            return source.Where(predicate);
        }
    }
}
