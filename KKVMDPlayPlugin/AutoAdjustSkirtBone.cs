using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class AutoAdjustSkirtBone
{
	
	public void DoSetup(GameObject charaGo)
	{
		this.waist01go = charaGo.transform.Find("BodyTop/p_cf_body_bone/cf_j_root/cf_n_height/cf_j_hips/cf_j_waist01").gameObject;
		this.waist02go = this.waist01go.transform.Find("cf_j_waist02").gameObject;
		this.thigh00go_L = this.waist02go.transform.Find("cf_j_thigh00_L").gameObject;
		this.thigh00go_R = this.waist02go.transform.Find("cf_j_thigh00_R").gameObject;
		this.leg01go_L = this.thigh00go_L.transform.Find("cf_j_leg01_L").gameObject;
		this.leg01go_R = this.thigh00go_R.transform.Find("cf_j_leg01_R").gameObject;
		this.skirtRoot = this.waist01go.transform.Find("cf_d_sk_top").gameObject;
		this.skirtBones = new Dictionary<int, List<GameObject>>();
		for (int i = 0; i < 8; i++)
		{
			List<GameObject> list = new List<GameObject>();
			this.skirtBones[i] = list;
			GameObject gameObject = this.skirtRoot.transform.Find("cf_d_sk_0" + i + "_00").gameObject;
			for (int j = 0; j < 6; j++)
			{
				GameObject gameObject2 = gameObject.transform.Find(string.Concat(new object[]
				{
					"cf_j_sk_0",
					i,
					"_0",
					j
				})).gameObject;
				list.Add(gameObject2);
				gameObject = gameObject2;
			}
		}
		if (this.useColliderCheck)
		{
			this.boneColliders.AddRange(this.waist02go.GetComponentsInChildren<DynamicBoneCollider>());
		}
	}

	
	public void UpdateSkirtBones()
	{
		int num = this.AdjustSkirtLR(this.waist01go, this.thigh00go_R, this.leg01go_R, AutoAdjustSkirtBone.skirtIndices_R);
		int num2 = this.AdjustSkirtLR(this.waist01go, this.thigh00go_L, this.leg01go_L, AutoAdjustSkirtBone.skirtIndices_L);
		if (num == 1 && num2 == 7)
		{
			this.AdjustSkirtCenters(7, 1, 0);
		}
		if (num == 3 && num2 == 5)
		{
			this.AdjustSkirtCenters(5, 3, 4);
		}
		bool flag = this.useColliderCheck;
	}

	
	private void AdjustSkirtCenters(int left, int right, int center)
	{
		Vector3 position = this.skirtBones[left][1].transform.position;
		Vector3 position2 = this.skirtBones[right][1].transform.position;
		Vector3 vector = (position + position2) / 2f;
		this.skirtBones[center][0].transform.localRotation = Quaternion.identity;
		Vector3 vector2 = this.skirtBones[center][0].transform.InverseTransformPoint(vector);
		Quaternion localRotation = Quaternion.FromToRotation(this.skirtBones[center][0].transform.InverseTransformPoint(this.skirtBones[center][1].transform.position), vector2);
		this.skirtBones[center][0].transform.localRotation = localRotation;
	}

	
	private int AdjustSkirtLR(GameObject waist, GameObject thigh, GameObject leg, int[] skirtRootIndices)
	{
		Vector3 position = thigh.transform.position;
		Vector3 position2 = waist.transform.position;
		Vector3 p2 = leg.transform.position;
		Vector3 vector = position2 - position;
		Vector3 vector2 = p2 - position;
		vector = vector.normalized;
		vector2 = vector2.normalized;
		float num = Mathf.Acos(Vector3.Dot(vector, vector2));
		Vector3 normalized = Vector3.Cross(vector, vector2).normalized;
		if (float.IsNaN(num) || num == 0f || (0f <= num && num < 2.443461f))
		{
			Dictionary<int, Vector3> dictionary = new Dictionary<int, Vector3>();
			for (int i = 0; i < skirtRootIndices.Length; i++)
			{
				dictionary.Add(skirtRootIndices[i], this.skirtBones[skirtRootIndices[i]][0].transform.position);
			}
			List<KeyValuePair<int, Vector3>> list = (from e in dictionary
			orderby (e.Value - p2).magnitude
			select e).ToList<KeyValuePair<int, Vector3>>();
			int key = list[0].Key;
			int key2 = list[1].Key;
			Transform transform = this.skirtBones[key][0].transform;
			Transform transform2 = this.skirtBones[key2][0].transform;
			Vector3 vector3 = this.skirtBones[key][0].transform.InverseTransformPoint(p2);
			Vector3 vector4 = this.skirtBones[key][0].transform.InverseTransformPoint(transform2.position);
			float num2 = Vector3.Angle(vector3, vector4);
			Vector3 targetPoint = p2;
			int num3 = 1;
			if (key == 1 || key == 7)
			{
				targetPoint = leg.transform.TransformPoint(new Vector3(0.01f, 0f, 0.05f));
				for (int j = 0; j < num3; j++)
				{
					this.AdjustRotation(this.skirtBones[key], targetPoint, 1f, j);
					if (num2 < 90f)
					{
						this.AdjustRotation(this.skirtBones[key2], targetPoint, 0.8f, j);
					}
				}
			}
			else if (key == 3 || key == 5)
			{
				targetPoint = leg.transform.TransformPoint(new Vector3(0.01f, 0f, 0.05f));
				for (int k = 0; k < num3; k++)
				{
					this.AdjustRotation(this.skirtBones[key], targetPoint, 1f, k);
					if (num2 < 90f)
					{
						this.AdjustRotation(this.skirtBones[key2], targetPoint, 0.8f, k);
					}
				}
			}
			else
			{
				for (int l = 0; l < num3; l++)
				{
					this.AdjustRotation(this.skirtBones[key], targetPoint, 1f, l);
					this.AdjustRotation(this.skirtBones[key2], targetPoint, 0.7f, l);
				}
			}
			return key;
		}
		return -1;
	}

	
	private void AdjustRotation(List<GameObject> bones, Vector3 targetPoint, float weight, int index = 0)
	{
		Quaternion localRotation = bones[index].transform.localRotation;
		bones[index].transform.localRotation = Quaternion.identity;
		Vector3 vector = bones[index].transform.InverseTransformPoint(bones[index + 1].transform.position);
		Vector3 vector2 = bones[index].transform.InverseTransformPoint(targetPoint);
		Vector3.Angle(vector, vector2);
		Quaternion quaternion = Quaternion.FromToRotation(vector, vector2);
		bones[index].transform.localRotation = Quaternion.LerpUnclamped(localRotation, quaternion, weight);
	}

	
	private Dictionary<int, List<GameObject>> skirtBones = new Dictionary<int, List<GameObject>>();

	
	private GameObject waist01go;

	
	private GameObject waist02go;

	
	private GameObject thigh00go_L;

	
	private GameObject thigh00go_R;

	
	private GameObject leg01go_L;

	
	private GameObject leg01go_R;

	
	private GameObject skirtRoot;

	
	private List<DynamicBoneCollider> boneColliders = new List<DynamicBoneCollider>();

	
	public bool useColliderCheck;

	
	private static int[] skirtIndices_R = new int[]
	{
		0,
		1,
		2,
		3,
		4
	};

	
	private static int[] skirtIndices_L = new int[]
	{
		0,
		7,
		6,
		5,
		4
	};
}
