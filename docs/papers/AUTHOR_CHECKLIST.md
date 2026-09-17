# 论文排版与待补材料


已生成中英文双栏 Word 排版稿，公式为可编辑 Word 数学对象；图 3 已同步至 Formal.md。

图 1、图 2 和图 4 为运行时截图；图 3 已由 draw.io 绘制并嵌入 SVG（附 PNG 兼容图）。伦理声明沿用作者提供的文本，研究记录仍由作者核对；最终页数另见 FINAL_REVIEW.md。

完整的未实施 AI 评价协议移到本清单，正文后续工作仅保留计划摘要。图 5 暂不进入论文，除非取得真实模型测试数据。



## AIxVR2027_VRMicroscope_Draft_Formal


> **【编辑标注说明｜提交前移除】** 图 1、图 2 和图 4 已嵌入 `Pic/` 中提供的运行时截图。图 3 已补齐 draw.io 原创矢量架构图，图 5 只有在获得真实模型测试数据后才制作。图片选择和剩余检查项见 [插图制作说明](AIxVR2027_Figure_Plan.md)。
> **【研究材料待核对｜提交前完成并移除此标注】** 作者已确认研究存在，原始材料位于项目目录之外。补充受试构建/日期、PC-VR 或一体机及后端配置、招募和先前经验、核心任务成功与超时标准、研究人员协助，以及实际使用 AI/语音的人数和次数。本节沿用原稿汇总，需与原始问卷和日志核对；不得将“仓库中没有原始数据”写成“原始数据已遗失”。
> **【待执行的补充评价｜不是已有结果】** 本节给出建议的真实模型测试协议。完成测试后用实际配置、数量和结果改写；若投稿前未执行，应将本节移到后续工作，并移除图 5 占位，不在摘要或结论中声称已评价 AI 质量。
> **【插图 5 占位｜可选，必须有真实数据】** 优先用一张单栏图展示范围内六类场景的最终结果分布：目标匹配、合法但不合目标、拒答、格式/审核错误、连接/超时错误，采用互斥分类并标注每类请求数。如页数允许，用第二面板显示端到端时间散点或分布。不要用 mock 数据、推测的提升比例、只有均值却虚构的误差条。现有 SUS/TLX 表无需再画一张重复的柱状图。
>
> **拟图注（数据补齐后使用）：** 图 5. 真实模型在预定义状态场景中的最终指引结果。分布以用户请求为单位，包含格式修复与审核后的结果；延迟包含完整问答处理链路。

### 6.6 AI 指引质量的补充评价方案

> **【待执行的补充评价｜不是已有结果】** 本节给出建议的真实模型测试协议。完成测试后用实际配置、数量和结果改写；若投稿前未执行，应将本节移到后续工作，并移除图 5 占位，不在摘要或结论中声称已评价 AI 质量。

建议建立 30 个范围内的“问题—历史—状态”样例，覆盖一般下一步、部件实验入口、跨模块退出、SNOM 等待、仪器/样本定位和必要澄清六类，每类五例；另设十个范围外或无可用功能的边界样例。每例使用固定快照独立运行三次，记录模型标识、提示词/知识版本、采样参数、每轮候选、重生成、审核和客户端结果。数量是拟定方案，不能写成已完成样本量。

每个范围内样例由熟悉实验流程的评定者事先定义可接受动作或回答类别，允许同一目标存在多个有效下一步。先独立判断再处理分歧，并报告实际评定人数和流程。评定同时检查“动作此刻可用”与“动作符合学习目的”；例如展开界面中机械地推荐退出，可能合法但不合目的。原始问题固定，格式修复不算新用户请求。

建议报告首次结构通过率、最终目标匹配率、正常问题拒答率及服务错误率；分别以首次候选数、范围内请求数为分母，并保留每项计数。格式修复成功率以发生修复的请求为分母，同时说明修复后是否仍通过语义与客户端检查。边界样例单独报告，不混入正常请求的拒答率。端到端时间从用户提交至客户端得到可显示结果或终止错误计算，包含修复和审核；成功、失败及超时分别汇总。

> **【插图 5 占位｜可选，必须有真实数据】** 优先用一张单栏图展示范围内六类场景的最终结果分布：目标匹配、合法但不合目标、拒答、格式/审核错误、连接/超时错误，采用互斥分类并标注每类请求数。如页数允许，用第二面板显示端到端时间散点或分布。不要用 mock 数据、推测的提升比例、只有均值却虚构的误差条。现有 SUS/TLX 表无需再画一张重复的柱状图。
>
> **拟图注（数据补齐后使用）：** 图 5. 真实模型在预定义状态场景中的最终指引结果。分布以用户请求为单位，包含格式修复与审核后的结果；延迟包含完整问答处理链路。




