using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Mastic
{
    public class Login : MonoBehaviour
    {
        [SerializeField] private EasyVar sessionId = default;
        [SerializeField] private EasyVar loginUrl = default;
        [SerializeField, Min(1)] private int gameId = default;
        [SerializeField] private string nextSceneName = default;
        private PopupHandler popupHandler;
        private TMP_InputField email;
        private TMP_InputField password;
        private Button go;

        private void Awake()
        {
            popupHandler = FindAnyObjectByType<PopupHandler>();
            go = FindAnyObjectByType<Button>();
            List<TMP_InputField> fields = FindObjectsByType<TMP_InputField>(FindObjectsSortMode.None).ToList();
            email = fields.Where(x => x.name == nameof(email)).FirstOrDefault();
            password = fields.Where(x => x.name == nameof(password)).FirstOrDefault();
        }

        private void Start()
        {
            go.onClick.AddListener(Go);
            popupHandler.Send($"You are currently playing game_id: {gameId}");
            if (sessionId.IsSet())
                Debug.LogWarning($"'{nameof(sessionId)}' was already set in {nameof(Start)}(). This is not ideal.");
        }

        public void Go()
        {
            sessionId.Set("nonsense here");
            string url = $"{loginUrl}?game_id={gameId}&email={email.text}&password={password.text}";
            popupHandler.Send(url, Color.gray, 4.5f);
            

        }

        private void Receive()
        {
            sessionId.Set("dkwkjhfsevaksdgfjh");
            SceneManager.LoadScene(nextSceneName);
        }

        private void OnApplicationQuit() => sessionId.Set(string.Empty);
    }
}
