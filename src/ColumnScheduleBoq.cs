using System;using System.Collections.Generic;using System.Globalization;using System.IO;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* the column schedule DXF in the Bill of Quantities (owner, 22 November 2026: "yung revised sa formatter kaya din iestimate sa boq kapag
   niload sa column"): Column Estimate > Load column schedule DXF fills the Manual column schedules - one type per column mark and level
   range (section, two bar groups, tie size, the Z1 / rest spacing as a tie schedule, inner links and crossties as open ties per set) -
   and one Column Estimate row per mark (full height, quantity = the identical columns). The take-off is the existing per-level column
   estimate (bars lap into the range above). */
partial class ProgramOfWorksForm {
 static readonly CultureInfo ColInv=CultureInfo.InvariantCulture;
 void ColumnDxfButton(){var tab=tabs.TabPages.Cast<TabPage>().First(p=>p.Text=="Column Estimate");var bar=tab.Controls.OfType<FlowLayoutPanel>().First();Button(bar,"Load column schedule DXF",LoadColumnScheduleDxf);}
 /* ties within Lo at the Z1 spacing from each end (first tie 50 mm from the face), the rest spacing between */
 internal static string ColumnTieText(ColumnCell c){double s1=Math.Max(1,c.S1Main),s2=Math.Max(1,c.S2);int n=Math.Max(1,(int)Math.Ceiling((Math.Max(c.Lo,s1)-50)/s1-1e-9));return "1 - 50mm, "+n+" - "+s1.ToString("0",ColInv)+"mm, AND REST AT "+s2.ToString("0",ColInv)+"mm C/C";}
 /* inner closed links count as two open legs in their long direction, crossties as one */
 internal static int[] OpenTiesOf(ColumnCell c){int w=0,d=0;foreach(var l in c.Links.Skip(1)){bool tie=l.W<1||l.H<1;int legs=tie?1:2;if(tie?l.W<1:l.W<l.H)d+=legs;else w+=legs;}return new[]{w,d};}
 void LoadColumnScheduleDxf(){string path;using(var d=new OpenFileDialog{Filter="Column schedule DXF|*.dxf",Title="Column schedule DXF (Column Formatter output or the RCDC original)"}){if(d.ShowDialog(this)!=DialogResult.OK)return;path=d.FileName;}
  try{var s=ColumnScheduleReader.Read(File.ReadAllText(path));if(s.Originals.Count==0)s=ColumnFormatter.Merged(s,"C");int added=LoadColumnSchedule(s);
   MessageBox.Show(this,"Loaded "+Path.GetFileName(path)+": "+s.Marks.Count+" column marks, "+added+" level types in Manual column schedules, one Column Estimate row per mark.\n\nInner links count as two open ties per set and crossties as one (Open ties parallel Width / Depth).","Column schedule DXF",MessageBoxButtons.OK,MessageBoxIcon.Information);}
  catch(Exception ex){MessageBox.Show(this,ex.Message,"Column schedule DXF",MessageBoxButtons.OK,MessageBoxIcon.Warning);}}
 internal int LoadColumnSchedule(ColumnSchedule s){var m=memberSchedules.First(x=>x.Kind=="Column");var cs=m.Schedule;var est=m.Estimate;int markAt=est.Columns[ScheduleMarkColumn].Index;int types=0;
  /* check everything first, so a bad column changes nothing */
  foreach(var mark in s.Marks)for(int l=0;l<s.Levels.Count;l++){var c=s.Cell(mark,l);if(c!=null&&c.Counts.Count>2)throw new Exception("Column "+mark+", "+s.Levels[l].Text+": "+c.Counts.Count+" bar sizes; the column estimate takes two (main group 1 and 2).");}
  bool before=filling;filling=true;try{
   foreach(var mark in s.Marks){foreach(var r in cs.Rows.Cast<DataGridViewRow>().Where(r=>!r.IsNewRow&&string.Equals(Convert.ToString(r.Cells[0].Value).Trim(),mark,StringComparison.OrdinalIgnoreCase)).ToList())cs.Rows.Remove(r);
    string originals;s.Originals.TryGetValue(mark,out originals);string desc=string.IsNullOrWhiteSpace(originals)?"Column "+mark:"Columns "+originals;
    var levels=Enumerable.Range(0,s.Levels.Count).Where(l=>s.Cell(mark,l)!=null).OrderBy(l=>Math.Min(s.Levels[l].FromM,s.Levels[l].ToM)).ToList();if(levels.Count==0)continue;
    foreach(int l in levels){var c=s.Cell(mark,l);var bars=c.Counts.ToList();var open=OpenTiesOf(c);var lv=s.Levels[l];var v=new object[cs.Columns.Count];
     v[0]=mark;v[1]=desc;v[2]=c.B.ToString("0",ColInv);v[3]=c.H.ToString("0",ColInv);v[4]=bars[0].Key.ToString(ColInv);v[5]=bars[0].Value.ToString(ColInv);v[6]=bars.Count>1?bars[1].Key.ToString(ColInv):"";v[7]=bars.Count>1?bars[1].Value.ToString(ColInv):"";v[8]=c.TieDia.ToString(ColInv);v[9]="Spacing schedule";v[10]=ColumnTieText(c);v[11]=open[0].ToString(ColInv);v[12]=open[1].ToString(ColInv);
     v[13]=Math.Min(lv.FromM,lv.ToM).ToString("0.###",ColInv);v[14]=Math.Max(lv.FromM,lv.ToM).ToString("0.###",ColInv);for(int i=15;i<v.Length;i++)v[i]="";cs.Rows.Add(v);types++;}
    double from=levels.Min(l=>Math.Min(s.Levels[l].FromM,s.Levels[l].ToM)),to=levels.Max(l=>Math.Max(s.Levels[l].FromM,s.Levels[l].ToM));int qty=string.IsNullOrWhiteSpace(originals)?1:originals.Split(',').Count(x=>x.Trim()!="");var first=s.Cell(mark,levels[0]);
    var row=est.Rows.Cast<DataGridViewRow>().FirstOrDefault(r=>!r.IsNewRow&&string.Equals(Convert.ToString(r.Cells[markAt].Value).Trim(),mark,StringComparison.OrdinalIgnoreCase));
    if(row==null){int i=est.Rows.Add();row=est.Rows[i];foreach(var kv in new Dictionary<int,string>{{6,"16"},{7,"8"},{8,"12"},{9,"0"},{10,"10"},{11,"Spacing (mm)"},{12,"150"},{13,"0"},{14,"False"},{15,"0"}})row.Cells[kv.Key].Value=kv.Value;}
    row.Cells[0].Value=mark;row.Cells[1].Value=desc;row.Cells[2].Value=(to-from).ToString("0.###",ColInv);row.Cells[3].Value=first.B.ToString("0",ColInv);row.Cells[4].Value=first.H.ToString("0",ColInv);row.Cells[5].Value=qty.ToString(ColInv);}
   RefreshScheduleMarks(m);foreach(var mark in s.Marks){var row=est.Rows.Cast<DataGridViewRow>().FirstOrDefault(r=>!r.IsNewRow&&Convert.ToString(r.Cells[0].Value)==mark&&Convert.ToString(r.Cells[markAt].Value)=="");if(row!=null)row.Cells[markAt].Value=mark;}}
  finally{filling=before;}RefreshScheduleMarks(m);Recalculate();return types;}
}
}
