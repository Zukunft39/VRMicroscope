# Automated State Replay in the Unity Editor / Unity 编辑器中的自动状态回放

Open **Tools > AI Benchmark**. The same window is also available under **Tools > VR Microscope > AI Benchmark** and **Tools > Microscope > AI Benchmark**. If the menu is absent after compilation, right-click `Assets/m_Scripts/Editor/AssistantBenchmarkWindow.cs` in the Project window and choose **Reimport**, then run **Assets > Refresh** and check the Console. Fixed-state replay does not require Play Mode. Selecting a case supplies its question, history, guidance snapshot and navigation snapshot to the same assistant gateway used by normal chat. It does not change the 3D scene; the response is previewed and is not executed as a scene operation or marker.

打开 **Tools > AI Benchmark**。同一窗口也可从 **Tools > VR Microscope > AI Benchmark** 或 **Tools > Microscope > AI Benchmark** 打开。如果编译完成后仍没有菜单，请在 Project 窗口右键 `Assets/m_Scripts/Editor/AssistantBenchmarkWindow.cs`，选择 **Reimport**，然后执行 **Assets > Refresh** 并检查 Console。固定状态回放不需要 Play Mode。选择案例后，工具会将问题、历史、指导状态快照和导航状态快照发送到普通聊天使用的同一助手网关。它不会改变三维场景；回答只会预览，不会作为场景操作或标记执行。

## Start a run / 开始运行

From the repository root, rebuild the suite:

在项目根目录重新生成测试套件：

```powershell
python -B Tools/ai_tutor/prepare_fixed_state_benchmark.py
```

For a free integration check, start Mock mode:

进行不消耗 API 的联调检查时，启动 Mock 模式：

```powershell
python -B Tools/ai_tutor/server.py --mock --benchmark-traces BenchmarkRuns/traces
```

For real DeepSeek evaluation, stop Mock with `Ctrl+C`, then start:

进行真实 DeepSeek 评估时，先按 `Ctrl+C` 停止 Mock，再启动：

```powershell
python -B Tools/ai_tutor/server.py --prompt-key --benchmark-traces BenchmarkRuns/traces
```

Enter the key at the hidden prompt. Omit `--prompt-key` if `DEEPSEEK_API_KEY` is already configured in the server environment. `AI_ASSISTANT_MODEL` or `AI_TUTOR_MODEL` selects the model; the fallback is `deepseek-flash`. Restart the gateway after changing Python code or knowledge files.

在隐藏提示中输入密钥。如果服务器环境中已设置 `DEEPSEEK_API_KEY`，可以省略 `--prompt-key`。通过 `AI_ASSISTANT_MODEL` 或 `AI_TUTOR_MODEL` 选择模型；默认模型为 `deepseek-flash`。修改 Python 代码或知识文件后需要重启网关。

In Unity, allow scripts to compile, open the benchmark window, keep **Use saved Unity captures** off for the source-derived run, select **GC03**, and click **Send selected (1 request)**. The window checks gateway health and knowledge version, then shows the answer, action ID and preliminary G(s,q) match. Set **Repetitions per case = 3** and click **Run all (78 requests)** for the full batch. Use **Open saved run folder** to inspect results; **Stop batch** preserves completed records.

在 Unity 中等待脚本编译完成，打开 benchmark 窗口。使用源代码推导状态时关闭 **Use saved Unity captures**，选择 **GC03**，然后点击 **Send selected (1 request)**。窗口会检查网关状态和知识版本，并显示回答、动作 ID 以及初步的 G(s,q) 匹配结果。完整运行时将 **Repetitions per case** 设置为 3，再点击 **Run all (78 requests)**。使用 **Open saved run folder** 查看结果；**Stop batch** 会保留已经完成的记录。

The suite version and knowledge hash are checked before model requests. If they differ, rebuild the suite, restart the gateway and click **Reload case suite**. Requests run sequentially with at least 3.1 seconds between completed requests. One application request may cause generation, repair and semantic-review provider calls. Timeouts and HTTP 429 responses remain failures; there is no automatic retry-until-success.

模型请求前会检查套件版本和知识哈希。如果不一致，请重新生成套件、重启网关并点击 **Reload case suite**。请求按顺序运行，每次完成后至少间隔 3.1 秒。一次应用请求可能包含生成、格式修复和语义审核等多次服务商调用。超时和 HTTP 429 会保留为失败，不会自动重试直到成功。

## Automatically supplied cases / 自动提供的案例状态

| Case / 案例 | State supplied to AI / 提供给 AI 的状态 | Automatic question / 自动问题 |
|---|---|---|
| AV06 | Standard probe selected; installation allowed / 已选择 Standard 探针，可安装 | Next step with the selected probe / 选择探针后下一步 |
| AV07 | Probe installation in progress; only Exit / 探针安装中，只能 Exit | How to continue during installation / 安装期间如何继续 |
| GC03 | Upper Optical Assembly with NA entry / 带 NA 入口的 Upper Optical Assembly | How to explore NA and collection cone / 如何探索 NA 和收集锥 |
| GC04 | Objective Lens with spatial-frequency entry / 带空间频率入口的 Objective Lens | How to start spatial-frequency lesson / 如何开始空间频率实验 |
| GC05 | Active NA experiment / NA 实验运行中 | What to adjust to compare cones / 调整什么比较收集锥 |
| NV01 | Free-roaming navigation targets / 漫游导航目标 | Where to find THz s-SNOM / 到哪里学习 THz s-SNOM |

All 26 cases use the same process; no repeated mouse or VR sequence is needed. A(s), G(s,q) and expected outcomes are shown in the window, but only the request payload is sent to the model. Gold labels remain local for scoring.

