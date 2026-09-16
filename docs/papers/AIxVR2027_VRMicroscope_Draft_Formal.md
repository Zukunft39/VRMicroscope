# VRMicroscope：融合交互式显微仿真与任务状态感知 AI 指引的虚拟现实教学系统

**作者与单位：**匿名评审稿


> **【编辑标注说明｜提交前移除】** 本稿中的插图占位、待核实研究信息与补充评价方案均为编辑标注，不代表已制作的图片或新增实验结果。图 1–4 为建议优先制作的图，图 5 为有数据后才制作的可选评价图。具体分镜、图注与往届论文依据见 [插图制作说明](AIxVR2027_Figure_Plan.md)。

## 摘要

显微镜教学要求学习者将仪器结构、操作过程与观察结果联系起来，但真实高端仪器的使用时间、操作风险与教学资源通常受到限制。本文提出 VRMicroscope，一个将交互式虚拟显微实验与任务状态感知 AI 指引结合的虚拟现实教学系统。VR 环境支持样本拾取与放置、观察视点切换、粗细调焦、照明与物镜调整、部件展开查看，并通过关联模块展示数值孔径、空间频率与结构光以及太赫兹散射型近场光学显微镜（THz s-SNOM）的核心概念。系统明确区分教学视觉映射、简化数值计算与经过标定的光学测量。AI 助手结合项目知识、当前界面状态和可用动作目录，由大语言模型生成解释或候选指引，再由程序校验动作标识并检查响应是否仍适用于当前状态；空间导航基于实时场景几何生成目标标记，中英文语音输入则先形成可编辑文本后再提交。评价采用实现级一致性检查与探索性用户研究两部分。19 名参与者中有 18 名完成核心任务流程（94.7%），整体系统可用性量表（SUS）均值为 82.4/100；NASA-TLX 的描述性结果显示，付出努力（44.7）和心理需求（42.9）是相对较高的工作负荷维度，而挫败感均值为 24.2。由于研究采用单组探索性设计，本文将这些结果解释为工作流可行性与感知可用性的证据，而不据此推断 AI 带来的因果学习增益。本文的主要贡献是将显微实验操作、具有明确科学边界的教学仿真与受当前任务状态约束的对话指引连接到同一套可操作学习活动中。

**关键词：**虚拟现实；显微镜教学；交互式仿真；任务状态感知；大语言模型；语音输入。

## Abstract

Microscopy education requires learners to connect instrument structure and operating procedures with changes in observable results, yet access to advanced instruments is often constrained by limited training time, operational risk, and teaching resources. This paper presents VRMicroscope, a virtual-reality learning system that integrates interactive microscopy simulation with task-state-aware AI guidance. The VR environment supports specimen pickup and placement, observation viewpoints, coarse and fine focusing, illumination and objective adjustment, and exploded component inspection. Linked modules illustrate numerical aperture, spatial frequency and structured illumination, and the basic workflow of terahertz scattering-type scanning near-field optical microscopy (THz s-SNOM). The system explicitly distinguishes instructional visual mappings and simplified numerical models from calibrated optical measurement. The AI assistant combines project knowledge, current interface state, and a catalog of available actions: a large language model generates explanations or candidate guidance, while application code validates action identifiers and rejects recommendations that no longer match the current state. Spatial navigation creates target markers from live scene geometry, and Chinese/English speech input produces editable text before submission. Evaluation combines implementation-level conformance checks with an exploratory user study. Eighteen of 19 participants completed the core task sequence (94.7%), and the whole-sample mean System Usability Scale (SUS) score was 82.4/100. Descriptive NASA-TLX results showed that Effort (44.7) and Mental Demand (42.9) were the largest reported workload dimensions, whereas mean Frustration was 24.2. Because the study used a single-group exploratory design, these outcomes are interpreted as evidence of workflow feasibility and perceived usability rather than as causal evidence of learning gains attributable to AI. The main contribution is an integrated microscopy-learning environment in which physical interaction, scientifically bounded instructional simulation, and conversational guidance share an explicit representation of what the learner can currently do.

## 1. 引言

显微镜实训不仅要求学习者辨认三维仪器，还需要完成放样、调焦、照明调整和物镜切换，并解释观察视野为什么发生变化。虚拟实验室可以为这些操作提供可重复探索的环境，但必须说明哪些反馈对应真实原理，哪些只是为了便于教学而设计的表达。

VRMicroscope 通过两个互补层次处理这一问题。实验环境提供物件、控制方式、视觉反馈和实验界面；AI 助手将学习者对这些体验的提问连接到项目知识和当前可进入的学习活动。例如，用户正在查看上部光学组件时，可以先询问其功能，再进入关联的数值孔径实验；如果用户仍在房间其他位置，则首先需要位置指引。

因此，系统的核心问题是维持仪器界面、仿真科学边界和对话建议之间的一致性。真实共聚焦仪器的介绍可能涉及针孔调整或 Z-stack 采集，但本项目没有提供这些已确认的操作。另一方面，如果助手只知道“项目包含 NA 实验”，却不知道当前部件面板已经显示实验入口，也可能错误地让用户退出查看。

本文的主要贡献为：

1. 构建连接样本操作、调焦与照明反馈、结构认知及三类进阶教学模块的显微实验环境，明确各项输入、输出和建模边界。
2. 设计结合项目知识、当前部件说明、可用动作目录、响应校验、空间标记与确认式语音输入的 AI 指引架构。
3. 通过实现级一致性检查与一项包含 19 名参与者的探索性用户研究评价系统，分别报告任务完成、感知可用性和主观工作负荷，并避免将这些指标解释为未经对照验证的学习增益。

