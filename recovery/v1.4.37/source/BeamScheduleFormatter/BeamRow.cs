using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BeamScheduleFormatter
{
	internal class BeamRow
	{
		public string RevisedSpacing;

		public string RevisedReferences;

		public List<string>[] Cells = (from x in Enumerable.Range(0, 14)
			select new List<string>()).ToArray();

		public List<string> Numbers = new List<string>();

		public List<int> SourceRows = new List<int>();

		public List<string[]> OriginalShear = new List<string[]>();

		public List<string> Notes = new List<string>();

		public string Mark;

		public string Leg;

		public string Diameter;

		public string Spacing;

		public bool Cantilever
		{
			get
			{
				return Cell(13).ToUpperInvariant().Contains("CANTILEVER");
			}
		}

		public string Cell(int n)
		{
			if (Cells[n].Count != 0)
			{
				return Cells[n][0];
			}
			return "--";
		}

		public BeamRow Copy()
		{
			BeamRow beamRow = new BeamRow();
			beamRow.Cells = Cells.Select((List<string> v) => new List<string>(v)).ToArray();
			beamRow.Numbers.AddRange(Numbers);
			beamRow.SourceRows.AddRange(SourceRows);
			beamRow.OriginalShear.AddRange(OriginalShear.Select((string[] s) => (string[])s.Clone()));
			beamRow.Notes.AddRange(Notes);
			beamRow.Leg = Leg;
			beamRow.Diameter = Diameter;
			beamRow.Spacing = Spacing;
			return beamRow;
		}

		public string Signature()
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (int item in Enumerable.Range(0, 8).Concat(new int[3] { 11, 12, 13 }))
			{
				stringBuilder.Append("|");
				foreach (string item2 in Cells[item])
				{
					if (item2.Trim() != "+")
					{
						stringBuilder.Append(Engine.Normal(item2)).Append(";");
					}
				}
			}
			return stringBuilder.Append("|").Append(Leg).Append("|")
				.Append(Diameter)
				.Append("|")
				.Append(Spacing)
				.ToString();
		}
	}
}
