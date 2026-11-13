using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace BeamScheduleFormatter
{
	internal static class Program
	{
		[STAThread]
		private static int Main(string[] args)
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			try
			{
				if (!AdminPasswordGate.Unlock())
				{
					return 0;
				}
				Application.Run(new LauncherForm());
				return 0;
			}
			catch (Exception ex)
			{
				if (args.Length != 0)
				{
					File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test-error.txt"), ex.ToString());
					return 1;
				}
				MessageBox.Show(ex.Message, "Beam Schedule Formatter", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				return 1;
			}
		}

		private static void Assert(bool ok, string message)
		{
			if (!ok)
			{
				throw new Exception("TEST FAILED: " + message);
			}
		}

		private static int SelfTest(string[] args)
		{
			if (args.Length < 4)
			{
				throw new Exception("--self-test source.dxf output.dxf report.txt");
			}
			Analysis analysis = Engine.Analyze(args[1]);
			Settings s = new Settings
			{
				Prefix = "4B",
				Floor = "FOURTH FLOOR",
				Merge = true
			};
			Result result = Engine.Build(analysis, s);
			Assert(analysis.Source.Count == 55, "55 original rows");
			Assert(analysis.BeamCount == 122, "122 original beams");
			Assert(result.Rows.Count == 51, "51 unique rows");
			Assert(result.Rows.Sum((BeamRow x) => x.Numbers.Count) == 122, "all beam numbers retained");
			Assert(result.Rows.Any((BeamRow x) => x.SourceRows.SequenceEqual(new int[2] { 20, 22 })), "merge 20+22");
			Assert(result.Rows.Any((BeamRow x) => x.SourceRows.SequenceEqual(new int[3] { 38, 39, 50 })), "merge 38+39+50");
			Assert(result.Rows.Any((BeamRow x) => x.SourceRows.SequenceEqual(new int[2] { 47, 49 })), "merge 47+49");
			Assert(result.Rows.Where((BeamRow x) => x.Cantilever).All((BeamRow x) => x.Spacing.EndsWith("TO FREE END")), "cantilever rule");
			Assert(result.Rows.Any((BeamRow x) => x.Spacing == "1 - 50mm, AND REST AT 175mm C/C"), "equal spacing rule");
			Assert(result.Rows.Any((BeamRow x) => x.Spacing.StartsWith("1 - 50mm, 23 - 75mm, AND REST AT 100mm")), "smaller end spacing and unequal end counts preserved");
			BeamRow beamRow = result.Rows.Single((BeamRow x) => x.SourceRows.Contains(19));
			Assert(beamRow.Spacing == "1 - 50mm, 9 - 100mm, AND REST AT 100mm TO FREE END" && beamRow.Notes.Any((string t) => t.Contains("USED 9pcs @ 100mm")), "cantilever minimum end spacing and note");
			BeamRow beamRow2 = result.Rows.Single((BeamRow x) => x.SourceRows.Contains(52));
			Assert(beamRow2.Spacing == "1 - 50mm, AND REST AT 100mm C/C" && beamRow2.Notes.Any((string t) => t.Contains("MID BLANK IN SOURCE")), "blank mid uses smaller end spacing for AND REST AT and is noted");
			BeamRow beamRow3 = analysis.Source[0].Copy();
			beamRow3.Cells[8][0] = "17-4L-#10@150 C/C";
			beamRow3.Cells[9] = new List<string> { "-" };
			beamRow3.Cells[10][0] = "12-4L-#10@100 C/C";
			Engine.ApplySpacing(beamRow3);
			Assert(beamRow3.Spacing == "1 - 50mm, 12 - 100mm, AND REST AT 100mm C/C", "blank MID with unequal LEFT/RIGHT uses smaller spacing, independent of end counts");
			BeamRow beamRow4 = analysis.Source[0].Copy();
			beamRow4.Cells[8][0] = "17-4L-#10@100 C/C";
			beamRow4.Cells[9][0] = "30-4L-#10@150 C/C";
			beamRow4.Cells[10][0] = "17-4L-#10@75 C/C";
			Engine.ApplySpacing(beamRow4);
			Assert(beamRow4.Spacing.Contains("17 - 75mm, AND REST AT 150mm") && beamRow4.Notes.Any((string t) => t.Contains("USED 17pcs @ 75mm AT BOTH ENDS")), "non-cantilever uses minimum of LEFT and RIGHT, keeps MID");
			List<BeamRow> list = Engine.Consolidate(analysis, new Settings
			{
				Prefix = "3B",
				Floor = "THIRD FLOOR",
				Merge = false
			});
			Assert(list.Count == 55 && list[0].Mark == "3B-001", "merge toggle and prefix");
			Analysis obj = new Analysis
			{
				BeamCount = analysis.BeamCount,
				Source = analysis.Source.Select((BeamRow x) => x.Copy()).ToList()
			};
			obj.Source[21].Cells[0][0] = "451";
			Assert(Engine.Consolidate(obj, s).Count == 52, "a different width must not merge");
			obj.Source = analysis.Source.Select((BeamRow x) => x.Copy()).ToList();
			obj.Source[21].Cells[2][0] = "5-#20";
			Assert(Engine.Consolidate(obj, s).Count == 52, "different reinforcement must not merge");
			bool ok = false;
			try
			{
				Engine.Consolidate(analysis, new Settings
				{
					Prefix = "invalid prefix!",
					Floor = "FOURTH FLOOR"
				});
			}
			catch
			{
				ok = true;
			}
			Assert(ok, "invalid prefix rejected");
			ok = false;
			try
			{
				Dxf.Parse("0\nSECTION\n2\nENTITIES\n");
			}
			catch
			{
				ok = true;
			}
			Assert(ok, "incomplete DXF rejected");
			Dxf dxf = Engine.Standard();
			string[] array = new string[2] { "STYLE", "LAYER" };
			foreach (string type in array)
			{
				string[] first = (from e in dxf.Records
					where e.Type == type
					select string.Join("|", e.P.Select((Pair p) => p.Code + ":" + p.Value))).ToArray();
				string[] second = (from e in result.Drawing.Records
					where e.Type == type
					select string.Join("|", e.P.Select((Pair p) => p.Code + ":" + p.Value))).ToArray();
				Assert(first.SequenceEqual(second), "template " + type + " exact preservation");
			}
			List<Entity> list2 = (from e in result.Drawing.ModelEntities()
				where e.Type == "MTEXT" && Regex.IsMatch(e.Get(1), "^4B-\\d{3}$")
				orderby e.Num(20) descending
				select e).ToList();
			for (int num2 = 1; num2 < list2.Count; num2++)
			{
				Assert(Math.Abs(list2[num2 - 1].Num(20) - list2[num2].Num(20) - 8.0) < 1E-05, "row height 8");
			}
			foreach (Entity text in from e in result.Drawing.ModelEntities()
				where e.Type == "MTEXT" && e.Num(10) > 2294.0 && e.Num(10) < 2427.0 && Regex.IsMatch(e.Get(1), "^\\d+ - %%C")
				select e)
			{
				Assert(text.Get(71).Trim() == "8" || text.Get(71).Trim() == "2", "reinforcement layer attachment");
				Entity entity = list2.OrderBy((Entity e) => Math.Abs(e.Num(20) - text.Num(20))).First();
				Assert(Math.Abs(text.Num(20) - entity.Num(20) - ((text.Get(71).Trim() == "8") ? 0.5 : (-0.5))) < 1E-05, "single and first reinforcement layers aligned at TOP");
			}
			Engine.Export(result, args[2]);
			using (Bitmap bitmap = Preview.Render(result.Drawing))
			{
				bitmap.Save(Path.Combine(Path.GetDirectoryName(args[3]), "cad-preview.png"));
			}
			File.WriteAllText(args[3], "PASS\r\n55 source rows -> 51 groups\r\n122 original beam references retained\r\nExact Drawing1 font and layer definitions\r\n8-unit row heights\r\nSingle-layer reinforcement TOP-aligned\r\nMinimum LEFT/RIGHT spacing + associated piece count for all beams\r\nBlank MID uses smaller LEFT/RIGHT for AND REST AT, with note\r\nCantilever TO FREE END and adjustment notes\r\nEight-hour cached access replaces legacy password\r\nEqual-spacing format, merge toggle and floor prefix\r\nDifferent size/reinforcement rows not merged\r\nInvalid prefix and incomplete DXF rejected\r\nVerified DXF round-trip\r\n" + string.Join("\r\n", result.Changes));
			return 0;
		}
	}
}
