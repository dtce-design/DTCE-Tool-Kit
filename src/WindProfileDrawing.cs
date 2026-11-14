using System;using System.Linq;using System.Drawing;using System.Drawing.Drawing2D;using System.Globalization;
namespace BeamScheduleFormatter {
static class WindProfileDrawing {
 public static void Draw(Graphics g,RectangleF bounds,WindDirection data){var state=g.Save();try{g.SetClip(bounds);g.TranslateTransform(bounds.X,bounds.Y);float width=bounds.Width,height=bounds.Height;g.SmoothingMode=SmoothingMode.AntiAlias;
  if(data==null){CalculationDocument.Text(g,"Load terrain to show the elevation profile.",12,12,width-24,30);return;}
  CalculationDocument.Text(g,data.Name+" wind from · Exposure "+data.Exposure+" · "+data.Shape+" · Kzt "+(WindTin.Finite(data.Kzt)?data.Kzt.ToString("0.###"):"unresolved"),10,6,width-20,30,9,true);
  var points=data.Profile.Where(p=>WindTin.Finite(p.Z)).ToList();if(points.Count<2)return;
  double min=points.Min(p=>p.Z),max=points.Max(p=>p.Z);double pad=Math.Max(1,(max-min)*.12);min-=pad;max+=pad;
  float left=48,top=44,w=Math.Max(50,width-66),h=Math.Max(50,height-128);double first=points.First().D,last=points.Last().D;if(last<=first)return;
  Func<double,float> x=v=>left+(float)((v-first)/(last-first)*w),y=v=>top+h-(float)((v-min)/(max-min)*h);
  for(int i=0;i<=4;i++){double z=min+(max-min)*i/4;using(var pen=new Pen(PowTheme.Line,.5f))g.DrawLine(pen,left,y(z),left+w,y(z));CalculationDocument.Text(g,z.ToString("0.#",CultureInfo.InvariantCulture),0,y(z)-5,46,14,7);}
  for(int i=1;i<points.Count;i++){if(points[i].D-points[i-1].D>150)continue;Color color=WindColors.Cover(points[i-1].Cover);using(var fill=new SolidBrush(Color.FromArgb(28,color)))g.FillPolygon(fill,new[]{new PointF(x(points[i-1].D),top+h),new PointF(x(points[i-1].D),y(points[i-1].Z)),new PointF(x(points[i].D),y(points[i].Z)),new PointF(x(points[i].D),top+h)});using(var pen=new Pen(color,2))g.DrawLine(pen,x(points[i-1].D),y(points[i-1].Z),x(points[i].D),y(points[i].Z));}
  using(var pen=new Pen(PowTheme.Ink,1){DashStyle=DashStyle.Dash})g.DrawLine(pen,x(0),top,x(0),top+h);
  CalculationDocument.Text(g,"SITE",x(0)+3,top,45,16,7,true);
  for(int i=0;i<=4;i++){double d=first+(last-first)*i/4;CalculationDocument.Text(g,(d/1000).ToString("0.#",CultureInfo.InvariantCulture)+"km",x(d)-20,top+h+7,50,16,7);}
  CalculationDocument.Text(g,"Elevation (m) · +distance is upwind · B green / C gold / D blue / ? gray",10,height-60,width-20,22,7,false,PowTheme.Muted);
  CalculationDocument.Text(g,data.Status,10,height-35,width-20,32,7,false,PowTheme.Muted);
 }finally{g.Restore(state);}}
}
}
