using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;

public sealed class SNOMDemonstrationController : MonoBehaviour
{
    private enum WorkflowPhase
    {
        Idle,
        ProbeSelection,
        InstallingProbe,
        ReadyToStart,
        PrincipleTour
    }

    public enum DemonstrationStage
    {
        Overview,
        ThzGeneration,
        BeamSteering,
        IdentifyProbe,
        AfmFeedback,
        NearFieldCoupling,
        Scattering,
        HarmonicDemodulation,
        RasterScan,
        Results
    }

    [Serializable]
    private sealed class StageContent
    {
        public string title;
        public string explanation;
        public string formula;
        public SNOMDemonstrationGraphic.DiagramMode diagram;
    }

    private sealed class ComponentEntry
    {
        public Transform target;
        public string displayName;
        public string description;
        public GameObject proxy;
    }

    private sealed class ProbeOption
    {
        public string displayName;
        public string shortLabel;
        public string description;
        public string resolutionLabel;
        public float previewScale;
        public GameObject preview;
        public Vector3 previewPosition;
        public Quaternion previewRotation;
        public Vector3 previewLocalScale;
        public Button button;
        public Image buttonImage;
    }

    private const string ModelRootName = "TDs_edited_UnityVeryLowPoly";
    private static readonly Color PanelColor = new Color(0.015f, 0.045f, 0.06f, 0.97f);
    private static readonly Color SurfaceColor = new Color(0.035f, 0.09f, 0.115f, 0.98f);
    private static readonly Color BorderColor = new Color(0.09f, 0.32f, 0.39f, 1f);
    private static readonly Color MutedTextColor = new Color(0.56f, 0.66f, 0.70f, 1f);
    private static readonly Color AccentColor = new Color(0.08f, 0.78f, 0.94f, 1f);
    private static readonly Color ReadyColor = new Color(0.20f, 0.78f, 0.55f, 1f);
    private static readonly Color ActiveButtonColor = new Color(0.04f, 0.32f, 0.72f, 1f);
    private static readonly Color InactiveButtonColor = new Color(0.07f, 0.14f, 0.18f, 1f);
    private static readonly Color IncidentColor = new Color(1f, 0.55f, 0.08f, 1f);
    private static readonly Color ScatteredColor = new Color(0.08f, 0.82f, 0.94f, 1f);
    private static readonly Color AfmColor = new Color(0.18f, 0.92f, 0.49f, 1f);
    private static readonly Color SelectedProbeColor = new Color(0.02f, 0.48f, 0.76f, 1f);
    private static readonly DemonstrationStage[] PrincipleStages =
    {
        DemonstrationStage.ThzGeneration,
        DemonstrationStage.BeamSteering,
        DemonstrationStage.AfmFeedback,
        DemonstrationStage.NearFieldCoupling,
        DemonstrationStage.Scattering,
        DemonstrationStage.HarmonicDemodulation,
        DemonstrationStage.RasterScan,
        DemonstrationStage.Results
    };
    private static TMP_FontAsset cachedInterfaceFont;

    [Header("Fixed SNOM Root")]
    [SerializeField] private Transform snomRoot;

    [Header("Resolved Components")]
    [SerializeField] private Transform emitterAntenna;
    [SerializeField] private Transform receiverAntenna;
    [SerializeField] private Transform fastMirrorA;
    [SerializeField] private Transform fastMirrorB;
    [SerializeField] private Transform parabolicMirror;
    [SerializeField] private Transform afmHead;
    [SerializeField] private Transform probe;
    [SerializeField] private Transform probeMount;
    [SerializeField] private Transform probePositioningBlock;
    [SerializeField] private Transform afmLaser;
    [SerializeField] private Transform quadrantDetector;
    [SerializeField] private Transform scanStageA;
    [SerializeField] private Transform scanStageB;
    [SerializeField] private Transform preamplifier;
    [SerializeField] private Transform optionalDetector;
    [SerializeField] private Transform optionalConnector;

    [Header("Animation")]
    [SerializeField, Min(0.0001f)] private float exaggeratedProbeAmplitude = 0.0012f;
    [SerializeField, Min(0.1f)] private float probeFrequency = 2.2f;
    [SerializeField, Min(1f)] private float scanDuration = 8f;
    [SerializeField, Min(0.25f)] private float probeInstallationDuration = 1.25f;
    [SerializeField, Min(2f)] private float automaticStageDuration = 8f;
    [SerializeField] private bool automaticallyAdvancePrinciples = true;
    [SerializeField] private bool enableDebugLogs;

    [Header("Proximity Highlight")]
    [SerializeField, Min(0.1f)] private float proximityHighlightDistance = 0.7f;
    [SerializeField, Min(0f)] private float proximityExitHysteresis = 0.8f;
    [SerializeField, ColorUsage(false, true)] private Color proximityHighlightColor = new Color(0.08f, 0.75f, 1f, 1f);
    [SerializeField, Min(0f)] private float proximityHighlightIntensity = 3.2f;
    private MeshFilter[] proximityMeshes;
    private Renderer[] proximityRenderers;
    private Bounds proximityBounds;
    private Material proximityMaterial;
    private Camera proximityViewer;
    private bool isPlayerInInteractionRange;
    private bool proximityHighlighted;
    private float nextProximityCheck;

    private readonly List<ComponentEntry> components = new List<ComponentEntry>();
    private StageContent[] stages;
    private DemonstrationStage currentStage;
    private int selectedComponentIndex;
    private bool isRunning;
    private bool isComponentMode;
    private bool stagePlaying = true;
    private WorkflowPhase workflowPhase;
    private readonly List<ProbeOption> probeOptions = new List<ProbeOption>();
    private readonly List<GameObject> tourControlObjects = new List<GameObject>();
    private int selectedProbeIndex = -1;
    private int installedProbeIndex = -1;
    private float probeInstallationProgress;
    private float stageElapsed;
    private float activationPulseUntil;
    private Vector3 installingProbeStartPosition;
    private Quaternion installingProbeStartRotation;
    private Vector3 installingProbeStartScale;
    private Renderer[] installedProbeRenderers;
    private bool[] installedProbeRendererStates;
    private bool setupComplete;
    private bool warnedAboutRootPose;
    private float scanProgress;
    private float beamTravel;

    private Transform cachedRootParent;
    private Vector3 cachedRootLocalPosition;
    private Quaternion cachedRootLocalRotation;
    private Vector3 cachedRootLocalScale;
    private Vector3 cachedProbeLocalPosition;
    private bool hasCachedProbePose;

    private GameObject runtimeVisualRoot;
    private GameObject entryInteractionProxy;
    private LineRenderer incidentPath;
    private LineRenderer scatteredPath;
    private LineRenderer afmPath;
    private LineRenderer highlightRing;
    private GameObject beamMarker;
    private GameObject nearFieldHotspot;
    private GameObject sampleTile;
    private GameObject scanCursor;
    private Material incidentMaterial;
    private Material scatteredMaterial;
    private Material afmMaterial;
    private Material highlightMaterial;
    private Material darkMaterial;

    private GameObject canvasRoot;
    private GameObject launcherRoot;
    private GameObject panelRoot;
    private GameObject topBarRoot;
    private GameObject contentCardRoot;
    private GameObject diagramCardRoot;
    private GameObject surfaceCardRoot;
    private RectTransform surfaceCover;
    private RectTransform surfaceProbeMarker;
    private RectTransform surfaceScanLine;
    private Image surfaceResponse;
    private TextMeshProUGUI surfaceStatusText;
    private GameObject tourControlsRoot;
    private TextMeshProUGUI headerStatusText;
    private Image headerStatusDot;
    private TextMeshProUGUI stageNumberText;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI explanationText;
    private TextMeshProUGUI formulaText;
    private TextMeshProUGUI statusText;
    private TextMeshProUGUI playButtonText;
    private TextMeshProUGUI componentButtonText;
    private Image stageProgressFill;
    private SNOMDemonstrationGraphic diagramGraphic;
    private GameObject probeChoiceControlsRoot;
    private Button workflowActionButton;
    private Image workflowActionImage;
    private TextMeshProUGUI workflowActionText;
    private GameObject workflowChangeProbeButton;

    public Transform SnomRoot => snomRoot;
    public DemonstrationStage CurrentStage => currentStage;
    public bool IsRunning => isRunning;
    public bool SuppressAssistantReminders => isRunning &&
        (isComponentMode || workflowPhase == WorkflowPhase.InstallingProbe ||
         workflowPhase == WorkflowPhase.PrincipleTour && stagePlaying && !isComponentMode);

    public static bool TryHandleDesktopPrimaryClick(Ray ray, float maxDistance, bool isPointerOverUi)
    {
        if (isPointerOverUi)
        {
            return false;
        }

        SNOMDemonstrationController controller = FindObjectOfType<SNOMDemonstrationController>(true);
        return controller != null && controller.TryRevealLauncher(ray, maxDistance);
    }

