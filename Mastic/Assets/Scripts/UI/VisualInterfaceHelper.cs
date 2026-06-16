using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Mastic
{
    public class VisualInterfaceHelper : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private UnityEvent onPointerEnter = default;
        [SerializeField] private UnityEvent onPointerExit = default;

        private void Start()
            => onPointerExit?.Invoke();

        public void OnPointerEnter(PointerEventData eventData)
            => onPointerEnter?.Invoke();

        public void OnPointerExit(PointerEventData eventData)
            => onPointerExit?.Invoke();
    }
}
