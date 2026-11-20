using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Drawing2D;using System.Globalization;using System.Linq;
namespace BeamScheduleFormatter {
/* Bar bending schedule per element in the STAAD RCDC layout (owner, 20 November 2026: sample RCDC BBS DXFs for beams, columns,
   footings and slabs - "ganito ang gusto ko makita sa report, pero dagdagan mo if ano pa kailangan"): for every element a sketch,
   the table ELEMENT | BAR MARK | BAR NOS. | REBAR | BAR SHAPE | CUTTING LENGTH | DIMENSIONS A-F | R and a summary of length (m) and
   weight (kg) per bar size with the steel grade. Added beyond RCDC: member type, each-member and total counts, bend radius per
   NSCP 2015 Table 425.3.1, weight per element, steel ratio per concrete volume where known and a project summary by element. */
static partial class BbsReport {
 static readonly string[] ElementOrder={"Beam","Column","Footing","Wall footing","Slab"};
 /* inside bend radius: NSCP 2015 Table 425.3.1 (standard hooks 6db up to 25 mm, 8db for 28-36 mm, 10db above); Table 425.3.2
    (stirrups and ties 16 mm and smaller 4db); straight bars have none */
 internal static double BendRadius(CutPiece p){if(p.ShapeCode=="STRAIGHT"||p.ShapeCode=="CRANK")return 0;int d=p.Diameter;bool tie=p.ShapeCode=="HOOP"||p.ShapeCode=="CROSSTIE";
  double dia=tie&&d<=16?4*d:d<=25?6*d:d<=36?8*d:10*d;return dia/2;}
 static string ElementKey(CutPiece p){return string.IsNullOrEmpty(p.Line)?p.Member:p.Line;}
 static string Fy(CutSettings s,IEnumerable<int> dias){var g=dias.Select(d=>d<=12?s.Config.FySmall:s.Config.FyLarge).Distinct().OrderBy(x=>x).Select(x=>x.ToString("0",Inv));return "Fy"+string.Join("/",g);}
 internal static void AddElementSchedules(RichReport rep,CutResult r,CutSettings s,string number){
  rep.Heading(number+"  BAR BENDING SCHEDULE PER ELEMENT");
  rep.Paragraph("Each element: sketch, schedule and summary. Bar nos. = bars in one element x identical elements. SHAPE CODE refers to the bar shape reference in section 2 (shape key); A, B, C are the outside dimensions (mm) marked there. R = inside bend radius per NSCP 2015 Table 425.3.1 / 425.3.2. LGT = total length, WT = weight at the nominal unit mass.",true);
  int n=0;foreach(var group in ElementOrder){foreach(var el in r.Pieces.Where(p=>p.Group==group).GroupBy(ElementKey).OrderBy(g=>g.Key,StringComparer.OrdinalIgnoreCase)){var pieces=el.ToList();n++;
    string size=pieces.Where(p=>p.SecB>0&&p.SecH>0).Select(p=>(p.SecB).ToString("0",Inv)+"x"+(p.SecH).ToString("0",Inv)).FirstOrDefault();var geo=r.Geometry.FirstOrDefault(g=>g.Line==el.Key);
    if(size==null&&geo!=null&&geo.Width.Length>0)size=(geo.Width[0]*1000).ToString("0",Inv)+"x"+(geo.Depth[0]*1000).ToString("0",Inv);string title=el.Key+(size!=null?" : "+size:"");
    rep.Paragraph(number+"."+n+"  "+group.ToUpperInvariant()+"  "+title+"   ("+Fy(s,pieces.Select(p=>p.Diameter))+")",false,true);
    if(group=="Beam"&&geo!=null){var strips=BeamDetailPaper.Strips(geo);var g0=geo;foreach(var st0 in strips){var st=st0;rep.Figure(230,(g,rect)=>BeamDetailPaper.DrawElevation(g,rect,r,g0,st[0],st[1]),"Elevation "+el.Key+(strips.Count>1?" (strip "+(strips.IndexOf(st)+1)+" of "+strips.Count+")":"")+": bar marks as in the schedule below.");}}
    else if(group=="Column")rep.Figure(170,(g,rect)=>DrawColumnSketch(g,rect,pieces),"Section of "+el.Key+": main bars and hoops (not to scale).");
    else if(group=="Footing"||group=="Wall footing")rep.Figure(190,(g,rect)=>DrawFootingSketch(g,rect,pieces,s.Cover),"Plan and section of "+el.Key+": bars both ways (not to scale).");
    var rows=pieces.OrderBy(p=>p.Top?0:1).ThenBy(p=>p.BarMark,StringComparer.OrdinalIgnoreCase).ToList();bool first=true;
    /* owner (20 November 2026, "reference bar shape na lang na iisa, doon na siya ireference, dimension na lang sa table"): the bar
       shapes are drawn once in the shape key (section 2) with their letters; the table gives the shape code and A, B, C */
    rep.Table(new[]{"ELEMENT","BAR MARK","BAR NOS.","REBAR","SHAPE CODE","CUTTING LENGTH (mm)","A","B","C","D","R"},
     rows.Select(p=>{var row=new[]{first?el.Key:"",p.BarMark+(p.Manual?" *":p.Edited?" †":""),p.Pieces.ToString(Inv)+(p.Lines>1?"  ("+p.PerLine+" x "+p.Lines+")":""),"Ø"+p.Diameter,Code(p.ShapeCode),(p.Length*1000).ToString("0",Inv),Dim(p.ShapeCode=="STRAIGHT"&&p.DimA<=0?p.Length*1000:p.DimA),Dim(p.DimB),Dim(p.DimC),Dim(p.DimD),Dim(BendRadius(p))};first=false;return row;}),
     new float[]{10,11,12,7,8,11,8,8,8,8,7});
    var dias=pieces.Select(p=>p.Diameter).Distinct().OrderBy(d=>d).ToList();var lgt=dias.Select(d=>pieces.Where(p=>p.Diameter==d).Sum(p=>p.Pieces*p.Length)).ToList();var wt=dias.Select(d=>pieces.Where(p=>p.Diameter==d).Sum(p=>p.Kg)).ToList();
    var summary=new List<string[]>{new[]{"LGT (m)"}.Concat(lgt.Select(v=>v.ToString("0",Inv))).Concat(new[]{lgt.Sum().ToString("0",Inv)}).ToArray(),new[]{"WT (kg)"}.Concat(wt.Select(v=>v.ToString("0",Inv))).Concat(new[]{wt.Sum().ToString("0",Inv)}).ToArray()};
    double concrete=group=="Beam"?r.Spans.Where(x=>x.Line==el.Key).Sum(x=>x.Concrete):0;if(concrete>0)summary.Add(new[]{"kg / m³ concrete"}.Concat(dias.Select(d=>"")).Concat(new[]{(wt.Sum()/concrete).ToString("0",Inv)}).ToArray());
    rep.Table(new[]{"SUMMARY : "+el.Key+" : "+Fy(s,dias)}.Concat(dias.Select(d=>"Ø"+d)).Concat(new[]{"TOTAL"}).ToArray(),summary,new float[]{30}.Concat(dias.Select(d=>10f)).Concat(new float[]{12}).ToArray());}}
  /* project summary by element type and bar size */
  var all=r.Pieces.Select(p=>p.Diameter).Distinct().OrderBy(d=>d).ToList();var table=new List<string[]>();
  foreach(var group in ElementOrder){var ps=r.Pieces.Where(p=>p.Group==group).ToList();if(ps.Count==0)continue;table.Add(new[]{group+" ("+ps.Select(ElementKey).Distinct().Count()+" element(s))"}.Concat(all.Select(d=>{double v=ps.Where(p=>p.Diameter==d).Sum(p=>p.Kg);return v>0?v.ToString("0",Inv):"";})).Concat(new[]{ps.Sum(p=>p.Kg).ToString("0",Inv)}).ToArray());}
  table.Add(new[]{"TOTAL WT (kg)"}.Concat(all.Select(d=>r.Pieces.Where(p=>p.Diameter==d).Sum(p=>p.Kg).ToString("0",Inv))).Concat(new[]{r.Pieces.Sum(p=>p.Kg).ToString("0",Inv)}).ToArray());
  table.Add(new[]{"TOTAL LGT (m)"}.Concat(all.Select(d=>r.Pieces.Where(p=>p.Diameter==d).Sum(p=>p.Pieces*p.Length).ToString("0",Inv))).Concat(new[]{r.Pieces.Sum(p=>p.Pieces*p.Length).ToString("0",Inv)}).ToArray());
  table.Add(new[]{"No. of pieces"}.Concat(all.Select(d=>r.Pieces.Where(p=>p.Diameter==d).Sum(p=>p.Pieces).ToString(Inv))).Concat(new[]{r.Pieces.Sum(p=>p.Pieces).ToString(Inv)}).ToArray());
  rep.Paragraph(number+"."+(n+1)+"  PROJECT SUMMARY BY ELEMENT TYPE AND BAR SIZE",false,true).Table(new[]{"ELEMENT TYPE"}.Concat(all.Select(d=>"Ø"+d)).Concat(new[]{"TOTAL"}).ToArray(),table,new float[]{30}.Concat(all.Select(d=>10f)).Concat(new float[]{12}).ToArray(),row=>row[0].StartsWith("TOTAL")||row[0]=="No. of pieces"?(Color?)Color.FromArgb(232,236,240):null);}
 /* bar sketch with the length of each leg written on it (long straight legs drawn shortened, the values stay true) */
 internal static void DrawLabelled(Graphics g,RectangleF cell,CutPiece p){var q=new CutPiece{ShapeCode=p.ShapeCode,DimA=p.DimA,DimB=p.DimB,DimC=p.DimC,Diameter=p.Diameter};
  double hook=Math.Max(p.ShapeCode=="HOOK1"?p.DimB:p.ShapeCode=="HOOK2"?Math.Max(p.DimA,p.DimC):0,1);if(p.ShapeCode=="HOOK1"&&p.DimA>4*hook)q.DrawnStraight=4*hook;if(p.ShapeCode=="HOOK2"&&p.DimB>4*hook)q.DrawnStraight=4*hook;
  if(p.ShapeCode=="STRAIGHT"&&p.DimA<=0)q.DimA=p.Length*1000;
  using(var f=new Font("Segoe UI",7f,FontStyle.Regular,GraphicsUnit.Point))BarShapes.Draw(g,new RectangleF(cell.X+6,cell.Y+2,cell.Width-12,cell.Height-4),q,true,Color.FromArgb(25,90,160),f,true,1.4f);}
 /* column cross-section: main bars along the faces and the hoop */
 static void DrawColumnSketch(Graphics g,RectangleF rect,List<CutPiece> pieces){g.SmoothingMode=SmoothingMode.AntiAlias;var hoop=pieces.FirstOrDefault(p=>p.ShapeCode=="HOOP")??pieces.FirstOrDefault(p=>p.SecB>0);double b=hoop!=null&&hoop.SecB>0?hoop.SecB:400,h=hoop!=null&&hoop.SecH>0?hoop.SecH:400;
  var mains=pieces.Where(p=>p.ShapeCode=="STRAIGHT"||p.ShapeCode=="CRANK"||p.ShapeCode.StartsWith("HOOK")).Where(p=>p.Location==null||!p.Location.Contains("crosstie")).OrderByDescending(p=>p.Length).ToList();var main=mains.FirstOrDefault();int count=main==null?4:Math.Max(4,main.PerLine);
  float scale=(float)Math.Min((rect.Height-34)/h,(rect.Width*0.45f)/b);float w=(float)(b*scale),hh=(float)(h*scale),x=rect.Left+20,y=rect.Top+10;
  using(var ink=new Pen(Color.FromArgb(40,40,40),1.2f))using(var tie=new Pen(Color.FromArgb(25,90,160),1.4f))using(var bar=new SolidBrush(Color.FromArgb(200,40,40)))using(var f=new Font("Segoe UI",7.5f,GraphicsUnit.Point))using(var t=new SolidBrush(Color.FromArgb(40,40,40))){
   g.DrawRectangle(ink,x,y,w,hh);float c=Math.Max(6,40*scale);g.DrawRectangle(tie,x+c,y+c,w-2*c,hh-2*c);
   /* bars around the perimeter: corners first, the rest shared along the faces */
   int perSide=Math.Max(2,(int)Math.Ceiling(count/4.0)+1);var pts=new List<PointF>();float r0=Math.Max(2.2f,(float)((main==null?16:main.Diameter)*scale/2));float x0=x+c+r0,x1=x+w-c-r0,y0=y+c+r0,y1=y+hh-c-r0;
   for(int i=0;i<perSide;i++){float k=i/(float)(perSide-1);pts.Add(new PointF(x0+(x1-x0)*k,y0));pts.Add(new PointF(x0+(x1-x0)*k,y1));}for(int i=1;i<perSide-1;i++){float k=i/(float)(perSide-1);pts.Add(new PointF(x0,y0+(y1-y0)*k));pts.Add(new PointF(x1,y0+(y1-y0)*k));}
   foreach(var p in pts.Distinct().Take(count))g.FillEllipse(bar,p.X-r0,p.Y-r0,2*r0,2*r0);
   g.DrawString(b.ToString("0",Inv)+" mm",f,t,x+w/2-18,y+hh+4);float tx=x+w+24;float ty=y;
   foreach(var p in pieces.OrderBy(p=>p.ShapeCode=="HOOP"||p.ShapeCode=="CROSSTIE"?1:0)){g.DrawString(p.BarMark+":  "+p.PerLine+" - "+"Ø"+p.Diameter+"  "+(p.Location??p.Shape),f,t,tx,ty);ty+=13;if(ty>rect.Bottom-12)break;}}}
 /* footing plan and section with the bars both ways */
 static void DrawFootingSketch(Graphics g,RectangleF rect,List<CutPiece> pieces,double cover){g.SmoothingMode=SmoothingMode.AntiAlias;var bars=pieces.OrderByDescending(p=>p.Length).ToList();var a=bars.FirstOrDefault();var bb=bars.Skip(1).FirstOrDefault()??a;
  double L=(a==null?2:a.Length)*1000+2*cover,W=(bb==null?2:bb.Length)*1000+2*cover;float scale=(float)Math.Min((rect.Height-40)/W,(rect.Width*0.32f)/L);float w=(float)(L*scale),h=(float)(W*scale),x=rect.Left+20,y=rect.Top+12;
  using(var ink=new Pen(Color.FromArgb(40,40,40),1.2f))using(var dash=new Pen(Color.FromArgb(120,120,120),0.8f){DashStyle=DashStyle.Dash})using(var barPen=new Pen(Color.FromArgb(200,40,40),1.1f))using(var f=new Font("Segoe UI",7.5f,GraphicsUnit.Point))using(var t=new SolidBrush(Color.FromArgb(40,40,40))){
   g.DrawRectangle(ink,x,y,w,h);g.DrawRectangle(dash,x-6,y-6,w+12,h+12);float cw=w*0.22f,ch=h*0.22f;g.DrawRectangle(ink,x+w/2-cw/2,y+h/2-ch/2,cw,ch);
   float c=(float)(cover*scale);int nx=Math.Min(14,a==null?6:(int)Math.Max(3,a.PerLine)),ny=Math.Min(14,bb==null?6:(int)Math.Max(3,bb.PerLine));
   for(int i=0;i<ny;i++){float yy=y+c+(h-2*c)*i/(ny-1f);g.DrawLine(barPen,x+c,yy,x+w-c,yy);}for(int i=0;i<nx;i++){float xx=x+c+(w-2*c)*i/(nx-1f);g.DrawLine(barPen,xx,y+c,xx,y+h-c);}
   g.DrawString("PLAN  (bars "+((a==null?0:a.Length)*1000).ToString("0",Inv)+" x "+((bb==null?0:bb.Length)*1000).ToString("0",Inv)+" mm)",f,t,x,y+h+10);
   /* section */float sx=x+w+60,sy=y+h*0.45f,sw=w,th=Math.Max(16,h*0.18f);g.DrawRectangle(ink,sx,sy,sw,th);g.DrawRectangle(ink,sx+sw/2-cw/2,sy-h*0.4f,cw,h*0.4f);using(var fill=new SolidBrush(Color.FromArgb(60,60,60)))g.FillRectangle(fill,sx-8,sy+th,sw+16,5);
   g.DrawLine(barPen,sx+c,sy+th-c,sx+sw-c,sy+th-c);for(int i=0;i<nx;i++){float xx=sx+c+(sw-2*c)*i/(nx-1f);g.FillEllipse(Brushes.Firebrick,xx-1.6f,sy+th-c-5,3.2f,3.2f);}g.DrawString("SECTION",f,t,sx,sy+th+10);
   float tx=sx+sw+30,ty=y;foreach(var p in bars){g.DrawString(p.BarMark+":  "+"Ø"+p.Diameter+" x "+p.PerLine+"  "+(p.Location??p.Shape),f,t,tx,ty);ty+=13;if(ty>rect.Bottom-12)break;}}}
}
}
