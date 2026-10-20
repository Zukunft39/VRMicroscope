using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using VRMicroscope.Assistant;

// Editor-only snapshot replay. Never sets private controller fields or calls Guidance/Navigation.Apply.
public sealed class AssistantBenchmarkWindow : EditorWindow
{
    [Serializable] private sealed class Case
    {
        public string id, category, state, description, setup, expected, expected_target;
        public string[] expected_kinds, goal_actions, rubric;
        public bool has_navigation;
        public AssistantChatRequest payload;
    }
    [Serializable] private sealed class Suite
    {
        public string version, catalog_version, knowledge_version, provenance;
        public Case[] cases;
    }
    [Serializable] private sealed class Capture
    {
        public string case_id, suite_hash, captured_utc, scene, provenance;
        public AssistantChatRequest payload;
    }
    // Omit navigation completely for operation fixtures; Unity inline serialization
    // must not turn an absent navigation context into an invalid empty object.
    [Serializable] private sealed class OperationRequest
    {
        public string sessionId, requestId, question;
        public AssistantChatTurn[] history;
        public GuidanceSnapshot guidance;
    }
    [Serializable] private sealed class Health
    {
        public string mode, assistantModel, assistantKnowledgeVersion;
        public bool configured, benchmarkTracing;
    }
    [Serializable] private sealed class Trial
    {
        public string case_id, category, provenance;
        public int repetition;
        public string[] expected_kinds, goal_actions;
        public string expected_target;
        public AssistantChatRequest payload;
    }
    [Serializable] private sealed class Plan
    {
        public string run_id, suite_hash, suite_version, started_utc, endpoint;
        public int seed;
        public bool client_apply_executed;
        public Trial[] trials;
    }
    [Serializable] private sealed class Result
    {
        public string case_id, request_id, status, source, kind, action_id, answer, error_code, provenance;
        public int repetition;
        public double elapsed_ms;
        public bool response_contract_valid, expected_kind_ok;
        public string action_available, goal_corresponding, target_correct;
        public bool manual_review_pending = true, client_apply_executed;
        public AssistantChatResponse response;
    }

    private static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    private static string Folder => Path.Combine(Root, "docs/benchmarks/fixed_state_ai");
    private static string Runs => Path.Combine(Root, "BenchmarkRuns");
    private Suite suite;
    private GuidanceCatalog catalog;
    private string suiteHash, error = "", message = "Load the suite, start the gateway, then send one case or run the batch.";
    private string endpoint = "http://127.0.0.1:8765/assistant", lastReply = "", runFolder = "";
    private int selected, repetitions = 3, seed = 2027, nextTrial;
    private bool useCaptures, running, checkingHealth, disposed;
    private Vector2 scroll;
    private Plan plan;
    private UnityWebRequest request;
    private double requestStarted, nextSendAt;
    private Trial current;
    private readonly List<Result> results = new List<Result>();

    // Keep a flat entry as well as grouped entries: Unity versions/project menu
    // customizations can hide a newly created parent submenu until a refresh.
    [MenuItem("Tools/AI Benchmark", priority = 2100)]
    [MenuItem("Tools/VR Microscope/AI Benchmark", priority = 2100)]
    [MenuItem("Tools/Microscope/AI Benchmark", priority = 2100)]
    public static void Open() => GetWindow<AssistantBenchmarkWindow>("AI Benchmark");

    private void OnEnable()
    {
        disposed = false;
        minSize = new Vector2(560, 600);
        var settings = Resources.Load<LocalAssistantSettings>("LocalAssistantSettings");
        if (settings != null) endpoint = settings.chatEndpoint;
        LoadSuite();
        EditorApplication.update += Poll;
        AssemblyReloadEvents.beforeAssemblyReload += BeforeReload;
    }
    private void OnDisable()
    {
        disposed = true;
        Stop("window_closed");
        EditorApplication.update -= Poll;
        AssemblyReloadEvents.beforeAssemblyReload -= BeforeReload;
    }
    private void BeforeReload() => Stop("assembly_reload");

