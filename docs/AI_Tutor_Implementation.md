> 历史存档：本文件描述的内嵌 Tutor 已删除，文件中的旧脚本、资源和启动步骤不再适用。当前使用极光小助手，见 [现行说明](assistant_knowledge/CHAT_IMPLEMENTATION.md)。

# 实验状态驱动的 AI 教学助手

本次实现覆盖 NA 和空间频率实验。进入现有实验后自动出现 `Tutor` 按钮，不需要重新保存主场景。默认使用本地预设指导；打开远程开关并运行配置好的后端后，才使用模型生成解释。离线指导与 mock 测试响应均不会标记成真实 AI 回复。

## 快速体验

1. 等待 Unity 导入并编译脚本，打开现有主场景，按原有流程进入 NA 或空间频率实验。
2. 点击左下角 `Tutor`，点击 `Start activity`，选择预测答案。
3. 面板自动收起。使用原有滑块或 High / Middle / Low 控件改变参数。
4. 再次打开 `Tutor`，点击 `I have observed the change`，回答检查题，阅读反馈，再点击 `Next activity`。
5. `Explain` 解释当前知识点，`Hint` 提供观察提示，`Summary` 汇总最近四次已完成尝试。`Restart` 重做当前活动。`Hide` 仅收起面板；`End guidance` 结束本次指导，再次进入实验可开启新会话。

界面沿用现有实验的英文。空间频率第二项活动要求先选择样本：若没有样本，退出实验、选择样本后重新进入。预测与观察阶段之间保持样本和照明不变；NA 需至少改变 0.005，避免微小抖动被记为有效观察。

## Unity 配置

菜单：`Tools > AI Tutor > Create or Select Settings`。

首次使用会创建 `Assets/Resources/AITutorSettings.asset` 和四个可编辑的 `TeachingActivitySO` 资源；再次使用只选择已有配置，不覆盖修改。未创建配置时也能使用内置默认活动。

| 设置 | 默认值 | 作用 |
|---|---|---|
| Enable Tutor | 开 | 开启实验内教学入口 |
| Enable Remote | 关 | 开启后向后端发送实验状态、答案及最近四次活动结果 |
| Endpoint | `http://127.0.0.1:8765/tutor` | 教学服务地址，客户端没有模型 API 密钥 |
| Timeout Seconds | 20 | 网络超时，失败时继续显示预设指导 |
| Enable Local Logs | 开 | 将教学记录写入 Unity persistentDataPath |
| Activities | 四项活动 | 修改题目、选项、正确答案、观察提示与固定解释 |

远程知识库按 activityId / knowledgeId 精确匹配。调整教学概念时，同时更新 `Tools/ai_tutor/knowledge.json`；新增活动 ID 时还需更新后端 `ACTIVITIES` 白名单。仅编辑题目措辞不需要改通信协议。正确答案下标必须与选项对应；第一版不支持模型自动生成题目或判卷。

## 后端联调：不调用模型

在项目根目录执行：

```powershell
python -B Tools/ai_tutor/server.py --mock
```

随后在 Unity 配置中打开 `Enable Remote`。后端将返回明确标记的 `Backend test response`，用于确认 Unity → HTTP → 结构化响应的链路。终端 Ctrl+C 停止服务。

mock 没有真实模型调用，不能作为 AI 功能或模型质量评估结果。

## DeepSeek 官方 API 接入

后端已切换到 DeepSeek 官方 `https://api.deepseek.com/chat/completions`，只读取 `DEEPSEEK_API_KEY`，不会使用旧的 `OPENAI_API_KEY`。Unity 的 Endpoint 仍是本地后端地址，不要将其改成 DeepSeek 地址或填写密钥。

最简单的启动方式：在项目根目录打开交互式 PowerShell，执行：

```powershell
python -B Tools/ai_tutor/server.py --prompt-key
```

看到 `DeepSeek API key (hidden, not saved):` 后在本机输入密钥并回车。输入不回显、不作为命令参数、不写入文件，只保存在本次后端进程的环境中；关闭进程后需重新输入。不要把密钥发到聊天、写进脚本或 Unity Inspector。

如果已通过本机环境或服务管理器设置 `DEEPSEEK_API_KEY`，可直接执行：

```powershell
python -B Tools/ai_tutor/server.py
```

默认模型为 `deepseek-flash`，依据 2026-09-12 查阅的官方文档；可通过服务端环境变量 `AI_TUTOR_MODEL` 覆盖为账户支持的 DeepSeek 模型。如果曾配置 OpenAI 模型名，请清除旧值或改为 DeepSeek 模型，例如：

```powershell
$env:AI_TUTOR_MODEL = "deepseek-flash"
```

在 Unity 中打开 `Tools > AI Tutor > Create or Select Settings`，勾选 `Enable Remote`，保持 Endpoint 为 `http://127.0.0.1:8765/tutor`。进入实验，点击 Tutor 的 Explain / Hint；成功时显示 `AI guidance`。终端 Ctrl+C 停止后端。启动后可打开 `http://127.0.0.1:8765/health` 检查 provider 与配置状态；`configured: true` 仅表示本机设置了密钥，不证明密钥有效或账户有余额。

后端使用 Python 标准库，无需安装第三方包。采用非流式 Chat Completions、JSON Output、显式关闭 thinking；解析 `choices[0].message.content`。JSON 输出只约束语法，因此仍在后端和 Unity 校验字段、消息长度与动作白名单。空内容、截断、拒绝和非法 JSON 返回失败，Unity 继续使用预设指导，不自动重试产生额外调用。

