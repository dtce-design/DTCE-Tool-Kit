using System;using System.IO;using System.Xml;using System.Text;using System.Drawing;using System.Reflection;using System.Threading.Tasks;using System.Windows.Forms;
namespace BeamScheduleFormatter {
static class PowProjectFileWriter {
 internal static void AtomicWrite(string path,string xml){string full=Path.GetFullPath(path),temp=full+"."+Guid.NewGuid().ToString("N")+".tmp";try{using(var output=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)){byte[] bytes=new UTF8Encoding(false).GetBytes(xml);output.Write(bytes,0,bytes.Length);output.Flush(true);}if(File.Exists(full))File.Replace(temp,full,null);else File.Move(temp,full);}finally{if(File.Exists(temp))File.Delete(temp);}}
 internal static void WriteWithSources(string path,string snapshot,PowSourceFiles sources){var doc=new XmlDocument{XmlResolver=null};doc.LoadXml(snapshot);sources.Write(doc.DocumentElement);AtomicWrite(path,doc.OuterXml);}
}
sealed class PowSaveFeedback:Form {
 readonly Label state;readonly ProgressBar progress;
 internal PowSaveFeedback(){Text="Save Program of Works";ClientSize=new Size(490,142);FormBorderStyle=FormBorderStyle.FixedDialog;ControlBox=false;ShowInTaskbar=false;StartPosition=FormStartPosition.CenterParent;BackColor=Color.White;Font=new Font("Segoe UI",10);var logo=new PictureBox{Location=new Point(18,26),Size=new Size(74,74),SizeMode=PictureBoxSizeMode.Zoom};string name=
#if ADMIN_APP
 "DtceLogo.png";
#else
 "DpwhLogo.png";
#endif
 using(var input=Assembly.GetExecutingAssembly().GetManifestResourceStream(name))if(input!=null)using(var image=Image.FromStream(input))logo.Image=new Bitmap(image);logo.Disposed+=(s,e)=>{if(logo.Image!=null)logo.Image.Dispose();};state=new Label{Location=new Point(110,27),Size=new Size(350,30),Text="Preparing Program of Works…",Font=new Font("Segoe UI Semibold",12)};Controls.Add(logo);Controls.Add(state);progress=new ProgressBar{Location=new Point(110,75),Size=new Size(350,15),Style=ProgressBarStyle.Marquee,MarqueeAnimationSpeed=25};Controls.Add(progress);}
 internal void Writing(){state.Text="Saving project and source files…";}
 internal void Complete(){state.Text="✓ Program of Works saved";state.ForeColor=Color.FromArgb(35,113,74);progress.Style=ProgressBarStyle.Continuous;progress.Value=100;}
}
partial class ProgramOfWorksForm {
 bool savingPow;Label powSaveIndicator;
 string ChoosePowSavePath(){using(var dialog=new SaveFileDialog{Filter="Program of Works|*.pow.xml",FileName=string.IsNullOrEmpty(currentPowPath)?"Program-of-Works.pow.xml":Path.GetFileName(currentPowPath),OverwritePrompt=true})return dialog.ShowDialog(this)==DialogResult.OK?dialog.FileName:null;}
 async void SaveThenClose(){await SaveWithFeedback(false);if(!IsDisposed&&DataXml()==saved&&!beam.WorksDirty&&!slab.WorksDirty)Close();}
 async void SaveAs(){await SaveWithFeedback(true);}
 async Task SaveWithFeedback(bool saveAs){if(savingPow)return;try{Guard();string path=saveAs||string.IsNullOrEmpty(currentPowPath)?ChoosePowSavePath():currentPowPath;if(path==null)return;savingPow=true;bool enabled=Enabled;using(var feedback=new PowSaveFeedback()){feedback.Show(this);Enabled=false;try{await Task.Delay(40);string snapshot=BuildWorksProjectXml(false),fingerprint=DataXml(),beamState=beam.WorksSaveState(),slabState=slab.WorksSaveState();var sources=ProjectSources();feedback.Writing();if(powSaveIndicator!=null)powSaveIndicator.Text="Saving…";await Task.Run(()=>PowProjectFileWriter.WriteWithSources(path,snapshot,sources));beam.WorksSavedState(beamState);slab.WorksSavedState(slabState);saved=fingerprint;currentPowPath=Path.GetFullPath(path);if(powSaveIndicator!=null)powSaveIndicator.Text="Saved "+DateTime.Now.ToString("HH:mm:ss")+" · "+Path.GetFileName(path);feedback.Complete();await Task.Delay(900);}finally{Enabled=enabled;feedback.Close();}}}catch(Exception ex){if(powSaveIndicator!=null)powSaveIndicator.Text="Save failed — review required";MessageBox.Show(this,ex.Message,"Save Program of Works failed",MessageBoxButtons.OK,MessageBoxIcon.Warning);}finally{savingPow=false;}}
}
}
