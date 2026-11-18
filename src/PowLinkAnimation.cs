using System;using System.Drawing;using System.Drawing.Drawing2D;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Animated "POW -> Excel" link shown in the POW status bar while a workbook is linking or quantities are syncing
// (owner request 17 November 2026). Work continues in the background; this is only a visible activity cue.
sealed class LinkActivityIndicator:Control {
 readonly Timer timer=new Timer{Interval=40};int frame;
 internal LinkActivityIndicator(){SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.UserPaint|ControlStyles.ResizeRedraw,true);timer.Tick+=(s,e)=>{frame=(frame+1)%1000;Invalidate();};Visible=false;}
 internal bool Active{get{return Visible;}set{if(Visible==value)return;Visible=value;if(value){frame=0;timer.Start();}else timer.Stop();}}
 protected override void Dispose(bool disposing){if(disposing)timer.Dispose();base.Dispose(disposing);}
 protected override void OnPaint(PaintEventArgs e){
  var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(BackColor);
  float h=Math.Min(ClientSize.Height-4,20f),y=(ClientSize.Height-h)/2f,boxW=38f;
  var left=new RectangleF(2,y,boxW,h);var right=new RectangleF(ClientSize.Width-boxW-2,y,boxW,h);
  using(var font=new Font("Segoe UI",7f,FontStyle.Bold))using(var pow=new SolidBrush(PowTheme.Teal))using(var excel=new SolidBrush(Color.FromArgb(33,115,70)))using(var white=new SolidBrush(Color.White)){
   g.FillRectangle(pow,left);g.FillRectangle(excel,right);
   var center=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center};
   g.DrawString("POW",font,white,left,center);g.DrawString("Excel",font,white,right,center);}
  float x0=left.Right+6,x1=right.Left-6,mid=y+h/2f;if(x1<=x0)return;
  using(var track=new Pen(PowTheme.Line,1.5f){DashStyle=DashStyle.Dot})g.DrawLine(track,x0,mid,x1,mid);
  // Three dots travel from POW to Excel; each fades in and out along the path.
  for(int i=0;i<3;i++){float t=((frame*0.02f)+i/3f)%1f;float x=x0+(x1-x0)*t;int alpha=(int)(255*Math.Sin(Math.PI*t));
   using(var dot=new SolidBrush(Color.FromArgb(Math.Max(40,alpha),PowTheme.Teal)))g.FillEllipse(dot,x-3.5f,mid-3.5f,7,7);}
 }
}
partial class ProgramOfWorksForm {
 LinkActivityIndicator linkActivity;Timer linkActivityTimer;
 void InstallLinkAnimation(Panel bar){
  linkActivity=new LinkActivityIndicator{Name="ExcelLinkActivity",AccessibleName="Excel link activity",Dock=DockStyle.Right,Width=170,BackColor=bar.BackColor};
  bar.Controls.Add(linkActivity);
  linkActivityTimer=new Timer{Interval=150};linkActivityTimer.Tick+=(s,e)=>{if(linkActivity.IsDisposed)return;linkActivity.Active=linkingExcel||capturingExcelSource||syncingExcel||disconnectingExcel;};linkActivityTimer.Start();
  Disposed+=(s,e)=>linkActivityTimer.Dispose();
 }
}
}
