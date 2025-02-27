using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Spider.SourceGenerators.Shared;
using System.Reflection;
using Spider.SourceGenerators.Models;
using System.Runtime.Serialization.Json;
using System.Collections.Immutable;
using System.Data;
using Spider.SourceGenerators.Enums;
using Spider.SourceGenerators;

namespace Spider.SourceGenerators.Angular
{
    [Generator]
    public class NgControllersGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
//#if DEBUG
//            if (!Debugger.IsAttached)
//            {
//                Debugger.Launch();
//            }
//#endif
            IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = Helpers.GetClassIncrementalValuesProvider(context.SyntaxProvider, new List<NamespaceExtensionCodes>
                {
                    NamespaceExtensionCodes.Controllers,
                });

            IncrementalValueProvider<List<SpiderClass>> referencedProjectClasses = Helpers.GetIncrementalValueProviderClassesFromReferencedAssemblies(context,
                new List<NamespaceExtensionCodes>
                {
                    NamespaceExtensionCodes.Entities,
                    NamespaceExtensionCodes.DTO,
                });

            IncrementalValueProvider<string> callingProjectDirectory = context.GetCallingPath();

            var combined = classDeclarations.Collect()
                .Combine(referencedProjectClasses)
                .Combine(callingProjectDirectory);

            context.RegisterImplementationSourceOutput(combined, static (spc, source) =>
            {
                var (classesAndEntities, callingPath) = source;
                var (classes, referencedClasses) = classesAndEntities;

                Execute(classes, referencedClasses, callingPath, spc);
            });
        }

        private static void Execute(IList<ClassDeclarationSyntax> classes, List<SpiderClass> referencedProjectClasses, string callingProjectDirectory, SourceProductionContext context)
        {
            if (classes.Count <= 1)
                return; // FT: one because of config settings

            if (callingProjectDirectory.Contains(".WebAPI") == false)
                return;

            // ...\API\PlayertyLoyals.Business -> ...\Angular\src\app\business\services\api\api.service.generated.ts
            string outputPath = callingProjectDirectory.ReplaceEverythingAfter(@"\API\", @"\Angular\src\app\business\services\api\api.service.generated.ts");

            List<SpiderClass> spiderClasses = Helpers.GetSpiderClasses(classes, referencedProjectClasses);

            List<SpiderClass> controllerClasses = spiderClasses
                .Where(x => x.Namespace.EndsWith($".{NamespaceExtensionCodes.Controllers}"))
                .ToList();

            List<SpiderClass> referencedDTOClasses = referencedProjectClasses
                .Where(x => x.Namespace.EndsWith($".{NamespaceExtensionCodes.DTO}"))
                .ToList();

            List<SpiderClass> referencedEntityClasses = referencedProjectClasses
                .Where(x => x.Namespace.EndsWith($".{NamespaceExtensionCodes.Entities}"))
                .ToList();

            string result = $$"""
{{string.Join("\n", GetImports(referencedDTOClasses))}}

@Injectable({
    providedIn: 'root'
})
export class ApiGeneratedService extends ApiSecurityService {

    constructor(
        protected override http: HttpClient,
        protected override config: ConfigService
    ) {
        super(http, config);
    }

{{string.Join("\n\n", GetAngularHttpMethods(controllerClasses, referencedEntityClasses, referencedDTOClasses))}}

}
""";

            Helpers.WriteToTheFile(result, outputPath);
        }

        private static List<string> GetAngularHttpMethods(List<SpiderClass> controllerClasses, List<SpiderClass> entities, List<SpiderClass> referencedDTOClasses)
        {
            List<string> result = new();
            HashSet<string> alreadyAddedMethods = new HashSet<string>();
            HashSet<string> entityNamesForGeneration = new HashSet<string>();

            foreach (SpiderClass controllerClass in controllerClasses)
            {
                string controllerName = controllerClass.Name.Replace("Controller", "");

                if (controllerClass.BaseType != "SpiderBaseController")
                    entityNamesForGeneration.Add(controllerClass.BaseType.Replace("BaseController", ""));

                foreach (SpiderMethod controllerMethod in controllerClass.Methods)
                {
                    if (controllerMethod.HasUIDoNotGenerateAttribute())
                        continue;

                    alreadyAddedMethods.Add(controllerMethod.Name);

                    if (controllerMethod.Parameters.Any(x => x.HasFromFormAttribute()) && controllerMethod.Parameters.Any(x => x.Type == "IFormFile") == false)
                    {
                        result.Add(GetCustomFromFormControllerMethod(controllerMethod, controllerName, referencedDTOClasses));
                    }
                    else
                    {
                        result.Add(GetCustomAngularControllerMethod(controllerMethod, controllerName));
                    }
                }
            }

            foreach (SpiderClass entity in entities.Where(x => entityNamesForGeneration.Contains(x.Name)))
                result.Add(GetBaseAngularControllerMethods(entity, entities, alreadyAddedMethods));

            return result;
        }

        private static List<string> GetImports(List<SpiderClass> DTOClasses)
        {
            List<string> result = new();

            result.Add($$"""
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiSecurityService, TableFilter, TableResponse, Namebook, Codebook, LazyLoadSelectedIdsResult, VerificationTokenRequest, AuthResult, ExternalProvider } from '@playerty/spider';
import { ConfigService } from '../config.service';
""");

            foreach (SpiderClass DTOClass in DTOClasses)
            {
                string[] projectNameHelper = DTOClass.Namespace.Split('.');
                string projectName = projectNameHelper[projectNameHelper.Length - 2];

                if (projectName == "Security")
                    continue;

                string ngType = DTOClass.Name.Replace("DTO", "");

                if (Helpers.BaseClassNames.Contains(ngType))
                    continue;

                result.Add($$"""
import { {{ngType}} } from '../../entities/{{projectName.FromPascalToKebabCase()}}-entities.generated';
""");
            }

            return result;
        }

        #region Custom Angular Controller Method

        private static string GetCustomAngularControllerMethod(SpiderMethod controllerMethod, string controllerName)
        {
            string angularReturnType = Helpers.GetAngularType(controllerMethod.ReturnType);

            HttpTypeCodes httpType = GetHttpType(controllerMethod);

            Dictionary<string, string> inputParameters = controllerMethod.Parameters
                .ToDictionary(
                    x => x.Name,
                    x => Helpers.GetAngularType(x.Type)
                );

            string httpOptions = GetHttpOptions(controllerMethod);

            return GetAngularControllerMethod(controllerMethod.Name, inputParameters, angularReturnType, httpType, controllerName, httpOptions);
        }

        private static string GetHttpOptions(SpiderMethod controllerMethod)
        {
            if (Helpers.GetAngularType(controllerMethod.ReturnType) == "string")
                return Settings.HttpOptionsText;

            if (controllerMethod.ReturnType.Contains("IActionResult"))
                return Settings.HttpOptionsBlob;

            bool skipSpinner = controllerMethod.Attributes.Any(attr => attr.Name == "SkipSpinner");

            if (skipSpinner || 
                controllerMethod.ReturnType.Contains("NamebookDTO") || 
                controllerMethod.ReturnType.Contains("CodebookDTO") || 
                controllerMethod.ReturnType.Contains("TableResponseDTO") ||
                controllerMethod.ReturnType.Contains("LazyLoadSelectedIdsResultDTO"))
            {
                return Settings.HttpOptionsSkipSpinner;
            }

            return Settings.HttpOptionsBase;    
        }

        private static HttpTypeCodes GetHttpType(SpiderMethod controllerMethod)
        {
            if (controllerMethod.Attributes.Any(attr => attr.Name == "HttpGet"))
            {
                return HttpTypeCodes.Get;
            }
            else if (controllerMethod.Attributes.Any(attr => attr.Name == "HttpPost"))
            {
                return HttpTypeCodes.Post;
            }
            else if (controllerMethod.Attributes.Any(attr => attr.Name == "HttpPut"))
            {
                return HttpTypeCodes.Put;
            }
            else if (controllerMethod.Attributes.Any(attr => attr.Name == "HttpDelete"))
            {
                return HttpTypeCodes.Delete;
            }
            else
            {
                throw new NotImplementedException("Http type doesn't exist.");
            }
        }

        private static string GetBaseAngularControllerMethods(SpiderClass entity, List<SpiderClass> entities, HashSet<string> alreadyAddedMethods)
        {
            if (entity.IsManyToMany()) // TODO FT: Do something with M2M entities
                return null;

            return $$"""
{{GetBaseTableDataAngularControllerMethod(entity, alreadyAddedMethods)}}

{{GetBaseExportTableDataToExcelAngularControllerMethod(entity, alreadyAddedMethods)}}

{{GetBaseGetListAngularControllerMethod(entity, alreadyAddedMethods)}}

{{GetBaseGetMainUIFormAngularControllerMethod(entity, alreadyAddedMethods)}}

{{GetBaseGetAngularControllerMethod(entity, alreadyAddedMethods)}}

{{GetBaseGetListForAutocompleteAngularControllerMethods(entity, alreadyAddedMethods)}}

{{GetBaseGetListForDropdownAngularControllerMethods(entity, alreadyAddedMethods)}}

{{string.Join("\n\n", GetBaseOrderedOneToManyAngularControllerMethods(entity, entities, alreadyAddedMethods))}}

{{string.Join("\n\n", GetBaseManyToManyAngularControllerMethods(entity, entities, alreadyAddedMethods))}}

{{GetBaseSaveAngularControllerMethod(entity, alreadyAddedMethods)}}

{{string.Join("\n\n", GetBaseUploadBlobAngularControllerMethods(entity, entities, alreadyAddedMethods))}}

{{GetBaseDeleteAngularControllerMethods(entity, alreadyAddedMethods)}}

""";
        }

        private static string GetCustomFromFormControllerMethod(SpiderMethod controllerMethod, string controllerName, List<SpiderClass> DTOList)
        {
            SpiderParameter parameter = controllerMethod.Parameters.Single();
            SpiderClass parameterType = DTOList.Where(x => x.Name == parameter.Type).SingleOrDefault();

            return $$"""
    excelManualUpdatePoints = (dto: {{parameter.Type.Replace("DTO", "")}}): Observable<any> => { 
        let formData = new FormData();
{{string.Join("\n", GetFormDataAppends(parameterType))}}
        return this.http.post(`${this.config.apiUrl}/{{controllerName}}/ExcelManualUpdatePoints`, formData, this.config.httpOptions);
    }
""";
        }

        private static List<string> GetFormDataAppends(SpiderClass dto)
        {
            List<string> result = new();

            foreach (SpiderProperty property in dto.Properties)
            {
                
                    result.Add($$"""
        formData.append('{{property.Name}}', dto.{{GetFormDataAppendedValue(property)}});
""");
                
            }

            return result;
        }

        private static string GetFormDataAppendedValue(SpiderProperty property)
        {
            if (property.Type == "IFormFile")
                return property.Name.FirstCharToLower();
            else
                return $"{property.Name.FirstCharToLower()}.toString()";
        }

        #endregion

        #region Generated Angular Controller Methods

        #region Ordered One To Many

        private static List<string> GetBaseOrderedOneToManyAngularControllerMethods(SpiderClass entity, List<SpiderClass> entities, HashSet<string> alreadyAddedMethods)
        {
            List<string> result = new();

            List<SpiderProperty> uiOrderedOneToManyProperties = Helpers.GetUIOrderedOneToManyProperties(entity);

            foreach (SpiderProperty property in uiOrderedOneToManyProperties)
            {
                result.Add(GetBaseOrderedOneToManyAngularControllerMethod(property, entity, alreadyAddedMethods));
            }

            return result;
        }

        private static string GetBaseOrderedOneToManyAngularControllerMethod(SpiderProperty uiOrderedOneToManyProperty, SpiderClass entity, HashSet<string> alreadyAddedMethods)
        {
            string methodName = $"GetOrdered{uiOrderedOneToManyProperty.Name}For{entity.Name}";

            if (alreadyAddedMethods.Contains(methodName))
                return null;

            Dictionary<string, string> getAndDeleteParameter = new Dictionary<string, string> { { "id", "number" } };

            return GetAngularControllerMethod(
                methodName, getAndDeleteParameter, $"{Helpers.ExtractTypeFromGenericType(uiOrderedOneToManyProperty.Type)}[]", HttpTypeCodes.Get, entity.ControllerName, Settings.HttpOptionsBase
            );
        }

        #endregion

        #region Many To Many

        private static List<string> GetBaseManyToManyAngularControllerMethods(SpiderClass entity, List<SpiderClass> entities, HashSet<string> alreadyAddedMethods)
        {
            List<string> result = new();

            foreach (SpiderProperty property in entity.Properties)
            {
                if (property.IsMultiSelectControlType() ||
                    property.IsMultiAutocompleteControlType())
                {
                    result.Add(GetBaseManyToManyMultiControlTypesAngularControllerMethod(property, entity, alreadyAddedMethods));
                }
                else if (property.HasSimpleManyToManyTableLazyLoadAttribute())
                {
                    result.Add(GetBaseSimpleManyToManyTableDataAngularControllerMethod(property, entity, entities, alreadyAddedMethods));
                    result.Add(GetBaseSimpleManyToManyTableDataExportAngularControllerMethod(property, entity, alreadyAddedMethods));
                    result.Add(GetBaseSimpleManyToManyTableLazyLoadAngularControllerMethod(property, entity, alreadyAddedMethods));
                }
            }

            return result;
        }

        #region Simple Many To Many Table Lazy Load

        private static string GetBaseSimpleManyToManyTableLazyLoadAngularControllerMethod(SpiderProperty property, SpiderClass entity, HashSet<string> alreadyAddedMethods)
        {
            string methodName = $"LazyLoadSelected{property.Name}IdsFor{entity.Name}";

            if (alreadyAddedMethods.Contains(methodName))
                return null;

            Dictionary<string, string> postAndPutParameter = new Dictionary<string, string> { { "tableFilterDTO", "TableFilter" } };

            return GetAngularControllerMethod(methodName, postAndPutParameter, "LazyLoadSelectedIdsResult", HttpTypeCodes.Post, entity.ControllerName, Settings.HttpOptionsSkipSpinner);
        }

        private static string GetBaseSimpleManyToManyTableDataAngularControllerMethod(SpiderProperty property, SpiderClass entity, List<SpiderClass> entities, HashSet<string> alreadyAddedMethods)
        {
            string methodName = $"Get{property.Name}TableDataFor{entity.Name}";

            if (alreadyAddedMethods.Contains(methodName))
                return null;

            Dictionary<string, string> postAndPutParameter = new Dictionary<string, string> { { "tableFilterDTO", "TableFilter" } };

            SpiderClass extractedEntity = entities.Where(x => x.Name == Helpers.ExtractTypeFromGenericType(property.Type)).SingleOrDefault();

            return GetAngularControllerMethod(methodName, postAndPutParameter, $"TableResponse<{extractedEntity.Name}>", HttpTypeCodes.Post, entity.ControllerName, Settings.HttpOptionsSkipSpinner);
        }

        private static string GetBaseSimpleManyToManyTableDataExportAngularControllerMethod(SpiderProperty property, SpiderClass entity, HashSet<string> alreadyAddedMethods)
        {
            string methodName = $"Export{property.Name}TableDataToExcelFor{entity.Name}";

            if (alreadyAddedMethods.Contains(methodName))
                return null;

            Dictionary<string, string> postAndPutParameter = new Dictionary<string, string> { { "tableFilterDTO", "TableFilter" } };

            return GetAngularControllerMethod(methodName, postAndPutParameter, "any", HttpTypeCodes.Post, entity.ControllerName, Settings.HttpOptionsBlob);
        }

        #endregion

        #region Multi Control Types

        private static string GetBaseManyToManyMultiControlTypesAngularControllerMethod(SpiderProperty property, SpiderClass entity, HashSet<string> alreadyAddedMethods)
        {
            string methodName = $"Get{property.Name}NamebookListFor{entity.Name}";

            if (alreadyAddedMethods.Contains(methodName))
                return null;

            Dictionary<string, string> getAndDeleteParameter = new Dictionary<string, string> { { "id", "number" } };

            return GetAngularControllerMethod(
                methodName, getAndDeleteParameter, "Namebook[]", HttpTypeCodes.Get, entity.ControllerName, Settings.HttpOptionsSkipSpinner
            );
        }

        #endregion

        #endregion

        private static string GetBaseDeleteAngularControllerMethods(SpiderClass entity, HashSet<string> alreadyAddedMethods)
        {
            if (entity.IsReadonlyObject())
                return null;

            string methodName = $"Delete{entity.Name}";

            if (alreadyAddedMethods.Contains(methodName))
                return null;

            Dictionary<string, string> getAndDeleteParameters = new Dictionary<string, string> { { "id", "number" } };

            return GetAngularControllerMethod(methodName, getAndDeleteParameters, "any", HttpTypeCodes.Delete, entity.ControllerName, Settings.HttpOptionsBase);
        }

        private static List<string> GetBaseUploadBlobAngularControllerMethods(SpiderClass entity, List<SpiderClass> entities, HashSet<string> alreadyAddedMethods)
        {
            List<string> result = new();

            List<SpiderProperty> blobProperies = Helpers.GetBlobProperties(entity.Properties);

            foreach (SpiderProperty property in blobProperies)
            {
                result.Add(GetBaseUploadBlobAngularControllerMethod(property, entity, alreadyAddedMethods));
            }

            return result;
        }

        private static string GetBaseUploadBlobAngularControllerMethod(SpiderProperty blobProperty, SpiderClass entity, HashSet<string> alreadyAddedMethods)
        {
            string methodName = $"Upload{blobProperty.Name}For{entity.Name}";

            if (alreadyAddedMethods.Contains(methodName))
                return null;

            Dictionary<string, string> postAndPutParameter = new Dictionary<string, string> { { "file", "any" } };

            return GetAngularControllerMethod(methodName, postAndPutParameter, "string", HttpTypeCodes.Post, entity.ControllerName, Settings.HttpOptionsText);
        }

        private static string GetBaseSaveAngularControllerMethod(SpiderClass entity, HashSet<string> alreadyAddedMethods)
        {
            if (entity.IsReadonlyObject())
                return null;

            string methodName = $"Save{entity.Name}";

            if (alreadyAddedMethods.Contains(methodName))
                return null;

            Dictionary<string, string> postAndPutParameter = new Dictionary<string, string> { { "saveBodyDTO", $"{entity.Name}SaveBody" } };

            return GetAngularControllerMethod(methodName, postAndPutParameter, $"{entity.Name}SaveBody", HttpTypeCodes.Put, entity.ControllerName, Settings.HttpOptionsBase);
        }

        private static string GetBaseGetListForAutocompleteAngularControllerMethods(SpiderClass entity, HashSet<string> alreadyAddedMethods)
        {
            StringBuilder sb = new();

            foreach (SpiderProperty property in entity.Properties)
            {
                if (property.ShouldGenerateAutocompleteControllerMethod())
                {
                    string methodName = $"Get{property.Name}AutocompleteListFor{entity.Name}";

                    if (alreadyAddedMethods.Contains(methodName))
                        continue;

                    Dictionary<string, string> getAndDeleteParameters = new()
                    { 
                        { "limit", "number" }, 
                        { "filter", "string" }, 
                        { $"{entity.Name.FirstCharToLower()}Id?", "number"} 
                    };

                    sb.AppendLine(GetAngularControllerMethod(methodName, getAndDeleteParameters, "Namebook[]", HttpTypeCodes.Get, entity.ControllerName, Settings.HttpOptionsSkipSpinner));
                }
            }

            return sb.ToString();
        }

        private static string GetBaseGetListForDropdownAngularControllerMethods(SpiderClass entity, HashSet<string> alreadyAddedMethods)
        {
            StringBuilder sb = new();

            foreach (SpiderProperty property in entity.Properties)
            {
                if (property.ShouldGenerateDropdownControllerMethod())
                {
                    string methodName = $"Get{property.Name}DropdownListFor{entity.Name}";

                    if (alreadyAddedMethods.Contains(methodName))
                        continue;

                    Dictionary<string, string> getAndDeleteParameters = new() { { $"{entity.Name.FirstCharToLower()}Id?", "number"} };

                    sb.AppendLine(GetAngularControllerMethod(methodName, getAndDeleteParameters, "Namebook[]", HttpTypeCodes.Get, entity.ControllerName, Settings.HttpOptionsSkipSpinner));
                }
            }

            return sb.ToString();
        }

        private static string GetBaseGetMainUIFormAngularControllerMethod(SpiderClass entity, HashSet<string> alreadyAddedMethods)
        {
            string methodName = $"Get{entity.Name}MainUIFormDTO";

            if (alreadyAddedMethods.Contains(methodName))
                return null;

            Dictionary<string, string> getAndDeleteParameters = new() { { "id", "number" } };

            return GetAngularControllerMethod(methodName, getAndDeleteParameters, returnType:$"{entity.Name}MainUIForm", HttpTypeCodes.Get, entity.ControllerName, Settings.HttpOptionsBase);
        }

        private static string GetBaseGetAngularControllerMethod(SpiderClass entity, HashSet<string> alreadyAddedMethods)
        {
            string methodName = $"Get{entity.Name}";

            if (alreadyAddedMethods.Contains(methodName))
                return null;

            Dictionary<string, string> getAndDeleteParameters = new() { { "id", "number" } };

            return GetAngularControllerMethod(methodName, getAndDeleteParameters, $"{entity.Name}", HttpTypeCodes.Get, entity.ControllerName, Settings.HttpOptionsBase);
        }

        private static string GetBaseGetListAngularControllerMethod(SpiderClass entity, HashSet<string> alreadyAddedMethods)
        {
            string methodName = $"Get{entity.Name}List";

            if (alreadyAddedMethods.Contains(methodName))
                return null;

            return GetAngularControllerMethod(methodName, null, $"{entity.Name}[]", HttpTypeCodes.Get, entity.ControllerName, Settings.HttpOptionsBase);
        }

        private static string GetBaseExportTableDataToExcelAngularControllerMethod(SpiderClass entity, HashSet<string> alreadyAddedMethods)
        {
            string methodName = $"Export{entity.Name}TableDataToExcel";

            if (alreadyAddedMethods.Contains(methodName))
                return null;

            Dictionary<string, string> postAndPutParameter = new Dictionary<string, string> { { "tableFilterDTO", "TableFilter" } };

            return GetAngularControllerMethod(methodName, postAndPutParameter, "any", HttpTypeCodes.Post, entity.ControllerName, Settings.HttpOptionsBlob);
        }

        private static string GetBaseTableDataAngularControllerMethod(SpiderClass entity, HashSet<string> alreadyAddedMethods)
        {
            string methodName = $"Get{entity.Name}TableData";

            if (alreadyAddedMethods.Contains(methodName))
                return null;

            Dictionary<string, string> postAndPutParameter = new Dictionary<string, string> { { "tableFilterDTO", "TableFilter" } };

            return GetAngularControllerMethod(methodName, postAndPutParameter, $"TableResponse<{entity.Name}>", HttpTypeCodes.Post, entity.ControllerName, Settings.HttpOptionsSkipSpinner);
        }

        #endregion

        #region Helpers

        private static string GetAngularControllerMethod(
            string methodName,
            Dictionary<string, string> inputParameters,
            string returnType,
            HttpTypeCodes httpType,
            string controllerName,
            string httpOptions
        )
        {
            return $$"""
    {{methodName.FirstCharToLower()}} = ({{GetInputParameters(inputParameters)}}): Observable<{{returnType}}> => { 
        return this.http.{{httpType.ToString().FirstCharToLower()}}{{GetReturnTypeAfterHttpType(returnType)}}(`${this.config.apiUrl}/{{controllerName}}/{{methodName}}{{GetGetAndDeleteParameters(inputParameters, httpType)}}`{{GetPostAndPutParameters(inputParameters, httpType)}}{{httpOptions}});
    }
""";
        }

        private static string GetInputParameters(Dictionary<string, string> inputParameters)
        {
            if (inputParameters == null)
                return null;

            return string.Join(", ", inputParameters.Select(x => $"{x.Key}: {x.Value}"));
        }

        private static string GetReturnTypeAfterHttpType(string returnType)
        {
            if (returnType == "string")
                return null;

            if (returnType == "any")
                return null;

            return $"<{returnType}>";
        }

        private static string GetGetAndDeleteParameters(Dictionary<string, string> getAndDeleteParameters, HttpTypeCodes httpType)
        {
            if (httpType != HttpTypeCodes.Get && httpType != HttpTypeCodes.Delete)
                return null;

            if (getAndDeleteParameters == null || getAndDeleteParameters.Count == 0)
                return null;

            return $"?{string.Join("&", getAndDeleteParameters.Select(x => $"{x.Key.Replace("?", "")}=${{{x.Key.Replace("?", "")}}}"))}";
        }

        private static string GetPostAndPutParameters(Dictionary<string, string> postAndPutParameter, HttpTypeCodes httpType)
        {
            if (httpType != HttpTypeCodes.Post && httpType != HttpTypeCodes.Put)
                return null;

            if (postAndPutParameter == null || postAndPutParameter.Count == 0)
                return null;

            return $", {string.Join(", ", postAndPutParameter.Select(p => p.Key))}";
        }

        #endregion
    }
}
