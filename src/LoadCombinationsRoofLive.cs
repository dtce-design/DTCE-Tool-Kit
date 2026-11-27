using System;using System.Collections.Generic;using System.Drawing;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* the roof live load tab (owner, 27 November 2026: "bat walang tab for roof live load?"): the Lr chosen for each roof of the STAAD model
   (Table 205-3), a plan of every roof level coloured by Lr, and the LLR text that goes into the STAAD input */
partial class LoadCombinationsForm {
 readonly Label roofLiveStatus=new Label{Dock=DockStyle.Top,Height=58,Padding=new Padding(10,8,10,4),ForeColor=PowTheme.Ink};
 readonly DataGridView roofLiveGrid=new DataGridView{Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,BackgroundColor=Color.White};
 readonly FlowLayoutPanel roofLivePlans=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoScroll=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,BackColor=Color.White,Padding=new Padding(8)};
 readonly TextBox roofLiveText=new TextBox{Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Both,WordWrap=false,Font=new Font("Consolas",9.5f),BackColor=Color.White};
 string roofLiveShown="\u0001";
 void AddRoofLivePage(){var page=new TabPage("Roof live load (LLR)"){BackColor=Color.White};var sub=new NavigationTabs{Dock=DockStyle.Fill};
  var t1=new TabPage("Lr per roof"){BackColor=Color.White};t1.Controls.Add(roofLiveGrid);var t2=new TabPage("Roof plans"){BackColor=Color.White};t2.Controls.Add(roofLivePlans);
  var t3=new TabPage("STAAD LLR load"){BackColor=Color.White};var copy=new Button{Text="Copy LLR",AutoSize=true};copy.Click+=(s,e)=>{if(roofLiveText.Text.Length>0)Clipboard.SetText(roofLiveText.Text);};
  var bar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=44,Padding=new Padding(6)};bar.Controls.Add(copy);t3.Controls.Add(roofLiveText);t3.Controls.Add(bar);
  sub.TabPages.Add(t1);sub.TabPages.Add(t2);sub.TabPages.Add(t3);page.Controls.Add(sub);page.Controls.Add(roofLiveStatus);AddResultPage(page);
  foreach(var h in new[]{"Roof","Where","Basis (tributary area, slope)","Table 205-3 row","Lr (kPa)"})roofLiveGrid.Columns.Add(h,h);roofLiveGrid.Columns[2].FillWeight=220;GridAppearance.Apply(roofLiveGrid);}
 /* after each Refresh0: fills the tab from the last roof live load computed (RoofLiveText in Settings) */
 void UpdateRoofLivePage(){string key=On("RoofLive")+"|"+On("AutoPrimary")+"|"+roofLiveCache;if(key==roofLiveShown)return;roofLiveShown=key;
  roofLiveGrid.Rows.Clear();foreach(Control c in roofLivePlans.Controls.Cast<Control>().ToList()){var pb=c as PictureBox;if(pb!=null&&pb.Image!=null)pb.Image.Dispose();c.Dispose();}roofLivePlans.Controls.Clear();
  roofLiveText.Text=(roofLiveCache??"").Replace("\r","").Replace("\n","\r\n");
  if(!On("RoofLive")){roofLiveStatus.Text="The roof live load case LLR is off (Settings: Roof live load case LLR).";return;}
  if(!On("AutoPrimary")){roofLiveStatus.Text="Turn on Automatic primary loads to put the roof live load into the LLR case.";return;}
  if(string.IsNullOrEmpty(roofLiveCache)){roofLiveStatus.Text="Roof live load: "+(roofLiveNote==""?"paste the STAAD model (Nodes + Beams tables or the .std) on the Primary loads tab.":roofLiveNote);return;}
  roofLiveStatus.Text="NSCP 2015 Section 205.4, Table 205-3 (Method 1): Lr vertical on the horizontal projection, by roof slope and tributary area; the roofs are found on every level of the model. "+roofLiveNote;
  foreach(var r in RoofLiveRegions)roofLiveGrid.Rows.Add(r.Kind=="Truss"?"Trussed roof":"Slab roof",r.Where,r.Basis,r.Row,StaadCombinations.F(r.Lr));
  foreach(var y in RoofLevels()){var lv=y;float w=1000,h=RoofPlanHeight(lv,w);var bmp=new Bitmap((int)w+20,(int)h+20);using(var g=Graphics.FromImage(bmp)){g.Clear(Color.White);g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;DrawRoofPlan(g,new RectangleF(10,10,w,h),lv,12);}
   roofLivePlans.Controls.Add(new PictureBox{Image=bmp,SizeMode=PictureBoxSizeMode.AutoSize,Margin=new Padding(0,0,0,12),BorderStyle=BorderStyle.FixedSingle});}}
 internal static List<double> RoofLevels(){return RoofLiveLoads.LastPanels.Select(p=>p[0]).Concat(RoofLiveLoads.LastChords.Select(c=>c[0])).Distinct().OrderBy(v=>v).ToList();}
 static readonly double[] LrValues={1.0,0.75,0.70,0.60};
 static Color LrColor(double lr){return Math.Abs(lr-1.0)<1e-9?Color.FromArgb(196,74,52):Math.Abs(lr-0.75)<1e-9?Color.FromArgb(222,148,58):Math.Abs(lr-0.70)<1e-9?Color.FromArgb(206,176,70):Color.FromArgb(78,140,170);}
 /* the whole model's plan extent (all roofs), so every level is drawn at the same scale and position */
 static RectangleF Extent(){var xs=RoofLiveLoads.LastPanels.SelectMany(p=>new[]{p[1],p[2]}).Concat(RoofLiveLoads.LastChords.SelectMany(c=>new[]{c[1],c[3]})).ToList();var zs=RoofLiveLoads.LastPanels.SelectMany(p=>new[]{p[3],p[4]}).Concat(RoofLiveLoads.LastChords.SelectMany(c=>new[]{c[2],c[4]})).ToList();
  if(xs.Count==0)return new RectangleF(0,0,1,1);return RectangleF.FromLTRB((float)xs.Min(),(float)zs.Min(),(float)Math.Max(xs.Max(),xs.Min()+0.1),(float)Math.Max(zs.Max(),zs.Min()+0.1));}
 internal static float RoofPlanHeight(double y,float width){var e=Extent();float plotW=width-40;float s=Math.Min(plotW/e.Width,width*0.5f/e.Height);return 64+e.Height*s+40;}
 internal static void DrawRoofPlan(Graphics g,RectangleF rc,double y,float fs){var e=Extent();float plotW=rc.Width-40;float s=Math.Min(plotW/e.Width,rc.Width*0.5f/e.Height);float x0=rc.X+(rc.Width-e.Width*s)/2,y0=rc.Y+64;
  Func<double,double,PointF> P=(x,z)=>new PointF(x0+(float)(x-e.Left)*s,y0+(float)(z-e.Top)*s);g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
  var panels=RoofLiveLoads.LastPanels.Where(p=>Math.Abs(p[0]-y)<1e-6).ToList();var chords=RoofLiveLoads.LastChords.Where(c=>Math.Abs(c[0]-y)<1e-6).ToList();
  using(var tf=new Font("Segoe UI",fs*1.15f,FontStyle.Bold,GraphicsUnit.Pixel))using(var ink=new SolidBrush(PowTheme.Ink))g.DrawString("Roof at level "+StaadCombinations.F(y)+" m"+(panels.Count>0?" - slab roof panels (FLOOR LOAD)":"")+(chords.Count>0?(panels.Count>0?" and":" -")+" truss top chords (UNI PY)":""),tf,ink,rc.X,rc.Y);
  /* legend: the Lr values used on this level */
  float lx=rc.X;using(var lf=new Font("Segoe UI",fs,GraphicsUnit.Pixel))using(var ink=new SolidBrush(PowTheme.Ink))foreach(var v in LrValues.Where(v=>panels.Any(p=>Math.Abs(p[5]-v)<1e-9)||chords.Any(c=>Math.Abs(c[5]-v)<1e-9))){
   using(var b=new SolidBrush(LrColor(v)))g.FillRectangle(b,lx,rc.Y+fs*2.2f,fs*1.4f,fs);string t="Lr = "+StaadCombinations.F(v)+" kPa";g.DrawString(t,lf,ink,lx+fs*1.7f,rc.Y+fs*2.0f);lx+=fs*2.4f+g.MeasureString(t,lf).Width;}
  /* outline of the whole model's roofs in grey for context */
  using(var grey=new SolidBrush(Color.FromArgb(242,244,243)))using(var pen=new Pen(Color.FromArgb(200,206,204),1))foreach(var p in RoofLiveLoads.LastPanels.Where(q=>Math.Abs(q[0]-y)>1e-6)){var a=P(p[1],p[3]);var b=P(p[2],p[4]);g.FillRectangle(grey,a.X,a.Y,b.X-a.X,b.Y-a.Y);}
  foreach(var p in panels){var a=P(p[1],p[3]);var b=P(p[2],p[4]);using(var br=new SolidBrush(Color.FromArgb(150,LrColor(p[5]))))g.FillRectangle(br,a.X,a.Y,b.X-a.X,b.Y-a.Y);using(var pen=new Pen(Color.FromArgb(90,96,94),1))g.DrawRectangle(pen,a.X,a.Y,b.X-a.X,b.Y-a.Y);
   using(var lf=new Font("Segoe UI",fs*0.95f,FontStyle.Bold,GraphicsUnit.Pixel))using(var ink=new SolidBrush(PowTheme.Ink)){string t=StaadCombinations.F(p[5])+" kPa";var sz=g.MeasureString(t,lf);if(sz.Width<b.X-a.X-4&&sz.Height<b.Y-a.Y-4)g.DrawString(t,lf,ink,(a.X+b.X-sz.Width)/2,(a.Y+b.Y-sz.Height)/2);}}
  /* covered by a higher roof: say where its load is */
  using(var cf=new Font("Segoe UI",fs*0.85f,FontStyle.Italic,GraphicsUnit.Pixel))using(var mut=new SolidBrush(Color.FromArgb(120,126,124)))foreach(var p in RoofLiveLoads.LastPanels.Where(q=>q[0]>y+1e-6)){var a=P(p[1],p[3]);var b=P(p[2],p[4]);string t="roof at "+StaadCombinations.F(p[0])+" m";var sz=g.MeasureString(t,cf);if(!panels.Any(q=>q[1]<p[2]-1e-6&&q[2]>p[1]+1e-6&&q[3]<p[4]-1e-6&&q[4]>p[3]+1e-6)&&sz.Width<b.X-a.X-4)g.DrawString(t,cf,mut,(a.X+b.X-sz.Width)/2,(a.Y+b.Y-sz.Height)/2);}
  foreach(var c in chords){using(var pen=new Pen(LrColor(c[5]),Math.Max(3,fs*0.35f)))g.DrawLine(pen,P(c[1],c[2]),P(c[3],c[4]));}
  using(var f=new Font("Segoe UI",fs*0.9f,GraphicsUnit.Pixel))using(var ink=new SolidBrush(Color.FromArgb(90,96,94))){var b0=P(e.Left,e.Bottom);g.DrawString("X →   Z ↓   (top view of the STAAD model, m)",f,ink,b0.X,b0.Y+6);}}
}
}
