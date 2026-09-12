# VRMicroscope 交互目录（局外确认稿）

审阅日期：2026-09-12。范围：当前主场景与自定义运行时脚本。未运行 Unity/头显；所有条目待最终确认。

本 Markdown 由 interaction_catalog.json 同步生成，JSON 为结构化主目录。不要把 static_confirmed 理解成实机已验证。

## 总览

| ID | 内容 | 模块 | 审阅状态 |
|---|---|---|---|
| navigation | 漫游与视角 | navigation | static_confirmed |
| teleport | 指向目标移动 | navigation | conditional |
| sample_pick | 样本选择与拾取 | microscope | static_confirmed |
| sample_place_observe | 放置样本并进入观察 | microscope | static_confirmed |
| sample_remove | 取下载物台样本 | microscope | static_confirmed |
| illumination | 显微镜照明与光圈 | microscope | static_confirmed |
| objective_switch | 切换物镜 | microscope | static_confirmed |
| focus | 粗细调焦 | microscope | static_confirmed |
| assembly | 进入拆解、展开与复原 | structure | static_confirmed |
| parts | 部件查看 | structure | static_confirmed |
| na_experiment | 数值孔径交互实验 | numerical_aperture | static_confirmed |
| spatial_frequency | 空间频率与衍射实验 | spatial_frequency | static_confirmed |
| snom_entry | 进入 SNOM 操作演示 | snom | static_confirmed |
| snom_probe | 探针选择与安装 | snom | static_confirmed |
| snom_start | 启动 SNOM 并观察原理 | snom | static_confirmed |
| snom_controls | SNOM 导航与部件说明 | snom | static_confirmed |
| forced_tutorial | 强制交互教程 | tutorial | static_confirmed |
| optional_tutorial | 旧自由教程入口 | tutorial | conditional |
| stage_translation | 视野平移候选功能 | microscope | conditional |
| filter_prototype | 滤光片与二色镜候选原型 | optics_prototype | unavailable |
| confocal_background | 共聚焦原理说明 | confocal | knowledge_only |
| legacy_tutor | 上一版固定题目 Tutor | legacy | deprecated |
| aurora_assistant | 极光小球助手（规划） | assistant | planned |

## navigation — 漫游与视角

状态：static_confirmed；实机验证：否；批准用于自动指引：否。

位置：主实验室；未确认从出生点到各仪器的固定左右路线。

**前置条件**

- 处于允许自由移动的状态；未被强制教程或实验锁定。

**桌面操作**

- W/A/S/D 移动；按住鼠标右键并移动鼠标调整视角。
- E/Q 是该桌面控制器的上/下移动；LeftShift 加速。

**XR 操作**

- 输入资源包含左摇杆移动；具体头显 locomotion 组件启用状态须实测。

**退出/恢复**：无已确认步骤。

**边界**

- 当前主场景 CameraTryMove 启用；Move 组件禁用。不要把桌面调试上下移动解释成真实步行。

观察目标：观察玩家位置与视角变化。

完成依据：需要新增当前区域/位置上报；不能根据对话认定已到达。

证据：

- [Assets/m_Scripts/Microscope/CameraTryMove.cs](../../Assets/m_Scripts/Microscope/CameraTryMove.cs)
- [Assets/m_Scripts/Move.cs](../../Assets/m_Scripts/Move.cs)
- [Assets/m_Scripts/ProgressControl.cs](../../Assets/m_Scripts/ProgressControl.cs)

## teleport — 指向目标移动

状态：conditional；实机验证：否；批准用于自动指引：否。

位置：主实验室允许通行的表面

**前置条件**

- 自由移动未锁定；目标通过射线及坡度等检查。

**桌面操作**

- 鼠标指向可到达地面后按 G。

**XR 操作**

- Roaming/AutoMove 绑定左右 gripButton，但 Move 组件在主场景禁用，暂不作为确定可用指引。

**退出/恢复**：无已确认步骤。

