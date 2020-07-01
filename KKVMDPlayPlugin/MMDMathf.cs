using System;
using UnityEngine;


public class MMDMathf
{
	
	public static Matrix4x4 CreateRotationXMatrix(float rad)
	{
		Matrix4x4 identity = Matrix4x4.identity;
		float num = Mathf.Cos(rad);
		float num2 = Mathf.Sin(rad);
		identity.m11 = num;
		identity.m12 = -num2;
		identity.m21 = num2;
		identity.m22 = num;
		return identity;
	}

	
	public static Matrix4x4 CreateRotationYMatrix(float rad)
	{
		Matrix4x4 identity = Matrix4x4.identity;
		float num = Mathf.Cos(rad);
		float num2 = Mathf.Sin(rad);
		identity.m00 = num;
		identity.m02 = num2;
		identity.m20 = -num2;
		identity.m22 = num;
		return identity;
	}

	
	public static Matrix4x4 CreateRotationZMatrix(float rad)
	{
		Matrix4x4 identity = Matrix4x4.identity;
		float num = Mathf.Cos(rad);
		float num2 = Mathf.Sin(rad);
		identity.m01 = num;
		identity.m02 = -num2;
		identity.m11 = num2;
		identity.m12 = num;
		return identity;
	}

	
	public static Matrix4x4 CreateRotationMatrixFromRollPitchYaw(float r, float p, float y)
	{
		Matrix4x4 identity = Matrix4x4.identity;
		float num = Mathf.Cos(r);
		float num2 = Mathf.Sin(r);
		float num3 = Mathf.Cos(p);
		float num4 = Mathf.Sin(p);
		float num5 = Mathf.Cos(y);
		float num6 = Mathf.Sin(y);
		identity.m00 = num * num3;
		identity.m01 = num * num4 * num6 - num2 * num5;
		identity.m02 = num * num4 * num5 + num2 * num6;
		identity.m10 = num2 * num3;
		identity.m11 = num2 * num4 * num6 + num * num5;
		identity.m12 = num2 * num4 * num5 - num * num6;
		identity.m20 = -num4;
		identity.m21 = num3 * num6;
		identity.m22 = num3 * num5;
		return identity;
	}

	
	public static Vector3 CreatePositionFromMatrix(Matrix4x4 m)
	{
		return new Vector3(m.m30, m.m31, m.m33);
	}

	
	public static Quaternion CreateQuaternionFromRotationMatrix(Matrix4x4 m)
	{
		Quaternion quaternion;
		quaternion.x = (m.m00 + m.m11 + m.m22 + 1f) * 0.25f;
		quaternion.y = (m.m00 - m.m11 - m.m22 + 1f) * 0.25f;
		quaternion.z = (-m.m00 + m.m11 - m.m22 + 1f) * 0.25f;
		quaternion.w = (-m.m00 - m.m11 + m.m22 + 1f) * 0.25f;
		if (quaternion.x < 0f)
		{
			quaternion.x = 0f;
		}
		if (quaternion.y < 0f)
		{
			quaternion.y = 0f;
		}
		if (quaternion.z < 0f)
		{
			quaternion.z = 0f;
		}
		if (quaternion.w < 0f)
		{
			quaternion.w = 0f;
		}
		quaternion.x = Mathf.Sqrt(quaternion.x);
		quaternion.y = Mathf.Sqrt(quaternion.y);
		quaternion.z = Mathf.Sqrt(quaternion.z);
		quaternion.w = Mathf.Sqrt(quaternion.w);
		if (quaternion.x >= quaternion.y && quaternion.x >= quaternion.z && quaternion.x >= quaternion.w)
		{
			quaternion.x *= 1f;
			quaternion.y *= MMDMathf.Sign(m.m22 - m.m13);
			quaternion.z *= MMDMathf.Sign(m.m03 - m.m21);
			quaternion.w *= MMDMathf.Sign(m.m11 - m.m02);
		}
		else if (quaternion.y >= quaternion.x && quaternion.y >= quaternion.z && quaternion.y >= quaternion.w)
		{
			quaternion.x *= MMDMathf.Sign(m.m22 - m.m13);
			quaternion.y *= 1f;
			quaternion.z *= MMDMathf.Sign(m.m11 + m.m02);
			quaternion.w *= MMDMathf.Sign(m.m03 + m.m21);
		}
		else if (quaternion.z >= quaternion.x && quaternion.z >= quaternion.y && quaternion.z >= quaternion.w)
		{
			quaternion.x *= MMDMathf.Sign(m.m03 - m.m21);
			quaternion.y *= MMDMathf.Sign(m.m11 + m.m02);
			quaternion.z *= 1f;
			quaternion.w *= MMDMathf.Sign(m.m22 + m.m13);
		}
		else if (quaternion.w >= quaternion.x && quaternion.w >= quaternion.y && quaternion.w >= quaternion.z)
		{
			quaternion.x *= MMDMathf.Sign(m.m11 - m.m02);
			quaternion.y *= MMDMathf.Sign(m.m21 + m.m03);
			quaternion.z *= MMDMathf.Sign(m.m22 + m.m13);
			quaternion.w *= 1f;
		}
		float num = 1f / MMDMathf.Norm(quaternion.x, quaternion.y, quaternion.z, quaternion.w);
		quaternion.x *= num;
		quaternion.y *= num;
		quaternion.z *= num;
		quaternion.w *= num;
		return quaternion;
	}

	
	private static float Sign(float x)
	{
		if (x < 0f)
		{
			return -1f;
		}
		return 1f;
	}

	
	private static float Norm(float a, float b, float c, float d)
	{
		return Mathf.Sqrt(a * a + b * b + c * c + d * d);
	}
}
