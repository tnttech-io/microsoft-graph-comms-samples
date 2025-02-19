using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Graph.Communications.Common.Telemetry;
using CRFrontEnd;

namespace CRWorkerRole
{
    public class Worker : BackgroundService
    {
        private readonly IGraphLogger _logger;
        private readonly CRFrontEnd.IConfiguration _configuration;

        public Worker(IGraphLogger logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.Info("WorkerRole is running");

            try
            {
                // Set the maximum number of concurrent connections
                ServicePointManager.DefaultConnectionLimit = 12;

                // Enforce TLS 1.2
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                // Initialize and start the service
              //  var azureConfiguration = new AzureConfiguration(_configuration, _logger);
                Service.Instance.Initialize(_configuration, _logger);
                Service.Instance.Start();

                // Main loop
                while (!stoppingToken.IsCancellationRequested)
                {
                    _logger.Info("Working");
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Exception in WorkerRole");
                throw;
            }
            finally
            {
                Service.Instance.Stop();
                _logger.Info("WorkerRole has stopped");
            }
        }
    }
}