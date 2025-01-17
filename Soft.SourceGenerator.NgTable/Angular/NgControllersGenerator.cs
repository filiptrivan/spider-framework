using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using Soft.SourceGenerator.NgTable.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Soft.SourceGenerators.Helpers;
using System.Reflection;
using Soft.SourceGenerators.Models;
using System.Runtime.Serialization.Json;
using System.Collections.Immutable;
using System.Data;
using Soft.SourceGenerators.Enums;
using Soft.SourceGenerators;

namespace Soft.SourceGenerator.NgTable.Angular
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
            IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = Helper.GetClassInrementalValuesProvider(context.SyntaxProvider, new List<NamespaceExtensionCodes>
                {
                    NamespaceExtensionCodes.Controllers,
                });

            IncrementalValueProvider<List<SoftClass>> referencedProjectClasses = Helper.GetIncrementalValueProviderClassesFromReferencedAssemblies(context,
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

        private static void Execute(IList<ClassDeclarationSyntax> classes, List<SoftClass> referencedProjectClasses, string callingProjectDirectory, SourceProductionContext context)
        {
            if (classes.Count <= 1)
                return; // FT: one because of config settings

            // ...\API\PlayertyLoyals.Business -> ...\Angular\src\app\business\services\api\api.service.generated.ts
            string outputPath = callingProjectDirectory.ReplaceEverythingAfter(@"\API\", @"\Angular\src\app\business\services\api\api.service.generated.ts");

            List<SoftClass> softClasses = Helper.GetSoftClasses(classes, referencedProjectClasses);

            List<SoftClass> controllerClasses = softClasses
                .Where(x => x.Namespace.EndsWith($".{NamespaceExtensionCodes.Controllers}"))
                .ToList();

            List<SoftClass> referencedDTOClasses = referencedProjectClasses
                .Where(x => x.Namespace.EndsWith($".{NamespaceExtensionCodes.DTO}"))
                .ToList();

            List<SoftClass> referencedEntityClasses = referencedProjectClasses
                .Where(x => x.Namespace.EndsWith($".{NamespaceExtensionCodes.Entities}"))
                .ToList();

            string result = $$"""
{{string.Join("\n", GetImports(referencedDTOClasses))}}

@Injectable()
export class ApiGeneratedService extends ApiSecurityService {

    constructor(protected override http: HttpClient) {
        super(http);
    }

{{string.Join("\n\n", GetAngularHttpMethods(controllerClasses, referencedEntityClasses, referencedDTOClasses))}}

}
""";

            Helper.WriteToTheFile(result, outputPath);
        }

        private static List<string> GetAngularHttpMethods(List<SoftClass> controllerClasses, List<SoftClass> entities, List<SoftClass> referencedDTOClasses)
        {
            List<string> result = new List<string>();
            HashSet<string> alreadyAddedMethods = new HashSet<string>();
            HashSet<string> entityNamesForGeneration = new HashSet<string>();

            foreach (SoftClass controllerClass in controllerClasses)
            {
                string controllerName = controllerClass.Name.Replace("Controller", "");

                if (controllerClass.BaseType != "SoftBaseController")
                    entityNamesForGeneration.Add(controllerClass.BaseType.Replace("BaseController", ""));

                foreach (SoftMethod controllerMethod in controllerClass.Methods)
                {
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

            foreach (SoftClass entity in entities.Where(x => entityNamesForGeneration.Contains(x.Name)))
                result.Add(GetBaseAngularControllerMethods(entity, entities, alreadyAddedMethods));

            return result;
        }

        private static List<string> GetImports(List<SoftClass> DTOClasses)
        {
            List<string> result = new List<string>();

            result.Add($$"""
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { ApiSecurityService } from './api.service.security';
import { Namebook } from 'src/app/core/entities/namebook';
import { Codebook } from 'src/app/core/entities/codebook';
import { SimpleSaveResult } from 'src/app/core/entities/simple-save-result';
import { TableFilter } from 'src/app/core/entities/table-filter';
import { TableResponse } from 'src/app/core/entities/table-response';
import { LazyLoadSelectedIdsResult } from 'src/app/core/entities/lazy-load-selected-ids-result';
""");

            foreach (SoftClass DTOClass in DTOClasses)
            {
                string[] projectNameHelper = DTOClass.Namespace.Split('.');
                string projectName = projectNameHelper[projectNameHelper.Length - 2];
                string ngType = DTOClass.Name.Replace("DTO", "");

                if (Helper.BaseClassNames.Contains(ngType))
                    continue;

                result.Add($$"""
import { {{ngType}} } from '../../entities/{{projectName.FromPascalToKebabCase()}}-entities.generated';
""");
            }

            return result;
        }

        #region Custom Angular Controller Method

        private static string GetCustomAngularControllerMethod(SoftMethod controllerMethod, string controllerName)
        {
            string angularReturnType = Helper.GetAngularType(controllerMethod.ReturnType);

            HttpTypeCodes httpType = GetHttpType(controllerMethod);

            Dictionary<string, string> inputParameters = controllerMethod.Parameters
                .ToDictionary(
                    x => x.Name,
                    x => Helper.GetAngularType(x.Type)
                );

            string httpOptions = GetHttpOptions(controllerMethod);

            return GetAngularControllerMethod(controllerMethod.Name, inputParameters, angularReturnType, httpType, controllerName, httpOptions);
        }

        private static string GetHttpOptions(SoftMethod controllerMethod)
        {
            if (Helper.GetAngularType(controllerMethod.ReturnType) == "string")
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

        private static HttpTypeCodes GetHttpType(SoftMethod controllerMethod)
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

        private static string GetBaseAngularControllerMethods(SoftClass entity, List<SoftClass> entities, HashSet<string> alreadyAddedMethods)
        {
            return $$"""
{{GetBaseTableDataAngularControllerMethod(entity, alreadyAddedMethods)}}

{{GetBaseExportTableDataToExcelAngularControllerMethod(entity, alreadyAddedMethods)}}

{{GetBaseGetListAngularControllerMethod(entity, alreadyAddedMethods)}}

{{GetBaseGetAngularControllerMethod(entity, alreadyAddedMethods)}}

{{GetBaseGetListForAutocompleteAngularControllerMethod(entity, alreadyAddedMethods)}}

{{GetBaseGetListForDropdownAngularControllerMethod(entity, alreadyAddedMethods)}}

{{string.Join("\n\n", GetBaseOrderedOneToManyAngularControllerMethods(entity, entities, alreadyAddedMethods))}}

{{string.Join("\n\n", GetBaseManyToManyAngularControllerMethods(entity, entities, alreadyAddedMethods))}}

{{GetBaseSaveAngularControllerMethod(entity, alreadyAddedMethods)}}

{{string.Join("\n\n", GetBaseUploadBlobAngularControllerMethods(entity, entities, alreadyAddedMethods))}}

{{GetBaseDeleteAngularControllerMethods(entity, alreadyAddedMethods)}}

""";
        }

        private static string GetCustomFromFormControllerMethod(SoftMethod controllerMethod, string controllerName, List<SoftClass> DTOList)
        {
            SoftParameter parameter = controllerMethod.Parameters.Single();
            SoftClass parameterType = DTOList.Where(x => x.Name == parameter.Type).SingleOrDefault();

            return $$"""
    excelManualUpdatePoints = (dto: {{parameter.Type.Replace("DTO", "")}}): Observable<any> => { 
        let formData = new FormData();
{{string.Join("\n", GetFormDataAppends(parameterType))}}
        return this.http.post(`${environment.apiUrl}/{{controllerName}}/ExcelManualUpdatePoints`, formData, environment.httpOptions);
    }
""";
        }

        private static List<string> GetFormDataAppends(SoftClass dto)
        {
            List<string> result = new List<string>();

            foreach (SoftProperty property in dto.Properties)
            {
                
                    result.Add($$"""
        formData.append('{{property.Name}}', dto.{{GetFormDataAppendedValue(property)}});
""");
                
            }

            return result;
        }

        private static string GetFormDataAppendedValue(SoftProperty property)
        {
            if (property.Type == "IFormFile")
                return property.Name.FirstCharToLower();
            else
                return $"{property.Name.FirstCharToLower()}.toString()";
        }

        #endregion

        #region Generated Angular Controller Methods

        #region Ordered One To Many

        private static List<string> GetBaseOrderedOneToManyAngularControllerMethods(SoftClass entity, List<SoftClass> entities, HashSet<string> alreadyAddedMethods)
        {
            List<string> result = new List<string>();

            List<SoftProperty> uiOrderedOneToManyProperties = Helper.GetUIOrderedOneToManyProperties(entity);

            foreach (SoftProperty property in uiOrderedOneToManyProperties)
            {
                result.Add(GetBaseOrderedOneToManyAngularControllerMethod(property, entity, alreadyAddedMethods));
            }

            return result;
        }

        private static string GetBaseOrderedOneToManyAngularControllerMethod(SoftProperty uiOrderedOneToManyProperty, SoftClass entity, HashSet<string> alreadyAddedMethods)
        {
            string methodName = $"GetOrdered{uiOrderedOneToManyProperty.Name}For{entity.Name}";

            if (alreadyAddedMethods.Contains(methodName))
                return null;

            Dictionary<string, string> getAndDeleteParameter = new Dictionary<string, string> { { "id", "number" } };

            return GetAngularControllerMethod(
                methodName, getAndDeleteParameter, $"{Helper.ExtractTypeFromGenericType(uiOrderedOneToManyProperty.Type)}[]", HttpTypeCodes.Get, entity.ControllerName, Settings.HttpOptionsBase
            );
        }

        #endregion

        #region Many To Many

        private static List<string> GetBaseManyToManyAngularControllerMethods(SoftClass entity, List<SoftClass> entities, HashSet<string> alreadyAddedMethods)
        {
            List<string> result = new List<string>();

            foreach (SoftProperty property in entity.Properties)
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

        private static string GetBaseSimpleManyToManyTableLazyLoadAngularControllerMethod(SoftProperty property, SoftClass entity, HashSet<string> alreadyAddedMethods)
        {
            string methodName = $"LazyLoadSelected{property.Name}IdsFor{entity.Name}";

            if (alreadyAddedMethods.Contains(methodName))
                return null;

            Dictionary<string, string> postAndPutParameter = new Dictionary<string, string> { { "tableFilterDTO", "TableFilter" } };

            return GetAngularControllerMethod(methodName, postAndPutParameter, "LazyLoadSelectedIdsResult", HttpTypeCodes.Post, entity.ControllerName, Settings.HttpOptionsSkipSpinner);
        }

        private static string GetBaseSimpleManyToManyTableDataAngularControllerMethod(SoftProperty property, SoftClass entity, List<SoftClass> entities, HashSet<string> alreadyAddedMethods)
        {
            string methodName = $"Get{property.Name}TableDataFor{entity.Name}";

            if (alreadyAddedMethods.Contains(methodName))
                return null;

            Dictionary<string, string> postAndPutParameter = new Dictionary<string, string> { { "tableFilterDTO", "TableFilter" } };

            SoftClass extractedEntity = entities.Where(x => x.Name == Helper.ExtractTypeFromGenericType(property.Type)).SingleOrDefault();

            return GetAngularControllerMethod(methodName, postAndPutParameter, $"TableResponse<{extractedEntity.Name}>", HttpTypeCodes.Post, entity.ControllerName, Settings.HttpOptionsSkipSpinner);
        }

        private static string GetBaseSimpleManyToManyTableDataExportAngularControllerMethod(SoftProperty property, SoftClass entity, HashSet<string> alreadyAddedMethods)
        {
            string methodName = $"Export{property.Name}TableDataToExcelFor{entity.Name}";

            if (alreadyAddedMethods.Contains(methodName))
                return null;

            Dictionary<string, string> postAndPutParameter = new Dictionary<string, string> { { "tableFilterDTO", "TableFilter" } };

            return GetAngularControllerMethod(methodName, postAndPutParameter, "any", HttpTypeCodes.Post, entity.ControllerName, Settings.HttpOptionsBlob);
        }

        #endregion

        #region Multi Control Types

        private static string GetBaseManyToManyMultiControlTypesAngularControllerMethod(SoftProperty property, SoftClass entity, HashSet<string> alreadyAddedMethods)
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

        private static string GetBaseDeleteAngularControllerMethods(SoftClass entity, HashSet<string> alreadyAddedMethods)
        {
            if (entity.IsReadonlyObject())
                return null;

            string methodName = $"Delete{entity.Name}";

            if (alreadyAddedMethods.Contains(methodName))
                return null;

            Dictionary<string, string> getAndDeleteParameters = new Dictionary<string, string> { { "id", "number" } };

            return GetAngularControllerMethod(methodName, getAndDeleteParameters, "any", HttpTypeCodes.Delete, entity.ControllerName, Settings.HttpOptionsBase);
        }

        private static List<string> GetBaseUploadBlobAngularControllerMethods(SoftClass entity, List<SoftClass> entities, HashSet<string> alreadyAddedMethods)
        {
            List<string> result = new List<string>();

            List<SoftProperty> blobProperies = Helper.GetBlobProperties(entity.Properties);

            foreach (SoftProperty property in blobProperies)
            {
                result.Add(GetBaseUploadBlobAngularControllerMethod(property, entity, alreadyAddedMethods));
            }

            return result;
        }

        private static string GetBaseUploadBlobAngularControllerMethod(SoftProperty blobProperty, SoftClass entity, HashSet<string> alreadyAddedMethods)
        {
            string methodName = $"Upload{blobProperty.Name}For{entity.Name}";

            if (alreadyAddedMethods.Contains(methodName))
                return null;

            Dictionary<string, string> postAndPutParameter = new Dictionary<string, string> { { "file", "any" } };

            return GetAngularControllerMethod(methodName, postAndPutParameter, "string", HttpTypeCodes.Post, entity.ControllerName, Settings.HttpOptionsText);
        }

        private static string GetBaseSaveAngularControllerMethod(SoftClass entity, HashSet<string> alreadyAddedMethods)
        {
            if (entity.IsReadonlyObject())
                return null;

            string methodName = $"Save{entity.Name}";

            if (alreadyAddedMethods.Contains(methodName))
                return null;

            Dictionary<string, string> postAndPutParameter = new Dictionary<string, string> { { "saveBodyDTO", $"{entity.Name}SaveBody" } };

            return GetAngularControllerMethod(methodName, postAndPutParameter, $"{entity.Name}SaveBody", HttpTypeCodes.Put, entity.ControllerName, Settings.HttpOptionsBase);
        }

        private static string GetBaseGetListForDropdownAngularControllerMethod(SoftClass entity, HashSet<string> alreadyAddedMethods)
        {
            string methodName = $"Get{entity.Name}ListForDropdown";

            if (alreadyAddedMethods.Contains(methodName))
                return null;

            return GetAngularControllerMethod(methodName, null, "Namebook[]", HttpTypeCodes.Get, entity.ControllerName, Settings.HttpOptionsSkipSpinner);
        }

        private static string GetBaseGetListForAutocompleteAngularControllerMethod(SoftClass entity, HashSet<string> alreadyAddedMethods)
        {
            string methodName = $"Get{entity.Name}ListForAutocomplete";

            if (alreadyAddedMethods.Contains(methodName))
                return null;

            Dictionary<string, string> getAndDeleteParameters = new Dictionary<string, string> { { "limit", "number" }, { "query", "string" } };

            return GetAngularControllerMethod(methodName, getAndDeleteParameters, "Namebook[]", HttpTypeCodes.Get, entity.ControllerName, Settings.HttpOptionsSkipSpinner);
        }

        private static string GetBaseGetAngularControllerMethod(SoftClass entity, HashSet<string> alreadyAddedMethods)
        {
            string methodName = $"Get{entity.Name}";

            if (alreadyAddedMethods.Contains(methodName))
                return null;

            Dictionary<string, string> getAndDeleteParameters = new Dictionary<string, string> { { "id", "number" } };

            return GetAngularControllerMethod(methodName, getAndDeleteParameters, $"{entity.Name}", HttpTypeCodes.Get, entity.ControllerName, Settings.HttpOptionsBase);
        }

        private static string GetBaseGetListAngularControllerMethod(SoftClass entity, HashSet<string> alreadyAddedMethods)
        {
            string methodName = $"Get{entity.Name}List";

            if (alreadyAddedMethods.Contains(methodName))
                return null;

            return GetAngularControllerMethod(methodName, null, $"{entity.Name}[]", HttpTypeCodes.Get, entity.ControllerName, Settings.HttpOptionsBase);
        }

        private static string GetBaseExportTableDataToExcelAngularControllerMethod(SoftClass entity, HashSet<string> alreadyAddedMethods)
        {
            string methodName = $"Export{entity.Name}TableDataToExcel";

            if (alreadyAddedMethods.Contains(methodName))
                return null;

            Dictionary<string, string> postAndPutParameter = new Dictionary<string, string> { { "tableFilterDTO", "TableFilter" } };

            return GetAngularControllerMethod(methodName, postAndPutParameter, "any", HttpTypeCodes.Post, entity.ControllerName, Settings.HttpOptionsBlob);
        }

        private static string GetBaseTableDataAngularControllerMethod(SoftClass entity, HashSet<string> alreadyAddedMethods)
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
        return this.http.{{httpType.ToString().FirstCharToLower()}}{{GetReturnTypeAfterHttpType(returnType)}}(`${environment.apiUrl}/{{controllerName}}/{{methodName}}{{GetGetAndDeleteParameters(inputParameters, httpType)}}`{{GetPostAndPutParameters(inputParameters, httpType)}}{{httpOptions}});
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

            return $"?{string.Join("&", getAndDeleteParameters.Select(x => $"{x.Key}=${{{x.Key}}}"))}";
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
