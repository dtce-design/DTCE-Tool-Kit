using System;using System.Collections.Generic;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* Door and window finishing hardware as bought (owner, 23 November 2026):
   - hinges are Ball Bearing Stainless Door Hinges sold 2 pcs per pack, in four sizes ("ayusin quantities sa hinges based dito");
   - a double-leaf door gets 1 leverset, 1 pc 4" Chrome Plated Chain Bolt and 1 pc 4" Chrome Plated Foot Bolt;
   - optional deadbolt per door: Double Cylinder or Single Cylinder with Thumbturn, Satin Stainless Steel.
   Sizes are height x width (inches) x leaf thickness (mm), as in the supplier list. The automatic size follows the leaf: windows and
   cabinet leaves 3.0 x 3.0; doors with a leaf up to 0.80 m wide 3.5 x 3.5; up to 0.90 m 4.0 x 3.0; wider or exterior doors 4.0 x 4.0.
   Any row can choose another size. */
static class DoorHardware {
 internal const string Automatic="Automatic";
 internal static readonly string[] HingeSizes={"3.0x3.0x2.5mm","3.5x3.5x2.5mm","4.0x3.0x3.0mm","4.0x4.0x3.0mm"};
 internal const string None="None",DoubleCylinder="Double Cylinder",SingleThumbturn="Single Cylinder with Thumbturn";
 internal static readonly string[] Deadbolts={None,DoubleCylinder,SingleThumbturn};
 internal static string HingeName(string size){return "Ball Bearing Stainless Door Hinge "+size+" (2pcs/pack)";}
 internal const string ChainBolt="4\" Chrome Plated Chain Bolt",FootBolt="4\" Chrome Plated Foot Bolt";
 internal static string DeadboltName(string kind){return kind==DoubleCylinder?"Double Cylinder, Satin Stainless Steel Deadbolt":"Single Cylinder with Thumbturn, Satin Stainless Steel Deadbolt";}
 /* automatic hinge size of one leaf */
 internal static string AutoSize(string kind,double leafWidth,bool exterior){if(kind!="Door")return HingeSizes[0];if(exterior||leafWidth>.90+1e-9)return HingeSizes[3];if(leafWidth>.80+1e-9)return HingeSizes[2];return HingeSizes[1];}
 internal static int Packs(int pieces){return pieces<=0?0:(pieces+1)/2;}
 /* every hardware name this module adds to the Bill of Quantities (all go to 1004(2) Finishing Hardware) */
 internal static IEnumerable<string> Names(){foreach(var s in HingeSizes)yield return HingeName(s);yield return ChainBolt;yield return FootBolt;yield return DeadboltName(DoubleCylinder);yield return DeadboltName(SingleThumbturn);}
}
partial class OpeningItem {
 /* "" or Automatic = by the leaf width; otherwise one of DoorHardware.HingeSizes */
 public string HingeSize="";public string Deadbolt=DoorHardware.None;
 /* leaf widths of one door unit (owner, 23 November 2026: "minsan may double leaf na magkaiba ang size nilang dalawa"): typed as
    "0.90+0.30" (m) in Leaf widths, one value per leaf; blank = the leaf width split equally between the leaves */
 public string LeafWidths="";
 internal static double[] ParseLeafWidths(string text){if(string.IsNullOrWhiteSpace(text))return new double[0];var parts=text.Split(new[]{'+',',',';','/'},StringSplitOptions.RemoveEmptyEntries);var list=new List<double>();
  foreach(var p in parts){double v;if(!double.TryParse(p.Trim(),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out v)||double.IsNaN(v)||double.IsInfinity(v)||v<=0)throw new Exception("Leaf widths: enter the width of each leaf in m, for example 0.90+0.30.");if(v>20)v/=1000;/* typed in mm */list.Add(v);}return list.ToArray();}
 public double[] DoorLeaves(){int n=Math.Max(1,Leaves);var typed=ParseLeafWidths(LeafWidths);if(typed.Length>0){if(typed.Length!=n)throw new Exception("Opening "+Id+": "+typed.Length+" leaf width(s) for "+n+" leaves/unit; enter one width per leaf (for example 0.90+0.30).");double total=Wood?LeafWidth:Width;if(typed.Sum()>total+0.005)throw new Exception("Opening "+Id+": the leaf widths ("+typed.Sum().ToString("0.###",System.Globalization.CultureInfo.InvariantCulture)+" m) are wider than the "+(Wood?"leaf width":"opening")+" ("+total.ToString("0.###",System.Globalization.CultureInfo.InvariantCulture)+" m).");return typed;}
  double each=(Wood?LeafWidth:Width)/n;return Enumerable.Repeat(each,n).ToArray();}
 /* hinges of one unit by size: each leaf has its own count and (automatic) size */
 public Dictionary<string,int> HingesBySizePerUnit(){var d=new Dictionary<string,int>();if(EffectiveHinges<=0)return d;bool chosen=!string.IsNullOrEmpty(HingeSize)&&DoorHardware.HingeSizes.Contains(HingeSize);
  if(Kind!="Door"){d[chosen?HingeSize:DoorHardware.HingeSizes[0]]=EffectiveHinges;return d;}
  foreach(var w in DoorLeaves()){string size=chosen?HingeSize:DoorHardware.AutoSize(Kind,w,Exterior);int n=OpeningHardware.PerLeaf(Wood?LeafHeight:Height,w);int prev;d[size]=(d.TryGetValue(size,out prev)?prev:0)+n;}return d;}
 bool DoorHardwareApplies{get{return Kind=="Door"&&EffectiveLocks>0;}}
 /* one chain bolt and one foot bolt for each double (or more) leaf door unit */
 public int ChainBolts{get{return DoorHardwareApplies&&Leaves>=2?Quantity:0;}}public int FootBolts{get{return ChainBolts;}}
 public int Deadbolts{get{return DoorHardwareApplies&&Deadbolt!=DoorHardware.None&&DoorHardware.Deadbolts.Contains(Deadbolt)?Quantity:0;}}
}
partial class ProgramOfWorksForm {
 internal const string HingeSizeColumn="OpeningHingeSize",DeadboltColumn="OpeningDeadbolt",LeafWidthsColumn="OpeningLeafWidths";
 void AddDoorHardwareColumns(DataGridView g){
  var size=new DataGridViewComboBoxColumn{Name=HingeSizeColumn,HeaderText="Hinge size",Width=130,FlatStyle=FlatStyle.Flat,ToolTipText="Ball Bearing Stainless Door Hinge (2pcs/pack). Automatic: windows 3.0x3.0; door leaf up to 0.80 m 3.5x3.5; up to 0.90 m 4.0x3.0; wider or exterior 4.0x4.0."};
  size.Items.Add(DoorHardware.Automatic);size.Items.AddRange(DoorHardware.HingeSizes);size.DefaultCellStyle.BackColor=ModernUi.Input;g.Columns.Add(size);
  var widths=new DataGridViewTextBoxColumn{Name=LeafWidthsColumn,HeaderText="Leaf widths m",Width=110,ToolTipText="Doors with leaves of different widths: one width per leaf, for example 0.90+0.30. Blank = equal leaves. Each leaf gets its own hinges."};widths.DefaultCellStyle.BackColor=ModernUi.Input;g.Columns.Add(widths);
  var bolt=new DataGridViewComboBoxColumn{Name=DeadboltColumn,HeaderText="Deadbolt",Width=150,FlatStyle=FlatStyle.Flat,ToolTipText="Optional deadbolt for each door unit (Satin Stainless Steel). Double-leaf doors also get one 4\" chain bolt and one 4\" foot bolt."};
  bolt.Items.AddRange(DoorHardware.Deadbolts);bolt.DefaultCellStyle.BackColor=ModernUi.Input;g.Columns.Add(bolt);
  /* an empty cell shows and means Automatic / None, so no value is written into new rows */size.DefaultCellStyle.NullValue=DoorHardware.Automatic;bolt.DefaultCellStyle.NullValue=DoorHardware.None;
  g.CellValueChanged+=(s,e)=>{if(filling||e.RowIndex<0)return;var n=g.Columns[e.ColumnIndex].Name;if(n==HingeSizeColumn||n==DeadboltColumn||n==LeafWidthsColumn)Recalculate();};}
 static void PlaceDoorHardwareColumns(DataGridView g){if(g==null||!g.Columns.Contains(HingeSizeColumn))return;int after=g.Columns[12].DisplayIndex;g.Columns[LeafWidthsColumn].DisplayIndex=Math.Min(g.Columns.Count-1,g.Columns[20].DisplayIndex+1);after=g.Columns[12].DisplayIndex;g.Columns[HingeSizeColumn].DisplayIndex=Math.Min(g.Columns.Count-1,after+1);g.Columns[DeadboltColumn].DisplayIndex=Math.Min(g.Columns.Count-1,after+2);}
 static string RowHingeSize(DataGridViewRow row){if(row.DataGridView==null||!row.DataGridView.Columns.Contains(HingeSizeColumn))return "";var v=Convert.ToString(row.Cells[HingeSizeColumn].Value);return DoorHardware.HingeSizes.Contains(v)?v:"";}
 static string RowDeadbolt(DataGridViewRow row){if(row.DataGridView==null||!row.DataGridView.Columns.Contains(DeadboltColumn))return DoorHardware.None;var v=Convert.ToString(row.Cells[DeadboltColumn].Value);return DoorHardware.Deadbolts.Contains(v)?v:DoorHardware.None;}
 /* saved as entry attributes hs / db, only when not automatic / none, so older files open unchanged */
 static string RowLeafWidths(DataGridViewRow row){if(row.DataGridView==null||!row.DataGridView.Columns.Contains(LeafWidthsColumn))return "";return Convert.ToString(row.Cells[LeafWidthsColumn].Value).Trim();}
 static void WriteDoorHardware(DataGridViewRow row,System.Xml.XmlElement entry){string lw=RowLeafWidths(row);if(lw!="")entry.SetAttribute("lw",lw);string s=RowHingeSize(row);if(s!="")entry.SetAttribute("hs",s);string d=RowDeadbolt(row);if(d!=DoorHardware.None)entry.SetAttribute("db",d);}
 static void ReadDoorHardware(DataGridViewRow row,System.Xml.XmlElement entry){if(row.DataGridView==null||!row.DataGridView.Columns.Contains(HingeSizeColumn))return;string s=entry.GetAttribute("hs");row.Cells[HingeSizeColumn].Value=DoorHardware.HingeSizes.Contains(s)?s:null;string d=entry.GetAttribute("db");row.Cells[DeadboltColumn].Value=DoorHardware.Deadbolts.Contains(d)&&d!=DoorHardware.None?d:null;row.Cells[LeafWidthsColumn].Value=entry.GetAttribute("lw");}
 /* the Bill of Quantities lines: hinge packs by size, chain / foot bolts and deadbolts */
 List<BillQuantity> DoorHardwareQuantities(){var rows=new List<BillQuantity>();var items=openings.Where(o=>o.Material!="Manual"&&o.Material!="Opening").ToList();
  var bySize=items.Where(o=>o.Hinges>0).Select(o=>new{o,d=o.HingesBySizePerUnit()}).ToList();
  foreach(var size in DoorHardware.HingeSizes){var list=bySize.Where(x=>x.d.ContainsKey(size)).Select(x=>new{x.o,pcs=checked(x.d[size]*x.o.Quantity)}).ToList();int pcs=list.Sum(x=>x.pcs);if(pcs<=0)continue;
   var q=new BillQuantity{Section="Doors & Windows",Description=DoorHardware.HingeName(size),Quantity=DoorHardware.Packs(pcs),Unit="pack"};q.Sources.AddRange(MergeSources(list.Select(x=>SourceQuantity(x.o.Id+" | "+x.o.Description+" ("+x.pcs+" pcs)",x.pcs/2.0,"pack"))));rows.Add(q);}
  Action<string,Func<OpeningItem,int>> each=(name,count)=>{var list=items.Where(o=>count(o)>0).ToList();int n=list.Sum(count);if(n<=0)return;var q=new BillQuantity{Section="Doors & Windows",Description=name,Quantity=n,Unit="pcs"};q.Sources.AddRange(MergeSources(list.Select(o=>SourceQuantity(o.Id+" | "+o.Description,count(o),"pcs"))));rows.Add(q);};
  each(DoorHardware.ChainBolt,o=>o.ChainBolts);each(DoorHardware.FootBolt,o=>o.FootBolts);
  each(DoorHardware.DeadboltName(DoorHardware.DoubleCylinder),o=>o.Deadbolt==DoorHardware.DoubleCylinder?o.Deadbolts:0);each(DoorHardware.DeadboltName(DoorHardware.SingleThumbturn),o=>o.Deadbolt==DoorHardware.SingleThumbturn?o.Deadbolts:0);
  return rows;}
 internal static bool IsDoorHardwareKey(string key){if(!key.StartsWith("Doors & Windows|",StringComparison.Ordinal))return false;string rest=key.Substring("Doors & Windows|".Length);return DoorHardware.Names().Any(n=>rest.StartsWith(n+"|",StringComparison.Ordinal)||rest==n);}
}
}
