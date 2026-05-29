using System;
using UnityEngine;

namespace Mastic
{
    public static class Utils
    {
        public static T StringToEnum<T>(string str)
        {
            return (T)Enum.Parse(typeof(T), str);
        }

        public static bool IsStringValid(string str)
        {
            return !string.IsNullOrEmpty(str) && !string.IsNullOrWhiteSpace(str);
        }

        public static void LockMouse()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        public static void UnlockMouse()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        public static Vector3 Normalize(Vector3 normal)
        {
            normal.Normalize();
            if (normal == Vector3.zero)
                normal = Vector3.up;

            return normal;
        }

        public static int ConvertToTicks(float seconds, int standardTickrate)
        {
            return Mathf.RoundToInt(seconds * standardTickrate);
        }

        public static int GetCurrentServerTick(double networkTime, float standardInterval)
        {
            return Mathf.FloorToInt((float)networkTime / standardInterval);
        }

        public static Vector3 Flatten(Vector3 vec)
        {
            vec.y = 0f;
            return vec;
        }
    }
}