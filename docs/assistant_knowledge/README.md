# Assistant Knowledge Directory

English runtime edition: 2026-09-17. The local assistant, free-form chat, state-based operating guidance, location markers, and voice transcription are implemented. See [English localization and testing](ENGLISH_LOCALIZATION.md).

## Active files

| File | Purpose |
|---|---|
| SYSTEM_PROMPT.md | English generation rules, scientific limits, navigation/operation policy, and JSON output contract |
| PROJECT_KNOWLEDGE.md | Supported project explanations and actual capability boundaries |
| interaction_catalog.json | 23 stable interaction IDs with static evidence, conditions, device instructions, and sources |
| INTERACTIONS.md | Readable English guide generated from the interaction catalog |
| deepseek_reference_pack.md | Synchronized English reference pack for offline inspection; runtime snapshots are still required |
| context_example.json | Legacy illustrative context only; never actual player state |
| ENGLISH_LOCALIZATION.md | Deployment, English UI checks, preservation boundaries, and test commands |

The backend loads SYSTEM_PROMPT.md, PROJECT_KNOWLEDGE.md, and interaction_catalog.json as UTF-8. Operating instructions come from Assets/Resources/AssistantGuidanceActions.json. Current context comes from Unity through GUIDANCE_CONTEXT and NAVIGATION_CONTEXT. A model cannot read local files merely because a path is included in a question.

The shared catalog and runtime state decide which individual action can be recommended. A static interaction entry with approved_for_guidance=false does not veto an action explicitly offered by the live allowlist. Conversely, static_confirmed never establishes device-test success. Runtime verification flags have not been changed by translation.

## Request flow

1. The backend loads and versions trusted knowledge at startup.
2. Unity submits a question, recent conversation, and available current snapshots.
3. The backend provides project facts, valid current actions, and navigation targets to generation.
4. It checks response structure and identifiers, renders canonical operating prose, and performs semantic review. A failed structural response may receive one bounded repair using the same permissions.
5. Unity rechecks freshness and availability before showing an operating reminder or creating a marker. The learner performs the action.
6. Voice recording uses Groq transcription to create an editable draft. Only confirmation sends the question to chat.

Restart the backend after changing knowledge or catalogs. DeepSeek answers in English; transcription preserves the spoken language. Keep credentials on the backend.

## Historical implementation records

LOCAL_ASSISTANT_IMPLEMENTATION.md, CHAT_IMPLEMENTATION.md, VOICE_IMPLEMENTATION.md, NAVIGATION_IMPLEMENTATION.md, GUIDANCE_IMPLEMENTATION.md, INPUT_AND_GAPS.md, and PROMPT_DESIGN_REVIEW.md record earlier development stages and device-test plans. They are not loaded into the model by the current backend. Where historical stage-two statements conflict with current behavior, use the active files listed above.

source_audit.json is a historical source-hash snapshot, not proof of runtime correctness. navigation_scene_audit.json describes static scene positions; current positions come from runtime snapshots. Neither static audit can grant current controls or establish that a player has completed an experiment.
