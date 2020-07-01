using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using MMD.VMD;
using UnityEngine;

namespace KKVMDPlayPlugin
{
	
	public class VMDHSConverter
	{
		
		public VMDHSConverter(Dictionary<string, string> boneNameMap, ModelBaselineData hsModelBaseline, Dictionary<string, BoneAdjustment> boneAdjustment, Vector3 scale, Vector3 centerBasePos, Vector3 hipPositionAdjust, string faceGoPath, HashSet<string> faceNames)
		{
			this.HSModelBaseline = hsModelBaseline;
			this.boneNameMap = boneNameMap;
			this.boneAdjustment = boneAdjustment;
			this.centerBasePos = centerBasePos;
			this.hipCenterDiff = hipPositionAdjust;
			this.scale = scale;
			this.faceGoPath = faceGoPath;
			this.faceNames = new HashSet<string>(faceNames);
			this.usedFaceNames = new HashSet<string>();
		}

		
		public VMDHSConverter(Dictionary<string, BoneAdjustment> boneAdjustment, float scale)
		{
			this.boneAdjustment = boneAdjustment;
			this.scale = Vector3.one * scale;
		}

		
		public AnimationClip CreateAnimationClip(VMDFormat format, GameObject assign_pmd, int interpolationQuality)
		{
			AnimationClip animationClip = new AnimationClip();
			animationClip.name = format.clip_name;
			if (VMDHSConverter.p_legacy != null)
			{
				VMDHSConverter.p_legacy.SetValue(animationClip, true, null);
			}
			Dictionary<string, string> dic = new Dictionary<string, string>();
			this.FullSearchBonePath(assign_pmd.transform, assign_pmd.transform, dic);
			Dictionary<string, GameObject> obj = new Dictionary<string, GameObject>();
			this.FullEntryBoneAnimation(format, animationClip, dic, obj, interpolationQuality);
			this.CreateKeysForSkin(format, animationClip);
			this.ReadIKModeSettings(format);
			return animationClip;
		}

		
		private static Vector2 GetBezierHandle(byte[] interpolation, int type, int ab)
		{
			return new Vector2((float)interpolation[ab * 8 + type], (float)interpolation[ab * 8 + 4 + type]) / 127f;
		}

		
		private static Vector2 SampleBezier(Vector2 bezierHandleA, Vector2 bezierHandleB, float t)
		{
			Vector2 zero = Vector2.zero;
			Vector2 vector;
            vector = new Vector2(1f, 1f);
			Vector2 vector2 = Vector2.Lerp(zero, bezierHandleA, t);
			Vector2 vector3 = Vector2.Lerp(bezierHandleA, bezierHandleB, t);
			Vector2 vector4 = Vector2.Lerp(bezierHandleB, vector, t);
			Vector2 vector5 = Vector2.Lerp(vector2, vector3, t);
			Vector2 vector6 = Vector2.Lerp(vector3, vector4, t);
			return Vector2.Lerp(vector5, vector6, t);
		}

		
		private static bool IsLinear(byte[] interpolation, int type)
		{
			byte b = interpolation[type];
			byte b2 = interpolation[4 + type];
			byte b3 = interpolation[8 + type];
			byte b4 = interpolation[12 + type];
			return b == b2 && b3 == b4;
		}

		
		private int GetKeyframeCount(List<VMDFormat.Motion> mlist, int type, int interpolationQuality)
		{
			int num = 0;
			for (int i = 0; i < mlist.Count; i++)
			{
				if (i > 0 && !VMDHSConverter.IsLinear(mlist[i].interpolation, type))
				{
					num += interpolationQuality;
				}
				else
				{
					num++;
				}
			}
			return num;
		}

		
		private void AddDummyKeyframe(ref Keyframe[] keyframes)
		{
			if (keyframes.Length == 1)
			{
				Keyframe[] array = new Keyframe[]
				{
					keyframes[0],
					keyframes[0]
				};
				Keyframe[] array2 = array;
				int num = 1;
				array2[num].time = array2[num].time + 1.66666669E-05f;
				array[0].outTangent = 0f;
				array[1].inTangent = 0f;
				keyframes = array;
			}
		}

		
		private float GetLinearTangentForPosition(Keyframe from_keyframe, Keyframe to_keyframe)
		{
			return (to_keyframe.value - from_keyframe.value) / (to_keyframe.time - from_keyframe.time);
		}

		
		private float Mod360(float angle)
		{
			if (angle >= 0f)
			{
				return angle;
			}
			return angle + 360f;
		}

		
		private float GetLinearTangentForRotation(Keyframe from_keyframe, Keyframe to_keyframe)
		{
			float num = this.Mod360(to_keyframe.value);
			float num2 = this.Mod360(from_keyframe.value);
			float num3 = this.Mod360(num - num2);
			if (num3 < 180f)
			{
				return num3 / (to_keyframe.time - from_keyframe.time);
			}
			return (num3 - 360f) / (to_keyframe.time - from_keyframe.time);
		}

		
		private Dictionary<string, Keyframe[]> ToKeyframesForRotation(VMDHSConverter.QuaternionKeyframe[] custom_keys, ref Keyframe[] rx_keys, ref Keyframe[] ry_keys, ref Keyframe[] rz_keys, bool forceInfinitTangent = false)
		{
			rx_keys = new Keyframe[custom_keys.Length];
			ry_keys = new Keyframe[custom_keys.Length];
			rz_keys = new Keyframe[custom_keys.Length];
			Keyframe[] array = new Keyframe[custom_keys.Length];
			Keyframe[] array2 = new Keyframe[custom_keys.Length];
			Keyframe[] array3 = new Keyframe[custom_keys.Length];
			Keyframe[] array4 = new Keyframe[custom_keys.Length];
			Dictionary<string, Keyframe[]> dictionary = new Dictionary<string, Keyframe[]>();
			Quaternion quaternion = Quaternion.identity;
			for (int i = 0; i < custom_keys.Length; i++)
			{
				Vector3 eulerAngles = custom_keys[i].value.eulerAngles;
				rx_keys[i] = new Keyframe(custom_keys[i].time, eulerAngles.x);
				ry_keys[i] = new Keyframe(custom_keys[i].time, eulerAngles.y);
				rz_keys[i] = new Keyframe(custom_keys[i].time, eulerAngles.z);
				rx_keys[i].tangentMode = 21;
				ry_keys[i].tangentMode = 21;
				rz_keys[i].tangentMode = 21;
				if (i > 0)
				{
					float linearTangentForRotation = this.GetLinearTangentForRotation(rx_keys[i - 1], rx_keys[i]);
					float linearTangentForRotation2 = this.GetLinearTangentForRotation(ry_keys[i - 1], ry_keys[i]);
					float linearTangentForRotation3 = this.GetLinearTangentForRotation(rz_keys[i - 1], rz_keys[i]);
					rx_keys[i - 1].outTangent = linearTangentForRotation;
					ry_keys[i - 1].outTangent = linearTangentForRotation2;
					rz_keys[i - 1].outTangent = linearTangentForRotation3;
					rx_keys[i].inTangent = linearTangentForRotation;
					ry_keys[i].inTangent = linearTangentForRotation2;
					rz_keys[i].inTangent = linearTangentForRotation3;
				}
				Quaternion quaternion2 = Quaternion.Euler(eulerAngles);
				if (i > 0 && !forceInfinitTangent)
				{
					quaternion2 = Quaternion.Slerp(quaternion, quaternion2, 0.9999f);
				}
				float time = custom_keys[i].time;
				array[i] = new Keyframe(time, quaternion2.x);
				array2[i] = new Keyframe(time, quaternion2.y);
				array3[i] = new Keyframe(time, quaternion2.z);
				array4[i] = new Keyframe(time, quaternion2.w);
				array[i].tangentMode = 21;
				array2[i].tangentMode = 21;
				array3[i].tangentMode = 21;
				array4[i].tangentMode = 21;
				if (i > 0)
				{
					if (forceInfinitTangent)
					{
						array[i].tangentMode = 21;
						array2[i].tangentMode = 21;
						array3[i].tangentMode = 21;
						array4[i].tangentMode = 21;
						array[i - 1].outTangent = float.PositiveInfinity;
						array2[i - 1].outTangent = float.PositiveInfinity;
						array3[i - 1].outTangent = float.PositiveInfinity;
						array4[i - 1].outTangent = float.PositiveInfinity;
						array[i].inTangent = float.PositiveInfinity;
						array2[i].inTangent = float.PositiveInfinity;
						array3[i].inTangent = float.PositiveInfinity;
						array4[i].inTangent = float.PositiveInfinity;
					}
					else
					{
						float linearTangentForPosition = this.GetLinearTangentForPosition(array[i - 1], array[i]);
						float linearTangentForPosition2 = this.GetLinearTangentForPosition(array2[i - 1], array2[i]);
						float linearTangentForPosition3 = this.GetLinearTangentForPosition(array3[i - 1], array3[i]);
						float linearTangentForPosition4 = this.GetLinearTangentForPosition(array4[i - 1], array4[i]);
						array[i - 1].outTangent = linearTangentForPosition;
						array2[i - 1].outTangent = linearTangentForPosition2;
						array3[i - 1].outTangent = linearTangentForPosition3;
						array4[i - 1].outTangent = linearTangentForPosition4;
						array[i].inTangent = linearTangentForPosition;
						array2[i].inTangent = linearTangentForPosition2;
						array3[i].inTangent = linearTangentForPosition3;
						array4[i].inTangent = linearTangentForPosition4;
					}
				}
				quaternion = quaternion2;
			}
			this.AddDummyKeyframe(ref rx_keys);
			this.AddDummyKeyframe(ref ry_keys);
			this.AddDummyKeyframe(ref rz_keys);
			this.AddDummyKeyframe(ref array);
			this.AddDummyKeyframe(ref array2);
			this.AddDummyKeyframe(ref array3);
			this.AddDummyKeyframe(ref array4);
			dictionary.Add("localRotation.x", array);
			dictionary.Add("localRotation.y", array2);
			dictionary.Add("localRotation.z", array3);
			dictionary.Add("localRotation.w", array4);
			return dictionary;
		}

		
		private void CreateKeysForRotation(VMDFormat format, AnimationClip clip, string current_bone, string bone_path, int interpolationQuality)
		{
			try
			{
				if (this.boneNameMap.ContainsKey(current_bone))
				{
					string text = this.boneNameMap[current_bone];
					List<VMDFormat.Motion> list;
					if (!format.motion_list.motion.ContainsKey(text))
					{
						Console.WriteLine("bone {0} not found in motionlist", text);
						Console.WriteLine("Add dummy rotation frame (rot 0,0,0) for the bone {0}", text);
						list = new List<VMDFormat.Motion>();
						VMDFormat.Motion item = new VMDFormat.Motion(text, 0u, Vector3.zero, Quaternion.identity, new byte[64]);
						list.Add(item);
					}
					else
					{
						list = format.motion_list.motion[text];
					}
					Dictionary<string, AnimationCurve> dictionary = null;
					BoneAdjustment boneAdjustment = null;
					if (this.boneAdjustment.ContainsKey(current_bone))
					{
						boneAdjustment = this.boneAdjustment[current_bone];
					}
					VMDHSConverter.QuaternionKeyframe[] custom_keys = new VMDHSConverter.QuaternionKeyframe[this.GetKeyframeCount(list, 3, interpolationQuality)];
					VMDHSConverter.QuaternionKeyframe prev_keyframe = null;
					int num = 0;
					Quaternion quaternion = Quaternion.identity;
					for (int i = 0; i < list.Count; i++)
					{
						float num2 = list[i].flame_no * 0.0333333351f;
						Quaternion quaternion2 = list[i].rotation;
						if (dictionary != null)
						{
							AnimationCurve animationCurve = null;
							dictionary.TryGetValue("localEulerAngles.x", out animationCurve);
							AnimationCurve animationCurve2 = null;
							dictionary.TryGetValue("localEulerAngles.y", out animationCurve2);
							AnimationCurve animationCurve3 = null;
							dictionary.TryGetValue("localEulerAngles.z", out animationCurve3);
							Quaternion quaternion3 = Quaternion.identity;
							if (animationCurve != null && animationCurve2 != null && animationCurve3 != null)
							{
								quaternion3 = Quaternion.Euler(animationCurve.Evaluate(num2), animationCurve2.Evaluate(num2), animationCurve3.Evaluate(num2));
								quaternion2 *= quaternion3;
							}
						}
						if (boneAdjustment != null)
						{
							quaternion2 = boneAdjustment.GetAdjustedRotation(quaternion2);
						}
						if (i != 0)
						{
							quaternion2 = Quaternion.Slerp(quaternion, quaternion2, 0.99999f);
						}
						VMDHSConverter.QuaternionKeyframe quaternionKeyframe = new VMDHSConverter.QuaternionKeyframe(num2, quaternion2);
						VMDHSConverter.QuaternionKeyframe.AddBezierKeyframes(list[i].interpolation, 3, prev_keyframe, quaternionKeyframe, interpolationQuality, ref custom_keys, ref num);
						prev_keyframe = quaternionKeyframe;
						quaternion = quaternion2;
					}
					Keyframe[] array = null;
					Keyframe[] array2 = null;
					Keyframe[] array3 = null;
					Dictionary<string, Keyframe[]> dictionary2 = this.ToKeyframesForRotation(custom_keys, ref array, ref array2, ref array3, false);
					new AnimationCurve(array);
					new AnimationCurve(array2);
					new AnimationCurve(array3);
					AnimationCurve animationCurve4 = new AnimationCurve(dictionary2["localRotation.x"]);
					AnimationCurve animationCurve5 = new AnimationCurve(dictionary2["localRotation.y"]);
					AnimationCurve animationCurve6 = new AnimationCurve(dictionary2["localRotation.z"]);
					AnimationCurve animationCurve7 = new AnimationCurve(dictionary2["localRotation.w"]);
					clip.SetCurve(bone_path, typeof(Transform), "localRotation.x", animationCurve4);
					clip.SetCurve(bone_path, typeof(Transform), "localRotation.y", animationCurve5);
					clip.SetCurve(bone_path, typeof(Transform), "localRotation.z", animationCurve6);
					clip.SetCurve(bone_path, typeof(Transform), "localRotation.w", animationCurve7);
					if (text == "センター")
					{
						this.centerXCurve = animationCurve4;
						this.centerYCurve = animationCurve5;
						this.centerZCurve = animationCurve6;
						this.centerWCurve = animationCurve7;
					}
				}
			}
			catch (KeyNotFoundException)
			{
			}
		}

		
		private Keyframe[] ToKeyframesForLocation(VMDHSConverter.FloatKeyframe[] custom_keys)
		{
			Keyframe[] array = new Keyframe[custom_keys.Length];
			for (int i = 0; i < custom_keys.Length; i++)
			{
				array[i] = new Keyframe(custom_keys[i].time, custom_keys[i].value);
				array[i].tangentMode = 21;
				if (i > 0)
				{
					float linearTangentForPosition = this.GetLinearTangentForPosition(array[i - 1], array[i]);
					array[i - 1].outTangent = linearTangentForPosition;
					array[i].inTangent = linearTangentForPosition;
				}
			}
			this.AddDummyKeyframe(ref array);
			return array;
		}

		
		private Keyframe[] ToKeyframesForLocation(VMDHSConverter.FloatLinearKeyframe[] custom_keys)
		{
			Keyframe[] array = new Keyframe[custom_keys.Length];
			for (int i = 0; i < custom_keys.Length; i++)
			{
				array[i] = new Keyframe(custom_keys[i].time, custom_keys[i].value);
				array[i].tangentMode = 21;
				if (i > 0)
				{
					float linearTangentForPosition = this.GetLinearTangentForPosition(array[i - 1], array[i]);
					array[i - 1].outTangent = linearTangentForPosition;
					array[i].inTangent = linearTangentForPosition;
				}
			}
			this.AddDummyKeyframe(ref array);
			return array;
		}

		
		private void CreateKeysForLocation(VMDFormat format, AnimationClip clip, string current_bone, string bone_path, int interpolationQuality, GameObject current_obj = null)
		{
			try
			{
				if (this.boneNameMap.ContainsKey(current_bone))
				{
					string text = this.boneNameMap[current_bone];
					if (!format.motion_list.motion.ContainsKey(text))
					{
						Console.WriteLine("bone {0} not found in motionlist", text);
					}
					else
					{
						HashSet<string> hashSet = new HashSet<string>
						{
							"全ての親",
							"センター",
							"グルーブ",
							"右足IK親",
							"右足ＩＫ",
							"右つま先ＩＫ",
							"左足IK親",
							"左足ＩＫ",
							"左つま先ＩＫ"
						};
						Vector3 vector = Vector3.zero;
						List<VMDFormat.Motion> list = format.motion_list.motion[text];
						this.GetKeyframeCount(list, 0, interpolationQuality);
						this.GetKeyframeCount(list, 1, interpolationQuality);
						this.GetKeyframeCount(list, 2, interpolationQuality);
						List<VMDHSConverter.FloatKeyframe> list2 = new List<VMDHSConverter.FloatKeyframe>();
						List<VMDHSConverter.FloatKeyframe> list3 = new List<VMDHSConverter.FloatKeyframe>();
						List<VMDHSConverter.FloatKeyframe> list4 = new List<VMDHSConverter.FloatKeyframe>();
						VMDHSConverter.FloatKeyframe prev_keyframe = null;
						VMDHSConverter.FloatKeyframe prev_keyframe2 = null;
						VMDHSConverter.FloatKeyframe prev_keyframe3 = null;
						int num = 0;
						int num2 = 0;
						int num3 = 0;
						uint num4 = uint.MaxValue;
						for (int i = 0; i < list.Count; i++)
						{
							float num5 = list[i].flame_no * 0.0333333351f;
							if (num4 > list[i].flame_no)
							{
								num4 = list[i].flame_no;
							}
							Vector3 vector2 = list[i].location;
							if (!(vector2 == Vector3.zero) || hashSet.Contains(text))
							{
								vector2.z = -vector2.z;
								vector2.x = -vector2.x;
								if (float.IsNaN(vector2.x) || float.IsNaN(vector2.y) || float.IsNaN(vector2.z))
								{
                                    KKVMDPlugin.Logger.Log(BepInEx.Logging.LogLevel.Debug, string.Format("position value is NaN. Treat as / 100 of max. {0}", num5));
                                    vector2 = new Vector3(3.40282359E+36f, 3.40282359E+36f, 3.40282359E+36f);
								}
								else if (vector2.magnitude > 1E+07f)
								{
                                    KKVMDPlugin.Logger.Log(BepInEx.Logging.LogLevel.Debug, string.Format("position value is too large. Treat as / 100 to avoid clipping. {0}, ({1} {2} {3})", new object[]
									{
										num5,
										vector2.x,
										vector2.y,
										vector2.z
									}));
									vector2 /= 100f;
								}
								if (text == "センター")
								{
									vector = this.HSModelBaseline.centerBasePos;
								}
								if (text == "グルーブ")
								{
									vector = this.HSModelBaseline.grooveBasePos;
								}
								if (text == "左足IK親")
								{
									vector = this.HSModelBaseline.leftIKCenterPos;
								}
								else if (text == "右足IK親")
								{
									vector = this.HSModelBaseline.rightIKCenterPos;
								}
								if (text == "右足ＩＫ")
								{
									vector = this.HSModelBaseline.rightFootPos;
								}
								else if (text == "左足ＩＫ")
								{
									vector = this.HSModelBaseline.leftFootPos;
								}
								if (text == "右つま先ＩＫ")
								{
									vector = this.HSModelBaseline.rightToePosRel;
								}
								else if (text == "左つま先ＩＫ")
								{
									vector = this.HSModelBaseline.leftToePosRel;
								}
								VMDHSConverter.FloatKeyframe floatKeyframe = new VMDHSConverter.FloatKeyframe(num5, vector2.x * this.scale.x + vector.x);
								VMDHSConverter.FloatKeyframe floatKeyframe2 = new VMDHSConverter.FloatKeyframe(num5, vector2.y * this.scale.y + vector.y);
								VMDHSConverter.FloatKeyframe floatKeyframe3 = new VMDHSConverter.FloatKeyframe(num5, vector2.z * this.scale.z + vector.z);
								VMDHSConverter.FloatKeyframe.AddBezierKeyframes(list[i].interpolation, 0, prev_keyframe, floatKeyframe, interpolationQuality, ref list2, ref num);
								VMDHSConverter.FloatKeyframe.AddBezierKeyframes(list[i].interpolation, 1, prev_keyframe2, floatKeyframe2, interpolationQuality, ref list3, ref num2);
								VMDHSConverter.FloatKeyframe.AddBezierKeyframes(list[i].interpolation, 2, prev_keyframe3, floatKeyframe3, interpolationQuality, ref list4, ref num3);
								prev_keyframe = floatKeyframe;
								prev_keyframe2 = floatKeyframe2;
								prev_keyframe3 = floatKeyframe3;
							}
						}
						VMDHSConverter.FloatKeyframe[] custom_keys = list2.ToArray();
						VMDHSConverter.FloatKeyframe[] custom_keys2 = list3.ToArray();
						VMDHSConverter.FloatKeyframe[] custom_keys3 = list4.ToArray();
						if (list.Count != 0)
						{
							if (hashSet.Contains(text))
							{
								AnimationCurve animationCurve = new AnimationCurve(this.ToKeyframesForLocation(custom_keys));
								AnimationCurve animationCurve2 = new AnimationCurve(this.ToKeyframesForLocation(custom_keys2));
								AnimationCurve animationCurve3 = new AnimationCurve(this.ToKeyframesForLocation(custom_keys3));
								clip.SetCurve(bone_path, typeof(Transform), "localPosition.x", animationCurve);
								clip.SetCurve(bone_path, typeof(Transform), "localPosition.y", animationCurve2);
								clip.SetCurve(bone_path, typeof(Transform), "localPosition.z", animationCurve3);
							}
							else
							{
                                KKVMDPlugin.Logger.Log(BepInEx.Logging.LogLevel.Error, string.Format("Location data for non movable bone {0} found. Ignore.", text));
							}
						}
					}
				}
			}
			catch (KeyNotFoundException)
			{
			}
		}

		
		private Dictionary<string, AnimationCurve> CollectCurve(VMDFormat format, string boneName, int interpolationQuality)
		{
			if (!format.motion_list.motion.ContainsKey(boneName))
			{
				return null;
			}
			Dictionary<string, AnimationCurve> dictionary = new Dictionary<string, AnimationCurve>();
			List<VMDFormat.Motion> list = format.motion_list.motion[boneName];
			Vector3 zero = Vector3.zero;
			this.GetKeyframeCount(list, 0, interpolationQuality);
			this.GetKeyframeCount(list, 1, interpolationQuality);
			this.GetKeyframeCount(list, 2, interpolationQuality);
			List<VMDHSConverter.FloatKeyframe> list2 = new List<VMDHSConverter.FloatKeyframe>();
			List<VMDHSConverter.FloatKeyframe> list3 = new List<VMDHSConverter.FloatKeyframe>();
			List<VMDHSConverter.FloatKeyframe> list4 = new List<VMDHSConverter.FloatKeyframe>();
			VMDHSConverter.FloatKeyframe prev_keyframe = null;
			VMDHSConverter.FloatKeyframe prev_keyframe2 = null;
			VMDHSConverter.FloatKeyframe prev_keyframe3 = null;
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			for (int i = 0; i < list.Count; i++)
			{
				float time = list[i].flame_no * 0.0333333351f;
				VMDHSConverter.FloatKeyframe floatKeyframe = new VMDHSConverter.FloatKeyframe(time, list[i].location.x * this.scale.x + zero.x);
				VMDHSConverter.FloatKeyframe floatKeyframe2 = new VMDHSConverter.FloatKeyframe(time, list[i].location.y * this.scale.y + zero.y);
				VMDHSConverter.FloatKeyframe floatKeyframe3 = new VMDHSConverter.FloatKeyframe(time, list[i].location.z * this.scale.z + zero.z);
				VMDHSConverter.FloatKeyframe.AddBezierKeyframes(list[i].interpolation, 0, prev_keyframe, floatKeyframe, interpolationQuality, ref list2, ref num);
				VMDHSConverter.FloatKeyframe.AddBezierKeyframes(list[i].interpolation, 1, prev_keyframe2, floatKeyframe2, interpolationQuality, ref list3, ref num2);
				VMDHSConverter.FloatKeyframe.AddBezierKeyframes(list[i].interpolation, 2, prev_keyframe3, floatKeyframe3, interpolationQuality, ref list4, ref num3);
				prev_keyframe = floatKeyframe;
				prev_keyframe2 = floatKeyframe2;
				prev_keyframe3 = floatKeyframe3;
			}
			if (list.Count != 0)
			{
				VMDHSConverter.FloatKeyframe[] custom_keys = list2.ToArray();
				VMDHSConverter.FloatKeyframe[] custom_keys2 = list3.ToArray();
				VMDHSConverter.FloatKeyframe[] custom_keys3 = list4.ToArray();
				AnimationCurve value = new AnimationCurve(this.ToKeyframesForLocation(custom_keys));
				AnimationCurve value2 = new AnimationCurve(this.ToKeyframesForLocation(custom_keys2));
				AnimationCurve value3 = new AnimationCurve(this.ToKeyframesForLocation(custom_keys3));
				dictionary.Add("localPosition.x", value);
				dictionary.Add("localPosition.y", value2);
				dictionary.Add("localPosition.z", value3);
			}
			VMDHSConverter.QuaternionKeyframe[] custom_keys4 = new VMDHSConverter.QuaternionKeyframe[this.GetKeyframeCount(list, 3, interpolationQuality)];
			VMDHSConverter.QuaternionKeyframe prev_keyframe4 = null;
			int num4 = 0;
			for (int j = 0; j < list.Count; j++)
			{
				float time2 = list[j].flame_no * 0.0333333351f;
				Quaternion rotation = list[j].rotation;
				VMDHSConverter.QuaternionKeyframe quaternionKeyframe = new VMDHSConverter.QuaternionKeyframe(time2, rotation);
				VMDHSConverter.QuaternionKeyframe.AddBezierKeyframes(list[j].interpolation, 3, prev_keyframe4, quaternionKeyframe, interpolationQuality, ref custom_keys4, ref num4);
				prev_keyframe4 = quaternionKeyframe;
			}
			Keyframe[] array = null;
			Keyframe[] array2 = null;
			Keyframe[] array3 = null;
			this.ToKeyframesForRotation(custom_keys4, ref array, ref array2, ref array3, false);
			AnimationCurve value4 = new AnimationCurve(array);
			AnimationCurve value5 = new AnimationCurve(array2);
			AnimationCurve value6 = new AnimationCurve(array3);
			dictionary.Add("localEulerAngles.x", value4);
			dictionary.Add("localEulerAngles.y", value5);
			dictionary.Add("localEulerAngles.z", value6);
			return dictionary;
		}

		
		private void CreateKeysForSkin(VMDFormat format, AnimationClip clip)
		{
			this.usedFaceNames = new HashSet<string>();
			foreach (KeyValuePair<string, List<VMDFormat.SkinData>> keyValuePair in format.skin_list.skin)
			{
				string key = keyValuePair.Key;
				if (!this.faceNames.Contains(key))
				{
					Console.WriteLine("Face Setting for {0} not found. Ignore this face morph.", key);
				}
				else
				{
					List<VMDFormat.SkinData> value = keyValuePair.Value;
					Keyframe[] array = new Keyframe[keyValuePair.Value.Count];
					for (int i = 0; i < keyValuePair.Value.Count; i++)
					{
						array[i] = new Keyframe(value[i].flame_no * 0.0333333351f, value[i].weight);
						array[i].tangentMode = 21;
						if (i > 0)
						{
							float linearTangentForPosition = this.GetLinearTangentForPosition(array[i - 1], array[i]);
							array[i - 1].outTangent = linearTangentForPosition;
							array[i].inTangent = linearTangentForPosition;
						}
					}
					this.AddDummyKeyframe(ref array);
					AnimationCurve animationCurve = new AnimationCurve(array);
					clip.SetCurve(this.faceGoPath + "/" + key, typeof(Transform), "localPosition.z", animationCurve);
					this.usedFaceNames.Add(key);
				}
			}
		}

		
		private string GetBonePath(Transform transform, Transform root)
		{
			if (transform == root)
			{
				return "";
			}
			if (transform.parent == root)
			{
				return transform.name;
			}
			string bonePath = this.GetBonePath(transform.parent, root);
			return bonePath + "/" + transform.name;
		}

		
		private void FullSearchBonePath(Transform root, Transform transform, Dictionary<string, string> dic)
		{
			int childCount = transform.childCount;
			for (int i = 0; i < childCount; i++)
			{
				Transform child = transform.GetChild(i);
				this.FullSearchBonePath(root, child, dic);
			}
			string bonePath = this.GetBonePath(transform, root);
			if (dic.ContainsKey(transform.name))
			{
				string text = dic[transform.name];
				return;
			}
			dic.Add(transform.name, bonePath);
		}

		
		private void FullEntryBoneAnimation(VMDFormat format, AnimationClip clip, Dictionary<string, string> dic, Dictionary<string, GameObject> obj, int interpolationQuality)
		{
			foreach (KeyValuePair<string, string> keyValuePair in dic)
			{
				GameObject gameObject = null;
				if (obj.ContainsKey(keyValuePair.Key))
				{
					gameObject = obj[keyValuePair.Key];
					Rigidbody component = gameObject.GetComponent<Rigidbody>();
					if (component != null && !component.isKinematic)
					{
						continue;
					}
				}
				this.CreateKeysForLocation(format, clip, keyValuePair.Key, keyValuePair.Value, interpolationQuality, gameObject);
				this.CreateKeysForRotation(format, clip, keyValuePair.Key, keyValuePair.Value, interpolationQuality);
			}
		}

		
		private void GetGameObjects(Dictionary<string, GameObject> obj, GameObject assign_pmd)
		{
			for (int i = 0; i < assign_pmd.transform.childCount; i++)
			{
				Transform child = assign_pmd.transform.GetChild(i);
				try
				{
					obj.Add(child.name, child.gameObject);
				}
				catch (ArgumentException ex)
				{
					Debug.Log(ex.Message);
					Debug.Log("An element with the same key already exists in the dictionary. -> " + child.name);
				}
				if (!(child == null))
				{
					this.GetGameObjects(obj, child.gameObject);
				}
			}
		}

		
		public AnimationClip CreateCameraAnimationClip(VMDFormat format, GameObject camera_root, int interpolationQuality)
		{
			AnimationClip animationClip = new AnimationClip();
			if (VMDHSConverter.p_legacy != null)
			{
				VMDHSConverter.p_legacy.SetValue(animationClip, true, null);
			}
			BoneAdjustment boneAdjustment = this.boneAdjustment["camera"];
			if (camera_root.transform.Find("camera") == null)
			{
				new GameObject("camera").transform.parent = camera_root.transform;
			}
			if (camera_root.transform.Find("length") == null)
			{
				new GameObject("length").transform.parent = camera_root.transform;
			}
			if (camera_root.transform.Find("view_angle") == null)
			{
				new GameObject("view_angle").transform.parent = camera_root.transform;
			}
			if (camera_root.transform.Find("perspective") == null)
			{
				new GameObject("perspective").transform.parent = camera_root.transform;
			}
			List<VMDHSConverter.FloatKeyframe> list = new List<VMDHSConverter.FloatKeyframe>();
			List<VMDHSConverter.FloatKeyframe> list2 = new List<VMDHSConverter.FloatKeyframe>();
			List<VMDHSConverter.FloatKeyframe> list3 = new List<VMDHSConverter.FloatKeyframe>();
			List<VMDHSConverter.FloatLinearKeyframe> list4 = new List<VMDHSConverter.FloatLinearKeyframe>();
			List<VMDHSConverter.FloatLinearKeyframe> list5 = new List<VMDHSConverter.FloatLinearKeyframe>();
			List<Keyframe> list6 = new List<Keyframe>();
			VMDHSConverter.FloatKeyframe prev_keyframe = null;
			VMDHSConverter.FloatKeyframe prev_keyframe2 = null;
			VMDHSConverter.FloatKeyframe prev_keyframe3 = null;
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			while ((long)num4 < (long)((ulong)format.camera_list.camera_count))
			{
				VMDFormat.CameraData cameraData = format.camera_list.camera[num4];
				float num5 = 0.0333333351f * cameraData.flame_no;
				Vector3 location = cameraData.location;
				location.z = -location.z;
				location.x = -location.x;
				VMDHSConverter.FloatKeyframe floatKeyframe = new VMDHSConverter.FloatKeyframe(num5, location.x * this.scale.x);
				VMDHSConverter.FloatKeyframe floatKeyframe2 = new VMDHSConverter.FloatKeyframe(num5, location.y * this.scale.y);
				VMDHSConverter.FloatKeyframe floatKeyframe3 = new VMDHSConverter.FloatKeyframe(num5, location.z * this.scale.z);
				VMDHSConverter.FloatLinearKeyframe item = new VMDHSConverter.FloatLinearKeyframe(num5, cameraData.length * this.scale.z);
				VMDHSConverter.FloatLinearKeyframe item2 = new VMDHSConverter.FloatLinearKeyframe(num5, cameraData.viewing_angle);
				VMDHSConverter.FloatKeyframe.AddBezierKeyframes(cameraData.interpolation, 0, prev_keyframe, floatKeyframe, interpolationQuality, ref list, ref num);
				VMDHSConverter.FloatKeyframe.AddBezierKeyframes(cameraData.interpolation, 1, prev_keyframe2, floatKeyframe2, interpolationQuality, ref list2, ref num2);
				VMDHSConverter.FloatKeyframe.AddBezierKeyframes(cameraData.interpolation, 2, prev_keyframe3, floatKeyframe3, interpolationQuality, ref list3, ref num3);
				list4.Add(item);
				list5.Add(item2);
				prev_keyframe = floatKeyframe;
				prev_keyframe2 = floatKeyframe2;
				prev_keyframe3 = floatKeyframe3;
				Keyframe item3;
				item3 = new Keyframe(num5, (float)cameraData.perspective);
				list6.Add(item3);
				num4++;
			}
			if (format.camera_list.camera_count != 0u)
			{
				VMDHSConverter.FloatKeyframe[] custom_keys = list.ToArray();
				VMDHSConverter.FloatKeyframe[] custom_keys2 = list2.ToArray();
				VMDHSConverter.FloatKeyframe[] custom_keys3 = list3.ToArray();
				VMDHSConverter.FloatLinearKeyframe[] custom_keys4 = list4.ToArray();
				VMDHSConverter.FloatLinearKeyframe[] custom_keys5 = list5.ToArray();
				AnimationCurve animationCurve = new AnimationCurve(this.ToKeyframesForLocation(custom_keys));
				AnimationCurve animationCurve2 = new AnimationCurve(this.ToKeyframesForLocation(custom_keys2));
				AnimationCurve animationCurve3 = new AnimationCurve(this.ToKeyframesForLocation(custom_keys3));
				AnimationCurve animationCurve4 = new AnimationCurve(this.ToKeyframesForLocation(custom_keys4));
				AnimationCurve animationCurve5 = new AnimationCurve(this.ToKeyframesForLocation(custom_keys5));
				Keyframe[] array = list6.ToArray();
				this.AddDummyKeyframe(ref array);
				AnimationCurve animationCurve6 = new AnimationCurve(array);
				animationClip.SetCurve("camera", typeof(Transform), "localPosition.x", animationCurve);
				animationClip.SetCurve("camera", typeof(Transform), "localPosition.y", animationCurve2);
				animationClip.SetCurve("camera", typeof(Transform), "localPosition.z", animationCurve3);
				animationClip.SetCurve("length", typeof(Transform), "localPosition.z", animationCurve4);
				animationClip.SetCurve("view_angle", typeof(Transform), "localPosition.z", animationCurve5);
				animationClip.SetCurve("perspective", typeof(Transform), "localPosition.z", animationCurve6);
			}
			List<VMDHSConverter.QuaternionKeyframe> list7 = new List<VMDHSConverter.QuaternionKeyframe>();
			Quaternion identity = Quaternion.identity;
			int num6 = 0;
			while ((long)num6 < (long)((ulong)format.camera_list.camera_count))
			{
				VMDFormat.CameraData cameraData2 = format.camera_list.camera[num6];
				float time = 0.0333333351f * cameraData2.flame_no;
				Vector3 vector = cameraData2.rotation * 57.29578f;
				vector.y *= -1f;
				Quaternion quaternion = Quaternion.Euler(vector);
				quaternion = boneAdjustment.GetAdjustedRotation(quaternion);
				VMDHSConverter.QuaternionKeyframe item4 = new VMDHSConverter.QuaternionKeyframe(time, quaternion);
				list7.Add(item4);
				num6++;
			}
			Keyframe[] array2 = null;
			Keyframe[] array3 = null;
			Keyframe[] array4 = null;
			Dictionary<string, Keyframe[]> dictionary = this.ToKeyframesForRotation(list7.ToArray(), ref array2, ref array3, ref array4, false);
			AnimationCurve animationCurve7 = new AnimationCurve(dictionary["localRotation.x"]);
			AnimationCurve animationCurve8 = new AnimationCurve(dictionary["localRotation.y"]);
			AnimationCurve animationCurve9 = new AnimationCurve(dictionary["localRotation.z"]);
			AnimationCurve animationCurve10 = new AnimationCurve(dictionary["localRotation.w"]);
			animationClip.SetCurve("camera", typeof(Transform), "localRotation.x", animationCurve7);
			animationClip.SetCurve("camera", typeof(Transform), "localRotation.y", animationCurve8);
			animationClip.SetCurve("camera", typeof(Transform), "localRotation.z", animationCurve9);
			animationClip.SetCurve("camera", typeof(Transform), "localRotation.w", animationCurve10);
			return animationClip;
		}

		
		private void ReadIKModeSettings(VMDFormat format)
		{
		}

		
		private Dictionary<string, string> boneNameMap;

		
		private Dictionary<string, BoneAdjustment> boneAdjustment;

		
		public Vector3 scale = Vector3.one;

		
		public Vector3 centerBasePos;

		
		private Vector3 hipCenterDiff;

		
		public ModelBaselineData HSModelBaseline;

		
		public string faceGoPath;

		
		public HashSet<string> faceNames;

		
		public HashSet<string> usedFaceNames;

		
		private AnimationCurve centerXCurve;

		
		private AnimationCurve centerYCurve;

		
		private AnimationCurve centerZCurve;

		
		private AnimationCurve centerWCurve;

		
		private Dictionary<string, bool> IKMode;

		
		private static PropertyInfo p_legacy = typeof(AnimationClip).GetProperty("legacy", BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.SetProperty);

		
		private const int TangentModeBothLinear = 21;

		
		private abstract class CustomKeyframe<Type>
		{
			
