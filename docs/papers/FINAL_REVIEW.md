# Final document audit — 2026-09-19

## Current exports after manual review

Both Formal Markdown sources and all eight DOCX/PDF files in `正式版/` and `匿名版/` now include the fixed-state gateway regression and subsequent author review. Previous exports that omitted the benchmark and stated it had not been conducted have been replaced.

| Variant | PDF pages | References start |
|---|---:|---:|
| Chinese, identified | 9 | 9 |
| English, identified | 9 | 9 |
| Chinese, anonymous | 9 | 9 |
| English, anonymous | 9 | 9 |

Microsoft Word exported all PDFs. In these exports, body text including ethics and AI disclosure ends on page 8; page 9 contains references. Pagination may change with another renderer. This is a layout audit, not confirmation of submission eligibility.

### Reported evidence

- The fifth column (`validity`) of `HUMAN_REVIEW_QA_20260919_V4_ZH.csv` contains 75 correct and 3 incorrect labels: **75/78 (96.2%)** author acceptance. All incorrect labels concern GC01.
- Within the original 42-request operating cohort, **39/42 (92.9%)** final decisions were accepted. Disputed requests remain in the denominator. Raw/final action availability remains **42/42**, original goal-label conformance is **42/42 under the original rubric**, and navigation matches remain **12/12**.
- GC01 originally included an NA-learning history omitted from the question–answer review sheet. The original rubric rewarded expansion; subsequent review required clarification. Both languages disclose this as a post-run interpretation/rubric change.
- This is one binary author review of translated final answers, without independent raters or agreement analysis. The later clarification-policy revision is not presented as an executed real-model rerun. Median 1.82 s and P95 2.30 s refer to the original v4 run.

### Export checks

- Updated abstracts, Section 6.6, Table V, discussion, and conclusions. Fixed the export script that deleted Section 6.6 and inserted the obsolete unexecuted-evaluation statement.
- Adjusted figure placement and numeric-table column widths and condensed repeated prose; retained body font sizes.
- Preserved all seven previously repaired editable display-equation trees exactly. Inspected rendered benchmark and SIM-equation pages. Figure 3 retains SVG artwork and PNG fallback.
- Anonymous DOCX XML and PDF text/metadata contain none of the supplied author names, emails, or institutional names. Identified copies retain the seven authors.
- No visible editorial placeholders, obsolete unexecuted-evaluation statements, tracked insertions/deletions, or populated comments were found in the exports.
- Published files were verified by SHA-256 against audited staging copies. Backups: `../../Temp/PaperHumanReview/before/`; audit and hashes: `../../Temp/PaperHumanReview/audit.json`.

The earlier non-Formal Markdown drafts are not the current export sources. Supplied user-study records and ethics classification remain author-provided and were not independently verified by this document audit.

---

# Historical audit notes — superseded by the current export audit above

The following entries describe earlier snapshots and are retained only as history.

## Markdown benchmark update — 2026-09-19

The four manuscript Markdown sources now include the completed 78-request v4 fixed-state gateway regression. The formal bilingual sources also update the abstract, architecture, evaluation boundaries and conclusion. The earlier statements below about an unexecuted model evaluation describe the 17 September exports only. DOCX/PDF files have not been regenerated in this update; their page count and benchmark content must be checked after export. The new evidence does not include independent human prose ratings, live-client Apply/marker tests or stale-response trials. Source records and reproduction instructions are in `../benchmarks/fixed_state_ai/RESULTS_20260919_V4.md`.

## Completed

- Audited both `AIxVR2027_VRMicroscope_*_Formal.docx` files. No visible placeholder tokens, TODO/TBD labels, pending artwork notices, tracked insertions/deletions, or comment anchors remain. Removed the stale pending-declarations metadata comment.
- Added Figure 3 to both papers, with a full-width caption. The DOCX contains SVG artwork and a PNG fallback for viewers without SVG support.
- Editable source: `Pic/fig03_architecture.drawio`. Exported with official draw.io desktop 31.4.5 to SVG, PNG and PDF alongside the source.
- Checked the architecture against `Tools/ai_tutor/assistant_chat.py`, `speech.py`, and the Unity assistant chat, guidance and navigation code. The drawing distinguishes transcription, trusted knowledge, candidate generation, bounded format repair, semantic review, client validation and learner-executed operations.
- Reflowed the long SIM equation without changing its terms; both documents retain seven editable display equations.
- Replaced the SIM equation's renderer-sensitive `aligned`/`\quad` controls with a renderer-neutral `gathered` layout. The formula was checked in the regenerated Word OMML and PDF text/rendering; no square placeholder or visible alignment marker remains.
- Preserved the supplied seven-author list, corresponding-author designations, ethics statement and study results. Original DOCX backups are in `Temp/PaperLayout/before_fig03/`.

## Submission status

The identified temporary publication markers are absent from both DOCX files. After prose compression, LibreOffice PDF inspection gives 9 pages for each paper. The English references begin on page 8 and the Chinese references begin on page 8, so both bodies fit within an eight-page body limit in this export. Page breaks can differ slightly in Microsoft Word and should be checked once more in the official submission template.

The current versions contain author identities; use an anonymized version if required for the selected review track. External study records, ethics classification and author details remain author-provided information, not independently verified by this file audit. The statement that the proposed live-model evaluation has not yet been conducted is substantive and has intentionally been retained.

Editorial notes and the unimplemented evaluation protocol remain in source Markdown / AUTHOR_CHECKLIST.md for author use; they are not included in the final DOCX body. No study findings have been invented to fill these gaps.

## Anonymous review copies

Separate `_Anonymous` DOCX and PDF files were generated for double-blind Full/Short Paper review. They replace the complete author/affiliation block, corresponding-author details, document-author metadata, and the UESTC name in the ethics statement with anonymous wording. Package XML and PDF text/metadata were scanned for all seven names, institutional names, student numbers, and author email domains; no matches or editorial placeholders remain. The signed files are retained separately for camera-ready use.
