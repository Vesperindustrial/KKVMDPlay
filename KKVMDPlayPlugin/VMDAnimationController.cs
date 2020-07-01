using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using IllusionUtility.GetUtility;
using MMD.VMD;
using RootMotion.FinalIK;
using UnityEngine;

namespace KKVMDPlayPlugin
{
	
	public class VMDAnimationController : MonoBehaviour
	{
		
		public static VMDAnimationController Install(ChaControl chara)
		{
			VMDAnimationController vmdanimationController = chara.gameObject.GetComponent<VMDAnimationController>();
			if (vmdanimationController == null)
			{
				if (chara.objBody == null || !chara.loadEnd)
				{
					return null;
				}
				vmdanimationController = chara.gameObject.AddComponent<VMDAnimationController>();
				vmdanimationController.Init(chara);
				VMDAnimationMgr.Instance.controllers.Add(vmdanimationController);
				chara.objTop.AddComponent<VMDAnimationController.DestroyListener>().controller = vmdanimationController;
			}
			return vmdanimationController;
		}

		
		private void Init(ChaControl chara)
		{
			this.chara = chara;
			this._shoulderKey = new string[]
			{
				"cf_j_shoulder_L",
				"cf_j_shoulder_R"
			};
			this._armUpKey = new string[]
			{
				"cf_j_arm00_L",
				"cf_j_arm00_R"
			};
			this._armLowKey = new string[]
			{
				"cf_j_forearm01_L",
				"cf_j_forearm01_R"
			};
			this.quickAdjust = new VMDAnimationController.QuickAdjust(this);
			this.ResetBoneNameMap();
			this.ResetRotationMap();
			this.quickAdjust.ScaleModel = VMDAnimationController.SearchObjName(this.chara.objBodyBone.transform, "cf_n_height").localScale.y;
			this.CreateClone();
			this.animeOverride = DefaultCharaAnimOverride.Get(chara);
			this.faceController = new GameObject("_face")
			{
				transform = 
				{
					parent = this.vmdAnimationGo.transform,
					localPosition = Vector3.zero,
					localRotation = Quaternion.identity
				}
			}.AddComponent<KKAnimeFaceController>();
			this.faceController.Init(chara);
			this.skirtBoneAdjust = new AutoAdjustSkirtBone();
			this.skirtBoneAdjust.DoSetup(chara.gameObject);
		}

		
		public void AdjustMoveBonePosition()
		{
			this.localPositionBackup = new Dictionary<Transform, Vector3>();
			this.AdjustMoveYPosition(this.t_dummy_root, Vector3.zero);
			this.AdjustMoveYPosition(this.t_dummy_center, this.ModelBaseline.centerBasePos);
			this.AdjustMoveYPosition(this.t_dummy_groove, this.ModelBaseline.grooveBasePos);
			this.AdjustMoveYPosition(this.t_dummy_ik_root_L, this.ModelBaseline.leftIKCenterPos);
			this.AdjustMoveYPosition(this.t_dummy_ik_root_R, this.ModelBaseline.rightIKCenterPos);
			this.AdjustMoveYPosition(this.t_leftFootIK, this.ModelBaseline.leftFootPos);
			this.AdjustMoveYPosition(this.t_rightFootIK, this.ModelBaseline.rightFootPos);
			this.AdjustMoveYPosition(this.t_leftToeIK, this.ModelBaseline.leftToePosRel);
			this.AdjustMoveYPosition(this.t_rightToeIK, this.ModelBaseline.rightToePosRel);
			this.t_dummy_center.localPosition = this.t_dummy_center.localPosition + this.centerPosAdjust;
		}

		
		public void AdjustMoveYPosition(Transform target, Vector3 baseLocalPos)
		{
			this.localPositionBackup[target] = target.localPosition;
			if (!this.bakeScaleAdjustmentIntoAnimation)
			{
				Vector3 vector = (target.localPosition - baseLocalPos) * this.quickAdjust.ScaleModel;
				vector.y *= this.moveYScaleAdjust;
				target.localPosition = vector + baseLocalPos;
			}
		}

		
		public void RestoreAdjustedMoveBonePosition()
		{
			if (this.localPositionBackup != null)
			{
				foreach (KeyValuePair<Transform, Vector3> keyValuePair in this.localPositionBackup)
				{
					keyValuePair.Key.localPosition = keyValuePair.Value;
				}
			}
		}

		
		private void LateUpdate()
		{
		}

		
		private void CreateClone()
		{
			this.vmdAnimationGo = new GameObject("vmdAnimation");
			this.vmdAnimationGo.transform.parent = this.chara.objAnim.transform.parent;
			this.vmdAnimationGo.transform.localPosition = this.chara.objAnim.transform.localPosition;
			this.vmdAnimationGo.transform.localRotation = this.chara.objAnim.transform.localRotation;
			this.ResetBoneRotations();
			this.LoadAndCreate(this.boneNames, this.vmdAnimationGo, this.chara.objBodyBone);
		}

		
		private void LoadAndCreate(List<string> pathList, GameObject dummyGo, GameObject bodyBone)
		{
			Dictionary<GameObject, GameObject> dictionary = new Dictionary<GameObject, GameObject>();
			for (int i = 0; i < pathList.Count<string>(); i++)
			{
				string path = pathList[i];
				GameObject gameObject = VMDAnimationController.CreateBone(dummyGo, path);
				if (gameObject.name == "_dummy_root")
				{
					gameObject.transform.localPosition = Vector3.zero;
				}
				else if (gameObject.name == "_dummy_center")
				{
					gameObject.transform.localPosition = this.centerBasePos * 0.1f * this.centerPosAdjustRate;
				}
				else if (gameObject.name == "_dummy_groove")
				{
					gameObject.transform.localPosition = this.grooveBasePos * 0.1f * this.centerPosAdjustRate;
				}
				else if (gameObject.name == "_dummy_hips")
				{
					Transform transform = VMDAnimationController.SearchObjName(bodyBone.transform, "cf_j_siri_L_01");
					Transform transform2 = VMDAnimationController.SearchObjName(bodyBone.transform, "cf_j_siri_R_01");
					gameObject.transform.position = (transform.transform.position + transform2.transform.position) / 2f;
				}
				GameObject gameObject2 = TransformFindEx.FindLoop(bodyBone.transform, gameObject.name);
				if (gameObject2 != null)
				{
					Transform transform3 = gameObject2.transform;
					gameObject.transform.position = transform3.position;
					gameObject.transform.localRotation = Quaternion.identity;
					gameObject.transform.localScale = transform3.localScale;
					Transform transform4 = new GameObject("MR").transform;
					transform4.transform.parent = gameObject.transform;
					transform4.transform.localPosition = Vector3.zero;
					transform4.transform.rotation = transform3.rotation;
					dictionary[transform4.gameObject] = transform3.gameObject;
				}
			}
			Transform transform5 = dummyGo.transform;
			this.t_n_height_orig = VMDAnimationController.SearchObjName(bodyBone.transform, "cf_n_height");
			this.t_n_height = VMDAnimationController.SearchObjName(transform5, "cf_n_height");
			this.t_dummy_root = VMDAnimationController.SearchObjName(transform5, "_dummy_root");
			this.t_dummy_center = VMDAnimationController.SearchObjName(transform5, "_dummy_center");
			this.t_dummy_groove = VMDAnimationController.SearchObjName(transform5, "_dummy_groove");
			this.t_dummy_hips = VMDAnimationController.SearchObjName(transform5, "_dummy_hips");
			this.t_hips = VMDAnimationController.SearchObjName(transform5, "cf_j_hips");
			this.t_hips_orig = VMDAnimationController.SearchObjName(bodyBone.transform, "cf_j_hips");
			this.t_spine01 = VMDAnimationController.SearchObjName(transform5, "cf_j_spine01");
			this.t_waist01 = VMDAnimationController.SearchObjName(transform5, "cf_j_waist01");
			this.t_waist02 = VMDAnimationController.SearchObjName(transform5, "cf_j_waist02");
			this.t_leftLowerLeg = VMDAnimationController.SearchObjName(transform5, "cf_j_leg01_L");
			this.t_leftUpperLeg = VMDAnimationController.SearchObjName(transform5, "cf_j_thigh00_L");
			this.t_rightLowerLeg = VMDAnimationController.SearchObjName(transform5, "cf_j_leg01_R");
			this.t_rightUpperLeg = VMDAnimationController.SearchObjName(transform5, "cf_j_thigh00_R");
			this.t_leftFoot = VMDAnimationController.SearchObjName(transform5, "cf_j_leg03_L");
			this.t_rightFoot = VMDAnimationController.SearchObjName(transform5, "cf_j_leg03_R");
			this.t_leftHeel = VMDAnimationController.SearchObjName(transform5, "cf_j_foot_L");
			this.t_rightHeel = VMDAnimationController.SearchObjName(transform5, "cf_j_foot_R");
			this.t_leftToe = VMDAnimationController.SearchObjName(transform5, "cf_j_toes_L");
			this.t_rightToe = VMDAnimationController.SearchObjName(transform5, "cf_j_toes_R");
			this.t_dummy_ik_root_L = VMDAnimationController.SearchObjName(transform5, "_dummy_ik_root_L");
			this.t_dummy_ik_root_R = VMDAnimationController.SearchObjName(transform5, "_dummy_ik_root_R");
			this.t_dummy_ik_root_L.position = this.t_leftHeel.position;
			this.t_dummy_ik_root_L.rotation = Quaternion.identity;
			this.t_dummy_ik_root_R.position = this.t_rightHeel.position;
			this.t_dummy_ik_root_R.rotation = Quaternion.identity;
			this.t_leftFootIK = VMDAnimationController.SearchObjName(dummyGo.transform, "_FOOT_IK_L");
			this.t_leftFootIK.transform.position = this.t_leftFoot.position;
			this.t_leftFootIK.transform.rotation = Quaternion.identity;
			this.t_leftToeIK = VMDAnimationController.SearchObjName(dummyGo.transform, "_TOE_IK_L");
			this.t_leftToeIK.position = this.t_leftToe.position;
			this.t_leftToeIK.localRotation = Quaternion.identity;
			this.t_rightFootIK = VMDAnimationController.SearchObjName(dummyGo.transform, "_FOOT_IK_R");
			this.t_rightFootIK.transform.position = this.t_rightFoot.position;
			this.t_rightFootIK.transform.rotation = this.t_rightFoot.rotation;
			this.t_rightToeIK = VMDAnimationController.SearchObjName(dummyGo.transform, "_TOE_IK_R");
			this.t_rightToeIK.position = this.t_rightToe.position;
			this.t_rightToeIK.localRotation = Quaternion.identity;
			this.leftLegIKSolver = this.GetLegIK(this.t_leftFootIK, this.t_leftFoot, this.t_leftLowerLeg, this.t_leftUpperLeg);
			this.rightLegIKSolver = this.GetLegIK(this.t_rightFootIK, this.t_rightFoot, this.t_rightLowerLeg, this.t_rightUpperLeg);
			this.leftToeIKSolver = this.GetToeIK(this.t_leftFoot, this.t_leftHeel, this.t_leftToe, this.t_leftToeIK, this.t_leftLowerLeg, this.t_leftUpperLeg);
			this.rightToeIKSolver = this.GetToeIK(this.t_rightFoot, this.t_rightHeel, this.t_rightToe, this.t_rightToeIK, this.t_rightLowerLeg, this.t_rightUpperLeg);
			this.t_dummy_EYE_LR = VMDAnimationController.SearchObjName(transform5, "_dummy_EYE_LR");
			this.t_dummy_EYE_L = VMDAnimationController.SearchObjName(transform5, "_dummy_EYE_L");
			this.t_dummy_EYE_R = VMDAnimationController.SearchObjName(transform5, "_dummy_EYE_R");
			this.animationForVMD = dummyGo.AddComponent<Animation>();
			this.autoTrack = dummyGo.AddComponent<VMDAnimationController.AutoTrack>();
			this.autoTrack.map = dictionary;
			this.autoTrack.controller = this;
		}

		
		private void Remap()
		{
			List<string> list = this.boneNames;
			GameObject gameObject = this.vmdAnimationGo;
			GameObject objBodyBone = this.chara.objBodyBone;
			Dictionary<GameObject, GameObject> dictionary = new Dictionary<GameObject, GameObject>();
			for (int i = 0; i < list.Count<string>(); i++)
			{
				string text = list[i];
				GameObject gameObject2 = gameObject.transform.Find(text).gameObject;
				Transform transform = objBodyBone.transform.Find(text);
				objBodyBone.transform.Find(text);
				if (transform != null)
				{
					Transform transform2 = gameObject2.transform.Find("MR");
					dictionary[transform2.gameObject] = transform.gameObject;
				}
			}
			this.autoTrack = gameObject.GetComponent<VMDAnimationController.AutoTrack>();
			this.autoTrack.map = dictionary;
		}

		
		private static GameObject CreateBone(GameObject root, string path)
		{
			int num = path.IndexOf("/");
			if (num < 0)
			{
				GameObject gameObject = TransformFindEx.FindLoop(root.transform, path);
				if (gameObject == null)
				{
					gameObject = new GameObject(path);
					Transform transform = gameObject.transform;
					transform.parent = root.transform;
					transform.localPosition = Vector3.zero;
					transform.localRotation = Quaternion.identity;
				}
				return gameObject;
			}
			string text = path.Substring(0, num);
			string text2 = path.Substring(num + 1);
			Transform transform2 = VMDAnimationController.SearchObjName(root.transform, text);
			if (transform2 == null)
			{
				transform2 = new GameObject(text).transform;
				transform2.parent = root.transform;
				transform2.localPosition = Vector3.zero;
				transform2.localRotation = Quaternion.identity;
			}
			if (text2 == "")
			{
				return transform2.gameObject;
			}
			return VMDAnimationController.CreateBone(transform2.gameObject, text2);
		}

		
		public void ResetBoneNameMap()
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>
			{
				{
					"_dummy_root",
					"全ての親"
				},
				{
					"_dummy_center",
					"センター"
				},
				{
					"_dummy_groove",
					"グルーブ"
				},
				{
					"_dummy_hips",
					"腰"
				},
				{
					"cf_j_spine01",
					"上半身"
				},
				{
					"cf_j_spine02",
					"上半身2"
				},
				{
					"cf_j_spine03",
					"上半身2先"
				},
				{
					"cf_j_neck",
					"首"
				},
				{
					"cf_j_head",
					"頭"
				},
				{
					"cf_j_shoulder_L",
					"左肩"
				},
				{
					"cf_j_arm00_L",
					"左腕"
				},
				{
					"cf_j_forearm01_L",
					"左ひじ"
				},
				{
					"cf_j_hand_L",
					"左手首"
				},
				{
					"cf_j_shoulder_R",
					"右肩"
				},
				{
					"cf_j_arm00_R",
					"右腕"
				},
				{
					"cf_j_forearm01_R",
					"右ひじ"
				},
				{
					"cf_j_hand_R",
					"右手首"
				},
				{
					"cf_j_waist01",
					"下半身"
				},
				{
					"cf_j_thigh00_L",
					"左足"
				},
				{
					"cf_j_leg01_L",
					"左ひざ"
				},
				{
					"cf_j_leg03_L",
					"左足首"
				},
				{
					"cf_j_thigh00_R",
					"右足"
				},
				{
					"cf_j_leg01_R",
					"右ひざ"
				},
				{
					"cf_j_leg03_R",
					"右足首"
				},
				{
					"cf_j_thumb01_L",
					"左親指０"
				},
				{
					"cf_j_thumb02_L",
					"左親指１"
				},
				{
					"cf_j_thumb03_L",
					"左親指２"
				},
				{
					"cf_j_index01_L",
					"左人指１"
				},
				{
					"cf_j_index02_L",
					"左人指２"
				},
				{
					"cf_j_index03_L",
					"左人指３"
				},
				{
					"cf_j_middle01_L",
					"左中指１"
				},
				{
					"cf_j_middle02_L",
					"左中指２"
				},
				{
					"cf_j_middle03_L",
					"左中指３"
				},
				{
					"cf_j_ring01_L",
					"左薬指１"
				},
				{
					"cf_j_ring02_L",
					"左薬指２"
				},
				{
					"cf_j_ring03_L",
					"左薬指３"
				},
				{
					"cf_j_little01_L",
					"左小指１"
				},
				{
					"cf_j_little02_L",
					"左小指２"
				},
				{
					"cf_j_little03_L",
					"左小指３"
				},
				{
					"cf_j_thumb01_R",
					"右親指０"
				},
				{
					"cf_j_thumb02_R",
					"右親指１"
				},
				{
					"cf_j_thumb03_R",
					"右親指２"
				},
				{
					"cf_j_index01_R",
					"右人指１"
				},
				{
					"cf_j_index02_R",
					"右人指２"
				},
				{
					"cf_j_index03_R",
					"右人指３"
				},
				{
					"cf_j_middle01_R",
					"右中指１"
				},
				{
					"cf_j_middle02_R",
					"右中指２"
				},
				{
					"cf_j_middle03_R",
					"右中指３"
				},
				{
					"cf_j_ring01_R",
					"右薬指１"
				},
				{
					"cf_j_ring02_R",
					"右薬指２"
				},
				{
					"cf_j_ring03_R",
					"右薬指３"
				},
				{
					"cf_j_little01_R",
					"右小指１"
				},
				{
					"cf_j_little02_R",
					"右小指２"
				},
				{
					"cf_j_little03_R",
					"右小指３"
				},
				{
					"_dummy_ik_root_L",
					"左足IK親"
				},
				{
					"_dummy_ik_root_R",
					"右足IK親"
				},
				{
					"_FOOT_IK_L",
					"左足ＩＫ"
				},
				{
					"_FOOT_IK_R",
					"右足ＩＫ"
				},
				{
					"_TOE_IK_L",
					"左つま先ＩＫ"
				},
				{
					"_TOE_IK_R",
					"右つま先ＩＫ"
				},
				{
					"_dummy_EYE_LR",
					"両目"
				},
				{
					"_dummy_EYE_L",
					"左目"
				},
				{
					"_dummy_EYE_R",
					"右目"
				}
			};
			this.boneNameMap = dictionary;
		}

		
		public void AddBoneNameMap(string boneName, string vmxBone)
		{
			this.boneNameMap[boneName] = vmxBone;
		}

		
		public void RemoveBoneNameMap(string boneName)
		{
			if (this.boneNameMap.ContainsKey(boneName))
			{
				this.boneNameMap.Remove(boneName);
			}
		}

		
		private void InitializeBasePositions()
		{
			this.ResetBoneRotations(this.chara.objBodyBone.transform, false);
			this.ResetBoneRotations(this.vmdAnimationGo.transform, true);
			this.ResetDummyTransformPositions();
			Transform transform = this.t_dummy_root;
			Vector3 vector = (this.t_leftToe.position + this.t_rightToe.position) / 2f;
			Vector3 vector2 = (this.t_leftFoot.position + this.t_rightFoot.position) / 2f;
			transform.InverseTransformPoint(vector);
			transform.InverseTransformPoint(vector2);
			Vector3 vector3 = (this.t_leftHeel.position + this.t_rightHeel.position) / 2f;
			vector3 = transform.transform.position - vector3;
			this.t_dummy_ik_root_L.position = this.t_leftHeel.position;
			this.t_dummy_ik_root_R.position = this.t_rightHeel.position;
			this.t_leftToeIK.position = this.t_leftToe.position;
			this.t_leftFootIK.position = this.t_leftFoot.position;
			this.t_leftToeIK.position = this.t_leftToe.position;
			this.t_rightFootIK.position = this.t_rightFoot.position;
			this.t_rightToeIK.position = this.t_rightToe.position;
			this.ModelBaseline.hipsPos = transform.InverseTransformPoint(this.t_dummy_hips.transform.position);
			this.ModelBaseline.centerBasePos = this.t_dummy_center.localPosition;
			this.ModelBaseline.grooveBasePos = this.t_dummy_groove.localPosition;
			this.ModelBaseline.leftIKCenterPos = transform.InverseTransformPoint(this.t_dummy_ik_root_L.position);
			this.ModelBaseline.rightIKCenterPos = transform.InverseTransformPoint(this.t_dummy_ik_root_R.position);
			this.ModelBaseline.leftFootPos = this.t_dummy_ik_root_L.InverseTransformPoint(this.t_leftFoot.transform.position);
			this.ModelBaseline.leftToePos = this.t_dummy_ik_root_L.InverseTransformPoint(this.t_leftToe.transform.position);
			this.ModelBaseline.leftToePosRel = this.t_leftFoot.InverseTransformPoint(this.t_leftToe.position);
			this.ModelBaseline.rightFootPos = this.t_dummy_ik_root_R.InverseTransformPoint(this.t_rightFoot.transform.position);
			this.ModelBaseline.rightToePos = this.t_dummy_ik_root_R.InverseTransformPoint(this.t_rightToe.transform.position);
			this.ModelBaseline.rightToePosRel = this.t_rightFoot.InverseTransformPoint(this.t_rightToe.position);
		}

		
		private void ResetDummyTransformPositions()
		{
			this.t_dummy_root.localPosition = Vector3.zero;
			this.t_dummy_center.localPosition = this.centerBasePos * 0.1f * this.centerPosAdjustRate;
			this.t_dummy_groove.localPosition = this.grooveBasePos * 0.1f * this.centerPosAdjustRate;
			Transform transform = VMDAnimationController.SearchObjName(this.chara.objBodyBone.transform, "cf_j_siri_L_01");
			Transform transform2 = VMDAnimationController.SearchObjName(this.chara.objBodyBone.transform, "cf_j_siri_R_01");
			this.t_dummy_hips.position = (transform.transform.position + transform2.transform.position) / 2f;
			this.ResetDummyTransformPosition(this.t_hips, "cf_j_hips");
			this.ResetDummyTransformPosition(this.t_spine01, "cf_j_spine01");
			this.ResetDummyTransformPosition(this.t_waist01, "cf_j_waist01");
			this.ResetDummyTransformPosition(this.t_waist02, "cf_j_waist02");
			this.ResetDummyTransformPosition(this.t_leftLowerLeg, "cf_j_leg01_L");
			this.ResetDummyTransformPosition(this.t_leftUpperLeg, "cf_j_thigh00_L");
			this.ResetDummyTransformPosition(this.t_rightLowerLeg, "cf_j_leg01_R");
			this.ResetDummyTransformPosition(this.t_rightUpperLeg, "cf_j_thigh00_R");
			this.ResetDummyTransformPosition(this.t_leftFoot, "cf_j_leg03_L");
			this.ResetDummyTransformPosition(this.t_rightFoot, "cf_j_leg03_R");
			this.ResetDummyTransformPosition(this.t_leftHeel, "cf_j_foot_L");
			this.ResetDummyTransformPosition(this.t_rightHeel, "cf_j_foot_R");
			this.ResetDummyTransformPosition(this.t_leftToe, "cf_j_toes_L");
			this.ResetDummyTransformPosition(this.t_rightToe, "cf_j_toes_R");
		}

		
		private void ResetDummyTransformPosition(Transform dummy, string name)
		{
			Transform transform = VMDAnimationController.SearchObjName(this.chara.objBodyBone.transform, name).transform;
			dummy.position = transform.position;
			dummy.rotation = transform.rotation;
		}

		
		public void ResetBoneRotations()
		{
			Transform transform = this.chara.objBodyBone.transform;
			this.ResetBoneRotations(transform, false);
		}

		
		public void ResetBoneRotations(Transform root, bool forceZero = false)
		{
			foreach (string text in this.initialRotations.Keys)
			{
				Quaternion quaternion = this.initialRotations[text];
				Transform transform = VMDAnimationController.SearchObjName(root, text);
				if (transform != null)
				{
					if (transform.localRotation != quaternion)
					{
                        KKVMDPlugin.Logger.Log(BepInEx.Logging.LogLevel.Debug, string.Format("Initial rotation diff : {0}: pose ({1}, {2}, {3}), param: ({4}, {5}, {6})", new object[]
						{
							transform.name,
							transform.localRotation.eulerAngles.x,
							transform.localRotation.eulerAngles.y,
							transform.localRotation.eulerAngles.z,
							quaternion.eulerAngles.x,
							quaternion.eulerAngles.y,
							quaternion.eulerAngles.z
						}));
					}
					transform.localRotation = Quaternion.identity;
					if (!forceZero)
					{
						transform.localRotation = quaternion;
					}
				}
			}
		}

		
		public void ResetRotationMap()
		{
			this.boneAdjust = new Dictionary<string, BoneAdjustment>();
			this.AddRotAxisMap("_dummy_root", "-x,y,-z");
			this.AddRotAxisMap("_dummy_center", "-x,y,-z");
			this.AddRotAxisMap("_dummy_groove", "-x,y,-z");
			this.AddRotAxisMap("_dummy_hips", "-x,y,-z");
			this.AddRotAxisMap("cf_j_hips", "-x,y,-z");
			this.AddRotAxisMap("cf_j_spine01", "-x,y,-z");
			this.AddRotAxisMap("cf_j_spine02", "-x,y,-z");
			this.AddRotAxisMap("cf_j_neck", "-x,y,-z");
			this.AddRotAxisMap("cf_j_head", "-x,y,-z");
			this.AddRotAxisMap("cf_j_shoulder_L", "-x,y,-z");
			this.AddRotAxisMap("cf_j_arm00_L", "-x,y,-z");
			this.AddRotAxisMap("cf_j_forearm01_L", "-x,y,-z");
			this.AddRotAxisMap("cf_j_hand_L", "-x,y,-z");
			this.AddRotAxisMap("cf_j_shoulder_R", "-x,y,-z");
			this.AddRotAxisMap("cf_j_arm00_R", "-x,y,-z");
			this.AddRotAxisMap("cf_j_forearm01_R", "-x,y,-z");
			this.AddRotAxisMap("cf_j_hand_R", "-x,y,-z");
			this.AddRotAxisMap("cf_j_waist01", "-x,y,-z");
			this.AddRotAxisMap("cf_j_waist02", "-x,y,-z");
			this.AddRotAxisMap("cf_j_thigh00_L", "-x,y,-z");
			this.AddRotAxisMap("cf_j_leg01_L", "-x,y,-z");
			this.AddRotAxisMap("cf_j_leg03_L", "-x,y,-z");
			this.AddRotAxisMap("cf_j_foot_L", "-x,y,-z");
			this.AddRotAxisMap("cf_j_thigh00_R", "-x,y,-z");
			this.AddRotAxisMap("cf_j_leg01_R", "-x,y,-z");
			this.AddRotAxisMap("cf_j_leg03_R", "-x,y,-z");
			this.AddRotAxisMap("cf_j_foot_R", "-x,y,-z");
			this.AddRotAxisMap("_dummy_ik_root_L", "-x,y,-z");
			this.AddRotAxisMap("_dummy_ik_root_R", "-x,y,-z");
			this.AddRotAxisMap("_FOOT_IK_L", "-x,y,-z");
			this.AddRotAxisMap("_FOOT_IK_R", "-x,y,-z");
			this.AddRotAxisMap("_TOE_IK_L", "-x,y,-z");
			this.AddRotAxisMap("_TOE_IK_R", "-x,y,-z");
			foreach (string text in new string[]
			{
				"L",
				"R"
			})
			{
				for (int j = 1; j <= 3; j++)
				{
					this.AddRotAxisMap(string.Concat(new object[]
					{
						"cf_j_thumb0",
						j,
						"_",
						text
					}), "-x,y,-z");
					this.AddRotAxisMap(string.Concat(new object[]
					{
						"cf_j_index0",
						j,
						"_",
						text
					}), "-x,y,-z");
					this.AddRotAxisMap(string.Concat(new object[]
					{
						"cf_j_middle0",
						j,
						"_",
						text
					}), "-x,y,-z");
					this.AddRotAxisMap(string.Concat(new object[]
					{
						"cf_j_ring0",
						j,
						"_",
						text
					}), "-x,y,-z");
					this.AddRotAxisMap(string.Concat(new object[]
					{
						"cf_j_little0",
						j,
						"_",
						text
					}), "-x,y,-z");
				}
			}
			this.AddRotAxisMap("_dummy_EYE_LR", "-x,y,-z");
			this.AddRotAxisMap("_dummy_EYE_L", "-x,y,-z");
			this.AddRotAxisMap("_dummy_EYE_R", "-x,y,-z");
			this.quickAdjustBoneInitialized = true;
			this.quickAdjust.Set();
			this.boneAdjust[this._armLowKey[0]].rotAxisAdjustment = true;
			this.boneAdjust[this._armLowKey[1]].rotAxisAdjustment = true;
			this.boneAdjust["cf_j_neck"].rotationScale = 0.5f;
			this.AddInitialRotation("_dummy_root", 0f, 0f, 0f);
			this.AddInitialRotation("_dummy_center", 0f, 0f, 0f);
			this.AddInitialRotation("_dummy_groove", 0f, 0f, 0f);
			this.AddInitialRotation("_dummy_hips", 0f, 0f, 0f);
			this.AddInitialRotation("cf_j_hips", 0f, 0f, 0f);
			this.AddInitialRotation("cf_j_spine01", 0f, 0f, 0f);
			this.AddInitialRotation("cf_j_spine02", 0f, 0f, 0f);
			this.AddInitialRotation("cf_j_spine03", 0f, 0f, 0f);
			this.AddInitialRotation("cf_j_neck", 0f, 0f, 0f);
			this.AddInitialRotation("cf_j_head", 0f, 0f, 0f);
			this.AddInitialRotation("cf_j_shoulder_L", 0f, 0f, 0f);
			this.AddInitialRotation("cf_j_arm00_L", 0f, 357.5f, 0f);
			this.AddInitialRotation("cf_j_forearm01_L", 0f, 2.5f, 0f);
			this.AddInitialRotation("cf_j_hand_L", 0f, 0f, 0f);
			this.AddInitialRotation("cf_j_shoulder_R", 0f, 0f, 0f);
			this.AddInitialRotation("cf_j_arm00_R", 0f, 2.5f, 0f);
			this.AddInitialRotation("cf_j_forearm01_R", 0f, 357.5f, 0f);
			this.AddInitialRotation("cf_j_hand_R", 0f, 0f, 0f);
			this.AddInitialRotation("cf_j_waist01", 0f, 0f, 0f);
			this.AddInitialRotation("cf_j_waist02", 0f, 0f, 0f);
			this.AddInitialRotation("cf_j_thigh00_L", 1f, 0f, 0f);
			this.AddInitialRotation("cf_j_leg01_L", 1f, 0f, 0f);
			this.AddInitialRotation("cf_j_leg03_L", 0f, 0f, 0f);
			this.AddInitialRotation("cf_j_foot_L", 358f, 0f, 0f);
			this.AddInitialRotation("cf_j_thigh00_R", 1f, 0f, 0f);
			this.AddInitialRotation("cf_j_leg01_R", 1f, 0f, 0f);
			this.AddInitialRotation("cf_j_leg03_R", 0f, 0f, 0f);
			this.AddInitialRotation("cf_j_foot_R", 358f, 0f, 0f);
			this.AddInitialRotation("cf_j_toes_L", 0f, 0f, 0f);
			this.AddInitialRotation("cf_j_toes_R", 0f, 0f, 0f);
			foreach (string text2 in new string[]
			{
				"L",
				"R"
			})
			{
				for (int k = 2; k <= 3; k++)
				{
					this.AddInitialRotation(string.Concat(new object[]
					{
						"cf_j_thumb0",
						k,
						"_",
						text2
					}), 0f, 0f, 0f);
					this.AddInitialRotation(string.Concat(new object[]
					{
						"cf_j_index0",
						k,
						"_",
						text2
					}), 0f, 0f, 0f);
					this.AddInitialRotation(string.Concat(new object[]
					{
						"cf_j_middle0",
						k,
						"_",
						text2
					}), 0f, 0f, 0f);
					this.AddInitialRotation(string.Concat(new object[]
					{
						"cf_j_ring0",
						k,
						"_",
						text2
					}), 0f, 0f, 0f);
					this.AddInitialRotation(string.Concat(new object[]
					{
						"cf_j_little0",
						k,
						"_",
						text2
					}), 0f, 0f, 0f);
				}
			}
			this.AddInitialRotation("cf_j_thumb01_L", 60f, 30f, 20f);
			this.AddInitialRotation("cf_j_thumb01_R", 300f, 150f, 200f);
			this.AddInitialRotation("cf_j_index01_L", 0f, 0f, 0f);
			this.AddInitialRotation("cf_j_index01_R", 0f, 180f, 180f);
			this.AddInitialRotation("cf_j_middle01_L", 0f, 0f, 0f);
			this.AddInitialRotation("cf_j_middle01_R", 0f, 180f, 180f);
			this.AddInitialRotation("cf_j_ring01_L", 0f, 0f, 0f);
			this.AddInitialRotation("cf_j_ring01_R", 0f, 180f, 180f);
			this.AddInitialRotation("cf_j_little01_L", 0f, 0f, 0f);
			this.AddInitialRotation("cf_j_little01_R", 0f, 180f, 180f);
			this.AddInitialRotation("_dummy_ik_root_L", 0f, 0f, 0f);
			this.AddInitialRotation("_dummy_ik_root_R", 0f, 0f, 0f);
			this.AddInitialRotation("_FOOT_IK_L", 0f, 0f, 0f);
			this.AddInitialRotation("_FOOT_IK_R", 0f, 0f, 0f);
			this.AddInitialRotation("_TOE_IK_L", 0f, 0f, 0f);
			this.AddInitialRotation("_TOE_IK_R", 0f, 0f, 0f);
			this.AddInitialRotation("_dummy_EYE_LR", 0f, 0f, 0f);
			this.AddInitialRotation("_dummy_EYE_L", 0f, 0f, 0f);
			this.AddInitialRotation("_dummy_EYE_R", 0f, 0f, 0f);
		}

		
		public void AddInitialRotation(string boneName, float x, float y, float z)
		{
			this.initialRotations[boneName] = Quaternion.Euler(x, y, z);
		}

		
		public void AddRotAxisMap(string spec)
		{
			this.AddRotAxisMap(this.lastAdjustedBone, spec);
		}

		
		public void AddRotAxisMap(string boneName, string spec)
		{
			try
			{
				if (boneName != null)
				{
					BoneAdjustment boneAdjustment = null;
					this.boneAdjust.TryGetValue(boneName, out boneAdjustment);
					if (boneAdjustment == null)
					{
						boneAdjustment = BoneAdjustment.Init(boneName, spec, Vector3.zero, false);
						this.boneAdjust[boneName] = boneAdjustment;
					}
					else
					{
						boneAdjustment.SetSpec(spec);
					}
					this.lastAdjustedBone = boneName;
					this.lastModifiedAdjustment = boneAdjustment;
				}
			}
			catch (Exception arg)
			{
				Console.WriteLine("Parse error: {0}. {1}", spec, arg);
			}
		}

		
		public void AddRotationMap(string boneName, float x, float y, float z, bool adjustAxis, float axisX, float axisY, float axisZ)
		{
			if (this.boneNameMap.ContainsKey(boneName))
			{
				Vector3 rotAdjustment;
				rotAdjustment = new Vector3(x, y, z);
				Vector3 axisAdjustment;
                axisAdjustment = new Vector3(axisX, axisY, axisZ);
				BoneAdjustment boneAdjustment = null;
				this.boneAdjust.TryGetValue(boneName, out boneAdjustment);
				if (boneAdjustment == null)
				{
					boneAdjustment = BoneAdjustment.Init(boneName, "x,y,z", rotAdjustment, adjustAxis);
					boneAdjustment.SetAxisAdjustment(axisAdjustment);
					this.boneAdjust[boneName] = boneAdjustment;
				}
				else
				{
					boneAdjustment.SetRotAdjustment(rotAdjustment);
					boneAdjustment.rotAxisAdjustment = adjustAxis;
					boneAdjustment.SetAxisAdjustment(axisAdjustment);
				}
				this.lastAdjustedBone = boneName;
				this.lastModifiedAdjustment = boneAdjustment;
			}
		}

		
		public void AddRotationMap(string boneName, float x, float y, float z)
		{
			this.AddRotationMap(boneName, x, y, z, false, 0f, 0f, 0f);
		}

		
		public void RemoveRotationMap(string boneName)
		{
			if (this.boneAdjust.ContainsKey(boneName))
			{
				this.boneAdjust[boneName].SetRotAdjustment(Vector3.zero);
			}
		}

		
		public void ClearRotationMap()
		{
			this.boneAdjust = new Dictionary<string, BoneAdjustment>();
			this.lastAdjustedBone = null;
			this.lastModifiedAdjustment = null;
		}

		
		public void SetCurrentRot(string name)
		{
			if (this.boneAdjust.ContainsKey(name))
			{
				this.lastAdjustedBone = name;
				this.lastModifiedAdjustment = this.boneAdjust[name];
			}
		}

		
		public void DumpBoneAdjustInfo()
		{
			using (StreamWriter streamWriter = File.CreateText("__rotinfo.txt"))
			{
				foreach (string text in this.boneAdjust.Keys)
				{
					BoneAdjustment boneAdjustment = this.boneAdjust[text];
					streamWriter.WriteLine("{0}, {1}, {2}, {3}, {4}, {5}", new object[]
					{
						text,
						boneAdjustment.spec,
						boneAdjustment.RotX,
						boneAdjustment.RotY,
						boneAdjustment.RotZ,
						boneAdjustment.rotAxisAdjustment
					});
				}
			}
		}

		
		public void ProcessFootIK()
		{
			this.ProcessFootIK1();
		}

		
		public void ProcessFootIK1()
		{
			if (this.enableIK)
			{
				if (this.leftLegIKSolver == null || this.rightLegIKSolver == null)
				{
					Console.WriteLine("Leg IK Solver is null.");
					return;
				}
				if (this.IKWeight.limitIKPositionFloor)
				{
					this.EnsureIKPostionIsOnFloor(this.t_leftFootIK, this.ModelBaseline.leftFootPos.y);
					this.EnsureIKPostionIsOnFloor(this.t_leftToeIK, this.ModelBaseline.leftToePos.y);
					this.EnsureIKPostionIsOnFloor(this.t_rightFootIK, this.ModelBaseline.rightFootPos.y);
					this.EnsureIKPostionIsOnFloor(this.t_rightToeIK, this.ModelBaseline.rightToePos.y);
				}
				this.leftLegIKSolver.controll_weight = this.IKWeight.footControlWeight;
				this.leftLegIKSolver.iterations = this.IKWeight.footIteration;
				this.leftLegIKSolver.minDelta = this.IKWeight.minDelta * 0.0001f;
				this.leftLegIKSolver.lastFrameWeight = this.IKWeight.lastFrameRefRate;
				this.leftLegIKSolver.ikRotationWeight = this.IKWeight.footIKRotWeight;
				this.leftLegIKSolver.minKneeRot = this.IKWeight.minKneeRot;
				this.leftLegIKSolver.useLeg = this.IKWeight.useLegSolver;
				this.rightLegIKSolver.controll_weight = this.IKWeight.footControlWeight;
				this.rightLegIKSolver.iterations = this.IKWeight.footIteration;
				this.rightLegIKSolver.minDelta = this.IKWeight.minDelta * 0.0001f;
				this.rightLegIKSolver.lastFrameWeight = this.IKWeight.lastFrameRefRate;
				this.rightLegIKSolver.ikRotationWeight = this.IKWeight.footIKRotWeight;
				this.rightLegIKSolver.minKneeRot = this.IKWeight.minKneeRot;
				this.rightLegIKSolver.useLeg = this.IKWeight.useLegSolver;
				this.leftLegIKSolver.SolveLeg2();
				this.rightLegIKSolver.SolveLeg2();
				if (!this.IKWeight.disableToeIK)
				{
					this.leftToeIKSolver.controll_weight = this.IKWeight.toeControlWeight;
					this.leftToeIKSolver.iterations = this.IKWeight.toeIteration;
					this.leftToeIKSolver.lastFrameWeight = this.IKWeight.lastFrameRefRate;
					this.leftToeIKSolver.Solve();
					this.rightToeIKSolver.controll_weight = this.IKWeight.toeControlWeight;
					this.rightToeIKSolver.iterations = this.IKWeight.toeIteration;
					this.rightToeIKSolver.lastFrameWeight = this.IKWeight.lastFrameRefRate;
					this.rightToeIKSolver.Solve();
				}
			}
		}

		
		private void EnsureIKPostionIsOnFloor(Transform t_ikNode, float min = 0f)
		{
			Vector3 vector = this.t_dummy_root.InverseTransformPoint(t_ikNode.position);
			if (vector.y < min)
			{
				vector.y = min;
			}
			t_ikNode.position = this.t_dummy_root.TransformPoint(vector);
		}

		
		private CCDIKSolver GetLegIK(Transform footIK, Transform foot, Transform lowerLeg, Transform upperLeg)
		{
			return new CCDIKSolver
			{
				ikBone = footIK,
				target = foot,
				chains = new Transform[]
				{
					lowerLeg,
					upperLeg
				}
			};
		}

		
		private CCDIKSolver GetToeIK(Transform foot, Transform heel, Transform toe, Transform toeIK, Transform lowerLeg, Transform upperLeg)
		{
			return new CCDIKSolver
			{
				ikBone = toeIK,
				target = toe,
				chains = new Transform[]
				{
					heel
				}
			};
		}

		
		
		
		public bool faceAnimeEnabled
		{
			get
			{
				return this.faceController.enabled;
			}
			set
			{
				this.faceController.enabled = value;
			}
		}

		
		private void ProcessEyeLook()
		{
			if (!this.enableEyeControl)
			{
				return;
			}
			if (this.chara == null || this.chara.eyeLookCtrl == null || this.chara.eyeLookCtrl.eyeLookScript == null)
			{
				return;
			}
			try
			{
				EyeLookCalc eyeLookScript = this.chara.eyeLookCtrl.eyeLookScript;
				if (this.chara.eyeLookCtrl.ptnNo != 1 && this.chara.eyeLookCtrl.ptnNo != 2)
				{
					EyeObject eyeObject = eyeLookScript.eyeObjs[0];
					EyeObject eyeObject2 = eyeLookScript.eyeObjs[1];
					EyeTypeState eyeTypeState = eyeLookScript.eyeTypeStates[4];
					Quaternion rot = this.t_dummy_EYE_LR.localRotation * this.t_dummy_EYE_L.localRotation;
					Quaternion rot2 = this.t_dummy_EYE_LR.localRotation * this.t_dummy_EYE_R.localRotation;
					this.UpdateEyeRorationRate(eyeTypeState, eyeObject, rot, false);
					this.UpdateEyeRorationRate(eyeTypeState, eyeObject2, rot2, true);
					VMDAnimationController.m_AngleHRateCalc.Invoke(eyeLookScript, null);
					VMDAnimationController.m_AngleVRateCalc.Invoke(eyeLookScript, null);
					EyeLookMaterialControll[] componentsInChildren = this.chara.objHead.GetComponentsInChildren<EyeLookMaterialControll>();
					for (int i = 0; i < componentsInChildren.Length; i++)
					{
						componentsInChildren[i].Invoke("Update", 0f);
					}
				}
			}
			catch (Exception ex)
			{
                KKVMDPlugin.Logger.Log(BepInEx.Logging.LogLevel.Debug, ex);
			}
		}

		
		private void UpdateEyeRorationRate(EyeTypeState eyeTypeState, EyeObject eyeObject, Quaternion rot, bool isRight)
		{
			Vector3 vector = (Vector3)VMDAnimationController.f_referenceUpDir.GetValue(eyeObject);
			Vector3 vector2 = (Vector3)VMDAnimationController.f_referenceLookDir.GetValue(eyeObject);
			Vector3 vector3 = rot * Vector3.forward;
			float num = EyeLookCalc.AngleAroundAxis(vector2, vector3, vector);
			Vector3 vector4 = Vector3.Cross(vector, vector3);
			float num2 = EyeLookCalc.AngleAroundAxis(vector3 - Vector3.Project(vector3, vector), vector3, vector4);
			num *= 3f;
			num2 *= 3f;
			float num3 = Mathf.Max(0f, Mathf.Abs(num) - eyeTypeState.thresholdAngleDifference) * Mathf.Sign(num);
			float num4 = Mathf.Max(0f, Mathf.Abs(num2) - eyeTypeState.thresholdAngleDifference) * Mathf.Sign(num2);
			num = Mathf.Max(Mathf.Abs(num3) * Mathf.Abs(eyeTypeState.bendingMultiplier), Mathf.Abs(num) - eyeTypeState.maxAngleDifference) * Mathf.Sign(num) * Mathf.Sign(eyeTypeState.bendingMultiplier);
			num2 = Mathf.Max(Mathf.Abs(num4) * Mathf.Abs(eyeTypeState.bendingMultiplier), Mathf.Abs(num2) - eyeTypeState.maxAngleDifference) * Mathf.Sign(num2) * Mathf.Sign(eyeTypeState.bendingMultiplier);
			float num5 = eyeTypeState.maxBendingAngle;
			float num6 = eyeTypeState.minBendingAngle;
			if (eyeObject.eyeLR == (EYE_LR)1)
			{
				num5 = -eyeTypeState.minBendingAngle;
				num6 = -eyeTypeState.maxBendingAngle;
			}
			num = Mathf.Clamp(num, num6, num5);
			num2 = Mathf.Clamp(num2, eyeTypeState.upBendingAngle, eyeTypeState.downBendingAngle);
			VMDAnimationController.f_angleH.SetValue(eyeObject, num);
			VMDAnimationController.f_angleV.SetValue(eyeObject, num2);
		}

		
		
		
		public bool AdjustSkirtBones
		{
			get
			{
				return this.adjustSkirtBones;
			}
			set
			{
				this.adjustSkirtBones = value;
			}
		}

		
		public void ReloadVMDAnimation(bool play = true)
		{
			if (this.lastLoadedVMD != null)
			{
				this.LoadVMDAnimation(this.lastLoadedVMD, play);
			}
		}

		
		public void LoadVMDAnimation(string path)
		{
			this.LoadVMDAnimation(path, true);
		}

		
		public void LoadVMDAnimation(string path, bool play)
		{
			base.StartCoroutine(this.ResetAndLoadVMDAnimationCo(path, play));
		}

		
		public void LoadVMDAnimation_new(string path, bool play)
		{
			try
			{
				if (this.chara != null)
				{
					using (BinaryReader binaryReader = new BinaryReader(File.OpenRead(path)))
					{
						VMDFormat format = VMDLoader.Load(binaryReader, path, this.clipname);
						Transform transform = this.vmdAnimationGo.transform;
						if (this.animationForVMD == null)
						{
							this.animationForVMD = transform.gameObject.GetComponent<Animation>();
						}
						this.animationForVMD.Stop();
						VMDHSConverter vmdhsconverter;
						if (this.bakeScaleAdjustmentIntoAnimation)
						{
							Vector3 vector = Vector3.one * this.scaleBase;
							vector *= this.quickAdjust.ScaleModel;
							vector.y *= this.moveYScaleAdjust;
							vmdhsconverter = new VMDHSConverter(this.boneNameMap, this.ModelBaseline, this.boneAdjust, vector, this.centerBasePos, this.hipPositionAdjust, "_face", this.faceController.FacesToCheck);
						}
						else
						{
							vmdhsconverter = new VMDHSConverter(this.boneNameMap, this.ModelBaseline, this.boneAdjust, Vector3.one * this.scaleBase, this.centerBasePos, this.hipPositionAdjust, "_face", this.faceController.FacesToCheck);
						}
						AnimationClip animationClip = vmdhsconverter.CreateAnimationClip(format, this.vmdAnimationGo.gameObject, 4);
						if (this.loop)
						{
							animationClip.wrapMode = (WrapMode)2;
						}
						else
						{
							animationClip.wrapMode = (WrapMode)1;
						}
						this.lastLoadedVMD = path;
						this.clipname = "VMDAnim";
						this.animationForVMD.AddClip(animationClip, this.clipname);
						this.animationForVMD.clip = animationClip;
						this.animationForVMD[this.clipname].speed = this._speed;
						if (play)
						{
							this.animationForVMD.Play(this.clipname);
						}
					}
				}
			}
			catch (Exception value)
			{
				Console.WriteLine(value);
			}
		}

		
		private IEnumerator ResetAndLoadVMDAnimationCo(string path, bool play)
		{
			this.VMDAnimEnabled = false;
			this.DisableEnableDefaultAnim();
			yield return null;
			yield return new WaitForEndOfFrame();
			this.InitializeBasePositions();
			this.LoadVMDAnimation_new(path, play);
			this.VMDAnimEnabled = true;
			yield break;
		}

		
		public void DeleteAnim()
		{
			Animation animation = this.GetAnimation();
			if (animation != null && this.clipname != null)
			{
				AnimationClip clip = animation.GetClip(this.clipname);
				if (clip != null)
				{
					animation.RemoveClip(clip);
				}
				this.clipname = null;
			}
		}

		
		
		
		public float speed
		{
			get
			{
				return this._speed;
			}
			set
			{
				if (this._speed != value)
				{
					this._speed = value;
					this.SetSpeed(this._speed);
				}
			}
		}

		
		
		
		public bool Loop
		{
			get
			{
				return this.loop;
			}
			set
			{
				if (this.loop != value)
				{
					if (this.clipname != null)
					{
						if (value)
						{
							this.GetAnimation()[this.clipname].clip.wrapMode = (WrapMode)2;
							this.GetAnimation()[this.clipname].wrapMode = (WrapMode)2;
							this.GetAnimation().wrapMode = (WrapMode)2;
						}
						else
						{
							this.GetAnimation()[this.clipname].clip.wrapMode = (WrapMode)1;
							this.GetAnimation()[this.clipname].wrapMode = (WrapMode)1;
							this.GetAnimation().wrapMode = (WrapMode)1;
						}
					}
					this.loop = value;
				}
			}
		}

		
		private Animation GetAnimation()
		{
			return this.animationForVMD;
		}

		
		public void Play()
		{
			if (this.clipname != null && this.vmdAnimEnabled)
			{
				this.GetAnimation()[this.clipname].speed = this._speed;
				this.GetAnimation().Play(this.clipname);
			}
		}

		
		public void Pause()
		{
			if (this.clipname != null && this.vmdAnimEnabled)
			{
				this.GetAnimation()[this.clipname].speed = 0f;
			}
		}

		
		public void Stop()
		{
			if (this.clipname != null && this.vmdAnimEnabled)
			{
				this.GetAnimation().Stop(this.clipname);
				if (this.faceController != null)
				{
					this.faceController.ResetFaceValues();
				}
			}
		}

		
		public void Restart()
		{
			if (this.clipname != null && this.vmdAnimEnabled)
			{
				Animation animation = this.GetAnimation();
				animation[this.clipname].normalizedTime = 0f;
				animation[this.clipname].speed = this._speed;
				animation.Play(this.clipname);
				if (this.faceController != null)
				{
					this.faceController.ResetFaceValues();
				}
			}
		}

		
		public void SetAnimPosition(float time)
		{
			if (this.clipname != null && this.vmdAnimEnabled)
			{
				this.GetAnimation()[this.clipname].time = time;
			}
		}

		
		private void SetSpeed(float speed)
		{
			Animation animation = this.GetAnimation();
			if (animation != null && this.clipname != null)
			{
				animation[this.clipname].speed = speed;
			}
		}

		
		private float GetSpeed()
		{
			Animation animation = this.GetAnimation();
			if (animation != null && this.clipname != null)
			{
				return animation[this.clipname].speed;
			}
			return 0f;
		}

		
		public float GetAnimTime()
		{
			if (this.clipname != null)
			{
				return this.GetAnimation()[this.clipname].time;
			}
			return 0f;
		}

		
		public bool IsVMDAnimeActive()
		{
			return this.GetAnimation().GetClipCount() > 0 && this.GetAnimation().IsPlaying(this.clipname);
		}

		
		
		
		public bool VMDAnimEnabled
		{
			get
			{
				return this.vmdAnimEnabled;
			}
			set
			{
				if (this.vmdAnimEnabled != value)
				{
					if (value)
					{
						if (this.animeOverride.DefaultAnimeEnabled)
						{
							this.animeOverride.Aquire(this);
							this.vmdAnimEnabled = true;
							this.OnVMDAnimEnabledChanged();
							return;
						}
					}
					else
					{
						this.animeOverride.Release(this);
						if (this.animeOverride.DefaultAnimeEnabled)
						{
							this.vmdAnimEnabled = false;
							this.OnVMDAnimEnabledChanged();
						}
					}
				}
			}
		}

		
		private void DisableEnableDefaultAnim()
		{
			if (this.vmdAnimEnabled)
			{
				this.chara.animBody.enabled = false;
				FullBodyBipedIK component = this.chara.objAnim.gameObject.GetComponent<FullBodyBipedIK>();
				if (component.enabled)
				{
					this.oldIKEnabled = true;
					component.enabled = false;
					return;
				}
			}
			else
			{
				this.chara.animBody.enabled = true;
				FullBodyBipedIK component2 = this.chara.objAnim.gameObject.GetComponent<FullBodyBipedIK>();
				if (this.oldIKEnabled)
				{
					component2.enabled = true;
				}
			}
		}

		
		private void OnVMDAnimEnabledChanged()
		{
			this.DisableEnableDefaultAnim();
			if (this.animationForVMD != null)
			{
				this.animationForVMD.enabled = this.vmdAnimEnabled;
			}
		}

		
		private void OnDestroy()
		{
			base.StopAllCoroutines();
			VMDAnimationMgr.Instance.controllers.Remove(this);
			UnityEngine.Object.Destroy(this.animeOverride);
            UnityEngine.Object.Destroy(this.vmdAnimationGo);
            UnityEngine.Object.Destroy(this.faceController.gameObject);
		}

		
		private static Transform SearchObjName(Transform root, string name)
		{
			GameObject gameObject = TransformFindEx.FindLoop(root, name);
			if (gameObject)
			{
				return gameObject.transform;
			}
            KKVMDPlugin.Logger.Log(BepInEx.Logging.LogLevel.Debug, string.Concat(new string[]
			{
				"Try to find ",
				name,
				" from ",
				root.name,
				" but not found."
			}));
			return null;
		}

		
		public ChaControl chara;

		
		private string clipname;

		
		public const string CLIP_NAME = "VMDAnim";

		
		private float _speed = 1f;

		
		private bool loop;

		
		public float scale = 1f;

		
		public Vector3 centerBasePos = new Vector3(0f, 8f, 0f);

		
		public Vector3 grooveBasePos = new Vector3(0f, 0.2f, 0f);

		
		private float scaleBase = 0.095f;

		
		public Vector3 hipPositionAdjust = new Vector3(0f, 0f, 0f);

		
		public Vector3 centerPosAdjust = new Vector3(0f, 0f, 0f);

		
		public float centerPosAdjustRate = 0.65f;

		
		public float moveYScaleAdjust = 1f;

		
		public Dictionary<string, string> boneNameMap;

		
		private Transform t_n_height_orig;

		
		private Transform t_n_height;

		
		private Transform t_dummy_root;

		
		private Transform t_dummy_center;

		
		private Transform t_dummy_groove;

		
		private Transform t_dummy_hips;

		
		private Transform t_hips;

		
		private Transform t_hips_orig;

		
		private Transform t_spine01;

		
		private Transform t_waist01;

		
		private Transform t_waist02;

		
		private Transform t_leftHeel;

		
		private Transform t_rightHeel;

		
		private Transform t_leftFoot;

		
		private Transform t_rightFoot;

		
		private Transform t_leftToe;

		
		private Transform t_rightToe;

		
		private Transform t_leftLowerLeg;

		
		private Transform t_rightLowerLeg;

		
		private Transform t_leftUpperLeg;

		
		private Transform t_rightUpperLeg;

		
		public bool enableIK = true;

		
		private Transform t_dummy_ik_root_L;

		
		private Transform t_dummy_ik_root_R;

		
		private Transform t_leftFootIK;

		
		private Transform t_rightFootIK;

		
		private Transform t_leftToeIK;

		
		private Transform t_rightToeIK;

		
		private const string FOOT_IK_NAME_BASE = "_FOOT_IK_";

		
		private const string TOE_IK_NAME_BASE = "_TOE_IK_";

		
		private CCDIKSolver leftLegIKSolver;

		
		private CCDIKSolver rightLegIKSolver;

		
		private CCDIKSolver leftToeIKSolver;

		
		private CCDIKSolver rightToeIKSolver;

		
		public ModelBaselineData ModelBaseline = new ModelBaselineData();

		
		private Dictionary<string, Quaternion> initialRotations = new Dictionary<string, Quaternion>();

		
		private DefaultCharaAnimOverride animeOverride;

		
		public GameObject vmdAnimationGo;

		
		public string lastLoadedVMD;

		
		public Animation animationForVMD;

		
		public bool bakeScaleAdjustmentIntoAnimation;

		
		public VMDAnimationController.AutoTrack autoTrack;

		
		private Dictionary<Transform, Vector3> localPositionBackup;

		
		private List<string> boneNames = new List<string>
		{
			"cf_n_height",
			"cf_n_height/_dummy_root",
			"_dummy_root/_dummy_center",
			"_dummy_center/_dummy_groove",
			"_dummy_groove/_dummy_hips",
			"_dummy_hips/cf_j_hips",
			"cf_j_hips/cf_j_spine01",
			"cf_j_spine01/cf_j_spine02",
			"cf_j_spine02/cf_j_spine03",
			"cf_j_spine03/cf_j_neck",
			"cf_j_neck/cf_j_head",
			"cf_j_spine03/cf_d_shoulder_L",
			"cf_d_shoulder_L/cf_j_shoulder_L",
			"cf_j_shoulder_L/cf_j_arm00_L",
			"cf_j_arm00_L/cf_j_forearm01_L",
			"cf_j_forearm01_L/cf_j_hand_L",
			"cf_j_hand_L/cf_s_hand_L",
			"cf_s_hand_L/cf_j_thumb01_L",
			"cf_j_thumb01_L/cf_j_thumb02_L",
			"cf_j_thumb02_L/cf_j_thumb03_L",
			"cf_s_hand_L/cf_j_index01_L",
			"cf_j_index01_L/cf_j_index02_L",
			"cf_j_index02_L/cf_j_index03_L",
			"cf_s_hand_L/cf_j_middle01_L",
			"cf_j_middle01_L/cf_j_middle02_L",
			"cf_j_middle02_L/cf_j_middle03_L",
			"cf_s_hand_L/cf_j_ring01_L",
			"cf_j_ring01_L/cf_j_ring02_L",
			"cf_j_ring02_L/cf_j_ring03_L",
			"cf_s_hand_L/cf_j_little01_L",
			"cf_j_little01_L/cf_j_little02_L",
			"cf_j_little02_L/cf_j_little03_L",
			"cf_j_spine03/cf_d_shoulder_R",
			"cf_d_shoulder_R/cf_j_shoulder_R",
			"cf_j_shoulder_R/cf_j_arm00_R",
			"cf_j_arm00_R/cf_j_forearm01_R",
			"cf_j_forearm01_R/cf_j_hand_R",
			"cf_j_hand_R/cf_s_hand_R",
			"cf_s_hand_R/cf_j_thumb01_R",
			"cf_j_thumb01_R/cf_j_thumb02_R",
			"cf_j_thumb02_R/cf_j_thumb03_R",
			"cf_s_hand_R/cf_j_index01_R",
			"cf_j_index01_R/cf_j_index02_R",
			"cf_j_index02_R/cf_j_index03_R",
			"cf_s_hand_R/cf_j_middle01_R",
			"cf_j_middle01_R/cf_j_middle02_R",
			"cf_j_middle02_R/cf_j_middle03_R",
			"cf_s_hand_R/cf_j_ring01_R",
			"cf_j_ring01_R/cf_j_ring02_R",
			"cf_j_ring02_R/cf_j_ring03_R",
			"cf_s_hand_R/cf_j_little01_R",
			"cf_j_little01_R/cf_j_little02_R",
			"cf_j_little02_R/cf_j_little03_R",
			"cf_j_hips/cf_j_waist01",
			"cf_j_waist01/cf_j_waist02",
			"cf_j_waist02/cf_j_thigh00_L",
			"cf_j_thigh00_L/cf_j_leg01_L",
			"cf_j_leg01_L/cf_j_leg03_L",
			"cf_j_leg03_L/cf_j_foot_L",
			"cf_j_foot_L/cf_j_toes_L",
			"cf_j_waist02/cf_j_thigh00_R",
			"cf_j_thigh00_R/cf_j_leg01_R",
			"cf_j_leg01_R/cf_j_leg03_R",
			"cf_j_leg03_R/cf_j_foot_R",
			"cf_j_foot_R/cf_j_toes_R",
			"_dummy_root/_dummy_ik_root_L",
			"_dummy_ik_root_L/_FOOT_IK_L",
			"_FOOT_IK_L/_TOE_IK_L",
			"_dummy_root/_dummy_ik_root_R",
			"_dummy_ik_root_R/_FOOT_IK_R",
			"_FOOT_IK_R/_TOE_IK_R",
			"_dummy_root/_dummy_etc",
			"_dummy_etc/_dummy_EYE_LR",
			"_dummy_EYE_LR/_dummy_EYE_L",
			"_dummy_EYE_LR/_dummy_EYE_R"
		};

		
		private string[] _shoulderKey;

		
		private string[] _armUpKey;

		
		private string[] _armLowKey;

		
		public BoneAdjustment lastModifiedAdjustment;

		
		public string lastAdjustedBone;

		
		public VMDAnimationController.QuickAdjust quickAdjust;

		
		private bool quickAdjustBoneInitialized;

		
		public Dictionary<string, BoneAdjustment> boneAdjust = new Dictionary<string, BoneAdjustment>();

		
		public VMDAnimationController.IKWeightData IKWeight = new VMDAnimationController.IKWeightData();

		
		private const string FACE_GO_NAME = "_face";

		
		public KKAnimeFaceController faceController;

		
		public bool enableEyeControl;

		
		private Transform t_dummy_EYE_LR;

		
		private Transform t_dummy_EYE_L;

		
		private Transform t_dummy_EYE_R;

		
		private static FieldInfo f_referenceLookDir = typeof(EyeObject).GetField("referenceLookDir", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

		
		private static FieldInfo f_referenceUpDir = typeof(EyeObject).GetField("referenceUpDir", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

		
		private static FieldInfo f_angleH = typeof(EyeObject).GetField("angleH", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

		
		private static FieldInfo f_angleV = typeof(EyeObject).GetField("angleV", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

		
		private static MethodInfo m_AngleHRateCalc = typeof(EyeLookCalc).GetMethod("AngleHRateCalc", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod);

		
		private static MethodInfo m_AngleVRateCalc = typeof(EyeLookCalc).GetMethod("AngleVRateCalc", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod);

		
		private AutoAdjustSkirtBone skirtBoneAdjust;

		
		private bool adjustSkirtBones;

		
		private bool vmdAnimEnabled;

		
		private bool oldIKEnabled;

		
		public class AutoTrack : MonoBehaviour
		{
			
			private void LateUpdate()
			{
				try
				{
					if (this.controller.VMDAnimEnabled)
					{
						if (this.map != null)
						{
							this.controller.t_n_height.localScale = this.controller.t_n_height_orig.localScale;
							Vector3 position = this.controller.t_dummy_hips.position;
							if (float.IsNaN(position.x) || float.IsNaN(position.y) || float.IsNaN(position.z))
							{
								Vector3 vector;
                                vector = new Vector3(10000f, 10000f, 10000f);
								this.controller.t_hips_orig.position = vector;
                                KKVMDPlugin.Logger.Log(BepInEx.Logging.LogLevel.Error, string.Format("Position is NaN value. Temporarily move hips to ({0}, {1}, {2}).", vector.x, vector.y, vector.z));
							}
							else
							{
								this.controller.AdjustMoveBonePosition();
								try
								{
									this.controller.ProcessFootIK();
									this.controller.ProcessEyeLook();
									foreach (GameObject gameObject in this.map.Keys)
									{
										GameObject gameObject2 = this.map[gameObject];
										gameObject2.transform.rotation = gameObject.transform.rotation;
										gameObject2.transform.position = gameObject.transform.position;
									}
									if (this.controller.AdjustSkirtBones)
									{
										this.controller.skirtBoneAdjust.UpdateSkirtBones();
									}
								}
								finally
								{
									this.controller.RestoreAdjustedMoveBonePosition();
								}
							}
						}
					}
				}
				catch (Exception value)
				{
					Console.WriteLine(value);
				}
			}

			
			public Dictionary<GameObject, GameObject> map;

			
			internal VMDAnimationController controller;
		}

		
		public class QuickAdjust
		{
			
			public QuickAdjust(VMDAnimationController mgr)
			{
				this.mgr = mgr;
			}

			
			public void Set()
			{
				try
				{
					if (this.mgr.quickAdjustBoneInitialized)
					{
						this.mgr.boneAdjust[this.mgr._shoulderKey[0]].SetRotAdjustment(new Vector3(0f, 0f, this._shoulder));
						this.mgr.boneAdjust[this.mgr._shoulderKey[1]].SetRotAdjustment(new Vector3(0f, 0f, -1f * this._shoulder));
						this.mgr.boneAdjust[this.mgr._armUpKey[0]].SetRotAdjustment(new Vector3(0f, 0f, this._armUp));
						this.mgr.boneAdjust[this.mgr._armUpKey[1]].SetRotAdjustment(new Vector3(0f, 0f, -1f * this._armUp));
						this.mgr.boneAdjust[this.mgr._armLowKey[0]].SetRotAdjustment(new Vector3(0f, 0f, this._armLow));
						this.mgr.boneAdjust[this.mgr._armLowKey[1]].SetRotAdjustment(new Vector3(0f, 0f, -1f * this._armLow));
						this.mgr.boneAdjust[this.mgr._armUpKey[0]].SetAxisAdjustment(new Vector3(0f, 0f, -1f * this._shoulder));
						this.mgr.boneAdjust[this.mgr._armUpKey[1]].SetAxisAdjustment(new Vector3(0f, 0f, this._shoulder));
						this.mgr.boneAdjust[this.mgr._armLowKey[0]].SetAxisAdjustment(new Vector3(0f, 0f, -1f * this._armUp));
						this.mgr.boneAdjust[this.mgr._armLowKey[1]].SetAxisAdjustment(new Vector3(0f, 0f, this._armUp));
						this.mgr.scale = this.mgr.scaleBase * this._scaleModel;
					}
				}
				catch (Exception value)
				{
					Console.WriteLine(value);
				}
			}

			
			
			
			public float Shoulder
			{
				get
				{
					return this._shoulder;
				}
				set
				{
					if (this._shoulder != value)
					{
						this._shoulder = value;
						this.Set();
					}
				}
			}

			
			
			
			public float ArmUp
			{
				get
				{
					return this._armUp;
				}
				set
				{
					if (this._armUp != value)
					{
						this._armUp = value;
						this.Set();
					}
				}
			}

			
			
			
			public float ArmLow
			{
				get
				{
					return this._armLow;
				}
				set
				{
					if (this._armLow != value)
					{
						this._armLow = value;
						this.Set();
					}
				}
			}

			
			
			
			public float ScaleModel
			{
				get
				{
					return this._scaleModel;
				}
				set
				{
					if (this._scaleModel != value)
					{
						this._scaleModel = value;
						this.Set();
					}
				}
			}

			
			private VMDAnimationController mgr;

			
			private float _shoulder = 12f;

			
			private float _armUp = 30f;

			
			private float _armLow;

			
			private float _scaleModel = 1f;
		}

		
		public class IKWeightData
		{
			
			public bool disableToeIK;

			
			public float footIKPosWeight = 1f;

			
			public float footIKRotWeight;

			
			public int footIteration = 40;

			
			public float footControlWeight = 1.98967528f;

			
			public int toeIteration = 3;

			
			public float toeControlWeight = 4.18879032f;

			
			public float minDelta;

			
			public bool useHeelAsFootIKTarget;

			
			public float lastFrameRefRate = 0.9f;

			
			public bool useLegSolver = true;

			
			public bool limitIKPositionFloor = true;

			
			public float minKneeRot = 5f;
		}

		
		public class DestroyListener : MonoBehaviour
		{
			
			private void OnDestroy()
			{
				VMDAnimationMgr.Instance.gui.Clear();
				Console.WriteLine("Destroy VMD Controller. {0}", this.controller);
				UnityEngine.Object.Destroy(this.controller);
			}

			
			public VMDAnimationController controller;
		}
	}
}
