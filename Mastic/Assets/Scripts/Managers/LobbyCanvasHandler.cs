using UnityEngine;
using UnityEngine.UI;

namespace Mastic
{
    public class LobbyCanvasHandler : MonoBehaviour
    {
        [SerializeField] private GameObject prefab = default;
        [SerializeField] private EasyVar sens = default;
        [SerializeField] private EasyVar element = default;
        [SerializeField] private EasyVar maxConn = default;
        private GameObject canvas;
        private Dropdown elementField;
        private InputField sensField;
        private InputField maxConnField;
        
        public void Initialize()
        {
            canvas = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            elementField = canvas.transform.Find(nameof(elementField)).GetComponent<Dropdown>();
            sensField = canvas.transform.Find(nameof(sensField)).GetComponent<InputField>();
            maxConnField = canvas.transform.Find(nameof(maxConnField)).GetComponent<InputField>();

            elementField.value = element.Get<int>();
            sensField.text = sens.Get<string>();
            maxConnField.text = maxConn.Get<string>();

            elementField.onValueChanged.AddListener((int value) => { element.Set(value); });
            sensField.onValueChanged.AddListener((string value) => { sens.Set(value.Replace(',', '.')); });
            maxConnField.onValueChanged.AddListener((string value) => { maxConn.Set(value); });

            Enable();
        }

        public void Enable() 
            => canvas.SetActive(true);

        public void Disable() 
            => canvas.SetActive(false);
    }
}