			public CustomKeyframe(float time, Type value)
			{
				this.time = time;
				this.value = value;
			}

			
			
			
			public float time { get; set; }

			
			
			
			public Type value { get; set; }
		}

		
		private class FloatKeyframe : VMDHSConverter.CustomKeyframe<float>
		{
			
			public FloatKeyframe(float time, float value) : base(time, value)
			{
			}

			
			public static VMDHSConverter.FloatKeyframe Lerp(VMDHSConverter.FloatKeyframe from, VMDHSConverter.FloatKeyframe to, Vector2 t)
			{
				return new VMDHSConverter.FloatKeyframe(Mathf.Lerp(from.time, to.time, t.x), Mathf.Lerp(from.value, to.value, t.y));
			}

			
			public static void AddBezierKeyframes(byte[] interpolation, int type, VMDHSConverter.FloatKeyframe prev_keyframe, VMDHSConverter.FloatKeyframe cur_keyframe, int interpolationQuality, ref List<VMDHSConverter.FloatKeyframe> keyframes, ref int index)
			{
				if (prev_keyframe == null || VMDHSConverter.IsLinear(interpolation, type))
				{
					keyframes.Add(cur_keyframe);
					index = keyframes.Count<VMDHSConverter.FloatKeyframe>();
					return;
				}
				Vector2 bezierHandle = VMDHSConverter.GetBezierHandle(interpolation, type, 0);
				Vector2 bezierHandle2 = VMDHSConverter.GetBezierHandle(interpolation, type, 1);
				for (int i = 0; i < interpolationQuality; i++)
				{
					float t = (float)(i + 1) / (float)interpolationQuality;
					Vector2 t2 = VMDHSConverter.SampleBezier(bezierHandle, bezierHandle2, t);
					keyframes.Add(VMDHSConverter.FloatKeyframe.Lerp(prev_keyframe, cur_keyframe, t2));
					index = keyframes.Count<VMDHSConverter.FloatKeyframe>();
				}
			}
		}

		
		private class FloatLinearKeyframe : VMDHSConverter.CustomKeyframe<float>
		{
			
