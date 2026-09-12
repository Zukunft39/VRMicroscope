using UnityEngine;
using UnityEngine.EventSystems;

namespace VRMicroscope.Assistant
{
    public sealed class AssistantReadingSurface : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public LocalAssistantController owner;
        private int hoveringPointers;
        public void OnPointerEnter(PointerEventData eventData)
        {
            hoveringPointers++;
            if (owner != null) owner.SetPointerReading(true);
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            hoveringPointers = Mathf.Max(0, hoveringPointers - 1);
            if (owner != null) owner.SetPointerReading(hoveringPointers > 0);
        }
        private void OnDisable()
        {
            hoveringPointers = 0;
            if (owner != null) owner.SetPointerReading(false);
        }
    }
}
