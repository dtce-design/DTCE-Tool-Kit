using System;using System.Linq;using System.Drawing;using System.Drawing.Drawing2D;using System.Globalization;using System.Windows.Forms;
namespace BeamScheduleFormatter {
sealed class SteelSectionPreview:Control {
 readonly Func<SteelProfile> selected;readonly Func<bool> doubleAngle;readonly ToolTip hint=new ToolTip();
 double zoom=1;PointF pan;Point drag;bool dragging;string detail="";readonly Control[] watched;
 internal SteelProfile CurrentProfile{get{return selected();}}
 internal SteelSectionPreview(Func<SteelProfile> section,Func<bool> pair,params Control[] watched){
  selected=section;doubleAngle=pair;this.watched=watched;DoubleBuffered=true;ResizeRedraw=true;Width=1000;Height=380;Tag="FullWidth";TabStop=true;
  AccessibleName="Interactive selected steel section";AccessibleDescription="Wheel to zoom, drag to pan, double click or press Home to fit.";
  hint.SetToolTip(this,"Scroll to zoom • drag to pan • double-click / Home to fit. Dimensions in mm.");
  foreach(var control in watched){control.TextChanged+=ProfileChanged;var combo=control as ComboBox;if(combo!=null){combo.SelectedIndexChanged+=ProfileChanged;combo.SelectionChangeCommitted+=ProfileChanged;}}
  MouseDown+=(s,e)=>{if(e.Button!=MouseButtons.Left)return;Focus();drag=e.Location;dragging=true;Capture=true;};
  MouseMove+=(s,e)=>{if(!dragging)return;pan=new PointF(pan.X+e.X-drag.X,pan.Y+e.Y-drag.Y);drag=e.Location;Invalidate();};
  MouseUp+=(s,e)=>{dragging=false;Capture=false;};MouseCaptureChanged+=(s,e)=>{if(!Capture)dragging=false;};
  MouseWheel+=(s,e)=>{zoom=Math.Max(.4,Math.Min(4,zoom*Math.Pow(1.15,e.Delta/120.0)));Invalidate();};
  DoubleClick+=(s,e)=>ResetView();KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Home){ResetView();e.Handled=true;}};
 }
 void ProfileChanged(object sender,EventArgs e){if(!IsDisposed)ResetView();}
 void ResetView(){zoom=1;pan=PointF.Empty;Invalidate();}
 protected override void Dispose(bool disposing){if(disposing){foreach(var control in watched){control.TextChanged-=ProfileChanged;var combo=control as ComboBox;if(combo!=null){combo.SelectedIndexChanged-=ProfileChanged;combo.SelectionChangeCommitted-=ProfileChanged;}}hint.Dispose();}base.Dispose(disposing);}
 static string N(double v){return v.ToString("0.###",CultureInfo.InvariantCulture);}
 protected override void OnPaint(PaintEventArgs e){
  base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;var palette=DesktopAppearance.Palette;
  g.Clear(palette.Surface);using(var font=new Font("Segoe UI",9))using(var bold=new Font("Segoe UI",10,FontStyle.Bold)){
   SteelProfile p=selected();if(p==null){DrawLabel(g,"Select a catalog profile to display its section.",font,palette.Ink,12,12);return;}
   bool pair=doubleAngle!=null&&doubleAngle();bool pipe=p.Kind=="HSS circle / pipe";
   var plates=SteelSectionGeometry.Plates(p,pair);
   double minX=pipe?0:plates.Min(v=>v[0]),maxX=pipe?p.H:plates.Max(v=>v[0]+v[2]);
   double width=maxX-minX,height=p.H;
   float info=Math.Min(290,Math.Max(180,Width*.3f)),canvas=Width-info;
   DrawLabel(g,"SECTION PREVIEW",bold,palette.Ink,12,8);
   DrawLabel(g,"Scroll: zoom  •  Drag: pan  •  Double-click: fit",font,palette.Muted,12,32);
   float scale=(float)(Math.Min(Math.Max(80,canvas-140)/width,230/height)*zoom);
   float ox=canvas/2-(float)((minX+maxX)/2)*scale+pan.X,oy=205+(float)(height/2)*scale+pan.Y;
   var saved=g.Save();g.SetClip(new RectangleF(0,57,Math.Max(1,canvas),Height-80));
   using(var fill=new SolidBrush(Color.FromArgb(palette.Dark?139:65,126,175)))using(var outline=new Pen(palette.Ink,.8f)){
    if(pipe){using(var path=new GraphicsPath(FillMode.Alternate)){path.AddEllipse(ox,oy-(float)p.H*scale,(float)p.H*scale,(float)p.H*scale);path.AddEllipse(ox+(float)p.T*scale,oy-(float)(p.H-p.T)*scale,(float)(p.H-2*p.T)*scale,(float)(p.H-2*p.T)*scale);g.FillPath(fill,path);g.DrawPath(outline,path);}}
    else foreach(var v in plates){var box=new RectangleF(ox+(float)v[0]*scale,oy-(float)(v[1]+v[3])*scale,(float)v[2]*scale,(float)v[3]*scale);g.FillRectangle(fill,box);g.DrawRectangle(outline,box.X,box.Y,box.Width,box.Height);}
   }
   // Overall dimensions follow the same coordinate transform as the actual section.
   float x0=ox+(float)minX*scale,x1=ox+(float)maxX*scale,y0=oy-(float)p.H*scale;
   Dim(g,new PointF(x0-26,y0),new PointF(x0-26,oy),(pipe?"D":"H")+" = "+N(p.H),font,palette.Ink,true);
   Dim(g,new PointF(x0,oy+25),new PointF(x1,oy+25),(pair?"Assembly width (schematic)":pipe?"D":p.Name.StartsWith("Z")?"Overall width":"B")+" = "+N(width),font,palette.Ink,false);
   g.Restore(saved);
   using(var line=new Pen(palette.Line))g.DrawLine(line,canvas,65,canvas,Height-25);
   float tx=canvas+14;DrawLabel(g,p.Name,bold,palette.Ink,tx,70,info-25);
   detail=(pipe?"Outside diameter D": "Depth H")+" = "+N(p.H)+" mm\n"+(pipe?"":"Flange / leg B = "+N(p.B)+" mm\n")+"Thickness t = "+N(p.T)+" mm\n"+(p.Kind=="Purlin"?"Lip c = "+N(p.Lip)+" mm\n":"")+"Catalog mass = "+N(p.KgM)+" kg/m"+(pair?" per angle":"");
   DrawLabel(g,detail,font,palette.Ink,tx,125,info-25);
   DrawLabel(g,pair?"Double angle: mirrored pair; gap is illustrative. Actual orientation / gusset spacing must match the joint.":"Dimensions follow the selected catalog profile. Sharp-corner idealisation; bend radii are not shown.",font,palette.Muted,tx,245,info-25);
   DrawLabel(g,"View only — zoom and pan do not change design inputs.",font,palette.Muted,12,Height-23);
  }
 }
 static void DrawLabel(Graphics g,string s,Font f,Color c,float x,float y,float width=0){using(var brush=new SolidBrush(c)){if(width>0)g.DrawString(s,f,brush,new RectangleF(x,y,width,110));else g.DrawString(s,f,brush,x,y);}}
 static void Dim(Graphics g,PointF a,PointF b,string label,Font f,Color c,bool vertical){using(var pen=new Pen(c,1)){pen.StartCap=LineCap.ArrowAnchor;pen.EndCap=LineCap.ArrowAnchor;g.DrawLine(pen,a,b);}if(vertical)DrawLabel(g,label,f,c,a.X+4,(a.Y+b.Y)/2);else DrawLabel(g,label,f,c,a.X,a.Y+4);}
}
}
