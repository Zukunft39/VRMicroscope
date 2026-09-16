using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace VRMicroscope.Assistant
{
    public sealed class AssistantVoiceInput : MonoBehaviour
    {
        [Serializable] private sealed class Response { public string text, source, model, error; }
        public bool Busy => pending != null;
        public bool Recording { get; private set; }
        public int Seconds { get; private set; }
        public event Action<string> StatusChanged;
        public event Action<string, bool> Transcribed;
        private CancellationTokenSource pending;
        private AudioClip clip;
        private bool ownsMicrophone, stopRequested;

        public async void Toggle(LocalAssistantSettings settings)
        {
            if (Recording) { stopRequested = true; return; }
            if (Busy) return;
            var cts = new CancellationTokenSource();
            pending = cts; stopRequested = false; Seconds = 0;
            try
            {
                StatusChanged?.Invoke("Requesting microphone permission...");
                await RequestPermission(cts.Token);
                cts.Token.ThrowIfCancellationRequested();
                if (Microphone.devices.Length == 0) throw new VoiceException("No microphone found. Connect a default audio input device.");
                if (Microphone.IsRecording(null)) throw new VoiceException("The default microphone is already in use by another recording feature.");
                Microphone.GetDeviceCaps(null, out int min, out int max);
                int frequency = min == 0 && max == 0 ? 16000 : Mathf.Clamp(16000, min, max);
                if (frequency < 8000 || frequency > 48000) throw new VoiceException("The default microphone sample rate is unsupported. Try another input device.");
                // Null selects the OS default device, not the first enumerated device.
                clip = Microphone.Start(null, false, 31, frequency);
                ownsMicrophone = clip != null;
                if (clip == null) throw new VoiceException("The microphone could not start. Check system microphone permissions.");
                float deadline = Time.realtimeSinceStartup + 5;
                while (Microphone.GetPosition(null) <= 0)
                {
                    if (Time.realtimeSinceStartup > deadline) throw new VoiceException("No microphone data received. Check the default input device.");
                    await UniTask.Yield(PlayerLoopTiming.Update, cts.Token);
                }
                Recording = true;
                StatusChanged?.Invoke("Recording... Select again to stop (up to 30 seconds). Cancel discards the recording.");
                float started = Time.realtimeSinceStartup;
                int frames = 0;
                while (!stopRequested)
                {
                    frames = Microphone.GetPosition(null);
                    Seconds = Mathf.Clamp((int)(Time.realtimeSinceStartup - started), 0, 30);
                    if (!Microphone.IsRecording(null))
                    {
                        if (Time.realtimeSinceStartup - started < 30)
                            throw new VoiceException("Recording was interrupted. Check the microphone and try again.");
                        frames = clip.samples;
                        break;
                    }
                    if (frames >= clip.frequency * 30 || Seconds >= 30) break;
                    await UniTask.Yield(PlayerLoopTiming.Update, cts.Token);
                }
                cts.Token.ThrowIfCancellationRequested();
                if (Microphone.IsRecording(null)) frames = Microphone.GetPosition(null);
                frames = Mathf.Min(frames, clip.frequency * 30);
                if (frames < clip.frequency / 2) throw new VoiceException("The recording is too short. Finish your question before stopping.");
                var samples = new float[frames * clip.channels];
                if (!clip.GetData(samples, 0)) throw new VoiceException("Unable to read the recording. Please try again.");
                byte[] wav = AssistantWavEncoder.Encode(samples, frames, clip.channels, clip.frequency);
                ReleaseMicrophone();
                StatusChanged?.Invoke("Transcribing speech... Review the text in the input field before sending.");
                Response response = await Transcribe(wav, settings, cts.Token);
                cts.Token.ThrowIfCancellationRequested();
                Transcribed?.Invoke(response.text, response.source == "mock");
            }
            catch (OperationCanceledException) { }
            catch (VoiceException error) { if (pending == cts) StatusChanged?.Invoke(error.Message); }
            catch (Exception) { if (pending == cts) StatusChanged?.Invoke("Voice input failed. Check the microphone and backend connection, then try again."); }
            finally
            {
                if (pending == cts) { ReleaseMicrophone(); pending = null; }
                cts.Dispose();
            }
        }

        private static async UniTask RequestPermission(CancellationToken token)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone)) return;
            bool finished = false, granted = false;
            var callbacks = new UnityEngine.Android.PermissionCallbacks();
            callbacks.PermissionGranted += _ => { granted = true; finished = true; };
            callbacks.PermissionDenied += _ => finished = true;
            callbacks.PermissionDeniedAndDontAskAgain += _ => finished = true;
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone, callbacks);
            float deadline = Time.realtimeSinceStartup + 30;
            while (!finished && Time.realtimeSinceStartup < deadline) await UniTask.Yield(PlayerLoopTiming.Update, token);
            if (!granted) throw new VoiceException("Microphone permission was denied. Enable it in system settings and try again.");
