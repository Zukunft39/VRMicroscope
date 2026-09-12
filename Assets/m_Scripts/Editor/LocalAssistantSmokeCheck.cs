using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using VRMicroscope.Assistant;

// Explicit batch-mode check. Does not save changes to the laboratory scene.
public static class LocalAssistantSmokeCheck
{
    private static double started;
    private static bool captured;
    [InitializeOnLoadMethod]
    private static void ResumeAfterReload()
    {
        if (!SessionState.GetBool("LocalAssistantSmokeCheck",false)) return;
        started=EditorApplication.timeSinceStartup;
        EditorApplication.update-=Poll;
        EditorApplication.update+=Poll;
    }
    public static void Run()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Run this check in a separate Unity batch process.");
        EditorSceneManager.OpenScene("Assets/Scene/Scene_Laboratory/MainScene_Labortory.unity");
        CheckClock();
        SessionState.SetBool("LocalAssistantSmokeCheck",true);
        started=EditorApplication.timeSinceStartup;
        EditorApplication.update+=Poll;
        EditorApplication.isPlaying=true;
    }
    private static void Require(bool condition,string description)
    {
        if (!condition) throw new Exception("Assistant check failed: "+description);
        Debug.Log("ASSISTANT CHECK PASS: "+description);
    }
    private static void CheckClock()
    {
        var c=new AssistantIdleClock();
        Require(!c.Tick(29,false,30,2),"No reminder before 30 seconds");
        Require(c.Tick(1,false,30,2),"Reminder at threshold");
        Require(!c.Tick(60,true,30,2),"Tutorial/reading suspension");
        Require(!c.Tick(29,false,30,2),"Fresh quiet interval after suspension");
        Require(c.Tick(1,false,30,2),"Second reminder");
        Require(!c.Tick(120,false,30,2),"Idle burst limit");
        c.Activity();
        Require(c.Tick(30,false,30,2),"Activity rearms reminders");
        c.Activity();
        c.Tick(20,false,30,2); c.Activity();
        Require(!c.Tick(15,false,30,2),"Activity discards prior idle time");
        c.Activity();
        for(int i=0;i<8;i++) Require(c.Tick(30,false,30,0),"Unlimited reminders remain available");
    }
    private static void Poll()
    {
        try
        {
            if (EditorApplication.timeSinceStartup-started>120) throw new Exception("Play-mode startup timed out");
            if (!EditorApplication.isPlaying || Time.timeSinceLevelLoad<4 || captured) return;
            var assistants=UnityEngine.Object.FindObjectsOfType<LocalAssistantController>();
            Require(assistants.Length==1,"Exactly one assistant auto-created in main scene");
            var assistant=assistants[0];
            var config=assistant.settings;
            Require(config!=null && config.chineseFont!=null,"Serialized configuration and bundled Chinese font");
            Require(config.idleSeconds==30 && config.activationKey==KeyCode.H,"Default idle period and activation key");
            Require(!assistant.IsMessageVisible,"No unsolicited greeting at startup");
            assistant.Activate();
            Require(assistant.IsMessageVisible,"Activation opens greeting");
            var labels=assistant.GetComponentsInChildren<Text>(true);
            string greeting=labels.Single(t=>t.name=="Message Text").text;
            Require(config.greetings.Contains(greeting),"Greeting selected from local catalog");
            string characters=string.Join("",config.greetings)+string.Join("",config.reminderTemplates);
            config.chineseFont.RequestCharactersInTexture(characters,23);
            Require(characters.Where(c=>!char.IsWhiteSpace(c)).All(c=>config.chineseFont.HasCharacter(c)),"Font contains all greeting/template characters");
            var orb=assistant.GetComponentInChildren<AuroraOrbGraphic>();
            Require(assistant.GetComponentsInChildren<Graphic>(true)
                .All(graphic=>graphic.GetComponent<CanvasRenderer>()!=null),
                "Every assistant graphic has a CanvasRenderer before UI raycasting");
            Canvas.ForceUpdateCanvases();
            var eventSystem=UnityEngine.EventSystems.EventSystem.current;
            Require(eventSystem!=null,"Scene UI event system exists");
            var pointer=new UnityEngine.EventSystems.PointerEventData(eventSystem);
            var uiCanvas=orb.canvas;
            pointer.position=RectTransformUtility.WorldToScreenPoint(
                uiCanvas.renderMode==RenderMode.ScreenSpaceOverlay ? null : uiCanvas.worldCamera,
                orb.rectTransform.TransformPoint(orb.rectTransform.rect.center));
            var hits=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            uiCanvas.GetComponent<GraphicRaycaster>().Raycast(pointer,hits);
            Require(hits.Any(hit=>hit.gameObject==orb.gameObject),"Orb is hit by desktop UI raycast");
            using(var geometry=new VertexHelper())
            {
                typeof(AuroraOrbGraphic).GetMethod("OnPopulateMesh",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(orb,new object[] { geometry });
                Require(geometry.currentVertCount>1000,"Procedural aurora geometry populated");
            }
            Capture(assistant);
            assistant.Dismiss();
            Require(!assistant.IsMessageVisible,"Dismiss hides panel");
            var chat=assistant.GetComponentInChildren<AssistantChatPanel>(true);
            Require(chat!=null,"Knowledge chat panel created");
            chat.Open();
            Require(AssistantChatPanel.BlocksGameplay && assistant.ReadingOrTyping,
                "Chat captures gameplay input and suspends idle reminders");
            Require(chat.GetComponentInChildren<InputField>()!=null,"Question input is available");
            chat.Close();
            Require(!assistant.ReadingOrTyping && !chat.gameObject.activeSelf,
                "Closing chat releases reading state");
            Require(Resources.Load<UnityEngine.Object>("AITutorSettings")==null,"Old embedded Tutor resource removed");
            captured=true;
            SessionState.EraseBool("LocalAssistantSmokeCheck");
            Debug.Log("LOCAL_ASSISTANT_SMOKE_PASSED");
            EditorApplication.update-=Poll;
            EditorApplication.Exit(0);
        }
        catch(Exception e)
        {
            Debug.LogException(e);
            SessionState.EraseBool("LocalAssistantSmokeCheck");
            EditorApplication.update-=Poll;
            EditorApplication.Exit(1);
        }
    }
    private static void Capture(LocalAssistantController assistant)
    {
        // Render the real UI against a neutral background, isolated from laboratory geometry.
        var go=new GameObject("Assistant screenshot camera");
        var camera=go.AddComponent<Camera>();
        camera.transform.position=new Vector3(0,10000,-10);
        camera.orthographic=true; camera.orthographicSize=360;
        camera.clearFlags=CameraClearFlags.SolidColor;
        camera.backgroundColor=new Color(.025f,.04f,.075f);
        camera.cullingMask=1<<31;
        var canvas=assistant.GetComponentInChildren<Canvas>();
        canvas.renderMode=RenderMode.WorldSpace;
        var rect=(RectTransform)canvas.transform;
        rect.SetParent(null,false); rect.position=new Vector3(0,10000,0);
        rect.rotation=Quaternion.identity; rect.localScale=Vector3.one;
        rect.sizeDelta=new Vector2(1280,720);
        canvas.GetComponent<CanvasScaler>().enabled=false;
        foreach(var t in canvas.GetComponentsInChildren<Transform>(true)) t.gameObject.layer=31;
        var rt=new RenderTexture(1280,720,24);
        camera.targetTexture=rt;
        Canvas.ForceUpdateCanvases(); camera.Render();
        var previous=RenderTexture.active;
        RenderTexture.active=rt;
        var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);
        texture.ReadPixels(new Rect(0,0,1280,720),0,0); texture.Apply();
        Directory.CreateDirectory("Temp/LocalAssistantVerification");
        File.WriteAllBytes("Temp/LocalAssistantVerification/assistant-preview.png",texture.EncodeToPNG());
        RenderTexture.active=previous; camera.targetTexture=null;
        UnityEngine.Object.Destroy(texture); rt.Release(); UnityEngine.Object.Destroy(rt);
        UnityEngine.Object.Destroy(go);
        rect.SetParent(assistant.transform,false);
    }
}
