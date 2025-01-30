using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Azure;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Hosting;
using System.Globalization;
using System.Text;
using Spider.Shared.Helpers;
using Spider.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Spider.Shared.Exceptions;
using Spider.Shared.Terms;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Azure.Core;


namespace Spider.Shared.Extensions
{
    public static class StartupExtensions
    {
        #region ConfigureServices

        public static void SpiderConfigureServices<TDbContext>(this IServiceCollection services) where TDbContext : DbContext, IApplicationDbContext
        {
            services.AddMemoryCache();

            services.SpiderAddAuthentication();

            services.AddAuthorization();

            services.AddHttpContextAccessor();

            services.AddHttpClient();

            services.AddCors();

            services.SpiderConfigureCulture(); // FT: It's mandatory to be before AddControllers

            services.SpiderAddControllers();

            services.SpiderAddAzureClients();

            services.SpiderAddDbContext<TDbContext>(); // https://youtu.be/bN57EDYD6M0?si=CVztRqlj0hBSrFXb

            services.SpiderAddSwaggerGen();

            services.AddRateLimiters();
        }

        public static void SpiderAddAuthentication(this IServiceCollection services)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        string accessToken = context.Request.Query["access_token"];
                        PathString path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accessToken) &&
                            (path.StartsWithSegments("/api/hubs")))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };

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
        }

        public static void SpiderConfigureCulture(this IServiceCollection services)
        {
            services.Configure<RequestLocalizationOptions>(options =>
            {
                CultureInfo[] supportedCultures = new[]
                {
                    new CultureInfo("sr-Latn-RS")
                };

                options.DefaultRequestCulture = new RequestCulture("sr-Latn-RS");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
            });
        }

        public static void SpiderAddControllers(this IServiceCollection services)
        {
            services
                .AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = false;
                    options.JsonSerializerOptions.Converters.Add(new JsonDateTimeConverter());
                });
        }

        public static void SpiderAddAzureClients(this IServiceCollection services)
        {
            services.AddAzureClients(clientBuilder =>
            {
                clientBuilder.AddBlobServiceClient(SettingsProvider.Current.BlobStorageConnectionString);

                clientBuilder.AddClient<BlobContainerClient, BlobClientOptions>((options, provider) => // https://stackoverflow.com/questions/78430531/registering-blobcontainerclient-and-injecting-into-isolated-function
                {
                    string storageContainerName = SettingsProvider.Current.BlobStorageContainerName;

                    BlobServiceClient blobServiceClient = provider.GetRequiredService<BlobServiceClient>();

                    BlobContainerClient blobContainerClient = blobServiceClient.GetBlobContainerClient(storageContainerName);

                    return blobContainerClient;
                });
            });
        }

        public static void SpiderAddDbContext<TDbContext>(this IServiceCollection services) where TDbContext : DbContext, IApplicationDbContext
        {
            services.AddDbContext<IApplicationDbContext, TDbContext>(options =>
            {
                options
                    .UseLazyLoadingProxies()
                    .UseSqlServer(SettingsProvider.Current.ConnectionString);

#if DEBUG
                options
                    .LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information);
#endif
            });
        }

        public static void SpiderAddSwaggerGen(this IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "WebAPI",
                    Version = "v1"
                });
            });
        }

        public static void AddRateLimiters(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                {
                    string ipAddress = Helper.GetIPAddress(httpContext);
                    
                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: ipAddress,
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = SettingsProvider.Current.RequestsLimitNumber,
                            Window = TimeSpan.FromSeconds(SettingsProvider.Current.RequestsLimitWindow),
                        });
                });
            });
        }

        #endregion

        #region Configure

        /// <summary>
        /// Configuring app midlewares
        /// </summary>
        public static void SpiderConfigure(this IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.SpiderConfigureLocalization();

            app.SpiderConfigureCors(); // Allow CORS to connect with the Angular frontend

            app.SpiderConfigureSwagger();

            if (env.IsProduction())
            {
                app.UseHttpsRedirection();
            }

            app.SpiderConfigureExceptionHandling(env);

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.SpiderConfigureEndpoints();
        }

        public static void SpiderConfigureLocalization(this IApplicationBuilder app)
        {
            RequestLocalizationOptions localizationOptions = app.ApplicationServices
                .GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;

            app.UseRequestLocalization(localizationOptions);
        }

        public static void SpiderConfigureCors(this IApplicationBuilder app)
        {
            app.UseCors(builder =>
            {
                builder
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()
                    .WithOrigins(new[] { SettingsProvider.Current.FrontendUrl })
                    .WithExposedHeaders("Content-Disposition"); // to know how to parse the Excel file name on the front end
            });
        }

        public static void SpiderConfigureSwagger(this IApplicationBuilder app)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Your API V1");
            });
        }

        public static void SpiderConfigureExceptionHandling(this IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseExceptionHandler(appError =>
            {
                appError.Run(async context =>
                {
                    context.Response.ContentType = "application/json";

                    IExceptionHandlerFeature contextFeature = context.Features.Get<IExceptionHandlerFeature>();

                    if (contextFeature != null)
                    {
                        Guid guid = Guid.NewGuid();
                        Exception exception = contextFeature.Error;
                        // TODO FT: log here
                        string exceptionString = "";

                        if (env.IsDevelopment())
                            exceptionString = exception.ToString();

                        string message;
                        if (exception is BusinessException bussinessEx)
                        {
                            context.Response.StatusCode = StatusCodes.Status400BadRequest;
                            message = bussinessEx.Message;
                        }
                        else if (exception is UnauthorizedException unauthorizedEx)
                        {
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            message = unauthorizedEx.Message;
                        }
                        else if (exception is SecurityTokenException securityTokenEx)
                        {
                            context.Response.StatusCode = StatusCodes.Status419AuthenticationTimeout;
                            message = securityTokenEx.Message;
                        }
                        else if (exception is ExpiredVerificationException expiredVerificationEx)
                        {
                            context.Response.StatusCode = StatusCodes.Status419AuthenticationTimeout;
                            message = expiredVerificationEx.Message;
                        }
                        else if (exception is SqlException sqlEx && sqlEx.Number == 2627)
                        {
                            message = sqlEx.Message; // FT: Test this
                        }
                        else
                        {
                            message = $"{SharedTerms.GlobalError} {guid}";
                        }

                        await context.Response.WriteAsJsonAsync(new
                        {
                            StatusCode = context.Response.StatusCode,
                            Message = message,
                            Exception = exceptionString
                        });
                    }
                });
            });
        }

        public static void SpiderConfigureEndpoints(this IApplicationBuilder app)
        {
            app.UseRateLimiter();
        }

        #endregion


    }
}
