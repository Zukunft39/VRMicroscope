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
        [Header("Voice input (Groq key stays on the backend)")]
        public string speechEndpoint = "http://127.0.0.1:8765/speech";
        [Range(10,90)] public int speechTimeoutSeconds = 50;
        [Header("Learning location markers")]
        public bool navigationEnabled = true;
        [Min(.1f)] public float arrivalDistanceMeters = 1.2f;
        [Min(.01f)] public float worldUnitsPerMeter = 1f;
        [Min(10f)] public float markerLifetimeSeconds = 120f;
        [Header("Typewriter / Terminal style output")]
        public bool typewriterEnabled = true;
        [Range(10, 120)] public float charactersPerSecond = 45f;
        [TextArea(2, 4)] public string[] greetings = {
            "Hello! Which part of the microscopic world would you like to explore today?",
            "Welcome back! Take your time and explore at your own pace.",
            "Hi! How is your exploration going? Feel free to note down any questions.",
            "Hello! Even a small observation can lead to a new discovery.",
            "I am here to help you explore. Start with whatever catches your curiosity.",
            "Welcome to the microscopic world. Shall we explore light or specimens today?",
            "Hello! How are you feeling today? Take a break whenever you need one.",
            "Hi! We can explore the principles one step at a time.",
            "Good to see you again. Try noticing what changes in the experiment view.",
            "Hello! Observing and asking questions are both part of learning.",
            "I am here! What has caught your curiosity today?",
            "Hi, explorer! Different microscopy methods offer different perspectives.",
            "Hello! Get to know the components, then explore how they work together.",
            "Welcome back to exploring. Pause for a closer look whenever something is unclear.",
            "I am here. Focusing on one observation is a good place to start.",
            "Hello! Start with a simple question: what does this change mean?"
        };
        [TextArea(1, 3)] public string[] reminderTemplates = {
            "Perhaps we could explore {0}.",
            "Curious about {0}? Take a look at the related demonstrations and explanations.",
            "Next, you could take a closer look at {0}.",
            "For a different perspective, try exploring {0}.",
            "If you would like to continue, {0} is a topic worth exploring.",
            "Take your time. When you are ready, you can explore {0}.",
            "Perhaps {0} will inspire a new question.",
            "Feel free to take a break, then come back to {0}."
        };
    }
}
