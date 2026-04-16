using UnityEngine;

namespace Mastic
{
    public class CameraHandlerGUI : MonoBehaviour
    {
        public CameraHandler cameraHandler;

        public Color color = Color.white;
        public int padding = 2;
        public int width = 150;
        public int height = 25;

        private void OnGUI()
        {
            GUI.color = color;
            Rect rect = new Rect(Screen.width - width - padding, Screen.height - height - padding, width, height);
            GUIStyle style = GUI.skin.GetStyle("Label");
            style.alignment = TextAnchor.MiddleRight;
            GUI.Label(rect, $"{cameraHandler.Value} | {cameraHandler.IsInterjected}".ToLower(), style);
            GUI.color = Color.white;
        }
    }
}