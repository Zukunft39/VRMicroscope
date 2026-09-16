# 论文排版与待补材料


已生成中英文双栏 Word 排版稿，公式为可编辑 Word 数学对象。原始 Formal.md 未改动。

当前文件是正式版式的工作稿：图像、伦理说明及研究记录核对仍需完成；最终页数应在 Word/PDF 中检查，未宣称已经满足投稿页限。

完整的未实施 AI 评价协议移到本清单，正文后续工作仅保留计划摘要。插图 1–4 保留简短占位和图注，图 5 暂不进入论文。



## AIxVR2027_VRMicroscope_Draft_Formal


> **【编辑标注说明｜提交前移除】** 本稿中的插图占位、待核实研究信息与补充评价方案均为编辑标注，不代表已制作的图片或新增实验结果。图 1–4 为建议优先制作的图，图 5 为有数据后才制作的可选评价图。具体分镜、图注与往届论文依据见 [插图制作说明](AIxVR2027_Figure_Plan.md)。
> **【插图 1 占位｜优先制作｜建议通栏】** 从当前项目截取实验室全景作为主图，框选显微镜站、SNOM 装置及样本位置；配三个局部视图显示样本观察、部件展开和 SNOM 探针安装。统一使用 A–D 标注对应学习活动，保留一处小助手入口以说明它位于同一环境。不要使用 Unity 编辑器边框或无关工具面板。
>
> **拟图注：** 图 1. VRMicroscope 学习环境与核心活动。学习者在同一实验室中进行样本操作、显微观察、部件探索和 SNOM 流程演示，并通过助手获取相关解释与指引。
> **【插图 2 占位｜优先制作｜建议通栏】** 制作三行对比面板：(a) 同一物镜、样本和照明下的两种调焦状态；(b) 两个 NA 设置及对应光锥，显示界面真实参数；(c) 同一样本下两档载波的源频谱、混合侧带和恢复支持显示。使用实际运行截图，标注每组唯一变化的输入，不能把合成面板称为实测图像。空间不足时保留 NA 与频谱两行。
>
> **拟图注：** 图 2. 教学输入与可观察反馈的对应关系。调焦、NA 和照明载波分别改变样本显示、光锥几何及侧带/支持范围；光学反馈包含人工映射和简化数值显示。
> **【插图 3 占位｜优先制作｜建议通栏】** 绘制原创矢量架构图，分为 Unity、Python 网关、外部 API 三个区域。主线为“问题+当前快照→候选生成→动作/格式校验→标准文案→语义审核→客户端状态复核→提示卡或标记”；格式失败仅允许一次返回候选生成的修复箭头。语音支线为“麦克风→Groq 转写→可编辑草稿→用户确认”。另画“玩家实际操作→VR 状态更新”的闭环，不能画成模型直接控制仪器。
>
> **拟图注：** 图 3. 状态约束的问答架构。模型选择候选解释或指引，程序负责可用性校验、操作文案和响应时效；格式修复保持同一状态权限，实验操作由玩家完成。
> **【插图 4 占位｜优先制作｜建议通栏，四格】** 在同一次实际运行中依次截取：(a) 询问当前部件；(b) 基于面板内容的解释；(c) 请求体验后获得实验入口指引；(d) 用户关闭对话并启动实验后的参数/光锥画面。以短箭头连接顺序，保留真实问题与回答的关键句，不编造成功对话。若只有 mock 截图，必须标注为协议演示，不能作为模型质量结果。
>
> **拟图注：** 图 4. 从部件解释到实验操作的连续学习情境。助手依据当前面板提供解释和可用入口，玩家执行启动操作并观察反馈；该序列展示交互机制，不单独证明学习增益。
> **【研究材料待核对｜提交前完成并移除此标注】** 作者已确认研究存在，原始材料位于项目目录之外。补充受试构建/日期、PC-VR 或一体机及后端配置、招募和先前经验、核心任务成功与超时标准、研究人员协助，以及实际使用 AI/语音的人数和次数。本节沿用原稿汇总，需与原始问卷和日志核对；不得将“仓库中没有原始数据”写成“原始数据已遗失”。
> **【统计核对项｜提交前完成】** 从作者持有的个体问卷核对均值、有效样本量及缺失项，计算可用的标准差或置信区间；明确 Performance 方向与原始/加权 TLX 计分。表 4 暂不纳入该维度及总体 TLX。若原始记录支持完成时间分析，应先说明未完成者的计时与删失处理。只有确实无法取得相关记录时，才据实说明限制，不能假定记录缺失。
> **【待执行的补充评价｜不是已有结果】** 本节给出建议的真实模型测试协议。完成测试后用实际配置、数量和结果改写；若投稿前未执行，应将本节移到后续工作，并移除图 5 占位，不在摘要或结论中声称已评价 AI 质量。
> **【插图 5 占位｜可选，必须有真实数据】** 优先用一张单栏图展示范围内六类场景的最终结果分布：目标匹配、合法但不合目标、拒答、格式/审核错误、连接/超时错误，采用互斥分类并标注每类请求数。如页数允许，用第二面板显示端到端时间散点或分布。不要用 mock 数据、推测的提升比例、只有均值却虚构的误差条。现有 SUS/TLX 表无需再画一张重复的柱状图。
>
> **拟图注（数据补齐后使用）：** 图 5. 真实模型在预定义状态场景中的最终指引结果。分布以用户请求为单位，包含格式修复与审核后的结果；延迟包含完整问答处理链路。
> **【作者待填写｜正式提交前必须完成】** 按实际记录补充伦理审查机构及编号，或未进行审查的原因；说明参与者知情同意、退出机制，以及问卷、音频和云端处理的实际数据管理方式。本稿不预设已经取得某项审批或同意。

