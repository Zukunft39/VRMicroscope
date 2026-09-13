using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VRMicroscope.Assistant
{
    public sealed class AssistantChatPanel : MonoBehaviour
    {
        private static AssistantChatPanel active;
        private static int closedFrame=-1;
        public static bool BlocksGameplay => active != null && active.gameObject.activeInHierarchy || Time.frameCount==closedFrame;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { active=null; closedFrame=-1; }
        private LocalAssistantController owner;
        private LocalAssistantSettings settings;
        private InputField input;
        private Text transcript, status, sendLabel, cancelLabel;
        private Button send, cancel;
        private Button voiceButton;
        private Text voiceLabel;
        private AssistantVoiceInput voice;
        private bool VoiceBusy => voice != null && voice.Busy;
        private ScrollRect scroll;
        private CancellationTokenSource pending;
        private readonly List<AssistantChatTurn> history=new List<AssistantChatTurn>();
        private string session=Guid.NewGuid().ToString("N");
        private float nextSend;
        private int generation;
        private GameObject shield, keyboard;
        private Coroutine typingRoutine;
        private bool isTyping;
        private string fullAnswerToType, fullAnswerSource;

        public void Build(Transform parent,LocalAssistantController controller,LocalAssistantSettings config)
        {
            owner=controller; settings=config;
            voice=gameObject.AddComponent<AssistantVoiceInput>();
            voice.StatusChanged += message => { if(status!=null && gameObject.activeInHierarchy) status.text=message; };
            voice.Transcribed += (text, mock) =>
            {
                if (!gameObject.activeInHierarchy) return;
                string draft=string.IsNullOrWhiteSpace(input.text) ? text : input.text.TrimEnd()+"\n"+text;
                if (draft.Length>input.characterLimit)
                    status.text="草稿加上转写超过 1000 字，请先精简草稿后重新录制。原有文字已保留。";
                else
                {
                    input.text=draft;
                    status.text=mock ? "模拟转写（固定测试句，非语音识别）。" : "语音已转为文字，请检查后点击发送。";
                }
                owner.ReportActivity();
            };
            var root=(RectTransform)transform;
            root.SetParent(parent,false); root.anchorMin=root.anchorMax=root.pivot=new Vector2(0,1);
            root.anchoredPosition=new Vector2(154,-28); root.sizeDelta=new Vector2(860,540);
            var backdrop=new GameObject("Question Modal Shield",typeof(RectTransform),typeof(Image));
            var backdropRect=(RectTransform)backdrop.transform;
            backdropRect.SetParent(parent,false); backdropRect.anchorMin=Vector2.zero; backdropRect.anchorMax=Vector2.one;
            backdropRect.offsetMin=backdropRect.offsetMax=Vector2.zero;
            backdrop.GetComponent<Image>().color=new Color(.015f,.03f,.055f,.12f);
            shield=backdrop; shield.SetActive(false); root.SetAsLastSibling();
            var image=gameObject.AddComponent<Image>(); image.color=new Color(.025f,.055f,.095f,.82f);
            Label(root,"Header",new Vector2(22,-15),new Vector2(720,30),24,"微观问答");
            ButtonAt(root,"关闭",new Vector2(782,-12),new Vector2(60,32),Close);
            string[] titles={"显微镜", "NA", "空间频率", "共聚焦", "SNOM"};
            string[] questions={"显微镜的物镜有什么作用？", "NA 和倍率有什么区别？", "空间频率模块展示了什么？", "共聚焦针孔为什么能抑制离焦信号？", "THz s-SNOM 的近场耦合是什么？"};
            for(int i=0;i<titles.Length;i++)
            {
                string question=questions[i];
                ButtonAt(root,titles[i],new Vector2(22+i*163,-53),new Vector2(151,30),()=>
                { if(pending==null && !isTyping && !VoiceBusy) { input.text=question; owner.ReportActivity(); } });
            }
            var viewport=Rect(root,"Reading Area",new Vector2(22,-95),new Vector2(812,237));
            viewport.gameObject.AddComponent<Image>().color=new Color(.08f,.16f,.23f,.30f);
            viewport.gameObject.AddComponent<RectMask2D>();
            scroll=viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal=false; scroll.movementType=ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity=30; scroll.viewport=viewport;
            transcript=Label(viewport,"Conversation",new Vector2(12,-8),new Vector2(782,221),21,
                "可以询问显微镜、NA、空间频率、共聚焦背景和 THz s-SNOM。\n\n想亲手学习时，可以问我相关区域在哪里，我会标记当前可前往的学习区域。");
            transcript.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            scroll.content=transcript.rectTransform;
            status=Label(root,"Status",new Vector2(22,-337),new Vector2(636,38),17,"可输入文字或录音转写，确认后点击发送。");
            voiceButton=ButtonAt(root,"语音输入",new Vector2(674,-337),new Vector2(160,36),()=>
            {
                if(pending!=null || isTyping) return;
                keyboard.SetActive(false); input.DeactivateInputField();
                owner.ReportActivity(); voice.Toggle(settings); RefreshButtons();
            });
            voiceLabel=voiceButton.GetComponentInChildren<Text>();
            var field=Rect(root,"Question Input",new Vector2(22,-380),new Vector2(812,82));
            field.gameObject.AddComponent<Image>().color=new Color(.12f,.23f,.30f,.7f);
            input=field.gameObject.AddComponent<InputField>();
            input.textComponent=Label(field,"Input Text",new Vector2(12,-8),new Vector2(788,65),21,"");
            var placeholder=Label(field,"Placeholder",new Vector2(12,-8),new Vector2(788,65),21,"例如：NA 和倍率有什么区别？");
            placeholder.color=new Color(.60f,.73f,.79f,.85f); input.placeholder=placeholder;
            input.lineType=InputField.LineType.MultiLineNewline; input.characterLimit=1000;
            input.onValueChanged.AddListener(_=>{ owner.ReportActivity(); RefreshButtons(); });
            ButtonAt(root,"新对话",new Vector2(22,-483),new Vector2(108,36),NewConversation);
            ButtonAt(root,"下一步",new Vector2(282,-483),new Vector2(118,36),()=>
            { if(pending==null && !isTyping && !VoiceBusy) { input.text="根据我当前的实验状态，接下来应该怎么操作？"; owner.ReportActivity(); } });
            ButtonAt(root,"如何退出",new Vector2(410,-483),new Vector2(142,36),()=>
            { if(pending==null && !isTyping && !VoiceBusy) { input.text="当前实验应该如何退出？"; owner.ReportActivity(); } });
            ButtonAt(root,"屏幕键盘",new Vector2(142,-483),new Vector2(126,36),()=>{ if(!VoiceBusy) keyboard.SetActive(!keyboard.activeSelf); });
            cancel=ButtonAt(root,"取消请求",new Vector2(575,-483),new Vector2(126,36),CancelOrSkip);
            cancelLabel=cancel.GetComponentInChildren<Text>();
            send=ButtonAt(root,"发送",new Vector2(711,-483),new Vector2(123,36),Send);
            sendLabel=send.GetComponentInChildren<Text>();
            BuildKeyboard(root);
            RefreshButtons();
            gameObject.SetActive(false);
        }
        public void Open()
        {
            if (active!=null && active!=this) active.Close();
            owner.Dismiss();
            gameObject.SetActive(true); active=this; owner.ReadingOrTyping=true; owner.ReportActivity();
            shield.SetActive(true);
            BringToFront();
            // Do not select the field automatically: XR users can first choose a question or keyboard.
        }
        public void Close() { CancelPending(); gameObject.SetActive(false); }
        private void BringToFront()
        {
            // The shield must cover sibling HUD controls while remaining behind the dialog.
            if (shield != null) shield.transform.SetAsLastSibling();
            transform.SetAsLastSibling();
        }
        private void LateUpdate()
        {
            if (active == this) BringToFront();
        }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && string.IsNullOrEmpty(Input.compositionString)) Close();
            if (isTyping && Input.GetKeyDown(KeyCode.Space) && !input.isFocused) FinishTypingImmediately();
            RefreshButtons();
        }
        private void NewConversation()
        {
            owner.Navigation.Clear();
            owner.Guidance.Clear();
            CancelPending(); history.Clear(); session=Guid.NewGuid().ToString("N");
            input.text=""; transcript.text="新的对话开始了。你想了解什么？";
            status.text="仅保留当前会话最近四轮问答。";
            owner.ReportActivity();
        }
        private void BuildKeyboard(RectTransform parent)
        {
            var rect=Rect(parent,"Screen Keyboard",new Vector2(22,-95),new Vector2(812,237));
            keyboard=rect.gameObject;
            keyboard.AddComponent<Image>().color=new Color(.03f,.09f,.14f,.99f);
            string[] rows={"1234567890", "qwertyuiop", "asdfghjkl", "zxcvbnm,.?"};
            for(int row=0;row<rows.Length;row++)
                for(int col=0;col<rows[row].Length;col++)
                {
                    string character=rows[row][col].ToString();
                    ButtonAt(rect,character,new Vector2(8+col*79,-8-row*44),new Vector2(72,37),()=>
                    { if(pending==null && !VoiceBusy && input.text.Length<1000) { input.text+=character; owner.ReportActivity(); } });
                }
            ButtonAt(rect,"空格",new Vector2(8,-188),new Vector2(300,38),()=>{ if(pending==null && !VoiceBusy && input.text.Length<1000) input.text+=" "; });
            ButtonAt(rect,"退格",new Vector2(318,-188),new Vector2(150,38),()=>
            { if(pending==null && !VoiceBusy && input.text.Length>0) input.text=input.text.Substring(0,input.text.Length-1); });
            ButtonAt(rect,"收起键盘",new Vector2(478,-188),new Vector2(318,38),()=>keyboard.SetActive(false));
            keyboard.SetActive(false);
        }
        private async void Send()
        {
            if (isTyping) { FinishTypingImmediately(); return; }
            string question=input.text.Trim();
            if (pending!=null || VoiceBusy || question.Length==0 || Time.unscaledTime<nextSend) return;
            owner.ReportActivity(); nextSend=Time.unscaledTime+2;
            var cts=new CancellationTokenSource(); pending=cts;
            int ticket=++generation;
            var request=new AssistantChatRequest {sessionId=session,requestId=Guid.NewGuid().ToString("N"),
                question=question,history=history.ToArray(),navigation=owner.Navigation.Capture(),guidance=owner.Guidance.Capture()};
            status.text="正在查阅项目资料并检查回答…";
            keyboard.SetActive(false);
            RefreshButtons();
            try
            {
                var response=await AssistantChatClient.Request(request,settings,cts.Token);
                if (this==null || !gameObject.activeInHierarchy || ticket!=generation || pending!=cts) return;
                if(response.kind=="guide" && response.suggested_action_ids[0].StartsWith("learn:",StringComparison.Ordinal))
                    response.answer=owner.Guidance.Apply(response,request.guidance);
                else
                {
                    if(response.kind=="guide") owner.Guidance.Clear();
                    response.answer=owner.Navigation.Apply(response,request.navigation);
                }
                history.Add(new AssistantChatTurn {role="user",content=question});
                history.Add(new AssistantChatTurn {role="assistant",content=response.answer});
                while(history.Count>8) history.RemoveRange(0,2);
                input.text="";

                if (settings != null && (!settings.typewriterEnabled || settings.reducedMotion))
                {
                    transcript.text=BuildTranscriptText(response.answer, false);
                    status.text=response.source=="mock" ? "联调模拟 · 非 AI 回答" : "DeepSeek · 项目知识问答";
                    Canvas.ForceUpdateCanvases(); scroll.verticalNormalizedPosition=0;
                }
                else
                {
                    if (typingRoutine != null) StopCoroutine(typingRoutine);
                    fullAnswerSource = response.source;
                    typingRoutine=StartCoroutine(TypewriterRoutine(response.answer, response.source));
                }
            }
            catch(OperationCanceledException) { }
            catch(Exception exception)
            {
                if (this!=null && ticket==generation && pending==cts)
                {
                    long code=exception is AssistantConnectionException connection ? connection.Status : 0;
                    status.text=code==429 ? "请求较多，请稍后点击发送重试。" :
                        code==502 ? "回答未能完成或通过检查，请点击发送重试。" :
                        "暂时无法连接问答服务，请确认后端已启动，再点击发送重试。";
                    // Keep the draft and prior answers; never present service errors as knowledge refusal.
                }
            }
            finally
            {
                if (this!=null && pending==cts) { pending=null; RefreshButtons(); }
                cts.Dispose();
            }
        }
        private void CancelOrSkip()
        {
            if (isTyping) { FinishTypingImmediately(); return; }
            CancelByUser();
        }
        private void FinishTypingImmediately()
        {
            if (!isTyping) return;
            if (typingRoutine != null) { StopCoroutine(typingRoutine); typingRoutine = null; }
            isTyping = false;
            if (!string.IsNullOrEmpty(fullAnswerToType))
            {
                transcript.text = BuildTranscriptText(fullAnswerToType, false);
                status.text = fullAnswerSource == "mock" ? "联调模拟 · 非 AI 回答" : "DeepSeek · 项目知识问答";
                Canvas.ForceUpdateCanvases(); scroll.verticalNormalizedPosition = 0;
            }
            fullAnswerToType = null;
            fullAnswerSource = null;
            RefreshButtons();
        }
        private string BuildTranscriptText(string currentAssistantText, bool showCursor)
        {
            var lines = new List<string>();
            for (int i = 0; i < history.Count - 2; i += 2)
                lines.Add("你：" + history[i].content + "\n\n助手：" + history[i + 1].content);
            if (history.Count >= 2)
            {
                string userMsg = history[history.Count - 2].content;
                string cursor = showCursor ? " _" : "";
                lines.Add("你：" + userMsg + "\n\n助手：" + currentAssistantText + cursor);
            }
            return string.Join("\n\n────────────\n\n", lines);
        }
        private System.Collections.IEnumerator TypewriterRoutine(string targetAnswer, string source)
        {
            isTyping = true;
            fullAnswerToType = targetAnswer;
            status.text = (source == "mock" ? "联调模拟 · 正在输出回答…" : "DeepSeek · 正在逐字输出…") + " (可按空格或点击「跳过打字」立即显示)";
            RefreshButtons();

            float cps = (settings != null && settings.charactersPerSecond > 0) ? settings.charactersPerSecond : 45f;
            float baseDelay = 1f / cps;

            var sb = new System.Text.StringBuilder();
            float lastScrollTime = 0f;
            int length = targetAnswer.Length;

            for (int i = 0; i < length; i++)
            {
                char c = targetAnswer[i];
                sb.Append(c);

                bool cursorVisible = (i % 6 < 4);
                transcript.text = BuildTranscriptText(sb.ToString(), cursorVisible);

                if (Time.unscaledTime - lastScrollTime > 0.05f || c == '\n' || i == length - 1)
                {
                    Canvas.ForceUpdateCanvases();
                    scroll.verticalNormalizedPosition = 0;
                    lastScrollTime = Time.unscaledTime;
                }

                if (c == '\n')
                {
                    yield return new WaitForSecondsRealtime(baseDelay * 2.8f);
                }
                else if (c == '。' || c == '！' || c == '？' || c == '；' || c == '.' || c == '!' || c == '?')
                {
                    yield return new WaitForSecondsRealtime(baseDelay * 2.2f);
                }
                else if (c == '，' || c == '、' || c == ',')
                {
                    yield return new WaitForSecondsRealtime(baseDelay * 1.5f);
                }
                else
                {
                    yield return new WaitForSecondsRealtime(baseDelay);
                }
            }

            transcript.text = BuildTranscriptText(targetAnswer, false);
            status.text = source == "mock" ? "联调模拟 · 非 AI 回答" : "DeepSeek · 项目知识问答";
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 0;

            isTyping = false;
            typingRoutine = null;
            fullAnswerToType = null;
            fullAnswerSource = null;
            RefreshButtons();
        }
        private void CancelByUser()
        {
            CancelPending(); status.text="已取消，问题仍保留在输入框中。"; owner.ReportActivity();
        }
        private void CancelPending()
        {
            if(VoiceBusy && status!=null) status.text="语音已取消，原有草稿已保留。";
            if(voice!=null) voice.Cancel();
            generation++;
            var cancellation=pending; pending=null;
            cancellation?.Cancel();
            if (typingRoutine != null) { StopCoroutine(typingRoutine); typingRoutine = null; }
            isTyping = false;
            fullAnswerToType = null;
            fullAnswerSource = null;
            RefreshButtons();
        }
        private void RefreshButtons()
        {
            if(send!=null) send.interactable=pending==null && !isTyping && !VoiceBusy && !string.IsNullOrWhiteSpace(input.text) && Time.unscaledTime>=nextSend;
            if(cancel!=null) cancel.interactable=pending!=null || isTyping || VoiceBusy;
            if(cancelLabel!=null) cancelLabel.text=VoiceBusy ? "取消语音" : isTyping ? "跳过打字" : "取消请求";
            if(sendLabel!=null) sendLabel.text=pending!=null ? "等待回答" : isTyping ? "正在输出" : "发送";
            if(input!=null) input.interactable=pending==null && !isTyping && !VoiceBusy;
            if(voiceButton!=null) voiceButton.interactable=pending==null && !isTyping && (!VoiceBusy || voice.Recording);
            if(voiceLabel!=null) voiceLabel.text=voice.Recording ? $"停止 {voice.Seconds:00}/30" : VoiceBusy ? "处理中…" : "语音输入";
        }
        private void OnDisable()
        {
            if((pending!=null || VoiceBusy) && status!=null) status.text="已取消，问题仍保留在输入框中。";
            CancelPending();
            if(active==this) { active=null; closedFrame=Time.frameCount; }
            if(shield!=null) shield.SetActive(false);
            if(owner!=null) { owner.ReadingOrTyping=false; owner.ReportActivity(); }
            if(EventSystem.current!=null) EventSystem.current.SetSelectedGameObject(null);
        }
        private void OnDestroy() { if(shield!=null) Destroy(shield); }
        private RectTransform Rect(Transform parent,string name,Vector2 pos,Vector2 size)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent,false); r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);
            r.anchoredPosition=pos; r.sizeDelta=size; return r;
        }
        private Text Label(Transform parent,string name,Vector2 pos,Vector2 size,int fontSize,string value)
        {
            var text=Rect(parent,name,pos,size).gameObject.AddComponent<Text>();
            text.font=TMPro.TMP_Settings.defaultFontAsset.sourceFontFile; text.fontSize=fontSize; text.text=value;
            text.color=new Color(.88f,.95f,1); text.raycastTarget=false; text.supportRichText=false;
            text.alignment=TextAnchor.UpperLeft; text.horizontalOverflow=HorizontalWrapMode.Wrap;
            text.verticalOverflow=VerticalWrapMode.Truncate; return text;
        }
        private Button ButtonAt(Transform parent,string title,Vector2 pos,Vector2 size,UnityEngine.Events.UnityAction action)
        {
            var r=Rect(parent,title,pos,size); var image=r.gameObject.AddComponent<Image>();
            image.color=new Color(.16f,.32f,.41f,.85f);
            var button=r.gameObject.AddComponent<Button>(); button.targetGraphic=image;
            button.navigation=new Navigation {mode=Navigation.Mode.None};
            button.onClick.AddListener(action);
            Label(r,"Label",Vector2.zero,size,19,title).alignment=TextAnchor.MiddleCenter;
            return button;
        }
    }
}
