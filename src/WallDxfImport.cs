using System;using System.Collections.Generic;using System.Drawing;using System.Globalization;using System.IO;using System.Linq;using System.Text.RegularExpressions;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* Walls from an AutoCAD DXF (owner, 20 November 2026: "ilagay ko yung drawing sa DXF tapos, sa masonry works, may button akong
   iclick para iassign yung wall ... pwedeng dimension para kapag susukatin automatic na din kukunin yung width niya").
   Every DIMENSION in the drawing is one wall run: its measured value is the wall length. The Wall ID is the dimension's own
   text when it looks like an ID (W-01, W1, CHB-3 ...), else the nearest ID-like TEXT / MTEXT, else the next free W number.
   The thickness comes from a number in the layer name (CHB-100, WALL_150 ...), else the chosen default. Without dimensions,
   LINE / LWPOLYLINE segments on layers named with WALL, CHB or MASONRY are used. Units follow $INSUNITS (mm / m); without it,
   values above 50 are read as millimetres. */
sealed class DxfWall {public string Id,Layer,Source;public double LengthM,ThicknessMm;public bool IdFromDrawing;public double X,Y;}
static class WallDxfImport {
 static readonly CultureInfo IC=CultureInfo.InvariantCulture;
 static readonly Regex IdPattern=new Regex(@"^\s*([A-Za-z]{1,4}\s*-?\s*\d{1,4}[A-Za-z]?)\s*$");
 sealed class Ent {public string Type,Layer="";public Dictionary<int,List<string>> G=new Dictionary<int,List<string>>();
  public string S(int c){List<string> v;return G.TryGetValue(c,out v)&&v.Count>0?v[0]:"";}
  public double D(int c){double d;return double.TryParse(S(c),NumberStyles.Float,IC,out d)?d:double.NaN;}
  public List<double> All(int c){List<string> v;var r=new List<double>();if(G.TryGetValue(c,out v))foreach(var s in v){double d;if(double.TryParse(s,NumberStyles.Float,IC,out d))r.Add(d);}return r;}}
 internal static string CleanText(string t){t=Regex.Replace(t??"",@"\\[A-Za-z][^;\\]*;","");t=t.Replace("\\P"," ").Replace("{","").Replace("}","").Replace("<>","");return t.Trim();}
 internal static string AsId(string text){var m=IdPattern.Match(CleanText(text));return m.Success?Regex.Replace(m.Groups[1].Value,@"\s+","").ToUpperInvariant():null;}
 internal static double? LayerThickness(string layer){var m=Regex.Match(layer??"",@"(?<!\d)(75|90|100|125|150|175|200|250|300)(?!\d)");return m.Success?double.Parse(m.Value,IC):(double?)null;}
 static List<Ent> Entities(string text,out int units){units=0;var lines=text.Replace("\r\n","\n").Replace('\r','\n').Split('\n');var list=new List<Ent>();bool inEntities=false;Ent cur=null;
  for(int i=0;i+1<lines.Length;i+=2){int code;if(!int.TryParse(lines[i].Trim(),out code))continue;string v=lines[i+1].Trim();
   if(code==9&&v=="$INSUNITS"&&i+3<lines.Length){int u;if(int.TryParse(lines[i+3].Trim(),out u))units=u;continue;}
   if(code==0){if(cur!=null){list.Add(cur);cur=null;}if(v=="SECTION")continue;if(v=="ENDSEC"){inEntities=false;continue;}if(inEntities)cur=new Ent{Type=v};continue;}
   if(code==2&&!inEntities&&v=="ENTITIES"){inEntities=true;continue;}
   if(cur==null)continue;if(code==8)cur.Layer=v;List<string> slot;if(!cur.G.TryGetValue(code,out slot))cur.G[code]=slot=new List<string>();slot.Add(v);}
  if(cur!=null)list.Add(cur);return list;}
 static double ToMetres(double value,int units){if(units==6)return value;if(units==4)return value/1000;if(units==5)return value/100;if(units==1)return value*0.0254;return value>50?value/1000:value;}
 /* reads the walls of a DXF; defaultThicknessMm when the layer carries none */
 internal static List<DxfWall> Read(string text,double defaultThicknessMm){int units;var ents=Entities(text,out units);
  var labels=ents.Where(e=>e.Type=="TEXT"||e.Type=="MTEXT").Select(e=>new{Id=AsId(e.S(1)+(e.Type=="MTEXT"?e.S(3):"")),X=e.D(10),Y=e.D(20)}).Where(l=>l.Id!=null&&!double.IsNaN(l.X)&&!double.IsNaN(l.Y)).ToList();
  var walls=new List<DxfWall>();
  foreach(var d in ents.Where(e=>e.Type=="DIMENSION")){double measured=d.D(42);double x1=d.D(13),y1=d.D(23),x2=d.D(14),y2=d.D(24);
   if(double.IsNaN(measured)||measured<=0){if(double.IsNaN(x1+y1+x2+y2))continue;measured=Math.Sqrt((x2-x1)*(x2-x1)+(y2-y1)*(y2-y1));}
   if(measured<=0)continue;double cx=double.IsNaN(x1+x2)?d.D(11):(x1+x2)/2,cy=double.IsNaN(y1+y2)?d.D(21):(y1+y2)/2;
   var w=new DxfWall{LengthM=Math.Round(ToMetres(measured,units),3),Layer=d.Layer,ThicknessMm=LayerThickness(d.Layer)??defaultThicknessMm,Source="Dimension",X=cx,Y=cy};
   string own=AsId(d.S(1));if(own!=null){w.Id=own;w.IdFromDrawing=true;}walls.Add(w);}
  if(walls.Count==0)foreach(var e in ents.Where(x=>(x.Type=="LINE"||x.Type=="LWPOLYLINE")&&Regex.IsMatch(x.Layer??"","WALL|CHB|MASONRY|BLOCK",RegexOptions.IgnoreCase))){
   var xs=e.Type=="LINE"?new List<double>{e.D(10),e.D(11)}:e.All(10);var ys=e.Type=="LINE"?new List<double>{e.D(20),e.D(21)}:e.All(20);int n=Math.Min(xs.Count,ys.Count);
   for(int k=0;k+1<n;k++){double len=Math.Sqrt(Math.Pow(xs[k+1]-xs[k],2)+Math.Pow(ys[k+1]-ys[k],2));if(len<=0||double.IsNaN(len))continue;
    walls.Add(new DxfWall{LengthM=Math.Round(ToMetres(len,units),3),Layer=e.Layer,ThicknessMm=LayerThickness(e.Layer)??defaultThicknessMm,Source="Wall line",X=(xs[k]+xs[k+1])/2,Y=(ys[k]+ys[k+1])/2});}}
  /* nearest ID-like label for walls without one (within 20% of the drawing size) */
  if(labels.Count>0&&walls.Count>0){double span=Math.Max(1e-9,Math.Max(walls.Max(w=>w.X)-walls.Min(w=>w.X),walls.Max(w=>w.Y)-walls.Min(w=>w.Y)));double reach=Math.Max(span*0.2,units==6?2:units==5?200:2000);/* 20% of the drawing, at least about 2 m */
   var taken=new HashSet<string>(walls.Where(w=>w.Id!=null).Select(w=>w.Id),StringComparer.OrdinalIgnoreCase);
   foreach(var w in walls.Where(x=>x.Id==null).OrderBy(x=>x.Y).ThenBy(x=>x.X)){var best=labels.Where(l=>!taken.Contains(l.Id)).Select(l=>new{l.Id,Dist=Math.Sqrt(Math.Pow(l.X-w.X,2)+Math.Pow(l.Y-w.Y,2))}).Where(l=>l.Dist<=reach).OrderBy(l=>l.Dist).FirstOrDefault();
    if(best!=null){w.Id=best.Id;w.IdFromDrawing=true;taken.Add(best.Id);}}}
  /* plan order: top to bottom, left to right */
  return walls.OrderByDescending(w=>Math.Round(w.Y,1)).ThenBy(w=>w.X).ToList();}
 internal static string NextId(ISet<string> used){for(int n=1;;n++){string id="W"+n.ToString("000",IC);if(!used.Contains(id))return id;}}
}
partial class ProgramOfWorksForm {
 void AddWallDxfButton(){var page=tabs.TabPages.Cast<TabPage>().FirstOrDefault(p=>p.Text=="CHB Estimate");if(page==null)return;var bar=page.Controls.OfType<FlowLayoutPanel>().FirstOrDefault();if(bar==null)return;
  Button(bar,"Load walls from DXF",LoadWallsFromDxf);bar.Visible=true;}
 void LoadWallsFromDxf(){string path;using(var open=new OpenFileDialog{Title="Open the floor plan DXF with the wall dimensions",Filter="AutoCAD DXF (*.dxf)|*.dxf"}){if(open.ShowDialog(this)!=DialogResult.OK)return;path=open.FileName;}
  List<DxfWall> found;try{found=WallDxfImport.Read(File.ReadAllText(path),150);}catch(Exception ex){MessageBox.Show(this,"Cannot read the DXF: "+ex.Message,"Load walls from DXF");return;}
  if(found.Count==0){MessageBox.Show(this,"No wall dimensions were found. Dimension each CHB wall run (aligned or linear dimension), or draw the walls on a layer named WALL / CHB, and save as DXF.","Load walls from DXF");return;}
  ImportWalls(found,Path.GetFileName(path),true);}
 /* shows the walls found for review and adds / updates them on CHB Estimate; returns the number of rows added or updated */
 internal int ImportWalls(List<DxfWall> found,string source,bool ask){
  var used=new HashSet<string>(walls.Rows.Cast<DataGridViewRow>().Where(r=>!r.IsNewRow).Select(r=>Cell(r,0)).Where(s=>s!=""),StringComparer.OrdinalIgnoreCase);
  var reserved=new HashSet<string>(used.Concat(found.Where(w=>w.Id!=null).Select(w=>w.Id)),StringComparer.OrdinalIgnoreCase);
  foreach(var w in found.Where(x=>x.Id==null)){w.Id=WallDxfImport.NextId(reserved);reserved.Add(w.Id);}
  /* height is typed by the owner on CHB Estimate ("height is manual input"); new rows start blank */
  var include=found.Select(w=>true).ToList();
  if(ask){using(var form=new Form{Text="Walls from "+source,Width=900,Height=560,MinimumSize=new Size(700,420),StartPosition=FormStartPosition.CenterParent,Font=Font}){AppBrand.Apply(form);
    var grid=new DataGridView{Dock=DockStyle.Fill,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill};
    grid.Columns.Add(new DataGridViewCheckBoxColumn{HeaderText="Add",FillWeight=30});foreach(var h in new[]{"Wall ID","Length m","CHB thickness mm","Layer","From"})grid.Columns.Add(h,h);grid.Columns[4].ReadOnly=grid.Columns[5].ReadOnly=true;
    foreach(var w in found){int n=grid.Rows.Add(true,w.Id,w.LengthM.ToString("0.###",CultureInfo.InvariantCulture),w.ThicknessMm.ToString("0",CultureInfo.InvariantCulture),w.Layer,w.Source+(used.Contains(w.Id)?" (updates existing row)":""));}
    var top=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,Padding=new Padding(10,6,10,4)};
    top.Controls.Add(new Label{AutoSize=true,MaximumSize=new Size(840,0),ForeColor=PowTheme.Muted,Padding=new Padding(0,6,0,0),Text=found.Count+" wall run(s) found. Edit the ID, length or thickness before adding; an existing Wall ID gets its length and thickness updated. Enter the wall heights on CHB Estimate."});
    var bottom=new FlowLayoutPanel{Dock=DockStyle.Bottom,AutoSize=true,FlowDirection=FlowDirection.RightToLeft,Padding=new Padding(10)};var ok=new Button{Text="Add to CHB Estimate",AutoSize=true};var cancel=new Button{Text="Cancel",AutoSize=true,DialogResult=DialogResult.Cancel};bottom.Controls.Add(cancel);bottom.Controls.Add(ok);form.CancelButton=cancel;
    ok.Click+=(s,e)=>{grid.EndEdit();
     for(int i=0;i<found.Count;i++){var r=grid.Rows[i];include[i]=Convert.ToBoolean(r.Cells[0].Value);string id=Convert.ToString(r.Cells[1].Value).Trim();double len=0,t=0;
      if(include[i]&&(id==""||!double.TryParse(Convert.ToString(r.Cells[2].Value),NumberStyles.Float,CultureInfo.InvariantCulture,out len)||len<=0||!double.TryParse(Convert.ToString(r.Cells[3].Value),NumberStyles.Float,CultureInfo.InvariantCulture,out t)||t<=0)){MessageBox.Show(form,"Check row "+(i+1)+": Wall ID, length and thickness.",form.Text);return;}
      if(include[i]){found[i].Id=id;found[i].LengthM=len;found[i].ThicknessMm=t;}}
     form.DialogResult=DialogResult.OK;};
    form.Controls.Add(grid);form.Controls.Add(top);form.Controls.Add(bottom);if(form.ShowDialog(this)!=DialogResult.OK)return 0;}}
  int changed=0;bool before=filling;filling=true;try{
   for(int i=0;i<found.Count;i++){if(!include[i])continue;var w=found[i];var row=walls.Rows.Cast<DataGridViewRow>().FirstOrDefault(r=>!r.IsNewRow&&string.Equals(Cell(r,0),w.Id,StringComparison.OrdinalIgnoreCase));
    if(row==null){var works=walls as WorksDataGridView;row=works!=null?works.AddRowWithDefaults():walls.Rows[walls.Rows.Add()];row.Cells[0].Value=w.Id;row.Cells[1].Value="From "+source+(w.Layer!=""?" ("+w.Layer+")":"");row.Cells[4].Value="1";}
    row.Cells[2].Value=w.LengthM.ToString("0.###",CultureInfo.InvariantCulture);string t=w.ThicknessMm.ToString("0",CultureInfo.InvariantCulture);var thick=walls.Columns[18] as DataGridViewComboBoxColumn;if(thick==null||thick.Items.Contains(t))row.Cells[18].Value=t;changed++;}}
  finally{filling=before;}Recalculate();return changed;}
}
}