系统面向操作熟悉与概念探索。迁移到真实仪器的能力、定量成像能力，以及 AI 带来的额外学习收益，需要相应的评估证据支持。

## 2. 相关工作与研究定位

生成式 AI 与沉浸式教学的结合已经开始从“通用聊天窗口”转向与具体学习任务相连的交互设计。Chheang 等在 AIxVR 2024 的解剖学 VR 研究中比较了不同虚拟助手呈现形式，并将任务表现、SUS、NASA-TLX、存在感与访谈反馈作为不同层次的评价指标 [1]。Geris 和 Alce 在 AIxVR 2026 的语音 XR 辅导原型中，则通过结构化交互会话和自动日志重点评价响应延迟、反馈长度与运行成本 [2]。同届的 SpatialTutor 将物体感知、空间提示与 LLM 支持整合到混合现实医疗流程训练中 [3]。这些工作共同表明，AI+XR 系统的评价指标应直接对应具体贡献：交互性能、任务结果、主观体验和学习效果不能相互替代。

本文关注显微镜学习活动与指引之间的对应关系。一个操作不仅需要在当前界面中真实可用，其反馈也需要在明确的仿真科学边界内解释。因此，本文将 AI 视为实验环境中的受约束辅导层，而不是能够自主控制仪器的智能体；评价时也将实现级约束行为与用户层面的可用性结果分开报告。

定量共聚焦显微方法是本项目的重要科学参照 [4]。本项目介绍相关光学原理，但没有实现经过标定的共聚焦采集链路。数值孔径与空间频率教学资料 [5]、[6] 为模块的概念解释提供参考；遵循这些基本原理，并不意味着项目中的所有图形映射都已通过真实仪器测量验证。

## 3. VR 实验环境与仿真边界

### 3.1 实验室组织与交互状态

Unity 实验室支持桌面和 XR 交互。主交互状态包括 `Roaming`、`Observing` 和 `Tutorial`；部件查看及各实验控制器另行维护局部状态。视点、物件激活、输入权限和界面可见性共同决定用户当前能够进行的学习活动。

主状态切换延后到帧边界执行，教程输入限制则由独立检查处理。这些机制组织切换并减少同一输入触发多个行为的情况。AI 对话框打开时也会阻止常规实验操作，因此操作文案要求用户先关闭对话框。

整体学习内容可以连接为：处理样本、观察图像、调整仪器、查看部件、进入相关实验。这是一条可供探索的学习路径，并不表示所有用户都必须完成同一套强制教程。

> **【插图 1 占位｜优先制作｜建议通栏】** 从当前项目截取实验室全景作为主图，框选显微镜站、SNOM 装置及样本位置；配三个局部视图显示样本观察、部件展开和 SNOM 探针安装。统一使用 A–D 标注对应学习活动，保留一处小助手入口以说明它位于同一环境。不要使用 Unity 编辑器边框或无关工具面板。
>
> **拟图注：** 图 1. VRMicroscope 学习环境与核心活动。学习者在同一实验室中进行样本操作、显微观察、部件探索和 SNOM 流程演示，并通过助手获取相关解释与指引。

**表 1：项目现有活动的输入、输出与教学边界。**

| 活动与玩家输入 | 实际输出 | 预期教学联系 | 建模边界 |
|---|---|---|---|
| 拾取、放置、观察和取出样本 | 运行时样本的父对象、姿态、可见性与观察环境变化 | 建立样本放置与仪器观察的联系 | 脚本化对象操作，未经接触力学验证 |
| 粗细调焦与切换物镜 | 随焦点参数变化的显隐、透明度、渲染反馈及视野大小 | 先搜索可观察区间，再精细调整 | 人工设定焦点区间，没有共聚焦点扩散函数计算 |
| 调整照明 | 光锥线宽与样本着色器亮度输入变化 | 比较照明设置与观察效果 | 无量纲显示增益，非标定照度 |
| SuperAssembly 与选择部件 | 展开布局、部件说明及关联实验按钮 | 连接结构、功能与学习活动 | 示意性分离，不评价真实机械装配 |
| 调整数值孔径 | 光锥几何与样本外观示意变化 | 探索收集角与 NA | 固定介质教学模型，NA 不唯一决定倍率 |
| 选择空间频率与照明 | 光栅/衍射示意及源于样本纹理的傅里叶显示 | 连接周期结构、衍射与频谱支持范围 | 简化结构光说明 |
| 选择 SNOM 探针并控制演示 | 安装、敲击、扫描动画及示意图像 | 理解近场显微工作流程 | 使用合成输出的流程演示 |

### 3.2 样本操作与观察

拾取样本时，程序创建一个附着于手部的运行时副本。放置操作检查距离和手中实际带有指定标签的对象，再将样本移入显微镜观察环境，统一其姿态与比例。取出操作将其恢复为激活的手持对象；必要时，程序重新计算受焦点影响的可见性。

样本归属、可见性和用户观察模式分别维护。已经放置的样本可能因为调焦参数超出区间而被隐藏，隐藏不代表样本消失或回到样本柜。`Observing` 表示用户交互和视点状态，与手持或放置状态共同决定观察行为。

渲染设置使用不同的场景/观察视点对象以及显微图像相机，并通过渲染纹理在仪器屏幕中显示观察内容。切换物镜会调整图像相机的正交尺寸。这一结构在实验室中提供独立的仪器观察视图。

### 3.3 调焦、物镜与照明反馈

调焦练习通过较大的粗调步长寻找样本可观察区间，再以细调改善显示。设 $v$ 为无量纲滑块值，$j$ 为物镜索引，$f_j$、$r_j$、$c_j$ 分别为该物镜的焦点中心、可见区间半宽和完全不透明区间半宽。定义 $e_j=|v-f_j|$，样本在 $e_j\leq r_j$ 时激活，其透明度为：

