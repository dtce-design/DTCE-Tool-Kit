using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Drawing2D;using System.Globalization;using System.Linq;
namespace BeamScheduleFormatter {
/* Components and cladding (owner, 24 November 2026: "diba yung existing is for MWFRS, gawa ka naman ng for C&C"): NSCP 2015 Section 207E
   Part 1, low-rise enclosed / partially enclosed buildings, h <= 18 m (same method as ASCE 7-10 Chapter 30 Part 1):
     p = qh [(GCp) - (GCpi)]                     qh at the mean roof height, the exposure giving the highest load of any direction
     walls: zone 4 (interior), zone 5 (corners, width a)       roof: zone 1 (interior), 2 (edges), 3 (corners)
     a = min(0.1 least horizontal dimension, 0.4 h) >= max(0.04 least dimension, 0.9 m)
   GCp from Figures 207E.4-1 (walls) and 207E.4-2A/B/C (gable roofs, slope <= 7°, 7-27°, 27-45°), interpolated on log(A) between
   0.93 m² (10 ft²) and 9.3 m² (100 ft², roofs) or 46.5 m² (500 ft², walls); wall values x 0.9 when the roof slope is <= 10°.
   Minimum net design pressure 0.77 kPa (16 psf). Effective wind area A = span x the larger of the tributary width and span / 3. */
sealed class CcElement {public string Name;public double Area;}
sealed class CcResult {public string Element,Zone;public double Area,GCpPos,GCpNeg,PPos,PNeg;}
sealed class CcSummary {public double Qh,GCpi,A,Least,Angle,H;public string Exposure,Direction,RoofRange,Note;public List<CcResult> Rows=new List<CcResult>();}
static class WindComponentsCladding {
 static readonly CultureInfo Ci=CultureInfo.InvariantCulture;
 const double A10=0.9290304,A100=9.290304,A500=46.4515;const double MinPressure=0.77;
 /* linear in log(A) between (a1, v1) and (a2, v2), constant outside */
 static double LogInterp(double area,double a1,double v1,double a2,double v2){if(area<=a1)return v1;if(area>=a2)return v2;return v1+(v2-v1)*Math.Log10(area/a1)/Math.Log10(a2/a1);}
 internal static double WallPos(double area,double angle){double v=LogInterp(area,A10,1.0,A500,0.7);return angle<=10?0.9*v:v;}
 internal static double WallNeg(double area,int zone,double angle){double v=zone==5?LogInterp(area,A10,-1.4,A500,-0.8):LogInterp(area,A10,-1.1,A500,-0.8);return angle<=10?0.9*v:v;}
 internal static double RoofPos(double area,double angle){return angle<=7?LogInterp(area,A10,0.3,A100,0.2):angle<=27?LogInterp(area,A10,0.5,A100,0.3):LogInterp(area,A10,0.9,A100,0.8);}
 internal static double RoofNeg(double area,int zone,double angle){
  if(angle<=7)return zone==1?LogInterp(area,A10,-1.0,A100,-0.9):zone==2?LogInterp(area,A10,-1.8,A100,-1.1):LogInterp(area,A10,-2.8,A100,-1.1);
  if(angle<=27)return zone==1?LogInterp(area,A10,-0.9,A100,-0.8):zone==2?LogInterp(area,A10,-1.7,A100,-1.2):LogInterp(area,A10,-2.6,A100,-2.0);
  return zone==1?LogInterp(area,A10,-1.0,A100,-0.8):LogInterp(area,A10,-1.2,A100,-1.0);}
 internal static double EdgeWidth(double least,double h){return Math.Max(Math.Min(0.1*least,0.4*h),Math.Max(0.04*least,0.9));}
 /* "Roof sheet / fastener @ 0.93, Purlin @ 9.3" */
 internal static List<CcElement> ParseElements(string text){var list=new List<CcElement>();foreach(var part in (text??"").Split(new[]{',',';','\n'},StringSplitOptions.RemoveEmptyEntries)){var p=part.Split('@');double a;if(p.Length!=2||!double.TryParse(p[1].Trim(),NumberStyles.Float,Ci,out a)||a<=0)throw new Exception("C&C elements: write each element as name @ effective wind area m², e.g. Roof sheet @ 0.93, Purlin @ 9.3.");list.Add(new CcElement{Name=p[0].Trim(),Area=a});}if(list.Count==0)throw new Exception("Enter at least one C&C element (name @ effective wind area m²).");return list;}
 internal static CcSummary Calculate(WindBuildingInput b,List<WindPressureRow> mwfrsRows,string elements,IList<WindDirection> directions=null,double speed=double.NaN){
  var s=new CcSummary{Angle=b.Angle,H=b.Height,Least=Math.Min(b.Width,b.Length),GCpi=b.Enclosure=="Enclosed"?0.18:0.55};
  if(b.Height>18.3)throw new Exception("C&C Part 1 is for mean roof heights up to 18 m; this building is "+b.Height.ToString("0.##",Ci)+" m (use Part 3 / an engineer's method).");
  if(b.Angle>45)throw new Exception("C&C Part 1 gable roofs cover slopes up to 45°.");
  var top=mwfrsRows.OrderByDescending(r=>r.Qh).FirstOrDefault();if(top==null)throw new Exception("Building pressures are needed first (load the site terrain and enter the building).");
  s.Qh=top.Qh;s.Exposure=top.Exposure;s.Direction=top.Direction.Split('/')[0].Trim();
  /* C&C: the exposure giving the highest load of ANY of the eight directions, not only the four principal building axes */
  if(directions!=null&&WindTin.Finite(speed))foreach(var d in directions){if(d.Exposure!="B"&&d.Exposure!="C"&&d.Exposure!="D")continue;double kzt=WindBuildingLoads.TopographyAt(d,b.Height);if(!WindTin.Finite(kzt))continue;double q=.613*WindBuildingLoads.Kz(d.Exposure,b.Height)*kzt*b.Kd*Math.Pow(speed/3.6,2)/1000;if(q>s.Qh+1e-12){s.Qh=q;s.Exposure=d.Exposure;s.Direction=d.Name;}}s.A=EdgeWidth(s.Least,s.H);s.RoofRange=s.Angle<=7?"θ ≤ 7° (Figure 207E.4-2A)":s.Angle<=27?"7° < θ ≤ 27° (Figure 207E.4-2B)":"27° < θ ≤ 45° (Figure 207E.4-2C)";
  foreach(var e in ParseElements(elements)){
   /* a roof element gets the roof zones, a wall element the wall zones (by its name); anything else both */
   string n=e.Name.ToLowerInvariant();bool wallOnly=System.Text.RegularExpressions.Regex.IsMatch(n,"wall|girt|siding|column"),roofOnly=!wallOnly&&System.Text.RegularExpressions.Regex.IsMatch(n,"roof|purlin|rafter|truss|ridge|gutter|eave");
   if(!roofOnly)foreach(int zone in new[]{4,5}){double gp=WallPos(e.Area,s.Angle),gn=WallNeg(e.Area,zone,s.Angle);s.Rows.Add(Row(s,e,"Wall zone "+zone+(zone==5?" (corners)":" (interior)"),gp,gn));}
   if(!wallOnly)foreach(int zone in new[]{1,2,3}){double gp=RoofPos(e.Area,s.Angle),gn=RoofNeg(e.Area,zone,s.Angle);s.Rows.Add(Row(s,e,"Roof zone "+zone+(zone==1?" (interior)":zone==2?" (edges)":" (corners)"),gp,gn));}}
  s.Note="Wall GCp × 0.9 applies (roof slope ≤ 10°).";if(s.Angle>10)s.Note="";return s;}
 static CcResult Row(CcSummary s,CcElement e,string zone,double gp,double gn){double pp=s.Qh*(gp+s.GCpi),pn=s.Qh*(gn-s.GCpi);pp=Math.Max(pp,MinPressure);pn=Math.Min(pn,-MinPressure);return new CcResult{Element=e.Name,Zone=zone,Area=e.Area,GCpPos=gp,GCpNeg=gn,PPos=pp,PNeg=pn};}
 static string F(double v,string f="0.00"){return v.ToString(f,Ci);}
 /* report pages */
 internal static void AddPages(CalculationDocument doc,WindBuildingInput b,CcSummary s){
  doc.Sheets.Add(g=>{
   CalculationDocument.Section(g,"COMPONENTS AND CLADDING (SECTION 207E, PART 1)",130);
   CalculationDocument.Text(g,"Pressures for the roof and wall sheets, fasteners, purlins, girts and their connections: p = qh [(GCp) − (GCpi)], qh at the mean roof height with the exposure giving the highest load of any wind direction. GCp by zone and by the element's effective wind area A (span × the larger of the tributary width and span / 3). These values are for components and cladding only; the main frame uses the MWFRS pressures.",CalculationDocument.L,168,CalculationDocument.CW,60,8);
   CalculationDocument.Table(g,new[]{"Quantity","Value"},new List<string[]>{
    new[]{"Velocity pressure qh (governing: wind from "+s.Direction+", exposure "+s.Exposure+")",F(s.Qh,"0.000")+" kPa"},
    new[]{"Mean roof height h / roof slope θ",F(s.H)+" m / "+F(s.Angle,"0.0")+"° - "+s.RoofRange},
    new[]{"Least horizontal dimension",F(s.Least)+" m"},
    new[]{"Edge / corner width a = min(0.1 × "+F(s.Least)+", 0.4 × "+F(s.H)+") ≥ max(0.04 × "+F(s.Least)+", 0.9)",F(s.A,"0.000")+" m"},
    new[]{"Internal pressure coefficient GCpi",(b.Enclosure=="Enclosed"?"±0.18 (enclosed)":"±0.55 (partially enclosed)")},
    new[]{"Minimum net design pressure","±0.77 kPa"}},236,new float[]{340,147},26);
   Zones(g,new RectangleF(CalculationDocument.L,430,CalculationDocument.CW,250),b,s);
   CalculationDocument.Text(g,"Figure - C&C zones: roof 1 interior, 2 edges (width a)"+(b.Angle>7?" and along the ridge (a each side)":"")+", 3 corners (a × a)"+(b.Angle>7?" and ridge ends":"")+"; walls 4 interior, 5 corners (width a from each corner). "+s.Note,CalculationDocument.L,686,CalculationDocument.CW,30,7,false,PowTheme.Muted);});
  var rows=s.Rows;for(int start=0;start<rows.Count;start+=22){var part=rows.Skip(start).Take(22).ToList();doc.Sheets.Add(g=>{
   CalculationDocument.Section(g,"C&C DESIGN PRESSURES BY ELEMENT AND ZONE",130);
   CalculationDocument.Table(g,new[]{"Element","A (m²)","Zone","GCp +","GCp −","p + (kPa)","p − (kPa)"},part.Select(r=>new[]{r.Element,F(r.Area),r.Zone,F(r.GCpPos),F(r.GCpNeg),"+"+F(r.PPos),F(r.PNeg)}).ToList(),170,new float[]{120,48,108,44,44,62,61},22);
   CalculationDocument.Text(g,"p + = qh (GCp+ + |GCpi|) pushes on the surface; p − = qh (GCp− − |GCpi|) is suction (outward / uplift). Example, first row: p − = "+F(s.Qh,"0.000")+" × ("+F(part[0].GCpNeg)+" − "+F(s.GCpi)+") = "+F(s.Qh*(part[0].GCpNeg-s.GCpi))+" kPa"+(Math.Abs(s.Qh*(part[0].GCpNeg-s.GCpi))<MinPressure?" → minimum −0.77 kPa":"")+". Design each element for both signs in its zone; the corner and edge zones govern near the roof edges and wall corners.",CalculationDocument.L,170+22*(part.Count+1)+10,CalculationDocument.CW,60,8,false,PowTheme.Muted);});}}
 /* roof plan with zones 1-3 and the wall zones 4 / 5 along the outline */
 static void Zones(Graphics g,RectangleF rc,WindBuildingInput b,CcSummary s){
  using(var f=new Font("Segoe UI",7.5f,FontStyle.Bold,GraphicsUnit.Point))using(var ink=new SolidBrush(PowTheme.Ink)){
   double L=b.Length,W=b.Width;float k=(float)Math.Min((rc.Width-80)/L,(rc.Height-50)/W);float w=(float)(L*k),h=(float)(W*k),x0=rc.Left+(rc.Width-w)/2,y0=rc.Top+(rc.Height-h)/2;float a=(float)(s.A*k);
   using(var z1=new SolidBrush(Color.FromArgb(225,236,246)))g.FillRectangle(z1,x0,y0,w,h);
   using(var z2=new SolidBrush(Color.FromArgb(170,205,236))){g.FillRectangle(z2,x0,y0,w,a);g.FillRectangle(z2,x0,y0+h-a,w,a);g.FillRectangle(z2,x0,y0,a,h);g.FillRectangle(z2,x0+w-a,y0,a,h);}
   using(var z3=new SolidBrush(Color.FromArgb(80,140,205))){g.FillRectangle(z3,x0,y0,a,a);g.FillRectangle(z3,x0+w-a,y0,a,a);g.FillRectangle(z3,x0,y0+h-a,a,a);g.FillRectangle(z3,x0+w-a,y0+h-a,a,a);}
   /* gable roofs steeper than 7°: zone 2 also along the ridge (a each side) and zone 3 at both ridge ends */
   if(b.Angle>7){using(var z2=new SolidBrush(Color.FromArgb(170,205,236)))g.FillRectangle(z2,x0,y0+h/2-a,w,2*a);using(var z3=new SolidBrush(Color.FromArgb(80,140,205))){g.FillRectangle(z3,x0,y0+h/2-a,a,2*a);g.FillRectangle(z3,x0+w-a,y0+h/2-a,a,2*a);}}
   using(var edge=new Pen(PowTheme.Ink,1.2f))g.DrawRectangle(edge,x0,y0,w,h);
   if(b.Angle>7)using(var ridge=new Pen(Color.FromArgb(160,40,40),1f){DashStyle=DashStyle.Dash})g.DrawLine(ridge,x0,y0+h/2,x0+w,y0+h/2);
   Action<string,float,float> label=(t,cx,cy)=>{var sz=g.MeasureString(t,f);g.DrawString(t,f,ink,cx-sz.Width/2,cy-sz.Height/2);};
   label("1",x0+w/2,y0+(b.Angle>7?h/4+a/2:h/2));if(b.Angle>7){label("2",x0+w/2,y0+h/2-a/2);label("3",x0+a/2,y0+h/2+a/2);}label("2",x0+w/2,y0+a/2);label("2",x0+a/2,y0+(b.Angle>7?h/4+a/2:h/2));label("3",x0+a/2,y0+a/2);label("3",x0+w-a/2,y0+h-a/2);
   /* wall zones along the outline */
   using(var z5=new Pen(Color.FromArgb(190,40,40),4f))using(var z4=new Pen(Color.FromArgb(240,170,60),4f)){float o=6;
    g.DrawLine(z4,x0+a,y0-o,x0+w-a,y0-o);g.DrawLine(z5,x0,y0-o,x0+a,y0-o);g.DrawLine(z5,x0+w-a,y0-o,x0+w,y0-o);
    g.DrawLine(z4,x0-o,y0+a,x0-o,y0+h-a);g.DrawLine(z5,x0-o,y0,x0-o,y0+a);g.DrawLine(z5,x0-o,y0+h-a,x0-o,y0+h);}
   label("wall 4",x0+w/2,y0-18);label("5",x0+a/2,y0-18);label("5",x0+w-a/2,y0-18);
   using(var mf=new Font("Segoe UI",7f,FontStyle.Regular,GraphicsUnit.Point)){g.DrawString("a = "+F(s.A,"0.00")+" m   ·   "+F(L)+" m × "+F(W)+" m"+(b.Angle>7?"   ·   dashed = ridge":""),mf,ink,x0,y0+h+8);}}}
}
}
