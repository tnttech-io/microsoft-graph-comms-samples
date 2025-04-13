using EchoBot;
using EchoBot.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;

IHost host = Host.CreateDefaultBuilder(args)
   .UseWindowsService(options =>
   {
       options.ServiceName = "Echo Bot Service";
   })
   .ConfigureServices((context, services) =>
   {
       LoggerProviderOptions.RegisterProviderOptions<
           EventLogSettings, EventLogLoggerProvider>(services);

       services.AddSingleton<IBotHost, BotHost>();

       services.AddHostedService<EchoBotWorker>();

       services.AddSignalR();
   })
   .ConfigureWebHostDefaults(webBuilder =>
   {
       webBuilder.Configure(app =>
       {
           app.UseRouting();
           app.UseEndpoints(endpoints =>
           {
               endpoints.MapHub<SpeechHub>("/speechhub");
           });
       });
   })
   .Build();

await host.RunAsync();
