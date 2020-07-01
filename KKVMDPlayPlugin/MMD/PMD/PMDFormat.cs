using System;
using System.IO;
using UnityEngine;

namespace MMD.PMD
{
	
	public class PMDFormat : Format
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

		
		public PMDFormat(BinaryReader bin, GameObject caller, string path)
		{
			this.EntryPathes(path);
			this.caller = caller;
			try
			{
				this.head = new PMDFormat.Header(bin);
				this.vertex_list = new PMDFormat.VertexList(bin);
				this.face_vertex_list = new PMDFormat.FaceVertexList(bin);
				this.material_list = new PMDFormat.MaterialList(bin);
				this.bone_list = new PMDFormat.BoneList(bin);
				this.ik_list = new PMDFormat.IKList(bin);
				this.read_count++;
				this.skin_list = new PMDFormat.SkinList(bin);
				this.read_count++;
				this.skin_name_list = new PMDFormat.SkinNameList(bin);
				this.bone_name_list = new PMDFormat.BoneNameList(bin);
				this.bone_display_list = new PMDFormat.BoneDisplayList(bin);
				this.eg_head = new PMDFormat.EnglishHeader(bin);
				this.eg_bone_name_list = new PMDFormat.EnglishBoneNameList(bin, (int)this.bone_list.bone_count);
				this.eg_skin_name_list = new PMDFormat.EnglishSkinNameList(bin, (int)this.skin_list.skin_count);
				this.eg_bone_display_list = new PMDFormat.EnglishBoneDisplayList(bin, (int)this.bone_name_list.bone_disp_name_count);
				this.toon_texture_list = new PMDFormat.ToonTextureList(bin);
				this.rigidbody_list = new PMDFormat.RigidbodyList(bin);
				this.rigidbody_joint_list = new PMDFormat.RigidbodyJointList(bin);
			}
			catch
			{
			}
		}

		
		public string path;

		
		public string name;

		
		public string folder;

		
		public GameObject caller;

		
		public PMDFormat.Header head;

		
		public PMDFormat.VertexList vertex_list;

		
		public PMDFormat.FaceVertexList face_vertex_list;

		
		public PMDFormat.MaterialList material_list;

		
		public PMDFormat.BoneList bone_list;

		
		public PMDFormat.IKList ik_list;

		
		public PMDFormat.SkinList skin_list;

		
		public PMDFormat.SkinNameList skin_name_list;

		
		public PMDFormat.BoneNameList bone_name_list;

		
		public PMDFormat.BoneDisplayList bone_display_list;

		
		public PMDFormat.EnglishHeader eg_head;

		
		public PMDFormat.EnglishBoneNameList eg_bone_name_list;

		
		public PMDFormat.EnglishSkinNameList eg_skin_name_list;

		
		public PMDFormat.EnglishBoneDisplayList eg_bone_display_list;

		
		public PMDFormat.ToonTextureList toon_texture_list;

		
		public PMDFormat.RigidbodyList rigidbody_list;

		
		public PMDFormat.RigidbodyJointList rigidbody_joint_list;

		
		private int read_count;

		
		public class Header : Format
		{
			
			public Header(BinaryReader bin)
			{
				this.magic = bin.ReadBytes(3);
				this.version = bin.ReadSingle();
				this.model_name = base.ConvertByteToString(bin.ReadBytes(20));
				this.comment = base.ConvertByteToString(bin.ReadBytes(256));
			}

			
			public byte[] magic;

			
			public float version;

			
			public string model_name;

			
			public string comment;
		}

		
		public class VertexList : Format
		{
			
			public VertexList(BinaryReader bin)
			{
				this.vert_count = bin.ReadUInt32();
				this.vertex = new PMDFormat.Vertex[this.vert_count];
				int num = 0;
				while ((long)num < (long)((ulong)this.vert_count))
				{
					this.vertex[num] = new PMDFormat.Vertex(bin);
					num++;
				}
			}

			
			public uint vert_count;

			
			public PMDFormat.Vertex[] vertex;
		}

		
		public class Vertex : Format
		{
			
