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

            IncrementalValueProvider<IEnumerable<INamedTypeSymbol>> referencedProjectClasses = Helper.GetReferencedProjectsSymbolsDTO(context);

            var allClasses = classDeclarations.Collect()
                .Combine(referencedProjectClasses);

            context.RegisterImplementationSourceOutput(allClasses, static (spc, source) => Execute(source.Left, source.Right, spc));
        }
        private static void Execute(IList<ClassDeclarationSyntax> classes, IEnumerable<INamedTypeSymbol> referencedClassesDTO, SourceProductionContext context)
        {
            if (classes.Count <= 1) return; // FT: one because of config settings
            string outputPath = Helper.GetGeneratorOutputPath(nameof(NgControllersGenerator), classes);

            List<ClassDeclarationSyntax> controllerClasses = Helper.GetControllerClasses(classes);

            StringBuilder sb = new StringBuilder();
            List<SoftProperty> properties = new List<SoftProperty>();
            List<string> angularHttpMethods = new List<string>();
            foreach (ClassDeclarationSyntax controllerClass in controllerClasses) // FT: Big part of this method is not user because we changed the way of importing ng classes
            {
                string controllerName = controllerClass.Identifier.Text.Replace("Controller", "");

                foreach (MethodDeclarationSyntax endpointMethod in controllerClass.Members.OfType<MethodDeclarationSyntax>().ToList())
                {
                    List<SoftProperty> parameterProperties = endpointMethod.ParameterList.Parameters
                        .Select(x => new SoftProperty
                        {
                            Type = x.Type.ToString(),
                        })
                        .ToList();
                    properties.AddRange(parameterProperties);
                    string returnType = endpointMethod.ReturnType.ToString();
                    properties.Add(new SoftProperty { Type = returnType });
                    angularHttpMethods.Add(GetAngularHttpMethod(endpointMethod, Helper.GetAngularDataType(returnType), controllerName));
                }
            }
            List<string> importLines = new List<string>();
            foreach (INamedTypeSymbol symbol in referencedClassesDTO)
            {
                string[] projectNameHelper = symbol.ContainingNamespace.ToString().Split('.');
                string projectName = projectNameHelper[projectNameHelper.Length - 2];
                string ngType = symbol.Name.Replace("DTO", "");

                if (Helper.BaseClassNames.Contains(ngType))
                    continue;

                importLines.Add($"import {{ {ngType} }} from '../../entities/generated/{projectName.FromPascalToKebabCase()}-entities.generated';");
            }

            sb.AppendLine($$"""
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { ApiSecurityService } from './api.service.security';
import { Namebook } from '../../entities/namebook';
import { Codebook } from '../../entities/codebook';
import { TableFilter } from '../../entities/table-filter';
{{string.Join("\n", importLines)}}

@Injectable()
export class ApiGeneratedService extends ApiSecurityService {

    constructor(protected override http: HttpClient) {
        super(http);
    }

    {{string.Join("\n", angularHttpMethods)}}

}
""");

            Helper.WriteToTheFile(sb.ToString(), outputPath);
        }

        private static string GetAngularHttpMethod(MethodDeclarationSyntax endpointMethod, string returnType, string controllerName)
        {

            string methodName = endpointMethod.Identifier.Text;
            string inputParameters = string.Join(", ", endpointMethod.ParameterList.Parameters.Select(p => $"{p.Identifier.Text}: {Helper.GetAngularDataType(p.Type.ToString())}").ToList());

            string result = null;

            string getAndDeleteParameters = string.Join("&", endpointMethod.ParameterList.Parameters.Select(p => $"{p.Identifier.Text}=${{{p.Identifier.Text}}}").ToList());
            string postAndPutParameters = string.Join(", ", endpointMethod.ParameterList.Parameters.Select(p => p.Identifier.Text).ToList());

            if (string.IsNullOrEmpty(getAndDeleteParameters) == false)
                getAndDeleteParameters = $"?{getAndDeleteParameters}";
            if (string.IsNullOrEmpty(postAndPutParameters) == false)
                postAndPutParameters = $", {postAndPutParameters}";

            if (endpointMethod.AttributeLists.Any(attr => attr.Attributes.Any(a => a.Name.ToString() == "HttpGet")))
            {
                if (returnType.Contains("Namebook") || methodName.Contains("Autocomplete") || methodName.Contains("Dropdown"))
                {
                    result = @$"
    {methodName.FirstCharToLower()}({inputParameters}): Observable<{returnType}> {{
        return this.http.get<{returnType}>(`${{environment.apiUrl}}/{controllerName}/{methodName}{getAndDeleteParameters}`, environment.httpSkipSpinnerOptions);
    }}";
                }
                else
                {
                    result = @$"
    {methodName.FirstCharToLower()}({inputParameters}): Observable<{returnType}> {{
        return this.http.get<{returnType}>(`${{environment.apiUrl}}/{controllerName}/{methodName}{getAndDeleteParameters}`);
    }}";
                }

            }
            if (endpointMethod.AttributeLists.Any(attr => attr.Attributes.Any(a => a.Name.ToString() == "HttpPost")))
            {
                if (methodName.Contains("ForTable")) // FT HACK: Be carefull with method name
                {
                    result = @$"
    {methodName.FirstCharToLower()}({inputParameters}): Observable<{returnType}> {{ 
        return this.http.post<{returnType}>(`${{environment.apiUrl}}/{controllerName}/{methodName}`{postAndPutParameters}, environment.httpSkipSpinnerOptions);
    }}";
                }
                else
                {
                    result = @$"
    {methodName.FirstCharToLower()}({inputParameters}): Observable<{returnType}> {{ 
        return this.http.post<{returnType}>(`${{environment.apiUrl}}/{controllerName}/{methodName}`{postAndPutParameters}, environment.httpOptions);
    }}";
                }
            }
            if (endpointMethod.AttributeLists.Any(attr => attr.Attributes.Any(a => a.Name.ToString() == "HttpPut")))
            {
                result = @$"
    {methodName.FirstCharToLower()}({inputParameters}): Observable<{returnType}> {{ 
        return this.http.put<{returnType}>(`${{environment.apiUrl}}/{controllerName}/{methodName}`{postAndPutParameters}, environment.httpOptions);
    }}";
            }
            if (endpointMethod.AttributeLists.Any(attr => attr.Attributes.Any(a => a.Name.ToString() == "HttpDelete")))
            {
                result = @$"
    {methodName.FirstCharToLower()}({inputParameters}): Observable<{returnType}> {{ 
        return this.http.delete<{returnType}>(`${{environment.apiUrl}}/{controllerName}/{methodName}{getAndDeleteParameters}`);
    }}";
            }

            return result;
        }
    }
}