全部 26 个案例使用同一流程，不需要逐个重复鼠标或 VR 操作。窗口会显示 A(s)、G(s,q) 和预期结果，但只有 request payload 会发送给模型，金标准标签只保存在本地用于评分。

## Capture one real state and replay it / 采集一次真实状态并回放

Source-derived fixtures and synthetic coordinates are labelled `source_derived_fixture`. To capture a real Unity snapshot, enter Play Mode, reproduce the case once, close the normal chat, select the case, and click **Capture current Unity state for this case**. The tool checks mode, phase, sample/probe/part state, experiment settings and the complete offered action set; mismatches are reported instead of changing labels. Captures are stored in `BenchmarkRuns/Captures/<case-id>.json`, with an old file archived before replacement.

源代码推导的固定数据和合成坐标标记为 `source_derived_fixture`。采集真实 Unity 快照时，进入 Play Mode 重现一次案例，关闭普通对话框，在 benchmark 窗口中选择案例并点击 **Capture current Unity state for this case**。工具会检查模式、阶段、样本、探针、部件状态、实验参数和完整可用动作集合；不匹配时会报告错误，不会修改标签。快照保存到 `BenchmarkRuns/Captures/<case-id>.json`，旧文件会在替换前归档。

Enable **Use saved Unity captures** to replay captures. A single-case run needs its capture; **Run all** needs all 26. Captured navigation uses real positions but replay freezes them and is labelled `unity_capture_replay`; it does not prove live marker rendering, movement correctness or stale-state rejection. Do not use `Time.timeScale=0` to freeze a live state because that changes guidance to blocked. Captures are tied to the suite hash and must be reviewed after suite changes.

开启 **Use saved Unity captures** 后即可回放真实快照。单案例运行需要对应快照；**Run all** 需要 26 个快照全部存在。真实导航位置在回放时会被冻结，并标记为 `unity_capture_replay`；这不能证明实时标记渲染、移动正确性或过期状态拦截。不要使用 `Time.timeScale=0` 冻结状态，因为这会使指导状态变为 blocked。套件变化后必须重新检查快照。

## Saved files / 保存的文件

The editor creates a unique folder under `BenchmarkRuns/` for each run.

编辑器会在每轮运行时于 `BenchmarkRuns/` 下创建唯一文件夹。

| File / 文件 | Content / 内容 |
|---|---|
| `plan.json` | Trial order, questions, snapshots, labels, IDs, seed and endpoint / 测试顺序、问题、快照、标签、ID、随机种子和端点 |
| `suite.json` | Frozen copy of the test design / 本轮测试设计副本 |
| `health.json` | Mock/model mode, model, knowledge version and trace status; no keys / 模式、模型、知识版本和日志状态；不含密钥 |
| `results.csv` | One row per completed, failed or cancelled request / 每个完成、失败或取消请求一行 |
| `<request-id>.gateway.txt` | Raw gateway response or error / 网关原始回答或错误 |
| `<request-id>.result.json` | Structured preliminary checks and response / 结构化检查结果和回答 |
| `SUMMARY.md` | Run status and measurement limits / 运行状态和测量限制 |

With `--benchmark-traces BenchmarkRuns/traces`, tagged requests produce `BenchmarkRuns/traces/<run-id>/<request-id>_<unique-suffix>.json`. These traces retain the first candidate, repair candidate, pre-review answer, review verdict, final response/error and server-relative timing. They do not record API keys or complete system prompts; untagged normal chats are not traced.

使用 `--benchmark-traces BenchmarkRuns/traces` 后，带测试标记的请求会生成 `BenchmarkRuns/traces/<run-id>/<request-id>_<unique-suffix>.json`。日志会保存首次候选、修复候选、审核前回答、审核结果、最终回答或错误以及服务器相对耗时。日志不保存 API 密钥和完整系统提示词；普通未标记聊天不会被记录。

## Interpret results / 解读结果

`goal_corresponding=True` means the final action belongs to the predefined G set and passes response mapping checks. It is not a completed human semantic review. Waiting, explanation and refusal cases often have no action; mark those fields N/A rather than successful. The tool keeps `manual_review_pending=true` and `client_apply_executed=false`.

`goal_corresponding=True` 表示最终动作属于预先定义的 G 集合，并通过响应映射检查。这不代表人工语义审核已经完成。等待、解释和拒答案例通常没有动作，应记为 N/A，不能直接算成功。工具会保留 `manual_review_pending=true` 和 `client_apply_executed=false`。

Check the prose rubrics in `CASES.md`, especially unsupported diagnosis and capability-boundary explanations. The window does not call `Guidance.Apply()` or `Navigation.Apply()` for replayed snapshots because the live scene may differ. Therefore report these as gateway-replay results; displayed-action and stale-state metrics require separate Unity tests.

请按照 `CASES.md` 的文字评分标准检查回答，尤其关注不支持的诊断和能力边界解释。由于实时场景可能不同，窗口不会对回放快照调用 `Guidance.Apply()` 或 `Navigation.Apply()`。因此这里的结果只能报告为网关回放；显示动作和过期状态指标仍需单独进行 Unity 测试。

Compilation, offline contract tests and Mock runs prove tool integration, not DeepSeek accuracy. Keep Mock and real-model runs separate. `BenchmarkRuns` is ignored by Git; deliberately export an audited run when preparing a paper supplement.

编译、离线契约测试和 Mock 运行只能证明工具链联通，不能证明 DeepSeek 的准确性。Mock 和真实模型运行必须分开。`BenchmarkRuns` 已加入 Git 忽略规则；准备论文补充材料时，应明确导出经过审核的测试结果。
