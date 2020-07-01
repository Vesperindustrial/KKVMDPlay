using System;
using System.Collections.Generic;
using Manager;
using UnityEngine;

namespace KKVMDPlayPlugin
{
	
	public class EyeTextureCache
	{
		
		private EyeTextureCache()
		{
			this.PreloadGagEyeTextures();
		}

		
		public void PreloadGagEyeTextures()
		{
			foreach (ListInfoBase listInfoBase in Singleton<Character>.Instance.chaListCtrl.GetCategoryInfo((ChaListDefine.CategoryNo)2).Values)
			{
				if (listInfoBase.GetInfoInt((ChaListDefine.KeyType)19) != 0)
				{
					string info = listInfoBase.GetInfo((ChaListDefine.KeyType)36);
					string info2 = listInfoBase.GetInfo((ChaListDefine.KeyType)33);
					if ("0" != info && "0" != info2)
					{
						this.loadTexture(info, info2, string.Empty);
						this.loadTexture(info, info2 + "_low", string.Empty);
					}
				}
			}
		}

		
		public void ChangeEyesPtn(ChaControl chara, int ptn, bool blend = true)
		{
			chara.fileStatus.eyesPtn = ptn;
			Dictionary<int, ListInfoBase> categoryInfo = chara.lstCtrl.GetCategoryInfo((ChaListDefine.CategoryNo)2);
			ListInfoBase listInfoBase;
			if (!categoryInfo.TryGetValue(ptn, out listInfoBase))
			{
				ptn = (chara.fileStatus.eyesPtn = 0);
				categoryInfo.TryGetValue(ptn, out listInfoBase);
			}
			if (listInfoBase == null)
			{
				return;
			}
			GameObject referenceInfo = chara.GetReferenceInfo((ChaReference.RefObjKey)120);
			if (referenceInfo)
			{
				referenceInfo.SetActive(1 == listInfoBase.GetInfoInt((ChaListDefine.KeyType)11));
			}
			GameObject referenceInfo2 = chara.GetReferenceInfo((ChaReference.RefObjKey)121);
			if (referenceInfo2)
			{
				referenceInfo2.SetActive(listInfoBase.GetInfoInt((ChaListDefine.KeyType)14) != 0);
			}
			GameObject[] array = new GameObject[]
			{
				chara.GetReferenceInfo((ChaReference.RefObjKey)122),
				chara.GetReferenceInfo((ChaReference.RefObjKey)123),
				chara.GetReferenceInfo((ChaReference.RefObjKey)124)
			};
			int infoInt = listInfoBase.GetInfoInt((ChaListDefine.KeyType)19);
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i])
				{
					array[i].SetActive(i + 1 == infoInt);
				}
			}
			if (chara.rendEye != null && chara.rendEye.Length == 2)
			{
				Texture texture = null;
				string info = listInfoBase.GetInfo((ChaListDefine.KeyType)10);
				string text = listInfoBase.GetInfo((ChaListDefine.KeyType)9);
				float num = 0f;
				if ("0" != info && "0" != text)
				{
					if (!chara.hiPoly)
					{
						text += "_low";
					}
					texture = this.loadTexture(info, text, string.Empty);
					num = 1f;
				}
				foreach (Renderer renderer in chara.rendEye)
				{
					if (!(null == renderer))
					{
						renderer.material.SetTexture(ChaShader._expression, texture);
						renderer.material.SetFloat(ChaShader._exppower, num);
					}
				}
			}
			if (array != null && array.Length != 0 && infoInt != 0)
			{
				Texture tex = null;
				string info2 = listInfoBase.GetInfo((ChaListDefine.KeyType)36);
				string text2 = listInfoBase.GetInfo((ChaListDefine.KeyType)33);
				if ("0" != info2 && "0" != text2)
				{
					if (!chara.hiPoly)
					{
						text2 += "_low";
					}
					tex = this.loadTexture(info2, text2, string.Empty);
				}
				string[] array2 = listInfoBase.GetInfo((ChaListDefine.KeyType)63).Split(new char[]
				{
					'/'
				});
				Vector4 zero = Vector4.zero;
				if (3 <= array2.Length)
				{
					float.TryParse(array2[0], out zero.x);
					float.TryParse(array2[1], out zero.y);
					float.TryParse(array2[2], out zero.z);
				}
				float infoFloat = listInfoBase.GetInfoFloat((ChaListDefine.KeyType)59);
				float infoFloat2 = listInfoBase.GetInfoFloat((ChaListDefine.KeyType)60);
				float infoFloat3 = listInfoBase.GetInfoFloat((ChaListDefine.KeyType)2);
				float infoFloat4 = listInfoBase.GetInfoFloat((ChaListDefine.KeyType)65);
				this.ChangeGagEyesMaterial(chara, infoInt - 1, tex, zero, infoFloat, infoFloat2, infoFloat3, infoFloat4);
			}
			chara.SetForegroundEyesAndEyebrow();
			chara.ChangeSettingEyeShadowColor();
			chara.eyesCtrl.ChangePtn(listInfoBase.GetInfoInt((ChaListDefine.KeyType)21), blend);
		}

		
		public Texture2D loadTexture(string ab, string assetName, string manifest)
		{
			string key = ab + "|" + assetName;
			Texture2D texture2D;
			if (this.eyeTextureCache.ContainsKey(key))
			{
				texture2D = this.eyeTextureCache[key];
			}
			else
			{
				texture2D = CommonLib.LoadAsset<Texture2D>(ab, assetName, false, manifest);
				this.eyeTextureCache[key] = texture2D;
			}
			return texture2D;
		}

		
		public void ChangeGagEyesMaterial(ChaControl __instance, int no, Texture tex, Vector4 v4TileAnim, float sizeSpeed, float sizeWidth, float angleSpeed, float yurayura)
		{
			GameObject[] array = new GameObject[]
			{
				__instance.GetReferenceInfo((ChaReference.RefObjKey)122),
				__instance.GetReferenceInfo((ChaReference.RefObjKey)123),
				__instance.GetReferenceInfo((ChaReference.RefObjKey)124)
			};
			if (null == array[no])
			{
				return;
			}
			SkinnedMeshRenderer component = array[no].GetComponent<SkinnedMeshRenderer>();
			if (null == component)
			{
				return;
			}
			__instance.matGag[no].SetTexture(ChaShader._MainTex, null);
			__instance.matGag[no].SetTexture(ChaShader._MainTex, tex);
			__instance.matGag[no].SetVector(ChaShader._TileAnimation, v4TileAnim);
			__instance.matGag[no].SetFloat(ChaShader._SizeSpeed, sizeSpeed);
			__instance.matGag[no].SetFloat(ChaShader._SizeWidth, sizeWidth);
			__instance.matGag[no].SetFloat(ChaShader._angleSpeed, angleSpeed);
			__instance.matGag[no].SetFloat(ChaShader._yurayura, yurayura);
			component.material = __instance.matGag[no];
		}

		
		public static EyeTextureCache Instance = new EyeTextureCache();

		
		public Dictionary<string, Texture2D> eyeTextureCache = new Dictionary<string, Texture2D>();
	}
}
