using System;
using UnityEngine;

namespace Mastic
{
    [CreateAssetMenu(fileName = nameof(EasyVar), menuName = nameof(EasyVar))]
    public class EasyVar : ScriptableObject
    {
        public string text;

        public void Set(object value) => text = value.ToString();
        public bool IsSet() => Utils.IsStringValid(text);
        public T Get<T>() => (T)Convert.ChangeType(text, typeof(T));
        public override string ToString() => text;
    }
}
