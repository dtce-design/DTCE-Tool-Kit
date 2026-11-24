using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Drawing2D;using System.Linq;
namespace BeamScheduleFormatter {
/* The mapped surface roughness around the site drawn as a circle for the report (owner, 24 November 2026: "ipakita din yung diagram for
   surface roughness"; "siguro sa circle or sa map"): the roughness cells inside the assessment radius in clear colours, the eight 45°
   sectors with their exposure, the B fetch and D fetch + transition rings, north and a scale bar. With a highlighted direction only its
   two upwind sectors (−45° to +45°) stay bright and its five rays are drawn. */
static class WindRoughnessCircle {
 static PointF At(PointF c,double bearing,float r){double a=bearing*Math.PI/180;return new PointF(c.X+(float)(Math.Sin(a)*r),c.Y-(float)(Math.Cos(a)*r));}
 internal static void Draw(Graphics g,RectangleF rc,WindTerrain t,List<WindDirection> directions,int highlight,double height,bool labels){
  if(t==null)return;float R=Math.Min(rc.Width,rc.Height)/2-(labels?22:6);var c=new PointF(rc.Left+rc.Width/2,rc.Top+rc.Height/2);double s=R/t.Radius;
  using(var f=new Font("Segoe UI",7f,FontStyle.Regular,GraphicsUnit.Point))using(var b=new Font("Segoe UI",7f,FontStyle.Bold,GraphicsUnit.Point))using(var ink=new SolidBrush(PowTheme.Ink)){
   var state=g.Save();using(var clip=new GraphicsPath()){clip.AddEllipse(c.X-R,c.Y-R,2*R,2*R);g.SetClip(clip,CombineMode.Intersect);
    using(var bg=new SolidBrush(WindColors.Roughness("?")))g.FillRectangle(bg,c.X-R,c.Y-R,2*R,2*R);
    var grid=t.CoverGrid;double cos=Math.Cos(t.Lat*Math.PI/180);
    if(grid!=null&&grid.Size>0){double dLat=(grid.North-grid.South)/grid.Size,dLon=(grid.East-grid.West)/grid.Size;float cw=(float)(dLon*cos*111320*s)+.6f,ch=(float)(dLat*110540*s)+.6f;
     var brushes=new Dictionary<int,SolidBrush>();try{for(int row=0;row<grid.Size;row++)for(int col=0;col<grid.Size;col++){int code=grid.Codes[row*grid.Size+col];SolidBrush br;if(!brushes.TryGetValue(code,out br)){br=new SolidBrush(WindColors.Cover(code));brushes[code]=br;}
       double lat=grid.North-(row+.5)*dLat,lon=grid.West+(col+.5)*dLon;float x=c.X+(float)((lon-t.Lon)*cos*111320*s),y=c.Y-(float)((lat-t.Lat)*110540*s);g.FillRectangle(br,x-cw/2,y-ch/2,cw,ch);}}
     finally{foreach(var br in brushes.Values)br.Dispose();}}
    else{/* no cell grid saved: the ray samples as wedges */for(int n=0;n<t.Rays.Length;n++){double brg=n/3*45+(n%3-1)*22.5;var ray=t.Rays[n];for(int i=0;i<ray.Count;i++){float r0=(float)(ray[i].D*s),r1=(float)((i+1<ray.Count?ray[i+1].D:ray[i].D+100)*s);using(var br=new SolidBrush(WindColors.Cover(ray[i].Cover)))using(var path=new GraphicsPath()){path.AddArc(c.X-r1,c.Y-r1,2*r1,2*r1,(float)(brg-90-11.25),22.5f);if(r0>0)path.AddArc(c.X-r0,c.Y-r0,2*r0,2*r0,(float)(brg-90+11.25),-22.5f);else path.AddLine(c,c);path.CloseFigure();g.FillPath(br,path);}}}}
    if(highlight>=0){/* dim everything outside the two upwind sectors */using(var veil=new SolidBrush(Color.FromArgb(185,255,255,255)))g.FillPie(veil,c.X-R-1,c.Y-R-1,2*R+2,2*R+2,(float)(highlight*45-90+45),270f);}}
   g.Restore(state);
   using(var ring=new Pen(PowTheme.Ink,1f))g.DrawEllipse(ring,c.X-R,c.Y-R,2*R,2*R);
   using(var thin=new Pen(Color.FromArgb(150,40,52,64),.6f))for(int i=0;i<8;i++)g.DrawLine(thin,c,At(c,i*45+22.5,R));
   if(highlight>=0)using(var rayPen=new Pen(Color.FromArgb(30,40,50),1f))using(var edge=new Pen(PowTheme.Ink,1.4f)){for(int k=-2;k<=2;k++)g.DrawLine(rayPen,c,At(c,highlight*45+k*22.5,R));g.DrawLine(edge,c,At(c,highlight*45-45,R));g.DrawLine(edge,c,At(c,highlight*45+45,R));}
   if(WindTin.Finite(height)&&height>0)using(var dash=new Pen(Color.FromArgb(40,52,64),.8f){DashStyle=DashStyle.Dash})foreach(var d in new[]{WindAssessment.BFetch(height),WindAssessment.DFetch(height)+WindAssessment.DTransition(height)}.Distinct()){if(d>=t.Radius)continue;float r=(float)(d*s);g.DrawEllipse(dash,c.X-r,c.Y-r,2*r,2*r);if(labels){var txt=(d==WindAssessment.BFetch(height)?"B fetch ":"D fetch + transition ")+d.ToString("0")+" m";g.DrawString(txt,f,ink,c.X+3,c.Y-r+1);}}
   using(var site=new SolidBrush(Color.FromArgb(30,40,50)))g.FillEllipse(site,c.X-3,c.Y-3,6,6);
   /* north and the direction labels */
   if(!labels){var n0=At(c,0,R+4);g.DrawString("N",b,ink,n0.X-4,n0.Y-10);}
   if(labels&&directions!=null)for(int i=0;i<8&&i<directions.Count;i++){var p=At(c,i*45,R+12);string txt=directions[i].Name+" "+directions[i].Exposure;var sz=g.MeasureString(txt,b);g.DrawString(txt,b,ink,p.X-sz.Width/2,p.Y-sz.Height/2);}
   /* scale bar */
   if(labels){double bar=t.Radius>2500?1000:500;float bw=(float)(bar*s);float bx=rc.Left+4,by=rc.Bottom-10;using(var p=new Pen(PowTheme.Ink,1.2f)){g.DrawLine(p,bx,by,bx+bw,by);g.DrawLine(p,bx,by-3,bx,by+3);g.DrawLine(p,bx+bw,by-3,bx+bw,by+3);}g.DrawString(bar.ToString("0")+" m",f,ink,bx+bw+4,by-6);}}}
}
}
