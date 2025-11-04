using System;
using UnityEngine;

namespace LootManager
{
    public sealed class DebounceInvoker : MonoBehaviour
    {
        private Action _pending;
        private float _dueTime;
        private bool _armed;

        public static DebounceInvoker Attach(GameObject host)
        {
            var d = host.GetComponent<DebounceInvoker>();
            if (d == null) d = host.AddComponent<DebounceInvoker>();
            return d;
        }

        public void Schedule(Action action, float delaySeconds)
        {
            _pending = action;
            _dueTime = Time.unscaledTime + delaySeconds;
            _armed = true;
        }

        private void Update()
        {
            if (!_armed) return;
            if (Time.unscaledTime >= _dueTime)
            {
                _armed = false;
                var a = _pending;
                _pending = null;
                a?.Invoke();
            }
        }
    }
}