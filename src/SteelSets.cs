using System;using System.Collections.Generic;using System.Drawing;using System.Linq;using System.Windows.Forms;using System.Xml;
namespace BeamScheduleFormatter {
// Steel Works quantity breakdown (owner, 17 November 2026): Qty / unit x Units / set x Sets on every Structural
// Steel sheet (purlins, angle bars, machine / anchor bolts, plates, cross bracing, fascia frame). The existing Qty
// column becomes "Qty / unit"; blank Units / set or Sets count as 1, so older projects compute exactly as before.
// The two new inputs are named columns placed before Status and saved as their own attributes (u, s) so that the
// index-based c0..cN entries of older files are unchanged.
partial class ProgramOfWorksForm {
 internal const string SteelUnitsColumn="SteelUnitsPerSet",SteelSetsColumn="SteelSets",SteelMultiplierColumn="SteelMultiplier";
 /* Owner (17 November 2026): one more factor, "Multiplier" (caption to be renamed when the owner chooses one). */
 static readonly Dictionary<string,int> SteelQtyColumns=new Dictionary<string,int>(StringComparer.Ordinal){{"SteelPurlins",5},{"SteelAngles",5},{"MachineBolts",5},{"AnchorBolts",5},{"SteelPlates",6},{"CrossBracing",6},{"FasciaFrame",6}};
 void AddSteelSetColumns(){foreach(var item in SteelTables()){var g=item.Value;if(g.Columns.Contains(SteelUnitsColumn))continue;int qty=SteelQtyColumns[item.Key];
   string caption=g.Columns[qty].HeaderText;g.Columns[qty].HeaderText=caption=="Qty"?"Qty / unit":caption+" / unit";g.Columns[qty].ToolTipText="Quantity in ONE unit (e.g. per truss or per frame). Total = Qty / unit x Units / set x Sets x Multiplier.";
   foreach(var spec in new[]{new[]{SteelUnitsColumn,"Units / set","Number of units in one set (e.g. 6 trusses per building). Blank = 1."},new[]{SteelSetsColumn,"Sets","Number of sets (e.g. 2 buildings). Blank = 1."},new[]{SteelMultiplierColumn,"Multiplier","Additional multiplier applied to the whole row. Blank = 1."}}){
    var column=new DataGridViewTextBoxColumn{Name=spec[0],HeaderText=spec[1],ToolTipText=spec[2],Width=80};column.DefaultCellStyle.BackColor=ModernUi.Input;g.Columns.Insert(g.Columns.Count-1,column);}}}
 // Shown right after Qty / unit (standard WinForms DisplayIndex semantics; Mono's DataGridView does not reorder reliably).
 void PlaceSteelSetColumns(){foreach(var item in SteelTables()){var g=item.Value;if(!g.Columns.Contains(SteelUnitsColumn))continue;var qty=g.Columns[SteelQtyColumns[item.Key]];
  g.Columns[SteelUnitsColumn].DisplayIndex=Math.Min(g.Columns.Count-1,qty.DisplayIndex+1);g.Columns[SteelSetsColumn].DisplayIndex=Math.Min(g.Columns.Count-1,g.Columns[SteelUnitsColumn].DisplayIndex+1);g.Columns[SteelMultiplierColumn].DisplayIndex=Math.Min(g.Columns.Count-1,g.Columns[SteelSetsColumn].DisplayIndex+1);}}
 static bool IsSteelSetColumn(DataGridViewColumn c){return c.Name==SteelUnitsColumn||c.Name==SteelSetsColumn||c.Name==SteelMultiplierColumn;}
 static int SteelSetFactor(DataGridViewRow row,string name){if(!row.DataGridView.Columns.Contains(name))return 1;int c=row.DataGridView.Columns[name].Index;return Cell(row,c)==""?1:Count(row,c);}
 // Total pieces of a steel row = Qty / unit x Units / set x Sets.
 static int SteelQty(DataGridViewRow row,int qtyColumn){return checked(Count(row,qtyColumn)*SteelSetFactor(row,SteelUnitsColumn)*SteelSetFactor(row,SteelSetsColumn)*SteelSetFactor(row,SteelMultiplierColumn));}
 static string SteelQtyText(DataGridViewRow row,int qtyColumn){int u=SteelSetFactor(row,SteelUnitsColumn),s=SteelSetFactor(row,SteelSetsColumn),m=SteelSetFactor(row,SteelMultiplierColumn);return u==1&&s==1&&m==1?Cell(row,qtyColumn):Cell(row,qtyColumn)+" x "+u+" units x "+s+" sets"+(m==1?"":" x "+m);}
 static void WriteSteelSets(string section,DataGridViewRow row,XmlElement entry){if(!SteelQtyColumns.ContainsKey(section)||!row.DataGridView.Columns.Contains(SteelUnitsColumn))return;
  string u=Cell(row,row.DataGridView.Columns[SteelUnitsColumn].Index),s=Cell(row,row.DataGridView.Columns[SteelSetsColumn].Index);if(u!="")entry.SetAttribute("u",u);if(s!="")entry.SetAttribute("s",s);if(row.DataGridView.Columns.Contains(SteelMultiplierColumn)){string m=Cell(row,row.DataGridView.Columns[SteelMultiplierColumn].Index);if(m!="")entry.SetAttribute("m",m);}}
 static void ReadSteelSets(string section,DataGridViewRow row,XmlElement entry){if(!SteelQtyColumns.ContainsKey(section)||!row.DataGridView.Columns.Contains(SteelUnitsColumn))return;
  row.Cells[SteelUnitsColumn].Value=entry.GetAttribute("u");row.Cells[SteelSetsColumn].Value=entry.GetAttribute("s");if(row.DataGridView.Columns.Contains(SteelMultiplierColumn))row.Cells[SteelMultiplierColumn].Value=entry.GetAttribute("m");}
}
}
