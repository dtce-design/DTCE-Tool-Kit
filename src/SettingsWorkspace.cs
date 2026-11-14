using System;using System.Drawing;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Reuse the actual controls, so saved estimates and existing calculation events stay authoritative.
static class SettingsWorkspace {
 public static void Show(IWin32Window owner,string title,string[] names,Control[] panels){
  using(var dialog=new Form{Text=title+" | Settings",ClientSize=new Size(900,610),MinimumSize=new Size(720,480),StartPosition=FormStartPosition.CenterParent,Font=new Font("Segoe UI",10)}){
   AppBrand.Apply(dialog);var tabs=new NavigationTabs{Dock=DockStyle.Fill};var footer=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=54,Padding=new Padding(10),FlowDirection=FlowDirection.RightToLeft};
   var done=new Button{Text="Done",Width=110,Height=32,DialogResult=DialogResult.OK};footer.Controls.Add(done);
   var note=new Label{Text="Changes apply immediately. Save the estimate/project to keep them.",AutoSize=true,Padding=new Padding(0,8,14,0)};footer.Controls.Add(note);dialog.Controls.Add(tabs);dialog.Controls.Add(footer);dialog.AcceptButton=done;dialog.CancelButton=done;
   var parents=panels.Select(p=>p.Parent).ToArray();var indices=panels.Select(p=>p.Parent==null?0:p.Parent.Controls.GetChildIndex(p)).ToArray();var docks=panels.Select(p=>p.Dock).ToArray();var sizes=panels.Select(p=>p.Size).ToArray();var shown=panels.Select(p=>p.Visible).ToArray();
   try{for(int i=0;i<panels.Length;i++){var page=new TabPage(names[i]){Padding=new Padding(16)};tabs.TabPages.Add(page);page.Controls.Add(panels[i]);panels[i].Dock=DockStyle.Fill;panels[i].Visible=true;}PowTheme.Apply(dialog);dialog.ShowDialog(owner);}
   finally{for(int i=0;i<panels.Length;i++){tabs.TabPages[i].Controls.Remove(panels[i]);if(parents[i]!=null){parents[i].Controls.Add(panels[i]);parents[i].Controls.SetChildIndex(panels[i],Math.Min(indices[i],parents[i].Controls.Count-1));}panels[i].Dock=docks[i];panels[i].Size=sizes[i];panels[i].Visible=shown[i];}}
  }
 }
 public static void MoveButtons(FlowLayoutPanel tools,FlowLayoutPanel settings,params string[] names){foreach(var button in tools.Controls.OfType<Button>().Where(b=>names.Contains(b.Text)).ToArray())settings.Controls.Add(button);}
}
}