**边界**

- XR 路径待验证；不可承诺任何点击位置都能到达。

观察目标：到达目标位置；可能受碰撞/可达性限制。

完成依据：需运行时位置变化确认。

证据：

- [Assets/m_Scripts/Microscope/CameraTryMove.cs](../../Assets/m_Scripts/Microscope/CameraTryMove.cs)
- [Assets/m_Scripts/Move.cs](../../Assets/m_Scripts/Move.cs)

## sample_pick — 样本选择与拾取

状态：static_confirmed；实机验证：否；批准用于自动指引：否。

位置：主场景带 InteractableSamples 的样本对象；样本桌相对方位未确认。

**前置条件**

- 进入样本交互范围并被注册为当前可交互样本。
- 没有优先处理的 SNOM/拆解交互；若载物台已有样本，取下样本优先。

**桌面操作**

- 靠近可交互样本，鼠标左键触发拾取。

**XR 操作**

- 靠近可交互样本，按右手扳机。

**退出/恢复**

- 可选择其他样本；不要声称存在通用丢弃键。

**边界**

- 命中射线不等于样本已注册；不是旧说明中的 R 拾取。

观察目标：样本实例与物品栏贴图更新。

完成依据：HasSampleOnHand、CurrentSampleTexture、SampleChanged（已有代码接口，未来助手需接入）。

证据：

- [Assets/m_Scripts/Interact/InteractWithSamples.cs](../../Assets/m_Scripts/Interact/InteractWithSamples.cs)
- [Assets/m_Scripts/Interact/InteractableSamples.cs](../../Assets/m_Scripts/Interact/InteractableSamples.cs)
- [Assets/m_Scripts/Interactor/Interactor.cs](../../Assets/m_Scripts/Interactor/Interactor.cs)
- [Assets/m_Scripts/Microscope/CameraTryMove.cs](../../Assets/m_Scripts/Microscope/CameraTryMove.cs)

## sample_place_observe — 放置样本并进入观察

状态：static_confirmed；实机验证：否；批准用于自动指引：否。

位置：主实验室的可操作显微镜；进入其交互触发范围。

**前置条件**

- 已持有 ObserveObjects 样本；isNear 与 MicroUI.setTrue 条件满足。

**桌面操作**

- 按 Z 放置样本。
- 样本已在载物台时继续按 Z 推进观察视点，直到进入 Observing。

**XR 操作**

- 按左手扳机放置样本；随后按相同按钮推进观察视点。

**退出/恢复**

- Observing 中按 Z 或 Esc；XR 用左手扳机退出。

**边界**

- PutAndObserve 的放置与 pointer 视点推进分开，不承诺一次按键完成全部过程。

观察目标：样本放上载物台，视角进入观察相机。

完成依据：HasPlacedSample 与 Interactor.CurrentState=Observing；需要助手采集。

证据：

- [Assets/m_Scripts/Microscope/Microscope.cs](../../Assets/m_Scripts/Microscope/Microscope.cs)
- [Assets/m_Scripts/Interactor/Interactor.cs](../../Assets/m_Scripts/Interactor/Interactor.cs)
- [Assets/m_Scripts/Microscope/CameraTryMove.cs](../../Assets/m_Scripts/Microscope/CameraTryMove.cs)

## sample_remove — 取下载物台样本

状态：static_confirmed；实机验证：否；批准用于自动指引：否。

位置：可操作显微镜交互范围

**前置条件**

- 先退出内部观察；载物台有样本。

**桌面操作**

- 在显微镜交互范围内鼠标左键触发取样。

**XR 操作**

- 按右手扳机触发取样。

**退出/恢复**：无已确认步骤。

**边界**

- 同一输入还处理部件/SNOM 选择，须确保处于正确模式。

观察目标：样本从显微镜移回玩家侧。

完成依据：HasPlacedSample 变化；不要仅检查物品栏图标。

证据：

