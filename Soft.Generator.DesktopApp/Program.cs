using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using Soft.Generator.DesktopApp.Services;
using System;
using System.Windows.Forms.Design;

namespace Soft.Generator.DesktopApp
{
    internal static class Program
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            ServiceCollection serviceCollection = new ServiceCollection();
            
            ConfigureServices(serviceCollection);

            ServiceProvider = serviceCollection.BuildServiceProvider();

            Application.Run(ServiceProvider.GetRequiredService<Form1>());
        }

        private static void ConfigureServices(ServiceCollection services)
        {
            //services.AddMySqlDataSource(Settings.ConnectionString);
            services.AddScoped<MySqlConnection>(_ => new MySqlConnection(Settings.ConnectionString));
            services.AddScoped<DesktopAppService>();

            services.AddTransient<Form1>();
        }
    }
}