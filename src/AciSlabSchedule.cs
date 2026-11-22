using System;using System.Collections.Generic;using System.Globalization;using System.Linq;using System.Text.RegularExpressions;
namespace BeamScheduleFormatter {
/* the ACI option of the Slab Formatter as the ACI Detailing Manual MNL-66(20) templates (owner, 22 November 2026: "kapag sinabi kong
   ACI yung template mismo ng ACI ang gagamitin", "based sa schedule ko dimo ba machecheck sa pattern ng bars if 2 way or 1 way"):
   each slab is one-way or two-way by its bars - one-way when the long span has no curtailed bottom bars and no top bars over the
   short supports (the long span bars are then distribution) - and goes in Fig. SLAB-1 ONE-WAY CONCRETE SLAB SCHEDULE (MARK | DEPTH |
   MAIN REINFORCEMENT TOP LEFT / BOTTOM / TOP RIGHT | TEMPERATURE REINFORCEMENT TOP / BOT | REMARKS) or Fig. SLAB-2 TWO-WAY CONCRETE
   SLAB SCHEDULE (MARK | DEPTH | TOP A B C D I J | BOTTOM E F G H | REMARKS; a beam-supported panel has the same bars in its column and
   middle strips, so one table), each with its typical section. Each row is kept as hidden data so the revised DXF still loads. */
static class AciSlabSchedule {
 static readonly CultureInfo Inv=CultureInfo.InvariantCulture;const string Data="DTCE-DATA";
 /* bars: 0 / 1 bottom short span full / curtailed, 2 / 3 bottom long span full / curtailed, 4 / 5 top over the long support continuous
    / end, 6 / 7 top over the short support continuous / end, 8 distribution */
 internal static bool OneWay(SlabScheduleRow r){return r.Bars[3]==null&&r.Bars[6]==null&&r.Bars[7]==null;}
 static string B(SlabBar b){return b==null?"-":"#"+b.Diameter+" @ "+b.SpacingMm.ToString("0.##",Inv);}
 static string Two(SlabBar a,SlabBar b){if(a==null)return B(b);if(b==null)return B(a);return B(a)+" + "+B(b);}
 static string Depth(double t){return (t<5?t*1000:t).ToString("0",Inv);}
 static double Table(ColumnSheet sh,double y,string title,string[] groups,int[] spans,string[] heads,double[] w,List<string[]> rows,List<string> data){double width=w.Sum(),x0=0;Func<int,double> X=i=>x0+w.Take(i).Sum();
  sh.Rect(x0,y-10,x0+width,y,"SCHEDULE");sh.Text(title,width/2,y-5,4.4,5,true,"TITLE",width);y-=10;double h1=6,h2=6;
  int c=0;for(int g=0;g<groups.Length;g++){double a=X(c),b=X(c+spans[g]);if(groups[g]==""){c+=spans[g];continue;}sh.Rect(a,y-h1,b,y,"SCHEDULE");sh.Text(groups[g],(a+b)/2,y-h1/2,2,5,false,"HEAD",b-a-1);c+=spans[g];}
  for(int i=0;i<heads.Length;i++){bool tall=Array.IndexOf(new[]{"MARK","DEPTH (mm)","REMARKS"},heads[i])>=0;double top=tall?y:y-h1;sh.Rect(X(i),y-h1-h2,X(i+1),top,"SCHEDULE");sh.Text(heads[i],X(i)+w[i]/2,(top+y-h1-h2)/2,2,5,false,"HEAD",w[i]-1);}
  y-=h1+h2;for(int r=0;r<rows.Count;r++){var v=rows[r];int lines=1;for(int i=0;i<v.Length;i++)lines=Math.Max(lines,(int)Math.Ceiling(v[i].Length*2*.62/(w[i]-1.5)));double rh=Math.Max(6,lines*3.2+2.4);
   for(int i=0;i<v.Length;i++){sh.Rect(X(i),y-rh,X(i+1),y,"SCHEDULE");sh.Text(v[i],X(i)+w[i]/2,y-rh/2,2,5,i==0,i==0?"MARK":"CELLTEXT",w[i]-1.5);}sh.Text(data[r],X(0)+w[0]/2,y-rh/2,.8,5,false,Data);y-=rh;}
  return y;}
 /* the typical sections (Fig. SLAB-1 / SLAB-2_2), not to scale */
 static double Section(ColumnSheet sh,double y,double width,bool oneWay){const string L="ELEVATION",T="DIAGRAM";double t=8,top=y-16,bot=top-t,x0=10,span=(width-50)/2.1,beamW=10;double s0=x0,s1=s0+beamW+span,s2=s1+beamW+span,end=Math.Min(width-4,s2+beamW+span*.15);
  foreach(var sx in new[]{s0,s1,s2}){sh.Line(sx,bot,sx,bot-14,L);sh.Line(sx+beamW,bot,sx+beamW,bot-14,L);sh.Line(sx,bot-14,sx+beamW,bot-14,L);}
  sh.Line(s0,top,end,top,L);sh.Line(s0,bot,s0,top,L);for(int i=0;i<2;i++){double a=(i==0?s0:s1)+beamW,b=(i==0?s1:s2);sh.Line(a,bot,b,bot,L);}sh.Line(s2+beamW,bot,end,bot,L);
  double c=1.4;/* top bars at the end support and over the interior supports, bottom bars, temperature / distribution dots */
  sh.Line(s0+c,bot+c+3,s0+c,top-c,T);sh.Line(s0+c,top-c,s0+beamW+span/4,top-c,T);foreach(var sx in new[]{s1,s2}){sh.Line(sx-span/4,top-c,Math.Min(end-2,sx+beamW+span/4),top-c,T);}
  sh.Line(s0+c,bot+c,end,bot+c,T);for(double d=s0+beamW+3;d<end-3;d+=4){sh.Circle(d,bot+c+.8,.35,T);sh.Circle(d+2,top-c-.8,.35,T);}
  Action<string,double,double,double,double> tag=(s,x,yy,tx,ty)=>{sh.Line(x,yy,tx,ty,"DIM");sh.Text(s,tx,ty+(ty>yy?1.2:-1.2),2,ty>yy?8:2,false,"DIM",30);};
  Action<double,double,double,string> dim=(a,b,yy,s)=>{sh.Line(a,yy,b,yy,"DIM");sh.Line(a,yy-1,a,yy+1,"DIM");sh.Line(b,yy-1,b,yy+1,"DIM");sh.Text(s,(a+b)/2,yy+.8,1.9,8,false,"DIM",Math.Max(8,b-a));};
  if(oneWay){tag("TOP LEFT",s0+beamW+span/8,top-c,s0+beamW+span/8,top+6);tag("TOP RIGHT",s1-span/8,top-c,s1-span/8,top+6);tag("BOTTOM (MAIN)",s0+beamW+span/2,bot+c,s0+beamW+span/2,bot-18);tag("TEMPERATURE BARS TOP / BOT",s1+beamW+span*.5,top-c-.8,s1+beamW+span*.5,top+6);}
  else{tag("A (DISCONTINUOUS EDGE)",s0+beamW+span/8,top-c,s0+beamW+span/8,top+6);tag("C (CONTINUOUS SUPPORT)",s1-span/8,top-c,s1-span/8,top+6);tag("E / F (BOTTOM)",s0+beamW+span/2,bot+c,s0+beamW+span/2,bot-18);tag("SECOND DIRECTION (B, D, G, H)",s1+beamW+span*.5,top-c-.8,s1+beamW+span*.5,top+6);}
  dim(s0+beamW,s0+beamW+span/4,top+17,"Ln/4");dim(s1-span/4,s1,top+17,"Ln/4");dim(s1+beamW,s1+beamW+span/4,top+17,"Ln/4");dim(s0+beamW,s1,bot-28,"L1");dim(s1+beamW,s2,bot-28,"L2");
  double secY=bot-36;string ttl=oneWay?"ONE-WAY CONCRETE SLAB - TYPICAL SECTION (NOT TO SCALE)":"TWO-WAY CONCRETE SLAB - TYPICAL SECTION (NOT TO SCALE)";sh.Text(ttl,width/2,secY,3.6,5,true,"TITLE",width);sh.Line(width/2-60,secY-3.4,width/2+60,secY-3.4,"TITLE");return secY-8;}
 static string DataOf(SlabFormatGroup g,int index){var r=g.Schedule;return "ACI SLAB|"+index+"|"+g.Mark+"|"+r.Thickness.ToString("R",Inv)+"|"+string.Join("|",r.Bars.Select(b=>b==null?"":b.Diameter+"@"+b.SpacingMm.ToString("R",Inv)));}
 public static Dxf Build(SlabFormatResult result,string floor){var sh=new ColumnSheet();double y=0;var one=result.Groups.Where(g=>OneWay(g.Schedule)).ToList();var two=result.Groups.Where(g=>!OneWay(g.Schedule)).ToList();double width=0;
  if(!string.IsNullOrWhiteSpace(floor))sh.Text(floor.Trim().ToUpperInvariant(),0,y+8,4,7,true,"TITLE",200);
  if(one.Count>0){double[] w={24,22,32,40,32,32,32,60};width=Math.Max(width,w.Sum());
   y=Table(sh,y,"ONE-WAY CONCRETE SLAB SCHEDULE",new[]{"","MAIN REINFORCEMENT","TEMPERATURE REINFORCEMENT",""},new[]{2,3,2,1},new[]{"MARK","DEPTH (mm)","TOP LEFT","BOTTOM","TOP RIGHT","TOP","BOT","REMARKS"},w,
    one.Select(g=>{var b=g.Schedule.Bars;return new[]{g.Mark,Depth(g.Schedule.Thickness),B(b[5]),Two(b[0],b[1]),B(b[4]),B(b[8]),B(b[2]),g.Originals};}).ToList(),one.Select(g=>DataOf(g,result.Groups.IndexOf(g))).ToList());
   y=Section(sh,y-12,w.Sum(),true);}
  if(two.Count>0){double[] w={22,20,24,24,24,24,16,16,24,24,24,24,50};width=Math.Max(width,w.Sum());y-=6;
   y=Table(sh,y,"TWO-WAY CONCRETE SLAB SCHEDULE",new[]{"","TOP","BOTTOM",""},new[]{2,6,4,1},new[]{"MARK","DEPTH (mm)","A","B","C","D","I","J","E","F","G","H","REMARKS"},w,
    two.Select(g=>{var b=g.Schedule.Bars;return new[]{g.Mark,Depth(g.Schedule.Thickness),B(b[5]),B(b[7]),B(b[4]),B(b[6]),"-","-",B(b[0]),B(b[1]),B(b[2]),B(b[3]),g.Originals+(b[8]!=null?"; DISTRIBUTION "+B(b[8]):"")};}).ToList(),two.Select(g=>DataOf(g,result.Groups.IndexOf(g))).ToList());
   y=Section(sh,y-12,w.Sum(),false);}
  var notes=new List<string>{"NOTES:","1. ONE-WAY / TWO-WAY BY THE BARS OF EACH SLAB: ONE-WAY WHEN THE LONG SPAN HAS NO CURTAILED BOTTOM BARS AND NO TOP BARS OVER THE SHORT SUPPORTS.",
   "2. ONE-WAY: MAIN REINFORCEMENT ALONG THE SHORT SPAN (TOP LEFT = OVER THE END SUPPORT, TOP RIGHT = OVER THE CONTINUOUS SUPPORT, BOTTOM = FULL LENGTH + CURTAILED); TEMPERATURE REINFORCEMENT: TOP = DISTRIBUTION BARS, BOT = LONG SPAN BARS (ACI 318 7.6.4, 24.4).",
   "3. TWO-WAY (BEAM-SUPPORTED, THE SAME BARS IN THE COLUMN AND MIDDLE STRIPS): TOP A / B = OVER THE DISCONTINUOUS EDGE, SHORT / LONG DIRECTION; C / D = OVER THE CONTINUOUS SUPPORT, SHORT / LONG; I / J = CANTILEVER (NONE); BOTTOM E / F = SHORT DIRECTION FULL LENGTH / CURTAILED, G / H = LONG DIRECTION FULL LENGTH / CURTAILED.",
   "4. TOP BARS EXTEND Ln/4 PAST THE SUPPORT FACE (THE CURTAILMENT OF THE BAR BENDING SCHEDULE); AT LEAST 1/4 OF THE BOTTOM BARS CONTINUOUS INTO THE SUPPORT; LAP SPLICES CLASS B. BAR NOTATION #10 = 10 mm BAR, SPACING IN mm O.C."};
  width=Math.Max(width,200);foreach(var n in notes){sh.Text(n,0,y,2,7,n=="NOTES:","NOTES",width);y-=n.Length*2*.62>width?7.4:4;}
  sh.Left=0;sh.Right=width;sh.Top=12;sh.Bottom=y-6;return ColumnFormatter.ToDxf(sh,null,null);}
 /* reading the ACI slab schedule back from its hidden data (null when it is not one) */
 public static List<SlabScheduleRow> Read(Dxf drawing){var data=drawing.ModelEntities().Where(e=>(e.Type=="MTEXT"||e.Type=="TEXT")&&e.Get(8)==Data).Select(SlabFormatter.Plain).Where(t=>t.StartsWith("ACI SLAB|")).ToList();if(data.Count==0)return null;
  var rows=new List<KeyValuePair<int,SlabScheduleRow>>();foreach(var t in data){var p=t.Split('|');if(p.Length<13)continue;var r=new SlabScheduleRow{Mark=p[2],Thickness=double.Parse(p[3],Inv),Bars=new SlabBar[9]};
   for(int i=0;i<9;i++){var m=Regex.Match(p[4+i],@"^(\d+)@(.+)$");if(m.Success)r.Bars[i]=new SlabBar{Diameter=int.Parse(m.Groups[1].Value,Inv),SpacingMm=double.Parse(m.Groups[2].Value,Inv),Raw="#"+m.Groups[1].Value+" @ "+m.Groups[2].Value};}rows.Add(new KeyValuePair<int,SlabScheduleRow>(int.Parse(p[1],Inv),r));}
  /* in the formatted order */return rows.OrderBy(q=>q.Key).Select(q=>q.Value).ToList();}
}
}
