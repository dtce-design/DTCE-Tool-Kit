using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Drawing2D;using System.Globalization;using System.Linq;
namespace BeamScheduleFormatter {
/* Wind load cases on the building for the report (owner, 24 November 2026: "dapat ipakita ito sa report. pero diba if wind galing sa NE,
   may windward load, leeward load, tapos sidewall pa"): the four principal wind directions W1-W4, each with +GCpi (a) and -GCpi (b);
   for each case a plan with every face loaded (windward pushed in, leeward and both side walls pulled out, the roof) and the table of
   every face with Cp, the external pressure q G Cp and the net pressures of (a) and (b). Values come from WindBuildingLoads. */
static class WindLoadCasesReport {
 static readonly CultureInfo Ci=CultureInfo.InvariantCulture;
 static string F(double v){return v.ToString("0.00",Ci);}
 /* the load case text without its internal-pressure sign: the (a) and (b) rows of a face belong together */
 static string Plain(string c){return (c??"").Replace("; GCpi +","").Replace("; GCpi −","").Replace("; GCpi -","").Trim();}
 static void Arrow(Graphics g,Color color,float width,PointF from,PointF to){using(var pen=new Pen(color,width))g.DrawLine(pen,from,to);double a=Math.Atan2(to.Y-from.Y,to.X-from.X);float h=width*3.2f+3;var p1=new PointF(to.X-(float)(Math.Cos(a-.45)*h),to.Y-(float)(Math.Sin(a-.45)*h));var p2=new PointF(to.X-(float)(Math.Cos(a+.45)*h),to.Y-(float)(Math.Sin(a+.45)*h));using(var br=new SolidBrush(color))g.FillPolygon(br,new[]{to,p1,p2});}
 static string Signed(double v){return (v>=0?"+":"")+v.ToString("0.00",Ci);}
 sealed class Face {public string Name,Cp,External,NetA,NetB;public double ExtMin,ExtMax;}
 sealed class Case {public string Id,From,Relation,Windward,Leeward,Side1,Side2;public int Index;public double Bearing;public bool Normal;public double Qi,GCpi;public List<Face> Faces=new List<Face>();public double Windward0,WindwardTop,LeewardExt,SideExt;public string RoofText;}
 static readonly string[] Dir={"N","NE","E","SE","S","SW","W","NW"};
 /* the building face each assessed terrain direction stands for (owner, 26 November 2026: "DIBA DAPAT ITO AY NAKA BASE NA SA DIRECTION NG
    BUILDING"): terrain direction index -> "W1, across the ridge: windward E long wall" */
 internal static Dictionary<int,string> FaceLabels(WindBuildingInput b,List<WindPressureRow> rows){var map=new Dictionary<int,string>();
  int n=0;Func<double,string> nm=WindBuildingLoads.BearingName;foreach(var group in rows.GroupBy(r=>r.Direction)){var first=group.First();bool normal=group.Key.EndsWith("normal");
   string t="W"+(++n).ToString(Ci)+", wind from "+nm(first.Bearing)+" "+(normal?"across the ridge":"along the ridge")+": windward "+nm(first.Bearing)+(normal?" long wall":" gable end");
   string old;map[first.TerrainDirection]=map.TryGetValue(first.TerrainDirection,out old)?old+"; "+t:t;}return map;}
 static List<Case> Cases(WindBuildingInput b,List<WindPressureRow> rows){var list=new List<Case>();int n=0;
  foreach(var group in rows.GroupBy(r=>r.Direction)){var first=group.First();int idx=first.TerrainDirection;bool normal=group.Key.EndsWith("normal");double brg=first.Bearing;Func<double,string> nm=WindBuildingLoads.BearingName;var c=new Case{Id="W"+(++n).ToString(Ci),Index=idx,Bearing=brg,Normal=normal,From=nm(brg),Relation=normal?"across the ridge":"along the ridge",
    Windward=nm(brg)+(normal?" long wall":" gable end"),Leeward=nm(brg+180)+(normal?" long wall":" gable end"),Side1=nm(brg+90)+(normal?" gable end":" long wall"),Side2=nm(brg+270)+(normal?" gable end":" long wall"),Qi=first.Qi,GCpi=Math.Abs(first.GCpi)};
   Func<WindPressureRow,double> ext=r=>r.Q*r.G*r.Cp;Func<WindPressureRow,bool> plus=r=>r.GCpi>0;
   var ww=group.Where(r=>r.Surface=="Windward wall").ToList();
   if(ww.Count>0){var a=ww.Where(plus).ToList();var bb=ww.Where(r=>!plus(r)).ToList();c.Windward0=ww.Min(ext);c.WindwardTop=ww.Max(ext);
    c.Faces.Add(new Face{Name="Windward wall ("+c.Windward+")",Cp="+0.80",External=Signed(c.Windward0)+" to "+Signed(c.WindwardTop),NetA=Signed(a.Min(r=>r.P))+" to "+Signed(a.Max(r=>r.P)),NetB=Signed(bb.Min(r=>r.P))+" to "+Signed(bb.Max(r=>r.P))});}
   foreach(var surface in group.Where(r=>r.Surface!="Windward wall").GroupBy(r=>r.Surface+"|"+Plain(r.Case))){
    var rowsA=surface.Where(plus).ToList();var rowsB=surface.Where(r=>!plus(r)).ToList();if(rowsA.Count==0||rowsB.Count==0)continue;var r0=rowsA[0];string name=r0.Surface;
    if(name=="Leeward wall"){name="Leeward wall ("+c.Leeward+")";c.LeewardExt=ext(r0);}else if(name=="Side walls"){name="Side walls ("+c.Side1+", "+c.Side2+")";c.SideExt=ext(r0);}else name=r0.Surface+" ("+Plain(r0.Case).Replace(" from windward edge","").Replace("Cp branch","Cp")+")";
    c.Faces.Add(new Face{Name=name,Cp=Signed(r0.Cp),External=Signed(ext(r0)),NetA=Signed(rowsA[0].P),NetB=Signed(rowsB[0].P)});}
   var roof=group.Where(r=>r.Surface.Contains("roof")||r.Surface.StartsWith("Roof")).Select(ext).ToList();c.RoofText=roof.Count==0?"":"Roof q·G·Cp: "+Signed(roof.Min())+(roof.Max()>roof.Min()+1e-9?" to "+Signed(roof.Max()):"")+" kPa";
   list.Add(c);}
  return list;}
 internal static void AddPages(CalculationDocument doc,WindBuildingInput b,List<WindPressureRow> rows,string ridge){var cases=Cases(b,rows);if(cases.Count==0)return;
  doc.Sheets.Add(g=>{
   CalculationDocument.Section(g,"MWFRS WIND LOAD CASES ON THE BUILDING",130);
   CalculationDocument.Text(g,"Ridge "+ridge+". Each wind direction is one load case in which every face is loaded at the same time: the windward wall is pushed in (pressure growing with height), the leeward wall and both side walls are pulled out (uniform, at the mean roof height), and the roof takes its Cp; the internal pressure ±GCpi acts on all inside faces together, so each case is used twice: (a) with +GCpi and (b) with −GCpi. p = q G Cp − qi (GCpi); positive toward the surface, negative away (suction). Use all eight cases (W1a … W4b) in every load combination with wind.",CalculationDocument.L,170,CalculationDocument.CW,84,8);
   var table=new List<string[]>();foreach(var c in cases){table.Add(new[]{c.Id+"a / "+c.Id+"b\nSTAAD TYPE "+c.Id.Substring(1),"from "+c.From,c.Relation,c.Windward,c.Leeward,c.Side1+", "+c.Side2});}
   CalculationDocument.Table(g,new[]{"Load case","Wind","Direction","Windward face","Leeward face","Side walls"},table,262,new float[]{66,52,74,96,96,103},34);
   CalculationDocument.Text(g,"(a) = +GCpi (internal pressure, pushes out on every inside face: governs side-wall and roof suction). (b) = −GCpi (internal suction: governs the windward wall). GCpi = ±"+cases[0].GCpi.ToString("0.00",Ci)+" × qi (qi = "+F(cases[0].Qi)+" kPa for "+cases[0].From+").",CalculationDocument.L,262+34*(table.Count+1)+10,CalculationDocument.CW,40,8,false,PowTheme.Muted);
  });
  foreach(var c0 in cases){var c=c0;doc.Sheets.Add(g=>{
   CalculationDocument.Section(g,"LOAD CASE "+c.Id+" - WIND FROM "+c.From+" ("+c.Relation.ToUpperInvariant()+")",130);
   Plan(g,new RectangleF(CalculationDocument.L,166,CalculationDocument.CW,236),b,c);
   var t=c.Faces.Select(f=>new[]{f.Name,f.Cp,f.External,f.NetA,f.NetB}).ToList();
   CalculationDocument.Table(g,new[]{"Face","Cp","External q·G·Cp (kPa)","Net "+c.Id+"a, +GCpi (kPa)","Net "+c.Id+"b, −GCpi (kPa)"},t,412,new float[]{175,44,96,86,86},22);
   CalculationDocument.Text(g,"Windward wall values run from the ground to the "+(c.Normal?"eave":"apex")+" (qz by height). Leeward wall, side walls and roof use qh at the mean roof height "+F(b.Height)+" m. Internal pressure qi·GCpi = ±"+F(c.Qi*c.GCpi)+" kPa. For STAAD member loads multiply the net pressure by the tributary width of each column / girt / rafter; roof pressures act normal to the roof surface. Main-frame (MWFRS) values; cladding, purlins and girts need the components and cladding pressures.",CalculationDocument.L,412+22*(t.Count+1)+10,CalculationDocument.CW,70,8,false,PowTheme.Muted);
  });}}
 /* plan view, north up: the building turned to the ridge, the wind arrow, and an arrow with the external pressure on each face */
 static void Plan(Graphics g,RectangleF rc,WindBuildingInput b,Case c){
  using(var f=new Font("Segoe UI",7.5f,FontStyle.Regular,GraphicsUnit.Point))using(var bf=new Font("Segoe UI",7.5f,FontStyle.Bold,GraphicsUnit.Point))using(var ink=new SolidBrush(PowTheme.Ink)){
   var cen=new PointF(rc.Left+rc.Width/2,rc.Top+rc.Height/2+6);double th=b.Ridge*Math.PI/180;double k=Math.Min(rc.Width*.36,rc.Height*.52)/Math.Max(b.Width,b.Length);
   Func<double,double,PointF> P=(x,y)=>new PointF(cen.X+(float)((x*Math.Cos(th)+y*Math.Sin(th))*k),cen.Y-(float)((-x*Math.Sin(th)+y*Math.Cos(th))*k));
   double w=b.Width/2,l=b.Length/2;var poly=new[]{P(-w,-l),P(w,-l),P(w,l),P(-w,l)};
   using(var fill=new SolidBrush(Color.FromArgb(238,242,246)))g.FillPolygon(fill,poly);using(var edge=new Pen(PowTheme.Ink,1.6f))g.DrawPolygon(edge,poly);
   using(var ridgePen=new Pen(Color.FromArgb(150,40,40),1.2f){DashStyle=DashStyle.Dash})g.DrawLine(ridgePen,P(0,-l),P(0,l));
   /* faces: outward normal bearing of each face (local x = across, y = along the ridge) */
   var faces=new[]{new{x=w,y=0.0,nx=1.0,ny=0.0,half=l},new{x=-w,y=0.0,nx=-1.0,ny=0.0,half=l},new{x=0.0,y=l,nx=0.0,ny=1.0,half=w},new{x=0.0,y=-l,nx=0.0,ny=-1.0,half=w}};
   double windBearing=c.Bearing;
   foreach(var face in faces){double e=face.nx*Math.Cos(th)+face.ny*Math.Sin(th),nn=-face.nx*Math.Sin(th)+face.ny*Math.Cos(th);double brg=(Math.Atan2(e,nn)*180/Math.PI+360)%360;double diff=Math.Abs(((brg-windBearing)%360+540)%360-180);
    string role;double value;bool inward;Color color;
    if(diff<45){role="Windward";value=c.WindwardTop;inward=true;color=Color.FromArgb(190,40,40);}else if(diff>135){role="Leeward";value=c.LeewardExt;inward=false;color=Color.FromArgb(40,90,170);}else{role="Side wall";value=c.SideExt;inward=false;color=Color.FromArgb(40,90,170);}
    var mid=P(face.x,face.y);var dir=new PointF((float)e,(float)-nn);float len=34;var outer=new PointF(mid.X+dir.X*(len+6),mid.Y+dir.Y*(len+6));var near=new PointF(mid.X+dir.X*4,mid.Y+dir.Y*4);
    if(inward)Arrow(g,color,1.8f,outer,near);else Arrow(g,color,1.8f,near,outer);
    string label=role+(role=="Windward"?" up to ":" ")+Signed(value)+" kPa";var sz=g.MeasureString(label,f);var lp=new PointF(mid.X+dir.X*(len+14),mid.Y+dir.Y*(len+14));float lx=lp.X-(dir.X<-.3f?sz.Width:dir.X>.3f?0:sz.Width/2),ly=lp.Y-(dir.Y<-.3f?sz.Height:dir.Y>.3f?0:sz.Height/2);
    lx=Math.Max(rc.Left,Math.Min(rc.Right-sz.Width,lx));using(var bg=new SolidBrush(Color.FromArgb(220,255,255,255)))g.FillRectangle(bg,lx,ly,sz.Width,sz.Height);g.DrawString(label,f,ink,lx,ly);}
   /* wind indicator in the top-left corner: the arrow points the way the wind blows */
   {double a=windBearing*Math.PI/180;var hub=new PointF(rc.Left+52,rc.Top+62);float r=34;var from=new PointF(hub.X+(float)(Math.Sin(a)*r),hub.Y-(float)(Math.Cos(a)*r));var to=new PointF(hub.X-(float)(Math.Sin(a)*r),hub.Y+(float)(Math.Cos(a)*r));
    using(var ring=new Pen(Color.FromArgb(200,205,210),.8f))g.DrawEllipse(ring,hub.X-r-4,hub.Y-r-4,2*r+8,2*r+8);Arrow(g,Color.FromArgb(30,110,80),3f,from,to);var wl="WIND from "+c.From;var ws=g.MeasureString(wl,bf);g.DrawString(wl,bf,ink,hub.X-ws.Width/2,hub.Y+r+8);}
   if(c.RoofText!=""){var rs=g.MeasureString(c.RoofText,f);using(var bg=new SolidBrush(Color.FromArgb(225,255,255,255)))g.FillRectangle(bg,cen.X-rs.Width/2,cen.Y+6,rs.Width,rs.Height);g.DrawString(c.RoofText,f,ink,cen.X-rs.Width/2,cen.Y+6);}
   g.DrawString("N ↑",bf,ink,rc.Left+46,rc.Top);g.DrawString("Arrows: red = pushes on the face, blue = pulls away (suction); external q·G·Cp shown, internal ±qi·GCpi added in the table.",f,ink,rc.Left,rc.Bottom-12);}}
}
}