			public Vertex(BinaryReader bin)
			{
				this.pos = base.ReadSinglesToVector3(bin);
				this.normal_vec = base.ReadSinglesToVector3(bin);
				this.uv = base.ReadSinglesToVector2(bin);
				this.bone_num = base.ReadUInt16s(bin, 2u);
				this.bone_weight = bin.ReadByte();
				this.edge_flag = bin.ReadByte();
			}

			
			public Vector3 pos;

			
			public Vector3 normal_vec;

			
			public Vector2 uv;

			
			public ushort[] bone_num;

			
			public byte bone_weight;

			
			public byte edge_flag;
		}

		
		public class FaceVertexList : Format
		{
			
			public FaceVertexList(BinaryReader bin)
			{
				this.face_vert_count = bin.ReadUInt32();
				this.face_vert_index = base.ReadUInt16s(bin, this.face_vert_count);
			}

			
			public uint face_vert_count;

			
			public ushort[] face_vert_index;
		}

		
		public class MaterialList : Format
		{
			
			public MaterialList(BinaryReader bin)
			{
				this.material_count = bin.ReadUInt32();
				this.material = new PMDFormat.Material[this.material_count];
				int num = 0;
				while ((long)num < (long)((ulong)this.material_count))
				{
					this.material[num] = new PMDFormat.Material(bin);
					num++;
				}
			}

			
			public uint material_count;

			
			public PMDFormat.Material[] material;
		}

		
		public class Material : Format
		{
			
			private string CutTheUnknownDotSlash(string str)
			{
				string text = "";
				string[] array = str.Split(new char[]
				{
					'/'
				});
				if (array[0] == ".")
				{
					text += array[1];
					for (int i = 2; i < array.Length; i++)
					{
						text = text + "/" + array[i];
					}
				}
				else
				{
					text = str;
				}
				return text;
			}

			
			public Material(BinaryReader bin)
			{
				this.diffuse_color = base.ReadSinglesToColor(bin, 1f);
				this.alpha = bin.ReadSingle();
				this.specularity = bin.ReadSingle();
				this.specular_color = base.ReadSinglesToColor(bin, 1f);
				this.mirror_color = base.ReadSinglesToColor(bin, 1f);
				this.toon_index = bin.ReadByte();
				this.edge_flag = bin.ReadByte();
				this.face_vert_count = bin.ReadUInt32();
				string text = base.ConvertByteToString(bin.ReadBytes(20));
				if (!string.IsNullOrEmpty(text.Trim()))
				{
					foreach (string text2 in text.Trim().Split(new char[]
					{
						'*'
					}))
					{
						string str = "";
						string extension = Path.GetExtension(text2);
						if (extension == ".sph" || extension == ".spa")
						{
							this.sphere_map_name = text2;
						}
						else
						{
							if (text2.Split(new char[]
							{
								'/'
							})[0] == ".")
							{
								string[] array2 = text2.Split(new char[]
								{
									'/'
								});
								for (int j = 1; j < array2.Length - 1; j++)
								{
									str = str + array2[j] + "/";
								}
								str += array2[array2.Length - 1];
							}
							else
							{
								str = text2;
							}
							this.texture_file_name = str;
						}
					}
				}
				else
				{
					this.sphere_map_name = "";
					this.texture_file_name = "";
				}
				if (string.IsNullOrEmpty(this.texture_file_name))
				{
					this.texture_file_name = "";
				}
			}

			
			public Color diffuse_color;

			
			public float alpha;

			
			public float specularity;

			
			public Color specular_color;

			
			public Color mirror_color;

			
			public byte toon_index;

			
			public byte edge_flag;

			
			public uint face_vert_count;

			
			public string texture_file_name;

			
			public string sphere_map_name;
		}

		
		public class BoneList : Format
		{
			