$$
\alpha_j(v)=
\begin{cases}
1,&e_j<c_j,\\
1-\dfrac{e_j-c_j}{r_j-c_j},&c_j\leq e_j\leq r_j.
\end{cases}
$$

区间外样本被隐藏；区间内另以 $300e_j/r_j$ 设置渲染焦点参数。当前四档物镜标签为 5、10、50、100，可见区间半宽依次为 0.17、0.13、0.07、0.04。随着区间缩小，学习者需要更细致地调整焦点。代码配置下粗细调滑块增量的比例为 25:1。这些参数定义视觉反馈和操作灵敏度，不是以微米标定的位移或共聚焦针孔传递函数。

切换物镜时，转盘旋转并改变图像相机的正交尺寸，使玩家比较观察尺度。照明调整同时改变光束线宽和样本着色器的无量纲亮度输入。调焦、视野和照明分别提供可操纵的反馈通道，帮助学习者区分“样本是否进入观察区间”“显示尺度”和“显示亮度”。焦点中心、光强增益及相机尺寸属于软件配置，后续物理标定应另行进行。

### 3.4 结构认知与教程限制

SuperAssembly 根据部件中心相对于模型中心的方向展开部件，并允许配置位移幅度和方向。正常与展开模型按模式切换，学习者可在展开布局中选择部件，查看名称、功能说明和关联实验。

上部光学组件关联数值孔径实验，物镜关联空间频率实验，两者位于同一显微镜学习区域。部件名称、说明和实验入口共同构成 AI 可读取的学习上下文，使“这个部件有什么作用”与“如何体验相关实验”可以在同一界面连续完成。

教程通过输入条件和步骤检查限制流程推进。完成记录表示程序要求的操作已经满足；概念理解需要通过独立学习测量评价。

### 3.5 数值孔径实验

NA 模块提供 0.03–0.95 的滑块范围，在固定折射率 $n=1$ 下使用：

$$
\mathrm{NA}=n\sin\theta,\qquad \theta=\arcsin(\mathrm{NA}/n).
$$

$\theta$ 为光锥半角。界面同步更新半角、全孔径角和光锥几何，使收集角的变化可直接观察。理论关系依据显微光学 [7]，呈现方式参考光锥教学资料 [5]。玩家比较不同设置时，可以将数值变化与几何变化对应起来。参考倍率锚点和样本外观变化用于辅助说明；NA 与倍率仍是不同属性，外观变化不作为亮度、景深或分辨率的定量测量。

### 3.6 空间频率与结构光教学显示

空间频率界面提供 250、125、62.5 线/mm 三档光栅及照明选择。设空间频率为 $\nu_g$、示意波长为 $\lambda$、示意焦距为 $f_{\mathrm{obj}}$，图示采用：

$$
D=\frac{1}{\nu_g},\qquad
\sin\psi=\frac{\lambda}{D},\qquad
\rho=f_{\mathrm{obj}}\tan\psi.
$$

其中 $\rho$ 表示后焦平面偏移，区别于下文的样本频谱。小角度下 $\rho/f_{\mathrm{obj}}\simeq\lambda/D$，可将光栅间距与衍射位置联系起来 [6], [8], [9]。

样本纹理先转为标量场，对选中样本应用可分离的 Hann 型窗以减弱边界效应 [10]，随后乘以 $(-1)^{x+y}$ 将零频移到显示中心，并执行按行、按列的 radix-2 FFT [11]。默认尺寸为 $128\times128$，支持 64–256 的二次幂尺寸。幅值经对数压缩、归一化和显示增强后形成样本频谱面板。

记样本频谱为 $O(\mathbf{k})$，照明载波为 $\mathbf{k}_0$，调制度为 $\mu$，照明相位为 $\varphi$。标准 SIM 的理论背景可概括为 [12], [13]：

$$
G_{\varphi}(\mathbf{k})=
H_{\mathrm{opt}}(\mathbf{k})
\left[
O(\mathbf{k})+
\frac{\mu}{2}e^{i\varphi}O(\mathbf{k}-\mathbf{k}_0)+
\frac{\mu}{2}e^{-i\varphi}O(\mathbf{k}+\mathbf{k}_0)
\right].
$$

$H_{\mathrm{opt}}$ 为理论光学传递函数。当前混合面板仅演示一对移位侧带，其显示前幅值为：

$$
B(\mathbf{k})=
P_{\mathrm{demo}}(\mathbf{k})
\left|
\frac{\mu}{2}
\left[O(\mathbf{k}-\mathbf{k}_0)+O(\mathbf{k}+\mathbf{k}_0)\right]
\right|.
$$

$P_{\mathrm{demo}}$ 是代码定义的非负示意 pupil 权重，不等同于实测 $H_{\mathrm{opt}}$。$B$ 还经过显示映射，并非直接显示复数频谱。恢复支持面板则将 0°、60°、120° 的中心与移位覆盖组合，作用于经过显示处理的样本频谱幅值。两类面板帮助比较“频谱搬移”和“多方向覆盖”，没有执行完整多相位采集、相位分离和定量重建。改变载波会改变侧带和支持范围，而未变化样本的源频谱保持其原有含义。

> **【插图 2 占位｜优先制作｜建议通栏】** 制作三行对比面板：(a) 同一物镜、样本和照明下的两种调焦状态；(b) 两个 NA 设置及对应光锥，显示界面真实参数；(c) 同一样本下两档载波的源频谱、混合侧带和恢复支持显示。使用实际运行截图，标注每组唯一变化的输入，不能把合成面板称为实测图像。空间不足时保留 NA 与频谱两行。
>
> **拟图注：** 图 2. 教学输入与可观察反馈的对应关系。调焦、NA 和照明载波分别改变样本显示、光锥几何及侧带/支持范围；光学反馈包含人工映射和简化数值显示。

