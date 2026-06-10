using System;
using UnityEngine;

namespace Mastic
{
    [CreateAssetMenu(fileName = nameof(EasyVar), menuName = nameof(EasyVar))]
    public class EasyVar : ScriptableObject
    {
        public string data;

        public void Set(object value) => data = value.ToString();
        public bool IsSet() => Utils.IsStringValid(data);
        public T Get<T>() => (T)Convert.ChangeType(data, typeof(T));
        public override string ToString() => data;
    }
}
