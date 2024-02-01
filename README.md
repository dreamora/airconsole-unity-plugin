# Info

The AirConsole-Unity-Plugin is a C# wrapper for the AirConsole Javascript API.
AirConsole provides a simple to use Javascript API for game developers to build their own local multiplayer realtime browser games.
You can find the Javascript API documentation here: <http://developers.airconsole.com/#/api>

IMPORTANT: The plugin comes with an embedded webserver / websocket-server for the communication between the AirConsole backend and the Unity-Editor.
You don't need to install any other webserver or services.

# Upgrade Notes

## Upgrading from v2.14- to v2.5.0

1. The content of airconsole-unity-plugin.js has changed.
You need to copy the correct version from `WebGLTemplates/AirConsole` for Unity 2019 and below or `WebGLTemplates/AirConsole-2020` for Unity 2020+ to the WebGLTemplate folder you use.

2. The content of the index.html / screen.html has changed.
Search for `<script src="translation.js"></script>` and replace it with `<script src="airconsole-settings.js"></script>`, otherwise neither Translations nor Player Silencing will work.

## Upgrading from v2.11 to v2.12+

The location of the Webview has changed in this release.

When upgrading from v2.11 and before, you need manually remove the old Webview plugin parts:

- `Assets/AirConsole/plugins/WebViewObject.cs`
- `Assets/AirConsole/plugins/Editor/UnityWebViewPostprocessBuild.cs`
- `Assets/AirConsole/plugins/WebView.bundle`
- `Assets/AirConsole/plugins/WebViewSeparated.bundle`
- `Assets/AirConsole/plugins/Anrdoid/WebViewPlugin.jar`
- `Assets/AirConsole/plugins/iOS/WebView.mm`
- `Assets/AirConsole/plugins/X86_64/WebView.bundle`

## Upgrading from older versions to v2.11+

With v2.11, the `AirConsole.instance.OnMute` event was removed and the AirConsole platform stopped invoking it for older projects in July 2023.
When upgrading you need to update your event handlers for `AirConsole.instance.OnAdShow` and `AirConsole.instance.OnAdComplete` to take care of muting the game.

# Documentation

All install instructions and examples are documented in the file "Documentation_1.7.0.pdf" inside this folder.
There are more examples on the website: <http://developers.airconsole.com/#/guides>

# Platforms

AirConsole supports WebGL and AndroidTV as targets. To make use of this plugin, you  need to make sure to switch your target to either WebGL or Android.

# Support

If you need support, please visit: <http://developers.airconsole.com/#/help>
