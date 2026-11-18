using System;using System.Collections.Generic;using System.Drawing;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Searchable BACKUP pay item picker for Map BACKUP items (owner, 17 November 2026). Type an item number or any
// words of the description; every word must match. Double-click or Enter selects.
sealed class BackupTargetPicker:Form {
 readonly TextBox search=new TextBox{Dock=DockStyle.Top};readonly ListView list=new ListView{Dock=DockStyle.Fill,View=View.Details,FullRowSelect=true,MultiSelect=false,HideSelection=false};
 readonly List<BackupTarget> all;internal BackupTarget Selected{get;private set;}
 internal static bool Matches(BackupTarget t,string query){var words=(query??"").Split(new[]{' ','\t'},StringSplitOptions.RemoveEmptyEntries);string hay=(t.Code+" "+t.Description+" "+t.Unit+" BC"+t.Row).ToUpperInvariant();return words.All(w=>hay.Contains(w.ToUpperInvariant()));}
 internal BackupTargetPicker(string title,IEnumerable<BackupTarget> targets,BackupTarget current){AppBrand.Apply(this);Text="Choose BACKUP item for: "+title;ClientSize=new Size(1000,560);MinimumSize=new Size(700,400);StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",9.5f);
  all=targets.ToList();list.Columns.Add("Item no.",120);list.Columns.Add("Description",620);list.Columns.Add("Unit",80);list.Columns.Add("Row",80);
  var hint=new Label{Dock=DockStyle.Top,Height=26,Padding=new Padding(2,6,0,0),Text="Search item number or description words (e.g. \"1016 membrane\" or \"900(1)d gutter\"):"};
  var buttons=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=46,FlowDirection=FlowDirection.RightToLeft,Padding=new Padding(6)};var ok=new Button{Text="Select",Width=110,Height=30};var cancel=new Button{Text="Cancel",Width=100,Height=30,DialogResult=DialogResult.Cancel};buttons.Controls.Add(cancel);buttons.Controls.Add(ok);
  var body=new Panel{Dock=DockStyle.Fill,Padding=new Padding(10)};body.Controls.Add(list);body.Controls.Add(search);body.Controls.Add(hint);Controls.Add(body);Controls.Add(buttons);CancelButton=cancel;
  Action fill=()=>{list.BeginUpdate();list.Items.Clear();foreach(var t in all.Where(t=>Matches(t,search.Text))){var item=new ListViewItem(new[]{t.Code,t.Description,t.Unit,"BC"+t.Row}){Tag=t};list.Items.Add(item);if(current!=null&&t.Row==current.Row){item.Selected=true;item.EnsureVisible();}}if(list.SelectedItems.Count==0&&list.Items.Count>0)list.Items[0].Selected=true;list.EndUpdate();};
  Action choose=()=>{if(list.SelectedItems.Count==0)return;Selected=(BackupTarget)list.SelectedItems[0].Tag;DialogResult=DialogResult.OK;};
  search.TextChanged+=(s,e)=>fill();list.DoubleClick+=(s,e)=>choose();ok.Click+=(s,e)=>choose();
  search.KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Down&&list.Items.Count>0){list.Focus();e.Handled=true;}else if(e.KeyCode==Keys.Enter){choose();e.Handled=e.SuppressKeyPress=true;}};
  list.KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Enter){choose();e.Handled=true;}};
  Shown+=(s,e)=>search.Focus();fill();PowTheme.Apply(this);}
}
}
