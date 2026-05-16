using System;
using System.Collections.Generic;

namespace Mastic
{
    public enum Occasion { DoUnlocalMovementAbilities }
    
    public static class EventManager
    {
        private readonly static Dictionary<Occasion, Action> events 
            = new Dictionary<Occasion, Action>();

        public static void RaiseEvent(Occasion occasian)
        {
            if (!events.ContainsKey(occasian))
                return;

            events[occasian]?.Invoke();
        }

        public static void AddListener(Occasion occasian, Action listener)
        {
            if (!events.ContainsKey(occasian))
                events.Add(occasian, null);

            events[occasian] += listener;
        }

        public static void RemoveListener(Occasion occasian, Action listener)
        {
            if (!events.ContainsKey(occasian))
                return;

            events[occasian] -= listener;
        }
    }

    public static class EventManager<T>
    {
        private static readonly Dictionary<Occasion, Action<T>> events 
            = new Dictionary<Occasion, Action<T>>();

        public static void RaiseEvent(Occasion eventType, T t)
        {
            if (!events.ContainsKey(eventType))
                return;

            events[eventType]?.Invoke(t);
        }

        public static void AddListener(Occasion eventType, Action<T> listener)
        {
            if (!events.ContainsKey(eventType))
                events.Add(eventType, null);

            events[eventType] += listener;
        }

        public static void RemoveListener(Occasion eventType, Action<T> listener)
        {
            if (!events.ContainsKey(eventType))
                return;

            events[eventType] -= listener;
        }
    }
}