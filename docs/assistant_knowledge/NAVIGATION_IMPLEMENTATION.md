# 第三批（部分）：学习区域定位与视觉标记

实现版本：`assistant-chat-v2-navigation`。本次覆盖“想学什么 → 去哪个区域 → 相对方位和距离 → 标记 → 靠近消失”。具体仪器操作步骤、自动控制和行走路径规划不在本次实现中。

## 已核对的区域与功能

场景：`Assets/Scene/Scene_Laboratory/MainScene_Labortory.unity`。下表为资产层级、父节点 TRS、Prefab 场景覆盖共同计算的**静态 Transform 原点**，不是实时包围盒中心。可用 `python Tools/local_assistant/audit_navigation_scene.py` 重新生成 `navigation_scene_audit.json`。

| 目标 | 静态世界原点（Unity 单位） | 学习内容与依据 |
|---|---|---|
| 显微镜区域 | (-7.005, 1.187, 4.208) | 结构与部件功能。直接使用模式控制器已经绑定的 microscopeRoot |
| 上部光学组件 | (-7.1211, 1.187, 4.208) | 部件选择控制器将其绑定至 Start Numerical Aperture Experiment |
| 物镜 | (-7.02231, 1.05083, 4.20788) | 部件选择控制器将其绑定至 Start Spatial Frequency Experiment |
| THz s-SNOM | (-4.699, 0, 7.357) | 现有控制器通过 TDs_edited_UnityVeryLowPoly 识别装置；探针、近场耦合及扫描教学演示 |

前三项属于同一显微镜区域。上部组件和物镜会在展开/选择时移动，因此本次远处指引均标记原始显微镜仪器，而不是追踪隐藏、展开或移到镜头前的部件副本。共聚焦针孔、Z-stack 没有已确认可操作区域，不创建对应目标。

## 请求与回答流程

1. 玩家发送问题时，Unity 采集 `Camera.main` 世界位置及前向、单位换算、当前可见目标 MeshRenderer 世界包围盒中心。只在自由漫游、Normal 模式、无强制教程和 SNOM 演示时提供目标。
2. 请求的 `navigation` 字段包含 `snapshotId`、`playerPosition`、`playerForward`、`worldUnitsPerMeter`、`targets[{id,worldPosition}]`。目标名称、知识对应关系和允许动作由后端固定白名单补充，不接受玩家聊天文字中的配置。
3. 后端计算水平距离与基于视线的前/后/左/右，原始坐标与校验结果一起提供给 DeepSeek。正前方 ±45°；正后方 ≥135°；其余按左右分类。高度不计入距离，默认 1 Unity 单位 = 1 米。
4. DeepSeek 判断是否应定位，并用五字段 JSON 选择一个允许目标。后端检查 ID、动作与知识主题一一对应，再调用独立语义审核，检查主题匹配和是否夹带无关任务、虚构步骤。
5. Unity 检查请求/会话/快照 ID，重新核对目标和当前模式；以**回答到达时**的视角重新计算方位。创建标记成功后才生成“已经添加效果”的最终句子，之后沿用现有打字机输出。
6. 关闭问答窗后标记仍保留，玩家可行走。目标轮廓框、顶部光柱和上方定位条每 0.2 秒更新。靠近、超时、目标失效、离开自由漫游、取消标记或新对话时清除；离开后不会自动重现旧标记。

响应的 guide 示例（模型仅申请标记，不声明已成功）：

```json
{"kind":"guide","answer":"你可以通过显微镜学习区域了解数值孔径。它位于你左侧约 3.2 米处。","interaction_ids":["na_experiment"],"suggested_action_ids":["highlight:na_experiment"],"knowledge_topics":["numerical_aperture"]}
```

上例距离只是示例，不发送为固定事实。服务端另附 `navigationSnapshotId` 和现有身份/来源/知识版本元数据。客户端不通过搜索“NA”“标记”等自然语言关键词执行动作。

## 标记表现及可调参数

`AssistantNavigation.cs` 创建独立 LineRenderer 轮廓框和光柱，使用 Resources 内的 `AssistantNavigationHighlight.shader`。Shader 用 URP、透明混合与 `ZTest Always`，因此可以透过场景遮挡看到方向；它不是物体逐像素描边或无障碍路线。原材质与 SNOM 原有近距离效果保持独立。

在 `Assets/Resources/LocalAssistantSettings.asset` 的 Inspector 调整：

| 参数 | 默认 | 含义 |
|---|---|---|
| Navigation Enabled | 开 | 启用区域定位 |
| Arrival Distance Meters | 1.2 | 玩家水平位置到目标世界包围盒最近边缘的距离，小于等于此值清除 |
| World Units Per Meter | 1 | 世界单位/米；场景整体缩放后需核对 |
| Marker Lifetime Seconds | 120 | 标记最长持续时间 |
| Reduced Motion | 沿用原设置 | 开启时停止脉冲亮度变化 |

对话中的距离到目标包围盒中心；到达判定使用包围盒边缘，避免要求玩家走进仪器。已经处于到达范围时回复“你已经靠近……”，不闪现远处标记。Shader 不可用时只给方位并说明效果不可用，不谎称成功。

## 测试方法

修改后重启后端以加载新协议和提示词，并重新进入 Play 模式。先可用不消耗 API 的联调：

```powershell
python Tools/ai_tutor/server.py --mock
```

完成初始教程，回到实验室自由漫游，走到距离目标较远处。H →「问点什么」→ 输入“我想学习数值孔径，应该去哪里？”或“我想学习 SNOM，应该去哪里？”。模拟模式只用少量固定关键词联调，界面会注明非 AI。

预期：回答说明区域和入口关系、距离与方位；关闭问答窗可看到轮廓框和光柱，背对目标时定位条变成后方，走近后消失。再询问另一个目标应替换旧标记。测试期间先不要进入拆解或其他实验模式，否则标记会按规则清除。

真实模型测试沿用 `python Tools/ai_tutor/server.py --prompt-key`，密钥留在后端。测试“NA 是什么意思？”应解释且不新增标记；“我想调共聚焦针孔”不应标记 NA/SNOM；无关问题仍固定拒答。

自动检查：`python -m unittest discover -s Tools/ai_tutor -p "test_*.py"`；`python Tools/local_assistant/verify.py`。前者含坐标、白名单、模拟响应和模拟语义审核，后者编译 Unity 运行时/编辑器源码并检查提醒时序。真实 GPU 效果、PC/XR 行走距离体验与真实 DeepSeek 意图判断仍需场景实测，不能由编译或模拟测试替代。
