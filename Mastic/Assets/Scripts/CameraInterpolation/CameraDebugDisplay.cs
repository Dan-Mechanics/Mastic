using UnityEngine;

namespace Mastic
{
    public class CameraDebugDisplay : MonoBehaviour
    {
        [SerializeField] private Color color = default;
        [SerializeField] private int padding = default;
        [SerializeField] private int width = default;
        [SerializeField] private int height = default;
        private ICameraInterpolation interpolation;

        private void Awake() => interpolation = GetComponent<ICameraInterpolation>();

        private void OnGUI()
        {
            if (interpolation == null)
                return;
            
            GUI.color = color;
            Rect rect = new Rect(Screen.width - width - padding, Screen.height - height - padding, width, height);
            GUIStyle style = GUI.skin.GetStyle("Label");
            style.alignment = TextAnchor.MiddleRight;
            GUI.Label(rect, $"v {interpolation.Value} | i {interpolation.IsInterjected}", style);
            GUI.color = Color.white;
        }
    }
}