    public static bool TryHandleRightTrigger()
    {
        SNOMDemonstrationController controller = FindObjectOfType<SNOMDemonstrationController>(true);
        if (controller == null || !controller.isPlayerInInteractionRange || controller.isRunning ||
            controller.entryInteractionProxy == null ||
            !controller.entryInteractionProxy.activeInHierarchy)
        {
            return false;
        }

        XRRayInteractor[] rayInteractors = FindObjectsOfType<XRRayInteractor>(true);
        for (int i = 0; i < rayInteractors.Length; i++)
        {
            XRRayInteractor rayInteractor = rayInteractors[i];
            if (rayInteractor == null || !rayInteractor.isActiveAndEnabled ||
                !rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
            {
                continue;
            }

            SNOMEntryInteractionProxy proxy = hit.collider.GetComponentInParent<SNOMEntryInteractionProxy>();
            if (proxy != null && proxy.Controller == controller)
            {
                return controller.RevealLauncherFromInteraction();
            }
        }

        return false;
    }

    public void Configure(Transform fixedSnomRoot)
    {
        if (fixedSnomRoot != null)
        {
            snomRoot = fixedSnomRoot;
        }

        if (Application.isPlaying && !setupComplete)
        {
            SetupRuntime();
        }
    }

    private void Start()
    {
        SetupRuntime();
    }

    private void Update()
    {
        if (!setupComplete)
        {
            SetupRuntime();
        }

        if (!isRunning)
        {
            return;
        }

        HandleKeyboardAndMouse();
        UpdateAnimation(Time.unscaledDeltaTime, Time.unscaledTime);
        UpdateSurfaceObservation();
        ValidateFixedRootPose();
    }

    private void LateUpdate()
    {
        UpdateProximityHighlight();
        if (!isRunning || !setupComplete)
        {
            return;
        }

        UpdateWorldVisuals(Time.unscaledTime);
    }

    private void OnDisable()
    {
        isRunning = false;
        isComponentMode = false;
        stagePlaying = false;
        workflowPhase = WorkflowPhase.Idle;
        proximityHighlighted = false;
        RestoreProbePose();
        RestoreInstalledProbeVisibility();
        SetProbePreviewsActive(false);
        SetOwnedVisualsActive(false);
        SetProxyObjectsActive(false);
        SetEntryInteractionActive(true);
        if (canvasRoot != null)
        {
            canvasRoot.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        RestoreProbePose();
        RestoreInstalledProbeVisibility();
        if (canvasRoot != null)
        {
            Destroy(canvasRoot);
        }

        DestroyOwnedMaterial(incidentMaterial);
        DestroyOwnedMaterial(scatteredMaterial);
        DestroyOwnedMaterial(afmMaterial);
        DestroyOwnedMaterial(highlightMaterial);
        DestroyOwnedMaterial(darkMaterial);
        DestroyOwnedMaterial(proximityMaterial);
    }

    private void SetupRuntime()
    {
        if (setupComplete)
        {
            return;
        }

        if (snomRoot == null)
        {
            snomRoot = FindSnomRoot();
        }

        if (snomRoot == null)
        {
            return;
        }

        CacheFixedRootPose();
        ResolveComponentReferences();
        BuildStageContent();
        BuildComponentCatalog();
        BuildProximityHighlight();
        BuildRuntimeVisuals();
        BuildProbeOptions();
        BuildEntryInteractionProxy();
        BuildUserInterface();
        BuildSelectionProxies();
        setupComplete = true;
        HideAllInterface();
        SetEntryInteractionActive(true);
        DebugLog("Runtime demonstration prepared. Select the SNOM system to reveal the tour launcher.");
    }

    public void BeginTour()
    {
        if (!setupComplete)
        {
            SetupRuntime();
        }

        if (!setupComplete || !isPlayerInInteractionRange)
        {
            HideAllInterface();
            return;
        }

        isRunning = true;
        isComponentMode = false;
        stagePlaying = false;
        warnedAboutRootPose = false;
        canvasRoot.SetActive(true);
        launcherRoot.SetActive(false);
        panelRoot.SetActive(true);
        SetEntryInteractionActive(false);
        SetProxyObjectsActive(false);
        BeginProbeSelection();
    }

    public void ExitDemonstration()
    {
        ResetDemonstrationState();
        ValidateFixedRootPose();
    }

    private void ResetDemonstrationState()
    {
        isRunning = false;
        isComponentMode = false;
        stagePlaying = false;
        workflowPhase = WorkflowPhase.Idle;
        currentStage = DemonstrationStage.Overview;
        selectedComponentIndex = 0;
        selectedProbeIndex = -1;
        installedProbeIndex = -1;
        probeInstallationProgress = 0f;
        stageElapsed = 0f;
        activationPulseUntil = 0f;
        scanProgress = 0f;
        beamTravel = 0f;
        RestoreProbePose();
        RestoreInstalledProbeVisibility();
        SetProbePreviewsActive(false);
        ResetStageVisuals();
        SetOwnedVisualsActive(false);
        SetProxyObjectsActive(false);
        HideAllInterface();
        SetEntryInteractionActive(true);
        if (diagramGraphic != null)
        {
            diagramGraphic.SetMode(SNOMDemonstrationGraphic.DiagramMode.Overview);
            diagramGraphic.SetProgress(0f);
        }
        SetStageProgress(0f);
    }

    public void Next()
    {
        if (workflowPhase != WorkflowPhase.PrincipleTour)
        {
            return;
        }

        if (isComponentMode)
        {
            ShowComponent(selectedComponentIndex + 1);
            return;
        }

        int current = GetPrincipleStageIndex(currentStage);
        EnterStage(PrincipleStages[Mathf.Min(PrincipleStages.Length - 1, current + 1)]);
    }

    public void Previous()
    {
        if (workflowPhase != WorkflowPhase.PrincipleTour)
        {
            return;
        }

        if (isComponentMode)
        {
            ShowComponent(selectedComponentIndex - 1);
            return;
        }

        int current = GetPrincipleStageIndex(currentStage);
        EnterStage(PrincipleStages[Mathf.Max(0, current - 1)]);
    }

    public void ReplayCurrent()
    {
        if (workflowPhase != WorkflowPhase.PrincipleTour)
        {
            return;
        }

        if (isComponentMode)
        {
            ShowComponent(selectedComponentIndex);
            return;
        }

        EnterStage(currentStage);
    }

    public void TogglePlayPause()
    {
        if (workflowPhase != WorkflowPhase.PrincipleTour || currentStage == DemonstrationStage.Results)
        {
            return;
        }

        stagePlaying = !stagePlaying;
        if (currentStage == DemonstrationStage.RasterScan && stagePlaying && scanProgress >= 0.999f)
        {
            scanProgress = 0f;
        }

        RefreshPlayButton();
    }

    public void ToggleComponentMode()
    {
        if (!isRunning || workflowPhase != WorkflowPhase.PrincipleTour)
        {
            return;
        }

        isComponentMode = !isComponentMode;
        RestoreProbePose();
        ResetStageVisuals();
        if (isComponentMode)
        {
            ShowComponent(selectedComponentIndex);
        }
        else
        {
            EnterStage(currentStage);
        }
    }

    public void ShowComponent(int index)
    {
        if (components.Count == 0)
        {
            return;
        }

        selectedComponentIndex = (index % components.Count + components.Count) % components.Count;
        ComponentEntry entry = components[selectedComponentIndex];
        isComponentMode = true;
        stagePlaying = false;
        ResetStageVisuals();
        SetHighlightTarget(entry.target);
        stageNumberText.text = $"COMPONENT {selectedComponentIndex + 1:00}/{components.Count:00}";
        titleText.text = entry.displayName;
        explanationText.text = entry.description;
        formulaText.text = "Point at another highlighted component, or use Previous / Next.";
        statusText.text = "Free component exploration: physical parts remain installed and fixed.";
        diagramGraphic.SetMode(GetComponentDiagram(entry.target));
        diagramGraphic.SetProgress(1f);
        SetStageProgress((selectedComponentIndex + 1f) / components.Count);
        componentButtonText.text = "Back to Tour";
        ApplyComponentLayout();
        SetHeaderStatus("COMPONENT DETAIL", ReadyColor);
        RefreshPlayButton();
    }

    private void EnterStage(DemonstrationStage stage)
    {
        if (stages == null || stages.Length == 0)
        {
            return;
        }

        RestoreProbePose();
        ResetStageVisuals();
        currentStage = stage;
        workflowPhase = WorkflowPhase.PrincipleTour;
        isComponentMode = false;
        stagePlaying = stage != DemonstrationStage.Results;
        scanProgress = stage == DemonstrationStage.RasterScan ? 0f : 1f;
        beamTravel = 0f;
        stageElapsed = 0f;

        StageContent content = stages[(int)stage];
        int principleIndex = GetPrincipleStageIndex(stage);
        stageNumberText.text = $"PRINCIPLE {principleIndex + 1:00}/{PrincipleStages.Length:00}";
        titleText.text = content.title;
        explanationText.text = content.explanation;
        formulaText.text = content.formula;
        diagramGraphic.SetMode(content.diagram);
        diagramGraphic.SetProgress(scanProgress);
        SetStageProgress((principleIndex + 1f) / PrincipleStages.Length);
        componentButtonText.text = "Components";
        statusText.text = GetStageStatus(stage);
        SetWorkflowControlsVisible(false);
        SetTourControlsVisible(true);
        ApplyPrincipleLayout();
        SetHeaderStatus("SYSTEM RUNNING", ReadyColor);
        ApplyStageVisuals(stage);
        RefreshPlayButton();
    }

    private void ApplyStageVisuals(DemonstrationStage stage)
    {
        if (runtimeVisualRoot == null)
        {
            return;
        }

        runtimeVisualRoot.SetActive(true);
        sampleTile.SetActive(stage >= DemonstrationStage.NearFieldCoupling);

        switch (stage)
        {
            case DemonstrationStage.Overview:
                // Avoid a giant ring when this imported node owns most of the apparatus.
                SetHighlightTarget(null);
                break;
            case DemonstrationStage.ThzGeneration:
                incidentPath.gameObject.SetActive(true);
                beamMarker.SetActive(true);
                SetHighlightTarget(emitterAntenna);
                break;
            case DemonstrationStage.BeamSteering:
                incidentPath.gameObject.SetActive(true);
                beamMarker.SetActive(true);
                SetHighlightTarget(parabolicMirror != null ? parabolicMirror : fastMirrorA);
                break;
            case DemonstrationStage.IdentifyProbe:
                SetHighlightTarget(probe);
                break;
            case DemonstrationStage.AfmFeedback:
                afmPath.gameObject.SetActive(true);
                beamMarker.SetActive(true);
                SetHighlightTarget(quadrantDetector != null ? quadrantDetector : afmLaser);
                break;
            case DemonstrationStage.NearFieldCoupling:
                incidentPath.gameObject.SetActive(true);
                nearFieldHotspot.SetActive(true);
                SetHighlightTarget(probe);
                break;
            case DemonstrationStage.Scattering:
                incidentPath.gameObject.SetActive(true);
                scatteredPath.gameObject.SetActive(true);
                beamMarker.SetActive(true);
                nearFieldHotspot.SetActive(true);
                SetHighlightTarget(receiverAntenna);
                break;
            case DemonstrationStage.HarmonicDemodulation:
                nearFieldHotspot.SetActive(true);
                SetHighlightTarget(preamplifier != null ? preamplifier : probe);
                break;
            case DemonstrationStage.RasterScan:
                nearFieldHotspot.SetActive(true);
                scanCursor.SetActive(true);
                SetHighlightTarget(scanStageA != null ? scanStageA : probe);
                break;
            case DemonstrationStage.Results:
                SetHighlightTarget(null);
                break;
        }
    }

    private void ResetStageVisuals()
    {
        if (incidentPath != null) incidentPath.gameObject.SetActive(false);
        if (scatteredPath != null) scatteredPath.gameObject.SetActive(false);
        if (afmPath != null) afmPath.gameObject.SetActive(false);
        if (beamMarker != null) beamMarker.SetActive(false);
        if (nearFieldHotspot != null) nearFieldHotspot.SetActive(false);
        if (sampleTile != null) sampleTile.SetActive(false);
        if (scanCursor != null) scanCursor.SetActive(false);
        SetHighlightTarget(null);
    }

    private void UpdateAnimation(float deltaTime, float time)
    {
        if (workflowPhase == WorkflowPhase.InstallingProbe)
        {
            UpdateProbeInstallation(deltaTime);
            return;
        }

        if (workflowPhase != WorkflowPhase.PrincipleTour)
        {
            return;
        }

        if (!stagePlaying || isComponentMode)
        {
            return;
        }

        if (currentStage >= DemonstrationStage.NearFieldCoupling &&
            currentStage <= DemonstrationStage.RasterScan)
        {
            AnimateProbe(time);
        }

        beamTravel = Mathf.Repeat(beamTravel + deltaTime * 0.30f, 1f);
        if (currentStage == DemonstrationStage.RasterScan)
        {
            scanProgress = Mathf.Clamp01(scanProgress + deltaTime / scanDuration);
            diagramGraphic.SetProgress(scanProgress);
            if (scanProgress >= 0.999f)
            {
                if (automaticallyAdvancePrinciples)
                {
                    EnterStage(DemonstrationStage.Results);
                }
                else
                {
                    stagePlaying = false;
                    statusText.text = "Scan complete: registered height and near-field maps are available.";
                    RefreshPlayButton();
                }
            }

            return;
        }

        stageElapsed += deltaTime;
        if (automaticallyAdvancePrinciples && currentStage != DemonstrationStage.Results &&
            stageElapsed >= automaticStageDuration)
        {
            Next();
        }
    }

    private void UpdateWorldVisuals(float time)
    {
        UpdateLinePositions();
        UpdateHighlightRing(time);

        Vector3 tip = GetProbeTipPosition();
        if (nearFieldHotspot != null && nearFieldHotspot.activeSelf)
        {
            nearFieldHotspot.transform.position = tip + Vector3.down * 0.0015f;
            float pulse = 0.75f + 0.25f * Mathf.Sin(time * 8f);
            nearFieldHotspot.transform.localScale = Vector3.one * (0.009f * pulse);
        }

        UpdateSampleAndScanCursor(tip);
        UpdateBeamMarker();
    }

    private void AnimateProbe(float time)
    {
        if (probe == null || probe.parent == null || !hasCachedProbePose)
        {
            return;
        }

        float offset = Mathf.Sin(time * probeFrequency * Mathf.PI * 2f) * exaggeratedProbeAmplitude;
        Vector3 parentLocalOffset = probe.parent.InverseTransformVector(Vector3.up * offset);
        probe.localPosition = cachedProbeLocalPosition + parentLocalOffset;
    }

    private void UpdateSampleAndScanCursor(Vector3 tip)
    {
        if (sampleTile == null)
        {
            return;
        }

        Vector3 samplePosition = tip + Vector3.down * 0.006f;
        sampleTile.transform.position = samplePosition;
        sampleTile.transform.rotation = Quaternion.identity;

        if (scanCursor == null || !scanCursor.activeSelf)
        {
            return;
        }

        const int rows = 9;
        float rowValue = scanProgress * rows;
        int row = Mathf.Clamp(Mathf.FloorToInt(rowValue), 0, rows - 1);
        float along = Mathf.Repeat(rowValue, 1f);
        if (row % 2 == 1)
        {
            along = 1f - along;
        }

        float x = Mathf.Lerp(-0.022f, 0.022f, along);
        float z = Mathf.Lerp(-0.022f, 0.022f, row / (float)(rows - 1));
        scanCursor.transform.position = samplePosition + new Vector3(x, 0.002f, z);
    }

    private void UpdateLinePositions()
    {
        Vector3 emitter = GetVisualCenter(emitterAntenna);
        Vector3 receiver = GetVisualCenter(receiverAntenna);
        Vector3 tip = GetProbeTipPosition();
        Vector3 mirrorA = GetVisualCenter(fastMirrorA);
        Vector3 mirrorB = GetVisualCenter(fastMirrorB);
        Vector3 parabola = GetVisualCenter(parabolicMirror);

        if (incidentPath != null)
        {
            if (currentStage == DemonstrationStage.ThzGeneration)
            {
                SetLine(incidentPath, emitter, mirrorA != Vector3.zero ? mirrorA : tip);
            }
            else
            {
                SetLine(incidentPath, emitter,
                    mirrorA != Vector3.zero ? mirrorA : Vector3.Lerp(emitter, tip, 0.35f),
                    parabola != Vector3.zero ? parabola : Vector3.Lerp(emitter, tip, 0.70f), tip);
            }
        }

        if (scatteredPath != null)
        {
            SetLine(scatteredPath, tip,
                mirrorB != Vector3.zero ? mirrorB : Vector3.Lerp(tip, receiver, 0.55f), receiver);
        }

        if (afmPath != null)
        {
            SetLine(afmPath, GetVisualCenter(afmLaser), tip, GetVisualCenter(quadrantDetector));
        }
    }

    private void UpdateBeamMarker()
    {
        if (beamMarker == null || !beamMarker.activeSelf)
        {
            return;
        }

        LineRenderer activeLine = afmPath != null && afmPath.gameObject.activeSelf
            ? afmPath
            : scatteredPath != null && scatteredPath.gameObject.activeSelf
                ? scatteredPath
                : incidentPath;
        if (activeLine == null || activeLine.positionCount < 2)
        {
            return;
        }

        beamMarker.transform.position = GetPointAlongLine(activeLine, beamTravel);
        float size = activeLine == afmPath ? 0.006f : 0.010f;
        beamMarker.transform.localScale = Vector3.one * size;
        MeshRenderer renderer = beamMarker.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = activeLine == afmPath ? afmMaterial :
                activeLine == scatteredPath ? scatteredMaterial : incidentMaterial;
        }
    }

    private void UpdateHighlightRing(float time)
    {
        if (highlightRing == null || !highlightRing.gameObject.activeSelf)
        {
            return;
        }

        Transform target = highlightRing.transform.parent;
        if (target == null || !TryGetBounds(target, out Bounds bounds))
        {
            return;
        }

        Camera camera = Camera.main;
        Vector3 right = camera != null ? camera.transform.right : Vector3.right;
        Vector3 up = camera != null ? camera.transform.up : Vector3.up;
        float radius = Mathf.Max(0.015f, Mathf.Max(bounds.extents.x, Mathf.Max(bounds.extents.y, bounds.extents.z)) * 1.25f);
        radius = Mathf.Min(radius, 0.12f);
        radius *= 1f + Mathf.Sin(time * 4f) * 0.04f;
        const int segments = 48;
        highlightRing.positionCount = segments;
        for (int i = 0; i < segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;
            highlightRing.SetPosition(i, bounds.center + (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * radius);
        }
    }

    private void SetHighlightTarget(Transform target)
    {
        if (highlightRing == null)
        {
            return;
        }

        if (target == null)
        {
            highlightRing.transform.SetParent(runtimeVisualRoot != null ? runtimeVisualRoot.transform : transform, true);
            highlightRing.gameObject.SetActive(false);
            return;
        }

        highlightRing.transform.SetParent(target, true);
        highlightRing.gameObject.SetActive(true);
    }

    private void HandleKeyboardAndMouse()
    {
        if (VRMicroscope.Assistant.AssistantChatPanel.BlocksGameplay) return;
        if (workflowPhase == WorkflowPhase.ProbeSelection)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) SelectProbeOption(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SelectProbeOption(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SelectProbeOption(2);
            if (Input.GetKeyDown(KeyCode.E)) InstallSelectedProbe();
        }
        else if (workflowPhase == WorkflowPhase.ReadyToStart && Input.GetKeyDown(KeyCode.Space))
        {
            ActivateSystem();
            return;
        }

        if (workflowPhase != WorkflowPhase.PrincipleTour)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) ExitDemonstration();
            return;
        }

        if (Input.GetKeyDown(KeyCode.RightArrow)) Next();
        if (Input.GetKeyDown(KeyCode.LeftArrow)) Previous();
        if (Input.GetKeyDown(KeyCode.Space)) TogglePlayPause();
        if (Input.GetKeyDown(KeyCode.R)) ReplayCurrent();
        if (Input.GetKeyDown(KeyCode.Escape)) ExitDemonstration();

        if (!Input.GetMouseButtonDown(0) ||
            (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
        {
            return;
        }

        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f, ~0, QueryTriggerInteraction.Collide);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            SNOMComponentProxy proxy = hits[i].collider.GetComponent<SNOMComponentProxy>();
            if (proxy != null && proxy.Controller == this)
            {
                ShowComponent(proxy.ComponentIndex);
                return;
            }
        }
    }

