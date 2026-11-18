using System;using System.Collections.Generic;using System.Globalization;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Railing estimate in Finishing Works (owner, 17 November 2026).
// Each row is one railing member: top / mid / bottom rail, post, baluster or other. Sections come from the Regan 2026
// handbook tables (RailingCatalog); round and square bars are not in the handbook and are computed from 7850 kg/m3. Pieces per run = count, or ceil(run / spacing) + 1, or 1 for
// a rail. Cuts longer than 6 m are split into equal pieces (approved splice assumed). All cuts of the same size share
// 6 m stocks with best-fit offcut reuse (SteelCutPlanner). Painting is the installed length x painted perimeter.
static class RailingSections {
 internal static readonly string[] Families={"Pipe (heavy gauge)","Pipe (light gauge)","Pipe (Sch 40)","Pipe (Sch 80)","Pipe (Sch 160)","Strux tube","Rectangular tube","Square tube","Flat bar","Round bar","Square bar"};
 internal static readonly string[] Members={"Top rail","Mid rail","Bottom rail","Post","Baluster","Other"};
 internal static IEnumerable<SteelProfile> For(string family){var all=RailingCatalog.All();switch(family){
  case "Pipe":case "Pipe (heavy gauge)":return all.Where(p=>p.Name.Contains("heavy gauge"));
  case "Pipe (light gauge)":return all.Where(p=>p.Name.Contains("light gauge"));
  case "Pipe (Sch 40)":return all.Where(p=>p.Name.Contains("seamless Sch 40 "));
  case "Pipe (Sch 80)":return all.Where(p=>p.Name.Contains("seamless Sch 80 "));
  case "Pipe (Sch 160)":return all.Where(p=>p.Name.Contains("seamless Sch 160 "));
  case "Strux tube":return all.Where(p=>p.Name.StartsWith("Strux",StringComparison.Ordinal));
  case "Rectangular tube":return all.Where(p=>p.Name.StartsWith("RHS",StringComparison.Ordinal));
  case "Square tube":return all.Where(p=>p.Name.StartsWith("SHS",StringComparison.Ordinal));
  case "Flat bar":return all.Where(p=>p.Kind=="Flat bar");
  case "Round bar":return all.Where(p=>p.Kind=="Round bar");
  case "Square bar":return all.Where(p=>p.Kind=="Square bar");
  default:return Enumerable.Empty<SteelProfile>();}}
 /* Railing catalogue first; a size saved by v1.4.96 (e.g. "Pipe OD33.4x3.4mm") is still found in the steel catalogue. */
 internal static SteelProfile Find(string name){return RailingCatalog.All().Concat(SteelCatalog.Profiles).FirstOrDefault(p=>string.Equals(p.Name,name,StringComparison.OrdinalIgnoreCase));}
 internal static int PiecesPerRun(double runM,double spacingMm,int count){if(count>0)return count;if(spacingMm>0){SteelWeight.Positive(runM,spacingMm);return checked((int)Math.Ceiling(runM*1000/spacingMm-1e-9)+1);}return 1;}
 internal static int Segments(double pieceM){return Math.Max(1,(int)Math.Ceiling(pieceM/SteelCutPlanner.StockLength-1e-9));}
}
class RailingTakeoff {public string Id,Description,Member,Profile;public double PieceLength,KgM,PaintPerMetre;public int Pieces,Segments;public bool Painted=true;public SteelCutDemand Demand;
 public double Installed{get{return PieceLength*Pieces;}}public double InstalledKg{get{return Installed*KgM;}}public double Paint{get{return Painted?Installed*PaintPerMetre:0;}}}
