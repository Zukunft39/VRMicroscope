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

    private const string ModelRootName = "TDs_edited_UnityVeryLowPoly";
    private static readonly Color PanelColor = new Color(0.025f, 0.055f, 0.09f, 0.94f);
    private static readonly Color AccentColor = new Color(0.04f, 0.66f, 0.86f, 1f);
    private static readonly Color ActiveButtonColor = new Color(0.03f, 0.36f, 0.74f, 1f);
    private static readonly Color InactiveButtonColor = new Color(0.18f, 0.27f, 0.38f, 1f);
    private static readonly Color IncidentColor = new Color(1f, 0.55f, 0.08f, 1f);
    private static readonly Color ScatteredColor = new Color(0.08f, 0.82f, 0.94f, 1f);
    private static readonly Color AfmColor = new Color(0.18f, 0.92f, 0.49f, 1f);
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
    [SerializeField] private bool enableDebugLogs;

    private readonly List<ComponentEntry> components = new List<ComponentEntry>();
    private StageContent[] stages;
    private DemonstrationStage currentStage;
    private int selectedComponentIndex;
    private bool isRunning;
    private bool isComponentMode;
    private bool stagePlaying = true;
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
    private TextMeshProUGUI stageNumberText;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI explanationText;
    private TextMeshProUGUI formulaText;
    private TextMeshProUGUI statusText;
    private TextMeshProUGUI playButtonText;
    private TextMeshProUGUI componentButtonText;
    private Image stageProgressFill;
    private SNOMDemonstrationGraphic diagramGraphic;

    public Transform SnomRoot => snomRoot;
    public DemonstrationStage CurrentStage => currentStage;
    public bool IsRunning => isRunning;

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
        if (controller == null || controller.isRunning || controller.entryInteractionProxy == null ||
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
                controller.RevealLauncherFromInteraction();
                return true;
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
        ValidateFixedRootPose();
    }

    private void LateUpdate()
    {
        if (!isRunning || !setupComplete)
        {
            return;
        }

        UpdateWorldVisuals(Time.unscaledTime);
    }

    private void OnDisable()
    {
        RestoreProbePose();
        SetOwnedVisualsActive(false);
        if (canvasRoot != null)
        {
            canvasRoot.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        RestoreProbePose();
        if (canvasRoot != null)
        {
            Destroy(canvasRoot);
        }

        DestroyOwnedMaterial(incidentMaterial);
        DestroyOwnedMaterial(scatteredMaterial);
        DestroyOwnedMaterial(afmMaterial);
        DestroyOwnedMaterial(highlightMaterial);
        DestroyOwnedMaterial(darkMaterial);
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
        BuildRuntimeVisuals();
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

        if (!setupComplete)
        {
            return;
        }

        isRunning = true;
        isComponentMode = false;
        stagePlaying = true;
        warnedAboutRootPose = false;
        canvasRoot.SetActive(true);
        launcherRoot.SetActive(false);
        panelRoot.SetActive(true);
        SetEntryInteractionActive(false);
        SetProxyObjectsActive(true);
        EnterStage(DemonstrationStage.Overview);
    }

    public void ExitDemonstration()
    {
        isRunning = false;
        isComponentMode = false;
        stagePlaying = false;
        scanProgress = 0f;
        RestoreProbePose();
        SetOwnedVisualsActive(false);
        SetProxyObjectsActive(false);
        HideAllInterface();
        SetEntryInteractionActive(true);

        ValidateFixedRootPose();
    }

    public void Next()
    {
        if (isComponentMode)
        {
            ShowComponent(selectedComponentIndex + 1);
            return;
        }

        int next = Mathf.Min((int)DemonstrationStage.Results, (int)currentStage + 1);
        EnterStage((DemonstrationStage)next);
    }

    public void Previous()
    {
        if (isComponentMode)
        {
            ShowComponent(selectedComponentIndex - 1);
            return;
        }

        int previous = Mathf.Max(0, (int)currentStage - 1);
        EnterStage((DemonstrationStage)previous);
    }

    public void ReplayCurrent()
    {
        if (isComponentMode)
        {
            ShowComponent(selectedComponentIndex);
            return;
        }

        EnterStage(currentStage);
    }

    public void TogglePlayPause()
    {
        stagePlaying = !stagePlaying;
        if (currentStage == DemonstrationStage.RasterScan && stagePlaying && scanProgress >= 0.999f)
        {
            scanProgress = 0f;
        }

        RefreshPlayButton();
    }

    public void ToggleComponentMode()
    {
        if (!isRunning)
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
        isComponentMode = false;
        stagePlaying = stage != DemonstrationStage.RasterScan;
        scanProgress = stage == DemonstrationStage.RasterScan ? 0f : 1f;
        beamTravel = 0f;

        StageContent content = stages[(int)stage];
        stageNumberText.text = $"STAGE {(int)stage + 1:00}/{stages.Length:00}";
        titleText.text = content.title;
        explanationText.text = content.explanation;
        formulaText.text = content.formula;
        diagramGraphic.SetMode(content.diagram);
        diagramGraphic.SetProgress(scanProgress);
        SetStageProgress(((int)stage + 1f) / stages.Length);
        componentButtonText.text = "Components";
        statusText.text = GetStageStatus(stage);
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
                stagePlaying = false;
                statusText.text = "Scan complete: registered height and near-field maps are available.";
                RefreshPlayButton();
            }
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
                "这套系统把太赫兹时域光谱与振动式 AFM 探针结合。它不是一次拍摄整张图，而是在扫描过程中逐点记录局部响应。",
                "Output: topography + near-field amplitude + phase + local spectrum",
                SNOMDemonstrationGraphic.DiagramMode.Overview),
            Stage("Broadband THz Pulse Generation",
                "飞秒激光激发发射光导天线，产生宽带太赫兹瞬态；接收光导天线采样返回电场。这里的光导天线与 AFM 检测激光承担不同任务。",
                "E(t)  --Fourier transform-->  E(f)",
                SNOMDemonstrationGraphic.DiagramMode.TimeDomain),
            Stage("Beam Steering and Focusing",
                "反射镜引导太赫兹脉冲，抛物面光学元件把它聚焦到金属针尖附近。远场焦点本身仍不是纳米尺度，真正的局域化由针尖完成。",
                "Orange path = schematic incident THz path",
                SNOMDemonstrationGraphic.DiagramMode.BeamFocus),
            Stage("Identify the Near-Field Probe",
                "金属探针是空间采样器。针尖半径而不是自由空间太赫兹波长，主要决定横向分辨率。真实探针保持原位，青色标记只是选择辅助。",
                "Resolution is governed primarily by the tip radius",
                SNOMDemonstrationGraphic.DiagramMode.Probe),
            Stage("AFM Distance Feedback",
                "绿色 AFM 激光从悬臂反射到四象限探测器。光点偏移形成反馈误差，使探针跟随表面形貌而不撞击样本。它不是太赫兹照明光路。",
                "deflection -> feedback error -> height correction",
                SNOMDemonstrationGraphic.DiagramMode.AfmFeedback),
            Stage("Tapping and Near-Field Coupling",
                "探针以频率 Omega 振动。靠近样本时，天线效应和避雷针效应把电场限制在针尖间隙，并与样本中的镜像偶极耦合。动画振幅经过夸张。",
                "z(t) = z0 + A cos(Omega t)",
                SNOMDemonstrationGraphic.DiagramMode.NearField),
            Stage("Weak Scattering over Background",
                "接收端同时得到针尖近场散射和更强的远场背景。近场信息可能仅占总散射的 10^-3 到 10^-4，因此原始强度不能直接当作近场图像。",
                "measured scattering = background + near field",
                SNOMDemonstrationGraphic.DiagramMode.Background),
            Stage("Harmonic Demodulation",
                "近场随针尖距离非线性变化，因此会出现在振动高次谐波中。读取 2 Omega 或 3 Omega 可以抑制远场背景，但不会凭空增加信号能量。",
                "near-field channel: s2 at 2 Omega or s3 at 3 Omega",
                SNOMDemonstrationGraphic.DiagramMode.Harmonics),
            Stage("Raster Scan",
                "系统沿蛇形路径逐点记录 AFM 高度和解调后的近场信号，并逐行构建配准图像。按 Play/Start Scan 开始演示。",
                "one position -> one measurement -> one image pixel",
                SNOMDemonstrationGraphic.DiagramMode.Scan),
            Stage("Correlated Results and Local Spectrum",
                "形貌图描述表面高度，近场幅值和相位描述局部电磁响应。选择像素还可以查看 E(t) 及其傅里叶频谱，从而区分形貌相近但材料不同的区域。",
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
            "当前默认作为 TDS 发射端：将超快光脉冲转换为宽带太赫兹瞬态。最终发射/接收方向仍应依据实际光路确认。 ");
        AddComponent(receiverAntenna, "Photoconductive Antenna B",
            "当前默认作为 TDS 接收端：采样返回的太赫兹电场。它与负责 AFM 反馈的四象限探测器不同。 ");
        AddComponent(fastMirrorA, "Fast Steering Mirror",
            "用于改变太赫兹传播方向并保持宽带脉冲对准。演示光线是低成本示意线，不替代实际光机校准。 ");
        AddComponent(parabolicMirror, "Parabolic THz Optic",
            "对宽带太赫兹波进行聚焦或收集，避免普通透镜可能产生的明显色差。纳米局域化最终发生在针尖间隙。 ");
        AddComponent(probe, "AFM Near-Field Probe",
            "金属针尖同时承担局域场增强和空间采样。探针振动使近场信息能够在高次谐波中与背景分离。 ");
        AddComponent(afmLaser, "AFM Readout Laser",
            "读取悬臂偏转并服务于距离反馈，不是太赫兹照明源。绿色路径只代表 AFM 读出链路。 ");
        AddComponent(quadrantDetector, "Quadrant Detector",
            "检测 AFM 激光光点的位置变化，并把上下/左右偏移转换为反馈误差信号。 ");
        AddComponent(scanStageA, "Scan Stage",
            "控制探针与样本的相对位置，以蛇形路径逐点构建形貌和近场图像。 ");
        AddComponent(preamplifier, "Head Preamplifier",
            "在解调之前对微弱电信号进行前置调理，减少后续传输和采集中的噪声影响。 ");
        AddComponent(optionalDetector, "Optional Detector Module",
            "该节点目前独立于 MX-Thz 主机构，硬件身份未完全确认，因此只作可选接收模块说明，不加入默认 TDS 链路。 ");
        AddComponent(optionalConnector, "Optional Connector",
            "与独立 Detector 相邻的连接或转接组件。在获得装配图之前不把它强行安装到主光路。 ");
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
            new Vector2(0.025f, 0.79f), new Vector2(0.285f, 0.93f), PanelColor);
        CreateText(launcherRoot.transform, "Launcher Title", "THz s-SNOM Explorer",
            new Vector2(0.08f, 0.61f), new Vector2(0.92f, 0.88f), 24f,
            Color.white, FontStyles.Bold);
        CreateText(launcherRoot.transform, "Launcher Subtitle",
            "Device selected. Open the guided component and principle tour.",
            new Vector2(0.08f, 0.38f), new Vector2(0.92f, 0.60f), 15f,
            new Color(0.72f, 0.82f, 0.89f, 1f), FontStyles.Normal);
        Button launcher = CreateButton(launcherRoot.transform, "Start Tour", "Start SNOM Tour",
            new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.34f), ActiveButtonColor, 18f, out _);
        launcher.onClick.AddListener(BeginTour);

        panelRoot = CreatePanel(canvasRoot.transform, "SNOM Explanation Panel",
            new Vector2(0.025f, 0.285f), new Vector2(0.35f, 0.955f), PanelColor);
        Image accent = CreateImage(panelRoot.transform, "Accent", new Vector2(0f, 0f), new Vector2(0.014f, 1f), AccentColor);
        accent.raycastTarget = false;

        stageNumberText = CreateText(panelRoot.transform, "Stage Number", "STAGE 01/10",
            new Vector2(0.06f, 0.94f), new Vector2(0.94f, 0.98f), 17f, AccentColor, FontStyles.Bold);
        Image progressTrack = CreateImage(panelRoot.transform, "Stage Progress Track",
            new Vector2(0.06f, 0.915f), new Vector2(0.94f, 0.925f), new Color(0.12f, 0.20f, 0.28f, 1f));
        progressTrack.raycastTarget = false;
        stageProgressFill = CreateImage(progressTrack.transform, "Fill", Vector2.zero, Vector2.one, AccentColor);
        stageProgressFill.raycastTarget = false;
        stageProgressFill.type = Image.Type.Filled;
        stageProgressFill.fillMethod = Image.FillMethod.Horizontal;
        stageProgressFill.fillOrigin = 0;
        titleText = CreateText(panelRoot.transform, "Title", "THz s-SNOM",
            new Vector2(0.06f, 0.835f), new Vector2(0.94f, 0.905f), 29f, Color.white, FontStyles.Bold);
        diagramGraphic = CreateDiagram(panelRoot.transform, new Vector2(0.06f, 0.50f), new Vector2(0.94f, 0.815f));
        explanationText = CreateText(panelRoot.transform, "Explanation", string.Empty,
            new Vector2(0.06f, 0.275f), new Vector2(0.94f, 0.475f), 22f, new Color(0.88f, 0.93f, 0.97f, 1f), FontStyles.Normal);
        explanationText.alignment = TextAlignmentOptions.TopLeft;
        formulaText = CreateText(panelRoot.transform, "Formula", string.Empty,
            new Vector2(0.06f, 0.205f), new Vector2(0.94f, 0.265f), 18f, new Color(0.47f, 0.86f, 0.96f, 1f), FontStyles.Italic);
        statusText = CreateText(panelRoot.transform, "Status", string.Empty,
            new Vector2(0.06f, 0.145f), new Vector2(0.94f, 0.198f), 15f, new Color(0.65f, 0.73f, 0.80f, 1f), FontStyles.Normal);

        Button previous = CreateButton(panelRoot.transform, "Previous", "Previous",
            new Vector2(0.06f, 0.065f), new Vector2(0.225f, 0.13f), InactiveButtonColor, 16f, out _);
        Button play = CreateButton(panelRoot.transform, "Play", "Pause",
            new Vector2(0.235f, 0.065f), new Vector2(0.40f, 0.13f), ActiveButtonColor, 16f, out playButtonText);
        Button replay = CreateButton(panelRoot.transform, "Replay", "Replay",
            new Vector2(0.41f, 0.065f), new Vector2(0.575f, 0.13f), InactiveButtonColor, 16f, out _);
        Button next = CreateButton(panelRoot.transform, "Next", "Next",
            new Vector2(0.585f, 0.065f), new Vector2(0.75f, 0.13f), ActiveButtonColor, 16f, out _);
        Button exit = CreateButton(panelRoot.transform, "Exit", "Exit",
            new Vector2(0.76f, 0.065f), new Vector2(0.94f, 0.13f), new Color(0.70f, 0.14f, 0.12f, 1f), 16f, out _);
        Button componentsButton = CreateButton(panelRoot.transform, "Components", "Components",
            new Vector2(0.06f, 0.012f), new Vector2(0.94f, 0.052f), new Color(0.10f, 0.43f, 0.48f, 1f), 15f, out componentButtonText);

        previous.onClick.AddListener(Previous);
        play.onClick.AddListener(TogglePlayPause);
        replay.onClick.AddListener(ReplayCurrent);
        next.onClick.AddListener(Next);
        exit.onClick.AddListener(ExitDemonstration);
        componentsButton.onClick.AddListener(ToggleComponentMode);
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

    private void SetEntryInteractionActive(bool active)
    {
        if (entryInteractionProxy != null)
        {
            entryInteractionProxy.SetActive(active);
        }
    }

    private void RevealLauncherFromInteraction()
    {
        if (!setupComplete || isRunning)
        {
            return;
        }

        ShowLauncher();
        DebugLog("SNOM selected; tour launcher revealed.");
    }

    private bool TryRevealLauncher(Ray ray, float maxDistance)
    {
        if (!setupComplete || isRunning || entryInteractionProxy == null ||
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
                RevealLauncherFromInteraction();
                return true;
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
        text.fontSize = fontSize;
        text.color = color;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Left;
        text.enableWordWrapping = true;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(11f, fontSize * 0.68f);
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

        TMP_FontAsset[] loadedFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        for (int i = 0; i < loadedFonts.Length; i++)
        {
            TMP_FontAsset candidate = loadedFonts[i];
            if (candidate != null && candidate.name.IndexOf("simfang", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                cachedInterfaceFont = candidate;
                return cachedInterfaceFont;
            }
        }

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
