// Three.js Exporter for Unity
// http://threejsexporter.nickjanssen.com/
// Written by Nick Janssen - info@nickjanssen.com

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace Three {
	public class Exporter : EditorWindow
	{
		public string[] threeRevisions = new string[] {"r71", "r72 and above"};

		public Settings settings;
		private static Core threeExporter = new Core();
		
		private static bool CreateTargetFolder(string folder) {
			try {
				System.IO.Directory.CreateDirectory(folder);
			}
			catch (Exception e) {
				Debug.LogException(e);
				return false;
			}
			
			return true;
		}

		private static bool CleanDirectory(string folder) {
			System.IO.DirectoryInfo dirInfo = new DirectoryInfo(folder);
			
			try {
				foreach (FileInfo file in dirInfo.GetFiles()) {
					file.Delete(); 
				}
				foreach (DirectoryInfo dir in dirInfo.GetDirectories()) {
					dir.Delete(true); 
				}
			}
			catch (Exception e) {
				Debug.LogException(e);
				return false;
			}
			
			return true;
		}


		bool advancedSettings;
		int decimalPlaces = 4;
		float lightMapRGBMConstant = 5.0f;
		string targetPath = Path.GetFullPath(".") + Path.DirectorySeparatorChar + "ThreeExport";
		string subFolderName;
		string jsonFilename = "scene.json";
		bool openFolderAfterExporting = true;
		bool exportCameras = true;
		bool exportColliders = true;
		bool exportTextures = true;
		bool exportLightmaps = true;
		bool exportLights = true;
		bool exportAmbientLight = true;
		bool exportScripts = true;
		bool includeInactiveObjects = false;
		int threeRevision = 1;

		Vector2 scrollPos;

		[MenuItem("Window/Three.js Exporter")]
		public static void ShowWindow() {
			EditorWindow.GetWindow(typeof(Exporter));
		}

		void OnEnable()
		{
			SetSceneName (false);
		}

		void Export() {
			settings.saveFolder = targetPath + System.IO.Path.DirectorySeparatorChar + System.IO.Path.GetFileNameWithoutExtension(subFolderName);
			settings.decimalPlaces = decimalPlaces;
			settings.lightMapRGBMConstant = lightMapRGBMConstant;
			settings.exportCameras = exportCameras;
			settings.exportColliders = exportColliders;
			settings.exportTextures = exportTextures;
			settings.exportLightmaps = exportLightmaps;
			settings.exportLights = exportLights;
			settings.exportAmbientLight = exportAmbientLight;
			settings.threeRevision = threeRevision + 71;

			settings.exportScripts = exportScripts;

			Transform[] selection = Selection.GetTransforms (SelectionMode.Editable | SelectionMode.ExcludePrefab);
			
			if (selection.Length == 0) {
				EditorUtility.DisplayDialog ("Nothing selected.", "Please select one or more objects.", "");
				return;
			}

			if (!CreateTargetFolder (settings.saveFolder)) {
				return;
			}
			
	//		if (!CleanDirectory (saveFolder))
	//			return;		

			List<Transform> allSelectedObjects = new List<Transform>();
			foreach (Transform selectedObject in selection) {
				foreach (var child in selectedObject.GetComponentsInChildren<Transform>(includeInactiveObjects)) {
					allSelectedObjects.Add(child);
				}
			}
			Transform[] allSelectedObjectsArray = allSelectedObjects.ToArray ();
		
			string meshToJSON = threeExporter.ConvertToJSON(allSelectedObjectsArray, settings);
			string fullSavePath = System.IO.Path.Combine(settings.saveFolder, jsonFilename);
			
			using (StreamWriter sw = new StreamWriter(fullSavePath)) {
				sw.Write (meshToJSON);
			}

			if (exportTextures) {
				threeExporter.CopyTextures (allSelectedObjectsArray, settings);
			}

			EditorUtility.DisplayDialog("Completed.", "Exported " + allSelectedObjectsArray.Length + " objects to " + fullSavePath, "");

			if (openFolderAfterExporting) {
				EditorUtility.RevealInFinder (settings.saveFolder);
			}

		}

		void SetSceneName(bool showWarning)
		{
			string name = EditorSceneManager.GetActiveScene ().name;

			if (showWarning && name == "") {
				EditorUtility.DisplayDialog ("Cannot find scene.", "Please save the scene you are currently working on or load a different one.", "");
			} else {
				subFolderName = Path.GetFileNameWithoutExtension (name);
			}
		}

		
		void OnGUI()
		{
			bool cannotContinue = false;

			EditorGUILayout.BeginVertical();
			scrollPos = 
				EditorGUILayout.BeginScrollView(scrollPos);
			{
				GUILayout.Label ("Basic Settings", EditorStyles.boldLabel);

				EditorGUILayout.LabelField ("Target Folder");
				EditorGUILayout.HelpBox (targetPath, MessageType.None);

				GUILayout.BeginHorizontal ();
				GUILayout.FlexibleSpace ();

				if (GUILayout.Button ("Change Target Path")) {
					targetPath = EditorUtility.OpenFolderPanel ("Choose Target Path", targetPath, "");
				}

				GUILayout.EndHorizontal ();

				EditorGUILayout.HelpBox ("Inside the Target Path, I will create an additional folder that contains a JSON file and all textures.", MessageType.Info);


				subFolderName = EditorGUILayout.TextField ("Folder Name", subFolderName);

				GUILayout.BeginHorizontal ();
				GUILayout.FlexibleSpace ();
				if (GUILayout.Button ("Set to Scene Name")) {
					SetSceneName (true);
				}
				GUILayout.EndHorizontal ();

				EditorGUILayout.Space ();

				jsonFilename = EditorGUILayout.TextField ("JSON Filename ", jsonFilename);

				EditorGUILayout.Space ();

				GUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Target Three.js Revision");
				GUILayout.FlexibleSpace ();
				threeRevision = EditorGUILayout.Popup(threeRevision, threeRevisions);
				GUILayout.EndHorizontal ();



				EditorGUILayout.Space ();

				exportCameras = EditorGUILayout.Toggle ("Export Cameras", exportCameras);
				exportColliders = EditorGUILayout.Toggle ("Export Colliders", exportColliders);
				exportTextures = EditorGUILayout.Toggle ("Export Textures", exportTextures);
				exportLightmaps = EditorGUILayout.Toggle ("Export Lightmaps", exportLightmaps);


				if (exportLightmaps && Lightmapping.giWorkflowMode == Lightmapping.GIWorkflowMode.Iterative) {
					cannotContinue = true;
					EditorGUILayout.HelpBox ("Please turn off Auto Baking and rebake the scene once in order for me to access the lightmaps.", MessageType.Error);

					GUILayout.BeginHorizontal ();
					GUILayout.FlexibleSpace ();
					if (GUILayout.Button ("Turn off Auto Baking")) {
						Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;
						Lightmapping.BakeAsync ();
					}
					GUILayout.EndHorizontal ();
				}


				exportLights = EditorGUILayout.Toggle ("Export Lights", exportLights);
				if (exportLights) {
					EditorGUILayout.HelpBox ("Lights that are only used for lightmapping will not be exported. Check the Baking property of your lights and change it to Mixed if you do want them.", MessageType.Info);
				}

				exportAmbientLight = EditorGUILayout.Toggle ("Export Ambient Light", exportAmbientLight);
				if (exportAmbientLight) {
					EditorGUILayout.HelpBox ("An ambient light will be exported with the Ambient Color set in your scene's Lighting Settings.", MessageType.Info);
				}

				exportScripts = EditorGUILayout.Toggle ("Export Script Properties", exportScripts);
				if (exportScripts) {
					EditorGUILayout.HelpBox ("If your Gameobjects contain Scripts, I will export their public properties. I will take all public properties of each script and put them in the userData field of the object. The actual scripts will not be exported.", MessageType.Info);
				}

				includeInactiveObjects = EditorGUILayout.Toggle ("Include Inactive Objects", includeInactiveObjects);

				EditorGUILayout.Space ();
			
				advancedSettings = EditorGUILayout.BeginToggleGroup ("Advanced Settings", advancedSettings);

				lightMapRGBMConstant = EditorGUILayout.FloatField ("Lightmap Contrast", lightMapRGBMConstant);
				EditorGUILayout.HelpBox ("In order to convert Unity lightmaps to Three.js, I have to create PNG's from Unity's EXR format. This may cause the resulting PNG lightmaps to look slightly different. Adjust this constant to tweak the contrast of your exported PNG lightmaps. Lower values make your lightmaps darker, higher values make them brighter.", MessageType.Info);

				decimalPlaces = EditorGUILayout.IntField ("Decimal Places", decimalPlaces);
				EditorGUILayout.HelpBox ("I will round all position, rotation and scale vectors using these given decimal places. A lower value results in a lighter file but may cause the model to look inaccurate. ", MessageType.Info);

				EditorGUILayout.EndToggleGroup ();

				GUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Open Target Folder After Exporting");
				GUILayout.FlexibleSpace ();
				openFolderAfterExporting = EditorGUILayout.Toggle (openFolderAfterExporting);
				GUILayout.EndHorizontal ();
			}
			EditorGUILayout.EndScrollView();
			EditorGUILayout.EndVertical();

			if (cannotContinue) {
				GUI.enabled = false;
			}
			if (GUILayout.Button ("Export")) {
				Export();
			}
			GUI.enabled = true;
		}
	}
}