### 3.7 THz s-SNOM 演示

SNOM 模块提供“选择探针—安装—启动系统”的操作流程，随后演示 THz 产生与传播、探针敲击、近场耦合、背景抑制及栅格扫描。THz 时域光谱和近场显微方法为教学内容提供理论背景 [14]–[17]。

程序通过周期性探针运动、扫描光标、示意波形和表面/近场图像连接阶段与观察结果。高次谐波内容用于解释在探针敲击频率的整数倍处提取散射信号分量，以帮助区分近场贡献与背景 [16], [17]；当前界面展示这一概念，不对实测信号执行谐波解调。

三种探针的 3×、2×、1× 标签表示相对细节的教学比较。波形和图像均为程序生成示例，支持流程理解与概念观察；任意像素的实测波形、实验幅相反演和介电常数求解不属于该模块。设备模型的来源与部件对应关系需与作者持有的项目资料核对。

## 4. 与实验环境结合的 AI 指引

### 4.1 对话入口与知识数据流

动态小助手在屏幕角落提供问答入口，本地问候与空闲建议用于提示学习主题。明确提交的问题通过 Python 网关发送到 DeepSeek。网关将人工整理的项目知识、交互描述和本次状态快照组合为上下文；模型无需访问项目文件或观察画面即可获得相关部件与可用活动的信息。

系统使用固定知识包注入和共享动作目录。知识包支持概念解释，动作目录规定具体控制步骤，两者通过当前实验状态连接。响应携带提示词和知识版本，便于将后续记录对应到实际运行配置。语音转写是独立入口：录音先形成文字草稿，用户确认后才进入相同问答流程。

> **【插图 3 占位｜优先制作｜建议通栏】** 绘制原创矢量架构图，分为 Unity、Python 网关、外部 API 三个区域。主线为“问题+当前快照→候选生成→动作/格式校验→标准文案→语义审核→客户端状态复核→提示卡或标记”；格式失败仅允许一次返回候选生成的修复箭头。语音支线为“麦克风→Groq 转写→可编辑草稿→用户确认”。另画“玩家实际操作→VR 状态更新”的闭环，不能画成模型直接控制仪器。
>
> **拟图注：** 图 3. 状态约束的问答架构。模型选择候选解释或指引，程序负责可用性校验、操作文案和响应时效；格式修复保持同一状态权限，实验操作由玩家完成。

### 4.2 任务状态与可用活动

**表 2：VR 活动与 AI 上下文的对应关系。**

| VR 情境 | 提供的信息 | 支持的指引 |
|---|---|---|
| 样本操作 | 手持对象、放置状态、可用动作 | 拾取、放置、观察或取出 |
| 显微观察 | 模式、观察点、可用控制 | 调焦、亮度或物镜操作说明 |
| 部件查看 | 名称、说明、关联实验、可用启动动作 | 部件解释及实验入口 |
| NA/空间频率 | 当前模块参数和控制 | 参数含义与当前操作 |
| SNOM | 阶段、探针、播放及部件模式 | 当前流程和前置条件 |
| 空间定位 | 可用目标、世界位置、玩家朝向及尺度 | 仪器区域或具体样本标记 |

快照不含焦点滑块值、焦点误差、当前物镜索引、图像质量测量或粗细调选择。因此，调焦指引提供操作方法与观察目标，由用户比较显示变化；助手不能计算保证改善清晰度的调整方向。当前模块之外的默认字段不作为实验读数。

### 4.3 受约束生成与一次格式修复

设 $\mathcal{C}$ 为共享动作目录，$P(a,s)$ 为状态 $s$ 下动作 $a$ 的可用性条件，则：

$$
\mathcal{A}(s)=\{a\in\mathcal{C}\mid P(a,s)\}.
$$

Unity 根据界面、控制器、距离和输入绑定产生 $\mathcal{A}(s)$。DeepSeek 返回解释、指引、澄清或拒答；操作指引每次选择一个允许动作，位置指引则选择一个当前目标。后端验证动作、交互及主题的对应关系，客户端在显示前再次验证。

合法操作的模型文案被替换为目录中的标准步骤和观察提示。因此，已经确定会被替换的文字长度或箭头格式不会使合法操作失败。如果候选 JSON、字段或动作选择未通过结构校验，系统按原问题和同一快照最多重新生成一次。重生成仍接受完整权限检查；超时和上游访问异常不按格式错误重试。

非拒答候选随后接受一次语义审核，检查回答范围、事实支持和操作是否符合问题目标。生成器与审核器采用相同模型配置，因此审核属于附加检查，而非独立正确性保证。生成格式失败、审核格式异常、访问权限和超时通过不同服务提示报告，正常语义拒答与系统故障分开显示。

### 4.4 连续学习与响应时效

在部件界面中，“这个部件”由选中名称和说明解析；要求体验时，优先选择当前可用的关联实验。一般性的“下一步”优先推进当前学习路径，例如预装配后的展开、展开后的部件选择。退出用于明确退出、切换模块或必要的前置路线，而不是默认建议。

每次请求保存状态序列化文本和快照标识。操作回答到达时，客户端重新读取状态，比较标识、内容和动作权限；过期建议不作为当前指令显示。对话框关闭后，提示卡继续提醒玩家并周期性检查上下文。该机制保持实际控制权在玩家手中，提示出现本身不表示操作完成。

### 4.5 仪器与样本空间定位

