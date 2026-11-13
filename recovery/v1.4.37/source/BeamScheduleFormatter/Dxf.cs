using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BeamScheduleFormatter
{
	internal class Dxf
	{
		public List<Entity> Records = new List<Entity>();

		public static Dxf Read(string path)
		{
			using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			{
				using (StreamReader streamReader = new StreamReader(stream, Encoding.UTF8, true))
				{
					return Parse(streamReader.ReadToEnd());
				}
			}
		}

		public static Dxf Parse(string text)
		{
			if (text.StartsWith("AutoCAD Binary DXF"))
			{
				throw new Exception("Binary DXF ito. Sa AutoCAD, i-save bilang ASCII DXF muna.");
			}
			string[] array = text.Replace("\r", "").Split('\n');
			int num = array.Length;
			while (num > 0 && array[num - 1] == "")
			{
				num--;
			}
			if (num % 2 != 0)
			{
				throw new Exception("Hindi kumpleto ang DXF file (may nawawalang group-code pair).");
			}
			Dxf dxf = new Dxf();
			Entity entity = null;
			for (int i = 0; i < num; i += 2)
			{
				int result;
				if (!int.TryParse(array[i].Trim(), out result))
				{
					throw new Exception("Hindi valid na ASCII DXF ang file. Line " + (i + 1) + ".");
				}
				if (result == 0 || entity == null)
				{
					entity = new Entity();
					dxf.Records.Add(entity);
				}
				entity.P.Add(new Pair(result, array[i + 1]));
			}
			if (!dxf.Records.Any((Entity x) => x.Type == "EOF"))
			{
				throw new Exception("Hindi kumpleto ang DXF: walang EOF.");
			}
			return dxf;
		}

		public List<Entity> ModelEntities()
		{
			bool flag = false;
			List<Entity> list = new List<Entity>();
			foreach (Entity record in Records)
			{
				if (record.Type == "SECTION" && record.Get(2) == "ENTITIES")
				{
					flag = true;
					continue;
				}
				if (flag && record.Type == "ENDSEC")
				{
					break;
				}
				if (flag && record.Get(67, "0").Trim() != "1" && record.Get(410, "Model") == "Model")
				{
					list.Add(record);
				}
			}
			return list;
		}

		public void Write(string path)
		{
			using (StreamWriter streamWriter = new StreamWriter(path, false, new UTF8Encoding(false)))
			{
				foreach (Entity record in Records)
				{
					foreach (Pair item in record.P)
					{
						streamWriter.WriteLine(item.Code.ToString().PadLeft(3));
						streamWriter.WriteLine(item.Value);
					}
				}
			}
		}
	}
}
