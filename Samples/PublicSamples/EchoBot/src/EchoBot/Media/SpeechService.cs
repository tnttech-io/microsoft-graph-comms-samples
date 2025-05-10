using EchoBot.Bot;
using EchoBot.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Graph.Communications.Calls;
using Microsoft.Skype.Bots.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace EchoBot.Media
{
    /// <summary>
    /// Class SpeechService.
    /// </summary>
    public class SpeechService
    {
        /// <summary>
        /// The is the indicator if the media stream is running
        /// </summary>
        private bool _isRunning = false;
        /// <summary>
        /// The is draining indicator
        /// </summary>
        protected bool _isDraining;

        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger _logger;
        private readonly PushAudioInputStream _audioInputStream = AudioInputStream.CreatePushStream(AudioStreamFormat.GetWaveFormatPCM(16000, 16, 1));
        private readonly AudioOutputStream _audioOutputStream = AudioOutputStream.CreatePullStream();

        private readonly SpeechConfig _speechConfig;
        private SpeechRecognizer _recognizer;
        private readonly SpeechSynthesizer _synthesizer;

        private readonly IHubContext<SpeechHub> _hubContext;

        // A reference to the BotMediaStream that contains the participants list
        private BotMediaStream _botMediaStream;

        // Store the active speaker information
        private string _currentSpeakerName = "Unknown Speaker";
        private uint[] _activeSpeakerIds;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpeechService" /> class.
        /// </summary>
        public SpeechService(AppSettings settings, ILogger logger, IHubContext<SpeechHub> hubContext)
        {
            _hubContext = hubContext;
            _logger = logger;

            _speechConfig = SpeechConfig.FromSubscription(settings.SpeechConfigKey, settings.SpeechConfigRegion);
            _speechConfig.SpeechSynthesisLanguage = settings.BotLanguage;
            _speechConfig.SpeechRecognitionLanguage = settings.BotLanguage;

            var audioConfig = AudioConfig.FromStreamOutput(_audioOutputStream);
            _synthesizer = new SpeechSynthesizer(_speechConfig, audioConfig);
        }

        /// <summary>
        /// Sets the reference to the BotMediaStream
        /// </summary>
        public void SetBotMediaStream(BotMediaStream botMediaStream)
        {
            _botMediaStream = botMediaStream;
        }

        /// <summary>
        /// Appends the audio buffer.
        /// </summary>
        /// <param name="audioBuffer"></param>
        public async Task AppendAudioBuffer(AudioMediaBuffer audioBuffer)
        {
            if (!_isRunning)
            {
                Start();
                await ProcessSpeech();
            }

            try
            {
                // Store the active speakers from the audio buffer
                if (audioBuffer.ActiveSpeakers != null && audioBuffer.ActiveSpeakers.Length > 0)
                {
                    _activeSpeakerIds = audioBuffer.ActiveSpeakers;
                    UpdateCurrentSpeakerName();
                }

                // audio for a 1:1 call
                var bufferLength = audioBuffer.Length;
                if (bufferLength > 0)
                {
                    var buffer = new byte[bufferLength];
                    Marshal.Copy(audioBuffer.Data, buffer, 0, (int)bufferLength);

                    _audioInputStream.Write(buffer);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception happened writing to input stream");
            }
        }

        /// <summary>
        /// Updates the current speaker name based on active speaker IDs
        /// </summary>
        private void UpdateCurrentSpeakerName()
        {
            if (_botMediaStream == null || _activeSpeakerIds == null || _activeSpeakerIds.Length == 0)
            {
                return;
            }

            var participants = _botMediaStream.GetParticipants();
            if (participants == null || participants.Count == 0)
            {
                return;
            }

            // For simplicity, we'll use the first active speaker
            var speakerId = _activeSpeakerIds[0];

            // Try to find a participant that matches this media stream ID
            foreach (var participant in participants)
            {
                // Note: We're making an assumption here that MediaStreamId can be cast to uint
                // You may need to adjust this logic based on how your IParticipant implementation works
                if (participant.Resource?.MediaStreams != null)
                {
                    foreach (var stream in participant.Resource.MediaStreams)
                    {
                        if (stream.SourceId == speakerId.ToString() ||
                            (uint.TryParse(stream.SourceId, out uint id) && id == speakerId))
                        {
                            // We found our speaker
                            var user = participant.Resource?.Info?.Identity?.User;
                            if (user != null && !string.IsNullOrEmpty(user.DisplayName))
                            {
                                _currentSpeakerName = user.DisplayName;
                                _logger.LogInformation($"Current speaker identified: {_currentSpeakerName}");
                                return;
                            }
                        }
                    }
                }
            }
        }

        public virtual void OnSendMediaBufferEventArgs(object sender, MediaStreamEventArgs e)
        {
            if (SendMediaBuffer != null)
            {
                SendMediaBuffer(this, e);
            }
        }

        public event EventHandler<MediaStreamEventArgs> SendMediaBuffer;

        /// <summary>
        /// Ends this instance.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task ShutDownAsync()
        {
            if (!_isRunning)
            {
                return;
            }

            if (_isRunning)
            {
                await _recognizer.StopContinuousRecognitionAsync();
                _recognizer.Dispose();
                _audioInputStream.Close();

                _audioInputStream.Dispose();
                _audioOutputStream.Dispose();
                _synthesizer.Dispose();

                _isRunning = false;
            }
        }

        /// <summary>
        /// Starts this instance.
        /// </summary>
        private void Start()
        {
            if (!_isRunning)
            {
                _isRunning = true;
            }
        }

        /// <summary>
        /// Processes this instance.
        /// </summary>
        private async Task ProcessSpeech()
        {
            try
            {
                var stopRecognition = new TaskCompletionSource<int>();

                using (var audioInput = AudioConfig.FromStreamInput(_audioInputStream))
                {
                    if (_recognizer == null)
                    {
                        _logger.LogInformation("init recognizer");
                        _recognizer = new SpeechRecognizer(_speechConfig, audioInput);
                    }
                }

                _recognizer.Recognizing += (s, e) =>
                {
                    _logger.LogInformation($"RECOGNIZING: Text={e.Result.Text}");
                };

                _recognizer.Recognized += async (s, e) =>
                {
                    if (e.Result.Reason == ResultReason.RecognizedSpeech)
                    {
                        if (string.IsNullOrEmpty(e.Result.Text))
                            return;

                        _logger.LogInformation($"RECOGNIZED: Text={e.Result.Text} from Speaker={_currentSpeakerName}");

                        // We recognized the speech

                        // Send transcript to Azure function with speaker information
                        var payload = JsonSerializer.Serialize(new
                        {
                            transcript = e.Result.Text,
                            speaker = _currentSpeakerName
                        });

                        string response = await SendTranscriptToAzureFunctionAsync(payload);
                        if (!string.IsNullOrEmpty(response))
                        {
                            // Now do Speech to Text
                            await TextToSpeech(e.Result.Text);
                        }
                    }
                    else if (e.Result.Reason == ResultReason.NoMatch)
                    {
                        _logger.LogInformation($"NOMATCH: Speech could not be recognized.");
                    }
                };

                _recognizer.Canceled += (s, e) =>
                {
                    _logger.LogInformation($"CANCELED: Reason={e.Reason}");

                    if (e.Reason == CancellationReason.Error)
                    {
                        _logger.LogInformation($"CANCELED: ErrorCode={e.ErrorCode}");
                        _logger.LogInformation($"CANCELED: ErrorDetails={e.ErrorDetails}");
                        _logger.LogInformation($"CANCELED: Did you update the subscription info?");
                    }

                    stopRecognition.TrySetResult(0);
                };

                _recognizer.SessionStarted += async (s, e) =>
                {
                    _logger.LogInformation("\nSession started event.");
                    await TextToSpeech("Hello Playbook V-NEXT");
                };

                _recognizer.SessionStopped += (s, e) =>
                {
                    _logger.LogInformation("\nSession stopped event.");
                    _logger.LogInformation("\nStop recognition.");
                    stopRecognition.TrySetResult(0);
                };

                // Starts continuous recognition. Uses StopContinuousRecognitionAsync() to stop recognition.
                await _recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

                // Waits for completion.
                // Use Task.WaitAny to keep the task rooted.
                Task.WaitAny(new[] { stopRecognition.Task });

                // Stops recognition.
                await _recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
            }
            catch (ObjectDisposedException ex)
            {
                _logger.LogError(ex, "The queue processing task object has been disposed.");
            }
            catch (Exception ex)
            {
                // Catch all other exceptions and log
                _logger.LogError(ex, "Caught Exception");
            }

            _isDraining = false;
        }

        private async Task<string> SendTranscriptToAzureFunctionAsync(string payload)
        {
            try
            {
                // Define the Azure Function URL and key inside the method
                string functionUrl = "https://vnextfunctionapp.azurewebsites.net/api/transcript";
                string functionKey = "ZTNlatJExoaQybKVUN9kVkx-2Q-Wqrg9R_hfWmaUoccRAzFucUHxWQ==";

                // Create an HttpClient instance
                using var httpClient = new HttpClient();

                // Add the function key as a query parameter
                var requestUrl = $"{functionUrl}?code={functionKey}";

                // Create the HTTP content with the payload
                var content = new StringContent(payload, Encoding.UTF8, "application/json");

                // Log the request with speaker information
                _logger.LogInformation($"Sending transcript from {_currentSpeakerName}: {payload}");

                // Send the POST request
                var response = await httpClient.PostAsync(requestUrl, content);

                // Ensure the response is successful
                response.EnsureSuccessStatusCode();

                // Read and return the response content
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                // Log any errors
                _logger.LogError(ex, "Error occurred while calling Azure Function.");
                throw;
            }
        }

        private async Task TextToSpeech(string text)
        {
            // convert the text to speech
            SpeechSynthesisResult result = await _synthesizer.SpeakTextAsync(text);
            // take the stream of the result
            // create 20ms media buffers of the stream
            // and send to the AudioSocket in the BotMediaStream
            using (var stream = AudioDataStream.FromResult(result))
            {
                var currentTick = DateTime.Now.Ticks;
                MediaStreamEventArgs args = new MediaStreamEventArgs
                {
                    AudioMediaBuffers = Util.Utilities.CreateAudioMediaBuffers(stream, currentTick, _logger)
                };
                OnSendMediaBufferEventArgs(this, args);
            }
        }
    }
}