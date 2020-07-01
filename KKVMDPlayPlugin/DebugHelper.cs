using System;
using System.Reflection;
using BepInEx;
using Studio;
using UnityEngine;

namespace KKVMDPlayPlugin
{
	
	internal class DebugHelper : MonoBehaviour
	{
		public static DebugHelper Instance
		{
			get
			{
				return DebugHelper._instance;
			}
		}

		
		public static DebugHelper Install(GameObject container)
		{
			if (DebugHelper._instance == null)
			{
				DebugHelper._instance = container.AddComponent<DebugHelper>();
			}
			return DebugHelper._instance;
		}

		
		public void CopyShader(GameObject go)
		{
			Shader shader = go.GetComponent<MeshRenderer>().material.shader;
			Console.WriteLine(shader.name);
			this.SetMaterial(shader);
		}

		
		private void SetMaterial(Shader shader)
		{
			this._boneMarkerMat = new Material(shader);
			if (this._boneMarkerTex == null)
			{
				this._boneMarkerTex = new Texture2D(1, 1, (TextureFormat)5, false);
				this._boneMarkerTex.SetPixel(0, 0, new Color(0.8f, 0.8f, 0f, 0.5f));
				this._boneMarkerTex.Apply();
			}
			this._boneMarkerMat.mainTexture = this._boneMarkerTex;
		}

		
		public string GetLayer(GameObject go)
		{
			return LayerMask.LayerToName(go.layer);
		}

		
		public string GetShader(GameObject go)
		{
			Renderer component = go.GetComponent<Renderer>();
			if (component != null)
			{
				return component.material.shader.name;
			}
			return "<not a Renderer>";
		}

		
		public void SetColor(GameObject go, float r = 1f, float g = 1f, float b = 1f, float alpha = 1f)
		{
			Renderer component = go.GetComponent<Renderer>();
			if (component != null)
			{
				Material material = component.material;
				Color color;
				color = new Color(r, g, b, alpha);
				material.SetColor("_Color", color);
				component.material = material;
			}
		}

		
		private void Start()
		{
			GameObject gameObject = GameObject.CreatePrimitive(0);
			MeshFilter component = gameObject.GetComponent<MeshFilter>();
			gameObject.GetComponent<MeshRenderer>();
			this._boneMarkerMesh = component.mesh;
			Vector3[] vertices = this._boneMarkerMesh.vertices;
			Vector3[] array = new Vector3[vertices.Length];
			for (int i = 0; i < vertices.Length; i++)
			{
				array[i] = vertices[i] * DebugHelper.MARKER_SIZE;
			}
			this._boneMarkerMesh.vertices = array;
			this.guideObjectLayer = LayerMask.NameToLayer("Studio/Select");
			Material material = this.CreateZTransShader();
			material.SetColor("_Color", new Color(0.8f, 0.8f, 0f, 0.8f));
			this._boneMarkerMat = material;
			DestroyImmediate(gameObject);
		}

		
		private void Update()
		{
		}

		
		public void InstallBoneVisualizer(Transform t)
		{
			if (t == null)
			{
				return;
			}
			if (this._boneMarkerMat == null)
			{
				GameObject gameObject = typeof(GuideObjectManager).GetField("objectOriginal", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(Singleton<GuideObjectManager>.Instance) as GameObject;
				KKVMDPlugin.Logger.Log(BepInEx.Logging.LogLevel.Debug, "Found template guide object. " + gameObject);
				GameObject gameObject2 = gameObject.transform.Find("Sphere").gameObject;
                KKVMDPlugin.Logger.Log(BepInEx.Logging.LogLevel.Debug, "Found template sphere object. " + gameObject2);
				this._boneMarkerMat = gameObject2.GetComponent<Renderer>().material;
                KKVMDPlugin.Logger.Log(BepInEx.Logging.LogLevel.Debug, "Found template shader " + this._boneMarkerMat.shader);
				this._boneMarkerMat.SetColor("_Color", new Color(0.8f, 0.8f, 0f, 0.8f));
			}
			if (t.gameObject.GetComponent<Renderer>() == null)
			{
				t.gameObject.AddComponent<MeshFilter>().sharedMesh = this._boneMarkerMesh;
				t.gameObject.AddComponent<MeshRenderer>().sharedMaterial = this._boneMarkerMat;
			}
			t.gameObject.layer = this.guideObjectLayer;
			for (int i = 0; i < t.childCount; i++)
			{
				Transform child = t.GetChild(i);
				this.InstallBoneVisualizer(child);
			}
		}

		
		public void ShowHideSub(bool show, Transform t)
		{
			MeshRenderer component = t.gameObject.GetComponent<MeshRenderer>();
			if (component != null && component.sharedMaterial == this._boneMarkerMat)
			{
				component.enabled = show;
			}
			for (int i = 0; i < t.childCount; i++)
			{
				this.ShowHideSub(show, t.GetChild(i));
			}
		}

		
		private Material CreateZTransShader()
		{
			return new Material("Shader \"Custom/ColorZOrder\" {\r\nProperties {\r\n\t_Color (\"Main Color\", Color) = (1,1,1,1)\r\n\t_MainTex (\"Base (RGB) Trans (A)\", 2D) = \"white\" {}\r\n}\r\n\r\nSubShader {\r\n\tTags {\"Queue\"=\"Transparent+2\" \"IgnoreProjector\"=\"True\" \"RenderType\"=\"Transparent\"}\r\n\tLOD 200\r\n\r\nCGPROGRAM\r\n#pragma surface surf Lambert alpha\r\n\r\nsampler2D _MainTex;\r\nfixed4 _Color;\r\n\r\nstruct Input {\r\n\tfloat2 uv_MainTex;\r\n};\r\n\r\nvoid surf (Input IN, inout SurfaceOutput o) {\r\n\tfixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;\r\n\to.Albedo = c.rgb;\r\n\to.Alpha = c.a;\r\n}\r\nENDCG\r\n}\r\n\r\nFallback \"Transparent/Unlit\"\r\n}");
		}

		
		private void Clear()
		{
		}

		
		private static DebugHelper _instance;

		
		private Mesh _boneMarkerMesh;

		
		private Material _boneMarkerMat;

		
		private int guideObjectLayer;

		
		public static bool fixEnabled = true;

		
		public static float weight = 0.3f;

		
		public static float axisX = 30f;

		
		public static float axisY = 0f;

		
		public static float axisZ = 0f;

		
		public static float MARKER_SIZE = 0.01f;

		
		private Texture2D _boneMarkerTex;
	}
}
