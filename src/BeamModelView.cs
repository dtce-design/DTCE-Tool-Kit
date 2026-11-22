using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Drawing2D;using System.Globalization;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* Interactive beam model (owner, 19 November 2026: "Gawin interactive yung model na kayang magmodel yung cross section ng beam or
   elevation niya nakikita mo pano yung splicing details niya lahat"). The elevation of one beam line shows the supports, spans,
   allowed splice zones, stirrup zones, every bar on its own lane with laps and hooks; the cross-section shows the bars at the
   clicked point. Scroll zooms, drag pans, double-click fits; clicking a bar selects it (and its row in the lists). */
class BeamModelView:Control {
 static readonly CultureInfo Inv=CultureInfo.InvariantCulture;
 CutResult result;CutLineGeometry geo;List<CutPiece> pieces=new List<CutPiece>();readonly Dictionary<CutPiece,int> lane=new Dictionary<CutPiece,int>();int topLanes,bottomLanes;
 double zoom=1,offset;bool dragging;Point dragStart;double dragOffset,dragOffsetY;double sectionX=double.NaN;string selected;readonly ToolTip tip=new ToolTip();string lastTip="";
 public event Action<string> BarSelected;
 public BeamModelView(){DoubleBuffered=true;ResizeRedraw=true;BackColor=Color.White;Dock=DockStyle.Fill;TabStop=true;}
 public string Line{get{return geo==null?null:geo.Line;}}
 public void SetResult(CutResult r,string line){result=r;geo=r==null?null:r.Geometry.FirstOrDefault(g=>g.Line==line)??(r==null?null:r.Geometry.FirstOrDefault());pieces=geo==null?new List<CutPiece>():r.Pieces.Where(p=>p.Line==geo.Line).ToList();Lanes();if(geo!=null&&(double.IsNaN(sectionX)||sectionX>geo.Total))sectionX=geo.FaceLeft[0]+geo.Ln[0]/2;Invalidate();}
 public string Selected{get{return selected;}set{if(selected==value)return;selected=value;var p=pieces.FirstOrDefault(x=>x.BarMark==value);if(p!=null&&geo!=null&&!(p.ShapeCode=="HOOP"||p.ShapeCode=="CROSSTIE"))sectionX=Math.Max(0,Math.Min(geo.Total,(p.Start+p.End)/2));Invalidate();}}
 public void Fit(){zoom=1;offset=0;offsetY=0;Invalidate();}
 static bool Longitudinal(CutPiece p){return BeamCutting.Longitudinal(p);}
 /* lanes: pieces of a face that overlap (laps, extra bars) go on separate lanes */
 void Lanes(){lane.Clear();topLanes=bottomLanes=0;foreach(bool top in new[]{true,false}){int cnt;foreach(var kv in BeamDetailPaper.AssignLanes(pieces.Where(x=>Longitudinal(x)&&x.Top==top),out cnt))lane[kv.Key]=kv.Value;if(top)topLanes=cnt;else bottomLanes=cnt;}}
 RectangleF Elevation{get{return new RectangleF(40,46,Math.Max(100,Width-SectionWidth-80),Math.Max(120,Height-150));}}
 int SectionWidth{get{return Math.Min(300,Math.Max(220,Width/4));}}
 float X(double x){var e=Elevation;return (float)(e.Left+(x/geo.Total*e.Width)*zoom-offset);}
 double FromX(float px){var e=Elevation;return ((px+offset-e.Left)/zoom)/e.Width*geo.Total;}
 /* true scale (owner: "Iscale naman yung drawing"): the depth uses the same pixels per metre as the length; drag up / down to pan */
 bool trueScale=true;double offsetY;public bool TrueScale{get{return trueScale;}set{trueScale=value;offsetY=0;Invalidate();}}
 float PxPerM{get{return geo==null?1:(float)(Elevation.Width/geo.Total*zoom);}}float DepthPx{get{return (float)(geo.Depth.Max()*PxPerM);}}
 float BeamTop{get{if(!trueScale)return Elevation.Top+40;var e=Elevation;return (float)((e.Top+e.Bottom)/2-DepthPx/2+offsetY);}}float BeamBottom{get{if(!trueScale)return Elevation.Bottom-40;return BeamTop+DepthPx;}}
 float LaneOffset(int k,int dia){if(!trueScale){float pitch=Math.Min(10,(BeamBottom-BeamTop)/Math.Max(6,topLanes+bottomLanes+4));return 12+k*pitch;}float m=PxPerM;return (float)((geo.Cover+.010+dia/2000.0)*m+k*Math.Max(2.5,(dia/1000.0+.025)*m));}
 float LaneY(CutPiece p){int k;lane.TryGetValue(p,out k);float off=LaneOffset(k,p.Diameter);return p.Top?BeamTop+off:BeamBottom-off;}
 static Color BarColor(CutPiece p,bool selected){if(selected)return Color.FromArgb(220,90,20);if(p.Shape=="REVIEW")return Color.Firebrick;if(p.Location.Contains("continuous"))return p.Top?Color.FromArgb(25,90,160):Color.FromArgb(20,120,90);return p.Top?Color.FromArgb(110,150,210):Color.FromArgb(90,170,130);}
 protected override void OnPaint(PaintEventArgs e){/* drawings stay paper-white with dark ink in dark mode (owner, 21 November 2026: "pangit ng dark mode") */if(DesktopAppearance.Palette.Dark)e.Graphics.Clear(Color.White);using(new ReportPaperScope())PaintPaper(e);}
 void PaintPaper(PaintEventArgs e){base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;var font=ModernUi.Body;var small=new Font("Segoe UI",7.5f);
  using(var ink=new SolidBrush(PowTheme.Ink))using(var muted=new SolidBrush(PowTheme.Muted)){
   if(geo==null){g.DrawString("Load a schedule and enter the beam lines to see the model.",font,muted,20,20);return;}
   g.DrawString("ELEVATION - "+geo.Line+"   (scroll: zoom  •  drag: pan  •  double-click: fit  •  click a bar to select it, click the beam for its section)",font,ink,12,10);
   var el=Elevation;var clip=g.Clip;/* the elevation stops before the section panel (owner, 22 November 2026: "lagyan ng distinct separator yung elevation at section") */float sepX=Width-SectionWidth-20;g.SetClip(new RectangleF(el.Left-30,el.Top-10,Math.Max(10,Math.Min(el.Width+60,sepX-6-(el.Left-30))),el.Height+80));
   /* supports and splice zones */
   using(var support=new HatchBrush(HatchStyle.BackwardDiagonal,Color.FromArgb(160,160,160),Color.FromArgb(235,235,235)))using(var edge=new Pen(Color.Gray))for(int i=0;i<geo.SupportStart.Length;i++){float a=X(geo.SupportStart[i]),b=X(geo.SupportStart[i]+geo.SupportWidth[i]);float ext=trueScale?Math.Max(20,DepthPx*.8f):30;g.FillRectangle(support,a,BeamTop-ext,b-a,BeamBottom-BeamTop+2*ext);g.DrawRectangle(edge,a,BeamTop-ext,b-a,BeamBottom-BeamTop+2*ext);g.DrawString("S"+(i+1),small,muted,a,BeamBottom+32);}
   /* allowed splice zones: dimensions under the spans (no fill in the beam) */using(var zp=new Pen(Color.FromArgb(60,140,90))){Action<List<double[]>,float,string> zd=(zones,zy,name)=>{foreach(var z in zones){float za=X(z[0]),zb=X(z[1]);g.DrawLine(zp,za,zy,zb,zy);g.DrawLine(zp,za,zy-3,za,zy+3);g.DrawLine(zp,zb,zy-3,zb,zy+3);string t=name;using(var sf=new Font("Segoe UI",7.5f)){var sz=g.MeasureString(t,sf);if(zb-za>sz.Width)using(var zb2=new SolidBrush(Color.FromArgb(40,120,75)))g.DrawString(t,sf,zb2,(za+zb)/2-sz.Width/2,zy-sz.Height);}}};zd(geo.TopZones,BeamBottom+94,"splice zone, top");zd(geo.BottomZones,BeamBottom+114,"splice zone, bottom");}
   /* beam outline */
   using(var outline=new Pen(PowTheme.Ink,1.4f))g.DrawRectangle(outline,X(0),BeamTop,X(geo.Total)-X(0),BeamBottom-BeamTop);
   /* stirrups */
   using(var tie=new Pen(Color.FromArgb(150,150,150),1))using(var lapTie=new Pen(Color.FromArgb(200,110,20),1.2f))for(int j=0;j<geo.Ties.Count;j++){foreach(var q in BeamCutting.StirrupDrawing(geo,pieces,j)){float px=X(q.Key);if(px<el.Left-20||px>el.Right+20)continue;g.DrawLine(q.Value?lapTie:tie,px,BeamTop+3,px,BeamBottom-3);}
    foreach(var z in geo.Ties[j]){if(z.Count<=0||z.Spacing<=0)continue;float mid=(X(geo.FaceLeft[j]+z.From)+X(geo.FaceLeft[j]+z.To))/2;string label=z.Legs+"L-"+z.Dia+" @ "+(z.Spacing*1000).ToString("0",Inv);var size=g.MeasureString(label,small);if(X(geo.FaceLeft[j]+z.To)-X(geo.FaceLeft[j]+z.From)>size.Width+4)g.DrawString(label,small,muted,mid-size.Width/2,BeamBottom+4);}}
   /* bars */
   foreach(var p in pieces.Where(Longitudinal)){bool sel=p.BarMark==selected;float y=LaneY(p),a=X(p.Start),b=X(p.End);using(var pen=new Pen(BarColor(p,sel),sel?3.2f:2f)){BeamDetailPaper.BarLine(g,pen,p,a,b,y,v=>X(v));if(p.ShapeCode=="HOOK1"||p.ShapeCode=="HOOK2"||p.ShapeCode=="CRANKHOOK"){/* the hook leg: B of an L-bar, A / C of a U-bar (B is its straight length - owner, 20 November 2026: "ano itong mahaba"), E of a K5 bar */
      Func<double,float> leg=mm=>trueScale?(float)Math.Max(5,mm/1000.0*PxPerM):14;double legStart=p.ShapeCode=="HOOK2"?p.DimA:p.ShapeCode=="HOOK1"?p.DimB:0,legEnd=p.ShapeCode=="HOOK2"?p.DimC:p.ShapeCode=="HOOK1"?p.DimB:p.DimE;
      bool startHook=p.Start<=geo.Cover+1e-6,endHook=p.End>=geo.Total-geo.Cover-1e-6;if(startHook&&legStart>0){float h=leg(legStart);g.DrawLine(pen,a,y,a,y+(p.Top?h:-h));}if(endHook&&legEnd>0){float h=leg(legEnd);g.DrawLine(pen,b,y,b,y+(p.Top?h:-h));}}}
    /* only the selected bar is labelled (true scale leaves no room for every label) */if(sel){string t=p.BarMark+"  "+p.PerLine+"-"+p.Diameter+"mm  "+p.Length.ToString("0.00",Inv)+" m";var size=g.MeasureString(t,small);float ty=p.Top?y-size.Height:y+1;g.DrawString(t,small,sel?Brushes.Black:muted,Math.Max(a,el.Left),ty);}}
   /* curtailment of the extra bars (owner, 22 November 2026: "ipakita mo din kaya yung curtailment length"): from the support face
      to the curtailment point (L/4, L/3, L/6 of the clear span, as the Curtailment Settings) and, past it, the cut-end allowance */
   using(var cp=new Pen(Color.FromArgb(150,60,150),1f))using(var cb=new SolidBrush(Color.FromArgb(150,60,150))){int topRow=0,botRow=0;var drawnDims=new HashSet<string>();
    foreach(var p in pieces.Where(x=>Longitudinal(x)&&x.Location!=null&&x.Location.Contains("extra")).OrderBy(x=>x.Start)){
     bool mid=p.Location.Contains("midspan")||(!p.Top&&p.Location.StartsWith("Bottom extra, span"));
     foreach(bool atEnd in new[]{false,true}){double xb=atEnd?p.End:p.Start;int j=-1;for(int k=0;k<geo.Ln.Length;k++)if(xb>geo.FaceLeft[k]+1e-6&&xb<geo.FaceRight[k]-1e-6)j=k;if(j<0)continue;
      double frac=mid?(p.Top?.25:geo.BottomCut):(p.Top?geo.TopExtension:.25);bool fromLeft=mid?!atEnd:atEnd;double face=fromLeft?geo.FaceLeft[j]:geo.FaceRight[j],point=fromLeft?face+frac*geo.Ln[j]:face-frac*geo.Ln[j];
      string dimKey=(p.Top?"T":"B")+Math.Round(face,3)+"|"+Math.Round(point,3)+"|"+Math.Round(xb,3);if(!drawnDims.Add(dimKey))continue;
      float y=p.Top?BeamTop-12-(topRow%3)*13:BeamBottom+134+(botRow%2)*12;float fa=X(face),fp=X(point),fe=X(xb);
      Action<float,float,string> dimLine=(u,v,t)=>{if(Math.Abs(v-u)<2)return;g.DrawLine(cp,u,y,v,y);g.DrawLine(cp,u,y-3,u,y+3);g.DrawLine(cp,v,y-3,v,y+3);var sz=g.MeasureString(t,small);if(Math.Abs(v-u)>sz.Width+2)g.DrawString(t,small,cb,(u+v)/2-sz.Width/2,y-sz.Height+1);};
      dimLine(Math.Min(fa,fp),Math.Max(fa,fp),Fraction(frac)+" = "+(frac*geo.Ln[j]*1000).ToString("0",Inv));
      double beyond=fromLeft?xb-point:point-xb;if(beyond>.001)dimLine(Math.Min(fp,fe),Math.Max(fp,fe),"+"+(beyond*1000).ToString("0",Inv));
      using(var tick=new Pen(Color.FromArgb(150,60,150),1f){DashStyle=DashStyle.Dot})g.DrawLine(tick,fp,y,fp,p.Top?BeamTop:BeamBottom);}
     if(p.Top)topRow++;else botRow++;}}
   /* layer spacer bars seen end-on between the 1st and 2nd layer */
   using(var spb=new SolidBrush(Color.FromArgb(150,110,60)))foreach(var sp in pieces.Where(x=>x.Location!=null&&x.Location.StartsWith("Layer spacer"))){int count=Math.Max(1,sp.PerLine);float off=(LaneOffset(0,16)+LaneOffset(1,16))/2;float y=sp.Top?BeamTop+off:BeamBottom-off;float d=trueScale?(float)Math.Max(3,sp.Diameter/1000.0*PxPerM):4;
    for(int q=0;q<count;q++){double xm=count==1?(sp.Start+sp.End)/2:sp.Start+(sp.End-sp.Start)*q/(count-1);float px=X(xm);g.FillEllipse(spb,px-d/2,y-d/2,d,d);}}
   /* spans left out of the estimate: greyed, only the dowels of the spans beside them */
   if(geo.Included!=null)for(int j=0;j<geo.Included.Length&&j<geo.FaceLeft.Length;j++)if(!geo.Included[j]){float a=X(geo.FaceLeft[j]),b=X(geo.FaceRight[j]);var box=new RectangleF(a,BeamTop-6,b-a,BeamBottom-BeamTop+12);using(var veil=new SolidBrush(Color.FromArgb(150,236,238,240)))g.FillRectangle(veil,box);using(var hatch=new HatchBrush(HatchStyle.WideUpwardDiagonal,Color.FromArgb(70,120,120,120),Color.Transparent))g.FillRectangle(hatch,box);using(var dash=new Pen(Color.FromArgb(150,150,150)){DashStyle=DashStyle.Dash})g.DrawRectangle(dash,box.X,box.Y,box.Width,box.Height);
    string t="SPAN "+(j+1)+" NOT INCLUDED - dowels (abang) only";using(var f=new Font("Segoe UI Semibold",9f)){var sz=g.MeasureString(t,f);if(sz.Width<box.Width-8)using(var bg=new SolidBrush(Color.FromArgb(230,255,255,255))){float tx=a+(b-a-sz.Width)/2,ty=(BeamTop+BeamBottom-sz.Height)/2;g.FillRectangle(bg,tx-3,ty,sz.Width+6,sz.Height);g.DrawString(t,f,Brushes.DimGray,tx,ty);}}}
   /* laps */
   if(result!=null)using(var lapPen=new Pen(Color.FromArgb(230,140,20),1.2f){DashStyle=DashStyle.Dash})foreach(var c in result.Splices.Where(c=>c.Line==geo.Line)){var piece=pieces.FirstOrDefault(p=>p.Id.StartsWith(c.Bar+".")&&Math.Abs(p.End-c.To)<1e-6);bool top=piece!=null&&piece.Top;float reach=LaneOffset(Math.Max(1,top?topLanes:bottomLanes),c.Diameter);float y1=top?BeamTop+2:BeamBottom-2,y2=top?BeamTop+reach:BeamBottom-reach;float a=X(c.From),b=X(c.To);using(var fill=new SolidBrush(c.Ok?Color.FromArgb(50,240,170,40):Color.FromArgb(70,220,40,40)))g.FillRectangle(fill,a,Math.Min(y1,y2),b-a,Math.Abs(y2-y1));g.DrawRectangle(lapPen,a,Math.Min(y1,y2),b-a,Math.Abs(y2-y1));
    string t="Lap "+c.Diameter+"mm "+(c.Lap*1000).ToString("0",Inv)+(c.Ok?"":" NG");var size=g.MeasureString(t,small);g.DrawString(t,small,c.Ok?Brushes.DarkOrange:Brushes.Firebrick,(a+b)/2-size.Width/2,top?BeamTop-14:BeamBottom+16);}
   /* section cut and span dimensions */
   if(!double.IsNaN(sectionX))using(var cut=new Pen(Color.FromArgb(200,40,40),1.4f){DashStyle=DashStyle.DashDot}){float px=X(sectionX);g.DrawLine(cut,px,BeamTop-24,px,BeamBottom+24);g.DrawString("A",font,Brushes.Firebrick,px+3,BeamTop-26);}
   using(var dim=new Pen(PowTheme.Muted))for(int j=0;j<geo.Ln.Length;j++){float a=X(geo.FaceLeft[j]),b=X(geo.FaceRight[j]),y=BeamBottom+52;g.DrawLine(dim,a,y,b,y);g.DrawLine(dim,a,y-4,a,y+4);g.DrawLine(dim,b,y-4,b,y+4);string t="Span "+(j+1)+" ("+geo.Marks[j]+")  Ln = "+geo.Ln[j].ToString("0.000",Inv)+" m";var size=g.MeasureString(t,small);if(b-a>size.Width)g.DrawString(t,small,muted,(a+b)/2-size.Width/2,y+3);}
   g.Clip=clip;
   /* legend */
   float lx=el.Left,ly=Height-26;foreach(var item in new[]{new{T="Top continuous",C=Color.FromArgb(25,90,160)},new{T="Top extra",C=Color.FromArgb(110,150,210)},new{T="Bottom continuous",C=Color.FromArgb(20,120,90)},new{T="Bottom extra",C=Color.FromArgb(90,170,130)},new{T="Lap",C=Color.FromArgb(230,140,20)},new{T="Allowed splice zone (dimension)",C=Color.FromArgb(60,140,90)},new{T="Curtailment from face (+ cut-end allowance)",C=Color.FromArgb(150,60,150)},new{T="Layer spacer bar",C=Color.FromArgb(150,110,60)}}){using(var b=new SolidBrush(item.C))g.FillRectangle(b,lx,ly+4,16,8);g.DrawString(item.T,small,muted,lx+20,ly);lx+=g.MeasureString(item.T,small).Width+40;}
   {double ratio=e.Graphics.DpiX/25.4/(PxPerM/1000);string t=(trueScale?"Scale 1:":"Horizontal scale 1:")+ratio.ToString(ratio>=100?"0":"0.#",Inv)+" on screen"+(trueScale?" (both directions)":" - depth exaggerated");var sz=g.MeasureString(t,small);g.DrawString(t,small,muted,el.Right-sz.Width,Height-26);}
   DrawSection(g,font,small,ink,muted);}}
 void DrawSection(Graphics g,Font font,Font small,Brush ink,Brush muted){var box=new RectangleF(Width-SectionWidth-10,40,SectionWidth,Height-80);{var panel=new RectangleF(box.Left-10,box.Top-12,Width-(box.Left-10),box.Height+8);using(var bg=new SolidBrush(Color.FromArgb(244,246,249)))g.FillRectangle(bg,panel);using(var line=new Pen(Color.FromArgb(70,85,105),2f))g.DrawLine(line,panel.Left,panel.Top,panel.Left,panel.Bottom);}if(double.IsNaN(sectionX))return;
  int j=0;for(int k=0;k<geo.Ln.Length;k++)if(sectionX>=geo.SupportStart[k])j=k;bool inSupport=sectionX<geo.FaceLeft[j]||sectionX>geo.FaceRight[j];double b=geo.Width[j]*1000,h=geo.Depth[j]*1000,cover=geo.Cover*1000;
  g.DrawString("SECTION A-A  at "+sectionX.ToString("0.000",Inv)+" m",font,ink,box.Left,box.Top);g.DrawString((inSupport?"in support "+(sectionX<geo.FaceLeft[j]?j+1:j+2):"span "+(j+1))+", "+geo.Marks[j]+"  "+b.ToString("0",Inv)+" x "+h.ToString("0",Inv)+" mm",small,muted,box.Left,box.Top+20);
  float scale=(float)Math.Min((box.Width-40)/b,(box.Height-170)/h);float w=(float)(b*scale),hh=(float)(h*scale),x0=box.Left+(box.Width-w)/2,y0=box.Top+50;
  using(var concrete=new SolidBrush(Color.FromArgb(236,236,232)))g.FillRectangle(concrete,x0,y0,w,hh);using(var edge=new Pen(PowTheme.Ink,1.4f))g.DrawRectangle(edge,x0,y0,w,hh);
  float c=(float)(cover*scale);using(var tie=new Pen(Color.Gray,1.2f))g.DrawRectangle(tie,x0+c,y0+c,w-2*c,hh-2*c);
  var lines=new List<string>();double tieMm=10;var tz=geo.Ties[j].FirstOrDefault();if(tz!=null)tieMm=tz.Dia;double localX=sectionX-geo.FaceLeft[j];string zoneName=localX<.25*geo.Ln[j]?"left":localX>.75*geo.Ln[j]?"right":"middle";
  /* the bars that cross the cut, the same bars as the elevation (owner, 22 November 2026) */
  foreach(bool top in new[]{true,false}){var layers=BeamCutting.CrossingLayers(geo,pieces,sectionX,top);if(layers.Count==0)continue;double offMm=cover+tieMm;
   foreach(var layer in layers){int dmax=layer.Max(p=>p.Diameter);double inner=b-2*cover-2*tieMm;for(int q=0;q<layer.Count;q++){var p=layer[q];double cxMm=layer.Count==1?b/2:cover+tieMm+dmax/2.0+(inner-dmax)*q/(layer.Count-1);double cyMm=offMm+dmax/2.0;float d=(float)Math.Max(5,p.Diameter*scale);float cx=x0+(float)(cxMm*scale),cy=top?y0+(float)(cyMm*scale):y0+hh-(float)(cyMm*scale);bool isSel=selected!=null&&p.BarMark==selected;
     using(var bar=new SolidBrush(BarColor(p,isSel)))g.FillEllipse(bar,cx-d/2,cy-d/2,d,d);if(isSel)using(var ring=new Pen(Color.FromArgb(220,90,20),2f))g.DrawEllipse(ring,cx-d/2-3,cy-d/2-3,d+6,d+6);}offMm+=dmax+25;}
   string text=BeamCutting.CrossingText(layers);lines.Add((top?"Top: ":"Bottom: ")+text+(layers.Count>1?" ("+string.Join(" / ",new[]{"1st","2nd","3rd","4th"}.Take(layers.Count))+" layer)":"")+"  ("+string.Join(", ",layers.SelectMany(l=>l).Select(p=>p.Short).Distinct())+")");
   var scheduled=inSupport?null:BeamCutting.SectionLayers(geo,sectionX,top);if(scheduled!=null&&scheduled.Count>0){string want=BeamCutting.LayersText(scheduled);if(want!=text)lines.Add("   schedule, "+zoneName+" zone: "+want+" - the extra bars run on to their cut-off (L/"+(top?"4":"6")+" from the face + end allowance)");}}
  var ties=geo.Ties[j].FirstOrDefault(z=>sectionX-geo.FaceLeft[j]>=z.From-1e-9&&sectionX-geo.FaceLeft[j]<=z.To+1e-9);if(ties!=null&&!inSupport)lines.Add("Stirrups: "+ties.Legs+"L-"+ties.Dia+"mm @ "+(ties.Spacing*1000).ToString("0",Inv)+" mm ("+ties.Name+" zone)");
  var lap=result==null?null:result.Splices.FirstOrDefault(s=>s.Line==geo.Line&&sectionX>=s.From&&sectionX<=s.To);if(lap!=null)lines.Add("Inside a lap of "+lap.Bar+" ("+lap.Diameter+"mm, "+(lap.Lap*1000).ToString("0",Inv)+" mm) - "+lap.Status);
  float ty=y0+hh+14;foreach(var t in lines){var rect=new RectangleF(box.Left,ty,box.Width,60);g.DrawString(t,small,ink,rect);ty+=g.MeasureString(t,small,(int)box.Width).Height+4;}}
 static string Fraction(double f){foreach(int d in new[]{2,3,4,5,6,8})if(Math.Abs(f*d-1)<1e-6)return "L/"+d;return (f*100).ToString("0.#",Inv)+"% L";}
 CutPiece Hit(Point p){if(geo==null)return null;CutPiece best=null;float bestDistance=6;foreach(var piece in pieces.Where(Longitudinal)){float y=LaneY(piece),a=X(piece.Start),b=X(piece.End);if(p.X<a-3||p.X>b+3)continue;float d=Math.Abs(p.Y-y);if(d<bestDistance){bestDistance=d;best=piece;}}return best;}
 protected override void OnMouseWheel(MouseEventArgs e){base.OnMouseWheel(e);if(geo==null)return;double before=FromX(e.X);zoom=Math.Max(1,Math.Min(40,zoom*(e.Delta>0?1.25:.8)));var el=Elevation;offset=el.Left+(before/geo.Total*el.Width)*zoom-e.X;Invalidate();}
 protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);Focus();if(e.Button==MouseButtons.Left){dragging=true;dragStart=e.Location;dragOffset=offset;dragOffsetY=offsetY;}}
 protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(dragging&&(Math.Abs(e.X-dragStart.X)>3||Math.Abs(e.Y-dragStart.Y)>3)){offset=dragOffset-(e.X-dragStart.X);if(trueScale)offsetY=dragOffsetY+(e.Y-dragStart.Y);Invalidate();return;}var hit=Hit(e.Location);string text=hit==null?"":hit.BarMark+" - "+hit.Location+"\n"+hit.PerLine+" x "+hit.Diameter+" mm, cut "+hit.Length.ToString("0.000",Inv)+" m, "+hit.Shape+"\nfrom "+hit.Start.ToString("0.000",Inv)+" to "+hit.End.ToString("0.000",Inv)+" m";if(text!=lastTip){lastTip=text;tip.SetToolTip(this,text);}}
 protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);bool moved=dragging&&(Math.Abs(e.X-dragStart.X)>3||Math.Abs(e.Y-dragStart.Y)>3);dragging=false;if(moved||geo==null||e.Button!=MouseButtons.Left)return;var hit=Hit(e.Location);if(hit!=null){selected=hit.BarMark;if(BarSelected!=null)BarSelected(hit.BarMark);}if(e.X<Elevation.Right+10)sectionX=Math.Max(0,Math.Min(geo.Total,FromX(e.X)));Invalidate();}
 protected override void OnDoubleClick(EventArgs e){base.OnDoubleClick(e);Fit();}
 protected override void Dispose(bool disposing){if(disposing)tip.Dispose();base.Dispose(disposing);}
}
}
