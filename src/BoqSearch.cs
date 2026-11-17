using System;using System.Drawing;using System.Runtime.InteropServices;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Search box in the Bill of Quantities toolbar (owner, 17 November 2026; moved from its own row).
partial class ProgramOfWorksForm {
 TextBox boqSearch;
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern IntPtr SendMessage(IntPtr hWnd,int msg,IntPtr wParam,string lParam);
 void AddBoqSearch(FlowLayoutPanel toolbar){
  var box=new FlowLayoutPanel{AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,WrapContents=false,Margin=new Padding(18,3,3,3),Padding=Padding.Empty,BackColor=Color.Transparent};
  boqSearch=new TextBox{Name="BoqSearch",AccessibleName="Search Bill of Quantities",Width=300,Font=ModernUi.Body,Margin=new Padding(0,6,4,0)};
  var clear=new Button{Text="✕",Width=34,Height=boqSearch.Height+4,Margin=new Padding(0,5,0,0),AccessibleName="Clear search"};
  var tips=new ToolTip();tips.SetToolTip(boqSearch,"Search item no. or description words, e.g. \"900 concrete\" or \"1032 painting\". Esc clears.");tips.SetToolTip(clear,"Clear search");Disposed+=(s,e)=>tips.Dispose();
  boqSearch.HandleCreated+=(s,e)=>{try{SendMessage(boqSearch.Handle,0x1501,(IntPtr)1,"Search item no. / description");}catch(Exception){}};
  boqSearch.TextChanged+=(s,e)=>{if(boqDisclosure==null)return;boqDisclosure.Filter=boqSearch.Text;boqDisclosure.Apply();};
  boqSearch.KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Escape){boqSearch.Clear();e.Handled=e.SuppressKeyPress=true;}};
  clear.Click+=(s,e)=>{boqSearch.Clear();boqSearch.Focus();};
  box.Controls.Add(boqSearch);box.Controls.Add(clear);toolbar.Controls.Add(box);}
}
}
