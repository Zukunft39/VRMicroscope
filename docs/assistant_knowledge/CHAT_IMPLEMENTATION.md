# 第二批：DeepSeek 项目知识问答

后续更新：当前已启用第三批的学习区域定位，协议版本为 `assistant-chat-v2-navigation`。下文的“本批”描述保留第二批范围；当前定位能力、请求新增字段和验收方式以 [NAVIGATION_IMPLEMENTATION.md](NAVIGATION_IMPLEMENTATION.md) 为准。

## 当前流程

运行主实验场景 → H 或点击小球 → 本地问候 → 点击「问点什么」→ 输入问题 → 点击「发送」→ 本地后端加载项目资料并调用 DeepSeek → 检查回复 → 在问答窗口显示。

问候、闲置提醒、打开问答窗口、选择主题和输入文字均不调用 API。只有点击发送才请求。服务错误不会显示成固定知识拒答。

已经实现自由知识问答。本批不采集真实实验参数、不批准目录条目、不生成具体操作步骤，也不自动操控实验。依据当前状态指向部件与交互操作属于第三批。

## 启动后端

在项目根目录的交互式终端运行：

```powershell
python Tools/ai_tutor/server.py --prompt-key
```

按终端提示输入 DeepSeek Key，输入不回显，仅保留在该后端进程环境中，不写入 Unity 资源、脚本或日志。关闭该进程后重新启动需再次输入。也可使用已配置的 `DEEPSEEK_API_KEY` 环境变量并省略 `--prompt-key`。

默认模型 `deepseek-flash`，与当前项目一致。只为新问答更换模型时，启动前设置：

```powershell
$env:AI_ASSISTANT_MODEL = 'deepseek-flash'
python Tools/ai_tutor/server.py --prompt-key
```

模型优先级：AI_ASSISTANT_MODEL → AI_TUTOR_MODEL → deepseek-flash。模型名称必须是账户实际支持的 API 模型标识。已有旧后端进程需要重启才能加载新 `/assistant` 路由与资料；端口占用时先在其终端 Ctrl+C 停止，不同时启动多个相同端口进程。

Unity 配置位于 `Assets/Resources/LocalAssistantSettings.asset`：

- Chat Endpoint：默认 `http://127.0.0.1:8765/assistant`。
- Chat Timeout Seconds：默认 50 秒，包含生成和内容复核的等待时间。

后端只监听本机回环地址，适用于 PC 编辑器/PC 构建以及连接同一 PC 的头显。独立头显上的 127.0.0.1 指向头显自身，不能直接连接电脑；独立设备联机需后续配置受保护的 HTTPS 网关，客户端允许 HTTPS 地址。不要把 API Key 放到头显客户端。

## 问答窗口

支持中文输入法、最多 1000 字的问题、滚动阅读、取消请求、失败后手动重试和新对话。Enter 换行，点击「发送」提交；避免中文输入法确认候选时误发送。请求期间不允许重复发送和改写输入内容；取消或失败保留草稿。

顶部提供五个主题示例，点击只填入问题，不自动发送。VR 可使用现有 UI 射线点击这些示例和按钮，并使用「屏幕键盘」输入英文、数字及基本标点；屏幕键盘不是中文拼音输入法。PC 使用系统输入法输入中文。没有接入语音服务。

只在内存保留最近四轮成功问答；新对话清除历史并更换会话 ID，退出场景后不持久化。发送时这些近期文字随当前问题提供给 DeepSeek，作为非可信对话内容；不能改写系统配置。后端不记录问题、答案或密钥。

问答打开时暂停闲置提醒，遮罩阻止点击穿透到实验 UI，并拦截项目中的桌面快捷键、Interactor 操作、NA 摇杆输入及教程按键监听。关闭当帧继续拦截，避免同一个 Escape 既关闭问答又退出实验。实验动画继续运行，问答不绕过或自动完成教程。使用真实头显仍需检查设备自身的移动组件及 UI 射线行为。

关闭、取消、新对话或场景销毁会取消当前客户端请求；请求 ID、会话 ID 和本地请求序号共同防止旧回复覆盖新对话。取消客户端等待不能保证撤销已经到达 DeepSeek 的请求或其计费；系统不自动重试付费请求。

## 知识与回复检查

`Tools/ai_tutor/assistant_chat.py` 在服务启动时读取：

1. SYSTEM_PROMPT.md。
2. PROJECT_KNOWLEDGE.md。
3. interaction_catalog.json。

按文件内容生成知识版本摘要。修改资料后重启服务生效。旧内嵌 Tutor 及其 `/tutor` 路由已经删除；后端仅提供 `/assistant` 问答和 `/health` 状态接口。启动脚本路径保持不变。

第二批只传递目录 ID、名称、模块、状态和功能边界，不提供具体操作步骤；强制所有提供给模型的 approved_for_guidance=false、context_valid=false、allowed_actions=[]。没有读取 context_example.json 冒充玩家状态。

生成采用 DeepSeek Chat Completions 非流式 JSON 输出。后端严格验证五字段、类型、长度、知识主题以及空动作数组。只接受 explain/clarify/refuse，拒绝 guide 和任何动作 ID。客户端再次检查回复身份和协议。

对非拒答候选，再单独调用一次 DeepSeek 检查整个请求是否在范围内、答案是否有资料依据、是否夹带具体操作指令。审核不通过时固定拒答；审核超时或返回非法结构时显示服务失败，不展示未经检查的候选。模型已经拒答时不再发起第二次调用。

因此正常知识回答通常需要两次 DeepSeek 调用，等待时间和用量高于单次调用。JSON 模式保证语法不等于保证事实；独立复核仍是模型判断，不能宣称能绝对阻止所有越界或幻觉。论文或正式教学使用前仍需真实模型评测。

拒答正文由程序统一为：

> 这个问题我暂时不知道哦，问问看别的吧

连接失败、请求过多、超时、截断输出、非法 JSON 与协议不符都有单独提示。不会将服务不可用解释成“这个知识我不知道”。

## 联调与验证

无密钥联调：

```powershell
python Tools/ai_tutor/server.py --mock
```

模拟后端只用少量关键词和预置知识检查界面链路，不调用模型、不收费，也不代表真实范围判定效果。窗口明确显示「联调模拟 · 非 AI 回答」。不能用模拟模式宣称提示词评测已通过。

自动检查：

```powershell
python -m unittest discover -s Tools/ai_tutor -p 'test_*.py'
python Tools/local_assistant/verify.py
```

本次通过：39 项 Python 测试（含旧路由回归、新路由真实本机 HTTP 联调、身份/历史/动作校验、复核失败、固定拒答与服务错误区分），完整 Unity 运行时与编辑器代码编译，以及 26 项计时检查。所有模型响应均为测试替身；没有使用真实 Key 或调用付费 API。

主场景检查入口 LocalAssistantSmokeCheck.Run 已补充问答窗口创建、输入控件、阅读暂停与关闭状态检查。本次未在已占用的主项目上重跑 Unity 批处理；桌面输入法、实际 UI 布局、VR 键盘/射线及真实服务响应仍需运行验收。

官方接口依据：[DeepSeek Chat Completions](https://api-docs.deepseek.com/api/create-chat-completion/)。

## 旧内嵌 AI 清理后的回归

删除后重新通过完整 Unity 脚本编译、26 项计时检查和 24 项现有助手后端测试，其中新增旧 /tutor 接口返回 404 的检查。旧 Tutor 的 16 项专用测试及旧知识文件随功能删除，不再计入当前测试数量。
