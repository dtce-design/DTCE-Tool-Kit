using System;using System.IO;using System.Linq;using System.Drawing;using System.Windows.Forms;
namespace BeamScheduleFormatter {
partial class EstimatorForm {internal readonly PowSourceFiles sourceAttachments=new PowSourceFiles();}
partial class SlabEstimatorForm {internal readonly PowSourceFiles sourceAttachments=new PowSourceFiles();}
partial class ProgramOfWorksForm {
 readonly PowSourceFiles projectSourceFiles=new PowSourceFiles();
 PowSourceFiles ProjectSources(){var result=new PowSourceFiles();result.AddRange(projectSourceFiles.Files);result.AddRange(beam.sourceAttachments.Files);result.AddRange(slab.sourceAttachments.Files);return result;}
 void ManageProjectFiles(){Guard();using(var form=new Form{Text="Program of Works / Project files",Width=940,Height=520,MinimumSize=new Size(700,420),StartPosition=FormStartPosition.CenterParent}){
  var list=new ListView{Dock=DockStyle.Fill,View=View.Details,FullRowSelect=true,MultiSelect=false};list.Columns.Add("Imported file",340);list.Columns.Add("Source",190);list.Columns.Add("Size",100);list.Columns.Add("Copy",160);
  Action refresh=()=>{list.Items.Clear();foreach(var file in ProjectSources().Files){var row=new ListViewItem(new[]{file.Name,file.Kind,(file.Size/1024.0).ToString("0.0")+" KB",file.RecoveredText?"Recovered DXF text":"Original bytes"}){Tag=file};list.Items.Add(row);}};
  var tools=new FlowLayoutPanel{Dock=DockStyle.Top,Height=48,Padding=new Padding(8)};var add=new Button{Text="Add project files",AutoSize=true};add.Click+=(s,e)=>{try{using(var d=new OpenFileDialog{Filter="All files|*.*",Multiselect=true})if(d.ShowDialog(form)==DialogResult.OK){var staged=ProjectSources();foreach(string path in d.FileNames)staged.CaptureFile(path,"Project attachment");projectSourceFiles.Replace(staged);refresh();}}catch(Exception ex){MessageBox.Show(form,ex.Message,"Project files");}};var export=new Button{Text="Save selected copy",AutoSize=true};export.Click+=(s,e)=>{try{if(list.SelectedItems.Count==0)return;var file=(PowSourceFile)list.SelectedItems[0].Tag;using(var d=new SaveFileDialog{Filter="All files|*.*",FileName=file.Name,OverwritePrompt=true})if(d.ShowDialog(form)==DialogResult.OK)file.CopyTo(d.FileName);}catch(Exception ex){MessageBox.Show(form,ex.Message,"Project files");}};tools.Controls.Add(add);tools.Controls.Add(export);
  var note=new Label{Dock=DockStyle.Bottom,Height=68,Padding=new Padding(10),Text="Imported source copies travel inside the saved POW. Use Save Program of Works / Ctrl+S after adding files. Older DXFs are recovered from embedded schedule text when original bytes were not recorded. Linked Excel originals are snapshots from link time; the working workbook remains a separate file. No fixed attachment size or count limit; originals are streamed through local disk storage."};form.Controls.Add(list);form.Controls.Add(tools);form.Controls.Add(note);refresh();PowTheme.Apply(form);form.ShowDialog(this);
 }}
}
}
