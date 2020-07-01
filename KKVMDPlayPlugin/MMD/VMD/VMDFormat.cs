using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MMD.VMD
{
	
	public class VMDFormat
	{
		
		private void EntryPathes(string path)
		{
			this.path = path;
			string[] array = path.Split(new char[]
			{
				'/'
			});
			this.name = array[array.Length - 1];
			this.name = this.name.Split(new char[]
			{
				'.'
			})[0];
			this.folder = array[0];
			for (int i = 1; i < array.Length - 1; i++)
			{
				this.folder = this.folder + "/" + array[i];
			}
		}

		
		public VMDFormat(BinaryReader bin, string path, string clip_name)
		{
			try
			{
				this.clip_name = clip_name;
				this.header = new VMDFormat.Header(bin);
				this.read_count++;
				this.motion_list = new VMDFormat.MotionList(bin);
				this.read_count++;
				this.skin_list = new VMDFormat.SkinList(bin);
				this.read_count++;
				this.camera_list = new VMDFormat.CameraList(bin);
				this.read_count++;
				this.light_list = new VMDFormat.LightList(bin);
				this.read_count++;
				this.self_shadow_list = new VMDFormat.SelfShadowList(bin);
				this.read_count++;
			}
			catch (EndOfStreamException)
			{
				if (this.read_count <= 0)
				{
					this.header = null;
				}
				if (this.read_count <= 1 || this.motion_list.motion_count <= 0u)
				{
					this.motion_list = null;
				}
				if (this.read_count <= 2 || this.skin_list.skin_count <= 0u)
				{
					this.skin_list = null;
				}
				if (this.read_count <= 3 || this.camera_list.camera_count <= 0u)
				{
					this.camera_list = null;
				}
				if (this.read_count <= 4 || this.light_list.light_count <= 0u)
				{
					this.light_list = null;
				}
				if (this.read_count <= 5 || this.self_shadow_list.self_shadow_count <= 0u)
				{
					this.self_shadow_list = null;
				}
			}
		}

		
		public string name;

		
		public string path;

		
		public string folder;

		
		public string clip_name;

		
		public GameObject pmd;

		
		public VMDFormat.Header header;

		
		public VMDFormat.MotionList motion_list;

		
		public VMDFormat.SkinList skin_list;

		
		public VMDFormat.LightList light_list;

		
		public VMDFormat.CameraList camera_list;

		
		public VMDFormat.SelfShadowList self_shadow_list;

		
		private int read_count;

		
		public class Header : Format
		{
			
			public Header(BinaryReader bin)
			{
				this.vmd_header = base.ConvertByteToString(bin.ReadBytes(30));
				this.vmd_model_name = base.ConvertByteToString(bin.ReadBytes(20));
			}

			
			public string vmd_header;

			
			public string vmd_model_name;
		}

		
		public class MotionList : Format
		{
			
			public MotionList(BinaryReader bin)
			{
				this.motion_count = bin.ReadUInt32();
				this.motion = new Dictionary<string, List<VMDFormat.Motion>>();
				VMDFormat.Motion[] array = new VMDFormat.Motion[this.motion_count];
				int num = 0;
				while ((long)num < (long)((ulong)this.motion_count))
				{
					array[num] = new VMDFormat.Motion(bin);
					num++;
				}
				Array.Sort<VMDFormat.Motion>(array);
				int num2 = 0;
				while ((long)num2 < (long)((ulong)this.motion_count))
				{
					try
					{
						this.motion.Add(array[num2].bone_name, new List<VMDFormat.Motion>());
					}
					catch
					{
					}
					num2++;
				}
				int num3 = 0;
				while ((long)num3 < (long)((ulong)this.motion_count))
				{
					this.motion[array[num3].bone_name].Add(array[num3]);
					num3++;
				}
			}

			
			public uint motion_count;

			
			public Dictionary<string, List<VMDFormat.Motion>> motion;
		}

		
		public class Motion : Format
		{
			
			public Motion(BinaryReader bin)
			{
				this.bone_name = base.ConvertByteToString(bin.ReadBytes(15));
				this.flame_no = bin.ReadUInt32();
				this.location = base.ReadSinglesToVector3(bin);
				this.rotation = base.ReadSinglesToQuaternion(bin);
				this.interpolation = bin.ReadBytes(64);
				this.count = (int)this.flame_no;
			}

			
			public Motion(string bone_name, uint frame_no, Vector3 location, Quaternion rotation, byte[] interpolation)
			{
				this.bone_name = bone_name;
				this.flame_no = frame_no;
				this.location = location;
				this.rotation = rotation;
				this.interpolation = interpolation;
				this.count = (int)this.flame_no;
			}

			
			public byte GetInterpolation(int i, int j, int k)
			{
				return this.interpolation[i * 16 + j * 4 + k];
			}

			
			public void SetInterpolation(byte val, int i, int j, int k)
			{
				this.interpolation[i * 16 + j * 4 + k] = val;
			}

			
			public string bone_name;

			
			public uint flame_no;

			
			public Vector3 location;

			
			public Quaternion rotation;

			
			public byte[] interpolation;
		}

		
		public class SkinList : Format
		{
			
			public SkinList(BinaryReader bin)
			{
				this.skin_count = bin.ReadUInt32();
				this.skin = new Dictionary<string, List<VMDFormat.SkinData>>();
				VMDFormat.SkinData[] array = new VMDFormat.SkinData[this.skin_count];
				int num = 0;
				while ((long)num < (long)((ulong)this.skin_count))
				{
					array[num] = new VMDFormat.SkinData(bin);
					num++;
				}
				Array.Sort<VMDFormat.SkinData>(array);
				int num2 = 0;
				while ((long)num2 < (long)((ulong)this.skin_count))
				{
					try
					{
						this.skin.Add(array[num2].skin_name, new List<VMDFormat.SkinData>());
					}
					catch
					{
					}
					num2++;
				}
				int num3 = 0;
				while ((long)num3 < (long)((ulong)this.skin_count))
				{
					this.skin[array[num3].skin_name].Add(array[num3]);
					num3++;
				}
			}

			
			public uint skin_count;

			
			public Dictionary<string, List<VMDFormat.SkinData>> skin;
		}

		
		public class SkinData : Format
		{
			
			public SkinData(BinaryReader bin)
			{
				this.skin_name = base.ConvertByteToString(bin.ReadBytes(15));
				this.flame_no = bin.ReadUInt32();
				this.weight = bin.ReadSingle();
				this.count = (int)this.flame_no;
			}

			
			public string skin_name;

			
			public uint flame_no;

			
			public float weight;
		}

		
		public class CameraList : Format
		{
			
			public CameraList(BinaryReader bin)
			{
				this.camera_count = bin.ReadUInt32();
				this.camera = new VMDFormat.CameraData[this.camera_count];
				int num = 0;
				while ((long)num < (long)((ulong)this.camera_count))
				{
					this.camera[num] = new VMDFormat.CameraData(bin);
					num++;
				}
				Array.Sort<VMDFormat.CameraData>(this.camera);
			}

			
			public uint camera_count;

			
			public VMDFormat.CameraData[] camera;
		}

		
		public class CameraData : Format
		{
			
			public CameraData(BinaryReader bin)
			{
				this.flame_no = bin.ReadUInt32();
				this.length = bin.ReadSingle();
				this.location = base.ReadSinglesToVector3(bin);
				this.rotation = base.ReadSinglesToVector3(bin);
				this.interpolation = bin.ReadBytes(24);
				this.viewing_angle = bin.ReadUInt32();
				this.perspective = bin.ReadByte();
				this.count = (int)this.flame_no;
			}

			
			public byte GetInterpolation(int i, int j)
			{
				return this.interpolation[i * 6 + j];
			}

			
			public void SetInterpolation(byte val, int i, int j)
			{
				this.interpolation[i * 6 + j] = val;
			}

			
			public uint flame_no;

			
			public float length;

			
			public Vector3 location;

			
			public Vector3 rotation;

			
			public byte[] interpolation;

			
			public uint viewing_angle;

			
			public byte perspective;
		}

		
		public class LightList : Format
		{
			
			public LightList(BinaryReader bin)
			{
				this.light_count = bin.ReadUInt32();
				this.light = new VMDFormat.LightData[this.light_count];
				int num = 0;
				while ((long)num < (long)((ulong)this.light_count))
				{
					this.light[num] = new VMDFormat.LightData(bin);
					num++;
				}
				Array.Sort<VMDFormat.LightData>(this.light);
			}

			
			public uint light_count;

			
			public VMDFormat.LightData[] light;
		}

		
		public class LightData : Format
		{
			
			public LightData(BinaryReader bin)
			{
				this.flame_no = bin.ReadUInt32();
				this.rgb = base.ReadSinglesToColor(bin, 1f);
				this.location = base.ReadSinglesToVector3(bin);
				this.count = (int)this.flame_no;
			}

			
			public uint flame_no;

			
			public Color rgb;

			
			public Vector3 location;
		}

		
		public class SelfShadowList : Format
		{
			
			public SelfShadowList(BinaryReader bin)
			{
				this.self_shadow_count = bin.ReadUInt32();
				this.self_shadow = new VMDFormat.SelfShadowData[this.self_shadow_count];
				int num = 0;
				while ((long)num < (long)((ulong)this.self_shadow_count))
				{
					this.self_shadow[num] = new VMDFormat.SelfShadowData(bin);
					num++;
				}
				Array.Sort<VMDFormat.SelfShadowData>(this.self_shadow);
			}

			
			public uint self_shadow_count;

			
			public VMDFormat.SelfShadowData[] self_shadow;
		}

		
		public class SelfShadowData : Format
		{
			
			public SelfShadowData(BinaryReader bin)
			{
				this.flame_no = bin.ReadUInt32();
				this.mode = bin.ReadByte();
				this.distance = bin.ReadSingle();
				this.count = (int)this.flame_no;
			}

			
			public uint flame_no;

			
			public byte mode;

			
			public float distance;
		}
	}
}
