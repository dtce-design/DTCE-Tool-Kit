using System;using System.Drawing;using System.Runtime.InteropServices;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Search box in the Bill of Quantities toolbar (owner, 17 November 2026; moved from its own row).
partial class ProgramOfWorksForm {
 TextBox boqSearch;
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern IntPtr SendMessage(IntPtr hWnd,int msg,IntPtr wParam,string lParam);
 void AddBoqSearch(FlowLayoutPanel toolbar){/* toolbar: the strip above the BOQ table (v1.19.0) */
  /* Same 38 px row height as the toolbar buttons: the text box is centred vertically in a bordered box and the clear
     action is a small link-style label, so the search lines up with the buttons beside it. */
  var box=new Panel{Width=340,Height=38,Margin=new Padding(0,2,0,2),Padding=new Padding(30,3,8,3),BackColor=ModernUi.Input,BorderStyle=BorderStyle.None};/* v1.19.4: the clear button sits inside the border (it covered the border at the right end) */
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
  /* Owner (18 November 2026: "yung search bar di nagbago"): the app theme re-styled the embedded text box with its
     own border and recoloured the container afterwards, so the earlier fix never showed. The search keeps its own
     look: one rounded border painted by the box (accent while typing), a magnifier icon, borderless text inside. */
  Action keep=()=>{if(boqSearch.BorderStyle!=BorderStyle.None)boqSearch.BorderStyle=BorderStyle.None;if(box.BackColor!=ModernUi.Input)box.BackColor=ModernUi.Input;if(boqSearch.BackColor!=ModernUi.Input)boqSearch.BackColor=ModernUi.Input;if(clear.BackColor!=ModernUi.Input)clear.BackColor=ModernUi.Input;};
  boqSearch.BorderStyleChanged+=(s,e)=>keep();box.BackColorChanged+=(s,e)=>keep();boqSearch.BackColorChanged+=(s,e)=>keep();clear.BackColorChanged+=(s,e)=>keep();box.ParentChanged+=(s,e)=>keep();
  boqSearch.GotFocus+=(s,e)=>box.Invalidate();boqSearch.LostFocus+=(s,e)=>box.Invalidate();
  box.Paint+=(s,e)=>{keep();var g=e.Graphics;g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
   using(var path=ModernUi.Rounded(new RectangleF(.5f,.5f,box.Width-1,box.Height-1),4))using(var pen=new Pen(boqSearch.Focused?WorksheetColors.Focus:PowTheme.Line,boqSearch.Focused?1.6f:1f))g.DrawPath(pen,path);
   int cy=box.Height/2;using(var lens=new Pen(PowTheme.Muted,1.7f){EndCap=System.Drawing.Drawing2D.LineCap.Round}){g.DrawEllipse(lens,10,cy-7,10,10);g.DrawLine(lens,18.5f,cy+1.5f,23,cy+6);}};
  box.Click+=(s,e)=>boqSearch.Focus();
  box.Controls.Add(clear);box.Controls.Add(boqSearch);toolbar.Controls.Add(box);boqSearch.Location=new Point(box.Padding.Left,(box.ClientSize.Height-boqSearch.Height)/2);boqSearch.Width=box.ClientSize.Width-box.Padding.Left-clear.Width-6;}
}
}
