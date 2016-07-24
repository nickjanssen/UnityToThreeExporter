// Three.js Exporter for Unity
// http://threejsexporter.nickjanssen.com/
// Written by Nick Janssen - info@nickjanssen.com

using UnityEngine;
using UnityEditor;
using System;
using System.Text;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Three {
	public class Core {

		List<string> commonTextures = new List<string>
		{ "_MainTex", "_BumpMap", "_OcclusionMap" };

		string GetValidFileNameFromString(string file) {
			Array.ForEach(Path.GetInvalidFileNameChars(), 
              c => file = file.Replace(c.ToString(), String.Empty));

			return file;
		}

		public bool CopyTextures(Transform[] transforms, Settings settings) {
				
			Dictionary<string, Material> usedMaterials = new Dictionary<string, Material> ();		
			
			foreach (Transform transform in transforms) {
				MeshFilter meshFilter = transform.GetComponent<MeshFilter>();
				if (meshFilter != null) {
					Material[] mats = meshFilter.GetComponent<Renderer>().sharedMaterials;
					
					for (int i=0; i < mats.Length; i ++) {
						Material mat = mats[i];

						if (!mat) {
							continue;
						}
						
						if (!usedMaterials.ContainsValue(mat)) {
							usedMaterials.Add(System.Guid.NewGuid().ToString(), mat);
						}
					}
				}
			}


			foreach(KeyValuePair<string, Material> entry in usedMaterials) {							
				commonTextures.ForEach(delegate(String texName) {			
					if (!entry.Value.HasProperty(texName) || !entry.Value.GetTexture(texName)) return;

					Texture2D tex = (Texture2D) entry.Value.GetTexture(texName);

					var texturePath = AssetDatabase.GetAssetPath(tex);
					var textureExtension = System.IO.Path.GetExtension(texturePath).ToLower();

//						var filename = System.IO.Path.GetFileName(texturePath);
//						var destFile = System.IO.Path.Combine(settings.saveFolder, filename);
//						System.IO.File.Copy(texturePath, destFile, true);
//					}
//					else {
	//				else if (textureExtension != ".psd") {
					var texturePathPNG = System.IO.Path.ChangeExtension(texturePath, ".png");
					var texturePathJPG = System.IO.Path.ChangeExtension(texturePath, ".jpg");				

					var filenamePNG = GetValidFileNameFromString(entry.Value.name) + "_" + System.IO.Path.GetFileName(texturePathPNG);
					var filenameJPG = GetValidFileNameFromString(entry.Value.name) + "_" + System.IO.Path.GetFileName(texturePathJPG);
						
					var destFilePNG = System.IO.Path.Combine(settings.saveFolder, filenamePNG);
					var destFileJPG = System.IO.Path.Combine(settings.saveFolder, filenameJPG);
						
					TextureImporter ti = (TextureImporter) TextureImporter.GetAtPath(texturePath);			
//					TextureImporterFormat oldformat = ti.textureFormat;
					TextureImporterType oldtype = ti.textureType;

					if (texName == "_BumpMap") {
						ti.textureType = TextureImporterType.Image;
					}

					ti.textureFormat = TextureImporterFormat.RGBA32;
					ti.isReadable = true;

					AssetDatabase.ImportAsset(texturePath);

					if (textureExtension == ".jpg") {
						File.WriteAllBytes(destFileJPG, tex.EncodeToJPG());
					}
					else {
						File.WriteAllBytes(destFilePNG, tex.EncodeToPNG());
					}
						

					ti.textureType = oldtype;
//					ti.textureFormat = oldformat;
					ti.isReadable = false;
					AssetDatabase.ImportAsset(texturePath);

				});			
			}		

			// Copy lightmaps
			LightmapData[] lightmaps = UnityEngine.LightmapSettings.lightmaps;
			List<Texture2D> lightmapTextures = new List<Texture2D>();
			if (settings.exportLightmaps) {
				for (int i = 0; i < lightmaps.Length; i++) {
					if (lightmaps [i].lightmapFar) {
						lightmapTextures.Add (lightmaps [i].lightmapFar);
					}
					if (lightmaps [i].lightmapNear) {
						lightmapTextures.Add (lightmaps [i].lightmapNear);
					}
				}
			}


			lightmapTextures.ForEach (delegate(Texture2D tex) {

				var texturePath = AssetDatabase.GetAssetPath (tex);
				var texturePathPNG = System.IO.Path.ChangeExtension (texturePath, ".png");

				//System.IO.File.Copy(texturePath, System.IO.Path.Combine(settings.saveFolder, System.IO.Path.GetFileName(texturePath)), true);
				
				var destFilePNG = System.IO.Path.Combine (settings.saveFolder, System.IO.Path.GetFileName (texturePathPNG));

				TextureImporter ti = (TextureImporter)TextureImporter.GetAtPath (texturePath);			
				TextureImporterFormat oldformat = ti.textureFormat;
				TextureImporterType oldType = ti.textureType;
				TextureImporterSettings textureImporterSettings = new TextureImporterSettings ();
				if (oldType == TextureImporterType.Lightmap) {
					ti.textureType = TextureImporterType.Advanced;
					ti.lightmap = false;

					ti.ReadTextureSettings (textureImporterSettings);
					//				settings.rgbm = TextureImporterRGBMMode.Off;
					ti.SetTextureSettings (textureImporterSettings);
				}

				ti.textureFormat = TextureImporterFormat.RGBA32;
				ti.isReadable = true;
				ti.SaveAndReimport ();

				Color[] pix = tex.GetPixels (0, 0, tex.width, tex.height);

				for (int i = 0; i < pix.Length; i++) {
					// http://graphicrants.blogspot.jp/2009/04/rgbm-color-encoding.html
					pix [i].r *= pix [i].a * settings.lightMapRGBMConstant;
					pix [i].g *= pix [i].a * settings.lightMapRGBMConstant;
					pix [i].b *= pix [i].a * settings.lightMapRGBMConstant;			
				}

				Texture2D destTex = new Texture2D (tex.width, tex.height, TextureFormat.RGB24, false);
				destTex.hideFlags = HideFlags.HideAndDontSave;
				destTex.SetPixels (pix);
				destTex.Apply ();

				File.WriteAllBytes (destFilePNG, destTex.EncodeToPNG ());

				GameObject.DestroyImmediate (destTex);

				if (oldType == TextureImporterType.Lightmap) {
					ti.textureType = TextureImporterType.Lightmap;
					ti.lightmap = true;

					ti.ReadTextureSettings (textureImporterSettings);
					textureImporterSettings.rgbm = TextureImporterRGBMMode.Auto;
					ti.SetTextureSettings (textureImporterSettings);
				}
				ti.textureFormat = oldformat;
				ti.isReadable = false;
				ti.SaveAndReimport ();

			});

			return true;
		}
		
		public struct Submesh {
			public MeshFilter meshFilter;
			public Mesh mesh;
			public int subMeshIndex;
		}

		public struct TextureImageLink {
			public Texture texture;
			public string filename;
		}

		public struct MeshMaterialLink {
			public MeshFilter meshFilter;
			public Material material;
			public int subMeshIndex;
		}

		public struct MaterialTextureLink {
			public Material material;
			public Texture texture;
			public string textureUnityIdentifier;
			public bool needsCleanup;
		}

		private StringBuilder jsonFile = new StringBuilder();
		
		private void writeJSON(int tabs, string content, bool writeNewLine=true) {
			for (int i = 0; i < tabs; i++) {
				jsonFile.Append("\t");
			}
			jsonFile.Append(content);
			if (writeNewLine) {
				jsonFile.Append(System.Environment.NewLine);
			}
		}

		private void writeMatrix(int tabs, Transform transform, Settings settings) {
			writeJSON(tabs, "\"matrix\": [", false);
			{
				Matrix4x4 m = Matrix4x4.TRS(new Vector3(), Quaternion.identity, new Vector3(-1, 1, 1));
				m *= transform.localToWorldMatrix;
				m *= Matrix4x4.TRS(new Vector3(), Quaternion.AngleAxis(180, new Vector3(1, 0, 0)), new Vector3(1, 1, 1));
	            m *= Matrix4x4.TRS(new Vector3(), Quaternion.AngleAxis(90, new Vector3(0, 1, 0)), new Vector3(1, 1, 1));
				m *= Matrix4x4.TRS(new Vector3(), Quaternion.identity, new Vector3(1, -1, 1));

				for (int j = 0; j <= 3; j++) {
					for (int k = 0; k <= 3; k++) {
						float test = m[k,j];
						test = (float)Math.Round(test, settings.decimalPlaces);
						writeJSON(0, test.ToString(), false);
						if (!(j == 3 && k == 3)) {
							writeJSON(0, ",", false);
						}
					}
				}
				writeJSON(0, "]");
			}
		}
		
		
		public string ConvertToJSON(Transform[] transforms, Settings settings) {

			Dictionary<string, Material> usedMaterials = new Dictionary<string, Material> ();		

			foreach (Transform transform in transforms) {
				MeshFilter meshFilter = transform.GetComponent<MeshFilter>();
				if (meshFilter != null) {
					Material[] mats = meshFilter.GetComponent<Renderer>().sharedMaterials;

					for (int i=0; i < mats.Length; i ++) {
						Material mat = mats[i];

						if (!mat) {
							continue;
						}

						if (!usedMaterials.ContainsValue(mat)) {
							usedMaterials.Add(System.Guid.NewGuid().ToString(), mat);
						}
					}
				}
			}
			
			Dictionary<string, Submesh> usedSubmeshes = new Dictionary<string, Submesh> ();

			foreach (Transform transform in transforms) {
				MeshFilter meshFilter = transform.GetComponent<MeshFilter>();
				if (meshFilter != null) {
					for (int i = 0; i < meshFilter.sharedMesh.subMeshCount; i++) {
						Submesh submesh;
						submesh.mesh = meshFilter.sharedMesh;
						submesh.meshFilter = meshFilter;
						submesh.subMeshIndex = i;
						usedSubmeshes.Add(System.Guid.NewGuid().ToString(), submesh);
					}
				}
			}

			Dictionary<string, MeshMaterialLink> meshMaterialLinks = new Dictionary<string, MeshMaterialLink> ();
			Dictionary<string, MaterialTextureLink> usedTextures = new Dictionary<string, MaterialTextureLink> ();
			Dictionary<string, TextureImageLink> usedImages = new Dictionary<string, TextureImageLink> ();
			List<Texture> addedTextures = new List<Texture>();

			foreach (Transform transform in transforms) {
				MeshFilter meshFilter = transform.GetComponent<MeshFilter>();
				if (meshFilter != null) {
					Material[] mats = meshFilter.GetComponent<Renderer>().sharedMaterials;		
					
					for (int i=0; i < mats.Length; i ++) {

						if (!mats[i]) {
							Debug.LogWarning("GameObject has a bad/missing material: " + transform.name);
							continue;
						}

						MeshMaterialLink meshMaterialLink;

						meshMaterialLink.meshFilter = meshFilter;
						meshMaterialLink.material = mats[i];
						meshMaterialLink.subMeshIndex = i;

						commonTextures.ForEach(delegate(String texName) {			
							
							if ( !meshMaterialLink.material.HasProperty(texName) ) return;
							
							Texture tex = meshMaterialLink.material.GetTexture(texName);
							if (tex != null && !addedTextures.Contains(tex)) {
								
								MaterialTextureLink materialTextureLink;
								materialTextureLink.texture = tex;
								materialTextureLink.material = meshMaterialLink.material;
								materialTextureLink.textureUnityIdentifier = texName;
								materialTextureLink.needsCleanup =  false;
																			
								TextureImageLink textureImageLink;
								textureImageLink.texture = materialTextureLink.texture;
								string texturePath = AssetDatabase.GetAssetPath(materialTextureLink.texture);
								
								var textureExtension = System.IO.Path.GetExtension(texturePath).ToLower();
								if (textureExtension != ".png" && textureExtension != ".jpg") {
									// Other textures get converted to png automatically
									texturePath = System.IO.Path.ChangeExtension (texturePath, ".png");			
								}
								
								textureImageLink.filename = GetValidFileNameFromString(meshMaterialLink.material.name) + "_" +System.IO.Path.GetFileName(texturePath);	
								
								addedTextures.Add(tex);
								usedTextures.Add(System.Guid.NewGuid().ToString(), materialTextureLink);
								usedImages.Add(System.Guid.NewGuid().ToString(), textureImageLink);
							}
						});

						meshMaterialLinks.Add(System.Guid.NewGuid().ToString(), meshMaterialLink);

					}
				}
			}			

			// Add lightmaps
			LightmapData[] lightmapData = UnityEngine.LightmapSettings.lightmaps;
			if (settings.exportLightmaps) {
				for (int i = 0; i < lightmapData.Length; i++) {
					if (lightmapData [i].lightmapFar) {
						TextureImageLink textureImageLink;
						textureImageLink.texture = lightmapData [i].lightmapFar;

						string texturePath = AssetDatabase.GetAssetPath (lightmapData [i].lightmapFar);
						string texturePathPNG = System.IO.Path.ChangeExtension (texturePath, ".png");			
						textureImageLink.filename = System.IO.Path.GetFileName (texturePathPNG);

						Material lightMapMaterial = new Material (Shader.Find ("Diffuse"));
						lightMapMaterial.hideFlags = HideFlags.HideAndDontSave;
						MaterialTextureLink materialTextureLink;
						materialTextureLink.texture = textureImageLink.texture;
						materialTextureLink.material = lightMapMaterial;
						materialTextureLink.textureUnityIdentifier = "_MainTex";
						materialTextureLink.needsCleanup =  true;

						usedTextures.Add (System.Guid.NewGuid ().ToString (), materialTextureLink);

						usedImages.Add (System.Guid.NewGuid ().ToString (), textureImageLink);
					}

					if (lightmapData [i].lightmapNear) {
						TextureImageLink textureImageLink;
						textureImageLink.texture = lightmapData [i].lightmapNear;

						string texturePath = AssetDatabase.GetAssetPath (lightmapData [i].lightmapNear);
						string texturePathPNG = System.IO.Path.ChangeExtension (texturePath, ".png");			
						textureImageLink.filename = System.IO.Path.GetFileName (texturePathPNG);

						usedImages.Add (System.Guid.NewGuid ().ToString (), textureImageLink);
					}
				}
			}

			jsonFile.Length = 0;
		
			writeJSON (0, "{");

			// Metadata
			writeJSON (1, "\"metadata\": {");
			{
				
				writeJSON(2, "\"version\": \"" + 4.3f + "\",");
				writeJSON(2, "\"type\": " + "\"Object\",");
				writeJSON(2, "\"generator\": " + "\"UnityThreeExporter\"");
			
			}

			writeJSON (1, "},");

			// Geometries
			writeJSON (1, "\"geometries\": [");		
			{
				int count = 0;
				foreach(KeyValuePair<string, Submesh> entry in usedSubmeshes) {

					writeJSON (2, "{");

					int[] subMeshTriangles = entry.Value.mesh.GetTriangles(entry.Value.subMeshIndex);
					// List<int> subMeshTrianglesList = new List<int>(subMeshTriangles);

					writeJSON(3, "\"uuid\": \"" + entry.Key + "\", ");
					writeJSON(3, "\"type\": \"" + "Geometry" + "\", ");				

					int lowestFaceIndex = int.MaxValue;					
					for (int i = 0; i < subMeshTriangles.Length; i++) {
						if (subMeshTriangles[i] < lowestFaceIndex) {
							lowestFaceIndex = subMeshTriangles[i];
						}
					}

					int highestFaceIndex = -1;					
					for (int i = 0; i < subMeshTriangles.Length; i++) {
						if (subMeshTriangles[i] > highestFaceIndex) {
							highestFaceIndex = subMeshTriangles[i];
						}
					}

					writeJSON(3, "\"data\": {");
					{

						writeJSON(4, "\"vertices\": [", false);
						{
							for (int i = lowestFaceIndex; i <= highestFaceIndex; i++) {
								Vector3 vertex = entry.Value.mesh.vertices[i];
								writeJSON(0, Math.Round(vertex.z, settings.decimalPlaces) + ",", false);
								writeJSON(0, Math.Round(vertex.y, settings.decimalPlaces) + ",", false);
								writeJSON(0, Math.Round(vertex.x, settings.decimalPlaces) + ((i < highestFaceIndex) ? "," : ""), false);
							}
						}
						writeJSON(0, "],");

						writeJSON(4, "\"normals\": [", false);
						{
							if (entry.Value.mesh.normals.Length > 0) {
								for (int i = lowestFaceIndex; i <= highestFaceIndex; i++) {
									Vector3 normal = entry.Value.mesh.normals[i];
									writeJSON(0, Math.Round(normal.z, settings.decimalPlaces) + ",", false);
									writeJSON(0, Math.Round(normal.y, settings.decimalPlaces) + ",", false);
									writeJSON(0, Math.Round(normal.x, settings.decimalPlaces) + ((i < highestFaceIndex) ? "," : ""), false);
								}
							}
						}
						writeJSON(0, "],");

						writeJSON(4, "\"uvs\": [", false);
						{					
							Vector2[] meshUv1 = entry.Value.mesh.uv;
							Vector2[] meshUv2 = entry.Value.mesh.uv2;

							if (entry.Value.meshFilter.GetComponent<Renderer>().lightmapIndex >= 0 &&
							    meshUv2.Length == 0) {
								meshUv2 = (Vector2[]) meshUv1.Clone();								
							}

							// Alter lightmaps
							if (entry.Value.meshFilter.GetComponent<Renderer>().lightmapIndex >= 0) {
								if (entry.Value.mesh.uv2.Length > 0) {
									for (int i = lowestFaceIndex; i <= highestFaceIndex; i++) {
										meshUv2[i].x *= entry.Value.meshFilter.GetComponent<Renderer>().lightmapScaleOffset.x;
										meshUv2[i].y *= entry.Value.meshFilter.GetComponent<Renderer>().lightmapScaleOffset.y;								

										meshUv2[i].x += entry.Value.meshFilter.GetComponent<Renderer>().lightmapScaleOffset.z;
										meshUv2[i].y += entry.Value.meshFilter.GetComponent<Renderer>().lightmapScaleOffset.w;	
									}
								}
								else {
									for (int i = lowestFaceIndex; i <= highestFaceIndex; i++) {
										meshUv1[i].x *= entry.Value.meshFilter.GetComponent<Renderer>().lightmapScaleOffset.x;
										meshUv1[i].y *= entry.Value.meshFilter.GetComponent<Renderer>().lightmapScaleOffset.y;								
										
										meshUv1[i].x += entry.Value.meshFilter.GetComponent<Renderer>().lightmapScaleOffset.z;
										meshUv1[i].y += entry.Value.meshFilter.GetComponent<Renderer>().lightmapScaleOffset.w;	
									}
								}
							}	

							writeJSON(0, "[", false);
							{
								if (meshUv1.Length > 0) {
									for (int i = lowestFaceIndex; i <= highestFaceIndex; i++) {
										Vector3 uv = meshUv1[i];
										writeJSON(0, uv.x + ",", false);
										writeJSON(0, uv.y + ((i < highestFaceIndex) ? "," : ""), false);							
									}
								}
							}
							writeJSON(0, "],", false);


							writeJSON(0, "[", false);
							{
								int lightmapIndex = entry.Value.meshFilter.GetComponent<Renderer>().lightmapIndex;
								if (entry.Value.mesh.uv2.Length > 0) {
									for (int i = lowestFaceIndex; i <= highestFaceIndex; i++) {
										Vector3 uv = meshUv2[i];
										writeJSON(0, uv.x + ",", false);
										writeJSON(0, uv.y + ((i < highestFaceIndex) ? "," : ""), false);							
									}
								}	
								else if (entry.Value.meshFilter.GetComponent<Renderer>().lightmapIndex >= 0 &&
							        	entry.Value.mesh.uv2.Length == 0) {
									for (int i = lowestFaceIndex; i <= highestFaceIndex; i++) {
										Vector3 uv = meshUv1[i];
										writeJSON(0, uv.x + ",", false);
										writeJSON(0, uv.y + ((i < highestFaceIndex) ? "," : ""), false);							
									}
								}
							}			
							writeJSON(0, "]", false);		
							
						}
						writeJSON(0, "],");

						writeJSON(4, "\"faces\": [", false);
						{
							
							for (int i = 0; i < subMeshTriangles.Length; i++) {
								subMeshTriangles[i] = subMeshTriangles[i] - lowestFaceIndex;
							}
							
							int[] triangles = subMeshTriangles;
							for (int j = 0; j < triangles.Length; j += 3) {

								writeJSON(0, 8 + ",", false);

								writeJSON(0, triangles[j+2] + ",", false);
								writeJSON(0, triangles[j+1] + ",", false);
								writeJSON(0, triangles[j].ToString(), false);

								if (entry.Value.mesh.uv.Length > 0) {
									writeJSON(0, ",", false);	
									writeJSON(0, triangles[j+2] + ",", false);
									writeJSON(0, triangles[j+1] + ",", false);
									writeJSON(0, triangles[j].ToString(), false);
								}

								if (entry.Value.mesh.uv2.Length > 0 ||
								    (entry.Value.meshFilter.GetComponent<Renderer>().lightmapIndex >= 0 &&
								 	entry.Value.mesh.uv2.Length == 0)) {	
									writeJSON(0, ",", false);	
									writeJSON(0, triangles[j+2] + ",", false);
									writeJSON(0, triangles[j+1] + ",", false);
									writeJSON(0, triangles[j].ToString(), false);								
								}

								writeJSON(0, ((j < triangles.Length - 3) ? "," : ""), false);

							}

						}
						writeJSON(0, "]");
					}
					writeJSON (3, "}");


					writeJSON (2, "}", false);

					if (count < usedSubmeshes.Count - 1) {
						writeJSON (0, ",");
					}
					else {
						writeJSON (0, "");
					}

					count++;
				}
			}
			writeJSON (1, "],");
			
			// Materials
			writeJSON (1, "\"materials\": [");		
			{
				int count = 0;
				foreach(KeyValuePair<string, MeshMaterialLink> meshMaterialLink in meshMaterialLinks) {

					writeJSON (2, "{");
								
					Material mat = meshMaterialLink.Value.material;
										
					writeJSON(3, "\"uuid\": \"" + meshMaterialLink.Key + "\",");
					writeJSON(3, "\"name\": \"" + mat.name + "\", ");

					writeJSON(3, "\"type\": \"" + "MeshPhongMaterial" + "\",");

					if (meshMaterialLink.Value.material.HasProperty("_Color")) {
						writeJSON(3, "\"color\": \"" + "0x" + Util.ColorToHex(meshMaterialLink.Value.material.GetColor("_Color")) + "\",");
					}

					if (meshMaterialLink.Value.material.HasProperty("_SpecColor")) {
						writeJSON(3, "\"specular\": \"" + "0x" + Util.ColorToHex(meshMaterialLink.Value.material.GetColor("_SpecColor")) + "\",");
					}
					if (meshMaterialLink.Value.material.HasProperty("_EmissionColor")) {
						writeJSON(3, "\"emissive\": \"" + "0x" + Util.ColorToHex(meshMaterialLink.Value.material.GetColor("_EmissionColor")) + "\",");
					}

					if (meshMaterialLink.Value.material.HasProperty("_Shininess")) {
						writeJSON(3, "\"shininess\": " + meshMaterialLink.Value.material.GetFloat("_Shininess") + ",");
					}

					if (meshMaterialLink.Value.material.HasProperty("_Color")) {
						writeJSON(3, "\"opacity\": " + (meshMaterialLink.Value.material.GetColor("_Color").a) + ",");
					}

					string mapId = null;
					string normalMapId = null;
					string aoMapId = null;

					foreach(KeyValuePair<string, Material> usedMaterial in usedMaterials) {
						if (usedMaterial.Value == meshMaterialLink.Value.material) {
							if (meshMaterialLink.Value.material.HasProperty("_MainTex") &&
							    mat.HasProperty("_MainTex") ) {
								mapId = usedMaterial.Key + "_MainTex";
							}
							if (meshMaterialLink.Value.material.HasProperty("_BumpMap") &&
							    mat.HasProperty("_BumpMap") ) {
								normalMapId = usedMaterial.Key + "_BumpMap";
							}
							if (meshMaterialLink.Value.material.HasProperty("_OcclusionMap") &&
							    mat.HasProperty("_OcclusionMap") ) {
								aoMapId = usedMaterial.Key + "_OcclusionMap";
							}
						}
					}


					if (mapId != null) {
						writeJSON(3, "\"map\": \"" + mapId + "\",");
					}
					if (normalMapId != null) {
						writeJSON(3, "\"normalMap\": \"" + normalMapId + "\",");
					}
					if (aoMapId != null) {
						writeJSON(3, "\"aoMap\": \"" + aoMapId + "\",");
					}

					// Add lightmaps
					// Problem is that in Unity, lightmaps are per object while in ThreeJS
					// they are per material. Because of this, we have create a separate material for each object.
					int lightmapIndex = meshMaterialLink.Value.meshFilter.GetComponent<Renderer>().lightmapIndex;

					// Only if the object is lightmapped
					if (lightmapIndex >= 0) {
						LightmapData usedLightMap = lightmapData[lightmapIndex];

						foreach(KeyValuePair<string, MaterialTextureLink> usedTexture in usedTextures) {
							if (usedTexture.Value.texture == usedLightMap.lightmapFar) {
								writeJSON(3, "\"lightMap\": \"" + usedTexture.Key + "\",");
							}
						}
					}				

					bool transparent = false;
					if (meshMaterialLink.Value.material.HasProperty("_Mode") && 
					    meshMaterialLink.Value.material.GetFloat("_Mode") != 0) {
						transparent = true;
					}
					else if (meshMaterialLink.Value.material.shader.name.Contains("Transparent")) {
						transparent = true;
					}

					if (meshMaterialLink.Value.material.shader.name.Contains("Additive")) {
						transparent = true;
						writeJSON(3, "\"blending\": 2,");
					}

					writeJSON(3, "\"transparent\": " + ((transparent) ? "true" : "false") + ",");

					bool wireframe = false;
					writeJSON(3, "\"wireframe\": " + ((wireframe) ? "true" : "false"));

					writeJSON (2, "}", false);

					if (count < meshMaterialLinks.Count - 1) {
						writeJSON (0, ",");
					}
					else {
						writeJSON (0, "");
					}
					
					count++;

				}		
				
			}	
			writeJSON (1, "],");


			writeJSON (1, "\"object\": {");		
			// Objects
			{		
				writeJSON(2, "\"uuid\": \"" + System.Guid.NewGuid().ToString() + "\",");
				writeJSON(2, "\"type\": \"" + "Object3D" + "\",");
				writeJSON(2, "\"name\": \"" + "World" + "\",");
							
				writeJSON(2, "\"matrix\": [", false);
				{
					Matrix4x4 m = Matrix4x4.identity;

					for (int j = 0; j <= 3; j++) {
						for (int k = 0; k <= 3; k++) {
							float test = m[k,j];
							test = (float)Math.Round(test, settings.decimalPlaces);
							writeJSON(0, test.ToString(), false);
							if (!(j == 3 && k == 3)) {
								writeJSON(0, ",", false);
							}
						}
					}			
				}
				writeJSON(0, "],");

				writeJSON(2, "\"children\": [");

				if (settings.exportAmbientLight) {
					// Add ambient light
					writeJSON(3, "{");				
					{
						writeJSON(4, "\"uuid\": \"" + System.Guid.NewGuid().ToString() + "\",");					
						writeJSON(4, "\"type\": \"" + "AmbientLight" + "\",");										
						writeJSON(4, "\"color\": \"" + "#" + Util.ColorToHex(RenderSettings.ambientLight) + "\",");							
						writeJSON(4, "\"name\": \"RenderSettingsAmbientLight\"");				
					}				
					writeJSON(3, "},", true);
				}

				int transformsThatWillBeWritten = 0;
				foreach (Transform transform in transforms) {

					if (settings.exportCameras) {
						Camera camera = transform.GetComponent<Camera>();
						if (camera != null) {												
							writeJSON(3, "{");

							{
								writeJSON(4, "\"uuid\": \"" + System.Guid.NewGuid().ToString() + "\",");
								writeJSON(4, "\"type\": \"" + "PerspectiveCamera" + "\",");
								writeJSON(4, "\"name\": \"" + transform.name + "\",");
								writeJSON(4, "\"fov\": \"" + camera.fieldOfView.ToString() + "\",");
								writeJSON(4, "\"aspect\": \"" + camera.aspect.ToString() + "\",");
								writeJSON(4, "\"near\": \"" + camera.nearClipPlane.ToString() + "\",");
								writeJSON(4, "\"far\": \"" + camera.farClipPlane.ToString() + "\", ");

								writeMatrix(4, transform, settings);
							}

							writeJSON(3, "},", true);

							transformsThatWillBeWritten++;
						}
					}

					if (settings.exportLights) {
						Light light = transform.GetComponent<Light>();
						if (light != null) {
							if (light.type == LightType.Directional || light.type == LightType.Point || light.type == LightType.Spot) {												

								SerializedObject serialObj = new SerializedObject(light); 
								SerializedProperty lightmapProp = serialObj.FindProperty("m_Lightmapping");		

//								Debug.Log ("m_Lightmapping");
//								Debug.Log (lightmapProp.intValue);

								// Only add the lights if they are set to something else than Baked Only
								if (lightmapProp.intValue != 2) {
									writeJSON(3, "{");
									
									{
										writeJSON(4, "\"uuid\": \"" + System.Guid.NewGuid().ToString() + "\",");

										if (light.type == LightType.Directional) {
											writeJSON(4, "\"type\": \"" + "DirectionalLight" + "\",");
										}

										if (light.type == LightType.Point) {
											writeJSON(4, "\"type\": \"" + "PointLight" + "\",");
										}

										if (light.type == LightType.Spot) {
											writeJSON(4, "\"type\": \"" + "SpotLight" + "\",");
											writeJSON(4, "\"distance\": " + light.range + ",");
											writeJSON(4, "\"angle\": " + light.spotAngle + ",");
										}


										// THREE.SpotLight( data.color, data.intensity, data.distance, data.angle, data.exponent, data.decay );


										writeJSON(4, "\"color\": \"" + "#" + Util.ColorToHex(light.color) + "\",");

										writeJSON(4, "\"name\": \"" + transform.name + "\",");
										writeJSON(4, "\"intensity\": " + light.intensity.ToString() + ",");
										
										writeMatrix(4, transform, settings);
									}
									
									writeJSON(3, "},", true);
									
									transformsThatWillBeWritten++;
								}
							}
						}
					}


					writeJSON(3, "{");

					writeJSON(4, "\"uuid\": \"" + System.Guid.NewGuid().ToString() + "\",");
					writeJSON(4, "\"type\": \"" + "Object3D" + "\",");
					writeJSON(4, "\"name\": \"" + transform.name + "\",");


					if ((settings.exportColliders && transform.GetComponent<Collider>() != null) ||
					    (transform.GetComponent<MeshFilter>() != null) ||
					    (settings.exportScripts && transform.GetComponent<MonoBehaviour>() != null)) {

						writeJSON(4, "\"children\": [");

						if (settings.exportColliders) {
							Collider collider = transform.GetComponent<Collider>();
							if (collider != null) {												
								writeJSON(5, "{");
								
								{
									writeJSON(6, "\"uuid\": \"" + System.Guid.NewGuid().ToString() + "\",");
									writeJSON(6, "\"name\": \"" + transform.name + "_Collider\",");								
									writeJSON(6, "\"type\": \"" + "Object3D" + "\",");

									writeJSON(6, "\"userData\": {");


									BoxCollider boxCollider = transform.GetComponent<BoxCollider>();
									if (boxCollider != null) {
										writeJSON(7, "\"type\": \"" + "BoxCollider" + "\",");
									}
									
									SphereCollider sphereCollider = transform.GetComponent<SphereCollider>();
									if (sphereCollider != null) {
										writeJSON(7, "\"type\": \"" + "SphereCollider" + "\",");
									}
									
									MeshCollider meshCollider = transform.GetComponent<MeshCollider>();
									if (meshCollider != null) {
										writeJSON(7, "\"type\": \"" + "MeshCollider" + "\"");
										// NO COMMA!
									}
									
									CapsuleCollider capsuleCollider = transform.GetComponent<CapsuleCollider>();
									if (capsuleCollider != null) {
										writeJSON(7, "\"type\": \"" + "CapsuleCollider" + "\",");
									}
								

									{
										if (boxCollider != null) {

											Vector3 adjustedCenter = boxCollider.center;

											//adjustedCenter.x *= -1;

											string[] center = {adjustedCenter.z.ToString(), 
												adjustedCenter.y.ToString(), 
												adjustedCenter.x.ToString()};

											writeJSON(7, "\"center\": " + Util.ToJSONArray(center) + ",");

											string[] size = {boxCollider.size.z.ToString(), 
												boxCollider.size.y.ToString(), 
												boxCollider.size.x.ToString()};
											
											writeJSON(7, "\"size\": " + Util.ToJSONArray(size) + "");
										}
									}							
									{
										if (sphereCollider != null) {

											Vector3 adjustedCenter = sphereCollider.center;
											
											//adjustedCenter.x *= -1;

											string[] center = {adjustedCenter.z.ToString(), 
												adjustedCenter.y.ToString(), 
												adjustedCenter.x.ToString()};

											
											writeJSON(7, "\"center\": " + Util.ToJSONArray(center) + ",");
											writeJSON(7, "\"radius\": " + sphereCollider.radius.ToString() + "");
										}
									}
									{
										if (capsuleCollider != null) {

											Vector3 adjustedCenter = capsuleCollider.center;
											
											//adjustedCenter.x *= -1;

											string[] center = {adjustedCenter.z.ToString(), 
												adjustedCenter.y.ToString(), 
												adjustedCenter.x.ToString()};										
											
											writeJSON(7, "\"center\": " + Util.ToJSONArray(center) + ",");
											writeJSON(7, "\"radius\": " + capsuleCollider.radius.ToString() + ",");
											writeJSON(7, "\"height\": " + capsuleCollider.height.ToString() + "");
										}
									}
									writeJSON(6, "}");
								}
								
								writeJSON(5, "},", true);
							
							}
						}

						MeshFilter meshFilter = transform.GetComponent<MeshFilter>();
						if (meshFilter != null) {

							for (int i = 0; i < meshFilter.sharedMesh.subMeshCount; i++) {

								string geoKey = null;
								foreach(KeyValuePair<string, Submesh> usedSubmesh in usedSubmeshes) {					
									if (usedSubmesh.Value.meshFilter == meshFilter && usedSubmesh.Value.subMeshIndex == i) {
										geoKey = usedSubmesh.Key;
									}
								}

								string matKey = null;
								foreach(KeyValuePair<string, MeshMaterialLink> meshMaterialLink in meshMaterialLinks) {
									if (meshMaterialLink.Value.meshFilter == meshFilter && i == meshMaterialLink.Value.subMeshIndex) {
										matKey = meshMaterialLink.Key;
									}
								}

								if (geoKey != null && matKey != null ) {
									writeJSON(5, "{");
									{
										writeJSON(6, "\"uuid\": \"" + System.Guid.NewGuid().ToString() + "\",");
										writeJSON(6, "\"type\": \"" + "Mesh" + "\",");
										writeJSON(6, "\"name\": \"" + meshFilter.name + "_SubMesh " + i + "\",");
												
										foreach(KeyValuePair<string, Submesh> usedSubmesh in usedSubmeshes) {					
											if (usedSubmesh.Value.meshFilter == meshFilter && usedSubmesh.Value.subMeshIndex == i) {
												writeJSON(6, "\"geometry\": \"" + geoKey + "\",");



												foreach(KeyValuePair<string, MeshMaterialLink> meshMaterialLink in meshMaterialLinks) {
													if (meshMaterialLink.Value.meshFilter == meshFilter && i == meshMaterialLink.Value.subMeshIndex) {
														writeJSON(6, "\"material\": \"" + matKey + "\",");
														break;
													}
												}

												break;
											}
										}


										writeJSON(6, "\"userdata\": {}");



									}
									writeJSON(5, "},", true);
								}
								
							}
						}

						MonoBehaviour monoBehaviour = transform.GetComponent<MonoBehaviour>();
						if (monoBehaviour != null) {
								
							writeJSON(5, "{");
							{
								writeJSON(6, "\"uuid\": \"" + System.Guid.NewGuid().ToString() + "\",");
								writeJSON(6, "\"type\": \"" + "Object3D" + "\",");
								writeJSON(6, "\"name\": \"" + monoBehaviour.name + "_Script\",");
								
								writeJSON(6, "\"userData\": {");

								{
									const BindingFlags flags = /*BindingFlags.NonPublic | */BindingFlags.Public | 
										BindingFlags.Instance | BindingFlags.Static;
									FieldInfo[] fields = monoBehaviour.GetType().GetFields(flags);
									foreach (FieldInfo fieldInfo in fields)
									{
										object fieldObj = fieldInfo.GetValue(monoBehaviour);
										if (fieldObj != null) {
											string fieldValue = fieldObj.ToString();
	//										if (fieldInfo.FieldType == typeof(float) ||
	//										    fieldInfo.FieldType == typeof(int) ||
	//										    fieldInfo.FieldType == typeof(double)) {
											double n;
											bool needQuotes = !double.TryParse(fieldValue, out n);

											if (fieldValue == "True" || fieldValue == "False") {
												fieldValue = fieldValue.ToLower();
												needQuotes = false;
											}

											if (!needQuotes) {
												writeJSON(7, "\"" + fieldInfo.Name + "\": " + fieldValue + ",");
											}
											else {
												writeJSON(7, "\"" + fieldInfo.Name + "\": \"" + fieldValue + "\",");
											}
										}
									}

									writeJSON(7, "\"type\": \"" + "Script" + "\"");

								}	
								writeJSON(6, "}");	
							}
							writeJSON(5, "},", true);


						}

						jsonFile.Length = jsonFile.Length - (1 + System.Environment.NewLine.Length);
						writeJSON(0, "", true);

						writeJSON(4, "],");
											
					}//s

					writeMatrix(4, transform, settings);

					writeJSON(3, "},", true);
					transformsThatWillBeWritten++;

				}



				// Remove the final comma
				if (transformsThatWillBeWritten > 0) {
					jsonFile.Length = jsonFile.Length - (1 + System.Environment.NewLine.Length);
					writeJSON(0, "", true);
				}

				writeJSON (2, "]");
			}
			writeJSON (1, "},");

			// Images
			writeJSON (1, "\"images\": [");		
			{
				int count = 0;						
				foreach(KeyValuePair<string, TextureImageLink> entry in usedImages) {
					writeJSON (2, "{");					

					writeJSON(3, "\"url\": \"" + entry.Value.filename + "\",");
					writeJSON(3, "\"uuid\": \"" + entry.Key + "\",");
					writeJSON(3, "\"name\": \"" + entry.Value.filename + "\",");
					writeJSON(3, "\"originalUrl\": \"" + entry.Value.filename + "\"");

					writeJSON (2, "}", false);	

					if (count < usedImages.Count - 1) {
						writeJSON (0, ",");
					}
					else {
						writeJSON (0, "");
					}		

					count++;
				}		
			}	
			writeJSON (1, "],");

			// Textures
			writeJSON (1, "\"textures\": [");	
			{
				int count = 0;		

				foreach(KeyValuePair<string, Material> usedMaterial in usedMaterials) {
										
					commonTextures.ForEach (delegate(String texName) {			
						if (!usedMaterial.Value.HasProperty (texName) || !usedMaterial.Value.GetTexture (texName))
							return;

						Texture2D tex = (Texture2D)usedMaterial.Value.GetTexture (texName);

						writeJSON (2, "{");	

						writeJSON (3, "\"uuid\": \"" + usedMaterial.Key + texName + "\",");
						// writeJSON (3, "\"name\": \"_" + usedMaterial.Value.name + "\",");
						writeJSON (3, "\"offset\": [", false);	
						{
							Vector2 vecOffset = usedMaterial.Value.GetTextureOffset (texName);

							writeJSON (0, vecOffset.x + ", ", false);	
							writeJSON (0, vecOffset.y.ToString (), false);
						}
						writeJSON (0, "],");		

						writeJSON (3, "\"repeat\": [", false);	
						{
							Vector2 vecScale = usedMaterial.Value.GetTextureScale ("_MainTex");
							writeJSON (0, vecScale.x + ", ", false);	
							writeJSON (0, vecScale.y.ToString (), false);	
						}
						writeJSON (0, "],");	

						if (tex.filterMode == FilterMode.Point) {
							if (settings.threeRevision == 71) {
								writeJSON (3, "\"magFilter\": \"" + "NearestFilter" + "\",");
								writeJSON (3, "\"minFilter\": \"" + "NearestMipMapNearestFilter" + "\",");
							} else {
								writeJSON (3, "\"magFilter\": " + "1003" + ",");
								writeJSON (3, "\"minFilter\": " + "1004" + ",");
							}
						} else {
							if (settings.threeRevision == 71) {
								writeJSON (3, "\"magFilter\": \"" + "LinearFilter" + "\",");
								writeJSON (3, "\"minFilter\": \"" + "LinearMipMapLinearFilter" + "\",");
							} else {
								writeJSON (3, "\"magFilter\": " + "1006" + ",");
								writeJSON (3, "\"minFilter\": " + "1008" + ",");
							}
						}

						writeJSON (3, "\"wrap\": [", false);	
						{
							if (tex.wrapMode == TextureWrapMode.Repeat) {
								if (settings.threeRevision == 71) {
									writeJSON (0, "\"RepeatWrapping\",", false);	
									writeJSON (0, "\"RepeatWrapping\"", false);					
								} else {
									writeJSON (0, "1000,", false);	
									writeJSON (0, "1000", false);			
								}
							} else {
								if (settings.threeRevision == 71) {
									writeJSON (0, "\"ClampToEdgeWrapping\",", false);	
									writeJSON (0, "\"ClampToEdgeWrapping\"", false);			
								} else {
									writeJSON (0, "1001,", false);	
									writeJSON (0, "1001", false);			
								}
							}
						}
						writeJSON (0, "],");

						foreach (KeyValuePair<string, TextureImageLink> usedImage in usedImages) {
							if (usedImage.Value.texture == tex) {
								writeJSON (3, "\"image\": \"" + usedImage.Key + "\",");
								writeJSON (3, "\"name\": \"" + usedImage.Value.filename + "\",");
							}
						}

						writeJSON (3, "\"anisotropy\": " + 16 + "");
						
						writeJSON (2, "}", false);

						if (count < usedMaterials.Count - 1) {
							writeJSON (0, ",");
						} else {
							writeJSON (0, "");
						}		

					});

					count++;
					
				}					
			}	
			writeJSON (1, "]");

			writeJSON (0, "}", false);

			// Free created Materials
			foreach (KeyValuePair<string, MaterialTextureLink> usedTexture in usedTextures) {
				if (usedTexture.Value.needsCleanup) {
					GameObject.DestroyImmediate(usedTexture.Value.material);
				}
			}

			return jsonFile.ToString();
		}


	}
}