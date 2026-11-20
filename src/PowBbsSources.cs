using System;using System.Collections.Generic;using System.Globalization;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* the open Program of Works hands its column, slab and footing take-off to the Bar Bending Schedule (owner, 20 November 2026) */
partial class ProgramOfWorksForm {
 internal static WeakReference Live;
 internal static ProgramOfWorksForm Current{get{var f=Live==null?null:Live.Target as ProgramOfWorksForm;return f==null||f.IsDisposed?null:f;}}
 static int Qty(string q){double v;return double.TryParse(q,NumberStyles.Float,CultureInfo.InvariantCulture,out v)&&v>=1?(int)Math.Round(v):1;}
 internal List<BbsMemberSource> BbsSources(){var list=new List<BbsMemberSource>();
  ExtraWorkGrid columns;if(extraGrids.TryGetValue("StructuralColumn",out columns))foreach(DataGridViewRow row in columns.Grid.Rows){var q=row.Tag as MiscStructureQuantity;if(q==null||q.Steel==null)continue;double w,d;double.TryParse(Convert.ToString(row.Cells[3].Value,CultureInfo.InvariantCulture),NumberStyles.Float,CultureInfo.InvariantCulture,out w);double.TryParse(Convert.ToString(row.Cells[4].Value,CultureInfo.InvariantCulture),NumberStyles.Float,CultureInfo.InvariantCulture,out d);
   list.Add(new BbsMemberSource{Kind="Column",Id=q.Id,Description=q.Description,Quantity=Math.Max(1,q.Quantity),WidthMm=w,DepthMm=d,Steel=q.Steel});}
  if(slab!=null)foreach(var t in slab.Takeoffs())list.Add(new BbsMemberSource{Kind="Slab",Id=string.IsNullOrEmpty(t.Steel.Beam)?t.Steel.Description:t.Steel.Beam,Description=t.Steel.Description,Quantity=Qty(t.Steel.Quantity),Steel=t.Steel});
  foreach(var f in footingResults)if(f!=null&&f.Steel!=null)list.Add(new BbsMemberSource{Kind="Footing",Id=f.Id,Description=f.Description,Quantity=Math.Max(1,f.Quantity),Steel=f.Steel});
  foreach(var f in wallFootingResults)if(f!=null&&f.Steel!=null)list.Add(new BbsMemberSource{Kind="Wall footing",Id=f.Id,Description=f.Description,Quantity=Math.Max(1,f.Quantity),Steel=f.Steel});
  return list;}
}
partial class SlabEstimatorForm {
 internal List<SlabTakeoff> Takeoffs(){var list=new List<SlabTakeoff>();foreach(DataGridViewRow r in grid.Rows){var t=r.Tag as SlabTakeoff;if(t!=null&&t.Steel!=null)list.Add(t);}return list;}
}
}