在可用的自由观察状态下，部件、NA 和空间频率指向同一显微镜站，SNOM 指向另一套装置。最新目录还提供红、绿、蓝、黄四种样本目标；只有本次快照实际包含的对象可以被标记。具体样本与样本柜是不同目标，当前能力是对象定位，不是任意家具或路线搜索。

设目标相对玩家的水平位移为 $\Delta_h$，每米场景单位为 $u$，显示距离为：

$$
d=\|\Delta_h\|/u.
$$

相对水平视线的有符号角 $\beta$ 按前方（$|\beta|\leq45^\circ$）、后方（$|\beta|\geq135^\circ$）及其余左/右区间离散。后端提供几何关系，客户端应用响应时再次按当前场景计算。

成功创建轮廓和光柱后，助手才确认目标已标记。显示距离以包围盒中心计算，到达判断则使用玩家到包围盒最近点的水平距离，默认阈值为 1.2 m。靠近、取消、超时或状态切换会清除效果。提示给出直线方位，玩家仍需自行沿可通行区域前往。

### 4.6 确认式中英文语音输入

点击语音选项开始使用系统默认麦克风录音，再次点击结束，最长 30 秒。客户端将实际音频转为单声道 PCM16 WAV，优先使用 16 kHz 设备采样率，完整上传到后端并由 Groq `whisper-large-v3-turbo` 转写 [18]。

转写不请求翻译，结果进入可编辑草稿，由玩家确认后提交。这样允许用户纠正显微术语，也使语音入口复用同一状态与校验链路。当前输出为文字；麦克风部署、中英混说准确率和端到端等待时间须分别测量。论文的设备描述应注明受试版本使用 PC-VR 还是一体机及实际后端连接方式。


## 5. 综合学习情境

### 5.1 样本观察与有限状态下的调焦建议

用户拾取样本，走近显微镜，放置并进入观察。随后切换物镜、比较视野，再用粗细调寻找合适的观察区间。请求帮助时，助手可以选择当前允许的调焦动作，并说明操作后应观察哪些变化。

这里连接的是项目真实存在的控制与教学提示，不意味着 AI 已读取焦点误差、识别黑屏原因或计算出正确调节方向。学习者对变化的观察补充了快照中没有的图像信息。

### 5.2 从部件提问进入实验

在 SuperAssembly 中，用户选中上部光学组件并询问其用途。助手结合面板说明解释其与 NA 的关系；随后用户要求体验实验，助手选择当前可用的 NA 启动动作。用户关闭对话框、点击按钮，再调整滑块比较光锥变化。

选择物镜时，对应的是空间频率入口。如果用户还在房间其他位置，则首先得到同一显微镜学习区域的标记，而不是被引导到一个不存在的独立傅里叶实验台。


> **【插图 4 占位｜优先制作｜建议通栏，四格】** 在同一次实际运行中依次截取：(a) 询问当前部件；(b) 基于面板内容的解释；(c) 请求体验后获得实验入口指引；(d) 用户关闭对话并启动实验后的参数/光锥画面。以短箭头连接顺序，保留真实问题与回答的关键句，不编造成功对话。若只有 mock 截图，必须标注为协议演示，不能作为模型质量结果。
>
> **拟图注：** 图 4. 从部件解释到实验操作的连续学习情境。助手依据当前面板提供解释和可用入口，玩家执行启动操作并观察反馈；该序列展示交互机制，不单独证明学习增益。

### 5.3 SNOM 前置条件与响应变化

选择探针时，助手可以建议允许的选择或安装动作；安装期间则解释等待或提供当前允许的退出操作，不编造立即扫描的快捷方式。安装完成后，系统启动步骤才成为可用动作。

随后用户通过演示控制探索敲击、耦合和扫描。若返回的操作建议与请求时采集的状态不再匹配，客户端拒绝应用。该情境说明流程条件如何与 AI 指引连接，本身不构成对抗鲁棒性的实验结论。

## 6. 评价方法与结果

为使评价指标与论文贡献保持一致，本文将系统评价分为实现级一致性检查与探索性用户研究两部分。前者回答“系统是否按照所描述的状态约束工作”，后者回答“学习者能否完成核心流程，以及如何评价系统的可用性和主观工作负荷”。这种分离与已有 AIxVR 教育型 XR 工作中将客观任务表现和主观量表分别报告的做法一致 [1]，也避免将代码行为、交互性能和学习成效混为同一种证据。

### 6.1 评价问题与实现级一致性检查

为便于报告，本文将用户研究组织为两个评价问题：

- **RQ1（流程可行性）：**参与者能否在 VR 环境中完成样本操作、显微观察、调焦/照明调整和结构探索等核心任务？
- **RQ2（感知体验）：**参与者如何评价系统的整体可用性与任务工作负荷？

实现级检查不作为第三个用户研究问题，而用于确认论文前文描述的约束机制是否与当前实现一致。检查范围包括：根据当前界面生成可用动作集合；拒绝目录外或当前状态下不可用的操作标识；在请求后的实验状态发生变化时拒绝过期指引；以及根据玩家和目标的实时位置计算空间方位与距离。它们属于确定性的程序一致性属性，因此本文不将其包装为“AI 准确率”或“安全成功率”。帧率、语音延迟和模型响应时间等性能指标也不属于本次用户研究的结果变量。

### 6.2 参与者与实验流程

> **【研究材料待核对｜提交前完成并移除此标注】** 作者已确认研究存在，原始材料位于项目目录之外。补充受试构建/日期、PC-VR 或一体机及后端配置、招募和先前经验、核心任务成功与超时标准、研究人员协助，以及实际使用 AI/语音的人数和次数。本节沿用原稿汇总，需与原始问卷和日志核对；不得将“仓库中没有原始数据”写成“原始数据已遗失”。