- [Assets/m_Scripts/Microscope/Microscope.cs](../../Assets/m_Scripts/Microscope/Microscope.cs)
- [Assets/m_Scripts/Interactor/Interactor.cs](../../Assets/m_Scripts/Interactor/Interactor.cs)
- [Assets/m_Scripts/Microscope/CameraTryMove.cs](../../Assets/m_Scripts/Microscope/CameraTryMove.cs)

## illumination — 显微镜照明与光圈

状态：static_confirmed；实机验证：否；批准用于自动指引：否。

位置：可操作显微镜及观察界面

**前置条件**

- 光源开关要求靠近仪器且外部交互可用。

**桌面操作**

- 在外部交互状态按 B 切换光源。
- 按住 X，使用上/下方向键提供照明调节输入。

**XR 操作**

- 外部交互时按右手 secondaryButton 切换光源。
- 按住左手 primaryButton 并上下推动右摇杆提供照明调节输入。

**退出/恢复**：无已确认步骤。

**边界**

- 不要沿用旧说明的 Q 开灯；Q 在当前桌面控制器中用于下降。内部 Observing 不绑定 LightSwitch 动作。

观察目标：观察光源/光圈示意及观察画面变化。

完成依据：现有 GetLight 等状态可供后续采集；不能从按键直接推断光源状态。

证据：

- [Assets/m_Scripts/Microscope/Microscope.cs](../../Assets/m_Scripts/Microscope/Microscope.cs)
- [Assets/m_Scripts/Interactor/Interactor.cs](../../Assets/m_Scripts/Interactor/Interactor.cs)
- [Assets/m_Scripts/Microscope/CameraTryMove.cs](../../Assets/m_Scripts/Microscope/CameraTryMove.cs)

## objective_switch — 切换物镜

状态：static_confirmed；实机验证：否；批准用于自动指引：否。

位置：可操作显微镜或内部观察状态

**前置条件**

- 进入显微镜可交互状态，等待旋转动画完成。

**桌面操作**

- 按 R 触发物镜切换。

**XR 操作**

- 按右手 primaryButton 触发物镜切换。

**退出/恢复**：无已确认步骤。

**边界**

- R 在 SNOM 原理演示中是 Replay，必须按模块区分；倍率不是 NA 的同义词。

观察目标：观察物镜盘或对应观察倍率变化。

完成依据：物镜档位/相机状态需新增上报。

证据：

- [Assets/m_Scripts/Microscope/Microscope.cs](../../Assets/m_Scripts/Microscope/Microscope.cs)
- [Assets/m_Scripts/Interactor/Interactor.cs](../../Assets/m_Scripts/Interactor/Interactor.cs)
- [Assets/m_Scripts/Microscope/CameraTryMove.cs](../../Assets/m_Scripts/Microscope/CameraTryMove.cs)

## focus — 粗细调焦

状态：static_confirmed；实机验证：否；批准用于自动指引：否。

位置：显微镜观察界面

**前置条件**

- 已进入 Observing。

**桌面操作**

- 按 Tab 切换粗/细调模式。
- 按住 X，使用左/右方向键调焦。

**XR 操作**

- 按右摇杆按压切换粗/细调。
- 按住左手 primaryButton，左右推动右摇杆调焦。

**退出/恢复**

- Z 或 Esc 退出观察；XR 左手扳机退出。

**边界**

- 依据当前输入链路，不使用旧 M/B/N 调焦说明。视觉反馈不是标定成像。

观察目标：观察焦点清晰程度和粗细调状态的变化。

完成依据：调焦参数与模式需要后续助手采集。

证据：

- [Assets/m_Scripts/Microscope/Microscope.cs](../../Assets/m_Scripts/Microscope/Microscope.cs)
- [Assets/m_Scripts/Interactor/Interactor.cs](../../Assets/m_Scripts/Interactor/Interactor.cs)
- [Assets/m_Scripts/Microscope/CameraTryMove.cs](../../Assets/m_Scripts/Microscope/CameraTryMove.cs)

