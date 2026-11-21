using System;using System.Collections.Generic;using System.Globalization;using System.Linq;
namespace BeamScheduleFormatter {
/* Lintel beams over the openings of CHB walls (owner, 21 November 2026: "Sa calculation ng masonry wall kapag may nailagay ng openings
   agad na magdagdag ng lintel beam based dito" - the Lintel Beam Design Calculation Report, LB-4A to LB-6E). The mark follows the CHB
   thickness and the clear opening (between two ranges the larger mark); fc' 21 MPa, Grade 40, 20 mm cover to 10 mm ties, gravity load
   of at most 2.5 m of CHB wall above. */
sealed class LintelMark {
 public string Mark,Brace="";public int Chb,Width,Depth,BottomCount,BottomDia,TopCount,TopDia=12,MidCount,MidDia=10,TieSpacing,Bearing;public double MaxOpening;public bool SingleLeg;
 public int JambBarsPerSide{get{return Bearing<=200?1:2;}}
 public string Bottom{get{return BottomCount+"-"+BottomDia;}}public string Top{get{return TopCount+"-"+TopDia;}}public string Mid{get{return MidCount>0?MidCount+"-"+MidDia:"-";}}
 public string Ties{get{return (SingleLeg?"single-leg @ ":"closed @ ")+TieSpacing;}}
}
static class LintelSchedule {
 static LintelMark M(string mark,int chb,double max,int b,int h,int nb,int db,int nt,int nm,int s,int bearing,string brace){return new LintelMark{Mark=mark,Chb=chb,MaxOpening=max,Width=b,Depth=h,BottomCount=nb,BottomDia=db,TopCount=nt,MidCount=nm,TieSpacing=s,Bearing=bearing,SingleLeg=chb==4,Brace=brace};}
 public static readonly LintelMark[] Marks={
  M("LB-4A",4,2.00,100,300,1,16,1,0,125,200,""),M("LB-4B",4,3.00,100,400,1,20,1,0,150,300,""),M("LB-4C",4,4.00,100,500,1,20,1,0,200,300,""),
  M("LB-4D",4,5.00,100,700,1,25,1,2,250,400,"Required"),M("LB-4E",4,6.00,100,800,1,25,1,2,250,400,"Required"),
  M("LB-6A",6,2.00,150,300,2,12,2,0,125,200,""),M("LB-6B",6,3.00,150,400,2,16,2,0,150,300,""),M("LB-6C",6,4.00,150,500,2,20,2,0,200,300,""),
  M("LB-6D",6,5.00,150,600,2,20,2,0,250,400,""),M("LB-6E",6,6.00,150,700,2,25,2,2,250,400,"")};
 public const double CoverToTie=20,Tie=10,WallAbove=2.5,WindowHead=2.10;
 /* 4" = 100 mm CHB, 6" = 150 mm (an 8" wall uses the 6" marks: report scope is 4" and 6") */
 public static int Chb(double thicknessMm){return thicknessMm<=125?4:6;}
 public static LintelMark Select(double thicknessMm,double clearOpening){if(double.IsNaN(clearOpening)||clearOpening<=0)throw new Exception("Opening width must be positive.");int chb=Chb(thicknessMm);
  var mark=Marks.Where(m=>m.Chb==chb).OrderBy(m=>m.MaxOpening).FirstOrDefault(m=>clearOpening<=m.MaxOpening+1e-9);
  if(mark==null)throw new Exception("clear opening "+clearOpening.ToString("0.00",CultureInfo.InvariantCulture)+" m is wider than 6.00 m, outside the lintel schedule: design this lintel separately.");return mark;}
 /* 90° hook extension = 12 bar diameters, rounded up to 50 mm (12 -> 150, 16 -> 200, 20 -> 250, 25 -> 300 mm as in the report) */
 public static double HookExtension(int dia){return Math.Ceiling(12.0*dia/50-1e-9)*50;}
 static double Up(double metres){return Math.Ceiling(metres/.025-1e-10)*.025;}
 /* the lintel over one opening group (Quantity identical openings) of one wall */
 public static MiscStructureQuantity Takeoff(OpeningItem opening,WallItem wall,LengthConfiguration cfg,out LintelMark mark){
  mark=Select(wall.ThicknessMm,opening.Width);double clear=opening.Width,bearing=mark.Bearing/1000.0,L=clear+2*bearing,b=mark.Width/1000.0,h=mark.Depth/1000.0;int qty=Math.Max(1,opening.Quantity);
  string id=mark.Mark+" @ "+opening.Id+" / "+wall.Id;var steel=new EstimateInput{Beam="Lintel Beam "+id,Description="Lintel over "+opening.Id+" ("+(opening.Kind??"Opening")+" "+F(opening.Width)+" x "+F(opening.Height)+" m) in wall "+wall.Id,Quantity=qty.ToString(CultureInfo.InvariantCulture),Length=L.ToString("R",CultureInfo.InvariantCulture),Result=new BeamEstimate()};
  double barCover=(CoverToTie+Tie)/1000;
  Action<string,int,int,double,string> add=(part,dia,count,cut,formula)=>{if(count<=0)return;double c=Up(cut),metres=c*count*qty;steel.Result.Lines.Add(new EstimateLine{Beam=steel.Beam,Part=part,Diameter=dia,Grade=dia<=12?cfg.GradeSmall:cfg.GradeLarge,PiecesPerBeam=count,CutLength=c,Metres=metres,Kg=metres*EstimatorEngine.UnitMass(dia),Formula=formula});};
  add("Bottom longitudinal",mark.BottomDia,mark.BottomCount,L-2*barCover+2*HookExtension(mark.BottomDia)/1000,mark.Mark+": L = clear "+F(clear)+" + 2 x bearing "+mark.Bearing+" mm; cut = L - 2 x 30 mm + two 90° hooks turned up, "+HookExtension(mark.BottomDia)+" mm each");
  add("Top longitudinal",mark.TopDia,mark.TopCount,L-2*barCover+2*HookExtension(mark.TopDia)/1000,"cut = L - 2 x 30 mm + two 90° hooks turned down, "+HookExtension(mark.TopDia)+" mm each");
  if(mark.MidCount>0)add("Mid bars",mark.MidDia,mark.MidCount,L-2*barCover,"straight, L - 2 x 30 mm");
  int ties=(int)Math.Ceiling((L*1000-2*50)/mark.TieSpacing-1e-10)+1;var v=cfg.Values((int)Tie,false);double r=((v!=null&&v.TieInside>0?v.TieInside:4*Tie)+Tie)/2000,tail=(v!=null&&v.Tail135>0?v.Tail135:75)/1000,hc=(mark.Depth-2*CoverToTie-Tie)/1000,wc=(mark.Width-2*CoverToTie-Tie)/1000;
  double tieCut=mark.SingleLeg?hc-2*r+2*(135*Math.PI/180*r)+2*tail:2*(wc+hc)-8*r+3*Math.PI*r+2*tail;
  steel.Result.Lines.Add(new EstimateLine{Beam=steel.Beam,Part="Stirrups",Diameter=(int)Tie,Grade=cfg.GradeSmall,PiecesPerBeam=ties,TieSetsPerBeam=ties,MainHoopCut=Up(tieCut),CutLength=Up(tieCut),Metres=Up(tieCut)*ties*qty,Kg=Up(tieCut)*ties*qty*EstimatorEngine.UnitMass((int)Tie),Formula=(mark.SingleLeg?"single-leg tie with 135° hooks around the top and bottom bars":"closed stirrup, 135° hooks")+" @ "+mark.TieSpacing+" mm; "+ties+" per lintel (50 mm from each end)"});
  /* jamb bars: one 12 mm bar per core under the bearing, each side, from the floor dowel (90°, 150 mm) to a 90° hook (150 mm) in the lintel */
  double head=opening.Kind=="Window"?Math.Max(WindowHead,opening.Height):opening.Height;
  add("Jamb bars",12,2*mark.JambBarsPerSide,head+.150+.150+h-barCover,mark.JambBarsPerSide+" x 12 mm each side, grouted cores; floor to lintel "+F(head)+" m"+(opening.Kind=="Window"?" (window head taken at "+F(WindowHead)+" m or the opening height)":"")+" + 150 mm dowel + 150 mm hook in the lintel");
  return new MiscStructureQuantity{Kind="Lintel Beam",Id=id,Description=steel.Description+" - "+mark.Mark+" "+mark.Width+" x "+mark.Depth+", bottom "+mark.Bottom+", top "+mark.Top+(mark.MidCount>0?", mid "+mark.Mid:"")+", ties "+mark.Ties+", bearing "+mark.Bearing+" mm"+(mark.Brace!=""?"; lateral brace: wall vertical bars 10 mm @ 800 max hooked into the lintel and dowelled into the beam / slab above":""),
   Fc=cfg.Input.Fc,Quantity=qty,Concrete=L*b*h*qty,Forms=(2*h*L+b*clear)*qty,Steel=steel};}
 static string F(double v){return v.ToString("0.00",CultureInfo.InvariantCulture);}
}
}
