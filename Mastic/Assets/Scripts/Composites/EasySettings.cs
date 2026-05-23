using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using UnityEngine;

namespace Mastic
{
    public class EasySettings : MonoBehaviour
    {
        public static EasySettings Current => FindAnyObjectByType<EasySettings>();
        
        [SerializeField] private TextAsset text = default;
        private Dictionary<string, string> dictionary;

        public T Get<T>(string name)
        {
            CheckInitialization();
            name = name.ToLowerInvariant();
            string value = string.Empty;
            if (dictionary.ContainsKey(name)) 
                value = dictionary[name];

            return (T)Convert.ChangeType(value, typeof(T));
        }

        private void CheckInitialization()
        {
            if (dictionary != null)
                return;

            dictionary = new Dictionary<string, string>();
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            using StringReader stringReader = new StringReader(text.text);

            string line;
            while ((line = stringReader.ReadLine()) != null)
            {
                line = line.Trim();
                if (!Utils.IsStringValid(line))
                    continue;

                string[] split = line.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                if (split.Length != 2)
                    continue;

                split[0] = split[0].Trim();
                split[1] = split[1].Trim();
                if (!Utils.IsStringValid(split[0]) || !Utils.IsStringValid(split[1]))
                    continue;

                dictionary[split[0].ToLowerInvariant()] = split[1];
            }
        }

        public void Log(Action<string> onLog)
        {
            CheckInitialization();
            foreach (KeyValuePair<string, string> pair in dictionary)
            {
                onLog?.Invoke($"|{pair.Key}|   |{pair.Value}|");
            }
        }
    }
}