### 6.6 AI 指引质量的补充评价方案

> **【待执行的补充评价｜不是已有结果】** 本节给出建议的真实模型测试协议。完成测试后用实际配置、数量和结果改写；若投稿前未执行，应将本节移到后续工作，并移除图 5 占位，不在摘要或结论中声称已评价 AI 质量。

建议建立 30 个范围内的“问题—历史—状态”样例，覆盖一般下一步、部件实验入口、跨模块退出、SNOM 等待、仪器/样本定位和必要澄清六类，每类五例；另设十个范围外或无可用功能的边界样例。每例使用固定快照独立运行三次，记录模型标识、提示词/知识版本、采样参数、每轮候选、重生成、审核和客户端结果。数量是拟定方案，不能写成已完成样本量。

每个范围内样例由熟悉实验流程的评定者事先定义可接受动作或回答类别，允许同一目标存在多个有效下一步。先独立判断再处理分歧，并报告实际评定人数和流程。评定同时检查“动作此刻可用”与“动作符合学习目的”；例如展开界面中机械地推荐退出，可能合法但不合目的。原始问题固定，格式修复不算新用户请求。

建议报告首次结构通过率、最终目标匹配率、正常问题拒答率及服务错误率；分别以首次候选数、范围内请求数为分母，并保留每项计数。格式修复成功率以发生修复的请求为分母，同时说明修复后是否仍通过语义与客户端检查。边界样例单独报告，不混入正常请求的拒答率。端到端时间从用户提交至客户端得到可显示结果或终止错误计算，包含修复和审核；成功、失败及超时分别汇总。

> **【插图 5 占位｜可选，必须有真实数据】** 优先用一张单栏图展示范围内六类场景的最终结果分布：目标匹配、合法但不合目标、拒答、格式/审核错误、连接/超时错误，采用互斥分类并标注每类请求数。如页数允许，用第二面板显示端到端时间散点或分布。不要用 mock 数据、推测的提升比例、只有均值却虚构的误差条。现有 SUS/TLX 表无需再画一张重复的柱状图。
>
> **拟图注（数据补齐后使用）：** 图 5. 真实模型在预定义状态场景中的最终指引结果。分布以用户请求为单位，包含格式修复与审核后的结果；延迟包含完整问答处理链路。