## assembly — 进入拆解、展开与复原

状态：static_confirmed；实机验证：否；批准用于自动指引：否。

位置：主场景显微镜装配模型；以整体模型为目标。

**前置条件**

- 教程未锁定模式；没有实验转场或独立实验锁定。

**桌面操作**

- 鼠标左键点击整体装配目标进入 PreAssembly。
- 再次点击装配目标进入 SuperAssembly，等待展开动画结束。

**XR 操作**

- 右手射线指向装配目标，按右手扳机进入 PreAssembly。
- 再次指向目标并按扳机进入 SuperAssembly。

**退出/恢复**

- 先退出部件选择；在 SuperAssembly 点击/指向模型外区域退出到 PreAssembly；再在模型外操作返回 Normal。

**边界**

- 输入按状态逐步处理，不能承诺一次点击空白区就完成所有返回。

观察目标：模型从整体展示变为展开结构。

完成依据：CurrentMode 与 ModelExploder.IsExploded/IsAnimating。

证据：

- [Assets/m_Scripts/ExplodeModel/MicroscopeExploderModeController.cs](../../Assets/m_Scripts/ExplodeModel/MicroscopeExploderModeController.cs)
- [Assets/m_Scripts/ExplodeModel/ModelExploder.cs](../../Assets/m_Scripts/ExplodeModel/ModelExploder.cs)
- [Assets/m_Scripts/Microscope/CameraTryMove.cs](../../Assets/m_Scripts/Microscope/CameraTryMove.cs)

## parts — 部件查看

状态：static_confirmed；实机验证：否；批准用于自动指引：否。

位置：SuperAssembly 展开的显微镜部件

**前置条件**

- 展开结束；未被其他实验锁定。

**桌面操作**

- 鼠标左键选择部件，阅读右侧名称与说明。
- 在说明面板外点击，结束当前部件查看，再选其他部件。

**XR 操作**

- 右手射线选择部件并按扳机；在选择 UI 外再次按扳机退出当前选择。

**退出/恢复**

- 退出选择后按 assembly 条目返回。

**边界**

- Upper Optical Assembly 的内部光学元件未逐个确认；不可随意叫作二色镜/针孔。后三项没有已配置独立实验按钮。

观察目标：已配置名称：Objective Lens、Upper Optical Assembly、Stage Position Control、Optical Breadboard、Focus Adjustment。

完成依据：IsSelectionActive、选中部件；需要完整上下文接口。

证据：

- [Assets/m_Scripts/ExplodeModel/SuperAssemblyPartSelectionController.cs](../../Assets/m_Scripts/ExplodeModel/SuperAssemblyPartSelectionController.cs)

## na_experiment — 数值孔径交互实验

状态：static_confirmed；实机验证：否；批准用于自动指引：否。

位置：展开模型中的 Upper Optical Assembly（内部节点 AboveMirror）

**前置条件**

- 完成 assembly 并选中 Upper Optical Assembly。

**桌面操作**

- 点击 Start Numerical Aperture Experiment，等待转场。
- 鼠标拖动 NA 滑块，比较角度、光锥与近似倍率显示。

**XR 操作**

- 用射线操作同名按钮与 NA 滑块。
- 备用摇杆路径读取 Roaming/ChangeFocusOrChangeLIght 的 x 轴；通常需左 primary 修饰，实际设备须验证。

**退出/恢复**

- 点击 Exit 返回部件查看；再按 parts/assembly 返回。

**边界**

- NA=n sin(theta)；倍率是参考档位插值，亮度/清晰度/景深反馈为教学映射。

观察目标：NA 连续范围 0.03–0.95；观察 theta、全角和光锥变化。

完成依据：IsExperimentActive、IsTransitioning、CurrentNA、CaptureTeachingState 已有。

证据：

