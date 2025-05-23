# Safe Area Support

The AirConsole Unity plugin can automatically adjust your game to visible regions on devices that use a fullscreen overlay. When **Native Game Sizing** is enabled in the AirConsole component inspector the platform will report the safe area for rendering the game. Whenever the safe area changes the `OnSafeAreaChanged` event of the `AirConsole` instance is invoked and the current region can also be read from `AirConsole.instance.SafeArea`.

Use this event to update the pixel rect of your cameras and any `CanvasScaler` that uses a reference resolution.

## Basic Usage

```csharp
void Awake()
{
    AirConsole.instance.OnSafeAreaChanged += ApplySafeArea;
    ApplySafeArea(AirConsole.instance.SafeArea);
}

void ApplySafeArea(Rect rect)
{
    Camera.main.pixelRect = rect;
}

void OnDestroy()
{
    if (AirConsole.instance)
        AirConsole.instance.OnSafeAreaChanged -= ApplySafeArea;
}
```

## UI Camera and Canvas Scaler

When your UI is rendered by a separate camera using a `CanvasScaler`, update the reference resolution according to the new safe area height.

```csharp
public Camera uiCamera;
public CanvasScaler scaler;

void Awake()
{
    AirConsole.instance.OnSafeAreaChanged += UpdateUICamera;
    UpdateUICamera(AirConsole.instance.SafeArea);
}

void UpdateUICamera(Rect rect)
{
    if (uiCamera)
        uiCamera.pixelRect = rect;
    if (scaler)
        scaler.referenceResolution =
            new Vector2(scaler.referenceResolution.x,
                scaler.referenceResolution.y * Screen.height / rect.height);
}

void OnDestroy()
{
    if (AirConsole.instance)
        AirConsole.instance.OnSafeAreaChanged -= UpdateUICamera;
}
```

## Split Screen Example

For local multiplayer you might want to divide the reported safe area among several cameras. The following setup supports two, three and four player layouts.

```csharp
public Camera[] playerCameras; // order: player1..player4
public bool twoPlayerVertical = true; // horizontal if false
public int activePlayers = 2; // 2, 3 or 4

void Awake()
{
    AirConsole.instance.OnSafeAreaChanged += ApplySplitLayout;
    ApplySplitLayout(AirConsole.instance.SafeArea);
}

void ApplySplitLayout(Rect rect)
{
    Rect area = rect;
    switch (activePlayers)
    {
        case 2:
            if (twoPlayerVertical)
            {
                playerCameras[0].pixelRect = new Rect(area.x, area.y, area.width * 0.5f, area.height);
                playerCameras[1].pixelRect = new Rect(area.x + area.width * 0.5f, area.y, area.width * 0.5f, area.height);
            }
            else
            {
                playerCameras[0].pixelRect = new Rect(area.x, area.y + area.height * 0.5f, area.width, area.height * 0.5f);
                playerCameras[1].pixelRect = new Rect(area.x, area.y, area.width, area.height * 0.5f);
            }
            break;
        case 3:
            playerCameras[0].pixelRect = new Rect(area.x, area.y + area.height * 0.5f, area.width * 0.5f, area.height * 0.5f);
            playerCameras[1].pixelRect = new Rect(area.x + area.width * 0.5f, area.y + area.height * 0.5f, area.width * 0.5f, area.height * 0.5f);
            playerCameras[2].pixelRect = new Rect(area.x, area.y, area.width * 0.5f, area.height * 0.5f);
            if (playerCameras.Length > 3)
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

void OnDestroy()
{
    if (AirConsole.instance)
        AirConsole.instance.OnSafeAreaChanged -= ApplySplitLayout;
}
```

The fourth camera can be used for an additional UI or a birds-eye view when only three players are active.

## Inspector Option

The **Native Game Sizing** checkbox in the AirConsole component enables the safe area reporting from the platform. When disabled, the safe area events are not triggered on Android Automotive devices and the game is expected to fill the entire screen.

Refer to the sample scripts under `Assets/AirConsole/examples` for working implementations.