## AIxVR2027_VRMicroscope_Paper_EN_Formal


> **[EDITORIAL NOTE — REMOVE BEFORE SUBMISSION]** Figure placeholders, study-record checks, and the supplementary evaluation protocol are editorial annotations, not completed figures or new results. Figures 1–4 are prioritized; optional Figure 5 requires data. Layouts, captions, and prior-paper examples are documented in the [figure plan](AIxVR2027_Figure_Plan.md).
> **[FIGURE 1 PLACEHOLDER — PRIORITY; TWO COLUMNS RECOMMENDED]** Use a runtime laboratory overview to identify the microscope station, SNOM apparatus, and specimen locations. Add three crops for specimen observation, exploded components, and SNOM probe installation. Label activities A–D and retain one view of the assistant entry point. Exclude editor chrome and unrelated panels.
>
> **Proposed caption:** Fig. 1. VRMicroscope learning environment and core activities. Learners handle specimens, observe microscope feedback, inspect components, and explore the SNOM workflow within one laboratory, with access to contextual assistant guidance.
> **[FIGURE 2 PLACEHOLDER — PRIORITY; TWO COLUMNS RECOMMENDED]** Use a three-row comparison: (a) two focus states with objective, specimen, and illumination held fixed; (b) two NA settings and their cones, retaining the actual displayed parameter values; (c) two carrier settings for the same specimen, showing source spectrum, mixed sidebands, and recovered support. Use runtime captures and identify the changed input in each comparison. Label synthetic displays accurately. If space is limited, retain the NA and spectrum rows.
>
> **Proposed caption:** Fig. 2. Mapping instructional inputs to observable feedback. Focus, NA, and illumination carrier affect specimen display, cone geometry, and sideband/support displays, respectively. The feedback combines authored mappings and simplified numerical visualization.
> **[FIGURE 3 PLACEHOLDER — PRIORITY; TWO COLUMNS RECOMMENDED]** Draw an original vector architecture diagram with Unity, Python gateway, and external API regions. Show question/current snapshots → candidate generation → action/schema checks → canonical instructions → semantic review → client freshness checks → reminder or marker. Include at most one format-repair loop back to generation. The speech branch is microphone → Groq transcription → editable draft → user confirmation. Close the loop through learner action and VR state update; do not draw the model directly operating the instrument.
>
> **Proposed caption:** Fig. 3. State-constrained question-answering architecture. The model selects candidate explanations or guidance, while application code checks availability, renders operating instructions, and verifies freshness. Format repair retains the same permissions; learners execute experimental operations.
> **[FIGURE 4 PLACEHOLDER — PRIORITY; TWO COLUMNS, FOUR PANELS]** Capture one actual session: (a) a question about the selected component; (b) an explanation based on its panel; (c) a request to try the associated experiment and the returned entry instruction; (d) the learner closes the dialog, starts the experiment, and sees parameter/cone feedback. Connect panels sequentially and retain short authentic dialogue excerpts. Label mock captures as protocol demonstrations, not live-model quality evidence.
>
> **Proposed caption:** Fig. 4. Continuous learning from component explanation to experimental interaction. The assistant uses the current panel to explain the component and identify an available entry; the learner starts the experiment and observes feedback. The sequence illustrates interaction, not learning gains by itself.
> **[STUDY RECORD CHECK — COMPLETE AND REMOVE BEFORE SUBMISSION]** The authors confirm that the study exists and that original materials are outside the repository. Specify the evaluated build/date, PC-VR or standalone/gateway setup, recruitment and prior experience, task success/timeout rules, researcher assistance, and actual AI/speech use. Reconcile the retained summaries with questionnaires and logs. Absence from the repository does not establish loss of original records.
> **[STATISTICAL CHECK — COMPLETE BEFORE SUBMISSION]** Use the authors' individual questionnaires to verify means, valid sample sizes, and missing items, and calculate supported SDs or confidence intervals. Establish the Performance scale direction and raw versus weighted TLX scoring; Table 4 temporarily omits that dimension and composite TLX. If completion-time records are available, define handling of incomplete cases and censoring before analysis. Describe missing-record limitations only if the relevant records genuinely cannot be obtained.
> **[PROPOSED EVALUATION — NOT AN EXISTING RESULT]** This section specifies a recommended live-model protocol. Replace it with the actual configuration, counts, and results after execution. If it is not run before submission, move the protocol to future work and remove the Figure 5 placeholder; do not claim evaluated AI quality in the abstract or conclusion.
> **[FIGURE 5 PLACEHOLDER — OPTIONAL; REAL DATA REQUIRED]** Prefer a single-column plot of final outcomes for the six in-scope categories: goal-matched, legal but goal-mismatched, refusal, format/review error, and connection/timeout error. Use mutually exclusive categories and label request counts. If space permits, add an end-to-end timing distribution or individual points. Do not use mock results, hypothetical gains, or invented error bars. Do not duplicate the existing SUS/TLX table with an equivalent bar chart.
>
> **Proposed caption, after data collection:** Fig. 5. Final guidance outcomes for the live model across predefined state scenarios. Distributions use user requests as the unit and include outcomes after format repair and review; timing covers the complete question-answering pipeline.
> **[AUTHOR COMPLETION REQUIRED BEFORE SUBMISSION]** Provide the actual ethics-review body and identifier, or explain why review was not performed. Describe participant consent, withdrawal, and the actual handling of questionnaires, audio, and cloud processing. This draft does not presume that approval or consent has already been documented.

