using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Skype.Bots.Media;
using CRFrontEnd;

namespace CRWorkerRole
{
    internal class AzureConfiguration : CRFrontEnd.IConfiguration
    {
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;
        private readonly IGraphLogger _logger;

        public AzureConfiguration(Microsoft.Extensions.Configuration.IConfiguration configuration, IGraphLogger logger)
        {
            _configuration = configuration;
            _logger = logger;
            Initialize();
        }

        public string ServiceDnsName { get; private set; }
        public IEnumerable<Uri> CallControlListeningUrls { get; private set; }
        public Uri CallControlBaseUrl { get; private set; }
        public Uri PlaceCallEndpointUrl { get; private set; }
        public string AadAppId { get; private set; }
        public string AadAppSecret { get; private set; }
        public MediaPlatformSettings MediaPlatformSettings { get; private set; }

        private void Initialize()
        {
            // Read service DNS name
            ServiceDnsName = _configuration["ServiceConfiguration:ServiceDnsName"];

            // Read and validate AadAppId
            AadAppId = _configuration["ServiceConfiguration:AadAppId"];
            if (string.IsNullOrEmpty(AadAppId))
            {
                throw new ConfigurationException("AadAppId", "Missing AadAppId in appsettings.json.");
            }

            // Read and validate AadAppSecret
            AadAppSecret = _configuration["ServiceConfiguration:AadAppSecret"];
            if (string.IsNullOrEmpty(AadAppSecret))
            {
                throw new ConfigurationException("AadAppSecret", "Missing AadAppSecret in appsettings.json.");
            }

            // Read PlaceCallEndpointUrl
            var placeCallEndpointUrlStr = _configuration["ServiceConfiguration:PlaceCallEndpointUrl"];
            if (!string.IsNullOrEmpty(placeCallEndpointUrlStr))
            {
                PlaceCallEndpointUrl = new Uri(placeCallEndpointUrlStr);
            }

            // Read Call Control URLs from array in appsettings.json
            var callControlUrls = _configuration.GetSection("ServiceConfiguration:CallControlListeningUrls").Get<string[]>();
            if (callControlUrls != null)
            {
                CallControlListeningUrls = callControlUrls.Select(url => new Uri(url)).ToList();
            }
            else
            {
                throw new ConfigurationException("CallControlListeningUrls", "No call control URLs configured.");
            }

            // Fetch certificate from the store
            var defaultCertificate = GetCertificateFromStore(_configuration["Certificates:Default:Thumbprint"]);

            // Read Media Platform settings
            MediaPlatformSettings = new MediaPlatformSettings
            {
                MediaPlatformInstanceSettings = new MediaPlatformInstanceSettings
                {
                    CertificateThumbprint = defaultCertificate.Thumbprint,
                    InstanceInternalPort = int.Parse(_configuration["Endpoints:InstanceCallControlEndpoint:LocalPort"] ?? "10100"),
                    InstancePublicIPAddress = IPAddress.Any, // Change if you need a specific IP
                    InstancePublicPort = int.Parse(_configuration["Endpoints:InstanceCallControlEndpoint:PublicPortRange:Min"] ?? "10100"),
                    ServiceFqdn = ServiceDnsName,
                },
                ApplicationId = AadAppId,
            };

            // Logging configuration details
            _logger.Info($"ServiceDnsName -> {ServiceDnsName}");
            _logger.Info($"PlaceCallEndpointUrl -> {PlaceCallEndpointUrl}");
            foreach (var uri in CallControlListeningUrls)
            {
                _logger.Info($"Call control listening Uri -> {uri}");
            }
        }

        private X509Certificate2 GetCertificateFromStore(string thumbprint)
        {
            if (string.IsNullOrEmpty(thumbprint))
            {
                throw new ConfigurationException("Certificates:Default:Thumbprint", "No certificate thumbprint provided in appsettings.json.");
            }

            using (var store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadOnly);
                var certs = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false);
                if (certs.Count == 0)
                {
                    throw new ConfigurationException("DefaultCertificate", $"No certificate found with thumbprint {thumbprint}.");
                }
                return certs[0];
            }
        }

        public void Dispose()
        {
            // Dispose if needed
        }
    }
}
