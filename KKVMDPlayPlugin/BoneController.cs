using System;
using System.Linq;
using UnityEngine;


public class BoneController : MonoBehaviour
{
	
	private void Start()
	{
		if (this.ik_solver != null)
		{
			this.ik_solver = base.transform.GetComponent<CCDIKSolver>();
			if (this.ik_solver_targets.Length == 0)
			{
				this.ik_solver_targets = (from x in Enumerable.Repeat<Transform>(this.ik_solver.target, 1).Concat(this.ik_solver.chains)
				select x.GetComponent<BoneController>()).ToArray<BoneController>();
			}
		}
		this.UpdatePrevTransform();
	}

	
	public void Process()
	{
		if (null != this.additive_parent)
		{
			BoneController.LiteTransform deltaTransform = this.additive_parent.GetDeltaTransform(this.add_local);
			if (this.add_move)
			{
				base.transform.localPosition += deltaTransform.position * this.additive_rate;
			}
			if (this.add_rotate)
			{
				Quaternion quaternion;
				if (0f <= this.additive_rate)
				{
					quaternion = Quaternion.Slerp(Quaternion.identity, deltaTransform.rotation, this.additive_rate);
				}
				else
				{
					Quaternion quaternion2 = Quaternion.Inverse(deltaTransform.rotation);
					quaternion = Quaternion.Slerp(Quaternion.identity, quaternion2, -this.additive_rate);
				}
				base.transform.localRotation *= quaternion;
			}
		}
	}

	
	public BoneController.LiteTransform GetDeltaTransform(bool is_add_local)
	{
		BoneController.LiteTransform result;
		if (is_add_local)
		{
			result = new BoneController.LiteTransform(base.transform.position - this.prev_global_.position, Quaternion.Inverse(this.prev_global_.rotation) * base.transform.rotation);
		}
		else
		{
			result = new BoneController.LiteTransform(base.transform.localPosition - this.prev_local_.position, Quaternion.Inverse(this.prev_local_.rotation) * base.transform.localRotation);
		}
		return result;
	}

	
	public void UpdatePrevTransform()
	{
		this.prev_global_ = new BoneController.LiteTransform(base.transform.position, base.transform.rotation);
		this.prev_local_ = new BoneController.LiteTransform(base.transform.localPosition, base.transform.localRotation);
	}

	
	public BoneController additive_parent;

	
	public float additive_rate;

	
	public CCDIKSolver ik_solver;

	
	public BoneController[] ik_solver_targets;

	
	public bool add_local;

	
	public bool add_move;

	
	public bool add_rotate;

	
	private BoneController.LiteTransform prev_global_;

	
	private BoneController.LiteTransform prev_local_;

	
	[Serializable]
	public class LiteTransform
	{
		
		public LiteTransform(Vector3 p, Quaternion r)
		{
			this.position = p;
			this.rotation = r;
		}

		
		public Vector3 position;

		
		public Quaternion rotation;
	}
}
