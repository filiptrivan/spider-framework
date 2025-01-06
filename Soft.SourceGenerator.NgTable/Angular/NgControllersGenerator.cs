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
            IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => Helper.IsSyntaxTargetForGenerationControllers(s),
                    transform: static (ctx, _) => Helper.GetSemanticTargetForGenerationControllers(ctx))
                .Where(static c => c is not null);

            IncrementalValueProvider<List<SoftClass>> referencedProjectClasses = Helper.GetIncrementalValueProviderClassesFromReferencedAssemblies(context,
                new List<NamespaceExtensionCodes>
                {
                    NamespaceExtensionCodes.Entities,
                    NamespaceExtensionCodes.DTO,
                });

            var allClasses = classDeclarations.Collect()
                .Combine(referencedProjectClasses);

            context.RegisterImplementationSourceOutput(allClasses, static (spc, source) => Execute(source.Left, source.Right, spc));
        }
        private static void Execute(IList<ClassDeclarationSyntax> classes, List<SoftClass> referencedProjectClasses, SourceProductionContext context)
        {
            if (classes.Count <= 1)
                return; // FT: one because of config settings

            string outputPath = Helper.GetGeneratorOutputPath(nameof(NgControllersGenerator), classes);

            if (outputPath == null)
                return;

            List<SoftClass> softClasses = Helper.GetSoftClasses(classes);

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

{{string.Join("\n\n", GetAngularHttpMethods(controllerClasses, referencedEntityClasses))}}

}
""";

            Helper.WriteToTheFile(result, outputPath);
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

        private static List<string> GetAngularHttpMethods(List<SoftClass> controllerClasses, List<SoftClass> entities)
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

                    result.Add(GetCustomAngularControllerMethod(controllerMethod, controllerName));
                }
            }

            foreach (SoftClass entity in entities.Where(x => entityNamesForGeneration.Contains(x.Name)))
                result.Add(GetBaseAngularControllerMethods(entity, entities, alreadyAddedMethods));

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

{{GetBaseSaveAngularControllerMethod(entity, alreadyAddedMethods)}}

{{string.Join("\n\n", GetBaseUploadBlobAngularControllerMethods(entity, entities, alreadyAddedMethods))}}

{{GetBaseDeleteAngularControllerMethods(entity, alreadyAddedMethods)}}

""";
        }

        #endregion

        #region Generated Angular Controller Methods

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

        private static List<string> GetBaseOneToManyAngularControllerMethods(SoftClass entity, List<SoftClass> entities, HashSet<string> alreadyAddedMethods)
        {
            List<string> result = new List<string>();

            foreach (SoftProperty manyToOneProperty in entity.Properties.Where(x => x.Type.IsManyToOneType()))
            {
                SoftClass manyToOnePropertyClass = entities.Where(x => x.Name == manyToOneProperty.Type).SingleOrDefault();
                string manyToOnePropertyIdType = Helper.GetIdType(manyToOnePropertyClass, entities);

                //if (manyToOneProperty.IsAutocomplete())
                //{
                //result.Add(GetBaseGetListForAutocompleteAngularControllerMethod(manyToOneProperty, entity, alreadyAddedMethods));
                //}

                //if (manyToOneProperty.IsDropdown())
                //{
                //result.Add(GetBaseGetListForDropdownAngularControllerMethod(manyToOneProperty, entity, alreadyAddedMethods));
                //}
            }

            return result;
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

            return GetAngularControllerMethod(methodName, postAndPutParameter, "any", HttpTypeCodes.Post, entity.ControllerName, Settings.HttpOptionsBase);
        }

        private static string GetBaseTableDataAngularControllerMethod(SoftClass entity, HashSet<string> alreadyAddedMethods)
        {
            string methodName = $"Get{entity.Name}TableData";

            if (alreadyAddedMethods.Contains(methodName))
                return null;

            Dictionary<string, string> postAndPutParameter = new Dictionary<string, string> { { "tableFilterDTO", "TableFilter" } };

            return GetAngularControllerMethod(methodName, postAndPutParameter, "TableResponse<Segmentation>", HttpTypeCodes.Post, entity.ControllerName, Settings.HttpOptionsSkipSpinner);
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
