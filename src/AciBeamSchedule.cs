using System;using System.Collections.Generic;using System.Globalization;using System.Linq;using System.Text.RegularExpressions;
namespace BeamScheduleFormatter {
/* the ACI option of the Beam Formatter as the ACI Detailing Manual MNL-66(20) Fig. BM-1_1 REINFORCED CONCRETE BEAM SCHEDULE (owner,
   22 November 2026: "di naman sinundan yung format or template ng ACI"): MARK | SIZE B x H | TYPE | MAIN HORIZONTAL REINFORCEMENT -
   TOP A (continuous), L4, B (added at the left support), C (added at the right support), L5, BOTTOM E (continuous), F (added at
   midspan), SIDE BAR EACH | TIES TR (end zones), TYPE, REST, TYPE | REMARKS, with the hoop (H1, H3, H4) and stirrup (S1, S2) details
   that the schedule uses. The values come from the formatted rows; L4 / L5 (cut-off of the added top bars) are not in the source
   schedule and are left to the design. Each row is kept as hidden data so the revised DXF still loads in the Bar Bending Schedule and
   the Bill of Quantities. */
static class AciBeamSchedule {
 static readonly CultureInfo Inv=CultureInfo.InvariantCulture;const string Data="DTCE-DATA";
 static string N(string t){return AciDetailing.Notation(Engine.Format(t)).Replace(" - ","-");}
 static List<string> Groups(BeamRow r,int c){return r.Cells[c].Where(t=>t.Trim()!="+"&&t.Trim()!="-"&&t.Trim()!="--"&&t.Trim()!="").Select(t=>t.Trim()).ToList();}
 static string Key(string t){return Engine.Normal(t);}
 /* the groups of a support not in the continuous groups */
 static List<string> Added(List<string> at,List<string> continuous){var rest=new List<string>(at);foreach(var c in continuous){int i=rest.FindIndex(x=>Key(x)==Key(c));if(i>=0)rest.RemoveAt(i);}return rest;}
 static string Join(IEnumerable<string> g){var l=g.ToList();return l.Count==0?"-":string.Join(" + ",l.Select(N));}
 internal static string[] Values(BeamRow r){var topL=Groups(r,5);var topM=Groups(r,6);var topR=Groups(r,7);var botL=Groups(r,2);var botM=Groups(r,3);var botR=Groups(r,4);
  var a=topM.Count>0?topM:topL.Take(1).ToList();var e=botL.Where(x=>botR.Any(y=>Key(y)==Key(x))&&botM.Any(y=>Key(y)==Key(x))).ToList();if(e.Count==0)e=botM.Take(1).ToList();
  string dia=AciDetailing.Notation(Engine.Format(r.Diameter??""));string tr="-",rest="-";var m=Regex.Match(r.Spacing??"",@"^\s*1\s*-\s*(\d+)mm,\s*(?:(\d+)\s*-\s*(\d+)mm,\s*)?AND\s+REST\s+AT\s+(\d+)mm",RegexOptions.IgnoreCase);
  if(m.Success){if(m.Groups[2].Success)tr=dia+": 1 @ "+m.Groups[1].Value+", "+m.Groups[2].Value+" @ "+m.Groups[3].Value;rest=dia+" @ "+m.Groups[4].Value;}else if(!string.IsNullOrWhiteSpace(r.Spacing)&&r.Spacing!="--")tr=dia+": "+AciDetailing.Notation(r.Spacing,"Beam");
  int legs;int.TryParse(Regex.Match(r.Leg??"",@"\d+").Value,out legs);string hoop=legs>=4?"H3":legs==3?"H4":"H1",stir=legs>=3?"S2":"S1";string sfr=r.Cell(11);bool skin=sfr!="--"&&sfr!="-";double d;double.TryParse(r.Cell(1),NumberStyles.Float,Inv,out d);
  var remarks=new List<string>();if(r.Numbers.Count>0)remarks.Add(string.Join(", ",r.Numbers));if(r.Cantilever)remarks.Add("CANTILEVER");var bL=Added(botL,e);var bR=Added(botR,e);if(bL.Count>0)remarks.Add("BOT. LEFT +"+Join(bL));if(bR.Count>0)remarks.Add("BOT. RIGHT +"+Join(bR));
  return new[]{r.Mark,r.Cell(0)+" x "+r.Cell(1),skin||d>900?"2":"1",Join(a),"*",Join(Added(topL,a)),Join(Added(topR,a)),"*",Join(e),Join(Added(botM,e)),skin?N(sfr):"-",tr,tr=="-"?"-":hoop,rest,rest=="-"?"-":stir,string.Join("; ",remarks)};}
 public static Dxf Build(Result result,string floor){var sh=new ColumnSheet();
  double[] w={22,26,12,30,10,30,30,10,30,30,22,40,12,24,12,46};double x0=0,top=0,th=2;double width=w.Sum();Func<int,double> X=i=>x0+w.Take(i).Sum();
  if(!string.IsNullOrWhiteSpace(floor))sh.Text(floor.Trim().ToUpperInvariant(),0,top+12,4,7,true,"TITLE",width);
  double y=top;sh.Rect(x0,y-9,x0+width,y,"SCHEDULE");sh.Text("REINFORCED CONCRETE BEAM SCHEDULE",x0+width/2,y-4.5,4.2,5,true,"TITLE",width-4);y-=9;
  double h1=6,h2=6,h3=6,hy=y,hb=y-h1-h2-h3;sh.Rect(x0,hb,x0+width,hy,"SCHEDULE");
  Action<int,int,double,double,string> head=(c0,c1,ya,yb,t)=>{sh.Rect(X(c0),yb,X(c1+1),ya,"SCHEDULE");sh.Text(t,(X(c0)+X(c1+1))/2,(ya+yb)/2,2,5,false,"HEAD",X(c1+1)-X(c0)-1);};
  head(0,0,hy,hb,"MARK");head(1,1,hy,hy-h1,"SIZE");head(1,1,hy-h1,hb,"B x H");head(2,2,hy,hb,"TYPE");head(3,10,hy,hy-h1,"MAIN HORIZONTAL REINFORCEMENT");head(3,7,hy-h1,hy-h1-h2,"TOP");head(8,9,hy-h1,hy-h1-h2,"BOTTOM");head(10,10,hy-h1,hb,"SIDE BAR EACH");
  string[] letters={"A","L4","B","C","L5","E","F"};for(int i=0;i<7;i++)head(3+i,3+i,hy-h1-h2,hb,letters[i]);head(11,14,hy,hy-h1,"TIES");head(11,11,hy-h1,hb,"TR");head(12,12,hy-h1,hb,"TYPE");head(13,13,hy-h1,hb,"REST");head(14,14,hy-h1,hb,"TYPE");head(15,15,hy,hb,"REMARKS");
  y=hb;var used=new HashSet<string>();
  foreach(var r in result.Rows){var v=Values(r);int lines=1;for(int i=0;i<v.Length;i++)lines=Math.Max(lines,(int)Math.Ceiling(v[i].Length*th*.62/(w[i]-1.5)));double rh=Math.Max(6,lines*th*1.6+2.4);
   for(int i=0;i<v.Length;i++){sh.Rect(X(i),y-rh,X(i+1),y,"SCHEDULE");sh.Text(v[i],X(i)+w[i]/2,y-rh/2,th,5,i==0,i==0?"MARK":"CELLTEXT",w[i]-1.5);}
   if(v[12]!="-")used.Add(v[12]);if(v[14]!="-")used.Add(v[14]);
   sh.Text("ACI BEAM|"+r.Mark+"|"+(r.Leg??"")+"|"+(r.Diameter??"")+"|"+(r.Spacing??"")+"|"+string.Join("|",Enumerable.Range(0,12).Select(c=>string.Join("~",r.Cells[c]))),X(0)+w[0]/2,y-rh/2,.8,5,false,Data);y-=rh;}
  /* hoop and stirrup details (Fig. BM-1_1) */
  y-=10;double dx=0;sh.Text("HOOP DETAILS",0,y,2.8,7,true,"TITLE",60);sh.Text("STIRRUP DETAILS",width/2,y,2.8,7,true,"TITLE",60);y-=6;double hx=0,sx=width/2;
  foreach(var t in new[]{"H1","H3","H4","S1","S2"}.Where(used.Contains)){bool hoopT=t.StartsWith("H");double bx=hoopT?hx:sx;Detail(sh,t,bx+4,y-26);sh.Text(t,bx+12,y-30,2.4,5,true,"TITLE",12);sh.Line(bx+9,y-31.6,bx+15,y-31.6,"TITLE");if(hoopT)hx+=30;else sx+=30;}
  y-=40;var notes=new List<string>{"NOTES:","1. MINIMUM INSIDE BEND DIAMETER AND STANDARD HOOK GEOMETRY PER ACI 318 25.3; HOOPS WITH 135° SEISMIC HOOKS.","2. A = CONTINUOUS TOP BARS, B / C = ADDED TOP BARS AT THE LEFT / RIGHT SUPPORT, E = CONTINUOUS BOTTOM BARS, F = ADDED BOTTOM BARS AT MIDSPAN; TR = TIES IN THE END ZONES, REST = TIES ELSEWHERE.","3. * L4 / L5: CUT-OFF LENGTHS OF THE ADDED TOP BARS FROM THE SUPPORT FACE, PER THE DESIGN (NOT GIVEN IN THE SOURCE SCHEDULE).","4. TYPE 1: TYPICAL BEAM SECTION; TYPE 2: BEAM WITH SIDE (SKIN) BARS EACH FACE. DIMENSIONS IN mm; BAR NOTATION #16 = 16 mm BAR."};
  foreach(var n in notes){sh.Text(n,0,y,2,7,n=="NOTES:","NOTES",width);y-=4;}
  y-=6;sh.Text("REINFORCED CONCRETE BEAM SCHEDULE",width/2,y,5,5,true,"TITLE",width);sh.Line(width/2-48,y-4.4,width/2+48,y-4.4,"TITLE");
  sh.Left=0;sh.Right=width;sh.Top=top+(string.IsNullOrWhiteSpace(floor)?2:16);sh.Bottom=y-8;return ColumnFormatter.ToDxf(sh,null,null);}
 /* the hoop and stirrup shapes, about 16 x 22 */
 static void Detail(ColumnSheet sh,string t,double x,double y){double w=16,h=22,r=1.2,k=.707;const string L="DETAIL";
  Action<double,double,double,double> hoop=(a,b,c,d)=>{sh.RoundRect(a,b,c,d,r,L);sh.Line(a+r*(1-k),d-r*(1-k),a+r*(1-k)+2.6,d-r*(1-k)-2.6,L);sh.Line(a+r*(1-k)+1,d-r*(1-k)+.2,a+r*(1-k)+3.6,d-r*(1-k)-2.4,L);};
  Action<double,double,double> stirrup=(a,c,b)=>{sh.PLine(new List<double[]>{new[]{a+2.2,y+h-2.2,0},new[]{a,y+h,0},new[]{a,b+r,.41421356},new[]{a+r,b,0},new[]{c-r,b,.41421356},new[]{c,b+r,0},new[]{c,y+h,0},new[]{c-2.2,y+h-2.2,0}},false,0,L);};
  if(t=="H1")hoop(x,y,x+w,y+h);else if(t=="H3"){hoop(x,y,x+w*.65,y+h);hoop(x+w*.35,y+1,x+w,y+h-1);}else if(t=="H4"){hoop(x,y,x+w,y+h);stirrup(x+w*.3,x+w*.7,y+1);}
  else if(t=="S1")stirrup(x,x+w,y);else if(t=="S2"){stirrup(x,x+w*.65,y);stirrup(x+w*.35,x+w,y+1);}}
 /* reading the ACI schedule back: the rows from the hidden data */
 public static Analysis Read(List<Entity> entities,string name){var data=entities.Where(e=>(e.Type=="MTEXT"||e.Type=="TEXT")&&e.Get(8)==Data).Select(e=>string.Concat(e.P.Where(p=>p.Code==3||p.Code==1).Select(p=>p.Value))).Where(t=>t.StartsWith("ACI BEAM|")).ToList();if(data.Count==0)return null;
  var a=new Analysis{Path=name,Floor=""};foreach(var t in data){var p=t.Split('|');if(p.Length<17)continue;var r=new BeamRow{Mark=p[1],Leg=p[2],Diameter=p[3],Spacing=p[4],RevisedSpacing=p[4]};r.Numbers.Add(r.Mark);r.SourceRows.Add(a.Source.Count+1);
   for(int c=0;c<12;c++)if(p[5+c]!="")r.Cells[c].AddRange(p[5+c].Split('~'));r.Cells[8].Clear();r.Cells[8].Add(r.Leg);r.Cells[9].Clear();r.Cells[9].Add(r.Diameter);r.Cells[10].Clear();r.Cells[10].Add(r.Spacing);
   r.Cells[12].Add("-");r.Cells[13].Add(r.Spacing.IndexOf("TO FREE END",StringComparison.OrdinalIgnoreCase)>=0?"CANTILEVERED":"-");a.Source.Add(r);}
  a.BeamCount=a.Source.Count;a.Prefix=a.Source.Count>0?Regex.Match(a.Source[0].Mark,@"^\d*[A-Z]+").Value:"";return a.Source.Count>0?a:null;}
}
}