每次向 DeepSeek 发送当前实验状态和有限教学上下文，不上传样本图像、纹理内容或用户姓名。API 密钥仅用于向官方接口发送 Authorization 请求头，不返回 Unity，不写入教学日志。模型的 `reasoning_content` 不传回客户端。

接口依据：[DeepSeek 首次调用](https://api-docs.deepseek.com/)、[JSON Output](https://api-docs.deepseek.com/guides/json_mode/)、[Thinking Mode](https://api-docs.deepseek.com/guides/thinking_mode/)。本次适配未读取你的实际密钥，也未执行真实模型调用。

服务默认只监听 `127.0.0.1`，限制请求体 16 KiB、并发 2、每分钟 30 次请求，拒绝带浏览器 Origin 的调用，不开放 CORS。本服务是本机原型，不包含公网登录体系。远程头显部署需要带身份认证与 TLS 的服务入口；当前原型未实现用户身份接入。PC VR 使用同一台 PC 上的 Unity 与网关即可联调。独立头显中的 `127.0.0.1` 指头显自身，不能直接访问开发电脑。

## 实现结构

| 文件 | 职责 |
|---|---|
| `ExperimentContextProvider.cs` | 从两个实验读取只读快照，订阅参数变化 |
| `LearningSessionController.cs` | 预测、观察、检查、反馈状态机及规则判定 |
| `TeachingActivitySO.cs` | 四项默认教学活动和可编辑配置类型 |
| `AITutorRuntime.cs` | 生命周期、会话、请求取消、备用反馈及日志协调 |
| `AITutorPanel.cs` | 继承实验 Canvas 的可折叠界面、桌面/XR 射线接入 |
| `AITutorClient.cs` / `TutorProtocol.cs` | UniTask HTTP、协议类型、回复校验 |
| `LearningEventLogger.cs` | 每会话 JSONL 记录 |
| `AITutorSetup.cs` | 创建配置资源的编辑器菜单 |
| `Tools/ai_tutor/server.py` | 请求校验、知识条目选择、模型调用及输出校验 |

原实验控制器只增加快照、参数事件及实验开始/退出钩子。图像计算、控制器输入和原教程逻辑保持原有实现。AI 返回的 `focusTarget` 仅转为固定观察对象的文字提示，不移动、高亮或调用任意场景对象；`suggestedAction` 限定为 observe / reflect，不推进状态机。

## 请求与故障行为

- 滑块变化只更新状态版本，停稳 0.5 秒后写一条参数摘要；不逐帧请求模型或记录。
- 仅点击解释/提示/总结，或提交检查答案时发起模型请求；两次请求至少间隔 2 秒。
- 每次请求包含会话 ID、请求 ID、状态版本。参数变化、重新答题、换活动或退出会取消请求；返回时再次检查版本和会话是否仍有效。
- 回复必须通过消息长度、合法动作、知识点与目标校验，富文本标记被拒绝。
- AI 无法设置正确答案、完成任务、切换样本或改变实验参数。
- 断网、超时、模型拒绝、JSON 不合法时使用当前活动的预设反馈。
- 面板固定显示仿真边界，AI 文本不能替换它：NA 不等于倍率；频域支持域不是完整 SIM 重建。

格式约束不能证明自然语言的科学正确性，真实模型输出仍需要领域人员审核。当前四项题目是形成性练习，不是经过验证的学习量表；预测与检查的语义接近，不能用答对次数直接声称迁移能力提高。

## 日志

位置：`Application.persistentDataPath/AITutor/<随机会话ID>.jsonl`。

记录阶段、参数快照、预测与检查答案、提示次数、活动重做、请求失败/取消、响应耗时、模型与提示版本。模型文本也记录在本地响应条目中，以便审核。没有稳定个人标识；不会自动上传日志。关闭 `Enable Local Logs` 可禁用。重复练习作为多次尝试记录，摘要只显示最近四次。

## 已执行验证与待验证项

自动检查命令：

```powershell
python -B -m unittest discover -s Tools/ai_tutor -v
python -B Tools/ai_tutor/verify_csharp.py
```

第二个命令使用项目已生成的 Unity 编译参数和安装的 Unity 编译器，把验证产物写入 `Temp/AITutorVerification`，不会覆盖 Unity 的程序集缓存；还需要 .NET 8 SDK/runtime 运行纯逻辑检查。

已有验证：运行时及编辑器 C# 源码编译、31 项实际状态机与响应校验断言。本次 DeepSeek 适配覆盖 16 项 Python 后端测试，包含模拟官方响应的请求/解析链路、空内容/截断/拒绝、密钥隔离和本机 HTTP 回退路径。C# 纯逻辑检查仅替代 ScriptableObject 构造与 Inspector 特性，不验证 Unity 原生 UI。

尚未验证：Unity Play Mode 完整教学流程、头显射线命中/布局可读性、真实模型往返请求、真实模型科学准确性和运行性能。

手动验收步骤：

1. 在 NA 完成两项活动，检查原滑块、退出按钮及教程行为。
2. 在无样本时完成光栅活动，确认第二项活动被前置条件阻止；选择样本后完成两项。
3. 观察时切换照明/样本，确认不能把混杂变量的操作直接提交为有效观察。
4. 请求 AI 后立即拖动参数、重做活动或退出，确认旧答案不会再次出现。
5. 停止后端，确认预设指导可继续使用；重新启动 mock 后核对响应来源标签。
6. 使用 XR 射线验证 Tutor、选项、Hide 和原实验控件；特别检查面板展开时遮挡与文字尺寸。

SNOM、自适应实验排序、自由问答、语音、跨会话能力建模不在本次实现范围内，可在这套状态接口上继续扩展。