- [Assets/m_Scripts/Experiment/NumericalApertureExperimentController.cs](../../Assets/m_Scripts/Experiment/NumericalApertureExperimentController.cs)
- [Assets/m_Scripts/ExplodeModel/SuperAssemblyPartSelectionController.cs](../../Assets/m_Scripts/ExplodeModel/SuperAssemblyPartSelectionController.cs)

## spatial_frequency — 空间频率与衍射实验

状态：static_confirmed；实机验证：否；批准用于自动指引：否。

位置：展开模型中的 Objective Lens（内部节点 ObjectLen）

**前置条件**

- 完成 assembly 并选中 Objective Lens；若要看样本频谱，先选择样本。

**桌面操作**

- 点击 Start Spatial Frequency Experiment，等待转场。
- 点击 High/Middle/Low，分别对应 250/125/62.5 lines/mm。
- 点击 White Light / Laser Excitation 比较照明；做控制变量比较时保持照明与样本不变。

**XR 操作**

- 右手 UI 射线操作相同按钮和频率选项。

**退出/恢复**

- 点击 Exit 返回部件查看。

**边界**

- 无样本时三幅样本派生频谱为空；不等于共聚焦针孔仿真，也没有完整 SIM 图像重建。

观察目标：观察光栅、衍射级次间距，以及样本频谱、侧带和多方向支持域。

完成依据：IsExperimentActive、CaptureTeachingState、SampleChanged 可用于后续采集。

证据：

- [Assets/m_Scripts/Experiment/SpatialFrequencyExperimentController.cs](../../Assets/m_Scripts/Experiment/SpatialFrequencyExperimentController.cs)
- [Assets/m_Scripts/Experiment/SpatialFrequencyExperimentDiagramView.cs](../../Assets/m_Scripts/Experiment/SpatialFrequencyExperimentDiagramView.cs)
- [Assets/m_Scripts/Experiment/FourierOpticsCpuSimulator.cs](../../Assets/m_Scripts/Experiment/FourierOpticsCpuSimulator.cs)
- [Assets/m_Scripts/ExplodeModel/SuperAssemblyPartSelectionController.cs](../../Assets/m_Scripts/ExplodeModel/SuperAssemblyPartSelectionController.cs)

## snom_entry — 进入 SNOM 操作演示

状态：static_confirmed；实机验证：否；批准用于自动指引：否。

位置：主场景模型 TDs_edited_UnityVeryLowPoly；空间方位须现场确认。

**前置条件**

- 靠近 SNOM 系统；运行时控制器已成功找到模型并创建入口。

**桌面操作**

- 鼠标点击 SNOM 交互目标，显示入口。
- 点击 Begin Operation。

**XR 操作**

- 右手射线指向 SNOM 入口目标并按扳机；点击 Begin Operation。

**退出/恢复**

- 点击 Exit；桌面 Esc。

**边界**

- 此模块由 RuntimeBootstrap 动态创建，不能只凭场景没有直接挂脚本判断不存在。

观察目标：出现探针选择及安装界面。

完成依据：CurrentStage/工作流阶段存在于控制器，未来助手需扩展状态采集。

证据：

- [Assets/m_Scripts/Experiment/SNOMDemonstrationController.cs](../../Assets/m_Scripts/Experiment/SNOMDemonstrationController.cs)

## snom_probe — 探针选择与安装

状态：static_confirmed；实机验证：否；批准用于自动指引：否。

位置：SNOM 操作面板 ProbeSelection 阶段

**前置条件**

- 已进入 Begin Operation。

**桌面操作**

- 点击 Fine / Standard / Robust 探针选项，或按 1/2/3。
- 点击 Install Probe，或按 E；等待安装结束。

**XR 操作**

- 用 UI 射线选择探针卡片，再点击 Install Probe。

**退出/恢复**

- 点击 Exit 或桌面 Esc。

**边界**

- 3x/2x/1x 是相对细节教学标签，不是物镜倍率；复用模型网格，不是三个已校准物理探针。

观察目标：运行时探针预览移动到安装位置，完成后显示 Start System。

