# 第一批：本地极光助手

本文件记录第一批：小球动画、按键/点击问候、30 秒闲置提醒。这些本地行为不发出网络请求。后续第二批已实现「问点什么」自由知识问答，启动与配置见 CHAT_IMPLEMENTATION.md。

## 使用方式

进入 `Assets/Scene/Scene_Laboratory/MainScene_Labortory.unity` 并运行。脚本自动创建 `Local Aurora Assistant`，不需要手动往场景挂组件；离开场景后销毁。

- PC：左上角显示极光小球和 `H · 唤醒助手`。按 H 或点击小球显示本地随机问候；连续触发有 0.6 秒防抖。
- XR：使用跟随主相机的世界空间 Canvas，显示在视野左上方。使用现有 XR 射线点击小球，不占用现有实验手柄按键。需要场景的 XR UI 输入系统和射线 UI 交互已开启。
- 对话显示在上方，默认至少 10 秒；较长文本按阅读长度延长。鼠标或射线停留在对话框上时保持显示，离开后至少保留 3 秒。
- 点 `×` 关闭当前消息；点 `安静 5 分钟` 暂停自动提醒，H 或点击仍可主动唤醒。
- 启动时只显示小球，不自动弹出问候。第二批新增问候框内的「问点什么」，点击后进入独立问答窗口；打开窗口不调用 API。

## 美术与文本

图标保持原创的半透明球体、三层连续环绕极光和轨道粒子，只参考黑白配色增强辨识度。球心使用低不透明度的中性白色，轮廓为黑白细描边；极光采用柔和白光、暗色衬线和白色细光丝，不使用开口圆环或斜线标志。唤醒文字保持浅白配黑描边。图形由 AuroraOrbGraphic 直接绘制，无新增图片或外部 Shader 依赖。

内置 16 条问候、8 个提醒句式，与实验室、NA、空间频率、SNOM 的主题组合。连续问候、提醒句式与同模块主题避免直接重复。文案只邀请观察，不杜撰按钮、任务完成或玩家能力。共聚焦只称背景原理，不声称有针孔操作。

## 闲置与暂停规则

默认每累计 30 秒有效闲置触发一次。键盘按住、鼠标点击/明显移动/滚轮，以及 XR 扳机、握持、按钮与摇杆输入都会重置计时。头显位姿变化不直接计为操作，以避免追踪抖动让提醒永远不出现。

以下时间不计入闲置：应用失焦、游戏暂停、输入框编辑、助手消息阅读、强制/可选教程、视频播放、部件说明阅读、实验切换、SNOM 探针安装、SNOM 自动原理演示及部件说明。暂停结束后重新累计 30 秒，不立即补发提醒。

NA/空间频率实验已进入时，底层保留的部件选中状态不会阻止实验提醒。SNOM 演示暂停且没有阅读部件时，可恢复闲置提醒。

默认持续按上述安静间隔提醒；可设置每段闲置最多提醒几次，发生新操作后重新计数。提醒显示本身属于阅读时间，因此不是不顾对话是否在显示就每隔墙钟 30 秒强制弹一次。

## Inspector 配置

选择 `Assets/Resources/LocalAssistantSettings.asset`：

| 字段 | 默认值 / 用途 |
|---|---|
| Assistant Enabled | 启用；修改后重新运行场景 |
| Activation Key | H，可改为不与现有操作冲突的键 |
| Idle Seconds | 30 秒 |
| Message Seconds | 最少显示 10 秒 |
| Max Reminders Per Idle Period | 0 表示持续提醒，正整数限制每段闲置次数 |
| Reduced Motion | 关闭；启用后使用静态球体，减少动态效果 |
| Chinese Font | 已绑定项目中文字体 |
| Greetings | 可编辑的随机问候列表 |
| Reminder Templates | 可编辑句式，`{0}` 替换为当前模块主题 |

主题清单在 `LocalAssistantController` 的 LabTopics、NaTopics、SfTopics、SnomTopics 中。新增未知模块不会自动被推断为有对应操作。

接口：`Activated` 保留为本地唤醒事件；第二批问答面板已使用 `ReadingOrTyping` 暂停提醒，并用 `ReportActivity()` 通知计时器。只有问答窗口的发送操作会调用 API，不会自动执行实验动作。

旧内嵌 AI 已删除：AITutor 脚本、配置、固定题目资源和编辑器创建菜单均已移除，NA/空间频率控制器不再包含旧 Tutor 调用或专用状态适配接口。普通教程与显微实验继续保留，仅使用极光小助手。

## 验证结果与重跑

已通过整个项目运行时和编辑器代码编译，以及 26 项独立计时检查（首次阈值、阅读/教程暂停、操作重置、提醒次数限制与重新启用等）。运行：

```powershell
python Tools/local_assistant/verify.py
```

新增显式 Unity 批处理场景检查入口 `LocalAssistantSmokeCheck.Run`，检查主场景自动创建、问候、字体覆盖、网格及关闭，并输出实际 UI 渲染图到 `Temp/LocalAssistantVerification/assistant-preview.png`。

本次尝试运行主场景检查时，Unity 报告项目已由另一个实例打开，因此检查未执行，也没有生成已验收的运行截图。没有关闭现有编辑器；不能将脚本编译通过当作 PC 或头显实测通过。后续关闭本项目编辑器后，可在单独批处理进程运行：

```powershell
& 'G:/Unity/2021.3.34f1c1/Editor/Unity.exe' -batchmode -projectPath G:/VRMicroscope -executeMethod LocalAssistantSmokeCheck.Run -logFile G:/VRMicroscope/Temp/LocalAssistantVerification/unity-smoke.log
```

手动验收：主场景 H/点击问候、中文无缺字、消息不穿透点击到样本、30 秒闲置、操作后重新计时、教程/演示期间不提醒、NA/SF 主题切换、关闭与静音、再次进入场景没有重复助手。XR 还需用实际头显确认双眼可读性、位置和射线命中。

交互目录仍保留 runtime_verified=false、approved_for_guidance=false；只把小助手从规划更新为已有源码实现，不擅自批准用于模型操作指引。