    private void LoadSuite()
    {
        try
        {
            string text = File.ReadAllText(Path.Combine(Folder, "editor_suite.json"));
            suiteHash = Hash(text);
            suite = JsonUtility.FromJson<Suite>(text);
            var asset = Resources.Load<TextAsset>("AssistantGuidanceActions");
            catalog = asset != null ? JsonUtility.FromJson<GuidanceCatalog>(asset.text) : null;
            if (suite?.cases == null || suite.cases.Length == 0 || catalog == null || suite.catalog_version != catalog.version)
                throw new InvalidDataException("Suite is missing or its action catalogue differs from this project.");
            selected = Mathf.Clamp(selected, 0, suite.cases.Length - 1);
            foreach (var item in suite.cases)
            {
                if (item.payload?.guidance == null || item.expected_kinds == null || item.goal_actions == null)
                    throw new InvalidDataException("Incomplete case: " + item.id);
                if (!item.has_navigation) item.payload.navigation = null;
            }
            error = "";
        }
        catch (Exception e) { suite = null; error = e.Message + " Run: python -B Tools/ai_tutor/prepare_fixed_state_benchmark.py"; }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Fixed-state AI benchmark", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Switch the state supplied to the assistant instantly. Replies use the same /assistant gateway and review rules as normal chat. The 3D scene is not switched, and replies are not applied as scene instructions or markers.", MessageType.Info);
        if (!string.IsNullOrEmpty(error)) EditorGUILayout.HelpBox(error, MessageType.Error);
        using (new EditorGUI.DisabledScope(running))
        {
            endpoint = EditorGUILayout.TextField("Assistant endpoint", endpoint);
            useCaptures = EditorGUILayout.Toggle("Use saved Unity captures", useCaptures);
            repetitions = EditorGUILayout.IntSlider("Repetitions per case", repetitions, 1, 10);
            seed = EditorGUILayout.IntField("Shuffle seed", seed);
            if (GUILayout.Button("Reload case suite")) LoadSuite();
        }
        if (suite == null) return;
        scroll = EditorGUILayout.BeginScrollView(scroll);
        using (new EditorGUI.DisabledScope(running))
        {
            selected = EditorGUILayout.Popup("Case / state", selected,
                suite.cases.Select(c => c.id + " | " + c.state + " | " + c.category).ToArray());
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Previous")) selected = (selected + suite.cases.Length - 1) % suite.cases.Length;
                if (GUILayout.Button("Next")) selected = (selected + 1) % suite.cases.Length;
            }
        }
        var item = suite.cases[selected];
        EditorGUILayout.LabelField("State", item.description, EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField("Setup", item.setup, EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField("Question", EditorStyles.boldLabel);
        EditorGUILayout.SelectableLabel(item.payload.question, EditorStyles.textArea, GUILayout.MinHeight(52));
        AssistantChatRequest preview = item.payload;
        if (useCaptures)
        {
            try { preview = ReadCapture(item); }
            catch (Exception e) { EditorGUILayout.HelpBox(e.Message, MessageType.Warning); }
        }
        EditorGUILayout.LabelField("A(s)", string.Join(", ", Actions(preview)), EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField("G(s,q) - local scoring only", string.Join(", ", item.goal_actions), EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField("Expected", item.expected, EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(running || !EditorApplication.isPlaying || AssistantChatPanel.BlocksGameplay))
            if (GUILayout.Button("Capture current Unity state for this case")) CaptureLive(item);
        EditorGUILayout.LabelField("Capture requires Play Mode with chat closed. Capture once, then replay it for every repetition.", EditorStyles.wordWrappedMiniLabel);
        using (new EditorGUI.DisabledScope(running))
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Send selected (1 request)")) Begin(new[] { item }, 1);
            if (GUILayout.Button("Run all (" + (suite.cases.Length * repetitions) + " requests)")) Begin(suite.cases, repetitions);
        }
        using (new EditorGUI.DisabledScope(!running))
            if (GUILayout.Button("Stop batch")) Stop("user_cancelled");
        EditorGUILayout.HelpBox(message, MessageType.None);
        if (running) EditorGUILayout.LabelField("Progress", results.Count + " / " + plan.trials.Length);
        if (!string.IsNullOrEmpty(lastReply))
        {
            EditorGUILayout.LabelField("Last gateway reply (manual review still required)", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(lastReply, EditorStyles.textArea, GUILayout.MinHeight(140));
        }
        if (!string.IsNullOrEmpty(runFolder) && GUILayout.Button("Open saved run folder")) EditorUtility.RevealInFinder(runFolder);
        EditorGUILayout.EndScrollView();
    }

    private static string[] Actions(AssistantChatRequest p)
    {
        return (p.guidance?.allowedActionIds ?? new string[0])
            .Concat((p.navigation?.targets ?? new NavigationTargetSnapshot[0]).Select(t => "highlight:" + t.id)).ToArray();
    }
    private static string CapturePath(Case item) => Path.Combine(Runs, "Captures", item.id + ".json");
    private void CaptureLive(Case item)
    {
        try
        {
            var owner = UnityEngine.Object.FindObjectOfType<LocalAssistantController>();
            if (owner?.Guidance == null || owner.Navigation == null) throw new InvalidOperationException("The live assistant is not ready.");
            var p = Clone(item.payload);
            p.navigation = owner.Navigation.Capture();
            p.guidance = owner.Guidance.Capture();
            CheckCaptureState(item, p);
            var capture = new Capture { case_id = item.id, suite_hash = suiteHash, captured_utc = DateTime.UtcNow.ToString("o"),
                scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path, provenance = "unity_capture_replay", payload = p };
            string path = CapturePath(item);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            if (File.Exists(path)) File.Copy(path, Path.ChangeExtension(path, "." + Guid.NewGuid().ToString("N") + ".backup.json"));
            File.WriteAllText(path, JsonUtility.ToJson(capture, true), new UTF8Encoding(false));
            message = item.id + " captured. The existing capture was archived if present. Review the full allowed set before collecting results.";
            error = "";
        }
        catch (Exception e) { error = e.Message; }
    }
    private void CheckCaptureState(Case item, AssistantChatRequest p)
    {
        var a = item.payload.guidance; var b = p.guidance;
        if (b == null || a.mode != b.mode || a.blocked != b.blocked || a.selectedPart != b.selectedPart ||
            a.partExperiment != b.partExperiment || a.snomPhase != b.snomPhase ||
            a.hasSample != b.hasSample || a.placedSample != b.placedSample ||
            a.selectedProbe != b.selectedProbe || a.installedProbe != b.installedProbe ||
            a.playing != b.playing || a.componentMode != b.componentMode ||
            (a.mode == "snom" && a.snomStage != b.snomStage) ||
            (a.mode == "spatial_frequency" && (a.frequencyProfile != b.frequencyProfile || a.illumination != b.illumination)) ||
            (a.mode == "na" && Mathf.Abs(a.na - b.na) > .001f))
            throw new InvalidOperationException("The scene does not match " + item.state + ". Reproduce its stated mode, phase, selection and parameters first.");
        if (b.catalogVersion != suite.catalog_version || !item.goal_actions.All(id => Actions(p).Contains(id)))
            throw new InvalidOperationException("The captured catalogue/allowed set does not support the proposed gold actions. Review this case first.");
        if (!Actions(p).OrderBy(id => id).SequenceEqual(Actions(item.payload).OrderBy(id => id)))
            throw new InvalidOperationException("The live allowed set differs from this case. Check controls, bindings, proximity and navigationEnabled against Setup. Review/rebuild the case labels before saving a different condition.");
    }
    private AssistantChatRequest ReadCapture(Case item)
    {
        string path = CapturePath(item);
        if (!File.Exists(path)) throw new InvalidDataException("No Unity capture for " + item.id + ". Capture it once, or turn off saved captures for fixture replay.");
        var saved = JsonUtility.FromJson<Capture>(File.ReadAllText(path));
        if (saved == null || saved.suite_hash != suiteHash || saved.case_id != item.id || saved.payload == null)
            throw new InvalidDataException("Saved capture has a different suite version: " + item.id);
        CheckCaptureState(item, saved.payload);
        var p = Clone(saved.payload);
        // Questions/history are immutable design data, not inherited from a previous conversation.
        p.question = item.payload.question; p.history = item.payload.history;
        return p;
    }
    private static AssistantChatRequest Clone(AssistantChatRequest p)
    {
        var result = JsonUtility.FromJson<AssistantChatRequest>(JsonUtility.ToJson(p));
        if (p.navigation == null) result.navigation = null;
        return result;
    }
    private static string RequestBody(AssistantChatRequest p)
    {
        if (p.navigation != null) return JsonUtility.ToJson(p);
        return JsonUtility.ToJson(new OperationRequest { sessionId = p.sessionId, requestId = p.requestId,
            question = p.question, history = p.history ?? new AssistantChatTurn[0], guidance = p.guidance });
    }
    private static string Hash(string text)
    {
        using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "").ToLowerInvariant();
    }

    private void Begin(Case[] cases, int count)
    {
        try
        {
            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) ||
                !(uri.Scheme == "https" || uri.Scheme == "http" && uri.IsLoopback))
                throw new InvalidOperationException("Use HTTPS or a loopback HTTP assistant gateway.");
            var trials = new List<Trial>();
            var rng = new System.Random(seed);
            for (int repetition = 1; repetition <= count; repetition++)
            {
                var shuffled = cases.ToArray();
                for (int i = shuffled.Length - 1; i > 0; i--)
                { int j = rng.Next(i + 1); var tmp = shuffled[i]; shuffled[i] = shuffled[j]; shuffled[j] = tmp; }
                foreach (var item in shuffled)
                {
                    var p = useCaptures ? ReadCapture(item) : Clone(item.payload);
                    p.sessionId = Guid.NewGuid().ToString("N"); p.requestId = Guid.NewGuid().ToString("N");
                    p.guidance.snapshotId = Guid.NewGuid().ToString("N");
                    if (p.navigation != null) p.navigation.snapshotId = Guid.NewGuid().ToString("N");
                    trials.Add(new Trial { case_id = item.id, category = item.category, repetition = repetition,
                        provenance = useCaptures ? "unity_capture_replay" : suite.provenance,
                        payload = p, goal_actions = item.goal_actions, expected_kinds = item.expected_kinds, expected_target = item.expected_target });
                }
            }
            plan = new Plan { run_id = Guid.NewGuid().ToString("N"), started_utc = DateTime.UtcNow.ToString("o"),
                suite_hash = suiteHash, suite_version = suite.version, endpoint = endpoint, seed = seed, trials = trials.ToArray() };
            runFolder = Path.Combine(Runs, DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + "_" + plan.run_id);
            Directory.CreateDirectory(runFolder);
            File.WriteAllText(Path.Combine(runFolder, "plan.json"), JsonUtility.ToJson(plan, true), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(runFolder, "suite.json"), JsonUtility.ToJson(suite, true), new UTF8Encoding(false));
            results.Clear(); current = null; nextTrial = 0; lastReply = ""; error = "";
            running = true; checkingHealth = true;
            request = UnityWebRequest.Get(new Uri(uri, "/health").AbsoluteUri);
            request.timeout = 8; requestStarted = EditorApplication.timeSinceStartup; request.SendWebRequest();
            message = "Checking gateway mode, knowledge version and trace recording...";
        }
        catch (Exception e) { error = e.Message; Stop("setup_failed"); }
    }
    private void Poll()
    {
        if (disposed || !running) return;
        try
        {
            if (request != null)
            {
                if (!request.isDone) return;
                if (checkingHealth)
                {
                    if (request.result != UnityWebRequest.Result.Success) throw new InvalidOperationException("Gateway health check failed. Start server.py first.");
                    var health = JsonUtility.FromJson<Health>(request.downloadHandler.text);
                    if (health == null || !(health.mode == "mock" || health.mode == "model") ||
                        (health.mode == "model" && !health.configured)) throw new InvalidOperationException("Gateway model credentials are not configured.");
                    if (health.assistantKnowledgeVersion != suite.knowledge_version)
                        throw new InvalidOperationException("Knowledge differs from the suite. Regenerate the suite, restart the gateway and reload cases.");
                    File.WriteAllText(Path.Combine(runFolder, "health.json"), request.downloadHandler.text, new UTF8Encoding(false));
                    message = health.mode.ToUpperInvariant() + " run. " + (health.benchmarkTracing ? "Backend traces enabled." : "Traces disabled; raw/repair/review metrics will be unavailable.");
                    request.Dispose(); request = null; checkingHealth = false; nextSendAt = 0;
                }
                else CompleteRequest();
            }
            if (request == null && EditorApplication.timeSinceStartup >= nextSendAt)
            {
                if (nextTrial >= plan.trials.Length) { running = false; SaveSummary("complete"); message = "Batch complete. Review results.csv and raw records. Semantic judgments remain pending."; }
                else
                {
                    current = plan.trials[nextTrial++];
                    request = new UnityWebRequest(plan.endpoint, "POST");
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(RequestBody(current.payload)));
                    request.downloadHandler = new DownloadHandlerBuffer(); request.timeout = 90;
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.SetRequestHeader("X-Assistant-Benchmark", plan.run_id);
                    requestStarted = EditorApplication.timeSinceStartup; request.SendWebRequest();
                    message = "Sending " + current.case_id + ", repetition " + current.repetition + " (" + nextTrial + "/" + plan.trials.Length + ").";
                }
            }
            Repaint();
        }
        catch (Exception e) { error = e.Message; Stop("runner_error"); }
    }
    private void CompleteRequest()
    {
        var r = new Result { case_id = current.case_id, request_id = current.payload.requestId,
            repetition = current.repetition, provenance = current.provenance,
            elapsed_ms = Math.Round((EditorApplication.timeSinceStartup - requestStarted) * 1000, 3),
            status = "gateway_error", action_available = "NA", goal_corresponding = "NA", target_correct = "NA" };
        string raw = request.downloadHandler.text ?? "";
        File.WriteAllText(Path.Combine(runFolder, current.payload.requestId + ".gateway.txt"), raw, new UTF8Encoding(false));
        if (request.result != UnityWebRequest.Result.Success)
            r.error_code = request.responseCode == 0 ? "connection_or_timeout" : "http_" + request.responseCode;
        else
        {
            try { r.response = JsonUtility.FromJson<AssistantChatResponse>(raw); }
            catch (Exception) { r.error_code = "response_invalid"; }
            if (r.response != null)
            {
                r.source = r.response.source; r.kind = r.response.kind; r.answer = r.response.answer;
                try { r.response_contract_valid = raw.Length <= 32768 && AssistantChatClient.Valid(r.response, current.payload) && MappingValid(r.response); }
                catch (Exception) { r.response_contract_valid = false; }
                r.status = r.response_contract_valid ? "gateway_returned" : "response_invalid";
                r.expected_kind_ok = current.expected_kinds.Contains(r.kind);
                r.action_id = r.response.suggested_action_ids?.FirstOrDefault() ?? "";
                if (r.kind == "guide")
                {
                    r.action_available = (r.response_contract_valid && Actions(current.payload).Contains(r.action_id)).ToString();
                    r.goal_corresponding = (r.response_contract_valid && current.goal_actions.Contains(r.action_id)).ToString();
                }
                else if (current.expected_kinds.Length == 1 && current.expected_kinds[0] == "guide")
                    r.goal_corresponding = "False";
                if (!string.IsNullOrEmpty(current.expected_target))
                    r.target_correct = (r.response_contract_valid && r.kind == "guide" && r.action_id == "highlight:" + current.expected_target).ToString();
            }
        }
        lastReply = current.case_id + " | " + r.status + " | " + r.source + " | " + r.elapsed_ms + " ms\n" +
                    "Action: " + r.action_id + "\nGoal set match: " + r.goal_corresponding + "\n\n" + (r.answer ?? r.error_code);
        results.Add(r); SaveResult(r); request.Dispose(); request = null; current = null;
        // Sequential pacing keeps even a fast mock run below the gateway's 30/minute limit.
        nextSendAt = EditorApplication.timeSinceStartup + 3.1;
        SaveSummary("running");
    }
    private bool MappingValid(AssistantChatResponse response)
    {
        if (response.kind != "guide") return true;
        if (response.suggested_action_ids == null || response.suggested_action_ids.Length != 1) return false;
        string id = response.suggested_action_ids[0];
        if (id == null || !id.StartsWith("learn:", StringComparison.Ordinal)) return true; // Client validates navigation mapping.
        var entry = catalog.actions.FirstOrDefault(a => a.id == id);
        return entry != null && response.interaction_ids != null && response.knowledge_topics != null &&
               response.interaction_ids.SequenceEqual(new[] { entry.interaction_id }) && response.knowledge_topics.SequenceEqual(new[] { entry.knowledge_topic });
    }
    private void SaveResult(Result r)
    {
        File.WriteAllText(Path.Combine(runFolder, r.request_id + ".result.json"), JsonUtility.ToJson(r, true), new UTF8Encoding(false));
        string path = Path.Combine(runFolder, "results.csv");
        if (!File.Exists(path)) File.WriteAllText(path, "case_id,repetition,request_id,status,source,provenance,kind,action_id,contract_valid,expected_kind_ok,action_available,goal_corresponding,target_correct,latency_ms,client_apply_executed,manual_review_pending,error_code,answer\n", new UTF8Encoding(true));
        var cells = new object[] { r.case_id, r.repetition, r.request_id, r.status, r.source, r.provenance, r.kind, r.action_id,
            r.response_contract_valid, r.expected_kind_ok, r.action_available, r.goal_corresponding, r.target_correct,
            r.elapsed_ms.ToString(System.Globalization.CultureInfo.InvariantCulture), false, true, r.error_code, r.answer };
        File.AppendAllText(path, string.Join(",", cells.Select(Csv)) + "\n", new UTF8Encoding(false));
    }
    private static string Csv(object value)
    {
        string s = value?.ToString() ?? "";
        if (s.Length > 0 && "=+-@\t\r".IndexOf(s[0]) >= 0) s = "'" + s;
        return "\"" + s.Replace("\"", "\"\"") + "\"";
    }
    private void SaveSummary(string status)
    {
        if (plan == null || string.IsNullOrEmpty(runFolder)) return;
        string text = "# Assistant benchmark run\n\nStatus: " + status + "\n\n" +
            "Recorded trials: " + results.Count + " / " + plan.trials.Length + "\n\n" +
            "Unrecorded trials remain not run. Errors and cancellations are retained; no automatic retries.\n\n" +
            "This is fixed-snapshot gateway replay. Client Apply, marker rendering and stale-state rejection were not executed. " +
            "Goal-set matches and contract checks are preliminary; prose rubrics require human review. Mock results are integration checks only.\n\n" +
            "Backend trace folder key: " + plan.run_id + "\n";
        File.WriteAllText(Path.Combine(runFolder, "SUMMARY.md"), text, new UTF8Encoding(false));
    }
    private void Stop(string reason)
    {
        if (!running && request == null) return;
        running = false;
        if (request != null)
        {
            request.Abort(); request.Dispose(); request = null;
            if (current != null)
            {
                var r = new Result { case_id = current.case_id, repetition = current.repetition,
                    request_id = current.payload.requestId, provenance = current.provenance, status = "cancelled",
                    error_code = reason, elapsed_ms = (EditorApplication.timeSinceStartup - requestStarted) * 1000,
                    action_available = "NA", goal_corresponding = "NA", target_correct = "NA" };
                results.Add(r); SaveResult(r);
            }
        }
        current = null; checkingHealth = false; SaveSummary(reason);
        message = "Batch stopped. Completed records are saved; the backend may finish the current upstream call.";
    }
}