### 6.6 Supplementary Protocol for AI Guidance Quality

> **[PROPOSED EVALUATION — NOT AN EXISTING RESULT]** This section specifies a recommended live-model protocol. Replace it with the actual configuration, counts, and results after execution. If it is not run before submission, move the protocol to future work and remove the Figure 5 placeholder; do not claim evaluated AI quality in the abstract or conclusion.

A proposed suite contains 30 in-scope question/history/state cases: five each for generic next steps, component-experiment entry, cross-module exit, SNOM waiting, instrument/specimen localization, and necessary clarification. Ten additional cases cover out-of-scope requests or unavailable capabilities. Run each case independently three times with fixed snapshots. Record model identity, prompt/knowledge versions, sampling parameters, candidates, regeneration, review, and client outcomes. These are proposed sample counts, not completed trials.

Before running the suite, evaluators familiar with the workflow should define acceptable actions or response categories for each in-scope case, allowing multiple valid next steps. Assess independently before resolving disagreements and report the actual number of evaluators and procedure. Judge both availability and correspondence to the learning goal: suggesting exit from an expanded model may be legal but unhelpful. A format repair remains part of the original user request.

Report first-candidate structural acceptance, final goal matching, refusal of normal questions, and service-error rates, with counts and explicit denominators: initial candidates for the first metric and in-scope requests for the others. Repair success uses repaired requests as its denominator and must state whether semantic and client checks also passed. Boundary cases are reported separately. Measure end-to-end time from user submission to a displayable result or terminal error, including repair and review, and distinguish success, failure, and timeout outcomes.

> **[FIGURE 5 PLACEHOLDER — OPTIONAL; REAL DATA REQUIRED]** Prefer a single-column plot of final outcomes for the six in-scope categories: goal-matched, legal but goal-mismatched, refusal, format/review error, and connection/timeout error. Use mutually exclusive categories and label request counts. If space permits, add an end-to-end timing distribution or individual points. Do not use mock results, hypothetical gains, or invented error bars. Do not duplicate the existing SUS/TLX table with an equivalent bar chart.
>
> **Proposed caption, after data collection:** Fig. 5. Final guidance outcomes for the live model across predefined state scenarios. Distributions use user requests as the unit and include outcomes after format repair and review; timing covers the complete question-answering pipeline.

