using EchoBot;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;

IHost host = Host.CreateDefaultBuilder(args)
   .UseWindowsService(options =>
   {
       options.ServiceName = "Echo Bot Service";
   })
   .ConfigureServices(services =>
   {
       // Register EventLog logger provider options
       LoggerProviderOptions.RegisterProviderOptions<
           EventLogSettings, EventLogLoggerProvider>(services);

       // Register BotHost and EchoBotWorker
       services.AddSingleton<IBotHost, BotHost>();
       services.AddHostedService<EchoBotWorker>();

       // Retrieve SignalR connection string from Azure Key Vault
       //var keyVaultEndpoint = new Uri("https://XXXXXXXXXXXXXXXXXXXXXX");
       //var client = new SecretClient(keyVaultEndpoint, new DefaultAzureCredential());
       //var secret = client.GetSecret("SignalRConnectionString");
       //var signalRConnectionString = secret.Value.Value;
       
       //// Add SignalR with Azure SignalR Service
       //services.AddSignalR().AddAzureSignalR("Endpoint=https://vnext.service.signalr.net;AccessKey=D3jML1fHpdfHhOOnkwRBbp7rP9jolaKVqe1Wv5WBFWi5ZhvrDWceJQQJ99BDACHYHv6XJ3w3AAAAASRSeDBN;Version=1.0;").AddHubOptions<SpeechHub>(options =>
       ////services.AddSignalR().AddAzureSignalR(signalRConnectionString).AddHubOptions<SpeechHub>(options =>
       //{
       //    // Increase the time the server waits for a ping before considering the connection lost.
       //    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
       //    // Set a keep-alive interval that pings the client regularly.
       //    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
       //    options.EnableDetailedErrors = true;

       //});

       //services.AddSingleton<IBotService, BotService>();
       //SignalRHelper.InitializeAsync("Endpoint=https://vnext.service.signalr.net;AccessKey=XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX;Version=1.0;").GetAwaiter().GetResult();
       // Register SpeechService
       //services.AddSingleton<SpeechService>();
       

       // Register AppSettings from configuration
       //services.Configure<AppSettings>(context.Configuration.GetSection("AppSettings"));
   })
   //.ConfigureWebHostDefaults(webBuilder =>
   //{
   //    webBuilder.Configure(app =>
   //    {
   //        app.UseRouting();
   //        app.UseEndpoints(endpoints =>
   //        {
   //            // Map SignalR hub
   //            endpoints.MapHub<SpeechHub>("/speechhub");
   //        });
   //    });
   //})
   .Build();

await host.RunAsync();