partial class ProgramOfWorksForm {
 ExtraWorkGrid railingGrid;readonly List<RailingTakeoff> railings=new List<RailingTakeoff>();List<SteelStock> railingStocks=new List<SteelStock>();
 void AddRailingWorks(){
  railingGrid=NewExtraGrid("Railing","Railing Estimate","Finishing Works",new[]{"Railing ID","Description / location","Member","Section","Size","Run length m","Piece length m (blank = run length)","Spacing mm","Count / run","Railing qty","With painting?","Pieces","Installed length m","kg/m","Installed kg","Purchase kg (6 m stock)","Paint m2 (installed)","Status"},11,new Dictionary<int,string>{{2,"Top rail"},{3,"Pipe (heavy gauge)"},{10,"True"}},
   "One row per railing member. Rails: Count / run = number of rails (blank = 1), piece length = run length. Posts / balusters: Piece length = height; enter Spacing mm OR Count / run (pieces = run / spacing + 1). Railing qty multiplies the whole row. All cuts of the same size share 6 m stocks and reuse offcuts; painting uses the installed length.");
  var g=railingGrid.Grid;ExtraChoice(g,2,RailingSections.Members);ExtraChoice(g,3,RailingSections.Families);ExtraChoice(g,4,RailingSections.Families.SelectMany(RailingSections.For).Select(p=>p.Name).Concat(SteelCatalog.Profiles.Where(p=>p.Kind=="HSS circle / pipe").Select(p=>p.Name)).Distinct().ToArray());
  g.Columns[1].Width=220;g.Columns[4].Width=290;g.Columns[3].Width=150;g.Columns[6].Width=150;g.DataError+=(s,e)=>{if(e.ColumnIndex==4)e.ThrowException=false;};
  /* Owner (18 November 2026): tick when the member is painted; unticked members are left out of railing painting. */ExtraCheck(g,10);g.Columns[10].ToolTipText="Checked: this member is painted (included in Railing metal painting). Unchecked: no painting.";
  g.Columns[7].ToolTipText="Posts / balusters: centre-to-centre spacing in mm. Leave blank when Count / run is entered.";g.Columns[8].ToolTipText="Number of pieces per run (rails: number of rails). Use this OR Spacing.";}
 void PrepareRailingSizes(DataGridViewRow row){var cell=row.Cells[4] as DataGridViewComboBoxCell;if(cell==null)return;var names=RailingSections.For(Cell(row,3)).Select(p=>p.Name).ToArray();if(names.Length==0)return;cell.Items.Clear();cell.Items.AddRange(names);string current=Cell(row,4);if(current!=""&&!names.Contains(current)){var saved=RailingSections.Find(current);if(saved!=null)cell.Items.Add(saved.Name);else cell.Value=null;}}
 void RecalculateRailings(){railings.Clear();railingStocks=new List<SteelStock>();if(railingGrid==null)return;var g=railingGrid.Grid;var ids=new HashSet<string>(StringComparer.OrdinalIgnoreCase);var demands=new List<SteelCutDemand>();
  foreach(DataGridViewRow row in g.Rows){if(row.IsNewRow)continue;row.Tag=null;for(int c=railingGrid.Inputs;c<g.Columns.Count;c++)row.Cells[c].Value="";PrepareRailingSizes(row);if(Empty(row))continue;
   try{if(Cell(row,0)==""||!ids.Add(Cell(row,0)))throw new Exception("Enter a unique railing ID.");var profile=RailingSections.Find(Cell(row,4));if(profile==null)throw new Exception("Select the section and size.");
    double run=Number(row,5);double piece=Cell(row,6)==""?run:Number(row,6);double spacing=OptionalNumber(row,7);int count=Cell(row,8)==""?0:Count(row,8);if(spacing>0&&count>0)throw new Exception("Enter Spacing OR Count / run, not both.");int qty=Count(row,9);
    int perRun=RailingSections.PiecesPerRun(run,spacing,count);int pieces=checked(perRun*qty);int segments=RailingSections.Segments(piece);
    var t=new RailingTakeoff{Id=Cell(row,0),Description=Cell(row,1),Member=Cell(row,2),Profile=profile.Name,PieceLength=piece,KgM=profile.KgM,PaintPerMetre=SteelPainting.AreaPerMetre(profile),Pieces=pieces,Segments=segments,Painted=Cell(row,10)!=""&&bool.Parse(Cell(row,10))};
    var demand=new SteelCutDemand{Member="Railing "+t.Id,Specification=profile.Name,Length=piece/segments,Quantity=checked(pieces*segments),KgM=profile.KgM};t.Demand=demand;demands.Add(demand);railings.Add(t);row.Tag=t;
    row.Cells[11].Value=pieces;Measure(row,12,t.Installed);row.Cells[13].Value=profile.KgM.ToString("0.###",CultureInfo.InvariantCulture);Measure(row,14,t.InstalledKg);Measure(row,16,t.Paint);
    row.Cells[17].Value=segments>1?"Estimated - each "+piece.ToString("0.###",CultureInfo.InvariantCulture)+" m piece spliced into "+segments+" cuts of "+(piece/segments).ToString("0.###",CultureInfo.InvariantCulture)+" m":"Estimated";}
   catch(Exception ex){row.Cells[g.Columns.Count-1].Value=ex.Message;errors.Add("Railing "+Cell(row,0)+": "+ex.Message);}}
  if(demands.Count==0)return;try{railingStocks=SteelCutPlanner.Allocate(demands);foreach(DataGridViewRow row in g.Rows){var t=row.Tag as RailingTakeoff;if(t!=null)Measure(row,15,SteelCutPlanner.PurchaseKg(railingStocks,t.Demand));}}catch(Exception ex){errors.Add("Railing cutting: "+ex.Message);}}
 internal List<BillQuantity> RailingQuantities(){var list=new List<BillQuantity>();if(railings.Count==0)return list;
  /* Owner (18 November 2026): no railing weights in Bill of Quantities; one BOQ line to map ("isang map lang") with the
     6 m pieces to buy broken down per material (profile / size), not per member type. Members of the same material share
     stock lengths and offcuts; each material line names the railing rows it covers. */
  var pieces=new BillQuantity{Section="Railing",Description="Railing pieces to buy (6 m stock lengths)",Unit="pcs",Quantity=railingStocks.Count};
  foreach(var group in railingStocks.GroupBy(x=>x.Specification).OrderBy(x=>x.Key)){var rows=railings.Where(r=>r.Profile==group.Key).ToList();
   pieces.Sources.Add(new BillQuantity{Description=group.Key+": "+group.Count()+" pcs x 6 m (for "+string.Join("; ",rows.Select(r=>r.Id+" "+r.Member+" "+r.Pieces+" pcs x "+r.PieceLength.ToString("0.###",CultureInfo.InvariantCulture)+" m"))+")",Quantity=group.Count(),Unit="pcs"});}
  list.Add(pieces);
  var paint=new BillQuantity{Section="Railing",Description="Railing metal painting - installed",Unit="m2",Quantity=railings.Sum(r=>r.Paint)};
  foreach(var r in railings)if(r.Painted)paint.Sources.Add(new BillQuantity{Description=r.Id+" | "+r.Member+" | "+r.Profile+": "+r.Installed.ToString("0.###",CultureInfo.InvariantCulture)+" m x "+r.PaintPerMetre.ToString("0.####",CultureInfo.InvariantCulture)+" m2/m",Quantity=r.Paint,Unit="m2"});
  list.Add(paint);return list;}
}
}
