using System;using System.Collections.Generic;using System.Globalization;using System.Linq;using System.Runtime.InteropServices;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* Live AutoCAD link for walls (owner, 20 November 2026: "may iclick ako sa app, tapos pupunta sa AutoCAD para ilapat ang wall ID at
   kukunin nito sa AutoCAD ang dimension"). "Pick walls in AutoCAD" on CHB Estimate switches to the running AutoCAD (full AutoCAD:
   AutoCAD LT has no automation), where the owner picks one dimension (or wall line / polyline) per wall run. For each pick the
   measured length is read, the Wall ID is taken from the dimension text when it is an ID or else the next free W number, and the
   Wall ID is written into the drawing as a TEXT on layer DTCE-WALL-ID at the picked point. Enter / Esc ends the picking; the walls
   then go through the same preview as "Load walls from DXF". Nothing is saved in the drawing - the owner saves it in AutoCAD. */
static class AutoCadLink {
 [DllImport("user32.dll")]static extern bool SetForegroundWindow(IntPtr hWnd);
 [DllImport("ole32.dll")]static extern int CoRegisterMessageFilter(IOleMessageFilter filter,out IOleMessageFilter old);
 internal const string LabelLayer="DTCE-WALL-ID";
 internal static object Running(){try{return Marshal.GetActiveObject("AutoCAD.Application");}catch(COMException){return null;}catch(Exception){return null;}}
 static bool Busy(Exception e){return ExcelBusyFilter.IsBusy(e);}
 static T Call<T>(Func<T> work){for(int i=0;;i++){try{return work();}catch(Exception e){if(!Busy(e)||i>=40)throw;System.Threading.Thread.Sleep(250);}}}
 static void Do(Action work){Call<object>(()=>{work();return null;});}
 /* picks walls until Enter / Esc; returns the walls read (empty when nothing was picked) */
 internal static List<DxfWall> PickWalls(object application,ISet<string> usedIds,double defaultThicknessMm){
  dynamic acad=application;var walls=new List<DxfWall>();IOleMessageFilter old=null;bool filter=false;
  try{try{CoRegisterMessageFilter(new ExcelBusyFilter(),out old);filter=true;}catch(Exception){}
   dynamic doc=Call<object>(()=>acad.ActiveDocument);if(doc==null)throw new Exception("Open the floor plan drawing in AutoCAD first.");
   int units=0;try{units=Convert.ToInt32(Call<object>(()=>doc.GetVariable("INSUNITS")),CultureInfo.InvariantCulture);}catch(Exception){}
   double textHeight=units==6?0.25:units==5?25:250;
   try{Do(()=>acad.Visible=true);}catch(Exception){}
   try{long hwnd=Convert.ToInt64(Call<object>(()=>acad.HWND),CultureInfo.InvariantCulture);SetForegroundWindow(new IntPtr(hwnd));}catch(Exception){}
   try{dynamic layers=doc.Layers;bool has=false;foreach(dynamic l in layers)if(string.Equals((string)l.Name,LabelLayer,StringComparison.OrdinalIgnoreCase)){has=true;break;}if(!has){dynamic layer=layers.Add(LabelLayer);try{layer.color=1;}catch(Exception){}}}catch(Exception){}
   var reserved=new HashSet<string>(usedIds,StringComparer.OrdinalIgnoreCase);var picked=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
   try{doc.Utility.Prompt("\nDTCE Tool Kit: pick one dimension (or wall line) per CHB wall run. Press Enter or Esc to finish.\n");}catch(Exception){}
   while(true){object ent=null,pt=null;
    try{doc.Utility.GetEntity(out ent,out pt,"\nDTCE: pick a wall dimension or wall line (Enter / Esc to finish): ");}
    catch(Exception e){if(Busy(e)){System.Threading.Thread.Sleep(250);continue;}break;}
    if(ent==null)break;dynamic e2=ent;string type="";try{type=(string)e2.ObjectName;}catch(Exception){}
    string handle="";try{handle=(string)e2.Handle;}catch(Exception){}
    if(handle!=""&&!picked.Add(handle)){try{doc.Utility.Prompt("\nAlready picked - choose another wall.\n");}catch(Exception){}continue;}
    double length=double.NaN;string ownText="";
    if(type.IndexOf("Dimension",StringComparison.OrdinalIgnoreCase)>=0){try{length=Convert.ToDouble(e2.Measurement,CultureInfo.InvariantCulture);}catch(Exception){}try{ownText=(string)e2.TextOverride;}catch(Exception){}}
    else{try{length=Convert.ToDouble(e2.Length,CultureInfo.InvariantCulture);}catch(Exception){}}
    if(double.IsNaN(length)||length<=0){try{doc.Utility.Prompt("\nNot a dimension or a wall line - pick again.\n");}catch(Exception){}if(handle!="")picked.Remove(handle);continue;}
    string layerName="";try{layerName=(string)e2.Layer;}catch(Exception){}
    string id=WallDxfImport.AsId(ownText);bool fromDrawing=id!=null;if(id==null){id=WallDxfImport.NextId(reserved);}reserved.Add(id);
    var w=new DxfWall{Id=id,IdFromDrawing=fromDrawing,Layer=layerName,LengthM=Math.Round(WallDxfImport.ToMetres(length,units),3),ThicknessMm=WallDxfImport.LayerThickness(layerName)??defaultThicknessMm,Source="AutoCAD pick"};
    walls.Add(w);
    /* the Wall ID into the drawing, at the picked point */
    try{var p=(double[])pt;dynamic text=doc.ModelSpace.AddText(id,new double[]{p[0],p[1],0},textHeight);try{text.Layer=LabelLayer;}catch(Exception){}try{text.Update();}catch(Exception){}}catch(Exception){}
    try{doc.Utility.Prompt("\n"+id+": "+w.LengthM.ToString("0.###",CultureInfo.InvariantCulture)+" m, "+w.ThicknessMm.ToString("0",CultureInfo.InvariantCulture)+" mm\n");}catch(Exception){}}
  }finally{if(filter){try{IOleMessageFilter ignored;CoRegisterMessageFilter(old,out ignored);}catch(Exception){}}}
  return walls;}
}
partial class ProgramOfWorksForm {
 void AddAutoCadPickButton(){var page=tabs.TabPages.Cast<TabPage>().FirstOrDefault(p=>p.Text=="CHB Estimate");if(page==null)return;var bar=page.Controls.OfType<FlowLayoutPanel>().FirstOrDefault();if(bar==null)return;
  Button(bar,"Pick walls in AutoCAD",PickWallsInAutoCad);bar.Visible=true;}
 void PickWallsInAutoCad(){var acad=AutoCadLink.Running();
  if(acad==null){MessageBox.Show(this,"Open the floor plan in AutoCAD first, then click Pick walls in AutoCAD.\n\nFull AutoCAD is needed (AutoCAD LT cannot be linked). You can also save the plan as DXF and use Load walls from DXF.","Pick walls in AutoCAD");return;}
  var used=new HashSet<string>(walls.Rows.Cast<DataGridViewRow>().Where(r=>!r.IsNewRow).Select(r=>Cell(r,0)).Where(s=>s!=""),StringComparer.OrdinalIgnoreCase);
  List<DxfWall> picked;string name="AutoCAD";try{dynamic a=acad;name="AutoCAD: "+(string)a.ActiveDocument.Name;}catch(Exception){}
  try{picked=AutoCadLink.PickWalls(acad,used,150);}catch(Exception ex){MessageBox.Show(this,"AutoCAD did not answer: "+ex.Message+"\nFinish or cancel the running AutoCAD command (Esc) and try again.","Pick walls in AutoCAD");return;}
  finally{try{var host=FindForm()??this;host.Activate();}catch(Exception){}}
  if(picked.Count==0){MessageBox.Show(this,"No wall was picked.","Pick walls in AutoCAD");return;}
  ImportWalls(picked,name,true);}
}
}
