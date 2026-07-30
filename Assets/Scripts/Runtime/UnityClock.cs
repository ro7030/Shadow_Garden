using UnityEngine;
using UnityEngine.InputSystem;

namespace ShadowGarden.Runtime
{
    public sealed class UnityClock
    {
        public long DeltaMilliseconds =>
            Mathf.Max(0, Mathf.RoundToInt(Time.unscaledDeltaTime * 1000f));
    }

    public sealed class ApplicationFocusBridge : MonoBehaviour
    {
        public event System.Action<bool> FocusChanged;

        private void OnApplicationFocus(bool hasFocus)
        {
            FocusChanged?.Invoke(hasFocus);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                FocusChanged?.Invoke(false);
            }
            else
            {
                FocusChanged?.Invoke(true);
            }
        }
    }
}
