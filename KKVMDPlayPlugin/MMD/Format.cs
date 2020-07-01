using System;
using System.IO;
using HexDump;
using KKVMDPlayPlugin;
using UnityEngine;

namespace MMD
{
	
	public class Format : IComparable
	{
		
		protected string ConvertByteToString(byte[] bytes)
		{
			if (bytes[0] == 0)
			{
				return "";
			}
			int num = 0;
			while (num < bytes.Length && bytes[num] != 0)
			{
				num++;
			}
			byte[] array = new byte[num];
			for (int i = 0; i < num; i++)
			{
				array[i] = bytes[i];
			}
			string result;
			try
			{
				result = ToEncoding.ToUnicode(array);
			}
			catch (Exception)
			{
				byte[] array2 = new byte[array.Length - 1];
				for (int j = 0; j < array.Length - 1; j++)
				{
					array2[j] = array[j];
				}
				try
				{
					result = ToEncoding.ToUnicode(array2);
				}
				catch (Exception ex)
				{
					Console.WriteLine("Failed to read buf: {0}", Utils.HexDump(array, 16));
					using (FileStream fileStream = File.Create("__debug.dat"))
					{
						fileStream.Write(array, 0, array.Length);
					}
					throw ex;
				}
			}
			return result;
		}

		
		protected float[] ReadSingles(BinaryReader bin, uint count)
		{
			float[] array = new float[count];
			int num = 0;
			while ((long)num < (long)((ulong)count))
			{
				array[num] = bin.ReadSingle();
				num++;
			}
			return array;
		}

		
		protected Vector3 ReadSinglesToVector3(BinaryReader bin)
		{
			float[] array = new float[3];
			for (int i = 0; i < 3; i++)
			{
				array[i] = bin.ReadSingle();
			}
			return new Vector3(array[0], array[1], array[2]);
		}

		
		protected Vector2 ReadSinglesToVector2(BinaryReader bin)
		{
			float[] array = new float[2];
			for (int i = 0; i < 2; i++)
			{
				array[i] = bin.ReadSingle();
			}
			return new Vector2(array[0], array[1]);
		}

		
		protected Color ReadSinglesToColor(BinaryReader bin)
		{
			float[] array = new float[4];
			for (int i = 0; i < 4; i++)
			{
				array[i] = bin.ReadSingle();
			}
			return new Color(array[0], array[1], array[2], array[3]);
		}

		
		protected Color ReadSinglesToColor(BinaryReader bin, float fix_alpha)
		{
			float[] array = new float[3];
			for (int i = 0; i < 3; i++)
			{
				array[i] = bin.ReadSingle();
			}
			return new Color(array[0], array[1], array[2], fix_alpha);
		}

		
		protected uint[] ReadUInt32s(BinaryReader bin, uint count)
		{
			uint[] array = new uint[count];
			int num = 0;
			while ((long)num < (long)((ulong)count))
			{
				array[num] = bin.ReadUInt32();
				num++;
			}
			return array;
		}

		
		protected ushort[] ReadUInt16s(BinaryReader bin, uint count)
		{
			ushort[] array = new ushort[count];
			for (uint num = 0u; num < count; num += 1u)
			{
				array[(int)num] = bin.ReadUInt16();
			}
			return array;
		}

		
		protected Quaternion ReadSinglesToQuaternion(BinaryReader bin)
		{
			float[] array = new float[4];
			for (int i = 0; i < 4; i++)
			{
				array[i] = bin.ReadSingle();
			}
			return new Quaternion(array[0], array[1], array[2], array[3]);
		}

		
		public int CompareTo(object obj)
		{
			return this.count - ((Format)obj).count;
		}

		
		protected int count;
	}
}
