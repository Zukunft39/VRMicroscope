# 语音输入：Groq Whisper

实现日期：2026-09-13。用于现有小助手问答窗口，默认设备录音 → Groq 原语言转写 → 填入草稿 → 玩家确认发送 → 原有 DeepSeek 问答与实验指引。

## 启动与配置

在项目根目录的 PowerShell 中运行；若已有后端运行，先在它的终端按 Ctrl+C 停止再重启：

```powershell
python Tools/ai_tutor/server.py --prompt-key --prompt-groq-key
```

按各自提示输入 Groq 和 DeepSeek 密钥。输入不回显，只保留在当前后端进程环境，不写入项目。不要将真实密钥发到聊天中，也不要填入 Unity Inspector。已有 `DEEPSEEK_API_KEY` / `GROQ_API_KEY` 环境变量时，可省略对应的提示参数。Groq 未配置时，原有文字问答仍可使用；语音识别会提示缺少配置。

`Assets/Resources/LocalAssistantSettings.asset` 新增：

- **Speech Endpoint**：`http://127.0.0.1:8765/speech`。
- **Speech Timeout Seconds**：50 秒，后端 Groq 请求超时为 35 秒。

Groq 模型固定在 `Tools/ai_tutor/speech.py` 的 `MODEL`，当前为 `whisper-large-v3-turbo`。调用官方 `/openai/v1/audio/transcriptions`，不调用翻译接口，不指定 `language`，不注入翻译提示词。中文语音转中文，英文语音转英文；混合语言和专业词的实际准确率需实测。转写文本作为普通用户草稿，不能改变系统提示词或直接执行实验动作。DeepSeek 回答语言规则不在本次修改范围内。

依据：[Groq Speech to Text 官方文档](https://console.groq.com/docs/speech-to-text)。

## 玩家操作

1. 运行 `MainScene_Labortory`，按 H 或点击小球，进入「问点什么」。
2. 点击输入框上方右侧「语音输入」。首次使用允许系统麦克风权限。
3. 对系统默认麦克风说话；按钮显示录音秒数。再次点击「停止」结束并转写，最长 30 秒自动结束并转写。
4. 识别结果填入输入框。已有草稿时另起一行追加；合计超过 1000 字则提示精简，保留原草稿，不静默截断文字。
5. 检查、修改文字后点击「发送」，才会调用 DeepSeek。语音识别本身不会提交问答或添加实验标记。

点击「取消语音」、关闭窗口、新对话、离开场景都会取消当前语音操作。录音期间应用失去焦点或进入后台也会停止并丢弃录音。权限弹窗阶段不因失去焦点而取消。取消后迟到的识别结果不会填入新会话。

录音期间与转写等待期间锁定输入框和发送，避免覆盖玩家正在编辑的草稿。转写失败后仍保留原文字，不自动重试。音频只暂存在客户端及后端内存，转写时发送给 Groq，本项目不保存录音文件或记录音频、识别文本、密钥日志。已发送给供应商的请求不保证能撤回，取消客户端不等于取消供应商计费。

## 设备与平台

Unity `Microphone.Start(null, ...)` 使用系统默认输入设备。PC / PC VR 请在 Windows 声音设置中将需要的麦克风（例如头显麦克风）设为默认输入，并允许桌面应用访问麦克风。更改默认设备后重新开始录音。没有设备、权限不足、启动无数据、设备中断、录音不足半秒均显示提示。

优先申请 16 kHz，按设备能力选择 8–48 kHz 范围内的采样率；录音下混为单声道 PCM16 WAV，只编码实际录制的帧。后端限制半秒至 30 秒和请求大小，拒绝损坏音频及数字静音。数字静音检测不是完整的人声活动检测，环境噪声仍可能导致误识别，请确认文字后发送。

已加入 Android 运行时麦克风权限申请。**独立 Quest 的网络部署仍需额外配置**：现有 Python 后端仅监听电脑回环地址；头显上的 `127.0.0.1` 指向头显自身。PC VR 可沿用本机地址，独立头显需部署可达且受保护的 HTTPS 网关，并分别配置 Speech Endpoint 与 Chat Endpoint。本次未开放局域网监听，也未验证 Android 构建/真机录音。其他平台发布前需补充该平台的麦克风用途描述及权限配置。

## 验收

自动检查：

```powershell
python -B -m unittest discover -s Tools/ai_tutor -p "test_*.py"
python -B Tools/local_assistant/verify.py
```

覆盖音频格式/时长/截断/静音、multipart 与模型/接口选择、中英文原文保留、缺失密钥、供应商限额/鉴权/超时、错误脱敏、HTTP 路由、并发限制及失败后恢复；同时编译实际 Unity runtime/editor 脚本，并检查 WAV 编码与现有闲置策略。测试替换外部调用，不使用真实密钥，也不证明真实识别准确率。

不用密钥可运行 `python Tools/ai_tutor/server.py --mock`。录音入口仍使用真实麦克风，但后端只返回固定中文测试句，UI 明确标为「模拟转写」，不是识别录音。这个选项同时模拟 DeepSeek 与 Groq，不能用于检验真实中英文识别。

真机手测（尚待执行）：

| 测试 | 预期 |
|---|---|
| 中文：「数值孔径如何影响分辨率？」 | 中文草稿，未自动发送 |
| 英文：「What does the objective lens do?」 | 英文草稿，未自动翻译 |
| 中文夹专业词：「SNOM 的 probe 有什么作用？」 | 检查专业词准确性，可手动修正 |
| 已有草稿，再录音 | 追加转写，原草稿保留 |
| 停止不足半秒、静音、拔出设备 | 合适错误提示，无错误内容回填 |
| 录制超过 30 秒 | 自动停止并转写，麦克风释放 |
| 录音或转写时取消/关闭/新对话，随后再打开 | 无残留录音、无旧结果覆盖，可重新录音 |
| 录音时切出应用/摘下触发暂停的头显 | 录音取消，无后台持续录音 |
| 拒绝麦克风权限，再去系统设置开启 | 能提示问题并在重新点击后恢复 |
| Groq 密钥无效/网络断开/免费额度触发 429 | 草稿保留，可稍后手动重试 |
| 转写后点击发送，并询问当前部件或实验 | 原有 DeepSeek 问答、状态校验与指引正常 |

## 代码入口

- `AssistantChatPanel.cs`：按钮、状态、草稿追加、请求互斥。
- `AssistantVoiceInput.cs`：权限、录音生命周期、上传、取消与错误显示。
- `AssistantWavEncoder.cs`：可独立验证的 WAV 编码。
- `Tools/ai_tutor/server.py`：`/speech` 路由和隐藏密钥输入。
- `Tools/ai_tutor/speech.py`：音频校验、Groq multipart 请求与响应处理。
