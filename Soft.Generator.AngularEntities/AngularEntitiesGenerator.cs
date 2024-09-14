using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
// ako se menja nesto mora da se bilduje prethodno da bi iz poweshela mogao da izvrsis
namespace Soft.Generator.AngularEntities
{
    [Obsolete("We are using source generator.")]
    public class AngularEntitiesGenerator
    {
        public void Init()
        {
            List<string> allProjectNames = new List<string>();
            //List<string> excludeFromGenerating = new List<string>();
            List<string> dtoFoldersForGenerating = new List<string>();
            string currentDirectoryPath = "E:\\Projects\\Lutador.Portal\\Source\\Lutador.Portal\\bin\\Release\\net6.0\\Lutador.Portal.dll";

            //if (args.Length == 0)
            //{
            //    throw new ArgumentException("Izvrsio si skriptu bez argumenata.");
            //}
            int argumentVariableKeyIndex;
            //foreach (string arg in args)
            //{
            //    argumentVariableKeyIndex = arg.IndexOf(":");
            //    switch (arg.Substring(0, argumentVariableKeyIndex))
            //    {
            //        case "executingAssemblyPath:":
            //            currentDirectoryPath = arg.Substring(argumentVariableKeyIndex + 1);
            //            break;
            //        case "excludeFromGenerating:":
            //            dtoFoldersForGenerating.Add(arg.Substring(argumentVariableKeyIndex + 1));
            //            break;
            //        default:
            //            break;
            //    }
            //}

            dtoFoldersForGenerating.Add("E:\\Projects\\Lutador.Portal\\Source\\Business.Client\\DTO");

            int last = currentDirectoryPath.IndexOf("Source") + 6;
            var sourceDirectory = currentDirectoryPath.Substring(0, last);
            int last2 = currentDirectoryPath.IndexOf("Source");
            var bigProjectName = currentDirectoryPath.Substring(12, last2 - 13); // pr. Lutador.Portal

            if (!string.IsNullOrEmpty(currentDirectoryPath))
            {
                string[] subDirectories = Directory.GetDirectories(sourceDirectory);

                foreach (string subDir in subDirectories)
                {
                    string subDirName = Path.GetFileName(subDir);

                    if (subDirName.StartsWith("Business", StringComparison.OrdinalIgnoreCase))
                    {
                        allProjectNames.Add(subDir);
                    }
                }
            }

            foreach (var project in allProjectNames)
            {
                var projectName = project.Substring(project.IndexOf("Business"));
                string assemblyPath = $@"{project}\bin\Release\net6.0\{projectName}.dll";
                try
                {
                    Assembly[] allAssemblies = AppDomain.CurrentDomain.GetAssemblies();

                    foreach (Assembly assembly in allAssemblies)
                    {
                        Console.WriteLine(assembly.FullName);
                        Type[] types = assembly.GetTypes();
                    }

                    //List<Type> allClassesForGeneration = GetLoadableTypes(assembly);

                    #region i think ill never need this
                    //foreach (Type class_ in allClassesForGeneration)
                    //{
                    //    if (class_.Name.EndsWith("DTO") == false)
                    //    {
                    //        continue;
                    //    }
                    //    List<PropertyInfo> classProperties = class_.GetProperties().ToList();

                    //    string kebabCaseNameClassName = GetKebabCaseClassName(class_); // age-category-dto

                    //    List<string> imports = new List<string>();
                    //    List<string> properties = new List<string>();
                    //    //using (StreamWriter sw = new StreamWriter($@"{sourceDirectory}{bigProjectName}.SPA\src\app\entities\{kebabCaseNameClassName}.generated.ts"))
                    //    StreamWriter sw = new StreamWriter($@"C:\Users\user\Downloads\{kebabCaseNameClassName}.generated.txt", false);
                    //    StringBuilder sb = new StringBuilder();
                    //    foreach (PropertyInfo item in classProperties)
                    //    {

                    //    }

                    //        if (class_.Name == "ReportFilterDTO")
                    //        {

                    //        }
                    //        foreach (var s in classProperties)
                    //        {
                    //            if (s.PropertyType != typeof(string) && s.PropertyType != typeof(List<string>) && s.PropertyType != typeof(int) && s.PropertyType != typeof(long) && s.PropertyType != typeof(short) && s.PropertyType != typeof(double) && s.PropertyType != typeof(decimal)&& s.PropertyType != typeof(byte) && s.PropertyType != typeof(List<int>) && s.PropertyType != typeof(List<long>) && s.PropertyType != typeof(List<short>) && s.PropertyType != typeof(List<double>) && s.PropertyType != typeof(List<decimal>)&& s.PropertyType != typeof(List<byte>) && s.PropertyType
                    //            != typeof(DateTime) && s.PropertyType != typeof(List<DateTime>) && s.PropertyType != typeof(bool) && s.PropertyType.Name != "Nullable`1" && s.PropertyType.Name != "T")
                    //            {
                    //                if (s.PropertyType.IsGenericType && s.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
                    //                {
                    //                    Type elementType = s.PropertyType.GetGenericArguments()[0];
                    //                    string kebabCasePropertyTypeName = Regex.Replace(elementType.Name, "([a-z])([A-Z])", "$1-$2").ToLower();
                    //                    if (imports.Contains(elementType.Name) == false)
                    //                    {
                    //                        if (elementType.IsGenericType)
                    //                        {
                    //                            kebabCasePropertyTypeName = Regex.Replace(elementType.Name.Substring(0, elementType.Name.Length - 2), "([a-z])([A-Z])", "$1-$2").ToLower();
                    //                            sw.WriteLine($"import {{ {elementType.Name.Substring(0, elementType.Name.Length - 2)} }} from \"./{kebabCasePropertyTypeName}.generated\";"); // List<Object<int>>
                    //                            imports.Add(elementType.Name);
                    //                        }
                    //                        else
                    //                        {
                    //                            sw.WriteLine($"import {{ {elementType.Name} }} from \"./{kebabCasePropertyTypeName}.generated\";"); //UserDTO
                    //                            imports.Add(elementType.Name);
                    //                        }
                    //                    }
                    //                }
                    //                else
                    //                {
                    //                    string kebabCasePropertyTypeName = Regex.Replace(s.PropertyType.Name, "([a-z])([A-Z])", "$1-$2").ToLower();
                    //                    sw.WriteLine($"import {{ {s.PropertyType.Name} }} from \"./{kebabCasePropertyTypeName}.generated\";");
                    //                }
                    //            }
                    //        }
                    //        if (class_.BaseType != null && class_.BaseType != typeof(object))
                    //        {
                    //            string kebabCaseNameBaseClassName = Regex.Replace(class_.BaseType.Name, "([a-z])([A-Z])", "$1-$2").ToLower();
                    //            sw.WriteLine($"import {{ {class_.BaseType.Name} }} from \"./{kebabCaseNameBaseClassName}.generated\";");

                    //        }
                    //        if (class_.IsGenericType)
                    //        {
                    //            sw.WriteLine();
                    //            sw.Write($"export interface {class_.Name.Substring(0, class_.Name.Length - 2)}");
                    //        }
                    //        else
                    //        {
                    //            sw.WriteLine();
                    //            sw.Write($"export interface {class_.Name} ");
                    //        }
                    //        if (class_.BaseType != null && class_.BaseType != typeof(object))
                    //        {
                    //            sw.Write($"extends {class_.BaseType.Name}");

                    //        }
                    //        sw.WriteLine("{");
                    //        foreach (var s in classProperties)
                    //        {
                    //            var propertyType = s.PropertyType;

                    //            if (propertyType == typeof(string))
                    //            {
                    //                sw.WriteLine($"{char.ToLower(s.Name[0])}{s.Name.Substring(1)}?: string;");
                    //            }
                    //            else if (propertyType == typeof(List<string>))
                    //            {
                    //                sw.WriteLine($"{char.ToLower(s.Name[0])}{s.Name.Substring(1)}?: string[];");
                    //            }
                    //            else if (propertyType == typeof(int) || propertyType == typeof(long) || propertyType == typeof(short) ||
                    //                     propertyType == typeof(double) || propertyType == typeof(decimal) || propertyType == typeof(byte))
                    //            {
                    //                sw.WriteLine($"{char.ToLower(s.Name[0])}{s.Name.Substring(1)}: number;");
                    //            }
                    //            else if (propertyType == typeof(int?) || propertyType == typeof(long?) || propertyType == typeof(short?) ||
                    //                     propertyType == typeof(double?) || propertyType == typeof(decimal?) || propertyType == typeof(byte?))
                    //            {
                    //                sw.WriteLine($"{char.ToLower(s.Name[0])}{s.Name.Substring(1)}?: number;");
                    //            }
                    //            else if (propertyType == typeof(List<int>) || propertyType == typeof(List<long>) || propertyType == typeof(List<short>) ||
                    //                     propertyType == typeof(List<double>) || propertyType == typeof(List<decimal>) || propertyType == typeof(List<byte>))
                    //            {
                    //                sw.WriteLine($"{char.ToLower(s.Name[0])}{s.Name.Substring(1)}?: number[];");
                    //            }
                    //            else if (propertyType == typeof(bool))
                    //            {
                    //                sw.WriteLine($"{char.ToLower(s.Name[0])}{s.Name.Substring(1)}: boolean;");
                    //            }
                    //            else if (propertyType == typeof(bool?))
                    //            {
                    //                sw.WriteLine($"{char.ToLower(s.Name[0])}{s.Name.Substring(1)}?: boolean;");
                    //            }
                    //            else if (propertyType == typeof(DateTime))
                    //            {
                    //                sw.WriteLine($"{char.ToLower(s.Name[0])}{s.Name.Substring(1)}: Date;");
                    //            }
                    //            else if (propertyType == typeof(DateTime?))
                    //            {
                    //                sw.WriteLine($"{char.ToLower(s.Name[0])}{s.Name.Substring(1)}?: Date;");
                    //            }
                    //            else if (propertyType == typeof(List<DateTime>))
                    //            {
                    //                sw.WriteLine($"{char.ToLower(s.Name[0])}{s.Name.Substring(1)}?: Date[];");
                    //            }
                    //            else if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))
                    //            {
                    //                Type elementType = propertyType.GetGenericArguments()[0]; // List<UserDTO>

                    //                if (elementType.IsGenericType)
                    //                {
                    //                    sw.WriteLine($"{char.ToLower(s.Name[0])}{s.Name.Substring(1)}?: {elementType.Name.Substring(0, elementType.Name.Length - 2)}[];");
                    //                }
                    //                else
                    //                {
                    //                    sw.WriteLine($"{char.ToLower(s.Name[0])}{s.Name.Substring(1)}?: {elementType.Name}[];");
                    //                }
                    //            }
                    //            else
                    //            {
                    //                sw.WriteLine($"{char.ToLower(s.Name[0])}{s.Name.Substring(1)}: any;");
                    //            }
                    //        }
                    //        sw.WriteLine("}");
                    //        sw.Close();

                    //}
                    #endregion

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading the assembly: {ex.Message}");
                }
            }
            }
        public string ExtractClassName(string dtoContent)
        {
            // Use regular expressions or other methods to extract the class name
            // For simplicity, let's assume a simple pattern: "class ClassName"
            Match match = Regex.Match(dtoContent, @"class\s+(\w+)");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            // Handle if no match is found
            return null;
        }

        public string GetKebabCaseClassName(Type class_)
        {
            if (class_.IsGenericType)
            {
                string className = class_.Name.Substring(0, class_.Name.Length - 2); // zasto ovo radim?
                return Regex.Replace(className, "([a-z])([A-Z])", "$1-$2").ToLower();
            }
            else
            {
                string className = class_.Name;
                return Regex.Replace(className, "([a-z])([A-Z])", "$1-$2").ToLower();
            }
        }

        public string GenerateTypeScriptEntity(string className, string dtoContent)
        {
            // uzimam className
            string tsEntity = $"class {className}";

            // proveravam da li extenduje nesto
            string inheritancePattern = @"class\s+\w+\s*:\s*(\w+)";
            Match match = Regex.Match(dtoContent, inheritancePattern);
            if (match.Success)
            {
                // uzimam ime extendujuce
                string baseTypeName = match.Groups[1].Value;

                tsEntity += GenerateTypeScriptEntity(baseTypeName, dtoContent);
            }

            tsEntity += " {\n\t// Add TypeScript properties here\n}\n";

            return tsEntity;
        }
}
}
