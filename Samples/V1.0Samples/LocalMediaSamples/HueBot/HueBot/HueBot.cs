// <copyright file="HueBot.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace HueBot
{
    using System.IO;
    using System.Reflection;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Graph.Communications.Common.Telemetry;
    using Microsoft.Graph.Models.TermStore;
    using Sample.Common.Logging;
    using Sample.HueBot.Bot;

    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance.
    /// </summary>
    public class HueBot
    {
        private IGraphLogger logger;
        private SampleObserver observer;
        private IConfiguration configuration;
        private BotOptions botOptions;
        private Bot bot;

        /// <summary>
        /// Initializes a new instance of the <see cref="HueBot" /> class.
        /// </summary>
        /// <param name="logger">Global logger instance.</param>
        /// <param name="observer">Global observer instance.</param>
        public HueBot(IGraphLogger logger, SampleObserver observer)
        {
            this.logger = logger;
            this.observer = observer;

           // Set directory to where the assemblies are running from.
            var location = Assembly.GetExecutingAssembly().Location;
            Directory.SetCurrentDirectory(Path.GetDirectoryName(location));
        }

        /// <summary>
        /// Optional override to create listeners (like tcp, http) for this service instance.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        public void Run()
        {
            this.configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            this.botOptions = this.configuration.GetSection("Bot").Get<BotOptions>();
            this.bot = new Bot(this.botOptions, this.logger);

            var host = new WebHostBuilder()
                .UseKestrel() // Use Kestrel instead of HTTP.sys
                .ConfigureServices(services =>
                {
                    services.AddSingleton(this.logger);
                    services.AddSingleton(this.observer);
                    services.AddSingleton(this.botOptions);
                    services.AddSingleton(this.bot);
                    services.AddControllers();
                })
                .Configure(app =>
                {
                    app.UseDeveloperExceptionPage();
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapControllers();
                    });
                })
                .UseUrls(this.botOptions.BotBaseUrl.ToString())
                .Build();

            host.Run();
        }
    }
}
