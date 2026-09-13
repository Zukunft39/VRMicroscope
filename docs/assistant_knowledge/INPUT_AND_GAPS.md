# 输入核查与待确认项

2026-09-13 更新：已实现动作级运行时检查。调焦说明只在观察相机激活时提供；固定桌面键位与当前配置不符时不推荐该动作。详见 GUIDANCE_IMPLEMENTATION.md；下表保留源码映射信息，映射存在不代表在所有状态生效。

依据：主场景中 CameraTryMove 的序列化键值、Interactor 的回调、Assets/Config/XRI Default Input Actions.inputactions。该输入资产 GUID 为 c348712bda248c246b8c49b3db54643f，与主场景 Interactor 引用一致。键位不是由旧 txt 文档推定。

## 当前桌面映射

| 状态 | 操作 | 输入 |
|---|---|---|
| 自由移动 | 移动/加速 | WASD / LeftShift |
| 自由移动 | 鼠标视角 | 按住鼠标右键拖动 |
| 自由移动 | 调试升降 | E / Q |
| 自由移动 | 指向位置移动 | 鼠标指向可达地面，G |
| 正确交互范围/模式 | 选择模型/部件/样本、优先取下已放置样本 | 鼠标左键 |
| 显微镜外部 | 放样本、推进观察视点 | Z，可能需分步骤按下 |
| Observing | 退出观察 | Z 或 Esc |
| 显微镜可交互状态 | 切换物镜 | R |
| 显微镜外部 | 光源开关 | B |
| 显微镜调节 | 焦点/照明输入 | 按住 X＋左右/上下方向键 |
| Observing | 粗细调切换 | Tab |
| 已有效注册自由教程时 | 打开教程 | Y，入口条件待确认 |
| 自由教程内部 | 导航/确认 | WASD/方向键；Space/Enter/鼠标左键 |
| SNOM 选探针 | 选择/安装 | 1/2/3，E |
| SNOM ReadyToStart | 启动 | Space |
| SNOM PrincipleTour | 前后阶段/播放暂停/重播/退出 | 左右方向键 / Space / R / Esc |

XR 名称以动作路径为准：左 triggerButton 放置/退出观察，右 triggerButton 选择/取样，右 primaryButton 切物镜，右 secondaryButton 切灯，右 thumbstickClicked 切粗细调，左 primaryButton 修饰＋右 primary2DAxis 提供调焦/照明。不同控制器的物理 A/B/X/Y 标识需按实际设备确认。

NA 摇杆读取实验控制器 ResolveSliderAction 所选动作的 x 分量，不要把桌面 X＋方向键路径未经验证地承诺在独立 NA 模块也生效。优先使用可见滑块。

## 已发现的冲突

1. `PC下移动操作方式.txt` 记载 R 取样、E 观察、Q 开灯、T 换物镜、M/B/N 调焦、H 教程；与当前 CameraTryMove 及场景绑定不同。不要直接给模型使用该旧文档。
2. `按键事件绑定.txt` 记载左 secondary 切粗细调；当前 Observing/ChangeMode 绑定右 thumbstickClicked，左 secondary 实际用于教程。
3. 旧说明有 F 切换样本贴图；当前 ShowObject 没有对应输入实现，不能列为现有功能。
4. 当前主场景 Move 组件 m_Enabled=0；不能承诺 XR grip 移动已可用。CameraTryMove 为启用状态，但 activeInHierarchy 和运行时切换尚未检查。
5. 主场景 Interactor.currentTutorial 与 tutorialButtonInput 的直接引用为 null，Tutorial 脚本无直接挂载；可能依赖 Prefab/运行时注册。Y 菜单必须实测，不能推荐为可靠入口。
6. ScreenMove 无主场景直接脚本引用；Prefab 或运行时绑定需确认。WASD 视野平移暂不纳入正式指引。
7. SNOM 通过 RuntimeBootstrap 查找 TDs_edited_UnityVeryLowPoly 并创建控制器；这是有代码入口的功能，但需要运行确认创建和 UI 交互成功。
8. 共聚焦部件 Focus Adjustment 的场景说明提到 Z-stack 概念，不能据此声称项目已实现 Z-stack 采集按钮。
9. SNOM 文档提到可选 Raw/2Ω/3Ω 按钮，代码审阅未确认相应运行时控件，不能推荐点击。
10. 旧内嵌 Tutor 的脚本、资源与接入已删除；新极光助手默认 H/点击唤醒，第二批通过「问点什么」进入知识问答，窗口打开期间拦截实验快捷键。运行与头显验收仍待完成，实时操作导航未接入。旧文档的 H 教程说明不代表当前映射。

## 场景位置边界

目前只确认仪器对象/部件名与实验入口关系，未实测从出生点的方位、行走路径、是否可见和是否有中文标牌。助手不能说“左转第二张桌子”“前进两米”。先通过屏幕真实标签、选中部件和当前界面定位；后续增加区域 ID 或导航锚点后再描述路线。

## 局外运行验收

- 逐设备检查上述按键，记录是否被强制教程、独立实验或 SNOM 模式屏蔽。
- 完整走通样本拾取→放置→观察→调焦/照明/物镜→退出→取下。
- 验证 Normal→PreAssembly→SuperAssembly→部件查看→NA/SF→返回。
- 验证五个部件名称及两个实验按钮；确认其余部件不出现虚假实验入口。
- 验证空间频率有/无样本两种状态，保持样本不变时切换频率。
- 验证 SNOM 靠近与点击入口、选择探针、安装完成、启动、八阶段与退出；检查 E/R/Space 是否触发其他模块。
- 验证教程锁定期间可用操作，以及自动演示/阅读期间应暂停闲置提醒。
- 记录在每个状态下真正允许的动作；未来 CURRENT_CONTEXT.allowed_actions 由程序产生。

编辑器自动保存、模型替换、场景搭建、调试任务按钮，以及样例/测试场景，不作为玩家可学习交互推荐。
