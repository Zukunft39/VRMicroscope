using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace VRMicroscope.Assistant
{
    [Serializable] public sealed class AssistantChatTurn { public string role, content; }
    [Serializable] public sealed class AssistantChatRequest
    {
        public string sessionId, requestId, question;
        public AssistantChatTurn[] history;
        public NavigationSnapshot navigation;
        public GuidanceSnapshot guidance;
    }
    [Serializable] public sealed class AssistantChatResponse
    {
        public string sessionId, requestId, kind, answer, source, model, promptVersion, knowledgeVersion;
        public string[] interaction_ids, suggested_action_ids, knowledge_topics;
        public string navigationSnapshotId;
        public string guidanceSnapshotId;
    }
    public sealed class AssistantConnectionException : Exception
    {
        public readonly long Status;
        public readonly string Code;
        public AssistantConnectionException(long status, string code = "") : base("Assistant service unavailable") { Status=status; Code=code; }
    }
    public static class AssistantChatClient
    {
        [Serializable] private sealed class ErrorResponse { public string error; }
        public static string ErrorMessage(AssistantConnectionException error)
        {
            switch (error.Code)
            {
                case "answer_format": return "The AI response format is invalid after one repair attempt. Please resend your question.";
                case "review_format": return "The answer review returned an invalid format. Please resend your question.";
                case "model_timeout": return "The AI service timed out. Try again later; your draft is saved.";
                case "model_credentials": return "The AI service credentials or permissions are invalid. Check the backend configuration.";
                case "model_configuration": return "The AI service is not configured. Check the backend model and key settings.";
                case "model_unavailable": return "The backend cannot reach the AI service. Check the connection and try again.";
                case "invalid_request": return "State data does not match the backend. Update and restart both the backend and Unity.";
                case "response_invalid": return "The response does not match this request or client protocol. Please try again.";
            }
            if (error.Status == 429) return "The AI service is busy or rate-limited. Please try again later.";
            if (error.Status == 504) return "The AI service timed out. Please try again later.";
            if (error.Status == 502) return "The chat service failed. Check the backend error and try again.";
            return "The chat connection was interrupted or timed out. Check the backend and network.";
        }
        public const string Refusal = "I do not know the answer to that yet. Please ask me something else.";
        public static async UniTask<AssistantChatResponse> Request(AssistantChatRequest payload,
            LocalAssistantSettings settings, CancellationToken token)
        {
            if (!Uri.TryCreate(settings.chatEndpoint,UriKind.Absolute,out var uri) ||
                !(uri.Scheme=="https" || uri.Scheme=="http" && uri.IsLoopback))
                throw new AssistantConnectionException(0);
            using(var request=new UnityWebRequest(settings.chatEndpoint,"POST"))
            {
                request.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload)));
                request.downloadHandler=new DownloadHandlerBuffer();
                // Generation + at most one repair + review each have an 18-second backend timeout.
                request.timeout=Mathf.Clamp(settings.chatTimeoutSeconds,60,90);
                request.SetRequestHeader("Content-Type","application/json");
                try { await request.SendWebRequest().ToUniTask(cancellationToken:token); }
                catch(OperationCanceledException) { throw; }
                catch(Exception) { /* Read the gateway's bounded error code below. */ }
                token.ThrowIfCancellationRequested();
                if (request.result!=UnityWebRequest.Result.Success)
                {
                    string code = "";
                    if (request.downloadHandler.data != null && request.downloadHandler.data.Length <= 4096)
                    {
                        try { code = JsonUtility.FromJson<ErrorResponse>(request.downloadHandler.text)?.error ?? ""; }
                        catch(Exception) { }
                    }
                    throw new AssistantConnectionException(request.responseCode, code);
                }
                if (request.downloadHandler.data == null || request.downloadHandler.data.Length>32768)
                    throw new AssistantConnectionException(502, "response_invalid");
                AssistantChatResponse response;
                try { response=JsonUtility.FromJson<AssistantChatResponse>(request.downloadHandler.text); }
                catch(Exception) { throw new AssistantConnectionException(502, "response_invalid"); }
                if (!Valid(response,payload)) throw new AssistantConnectionException(502, "response_invalid");
                if (response.kind=="refuse") response.answer=Refusal;
                return response;
            }
        }
        public static bool Valid(AssistantChatResponse r,AssistantChatRequest q)
        {
            if (r==null || r.sessionId!=q.sessionId || r.requestId!=q.requestId ||
                string.IsNullOrWhiteSpace(r.answer) || r.answer.Length>650 ||
                r.interaction_ids==null ||
                r.suggested_action_ids==null || r.knowledge_topics==null ||
                !(r.source=="mock" || r.source=="model") || string.IsNullOrEmpty(r.knowledgeVersion) ||
                r.promptVersion!="assistant-chat-v3-guidance" ||
                !(r.kind=="guide" || r.kind=="explain" || r.kind=="clarify" || r.kind=="refuse")) return false;
            if (r.kind=="guide")
            {
                if (r.suggested_action_ids.Length==1 && r.suggested_action_ids[0]!=null && r.suggested_action_ids[0].StartsWith("learn:", StringComparison.Ordinal))
                {
                    if(q.guidance==null || q.guidance.blocked || q.guidance.allowedActionIds==null ||
                        r.guidanceSnapshotId!=q.guidance.snapshotId || r.interaction_ids.Length!=1 ||
                        r.knowledge_topics.Length!=1 || Array.IndexOf(q.guidance.allowedActionIds,r.suggested_action_ids[0])<0) return false;
                }
                else
                {
                    if (q.navigation==null || q.navigation.targets==null || r.navigationSnapshotId!=q.navigation.snapshotId ||
                        r.interaction_ids.Length!=1 || r.suggested_action_ids.Length!=1 ||
                        r.suggested_action_ids[0]!="highlight:"+r.interaction_ids[0]) return false;
                    string id=r.interaction_ids[0];
                    string topic=(id=="parts" || id=="sample_red" || id=="sample_green" || id=="sample_blue" || id=="sample_yellow" || id.StartsWith("sample_", StringComparison.Ordinal)) ? "microscope_basics" :
                        id=="na_experiment" ? "numerical_aperture" :
                        id=="spatial_frequency" ? "spatial_frequency" :
                        id=="snom_entry" ? "snom" : null;
                    if(topic==null || r.knowledge_topics.Length!=1 || r.knowledge_topics[0]!=topic) return false;
                    bool offered=false;
                    foreach(var target in q.navigation.targets) if(target.id==id) offered=true;
                    if(!offered) return false;
                }
            }
            else if(r.interaction_ids.Length!=0 || r.suggested_action_ids.Length!=0) return false;
            if (r.kind=="explain" && r.knowledge_topics.Length==0) return false;
            if (r.kind=="refuse" && r.knowledge_topics.Length!=0) return false;
            foreach(var topic in r.knowledge_topics)
                if (topic!="microscope_basics" && topic!="numerical_aperture" && topic!="spatial_frequency" &&
                    topic!="confocal_background" && topic!="snom" && topic!="assistant_usage") return false;
            return true;
        }
    }
}
