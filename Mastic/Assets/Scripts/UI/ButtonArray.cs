using System;
using UnityEngine;
using UnityEngine.UI;

namespace Mastic
{
    /// <summary>
    /// https://discussions.unity.com/t/how-do-i-detect-the-index-of-a-button-in-a-list-when-clicked-using-addlistener/883570/7
    /// </summary>
    public class ButtonArray : MonoBehaviour
    {
        public Action<int> OnSelectIndex;
        private Button[] buttons;

        private void Awake()
        {
            buttons = new Button[transform.childCount];
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i] = transform.GetChild(i).GetComponent<Button>();
            }
        }

        private void Start()
        {
            for (int i = 0; i < buttons.Length; ++i)
            {
                int index = i;
                buttons[i].onClick.AddListener(() => SelectIndex(index));
            }
        }

        private void SelectIndex(int index)
        {
            index = Mathf.Clamp(index, 0, buttons.Length - 1);
            print($"{name.ToUpperInvariant()} --> {index}.");
            OnSelectIndex?.Invoke(index);
        }
    }
}