研究共包含 19 名参与者。由于专业背景不是实验操纵变量，本文仅将其作为样本构成描述，而不据此划分性能优劣或推断光学知识水平。

**表 3：参与者专业背景构成（$N=19$）。**

| 专业背景 | 人数 |
|---|---:|
| 物理学 | 6 |
| 软件工程 | 10 |
| 商学 | 2 |
| 哲学 | 1 |

实验在安静的室内环境中使用 Meta Quest 3 进行。参与者先进行约 3 分钟的控制器熟悉，随后依次完成六类活动：拾取样本；将样本放置到显微镜并进入观察；使用粗调和细调改变焦点；切换物镜并重新调焦/调整照明；退出观察并探索 SuperAssembly；在需要解释或操作帮助时使用 AI 问答。AI 提问是可选辅助通道，而不是一个单独的实验条件，因此该流程不能用于比较“有 AI”和“无 AI”的因果差异。

任务结束后，参与者填写 System Usability Scale（SUS）[19] 与 NASA Task Load Index（NASA-TLX）[20]，并参加约 10 分钟的半结构化访谈。本文的定量结果使用任务完成情况与标准量表；访谈仅作为形成性反馈来源，不进行频次统计或主题比例推断。

### 6.3 指标与分析方法

核心任务完成率为完成规定流程的参与者比例。SUS 用于评价感知可用性 [19]，NASA-TLX 用于描述任务主观负荷 [20]。本稿保留任务完成计数和问卷均值作为描述性结果；专业背景仅描述样本，不进行小样本组间推断。

> **【统计核对项｜提交前完成】** 从作者持有的个体问卷核对均值、有效样本量及缺失项，计算可用的标准差或置信区间；明确 Performance 方向与原始/加权 TLX 计分。表 4 暂不纳入该维度及总体 TLX。若原始记录支持完成时间分析，应先说明未完成者的计时与删失处理。只有确实无法取得相关记录时，才据实说明限制，不能假定记录缺失。

可用性与学习相关结果分别报告，与已有教育 XR 研究的评价区分一致 [21]。SUS 解释参考经验研究 [22]，不作为知识掌握分数。


### 6.4 用户研究结果

**表 4：全体参与者的描述性结果。**

| 指标 | 结果 |
|---|---:|
| 核心任务完成率 | 18/19（94.7%） |
| SUS（0–100） | 82.4 |
| NASA-TLX：心理需求 | 42.9 |
| NASA-TLX：身体需求 | 27.9 |
| NASA-TLX：时间压力 | 30.8 |
| NASA-TLX：付出努力 | 44.7 |
| NASA-TLX：挫败感 | 24.2 |

18 名参与者完成了核心任务流程，表明在本次研究条件下，绝大多数参与者能够完成从样本操作到显微观察和结构探索的主要交互序列。该指标反映流程可行性，不代表参与者已经掌握相关光学知识。

整体 SUS 均值为 82.4。按照既有 SUS 经验数据的解释框架，这一结果支持较高的感知可用性 [22]。

NASA-TLX 的描述性结果中，付出努力（44.7）和心理需求（42.9）是数值最高的两个已报告维度；身体需求（27.9）、时间压力（30.8）和挫败感（24.2）较低。由于本研究没有对照条件，也没有针对各维度提出预先假设，本文仅报告这一描述性分布，不对维度差异进行显著性检验或因果解释。

### 6.5 结果解释与效度边界

本研究是一项单组探索性评价。它支持的结论是：参与者总体能够完成核心 VR 显微操作流程，并对系统给出较高的可用性评价；它不支持“AI 提高了学习成绩”“AI 降低了工作负荷”或“系统可以替代真实仪器训练”等因果结论。SUS 和 NASA-TLX 都是主观体验量表，也不能替代光学知识前后测、真实仪器迁移测试或专家操作评分。

参与者的专业背景仅用于描述样本构成。由于各专业人数不平衡，尤其是商学和哲学参与者数量很少，本文不把专业类别作为统计比较组。当前实现中的语音识别准确率、端到端延迟、帧率以及对异常提示的系统鲁棒性，需要通过单独设计的仪器化基准或压力测试评价，不能从本次 SUS/NASA-TLX 结果中推断。

### 6.6 AI 指引质量的补充评价方案

> **【待执行的补充评价｜不是已有结果】** 本节给出建议的真实模型测试协议。完成测试后用实际配置、数量和结果改写；若投稿前未执行，应将本节移到后续工作，并移除图 5 占位，不在摘要或结论中声称已评价 AI 质量。

建议建立 30 个范围内的“问题—历史—状态”样例，覆盖一般下一步、部件实验入口、跨模块退出、SNOM 等待、仪器/样本定位和必要澄清六类，每类五例；另设十个范围外或无可用功能的边界样例。每例使用固定快照独立运行三次，记录模型标识、提示词/知识版本、采样参数、每轮候选、重生成、审核和客户端结果。数量是拟定方案，不能写成已完成样本量。

每个范围内样例由熟悉实验流程的评定者事先定义可接受动作或回答类别，允许同一目标存在多个有效下一步。先独立判断再处理分歧，并报告实际评定人数和流程。评定同时检查“动作此刻可用”与“动作符合学习目的”；例如展开界面中机械地推荐退出，可能合法但不合目的。原始问题固定，格式修复不算新用户请求。

建议报告首次结构通过率、最终目标匹配率、正常问题拒答率及服务错误率；分别以首次候选数、范围内请求数为分母，并保留每项计数。格式修复成功率以发生修复的请求为分母，同时说明修复后是否仍通过语义与客户端检查。边界样例单独报告，不混入正常请求的拒答率。端到端时间从用户提交至客户端得到可显示结果或终止错误计算，包含修复和审核；成功、失败及超时分别汇总。

