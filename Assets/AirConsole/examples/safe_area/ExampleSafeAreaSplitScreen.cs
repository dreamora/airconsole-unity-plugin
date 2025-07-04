namespace NDream.AirConsole.Examples {
    using UnityEngine;

    /// <summary>
    /// Demonstrates a split screen setup that respects the platform safe area.
    /// Supports layouts for two, three and four players.
    /// </summary>
    public class ExampleSafeAreaSplitScreen : MonoBehaviour {
        [Tooltip("Player cameras in order: 1..4. The fourth camera is used when three players are active.")]
        public Camera[] playerCameras = new Camera[4];
        public bool twoPlayerVertical = true;
        [Range(2,4)]
        public int activePlayers = 2;
        private Rect lastArea;
#if !DISABLE_AIRCONSOLE
        private void Awake() {
            AirConsole.instance.OnSafeAreaChanged += ApplyLayout;
            lastArea = AirConsole.instance.SafeArea;
            ApplyLayout(lastArea);
        }

        private void ApplyLayout(Rect area) {
            lastArea = area;
            switch (activePlayers) {
                case 2:
                    if (twoPlayerVertical) {
                        playerCameras[0].pixelRect = new Rect(area.x, area.y, area.width * 0.5f, area.height);
                        playerCameras[1].pixelRect = new Rect(area.x + area.width * 0.5f, area.y, area.width * 0.5f, area.height);
                    } else {
                        playerCameras[0].pixelRect = new Rect(area.x, area.y + area.height * 0.5f, area.width, area.height * 0.5f);
                        playerCameras[1].pixelRect = new Rect(area.x, area.y, area.width, area.height * 0.5f);
                    }
                    break;
                case 3:
                    playerCameras[0].pixelRect = new Rect(area.x, area.y + area.height * 0.5f, area.width * 0.5f, area.height * 0.5f);
                    playerCameras[1].pixelRect = new Rect(area.x + area.width * 0.5f, area.y + area.height * 0.5f, area.width * 0.5f, area.height * 0.5f);
                    playerCameras[2].pixelRect = new Rect(area.x, area.y, area.width * 0.5f, area.height * 0.5f);
                    if (playerCameras.Length > 3 && playerCameras[3])
                        playerCameras[3].pixelRect = new Rect(area.x + area.width * 0.5f, area.y, area.width * 0.5f, area.height * 0.5f);
                    break;
                case 4:
                    playerCameras[0].pixelRect = new Rect(area.x, area.y + area.height * 0.5f, area.width * 0.5f, area.height * 0.5f);
                    playerCameras[1].pixelRect = new Rect(area.x + area.width * 0.5f, area.y + area.height * 0.5f, area.width * 0.5f, area.height * 0.5f);
                    playerCameras[2].pixelRect = new Rect(area.x, area.y, area.width * 0.5f, area.height * 0.5f);
                    playerCameras[3].pixelRect = new Rect(area.x + area.width * 0.5f, area.y, area.width * 0.5f, area.height * 0.5f);
                    break;
            }
        }

        public void SetActivePlayers(int count) {
            activePlayers = Mathf.Clamp(count, 2, 4);
            ApplyLayout(lastArea);
        }

        private void OnDestroy() {
            if (AirConsole.instance) {
                AirConsole.instance.OnSafeAreaChanged -= ApplyLayout;
            }
        }
#endif
    }
}
