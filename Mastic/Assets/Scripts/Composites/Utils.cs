using System;
using UnityEngine;

namespace ApplyYourself
{
    public static class Utils
    {
        public static T StringToEnum<T>(string str)
        {
            return (T)Enum.Parse(typeof(T), str);
        }

        public static bool IsValid(string str)
        {
            return !string.IsNullOrEmpty(str) && !string.IsNullOrWhiteSpace(str);
        }
    }
}