> **【插图 5 占位｜可选，必须有真实数据】** 优先用一张单栏图展示范围内六类场景的最终结果分布：目标匹配、合法但不合目标、拒答、格式/审核错误、连接/超时错误，采用互斥分类并标注每类请求数。如页数允许，用第二面板显示端到端时间散点或分布。不要用 mock 数据、推测的提升比例、只有均值却虚构的误差条。现有 SUS/TLX 表无需再画一张重复的柱状图。
>
> **拟图注（数据补齐后使用）：** 图 5. 真实模型在预定义状态场景中的最终指引结果。分布以用户请求为单位，包含格式修复与审核后的结果；延迟包含完整问答处理链路。

## 7. 讨论与局限性

VRMicroscope 的主要系统贡献不是让大语言模型“控制显微镜”，而是把当前部件、当前可用控制和可观察结果建立显式联系。实验环境提供可操作对象和反馈，AI 层则在这些既有状态之上提供解释、操作说明和空间定位。状态快照、动作目录和客户端复核降低了生成不存在操作或使用过期步骤的风险，但它们并不保证所有自然语言解释在科学上都正确。

第二个设计重点是区分教学抽象与物理真实性。调焦可见区间和亮度映射属于人为设计的教学反馈；NA、光栅衍射、傅里叶频谱和 s-SNOM 阶段则建立在标准理论关系上，但仍采用了简化参数、合成图像或支持范围可视化。因此，本系统更适合被描述为“具有显式建模边界的教学仿真”，而不是数字孪生或定量显微模拟器。后续若要评价科学真实性，应由显微领域专家依据真实仪器行为对关键模块进行内容效度审查，并将可比较变量与实测数据进行标定。

用户研究提供的是可行性和可用性证据。与 Chheang 等采用明确实验条件的 within-subject 设计 [1]，以及 Duan 等同时设置学习相关结果和比较组的 AIxVR 研究 [21] 相比，本文的单组探索性研究更适合回答“系统是否可用、流程是否能完成”，而不能回答“AI 是否使学习更好”。下一阶段评价应加入 AI-on/AI-off 或不同辅导策略的对照条件、显微概念前后测、真实或专家评分的操作迁移任务，以及自动记录的 AI 交互次数、语音错误率和端到端延迟。这样才能把“系统可用”进一步推进到“学习有效”和“工程性能可重复验证”。

## 8. 结论与后续工作

本文提出 VRMicroscope，将可交互的虚拟显微实验、具有明确科学边界的光学教学模块和任务状态感知 AI 指引整合到同一学习环境中。系统通过实际界面状态生成可用动作集合，对模型返回的操作标识进行程序复核，并在状态变化后拒绝过期建议；与此同时，VR 层保留样本操作、调焦、照明、物镜切换、部件探索以及 NA、空间频率和 THz s-SNOM 等具体学习活动。

一项包含 19 名参与者的探索性研究表明，18 名参与者完成了核心任务流程，整体 SUS 均值为 82.4/100，支持系统的流程可行性和较高感知可用性。本文不把这些结果解释为 AI 的因果学习增益。后续工作将围绕三个方向展开：使用受控实验设计评价 AI 辅导的增量价值；通过知识前后测、专家评分和真实仪器迁移任务评价学习效果；以及使用自动日志系统化测量语音识别、AI 延迟和 XR 渲染性能。

## 研究伦理与生成式 AI 使用说明

> **【作者待填写｜正式提交前必须完成】** 按实际记录补充伦理审查机构及编号，或未进行审查的原因；说明参与者知情同意、退出机制，以及问卷、音频和云端处理的实际数据管理方式。本稿不预设已经取得某项审批或同意。

Codex 用于辅助代码实现及论文内容生成、修订和一致性检查，DeepSeek 用于系统运行时问答。人类作者负责核实实现描述、研究记录、分析及引用。若还使用其他生成式工具制作正文、图像或代码，应补充工具和用途；最终声明应按实际使用情况确认。

## 参考文献

