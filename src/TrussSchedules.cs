using System;using System.Collections.Generic;using System.Drawing;using System.Globalization;using System.Linq;using System.Windows.Forms;using System.Xml;
namespace BeamScheduleFormatter {
/* Truss schedules on Angle Bars (owner, 23 November 2026: "add button para assign schedule of trusses tapos yung schedule na lang input").
   Each truss type (T-1, T-2, ...) is entered once with its members (handbook angle profile, material, cut length, quantity per truss) and
   the number of trusses and sets. The Angle Bars sheet then gets one row per member (QTY = per truss, Units = trusses, Sets = sets), so the
   6 m stock, offcut sharing, painting and BOQ work exactly as for typed rows. Those rows are locked and follow the schedule: editing or
   deleting a type updates or removes its rows. The mark is saved on each row (attribute tm) and the types in <TrussSchedules>; files
   without them open as before. */
sealed class TrussMember {public string Name="",Profile="",Material="Project specification";public double Length;public int Qty=1;}
sealed class TrussType {public string Mark="",Description="";public int Trusses=1,Sets=1;public readonly List<TrussMember> Members=new List<TrussMember>();}
partial class ProgramOfWorksForm {
 internal const string TrussMarkColumn="TrussMark";
 readonly List<TrussType> trussTypes=new List<TrussType>();
 internal List<TrussType> TrussTypes{get{return trussTypes;}}
 static string[] AngleProfiles(){return SteelCatalog.Profiles.Where(p=>p.Kind=="Angle bar").Select(p=>p.Name).ToArray();}
 void InitializeTrussSchedules(){if(steelAngles==null||steelAngles.Columns.Contains(TrussMarkColumn))return;
  var mark=new DataGridViewTextBoxColumn{Name=TrussMarkColumn,HeaderText="Truss schedule",Width=100,ToolTipText="Rows made from a truss schedule follow it; edit them with Truss schedules."};steelAngles.Columns.Insert(steelAngles.Columns.Count-1,mark);
  var tab=tabs.TabPages.Cast<TabPage>().FirstOrDefault(p=>p.Text==SteelPageNames[1])??AllControls(tabs).OfType<TabPage>().FirstOrDefault(p=>p.Text==SteelPageNames[1]);if(tab==null)return;var bar=tab.Controls.OfType<FlowLayoutPanel>().FirstOrDefault();if(bar==null)return;
  Button(bar,"Truss schedules",ShowTrussSchedules);bar.Visible=true;}
 static string TrussMarkOf(DataGridViewRow row){return row.DataGridView!=null&&row.DataGridView.Columns.Contains(TrussMarkColumn)?Convert.ToString(row.Cells[TrussMarkColumn].Value).Trim():"";}
 /* rows of one truss type on Angle Bars: one per member */
 void ApplyTrussType(TrussType t,string oldMark){steelAngles.EndEdit();bool before=filling;filling=true;try{
   var old=steelAngles.Rows.Cast<DataGridViewRow>().Where(r=>!r.IsNewRow&&string.Equals(TrussMarkOf(r),oldMark??t.Mark,StringComparison.OrdinalIgnoreCase)).ToList();int at=old.Count>0?old.Min(r=>r.Index):-1;foreach(var r in old)steelAngles.Rows.Remove(r);
   if(t!=null){int n=0;foreach(var m in t.Members){var values=new object[steelAngles.Columns.Count];int index=at>=0&&at<=steelAngles.Rows.Count-(steelAngles.AllowUserToAddRows?1:0)?at+n:steelAngles.Rows.Count-(steelAngles.AllowUserToAddRows?1:0);steelAngles.Rows.Insert(index,1);var row=steelAngles.Rows[index];
     row.Cells[1].Value="Truss "+t.Mark+" - "+m.Name;row.Cells[2].Value=m.Profile;row.Cells[3].Value=m.Material;row.Cells[4].Value=m.Length.ToString("0.###",CultureInfo.InvariantCulture);row.Cells[5].Value=m.Qty.ToString(CultureInfo.InvariantCulture);
     if(steelAngles.Columns.Contains(SteelUnitsColumn)){row.Cells[SteelUnitsColumn].Value=t.Trusses.ToString(CultureInfo.InvariantCulture);row.Cells[SteelSetsColumn].Value=t.Sets.ToString(CultureInfo.InvariantCulture);if(steelAngles.Columns.Contains(SteelMultiplierColumn))row.Cells[SteelMultiplierColumn].Value="1";}
     row.Cells[TrussMarkColumn].Value=t.Mark;n++;}}}finally{filling=before;}AssignStructuralSteelIds();StyleTrussRows();}
 /* rows of a schedule are locked (edit the schedule instead) */
 void StyleTrussRows(){if(steelAngles==null||!steelAngles.Columns.Contains(TrussMarkColumn))return;int[] locked={1,2,3,4,5};
  foreach(DataGridViewRow r in steelAngles.Rows){if(r.IsNewRow)continue;bool on=TrussMarkOf(r)!="";var names=new List<int>(locked);foreach(var n in new[]{SteelUnitsColumn,SteelSetsColumn,SteelMultiplierColumn})if(steelAngles.Columns.Contains(n))names.Add(steelAngles.Columns[n].Index);
   {var tm=r.Cells[TrussMarkColumn];if(!tm.ReadOnly)tm.ReadOnly=true;}/* the mark itself is never typed */
   foreach(int c in names){var cell=r.Cells[c];if(cell.ReadOnly!=on)cell.ReadOnly=on;cell.Style.BackColor=on?Color.FromArgb(236,241,246):Color.Empty;cell.ToolTipText=on?"From truss schedule "+TrussMarkOf(r)+" - change it with Truss schedules.":"";}}}
 void ShowTrussSchedules(){steelAngles.EndEdit();
  using(var form=new Form{Text="Truss schedules",Width=980,Height=520,MinimumSize=new Size(720,360),StartPosition=FormStartPosition.CenterParent,Font=Font}){AppBrand.Apply(form);
   var heading=new WorksheetHeading{Text="Truss schedules",Subtitle="One entry per truss type  ·  its members go to Angle Bars"};heading.SetGuide("Enter each truss type once: its members (angle profile, material, cut length and quantity per truss) and how many trusses and sets use it. Angle Bars then lists every member with QTY per truss, Units = trusses and Sets, so no size is chosen row by row. Changing a type updates its rows; deleting it removes them.");
   var list=new ListView{Dock=DockStyle.Fill,View=View.Details,FullRowSelect=true,MultiSelect=false,HideSelection=false,GridLines=true};foreach(var h in new[]{"Mark","Description","Trusses","Sets","Members","Angle per truss (m)"})list.Columns.Add(h,h=="Description"?300:120);
   Action fill=()=>{string keep=list.SelectedItems.Count>0?list.SelectedItems[0].Text:null;list.BeginUpdate();list.Items.Clear();foreach(var t in trussTypes){var item=new ListViewItem(t.Mark){Tag=t};item.SubItems.Add(t.Description);item.SubItems.Add(t.Trusses.ToString(CultureInfo.InvariantCulture));item.SubItems.Add(t.Sets.ToString(CultureInfo.InvariantCulture));item.SubItems.Add(t.Members.Count.ToString(CultureInfo.InvariantCulture));item.SubItems.Add(t.Members.Sum(m=>m.Length*m.Qty).ToString("0.###",CultureInfo.InvariantCulture));list.Items.Add(item);if(item.Text==keep)item.Selected=true;}list.EndUpdate();};
   Func<TrussType> selected=()=>list.SelectedItems.Count>0?(TrussType)list.SelectedItems[0].Tag:null;
   Action<TrussType> edit=t=>{string oldMark=t==null?null:t.Mark;var result=EditTrussType(form,t);if(result==null)return;if(t==null)trussTypes.Add(result);ApplyTrussType(result,oldMark);fill();Recalculate();};
   var tools=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,Padding=new Padding(10,4,10,4)};
   var add=new Button{Text="Add new truss",AutoSize=true,Height=30};add.Click+=(x,e)=>edit(null);
   var change=new Button{Text="Edit selected",AutoSize=true,Height=30};change.Click+=(x,e)=>{var t=selected();if(t==null){MessageBox.Show(form,"Select a truss to edit.",form.Text);return;}edit(t);};
   var remove=new Button{Text="Delete selected",AutoSize=true,Height=30};remove.Click+=(x,e)=>{var t=selected();if(t==null){MessageBox.Show(form,"Select a truss to delete.",form.Text);return;}if(MessageBox.Show(form,"Delete truss "+t.Mark+" and its rows on Angle Bars?",form.Text,MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;trussTypes.Remove(t);ApplyTrussType(null,t.Mark);fill();Recalculate();};
   tools.Controls.AddRange(new Control[]{add,change,remove});list.DoubleClick+=(x,e)=>{var t=selected();if(t!=null)edit(t);};
   var bottom=new FlowLayoutPanel{Dock=DockStyle.Bottom,AutoSize=true,FlowDirection=FlowDirection.RightToLeft,Padding=new Padding(10)};var close=new Button{Text="Close",AutoSize=true,DialogResult=DialogResult.OK};bottom.Controls.Add(close);form.CancelButton=close;
   var host=new Panel{Dock=DockStyle.Fill,Padding=new Padding(12,6,12,6)};host.Controls.Add(list);form.Controls.Add(host);form.Controls.Add(tools);form.Controls.Add(bottom);form.Controls.Add(heading);fill();form.ShowDialog(this);}}
 /* entry form of one truss type; null = new. Returns the type (the same object when editing) or null when cancelled. */
 TrussType EditTrussType(IWin32Window owner,TrussType t){
  using(var form=new Form{Text=t==null?"Add truss":"Edit truss "+t.Mark,Width=900,Height=560,MinimumSize=new Size(700,420),StartPosition=FormStartPosition.CenterParent,Font=Font}){AppBrand.Apply(form);
   var top=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=4,Padding=new Padding(12,10,12,4)};
   var mark=new TextBox{Width=120,Text=t==null?"T-"+(trussTypes.Count+1):t.Mark};var desc=new TextBox{Width=360,Text=t==null?"":t.Description};
   var count=new NumericUpDown{Minimum=1,Maximum=100000,Width=100,Value=t==null?1:t.Trusses};var sets=new NumericUpDown{Minimum=1,Maximum=100000,Width=100,Value=t==null?1:t.Sets};
   Action<string,Control,int,int> put=(label,c,col,row)=>{top.Controls.Add(new Label{Text=label,AutoSize=true,Margin=new Padding(0,8,6,0)},col,row);top.Controls.Add(c,col+1,row);};
   put("Mark",mark,0,0);put("Description",desc,2,0);put("Number of trusses (units)",count,0,1);put("Sets",sets,2,1);
   var grid=new DataGridView{Dock=DockStyle.Fill,AllowUserToAddRows=true,AllowUserToDeleteRows=true,RowHeadersVisible=true,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill};
   grid.Columns.Add("Member","Member (e.g. top chord)");var profile=new DataGridViewComboBoxColumn{Name="Profile",HeaderText="Profile",FlatStyle=FlatStyle.Flat,FillWeight=160};profile.Items.AddRange(AngleProfiles());grid.Columns.Add(profile);
   grid.Columns.Add("Material","Material grade / finish");grid.Columns.Add("Length","Cut length m");grid.Columns.Add("Qty","Qty per truss");GridAppearance.Apply(grid);
   grid.DefaultValuesNeeded+=(x,e)=>{e.Row.Cells[2].Value="Project specification";e.Row.Cells[4].Value="1";};
   grid.CurrentCellDirtyStateChanged+=(x,e)=>{if(grid.IsCurrentCellDirty&&grid.CurrentCell is DataGridViewComboBoxCell)grid.CommitEdit(DataGridViewDataErrorContexts.Commit);};
   grid.DataError+=(x,e)=>{e.ThrowException=false;};
   if(t!=null)foreach(var m in t.Members)grid.Rows.Add(m.Name,AngleProfiles().Contains(m.Profile)?m.Profile:null,m.Material,m.Length.ToString("0.###",CultureInfo.InvariantCulture),m.Qty.ToString(CultureInfo.InvariantCulture));
   var note=new Label{Dock=DockStyle.Bottom,Height=40,ForeColor=Color.Firebrick,Padding=new Padding(12,4,12,0)};
   var bottom=new FlowLayoutPanel{Dock=DockStyle.Bottom,AutoSize=true,FlowDirection=FlowDirection.RightToLeft,Padding=new Padding(10)};var ok=new Button{Text="Save truss",AutoSize=true};var cancel=new Button{Text="Cancel",AutoSize=true,DialogResult=DialogResult.Cancel};bottom.Controls.Add(cancel);bottom.Controls.Add(ok);form.CancelButton=cancel;
   TrussType result=null;
   ok.Click+=(x,e)=>{grid.EndEdit();try{string m=mark.Text.Trim();if(m=="")throw new Exception("Enter the truss mark.");if(trussTypes.Any(o=>o!=t&&string.Equals(o.Mark,m,StringComparison.OrdinalIgnoreCase)))throw new Exception("Truss "+m+" already exists.");
     var members=new List<TrussMember>();foreach(DataGridViewRow r in grid.Rows){if(r.IsNewRow)continue;Func<int,string> v=c=>Convert.ToString(r.Cells[c].Value).Trim();if(v(0)==""&&v(1)==""&&v(3)=="")continue;
      string where="Member row "+(r.Index+1)+": ";if(v(0)=="")throw new Exception(where+"enter the member name.");if(!AngleProfiles().Contains(v(1)))throw new Exception(where+"choose the profile.");
      double len;if(!double.TryParse(v(3),NumberStyles.Float,CultureInfo.InvariantCulture,out len)||len<=0||len>100)throw new Exception(where+"enter the cut length in m.");int q;if(!int.TryParse(v(4),out q)||q<1||q>100000)throw new Exception(where+"enter a whole quantity per truss.");
      members.Add(new TrussMember{Name=v(0),Profile=v(1),Material=v(2)==""?"Project specification":v(2),Length=len,Qty=q});}
     if(members.Count==0)throw new Exception("Enter at least one member.");
     result=t??new TrussType();result.Mark=m;result.Description=desc.Text.Trim();result.Trusses=(int)count.Value;result.Sets=(int)sets.Value;result.Members.Clear();result.Members.AddRange(members);form.DialogResult=DialogResult.OK;}
    catch(Exception ex){note.Text=ex.Message;}};
   var host=new Panel{Dock=DockStyle.Fill,Padding=new Padding(12,4,12,4)};host.Controls.Add(grid);form.Controls.Add(host);form.Controls.Add(note);form.Controls.Add(bottom);form.Controls.Add(top);
   return form.ShowDialog(owner)==DialogResult.OK?result:null;}}
 void WriteTrussSchedules(XmlElement root){if(trussTypes.Count==0)return;var doc=root.OwnerDocument;var section=doc.CreateElement("TrussSchedules");section.SetAttribute("version","1");
  foreach(var t in trussTypes){var e=doc.CreateElement("Truss");e.SetAttribute("mark",t.Mark);e.SetAttribute("description",t.Description);e.SetAttribute("trusses",t.Trusses.ToString(CultureInfo.InvariantCulture));e.SetAttribute("sets",t.Sets.ToString(CultureInfo.InvariantCulture));
   foreach(var m in t.Members){var x=doc.CreateElement("Member");x.SetAttribute("name",m.Name);x.SetAttribute("profile",m.Profile);x.SetAttribute("material",m.Material);x.SetAttribute("length",m.Length.ToString("R",CultureInfo.InvariantCulture));x.SetAttribute("qty",m.Qty.ToString(CultureInfo.InvariantCulture));e.AppendChild(x);}section.AppendChild(e);}
  root.AppendChild(section);}
 void ReadTrussSchedules(XmlElement root){trussTypes.Clear();var section=root["TrussSchedules"];if(section==null)return;if(section.GetAttribute("version")!="1")throw new Exception("Unsupported truss schedule format.");
  foreach(XmlElement e in section.SelectNodes("Truss")){if(trussTypes.Count>5000)throw new Exception("Too many truss types.");int n,s;var t=new TrussType{Mark=e.GetAttribute("mark"),Description=e.GetAttribute("description"),Trusses=int.TryParse(e.GetAttribute("trusses"),out n)&&n>0?n:1,Sets=int.TryParse(e.GetAttribute("sets"),out s)&&s>0?s:1};
   foreach(XmlElement x in e.SelectNodes("Member")){double len;int q;t.Members.Add(new TrussMember{Name=x.GetAttribute("name"),Profile=x.GetAttribute("profile"),Material=x.GetAttribute("material"),Length=double.TryParse(x.GetAttribute("length"),NumberStyles.Float,CultureInfo.InvariantCulture,out len)?len:0,Qty=int.TryParse(x.GetAttribute("qty"),out q)?q:1});}
   trussTypes.Add(t);}}
 static void WriteTrussMark(DataGridViewRow row,XmlElement entry){string m=TrussMarkOf(row);if(m!="")entry.SetAttribute("tm",m);}
 static void ReadTrussMark(DataGridViewRow row,XmlElement entry){if(row.DataGridView!=null&&row.DataGridView.Columns.Contains(TrussMarkColumn))row.Cells[TrussMarkColumn].Value=entry.HasAttribute("tm")?entry.GetAttribute("tm"):null;}
}
}
