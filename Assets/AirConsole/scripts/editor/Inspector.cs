﻿#if !DISABLE_AIRCONSOLE && UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace NDream.AirConsole.Editor {
	[CustomEditor(typeof(AirConsole))]
	public class Inspector : UnityEditor.Editor {

		GUIStyle styleBlack = new GUIStyle ();
		Texture2D bg;
		Texture logo;
		AirConsole controller;
		private SerializedProperty gameId;
		private SerializedProperty gameVersion;
		private bool translationValue;
		private bool inactivePlayersSilencedValue;
		private const string TRANSLATION_ACTIVE = "var AIRCONSOLE_TRANSLATION = true;";
		private const string TRANSLATION_INACTIVE = "var AIRCONSOLE_TRANSLATION = false;";
		private const string INACTIVE_PLAYERS_SILENCED_ACTIVE = "var AIRCONSOLE_INACTIVE_PLAYERS_SILENCED = true;";
		private const string INACTIVE_PLAYERS_SILENCED_INACTIVE = "var AIRCONSOLE_INACTIVE_PLAYERS_SILENCED = false;";


		private static string SettingsPath => Application.dataPath + Settings.WEBTEMPLATE_PATH + "/airconsole-settings.js";

		[InitializeOnLoadMethod]
		private static void Migration() {
			MigrateVersion250(Application.dataPath + Settings.WEBTEMPLATE_PATH + "/translation.js", SettingsPath);
		}
		
		public void Awake()
		{
			if (System.IO.File.Exists(SettingsPath)) {
				string persistedSettings = System.IO.File.ReadAllText(SettingsPath);
				translationValue = persistedSettings.Contains(TRANSLATION_ACTIVE);
				inactivePlayersSilencedValue = persistedSettings.Contains(INACTIVE_PLAYERS_SILENCED_ACTIVE);
			}
		}

		public void OnEnable () {
			// get logos
			bg = (Texture2D)Resources.Load ("AirConsoleBg");
			logo = (Texture)Resources.Load ("AirConsoleLogoText");

			// setup style for airconsole logo
			styleBlack.normal.background = bg;
			styleBlack.normal.textColor = Color.white;
			styleBlack.alignment = TextAnchor.MiddleRight;
			styleBlack.margin.top = 5;
			styleBlack.margin.bottom = 5;
			styleBlack.padding.right = 2;
			styleBlack.padding.bottom = 2;
		}

		public override void OnInspectorGUI () {

			controller = (AirConsole)target;

			// show logo & version
			EditorGUILayout.BeginHorizontal (styleBlack, GUILayout.Height (30));
			GUILayout.Label (logo, GUILayout.Width (128), GUILayout.Height (30));
			GUILayout.FlexibleSpace ();
			GUILayout.Label ("v" + Settings.VERSION, styleBlack);
			EditorGUILayout.EndHorizontal ();

			// show default inspector property editor withouth script reference
			serializedObject.Update ();

			EditorGUILayout.PropertyField(serializedObject.FindProperty("controllerHtml"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("autoScaleCanvas"));
			DrawTranslationsToggle();
			DrawPlayerSilencingToggle();

			EditorGUILayout.PropertyField(serializedObject.FindProperty("androidTvGameVersion"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("androidUIResizeMode"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("webViewLoadingSprite"));

			EditorGUILayout.PropertyField(serializedObject.FindProperty("browserStartMode"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("devGameId"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("LocalIpOverride"));

			serializedObject.ApplyModifiedProperties ();


			EditorGUILayout.BeginHorizontal (styleBlack);
			// check if a port was exported
			if (System.IO.File.Exists (EditorPrefs.GetString ("airconsolePortPath") + "/screen.html")) {

				if (GUILayout.Button ("Open Exported Port", GUILayout.MaxWidth (130))) {

					Extentions.OpenBrowser (controller, EditorPrefs.GetString ("airconsolePortPath"));
				}
			}

			GUILayout.FlexibleSpace ();

			if (GUILayout.Button ("Settings")) {
				SettingWindow window = (SettingWindow)EditorWindow.GetWindow (typeof(SettingWindow));
				window.Show ();
			}

			EditorGUILayout.EndHorizontal ();
		}

		private void DrawTranslationsToggle() {
			bool oldTranslationValue = translationValue;
			translationValue = EditorGUILayout.Toggle("Translation", translationValue);
			if(oldTranslationValue != translationValue) {
				string path = Application.dataPath + Settings.WEBTEMPLATE_PATH + "/airconsole-settings.js";
				WriteConstructorSettings(path);
			}
		}

		private void DrawPlayerSilencingToggle() {
			bool oldInactivePlayersSilencedValue = inactivePlayersSilencedValue;
			inactivePlayersSilencedValue = EditorGUILayout.Toggle("Silence Player", inactivePlayersSilencedValue);
			if(oldInactivePlayersSilencedValue != inactivePlayersSilencedValue) {
				string path = Application.dataPath + Settings.WEBTEMPLATE_PATH + "/airconsole-settings.js";
				WriteConstructorSettings(path);
			}
		}

		private void WriteConstructorSettings(string path) {
			System.IO.File.WriteAllText(path, $"{(translationValue ? TRANSLATION_ACTIVE : TRANSLATION_INACTIVE)}\n{(inactivePlayersSilencedValue ? INACTIVE_PLAYERS_SILENCED_ACTIVE : INACTIVE_PLAYERS_SILENCED_INACTIVE)}");
		}

		private static void MigrateVersion250(string originalPath, string newPath) {
			if (!System.IO.File.Exists(originalPath)) {
				return;
			}

			if (!System.IO.File.Exists(newPath)) {
				Debug.LogWarning("Update settings file to new version, renaming from translation.js to game-settings.js");
				System.IO.File.Move(originalPath, newPath);
				System.IO.File.AppendAllText(newPath, $"\n{INACTIVE_PLAYERS_SILENCED_INACTIVE}");
			}
			else {
				Debug.LogError($"game-settings.js found [{newPath}]. Deleting prior translation.js [{originalPath}].");
				System.IO.File.Delete(originalPath);
			}
		}
	}
}
#endif