[1] V. Chheang et al., “Towards Anatomy Education with Generative AI-based Virtual Assistants in Immersive Virtual Reality Environments,” in *Proc. 2024 IEEE Int. Conf. on Artificial Intelligence and eXtended and Virtual Reality (AIxVR)*, pp. 21–30, 2024. [doi:10.1109/AIxVR59861.2024.00011](https://doi.org/10.1109/AIxVR59861.2024.00011).

[2] A. Geris and G. Alce, “Real-Time Voice-Based LLM Integration for XR Tutoring: A Prototype Implementation,” in *Proc. 2026 IEEE Int. Conf. on Artificial Intelligence and eXtended and Virtual Reality (AIxVR)*, pp. 285–289, 2026. [doi:10.1109/AIxVR67263.2026.00050](https://doi.org/10.1109/AIxVR67263.2026.00050).

[3] D. Wang et al., “SpatialTutor: Object-Aware Mixed Reality Training for Procedural Medical Skills Training with AI-Driven Support,” in *Proc. 2026 IEEE Int. Conf. on Artificial Intelligence and eXtended and Virtual Reality (AIxVR)*, pp. 57–66, 2026. [doi:10.1109/AIxVR67263.2026.00016](https://doi.org/10.1109/AIxVR67263.2026.00016).

[4] J. Jonkman et al., “Tutorial: guidance for quantitative confocal microscopy,” *Nature Protocols*, vol. 15, pp. 1585–1611, 2020. [doi:10.1038/s41596-020-0313-9](https://doi.org/10.1038/s41596-020-0313-9).

[5] Carl ZEISS Microscopy, “Numerical Aperture and Light Cone Geometry.” [Online tutorial](https://www.zeiss.com/microscopy/en/resources/insights-hub/foundational-knowledge/numerical-aperture-and-light-cone-geometry.html). Accessed Sep. 13, 2026.

[6] Carl ZEISS Microscopy, “Spatial Frequency and Image Resolution.” [Online tutorial](https://www.zeiss.com/microscopy/en/resources/insights-hub/foundational-knowledge/spatial-frequency-and-image-resolution.html). Accessed Sep. 13, 2026.

[7] C. Eggeling, K. I. Willig, S. J. Sahl, and S. W. Hell, “Lens-based fluorescence nanoscopy,” *Quarterly Reviews of Biophysics*, vol. 48, no. 2, pp. 178–243, 2015. [doi:10.1017/S0033583514000146](https://doi.org/10.1017/S0033583514000146).

[8] E. Abbe, “Beiträge zur Theorie des Mikroskops und der mikroskopischen Wahrnehmung,” *Archiv für Mikroskopische Anatomie*, vol. 9, no. 1, pp. 413–468, 1873. [doi:10.1007/BF02956173](https://doi.org/10.1007/BF02956173).

[9] S. B. Mehta and R. Oldenbourg, “Image simulation for biological microscopy: microlith,” *Biomedical Optics Express*, vol. 5, no. 6, pp. 1822–1838, 2014. [doi:10.1364/BOE.5.001822](https://doi.org/10.1364/BOE.5.001822).

[10] F. J. Harris, “On the Use of Windows for Harmonic Analysis with the Discrete Fourier Transform,” *Proceedings of the IEEE*, vol. 66, no. 1, pp. 51–83, 1978. [doi:10.1109/PROC.1978.10837](https://doi.org/10.1109/PROC.1978.10837).

[11] J. W. Cooley and J. W. Tukey, “An Algorithm for the Machine Calculation of Complex Fourier Series,” *Mathematics of Computation*, vol. 19, no. 90, pp. 297–301, 1965. [doi:10.1090/S0025-5718-1965-0178586-1](https://doi.org/10.1090/S0025-5718-1965-0178586-1).

[12] M. G. L. Gustafsson, “Surpassing the lateral resolution limit by a factor of two using structured illumination microscopy,” *Journal of Microscopy*, vol. 198, no. 2, pp. 82–87, 2000. [doi:10.1046/j.1365-2818.2000.00710.x](https://doi.org/10.1046/j.1365-2818.2000.00710.x).

[13] R. Heintzmann and T. Huser, “Super-Resolution Structured Illumination Microscopy,” *Chemical Reviews*, vol. 117, no. 23, pp. 13890–13908, 2017. [doi:10.1021/acs.chemrev.7b00218](https://doi.org/10.1021/acs.chemrev.7b00218).

[14] P. U. Jepsen, D. G. Cooke, and M. Koch, “Terahertz spectroscopy and imaging – Modern techniques and applications,” *Laser & Photonics Reviews*, vol. 5, no. 1, pp. 124–166, 2011. [doi:10.1002/lpor.201000011](https://doi.org/10.1002/lpor.201000011).

[15] T. L. Cocker *et al*., “Nanoscale terahertz scanning probe microscopy,” *Nature Photonics*, vol. 15, pp. 558–569, 2021. [doi:10.1038/s41566-021-00835-6](https://doi.org/10.1038/s41566-021-00835-6).

[16] F. Keilmann and R. Hillenbrand, “Near-field microscopy by elastic light scattering from a tip,” *Philosophical Transactions of the Royal Society A: Mathematical, Physical and Engineering Sciences*, vol. 362, no. 1817, pp. 787–805, 2004. [doi:10.1098/rsta.2003.1347](https://doi.org/10.1098/rsta.2003.1347).

[17] R. Hillenbrand, B. Knoll, and F. Keilmann, “Pure optical contrast in scattering-type scanning near-field microscopy,” *Journal of Microscopy*, vol. 202, no. 1, pp. 77–83, 2001. [doi:10.1046/j.1365-2818.2001.00794.x](https://doi.org/10.1046/j.1365-2818.2001.00794.x).

[18] Groq, “Speech to Text.” [API documentation](https://console.groq.com/docs/speech-to-text). Accessed Sep. 13, 2026.

[19] J. Brooke, “SUS: A ‘Quick and Dirty’ Usability Scale,” in *Usability Evaluation in Industry*, 1996. [doi:10.1201/9781498710411-35](https://doi.org/10.1201/9781498710411-35).

[20] S. G. Hart and L. E. Staveland, “Development of NASA-TLX (Task Load Index): Results of empirical and theoretical research,” *Advances in Psychology*, vol. 52, pp. 139–183, 1988. [Author text hosted by NASA](https://human-factors.arc.nasa.gov/publications/Hart_Staveland_ORIGINAL_1.pdf).

[21] Y. Duan, X. Xu, H. He, Y. Gu, S. Li, and J. Bueno Vesga, “Supplementing Patient Encounter Training for Preservice Nurses: Towards an AI×VR Approach,” in *Proc. 2026 IEEE Int. Conf. on Artificial Intelligence and eXtended and Virtual Reality (AIxVR)*, pp. 98–107, 2026. [doi:10.1109/AIxVR67263.2026.00020](https://doi.org/10.1109/AIxVR67263.2026.00020).

[22] A. Bangor, P. T. Kortum, and J. T. Miller, “An Empirical Evaluation of the System Usability Scale,” *International Journal of Human–Computer Interaction*, vol. 24, no. 6, pp. 574–594, 2008. [doi:10.1080/10447310802205776](https://doi.org/10.1080/10447310802205776).
