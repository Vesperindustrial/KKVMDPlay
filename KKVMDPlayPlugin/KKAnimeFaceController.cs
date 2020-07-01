using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using BepInEx;
using UnityEngine;

namespace KKVMDPlayPlugin
{
	
	public class KKAnimeFaceController : MonoBehaviour
	{
		
		
		
		public bool AnimeEnabled
		{
			get
			{
				return this.enableFaceAnime;
			}
			set
			{
				this.enableFaceAnime = value;
			}
		}

		
		
		
		public bool VMDFaceAnimeEnabled
		{
			get
			{
				return this.enableVMDFaceAnime;
			}
			set
			{
				this.enableVMDFaceAnime = value;
			}
		}

		
		private Dictionary<string, KKAnimeFaceController.FacePattern> LoadFacePatternDic()
		{
			Dictionary<string, KKAnimeFaceController.FacePattern> dictionary = new Dictionary<string, KKAnimeFaceController.FacePattern>();
			string text = "BepInEx/KKVMDPlayPlugin/FaceData.txt";
			string text2 = "BepInEx/KKVMDPlayPlugin/FaceData_sample.txt";
			if (!File.Exists(text) && File.Exists(text2))
			{
				File.Copy(text2, text);
			}
			string[] array = File.ReadAllText(text, Encoding.UTF8).Split(new string[]
			{
				"\r\n"
			}, StringSplitOptions.None);
			for (int i = 0; i < array.Length; i++)
			{
				string text3 = array[i].Trim();
				if (!NullCheck.IsNullOrEmpty(text3) && !text3.StartsWith("#"))
				{
					KKAnimeFaceController.FacePattern facePattern = this.LoadTextData(text3);
					if (facePattern != null)
					{
						dictionary.Add(facePattern.name, facePattern);
					}
				}
			}
			return dictionary;
		}

		
		private void Start()
		{
		}

		
		private void LateUpdate()
		{
			try
			{
				if (this.chara != null && this.chara.loadEnd && this.enableFaceAnime)
				{
					if (this.enableVMDFaceAnime)
					{
						this.UpdateFaceFromVMDAnime();
					}
					else if (this.enableLocalFaceAnim)
					{
						this.UpdateFaceFromSelectedPattern();
					}
				}
			}
			catch (Exception value)
			{
				Console.WriteLine(value);
			}
		}

		
		public void UpdateFaceFromVMDAnime()
		{
			KKAnimeFaceController.FacePattern facePattern = new KKAnimeFaceController.FacePattern();
			facePattern.type = 7;
			facePattern.eyebrowSliderValues = new float[this.numEyebrowPatterns];
			facePattern.eyesSliderValues = new float[this.numEyePatterns];
			facePattern.mouthSliderValues = new float[this.numMouthPatterns];
			facePattern.eyesFixedValue = 0f;
			facePattern.mouthFixedValue = 0f;
			float num = 0f;
			float num2 = 0f;
			float num3 = 0f;
			float num4 = 0f;
			foreach (string key in this.FacesToCheck)
			{
				float z = this.VMDFaceBlendValues[key].localPosition.z;
				if (z != 0f)
				{
					num += z;
					KKAnimeFaceController.FacePattern facePattern2;
					if (this.facePatternDic.TryGetValue(key, out facePattern2))
					{
						if ((facePattern2.type & 1) != 0)
						{
							this.AddPatternBlendValues(facePattern.eyebrowSliderValues, facePattern2.eyebrowSliderValues, z);
							num2 += z;
						}
						if ((facePattern2.type & 2) != 0)
						{
							this.AddPatternBlendValues(facePattern.eyesSliderValues, facePattern2.eyesSliderValues, z);
							facePattern.eyesFixedValue += facePattern2.eyesFixedValue * z;
							num3 += z;
						}
						if ((facePattern2.type & 4) != 0)
						{
							this.AddPatternBlendValues(facePattern.mouthSliderValues, facePattern2.mouthSliderValues, z);
							facePattern.mouthFixedValue += facePattern2.mouthFixedValue * z;
							if (facePattern2.tongueState != 0)
							{
								facePattern.tongueState = facePattern2.tongueState;
							}
							num4 += z;
						}
					}
				}
			}
			if (num2 < 1f)
			{
				this.AddPatternBlendValues(facePattern.eyebrowSliderValues, this.defaultPattern.eyebrowSliderValues, 1f - num2);
			}
			if (num3 == 0f)
			{
				facePattern.eyesFixedValue = -0.1f;
			}
			else if (num3 < 1f)
			{
				if (this.enableDefaultFace)
				{
					facePattern.eyesFixedValue += this.defaultPattern.eyesFixedValue * (1f - num3);
					this.AddPatternBlendValues(facePattern.eyesSliderValues, this.defaultPattern.eyesSliderValues, 1f - num3);
				}
			}
			else if (num3 > 1f)
			{
				facePattern.eyesFixedValue /= num3;
			}
			if (num4 == 0f)
			{
				facePattern.mouthFixedValue = -0.1f;
			}
			else if (num4 < 1f)
			{
				if (this.enableDefaultFace)
				{
					facePattern.mouthFixedValue += this.defaultPattern.mouthFixedValue * (1f - num4);
					this.AddPatternBlendValues(facePattern.mouthSliderValues, this.defaultPattern.mouthSliderValues, 1f - num4);
				}
			}
			else if (num4 > 1f)
			{
				facePattern.mouthFixedValue /= num4;
			}
			if (num > 0f)
			{
				this.ApplyFace(facePattern);
				return;
			}
			if (this.enableDefaultFace)
			{
				this.ApplyFace(this.defaultPattern);
			}
		}

		
		public void SetNextFacePattern(string facePatternName, float t = 1f)
		{
			if (!this.facePatternDic.ContainsKey(facePatternName))
			{
				return;
			}
			KKAnimeFaceController.FacePattern p = this.facePatternDic[facePatternName];
			if (t <= 0f || this.currentFacePattern == null)
			{
				this.currentFacePattern = p;
				this.nextFacePattern = null;
				this.facePatternTransitDiff = 0f;
				this.facePatternTransit = 0f;
				this.ApplyFace(p);
				return;
			}
			if (this.nextFacePattern != null)
			{
				this.currentFacePattern = this.nextFacePattern;
			}
			this.nextFacePattern = p;
			this.facePatternTransit = 0f;
			this.facePatternTransitDiff = 1f / t;
		}

		
		private KKAnimeFaceController.FacePattern GetCurrentFaceValue()
		{
			KKAnimeFaceController.FacePattern facePattern = new KKAnimeFaceController.FacePattern();
			facePattern.type = 7;
			facePattern.eyesFixedValue = this.chara.eyesCtrl.FixedRate;
			facePattern.mouthFixedValue = this.chara.mouthCtrl.FixedRate;
			IDictionary<int, float> dictionary = KKAnimeFaceController.f_dictNowFace.GetValue(this.chara.eyebrowCtrl) as IDictionary<int, float>;
			IDictionary<int, float> dictionary2 = KKAnimeFaceController.f_dictNowFace.GetValue(this.chara.eyesCtrl) as IDictionary<int, float>;
			IDictionary<int, float> dictionary3 = KKAnimeFaceController.f_dictNowFace.GetValue(this.chara.mouthCtrl) as IDictionary<int, float>;
			facePattern.eyebrowSliderValues = new float[this.numEyebrowPatterns];
			foreach (int num in dictionary.Keys)
			{
				facePattern.eyebrowSliderValues[num] = dictionary[num];
			}
			facePattern.eyesSliderValues = new float[this.numEyePatterns];
			foreach (int num2 in dictionary2.Keys)
			{
				facePattern.eyesSliderValues[num2] = dictionary2[num2];
			}
			facePattern.mouthSliderValues = new float[this.numMouthPatterns];
			foreach (int num3 in dictionary3.Keys)
			{
				facePattern.mouthSliderValues[num3] = dictionary3[num3];
			}
			facePattern.tongueState = this.chara.fileStatus.tongueState;
			return facePattern;
		}

		
		private void ApplyFace(KKAnimeFaceController.FacePattern p)
		{
			if (this.chara == null)
			{
				return;
			}
			this.chara.eyesCtrl.FixedRate = p.eyesFixedValue;
			this.chara.eyebrowCtrl.ChangeFace(this.CalcBlendDictionary(p.eyebrowSliderValues), false);
			int maxPatternNo = this.GetMaxPatternNo(p.eyesPatternValue);
			if (maxPatternNo != this.chara.fileStatus.eyesPtn)
			{
                KKVMDPlugin.Logger.Log(BepInEx.Logging.LogLevel.Debug, string.Format("Change to max eye pattern: {0} -> {1}", this.chara.fileStatus.eyesPtn, maxPatternNo));
				this.chara.ChangeEyesPtn(maxPatternNo, true);
			}
			this.chara.eyesCtrl.ChangeFace(this.CalcBlendDictionary(p.eyesSliderValues), false);
			if (this.inLipSyncMode)
			{
				this.chara.mouthCtrl.OpenMax = 1f;
			}
			else
			{
				this.chara.mouthCtrl.FixedRate = p.mouthFixedValue;
			}
			this.chara.mouthCtrl.ChangeFace(this.CalcBlendDictionary(p.mouthSliderValues), false);
			this.chara.fileStatus.tongueState = p.tongueState;
		}

		
		private int GetMaxPatternNo(Dictionary<int, float> values)
		{
			float num = float.NegativeInfinity;
			int result = 0;
			foreach (KeyValuePair<int, float> keyValuePair in values)
			{
				if (keyValuePair.Value > num)
				{
					result = keyValuePair.Key;
					num = keyValuePair.Value;
				}
			}
			return result;
		}

		
		private object ToValueString(float[] items)
		{
			string text = "";
			for (int i = 0; i < items.Length; i++)
			{
				if (items[i] > 0f)
				{
					text += string.Format("{0}:{1},", i, items[i]);
				}
			}
			return text;
		}

		
		private void ToggleTongueState()
		{
			if (this.chara.fileStatus.tongueState == 1)
			{
				this.chara.fileStatus.tongueState = 0;
				return;
			}
			this.chara.fileStatus.tongueState = 1;
		}

		
		private void AddPatternBlendValues(float[] to, float[] newValues, float blend)
		{
			for (int i = 0; i < to.Length; i++)
			{
				to[i] += newValues[i] * blend;
			}
		}

		
		private void AddPatternBlendValues(Dictionary<int, float> to, Dictionary<int, float> newValues, float blend)
		{
			foreach (int key in newValues.Keys)
			{
				float num = 0f;
				if (to.ContainsKey(key))
				{
					num = to[key];
				}
				to[key] = num + newValues[key] * blend;
			}
		}

		
		private Dictionary<int, float> CalcBlendDictionary(float[] values)
		{
			float num = 0f;
			for (int i = 0; i < values.Length; i++)
			{
				num += values[i];
			}
			Dictionary<int, float> dictionary = new Dictionary<int, float>();
			if (num == 0f)
			{
				dictionary[0] = 1f;
			}
			else
			{
				if (num < 1f)
				{
					num = 1f;
				}
				for (int j = 0; j < values.Length; j++)
				{
					if (values[j] > 0f)
					{
						dictionary[j] = values[j] / num;
					}
				}
			}
			return dictionary;
		}

		
		private static string CreateTextData(KKAnimeFaceController.FacePattern p)
		{
			string text = p.name + "=";
			switch (p.type)
			{
			case 1:
				text += "EYEBROW,";
				break;
			case 2:
				text += "EYE,";
				break;
			case 4:
				text += "MOUTH,";
				break;
			case 7:
				text += "ALL,";
				break;
			}
			if ((p.type & 1) != 0)
			{
				for (int i = 0; i < p.eyesSliderValues.Length; i++)
				{
					if (p.eyesSliderValues[i] > 0f)
					{
						text = string.Concat(new object[]
						{
							text,
							"eyebrow:",
							i,
							":",
							p.eyesSliderValues[i],
							","
						});
					}
				}
			}
			if ((p.type & 2) != 0)
			{
				text = string.Concat(new object[]
				{
					text,
					"eyeopen:",
					p.eyesFixedValue,
					","
				});
				for (int j = 0; j < p.eyesSliderValues.Length; j++)
				{
					if (p.eyesSliderValues[j] > 0f)
					{
						text = string.Concat(new object[]
						{
							text,
							"eye:",
							j,
							":",
							p.eyesSliderValues[j],
							","
						});
					}
				}
			}
			if ((p.type & 4) != 0)
			{
				text = string.Concat(new object[]
				{
					text,
					"mouthopen:",
					p.mouthFixedValue,
					","
				});
				for (int k = 0; k < p.mouthSliderValues.Length; k++)
				{
					if (p.mouthSliderValues[k] > 0f)
					{
						text = string.Concat(new object[]
						{
							text,
							"mouth:",
							k,
							":",
							p.mouthSliderValues[k],
							","
						});
					}
				}
				text = text + "tongue:" + p.tongueState;
			}
			return text;
		}

		
		private KKAnimeFaceController.FacePattern LoadTextData(string text)
		{
			if (text != null)
			{
				try
				{
					KKAnimeFaceController.FacePattern facePattern = new KKAnimeFaceController.FacePattern();
					Dictionary<int, ListInfoBase> categoryInfo = this.chara.lstCtrl.GetCategoryInfo((ChaListDefine.CategoryNo)2);
					int num = text.IndexOf("=");
					if (num == -1)
					{
						return null;
					}
					facePattern.name = text.Substring(0, num);
					text = text.Substring(num + 1, text.Length - num - 1);
					string[] array = text.Split(new char[]
					{
						','
					});
					float[] array2 = new float[this.numEyebrowPatterns];
					float[] array3 = new float[this.numEyePatterns];
					float[] array4 = new float[this.numMouthPatterns];
					Dictionary<int, float> dictionary = new Dictionary<int, float>();
					float eyesFixedValue = -1f;
					float mouthFixedValue = 0f;
					byte tongueState = 0;
					int type = 7;
					string a = array[0].ToUpper();
					if (!(a == "EYEBROW"))
					{
						if (!(a == "EYE"))
						{
							if (a == "MOUTH")
							{
								type = 4;
							}
						}
						else
						{
							type = 2;
						}
					}
					else
					{
						type = 1;
					}
					for (int i = 1; i < array.Length; i++)
					{
						string[] array5 = array[i].Trim().Split(new char[]
						{
							':'
						});
						if (array5[0] == "eye")
						{
							int key = int.Parse(array5[1]);
							int infoInt = categoryInfo[key].GetInfoInt((ChaListDefine.KeyType)21);
							float num2 = float.Parse(array5[2]);
							dictionary[key] = num2;
							array3[infoInt] = num2;
						}
						else if (array5[0] == "mouth")
						{
							int num3 = int.Parse(array5[1]);
							float num4 = float.Parse(array5[2]);
							array4[num3] = num4;
						}
						else if (array5[0] == "tongue")
						{
							tongueState = byte.Parse(array5[1]);
						}
						else if (array5[0] == "eyeopen")
						{
							eyesFixedValue = float.Parse(array5[1]);
						}
						else if (array5[0] == "mouthopen")
						{
							mouthFixedValue = float.Parse(array5[1]);
						}
						else if (array5[0] == "eyebrow")
						{
							int num5 = int.Parse(array5[1]);
							float num6 = float.Parse(array5[2]);
							array2[num5] = num6;
						}
					}
					facePattern.type = type;
					facePattern.eyebrowSliderValues = array2;
					facePattern.eyesSliderValues = array3;
					facePattern.eyesPatternValue = dictionary;
					facePattern.mouthSliderValues = array4;
					facePattern.eyesFixedValue = eyesFixedValue;
					facePattern.mouthFixedValue = mouthFixedValue;
					facePattern.tongueState = tongueState;
					return facePattern;
				}
				catch (Exception ex)
				{
					Console.WriteLine("Parse Error {0}: {1}", text, ex.ToString());
					Console.WriteLine(ex);
				}
			}
			return null;
		}

		
		public string GetCurrentPatternString()
		{
			return KKAnimeFaceController.CreateTextData(this.GetCurrentFaceValue());
		}

		
		public static KKAnimeFaceController Install(ChaControl chara)
		{
			KKAnimeFaceController kkanimeFaceController = chara.GetComponent<KKAnimeFaceController>();
			if (kkanimeFaceController == null)
			{
				kkanimeFaceController = chara.gameObject.AddComponent<KKAnimeFaceController>();
				kkanimeFaceController.Init(chara);
			}
			return kkanimeFaceController;
		}

		
		public void Init(ChaControl charFemale)
		{
			this.chara = charFemale;
			this.numEyebrowPatterns = this.GetMaxPatterns(this.chara.eyebrowCtrl);
			this.numEyePatterns = this.GetMaxPatterns(this.chara.eyesCtrl);
			this.numMouthPatterns = this.GetMaxPatterns(this.chara.mouthCtrl);
			if (this.facePatternDic == null)
			{
				this.facePatternDic = this.LoadFacePatternDic();
			}
			if (this.facePatternDic.ContainsKey("_DEFAULT_"))
			{
				this.defaultPattern = this.facePatternDic["_DEFAULT_"];
			}
			else
			{
				this.defaultPattern = new KKAnimeFaceController.FacePattern();
				this.defaultPattern.eyesFixedValue = 1f;
				this.defaultPattern.eyebrowSliderValues = new float[this.numEyebrowPatterns];
				this.defaultPattern.eyebrowSliderValues[0] = 1f;
				this.defaultPattern.mouthFixedValue = 0f;
				this.defaultPattern.eyesSliderValues = new float[this.numEyePatterns];
				this.defaultPattern.eyesSliderValues[0] = 1f;
				this.defaultPattern.mouthSliderValues = new float[this.numMouthPatterns];
				this.defaultPattern.mouthSliderValues[0] = 1f;
				this.defaultPattern.tongueState = 0;
			}
			this.InitFaceDic();
			this.InitDummyGo();
		}

		
		private int GetMaxPatterns(FBSBase fbs)
		{
			int num = 0;
			for (int i = 0; i < fbs.FBSTarget.Length; i++)
			{
				num = Math.Max(num, fbs.FBSTarget[i].PtnSet.Length);
			}
			return num;
		}

		
		private void InitFaceDic()
		{
			this.FacesToCheck = new HashSet<string>();
			this.BlendSet = new Dictionary<string, KKAnimeFaceController.FaceBlendSetPattern>();
			foreach (string text in this.facePatternDic.Keys)
			{
				KKAnimeFaceController.FaceBlendSetPattern faceBlendSetPattern = new KKAnimeFaceController.FaceBlendSetPattern();
				faceBlendSetPattern.controller = this;
				faceBlendSetPattern.faceName = text;
				this.BlendSet[text] = faceBlendSetPattern;
				this.FacesToCheck.Add(text);
			}
		}

		
		private void InitDummyGo()
		{
			foreach (string text in this.facePatternDic.Keys)
			{
				GameObject gameObject = new GameObject(text);
				gameObject.transform.parent = base.transform;
				gameObject.transform.localPosition = Vector3.zero;
				gameObject.transform.localRotation = Quaternion.identity;
				this.VMDFaceBlendValues[text] = gameObject.transform;
			}
		}

		
		public float GetValue(string vmdFaceName)
		{
			Transform transform = null;
			this.VMDFaceBlendValues.TryGetValue(vmdFaceName, out transform);
			if (transform == null)
			{
				return 0f;
			}
			return transform.localPosition.z;
		}

		
		public void SetValue(string vmdFaceName, float value)
		{
			this.VMDFaceBlendValues[vmdFaceName].localPosition = new Vector3(0f, 0f, value);
		}

		
		public void ResetFaceValues()
		{
			foreach (string key in this.VMDFaceBlendValues.Keys)
			{
				this.VMDFaceBlendValues[key].localPosition = Vector3.zero;
			}
		}

		
		private void UpdateFaceFromSelectedPattern()
		{
			if (this.nextFacePattern != null && this.facePatternTransitDiff > 0f)
			{
				this.facePatternTransit = Mathf.Clamp01(this.facePatternTransit + this.facePatternTransitDiff * Time.deltaTime);
				if (this.facePatternTransit >= 1f)
				{
					this.ApplyFace(this.nextFacePattern);
					this.currentFacePattern = this.nextFacePattern;
					this.facePatternTransit = 0f;
					this.facePatternTransitDiff = 0f;
					this.nextFacePattern = null;
					return;
				}
				KKAnimeFaceController.FacePattern p = this.MixFacePattern(this.currentFacePattern, this.nextFacePattern, this.facePatternTransit);
				this.ApplyFace(p);
			}
		}

		
		private KKAnimeFaceController.FacePattern MixFacePattern(KKAnimeFaceController.FacePattern currentFacePattern, KKAnimeFaceController.FacePattern nextFacePattern, float v)
		{
			KKAnimeFaceController.FacePattern facePattern = new KKAnimeFaceController.FacePattern();
			facePattern.type = 7;
			for (int i = 0; i < facePattern.eyebrowSliderValues.Length; i++)
			{
				facePattern.eyebrowSliderValues[i] = Mathf.Lerp(currentFacePattern.eyebrowSliderValues[i], nextFacePattern.eyebrowSliderValues[i], v);
			}
			facePattern.eyesFixedValue = Mathf.Lerp(currentFacePattern.eyesFixedValue, nextFacePattern.eyesFixedValue, v);
			facePattern.eyesSliderValues = new float[currentFacePattern.eyesSliderValues.Length];
			for (int j = 0; j < facePattern.eyesSliderValues.Length; j++)
			{
				facePattern.eyesSliderValues[j] = Mathf.Lerp(currentFacePattern.eyesSliderValues[j], nextFacePattern.eyesSliderValues[j], v);
			}
			facePattern.mouthFixedValue = Mathf.Lerp(currentFacePattern.mouthFixedValue, nextFacePattern.mouthFixedValue, v);
			facePattern.mouthSliderValues = new float[currentFacePattern.mouthSliderValues.Length];
			for (int k = 0; k < facePattern.mouthSliderValues.Length; k++)
			{
				facePattern.mouthSliderValues[k] = Mathf.Lerp(currentFacePattern.mouthSliderValues[k], nextFacePattern.mouthSliderValues[k], v);
			}
			if (v > 0f && nextFacePattern.tongueState > 0)
			{
				facePattern.tongueState = nextFacePattern.tongueState;
			}
			else
			{
				nextFacePattern.tongueState = 0;
			}
			return facePattern;
		}

		
		private ChaControl chara;

		
		public bool enableFaceAnime = true;

		
		public bool enableVMDFaceAnime = true;

		
		public bool enableLocalFaceAnim;

		
		public Dictionary<string, KKAnimeFaceController.FaceBlendSetPattern> BlendSet;

		
		public Dictionary<string, Transform> VMDFaceBlendValues = new Dictionary<string, Transform>();

		
		public HashSet<string> FacesToCheck;

		
		public bool enableDefaultFace;

		
		public const string DEFAULT_FACE_NAME = "_DEFAULT_";

		
		private KKAnimeFaceController.FacePattern defaultPattern = new KKAnimeFaceController.FacePattern();

		
		public const int PTYPE_EYEBROW = 1;

		
		public const int PTYPE_EYE = 2;

		
		public const int PTYPE_MOUTH = 4;

		
		public const int PTYPE_ALL = 7;

		
		private Dictionary<string, KKAnimeFaceController.FacePattern> facePatternDic;

		
		private int numEyebrowPatterns;

		
		private int numEyePatterns;

		
		private int numMouthPatterns;

		
		private KKAnimeFaceController.FacePattern currentFacePattern;

		
		private KKAnimeFaceController.FacePattern nextFacePattern;

		
		private float facePatternTransitDiff;

		
		private float facePatternTransit;

		
		public bool inLipSyncMode;

		
		private static FieldInfo f_dictNowFace = typeof(FBSBase).GetField("dictNowFace", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField);

		
		public class FaceBlendSetPattern
		{
			
			
			public Dictionary<string, KKAnimeFaceController.FaceBlendSetPattern> Set
			{
				get
				{
					this.Apply();
					return this.controller.BlendSet;
				}
			}

			
			public void Apply()
			{
				this.controller.SetNextFacePattern(this.faceName, 0f);
			}

			
			public void Dump()
			{
				Console.WriteLine(this.faceName);
			}

			
			public KKAnimeFaceController controller;

			
			public string faceName;
		}

		
		internal class FacePattern
		{
			
			internal int type;

			
			internal string name;

			
			internal float[] eyebrowSliderValues = new float[0];

			
			internal float[] eyesSliderValues = new float[0];

			
			internal float[] mouthSliderValues = new float[0];

			
			internal Dictionary<int, float> eyesPatternValue = new Dictionary<int, float>();

			
			internal float eyesFixedValue = 1f;

			
			internal float mouthFixedValue;

			
			internal byte tongueState;
		}
	}
}
