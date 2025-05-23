namespace NDream.AirConsole.Examples {
    using UnityEngine;

    /// <summary>
    /// Simple example that keeps the main camera inside the current safe area.
    /// Attach this script to any object in your scene when using Native Game Sizing.
    /// </summary>
    public class ExampleSafeAreaMainCamera : MonoBehaviour {
#if !DISABLE_AIRCONSOLE
        private void Awake() {
            AirConsole.instance.OnSafeAreaChanged += ApplySafeArea;
            ApplySafeArea(AirConsole.instance.SafeArea);
        }

        private void ApplySafeArea(Rect area) {
            if (Camera.main) {
                Camera.main.pixelRect = area;
            }
        }

        private void OnDestroy() {
            if (AirConsole.instance) {
                AirConsole.instance.OnSafeAreaChanged -= ApplySafeArea;
            }
        }
#endif
    }
}
