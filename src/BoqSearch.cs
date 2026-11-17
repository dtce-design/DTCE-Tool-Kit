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
  var clear=new Label{Text="✕",Dock=DockStyle.Right,Width=26,TextAlign=ContentAlignment.MiddleCenter,Cursor=Cursors.Hand,ForeColor=PowTheme.Muted,AccessibleName="Clear search",BackColor=ModernUi.Input};
  box.Resize+=(s,e)=>{boqSearch.Width=Math.Max(60,box.ClientSize.Width-box.Padding.Left-clear.Width-6);boqSearch.Location=new Point(box.Padding.Left,Math.Max(0,(box.ClientSize.Height-boqSearch.Height)/2));};
  var tips=new ToolTip();tips.SetToolTip(boqSearch,"Search item no. or description words, e.g. \"900 concrete\" or \"1032 painting\". Esc clears.");tips.SetToolTip(clear,"Clear search");Disposed+=(s,e)=>tips.Dispose();
  boqSearch.HandleCreated+=(s,e)=>{try{SendMessage(boqSearch.Handle,0x1501,(IntPtr)1,"Search item no. / description");}catch(Exception){}};
  boqSearch.TextChanged+=(s,e)=>{if(boqDisclosure==null)return;boqDisclosure.Filter=boqSearch.Text;boqDisclosure.Apply();};
  boqSearch.KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Escape){boqSearch.Clear();e.Handled=e.SuppressKeyPress=true;}};
  clear.Click+=(s,e)=>{boqSearch.Clear();boqSearch.Focus();};
  box.Controls.Add(clear);box.Controls.Add(boqSearch);toolbar.Controls.Add(box);boqSearch.Location=new Point(box.Padding.Left,(box.ClientSize.Height-boqSearch.Height)/2);boqSearch.Width=box.ClientSize.Width-box.Padding.Left-clear.Width-6;}
}
}
