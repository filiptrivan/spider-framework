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

{{string.Join("\n\n", GetAngularHttpMethods(controllerClasses))}}

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

            bool generateBaseControllerClassMethods = false;

            foreach (SoftClass controllerClass in controllerClasses)
            {
                string controllerName = controllerClass.Name.Replace("Controller", "");

                if (controllerClass.BaseType != "SoftControllerBase")
                    generateBaseControllerClassMethods = true;

                foreach (SoftMethod controllerMethod in controllerClass.Methods)
                {
                    alreadyAddedMethods.Add(controllerMethod.Name);

                    string angularReturnType = Helper.GetAngularType(controllerMethod.ReturnType);

                    string inputParameters = string.Join(", ", controllerMethod.Parameters.Select(p => $"{p.Name}: {Helper.GetAngularType(p.Type)}").ToList());

                    string getAndDeleteParameters = string.Join("&", controllerMethod.Parameters.Select(p => $"{p.Name}=${{{p.Name}}}").ToList());
                    string postAndPutParameters = string.Join(", ", controllerMethod.Parameters.Select(p => p.Name).ToList());

                    if (string.IsNullOrEmpty(getAndDeleteParameters) == false)
                        getAndDeleteParameters = $"?{getAndDeleteParameters}";

                    if (string.IsNullOrEmpty(postAndPutParameters) == false)
                        postAndPutParameters = $", {postAndPutParameters}";

                    bool skipSpinner = controllerMethod.Attributes.Any(attr => attr.Name == "SkipSpinner");

                    if (controllerMethod.Attributes.Any(attr => attr.Name == "HttpGet"))
                    {
                        if (controllerMethod.ReturnType.Contains("NamebookDTO") || controllerMethod.Name.Contains("Autocomplete") || controllerMethod.Name.Contains("Dropdown") || skipSpinner)
                        {
                            result.Add($$"""
    {{controllerMethod.Name.FirstCharToLower()}} = ({{inputParameters}}): Observable<{{angularReturnType}}> => {
        return this.http.get<{{angularReturnType}}>(`${environment.apiUrl}/{{controllerName}}/{{controllerMethod.Name}}{{getAndDeleteParameters}}`, environment.httpSkipSpinnerOptions);
    }
""");
                        }
                        else
                        {
                            result.Add($$"""
    {{controllerMethod.Name.FirstCharToLower()}} = ({{inputParameters}}): Observable<{{angularReturnType}}> => {
        return this.http.get<{{angularReturnType}}>(`${environment.apiUrl}/{{controllerName}}/{{controllerMethod.Name}}{{getAndDeleteParameters}}`);
    }
""");
                        }
                    }
                    if (controllerMethod.Attributes.Any(attr => attr.Name == "HttpPost"))
                    {
                        if (angularReturnType == "string")
                        {
                            result.Add($$"""
    {{controllerMethod.Name.FirstCharToLower()}} = ({{inputParameters}}): Observable<{{angularReturnType}}> => { 
        return this.http.post(`${environment.apiUrl}/{{controllerName}}/{{controllerMethod.Name}}`{{postAndPutParameters}}, {...environment.httpOptions, responseType: 'text'});
    }
""");
                        }
                        else if (controllerMethod.ReturnType.Contains("TableResponseDTO") || controllerMethod.ReturnType.Contains("LazyLoadSelectedIdsResultDTO") || skipSpinner)
                        {
                            result.Add($$"""
    {{controllerMethod.Name.FirstCharToLower()}} = ({{inputParameters}}): Observable<{{angularReturnType}}> => { 
        return this.http.post<{{angularReturnType}}>(`${environment.apiUrl}/{{controllerName}}/{{controllerMethod.Name}}`{{postAndPutParameters}}, environment.httpSkipSpinnerOptions);
    }
""");
                        }
                        else
                        {
                            result.Add($$"""
    {{controllerMethod.Name.FirstCharToLower()}} = ({{inputParameters}}): Observable<{{angularReturnType}}> => { 
        return this.http.post<{{angularReturnType}}>(`${environment.apiUrl}/{{controllerName}}/{{controllerMethod.Name}}`{{postAndPutParameters}}, environment.httpOptions);
    }
""");
                        }
                    }
                    if (controllerMethod.Attributes.Any(attr => attr.Name == "HttpPut"))
                    {
                        result.Add($$"""
    {{controllerMethod.Name.FirstCharToLower()}} = ({{inputParameters}}): Observable<{{angularReturnType}}> => { 
        return this.http.put<{{angularReturnType}}>(`${environment.apiUrl}/{{controllerName}}/{{controllerMethod.Name}}`{{postAndPutParameters}}, environment.httpOptions);
    }
""");
                    }
                    if (controllerMethod.Attributes.Any(attr => attr.Name == "HttpDelete"))
                    {
                        result.Add($$"""
    {{controllerMethod.Name.FirstCharToLower()}} = ({{inputParameters}}): Observable<{{angularReturnType}}> => { 
        return this.http.delete<{{angularReturnType}}>(`${environment.apiUrl}/{{controllerName}}/{{controllerMethod.Name}}{{getAndDeleteParameters}}`);
    }
""");
                    }
                }
            }

            if (generateBaseControllerClassMethods == true)
            {
                foreach (SoftClass entity in entities)
                {

                }
            }

            return result;
        }
    }
}
