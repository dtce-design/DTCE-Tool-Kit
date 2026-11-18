using System;using System.Collections.Generic;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Owner (18 November 2026, "kapag nag-delete ako ng previous rows, bakit ganito" - "Wall ID W013 has no CHB row; not
// deducted"): when a wall row is deleted, the openings assigned to it are released instead of being reported as an
// input error. They are parked for this session, so Undo (or re-entering the same Wall ID) brings them back.
partial class ProgramOfWorksForm {
 readonly List<OpeningLink> parkedOpeningLinks=new List<OpeningLink>();
 readonly List<object[]> parkedManualOpenings=new List<object[]>();
 void PruneDeletedWallLinks(){if(manualOpeningsGrid==null)return;
  var present=new HashSet<string>(MasonryGrids().Where(g=>g!=null).SelectMany(g=>g.Rows.Cast<DataGridViewRow>()).Where(r=>!r.IsNewRow).Select(r=>Cell(r,0)).Where(s=>s!=""),StringComparer.OrdinalIgnoreCase);
  /* restore what belongs to a wall ID that is back */
  foreach(var link in parkedOpeningLinks.Where(l=>present.Contains(l.Wall)).ToList()){parkedOpeningLinks.Remove(link);if(!openingAssignments.Any(x=>string.Equals(x.Wall,link.Wall,StringComparison.OrdinalIgnoreCase)&&string.Equals(x.Schedule,link.Schedule,StringComparison.OrdinalIgnoreCase)))openingAssignments.Add(link);}
  foreach(var values in parkedManualOpenings.Where(v=>present.Contains(Convert.ToString(v[2]))).ToList()){parkedManualOpenings.Remove(values);var row=manualOpeningsGrid.Rows[manualOpeningsGrid.Rows.Add()];for(int c=0;c<values.Length&&c<row.Cells.Count;c++)if(!(row.Cells[c] is DataGridViewButtonCell))row.Cells[c].Value=values[c];}
  /* release what belongs to a wall ID that no longer exists */
  foreach(var link in openingAssignments.Where(l=>!present.Contains(l.Wall)).ToList()){openingAssignments.Remove(link);parkedOpeningLinks.Add(link);}
  foreach(var row in manualOpeningsGrid.Rows.Cast<DataGridViewRow>().Where(r=>!r.IsNewRow&&Cell(r,2)!=""&&!present.Contains(Cell(r,2))).ToList()){parkedManualOpenings.Add(row.Cells.Cast<DataGridViewCell>().Select(c=>c.Value).ToArray());manualOpeningsGrid.Rows.Remove(row);}}
}
}
