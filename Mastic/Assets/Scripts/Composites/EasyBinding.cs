using UnityEngine;

namespace Mastic
{
    [CreateAssetMenu(fileName = nameof(EasyBinding), menuName = nameof(EasyBinding))]
    public class EasyBinding : ScriptableObject
    {
        public bool IsHeld => Input.GetKey(keyCode);
        public bool WasPressed => Input.GetKeyDown(keyCode);
        public bool WasReleased => Input.GetKeyUp(keyCode);
        
        public string keyName;
        public KeyCode keyCode;

        public void OnValidate()
        {
            keyCode = KeyCode.None;
            if (keyName.Length == 1)
                keyName = keyName.ToUpperInvariant();

            keyCode = Utils.StringToEnum<KeyCode>(keyName);
        }
    }
}