    private void BuildStageContent()
    {
        stages = new[]
        {
            Stage("THz s-SNOM System Overview",
                "This system combines THz time-domain spectroscopy with an oscillating AFM probe to map local surface response point by point.",
                "Output: topography + near-field amplitude + phase + local spectrum",
                SNOMDemonstrationGraphic.DiagramMode.Overview),
            Stage("Broadband THz Pulse Generation",
                "Ultrashort laser pulses excite a photoconductive emitter. A receiving antenna samples the returning THz electric field.",
                "E(t)  --Fourier transform-->  E(f)",
                SNOMDemonstrationGraphic.DiagramMode.TimeDomain),
            Stage("Beam Steering and Focusing",
                "Mirrors guide and focus the THz beam onto the tip. The sharp metal apex confines the interaction to a nanoscale region.",
                "Orange path = schematic incident THz path",
                SNOMDemonstrationGraphic.DiagramMode.BeamFocus),
            Stage("Identify the Near-Field Probe",
                "The metal tip probes local surface properties. Tip radius strongly influences lateral resolution.",
                "Resolution is governed primarily by the tip radius",
                SNOMDemonstrationGraphic.DiagramMode.Probe),
            Stage("AFM Distance Feedback",
                "A laser reflected from the cantilever reaches a quadrant detector. Feedback controls the tip-sample distance.",
                "deflection -> feedback error -> height correction",
                SNOMDemonstrationGraphic.DiagramMode.AfmFeedback),
            Stage("Tapping and Near-Field Coupling",
                "The oscillating metal tip concentrates the electric field near the surface. The displayed motion is exaggerated for clarity.",
                "z(t) = z0 + A cos(Omega t)",
                SNOMDemonstrationGraphic.DiagramMode.NearField),
            Stage("Weak Scattering over Background",
                "The detector receives both local tip scattering and far-field background. Signal processing separates the near-field contribution.",
                "measured scattering = background + near field",
                SNOMDemonstrationGraphic.DiagramMode.Background),
            Stage("Harmonic Demodulation",
                "Demodulation at higher harmonics of the tapping frequency suppresses background and isolates the local response.",
                "near-field channel: s2 at 2 Omega or s3 at 3 Omega",
                SNOMDemonstrationGraphic.DiagramMode.Harmonics),
            Stage("Raster Scan",
                "The system scans successive rows, recording surface height and near-field response at each position.",
                "one position -> one measurement -> one image pixel",
                SNOMDemonstrationGraphic.DiagramMode.Scan),
            Stage("Correlated Results and Local Spectrum",
                "Topography shows surface height. Near-field amplitude and phase reveal local optical properties; spectra show their frequency dependence.",
                "E(t) -> amplitude(f) + phase(f)",
                SNOMDemonstrationGraphic.DiagramMode.Results)
        };
    }

    private static StageContent Stage(string title, string explanation, string formula,
        SNOMDemonstrationGraphic.DiagramMode diagram)
    {
        return new StageContent
        {
            title = title,
            explanation = explanation,
            formula = formula,
            diagram = diagram
        };
    }

    private void BuildComponentCatalog()
    {
        components.Clear();
        AddComponent(emitterAntenna, "Photoconductive Antenna A",
            "Converts ultrashort optical pulses into broadband THz radiation. Assigned as the emitter in this demonstration.");
        AddComponent(receiverAntenna, "Photoconductive Antenna B",
            "Samples the returning THz electric field. Assigned as the receiver in this demonstration.");
        AddComponent(fastMirrorA, "Fast Steering Mirror",
            "Redirects the THz beam along the optical path.");
        AddComponent(parabolicMirror, "Parabolic THz Optic",
            "Focuses incident THz radiation or collects scattered radiation near the probe.");
        AddComponent(probe, "AFM Near-Field Probe",
            "Concentrates the field at its apex and probes local surface properties during tapping.");
        AddComponent(afmLaser, "AFM Readout Laser",
            "Illuminates the cantilever to measure deflection for AFM distance feedback.");
        AddComponent(quadrantDetector, "Quadrant Detector",
            "Measures displacement of the reflected laser spot and provides the AFM feedback signal.");
        AddComponent(scanStageA, "Scan Stage",
            "Controls relative tip-sample position for surface mapping.");
        AddComponent(preamplifier, "Head Preamplifier",
            "Amplifies weak electrical signals before further processing.");
        AddComponent(optionalDetector, "Optional Detector Module",
            "Optional detection module. Its specific role is not assigned in this demonstration.");
        AddComponent(optionalConnector, "Optional Connector",
            "Mechanical or electrical interface for the optional detector module.");
    }

    private void AddComponent(Transform target, string label, string description)
    {
        if (target == null)
        {
            return;
        }

        components.Add(new ComponentEntry
        {
            target = target,
            displayName = label,
            description = description
        });
    }

    private void ResolveComponentReferences()
    {
        List<Transform> antennas = FindAllDescendants(snomRoot, "光导天线");
        if (emitterAntenna == null && antennas.Count > 0) emitterAntenna = antennas[0];
        if (receiverAntenna == null && antennas.Count > 1) receiverAntenna = antennas[1];
        fastMirrorA = Resolve(fastMirrorA, "超快反射镜");
        fastMirrorB = Resolve(fastMirrorB, "超快反射镜.001");
        parabolicMirror = ResolveContains(parabolicMirror, "40立方体抛物镜安装20220601");
        afmHead = Resolve(afmHead, "触针固定块");
        probe = Resolve(probe, "探针");
        probeMount = Resolve(probeMount, "针尖座");
        probePositioningBlock = Resolve(probePositioningBlock, "MX-SNOM_AFM-TZJ-02探针定位块");
        afmLaser = Resolve(afmLaser, "激光器");
        quadrantDetector = Resolve(quadrantDetector, "四象限");
        scanStageA = Resolve(scanStageA, "滑台");
        scanStageB = Resolve(scanStageB, "滑台.001");
        preamplifier = Resolve(preamplifier, "Hea_preAmp_v2");
        optionalDetector = Resolve(optionalDetector, "Detector");
        optionalConnector = Resolve(optionalConnector, "Connector");

        if (probe != null)
        {
            cachedProbeLocalPosition = probe.localPosition;
            hasCachedProbePose = true;
        }
    }