完成依据：ReadyToStart 工作流状态；不要把点击安装等同于动画已完成。

证据：

- [Assets/m_Scripts/Experiment/SNOMDemonstrationController.cs](../../Assets/m_Scripts/Experiment/SNOMDemonstrationController.cs)

## snom_start — 启动 SNOM 并观察原理

状态：static_confirmed；实机验证：否；批准用于自动指引：否。

位置：SNOM ReadyToStart 面板

**前置条件**

- 探针安装结束；Start System 已可用。

**桌面操作**

- 点击 Start System 或按 Space。

**XR 操作**

- 点击 Start System。

**退出/恢复**

- Exit 或桌面 Esc。

**边界**

- 安装探针与启动是用户操作；后续八阶段是内部过程讲解，不是八项额外仪器操作。

观察目标：八阶段自动演示：THz 产生、光束引导、AFM 反馈、近场耦合、散射背景、谐波解调、逐点扫描、结果与局部光谱。

完成依据：CurrentStage；还需采集播放/暂停与扫描进度。

证据：

- [Assets/m_Scripts/Experiment/SNOMDemonstrationController.cs](../../Assets/m_Scripts/Experiment/SNOMDemonstrationController.cs)
- [Assets/m_Scripts/Experiment/SNOMDemonstrationGraphic.cs](../../Assets/m_Scripts/Experiment/SNOMDemonstrationGraphic.cs)

## snom_controls — SNOM 导航与部件说明

状态：static_confirmed；实机验证：否；批准用于自动指引：否。

位置：SNOM PrincipleTour 演示控制栏

**前置条件**

- 已启动系统进入原理讲解。

**桌面操作**

- Previous/Next 或左/右方向键切换讲解。
- 播放/暂停按钮或 Space；Replay 或 R 重播当前阶段。
- 点击 Components 进入部件说明，并选择高亮部件。
- Change Probe 仅在该按钮实际显示的阶段使用。

**XR 操作**

- 使用 UI 射线操作 Previous、Next、播放/暂停、Replay、Components；按当前可见按钮操作。

**退出/恢复**

- Exit；桌面 Esc。

**边界**

- 不要承诺 Raw/2Ω/3Ω 独立按钮：设计说明曾提出，但本轮没有确认实际控制按钮。图谱为合成示意。

观察目标：动画、图示和说明随阶段/部件变化。

完成依据：CurrentStage 与当前选择/播放状态；需新增完整上报。

证据：

- [Assets/m_Scripts/Experiment/SNOMDemonstrationController.cs](../../Assets/m_Scripts/Experiment/SNOMDemonstrationController.cs)
- [Assets/m_Scripts/Experiment/SNOMDemonstrationGraphic.cs](../../Assets/m_Scripts/Experiment/SNOMDemonstrationGraphic.cs)

## forced_tutorial — 强制交互教程

状态：static_confirmed；实机验证：否；批准用于自动指引：否。

位置：场景中的 MandatoryTutorialTrigger 与 StandaloneTutorialUI 面板

**前置条件**

- 触发区域/顺序/样本等前置条件满足。

**桌面操作**

- 按当前教程显示的目标输入完成步骤；不要把所有步骤都理解为按空格。

**XR 操作**

- 按教程指定的手柄动作；组合动作可能需要持续按住修饰键。

**退出/恢复**

- 按教程自身流程完成；未确认存在通用跳过按钮。

**边界**

- 教程可能锁定移动、拆解或全局输入；闲置提醒应暂停。

观察目标：步骤在匹配输入或事件后推进；某些展示步骤会自动推进。

完成依据：教程完成事件与触发器状态；需接入助手上下文。

证据：

