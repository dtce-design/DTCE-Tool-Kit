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
 void Lanes(){lane.Clear();topLanes=bottomLanes=0;foreach(bool top in new[]{true,false}){var ends=new List<double>();foreach(var p in pieces.Where(x=>Longitudinal(x)&&x.Top==top).OrderBy(x=>x.Location.Contains("continuous")?0:1).ThenBy(x=>x.Start)){int k=ends.FindIndex(e=>e<p.Start-1e-6);if(k<0){ends.Add(p.End);k=ends.Count-1;}else ends[k]=p.End;lane[p]=k;}if(top)topLanes=ends.Count;else bottomLanes=ends.Count;}}
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
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;var font=ModernUi.Body;var small=new Font("Segoe UI",7.5f);
  using(var ink=new SolidBrush(PowTheme.Ink))using(var muted=new SolidBrush(PowTheme.Muted)){
   if(geo==null){g.DrawString("Load a schedule and enter the beam lines to see the model.",font,muted,20,20);return;}
   g.DrawString("ELEVATION - "+geo.Line+"   (scroll: zoom  •  drag: pan  •  double-click: fit  •  click a bar to select it, click the beam for its section)",font,ink,12,10);
   var el=Elevation;var clip=g.Clip;g.SetClip(new RectangleF(el.Left-30,el.Top-10,el.Width+60,el.Height+80));
   /* supports and splice zones */
   using(var support=new HatchBrush(HatchStyle.BackwardDiagonal,Color.FromArgb(160,160,160),Color.FromArgb(235,235,235)))using(var edge=new Pen(Color.Gray))for(int i=0;i<geo.SupportStart.Length;i++){float a=X(geo.SupportStart[i]),b=X(geo.SupportStart[i]+geo.SupportWidth[i]);float ext=trueScale?Math.Max(20,DepthPx*.8f):30;g.FillRectangle(support,a,BeamTop-ext,b-a,BeamBottom-BeamTop+2*ext);g.DrawRectangle(edge,a,BeamTop-ext,b-a,BeamBottom-BeamTop+2*ext);g.DrawString("S"+(i+1),small,muted,a,BeamBottom+32);}
   using(var zoneBrush=new SolidBrush(Color.FromArgb(40,60,170,90)))foreach(var z in geo.TopZones.Concat(geo.BottomZones).GroupBy(z=>z[0]+"|"+z[1]).Select(x=>x.First()))g.FillRectangle(zoneBrush,X(z[0]),BeamTop,X(z[1])-X(z[0]),BeamBottom-BeamTop);
   /* beam outline */
   using(var outline=new Pen(PowTheme.Ink,1.4f))g.DrawRectangle(outline,X(0),BeamTop,X(geo.Total)-X(0),BeamBottom-BeamTop);
   /* stirrups */
   using(var tie=new Pen(Color.FromArgb(150,150,150),1))for(int j=0;j<geo.Ties.Count;j++)foreach(var z in geo.Ties[j]){if(z.Count<=0||z.Spacing<=0)continue;double a=geo.FaceLeft[j]+z.From;for(int k=0;k<z.Count;k++){double x=z.Name=="left"?geo.FaceLeft[j]+Math.Max(.05,z.From)+k*z.Spacing:z.Name=="right"?geo.FaceLeft[j]+z.To-.05-k*z.Spacing:a+(k+1)*z.Spacing;float px=X(x);if(px<el.Left-20||px>el.Right+20)continue;g.DrawLine(tie,px,BeamTop+3,px,BeamBottom-3);}
     float mid=(X(geo.FaceLeft[j]+z.From)+X(geo.FaceLeft[j]+z.To))/2;string label=z.Legs+"L-"+z.Dia+" @ "+(z.Spacing*1000).ToString("0",Inv);var size=g.MeasureString(label,small);if(X(geo.FaceLeft[j]+z.To)-X(geo.FaceLeft[j]+z.From)>size.Width+4)g.DrawString(label,small,muted,mid-size.Width/2,BeamBottom+4);}
   /* bars */
   foreach(var p in pieces.Where(Longitudinal)){bool sel=p.BarMark==selected;float y=LaneY(p),a=X(p.Start),b=X(p.End);using(var pen=new Pen(BarColor(p,sel),sel?3.2f:2f)){g.DrawLine(pen,a,y,b,y);if(p.ShapeCode=="HOOK1"||p.ShapeCode=="HOOK2"){float hookLen=trueScale?(float)Math.Max(5,p.DimB/1000.0*PxPerM):14;float hook=p.Top?hookLen:-hookLen;bool startHook=p.Start<=geo.Cover+1e-6,endHook=p.End>=geo.Total-geo.Cover-1e-6;if(startHook)g.DrawLine(pen,a,y,a,y+hook);if(endHook)g.DrawLine(pen,b,y,b,y+hook);}}
    if(b-a>70||sel){string t=p.BarMark+"  "+p.PerLine+"-"+p.Diameter+"mm  "+p.Length.ToString("0.00",Inv)+" m";var size=g.MeasureString(t,small);float ty=p.Top?y-size.Height:y+1;g.DrawString(t,small,sel?Brushes.Black:muted,Math.Max(a,el.Left),ty);}}
   /* laps */
   if(result!=null)using(var lapPen=new Pen(Color.FromArgb(230,140,20),1.2f){DashStyle=DashStyle.Dash})foreach(var c in result.Splices.Where(c=>c.Line==geo.Line)){var piece=pieces.FirstOrDefault(p=>p.BarMark.StartsWith(c.Bar+".")&&Math.Abs(p.End-c.To)<1e-6);bool top=piece!=null&&piece.Top;float reach=LaneOffset(Math.Max(1,top?topLanes:bottomLanes),c.Diameter);float y1=top?BeamTop+2:BeamBottom-2,y2=top?BeamTop+reach:BeamBottom-reach;float a=X(c.From),b=X(c.To);using(var fill=new SolidBrush(c.Ok?Color.FromArgb(50,240,170,40):Color.FromArgb(70,220,40,40)))g.FillRectangle(fill,a,Math.Min(y1,y2),b-a,Math.Abs(y2-y1));g.DrawRectangle(lapPen,a,Math.Min(y1,y2),b-a,Math.Abs(y2-y1));
    string t="Lap "+c.Diameter+"mm "+(c.Lap*1000).ToString("0",Inv)+(c.Ok?"":" NG");var size=g.MeasureString(t,small);g.DrawString(t,small,c.Ok?Brushes.DarkOrange:Brushes.Firebrick,(a+b)/2-size.Width/2,top?BeamTop-14:BeamBottom+16);}
   /* section cut and span dimensions */
   if(!double.IsNaN(sectionX))using(var cut=new Pen(Color.FromArgb(200,40,40),1.4f){DashStyle=DashStyle.DashDot}){float px=X(sectionX);g.DrawLine(cut,px,BeamTop-24,px,BeamBottom+24);g.DrawString("A",font,Brushes.Firebrick,px+3,BeamTop-26);}
   using(var dim=new Pen(PowTheme.Muted))for(int j=0;j<geo.Ln.Length;j++){float a=X(geo.FaceLeft[j]),b=X(geo.FaceRight[j]),y=BeamBottom+52;g.DrawLine(dim,a,y,b,y);g.DrawLine(dim,a,y-4,a,y+4);g.DrawLine(dim,b,y-4,b,y+4);string t="Span "+(j+1)+" ("+geo.Marks[j]+")  Ln = "+geo.Ln[j].ToString("0.000",Inv)+" m";var size=g.MeasureString(t,small);if(b-a>size.Width)g.DrawString(t,small,muted,(a+b)/2-size.Width/2,y+3);}
   g.Clip=clip;
   /* legend */
   float lx=el.Left,ly=Height-26;foreach(var item in new[]{new{T="Top continuous",C=Color.FromArgb(25,90,160)},new{T="Top extra",C=Color.FromArgb(110,150,210)},new{T="Bottom continuous",C=Color.FromArgb(20,120,90)},new{T="Bottom extra",C=Color.FromArgb(90,170,130)},new{T="Lap",C=Color.FromArgb(230,140,20)},new{T="Allowed splice zone",C=Color.FromArgb(150,200,160)}}){using(var b=new SolidBrush(item.C))g.FillRectangle(b,lx,ly+4,16,8);g.DrawString(item.T,small,muted,lx+20,ly);lx+=g.MeasureString(item.T,small).Width+40;}
   {double ratio=e.Graphics.DpiX/25.4/(PxPerM/1000);string t=(trueScale?"Scale 1:":"Horizontal scale 1:")+ratio.ToString(ratio>=100?"0":"0.#",Inv)+" on screen"+(trueScale?" (both directions)":" - depth exaggerated");var sz=g.MeasureString(t,small);g.DrawString(t,small,muted,el.Right-sz.Width,Height-26);}
   DrawSection(g,font,small,ink,muted);}}
 void DrawSection(Graphics g,Font font,Font small,Brush ink,Brush muted){var box=new RectangleF(Width-SectionWidth-10,40,SectionWidth,Height-80);using(var line=new Pen(PowTheme.Line))g.DrawLine(line,box.Left-10,box.Top,box.Left-10,box.Bottom);if(double.IsNaN(sectionX))return;
  int j=0;for(int k=0;k<geo.Ln.Length;k++)if(sectionX>=geo.SupportStart[k])j=k;bool inSupport=sectionX<geo.FaceLeft[j]||sectionX>geo.FaceRight[j];double b=geo.Width[j]*1000,h=geo.Depth[j]*1000,cover=geo.Cover*1000;
  g.DrawString("SECTION A-A  at "+sectionX.ToString("0.000",Inv)+" m",font,ink,box.Left,box.Top);g.DrawString((inSupport?"in support "+(sectionX<geo.FaceLeft[j]?j+1:j+2):"span "+(j+1))+", "+geo.Marks[j]+"  "+b.ToString("0",Inv)+" x "+h.ToString("0",Inv)+" mm",small,muted,box.Left,box.Top+20);
  float scale=(float)Math.Min((box.Width-40)/b,(box.Height-170)/h);float w=(float)(b*scale),hh=(float)(h*scale),x0=box.Left+(box.Width-w)/2,y0=box.Top+50;
  using(var concrete=new SolidBrush(Color.FromArgb(236,236,232)))g.FillRectangle(concrete,x0,y0,w,hh);using(var edge=new Pen(PowTheme.Ink,1.4f))g.DrawRectangle(edge,x0,y0,w,hh);
  if(geo.Slab[j]>0){float ts=(float)(geo.Slab[j]*1000*scale);using(var slab=new Pen(PowTheme.Muted){DashStyle=DashStyle.Dash})g.DrawLine(slab,x0-20,y0+ts,x0+w+20,y0+ts);g.DrawString("slab",small,muted,x0+w+2,y0+ts-12);}
  float c=(float)(cover*scale);using(var tie=new Pen(Color.Gray,1.2f))g.DrawRectangle(tie,x0+c,y0+c,w-2*c,hh-2*c);
  var here=pieces.Where(p=>Longitudinal(p)&&p.Start<=sectionX+1e-9&&p.End>=sectionX-1e-9).ToList();var lines=new List<string>();
  foreach(bool top in new[]{true,false}){var bars=here.Where(p=>p.Top==top).OrderByDescending(p=>p.Location.Contains("continuous")).ToList();int count=bars.Sum(p=>p.PerLine);if(count==0)continue;var spots=new List<KeyValuePair<CutPiece,int>>();foreach(var p in bars)for(int k=0;k<p.PerLine;k++)spots.Add(new KeyValuePair<CutPiece,int>(p,k));
   int perRow=Math.Max(2,Math.Min(spots.Count,(int)((b-2*cover)/40)+1));for(int k=0;k<spots.Count;k++){int row=k/perRow,col=k%perRow;int inRow=Math.Min(perRow,spots.Count-row*perRow);var p=spots[k].Key;float d=(float)Math.Max(5,p.Diameter*scale);float cx=inRow==1?x0+w/2:x0+c+d/2+4+(w-2*c-d-8)*col/(inRow-1);float cy=top?y0+c+d/2+4+row*(d+6):y0+hh-c-d/2-4-row*(d+6);using(var bar=new SolidBrush(BarColor(p,p.BarMark==selected)))g.FillEllipse(bar,cx-d/2,cy-d/2,d,d);}
   lines.Add((top?"Top: ":"Bottom: ")+string.Join(" + ",bars.GroupBy(p=>p.Diameter).Select(x=>x.Sum(p=>p.PerLine)+"-"+x.Key+"mm"))+"  ("+string.Join(", ",bars.Select(p=>p.BarMark))+")");}
  var ties=geo.Ties[j].FirstOrDefault(z=>sectionX-geo.FaceLeft[j]>=z.From-1e-9&&sectionX-geo.FaceLeft[j]<=z.To+1e-9);if(ties!=null&&!inSupport)lines.Add("Stirrups: "+ties.Legs+"L-"+ties.Dia+"mm @ "+(ties.Spacing*1000).ToString("0",Inv)+" mm ("+ties.Name+" zone)");
  var lap=result==null?null:result.Splices.FirstOrDefault(s=>s.Line==geo.Line&&sectionX>=s.From&&sectionX<=s.To);if(lap!=null)lines.Add("Inside a lap of "+lap.Bar+" ("+lap.Diameter+"mm, "+(lap.Lap*1000).ToString("0",Inv)+" mm) - "+lap.Status);
  float ty=y0+hh+14;foreach(var t in lines){var rect=new RectangleF(box.Left,ty,box.Width,60);g.DrawString(t,small,ink,rect);ty+=g.MeasureString(t,small,(int)box.Width).Height+4;}}
 CutPiece Hit(Point p){if(geo==null)return null;CutPiece best=null;float bestDistance=6;foreach(var piece in pieces.Where(Longitudinal)){float y=LaneY(piece),a=X(piece.Start),b=X(piece.End);if(p.X<a-3||p.X>b+3)continue;float d=Math.Abs(p.Y-y);if(d<bestDistance){bestDistance=d;best=piece;}}return best;}
 protected override void OnMouseWheel(MouseEventArgs e){base.OnMouseWheel(e);if(geo==null)return;double before=FromX(e.X);zoom=Math.Max(1,Math.Min(40,zoom*(e.Delta>0?1.25:.8)));var el=Elevation;offset=el.Left+(before/geo.Total*el.Width)*zoom-e.X;Invalidate();}
 protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);Focus();if(e.Button==MouseButtons.Left){dragging=true;dragStart=e.Location;dragOffset=offset;dragOffsetY=offsetY;}}
 protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(dragging&&(Math.Abs(e.X-dragStart.X)>3||Math.Abs(e.Y-dragStart.Y)>3)){offset=dragOffset-(e.X-dragStart.X);if(trueScale)offsetY=dragOffsetY+(e.Y-dragStart.Y);Invalidate();return;}var hit=Hit(e.Location);string text=hit==null?"":hit.BarMark+" - "+hit.Location+"\n"+hit.PerLine+" x "+hit.Diameter+" mm, cut "+hit.Length.ToString("0.000",Inv)+" m, "+hit.Shape+"\nfrom "+hit.Start.ToString("0.000",Inv)+" to "+hit.End.ToString("0.000",Inv)+" m";if(text!=lastTip){lastTip=text;tip.SetToolTip(this,text);}}
 protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);bool moved=dragging&&(Math.Abs(e.X-dragStart.X)>3||Math.Abs(e.Y-dragStart.Y)>3);dragging=false;if(moved||geo==null||e.Button!=MouseButtons.Left)return;var hit=Hit(e.Location);if(hit!=null){selected=hit.BarMark;if(BarSelected!=null)BarSelected(hit.BarMark);}if(e.X<Elevation.Right+10)sectionX=Math.Max(0,Math.Min(geo.Total,FromX(e.X)));Invalidate();}
 protected override void OnDoubleClick(EventArgs e){base.OnDoubleClick(e);Fit();}
 protected override void Dispose(bool disposing){if(disposing)tip.Dispose();base.Dispose(disposing);}
}
}
