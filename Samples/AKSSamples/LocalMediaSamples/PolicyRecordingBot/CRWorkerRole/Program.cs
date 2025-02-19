using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph.Communications.Common.Telemetry;
using CRWorkerRole;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                      .AddEnvironmentVariables();
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddSingleton<IGraphLogger, GraphLogger>();
                services.AddSingleton<CRFrontEnd.IConfiguration, AzureConfiguration>(provider =>
                    new AzureConfiguration(provider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>(), provider.GetRequiredService<IGraphLogger>()));
                services.AddHostedService<Worker>();
            });
}