    private void BuildRuntimeVisuals()
    {
        runtimeVisualRoot = new GameObject("SNOM Runtime Visuals");
        runtimeVisualRoot.transform.SetParent(transform, false);
        runtimeVisualRoot.transform.position = Vector3.zero;
        runtimeVisualRoot.transform.rotation = Quaternion.identity;
        runtimeVisualRoot.transform.localScale = Vector3.one;

        incidentMaterial = CreateRuntimeMaterial(IncidentColor);
        scatteredMaterial = CreateRuntimeMaterial(ScatteredColor);
        afmMaterial = CreateRuntimeMaterial(AfmColor);
        highlightMaterial = CreateRuntimeMaterial(AccentColor);
        darkMaterial = CreateRuntimeMaterial(new Color(0.025f, 0.08f, 0.12f, 1f));

        incidentPath = CreateLine("Incident THz Path", incidentMaterial, 0.004f);
        scatteredPath = CreateLine("Scattered THz Path", scatteredMaterial, 0.003f);
        afmPath = CreateLine("AFM Readout Path", afmMaterial, 0.002f);
        highlightRing = CreateLine("Component Highlight", highlightMaterial, 0.0025f);
        highlightRing.loop = true;

        beamMarker = CreateSphere("Travelling Signal", incidentMaterial);
        nearFieldHotspot = CreateSphere("Near-Field Hotspot", incidentMaterial);
        sampleTile = CreateCube("Runtime Sample Schematic", darkMaterial, new Vector3(0.05f, 0.001f, 0.05f));
        scanCursor = CreateSphere("Scan Cursor", scatteredMaterial);
        ResetStageVisuals();
        runtimeVisualRoot.SetActive(false);
    }

    private void BuildProbeOptions()
    {
        probeOptions.Clear();
        probeOptions.Add(new ProbeOption
        {
            displayName = "Fine Metallic Probe",
            shortLabel = "1  Fine | 3x detail",
            resolutionLabel = "Small tip radius | highest relative spatial detail",
            description = "Small-radius tip for fine surface detail. Requires careful distance control."
        });
        probeOptions.Add(new ProbeOption
        {
            displayName = "Standard Metallic Probe",
            shortLabel = "2  Standard | 2x",
            resolutionLabel = "Medium tip radius | balanced detail and stability",
            description = "General-purpose tip for balanced spatial detail and stable scanning."
        });
        probeOptions.Add(new ProbeOption
        {
            displayName = "Robust Metallic Probe",
            shortLabel = "3  Robust | 1x",
            resolutionLabel = "Larger tip radius | robust wide-area scanning",
            description = "Larger-radius tip for robust scanning, with reduced spatial detail."
        });

        installedProbeRenderers = probe != null ? probe.GetComponentsInChildren<Renderer>(true) : Array.Empty<Renderer>();
        installedProbeRendererStates = new bool[installedProbeRenderers.Length];
        for (int i = 0; i < installedProbeRenderers.Length; i++)
        {
            installedProbeRendererStates[i] = installedProbeRenderers[i] != null && installedProbeRenderers[i].enabled;
        }

        if (probe == null)
        {
            Debug.LogWarning("[SNOMDemonstration] The installed probe asset could not be resolved; probe selection will remain schematic.", this);
            return;
        }

        for (int i = 0; i < probeOptions.Count; i++)
        {
            ProbeOption option = probeOptions[i];
            GameObject preview = Instantiate(probe.gameObject, runtimeVisualRoot.transform);
            preview.name = $"Probe Choice Preview {i + 1}";
            PrepareProbePreview(preview);
            option.preview = preview;
            option.previewScale = 0.86f + i * 0.14f;
            preview.SetActive(false);
        }
    }

    private static void PrepareProbePreview(GameObject preview)
    {
        Collider[] colliders = preview.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
            Destroy(colliders[i]);
        }

