using System;
using UnityEngine;
using UnityEngine.UI;

namespace Mastic
{
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
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].onClick.AddListener(() => { int index = i;  SelectIndex(index); });
            }
        }

        private void SelectIndex(int index)
        {
            print($"{name} {index}.");
            OnSelectIndex?.Invoke(index);
        }
    }
}
