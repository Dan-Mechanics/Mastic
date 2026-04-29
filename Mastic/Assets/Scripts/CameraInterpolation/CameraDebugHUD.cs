using UnityEngine;

namespace Mastic
{
    public class CameraDebugHUD : MonoBehaviour
    {
        [SerializeField] private Color color = default;
        [SerializeField] private int padding = default;
        [SerializeField] private int width = default;
        [SerializeField] private int height = default;
        private ICameraInterpolation interpolation;

        private void OnGUI()
        {
            interpolation ??= GetComponent<ICameraInterpolation>();

            GUI.color = color;
            Rect rect = new Rect(Screen.width - width - padding, Screen.height - height - padding, width, height);
            GUIStyle style = GUI.skin.GetStyle("Label");
            style.alignment = TextAnchor.MiddleRight;
            GUI.Label(rect, $"{interpolation.LerpValue} | {interpolation.IsInterjected}", style);
            GUI.color = Color.white;
        }
    }
}