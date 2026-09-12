# 项目知识与讲解边界

版本：2026-09-12。来源为本项目源码与既有设计说明；不是硬件厂商认证或实测光学报告。

供回答引用的稳定知识主题 ID：显微镜结构与基础操作 = `microscope_basics`；NA = `numerical_aperture`；空间频率、衍射与结构光 = `spatial_frequency`；Confocal microscopy = `confocal_background`；THz s-SNOM = `snom`；助手问答能力 = `assistant_usage`。仅提供部分章节时，只允许引用实际提供的主题。

## 助手问答能力

小球的问候、闲置提醒和外观动画由本地程序实现，不调用模型。第二批已实现项目知识问答窗口，支持中文文字问题、主题示例、基础英文屏幕键盘、取消与新对话。只有提交问题才通过本地后端调用 DeepSeek，实际使用要求后端已启动并配置好密钥。程序不自动修改实验，不提供语音识别，也没有接入实时参数或具体操作导航。屏幕键盘不是中文拼音输入法；PC 可使用系统中文输入法。模型不能读取玩家电脑或屏幕，也不能知道未由程序提供的实时实验状态。

依据：`Assets/m_Scripts/Assistant/AssistantChatPanel.cs`、`Tools/ai_tutor/assistant_chat.py`、`CHAT_IMPLEMENTATION.md`。以上为源码实现范围，不代替主场景和设备运行验收。

## 显微镜结构与基础操作

已配置部件名称：Objective Lens（物镜）、Upper Optical Assembly（上部光学组件）、Stage Position Control（载物台定位控制）、Optical Breadboard（光学平台）、Focus Adjustment（调焦部件）。结构查看与两个参数实验可相互关联。部件说明存在，不代表每个部件均有可操作实验。

物镜用于聚焦与光收集；NA 与倍率是不同属性。调焦表达轴向相对定位，不应解释为玩家改变了物镜固有焦距。载物台定位不是共聚焦扫描器。Upper Optical Assembly 的内部元件未逐个辨认，不得随意指定为针孔或二色镜。

依据：`Assets/m_Scripts/Experiment/ConfocalComponentBackground.md`、主场景部件说明、`Microscope.cs`。

## NA

NA=n sin(theta)，theta 是光轴与收集光锥边界的半角；同一介质中增大 NA 对应更大半角。项目使用空气 n=1 的默认教学设置，NA 范围 0.03–0.95。具体当前参数由 CURRENT_CONTEXT 提供，不把默认值当作当前值。

亮度、清晰度与深度容差是教学映射；近似倍率由七个参考档位插值。不能声称 NA 唯一决定倍率，也不能把视觉差异当作实测分辨率或景深数值。

依据：`NumericalApertureInteractionDesign.md`、`NumericalApertureExperimentController.cs`。

## 空间频率、衍射与结构光

High/Middle/Low 为 250/125/62.5 lines/mm。在波长固定时，光栅间距 D 越小，一阶衍射角越大；项目用 sin(psi)=lambda/D、后焦面间距 f tan(psi) 计算示意值。

样本频谱来自选定纹理、窗口处理与 CPU 二维 FFT，默认 128×128。保持样本不变，只切换光栅载频时，样本本身的频谱不应随之改变；侧带位移和频域支持范围可变化。无样本时样本派生频谱面板为空。

中间面板展示单一方向的分离侧带，最后面板展示三个方向的支持域融合。不是完整多相位 SIM 重建，更不是共聚焦针孔滤波。White Light 与 Laser Excitation 是教学比较模式。

依据：`SpatialFrequencyInteractionDesign.md`、`SpatialFrequencyExperimentController.cs`、`FourierOpticsCpuSimulator.cs`。

## Confocal microscopy

可讲解共聚焦基础：通过聚焦激发与收集、共轭像面针孔抑制离焦信号，结合扫描形成图像，轴向采集可形成 Z-stack。以上为背景概念。

当前目录没有已确认的针孔调节、检测器控制、Z-stack 采集或完整共聚焦扫描任务。玩家希望亲手做这类操作时，应说明当前没有对应可操作模块；可推荐结构或 NA 作为相关基础，但明确不是等价替代实验。

依据：`ConfocalComponentBackground.md`。更具体的定量问题若材料中没有答案，则拒答。

## THz s-SNOM

教学操作是选择并安装探针，然后启动系统。系统随后自动讲解：

| 顺序 | 标题/概念 | 可引导观察 |
|---|---|---|
| 1 | Broadband THz Pulse Generation | 脉冲产生与时域波形示意 |
| 2 | Beam Steering and Focusing | THz 引导与聚焦路径 |
| 3 | AFM Distance Feedback | AFM 读出与距离反馈示意 |
| 4 | Tapping and Near-Field Coupling | 探针敲击及局域场示意 |
| 5 | Weak Scattering over Background | 近场信号与背景对比 |
| 6 | Harmonic Demodulation | 高次谐波抑制背景的原理 |
| 7 | Raster Scan | 图像逐点形成 |
| 8 | Correlated Results and Local Spectrum | 形貌、幅度、相位与局部光谱示意 |

学习这些主题时，通过 snom_entry→snom_probe→snom_start→snom_controls 引导。演示自动推进，因此不能说“现在就是第几阶段”；根据实时 CurrentStage 或玩家看到的标题，提示用 Previous/Next 到达对应阶段。

AFM 读出激光不等于 THz 激发光源，四象限读出不等于 THz 探测器。探针局域作用与远场物镜成像不同；3x/2x/1x 是相对细节示意，不是物镜倍率。光路与探针动画经过教学简化；结果图由程序生成，不是测得的样本材料数据。禁止杜撰实际半径、绝对分辨率或已验证仪器光路方向。

依据：`SNOMInteractionDesign.md`、`SNOMDemonstrationController.cs`、`SNOMDemonstrationGraphic.cs`。