#else
            if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
            {
                var operation = Application.RequestUserAuthorization(UserAuthorization.Microphone);
                float deadline = Time.realtimeSinceStartup + 30;
                while (!operation.isDone && Time.realtimeSinceStartup < deadline)
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                if (!operation.isDone || !Application.HasUserAuthorization(UserAuthorization.Microphone))
                    throw new VoiceException("Microphone permission was denied. Enable it in system settings and try again.");
            }
#endif
        }

        private static async UniTask<Response> Transcribe(byte[] wav, LocalAssistantSettings settings, CancellationToken token)
        {
            if (!Uri.TryCreate(settings.speechEndpoint, UriKind.Absolute, out var uri) ||
                !(uri.Scheme == "https" || uri.Scheme == "http" && uri.IsLoopback))
                throw new VoiceException("Invalid speech service address. Use HTTPS or a local backend address.");
            using (var request = new UnityWebRequest(uri, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(wav);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = Mathf.Clamp(settings.speechTimeoutSeconds, 10, 90);
                request.SetRequestHeader("Content-Type", "audio/wav");
                try { await request.SendWebRequest().ToUniTask(cancellationToken: token); }
                catch (OperationCanceledException) { request.Abort(); throw; }
                catch (Exception) { /* Decode only bounded, known gateway error codes below. */ }
                token.ThrowIfCancellationRequested();
                Response response = null;
                if (request.downloadHandler.data != null && request.downloadHandler.data.Length <= 16384)
                {
                    try { response = JsonUtility.FromJson<Response>(request.downloadHandler.text); }
                    catch (Exception) { }
                }
                if (request.result != UnityWebRequest.Result.Success)
                    throw new VoiceException(ErrorMessage(response?.error, request.responseCode));
                if (response == null || string.IsNullOrWhiteSpace(response.text) || response.text.Length > 1000 ||
                    !(response.source == "model" || response.source == "mock"))
                    throw new VoiceException("No valid transcript received. Please try again.");
                return response;
            }
        }

        private static string ErrorMessage(string code, long status)
        {
            switch (code)
            {
                case "speech_not_configured": return "No Groq key is configured. Start the backend with --prompt-groq-key.";
                case "speech_key_rejected": return "The Groq key or model permissions are invalid. Check the backend configuration.";
                case "no_speech": return "No speech detected. Move closer to the microphone and record again.";
                case "transcript_too_long": return "The transcript exceeds 1,000 characters. Record a shorter question.";
                case "invalid_audio": return "Unsupported recording format or duration. Please record again.";
            }
            if (status == 429) return "The speech service is busy or rate-limited. Please try again later.";
            if (status == 504 || status == 0) return "The speech service could not connect or timed out. Check the network and local backend.";
            return "Speech recognition is unavailable. Try again later; your draft is saved.";
        }

        public void Cancel()
        {
            var cts = pending; pending = null;
            cts?.Cancel();
            ReleaseMicrophone();
        }
        private void ReleaseMicrophone()
        {
            if (ownsMicrophone) Microphone.End(null);
            ownsMicrophone = false; Recording = false;
            if (clip != null) Destroy(clip);
            clip = null;
        }
        private void OnApplicationFocus(bool focused)
        {
            if (!focused && ownsMicrophone) { Cancel(); StatusChanged?.Invoke("Recording cancelled because the window lost focus. Your draft is saved."); }
        }
        private void OnApplicationPause(bool paused)
        {
            if (paused && ownsMicrophone) { Cancel(); StatusChanged?.Invoke("Recording was paused and discarded. Select Voice Input to start again."); }
        }
        private void OnDisable() { Cancel(); }
        private sealed class VoiceException : Exception { public VoiceException(string message) : base(message) { } }
    }
}
