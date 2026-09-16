using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VRMicroscope.Assistant
{
    [Serializable] public sealed class NavigationTargetSnapshot { public string id; public Vector3 worldPosition; }
    [Serializable] public sealed class NavigationSnapshot
    {
        public string snapshotId;
        public Vector3 playerPosition, playerForward;
        public float worldUnitsPerMeter;
        public NavigationTargetSnapshot[] targets;
    }

    // Location-only whitelist. Never executes instrument controls or modifies their materials.
    public sealed class AssistantNavigation : MonoBehaviour
    {
        private LocalAssistantSettings settings;
        private readonly Dictionary<string, Transform> offered = new Dictionary<string, Transform>();
        private string snapshotId, targetId;
        private Transform target;
        private Bounds bounds;
        private GameObject marker, hudRoot;
        private LineRenderer outline, beam;
        private Material material;
        private Text hud;
        private float expires, nextRefresh;
        private Vector3 lastHorizontalForward = Vector3.forward;
        private float Units => Mathf.Clamp(settings.worldUnitsPerMeter, .01f, 1000f);

        public void Build(Transform parent, LocalAssistantSettings config)
        {
            settings = config;
            hudRoot = new GameObject("Learning Location", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = (RectTransform)hudRoot.transform;
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(154, -580); rect.sizeDelta = new Vector2(860, 66);
            hudRoot.GetComponent<Image>().color = new Color(.025f, .055f, .095f, .9f);
            var textObject = new GameObject("Location", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(rect, false);
            hud = textObject.GetComponent<Text>(); hud.font = TMPro.TMP_Settings.defaultFontAsset.sourceFontFile; hud.fontSize = 20;
            hud.color = Color.white; hud.supportRichText = false; hud.raycastTarget = false;
            hud.resizeTextForBestFit = true; hud.resizeTextMinSize = 16; hud.resizeTextMaxSize = 20;
            hud.alignment = TextAnchor.MiddleLeft;
            hud.rectTransform.anchorMin = Vector2.zero; hud.rectTransform.anchorMax = Vector2.one;
            hud.rectTransform.offsetMin = new Vector2(12, 4); hud.rectTransform.offsetMax = new Vector2(-100, -4);
            var cancel = new GameObject("Cancel Marker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            var cr = (RectTransform)cancel.transform; cr.SetParent(rect, false);
            cr.anchorMin = cr.anchorMax = new Vector2(1, .5f); cr.anchoredPosition = new Vector2(-46, 0); cr.sizeDelta = new Vector2(84, 46);
            cancel.GetComponent<Image>().color = new Color(.14f, .25f, .32f, 1);
            cancel.GetComponent<Button>().onClick.AddListener(Clear);
            var label = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            label.transform.SetParent(cr, false);
            var t = label.GetComponent<Text>(); t.font = TMPro.TMP_Settings.defaultFontAsset.sourceFontFile; t.text = "Clear Marker"; t.fontSize = 17; t.alignment = TextAnchor.MiddleCenter; t.raycastTarget = false;
            t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one; t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;
            hudRoot.SetActive(false);
        }

        private bool Available()
        {
            if (settings == null || !settings.navigationEnabled || Camera.main == null || MicroscopeExploderModeController.IsTutorialModeLocked) return false;
            if (Interactor.Instance == null || Interactor.Instance.CurrentState != Interactor.GameState.Roaming) return false;
            var mode = MicroscopeExploderModeController.Instance;
            if (mode == null || mode.CurrentMode != MicroscopeExploderModeController.AssemblyMode.Normal) return false;
            foreach (var tutorial in FindObjectsOfType<StandaloneTutorialUI>()) if (tutorial.IsPlaying) return false;
            foreach (var snom in FindObjectsOfType<SNOMDemonstrationController>()) if (snom.IsRunning) return false;
            return true;
        }

        public bool NearMicroscope()
        {
            var mode = MicroscopeExploderModeController.Instance;
            return Camera.main != null && mode != null && mode.LearningStationRoot != null &&
                TryBounds(mode.LearningStationRoot, out var b) && EdgeDistance(b) <= Mathf.Max(.1f, settings.arrivalDistanceMeters);
        }

        public NavigationSnapshot Capture()
        {
            offered.Clear(); snapshotId = Guid.NewGuid().ToString("N");
            var camera = Camera.main;
            var result = new NavigationSnapshot { snapshotId = snapshotId, worldUnitsPerMeter = Units,
                playerPosition = camera != null ? camera.transform.position : Vector3.zero,
                playerForward = camera != null ? camera.transform.forward : Vector3.forward };
            var list = new List<NavigationTargetSnapshot>();
            if (Available() && Vector3.ProjectOnPlane(result.playerForward, Vector3.up).sqrMagnitude > .0001f)
            {
                var microscope = MicroscopeExploderModeController.Instance.LearningStationRoot;
                Add("parts", microscope, list);
                Add("na_experiment", microscope, list);
                Add("spatial_frequency", microscope, list);
                Add("snom_entry", SNOMDemonstrationController.FindSnomRoot(), list);

                var sampleComponents = FindObjectsOfType<InteractableSamples>(true);
                for (int i = 0; i < sampleComponents.Length; i++)
                {
                    var s = sampleComponents[i];
                    if (s == null || !s.gameObject.activeInHierarchy) continue;
                    string key = s.SampleTargetId;
                    if (!string.IsNullOrEmpty(key) && !offered.ContainsKey(key))
                    {
                        Add(key, s.transform, list);
                    }
                }
            }
            result.targets = list.ToArray(); return result;
        }

        private void Add(string id, Transform root, List<NavigationTargetSnapshot> list)
        {
            if (root == null || !root.gameObject.activeInHierarchy || !TryBounds(root, out var b)) return;
            offered[id] = root; list.Add(new NavigationTargetSnapshot { id = id, worldPosition = b.center });
        }

        public string Apply(AssistantChatResponse response, NavigationSnapshot request)
        {
            if (response.kind != "guide") return response.answer;
            Clear();
            string id = response.interaction_ids[0];
            if (request == null || request.snapshotId != snapshotId || response.navigationSnapshotId != snapshotId || !Available() ||
                !offered.TryGetValue(id, out var root) || root == null || !root.gameObject.activeInHierarchy || !TryBounds(root, out bounds))
                return "The scene or learning state has changed. Return to free exploration in the laboratory and ask again to mark this location.";
            if (EdgeDistance(bounds) <= Mathf.Max(.1f, settings.arrivalDistanceMeters))
                return "You are already near " + Name(id) + ". Here you can learn about " + Topic(id) + ". No distant marker is needed.";
            var shader = Resources.Load<Shader>("AssistantNavigationHighlight");
            if (shader == null || !shader.isSupported) return "You can use " + Name(id) + " to learn about " + Topic(id) + ". It is " + Relation(bounds.center) + "; the marker effect is currently unavailable.";
            material = new Material(shader);
            marker = new GameObject("Assistant Learning Marker");
            outline = Line("Instrument bounds", 16); beam = Line("Location beacon", 2);
            target = root; targetId = id; expires = Time.unscaledTime + Mathf.Max(10f, settings.markerLifetimeSeconds);
            RefreshMarker(); hudRoot.SetActive(true);
            return "You can use " + Name(id) + " to learn about " + Topic(id) + ". It is " + Relation(bounds.center) +
                ". An outline and light column now mark it and will disappear as you approach. This is a straight-line direction; follow an accessible route.";
        }

        private LineRenderer Line(string name, int count)
        {
            var go = new GameObject(name); go.transform.SetParent(marker.transform, false);
            var line = go.AddComponent<LineRenderer>(); line.sharedMaterial = material; line.useWorldSpace = true;
            line.positionCount = count; line.widthMultiplier = .012f * Units;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; line.receiveShadows = false;
            return line;
        }

        private void Update()
        {
            if (target == null) { if (marker != null) Clear(); return; }
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + .2f;
            if (!Available() || !target.gameObject.activeInHierarchy || Time.unscaledTime >= expires ||
                !TryBounds(target, out bounds) || EdgeDistance(bounds) <= Mathf.Max(.1f, settings.arrivalDistanceMeters)) { Clear(); return; }
            RefreshMarker();
        }

        private void RefreshMarker()
        {
            var min = bounds.min - Vector3.one * .025f * Units; var max = bounds.max + Vector3.one * .025f * Units;
            var corners = new[] { new Vector3(min.x,min.y,min.z), new Vector3(max.x,min.y,min.z), new Vector3(max.x,min.y,max.z), new Vector3(min.x,min.y,max.z),
                new Vector3(min.x,max.y,min.z), new Vector3(max.x,max.y,min.z), new Vector3(max.x,max.y,max.z), new Vector3(min.x,max.y,max.z) };
            int[] path = { 0,1,2,3,0,4,5,1,5,6,2,6,7,3,7,4 };
            for (int i = 0; i < path.Length; i++) outline.SetPosition(i, corners[path[i]]);
            var top = new Vector3(bounds.center.x, max.y, bounds.center.z);
            beam.SetPosition(0, top); beam.SetPosition(1, top + Vector3.up * .65f * Units);
            float alpha = settings.reducedMotion ? .85f : .72f + .18f * Mathf.Sin(Time.unscaledTime * 3f);
            material.SetColor("_Color", new Color(.4f, 1f, 1f, alpha));
            hud.text = Name(targetId) + " · " + Relation(bounds.center) + "\nClears on approach - straight-line direction";
        }

        private float EdgeDistance(Bounds b)
        {
            var p = Camera.main.transform.position; p.y = b.center.y;
            return Vector3.Distance(p, b.ClosestPoint(p)) / Units;
        }
        private string Relation(Vector3 position)
        {
            var camera = Camera.main.transform; var delta = position - camera.position; delta.y = 0;
            var forward = Vector3.ProjectOnPlane(camera.forward, Vector3.up);
            if (forward.sqrMagnitude > .0001f) lastHorizontalForward = forward.normalized;
            float angle = Vector3.SignedAngle(lastHorizontalForward, delta, Vector3.up);
            string direction = Mathf.Abs(angle) <= 45 ? "ahead" : Mathf.Abs(angle) >= 135 ? "behind you" : angle > 0 ? "to your right" : "to your left";
            return direction + " about " + (delta.magnitude / Units).ToString("F1") + " m away";
        }
        private static bool TryBounds(Transform root, out Bounds result)
        {
            result = new Bounds(); bool found = false;
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>())
            {
                if (!renderer.enabled || renderer.GetComponent<MeshFilter>() == null) continue;
                if (!found) result = renderer.bounds; else result.Encapsulate(renderer.bounds);
                found = true;
            }
            if (!found)
            {
                foreach (var col in root.GetComponentsInChildren<Collider>())
                {
                    if (!col.enabled) continue;
                    if (!found) result = col.bounds; else result.Encapsulate(col.bounds);
                    found = true;
                }
            }
            if (!found && root != null)
            {
                result = new Bounds(root.position, Vector3.one * 0.2f);
                found = true;
            }
            return found;
        }
        private static string Name(string id) =>
            id == "snom_entry" ? "the THz s-SNOM instrument" :
            id == "sample_red" ? "the red specimen slide" :
            id == "sample_green" ? "the green specimen slide" :
            id == "sample_blue" ? "the blue specimen slide" :
            id == "sample_yellow" ? "the yellow specimen slide" :
            "the microscope learning area";
        private static string Topic(string id) =>
            id == "snom_entry" ? "the demonstration of probe tapping, near-field coupling, and scanning" :
            id == "na_experiment" ? "numerical aperture (experiment linked to Upper Optical Assembly)" :
            id == "spatial_frequency" ? "spatial frequency (experiment linked to Objective Lens)" :
            id == "sample_red" ? "handling the red fluorescence specimen for microscope observation" :
            id == "sample_green" ? "handling the green fluorescence specimen for microscope observation" :
            id == "sample_blue" ? "handling the blue fluorescence specimen for microscope observation" :
            id == "sample_yellow" ? "handling the yellow fluorescence specimen for microscope observation" :
            "microscope structure and component functions";
        public void Clear()
        {
            target = null;
            if (marker != null) { marker.SetActive(false); Destroy(marker); } marker = null;
            if (material != null) Destroy(material); material = null;
            if (hudRoot != null) hudRoot.SetActive(false);
        }
        private void OnDisable() { Clear(); }
        private void OnDestroy() { Clear(); if (hudRoot != null) Destroy(hudRoot); }
    }
}
