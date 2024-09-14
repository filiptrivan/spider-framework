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

            context.RegisterImplementationSourceOutput(classDeclarations.Collect(),
            static (spc, source) => Execute(source, spc));

        }
        private static void Execute(IList<ClassDeclarationSyntax> controllerClasses, SourceProductionContext context)
        {
            if (controllerClasses.Count == 0) return;
            string[] namespacePartsWithoutLastElement = Helper.GetNamespacePartsWithoutLastElement(controllerClasses[0]);
            string[] namespacePartsWithoutTwoLastElements = namespacePartsWithoutLastElement.Take(namespacePartsWithoutLastElement.Length - 1).ToArray();

            //string projectBasePartOfNamespace = string.Join(".", namespacePartsWithoutLastElement); // eg. Soft.Generator.Security
            string wholeProjectBasePartOfNamespace = string.Join(".", namespacePartsWithoutTwoLastElements); // eg. Soft.Generator

            StringBuilder sb = new StringBuilder();
            List<Prop> properties = new List<Prop>();
            List<string> angularHttpMethods = new List<string>();
            foreach (ClassDeclarationSyntax controllerClass in controllerClasses)
            {
                string controllerName = controllerClass.Identifier.Text.Replace("Controller", "");

                foreach (MethodDeclarationSyntax endpointMethod in controllerClass.Members.OfType<MethodDeclarationSyntax>().ToList())
                {
                    List<Prop> parameterProperties = endpointMethod.ParameterList.Parameters
                        .Select(x => new Prop
                        {
                            Type = x.Type.ToString()
                        })
                        .ToList();
                    properties.AddRange(parameterProperties);
                    string returnType = endpointMethod.ReturnType.ToString();
                    properties.Add(new Prop { Type = returnType });
                    angularHttpMethods.Add(GetAngularHttpMethod(endpointMethod, Helper.GetAngularDataType(returnType), controllerName));
                }
            }

            sb.AppendLine($$"""
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
{{string.Join("\n", Helper.GetAngularImports(properties, "../../entities/generated/"))}}

@Injectable()
export class ApiGeneratedService {

    constructor(protected http: HttpClient) {}

    {{string.Join("\n", angularHttpMethods)}}

}
""");

            Helper.WriteToTheFile(sb.ToString(), $@"E:\Projects\{wholeProjectBasePartOfNamespace}\Source\{wholeProjectBasePartOfNamespace}.SPA\src\app\business\services\api\api.service.generated.ts");
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
        return this.http.get<{returnType}>(`${{environment.apiUrl}}/{controllerName}/{methodName}{getAndDeleteParameters}`, environment.httpDropdownOptions);
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
        return this.http.post<{returnType}>(`${{environment.apiUrl}}/{controllerName}/{methodName}`{postAndPutParameters}, environment.httpTableOptions);
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