        Rigidbody[] rigidbodies = preview.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rigidbodies.Length; i++)
        {
            rigidbodies[i].isKinematic = true;
            Destroy(rigidbodies[i]);
        }

        MonoBehaviour[] behaviours = preview.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            behaviours[i].enabled = false;
        }
    }

    private void BeginProbeSelection()
    {
        workflowPhase = WorkflowPhase.ProbeSelection;
        selectedProbeIndex = -1;
        installedProbeIndex = -1;
        RestoreProbePose();
        SetInstalledProbeVisible(false);
        SetOwnedVisualsActive(true);
        ResetStageVisuals();
        PositionProbeChoiceVisuals();
        SetProbePreviewsActive(true);
        SetTourControlsVisible(false);
        SetWorkflowControlsVisible(true);
        ApplyWorkflowLayout();
        SetHeaderStatus("SELECT PROBE", AccentColor);
        stageNumberText.text = "OPERATION 1 OF 2";
        titleText.text = "Select a Near-Field Probe";
        explanationText.text = "Choose a probe for the required surface detail. The 3x, 2x and 1x labels indicate relative detail, not optical magnification.";
        formulaText.text = "smaller tip radius -> finer lateral resolution";
        statusText.text = "Select 1 / 2 / 3, then press Install Probe (keyboard: 1-3 and E).";
        diagramGraphic.SetMode(SNOMDemonstrationGraphic.DiagramMode.Probe);
        diagramGraphic.SetProgress(0f);
        SetStageProgress(0.15f);
        RefreshProbeOptionButtons();
        RefreshWorkflowAction("Install Probe", false);
        SetHighlightTarget(probeMount != null ? probeMount : probe);
    }

    public void SelectProbeOption(int index)
    {
        if (workflowPhase != WorkflowPhase.ProbeSelection || index < 0 || index >= probeOptions.Count)
        {
            return;
        }

        selectedProbeIndex = index;
        ProbeOption option = probeOptions[index];
        titleText.text = option.displayName;
        explanationText.text = option.description;
        formulaText.text = option.resolutionLabel;
        statusText.text = "Probe selected. Press Install Probe to place it on the existing SNOM probe mount.";
        SetHeaderStatus("PROBE SELECTED", AccentColor);
        RefreshProbeOptionButtons();
        RefreshWorkflowAction("Install Selected Probe", true);
        SetProbePreviewEmphasis(index);
        DebugLog($"Probe option selected: {option.displayName}.");
    }

    public void InstallSelectedProbe()
    {
        if (workflowPhase != WorkflowPhase.ProbeSelection || selectedProbeIndex < 0 ||
            selectedProbeIndex >= probeOptions.Count)
        {
            return;
        }

        ProbeOption option = probeOptions[selectedProbeIndex];
        installedProbeIndex = selectedProbeIndex;
        workflowPhase = WorkflowPhase.InstallingProbe;
        SetHeaderStatus("INSTALLING PROBE", IncidentColor);
        probeInstallationProgress = 0f;
        if (option.preview != null)
        {
            installingProbeStartPosition = option.preview.transform.position;
            installingProbeStartRotation = option.preview.transform.rotation;
            installingProbeStartScale = option.preview.transform.localScale;
        }

        for (int i = 0; i < probeOptions.Count; i++)
        {
            if (probeOptions[i].preview != null)
            {
                probeOptions[i].preview.SetActive(i == selectedProbeIndex);
            }

            if (probeOptions[i].button != null)
            {
                probeOptions[i].button.interactable = false;
            }
        }

        SetHighlightTarget(probeMount != null ? probeMount : probe);
        stageNumberText.text = "OPERATION 1 OF 2";
        titleText.text = $"Installing {option.displayName}";
        explanationText.text = "Only the runtime preview moves toward the fixed mount. The authored SNOM root remains unchanged.";
        formulaText.text = "preview motion only | fixed instrument transform";
        statusText.text = "Installing the selected probe on the fixed probe mount...";
        RefreshWorkflowAction("Installing...", false);
        RefreshWorkflowPhaseVisuals();
    }

    private void UpdateProbeInstallation(float deltaTime)
    {
        if (installedProbeIndex < 0 || installedProbeIndex >= probeOptions.Count)
        {
            CompleteProbeInstallation();
            return;
        }

        ProbeOption option = probeOptions[installedProbeIndex];
        probeInstallationProgress = Mathf.Clamp01(
            probeInstallationProgress + deltaTime / Mathf.Max(0.25f, probeInstallationDuration));
        float eased = Mathf.SmoothStep(0f, 1f, probeInstallationProgress);
        if (option.preview != null && probe != null)
        {
            option.preview.transform.position = Vector3.Lerp(installingProbeStartPosition, probe.position, eased);
            option.preview.transform.rotation = Quaternion.Slerp(installingProbeStartRotation, probe.rotation, eased);
            option.preview.transform.localScale = Vector3.Lerp(installingProbeStartScale,
                installingProbeStartScale * 0.35f, eased);
        }

        SetStageProgress(Mathf.Lerp(0.2f, 0.48f, eased));
        statusText.text = $"Installing selected probe... {Mathf.RoundToInt(eased * 100f)}%";
        if (probeInstallationProgress >= 0.999f)
        {
            CompleteProbeInstallation();
        }
    }

    private void CompleteProbeInstallation()
    {
        workflowPhase = WorkflowPhase.ReadyToStart;
        SetProbePreviewsActive(false);
        SetInstalledProbeVisible(true);
        ProbeOption option = installedProbeIndex >= 0 && installedProbeIndex < probeOptions.Count
            ? probeOptions[installedProbeIndex]
            : null;
        ApplyWorkflowLayout();
        SetHeaderStatus("READY TO START", ReadyColor);
        stageNumberText.text = "OPERATION 2 OF 2";
        titleText.text = "Probe Installed";
        explanationText.text = option == null
            ? "The probe is installed. Start the system to begin the automatic measurement sequence."
            : $"{option.displayName} is installed. Start the system to begin the automatic measurement sequence.";
        formulaText.text = "operator workflow: select probe -> start system";
        statusText.text = "Press Start System (keyboard: Space). Remaining stages explain automatic internal processing.";
        SetStageProgress(0.5f);
        RefreshWorkflowAction("Start System", true);
        RefreshWorkflowPhaseVisuals();
        SetHighlightTarget(probe);
        DebugLog("Probe installation completed; system start is now enabled.");
    }

    public void ActivateSystem()
    {
        if (workflowPhase != WorkflowPhase.ReadyToStart)
        {
            return;
        }

        activationPulseUntil = Time.unscaledTime + 1.4f;
        SetProbePreviewsActive(false);
        SetWorkflowControlsVisible(false);
        SetTourControlsVisible(true);
        SetProxyObjectsActive(true);
        EnterStage(DemonstrationStage.ThzGeneration);
        DebugLog("SNOM system activated; automatic principle sequence started.");
    }

    private void PositionProbeChoiceVisuals()
    {
        Vector3 mount = GetVisualCenter(probeMount != null ? probeMount : probe);
        Camera viewer = Camera.main;
        Vector3 right = viewer != null ? viewer.transform.right : Vector3.right;
        Vector3 forward = viewer != null ? viewer.transform.forward : Vector3.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        forward.Normalize();
        Vector3 center = mount - forward * 0.08f + Vector3.up * 0.09f;

        for (int i = 0; i < probeOptions.Count; i++)
        {
            ProbeOption option = probeOptions[i];
            if (option.preview == null) continue;
            option.preview.SetActive(true);
            option.preview.transform.position = center + right * ((i - 1) * 0.065f);
            option.preview.transform.rotation = probe != null ? probe.rotation : Quaternion.identity;
            if (TryGetBounds(option.preview.transform, out Bounds bounds))
            {
                float largest = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
                if (largest > 0.0001f)
                {
                    option.preview.transform.localScale *= 0.045f / largest;
                }
            }

            option.preview.transform.localScale *= option.previewScale;
            option.previewPosition = option.preview.transform.position;
            option.previewRotation = option.preview.transform.rotation;
            option.previewLocalScale = option.preview.transform.localScale;
        }
    }

    private void SetProbePreviewEmphasis(int selectedIndex)
    {
        for (int i = 0; i < probeOptions.Count; i++)
        {
            ProbeOption option = probeOptions[i];
            if (option.preview == null) continue;
            option.preview.transform.position = option.previewPosition + (i == selectedIndex ? Vector3.up * 0.012f : Vector3.zero);
            option.preview.transform.rotation = option.previewRotation;
            option.preview.transform.localScale = option.previewLocalScale * (i == selectedIndex ? 1.18f : 0.92f);
        }
    }

    private void SetProbePreviewsActive(bool active)
    {
        for (int i = 0; i < probeOptions.Count; i++)
        {
            if (probeOptions[i].preview != null)
            {
                probeOptions[i].preview.SetActive(active);
            }
        }
    }

    private void SetInstalledProbeVisible(bool visible)
    {
        if (installedProbeRenderers == null) return;
        for (int i = 0; i < installedProbeRenderers.Length; i++)
        {
            if (installedProbeRenderers[i] != null)
            {
                installedProbeRenderers[i].enabled = visible &&
                    (installedProbeRendererStates == null || i >= installedProbeRendererStates.Length || installedProbeRendererStates[i]);
            }
        }
    }

    private void RestoreInstalledProbeVisibility()
    {
        if (installedProbeRenderers == null || installedProbeRendererStates == null) return;
        for (int i = 0; i < installedProbeRenderers.Length && i < installedProbeRendererStates.Length; i++)
        {
            if (installedProbeRenderers[i] != null)
            {
                installedProbeRenderers[i].enabled = installedProbeRendererStates[i];
            }
        }
    }

    private static int GetPrincipleStageIndex(DemonstrationStage stage)
    {
        for (int i = 0; i < PrincipleStages.Length; i++)
        {
            if (PrincipleStages[i] == stage) return i;
        }

        return 0;
    }

    private void BuildSelectionProxies()
    {
        for (int i = 0; i < components.Count; i++)
        {
            ComponentEntry entry = components[i];
            if (!TryGetBounds(entry.target, out Bounds bounds))
            {
                continue;
            }

            GameObject proxy = new GameObject($"Selection Proxy - {entry.displayName}");
            proxy.transform.SetParent(runtimeVisualRoot.transform, false);
            proxy.transform.position = bounds.center;
            proxy.transform.rotation = Quaternion.identity;
            proxy.transform.localScale = new Vector3(
                Mathf.Clamp(bounds.size.x * 1.20f, 0.025f, 0.22f),
                Mathf.Clamp(bounds.size.y * 1.20f, 0.025f, 0.22f),
                Mathf.Clamp(bounds.size.z * 1.20f, 0.025f, 0.22f));

            BoxCollider collider = proxy.AddComponent<BoxCollider>();
            collider.size = Vector3.one;
            collider.isTrigger = true;
            SNOMComponentProxy componentProxy = proxy.AddComponent<SNOMComponentProxy>();
            componentProxy.Configure(this, i);
            XRSimpleInteractable interactable = proxy.AddComponent<XRSimpleInteractable>();
            int capturedIndex = i;
            interactable.selectEntered.AddListener(_ => ShowComponent(capturedIndex));
            entry.proxy = proxy;
            proxy.SetActive(false);
        }
    }

    private void BuildEntryInteractionProxy()
    {
        if (entryInteractionProxy != null || !TryGetBounds(snomRoot, out Bounds bounds))
        {
            return;
        }

        entryInteractionProxy = new GameObject("SNOM Entry Interaction Proxy");
        entryInteractionProxy.transform.SetParent(transform, false);
        entryInteractionProxy.transform.position = bounds.center;
        entryInteractionProxy.transform.rotation = Quaternion.identity;
        entryInteractionProxy.transform.localScale = new Vector3(
            Mathf.Max(0.15f, bounds.size.x * 1.04f),
            Mathf.Max(0.15f, bounds.size.y * 1.04f),
            Mathf.Max(0.15f, bounds.size.z * 1.04f));

        BoxCollider collider = entryInteractionProxy.AddComponent<BoxCollider>();
        collider.size = Vector3.one;
        collider.isTrigger = true;

        SNOMEntryInteractionProxy proxy = entryInteractionProxy.AddComponent<SNOMEntryInteractionProxy>();
        proxy.Configure(this);
        XRSimpleInteractable interactable = entryInteractionProxy.AddComponent<XRSimpleInteractable>();
        interactable.selectEntered.AddListener(_ => RevealLauncherFromInteraction());
    }

    private void BuildProximityHighlight()
    {
        // Cache only the installed model, before any tutorial overlays are created.
        proximityMeshes = snomRoot.GetComponentsInChildren<MeshFilter>(true);
        proximityRenderers = new Renderer[proximityMeshes.Length];
        for (int i = 0; i < proximityMeshes.Length; i++)
            proximityRenderers[i] = proximityMeshes[i].GetComponent<MeshRenderer>();
        TryGetBounds(snomRoot, out proximityBounds);

        Material template = Resources.Load<Material>("SNOMProximityHighlight");
        if (template == null)
        {
            Debug.LogWarning("[SNOMDemonstration] Missing SNOMProximityHighlight material.", this);
            return;
        }

        proximityMaterial = new Material(template) { name = "SNOM Proximity Highlight (Runtime)" };
        proximityMaterial.SetColor("_HighlightColor", proximityHighlightColor);
        proximityMaterial.SetFloat("_Intensity", proximityHighlightIntensity);
        proximityMaterial.SetFloat("_boolean", 0f);
    }

    private void UpdateProximityHighlight()
    {
        if (!setupComplete || snomRoot == null) return;
        if (!snomRoot.gameObject.activeInHierarchy)
        {
            SetPlayerInInteractionRange(false);
            return;
        }

        if (Time.unscaledTime >= nextProximityCheck)
        {
            nextProximityCheck = Time.unscaledTime + 0.1f;
            proximityViewer = Camera.main;
            if (proximityViewer == null || !proximityViewer.isActiveAndEnabled)
            {
                SetPlayerInInteractionRange(false);
                return;
            }

            // Allow head height above the instrument; distance is measured from its footprint.
            Bounds reach = proximityBounds;
            reach.Expand(new Vector3(0f, 1.6f, 0f));
            float threshold = proximityHighlightDistance +
                              (isPlayerInInteractionRange ? proximityExitHysteresis : 0f);
            SetPlayerInInteractionRange(
                reach.SqrDistance(proximityViewer.transform.position) <= threshold * threshold);
        }

        bool roaming = Interactor.Instance == null ||
                       Interactor.Instance.CurrentState == Interactor.GameState.Roaming;
        bool activationPulse = Time.unscaledTime < activationPulseUntil;
        proximityHighlighted = (isPlayerInInteractionRange && !isRunning && roaming) || activationPulse;
        if (!proximityHighlighted || proximityMaterial == null || proximityViewer == null ||
            !proximityViewer.isActiveAndEnabled) return;
        for (int i = 0; i < proximityMeshes.Length; i++)
        {
            MeshFilter filter = proximityMeshes[i];
            Renderer source = proximityRenderers[i];
            if (filter == null || source == null || !source.enabled || source.forceRenderingOff ||
                !source.gameObject.activeInHierarchy || filter.sharedMesh == null) continue;

            Mesh mesh = filter.sharedMesh;
            for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
            {
                Graphics.DrawMesh(mesh, filter.transform.localToWorldMatrix, proximityMaterial,
                    source.gameObject.layer, proximityViewer, subMesh, null,
                    UnityEngine.Rendering.ShadowCastingMode.Off, false, null,
                    UnityEngine.Rendering.LightProbeUsage.Off);
            }
        }
    }

    private void SetPlayerInInteractionRange(bool isInRange)
    {
        if (isPlayerInInteractionRange == isInRange) return;

        isPlayerInInteractionRange = isInRange;
        if (isInRange)
        {
            DebugLog("Player entered the SNOM interaction range.");
            return;
        }

        proximityHighlighted = false;
        bool hadVisibleInterface = canvasRoot != null && canvasRoot.activeSelf;
        if (isRunning || hadVisibleInterface)
        {
            ResetDemonstrationState();
            DebugLog("Player left the SNOM interaction range; tutorial UI and state were reset.");
        }
    }

    private void BuildUserInterface()
    {
        EnsureEventSystem();
        XRUIInputModule xrInputModule = FindObjectOfType<XRUIInputModule>();
        if (xrInputModule != null)
        {
            xrInputModule.enableMouseInput = true;
        }

        canvasRoot = new GameObject("SNOM Demonstration UI", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(TrackedDeviceGraphicRaycaster));
        // Keep a screen-space canvas at scene root. A regular Transform parent can
        // offset normalized anchors differently between desktop and XR cameras.
        Canvas canvas = canvasRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 940;
        CanvasScaler scaler = canvasRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        launcherRoot = CreatePanel(canvasRoot.transform, "SNOM Launcher",
            new Vector2(0.73f, 0.075f), new Vector2(0.975f, 0.245f), PanelColor);
        AddBorder(launcherRoot, BorderColor, new Vector2(2f, -2f));
        CreateText(launcherRoot.transform, "Launcher Eyebrow", "DEVICE AVAILABLE",
            new Vector2(0.075f, 0.73f), new Vector2(0.92f, 0.90f), 13f,
            AccentColor, FontStyles.Bold);
        CreateText(launcherRoot.transform, "Launcher Title", "Select the SNOM",
            new Vector2(0.075f, 0.48f), new Vector2(0.92f, 0.73f), 25f,
            Color.white, FontStyles.Bold);
        CreateText(launcherRoot.transform, "Launcher Subtitle",
            "Two operator actions: install a probe, then start the system.",
            new Vector2(0.075f, 0.31f), new Vector2(0.92f, 0.48f), 14f,
            MutedTextColor, FontStyles.Normal);
        Button launcher = CreateButton(launcherRoot.transform, "Start Tour", "Begin Operation",
            new Vector2(0.075f, 0.07f), new Vector2(0.92f, 0.27f), ActiveButtonColor, 17f, out _);
        AddBorder(launcher.gameObject, AccentColor, Vector2.zero);
        launcher.onClick.AddListener(BeginTour);

        panelRoot = CreatePanel(canvasRoot.transform, "SNOM Explanation Panel",
            Vector2.zero, Vector2.one, Color.clear);
        panelRoot.GetComponent<Image>().raycastTarget = false;

        topBarRoot = CreatePanel(panelRoot.transform, "SNOM Status Bar",
            new Vector2(0.02f, 0.92f), new Vector2(0.98f, 0.985f), PanelColor);
        topBarRoot.GetComponent<Image>().raycastTarget = false;
        CreateText(topBarRoot.transform, "Brand", "THz s-SNOM",
            new Vector2(0.02f, 0.16f), new Vector2(0.35f, 0.84f), 24f,
            Color.white, FontStyles.Bold);
        headerStatusDot = CreateImage(topBarRoot.transform, "Status Dot",
            new Vector2(0.78f, 0.41f), new Vector2(0.788f, 0.59f), AccentColor);
        headerStatusDot.raycastTarget = false;
        headerStatusText = CreateText(topBarRoot.transform, "Device Status", "SELECT PROBE",
            new Vector2(0.80f, 0.20f), new Vector2(0.975f, 0.80f), 14f,
            MutedTextColor, FontStyles.Bold);
        headerStatusText.alignment = TextAlignmentOptions.MidlineRight;

        contentCardRoot = CreatePanel(panelRoot.transform, "Context Card",
            new Vector2(0.70f, 0.08f), new Vector2(0.98f, 0.90f), PanelColor);
        AddBorder(contentCardRoot, BorderColor, new Vector2(-2f, 0f));
        Image accent = CreateImage(contentCardRoot.transform, "Accent",
            new Vector2(0f, 0f), new Vector2(0.012f, 1f), AccentColor);
        accent.raycastTarget = false;

        stageNumberText = CreateText(contentCardRoot.transform, "Stage Number", "OPERATION 1 OF 2",
            new Vector2(0.06f, 0.93f), new Vector2(0.94f, 0.975f), 15f, AccentColor, FontStyles.Bold);
        Image progressTrack = CreateImage(contentCardRoot.transform, "Stage Progress Track",
            new Vector2(0.06f, 0.905f), new Vector2(0.94f, 0.914f), SurfaceColor);
        progressTrack.raycastTarget = false;
        stageProgressFill = CreateImage(progressTrack.transform, "Fill", Vector2.zero, Vector2.one, AccentColor);
        stageProgressFill.raycastTarget = false;
        stageProgressFill.type = Image.Type.Filled;
        stageProgressFill.fillMethod = Image.FillMethod.Horizontal;
        stageProgressFill.fillOrigin = 0;
        titleText = CreateText(contentCardRoot.transform, "Title", "THz s-SNOM",
            new Vector2(0.06f, 0.82f), new Vector2(0.94f, 0.90f), 29f, Color.white, FontStyles.Bold);
        explanationText = CreateText(contentCardRoot.transform, "Explanation", string.Empty,
            new Vector2(0.06f, 0.67f), new Vector2(0.94f, 0.81f), 20f,
            new Color(0.88f, 0.93f, 0.97f, 1f), FontStyles.Normal);
        explanationText.alignment = TextAlignmentOptions.TopLeft;
        formulaText = CreateText(contentCardRoot.transform, "Formula", string.Empty,
            new Vector2(0.06f, 0.60f), new Vector2(0.94f, 0.66f), 16f,
            AccentColor, FontStyles.Italic);
        statusText = CreateText(contentCardRoot.transform, "Status", string.Empty,
            new Vector2(0.06f, 0.54f), new Vector2(0.94f, 0.59f), 14f,
            MutedTextColor, FontStyles.Normal);

        diagramCardRoot = CreatePanel(panelRoot.transform, "Principle Diagram Card",
            new Vector2(0.03f, 0.62f), new Vector2(0.25f, 0.89f), PanelColor);
        AddBorder(diagramCardRoot, BorderColor, Vector2.zero);
        CreateText(diagramCardRoot.transform, "Diagram Label", "PRINCIPLE VIEW",
            new Vector2(0.06f, 0.88f), new Vector2(0.94f, 0.97f), 12f,
            MutedTextColor, FontStyles.Bold);
        diagramGraphic = CreateDiagram(diagramCardRoot.transform,
            new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.86f));

        BuildSurfaceObservation();
        tourControlsRoot = new GameObject("Principle Tour Controls", typeof(RectTransform));
        tourControlsRoot.transform.SetParent(contentCardRoot.transform, false);
        ConfigureRect(tourControlsRoot.GetComponent<RectTransform>(),
            new Vector2(0.67f, 0.06f), new Vector2(0.98f, 0.88f));
        Button previous = CreateButton(tourControlsRoot.transform, "Previous", "Previous",
            new Vector2(0f, 0.54f), new Vector2(0.19f, 1f), InactiveButtonColor, 14f, out _);
        Button play = CreateButton(tourControlsRoot.transform, "Play", "Pause",
            new Vector2(0.205f, 0.54f), new Vector2(0.395f, 1f), ActiveButtonColor, 14f, out playButtonText);
        Button replay = CreateButton(tourControlsRoot.transform, "Replay", "Replay",
            new Vector2(0.41f, 0.54f), new Vector2(0.60f, 1f), InactiveButtonColor, 14f, out _);
        Button next = CreateButton(tourControlsRoot.transform, "Next", "Next",
            new Vector2(0.615f, 0.54f), new Vector2(0.805f, 1f), ActiveButtonColor, 14f, out _);
        Button exit = CreateButton(tourControlsRoot.transform, "Exit", "Exit",
            new Vector2(0.82f, 0.54f), new Vector2(1f, 1f), new Color(0.55f, 0.10f, 0.10f, 1f), 14f, out _);
        Button componentsButton = CreateButton(tourControlsRoot.transform, "Components", "Components",
            new Vector2(0f, 0f), new Vector2(0.60f, 0.42f), new Color(0.06f, 0.36f, 0.42f, 1f), 14f,
            out componentButtonText);

        tourControlObjects.Add(previous.gameObject);
        tourControlObjects.Add(play.gameObject);
        tourControlObjects.Add(replay.gameObject);
        tourControlObjects.Add(next.gameObject);
        tourControlObjects.Add(exit.gameObject);
        tourControlObjects.Add(componentsButton.gameObject);

        probeChoiceControlsRoot = new GameObject("Probe Workflow Controls", typeof(RectTransform));
        probeChoiceControlsRoot.transform.SetParent(contentCardRoot.transform, false);
        ConfigureRect(probeChoiceControlsRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        for (int i = 0; i < probeOptions.Count; i++)
        {
            int capturedIndex = i;
            float minY = 0.40f - i * 0.13f;
            Button choice = CreateButton(probeChoiceControlsRoot.transform, $"Probe Option {i + 1}",
                probeOptions[i].shortLabel, new Vector2(0.06f, minY), new Vector2(0.94f, minY + 0.11f),
                InactiveButtonColor, 15f, out _);
            AddBorder(choice.gameObject, BorderColor, Vector2.zero);
            choice.onClick.AddListener(() => SelectProbeOption(capturedIndex));
            probeOptions[i].button = choice;
            probeOptions[i].buttonImage = choice.targetGraphic as Image;
        }

        workflowActionButton = CreateButton(probeChoiceControlsRoot.transform, "Workflow Action",
            "Install Probe", new Vector2(0.06f, 0.055f), new Vector2(0.72f, 0.125f),
            ActiveButtonColor, 17f, out workflowActionText);
        AddBorder(workflowActionButton.gameObject, AccentColor, Vector2.zero);
        workflowActionImage = workflowActionButton.targetGraphic as Image;
        workflowActionButton.onClick.AddListener(HandleWorkflowAction);
        Button workflowExit = CreateButton(probeChoiceControlsRoot.transform, "Workflow Exit", "Exit",
            new Vector2(0.74f, 0.055f), new Vector2(0.94f, 0.125f),
            new Color(0.70f, 0.14f, 0.12f, 1f), 16f, out _);
        workflowExit.onClick.AddListener(ExitDemonstration);
        Button changeProbe = CreateButton(probeChoiceControlsRoot.transform, "Change Probe", "Change Probe",
            new Vector2(0.06f, 0.15f), new Vector2(0.94f, 0.215f),
            InactiveButtonColor, 15f, out _);
        changeProbe.onClick.AddListener(BeginProbeSelection);
        workflowChangeProbeButton = changeProbe.gameObject;
        workflowChangeProbeButton.SetActive(false);

        previous.onClick.AddListener(Previous);
        play.onClick.AddListener(TogglePlayPause);
        replay.onClick.AddListener(ReplayCurrent);
        next.onClick.AddListener(Next);
        exit.onClick.AddListener(ExitDemonstration);
        componentsButton.onClick.AddListener(ToggleComponentMode);
        probeChoiceControlsRoot.SetActive(false);
        tourControlsRoot.SetActive(false);
        diagramCardRoot.SetActive(false);
        panelRoot.SetActive(false);
        launcherRoot.SetActive(false);
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null || FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        eventSystem.transform.position = Vector3.zero;
    }

    private void ShowLauncher()
    {
        if (isRunning)
        {
            return;
        }

        if (canvasRoot != null) canvasRoot.SetActive(true);
        if (launcherRoot != null) launcherRoot.SetActive(true);
        if (panelRoot != null) panelRoot.SetActive(false);
        SetProxyObjectsActive(false);
    }

    private void HideAllInterface()
    {
        if (launcherRoot != null) launcherRoot.SetActive(false);
        if (panelRoot != null) panelRoot.SetActive(false);
        if (canvasRoot != null) canvasRoot.SetActive(false);
    }

    private void HandleWorkflowAction()
    {
        if (workflowPhase == WorkflowPhase.ProbeSelection)
        {
            InstallSelectedProbe();
        }
        else if (workflowPhase == WorkflowPhase.ReadyToStart)
        {
            ActivateSystem();
        }
    }

    private void SetWorkflowControlsVisible(bool visible)
    {
        if (probeChoiceControlsRoot != null)
        {
            probeChoiceControlsRoot.SetActive(visible);
            if (visible)
            {
                RefreshWorkflowPhaseVisuals();
            }
        }
    }

    private void SetTourControlsVisible(bool visible)
    {
        if (tourControlsRoot != null)
        {
            tourControlsRoot.SetActive(visible);
        }

        for (int i = 0; i < tourControlObjects.Count; i++)
        {
            if (tourControlObjects[i] != null)
            {
                tourControlObjects[i].SetActive(visible);
            }
        }
    }

    private void BuildSurfaceObservation()
    {
        surfaceCardRoot = CreatePanel(panelRoot.transform, "Surface Observation",
            new Vector2(0.73f, 0.57f), new Vector2(0.97f, 0.89f), PanelColor);
        AddBorder(surfaceCardRoot, BorderColor, Vector2.zero);
        CreateText(surfaceCardRoot.transform, "Heading", "PROBE / SURFACE OBSERVATION",
            new Vector2(0.05f, 0.87f), new Vector2(0.95f, 0.98f), 15f, AccentColor, FontStyles.Bold);

        GameObject imageArea = new GameObject("Surface Image", typeof(RectTransform), typeof(RawImage),
            typeof(AspectRatioFitter));
        imageArea.transform.SetParent(surfaceCardRoot.transform, false);
        ConfigureRect(imageArea.GetComponent<RectTransform>(),
            new Vector2(0.05f, 0.22f), new Vector2(0.95f, 0.85f));
        RawImage image = imageArea.GetComponent<RawImage>();
        image.texture = Resources.Load<Texture2D>("SNOM/GlassSurface");
        image.raycastTarget = false;
        AspectRatioFitter fitter = imageArea.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = image.texture != null ? (float)image.texture.width / image.texture.height : 1.29f;
        // Fit within a dedicated holder so aspect correction cannot expand over captions.
        GameObject holder = new GameObject("Surface Image Bounds", typeof(RectTransform));
        holder.transform.SetParent(surfaceCardRoot.transform, false);
        ConfigureRect(holder.GetComponent<RectTransform>(),
            new Vector2(0.05f, 0.22f), new Vector2(0.95f, 0.85f));
        imageArea.transform.SetParent(holder.transform, false);
        Image cover = CreateImage(imageArea.transform, "Unscanned Area", Vector2.zero, Vector2.one, PanelColor);
        cover.raycastTarget = false;
        surfaceCover = cover.rectTransform;
        cover.color = new Color(0.015f, 0.045f, 0.06f, 1f);
        surfaceResponse = CreateImage(imageArea.transform, "Local Response (Schematic)",
            Vector2.zero, Vector2.zero, AccentColor);
        surfaceResponse.raycastTarget = false;
        surfaceScanLine = CreateImage(imageArea.transform, "Scan Line", Vector2.zero,
            Vector2.zero, AccentColor).rectTransform;
        surfaceScanLine.GetComponent<Image>().raycastTarget = false;
        surfaceProbeMarker = CreateImage(imageArea.transform, "Probe Position (Schematic)",
            Vector2.zero, Vector2.zero, Color.white).rectTransform;
        surfaceProbeMarker.GetComponent<Image>().raycastTarget = false;
        surfaceStatusText = CreateText(surfaceCardRoot.transform, "Scan Status", "Reference preview",
            new Vector2(0.05f, 0.11f), new Vector2(0.95f, 0.21f), 14f, Color.white, FontStyles.Normal);
        CreateText(surfaceCardRoot.transform, "Attribution", "AFM glass topography example | Chych / Materialscientist | PD",
            new Vector2(0.05f, 0.01f), new Vector2(0.95f, 0.10f), 11f, MutedTextColor, FontStyles.Normal);
        if (image.texture == null)
            Debug.LogWarning("[SNOMDemonstration] Missing SNOM/GlassSurface observation image.", this);
        surfaceCardRoot.SetActive(false);
    }

    private void UpdateSurfaceObservation()
    {
        if (surfaceCardRoot == null || !surfaceCardRoot.activeInHierarchy) return;
        bool scanning = currentStage == DemonstrationStage.RasterScan;
        float revealed = scanning ? Mathf.Clamp01(scanProgress) : 1f;
        surfaceCover.gameObject.SetActive(revealed < 1f);
        surfaceCover.anchorMax = new Vector2(1f, 1f - revealed);
        bool complete = currentStage == DemonstrationStage.Results;
        surfaceProbeMarker.gameObject.SetActive(!complete);
        surfaceResponse.gameObject.SetActive(!complete);
        surfaceScanLine.gameObject.SetActive(scanning && revealed < 1f);
        // Use the stage clock, which stops on pause and resets on replay.
        float pulse = 0.5f + 0.5f * Mathf.Sin(stageElapsed * probeFrequency * Mathf.PI * 2f);
        Vector2 position = new Vector2(0.5f, 0.5f);
        string activity;
        if (scanning)
        {
            const int rows = 9; // Match the world-space scan cursor.
            float rowValue = Mathf.Min(revealed, 0.99999f) * rows;
            int row = Mathf.FloorToInt(rowValue);
            float along = Mathf.Repeat(rowValue, 1f);
            position = new Vector2(row % 2 == 0 ? along : 1f - along, 1f - revealed);
            ConfigureRect(surfaceScanLine, new Vector2(0f, position.y), new Vector2(1f, position.y));
            surfaceScanLine.sizeDelta = new Vector2(0f, 2f);
            activity = $"Teaching scan: {Mathf.RoundToInt(revealed * 100f)}%";
        }
        else if (complete) activity = "Scan complete | Surface topography example";
        else if (currentStage == DemonstrationStage.AfmFeedback || currentStage == DemonstrationStage.NearFieldCoupling)
        {
            position.y += 0.07f * pulse;
            activity = "Schematic: probe tapping / local response";
        }
        else
        {
            position.x = Mathf.Lerp(0.15f, 0.85f, beamTravel);
            activity = "Schematic: " + stages[(int)currentStage].title;
        }
        ConfigureRect(surfaceProbeMarker, position, position);
        surfaceProbeMarker.sizeDelta = new Vector2(5f, 22f);
        ConfigureRect(surfaceResponse.rectTransform, position, position);
        surfaceResponse.rectTransform.sizeDelta = Vector2.one * (18f + pulse * 20f);
        Color responseColor = currentStage == DemonstrationStage.AfmFeedback ? AfmColor : AccentColor;
        responseColor.a = 0.18f + 0.22f * pulse;
        surfaceResponse.color = responseColor;
        surfaceStatusText.text = activity + (!stagePlaying && !complete ? " | Paused" : "");
    }

    private void ApplyWorkflowLayout()
    {
        if (contentCardRoot == null)
        {
            return;
        }

        ConfigureRect(contentCardRoot.GetComponent<RectTransform>(),
            new Vector2(0.70f, 0.08f), new Vector2(0.98f, 0.90f));
        if (diagramCardRoot != null) diagramCardRoot.SetActive(false);
        if (surfaceCardRoot != null) surfaceCardRoot.SetActive(false);

        ConfigureRect(stageNumberText.rectTransform,
            new Vector2(0.06f, 0.93f), new Vector2(0.94f, 0.975f));
        ConfigureProgressTrack(new Vector2(0.06f, 0.905f), new Vector2(0.94f, 0.914f));
        ConfigureRect(titleText.rectTransform,
            new Vector2(0.06f, 0.82f), new Vector2(0.94f, 0.90f));
        ConfigureRect(explanationText.rectTransform,
            new Vector2(0.06f, 0.67f), new Vector2(0.94f, 0.81f));
        ConfigureRect(formulaText.rectTransform,
            new Vector2(0.06f, 0.60f), new Vector2(0.94f, 0.66f));
        ConfigureRect(statusText.rectTransform,
            new Vector2(0.06f, 0.54f), new Vector2(0.94f, 0.59f));
        explanationText.fontSizeMax = 22f;
        titleText.fontSizeMax = 29f;
        RefreshWorkflowPhaseVisuals();
    }

    private void ApplyPrincipleLayout()
    {
        if (contentCardRoot == null)
        {
            return;
        }

        ConfigureRect(contentCardRoot.GetComponent<RectTransform>(),
            new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.235f));
        if (diagramCardRoot != null) diagramCardRoot.SetActive(true);
        if (surfaceCardRoot != null) surfaceCardRoot.SetActive(true);

        ConfigureRect(stageNumberText.rectTransform,
            new Vector2(0.02f, 0.68f), new Vector2(0.20f, 0.90f));
        ConfigureProgressTrack(new Vector2(0.02f, 0.92f), new Vector2(0.98f, 0.955f));
        ConfigureRect(titleText.rectTransform,
            new Vector2(0.02f, 0.12f), new Vector2(0.20f, 0.66f));
        ConfigureRect(explanationText.rectTransform,
            new Vector2(0.22f, 0.40f), new Vector2(0.64f, 0.88f));
        ConfigureRect(formulaText.rectTransform,
            new Vector2(0.22f, 0.22f), new Vector2(0.64f, 0.39f));
        ConfigureRect(statusText.rectTransform,
            new Vector2(0.22f, 0.04f), new Vector2(0.64f, 0.21f));
        ConfigureRect(tourControlsRoot.GetComponent<RectTransform>(),
            new Vector2(0.67f, 0.08f), new Vector2(0.98f, 0.88f));
        explanationText.fontSizeMax = 22f;
        titleText.fontSizeMax = 24f;
    }

    private void ApplyComponentLayout()
    {
        if (contentCardRoot == null)
        {
            return;
        }

        ConfigureRect(contentCardRoot.GetComponent<RectTransform>(),
            new Vector2(0.70f, 0.08f), new Vector2(0.98f, 0.90f));
        if (diagramCardRoot != null) diagramCardRoot.SetActive(true);
        if (surfaceCardRoot != null) surfaceCardRoot.SetActive(false);

        ConfigureRect(stageNumberText.rectTransform,
            new Vector2(0.06f, 0.91f), new Vector2(0.94f, 0.97f));
        ConfigureProgressTrack(new Vector2(0.06f, 0.885f), new Vector2(0.94f, 0.895f));
        ConfigureRect(titleText.rectTransform,
            new Vector2(0.06f, 0.79f), new Vector2(0.94f, 0.88f));
        ConfigureRect(explanationText.rectTransform,
            new Vector2(0.06f, 0.49f), new Vector2(0.94f, 0.77f));
        ConfigureRect(formulaText.rectTransform,
            new Vector2(0.06f, 0.40f), new Vector2(0.94f, 0.48f));
        ConfigureRect(statusText.rectTransform,
            new Vector2(0.06f, 0.32f), new Vector2(0.94f, 0.39f));
        ConfigureRect(tourControlsRoot.GetComponent<RectTransform>(),
            new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.26f));
        explanationText.fontSizeMax = 22f;
        titleText.fontSizeMax = 27f;
    }

    private void ConfigureProgressTrack(Vector2 anchorMin, Vector2 anchorMax)
    {
        if (stageProgressFill == null || stageProgressFill.transform.parent == null)
        {
            return;
        }

        RectTransform track = stageProgressFill.transform.parent.GetComponent<RectTransform>();
        if (track != null)
        {
            ConfigureRect(track, anchorMin, anchorMax);
        }
    }

    private void RefreshWorkflowPhaseVisuals()
    {
        bool selecting = workflowPhase == WorkflowPhase.ProbeSelection;
        for (int i = 0; i < probeOptions.Count; i++)
        {
            if (probeOptions[i].button != null)
            {
                probeOptions[i].button.gameObject.SetActive(selecting);
            }
        }

        if (workflowChangeProbeButton != null)
        {
            workflowChangeProbeButton.SetActive(workflowPhase == WorkflowPhase.ReadyToStart);
        }
    }

    private void SetHeaderStatus(string label, Color dotColor)
    {
        if (headerStatusText != null)
        {
            headerStatusText.text = label;
        }

        if (headerStatusDot != null)
        {
            headerStatusDot.color = dotColor;
        }
    }

    private void RefreshProbeOptionButtons()
    {
        for (int i = 0; i < probeOptions.Count; i++)
        {
            ProbeOption option = probeOptions[i];
            if (option.buttonImage != null)
            {
                option.buttonImage.color = i == selectedProbeIndex ? SelectedProbeColor : InactiveButtonColor;
            }

            if (option.button != null)
            {
                Outline outline = option.button.GetComponent<Outline>();
                if (outline != null)
                {
                    outline.effectColor = i == selectedProbeIndex ? AccentColor : BorderColor;
                    outline.effectDistance = i == selectedProbeIndex
                        ? new Vector2(2f, -2f)
                        : new Vector2(1f, -1f);
                }
            }

            if (option.button != null)
            {
                option.button.interactable = workflowPhase == WorkflowPhase.ProbeSelection;
            }
        }
    }

    private void RefreshWorkflowAction(string label, bool interactable)
    {
        if (workflowActionText != null)
        {
            workflowActionText.text = label;
        }

        if (workflowActionButton != null)
        {
            workflowActionButton.interactable = interactable;
        }

        if (workflowActionImage != null)
        {
            workflowActionImage.color = interactable ? ActiveButtonColor : InactiveButtonColor;
        }
    }

    private void SetEntryInteractionActive(bool active)
    {
        if (entryInteractionProxy != null)
        {
            entryInteractionProxy.SetActive(active);
        }
    }

    private bool RevealLauncherFromInteraction()
    {
        if (!setupComplete || !isPlayerInInteractionRange || isRunning)
        {
            return false;
        }

        ShowLauncher();
        DebugLog("SNOM selected; tour launcher revealed.");
        return true;
    }

    private bool TryRevealLauncher(Ray ray, float maxDistance)
    {
        if (!setupComplete || !isPlayerInInteractionRange || isRunning || entryInteractionProxy == null ||
            !entryInteractionProxy.activeInHierarchy)
        {
            return false;
        }

        RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < hits.Length; i++)
        {
            SNOMEntryInteractionProxy proxy = hits[i].collider.GetComponentInParent<SNOMEntryInteractionProxy>();
            if (proxy != null && proxy.Controller == this)
            {
                return RevealLauncherFromInteraction();
            }
        }

        return false;
    }

    private void RefreshPlayButton()
    {
        if (playButtonText == null)
        {
            return;
        }

        if (isComponentMode)
        {
            playButtonText.text = "Fixed";
        }
        else if (currentStage == DemonstrationStage.Results)
        {
            playButtonText.text = "Complete";
        }
        else if (currentStage == DemonstrationStage.RasterScan && scanProgress <= 0.001f && !stagePlaying)
        {
            playButtonText.text = "Start Scan";
        }
        else if (currentStage == DemonstrationStage.RasterScan && scanProgress >= 0.999f && !stagePlaying)
        {
            playButtonText.text = "Scan Again";
        }
        else
        {
            playButtonText.text = stagePlaying ? "Pause" : "Play";
        }
    }

    private void SetStageProgress(float normalized)
    {
        if (stageProgressFill != null)
        {
            stageProgressFill.fillAmount = Mathf.Clamp01(normalized);
        }
    }

    private string GetStageStatus(DemonstrationStage stage)
    {
        switch (stage)
        {
            case DemonstrationStage.BeamSteering:
                return "Schematic path: verify serialized optical anchors before treating it as exact alignment.";
            case DemonstrationStage.IdentifyProbe:
                return "PC: click the probe marker. XR: point and press the right trigger.";
            case DemonstrationStage.NearFieldCoupling:
                return "The visible tapping amplitude and hotspot size are intentionally exaggerated.";
            case DemonstrationStage.RasterScan:
                return "Press Start Scan. The runtime cursor moves; the fixed SNOM root does not.";
            default:
                return "Keyboard: Left/Right stage, Space play/pause, R replay, Esc exit.";
        }
    }

    private SNOMDemonstrationGraphic.DiagramMode GetComponentDiagram(Transform target)
    {
        if (target == probe) return SNOMDemonstrationGraphic.DiagramMode.Probe;
        if (target == afmLaser || target == quadrantDetector) return SNOMDemonstrationGraphic.DiagramMode.AfmFeedback;
        if (target == emitterAntenna || target == receiverAntenna) return SNOMDemonstrationGraphic.DiagramMode.TimeDomain;
        if (target == fastMirrorA || target == fastMirrorB || target == parabolicMirror)
            return SNOMDemonstrationGraphic.DiagramMode.BeamFocus;
        if (target == scanStageA || target == scanStageB) return SNOMDemonstrationGraphic.DiagramMode.Scan;
        return SNOMDemonstrationGraphic.DiagramMode.Overview;
    }

    private void SetProxyObjectsActive(bool active)
    {
        for (int i = 0; i < components.Count; i++)
        {
            if (components[i].proxy != null)
            {
                components[i].proxy.SetActive(active);
            }
        }
    }

    private void SetOwnedVisualsActive(bool active)
    {
        if (runtimeVisualRoot != null)
        {
            runtimeVisualRoot.SetActive(active);
        }
    }

    private void CacheFixedRootPose()
    {
        cachedRootParent = snomRoot.parent;
        cachedRootLocalPosition = snomRoot.localPosition;
        cachedRootLocalRotation = snomRoot.localRotation;
        cachedRootLocalScale = snomRoot.localScale;
    }

    private void ValidateFixedRootPose()
    {
        if (snomRoot == null)
        {
            return;
        }

        bool unchanged = snomRoot.parent == cachedRootParent &&
                         Vector3.SqrMagnitude(snomRoot.localPosition - cachedRootLocalPosition) < 0.00000001f &&
                         Quaternion.Angle(snomRoot.localRotation, cachedRootLocalRotation) < 0.001f &&
                         Vector3.SqrMagnitude(snomRoot.localScale - cachedRootLocalScale) < 0.00000001f;
        if (!unchanged && !warnedAboutRootPose)
        {
            warnedAboutRootPose = true;
            Debug.LogError("[SNOMDemonstration] The fixed SNOM root pose was changed by another system. " +
                           "This controller does not write to the root transform.", this);
        }
    }

    private void RestoreProbePose()
    {
        if (hasCachedProbePose && probe != null)
        {
            probe.localPosition = cachedProbeLocalPosition;
        }
    }

    private LineRenderer CreateLine(string objectName, Material material, float width)
    {
        GameObject lineObject = new GameObject(objectName, typeof(LineRenderer));
        lineObject.transform.SetParent(runtimeVisualRoot.transform, false);
        LineRenderer line = lineObject.GetComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.sharedMaterial = material;
        line.startColor = material.color;
        line.endColor = material.color;
        line.startWidth = width;
        line.endWidth = width * 0.75f;
        line.numCapVertices = 3;
        line.numCornerVertices = 3;
        line.positionCount = 0;
        return line;
    }

    private GameObject CreateSphere(string objectName, Material material)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = objectName;
        sphere.transform.SetParent(runtimeVisualRoot.transform, true);
        Collider collider = sphere.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        MeshRenderer renderer = sphere.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        sphere.transform.localScale = Vector3.one * 0.01f;
        return sphere;
    }

    private GameObject CreateCube(string objectName, Material material, Vector3 scale)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = objectName;
        cube.transform.SetParent(runtimeVisualRoot.transform, true);
        Collider collider = cube.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        cube.GetComponent<MeshRenderer>().sharedMaterial = material;
        cube.transform.localScale = scale;
        return cube;
    }

    private static Material CreateRuntimeMaterial(Color color)
    {
        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader)
        {
            color = color,
            hideFlags = HideFlags.DontSave
        };
        return material;
    }

    private static void DestroyOwnedMaterial(Material material)
    {
        if (material == null)
        {
            return;
        }

        if (Application.isPlaying) Destroy(material);
        else DestroyImmediate(material);
    }

    private static void SetLine(LineRenderer line, params Vector3[] positions)
    {
        if (line == null || positions == null)
        {
            return;
        }

        line.positionCount = positions.Length;
        line.SetPositions(positions);
    }

    private static Vector3 GetPointAlongLine(LineRenderer line, float normalized)
    {
        int count = line.positionCount;
        if (count == 0) return Vector3.zero;
        if (count == 1) return line.GetPosition(0);
        float segmentValue = Mathf.Clamp01(normalized) * (count - 1);
        int segment = Mathf.Min(count - 2, Mathf.FloorToInt(segmentValue));
        return Vector3.Lerp(line.GetPosition(segment), line.GetPosition(segment + 1), segmentValue - segment);
    }

    private Vector3 GetProbeTipPosition()
    {
        if (probe == null)
        {
            return snomRoot != null ? snomRoot.position : Vector3.zero;
        }

        if (TryGetBounds(probe, out Bounds bounds))
        {
            return new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        }

        return probe.position;
    }

    private static Vector3 GetVisualCenter(Transform target)
    {
        if (target == null)
        {
            return Vector3.zero;
        }

        return TryGetBounds(target, out Bounds bounds) ? bounds.center : target.position;
    }

    private static bool TryGetBounds(Transform target, out Bounds bounds)
    {
        bounds = default;
        if (target == null)
        {
            return false;
        }

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        bool found = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || renderer is ParticleSystemRenderer || renderer is LineRenderer)
            {
                continue;
            }

            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (!found)
        {
            bounds = new Bounds(target.position, Vector3.one * 0.025f);
        }

        return true;
    }

    private Transform Resolve(Transform current, string exactName)
    {
        return current != null ? current : FindDescendant(snomRoot, exactName, false);
    }

    private Transform ResolveContains(Transform current, string partialName)
    {
        return current != null ? current : FindDescendant(snomRoot, partialName, true);
    }

    private static Transform FindDescendant(Transform root, string name, bool contains)
    {
        if (root == null)
        {
            return null;
        }

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            string childName = children[i].name;
            if ((!contains && childName == name) || (contains && childName.Contains(name)))
            {
                return children[i];
            }
        }

        return null;
    }

    private static List<Transform> FindAllDescendants(Transform root, string baseName)
    {
        List<Transform> matches = new List<Transform>();
        if (root == null)
        {
            return matches;
        }

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == baseName || children[i].name.StartsWith(baseName + ".", StringComparison.Ordinal))
            {
                matches.Add(children[i]);
            }
        }

        matches.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return matches;
    }

    public static Transform FindSnomRoot()
    {
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform candidate = allTransforms[i];
            if (candidate != null && candidate.name == ModelRootName &&
                candidate.gameObject.scene.IsValid() && !candidate.gameObject.hideFlags.HasFlag(HideFlags.HideAndDontSave))
            {
                return candidate;
            }
        }

        return null;
    }

    private static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false);
        ConfigureRect(panel.GetComponent<RectTransform>(), anchorMin, anchorMax);
        panel.GetComponent<Image>().color = color;
        return panel;
    }

    private static void AddBorder(GameObject target, Color color, Vector2 distance)
    {
        if (target == null)
        {
            return;
        }

        Outline outline = target.GetComponent<Outline>();
        if (outline == null)
        {
            outline = target.AddComponent<Outline>();
        }

        outline.effectColor = color;
        outline.effectDistance = distance == Vector2.zero ? new Vector2(1f, -1f) : distance;
        outline.useGraphicAlpha = true;
    }

    private static Image CreateImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        ConfigureRect(imageObject.GetComponent<RectTransform>(), anchorMin, anchorMax);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string value,
        Vector2 anchorMin, Vector2 anchorMax, float fontSize, Color color, FontStyles style)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        ConfigureRect(textObject.GetComponent<RectTransform>(), anchorMin, anchorMax);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = ResolveInterfaceFont();
        text.text = value;
        fontSize = Mathf.Max(18f, fontSize);
        text.fontSize = fontSize;
        text.color = color;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Left;
        text.enableWordWrapping = true;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(16f, fontSize * 0.85f);
        text.fontSizeMax = fontSize;
        text.overflowMode = TextOverflowModes.Truncate;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateButton(Transform parent, string name, string label,
        Vector2 anchorMin, Vector2 anchorMax, Color color, float fontSize, out TextMeshProUGUI labelText)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        ConfigureRect(buttonObject.GetComponent<RectTransform>(), anchorMin, anchorMax);
        Image image = buttonObject.GetComponent<Image>();
        image.color = color;
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
        colors.pressedColor = new Color(0.72f, 0.82f, 0.92f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        labelText = CreateText(buttonObject.transform, "Label", label, Vector2.zero, Vector2.one,
            fontSize, Color.white, FontStyles.Bold);
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.enableWordWrapping = false;
        return button;
    }

    private static SNOMDemonstrationGraphic CreateDiagram(Transform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject diagram = new GameObject("Principle Diagram", typeof(RectTransform), typeof(CanvasRenderer),
            typeof(SNOMDemonstrationGraphic));
        diagram.transform.SetParent(parent, false);
        ConfigureRect(diagram.GetComponent<RectTransform>(), anchorMin, anchorMax);
        SNOMDemonstrationGraphic graphic = diagram.GetComponent<SNOMDemonstrationGraphic>();
        graphic.color = Color.white;
        graphic.raycastTarget = false;
        return graphic;
    }

    private static TMP_FontAsset ResolveInterfaceFont()
    {
        if (cachedInterfaceFont != null)
        {
            return cachedInterfaceFont;
        }

        // The project default references Inter, including its atlas material and fallbacks.
        cachedInterfaceFont = TMP_Settings.defaultFontAsset;
        return cachedInterfaceFont;
    }

    private static void ConfigureRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[SNOMDemonstration] {message}", this);
        }
    }
}

