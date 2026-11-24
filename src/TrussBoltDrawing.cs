using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;
namespace BeamScheduleFormatter {
 /* Interactive bolt layout (owner, 23 November 2026: "asan den yung interactive niya?"): the connected angle leg over the gusset with the
    bolt row drawn to scale - end distance, spacing, gauge from the heel, edge to the toe and to the gusset edge - and the two block-shear
    paths (owner: "hindi ba yung block shear e sa bolt yun?"). Beside it the section through a bolt: a single angle on the gusset (single
    shear) or two angles back to back with the gusset between them (gap = gusset thickness, double shear). Click a label to edit it. */
 sealed class TrussBoltDrawing : Control {
  readonly IDictionary<string,Control> inputs;
  readonly Dictionary<string,RectangleF> targets=new Dictionary<string,RectangleF>();
  string active="Edge";
  static readonly Color Paper=Color.White,InkC=Color.FromArgb(40,52,66),Hot=Color.FromArgb(25,102,192),MemberFill=Color.FromArgb(205,218,232),GussetFill=Color.FromArgb(228,228,222),TearMember=Color.FromArgb(200,40,40),TearGusset=Color.FromArgb(205,120,20);
  public TrussBoltDrawing(IDictionary<string,Control> values,bool live=true){
   inputs=values;DoubleBuffered=true;ResizeRedraw=true;Height=470;Width=1000;Tag="FullWidth";if(!live)return;
   AccessibleName="Interactive bolt layout";AccessibleDescription="Click a dimension to edit its input.";
   foreach(string key in new[]{"Bolts","BoltSpacing","Edge","BoltGauge","GussetEdge","Diameter","Hole","GussetThickness","GussetWidth","Family","Profile","ShearPlanes","Thickness"}){
    if(!inputs.ContainsKey(key))continue;string selected=key;inputs[key].TextChanged+=(s,e)=>Invalidate();var combo=inputs[key] as ComboBox;if(combo!=null)combo.SelectedIndexChanged+=(s,e)=>Invalidate();
    inputs[key].Enter+=(s,e)=>{active=selected;Invalidate();};}
   MouseMove+=(s,e)=>{Cursor=Hit(e.Location)==null?Cursors.Default:Cursors.Hand;};
   MouseClick+=(s,e)=>{string key=Hit(e.Location);if(key==null)return;active=key;inputs[key].Focus();var text=inputs[key] as TextBox;if(text!=null)text.SelectAll();Invalidate();};
  }
  internal static void Render(IDictionary<string,Control> values,Graphics g,RectangleF rc){if(rc.Width<=1||rc.Height<=1)return;int w=1400,h=Math.Max(200,(int)(w*rc.Height/rc.Width));
   using(var d=new TrussBoltDrawing(values,false){Width=w,Height=h})using(var bmp=new Bitmap(w,h)){using(var bg=Graphics.FromImage(bmp)){bg.Clear(Color.White);d.Draw(bg);}var st=g.Save();g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(bmp,rc);g.Restore(st);}}
  string Hit(Point p){foreach(var pair in targets)if(pair.Value.Contains(p))return pair.Key;return null;}
  double Read(string key){double v;return inputs.ContainsKey(key)&&double.TryParse(inputs[key].Text,NumberStyles.Float,CultureInfo.InvariantCulture,out v)&&!double.IsNaN(v)&&!double.IsInfinity(v)&&v>=0?v:double.NaN;}
  static string N(double v){return v.ToString("0.#",CultureInfo.InvariantCulture);}
  protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(Paper);using(new ReportPaperScope())Draw(e.Graphics);}
  void Draw(Graphics g){
   g.SmoothingMode=SmoothingMode.AntiAlias;targets.Clear();
   using(var font=new Font("Segoe UI",8.5f))using(var bold=new Font("Segoe UI",10,FontStyle.Bold)){
    string family=inputs["Family"].Text;var p=SteelCatalog.Find(inputs["Profile"].Text);
    Text(g,"BOLT LAYOUT  |  "+family+(p!=null?"  |  "+p.Name:""),bold,InkC,12,8);
    Text(g,"Click a dimension to edit it. Drawn to scale; dimensions in mm.",font,Color.DimGray,12,30);
    if(family.StartsWith("HSS")){Text(g,"Bolted HSS connections are not covered by this sheet (the source workbooks weld HSS members). Use the Weld design tab.",font,Color.DarkOrange,12,60);return;}
    if(p==null){Text(g,"Select an angle profile on Member and demand.",font,Color.DarkOrange,12,60);return;}
    double leg=p.H,t=p.T,n=Read("Bolts"),s=Read("BoltSpacing"),e=Read("Edge"),gg=Read("BoltGauge"),ge=Read("GussetEdge"),db=Read("Diameter"),dh=Read("Hole"),tg=Read("GussetThickness"),wg=Read("GussetWidth");
    if(double.IsNaN(n+s+e+db+dh)||n<1){Text(g,"Enter the number of bolts, spacing, end distance and bolt / hole diameters.",font,Color.DarkOrange,12,60);return;}
    int bolts=(int)Math.Min(30,Math.Max(1,Math.Round(n)));bool autoGauge=double.IsNaN(gg)||gg<=0;double gauge=autoGauge?TrussConnectionDesigner.AutoGauge(leg):gg;
    double gEdge=double.IsNaN(ge)||ge<=0?(double.IsNaN(wg)?leg:wg/2):ge;
    double along=e+(bolts-1)*s,memberShown=along+Math.Max(2*e,leg*0.8),gussetShown=along+e+Math.Max(120,leg);
    /* elevation: the gusset to the left, the member leg coming in from the right; heel at the top, toe at the bottom */
    float areaW=Math.Max(300,Width*0.62f)-40,areaH=Height-150;double spanX=gussetShown+memberShown-along+40,spanY=Math.Max(leg,gauge+gEdge)+40;
    float sc=(float)Math.Min(areaW/spanX,(areaH-30)/spanY);float ox=40,oy=118+(float)(20*sc);
    Func<double,double,PointF> P=(x,y)=>new PointF(ox+(float)(x*sc),oy+(float)(y*sc));/* x along the member (0 = gusset left edge), y down from the heel */
    double xEnd=gussetShown-along-e;/* the member end (left), so the last bolt sits e from the gusset end */
    double yBolt=gauge,gBottom=gauge+gEdge;
    var gr=Rect(P(0,-20),P(gussetShown,gBottom));using(var b=new SolidBrush(GussetFill))g.FillRectangle(b,gr);using(var pen=new Pen(InkC,1))g.DrawRectangle(pen,gr.X,gr.Y,gr.Width,gr.Height);
    var mr=Rect(P(xEnd,0),P(xEnd+memberShown,leg));using(var b=new SolidBrush(Color.FromArgb(170,MemberFill)))g.FillRectangle(b,mr);using(var pen=new Pen(InkC,1.4f))g.DrawRectangle(pen,mr.X,mr.Y,mr.Width,mr.Height);
    using(var heel=new Pen(InkC,3))g.DrawLine(heel,P(xEnd,0),P(xEnd+memberShown,0));
    Text(g,"heel",font,InkC,P(xEnd+memberShown,0).X+4,P(0,0).Y-8);Text(g,"toe",font,InkC,P(xEnd+memberShown,leg).X+4,P(0,leg).Y-8);Text(g,"gusset edge",font,InkC,gr.X+4,gr.Bottom+2);Text(g,"member end",font,InkC,P(xEnd,0).X-30,P(0,0).Y-30);
    /* bolts and holes */
    using(var hole=new Pen(InkC,1))using(var boltB=new SolidBrush(Color.FromArgb(90,100,115)))for(int k=0;k<bolts;k++){double bx=xEnd+e+k*s;var c=P(bx,yBolt);float rh=(float)(dh*sc/2),rb=(float)(db*sc/2);g.FillEllipse(boltB,c.X-rb,c.Y-rb,2*rb,2*rb);g.DrawEllipse(hole,c.X-rh,c.Y-rh,2*rh,2*rh);}
    using(var axis=new Pen(Color.Gray,1){DashStyle=DashStyle.DashDot})g.DrawLine(axis,P(xEnd-15,yBolt),P(xEnd+memberShown,yBolt));
    /* block-shear tear lines: the member leg (end to the last bolt, then to the toe) and the gusset (last bolt to its end, then to its edge) */
    double xLast=xEnd+e+(bolts-1)*s,xFirst=xEnd+e;
    using(var tm=new Pen(TearMember,2.2f){DashStyle=DashStyle.Dash})g.DrawLines(tm,new[]{P(xEnd,yBolt),P(xLast,yBolt),P(xLast,leg)});
    using(var tgp=new Pen(TearGusset,2.2f){DashStyle=DashStyle.Dot})g.DrawLines(tgp,new[]{P(gussetShown,yBolt+dh*0.0),P(xFirst,yBolt),P(xFirst,gBottom)});
    /* dimensions */
    float dimY=P(0,-20).Y-8;
    Dim(g,font,"Edge","e = "+N(e),P(xEnd,0).X,P(xEnd+e,0).X,dimY,true);
    if(bolts>1)Dim(g,font,"BoltSpacing",(bolts-1)+" @ s = "+N(s),P(xEnd+e,0).X,P(xLast,0).X,dimY,true);
    Text(g,"n = "+bolts+" bolts",font,active=="Bolts"?Hot:InkC,P(xLast,0).X+10,dimY-8);targets["Bolts"]=new RectangleF(P(xLast,0).X+8,dimY-10,90,20);
    float dimX=P(xEnd+memberShown,0).X+40;
    VDim(g,font,"BoltGauge","g = "+N(gauge)+(autoGauge?" (auto)":""),P(0,0).Y,P(0,yBolt).Y,dimX);
    VDim(g,font,"BoltGauge","toe edge = "+N(leg-gauge),P(0,yBolt).Y,P(0,leg).Y,dimX+0.01f);
    VDim(g,font,"GussetEdge","to gusset edge = "+N(gEdge)+(double.IsNaN(ge)||ge<=0?" (auto)":""),P(0,yBolt).Y,P(0,gBottom).Y,P(xFirst,0).X-14,true);
    var lab=P(xFirst,yBolt);string bt="Ø"+N(db)+" bolt, hole "+N(dh);Text(g,bt,font,active=="Diameter"||active=="Hole"?Hot:InkC,lab.X-8,lab.Y+(float)(dh*sc/2)+4);targets["Diameter"]=new RectangleF(lab.X-8,lab.Y+(float)(dh*sc/2)+4,110,18);
    /* legend */
    float ly=Height-58;using(var tm=new Pen(TearMember,2.2f){DashStyle=DashStyle.Dash})g.DrawLine(tm,12,ly+7,42,ly+7);Text(g,"member block shear: along the bolt line to the member end, across to the toe",font,InkC,48,ly);
    using(var tgp=new Pen(TearGusset,2.2f){DashStyle=DashStyle.Dot})g.DrawLine(tgp,12,ly+27,42,ly+27);Text(g,"gusset block shear: along the bolt line, across to the gusset edge",font,InkC,48,ly+20);
    /* section through a bolt */
    bool pair=family=="Double angle";float sx=Math.Max(P(xEnd+memberShown,0).X+170,Width*0.68f),sw=Width-sx-20;if(sw<160)return;
    double tgv=double.IsNaN(tg)||tg<=0?6:tg,total=(pair?2*leg:leg)+tgv+40;float ss=(float)Math.Min(sw/total,(Height-190)/(leg+40));float cx=sx+sw/2,top=110;
    Text(g,"SECTION THROUGH A BOLT",bold,InkC,sx,80);
    Func<double,double,PointF> Q=(x,y)=>new PointF(cx+(float)(x*ss),top+(float)(y*ss));
    using(var gb=new SolidBrush(GussetFill))using(var mb=new SolidBrush(MemberFill))using(var pen=new Pen(InkC,1.2f)){
     var gp=Rect(Q(-tgv/2,-15),Q(tgv/2,leg+25));g.FillRectangle(gb,gp);g.DrawRectangle(pen,gp.X,gp.Y,gp.Width,gp.Height);
     foreach(int side in pair?new[]{1,-1}:new[]{1}){double x0=side*tgv/2;var v=Rect(Q(x0,0),Q(x0+side*t,leg));var h=Rect(Q(x0,0),Q(x0+side*leg,t));foreach(var r in new[]{v,h}){g.FillRectangle(mb,r);g.DrawRectangle(pen,r.X,r.Y,r.Width,r.Height);}}
     double reach=(pair?t:0)+tgv/2+t+12;var b0=Q(-(pair?tgv/2+t+10:tgv/2+8),gauge);var b1=Q(tgv/2+t+10,gauge);float half=Math.Max(1f,(float)(db*ss/2));using(var sb=new SolidBrush(Color.FromArgb(120,130,145)))using(var hb=new SolidBrush(Color.FromArgb(70,80,95))){g.FillRectangle(sb,b0.X,b0.Y-half,b1.X-b0.X,2*half);float hw=(float)(0.65*db*ss),hh=(float)(1.7*db*ss);g.FillRectangle(hb,b0.X-hw,b0.Y-hh/2,hw,hh);g.FillRectangle(hb,b1.X,b1.Y-hh/2,hw,hh);}
    }
    string planes=inputs.ContainsKey("ShearPlanes")?inputs["ShearPlanes"].Text:"1";
    Text(g,pair?"Two angles back to back; gap = gusset thickness "+N(tgv)+" mm\nBolt in double shear (shear planes = "+planes+")":"Single angle on the gusset\nBolt in single shear (shear planes = "+planes+")",font,planes==(pair?"2":"1")?InkC:Color.DarkOrange,sx,Q(0,leg+30).Y);
    targets["GussetThickness"]=Rect(Q(-tgv/2-4,-15),Q(tgv/2+4,leg+25));
   }
  }
  static RectangleF Rect(PointF a,PointF b){return new RectangleF(Math.Min(a.X,b.X),Math.Min(a.Y,b.Y),Math.Abs(b.X-a.X),Math.Abs(b.Y-a.Y));}
  void Dim(Graphics g,Font f,string key,string label,float x0,float x1,float y,bool above){Color c=active==key?Hot:InkC;using(var pen=new Pen(c,1)){g.DrawLine(pen,x0,y,x1,y);g.DrawLine(pen,x0,y-5,x0,y+5);g.DrawLine(pen,x1,y-5,x1,y+5);}var size=g.MeasureString(label,f);var box=new RectangleF((x0+x1)/2-size.Width/2,y-size.Height-1,size.Width,size.Height);using(var b=new SolidBrush(c))g.DrawString(label,f,b,box.Location);Union(key,RectangleF.Union(box,new RectangleF(x0,y-6,Math.Max(1,x1-x0),12)));}
  void VDim(Graphics g,Font f,string key,string label,float y0,float y1,float x,bool left=false){Color c=active==key?Hot:InkC;using(var pen=new Pen(c,1)){g.DrawLine(pen,x,y0,x,y1);g.DrawLine(pen,x-5,y0,x+5,y0);g.DrawLine(pen,x-5,y1,x+5,y1);}var size=g.MeasureString(label,f);var at=new PointF(left?x-size.Width-6:x+6,(y0+y1)/2-size.Height/2);using(var b=new SolidBrush(c))g.DrawString(label,f,b,at);Union(key,RectangleF.Union(new RectangleF(at,size),new RectangleF(x-6,Math.Min(y0,y1),12,Math.Abs(y1-y0))));}
  void Union(string key,RectangleF box){RectangleF old;targets[key]=targets.TryGetValue(key,out old)?RectangleF.Union(old,box):box;}
  static void Text(Graphics g,string s,Font f,Color c,float x,float y){using(var b=new SolidBrush(c))g.DrawString(s,f,b,x,y);}
 }
}