			public BoneList(BinaryReader bin)
			{
				this.bone_count = bin.ReadUInt16();
				this.bone = new PMDFormat.Bone[(int)this.bone_count];
				for (int i = 0; i < (int)this.bone_count; i++)
				{
					this.bone[i] = new PMDFormat.Bone(bin);
				}
			}

			
			public ushort bone_count;

			
			public PMDFormat.Bone[] bone;
		}

		
		public class Bone : Format
		{
			
			public Bone(BinaryReader bin)
			{
				this.bone_name = base.ConvertByteToString(bin.ReadBytes(20));
				this.parent_bone_index = bin.ReadUInt16();
				this.tail_pos_bone_index = bin.ReadUInt16();
				this.bone_type = bin.ReadByte();
				this.ik_parent_bone_index = bin.ReadUInt16();
				this.bone_head_pos = base.ReadSinglesToVector3(bin);
			}

			
			public string bone_name;

			
			public ushort parent_bone_index;

			
			public ushort tail_pos_bone_index;

			
			public byte bone_type;

			
			public ushort ik_parent_bone_index;

			
			public Vector3 bone_head_pos;
		}

		
		public class IKList : Format
		{
			
			public IKList(BinaryReader bin)
			{
				this.ik_data_count = bin.ReadUInt16();
				this.ik_data = new PMDFormat.IK[(int)this.ik_data_count];
				for (int i = 0; i < (int)this.ik_data_count; i++)
				{
					this.ik_data[i] = new PMDFormat.IK(bin);
				}
			}

			
			public ushort ik_data_count;

			
			public PMDFormat.IK[] ik_data;
		}

		
		public class IK : Format
		{
			
			public IK(BinaryReader bin)
			{
				this.ik_bone_index = bin.ReadUInt16();
				this.ik_target_bone_index = bin.ReadUInt16();
				this.ik_chain_length = bin.ReadByte();
				this.iterations = bin.ReadUInt16();
				this.control_weight = bin.ReadSingle();
				this.ik_child_bone_index = base.ReadUInt16s(bin, (uint)this.ik_chain_length);
			}

			
			public ushort ik_bone_index;

			
			public ushort ik_target_bone_index;

			
			public byte ik_chain_length;

			
			public ushort iterations;

			
			public float control_weight;

			
			public ushort[] ik_child_bone_index;
		}

		
		public class SkinList : Format
		{
			
			public SkinList(BinaryReader bin)
			{
				this.skin_count = bin.ReadUInt16();
				this.skin_data = new PMDFormat.SkinData[(int)this.skin_count];
				for (int i = 0; i < (int)this.skin_count; i++)
				{
					this.skin_data[i] = new PMDFormat.SkinData(bin);
				}
			}

			
			public ushort skin_count;

			
			public PMDFormat.SkinData[] skin_data;
		}

		
		public class SkinData : Format
		{
			
			public SkinData(BinaryReader bin)
			{
				this.skin_name = base.ConvertByteToString(bin.ReadBytes(20));
				this.skin_vert_count = bin.ReadUInt32();
				this.skin_type = bin.ReadByte();
				this.skin_vert_data = new PMDFormat.SkinVertexData[this.skin_vert_count];
				int num = 0;
				while ((long)num < (long)((ulong)this.skin_vert_count))
				{
					this.skin_vert_data[num] = new PMDFormat.SkinVertexData(bin);
					num++;
				}
			}

			
			public string skin_name;

			
			public uint skin_vert_count;

			
			public byte skin_type;

			
			public PMDFormat.SkinVertexData[] skin_vert_data;
		}

		
		public class SkinVertexData : Format
		{
			
			public SkinVertexData(BinaryReader bin)
			{
				this.skin_vert_index = bin.ReadUInt32();
				this.skin_vert_pos = base.ReadSinglesToVector3(bin);
			}

			
			public uint skin_vert_index;

			
			public Vector3 skin_vert_pos;
		}

		
		public class SkinNameList : Format
		{
			
