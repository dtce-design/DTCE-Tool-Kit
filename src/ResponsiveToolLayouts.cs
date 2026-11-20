using System;using System.Drawing;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
partial class MainForm {
 void UseSlabFormatterLayout(){var originalTable=(TabPage)grid.Parent;tabs.Dock=DockStyle.Fill;tabs.TabPages[1].Text="DXF preview";detail.Height=92;var originalControls=Controls.Cast<Control>().ToArray();SuspendLayout();try{foreach(var c in originalControls)Controls.Remove(c);var title=new Label{Text="BEAM SCHEDULE FORMATTER",Dock=DockStyle.Top,Height=54,Padding=new Padding(12),Font=new Font("Segoe UI",18,FontStyle.Bold),ForeColor=PowTheme.Ink};var tools=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,WrapContents=true,Padding=new Padding(8)};browse.Text="Load source DXF";tools.Controls.Add(browse);tools.Controls.Add(new Label{Text="Prefix",AutoSize=true,Padding=new Padding(4,8,0,0)});prefixBox.Width=85;tools.Controls.Add(prefixBox);tools.Controls.Add(new Label{Text="Floor title",AutoSize=true,Padding=new Padding(4,8,0,0)});floorBox.Width=240;tools.Controls.Add(floorBox);merge.AutoSize=true;tools.Controls.Add(merge);tools.Controls.Add(process);tools.Controls.Add(export);ActionPanel=tools;sourceBox.Dock=DockStyle.Top;sourceBox.Height=32;sourceBox.BorderStyle=BorderStyle.None;sourceBox.ReadOnly=true;summary.Dock=DockStyle.Top;summary.Height=52;summary.Font=ModernUi.Body;var notes=new Label{Text="Matching beam geometry and reinforcement are grouped. Original beam numbers and stirrup details remain available below the schedule.",Dock=DockStyle.Top,Height=42,Padding=new Padding(10),ForeColor=PowTheme.Muted};Controls.Add(tabs);Controls.Add(notes);Controls.Add(summary);Controls.Add(sourceBox);Controls.Add(tools);Controls.Add(title);Controls.Add(status);}finally{ResumeLayout(true);}}
}
#if ADMIN_APP
partial class CodeGeneratorForm {
 void ConfigureResponsiveLayout(CheckedListBox apps,Button generate,Button import,Button copy,Button clear,CheckBox certification,TextBox certVersion,Button mobile){AutoScroll=true;FormBorderStyle=FormBorderStyle.Sizable;MaximizeBox=true;MinimumSize=new Size(850,560);bool arranging=false;
  /* compact, one screen without scrolling (owner, 20 November 2026: "gawin 1 screen kasya sa lahat") */
  Action fit=()=>{if(arranging)return;arranging=true;try{int width=Math.Min(1560,Math.Max(720,ClientSize.Width-48)),x=Math.Max(24,(ClientSize.Width-width)/2),left=Math.Min(380,Math.Max(300,width*27/100)),gap=28,rightX=x+left+gap,right=width-left-gap;
   var heading=Controls.OfType<Label>().First(l=>l.Text.StartsWith("DTCE TOOL KIT"));heading.Font=new Font("Segoe UI",15,FontStyle.Bold);heading.SetBounds(x,10,width,34);
   var appsTitle=Controls.OfType<Label>().First(l=>l.Text=="APPS ALLOWED");appsTitle.SetBounds(x,52,left,24);apps.SetBounds(x,78,left,apps.Items.Count*Math.Max(18,apps.ItemHeight)+8);
   var included=Controls.OfType<Label>().First(l=>l.Text.StartsWith("Project Information is included"));included.SetBounds(x,apps.Bottom+4,left,36);
   generate.SetBounds(x,included.Bottom+4,left,40);import.SetBounds(x,generate.Bottom+6,left,32);mobile.SetBounds(x,import.Bottom+6,left,32);message.SetBounds(x,mobile.Bottom+6,left,66);
   var requestLabel=Controls.OfType<Label>().First(l=>l.Text.StartsWith("Paste the request"));requestLabel.SetBounds(rightX,52,right,24);request.SetBounds(rightX,78,right,62);
   var generated=Controls.OfType<Label>().First(l=>l.Text=="GENERATED ACCESS CODE");generated.SetBounds(rightX,request.Bottom+10,right,24);
   output.SetBounds(rightX,generated.Bottom+4,right,Math.Max(120,Math.Min(420,ClientSize.Height-430)));copy.SetBounds(rightX,output.Bottom+8,(right-12)/2,36);clear.SetBounds(copy.Right+12,output.Bottom+8,(right-12)/2,36);
   certification.SetBounds(rightX,copy.Bottom+10,right,26);var versionLabel=Controls.OfType<Label>().First(l=>l.Text=="Exact User version");versionLabel.SetBounds(rightX,certification.Bottom+6,160,26);certVersion.SetBounds(rightX+170,versionLabel.Top-2,180,28);
   var note=Controls.OfType<Label>().First(l=>l.Text.StartsWith("Certification: no request"));note.SetBounds(rightX,versionLabel.Bottom+8,right,44);
   AutoScrollMinSize=new Size(0,Math.Max(message.Bottom,note.Bottom)+12);}finally{arranging=false;}};Resize+=(s,e)=>fit();Shown+=(s,e)=>fit();fit();}
}
#endif
}
