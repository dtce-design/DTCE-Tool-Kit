using System;using System.Collections.Generic;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Owner (18 November 2026, "kapag nag-delete ako ng previous rows, bakit ganito" - "Wall ID W013 has no CHB row; not
// deducted"; then "yung lumang opening na naka-save sa wall ID, dapat yung deleted rows naalis na unless nag-undo"):
// when a wall row is deleted, the openings assigned to it are removed with it. They come back only with Undo, i.e.
// when the same wall row returns with the same inputs - not when a new wall later receives the same automatic ID.
partial class ProgramOfWorksForm {
 sealed class ParkedWallOpenings {internal string Wall,Fingerprint;internal readonly List<OpeningLink> Links=new List<OpeningLink>();internal readonly List<object[]> Manual=new List<object[]>();}
 readonly List<ParkedWallOpenings> parkedWallOpenings=new List<ParkedWallOpenings>();
 readonly Dictionary<string,string> wallFingerprints=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
 static string WallFingerprint(DataGridViewRow row){return string.Join("\u001f",new[]{1,2,3,4,18,19}.Where(c=>c<row.Cells.Count).Select(c=>Cell(row,c)).ToArray());}
 void PruneDeletedWallLinks(){if(manualOpeningsGrid==null)return;
  var rows=MasonryGrids().Where(g=>g!=null).SelectMany(g=>g.Rows.Cast<DataGridViewRow>()).Where(r=>!r.IsNewRow&&Cell(r,0)!="").ToList();
  var present=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);foreach(var r in rows)if(!present.ContainsKey(Cell(r,0)))present[Cell(r,0)]=WallFingerprint(r);
  /* Undo: the deleted wall is back with the same inputs */
  foreach(var p in parkedWallOpenings.ToList()){string now;if(!present.TryGetValue(p.Wall,out now)||now!=p.Fingerprint)continue;parkedWallOpenings.Remove(p);
   foreach(var link in p.Links)if(!openingAssignments.Any(x=>string.Equals(x.Wall,link.Wall,StringComparison.OrdinalIgnoreCase)&&string.Equals(x.Schedule,link.Schedule,StringComparison.OrdinalIgnoreCase)))openingAssignments.Add(link);
   foreach(var values in p.Manual){var row=manualOpeningsGrid.Rows[manualOpeningsGrid.Rows.Add()];for(int c=0;c<values.Length&&c<row.Cells.Count;c++)if(!(row.Cells[c] is DataGridViewButtonCell))row.Cells[c].Value=values[c];}}
  /* a wall ID that no longer exists, or now belongs to a different (new) wall: its old openings are removed */
  var gone=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  foreach(var link in openingAssignments)if(!present.ContainsKey(link.Wall))gone.Add(link.Wall);
  foreach(DataGridViewRow row in manualOpeningsGrid.Rows)if(!row.IsNewRow&&Cell(row,2)!=""&&!present.ContainsKey(Cell(row,2)))gone.Add(Cell(row,2));
  foreach(string wall in gone){string fingerprint;wallFingerprints.TryGetValue(wall,out fingerprint);var parked=new ParkedWallOpenings{Wall=wall,Fingerprint=fingerprint??"\u0000"};
   foreach(var link in openingAssignments.Where(l=>string.Equals(l.Wall,wall,StringComparison.OrdinalIgnoreCase)).ToList()){openingAssignments.Remove(link);parked.Links.Add(link);}
   foreach(var row in manualOpeningsGrid.Rows.Cast<DataGridViewRow>().Where(r=>!r.IsNewRow&&string.Equals(Cell(r,2),wall,StringComparison.OrdinalIgnoreCase)).ToList()){parked.Manual.Add(row.Cells.Cast<DataGridViewCell>().Select(c=>c.Value).ToArray());manualOpeningsGrid.Rows.Remove(row);}
   parkedWallOpenings.Add(parked);}
  /* a new wall that later reuses a deleted wall's ID has different inputs, so it does not inherit the parked openings */
  wallFingerprints.Clear();foreach(var pair in present)wallFingerprints[pair.Key]=pair.Value;}
 void ClearParkedWallOpenings(){parkedWallOpenings.Clear();wallFingerprints.Clear();}
}
}