			public SkinNameList(BinaryReader bin)
			{
				this.skin_disp_count = bin.ReadByte();
				this.skin_index = base.ReadUInt16s(bin, (uint)this.skin_disp_count);
			}

			
			public byte skin_disp_count;

			
			public ushort[] skin_index;
		}

		
		public class BoneNameList : Format
		{
			
			public BoneNameList(BinaryReader bin)
			{
				this.bone_disp_name_count = bin.ReadByte();
				this.disp_name = new string[(int)this.bone_disp_name_count];
				for (int i = 0; i < (int)this.bone_disp_name_count; i++)
				{
					this.disp_name[i] = base.ConvertByteToString(bin.ReadBytes(50));
				}
			}

			
			public byte bone_disp_name_count;

			
			public string[] disp_name;
		}

		
		public class BoneDisplayList : Format
		{
			
			public BoneDisplayList(BinaryReader bin)
			{
				this.bone_disp_count = bin.ReadUInt32();
				this.bone_disp = new PMDFormat.BoneDisplay[this.bone_disp_count];
				int num = 0;
				while ((long)num < (long)((ulong)this.bone_disp_count))
				{
					this.bone_disp[num] = new PMDFormat.BoneDisplay(bin);
					num++;
				}
			}

			
			public uint bone_disp_count;

			
			public PMDFormat.BoneDisplay[] bone_disp;
		}

		
		public class BoneDisplay : Format
		{
			
			public BoneDisplay(BinaryReader bin)
			{
				this.bone_index = bin.ReadUInt16();
				this.bone_disp_frame_index = bin.ReadByte();
			}

			
			public ushort bone_index;

			
			public byte bone_disp_frame_index;
		}

		
		public class EnglishHeader : Format
		{
			
			public EnglishHeader(BinaryReader bin)
			{
				this.english_name_compatibility = bin.ReadByte();
				this.model_name_eg = base.ConvertByteToString(bin.ReadBytes(20));
				this.comment_eg = base.ConvertByteToString(bin.ReadBytes(256));
			}

			
			public byte english_name_compatibility;

			
			public string model_name_eg;

			
			public string comment_eg;
		}

		
		public class EnglishBoneNameList : Format
		{
			
			public EnglishBoneNameList(BinaryReader bin, int boneCount)
			{
				this.bone_name_eg = new string[boneCount];
				for (int i = 0; i < boneCount; i++)
				{
					this.bone_name_eg[i] = base.ConvertByteToString(bin.ReadBytes(20));
				}
			}

			
			public string[] bone_name_eg;
		}

		
		public class EnglishSkinNameList : Format
		{
			
			public EnglishSkinNameList(BinaryReader bin, int skinCount)
			{
				this.skin_name_eg = new string[skinCount];
				for (int i = 0; i < skinCount - 1; i++)
				{
					this.skin_name_eg[i] = base.ConvertByteToString(bin.ReadBytes(20));
				}
			}

			
			public string[] skin_name_eg;
		}

		
		public class EnglishBoneDisplayList : Format
		{
			
			public EnglishBoneDisplayList(BinaryReader bin, int boneDispNameCount)
			{
				this.disp_name_eg = new string[boneDispNameCount];
				for (int i = 0; i < boneDispNameCount; i++)
				{
					this.disp_name_eg[i] = base.ConvertByteToString(bin.ReadBytes(50));
				}
			}

			
			public string[] disp_name_eg;
		}

		
		public class ToonTextureList : Format
		{
			
			public ToonTextureList(BinaryReader bin)
			{
				this.toon_texture_file = new string[10];
				for (int i = 0; i < this.toon_texture_file.Length; i++)
				{
					this.toon_texture_file[i] = base.ConvertByteToString(bin.ReadBytes(100));
				}
			}

			
			public string[] toon_texture_file;
		}

		
		public class RigidbodyList : Format
		{
			
