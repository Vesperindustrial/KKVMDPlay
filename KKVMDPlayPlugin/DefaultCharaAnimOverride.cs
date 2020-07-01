using System;
using System.Collections;
using UnityEngine;

namespace KKVMDPlayPlugin
{
	
	public class DefaultCharaAnimOverride : MonoBehaviour
	{
		
		
		public bool DefaultAnimeEnabled
		{
			get
			{
				return this.defaultAnimeEnabled;
			}
		}

		
		
		public Animator defaultAnimator
		{
			get
			{
				return null;
			}
		}

		
		public static DefaultCharaAnimOverride Get(ChaControl chara)
		{
			DefaultCharaAnimOverride defaultCharaAnimOverride = chara.gameObject.GetComponent<DefaultCharaAnimOverride>();
			if (defaultCharaAnimOverride == null)
			{
				defaultCharaAnimOverride = chara.gameObject.AddComponent<DefaultCharaAnimOverride>();
				defaultCharaAnimOverride.Init(chara);
			}
			return defaultCharaAnimOverride;
		}

		
		public void Init(ChaControl chara)
		{
			this.chara = chara;
			base.StartCoroutine(this.DisableDefaultAnimIKCo());
		}

		
		private IEnumerator DisableDefaultAnimIKCo()
		{
			for (;;)
			{
				yield return null;
				this.DisableEnableDefaultAnim();
			}
			yield break;
		}

		
		public void DisableEnableDefaultAnim()
		{
			bool flag = this.defaultAnimeEnabled;
		}

		
		public void Aquire(MonoBehaviour controller)
		{
			if (this.defaultAnimeEnabled)
			{
				this.defaultAnimeEnabled = false;
				this.currentController = controller;
				this.DisableEnableDefaultAnim();
			}
		}

		
		public void Release(MonoBehaviour controller)
		{
			if (!this.defaultAnimeEnabled && controller == this.currentController)
			{
				this.defaultAnimeEnabled = true;
				this.currentController = null;
				this.DisableEnableDefaultAnim();
			}
		}

		
		public void IKUpdate()
		{
			bool flag = this.defaultAnimeEnabled;
		}

		
		public void IKLateUpdate()
		{
			bool flag = this.defaultAnimeEnabled;
		}

		
		public void ResetBoneRotations()
		{
			Transform transform = this.chara.objBodyBone.transform;
			for (int i = 0; i < transform.childCount; i++)
			{
				this.InitializeBasePositions(transform.GetChild(i));
			}
		}

		
		private void InitializeBasePositions(Transform t)
		{
			if (t.name.StartsWith("cf_d_bust00") || t.name.Contains("_j_head") || t.name.Contains("cf_d_sk_top"))
			{
				return;
			}
			t.localRotation = Quaternion.identity;
			for (int i = 0; i < t.childCount; i++)
			{
				this.InitializeBasePositions(t.GetChild(i));
			}
		}

		
		private void OnDestroy()
		{
			base.StopAllCoroutines();
		}

		
		private bool defaultAnimeEnabled = true;

		
		public MonoBehaviour currentController;

		
		private ChaControl chara;
	}
}
