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
    }
    [Serializable] public sealed class AssistantChatResponse
    {
        public string sessionId, requestId, kind, answer, source, model, promptVersion, knowledgeVersion;
        public string[] interaction_ids, suggested_action_ids, knowledge_topics;
        public string navigationSnapshotId;
    }
    public sealed class AssistantConnectionException : Exception
    {
        public readonly long Status;
        public AssistantConnectionException(long status) : base("Assistant service unavailable") { Status=status; }
    }
    public static class AssistantChatClient
    {
        public const string Refusal = "这个问题我暂时不知道哦，问问看别的吧";
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
                request.timeout=Mathf.Clamp(settings.chatTimeoutSeconds,5,90);
                request.SetRequestHeader("Content-Type","application/json");
                try { await request.SendWebRequest().ToUniTask(cancellationToken:token); }
                catch(OperationCanceledException) { throw; }
                catch(Exception) { throw new AssistantConnectionException(request.responseCode); }
                token.ThrowIfCancellationRequested();
                if (request.result!=UnityWebRequest.Result.Success || request.downloadHandler.data.Length>32768)
                    throw new AssistantConnectionException(request.responseCode);
                AssistantChatResponse response;
                try { response=JsonUtility.FromJson<AssistantChatResponse>(request.downloadHandler.text); }
                catch(Exception) { throw new AssistantConnectionException(502); }
                if (!Valid(response,payload)) throw new AssistantConnectionException(502);
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
                r.promptVersion!="assistant-chat-v2-navigation" ||
                !(r.kind=="guide" || r.kind=="explain" || r.kind=="clarify" || r.kind=="refuse")) return false;
            if (r.kind=="guide")
            {
                if (q.navigation==null || q.navigation.targets==null || r.navigationSnapshotId!=q.navigation.snapshotId ||
                    r.interaction_ids.Length!=1 || r.suggested_action_ids.Length!=1 ||
                    r.suggested_action_ids[0]!="highlight:"+r.interaction_ids[0]) return false;
                string id=r.interaction_ids[0];
                string topic=id=="parts" ? "microscope_basics" : id=="na_experiment" ? "numerical_aperture" :
                    id=="spatial_frequency" ? "spatial_frequency" : id=="snom_entry" ? "snom" : null;
                if(topic==null || r.knowledge_topics.Length!=1 || r.knowledge_topics[0]!=topic) return false;
                bool offered=false;
                foreach(var target in q.navigation.targets) if(target.id==id) offered=true;
                if(!offered) return false;
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
