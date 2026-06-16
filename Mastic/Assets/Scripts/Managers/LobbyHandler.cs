using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mastic
{
    public class LobbyHandler : MonoBehaviour
    {
        [SerializeField] private EasyVar sensitivity = default;
        [SerializeField] private EasyVar element = default;
        [SerializeField] private GameObject heroSelectPanel = default;

        private Button beginHeroSelect;
        private NetworkManager networkManager;
        private TMP_Dropdown connectionsField;
        private TMP_Dropdown sensitivityField;
        
        public void Initialize(NetworkManager networkManager)
        {
            this.networkManager = networkManager;
            Enable();
            EnableHeroSelect(true);

            connectionsField = transform.transform.Find(nameof(connectionsField)).GetComponent<TMP_Dropdown>();
            beginHeroSelect = transform.transform.Find(nameof(beginHeroSelect)).GetComponent<Button>();
            sensitivityField = transform.transform.Find(nameof(sensitivityField)).GetComponent<TMP_Dropdown>();

            beginHeroSelect.onClick.AddListener(() => { EnableHeroSelect(true); });
            ButtonArray selection = GetComponentInChildren<ButtonArray>();
            selection.OnSelectIndex += SelectHero;

            connectionsField.onValueChanged.AddListener(OnMaxConnectionsChanged);
            sensitivityField.onValueChanged.AddListener(OnSensitivityChanged);

            OnMaxConnectionsChanged(connectionsField.value);
            OnSensitivityChanged(sensitivityField.value);
            SelectHero(0);
        }

        public void EnableHeroSelect(bool value)
            => heroSelectPanel.SetActive(value);

        private void SelectHero(int index)
        {
            element.Set(index);
            EnableHeroSelect(false);
        }

        private void OnMaxConnectionsChanged(int value)
            => networkManager.maxConnections = int.Parse(connectionsField.options[value].text);

        private void OnSensitivityChanged(int value) 
            => sensitivity.Set(sensitivityField.options[value].text);

        public void Enable() 
            => gameObject.SetActive(true);

        public void Disable() 
            => gameObject.SetActive(false);
    }
}
