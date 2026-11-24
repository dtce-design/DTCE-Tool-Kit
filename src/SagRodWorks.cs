using System;using System.Collections.Generic;using System.Globalization;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Sag rods calculation sheet in Structural Steel Works (owner, 17 November 2026). Each row is one sag-rod type:
// plain round bar of the chosen diameter, piece length including threaded ends, and the same quantity factors as the
// other steel sheets (QTY x Units x Sets x Multiplier, all required). Nuts and washers are not included
// (owner, 18 November 2026). All cuts of the same diameter share 6 m stocks with offcut reuse (SteelCutPlanner); weight from
// 7850 kg/m3 (plain round bar is not in the steel catalogue); painting uses the installed length.
class SagRodTakeoff {public string Id,Description,Profile;public double PieceLength,KgM,PaintPerMetre;public int Pieces;public SteelCutDemand Demand;
 public double Installed{get{return PieceLength*Pieces;}}public double InstalledKg{get{return Installed*KgM;}}public double Paint{get{return Installed*PaintPerMetre;}}}
partial class ProgramOfWorksForm {
 internal const string SagRodPage="Sag Rods";
 static readonly string[] SagRodDiameters={"10","12","16","20"};
 ExtraWorkGrid sagRodGrid;readonly List<SagRodTakeoff> sagRods=new List<SagRodTakeoff>();List<SteelStock> sagRodStocks=new List<SteelStock>();
 void AddSagRodWorks(){
  sagRodGrid=NewExtraGrid("SagRods",SagRodPage,"Structural Steel Works",new[]{"Sag rod ID","Description / location","Rod dia mm","Rod length m (each, incl. threads)","QTY","Units","Sets","Multiplier","Total pcs","Installed length m","kg/m","Installed kg","Purchase kg (6 m stock)","Paint m2 (installed)","Status"},8,new Dictionary<int,string>{{2,"12"}},
   "One row per sag-rod type. Rod length = clear distance between purlins plus both threaded ends. Total pcs = QTY x Units x Sets x Multiplier (all required; use 1 where not applicable). Nuts and washers are not included. Cuts of the same diameter share 6 m stocks and reuse offcuts; painting uses the installed length.");
  var g=sagRodGrid.Grid;ExtraChoice(g,2,SagRodDiameters);g.Columns[1].Width=220;g.Columns[3].Width=170;
  g.Columns[5].ToolTipText="Number of units in one set (e.g. bays per frame line). Required.";g.Columns[6].ToolTipText="Number of sets (e.g. buildings). Required.";g.Columns[7].ToolTipText="Additional multiplier for the whole row. Required.";}
 static SteelProfile SagRodProfile(string dia){return RailingSections.Find("Round bar "+dia+"mm");}
 void RecalculateSagRods(){sagRods.Clear();sagRodStocks=new List<SteelStock>();if(sagRodGrid==null)return;var g=sagRodGrid.Grid;var ids=new HashSet<string>(StringComparer.OrdinalIgnoreCase);var demands=new List<SteelCutDemand>();
  foreach(DataGridViewRow row in g.Rows){if(row.IsNewRow)continue;row.Tag=null;for(int c=sagRodGrid.Inputs;c<g.Columns.Count;c++)row.Cells[c].Value="";if(Empty(row))continue;
   try{if(Cell(row,0)==""||!ids.Add(Cell(row,0)))throw new Exception("Enter a unique sag rod ID.");RequireDescription(row);var profile=SagRodProfile(Cell(row,2));if(profile==null)throw new Exception("Select the rod diameter.");
    double piece=Number(row,3);if(!(piece>0))throw new Exception("Enter the rod length.");if(new[]{4,5,6,7}.Any(c=>Cell(row,c)==""))throw new Exception("Enter QTY, Units, Sets and Multiplier (use 1 where not applicable).");int pieces=checked(Count(row,4)*Count(row,5)*Count(row,6)*Count(row,7));int segments=RailingSections.Segments(piece);
    var t=new SagRodTakeoff{Id=Cell(row,0),Description=Cell(row,1),Profile=profile.Name,PieceLength=piece,KgM=profile.KgM,PaintPerMetre=SteelPainting.AreaPerMetre(profile),Pieces=pieces};
    var demand=new SteelCutDemand{Member="Sag rod "+t.Id,Specification=profile.Name,Length=piece/segments,Quantity=checked(pieces*segments),KgM=profile.KgM};t.Demand=demand;demands.Add(demand);sagRods.Add(t);row.Tag=t;
    row.Cells[8].Value=pieces;Measure(row,9,t.Installed);row.Cells[10].Value=profile.KgM.ToString("0.###",CultureInfo.InvariantCulture);Measure(row,11,t.InstalledKg);Measure(row,13,t.Paint);
    row.Cells[14].Value=segments>1?"Estimated - each "+piece.ToString("0.###",CultureInfo.InvariantCulture)+" m rod spliced into "+segments+" cuts":"Estimated";}
   catch(Exception ex){row.Cells[g.Columns.Count-1].Value=ex.Message;errors.Add("Sag rod "+Cell(row,0)+": "+ex.Message);}}
  if(demands.Count==0)return;try{sagRodStocks=SteelCutPlanner.Allocate(demands);foreach(DataGridViewRow row in g.Rows){var t=row.Tag as SagRodTakeoff;if(t!=null)Measure(row,12,SteelCutPlanner.PurchaseKg(sagRodStocks,t.Demand));}}catch(Exception ex){errors.Add("Sag rod cutting: "+ex.Message);}}
 internal List<BillQuantity> SagRodQuantities(){var list=new List<BillQuantity>();if(sagRods.Count==0)return list;
  var buy=new BillQuantity{Section="Sag Rods",Description="Sag rods PURCHASE weight (6 m stock, offcuts reused)",Unit="kg",Quantity=sagRodStocks.Sum(s=>s.Length*s.KgM)};
  foreach(var group in sagRodStocks.GroupBy(s=>s.Specification))buy.Sources.Add(new BillQuantity{Description=group.Key+": "+group.Count()+" stock(s) of 6 m; installed "+group.Sum(s=>s.Net).ToString("0.###",CultureInfo.InvariantCulture)+" m; final reusable offcut "+group.Sum(s=>s.Remaining).ToString("0.###",CultureInfo.InvariantCulture)+" m",Quantity=group.Sum(s=>s.Length*s.KgM),Unit="kg"});
  var installed=new BillQuantity{Section="Sag Rods",Description="Sag rods installed weight (reference)",Unit="kg",Quantity=sagRods.Sum(r=>r.InstalledKg)};
  var paint=new BillQuantity{Section="Sag Rods",Description="Sag rod metal painting - installed",Unit="m2",Quantity=sagRods.Sum(r=>r.Paint)};
  foreach(var r in sagRods){string head=r.Id+" | "+r.Profile+": ";installed.Sources.Add(new BillQuantity{Description=head+r.Pieces+" pcs x "+r.PieceLength.ToString("0.###",CultureInfo.InvariantCulture)+" m",Quantity=r.InstalledKg,Unit="kg"});paint.Sources.Add(new BillQuantity{Description=head+r.Installed.ToString("0.###",CultureInfo.InvariantCulture)+" m x "+r.PaintPerMetre.ToString("0.####",CultureInfo.InvariantCulture)+" m2/m",Quantity=r.Paint,Unit="m2"});}
  list.Add(buy);list.Add(installed);list.Add(paint);return list;}
}
}
