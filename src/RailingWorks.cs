using System;using System.Collections.Generic;using System.Globalization;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Railing estimate in Finishing Works (owner, 17 November 2026).
// Each row is one railing member: top / mid / bottom rail, post, baluster or other. Sections: pipes and rectangular /
// square tubes from the Regan handbook catalogue already in the app; round and square bars are computed from
// 7850 kg/m3 (verify sizes against the Regan handbook). Pieces per run = count, or ceil(run / spacing) + 1, or 1 for
// a rail. Cuts longer than 6 m are split into equal pieces (approved splice assumed). All cuts of the same size share
// 6 m stocks with best-fit offcut reuse (SteelCutPlanner). Painting is the installed length x painted perimeter.
static class RailingSections {
 internal static readonly string[] Families={"Pipe","Rectangular tube","Square tube","Round bar","Square bar"};
 internal static readonly string[] Members={"Top rail","Mid rail","Bottom rail","Post","Baluster","Other"};
 static List<SteelProfile> bars;
 static List<SteelProfile> Bars(){if(bars!=null)return bars;var list=new List<SteelProfile>();
  foreach(int d in new[]{8,10,12,16,20,25})list.Add(new SteelProfile{Name="Round bar "+d+"mm",Kind="Round bar",H=d,B=d,T=d,KgM=Math.Round(Math.PI*d*d/4e6*SteelWeight.Density,3),Source="Computed 7850 kg/m3 (verify in Regan handbook)"});
  foreach(int a in new[]{10,12,16,20,25})list.Add(new SteelProfile{Name="Square bar "+a+"x"+a+"mm",Kind="Square bar",H=a,B=a,T=a,KgM=Math.Round(a*a/1e6*SteelWeight.Density,3),Source="Computed 7850 kg/m3 (verify in Regan handbook)"});
  bars=list;return list;}
 internal static IEnumerable<SteelProfile> For(string family){switch(family){
  case "Pipe":return SteelCatalog.Profiles.Where(p=>p.Kind=="HSS circle / pipe");
  case "Rectangular tube":return SteelCatalog.Profiles.Where(p=>p.Kind=="HSS rectangle"&&Math.Abs(p.H-p.B)>1e-9);
  case "Square tube":return SteelCatalog.Profiles.Where(p=>p.Kind=="HSS rectangle"&&Math.Abs(p.H-p.B)<=1e-9);
  case "Round bar":return Bars().Where(p=>p.Kind=="Round bar");
  case "Square bar":return Bars().Where(p=>p.Kind=="Square bar");
  default:return Enumerable.Empty<SteelProfile>();}}
 internal static SteelProfile Find(string name){return Families.SelectMany(For).FirstOrDefault(p=>string.Equals(p.Name,name,StringComparison.OrdinalIgnoreCase));}
 internal static int PiecesPerRun(double runM,double spacingMm,int count){if(count>0)return count;if(spacingMm>0){SteelWeight.Positive(runM,spacingMm);return checked((int)Math.Ceiling(runM*1000/spacingMm-1e-9)+1);}return 1;}
 internal static int Segments(double pieceM){return Math.Max(1,(int)Math.Ceiling(pieceM/SteelCutPlanner.StockLength-1e-9));}
}
class RailingTakeoff {public string Id,Description,Member,Profile;public double PieceLength,KgM,PaintPerMetre;public int Pieces,Segments;public SteelCutDemand Demand;
 public double Installed{get{return PieceLength*Pieces;}}public double InstalledKg{get{return Installed*KgM;}}public double Paint{get{return Installed*PaintPerMetre;}}}
