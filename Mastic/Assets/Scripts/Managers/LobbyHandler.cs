using Mirror;
using TMPro;
using UnityEngine;

namespace Mastic
{
    public class LobbyHandler : MonoBehaviour
    {
        [SerializeField] private GameObject prefab = default;
        [SerializeField] private EasyVar sensitivity = default;
        [SerializeField] private EasyVar element = default;
        private NetworkManager networkManager;
        private GameObject canvas;
        private TMP_Dropdown connectionsField;
        private TMP_Dropdown elementField;
        private TMP_Dropdown sensitivityField;
        
        public void Initialize(NetworkManager networkManager)
        {
            this.networkManager = networkManager;
            canvas = Instantiate(prefab, Vector3.zero, Quaternion.identity);

            connectionsField = canvas.transform.Find(nameof(connectionsField)).GetComponent<TMP_Dropdown>();
            elementField = canvas.transform.Find(nameof(elementField)).GetComponent<TMP_Dropdown>();
            sensitivityField = canvas.transform.Find(nameof(sensitivityField)).GetComponent<TMP_Dropdown>();

            connectionsField.onValueChanged.AddListener(OnMaxConnectionsChanged);
            elementField.onValueChanged.AddListener(OnElementChanged);
            sensitivityField.onValueChanged.AddListener(OnSensitivityChanged);

            OnMaxConnectionsChanged(connectionsField.value);
            OnElementChanged(elementField.value);
            OnSensitivityChanged(sensitivityField.value);
            Enable();
        }

        private void OnMaxConnectionsChanged(int value)
            => networkManager.maxConnections = int.Parse(connectionsField.options[value].text);

        private void OnSensitivityChanged(int value) 
            => sensitivity.Set(sensitivityField.options[value].text);

        private void OnElementChanged(int value)
            => element.Set(value);

        public void Enable() 
            => canvas.SetActive(true);

        public void Disable() 
            => canvas.SetActive(false);
    }
}
