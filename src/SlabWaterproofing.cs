using System;using System.Collections.Generic;using System.Globalization;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Waterproofing area of slabs marked "Roof deck / waterproofing" or "Concrete gutter / waterproofing"
// (owner, 17 November 2026): top area = short side x long side x quantity. It is a computed BOQ quantity the
// owner maps once to the waterproofing pay item already in the BACKUP (e.g. 1016(1)a or 1016(2)). The concrete of
// these rows still goes to the waterproofing-admixture concrete item, as before.
partial class SlabEstimatorForm {
 internal List<KeyValuePair<string,double>> WaterproofingAreas(){var list=new List<KeyValuePair<string,double>>();
  foreach(DataGridViewRow row in grid.Rows){if(row.IsNewRow||row.Tag==null)continue;string support=Cell(row,5);if(support.IndexOf("waterproof",StringComparison.OrdinalIgnoreCase)<0)continue;
   double s,l;int q;if(!double.TryParse(Cell(row,2),NumberStyles.Float,CultureInfo.InvariantCulture,out s)||!double.TryParse(Cell(row,3),NumberStyles.Float,CultureInfo.InvariantCulture,out l)||!int.TryParse(Cell(row,4),NumberStyles.Integer,CultureInfo.InvariantCulture,out q)||s<=0||l<=0||q<=0)continue;
   list.Add(new KeyValuePair<string,double>("Slab "+Cell(row,1)+" | "+Cell(row,0)+" ("+support+") "+s.ToString("0.###",CultureInfo.InvariantCulture)+" x "+l.ToString("0.###",CultureInfo.InvariantCulture)+" x "+q,s*l*q));}
  return list;}
}
partial class ProgramOfWorksForm {
 internal const string SlabWaterproofingDescription="Waterproofing - roof deck / concrete gutter slabs (top area)";
 BillQuantity SlabWaterproofingQuantity(){var areas=slab==null?new List<KeyValuePair<string,double>>():slab.WaterproofingAreas();if(areas.Count==0)return null;var item=new BillQuantity{Section="Waterproofing",Description=SlabWaterproofingDescription,Unit="m2",Quantity=areas.Sum(a=>a.Value)};foreach(var a in areas)item.Sources.Add(new BillQuantity{Description=a.Key,Quantity=a.Value,Unit="m2"});return item;}
}
}
