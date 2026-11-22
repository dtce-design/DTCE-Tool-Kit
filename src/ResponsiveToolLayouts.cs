using System;using System.Drawing;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
partial class MainForm {
 void UseSlabFormatterLayout(){var originalTable=(TabPage)grid.Parent;tabs.Dock=DockStyle.Fill;tabs.TabPages[1].Text="DXF preview";detail.Height=92;var originalControls=Controls.Cast<Control>().ToArray();SuspendLayout();try{foreach(var c in originalControls)Controls.Remove(c);var title=new Label{Text="BEAM SCHEDULE FORMATTER",Dock=DockStyle.Top,Height=54,Padding=new Padding(12),Font=new Font("Segoe UI",18,FontStyle.Bold),ForeColor=PowTheme.Ink};var tools=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,WrapContents=true,Padding=new Padding(8)};browse.Text="Load source DXF";tools.Controls.Add(browse);tools.Controls.Add(new Label{Text="Prefix",AutoSize=true,Padding=new Padding(4,8,0,0)});prefixBox.Width=85;tools.Controls.Add(prefixBox);tools.Controls.Add(new Label{Text="Floor title",AutoSize=true,Padding=new Padding(4,8,0,0)});floorBox.Width=240;tools.Controls.Add(floorBox);merge.AutoSize=true;tools.Controls.Add(merge);tools.Controls.Add(process);tools.Controls.Add(export);ActionPanel=tools;/* the same layout as the Slab Formatter (owner, 21 November 2026): one "Loaded DXF" line, no empty source box */sourceBox.Visible=false;summary.Dock=DockStyle.Top;summary.Height=45;summary.Padding=new Padding(12);summary.Font=ModernUi.Body;summary.ForeColor=PowTheme.Ink;summary.Text="Loaded DXF: none";var notes=new Label{Text="Matching beam geometry and reinforcement are grouped. Original beam numbers and stirrup details remain available below the schedule.",Dock=DockStyle.Top,Height=47,Padding=new Padding(10)};Controls.Add(tabs);Controls.Add(notes);Controls.Add(summary);Controls.Add(sourceBox);Controls.Add(tools);Controls.Add(title);Controls.Add(status);}finally{ResumeLayout(true);}}
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
namespace BeamScheduleFormatter {
/* one look for the Beam and Slab Formatters (owner, 21 November 2026: "sa schedule formatter ayusin mo please hindi parehas, ang pangit
   ng buttons and etc"): the same title, one command row with the fields and buttons on one line, one info card, the same table */
static class FormatterChrome {
 static System.Collections.Generic.IEnumerable<System.Windows.Forms.Control> All(System.Windows.Forms.Control root){foreach(System.Windows.Forms.Control c in root.Controls){yield return c;foreach(var d in All(c))yield return d;}}
 /* owner (22 November 2026, "hindi naman na makita yung nasa ibaba"): the selected-row details under the Beam and Slab schedules
    were a 92 px strip that cut the text. Both now use the same captioned details pane below the table, about 190 px tall, with
    scroll bars and a splitter the user can drag. */
 static void DetailPane(System.Windows.Forms.TabPage page){
  var grid=page.Controls.OfType<System.Windows.Forms.DataGridView>().FirstOrDefault();var box=page.Controls.OfType<System.Windows.Forms.TextBox>().FirstOrDefault(t=>t.Multiline&&t.ReadOnly&&t.Dock==System.Windows.Forms.DockStyle.Bottom);
  if(grid==null||box==null)return;
  page.Controls.Remove(grid);page.Controls.Remove(box);
  var split=new System.Windows.Forms.SplitContainer{Dock=System.Windows.Forms.DockStyle.Fill,Orientation=System.Windows.Forms.Orientation.Horizontal,FixedPanel=System.Windows.Forms.FixedPanel.Panel2,SplitterWidth=6,BackColor=System.Drawing.Color.FromArgb(210,220,230)};
  split.Panel1.BackColor=System.Drawing.Color.White;split.Panel2.BackColor=box.BackColor;
  var caption=new System.Windows.Forms.Label{Text="SELECTED ROW DETAILS",Dock=System.Windows.Forms.DockStyle.Top,Height=26,Padding=new System.Windows.Forms.Padding(10,6,0,0),Font=ModernUi.Strong,ForeColor=PowTheme.Ink,BackColor=box.BackColor};
  box.Dock=System.Windows.Forms.DockStyle.Fill;box.ScrollBars=System.Windows.Forms.ScrollBars.Both;box.WordWrap=false;box.Padding=new System.Windows.Forms.Padding(8);
  grid.Dock=System.Windows.Forms.DockStyle.Fill;split.Panel1.Controls.Add(grid);split.Panel2.Controls.Add(box);split.Panel2.Controls.Add(caption);page.Controls.Add(split);
  bool placed=false;System.EventHandler place=(s,e)=>{if(placed||split.Height<300)return;placed=true;try{split.Panel1MinSize=120;split.Panel2MinSize=90;split.SplitterDistance=split.Height-190-split.SplitterWidth;}catch(System.InvalidOperationException){}catch(System.ArgumentException){}};
  split.SizeChanged+=place;page.VisibleChanged+=place;}
 internal static void Apply(System.Windows.Forms.Form form){
  var all=All(form).ToList();
  foreach(var page in all.OfType<System.Windows.Forms.TabPage>().ToList())DetailPane(page);
  var title=all.OfType<System.Windows.Forms.Label>().FirstOrDefault(l=>l.Text.EndsWith("SCHEDULE FORMATTER"));if(title!=null){title.Height=52;title.Padding=new System.Windows.Forms.Padding(16,12,0,0);title.Font=new System.Drawing.Font("Segoe UI Semibold",16f);title.BackColor=ModernUi.Surface;}
  var tools=all.OfType<System.Windows.Forms.FlowLayoutPanel>().FirstOrDefault(p=>p.Controls.OfType<System.Windows.Forms.Button>().Any(b=>b.Text=="Load source DXF"));
  if(tools!=null){tools.AutoSize=true;tools.AutoSizeMode=System.Windows.Forms.AutoSizeMode.GrowAndShrink;tools.WrapContents=true;tools.Padding=new System.Windows.Forms.Padding(14,6,14,10);tools.BackColor=ModernUi.Surface;
   foreach(System.Windows.Forms.Control c in tools.Controls){
    if(c is System.Windows.Forms.Button){var b=(System.Windows.Forms.Button)c;b.AutoSize=true;b.Height=36;b.MinimumSize=new System.Drawing.Size(0,36);b.Margin=new System.Windows.Forms.Padding(0,0,8,0);if(b.Text=="Load source DXF")b.Margin=new System.Windows.Forms.Padding(0,0,18,0);if(b.Text=="Build preview")b.Margin=new System.Windows.Forms.Padding(14,0,8,0);}
    else if(c is System.Windows.Forms.Label){var l=(System.Windows.Forms.Label)c;l.AutoSize=true;l.Font=ModernUi.Body;l.ForeColor=PowTheme.Muted;l.Padding=System.Windows.Forms.Padding.Empty;l.Margin=new System.Windows.Forms.Padding(6,10,6,0);}
    else if(c is System.Windows.Forms.TextBox){var t=(System.Windows.Forms.TextBox)c;t.Font=ModernUi.Body;t.Margin=new System.Windows.Forms.Padding(0,6,10,0);t.Width=t.Text.Length>4||t.Width>150?220:70;}
    else if(c is System.Windows.Forms.CheckBox){var k=(System.Windows.Forms.CheckBox)c;k.AutoSize=true;k.Font=ModernUi.Body;k.Margin=new System.Windows.Forms.Padding(6,9,6,0);}}}
  foreach(var l in all.OfType<System.Windows.Forms.Label>()){
   if(l.Text.StartsWith("Loaded DXF")){l.Height=34;l.Padding=new System.Windows.Forms.Padding(16,10,8,0);l.Font=ModernUi.Strong;l.ForeColor=PowTheme.Ink;l.BackColor=ModernUi.Surface;l.AutoEllipsis=true;}
   else if(l.Text.StartsWith("Matching ")){l.Height=38;l.Padding=new System.Windows.Forms.Padding(16,2,8,10);l.Font=ModernUi.Body;l.ForeColor=PowTheme.Muted;l.BackColor=ModernUi.Surface;l.AutoEllipsis=true;}}}
}
}