- [Assets/m_Scripts/Tutorial/ForceTutorial/MandatoryTutorialTrigger.cs](../../Assets/m_Scripts/Tutorial/ForceTutorial/MandatoryTutorialTrigger.cs)
- [Assets/m_Scripts/Tutorial/ForceTutorial/StandaloneTutorialUI.cs](../../Assets/m_Scripts/Tutorial/ForceTutorial/StandaloneTutorialUI.cs)
- [Assets/m_Scripts/Tutorial/ForceTutorial/ForceTutorialSequenceController.cs](../../Assets/m_Scripts/Tutorial/ForceTutorial/ForceTutorialSequenceController.cs)
- [Assets/m_Scripts/Tutorial/ForceTutorial/PlayerInputBlocker.cs](../../Assets/m_Scripts/Tutorial/ForceTutorial/PlayerInputBlocker.cs)
- [Assets/m_Scripts/Microscope/CameraTryMove.cs](../../Assets/m_Scripts/Microscope/CameraTryMove.cs)

## optional_tutorial — 旧自由教程入口

状态：conditional；实机验证：否；批准用于自动指引：否。

位置：Interactor.currentTutorial 对应面板；当前主场景直接引用为 null。

**前置条件**

- 必须确认运行时存在 Tutorial 实例并注册 currentTutorial。

**桌面操作**

- 代码映射 Y 打开；已打开时方向/WASD 导航，Space/Enter 确认。

**XR 操作**

- Global/OpenTutorial 为左 secondaryButton；教程内右摇杆导航、右 primaryButton 确认。

**退出/恢复**：无已确认步骤。

**边界**

- 不要作为助手唤醒键；旧 H 文档不等于当前绑定；注册失败存在空引用风险。

观察目标：若正确注册，应显示教程列表或观察教程。

完成依据：缺少主场景直接挂载证据，不能承诺。

证据：

- [Assets/m_Scripts/Tutorial/Tutorial.cs](../../Assets/m_Scripts/Tutorial/Tutorial.cs)
- [Assets/m_Scripts/Tutorial/TutorialButtonInput.cs](../../Assets/m_Scripts/Tutorial/TutorialButtonInput.cs)
- [Assets/m_Scripts/Interactor/Interactor.cs](../../Assets/m_Scripts/Interactor/Interactor.cs)
- [Assets/m_Scripts/Microscope/CameraTryMove.cs](../../Assets/m_Scripts/Microscope/CameraTryMove.cs)

## stage_translation — 视野平移候选功能

状态：conditional；实机验证：否；批准用于自动指引：否。

位置：ScreenMove 需要挂到有样本子对象的目标上。

**前置条件**

- 确认 ScreenMove 实际实例及启用状态。

**桌面操作**

- 代码读取 Horizontal/Vertical 即 WASD/方向轴；尚未确认当前场景可用。

**XR 操作**：无已确认步骤。

**退出/恢复**：无已确认步骤。

**边界**

- 主场景无直接引用；可能位于 Prefab，不能仅凭脚本存在给玩家操作指令。

观察目标：样本在局部 XY 平面平移。

完成依据：未确认运行时挂载。

证据：

- [Assets/m_Scripts/Microscope/ScreenMove.cs](../../Assets/m_Scripts/Microscope/ScreenMove.cs)

## filter_prototype — 滤光片与二色镜候选原型

状态：unavailable；实机验证：否；批准用于自动指引：否。

位置：PCDesktop 脚本与光线示意资源；无完整入口证明。

**前置条件**

- 需验证实际 UI、拖放与槽位绑定。

**桌面操作**：无已确认步骤。

**XR 操作**：无已确认步骤。

**退出/恢复**：无已确认步骤。

**边界**

- PC_CanvasMgr.OpenApp 对应分支为空；不能指引玩家打开一个未确认存在的应用。

观察目标：存在 Filter/DichroicMirror、Slot 和光线组件代码。

完成依据：未确认可完成的学习流程。

证据：

