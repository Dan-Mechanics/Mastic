using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mastic
{
    public class PopupHandler : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group = default;
        [SerializeField] private TMP_Text text = default;
        [SerializeField] private Image image = default;
        [SerializeField] private Color defaultColor = default;
        [SerializeField, Range(0.1f, 0.9f)] private float backdropVisibility = default;
        [SerializeField, Min(0.1f)] private float defaultDuration = default;
        private float currentDuration;

        private void Awake()
        {
            currentDuration = defaultDuration;
            Send(string.Empty);
        }

        private void FixedUpdate()
        {
            group.alpha -= 1f / currentDuration * Time.fixedDeltaTime;
            group.alpha = Mathf.Clamp01(group.alpha);
        }

        public void Send(string str, Color color = default, float duration = default)
        {
            if (!Utils.IsStringValid(str))
            {
                group.alpha = 0f;
                text.text = string.Empty;
                return;
            }

            print(str);
            currentDuration = duration > 0 ? duration : defaultDuration;
            group.alpha = 1f;
            text.text = str;

            if (color.a <= 0f)
                color = defaultColor;

            color.a = backdropVisibility;
            image.color = color;
        }
    }
}
