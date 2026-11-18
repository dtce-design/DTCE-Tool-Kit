using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Globalization;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;

namespace BeamScheduleFormatter {
class EstimateOptions {
 public LengthConfiguration LengthConfig;public bool FixedCurtailment=true;
 public double Left=.25,Right=.25,Cover=40,Hook=6,Extra=0,Waste=0,Offset=50;
 public double Fc=21;public int GradeSmall=40,Grade=60,MainHook=0,Anchorage=0;public double BondRatio=1;
 public int GradeFor(int dia){if(dia<=12)return GradeSmall;if(dia>=16)return Grade;throw new Exception("13-15mm bars need a project-specific grade assignment.");}
 public void Validate(){if(Left<=0||Right<=0||Left+Right>=1)throw new Exception("Left + right zone percentages must be below 100%.");if(Cover<0||Hook<6||Extra<0||Waste<0||Offset<0)throw new Exception("Allowances cannot be negative; seismic tail factor must be at least 6.");if(LengthConfig==null?Fc!=21&&Fc!=28&&Fc!=35:Fc<17||Fc>70)throw new Exception("Select concrete strength 21, 28 or 35 MPa.");if((Grade!=40&&Grade!=60)||(GradeSmall!=40&&GradeSmall!=60))throw new Exception("Select Grade 40 or 60 steel.");if(BondRatio<=0||BondRatio>2.5)throw new Exception("Bond ratio must be >0 and <=2.5.");if((Anchorage==1||Anchorage==2)&&MainHook!=0)throw new Exception("Straight Ld/lap allowances cannot be combined with hooked ends.");if(Anchorage==3&&(MainHook==0||LengthConfig==null))throw new Exception("Hooked Ldh allowance requires a saved calculation configuration and 90/180deg main hooks.");}
 public string Description(){return string.Format(CultureInfo.InvariantCulture,"NSCP 2015 / ACI 318-14 basis; 16mm+ Grade {8}; main beam ends: {9}. Zones L/M/R: {0:0.##}% / {1:0.##}% / {2:0.##}%; cover {3:0.##}mm; seismic tie tails max({4:0.##}d,75mm); extra {5:0.###}m per actual main-bar piece; waste {6:0.##}%; first/last tie offset {7:0.##}mm.",Left*100,(1-Left-Right)*100,Right*100,Cover,Hook,Extra,Waste,Offset,Grade,MainHook==0?"straight":MainHook+"deg hooks at both ends")+" 12mm and below Grade "+GradeSmall+". Concrete f'c="+Fc+"MPa; (cb+Ktr)/db="+BondRatio.ToString("0.##",CultureInfo.InvariantCulture)+"; main-piece allowance="+(Anchorage==0?"none":Anchorage==1?"2 x straight Ld":"1 x Class B lap (1.3Ld)")+".";}
}
class TiePartsValue {public double MainHoop,AdditionalLeg;public int AdditionalCount;public double Total{get{return MainHoop+AdditionalCount*AdditionalLeg;}}}
static class HookGeometry {
 public static double Development(int dia,bool top,EstimateOptions o){if(o.LengthConfig!=null)return o.LengthConfig.Rounded(o.LengthConfig.Values(dia,top).Ld)/1000;double fy=o.GradeFor(dia)==40?280:420;return Math.Max(300,fy/(1.1*Math.Sqrt(o.Fc))*(top?1.3:1)*(dia<=19?.8:1)/o.BondRatio*dia)/1000;}
 public static double Inside(int dia,bool tie){if(dia<10||dia>36)throw new Exception("Standard geometry supports 10-36mm bars only.");if(tie){if(dia>25)throw new Exception("Tie diameters above 25mm require project bend detail.");return (dia<=16?4:6)*dia;}return (dia<=25?6:8)*dia;}
 public static double MainEnd(int dia,int angle){if(angle==0)return 0;if(angle!=90&&angle!=180)throw new Exception("Use straight, 90 or 180 degree main hooks.");double radius=(Inside(dia,false)+dia)/2;double tail=angle==90?12*dia:Math.Max(4*dia,65);return (radius*angle*Math.PI/180+tail)/1000;}
 public static TiePartsValue TieParts(double width,double depth,int dia,int legs,EstimateOptions o){var v=o.LengthConfig==null?null:o.LengthConfig.Values(dia,false);double r=((v==null?Inside(dia,true):v.TieInside)+dia)/2000;if(v!=null&&v.TieInside<=0)throw new Exception("Configuration needs project-specific tie bend diameter.");double w=(width-2*o.Cover-dia)/1000,h=(depth-2*o.Cover-dia)/1000;if(w<2*r||h<2*r)throw new Exception("Beam/tie dimensions cannot fit the minimum bend diameter.");double tail=(v==null?Math.Max(Math.Max(6,o.Hook)*dia,75):v.Tail135)/1000;double outer=2*(w+h)-8*r+3*Math.PI*r+2*tail;double extraTail90=dia<=16?Math.Max(6*dia,75)/1000.0:12*dia/1000.0;double cross=h-2*r+(90+130)*Math.PI/180*r+extraTail90+tail;return new TiePartsValue{MainHoop=outer,AdditionalLeg=cross,AdditionalCount=legs-2};}
 public static double TieCut(double width,double depth,int dia,int legs,EstimateOptions o){return TieParts(width,depth,dia,legs,o).Total;}
}
class EstimateLine {
 public string Beam,Part,Formula;public int Diameter,Grade,PiecesPerBeam;public double CutLength,MainHoopCut,AdditionalLegCut;public int TieSetsPerBeam,AdditionalLegs;public bool TieOverride;public double Metres,Kg;
}
class BeamEstimate {
 public List<EstimateLine> Lines=new List<EstimateLine>();public List<string> Notes=new List<string>();public int Ties;
 public double MainKg{get{return Lines.Where(x=>x.Part!="Stirrups").Sum(x=>x.Kg);}}
 public double LongKg{get{return Lines.Where(x=>x.Part!="Stirrups"&&x.Part!="Side-face").Sum(x=>x.Kg);}}
 public double SfrKg{get{return Lines.Where(x=>x.Part=="Side-face").Sum(x=>x.Kg);}}
 public double TieKg{get{return Lines.Where(x=>x.Part=="Stirrups").Sum(x=>x.Kg);}}
 public double Total{get{return MainKg+TieKg;}}
}
static class EstimatorEngine {
 public static readonly CultureInfo Inv=CultureInfo.InvariantCulture;
 public static string DisplayPart(string part){if(part.EndsWith(" CONTINUOUS"))return part.Replace(" CONTINUOUS"," STRAIGHT / CONTINUOUS");if(part=="Bottom CUT MIDSPAN")return "Bottom CURTAILED MIDSPAN";if(part=="Bottom CUT LEFT"||part=="Bottom CUT RIGHT")return part.Replace("CUT","END EXTRA");if(part.StartsWith("Top CUT "))return part.Replace("Top CUT ","Top EXTRA ");return part;}
 public static double UnitMass(int diameter){switch(diameter){case 10:return .617;case 12:return .888;case 16:return 1.578;case 20:return 2.466;case 25:return 3.853;case 28:return 4.834;case 32:return 6.313;case 36:return 7.990;case 40:return 9.865;case 50:return 15.41;default:throw new Exception(RebarDiameters.Message(diameter.ToString(System.Globalization.CultureInfo.InvariantCulture)));}}
 public static Dictionary<int,int> Bars(IEnumerable<string> raw,bool eachFace=false){
  var result=new Dictionary<int,int>();foreach(string text in raw){string s=Engine.Normal(text).Replace("#","O").Replace("Ø","O");if(s=="-"||s==""||s=="+")continue;
   var m=Regex.Match(s,@"^(\d+)-O(\d+)(EF)?$");if(!m.Success)throw new Exception("Unsupported bar notation: "+text);
   int dia=int.Parse(m.Groups[2].Value),n=int.Parse(m.Groups[1].Value)*(m.Groups[3].Success?2:1);if(dia<=0||n<=0)throw new Exception("Invalid bar size/count: "+text);
   if(!result.ContainsKey(dia))result[dia]=0;result[dia]+=n;
  }return result;
 }
 class Tie {public int N,Leg,Dia;public double Spacing;}
 static Tie ParseTie(string raw){if(raw=="-"||raw=="--"||string.IsNullOrWhiteSpace(raw))return null;
  var m=Regex.Match(Engine.Normal(raw).Replace("#","O").Replace("Ø","O"),@"^(\d+)-(\d+)L-O(\d+)@(\d+)C/C$");
  if(!m.Success)throw new Exception("Unsupported stirrup: "+raw);
  var t=new Tie{N=int.Parse(m.Groups[1].Value),Leg=int.Parse(m.Groups[2].Value),Dia=int.Parse(m.Groups[3].Value),Spacing=double.Parse(m.Groups[4].Value,Inv)/1000};
  if(t.Leg<2||t.Leg>20||t.Dia<=0||t.Spacing<=0)throw new Exception("Invalid stirrup geometry: "+raw);return t;
 }
 static string F(double n){return n.ToString("0.00",Inv);}
 static void Add(BeamEstimate r,string beam,string part,int dia,double metres,string formula,EstimateOptions o){
  if(double.IsNaN(metres)||double.IsInfinity(metres)||metres<0)throw new Exception("Invalid calculated length.");
  o.GradeFor(dia);r.Lines.Add(new EstimateLine{Beam=beam,Part=part,Diameter=dia,Grade=o.GradeFor(dia),Metres=metres,Kg=metres*UnitMass(dia),Formula=formula});
 }
 static void AddMainPiece(BeamEstimate result,string beam,string face,int dia,int pieces,int start,int end,double length,double[] zones,EstimateOptions o){
  if(pieces<=0)return;bool isTop=face=="Top";var p=o.LengthConfig;var v=p==null?null:p.Values(dia,isTop);double body=Enumerable.Range(start,end-start+1).Sum(z=>zones[z]);if(o.FixedCurtailment&&!(start==0&&end==2)){double[] begins=isTop?new[]{0.0,.25,.75}:new[]{0.0,1.0/6,.75};double[] ends=isTop?new[]{.25,.75,1.0}:new[]{.25,5.0/6,1.0};body=(ends[end]-begins[start])*length;}int exterior=(start==0?1:0)+(end==2?1:0);int internalEnds=2-exterior;double perHook=o.MainHook==0?0:p==null?HookGeometry.MainEnd(dia,o.MainHook):p.Rounded(o.MainHook==90?v.Cut90:v.Cut180)/1000;double hookLength=exterior*perHook;double ld=HookGeometry.Development(dia,isTop,o),allowance=0;
  if(o.Anchorage==1)allowance=2*ld;else if(o.Anchorage==2)allowance=p==null?1.3*ld:p.Rounded(v.Lap)/1000;else if(o.Anchorage==3)allowance=exterior*Math.Max(0,p.Rounded(v.Ldh)-(v.MainInside/2+dia))/1000+internalEnds*ld;
  double cut=body+o.Extra+hookLength+allowance;string[] names={"LEFT","MIDSPAN","RIGHT"};string kind=start==0&&end==2?"CONTINUOUS":"CUT "+string.Join("+",names.Skip(start).Take(end-start+1));Add(result,beam,face+" "+kind,dia,pieces*cut,pieces+" pcs x ("+F(body)+"m body + "+F(o.Extra)+"m extra + "+F(hookLength)+"m external hooks + "+F(allowance)+"m anchorage/lap) = "+F(pieces*cut)+"m",o);var line=result.Lines[result.Lines.Count-1];line.PiecesPerBeam=pieces;line.CutLength=cut;
 }
 static void AddMainBars(BeamEstimate result,BeamRow row,string beam,double length,double[] zones,EstimateOptions o){for(int face=0;face<2;face++){var q=Enumerable.Range(0,3).Select(z=>Bars(row.Cells[2+face*3+z])).ToArray();foreach(int dia in q.SelectMany(x=>x.Keys).Distinct().OrderBy(d=>d)){int[] counts=q.Select(x=>x.ContainsKey(dia)?x[dia]:0).ToArray();int previous=0;foreach(int level in counts.Where(n=>n>0).Distinct().OrderBy(n=>n)){int amount=level-previous;int z=0;while(z<3){if(counts[z]<level){z++;continue;}int start=z;while(z+1<3&&counts[z+1]>=level)z++;AddMainPiece(result,beam,face==0?"Bottom":"Top",dia,amount,start,z,length,zones,o);z++;}previous=level;}}}result.Notes.Add("Continuous bars = minimum L/M/R count for the SAME diameter. Remaining counts are cut bars; adjacent occupied zones share a piece. No continuity is inferred between different diameters.");result.Notes.Add("Fixed cut bodies: top support L/4; bottom support L/4; bottom MIDSPAN cutoffs L/6 from each end (2L/3 body). Hooks apply only at beam ends; internal cut ends use straight Ld in hooked-anchorage mode. Confirm actual cutoffs in the structural details.");}
 public static BeamEstimate Calculate(BeamRow row,string beam,double length,EstimateOptions o,int? countOverride=null,double? cutOverride=null){
  o.Validate();if(double.IsNaN(length)||double.IsInfinity(length)||length<=0||length>1000)throw new Exception("Enter beam length greater than 0 and at most 1000 m.");
  double width,depth;if(!double.TryParse(row.Cell(0),NumberStyles.Float,Inv,out width)||!double.TryParse(row.Cell(1),NumberStyles.Float,Inv,out depth)||width<=0||depth<=0)throw new Exception("Beam size must be numeric millimetres.");
  if(countOverride.HasValue&&(countOverride<=0||countOverride>100000))throw new Exception("Tie count override must be 1 to 100000.");
  if(cutOverride.HasValue&&(cutOverride<=0||double.IsNaN(cutOverride.Value)||double.IsInfinity(cutOverride.Value)))throw new Exception("Tie cut length must be greater than zero.");
  var result=new BeamEstimate();double[] zones={length*o.Left,length*(1-o.Left-o.Right),length*o.Right};string[] names={"LEFT","MIDSPAN","RIGHT"};
  AddMainBars(result,row,beam,length,zones,o);
  foreach(var bar in Bars(row.Cells[11],true)){double hooks=0,allowance=0;if(o.LengthConfig!=null){var p=o.LengthConfig;var v=p.Values(bar.Key,false);hooks=o.MainHook==0?0:2*p.Rounded(o.MainHook==90?v.Cut90:v.Cut180)/1000;allowance=o.Anchorage==1?2*p.Rounded(v.Ld)/1000:o.Anchorage==2?p.Rounded(v.Lap)/1000:o.Anchorage==3?2*Math.Max(0,p.Rounded(v.Ldh)-(v.MainInside/2+bar.Key))/1000:0;}Add(result,beam,"Side-face",bar.Key,bar.Value*(length+o.Extra+hooks+allowance),bar.Value+" bars (EF doubled) x ("+F(length)+"m span + "+F(o.Extra)+"m extra + "+F(hooks)+"m hooks + "+F(allowance)+"m Ld/lap)m",o);var sfr=result.Lines[result.Lines.Count-1];sfr.PiecesPerBeam=bar.Value;sfr.CutLength=length+o.Extra+hooks+allowance;}
  foreach(int col in new[]{12})if(row.Cells[col].Any(x=>Engine.Normal(x)!="-"&&Engine.Normal(x)!=""))throw new Exception("Unrecognised additional reinforcement: "+row.Cell(col)+". Review separately.");
  if(!string.IsNullOrEmpty(row.RevisedSpacing)){RevisedSchedule.AddTies(result,row,beam,length,o,countOverride,cutOverride);return result;}
  var ties=Enumerable.Range(8,3).Select(c=>ParseTie(row.Cell(c))).ToArray();if(ties.All(t=>t==null))throw new Exception("No stirrup spacing found.");
  if(ties[0]==null)ties[0]=ties[1]??ties[2];if(ties[2]==null)ties[2]=ties[1]??ties[0];
  if(ties[1]==null){ties[1]=ties[0].Spacing<=ties[2].Spacing?ties[0]:ties[2];result.Notes.Add("MIDSPAN blank: smaller end spacing/shape used for estimate.");}
  if(row.Cantilever){ties[1]=ties[2]=ties[0];result.Notes.Add("Cantilever assumption: LEFT stirrup spacing/shape applied throughout. Main-bar zones still use chosen percentages.");}
  double offset=o.Offset/1000;if(length<=2*offset)throw new Exception("Beam length is too short for first/last tie offsets.");
  double[] bounds={offset,length*o.Left,length*(1-o.Right),length-offset};
  if(bounds[1]<bounds[0]||bounds[2]>bounds[3])throw new Exception("End zones shorter than tie offset; adjust percentages/offset.");
  int[] counts=new int[3];for(int c=0;c<3;c++)counts[c]=(int)Math.Ceiling(Math.Max(0,bounds[c+1]-bounds[c])/ties[c].Spacing-1e-10);counts[2]++;
  if(countOverride.HasValue){if(ties.Select(t=>t.Leg+"/"+t.Dia).Distinct().Count()!=1)throw new Exception("Tie count override requires identical leg/diameter in all zones.");counts=new[]{0,countOverride.Value,0};result.Notes.Add("Manual stirrup count override: "+countOverride.Value+" sets.");}
  for(int c=0;c<3;c++)if(counts[c]>0){var t=ties[c];double w=(width-2*o.Cover-t.Dia)/1000,h=(depth-2*o.Cover-t.Dia)/1000;
   if(w<=0||h<=0)throw new Exception("Cover is too large for the beam/tie diameter.");
   double cut=cutOverride??HookGeometry.TieCut(width,depth,t.Dia,t.Leg,o);
   double metres=counts[c]*cut;Add(result,beam,"Stirrups",t.Dia,metres,names[c]+": "+counts[c]+" sets x "+F(cut)+"m/set = "+F(metres)+"m; "+t.Leg+"L"+(cutOverride.HasValue?" (manual cut)":"; w="+F(w)+"m,h="+F(h)+"m,r="+F((HookGeometry.Inside(t.Dia,true)+t.Dia)/2000)+"m,tail="+F(Math.Max(Math.Max(6,o.Hook)*t.Dia,75)/1000)+"m"),o);var detail=result.Lines[result.Lines.Count-1];var shape=cutOverride.HasValue?null:HookGeometry.TieParts(width,depth,t.Dia,t.Leg,o);detail.TieSetsPerBeam=counts[c];detail.CutLength=cut;detail.TieOverride=cutOverride.HasValue;detail.MainHoopCut=shape==null?0:shape.MainHoop;detail.AdditionalLegCut=shape==null?0:shape.AdditionalLeg;detail.AdditionalLegs=t.Leg-2;result.Ties+=counts[c];
  }
  result.Notes.Add("Main bars are decomposed per diameter into continuous and contiguous cut pieces. SFR EF is counted on both faces.");
  result.Notes.Add("Stirrups recounted from entered length and source spacings; source piece counts are reference only. Shared zone boundaries counted once; end offsets included.");
  result.Notes.Add(cutOverride.HasValue?"Manual total steel length per stirrup set overrides standard shape calculation.":"Tie model: one outer hoop (three90deg corners, two135deg hooks) + (legs-2) depth crossties (90deg + custom130deg). Centerline arcs/tangent deductions included. Crosstie90 tail=max(6db,75mm) up to16mm,12db for20-25mm; custom130 tail=max(6db,75mm). Verify this shape matches project.");
  result.Notes.Add("Hook geometry is the same for Grade 40/60; development and lap lengths are NOT hook tail lengths and must come from the project detail. Main hooks apply at exterior beam ends only.");
  result.Notes.Add("Straight Ld reference: ACI 318M-14 Eq 25.4.2.3a, normal-weight uncoated bars, lambda=1, psi_e=1, psi_s=0.8 up to 19mm else 1; top psi_t=1.3 conservatively, others=1.0; minimum 300mm. User bond ratio must match actual cover/spacing/confinement. No excess-steel reduction.");
  result.Notes.Add("Ld/lap allowance applies per actual main-bar piece. SFR uses full length and configured allowances. Verify splice locations, support embedment and continuity. Hooked development/seismic joint anchorage are not checked.");
  result.Notes.Add("Commercial stock cutting is calculated separately from net reinforcement and percentage allowance. Support widths and chairs are not inferred.");
  return result;
 }
}

class EstimateInput {public string ExcavationDepth="",WorkingAllowance="0.30",Swell="20",Shrink="10",OtherBuriedConcrete="0";public BeamEarthworks Earthworks;public string EarthworkError="";public string Support="Elevated",SlabThickness="0",MeasureError="";public BeamQuantities Measures;public string Description="",ScheduleLabel="";public string Beam;public BeamRow Source;public string Length="",Quantity="1",TieCount="",TieCut="";public BeamEstimate Result;public string Error="";}
}
