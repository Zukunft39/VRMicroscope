# English Runtime and Assistant Content

The application-owned visible UI, editor labels, assistant greetings, idle reminders, chat examples, service status messages, navigation directions, refusal, and shared operation instructions use English.

The active model knowledge sources are SYSTEM_PROMPT.md, PROJECT_KNOWLEDGE.md, and interaction_catalog.json. The operating catalog is Assets/Resources/AssistantGuidanceActions.json. Generation, repair, and semantic-review rules are also English. The merged reference pack and interaction guide are synchronized with those sources.

Chinese questions remain supported. Microphone input still transcribes Chinese and English in the original spoken language into an editable draft. DeepSeek is instructed to answer in English; it does not translate the voice draft automatically. Local integration mocks have English responses and recognize the main English intents.

## Apply and verify

1. Stop Play Mode, let Unity recompile, and restart the Python backend so it reloads the knowledge bundle and shared catalog.
2. Press H or select the orb. Check several greetings. Leave the player idle for 30 seconds and check the English reminder.
3. Open Ask a question. Check topic buttons, Keyboard, Voice Input, Next Step, How to Exit, Send, Cancel, and New Chat.
4. With the backend in mock mode, try the five example topics and a next-step request in a selected component panel. Mock results are labeled and do not establish live-model quality.
5. In a component with an available experiment, Next Step should recommend its current start action; How to Exit should recommend leaving the selection. Near/far specimen requests should use the current target and English relative direction.
6. In real-model mode, submit an English question and a Chinese question about NA; the response should be English. An unrelated question should use the English fixed refusal.
7. Record English and Chinese questions. Review the original-language transcript before sending. Cancelling recording or a request must preserve the existing draft as before.

## Preservation boundaries

Original Chinese papers, historical reports, code comments, source-audit snapshots, and imported asset identifiers are not learner-facing localization content and remain intact. Chinese model-node lookup strings are kept because SNOM component discovery depends on the imported hierarchy; renaming only those strings would break component binding. Chinese mock-input aliases are compatibility data, not output text. Tutorial/video filenames and GUID references are not renamed. The media pixels/audio have not been re-recorded or translated.

Reviewed translation tables and the repeatable migration script live in Tools/local_assistant. These are development resources, not runtime dictionaries. No translation service, credential, or paid API call was used for this migration.

## Verification

Use `python Tools/local_assistant/verify.py` for runtime/editor compilation plus idle/WAV checks. Use `python -m unittest discover -s Tools/ai_tutor -p 'test_*.py'` for backend contracts and English-output routing. `python Tools/local_assistant/audit_english.py` lists remaining Chinese literals, including intentional imported-model lookup strings. Device rendering and real API behavior must additionally be checked in Unity.
