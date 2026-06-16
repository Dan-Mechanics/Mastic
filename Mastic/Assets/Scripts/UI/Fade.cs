using UnityEngine;
using UnityEngine.UI;

namespace Mastic
{
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(CanvasGroup))]
    public class Fade : MonoBehaviour
    {
        [SerializeField] private float fadeSpeed = default;
        private CanvasGroup canvasGroup;
        private bool becomeVisible;
        private Image image;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            image = GetComponent<Image>();
        }

        private void Start()
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            Color temp = image.color;
            temp.a = 1f;
            image.color = temp;
        }

        public void SetAs(bool becomeVisible)
        {
            this.becomeVisible = becomeVisible;
            canvasGroup.alpha = (becomeVisible ? 0f : 1f);
        }

        private void Update()
            => canvasGroup.alpha += (becomeVisible ? 1f : -1f) * fadeSpeed * Time.deltaTime;

        public void Flash()
            => SetAs(false);
    }
}