## AIxVR2027_VRMicroscope_Paper_EN_Formal


> **[EDITORIAL NOTE — REMOVE BEFORE SUBMISSION]** Figures 1, 2, and 4 below use runtime captures supplied in `Pic/`. Figure 3 now includes the original draw.io vector architecture diagram, and the optional evaluation figure remains conditional on real model-test data. The [figure plan](AIxVR2027_Figure_Plan.md) records the selection and remaining editorial checks.
> **[STUDY RECORD CHECK — COMPLETE AND REMOVE BEFORE SUBMISSION]** The authors confirm that the study exists and that original materials are outside the repository. Specify the evaluated build/date, PC-VR or standalone/gateway setup, recruitment and prior experience, task success/timeout rules, researcher assistance, and actual AI/speech use. Reconcile the retained summaries with questionnaires and logs. Absence from the repository does not establish loss of original records.
> **[PROPOSED EVALUATION — NOT AN EXISTING RESULT]** This section specifies a recommended live-model protocol. Replace it with the actual configuration, counts, and results after execution. If it is not run before submission, move the protocol to future work and remove the Figure 5 placeholder; do not claim evaluated AI quality in the abstract or conclusion.
> **[FIGURE 5 PLACEHOLDER — OPTIONAL; REAL DATA REQUIRED]** Prefer a single-column plot of final outcomes for the six in-scope categories: goal-matched, legal but goal-mismatched, refusal, format/review error, and connection/timeout error. Use mutually exclusive categories and label request counts. If space permits, add an end-to-end timing distribution or individual points. Do not use mock results, hypothetical gains, or invented error bars. Do not duplicate the existing SUS/TLX table with an equivalent bar chart.
>
> **Proposed caption, after data collection:** Fig. 5. Final guidance outcomes for the live model across predefined state scenarios. Distributions use user requests as the unit and include outcomes after format repair and review; timing covers the complete question-answering pipeline.

### 6.6 Supplementary Protocol for AI Guidance Quality

> **[PROPOSED EVALUATION — NOT AN EXISTING RESULT]** This section specifies a recommended live-model protocol. Replace it with the actual configuration, counts, and results after execution. If it is not run before submission, move the protocol to future work and remove the Figure 5 placeholder; do not claim evaluated AI quality in the abstract or conclusion.

A proposed suite contains 30 in-scope question/history/state cases: five each for generic next steps, component-experiment entry, cross-module exit, SNOM waiting, instrument/specimen localization, and necessary clarification. Ten additional cases cover out-of-scope requests or unavailable capabilities. Run each case independently three times with fixed snapshots. Record model identity, prompt/knowledge versions, sampling parameters, candidates, regeneration, review, and client outcomes. These are proposed sample counts, not completed trials.

Before running the suite, evaluators familiar with the workflow should define acceptable actions or response categories for each in-scope case, allowing multiple valid next steps. Assess independently before resolving disagreements and report the actual number of evaluators and procedure. Judge both availability and correspondence to the learning goal: suggesting exit from an expanded model may be legal but unhelpful. A format repair remains part of the original user request.

Report first-candidate structural acceptance, final goal matching, refusal of normal questions, and service-error rates, with counts and explicit denominators: initial candidates for the first metric and in-scope requests for the others. Repair success uses repaired requests as its denominator and must state whether semantic and client checks also passed. Boundary cases are reported separately. Measure end-to-end time from user submission to a displayable result or terminal error, including repair and review, and distinguish success, failure, and timeout outcomes.

> **[FIGURE 5 PLACEHOLDER — OPTIONAL; REAL DATA REQUIRED]** Prefer a single-column plot of final outcomes for the six in-scope categories: goal-matched, legal but goal-mismatched, refusal, format/review error, and connection/timeout error. Use mutually exclusive categories and label request counts. If space permits, add an end-to-end timing distribution or individual points. Do not use mock results, hypothetical gains, or invented error bars. Do not duplicate the existing SUS/TLX table with an equivalent bar chart.
>
> **Proposed caption, after data collection:** Fig. 5. Final guidance outcomes for the live model across predefined state scenarios. Distributions use user requests as the unit and include outcomes after format repair and review; timing covers the complete question-answering pipeline.

