using System;using System.Drawing;using System.Runtime.InteropServices;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Search box in the Bill of Quantities toolbar (owner, 17 November 2026; moved from its own row).
partial class ProgramOfWorksForm {
 TextBox boqSearch;
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern IntPtr SendMessage(IntPtr hWnd,int msg,IntPtr wParam,string lParam);
 void AddBoqSearch(FlowLayoutPanel toolbar){
  /* Same 38 px row height as the toolbar buttons: the text box is centred vertically in a bordered box and the clear
     action is a small link-style label, so the search lines up with the buttons beside it. */
  var box=new Panel{Width=330,Height=38,Margin=new Padding(14,2,7,2),Padding=new Padding(8,0,4,0),BackColor=ModernUi.Input,BorderStyle=BorderStyle.FixedSingle};
  boqSearch=new TextBox{Name="BoqSearch",AccessibleName="Search Bill of Quantities",BorderStyle=BorderStyle.None,Font=ModernUi.Body,BackColor=ModernUi.Input,Anchor=AnchorStyles.Left|AnchorStyles.Right};
  /* Owner (17 November 2026): the plain "✕" glyph looked rough. The clear button is now a small painted round chip,
     shown only while there is search text, darker on hover. */
  var clear=new Panel{Dock=DockStyle.Right,Width=28,Cursor=Cursors.Hand,AccessibleName="Clear search",AccessibleRole=AccessibleRole.PushButton,BackColor=ModernUi.Input,Visible=false};
  bool hover=false;clear.MouseEnter+=(s,e)=>{hover=true;clear.Invalidate();};clear.MouseLeave+=(s,e)=>{hover=false;clear.Invalidate();};
  clear.Paint+=(s,e)=>{var g=e.Graphics;g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;int d=16;var r=new Rectangle((clear.Width-d)/2,(clear.Height-d)/2,d,d);
   using(var fill=new SolidBrush(hover?PowTheme.Ink:Color.FromArgb(150,PowTheme.Muted)))g.FillEllipse(fill,r);
   using(var pen=new Pen(ModernUi.Input,1.8f){StartCap=System.Drawing.Drawing2D.LineCap.Round,EndCap=System.Drawing.Drawing2D.LineCap.Round}){int c=r.Left+d/2,m=r.Top+d/2,k=4;g.DrawLine(pen,c-k,m-k,c+k,m+k);g.DrawLine(pen,c-k,m+k,c+k,m-k);}};
  box.Resize+=(s,e)=>{boqSearch.Width=Math.Max(60,box.ClientSize.Width-box.Padding.Left-clear.Width-6);boqSearch.Location=new Point(box.Padding.Left,Math.Max(0,(box.ClientSize.Height-boqSearch.Height)/2));};
  var tips=new ToolTip();tips.SetToolTip(boqSearch,"Search item no. or description words, e.g. \"900 concrete\" or \"1032 painting\". Esc clears.");tips.SetToolTip(clear,"Clear search");Disposed+=(s,e)=>tips.Dispose();
  boqSearch.HandleCreated+=(s,e)=>{try{SendMessage(boqSearch.Handle,0x1501,(IntPtr)1,"Search item no. / description");}catch(Exception){}};
  boqSearch.TextChanged+=(s,e)=>{clear.Visible=boqSearch.Text.Length>0;if(boqDisclosure==null)return;boqDisclosure.Filter=boqSearch.Text;boqDisclosure.Apply();};
  boqSearch.KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Escape){boqSearch.Clear();e.Handled=e.SuppressKeyPress=true;}};
  clear.Click+=(s,e)=>{boqSearch.Clear();boqSearch.Focus();};
  box.Controls.Add(clear);box.Controls.Add(boqSearch);toolbar.Controls.Add(box);boqSearch.Location=new Point(box.Padding.Left,(box.ClientSize.Height-boqSearch.Height)/2);boqSearch.Width=box.ClientSize.Width-box.Padding.Left-clear.Width-6;}
}
}
