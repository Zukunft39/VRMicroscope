using UnityEngine;

namespace VRMicroscope.Assistant
{
    [CreateAssetMenu(menuName = "VR Microscope/Local Assistant Settings")]
    public sealed class LocalAssistantSettings : ScriptableObject
    {
        public bool assistantEnabled = true;
        public KeyCode activationKey = KeyCode.H;
        [Min(5)] public float idleSeconds = 30;
        [Min(4)] public float messageSeconds = 10;
        [Tooltip("0 repeats every quiet interval; a positive value limits reminders until the next player action.")]
        [Range(0, 5)] public int maxRemindersPerIdlePeriod = 0;
        public bool reducedMotion;
        public Font chineseFont;
        [Header("Knowledge chat (key stays on the backend)")]
        public string chatEndpoint = "http://127.0.0.1:8765/assistant";
        [Range(5,90)] public int chatTimeoutSeconds = 50;
        [Header("Learning location markers")]
        public bool navigationEnabled = true;
        [Min(.1f)] public float arrivalDistanceMeters = 1.2f;
        [Min(.01f)] public float worldUnitsPerMeter = 1f;
        [Min(10f)] public float markerLifetimeSeconds = 120f;
        [Header("Typewriter / Terminal style output")]
        public bool typewriterEnabled = true;
        [Range(10, 120)] public float charactersPerSecond = 45f;
        [TextArea(2, 4)] public string[] greetings = {
            "你好，我在这里。今天想从显微世界的哪一部分开始探索？",
            "欢迎回来！不必着急，按自己的节奏观察就好。",
            "嗨，今天探索得怎么样？有困惑时，可以先把问题记下来。",
            "你好！一个小小的观察，也可能带来新的发现。",
            "我在这里陪你探索。先看看眼前最让你好奇的部分吧。",
            "欢迎来到微观世界。今天想关注光，还是关注样本？",
            "你好，今天状态怎么样？累了也可以停下来休息一下。",
            "嗨！不需要一次理解所有原理，我们可以一点一点来。",
            "很高兴再次见到你。试着留意实验画面里发生了什么变化。",
            "你好！观察现象、提出问题，都是学习的一部分。",
            "我收到你的呼唤啦。今天有什么让你好奇的现象？",
            "嗨，探索者！不同的显微方法，会带来不同的观察视角。",
            "你好！可以先认识部件，再慢慢理解它们之间的联系。",
            "欢迎继续探索。看不懂的地方，可以先停下来仔细观察。",
            "我在这里。把注意力放在一个现象上，也是一种不错的开始。",
            "你好！今天可以从一个简单的问题开始：这个变化意味着什么？"
        };
        [TextArea(1, 3)] public string[] reminderTemplates = {
            "也许我们可以把目光放到{0}上。",
            "对{0}感到好奇吗？可以先留意相关的演示与说明。",
            "接下来，不妨探索一下{0}。",
            "如果想换一个观察角度，可以关注{0}。",
            "想继续探索的话，{0}是一个可以关注的主题。",
            "不必着急。准备好时，可以再看看{0}。",
            "也许{0}能带来一个新的观察问题。",
            "休息一下也没关系，之后可以继续了解{0}。"
        };
    }
}
