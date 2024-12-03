using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Soft.Generator.WebAPI;
using LightInject;
using Soft.Generator.WebAPI.DI;
using Soft.Generator.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Soft.Generator.Shared.Interfaces;
using Microsoft.AspNetCore.Diagnostics;
using Soft.Generator.Shared.Terms;
using Soft.Generator.Shared.SoftExceptions;
using System.Data.SqlClient;
using FluentValidation;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using Soft.Generator.Shared.SoftFluentValidation;
using Soft.Generator.Shared.Helpers;
using Soft.Generator.Shared.Extensions;
using System.Linq;
using Microsoft.AspNetCore.Authorization;


public class Startup

{
    public static string JsonConfigurationFile = "appsettings.json";

    private static string _cachedConfigFile = null;
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
        Soft.Generator.WebAPI.SettingsProvider.Current = ReadAssemblyConfiguration<Soft.Generator.WebAPI.Settings>();
        Soft.Generator.Infrastructure.SettingsProvider.Current = ReadAssemblyConfiguration<Soft.Generator.Infrastructure.Settings>();
        Soft.Generator.Security.SettingsProvider.Current = ReadAssemblyConfiguration<Soft.Generator.Security.Settings>();
        Soft.Generator.Shared.SettingsProvider.Current = ReadAssemblyConfiguration<Soft.Generator.Shared.Settings>();
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        // Add services like controllers, services, etc.
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = SettingsProvider.Current.JwtIssuer,
                    ValidAudience = SettingsProvider.Current.JwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SettingsProvider.Current.JwtKey)),
                    ClockSkew = TimeSpan.FromMinutes(SettingsProvider.Current.ClockSkewMinutes),
                };
            });
        services.AddAuthorization();

        services.AddHttpContextAccessor();
        services.AddCors();

        services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = false;
        });

        SoftSecuritySettings securityOptions = new SoftSecuritySettings
        {
            UseGoogleAsExternalProvider = Soft.Generator.WebAPI.SettingsProvider.Current.GoogleClientId != null
        };

        services.AddSingleton(securityOptions);
        //services.AddDbContext<IApplicationDbContext, ApplicationDbContext>( // https://youtu.be/bN57EDYD6M0?si=CVztRqlj0hBSrFXb
        //    options =>
        //    {
        //        options
        //            .UseLazyLoadingProxies()
        //            .UseSqlServer(SettingsProvider.Current.ConnectionString)
        //            .LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information);
        //    });
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Soft.Generator.WebAPI",
                Version = "v1"
            });
        });

    }

    public void ConfigureContainer(IServiceContainer container)
    {
        // Register container (AntiPattern)
        container.RegisterInstance(typeof(IServiceContainer), container);

        // Init WebAPI
        container.RegisterFrom<CompositionRoot>();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        //app . nesto su sve middleware-i

        if (env.IsDevelopment())
        {
            GenerateAngularCode();
            app.UseDeveloperExceptionPage();
        }

        // Allow CORS to connect with the Angular frontend
        app.UseCors(builder =>
        {
            builder.WithOrigins(new[] { SettingsProvider.Current.FrontendUrl })
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("Content-Disposition"); // da bi znao kako da parsiram naziv Excel fajla na frontu
            //.AllowCredentials();//if we don't send this frontend will not get our jwt token
        });

        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Your API V1");
        });

        if (env.IsProduction())
        {
            app.UseHttpsRedirection();
        }
        app.UseExceptionHandler(appError =>
        {
            appError.Run(async context =>
            {
                context.Response.ContentType = "application/json";

                IExceptionHandlerFeature contextFeature = context.Features.Get<IExceptionHandlerFeature>();

                if (contextFeature != null)
                {
                    // TODO FT: log here
                    Guid guid = Guid.NewGuid();
                    Exception exception = contextFeature.Error;
                    string exceptionString = "";
                    if (env.IsDevelopment())
                    {
                        exceptionString = exception.ToString();
                    }

                    string message;
                    if (exception is BusinessException bussinessEx)
                        message = bussinessEx.Message;
                    else if (exception is UnauthorizedException unauthorizedEx)
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        message = unauthorizedEx.Message;
                    }
                    else if (exception is ExpiredVerificationException expiredVerificationEx)
                    {
                        context.Response.StatusCode = StatusCodes.Status419AuthenticationTimeout;
                        message = expiredVerificationEx.Message;
                    }
                    else if (exception is SqlException sqlEx && sqlEx.Number == 2627)
                        message = sqlEx.Message;
                    else if (exception is ValidationException fluentEx)
                        message = fluentEx.Message;
                    else
                        message = $"{SharedTerms.GlobalError} {guid}";

                    await context.Response.WriteAsJsonAsync(new
                    {
                        StatusCode = context.Response.StatusCode,
                        Message = message,
                        Exception = exceptionString
                    });
                }
            });
        });
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapGet("/", async context =>
            {
                await context.Response.WriteAsync("Hello from C# backend!");
            });
        });
    }

    public static T ReadAssemblyConfiguration<T>()
    {
        string name = typeof(T).Assembly.GetName().Name;
        string propertyName = "AppSettings";
        string text = ReadConfigFile();
        if (string.IsNullOrEmpty(text))
        {
            return default(T);
        }

        foreach (JProperty item in JObject.Parse(text)[propertyName]!.Children().OfType<JProperty>())
        {
            if (item.Name == name)
            {
                return item.Value.ToObject<T>();
            }
        }

        return default(T);
    }

    private static string ReadConfigFile()
    {
        if (!string.IsNullOrEmpty(_cachedConfigFile))
        {
            return _cachedConfigFile;
        }

        using StreamReader streamReader = new StreamReader(JsonConfigurationFile);
        return _cachedConfigFile = streamReader.ReadToEnd();
    }

    /// <summary>
    /// This method generates only two methods, which combine individual methods for validation and for translates, because the source generator generates a new file for each project in .NET
    /// </summary>
    private static void GenerateAngularCode()
    {
        string baseServicePath = "E:\\Projects\\Soft.Generator\\Source\\Soft.Generator.SPA\\src\\app\\business\\services";
        List<string> translationProjectNames = new List<string> { "Security" };
        List<string> validationProjectNames = new List<string> { "Security" };
        GenerateMergeMethodForTranslates(baseServicePath, translationProjectNames);
        GenerateMergeMethodForValidation(baseServicePath, validationProjectNames);
    }

    private static void GenerateMergeMethodForTranslates(string baseServicePath, List<string> projectNames)
    {
        List<string> dataClassNamesHelper = new List<string>();
        List<string> importClassNamesHelper = new List<string>();

        List<string> dataLabelsHelper = new List<string>();
        List<string> importLabelsHelper = new List<string>();

        foreach (string projectName in projectNames)
        {
            importClassNamesHelper.Add($$"""
import { getTranslatedClassName{{projectName}} } from './generated/{{projectName.FromPascalToKebabCase()}}-class-names.generated';
""");
            dataClassNamesHelper.Add($$"""
    result = getTranslatedClassName{{projectName}}(name);
    if (result != null)
        return result;
""");

            importLabelsHelper.Add($$"""
import { getTranslatedLabel{{projectName}} } from './generated/{{projectName.FromPascalToKebabCase()}}-labels.generated';
""");
            dataLabelsHelper.Add($$"""
    result = getTranslatedLabel{{projectName}}(name);
    if (result != null)
        return result;
""");
        }

        string classNamesData = $$"""
import { environment } from "src/environments/environment";
{{string.Join("\n", importClassNamesHelper)}}

export function getTranslatedClassName(name: string): string {
    let result: string = null;

{{string.Join("\n\n", dataClassNamesHelper)}}

    if (environment.production == false)
        console.error(`Class name translate: '${name}' doesn't exist`);

    return name;
}
""";

        string labelsData = $$"""
import { environment } from "src/environments/environment";
{{string.Join("\n", importLabelsHelper)}}

export function getTranslatedLabel(name: string): string {
    let result: string = null;

{{string.Join("\n\n", dataLabelsHelper)}}

    if (environment.production == false)
        console.error(`Property label translate with specified name: '${name}' doesn't exist.`);

    return name;
}
""";

        Helper.WriteToTheFile(classNamesData, $"{baseServicePath}\\translates\\translated-class-names.generated.ts");
        Helper.WriteToTheFile(labelsData, $"{baseServicePath}\\translates\\translated-labels.generated.ts");
    }

    private static void GenerateMergeMethodForValidation(string baseServicePath, List<string> projectNames)
    {
        List<string> dataHelper = new List<string>();
        List<string> importHelper = new List<string>();

        foreach (string projectName in projectNames)
        {
            dataHelper.Add($$"""
    result = getValidator{{projectName}}(formControl, className);
    if (result != null)
        return result;
""");
            importHelper.Add($$"""
import { getValidator{{projectName}} } from './generated/{{projectName.FromPascalToKebabCase()}}-validation-rules.generated';
""");
        }

        string data = $$"""
import { SoftFormControl, SoftValidatorFn } from 'src/app/core/components/soft-form-control/soft-form-control';
{{string.Join("\n", importHelper)}}

export function getValidator(formControl: SoftFormControl, className: string): SoftValidatorFn {
    let result: SoftValidatorFn = null;

{{string.Join("\n\n", dataHelper)}}

    return result;
}
""";
        Helper.WriteToTheFile(data, $"{baseServicePath}\\validation\\validation-rules.generated.ts");
    }
}
