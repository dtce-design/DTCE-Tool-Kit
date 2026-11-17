using System;using System.Drawing;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Search bar above the Bill of Quantities (owner, 17 November 2026).
partial class ProgramOfWorksForm {
 TextBox boqSearch;
 void AddBoqSearch(TabPage page){
  var bar=new Panel{Dock=DockStyle.Top,Height=40,Padding=new Padding(16,6,16,4),BackColor=Color.White};
  var label=new Label{Text="Search:",Dock=DockStyle.Left,Width=64,TextAlign=ContentAlignment.MiddleLeft,Font=ModernUi.Body};
  boqSearch=new TextBox{Name="BoqSearch",AccessibleName="Search Bill of Quantities",Dock=DockStyle.Left,Width=420,Font=ModernUi.Body};
  var clear=new Button{Text="Clear",Dock=DockStyle.Left,Width=80};
  var hint=new Label{Text="  item no. or description words, e.g. \"900 concrete\" or \"1032 painting\"",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,ForeColor=PowTheme.Muted,Font=ModernUi.Body};
  boqSearch.TextChanged+=(s,e)=>{if(boqDisclosure==null)return;boqDisclosure.Filter=boqSearch.Text;boqDisclosure.Apply();};
  boqSearch.KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Escape){boqSearch.Clear();e.Handled=e.SuppressKeyPress=true;}};
  clear.Click+=(s,e)=>{boqSearch.Clear();boqSearch.Focus();};
  bar.Controls.Add(hint);bar.Controls.Add(clear);bar.Controls.Add(boqSearch);bar.Controls.Add(label);page.Controls.Add(bar);}
}
}
