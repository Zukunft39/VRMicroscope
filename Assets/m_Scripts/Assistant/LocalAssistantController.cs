using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace VRMicroscope.Assistant
{
    public sealed class LocalAssistantController : MonoBehaviour
    {
        public LocalAssistantSettings settings;
        public event Action Activated;
        public bool IsMessageVisible => bubble != null && bubble.activeSelf;
        // Future question/reading UI can suspend reminders without altering gameplay input.
        public bool ReadingOrTyping { get; set; }
        private readonly AssistantIdleClock idle = new AssistantIdleClock();
        private readonly List<InputDevice> devices = new List<InputDevice>();
        private Canvas canvas;
        private CanvasScaler scaler;
        private RectTransform canvasRect;
        private GameObject bubble;
        private Text message, heading, hint;
        private AuroraOrbGraphic orb;
        private NumericalApertureExperimentController na;
        private SpatialFrequencyExperimentController sf;
        private SNOMDemonstrationController snom;
        private StandaloneTutorialUI[] tutorials = new StandaloneTutorialUI[0];
        private VideoPlayer[] videos = new VideoPlayer[0];
        private float hideAt, lookupAt, snoozeUntil, activateAt;
        private int lastGreeting = -1, lastTemplate = -1, lastTopic = -1;
        private string module;
        private bool ownsSettings, focused = true, xrMode;
        private Camera viewer;
        private Vector3 lastMouse;
        private Rect lastSafeArea;
        private RectTransform safeRoot;
        public AssistantNavigation Navigation { get; private set; }
        public AssistantGuidance Guidance { get; private set; }
        private bool pointerReading;
        private AssistantChatPanel chat;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
            SceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }
        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "MainScene_Labortory" || FindObjectOfType<LocalAssistantController>() != null) return;
            var config = Resources.Load<LocalAssistantSettings>("LocalAssistantSettings");
            if (config != null && !config.assistantEnabled) return;
            var go = new GameObject("Local Aurora Assistant");
            SceneManager.MoveGameObjectToScene(go, scene);
            go.AddComponent<LocalAssistantController>();
        }
        private void Start()
        {
            if (settings == null) settings = Resources.Load<LocalAssistantSettings>("LocalAssistantSettings");
            if (settings == null) { settings = ScriptableObject.CreateInstance<LocalAssistantSettings>(); ownsSettings = true; }
            if (!settings.assistantEnabled) { enabled = false; return; }
            BuildUI();
            lastMouse = Input.mousePosition;
            RefreshReferences();
        }
        public void ReportActivity() => idle.Activity();
        public void Activate()
        {
            if (settings == null || Time.unscaledTime < activateAt || IsTyping()) return;
            activateAt = Time.unscaledTime + .6f;
            ClearSelection();
            ReportActivity();
            string greeting = Pick(settings.greetings, ref lastGreeting, "Hello! I am here to help you explore.");
            Show(greeting, "Hello - good to see you");
            if (chat != null && chat.gameObject.activeSelf) chat.Close();
            Activated?.Invoke();
        }
        public void Dismiss() { if (bubble != null) bubble.SetActive(false); ClearSelection(); ReportActivity(); }
        private static void ClearSelection()
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }
        public void SetPointerReading(bool value) { pointerReading = value; }
        public void Snooze()
        {
            snoozeUntil = Time.unscaledTime + 300;
            Dismiss();
        }
        private void Update()
        {
            if (canvas == null) return;
            if (Time.unscaledTime >= lookupAt) RefreshReferences();
            UpdateCanvas();
            bool typing = IsTyping();
            if (focused && !typing && Input.GetKeyDown(settings.activationKey)) Activate();
            if (HasActivity()) ReportActivity();
            if (IsMessageVisible && pointerReading) hideAt = Mathf.Max(hideAt, Time.unscaledTime + 3);
            if (IsMessageVisible && Time.unscaledTime >= hideAt && !ReadingOrTyping) bubble.SetActive(false);
            orb.speaking = IsMessageVisible;
            orb.reducedMotion = settings.reducedMotion;
            string nextModule = snom != null && snom.IsRunning ? "snom" :
                na != null && na.IsExperimentActive ? "na" : sf != null && sf.IsExperimentActive ? "sf" : "lab";
            if (nextModule != module) { module = nextModule; lastTopic = -1; ReportActivity(); }
            bool suppressed = !focused || typing || ReadingOrTyping || IsMessageVisible ||
                Time.unscaledTime < snoozeUntil || Time.timeScale <= 0 || TeachingBusy();
            if (idle.Tick(Time.unscaledDeltaTime, suppressed, Mathf.Max(5,settings.idleSeconds),
                Mathf.Max(0,settings.maxRemindersPerIdlePeriod)))
            {
                string topic = Pick(Topics(module), ref lastTopic, "microscope structure");
                string template = Pick(settings.reminderTemplates, ref lastTemplate, "Perhaps you could explore {0}.");
                Show(template.Replace("{0}", topic), "Ideas to explore - at your own pace");
            }
        }
        private bool TeachingBusy()
        {
            if (Interactor.Instance != null && Interactor.Instance.CurrentState == Interactor.GameState.Tutorial) return true;
            if (SuperAssemblyPartSelectionController.HasActiveSelection &&
                !(na != null && na.IsExperimentActive) && !(sf != null && sf.IsExperimentActive)) return true;
            if (na != null && na.IsTransitioning || sf != null && sf.IsTransitioning) return true;
            if (snom != null && snom.SuppressAssistantReminders) return true;
            foreach (var tutorial in tutorials) if (tutorial != null && tutorial.isActiveAndEnabled && tutorial.IsPlaying) return true;
            foreach (var video in videos) if (video != null && video.isActiveAndEnabled && video.isPlaying) return true;
            return false;
        }
        private static bool IsTyping()
        {
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selected == null) return false;
            var tmp = selected.GetComponentInParent<TMP_InputField>();
            var legacy = selected.GetComponentInParent<InputField>();
            return tmp != null && tmp.isFocused || legacy != null && legacy.isFocused;
        }
        private bool HasActivity()
        {
            bool active = Input.anyKey || Input.mouseScrollDelta.sqrMagnitude > .001f;
            // Accumulate pointer displacement; sensor jitter and head tracking are not actions.
            if ((Input.mousePosition-lastMouse).sqrMagnitude > 9) { lastMouse = Input.mousePosition; active = true; }
            foreach (var device in devices)
            {
                if (!device.isValid) continue;
                if (device.TryGetFeatureValue(CommonUsages.trigger, out float trigger) && trigger > .1f ||
                    device.TryGetFeatureValue(CommonUsages.grip, out float grip) && grip > .1f ||
                    device.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 axis) && axis.sqrMagnitude > .04f ||
                    device.TryGetFeatureValue(CommonUsages.primaryButton, out bool primary) && primary ||
                    device.TryGetFeatureValue(CommonUsages.secondaryButton, out bool secondary) && secondary ||
                    device.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out bool click) && click) active = true;
            }
            return active;
        }
        private void RefreshReferences()
        {
            lookupAt = Time.unscaledTime + 1;
            if (na == null) na = FindObjectOfType<NumericalApertureExperimentController>();
            if (sf == null) sf = FindObjectOfType<SpatialFrequencyExperimentController>();
            if (snom == null) snom = FindObjectOfType<SNOMDemonstrationController>();
            tutorials = FindObjectsOfType<StandaloneTutorialUI>(true);
            videos = FindObjectsOfType<VideoPlayer>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller, devices);
        }
        private void Show(string text, string title)
        {
            message.text = text;
            heading.text = title;
            bubble.SetActive(true);
            // Long custom lines receive additional reading time.
            hideAt = Time.unscaledTime + Mathf.Max(settings.messageSeconds, text.Length / 5f);
            orb.SetVerticesDirty();
        }
        private static string Pick(string[] values, ref int previous, string fallback)
        {
            if (values == null || values.Length == 0) return fallback;
            bool excludePrevious = previous >= 0 && previous < values.Length && values.Length > 1;
            int index = UnityEngine.Random.Range(0,values.Length-(excludePrevious ? 1 : 0));
            if (excludePrevious && index >= previous) index++;
            previous = index;
            return string.IsNullOrWhiteSpace(values[index]) ? fallback : values[index];
        }
        private static readonly string[] LabTopics = { "the functions of microscope components", "numerical aperture and the collection cone", "gratings and spatial frequency", "THz s-SNOM probes and near fields", "the principles of confocal microscopy" };
        private static readonly string[] NaTopics = { "the relationship between NA and the collection cone", "the difference between NA and magnification", "illustrative changes in brightness and clarity" };
        private static readonly string[] SfTopics = { "the relationship between grating spacing and diffraction angle", "the difference between the specimen spectrum and sidebands", "frequency support in three orientations" };
        private static readonly string[] SnomTopics = { "probe tapping and near-field coupling", "AFM distance feedback", "near-field signals and background", "the role of harmonic demodulation" };
        private static string[] Topics(string key) => key == "na" ? NaTopics : key == "sf" ? SfTopics : key == "snom" ? SnomTopics : LabTopics;

        private void BuildUI()
        {
            var go = new GameObject("Assistant Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform,false);
            canvas = go.GetComponent<Canvas>();
            // Reserve the front UI layer for the assistant, above experiment and tutorial canvases.
            canvas.sortingLayerID = SortingLayer.NameToID("Front");
            canvas.sortingOrder = short.MaxValue;
            canvas.overrideSorting = true;
            canvasRect = (RectTransform)go.transform;
            scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280,720); scaler.matchWidthOrHeight = 0;
            go.AddComponent<TrackedDeviceGraphicRaycaster>();
            safeRoot = new GameObject("Safe Area",typeof(RectTransform)).GetComponent<RectTransform>();
            safeRoot.SetParent(canvasRect,false);
            safeRoot.anchorMin=Vector2.zero; safeRoot.anchorMax=Vector2.one;
            safeRoot.offsetMin=safeRoot.offsetMax=Vector2.zero;
            var sphere=Rect("Aurora Orb",safeRoot,new Vector2(18,-18),new Vector2(112,112));
            sphere.gameObject.AddComponent<CanvasRenderer>();
            orb=sphere.gameObject.AddComponent<AuroraOrbGraphic>();
            orb.raycastTarget=true;
            var button=sphere.gameObject.AddComponent<Button>();
            button.targetGraphic=orb; button.transition=Selectable.Transition.None; button.onClick.AddListener(Activate);
            hint=Label("Activation Hint",safeRoot,new Vector2(12,-132),new Vector2(132,30),17,new Color(.94f,.98f,1f),TextAnchor.MiddleCenter);
            var hintOutline=hint.gameObject.AddComponent<Outline>();
            hintOutline.effectColor=new Color(0,0,0,.9f);
            hintOutline.effectDistance=new Vector2(1,-1);
            hintOutline.useGraphicAlpha=true;
            var body=Rect("Message",safeRoot,new Vector2(154,-28),new Vector2(650,168));
            bubble=body.gameObject;
            bubble.AddComponent<AssistantReadingSurface>().owner=this;
            var background=bubble.AddComponent<Image>(); background.color=new Color(.035f,.075f,.12f,.76f);
            // The panel consumes pointer hits so reading/dismissing does not click through to samples.
            var edge=Rect("Accent",body,Vector2.zero,new Vector2(1,168)).gameObject.AddComponent<Image>();
            edge.color=new Color(.42f,.88f,1,.48f); edge.raycastTarget=false;
            var topEdge=Rect("Top Edge",body,Vector2.zero,new Vector2(650,1)).gameObject.AddComponent<Image>();
            topEdge.color=new Color(.62f,.61f,1,.22f); topEdge.raycastTarget=false;
            heading=Label("Heading",body,new Vector2(22,-12),new Vector2(540,25),17,new Color(.40f,.91f,.85f));
            message=Label("Message Text",body,new Vector2(22,-46),new Vector2(600,70),23,new Color(.91f,.96f,1));
            message.resizeTextForBestFit=true;
            message.resizeTextMinSize=18;
            message.resizeTextMaxSize=23;
            SmallButton("Close",body,new Vector2(599,-9),new Vector2(38,30),"×",Dismiss);
            SmallButton("Snooze",body,new Vector2(453,-124),new Vector2(174,30),"Snooze 5 min",Snooze);
            SmallButton("Ask",body,new Vector2(297,-124),new Vector2(142,30),"Ask a question",()=>chat.Open());
            Label("Companion",body,new Vector2(22,-126),new Vector2(260,25),15,new Color(.54f,.67f,.77f)).text="Explore the microscopic world";
            chat=new GameObject("Knowledge Chat",typeof(RectTransform)).AddComponent<AssistantChatPanel>();
            chat.Build(safeRoot,this,settings);
            Navigation = gameObject.AddComponent<AssistantNavigation>();
            Navigation.Build(safeRoot, settings);
            Guidance = gameObject.AddComponent<AssistantGuidance>();
            Guidance.Build(safeRoot, this, settings);
            bubble.SetActive(false);
            UpdateCanvas();
        }
        private static RectTransform Rect(string name, Transform parent, Vector2 pos, Vector2 size)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent,false); r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);
            r.anchoredPosition=pos; r.sizeDelta=size; return r;
        }
        private Text Label(string name,Transform parent,Vector2 pos,Vector2 size,int fontSize,Color color,TextAnchor align=TextAnchor.UpperLeft)
        {
            var text=Rect(name,parent,pos,size).gameObject.AddComponent<Text>();
            text.font=TMPro.TMP_Settings.defaultFontAsset.sourceFontFile;
            text.fontSize=fontSize; text.color=color; text.alignment=align;
            text.raycastTarget=false; text.supportRichText=false;
            text.horizontalOverflow=HorizontalWrapMode.Wrap; text.verticalOverflow=VerticalWrapMode.Truncate;
            return text;
        }
        private void SmallButton(string name,Transform parent,Vector2 pos,Vector2 size,string label,UnityEngine.Events.UnityAction action)
        {
            var r=Rect(name,parent,pos,size);
            var image=r.gameObject.AddComponent<Image>(); image.color=new Color(.20f,.38f,.48f,.28f);
            var b=r.gameObject.AddComponent<Button>(); b.targetGraphic=image; b.onClick.AddListener(action);
            Label("Label",r,Vector2.zero,size,17,new Color(.78f,.92f,.96f),TextAnchor.MiddleCenter).text=label;
        }
        private void UpdateCanvas()
        {
            bool xr=XRSettings.isDeviceActive;
            Camera camera=Camera.main;
            if (xr && camera != null)
            {
                if (!xrMode || viewer != camera)
                {
                    canvas.renderMode=RenderMode.WorldSpace; canvas.worldCamera=camera;
                    canvasRect.SetParent(camera.transform,false);
                    canvasRect.sizeDelta=new Vector2(1280,720); canvasRect.localScale=Vector3.one*.0015f;
                    canvasRect.localPosition=new Vector3(0,0,1.6f); canvasRect.localRotation=Quaternion.identity;
                    scaler.enabled=false;
                    safeRoot.anchorMin=Vector2.zero; safeRoot.anchorMax=Vector2.one;
                }
            }
            else
            {
                if (xrMode || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                {
                    canvasRect.SetParent(transform,false); canvas.renderMode=RenderMode.ScreenSpaceOverlay;
                    canvas.worldCamera=null; scaler.enabled=true;
                }
                if (lastSafeArea != Screen.safeArea || xrMode)
                {
                    lastSafeArea=Screen.safeArea;
                    safeRoot.anchorMin=new Vector2(lastSafeArea.xMin/Mathf.Max(1,Screen.width),lastSafeArea.yMin/Mathf.Max(1,Screen.height));
                    safeRoot.anchorMax=new Vector2(lastSafeArea.xMax/Mathf.Max(1,Screen.width),lastSafeArea.yMax/Mathf.Max(1,Screen.height));
                }
            }
            xrMode=xr && camera != null; viewer=camera;
            hint.text=xrMode ? "Select the orb to wake me" : settings.activationKey + " - Wake assistant";
        }
        private void OnApplicationFocus(bool value) { focused=value; ReportActivity(); }
        private void OnDisable()
        {
            if (Navigation != null) Navigation.Clear();
            if (Guidance != null) Guidance.Clear();
            if (chat != null) chat.Close();
            if (canvas != null) canvas.enabled=false;
            ReportActivity();
        }
        private void OnEnable()
        {
            if (canvas != null) canvas.enabled=true;
            ReportActivity();
        }
        private void OnDestroy()
        {
            // XR canvas is parented under the camera, outside this object's hierarchy.
            if (canvas != null) Destroy(canvas.gameObject);
            if (ownsSettings && settings != null) Destroy(settings);
        }
    }
}