			public FloatLinearKeyframe(float time, float value) : base(time, value)
			{
			}

			
			public static VMDHSConverter.FloatKeyframe Lerp(VMDHSConverter.FloatKeyframe from, VMDHSConverter.FloatKeyframe to, Vector2 t)
			{
				return new VMDHSConverter.FloatKeyframe(Mathf.Lerp(from.time, to.time, t.x), Mathf.Lerp(from.value, to.value, t.y));
			}
		}

		
		private class QuaternionKeyframe : VMDHSConverter.CustomKeyframe<Quaternion>
		{
			
			public QuaternionKeyframe(float time, Quaternion value) : base(time, value)
			{
			}

			
			public static VMDHSConverter.QuaternionKeyframe Lerp(VMDHSConverter.QuaternionKeyframe from, VMDHSConverter.QuaternionKeyframe to, Vector2 t)
			{
				return new VMDHSConverter.QuaternionKeyframe(Mathf.Lerp(from.time, to.time, t.x), Quaternion.Slerp(from.value, to.value, t.y));
			}

			
			public static void AddBezierKeyframes(byte[] interpolation, int type, VMDHSConverter.QuaternionKeyframe prev_keyframe, VMDHSConverter.QuaternionKeyframe cur_keyframe, int interpolationQuality, ref VMDHSConverter.QuaternionKeyframe[] keyframes, ref int index)
			{
				if (prev_keyframe == null || VMDHSConverter.IsLinear(interpolation, type))
				{
					VMDHSConverter.QuaternionKeyframe[] array = keyframes;
					int num = index;
					index = num + 1;
					array[num] = cur_keyframe;
					return;
				}
				Vector2 bezierHandle = VMDHSConverter.GetBezierHandle(interpolation, type, 0);
				Vector2 bezierHandle2 = VMDHSConverter.GetBezierHandle(interpolation, type, 1);
				for (int i = 0; i < interpolationQuality; i++)
				{
					float t = (float)(i + 1) / (float)interpolationQuality;
					Vector2 t2 = VMDHSConverter.SampleBezier(bezierHandle, bezierHandle2, t);
					VMDHSConverter.QuaternionKeyframe[] array2 = keyframes;
					int num = index;
					index = num + 1;
					array2[num] = VMDHSConverter.QuaternionKeyframe.Lerp(prev_keyframe, cur_keyframe, t2);
				}
			}
		}
	}
}
