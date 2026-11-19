using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Drawing2D;using System.Globalization;using System.Linq;
namespace BeamScheduleFormatter {
/* Beam elevation and sections for the printed bar bending schedule (owner, 19 November 2026: "Hindi mo sinama yung section ng
   beam at elevation sa report? Para ma visualize clearly? Gamit ng A3 para ok"). Drawn in points on A3: the elevation of a beam
   line with supports, every bar on its own lane with its mark, hooks, laps, allowed splice zones, hoop zones and spacing, cut-off
   and span dimensions; then the sections at the left face, midspan and right face of every span at a standard scale. */
static class BeamDetailPaper {
 static readonly CultureInfo Inv=CultureInfo.InvariantCulture;
 const float SectionBoxW=118,SectionBoxH=208,ElevationH=300;
 static bool Longitudinal(CutPiece p){return p.ShapeCode!="HOOP"&&p.ShapeCode!="CROSSTIE"&&!p.Location.StartsWith("Side-face");}
 static Color BarColor(CutPiece p){if(p.Shape=="REVIEW")return Color.Firebrick;if(p.Location.Contains("continuous"))return p.Top?Color.FromArgb(25,90,160):Color.FromArgb(20,120,90);return p.Top?Color.FromArgb(95,135,200):Color.FromArgb(70,160,115);}
 static string F0(double v){return v.ToString("0",Inv);}
 /* section cuts of a line: left face, midspan, right face of every span (metres along the line) */
 internal static List<KeyValuePair<string,double>> Cuts(CutLineGeometry geo){var list=new List<KeyValuePair<string,double>>();for(int j=0;j<geo.Ln.Length;j++){double d=geo.Depth[j],a=geo.FaceLeft[j],b=geo.FaceRight[j];double off=Math.Min(Math.Max(.15,d/2),(b-a)/4);
   list.Add(new KeyValuePair<string,double>((j+1)+"A",a+off));list.Add(new KeyValuePair<string,double>((j+1)+"B",(a+b)/2));list.Add(new KeyValuePair<string,double>((j+1)+"C",b-off));}return list;}
 /* height of the figure for a line at the given width */
 internal static float Height(CutLineGeometry geo,float width){int perRow=Math.Max(1,(int)(width/SectionBoxW));int rows=(Cuts(geo).Count+perRow-1)/perRow;return ElevationH+rows*SectionBoxH+10;}
 internal static int SectionRows(CutLineGeometry geo,float width){int perRow=Math.Max(1,(int)(width/SectionBoxW));return (Cuts(geo).Count+perRow-1)/perRow;}
 internal static float SectionsHeight(int rows){return rows*SectionBoxH+10;}
 static Font Small(){return new Font("Segoe UI",7f,FontStyle.Regular,GraphicsUnit.Point);}
 static Font Bold(){return new Font("Segoe UI",7.5f,FontStyle.Bold,GraphicsUnit.Point);}
 static void Center(Graphics g,string t,Font f,Brush b,float cx,float y){var sz=g.MeasureString(t,f);g.DrawString(t,f,b,cx-sz.Width/2,y);}
 static void DimH(Graphics g,Pen pen,Font f,Brush b,float a,float c,float y,string text){g.DrawLine(pen,a,y,c,y);g.DrawLine(pen,a,y-3,a,y+3);g.DrawLine(pen,c,y-3,c,y+3);var sz=g.MeasureString(text,f);if(c-a>sz.Width+2)g.DrawString(text,f,b,(a+c)/2-sz.Width/2,y-sz.Height);else g.DrawString(text,f,b,(a+c)/2-sz.Width/2,y+1);}
 /* ---------------- elevation ---------------- */
 public static void DrawElevation(Graphics g,RectangleF area,CutResult r,CutLineGeometry geo){var pieces=r.Pieces.Where(p=>p.Line==geo.Line).ToList();g.SmoothingMode=SmoothingMode.AntiAlias;
  using(var small=Small())using(var bold=Bold())using(var ink=new SolidBrush(PowTheme.Ink))using(var muted=new SolidBrush(PowTheme.Muted))using(var dim=new Pen(PowTheme.Muted,.6f)){
   g.DrawString("ELEVATION - BEAM LINE "+geo.Line+"  ("+string.Join(", ",geo.Marks.Distinct())+")",bold,ink,area.Left+6,area.Top+4);
   g.DrawString("Horizontal scale 1:"+F0(geo.Total*1000/((area.Width-60)/72*25.4))+" on A3; depth drawn larger for clarity. Bar marks per the schedule.",small,muted,area.Left+6,area.Top+16);
   /* lanes */
   var lane=new Dictionary<CutPiece,int>();int[] lanes=new int[2];foreach(bool top in new[]{true,false}){var ends=new List<double>();foreach(var p in pieces.Where(x=>Longitudinal(x)&&x.Top==top).OrderBy(x=>x.Location.Contains("continuous")?0:1).ThenBy(x=>x.Start)){int k=ends.FindIndex(e=>e<p.Start-1e-6);if(k<0){ends.Add(p.End);k=ends.Count-1;}else ends[k]=p.End;lane[p]=k;}lanes[top?0:1]=ends.Count;}
   float x0=area.Left+30,width=area.Width-60;Func<double,float> X=x=>(float)(x0+x/geo.Total*width);
   float labelRowsTop=Math.Max(1,lanes[0])*10,beamTop=area.Top+44+labelRowsTop,beamH=Math.Max(64,Math.Min(96,(lanes[0]+lanes[1])*7+40)),beamBottom=beamTop+beamH;
   Func<CutPiece,float> laneY=p=>{int k;lane.TryGetValue(p,out k);float off=7+k*6;return p.Top?beamTop+off:beamBottom-off;};
   /* supports */
   using(var hatch=new HatchBrush(HatchStyle.BackwardDiagonal,Color.FromArgb(150,150,150),Color.FromArgb(238,238,238)))using(var edge=new Pen(Color.Gray,.8f))for(int i=0;i<geo.SupportStart.Length;i++){float a=X(geo.SupportStart[i]),b=X(geo.SupportStart[i]+geo.SupportWidth[i]);g.FillRectangle(hatch,a,beamTop-22,b-a,beamH+44);g.DrawRectangle(edge,a,beamTop-22,b-a,beamH+44);Center(g,"S"+(i+1),bold,ink,(a+b)/2,beamTop-34);}
   /* allowed splice zones */
   using(var zone=new SolidBrush(Color.FromArgb(45,60,170,90)))foreach(var z in geo.TopZones.Concat(geo.BottomZones).GroupBy(z=>z[0].ToString("0.000",Inv)+"|"+z[1].ToString("0.000",Inv)).Select(x=>x.First()))g.FillRectangle(zone,X(z[0]),beamTop,X(z[1])-X(z[0]),beamH);
   using(var outline=new Pen(PowTheme.Ink,1.1f))g.DrawRectangle(outline,X(0),beamTop,X(geo.Total)-X(0),beamH);
   for(int j=0;j<geo.Slab.Length;j++)if(geo.Slab[j]>0){float ts=(float)(geo.Slab[j]/geo.Depth[j]*beamH);using(var slab=new Pen(PowTheme.Muted,.6f){DashStyle=DashStyle.Dash})g.DrawLine(slab,X(geo.FaceLeft[j]),beamTop+ts,X(geo.FaceRight[j]),beamTop+ts);}
   /* hoops: ticks at their spacing; zone labels below the beam */
   float below=beamBottom+4+Math.Max(1,lanes[1])*10;
   using(var tie=new Pen(Color.FromArgb(150,150,150),.5f))for(int j=0;j<geo.Ties.Count&&j<geo.FaceLeft.Length;j++)foreach(var z in geo.Ties[j]){if(z.Count<=0||z.Spacing<=0)continue;for(int k=0;k<z.Count;k++){double x=z.Name=="left"?geo.FaceLeft[j]+Math.Max(z.First>0?z.First:.05,z.From)+k*z.Spacing:z.Name=="right"?geo.FaceLeft[j]+z.To-(z.First>0?z.First:.05)-k*z.Spacing:geo.FaceLeft[j]+z.From+(k+1)*z.Spacing;g.DrawLine(tie,X(x),beamTop+2,X(x),beamBottom-2);}
     float a=X(geo.FaceLeft[j]+z.From),b=X(geo.FaceLeft[j]+z.To);string t=z.Legs+"L-Ø"+z.Dia+" @ "+F0(z.Spacing*1000);var sz=g.MeasureString(t,small);DimH(g,dim,small,muted,a,b,below+sz.Height+2,b-a>sz.Width+2?t:"");if(b-a<=sz.Width+2)Center(g,t,small,muted,(a+b)/2,below+sz.Height+4);}
   /* extra hoops over laps (418.6.3.3) */
   using(var extra=new Pen(Color.FromArgb(200,110,20),.6f))foreach(var p in pieces.Where(x=>x.Location.StartsWith("Extra hoops over lap"))){for(int k=0;k<p.PerLine;k++){double x=p.Start+(p.End-p.Start)*(k+.5)/p.PerLine;g.DrawLine(extra,X(x),beamTop+2,X(x),beamBottom-2);}}
   /* bars with leaders to their labels */
   foreach(var p in pieces.Where(Longitudinal)){float y=laneY(p),a=X(p.Start),b=X(p.End);using(var pen=new Pen(BarColor(p),1.4f)){g.DrawLine(pen,a,y,b,y);if(p.ShapeCode=="HOOK1"||p.ShapeCode=="HOOK2"){float hook=p.Top?12:-12;if(p.Start<=geo.Cover+1e-6)g.DrawLine(pen,a,y,a,y+hook);if(p.End>=geo.Total-geo.Cover-1e-6)g.DrawLine(pen,b,y,b,y+hook);}}
    int k;lane.TryGetValue(p,out k);string t=p.BarMark+"  "+p.PerLine+"-Ø"+p.Diameter+"  L="+F0(p.Length*1000);var sz=g.MeasureString(t,small);float lx=Math.Max(area.Left+2,Math.Min(area.Right-sz.Width-2,(a+b)/2-sz.Width/2)),ly=p.Top?beamTop-12-k*10:beamBottom+2+k*10;
    using(var lead=new Pen(Color.FromArgb(140,BarColor(p)),.5f))g.DrawLine(lead,(a+b)/2,y,(a+b)/2,p.Top?ly+sz.Height-2:ly+2);using(var bg=new SolidBrush(Color.FromArgb(225,255,255,255)))g.FillRectangle(bg,lx,ly,sz.Width,sz.Height-2);using(var tb=new SolidBrush(BarColor(p)))g.DrawString(t,small,tb,lx,ly-1);}
   /* laps */
   using(var lapPen=new Pen(Color.FromArgb(230,140,20),.9f){DashStyle=DashStyle.Dash})foreach(var c in r.Splices.Where(c=>c.Line==geo.Line)){var piece=pieces.FirstOrDefault(p=>p.BarMark.StartsWith(c.Bar+".")&&Math.Abs(p.End-c.To)<1e-6);bool top=piece!=null&&piece.Top;int n=Math.Max(2,top?lanes[0]:lanes[1]);float y1=top?beamTop+2:beamBottom-2,y2=top?beamTop+7+n*6:beamBottom-7-n*6;float a=X(c.From),b=X(c.To);
    using(var fill=new SolidBrush(c.Ok?Color.FromArgb(60,240,170,40):Color.FromArgb(80,220,40,40)))g.FillRectangle(fill,a,Math.Min(y1,y2),b-a,Math.Abs(y2-y1));g.DrawRectangle(lapPen,a,Math.Min(y1,y2),b-a,Math.Abs(y2-y1));{string lt="LAP "+F0(c.Lap*1000)+(c.Ok?"":" NG");var lsz=g.MeasureString(lt,small);using(var bg=new SolidBrush(Color.FromArgb(235,255,255,255)))g.FillRectangle(bg,(a+b)/2-lsz.Width/2,(y1+y2)/2-5,lsz.Width,lsz.Height-2);Center(g,lt,small,c.Ok?Brushes.DarkOrange:Brushes.Firebrick,(a+b)/2,(y1+y2)/2-5);}}
   /* section cuts */
   using(var cut=new Pen(Color.FromArgb(200,40,40),.9f){DashStyle=DashStyle.DashDot})foreach(var c in Cuts(geo)){float px=X(c.Value);g.DrawLine(cut,px,beamTop-8,px,beamBottom+8);using(var b=new SolidBrush(Color.FromArgb(200,40,40)))g.DrawString(c.Key,small,b,px+1,beamBottom-10);}
   /* dimensions: supports and clear spans, 2h hoop zones, overall */
   float dy=below+30;for(int i=0;i<geo.SupportStart.Length;i++)DimH(g,dim,small,muted,X(geo.SupportStart[i]),X(geo.SupportStart[i]+geo.SupportWidth[i]),dy,F0(geo.SupportWidth[i]*1000));
   for(int j=0;j<geo.Ln.Length;j++){DimH(g,dim,small,muted,X(geo.FaceLeft[j]),X(geo.FaceRight[j]),dy,"Ln "+F0(geo.Ln[j]*1000)+"  ("+geo.Marks[j]+" "+F0(geo.Width[j]*1000)+"x"+F0(geo.Depth[j]*1000)+")");
    double h2=2*geo.Depth[j];if(geo.Ln[j]>2*h2){DimH(g,dim,small,muted,X(geo.FaceLeft[j]),X(geo.FaceLeft[j]+h2),dy+14,"2h "+F0(h2*1000));DimH(g,dim,small,muted,X(geo.FaceRight[j]-h2),X(geo.FaceRight[j]),dy+14,"2h "+F0(h2*1000));}}
   DimH(g,dim,small,muted,X(0),X(geo.Total),dy+30,"overall "+F0(geo.Total*1000));
   /* legend */
   float lx2=area.Left+8,ly2=area.Bottom-14;foreach(var item in new[]{new{T="Top continuous",C=Color.FromArgb(25,90,160)},new{T="Top extra",C=Color.FromArgb(95,135,200)},new{T="Bottom continuous",C=Color.FromArgb(20,120,90)},new{T="Bottom extra",C=Color.FromArgb(70,160,115)},new{T="Lap (Class B)",C=Color.FromArgb(230,140,20)},new{T="Extra hoops over lap",C=Color.FromArgb(200,110,20)},new{T="Allowed splice zone",C=Color.FromArgb(150,200,160)},new{T="Section cut",C=Color.FromArgb(200,40,40)}}){using(var b=new SolidBrush(item.C))g.FillRectangle(b,lx2,ly2+3,12,6);g.DrawString(item.T,small,muted,lx2+15,ly2);lx2+=g.MeasureString(item.T,small).Width+28;}}}
 /* ---------------- sections ---------------- */
 static readonly int[] Scales={5,10,15,20,25,30,40,50};
 public static void DrawSections(Graphics g,RectangleF area,CutResult r,CutLineGeometry geo,int firstRow,int rowCount){var cuts=Cuts(geo);int perRow=Math.Max(1,(int)(area.Width/SectionBoxW));float boxW=area.Width/perRow;
  double maxB=geo.Width.Max()*1000,maxH=geo.Depth.Max()*1000;double ptPerMm=72/25.4;int scale=Scales.FirstOrDefault(n=>maxB*ptPerMm/n<=boxW-34&&maxH*ptPerMm/n<=SectionBoxH-92);if(scale==0)scale=Scales.Last();float k=(float)(ptPerMm/scale);
  var pieces=r.Pieces.Where(p=>p.Line==geo.Line).ToList();
  for(int i=firstRow*perRow;i<Math.Min(cuts.Count,(firstRow+rowCount)*perRow);i++){int row=i/perRow-firstRow,col=i%perRow;var box=new RectangleF(area.Left+col*boxW,area.Top+row*SectionBoxH,boxW,SectionBoxH);DrawSection(g,box,geo,pieces,cuts[i].Key,cuts[i].Value,k,scale);}}
 static void DrawSection(Graphics g,RectangleF box,CutLineGeometry geo,List<CutPiece> pieces,string name,double x,float k,int scale){g.SmoothingMode=SmoothingMode.AntiAlias;
  int j=0;for(int q=0;q<geo.Ln.Length;q++)if(x>=geo.SupportStart[q])j=q;double b=geo.Width[j]*1000,h=geo.Depth[j]*1000,cover=geo.Cover*1000;
  var zone=j<geo.Ties.Count?geo.Ties[j].FirstOrDefault(z=>x-geo.FaceLeft[j]>=z.From-1e-9&&x-geo.FaceLeft[j]<=z.To+1e-9):null;int tieDia=zone!=null?zone.Dia:10;int legs=zone!=null?zone.Legs:2;
  using(var small=Small())using(var bold=Bold())using(var ink=new SolidBrush(PowTheme.Ink))using(var muted=new SolidBrush(PowTheme.Muted))using(var dim=new Pen(PowTheme.Muted,.6f)){
   string where=name.EndsWith("B")?"midspan":name.EndsWith("A")?"near left face":"near right face";Center(g,"SECTION "+name,bold,ink,box.Left+box.Width/2,box.Top+2);Center(g,"span "+(j+1)+" "+where+" - 1:"+scale,small,muted,box.Left+box.Width/2,box.Top+13);
   var here=pieces.Where(p=>Longitudinal(p)&&p.Start<=x+1e-9&&p.End>=x-1e-9).ToList();var topBars=here.Where(p=>p.Top).ToList();var botBars=here.Where(p=>!p.Top).ToList();
   Func<List<CutPiece>,string> label=list=>string.Join(" + ",list.GroupBy(p=>p.Diameter).OrderByDescending(q=>q.Key).Select(q=>q.Sum(p=>p.PerLine)+"-Ø"+q.Key));
   float w=(float)(b*k),hh=(float)(h*k),x0=box.Left+(box.Width-w)/2+4,y0=box.Top+40;
   Center(g,label(topBars),small,new SolidBrush(Color.FromArgb(25,90,160)),x0+w/2,y0-12);
   using(var concrete=new SolidBrush(Color.FromArgb(236,236,232)))g.FillRectangle(concrete,x0,y0,w,hh);using(var edge=new Pen(PowTheme.Ink,.9f))g.DrawRectangle(edge,x0,y0,w,hh);
   if(geo.Slab[j]>0){float ts=(float)(geo.Slab[j]*1000*k);using(var slab=new Pen(PowTheme.Muted,.5f){DashStyle=DashStyle.Dash}){g.DrawLine(slab,x0-12,y0+ts,x0,y0+ts);g.DrawLine(slab,x0+w,y0+ts,x0+w+12,y0+ts);}}
   float c=(float)((cover+tieDia/2.0)*k);float rad=(float)Math.Max(1.5,2*tieDia*k);using(var tie=new Pen(Color.FromArgb(90,90,90),(float)Math.Max(.6,tieDia*k))){var path=new GraphicsPath();float L=x0+c,T=y0+c,R=x0+w-c,B=y0+hh-c;path.AddArc(L,T,2*rad,2*rad,180,90);path.AddArc(R-2*rad,T,2*rad,2*rad,270,90);path.AddArc(R-2*rad,B-2*rad,2*rad,2*rad,0,90);path.AddArc(L,B-2*rad,2*rad,2*rad,90,90);path.CloseFigure();g.DrawPath(tie,path);path.Dispose();
    float hk=(float)Math.Max(4,75*k);g.DrawLine(tie,L+rad,T+rad*.3f,L+rad+hk*.7f,T+rad*.3f+hk*.7f);g.DrawLine(tie,L+rad*.3f,T+rad,L+rad*.3f+hk*.7f,T+rad+hk*.7f);
    for(int q=1;q<legs-1;q++){float lx=L+(R-L)*q/(legs-1);g.DrawLine(tie,lx,T,lx,B);}}
   foreach(bool top in new[]{true,false}){var bars=top?topBars:botBars;var spots=new List<CutPiece>();foreach(var p in bars.OrderByDescending(p=>p.Location.Contains("continuous")))for(int q=0;q<p.PerLine;q++)spots.Add(p);if(spots.Count==0)continue;
    int dmax=spots.Max(p=>p.Diameter);double inner=b-2*cover-2*tieDia;int perRow=Math.Max(2,Math.Min(spots.Count,(int)Math.Floor((inner-dmax)/(dmax+25))+1));
    for(int q=0;q<spots.Count;q++){int row=q/perRow,col=q%perRow;int inRow=Math.Min(perRow,spots.Count-row*perRow);var p=spots[q];double cxMm=inRow==1?b/2:cover+tieDia+dmax/2.0+(inner-dmax)*col/(inRow-1);double off=cover+tieDia+dmax/2.0+row*(dmax+25);float cx=x0+(float)(cxMm*k),cy=top?y0+(float)(off*k):y0+hh-(float)(off*k);float d=(float)Math.Max(2.4,p.Diameter*k);using(var bar=new SolidBrush(BarColor(p)))g.FillEllipse(bar,cx-d/2,cy-d/2,d,d);}}
   foreach(var side in pieces.Where(p=>p.Location.StartsWith("Side-face")&&p.Start<=x+1e-9&&p.End>=x-1e-9)){int per=Math.Max(1,side.PerLine/2);for(int q=0;q<per;q++){float cy=y0+hh*(q+1)/(per+1);float d=(float)Math.Max(2,side.Diameter*k);using(var bar=new SolidBrush(Color.FromArgb(120,120,120))){g.FillEllipse(bar,x0+c-d/2,cy-d/2,d,d);g.FillEllipse(bar,x0+w-c-d/2,cy-d/2,d,d);}}}
   Center(g,label(botBars),small,new SolidBrush(Color.FromArgb(20,120,90)),x0+w/2,y0+hh+2);
   /* dimensions */
   DimH(g,dim,small,muted,x0,x0+w,y0+hh+22,F0(b));{float vx=x0-8;g.DrawLine(dim,vx,y0,vx,y0+hh);g.DrawLine(dim,vx-3,y0,vx+3,y0);g.DrawLine(dim,vx-3,y0+hh,vx+3,y0+hh);var st=g.Save();g.TranslateTransform(vx-2,y0+hh/2);g.RotateTransform(-90);var sz=g.MeasureString(F0(h),small);g.DrawString(F0(h),small,muted,-sz.Width/2,-sz.Height);g.Restore(st);}
   string tieText=zone!=null?legs+"L-Ø"+tieDia+" @ "+F0(zone.Spacing*1000)+(legs>2?" (crossties)":""):"in support";Center(g,tieText,small,ink,x0+w/2-4,y0+hh+30);
   Center(g,"cover "+F0(cover)+", hoops 135°",small,muted,x0+w/2-4,y0+hh+40);
   var marks=string.Join(", ",here.Select(p=>p.BarMark));var rect=new RectangleF(box.Left+4,y0+hh+51,box.Width-8,box.Bottom-(y0+hh+51));g.DrawString(marks,small,muted,rect);
   using(var frame=new Pen(PowTheme.Line,.5f))g.DrawRectangle(frame,box.Left+1,box.Top,box.Width-2,box.Height-4);}}
}
}
