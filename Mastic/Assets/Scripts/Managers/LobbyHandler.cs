using Mirror;
using TMPro;
using UnityEngine;

namespace Mastic
{
    public class LobbyHandler : MonoBehaviour
    {
        [SerializeField] private EasyVar sensitivity = default;
        [SerializeField] private EasyVar element = default;
        private NetworkManager networkManager;
        private TMP_Dropdown connectionsField;
        private TMP_Dropdown elementField;
        private TMP_Dropdown sensitivityField;
        
        public void Initialize(NetworkManager networkManager)
        {
            this.networkManager = networkManager;
        //    canvas = Instantiate(prefab, Vector3.zero, Quaternion.identity);

            connectionsField = transform.transform.Find(nameof(connectionsField)).GetComponent<TMP_Dropdown>();
            elementField = transform.transform.Find(nameof(elementField)).GetComponent<TMP_Dropdown>();
            sensitivityField = transform.transform.Find(nameof(sensitivityField)).GetComponent<TMP_Dropdown>();

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
            => gameObject.SetActive(true);

        public void Disable() 
            => gameObject.SetActive(false);
    }
}
