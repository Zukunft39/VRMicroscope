using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

namespace VRMicroscope.Assistant
{
    [Serializable] public sealed class GuidanceAction
    {
        public string id, interaction_id, knowledge_topic, desktop, xr, observation;
    }
    [Serializable] public sealed class GuidanceCatalog { public string version; public GuidanceAction[] actions; }
    [Serializable] public sealed class GuidanceSnapshot
    {
        public string snapshotId, catalogVersion, device, mode, selectedPart, snomPhase, snomStage, illumination;
        public string partDescription, partExperiment;
        public bool blocked, hasSample, placedSample, playing, componentMode;
        public float na;
        public int frequencyProfile, observationPoint, selectedProbe = -1, installedProbe = -1;
        public string[] allowedActionIds;
    }

    // Read-only adapter: these suggestions never invoke experiment methods or input callbacks.
    public sealed class AssistantGuidance : MonoBehaviour
    {
        private LocalAssistantController owner;
        private GuidanceCatalog catalog;
        private string pendingSignature, snapshotId, cardSignature;
        private GameObject card;
        private Text text;
        private float expires, nextCheck;
        public void Build(Transform parent, LocalAssistantController controller, LocalAssistantSettings config)
        {
            owner = controller;
            var asset = Resources.Load<TextAsset>("AssistantGuidanceActions");
            if (asset != null) catalog = JsonUtility.FromJson<GuidanceCatalog>(asset.text);
            card = new GameObject("Learning Step Reminder", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var r = (RectTransform)card.transform; r.SetParent(parent, false);
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1); r.anchoredPosition = new Vector2(154, -28); r.sizeDelta = new Vector2(760, 180);
            card.GetComponent<Image>().color = new Color(.025f, .055f, .095f, .9f);
            var label = new GameObject("Instruction", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            label.transform.SetParent(r, false); text = label.GetComponent<Text>();
            text.font = TMPro.TMP_Settings.defaultFontAsset.sourceFontFile; text.fontSize = 20; text.supportRichText = false; text.raycastTarget = false;
            text.resizeTextForBestFit = true; text.resizeTextMinSize = 16; text.resizeTextMaxSize = 20;
            text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(14, 12); text.rectTransform.offsetMax = new Vector2(-50, -12);
            card.GetComponent<Image>().raycastTarget = false;
            var close = new GameObject("Dismiss Step", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            var cr = (RectTransform)close.transform; cr.SetParent(r, false); cr.anchorMin = cr.anchorMax = new Vector2(1, 1);
            cr.anchoredPosition = new Vector2(-22, -22); cr.sizeDelta = new Vector2(32, 32);
            close.GetComponent<Image>().color = new Color(.13f, .24f, .3f, 1);
            close.GetComponent<Button>().onClick.AddListener(Clear);
            var cross = new GameObject("Close Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            cross.transform.SetParent(cr, false); var ct = cross.GetComponent<Text>(); ct.font = TMPro.TMP_Settings.defaultFontAsset.sourceFontFile; ct.text = "×";
            ct.fontSize = 24; ct.alignment = TextAnchor.MiddleCenter; ct.raycastTarget = false;
            ct.rectTransform.anchorMin = Vector2.zero; ct.rectTransform.anchorMax = Vector2.one; ct.rectTransform.offsetMin = ct.rectTransform.offsetMax = Vector2.zero;
            Clear();
        }

        public static bool Usable(Selectable control)
        {
            if (control == null || !control.isActiveAndEnabled || !control.IsInteractable()) return false;
            var canvas = control.GetComponentInParent<Canvas>();
            if (canvas == null || !canvas.isActiveAndEnabled) return false;
            foreach (var group in control.GetComponentsInParent<CanvasGroup>())
            {
                if (!group.interactable || !group.blocksRaycasts || group.alpha <= .01f) return false;
                if (group.ignoreParentGroups) break;
            }
            return true;
        }
        private static void Offer(List<string> ids, string id, Selectable control)
        { if (Usable(control)) ids.Add("learn:" + id); }
        private static T Active<T>() where T : Behaviour
        { foreach (var item in FindObjectsOfType<T>()) if (item.isActiveAndEnabled) return item; return null; }

        private GuidanceSnapshot Read()
        {
            var state = new GuidanceSnapshot { catalogVersion = catalog != null ? catalog.version : "missing",
                device = XRSettings.isDeviceActive ? "xr" : "desktop", mode = "unknown", selectedPart = "",
                snomPhase = "Idle", snomStage = "", illumination = "", partDescription = "", partExperiment = "", allowedActionIds = new string[0] };
            var ids = new List<string>();
            var interactor = Interactor.Instance;
            var mode = MicroscopeExploderModeController.Instance;
            var na = Active<NumericalApertureExperimentController>();
            var sf = Active<SpatialFrequencyExperimentController>();
            var snom = Active<SNOMDemonstrationController>();
            var selection = SuperAssemblyPartSelectionController.Instance;
            state.blocked = interactor == null || mode == null || catalog == null || Time.timeScale <= 0 ||
                MicroscopeExploderModeController.IsTutorialModeLocked || interactor.CurrentState == Interactor.GameState.Tutorial ||
                mode.AssistantAnimating || na != null && na.IsTransitioning || sf != null && sf.IsTransitioning;
            foreach (var tutorial in FindObjectsOfType<StandaloneTutorialUI>()) if (tutorial.isActiveAndEnabled && tutorial.IsPlaying) state.blocked = true;
            if (state.blocked) { state.mode = "blocked"; return state; }
            var samples = interactor.GetComponent<InteractWithSamples>();
            var microscope = interactor.AssistantMicroscope;
            state.hasSample = microscope != null ? microscope.AssistantHasHandObject : samples != null && samples.AssistantHasHandSample;
            state.placedSample = microscope != null && microscope.HasPlacedSample();
            state.observationPoint = microscope != null ? microscope.pointer : 0;
            if (na != null && na.IsExperimentActive)
            {
                state.mode = "na"; state.na = na.CurrentNA;
                Offer(ids, "na_adjust", na.AssistantSlider); Offer(ids, "na_exit", na.AssistantExit);
            }
            else if (sf != null && sf.IsExperimentActive)
            {
                state.mode = "spatial_frequency"; state.frequencyProfile = sf.AssistantProfile; state.illumination = sf.AssistantIllumination;
                string[] keys = { "sf_high", "sf_middle", "sf_low", "sf_white", "sf_laser", "sf_exit" };
                var controls = sf.AssistantControls;
                for (int i = 0; i < keys.Length; i++) Offer(ids, keys[i], controls[i]);
            }
            else if (snom != null && snom.IsRunning)
            {
                state.mode = "snom"; state.snomPhase = snom.AssistantPhase; state.snomStage = snom.CurrentStage.ToString();
                state.selectedProbe = snom.AssistantSelectedProbe; state.installedProbe = snom.AssistantInstalledProbe;
                state.playing = snom.AssistantPlaying; state.componentMode = snom.AssistantComponentMode;
                snom.AppendAssistantActions(ids);
            }
            else if (selection != null && selection.IsSelectionActive)
            {
                state.mode = "part_selected";
                state.selectedPart = selection.AssistantPartName;
                state.partDescription = selection.AssistantPartDescription;
                state.partExperiment = selection.AssistantExperimentId;
                ids.Add("learn:parts_exit");
                if (!string.IsNullOrEmpty(state.partExperiment) && selection.AssistantCanStartExperiment) ids.Add("learn:" + state.partExperiment);
            }
            else if (mode.CurrentMode != MicroscopeExploderModeController.AssemblyMode.Normal)
            {
                state.mode = mode.CurrentMode.ToString(); ids.Add("learn:assembly_exit");
                if (mode.CurrentMode == MicroscopeExploderModeController.AssemblyMode.PreAssembly) ids.Add("learn:assembly_expand");
                else if (selection != null && selection.CanSelectParts) ids.Add("learn:parts_select");
            }
            else
            {
                state.mode = interactor.CurrentState.ToString();
                if (interactor.CurrentState == Interactor.GameState.Observing)
                {
                    ids.Add("learn:observe_exit");
                    if (microscope != null && microscope.AssistantObserving)
                    {
                        ids.AddRange(new[] { "learn:focus", "learn:brightness" });
                        if (!microscope.AssistantObjectiveMoving) ids.Add("learn:objective");
                    }
                }
                else
                {
                    if (snom != null) snom.AppendAssistantActions(ids);
                    if (owner.Navigation.NearMicroscope()) ids.Add("learn:assembly_enter");
                    if (microscope != null && microscope.AssistantCanInteract)
                    {
                        ids.AddRange(new[] { "learn:light", "learn:brightness" });
                        if (!microscope.AssistantObjectiveMoving) ids.Add("learn:objective");
                        if (state.placedSample) ids.AddRange(new[] { "learn:sample_remove", "learn:sample_observe" });
                        else if (microscope.AssistantHasHandObject) ids.Add("learn:sample_place");
                    }
                    if (!state.placedSample && samples != null && samples.AssistantCanPick && !(snom != null && snom.AssistantInRange)) ids.Add("learn:sample_pick");
                }
            }
            ids.RemoveAll(id => Find(id) == null || state.device == "desktop" && !DesktopBindingMatches(id));
            ids.Sort(StringComparer.Ordinal); state.allowedActionIds = ids.ToArray();
            return state;
        }

        private static bool DesktopBindingMatches(string id)
        {
            var c = CameraTryMove.AssistantDesktop;
            if (id == "learn:sample_place" || id == "learn:sample_observe" || id == "learn:observe_exit") return c != null && c.leftTriggerButtonKey == KeyCode.Z;
            if (id == "learn:objective") return c != null && c.rightPrimaryButtonKey == KeyCode.R;
            if (id == "learn:light") return c != null && c.rightSecondaryButtonKey == KeyCode.B;
            if (id == "learn:focus") return c != null && c.leftPrimaryButtonKey == KeyCode.X && c.rightStickPressKey == KeyCode.Tab && c.rightStickLeftKey == KeyCode.LeftArrow && c.rightStickRightKey == KeyCode.RightArrow;
            if (id == "learn:brightness") return c != null && c.leftPrimaryButtonKey == KeyCode.X && c.rightStickUpKey == KeyCode.UpArrow && c.rightStickDownKey == KeyCode.DownArrow;
            if (id.StartsWith("learn:assembly_", StringComparison.Ordinal) || id.StartsWith("learn:parts_", StringComparison.Ordinal) ||
                id == "learn:sample_pick" || id == "learn:sample_remove" || id == "learn:snom_open") return c != null && c.primaryClickMouseButton == 0;
            return true;
        }

        public GuidanceSnapshot Capture()
        {
            var state = Read(); pendingSignature = JsonUtility.ToJson(state);
            snapshotId = Guid.NewGuid().ToString("N"); state.snapshotId = snapshotId; return state;
        }
        public GuidanceAction Find(string id)
        { if (catalog != null) foreach (var a in catalog.actions) if (a.id == id) return a; return null; }
        public string Apply(AssistantChatResponse response, GuidanceSnapshot request)
        {
            Clear();
            var current = Read(); string signature = JsonUtility.ToJson(current);
            if (request == null || request.snapshotId != snapshotId || response.guidanceSnapshotId != snapshotId || signature != pendingSignature)
                return "The experiment state has changed. Ask for the next step again for guidance on the current panel.";
            string id = response.suggested_action_ids[0]; var action = Find(id);
            if (action == null || Array.IndexOf(current.allowedActionIds, id) < 0 ||
                response.interaction_ids[0] != action.interaction_id || response.knowledge_topics[0] != action.knowledge_topic)
                return "This action is currently unavailable. Ask again using the current panel state.";
            string instruction = current.device == "xr" ? action.xr : action.desktop;
            string result = "After closing the chat window, " + instruction + "\nObserve: " + action.observation;
            owner.Navigation.Clear(); cardSignature = signature; expires = Time.unscaledTime + 120f;
            text.text = "Current step\n" + instruction + "\nObserve: " + action.observation;
            return result;
        }
        private void Update()
        {
            if (card == null || cardSignature == null) return;
            if (Time.unscaledTime >= nextCheck)
            {
                nextCheck = Time.unscaledTime + .5f;
                if (Time.unscaledTime >= expires || JsonUtility.ToJson(Read()) != cardSignature) { Clear(); return; }
            }
            card.SetActive(!AssistantChatPanel.BlocksGameplay && !owner.IsMessageVisible);
        }
        public void Clear() { cardSignature = null; if (card != null) card.SetActive(false); }
        private void OnDisable() { Clear(); }
        private void OnDestroy() { if (card != null) Destroy(card); }
    }
}
