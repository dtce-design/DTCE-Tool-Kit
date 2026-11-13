using System;
using System.Linq;
using System.Security.Cryptography;

namespace BeamScheduleFormatter
{
	internal class PdfSecurity
	{
		private static readonly byte[] PaddingBytes = new byte[32]
		{
			40, 191, 78, 94, 78, 117, 138, 65, 100, 0,
			78, 86, 255, 250, 1, 8, 46, 46, 0, 182,
			208, 104, 62, 128, 47, 12, 169, 254, 100, 83,
			105, 122
		};

		public readonly byte[] Id;

		public readonly byte[] Owner;

		public readonly byte[] User;

		private readonly byte[] key;

		public const int Permissions = -1852;

		private static byte[] Random(int size)
		{
			byte[] array = new byte[size];
			using (RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create())
			{
				randomNumberGenerator.GetBytes(array);
				return array;
			}
		}

		private static byte[] Md5(byte[] b)
		{
			using (MD5 mD = MD5.Create())
			{
				return mD.ComputeHash(b);
			}
		}

		private static byte[] Join(params byte[][] arrays)
		{
			return arrays.SelectMany((byte[] x) => x).ToArray();
		}

		private static byte[] Pad(byte[] password)
		{
			return password.Concat(PaddingBytes).Take(32).ToArray();
		}

		private static byte[] Rc4(byte[] k, byte[] b)
		{
			int[] array = Enumerable.Range(0, 256).ToArray();
			int num = 0;
			for (int i = 0; i < 256; i++)
			{
				num = (num + array[i] + k[i % k.Length]) & 0xFF;
				int num2 = array[i];
				array[i] = array[num];
				array[num] = num2;
			}
			byte[] array2 = new byte[b.Length];
			int num3 = 0;
			num = 0;
			for (int j = 0; j < b.Length; j++)
			{
				num3 = (num3 + 1) & 0xFF;
				num = (num + array[num3]) & 0xFF;
				int num4 = array[num3];
				array[num3] = array[num];
				array[num] = num4;
				array2[j] = (byte)(b[j] ^ array[(array[num3] + array[num]) & 0xFF]);
			}
			return array2;
		}

		public PdfSecurity()
		{
			Id = Random(16);
			byte[] array = Md5(Pad(Random(32)));
			for (int i = 0; i < 50; i++)
			{
				array = Md5(array);
			}
			Owner = Rc4(array, PaddingBytes);
			int j;
			for (j = 1; j <= 19; j++)
			{
				Owner = Rc4(array.Select((byte v) => (byte)(v ^ j)).ToArray(), Owner);
			}
			byte[] bytes = BitConverter.GetBytes(-1852);
			if (!BitConverter.IsLittleEndian)
			{
				Array.Reverse(bytes);
			}
			array = Md5(Join(PaddingBytes, Owner, bytes, Id));
			for (int num = 0; num < 50; num++)
			{
				array = Md5(array.Take(16).ToArray());
			}
			key = array.Take(16).ToArray();
			byte[] array2 = Rc4(key, Md5(Join(PaddingBytes, Id)));
			int i2;
			for (i2 = 1; i2 <= 19; i2++)
			{
				array2 = Rc4(key.Select((byte v) => (byte)(v ^ i2)).ToArray(), array2);
			}
			User = Join(array2, new byte[16]);
		}

		public byte[] Encrypt(byte[] bytes, int objectNumber)
		{
			byte[] obj = new byte[9] { 0, 0, 0, 0, 0, 115, 65, 108, 84 };
			obj[0] = (byte)objectNumber;
			obj[1] = (byte)(objectNumber >> 8);
			obj[2] = (byte)(objectNumber >> 16);
			byte[] array = obj;
			byte[] array2 = Md5(Join(key, array)).Take(16).ToArray();
			using (Aes aes = Aes.Create())
			{
				aes.Key = array2;
				aes.IV = Random(16);
				aes.Mode = CipherMode.CBC;
				aes.Padding = PaddingMode.PKCS7;
				using (ICryptoTransform cryptoTransform = aes.CreateEncryptor())
				{
					return Join(aes.IV, cryptoTransform.TransformFinalBlock(bytes, 0, bytes.Length));
				}
			}
		}

		public static string Hex(byte[] bytes)
		{
			return BitConverter.ToString(bytes).Replace("-", "");
		}

		public string Dictionary()
		{
			return "<< /Filter /Standard /V 4 /R 4 /Length 128 /O <" + Hex(Owner) + "> /U <" + Hex(User) + "> /P " + -1852 + " /EncryptMetadata true /CF << /StdCF << /CFM /AESV2 /AuthEvent /DocOpen /Length 16 >> >> /StmF /StdCF /StrF /StdCF >>";
		}
	}
}