public sealed class SNOMComponentProxy : MonoBehaviour
{
    public SNOMDemonstrationController Controller { get; private set; }
    public int ComponentIndex { get; private set; }

    public void Configure(SNOMDemonstrationController controller, int componentIndex)
    {
        Controller = controller;
        ComponentIndex = componentIndex;
    }
}

public sealed class SNOMEntryInteractionProxy : MonoBehaviour
{
    public SNOMDemonstrationController Controller { get; private set; }

    public void Configure(SNOMDemonstrationController controller)
    {
        Controller = controller;
    }
}

public static class SNOMDemonstrationRuntimeBootstrap
{
    private static bool hooked;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (!hooked)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            hooked = true;
        }

        EnsureController();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureController();
    }

    private static void EnsureController()
    {
        Transform root = SNOMDemonstrationController.FindSnomRoot();
        if (root == null)
        {
            return;
        }

        SNOMDemonstrationController existing = UnityEngine.Object.FindObjectOfType<SNOMDemonstrationController>(true);
        if (existing != null)
        {
            existing.Configure(root);
            return;
        }

        GameObject system = new GameObject("SNOM_DemonstrationSystem (Runtime)");
        SNOMDemonstrationController controller = system.AddComponent<SNOMDemonstrationController>();
        controller.Configure(root);
    }
}