partial class ProgramOfWorksForm {
 ExtraWorkGrid railingGrid;readonly List<RailingTakeoff> railings=new List<RailingTakeoff>();List<SteelStock> railingStocks=new List<SteelStock>();
 void AddRailingWorks(){
  railingGrid=NewExtraGrid("Railing","Railing Estimate","Finishing Works",new[]{"Railing ID","Description / location","Member","Section","Size","Run length m","Piece length m (blank = run length)","Spacing mm","Count / run","Railing qty","Pieces","Installed length m","kg/m","Installed kg","Purchase kg (6 m stock)","Paint m2 (installed)","Status"},10,new Dictionary<int,string>{{2,"Top rail"},{3,"Pipe"}},
   "One row per railing member. Rails: Count / run = number of rails (blank = 1), piece length = run length. Posts / balusters: Piece length = height; enter Spacing mm OR Count / run (pieces = run / spacing + 1). Railing qty multiplies the whole row. All cuts of the same size share 6 m stocks and reuse offcuts; painting uses the installed length.");
  var g=railingGrid.Grid;ExtraChoice(g,2,RailingSections.Members);ExtraChoice(g,3,RailingSections.Families);ExtraChoice(g,4,RailingSections.Families.SelectMany(RailingSections.For).Select(p=>p.Name).ToArray());
  g.Columns[1].Width=220;g.Columns[4].Width=200;g.Columns[6].Width=150;g.DataError+=(s,e)=>{if(e.ColumnIndex==4)e.ThrowException=false;};
  g.Columns[7].ToolTipText="Posts / balusters: centre-to-centre spacing in mm. Leave blank when Count / run is entered.";g.Columns[8].ToolTipText="Number of pieces per run (rails: number of rails). Use this OR Spacing.";}
 void PrepareRailingSizes(DataGridViewRow row){var cell=row.Cells[4] as DataGridViewComboBoxCell;if(cell==null)return;var names=RailingSections.For(Cell(row,3)).Select(p=>p.Name).ToArray();if(names.Length==0)return;cell.Items.Clear();cell.Items.AddRange(names);if(Cell(row,4)!=""&&!names.Contains(Cell(row,4)))cell.Value=null;}
 void RecalculateRailings(){railings.Clear();railingStocks=new List<SteelStock>();if(railingGrid==null)return;var g=railingGrid.Grid;var ids=new HashSet<string>(StringComparer.OrdinalIgnoreCase);var demands=new List<SteelCutDemand>();
  foreach(DataGridViewRow row in g.Rows){if(row.IsNewRow)continue;row.Tag=null;for(int c=railingGrid.Inputs;c<g.Columns.Count;c++)row.Cells[c].Value="";PrepareRailingSizes(row);if(Empty(row))continue;
   try{if(Cell(row,0)==""||!ids.Add(Cell(row,0)))throw new Exception("Enter a unique railing ID.");var profile=RailingSections.Find(Cell(row,4));if(profile==null)throw new Exception("Select the section and size.");
    double run=Number(row,5);double piece=Cell(row,6)==""?run:Number(row,6);double spacing=OptionalNumber(row,7);int count=Cell(row,8)==""?0:Count(row,8);if(spacing>0&&count>0)throw new Exception("Enter Spacing OR Count / run, not both.");int qty=Count(row,9);
    int perRun=RailingSections.PiecesPerRun(run,spacing,count);int pieces=checked(perRun*qty);int segments=RailingSections.Segments(piece);
    var t=new RailingTakeoff{Id=Cell(row,0),Description=Cell(row,1),Member=Cell(row,2),Profile=profile.Name,PieceLength=piece,KgM=profile.KgM,PaintPerMetre=SteelPainting.AreaPerMetre(profile),Pieces=pieces,Segments=segments};
    var demand=new SteelCutDemand{Member="Railing "+t.Id,Specification=profile.Name,Length=piece/segments,Quantity=checked(pieces*segments),KgM=profile.KgM};t.Demand=demand;demands.Add(demand);railings.Add(t);row.Tag=t;
    row.Cells[10].Value=pieces;Measure(row,11,t.Installed);row.Cells[12].Value=profile.KgM.ToString("0.###",CultureInfo.InvariantCulture);Measure(row,13,t.InstalledKg);Measure(row,15,t.Paint);
    row.Cells[16].Value=segments>1?"Estimated - each "+piece.ToString("0.###",CultureInfo.InvariantCulture)+" m piece spliced into "+segments+" cuts of "+(piece/segments).ToString("0.###",CultureInfo.InvariantCulture)+" m":"Estimated";}
   catch(Exception ex){row.Cells[g.Columns.Count-1].Value=ex.Message;errors.Add("Railing "+Cell(row,0)+": "+ex.Message);}}
  if(demands.Count==0)return;try{railingStocks=SteelCutPlanner.Allocate(demands);foreach(DataGridViewRow row in g.Rows){var t=row.Tag as RailingTakeoff;if(t!=null)Measure(row,14,SteelCutPlanner.PurchaseKg(railingStocks,t.Demand));}}catch(Exception ex){errors.Add("Railing cutting: "+ex.Message);}}
 internal List<BillQuantity> RailingQuantities(){var list=new List<BillQuantity>();if(railings.Count==0)return list;
  var buy=new BillQuantity{Section="Railing",Description="Railing steel PURCHASE weight (6 m stock, offcuts reused)",Unit="kg",Quantity=railingStocks.Sum(s=>s.Length*s.KgM)};
  foreach(var group in railingStocks.GroupBy(s=>s.Specification))buy.Sources.Add(new BillQuantity{Description=group.Key+": "+group.Count()+" stock(s) of 6 m; installed "+group.Sum(s=>s.Net).ToString("0.###",CultureInfo.InvariantCulture)+" m; final reusable offcut "+group.Sum(s=>s.Remaining).ToString("0.###",CultureInfo.InvariantCulture)+" m",Quantity=group.Sum(s=>s.Length*s.KgM),Unit="kg"});
  var installed=new BillQuantity{Section="Railing",Description="Railing steel installed weight (reference)",Unit="kg",Quantity=railings.Sum(r=>r.InstalledKg)};
  var paint=new BillQuantity{Section="Railing",Description="Railing metal painting - installed",Unit="m2",Quantity=railings.Sum(r=>r.Paint)};
  foreach(var r in railings){paint.Sources.Add(new BillQuantity{Description=r.Id+" | "+r.Member+" | "+r.Profile+": "+r.Installed.ToString("0.###",CultureInfo.InvariantCulture)+" m x "+r.PaintPerMetre.ToString("0.####",CultureInfo.InvariantCulture)+" m2/m",Quantity=r.Paint,Unit="m2"});installed.Sources.Add(new BillQuantity{Description=r.Id+" | "+r.Member+" | "+r.Profile+": "+r.Pieces+" pcs x "+r.PieceLength.ToString("0.###",CultureInfo.InvariantCulture)+" m",Quantity=r.InstalledKg,Unit="kg"});}
  list.Add(buy);list.Add(installed);list.Add(paint);return list;}
}
}
