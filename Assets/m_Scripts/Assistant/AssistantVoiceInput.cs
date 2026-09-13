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
                StatusChanged?.Invoke("正在请求麦克风权限…");
                await RequestPermission(cts.Token);
                cts.Token.ThrowIfCancellationRequested();
                if (Microphone.devices.Length == 0) throw new VoiceException("没有检测到麦克风，请连接默认音频输入设备。");
                if (Microphone.IsRecording(null)) throw new VoiceException("默认麦克风正在被项目中的其他录音功能使用。");
                Microphone.GetDeviceCaps(null, out int min, out int max);
                int frequency = min == 0 && max == 0 ? 16000 : Mathf.Clamp(16000, min, max);
                if (frequency < 8000 || frequency > 48000) throw new VoiceException("默认麦克风采样率不受支持，请更换输入设备。");
                // Null selects the OS default device, not the first enumerated device.
                clip = Microphone.Start(null, false, 31, frequency);
                ownsMicrophone = clip != null;
                if (clip == null) throw new VoiceException("麦克风启动失败，请检查系统麦克风权限。");
                float deadline = Time.realtimeSinceStartup + 5;
                while (Microphone.GetPosition(null) <= 0)
                {
                    if (Time.realtimeSinceStartup > deadline) throw new VoiceException("没有收到麦克风数据，请检查默认输入设备。");
                    await UniTask.Yield(PlayerLoopTiming.Update, cts.Token);
                }
                Recording = true;
                StatusChanged?.Invoke("正在录音…再次点击停止，最多 30 秒。取消可丢弃录音。");
                float started = Time.realtimeSinceStartup;
                int frames = 0;
                while (!stopRequested)
                {
                    frames = Microphone.GetPosition(null);
                    Seconds = Mathf.Clamp((int)(Time.realtimeSinceStartup - started), 0, 30);
                    if (!Microphone.IsRecording(null))
                    {
                        if (Time.realtimeSinceStartup - started < 30)
                            throw new VoiceException("麦克风录音已中断，请检查设备后重试。");
                        frames = clip.samples;
                        break;
                    }
                    if (frames >= clip.frequency * 30 || Seconds >= 30) break;
                    await UniTask.Yield(PlayerLoopTiming.Update, cts.Token);
                }
                cts.Token.ThrowIfCancellationRequested();
                if (Microphone.IsRecording(null)) frames = Microphone.GetPosition(null);
                frames = Mathf.Min(frames, clip.frequency * 30);
                if (frames < clip.frequency / 2) throw new VoiceException("录音太短，请说完一个问题后再停止。");
                var samples = new float[frames * clip.channels];
                if (!clip.GetData(samples, 0)) throw new VoiceException("无法读取录音，请重试。");
                byte[] wav = AssistantWavEncoder.Encode(samples, frames, clip.channels, clip.frequency);
                ReleaseMicrophone();
                StatusChanged?.Invoke("正在识别语音…结果将填入输入框，由你确认后发送。");
                Response response = await Transcribe(wav, settings, cts.Token);
                cts.Token.ThrowIfCancellationRequested();
                Transcribed?.Invoke(response.text, response.source == "mock");
            }
            catch (OperationCanceledException) { }
            catch (VoiceException error) { if (pending == cts) StatusChanged?.Invoke(error.Message); }
            catch (Exception) { if (pending == cts) StatusChanged?.Invoke("语音输入失败，请检查麦克风和后端连接后重试。"); }
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
            if (!granted) throw new VoiceException("未获得麦克风权限，请在系统设置中允许后重试。");
#else
            if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
            {
                var operation = Application.RequestUserAuthorization(UserAuthorization.Microphone);
                float deadline = Time.realtimeSinceStartup + 30;
                while (!operation.isDone && Time.realtimeSinceStartup < deadline)
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                if (!operation.isDone || !Application.HasUserAuthorization(UserAuthorization.Microphone))
                    throw new VoiceException("未获得麦克风权限，请在系统设置中允许后重试。");
            }
#endif
        }

        private static async UniTask<Response> Transcribe(byte[] wav, LocalAssistantSettings settings, CancellationToken token)
        {
            if (!Uri.TryCreate(settings.speechEndpoint, UriKind.Absolute, out var uri) ||
                !(uri.Scheme == "https" || uri.Scheme == "http" && uri.IsLoopback))
                throw new VoiceException("语音服务地址无效，请使用 HTTPS 或本机后端地址。");
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
                    throw new VoiceException("未收到有效转写文本，请重试。");
                return response;
            }
        }

        private static string ErrorMessage(string code, long status)
        {
            switch (code)
            {
                case "speech_not_configured": return "后端尚未配置 Groq 密钥，请使用 --prompt-groq-key 启动后端。";
                case "speech_key_rejected": return "Groq 密钥或模型访问权限无效，请检查后端配置。";
                case "no_speech": return "没有识别到语音，请靠近麦克风并重新录制。";
                case "transcript_too_long": return "转写超过 1000 字，请分成更短的问题重新录制。";
                case "invalid_audio": return "录音格式或时长不受支持，请重新录制。";
            }
            if (status == 429) return "语音请求已达限额或服务繁忙，请稍后重试。";
            if (status == 504 || status == 0) return "语音服务连接失败或超时，请检查网络和本地后端。";
            return "语音识别服务暂不可用，请稍后重试；原有草稿已保留。";
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
            if (!focused && ownsMicrophone) { Cancel(); StatusChanged?.Invoke("录音因窗口失去焦点而取消，草稿已保留。"); }
        }
        private void OnApplicationPause(bool paused)
        {
            if (paused && ownsMicrophone) { Cancel(); StatusChanged?.Invoke("录音已暂停并丢弃，请重新点击语音输入。"); }
        }
        private void OnDisable() { Cancel(); }
        private sealed class VoiceException : Exception { public VoiceException(string message) : base(message) { } }
    }
}
