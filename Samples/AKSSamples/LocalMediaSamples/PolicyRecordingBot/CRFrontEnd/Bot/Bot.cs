using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using CRFrontEnd;
using Microsoft.Graph;
using Microsoft.Graph.Communications.Calls;
using Microsoft.Graph.Communications.Calls.Media;
using Microsoft.Graph.Communications.Client;
using Microsoft.Graph.Communications.Common;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Graph.Communications.Resources;
using Microsoft.Graph.Models;
using Microsoft.Skype.Bots.Media;
using Sample.Common.Authentication;
using Sample.Common.Logging;
using Sample.Common;
using System.Data.Entity.Core;

namespace CRFrontEnd.Bot
{
    internal class Bot : IDisposable
    {
        public static Bot Instance { get; } = new Bot();

        public IGraphLogger Logger { get; private set; }
        public SampleObserver Observer { get; private set; }
        public ConcurrentDictionary<string, CallHandler> CallHandlers { get; } = new ConcurrentDictionary<string, CallHandler>();
        public ICommunicationsClient Client { get; private set; }

        public void Dispose()
        {
            this.Observer?.Dispose();
            this.Observer = null;
            this.Logger = null;
            this.Client?.Dispose();
            this.Client = null;
        }

        internal void Initialize(Service service, IGraphLogger logger)
        {
            Validator.IsNull(this.Logger, "Multiple initializations are not allowed.");

            this.Logger = logger;
            this.Observer = new SampleObserver(logger);

            var name = this.GetType().Assembly.GetName().Name;
            var builder = new CommunicationsClientBuilder(
                name,
                service.Configuration.AadAppId,
                this.Logger);

            var authProvider = new AuthenticationProvider(
                name,
                service.Configuration.AadAppId,
                service.Configuration.AadAppSecret,
                this.Logger);

            builder.SetAuthenticationProvider(authProvider);
            builder.SetNotificationUrl(service.Configuration.CallControlBaseUrl);
            builder.SetMediaPlatformSettings(service.Configuration.MediaPlatformSettings);
            builder.SetServiceBaseUrl(service.Configuration.PlaceCallEndpointUrl);

            this.Client = builder.Build();
            this.Client.Calls().OnIncoming += this.CallsOnIncoming;
            this.Client.Calls().OnUpdated += this.CallsOnUpdated;
        }

        private void CallsOnIncoming(ICallCollection sender, CollectionEventArgs<ICall> args)
        {
            args.AddedResources.ForEach(call =>
            {
                // Answer call
                call?.AnswerAsync(mediaSession: this.CreateLocalMediaSession(), participantCapacity: 10)
                    .ForgetAndLogExceptionAsync(
                        call.GraphLogger,
                        $"Answering call {call.Id} with scenario {call.ScenarioId}.");
            });
        }

        private void CallsOnUpdated(ICallCollection sender, CollectionEventArgs<ICall> args)
        {
            foreach (var call in args.AddedResources)
            {
                var callHandler = new CallHandler(call);
                this.CallHandlers[call.Id] = callHandler;
            }

            foreach (var call in args.RemovedResources)
            {
                if (this.CallHandlers.TryRemove(call.Id, out CallHandler handler))
                {
                    handler.Dispose();
                }
            }
        }
        internal async Task EndCallByCallLegIdAsync(string callLegId)
        {
            try
            {
                await this.GetHandlerOrThrow(callLegId).Call.DeleteAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Manually remove the call from SDK state.
                // This will trigger the ICallCollection.OnUpdated event with the removed resource.
                this.Client.Calls().TryForceRemove(callLegId, out ICall call);
            }
        }
        private ILocalMediaSession CreateLocalMediaSession(Guid mediaSessionId = default(Guid))
        {
            var videoSocketSettings = new List<VideoSocketSettings>();

            for (int i = 0; i < 3; i++) // Example: 3 video sockets
            {
                videoSocketSettings.Add(new VideoSocketSettings
                {
                    StreamDirections = StreamDirection.Recvonly,
                    ReceiveColorFormat = VideoColorFormat.H264,
                });
            }

            var vbssSocketSettings = new VideoSocketSettings
            {
                StreamDirections = StreamDirection.Recvonly,
                ReceiveColorFormat = VideoColorFormat.H264,
                MediaType = MediaType.Vbss,
                SupportedSendVideoFormats = new List<VideoFormat>
                {
                    VideoFormat.H264_1920x1080_1_875Fps,
                },
            };

            var mediaSession = this.Client.CreateMediaSession(
                new AudioSocketSettings
                {
                    StreamDirections = StreamDirection.Recvonly,
                    SupportedAudioFormat = AudioFormat.Pcm16K,
                },
                videoSocketSettings,
                vbssSocketSettings,
                mediaSessionId: mediaSessionId);

            return mediaSession;
        }
        private CallHandler GetHandlerOrThrow(string callLegId)
        {
            if (!this.CallHandlers.TryGetValue(callLegId, out CallHandler handler))
            {
                throw new ObjectNotFoundException($"call ({callLegId}) not found");
            }

            return handler;
        }
    }
}