			public RigidbodyList(BinaryReader bin)
			{
				this.rigidbody_count = bin.ReadUInt32();
				this.rigidbody = new PMDFormat.Rigidbody[this.rigidbody_count];
				int num = 0;
				while ((long)num < (long)((ulong)this.rigidbody_count))
				{
					this.rigidbody[num] = new PMDFormat.Rigidbody(bin);
					num++;
				}
			}

			
			public uint rigidbody_count;

			
			public PMDFormat.Rigidbody[] rigidbody;
		}

		
		public class Rigidbody : Format
		{
			
			public Rigidbody(BinaryReader bin)
			{
				this.rigidbody_name = base.ConvertByteToString(bin.ReadBytes(20));
				this.rigidbody_rel_bone_index = (int)bin.ReadUInt16();
				this.rigidbody_group_index = bin.ReadByte();
				this.rigidbody_group_target = bin.ReadUInt16();
				this.shape_type = bin.ReadByte();
				this.shape_w = bin.ReadSingle();
				this.shape_h = bin.ReadSingle();
				this.shape_d = bin.ReadSingle();
				this.pos_pos = base.ReadSinglesToVector3(bin);
				this.pos_rot = base.ReadSinglesToVector3(bin);
				this.rigidbody_weight = bin.ReadSingle();
				this.rigidbody_pos_dim = bin.ReadSingle();
				this.rigidbody_rot_dim = bin.ReadSingle();
				this.rigidbody_recoil = bin.ReadSingle();
				this.rigidbody_friction = bin.ReadSingle();
				this.rigidbody_type = bin.ReadByte();
			}

			
			public string rigidbody_name;

			
			public int rigidbody_rel_bone_index;

			
			public byte rigidbody_group_index;

			
			public ushort rigidbody_group_target;

			
			public byte shape_type;

			
			public float shape_w;

			
			public float shape_h;

			
			public float shape_d;

			
			public Vector3 pos_pos;

			
			public Vector3 pos_rot;

			
			public float rigidbody_weight;

			
			public float rigidbody_pos_dim;

			
			public float rigidbody_rot_dim;

			
			public float rigidbody_recoil;

			
			public float rigidbody_friction;

			
			public byte rigidbody_type;
		}

		
		public class RigidbodyJointList : Format
		{
			
			public RigidbodyJointList(BinaryReader bin)
			{
				this.joint_count = bin.ReadUInt32();
				this.joint = new PMDFormat.Joint[this.joint_count];
				int num = 0;
				while ((long)num < (long)((ulong)this.joint_count))
				{
					this.joint[num] = new PMDFormat.Joint(bin);
					num++;
				}
			}

			
			public uint joint_count;

			
			public PMDFormat.Joint[] joint;
		}

		
		public class Joint : Format
		{
			
			public Joint(BinaryReader bin)
			{
				this.joint_name = base.ConvertByteToString(bin.ReadBytes(20));
				this.joint_rigidbody_a = bin.ReadUInt32();
				this.joint_rigidbody_b = bin.ReadUInt32();
				this.joint_pos = base.ReadSinglesToVector3(bin);
				this.joint_rot = base.ReadSinglesToVector3(bin);
				this.constrain_pos_1 = base.ReadSinglesToVector3(bin);
				this.constrain_pos_2 = base.ReadSinglesToVector3(bin);
				this.constrain_rot_1 = base.ReadSinglesToVector3(bin);
				this.constrain_rot_2 = base.ReadSinglesToVector3(bin);
				this.spring_pos = base.ReadSinglesToVector3(bin);
				this.spring_rot = base.ReadSinglesToVector3(bin);
			}

			
			public string joint_name;

			
			public uint joint_rigidbody_a;

			
			public uint joint_rigidbody_b;

			
			public Vector3 joint_pos;

			
			public Vector3 joint_rot;

			
			public Vector3 constrain_pos_1;

			
			public Vector3 constrain_pos_2;

			
			public Vector3 constrain_rot_1;

			
			public Vector3 constrain_rot_2;

			
			public Vector3 spring_pos;

			
			public Vector3 spring_rot;
		}
	}
}
