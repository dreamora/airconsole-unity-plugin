namespace NDream.AirConsole.Examples {
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Keeps a dedicated UI camera and CanvasScaler in sync with the reported safe area.
    /// Useful when the UI is rendered by its own camera using a reference resolution.
    /// </summary>
    public class ExampleSafeAreaUICamera : MonoBehaviour {
        public Camera uiCamera;
        public CanvasScaler canvasScaler;
#if !DISABLE_AIRCONSOLE
        private void Awake() {
            AirConsole.instance.OnSafeAreaChanged += UpdateUI;
            UpdateUI(AirConsole.instance.SafeArea);
        }

        private void UpdateUI(Rect area) {
            if (uiCamera) {
                uiCamera.pixelRect = area;
            }

            if (canvasScaler) {
                canvasScaler.referenceResolution = new Vector2(canvasScaler.referenceResolution.x,
                    canvasScaler.referenceResolution.y * Screen.height / area.height);
            }
        }

        private void OnDestroy() {
            if (AirConsole.instance) {
                AirConsole.instance.OnSafeAreaChanged -= UpdateUI;
            }
        }
#endif
    }
}