- [Assets/m_Scripts/PCDesktop/PC_CanvasMgr.cs](../../Assets/m_Scripts/PCDesktop/PC_CanvasMgr.cs)
- [Assets/m_Scripts/PCDesktop/Slot.cs](../../Assets/m_Scripts/PCDesktop/Slot.cs)
- [Assets/m_Scripts/PCDesktop/Items/BaseItem.cs](../../Assets/m_Scripts/PCDesktop/Items/BaseItem.cs)
- [Assets/m_Scripts/PCDesktop/Items/Filter.cs](../../Assets/m_Scripts/PCDesktop/Items/Filter.cs)
- [Assets/m_Scripts/PCDesktop/Items/DichroicMirror.cs](../../Assets/m_Scripts/PCDesktop/Items/DichroicMirror.cs)
- [Assets/m_Scripts/PCDesktop/LightLine/LightLineMgr.cs](../../Assets/m_Scripts/PCDesktop/LightLine/LightLineMgr.cs)

## confocal_background — 共聚焦原理说明

状态：knowledge_only；实机验证：否；批准用于自动指引：否。

位置：知识讲解；可关联已验证的显微镜结构、NA 与空间频率内容，但没有完整共聚焦交互流程。

**前置条件**：无已确认步骤。

**桌面操作**：无已确认步骤。

**XR 操作**：无已确认步骤。

**退出/恢复**：无已确认步骤。

**边界**

- 不指引调针孔、采集 Z-stack、控制探测器或真实扫描参数；不把 NA/频谱实验说成共聚焦切片。

观察目标：仅说明激发、收集、针孔抑制离焦信号与扫描成像的概念。

完成依据：无可执行完成判据。

证据：

- [Assets/m_Scripts/Microscope/Microscope.cs](../../Assets/m_Scripts/Microscope/Microscope.cs)
- [Assets/m_Scripts/Experiment/FourierOpticsCpuSimulator.cs](../../Assets/m_Scripts/Experiment/FourierOpticsCpuSimulator.cs)

## legacy_tutor — 上一版固定题目 Tutor（已删除）

状态：deprecated；批准用于自动指引：否。

旧面板、固定题目、配置资源、编辑器创建菜单、实验接入代码及 /tutor 路由均已删除。没有可用入口或操作步骤；普通教程保留，当前仅使用极光小助手。

此 ID 仅作历史标记，防止模型继续推荐旧入口。参见 [当前问答说明](CHAT_IMPLEMENTATION.md)。

## aurora_assistant — 极光小球助手（本地第一批）

状态：static_confirmed；实机验证：否；批准用于自动指引：否。

位置：主场景左上角，运行时自动创建 Local Aurora Assistant；XR 使用跟随相机的世界空间 Canvas。

**前置条件**：主场景运行且 LocalAssistantSettings.assistantEnabled=true；实际显示与输入待实测。

**桌面操作**：默认 H 或点击小球触发随机本地问候；输入框编辑时忽略 H。问候框的「问点什么」打开问答；输入问题或选择主题示例后点击「发送」。

**XR 操作**：现有 XR UI 射线点击小球，不新增实验手柄按键绑定；头显显示与射线命中待实测。

**退出/恢复**：点击 × 关闭；点击 安静 5 分钟 暂停自动提醒，主动唤醒仍可用。

**边界**

- 本地助手源码已实现，PC/XR 运行验证与批准仍未完成。
- 第二批已实现自由知识问答，使用要求后端可用；第三批实时状态和操作导航尚未接入。

观察目标：动态极光球体、本地随机问候、累计 30 秒有效闲置后的模块主题提醒，以及第二批提交问题后返回的项目知识解释。

完成依据：消息显示/隐藏与闲置计时状态；已通过编译和纯计时检查，主场景运行检查被已有 Unity 实例占用阻止。

证据：

- [LocalAssistantController.cs](../../Assets/m_Scripts/Assistant/LocalAssistantController.cs)
- [LocalAssistantSettings.cs](../../Assets/m_Scripts/Assistant/LocalAssistantSettings.cs)
- [LocalAssistantSettings.asset](../../Assets/Resources/LocalAssistantSettings.asset)
