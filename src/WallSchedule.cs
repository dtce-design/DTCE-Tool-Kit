using System;using System.Collections.Generic;using System.Globalization;using System.Linq;using System.Text.RegularExpressions;
namespace BeamScheduleFormatter {
/* walls in the column schedule (owner, 22 November 2026: "how about yung sa walls combined walls etc?", "siguro incorporate sa
   schedule?"): the RCDC COMBINED WALL SCHEDULE (e.g. CW1 with its legs C12, C18 and C19 per level) is read with the column schedule -
   per wall mark and level the concrete, the bars of each leg ("C18 : 15-#10+12-#10+..."), the links of each leg (MAIN LINKS / OTHER
   LINKS, BE MAIN LINK / BE OTHERS / MID MAIN LINKS / MID OTHERS) and the plan as RCDC drew it (bars, links, outline) - and drawn in a
   WALL block of the revised schedule. The walls are a drawing and text only: the Bar Bending Schedule and the Bill of Quantities take
   the columns. */
class WallLeg {public string Name="",Bars="";public List<string[]> Links=new List<string[]>();}
class WallCell {public string Concrete="";public List<WallLeg> Legs=new List<WallLeg>();public List<string> Legend=new List<string>();
 public List<double[]> Circles=new List<double[]>();public List<List<double[]>> Plines=new List<List<double[]>>();public List<bool> Closed=new List<bool>();public List<double[]> Lines=new List<double[]>();
 public double Width{get{return Extent(0);}}public double Height{get{return Extent(1);}}
 public double Min(int k){var v=Circles.Select(c=>c[k]-c[2]).Concat(Plines.SelectMany(p=>p.Select(q=>q[k]))).Concat(Lines.SelectMany(l=>new[]{l[k],l[k+2]})).ToList();return v.Count==0?0:v.Min();}
 public double Max(int k){var v=Circles.Select(c=>c[k]+c[2]).Concat(Plines.SelectMany(p=>p.Select(q=>q[k]))).Concat(Lines.SelectMany(l=>new[]{l[k],l[k+2]})).ToList();return v.Count==0?0:v.Max();}
 double Extent(int k){return Max(k)-Min(k);}}
class WallMark {public string Mark="",Kind="";public Dictionary<int,WallCell> Cells=new Dictionary<int,WallCell>();}
static class WallScheduleReader {
 static readonly CultureInfo Inv=CultureInfo.InvariantCulture;
 public static void Read(Dxf d,List<Entity> all,ColumnSchedule s){
  var texts=all.Where(e=>e.Type=="TEXT"||e.Type=="MTEXT").Select(e=>new{T=SlabFormatter.Plain(e).Replace("%%u","").Replace("%%U","").Trim(),X=e.Num(10),Y=e.Num(20),L=e.Get(8)}).ToList();
  var title=texts.FirstOrDefault(t=>Regex.IsMatch(t.T,@"WALL\s+SCHEDULE",RegexOptions.IgnoreCase)&&t.T.IndexOf("COLUMN",StringComparison.OrdinalIgnoreCase)<0);if(title==null)return;
  var region=texts.Where(t=>t.X>=title.X-100).ToList();
  var label=region.FirstOrDefault(t=>Regex.IsMatch(t.T,@"^(COMBINED\s+)?WALLS?(\s+MARK(ED)?)?$",RegexOptions.IgnoreCase));if(label==null)return;
  var marks=region.Where(t=>t.X>label.X+500&&Math.Abs(t.Y-label.Y)<900&&Regex.IsMatch(t.T,@"^[A-Z]{1,4}-?\d+[A-Z]?$",RegexOptions.IgnoreCase)).OrderBy(t=>t.X).ToList();if(marks.Count==0)return;
  var levelTexts=region.Where(t=>t.X<marks[0].X&&Regex.IsMatch(t.T,@"^-?\d+(\.\d+)?\s*M$",RegexOptions.IgnoreCase)).ToList();var tos=region.Where(t=>t.T=="TO"&&t.X<marks[0].X).OrderBy(t=>t.Y).ToList();if(tos.Count==0)return;
  double labelRight=levelTexts.Count>0?levelTexts.Max(t=>t.X)+1500:marks[0].X-3000;
  var geoAll=all.Where(e=>e.X0()>=title.X-100).ToList();double maxX=geoAll.Count>0?geoAll.Max(e=>e.Num(10)):marks.Last().X+10000;
  double pitch=marks.Count>1?marks[1].X-marks[0].X:2*(maxX-marks[0].X);
  var hLines=all.Where(e=>e.Type=="LINE"&&Math.Abs(e.Num(20)-e.Num(21))<1&&Math.Min(e.Num(10),e.Num(11))>=title.X-100&&Math.Abs(e.Num(11)-e.Num(10))>(maxX-labelRight)*.6).Select(e=>e.Num(20)).Distinct().ToList();
  var old=ColumnScheduleReader.OldPolylinesOf(d).Where(p=>p.Value.Count>1&&p.Value[0][0]>=title.X-100).ToList();
  foreach(var m in marks)s.Walls.Add(new WallMark{Mark=m.T.ToUpperInvariant(),Kind=Regex.Replace(label.T.ToUpperInvariant(),@"\s+MARK(ED)?$","")});
  for(int i=0;i<tos.Count;i++){var to=tos[i];var nums=levelTexts.Where(t=>Math.Abs(t.X-to.X)<2500&&Math.Abs(t.Y-to.Y)<2500).Select(t=>double.Parse(Regex.Match(t.T,@"-?\d+(\.\d+)?").Value,Inv)).ToList();if(nums.Count<2)continue;
   double lo=nums.Min(),hi=nums.Max();int level=s.WallLevels.FindIndex(q=>Math.Abs(q.FromM-lo)<1e-6&&Math.Abs(q.ToM-hi)<1e-6);if(level<0){s.WallLevels.Add(new ColumnLevel{From=lo.ToString("0.###",Inv)+" M",To=hi.ToString("0.###",Inv)+" M",FromM=lo,ToM=hi});level=s.WallLevels.Count-1;}
   /* the band of the level: halfway to the neighbouring levels; the lowest down to the header row (below the levels) */
   double gap=tos.Count>1?(i>0?to.Y-tos[i-1].Y:tos[i+1].Y-to.Y):20000,by0=i>0?(to.Y+tos[i-1].Y)/2:(label.Y<to.Y?label.Y+300:to.Y-gap/2),by1=i+1<tos.Count?(to.Y+tos[i+1].Y)/2:to.Y+(to.Y-by0);
   /* better: the schedule lines across the bands just below and above the level text */
   {var hy=hLines.Where(y=>y<to.Y).DefaultIfEmpty(double.NaN).Max();var hy1=hLines.Where(y=>y>to.Y).DefaultIfEmpty(double.NaN).Min();if(!double.IsNaN(hy))by0=hy;if(!double.IsNaN(hy1))by1=hy1;}
   for(int k=0;k<marks.Count;k++){double cx0=k==0?labelRight:marks[k].X-pitch/2,cx1=k+1<marks.Count?marks[k].X+pitch/2:maxX+1;
    var inCell=region.Where(t=>t.X>cx0&&t.X<cx1&&t.Y>by0&&t.Y<by1).ToList();
    var legNames=inCell.Where(t=>Regex.IsMatch(t.T,@"^[A-Z]{1,4}\d+[A-Z]?$")&&t.T.IndexOf(':')<0).ToList();
    var barTexts=inCell.Where(t=>Regex.IsMatch(t.T,@"^[A-Z]{1,4}\d+[A-Z]?\s*:\s*\S")&&t.T.IndexOf("FY",StringComparison.OrdinalIgnoreCase)<0).ToList();
    var conc=inCell.FirstOrDefault(t=>Regex.IsMatch(t.T,@":\s*FY",RegexOptions.IgnoreCase));if(barTexts.Count==0&&conc==null)continue;
    double tablesX=legNames.Count>0&&barTexts.Count>0&&legNames.Min(t=>t.X)>barTexts.Min(t=>t.X)?legNames.Min(t=>t.X)-500:cx1;
    var cell=new WallCell{Concrete=conc==null?"":conc.T};
    foreach(var b in barTexts.OrderBy(t=>t.X)){var mm=Regex.Match(b.T,@"^([A-Z]{1,4}\d+[A-Z]?)\s*:\s*(.+)$");cell.Legs.Add(new WallLeg{Name=mm.Groups[1].Value,Bars=mm.Groups[2].Value.Trim()});}
    /* the link table of each leg: the headers under its name, each with the value just below */
    var heads=inCell.Where(t=>t.X>=tablesX&&Regex.IsMatch(t.T,@"LINK|OTHERS",RegexOptions.IgnoreCase)).ToList();var values=inCell.Where(t=>t.X>=tablesX&&t.T.IndexOf('@')>=0).ToList();
    foreach(var h in heads){var owner=legNames.Where(n=>n.X>=tablesX-1&&n.Y>h.Y).OrderBy(n=>n.Y-h.Y).FirstOrDefault();if(owner==null)continue;var v=values.Where(q=>q.Y<h.Y&&h.Y-q.Y<2000&&Math.Abs(q.X-h.X)<2000).OrderBy(q=>h.Y-q.Y+Math.Abs(q.X-h.X)).FirstOrDefault();
     var leg=cell.Legs.FirstOrDefault(l=>l.Name==owner.T);if(leg==null){leg=new WallLeg{Name=owner.T};cell.Legs.Add(leg);}leg.Links.Add(new[]{h.T,v==null?"-":Regex.Replace(v.T,@"\s*@\s*"," @ ")});}
    foreach(var leg in cell.Legs)leg.Links=leg.Links.OrderBy(l=>l[0].StartsWith("BE")?0:l[0].StartsWith("MID")?2:1).ToList();
    cell.Legend=inCell.Where(t=>t.X<tablesX&&Regex.IsMatch(t.T,@"^(#|%%[cC]|T)\d+$")).Select(t=>t.T).Distinct().ToList();
    /* the plan as RCDC drew it, relative to the cell */
    Func<double,double,bool> inArea=(x,y)=>x>cx0&&x<tablesX&&y>by0&&y<by1;
    bool reinfLayer=all.Any(e=>e.Type=="CIRCLE"&&e.Get(8).IndexOf("Reinf",StringComparison.OrdinalIgnoreCase)>=0);
    foreach(var e in all){if(e.Type=="CIRCLE"){double x=e.Num(10),y=e.Num(20);if(inArea(x,y)&&(!reinfLayer||e.Get(8).IndexOf("Reinf",StringComparison.OrdinalIgnoreCase)>=0))cell.Circles.Add(new[]{x-cx0,y-by0,e.Num(40)});}
     else if(e.Type=="LWPOLYLINE"){var p=ColumnScheduleReader.PointsB(e);if(p.Count>1&&p.All(q=>inArea(q[0],q[1]))){cell.Plines.Add(p.Select(q=>new[]{q[0]-cx0,q[1]-by0,q[2]}).ToList());cell.Closed.Add((((int)e.Num(70))&1)==1);}}
     else if(e.Type=="LINE"&&e.Get(8).IndexOf("Dim",StringComparison.OrdinalIgnoreCase)<0){double ax=e.Num(10),ay=e.Num(20),bx=e.Num(11),bya=e.Num(21);if(inArea(ax,ay)&&inArea(bx,bya)&&Math.Max(Math.Abs(bx-ax),Math.Abs(bya-ay))<(tablesX-cx0)*.9)cell.Lines.Add(new[]{ax-cx0,ay-by0,bx-cx0,bya-by0});}}
    foreach(var p in old)if(p.Value.All(q=>inArea(q[0],q[1]))){cell.Plines.Add(p.Value.Select(q=>new[]{q[0]-cx0,q[1]-by0,q[2]}).ToList());cell.Closed.Add((((int)p.Key.Num(70))&1)==1);}
    /* keep the wall itself: around its bars (not the legend marks), with a margin for the outline */
    var legendAt=inCell.Where(t=>t.X<tablesX&&Regex.IsMatch(t.T,@"^(#|%%[cC]|T)\d+$")).Select(t=>new[]{t.X-cx0,t.Y-by0}).ToList();cell.Circles.RemoveAll(b=>legendAt.Any(q=>Math.Abs(b[0]-q[0])<900&&Math.Abs(b[1]-q[1])<500));
    if(cell.Circles.Count>0){double bx0=cell.Circles.Min(b=>b[0])-700,bx1=cell.Circles.Max(b=>b[0])+700,b0=cell.Circles.Min(b=>b[1])-700,b1=cell.Circles.Max(b=>b[1])+700;Func<double,double,bool> near=(x,y)=>x>=bx0&&x<=bx1&&y>=b0&&y<=b1;
     for(int q=cell.Plines.Count-1;q>=0;q--)if(!cell.Plines[q].All(v=>near(v[0],v[1]))){cell.Plines.RemoveAt(q);cell.Closed.RemoveAt(q);}cell.Lines.RemoveAll(l=>!near(l[0],l[1])||!near(l[2],l[3]));}
    s.Walls[k].Cells[level]=cell;}}
  s.Walls.RemoveAll(w=>w.Cells.Count==0);}
 static double X0(this Entity e){return e.Num(10);}
}
static partial class ColumnFormatter {
 /* the WALL block: WALL MARKED, the levels from the lowest, per wall and level the plan (as RCDC drew it, monochrome, to scale) above
    the concrete, the bars of each leg and its links */
 internal static double DrawWalls(ColumnSheet sh,ColumnSchedule s,double x0,double yTop,bool aci,double maxRight){if(s.Walls.Count==0)return yTop;
  Func<string,string> note=t=>aci?Regex.Replace(t??"",@"%%[cC](?=\d)","#"):Regex.Replace(t??"",@"#(?=\d)","%%C");
  var cells=s.Walls.SelectMany(w=>w.Cells.Values).ToList();double gw=cells.Max(c=>c.Width),gh=cells.Max(c=>c.Height);double k=Math.Min(1.0/50,Math.Min(110/Math.Max(gw,1),80/Math.Max(gh,1)));
  var order=Enumerable.Range(0,s.WallLevels.Count).OrderBy(l=>Math.Min(s.WallLevels[l].FromM,s.WallLevels[l].ToM)).ToList();
  double labelW=39,headH=23,cellW=Math.Max(68,gw*k+16),drawH=gh*k+14;
  Func<WallCell,List<string>> lines=c=>{var l=new List<string>();if(c.Concrete!="")l.Add(c.Concrete);foreach(var leg in c.Legs){if(leg.Bars!="")l.Add(leg.Name+" : "+note(leg.Bars));if(leg.Links.Count>0)l.Add(leg.Name+" LINKS: "+string.Join(", ",leg.Links.Select(q=>q[0]+" "+note(q[1]))));}return l;};
  double textH=cells.Max(c=>lines(c).Sum(t=>Math.Ceiling(t.Length*2*.62/(cellW-3)))*3.4+4);double rowH=drawH+textH,y=yTop,width=labelW+cellW*order.Count;
  sh.Text("WALL SCHEDULE",x0,y+3,4,7,true,"W-TITLE");
  sh.Rect(x0,y-headH,x0+width,y,"SCHEDULE");sh.Text("WALL MARKED",x0+labelW/2,y-headH/2,2.5,5,true,"W-HEAD");
  for(int i=0;i<order.Count;i++){var lv=s.WallLevels[order[i]];double cx=x0+labelW+i*cellW;sh.Line(cx,y,cx,y-headH,"SCHEDULE");sh.Text(Math.Min(lv.FromM,lv.ToM).ToString("0.00#",Inv)+" TO "+Math.Max(lv.FromM,lv.ToM).ToString("0.00#",Inv),cx+cellW/2,y-headH/2,2.5,5,false,"W-LEVEL");}
  y-=headH;
  foreach(var w in s.Walls){sh.Rect(x0,y-rowH,x0+width,y,"SCHEDULE");sh.Line(x0+labelW,y,x0+labelW,y-rowH,"SCHEDULE");sh.Text(w.Mark,x0+labelW/2,y-rowH/2+2,2.5,5,true,"W-MARK");if(w.Kind!="")sh.Text("("+w.Kind+")",x0+labelW/2,y-rowH/2-2,1.8,5,false,"W-MARK");
   for(int i=0;i<order.Count;i++){double cx0=x0+labelW+i*cellW,cx1=cx0+cellW;if(i>0)sh.Line(cx0,y,cx0,y-rowH,"SCHEDULE");WallCell c;if(!w.Cells.TryGetValue(order[i],out c)){sh.Line(cx0,y,cx1,y-rowH,"SCHEDULE");sh.Line(cx0,y-rowH,cx1,y,"SCHEDULE");continue;}
    double mx=c.Min(0),my=c.Min(1),ox=(cx0+cx1)/2-c.Width*k/2,oy=y-drawH/2-c.Height*k/2;Func<double,double> X=v=>ox+(v-mx)*k;Func<double,double> Y=v=>oy+(v-my)*k;
    foreach(var l in c.Lines)sh.Line(X(l[0]),Y(l[1]),X(l[2]),Y(l[3]),"W-OUTLINE");
    for(int p=0;p<c.Plines.Count;p++)sh.PLine(c.Plines[p].Select(q=>new[]{X(q[0]),Y(q[1]),q[2]}).ToList(),c.Closed[p],0,"W-LINKS");
    foreach(var b in c.Circles)sh.Circle(X(b[0]),Y(b[1]),Math.Max(.25,b[2]*k),"W-BARS");
    sh.Rect(cx0,y-rowH,cx1,y-rowH+textH,"W-CELLBOX");sh.Text(string.Join("\n",lines(c)),cx0+1.5,y-rowH+textH-2,2,1,false,"W-TEXT",cellW-3);}
   y-=rowH;}
  sh.Text("WALL PLANS AS DRAWN BY RCDC (NOT TO SCALE; THE LEG LENGTHS AND THICKNESSES ARE IN THE RCDC SCHEDULE). (x-"+(aci?"#":"%%C")+"xx) DENOTES BARS WITHIN THE INTERSECTION OF THE LEGS.",x0,y-3,1.9,7,false,"W-TEXT",width);
  return y-8;}
}
}
