using System;
using UnityEngine;

namespace KKVMDPlayPlugin
{
	
	public class BoneAdjustment
	{

		public float RotX
		{
			get
			{
				return this.rotAdjustmentVec.x;
			}
			set
			{
				Vector3 vector = this.rotAdjustmentVec;
				vector.x = value;
				this.SetRotAdjustment(vector);
			}
		}


		public float RotY
		{
			get
			{
				return this.rotAdjustmentVec.y;
			}
			set
			{
				Vector3 vector = this.rotAdjustmentVec;
				vector.y = value;
				this.SetRotAdjustment(vector);
			}
		}

		
		
		
		public float RotZ
		{
			get
			{
				return this.rotAdjustmentVec.z;
			}
			set
			{
				Vector3 vector = this.rotAdjustmentVec;
				vector.z = value;
				this.SetRotAdjustment(vector);
			}
		}

		
		public static BoneAdjustment Init(string boneName, string axisSpec, Vector3 rotAdjustment, bool rotAxisAdjustment)
		{
			BoneAdjustment boneAdjustment = new BoneAdjustment();
			boneAdjustment.boneName = boneName;
			boneAdjustment.SetSpec(axisSpec);
			boneAdjustment.SetRotAdjustment(rotAdjustment);
			boneAdjustment.rotAxisAdjustment = rotAxisAdjustment;
			return boneAdjustment;
		}

		
		public void SetRotAdjustment(Vector3 rot)
		{
			this.rotAdjustmentVec = rot;
			this.rotAdjustment = Quaternion.Euler(rot);
		}

		
		public void SetAxisAdjustment(Vector3 rotV)
		{
			this.rotAxisAdjustmentVec = rotV;
			Quaternion quaternion = Quaternion.Euler(rotV);
			this.axisX = quaternion * Vector3.right;
			this.axisY = quaternion * Vector3.up;
			this.axisZ = quaternion * Vector3.forward;
			if (rotV != Vector3.zero)
			{
				this.rotAxisAdjustment = true;
				return;
			}
			this.rotAxisAdjustment = false;
		}

		
		public void SetSpec(string spec)
		{
			string[] array = spec.Split(new char[]
			{
				','
			});
			float[] array2 = new float[]
			{
				1f,
				1f,
				1f
			};
			for (int i = 0; i < array2.Length; i++)
			{
				array[i] = array[i].Trim();
				if (array[i].StartsWith("-"))
				{
					array[i] = array[i].Substring(1).Trim();
					array2[i] = -1f;
				}
				else
				{
					array[i] = array[i].Substring(0).Trim();
					array2[i] = 1f;
				}
			}
			this.spec = spec;
			this.xyz = array;
			this.sign = array2;
		}

		
		public Vector3 adjustAxis(Vector3 v)
		{
			string a = this.xyz[0];
			float num;
			if (!(a == "x"))
			{
				if (!(a == "y"))
				{
					if (!(a == "z"))
					{
						throw new Exception("Unexpected x: " + this.xyz[0]);
					}
					num = v.z * this.sign[0];
				}
				else
				{
					num = v.y * this.sign[0];
				}
			}
			else
			{
				num = v.x * this.sign[0];
			}
			a = this.xyz[1];
			float num2;
			if (!(a == "x"))
			{
				if (!(a == "y"))
				{
					if (!(a == "z"))
					{
						throw new Exception("Unexpected y: " + this.xyz[1]);
					}
					num2 = v.z * this.sign[1];
				}
				else
				{
					num2 = v.y * this.sign[1];
				}
			}
			else
			{
				num2 = v.x * this.sign[1];
			}
			a = this.xyz[2];
			float num3;
			if (!(a == "x"))
			{
				if (!(a == "y"))
				{
					if (!(a == "z"))
					{
						throw new Exception("Unexpected z: " + this.xyz[2]);
					}
					num3 = v.z * this.sign[2];
				}
				else
				{
					num3 = v.y * this.sign[2];
				}
			}
			else
			{
				num3 = v.x * this.sign[2];
			}
			return new Vector3(num, num2, num3);
		}

		
		public Quaternion GetAdjustedRotation(Quaternion baseRot)
		{
			Vector3 vector = baseRot.eulerAngles;
			vector = this.adjustAxis(vector);
			Quaternion quaternion;
			if (this.rotAxisAdjustment)
			{
				quaternion = Quaternion.AngleAxis(vector.y, this.axisY) * Quaternion.AngleAxis(vector.x, this.axisX) * Quaternion.AngleAxis(vector.z, this.axisZ) * this.rotAdjustment;
			}
			else
			{
				quaternion = Quaternion.Euler(vector) * this.rotAdjustment;
			}
			if (this.rotationScale != 1f)
			{
				quaternion = Quaternion.Slerp(Quaternion.identity, quaternion, this.rotationScale);
			}
			return quaternion;
		}

		
		public string GetAdjustedRotationV(float x, float y, float z)
		{
			Vector3 eulerAngles = this.GetAdjustedRotation(Quaternion.Euler(x, y, z)).eulerAngles;
			return string.Format("({0}, {1}, {2})", eulerAngles.x, eulerAngles.y, eulerAngles.z);
		}

		
		public override string ToString()
		{
			return string.Format("BoneAdjustment [spec: {0}, xyz: {1}, sign: {2}]", this.spec, this.xyz, this.sign);
		}

		
		public string boneName;

		
		public string spec;

		
		private string[] xyz;

		
		private float[] sign;

		
		public Quaternion rotAdjustment = Quaternion.identity;

		
		private Vector3 rotAdjustmentVec = Vector3.zero;

		
		public bool rotAxisAdjustment;

		
		private Vector3 rotAxisAdjustmentVec = Vector3.zero;

		
		public Vector3 axisX = Vector3.right;

		
		public Vector3 axisY = Vector3.up;

		
		public Vector3 axisZ = Vector3.forward;

		
		public float rotationScale = 1f;
	}
}
