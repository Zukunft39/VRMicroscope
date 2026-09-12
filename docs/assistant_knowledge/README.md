# 小助手知识目录

审阅日期：2026-09-12。第一批本地助手、第二批知识问答，以及第三批的学习区域定位与标记已实现。当前区域定位协议及测试见 NAVIGATION_IMPLEMENTATION.md；仪器操作步骤尚未启用，交互目录的运行验证状态未改变。

## 文件用途

| 文件 | 用途 |
|---|---|
| INTERACTIONS.md | 供项目负责人逐项审阅的交互指南，包含位置、条件、PC/XR 操作、观察与退出 |
| interaction_catalog.json | 结构化主目录：23 个条目、稳定 ID、关键词、验证状态和来源 |
| INPUT_AND_GAPS.md | 当前按键、旧说明冲突、未验证入口与运行验收清单 |
| PROJECT_KNOWLEDGE.md | 允许解释的项目知识与科学边界 |
| SYSTEM_PROMPT.md | 自由问答助手的提示词规则；不再使用固定题目教学任务作为主线 |
| PROMPT_DESIGN_REVIEW.md | 第三部分提示词设计依据、上下文与输出契约、28 个待执行验收用例 |
| LOCAL_ASSISTANT_IMPLEMENTATION.md | 第一批本地助手的使用、配置、暂停规则、验证结果与待实测项目 |
| CHAT_IMPLEMENTATION.md | 第二批自由知识问答的启动、输入、密钥配置、复核、联调与范围边界 |
| NAVIGATION_IMPLEMENTATION.md | 第三批区域定位、坐标输入、标记效果、到达消失与测试流程 |
| navigation_scene_audit.json | 当前场景的区域/部件世界原点与实验入口关联，静态审计而非实时位置 |
| deepseek_reference_pack.md | 本次版本的合并文本，可供本地后端读取后加入 messages |
| context_example.json | 未来 Unity 上下文结构示例，全部为示例值，不代表玩家实际状态 |
| source_audit.json | 主场景直接引用、组件 enabled 状态及 94 个自定义脚本的 SHA-256 快照 |

JSON 是交互事实主目录；修改后需同步更新 Markdown 与合并包，避免提供相互矛盾的版本。source_audit 是第一批编码之前的历史快照，未涵盖新增助手；其中的哈希用于发现源代码变更，不是运行正确性的证明。

## DeepSeek 能不能读取文件

普通 Chat Completions 不会访问开发电脑的 `G:/VRMicroscope/...`。只发送路径不会提供文件内容。

截至本次查阅，官方 Files API 用于上传和复用 JPEG、PNG、GIF、WebP 图片，并通过 file_id 在请求中引用；官方该页没有将 Markdown、JSON、TXT、PDF 列为支持上传的文档格式。不能把网页聊天产品的附件体验直接等同于这个 API。

本目录建议采用文本注入：本地 Python 后端以 UTF-8 读取文档，把内容作为消息文本提供给 DeepSeek。文本格式选用 Markdown 与 JSON，不需要将说明截图上传。

官方依据：

- [Files API](https://api-docs.deepseek.com/guides/files_api/)：已列出的图片格式与 file_id 使用方式。
- [首次 API 调用](https://api-docs.deepseek.com/)：Chat Completions 的 messages 请求结构。

## 建议的每次请求流程（尚未接入）

1. 服务启动时从受控的本地路径读取 SYSTEM_PROMPT.md、PROJECT_KNOWLEDGE.md 和 interaction_catalog.json；检查 schema_version，保留知识版本。
2. 每次用户提问时，根据问题、当前模块选择相关知识与交互条目。学习意图也应考虑其他模块，不能只搜当前模块。
3. 检查条目的批准状态和当前前置条件。模型不决定哪些条目已经验证。
4. 组装 system 消息：系统规则＋项目知识＋相关 INTERACTION_CATALOG。将玩家问题和近期对话作为单独的 user/assistant 消息，不把玩家原文拼进规则。
5. 加入当次真实 CURRENT_CONTEXT，包括输入设备、模块、模式和 allowed_actions。未知值保留 unknown/null。
6. 校验回复引用的交互 ID、允许动作、设备和知识范围；拒答由后端替换为固定句子。未确认条目只可讨论已知原理，不发布确定操作指令。
7. 记录知识版本、引用 ID 和回答；不保存密钥。

小规模原型可直接读取 deepseek_reference_pack.md 全文作为固定知识，但每次仍需附真实上下文。文件在本地只需读取一次/修改后重载，相关文本仍须随新请求提供；不要假定模型永久记住上一次调用的文件。无需向量数据库，也无需先调用一次模型来检索这份小目录。

**当前 server.py 提供 `/assistant` 路由**，通过 assistant_chat.py 加载本目录的规则、知识与目录。该路由只开放知识问答，强制空动作列表；旧内嵌 Tutor 的脚本、资源和 `/tutor` 路由均已删除。实时状态操作引导尚未接入，详情见 CHAT_IMPLEMENTATION.md。

## 验证状态和批准规则

- static_confirmed：源码和主场景/运行时入口有静态证据，尚未运行验证。
- conditional：依赖未确认的组件启用、实例注册或 Prefab 路径。
- knowledge_only：仅能进行知识说明，没有对应的已确认交互。
- unavailable：没有闭合的可用入口/操作流程。
- deprecated：代码还在，但用户已决定替换的上一版 Tutor。
- planned：新极光助手等设计，尚未实现。

所有 runtime_verified、approved_for_guidance 暂为 false。这是为了区分“检查过代码”和“玩家确实能完成操作”。局外确认时逐条验证 PC/XR；设备有差异时拆成不同条目或新增设备级批准字段，不要一次性全部置 true。

正式指引需要：approved_for_guidance=true、设备路径可用、当前前置条件满足。确认记录建议补充 tested_device、tester、tested_at、verified_steps 与 screenshots/video 路径。
