using System;
using System.IO;
using System.Threading;
using BepInEx;
using BepInEx.Logging;
using Manager;
using Sound;
using UnityEngine;

namespace KKVMDPlayPlugin
{
	
	public class CustomSoundMgr
	{

        
        
        
        public bool Loop
		{
			get
			{
				return this._loop;
			}
			set
			{
				this._loop = value;
				if (this.audioSource != null)
				{
					this.audioSource.loop = this._loop;
				}
			}
		}

		
		
		
		public float Speed
		{
			get
			{
				return this._speed;
			}
			set
			{
				this._speed = value;
				if (this.audioSource != null)
				{
					this.audioSource.pitch = this._speed;
				}
			}
		}

		
		
		public string audioFilePath
		{
			get
			{
				return this._audioFilePath;
			}
		}

		
		public void ClearClip()
		{
			this.DestroyAudioSource();
			this.currentAudioClip = null;
			this._audioFilePath = null;
		}

		
		public bool SetSoundClip(string path)
		{
			AudioClip audioClip = this.LoadSoundAsClip(path);
			if (audioClip != null)
			{
				this.currentAudioClip = audioClip;
				this._audioFilePath = path;
				this.DestroyAudioSource();
				this.CreateSoundAndSetClip(audioClip);
				return true;
			}
			return false;
		}

		
		private void DestroyAudioSource()
		{
			if (this.audioSource != null)
			{
				this.audioSource.Stop();
				UnityEngine.Object.DestroyObject(this.audioSource);
				this.audioSource = null;
			}
		}

		
		public AudioClip LoadSoundAsClip(string path)
		{
			try
			{
				if (File.Exists(path))
				{
					WWW www = new WWW(new Uri(path).AbsoluteUri);
					while (!www.isDone)
					{
						Thread.Sleep(100);
					}
					AudioClip audioClip = WWWAudioExtensions.GetAudioClip(www);
                    KKVMDPlugin.Logger.Log(LogLevel.Debug, string.Format("Successfully loaded {0} as AudioClip.", path));
					return audioClip;
				}
			}
			catch (Exception value)
			{
				Console.WriteLine(value);
			}
			return null;
		}

		
		public void PlaySound()
		{
			if (this.audioSource != null && this.audioSource.clip != null)
			{
				this.audioSource.Play();
				return;
			}
			this.PlaySound(this.currentAudioClip);
		}

		
		public void PauseSound()
		{
			if (this.audioSource != null && this.audioSource.clip != null)
			{
				this.audioSource.Pause();
			}
		}

		
		public void StopSound()
		{
			if (this.audioSource != null)
			{
				this.audioSource.Stop();
				this.audioSource.time = 0f;
			}
		}

		
		public void PlaySound(AudioClip audioClip)
		{
			if (audioClip != null)
			{
				KKVMDPlugin.Logger.Log(BepInEx.Logging.LogLevel.Debug, string.Format("Playing clip {0}", audioClip.name));
				this.CreateSoundAndSetClip(audioClip);
				this.audioSource.Play();
			}
		}

		
		private AudioSource CreateSoundAndSetClip(AudioClip audioClip)
		{
			if (this.audioSourceRoot == null)
			{
				this.audioSourceRoot = new GameObject();
				this.audioSourceRoot.transform.position = Vector3.zero;
				this.audioSourceRoot.transform.rotation = Quaternion.identity;
			}
			this.audioSource = Singleton<Manager.Sound>.Instance.Create((Manager.Sound.Type)1, audioClip);
			this.audioSource.gameObject.transform.parent = this.audioSourceRoot.transform;
			this.audioSource.clip = audioClip;
			this.audioSource.loop = this._loop;
			this.audioSource.pitch = this._speed;
			this.audioSource.spatialize = false;
			this.audioSource.spatialBlend = 0f;
			this.audioSource.playOnAwake = false;
			return this.audioSource;
		}

		
		
		public float SoundLength
		{
			get
			{
				if (this.audioSource != null && this.audioSource.clip != null)
				{
					return this.audioSource.clip.length;
				}
				return 0f;
			}
		}

		
		
		
		public float AnimePosition
		{
			get
			{
				if (this.audioSource != null && this.audioSource.clip != null)
				{
					return this.audioSource.time;
				}
				return 0f;
			}
			set
			{
				if (this.audioSource != null && this.audioSource.clip != null)
				{
					if (value < 0f)
					{
						value = 0f;
					}
					if (this.audioSource.clip.length >= value)
					{
						this.audioSource.time = value;
						return;
					}
					this.audioSource.time = this.audioSource.clip.length;
				}
			}
		}

		
		public AudioClip currentAudioClip;

		
		private string _audioFilePath;

		
		private GameObject audioSourceRoot;

		
		private AudioSource audioSource;

		
		private bool _loop;

		
		private float _speed = 1f;
	}
}
