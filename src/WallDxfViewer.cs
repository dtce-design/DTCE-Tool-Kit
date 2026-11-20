using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Drawing2D;using System.Globalization;using System.IO;using System.Linq;using System.Text;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* Wall IDs on the drawing inside the app (owner, 20 November 2026: "or upload DXF file, tapos mag open na lang ng window, tapos
   bubuksan sa app yung DXF tapos doon na magsusukat ng wall ID"). The DXF opens in a window; clicking a dimension or a wall line
   gives that wall run its length and a Wall ID (the dimension's own ID text, else the next free W number) shown on the drawing;
   "Measure 2 points" measures a run without a dimension (snapping to line ends); "Take all dimensions" picks every dimension.
   "Save DXF with Wall IDs" writes a copy of the drawing with the IDs as TEXT on layer DTCE-WALL-ID, for AutoCAD. */
sealed class DxfSeg {public double X1,Y1,X2,Y2,Measured,TextX,TextY;public string Layer="",OwnId;public bool Dimension;
 /* the dimension line as drawn: the measured points moved across to the dimension text */
 public double[] DimLine(){double dx=X2-X1,dy=Y2-Y1,l=Math.Sqrt(dx*dx+dy*dy);if(!Dimension||l<=0)return new[]{X1,Y1,X2,Y2};double nx=-dy/l,ny=dx/l,d=(TextX-X1)*nx+(TextY-Y1)*ny;return new[]{X1+nx*d,Y1+ny*d,X2+nx*d,Y2+ny*d};}
 public double Length{get{return Dimension&&Measured>0?Measured:Math.Sqrt((X2-X1)*(X2-X1)+(Y2-Y1)*(Y2-Y1));}}}
sealed class DxfLabel {public double X,Y,Height;public string Text,Layer;}
sealed class DxfDrawing {public int Units;public string Raw="";public readonly List<DxfSeg> Segs=new List<DxfSeg>();public readonly List<DxfLabel> Texts=new List<DxfLabel>();
 internal static DxfDrawing Parse(string text){var d=new DxfDrawing{Raw=text};int units;var ents=WallDxfImport.Entities(text,out units);d.Units=units;
  foreach(var e in ents){
   if(e.Type=="LINE"){var s=new DxfSeg{X1=e.D(10),Y1=e.D(20),X2=e.D(11),Y2=e.D(21),Layer=e.Layer};if(!double.IsNaN(s.X1+s.Y1+s.X2+s.Y2))d.Segs.Add(s);}
   else if(e.Type=="LWPOLYLINE"){var xs=e.All(10);var ys=e.All(20);int n=Math.Min(xs.Count,ys.Count);for(int k=0;k+1<n;k++)d.Segs.Add(new DxfSeg{X1=xs[k],Y1=ys[k],X2=xs[k+1],Y2=ys[k+1],Layer=e.Layer});
    int flag;if(int.TryParse(e.S(70),out flag)&&(flag&1)==1&&n>2)d.Segs.Add(new DxfSeg{X1=xs[n-1],Y1=ys[n-1],X2=xs[0],Y2=ys[0],Layer=e.Layer});}
   else if(e.Type=="DIMENSION"){var s=new DxfSeg{Dimension=true,X1=e.D(13),Y1=e.D(23),X2=e.D(14),Y2=e.D(24),Measured=e.D(42),TextX=e.D(11),TextY=e.D(21),Layer=e.Layer,OwnId=WallDxfImport.AsId(e.S(1))};
    if(double.IsNaN(s.X1+s.Y1+s.X2+s.Y2))continue;if(double.IsNaN(s.Measured))s.Measured=0;if(double.IsNaN(s.TextX+s.TextY)){s.TextX=(s.X1+s.X2)/2;s.TextY=(s.Y1+s.Y2)/2;}d.Segs.Add(s);}
   else if(e.Type=="TEXT"||e.Type=="MTEXT"){var l=new DxfLabel{X=e.D(10),Y=e.D(20),Height=e.D(40),Text=WallDxfImport.CleanText(e.S(1)+(e.Type=="MTEXT"?e.S(3):"")),Layer=e.Layer};if(!double.IsNaN(l.X+l.Y)&&l.Text!="")d.Texts.Add(l);}}
  return d;}
 internal RectangleF Extents(){var xs=Segs.SelectMany(s=>s.Dimension?new[]{s.X1,s.X2,s.TextX}:new[]{s.X1,s.X2}).Concat(Texts.Select(t=>t.X)).ToList();var ys=Segs.SelectMany(s=>s.Dimension?new[]{s.Y1,s.Y2,s.TextY}:new[]{s.Y1,s.Y2}).Concat(Texts.Select(t=>t.Y)).ToList();
  if(xs.Count==0)return new RectangleF(0,0,1,1);float x0=(float)xs.Min(),y0=(float)ys.Min();return new RectangleF(x0,y0,Math.Max(1e-3f,(float)xs.Max()-x0),Math.Max(1e-3f,(float)ys.Max()-y0));}
 /* the drawing text with the Wall IDs added as TEXT entities on layer DTCE-WALL-ID */
 internal string WithLabels(IEnumerable<DxfWall> walls,double height){var c=CultureInfo.InvariantCulture;var add=new StringBuilder();
  foreach(var w in walls)add.Append("0\nTEXT\n100\nAcDbEntity\n8\n"+AutoCadLink.LabelLayer+"\n62\n1\n100\nAcDbText\n10\n"+w.X.ToString("R",c)+"\n20\n"+w.Y.ToString("R",c)+"\n30\n0\n40\n"+height.ToString("R",c)+"\n1\n"+w.Id+"\n100\nAcDbText\n");
  string text=Raw.Replace("\r\n","\n");var lines=text.Split('\n');int entities=-1;
  for(int i=0;i+1<lines.Length;i+=2)if(lines[i].Trim()=="2"&&lines[i+1].Trim()=="ENTITIES"){entities=i;break;}
  if(entities<0)throw new Exception("The DXF has no ENTITIES section.");
  for(int i=entities;i+1<lines.Length;i+=2)if(lines[i].Trim()=="0"&&lines[i+1].Trim()=="ENDSEC"){var before=string.Join("\n",lines.Take(i));return before+"\n"+add.ToString()+string.Join("\n",lines.Skip(i));}
  throw new Exception("The ENTITIES section of the DXF is not closed.");}
}
sealed class WallDxfViewer:Form {
 readonly DxfDrawing drawing;readonly ISet<string> usedIds;readonly double defaultThickness;internal readonly List<DxfWall> Picked=new List<DxfWall>();readonly Dictionary<DxfWall,DxfSeg> pickedSeg=new Dictionary<DxfWall,DxfSeg>();
 readonly Canvas canvas;readonly DataGridView list=new DataGridView{Dock=DockStyle.Fill,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,SelectionMode=DataGridViewSelectionMode.FullRowSelect};
 readonly Label status=new Label{Dock=DockStyle.Bottom,Height=40,Padding=new Padding(10,4,10,4)};readonly CheckBox measure=new CheckBox{Text="Measure 2 points",Appearance=Appearance.Button,AutoSize=true};
 internal readonly string SourceName;double? firstX,firstY;
 /* linked to CHB Estimate (owner, 20 November 2026: "click ng row tapos magsusukat sa DXF viewer ilalapat niya agad yung wall ID";
    "ng CHB thickness sa CHB estimate na yun"): each pick goes straight to the selected row, which returns its Wall ID and thickness;
    null = the pick is not used */
 internal Func<DxfWall,bool> Assign;readonly Button addButton=new Button{Text="Add to CHB Estimate",AutoSize=true};readonly Button closeButton=new Button{Text="Cancel",AutoSize=true,DialogResult=DialogResult.Cancel};
 internal WallDxfViewer(DxfDrawing drawing,string sourceName,ISet<string> usedIds,double defaultThickness){this.drawing=drawing;this.usedIds=usedIds;this.defaultThickness=defaultThickness;SourceName=sourceName;
  AppBrand.Apply(this);Text="Wall IDs on "+sourceName;Width=1300;Height=820;MinimumSize=new Size(900,560);StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",9.5f);
  canvas=new Canvas(this){Dock=DockStyle.Fill};
  var tools=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,Padding=new Padding(8,6,8,4)};
  var all=new Button{Text="Take all dimensions",AutoSize=true};all.Click+=(s,e)=>{foreach(var seg in drawing.Segs.Where(x=>x.Dimension).OrderByDescending(x=>Math.Round(Mid(x).Y,1)).ThenBy(x=>Mid(x).X).ToList())Pick(seg);};
  var undo=new Button{Text="Undo last",AutoSize=true};undo.Click+=(s,e)=>{if(Picked.Count>0)Remove(Picked[Picked.Count-1]);};
  var fit=new Button{Text="Fit drawing",AutoSize=true};fit.Click+=(s,e)=>canvas.Fit();
  var save=new Button{Text="Save DXF with Wall IDs",AutoSize=true};save.Click+=(s,e)=>SaveWithLabels();
  measure.CheckedChanged+=(s,e)=>{firstX=firstY=null;Status();canvas.Invalidate();};
  tools.Controls.AddRange(new Control[]{all,measure,undo,fit,save});
  list.Columns.Add("Id","Wall ID");list.Columns.Add("Length","Length m");list.Columns.Add("Thickness","CHB thickness mm");list.Columns.Add("Layer","Layer");list.Columns[3].ReadOnly=true;
  list.CellEndEdit+=(s,e)=>ReadList();
  var remove=new Button{Text="Remove selected",AutoSize=true};remove.Click+=(s,e)=>{foreach(var w in list.SelectedRows.Cast<DataGridViewRow>().Select(r=>(DxfWall)r.Tag).ToList())Remove(w);};
  var side=new Panel{Dock=DockStyle.Right,Width=430,Padding=new Padding(8)};var sideBar=new FlowLayoutPanel{Dock=DockStyle.Bottom,AutoSize=true,FlowDirection=FlowDirection.RightToLeft};
  var add=addButton;add.Click+=(s,e)=>{list.EndEdit();ReadList();if(Picked.Count==0){MessageBox.Show(this,"Click the walls on the drawing first.",Text);return;}DialogResult=DialogResult.OK;};
  var cancel=closeButton;CancelButton=cancel;cancel.Click+=(s,e)=>{if(Assign!=null)Close();};sideBar.Controls.Add(cancel);sideBar.Controls.Add(add);sideBar.Controls.Add(remove);
  side.Controls.Add(list);side.Controls.Add(sideBar);side.Controls.Add(new Label{Dock=DockStyle.Top,Height=24,Text="Walls picked (edit ID, length or thickness here)",Font=new Font(Font,FontStyle.Bold)});
  Controls.Add(canvas);Controls.Add(side);Controls.Add(tools);Controls.Add(status);Status();Shown+=(s,e)=>canvas.Fit();}
 static PointF Mid(DxfSeg s){return new PointF((float)((s.X1+s.X2)/2),(float)((s.Y1+s.Y2)/2));}
 internal void Linked(){addButton.Visible=false;closeButton.Text="Close";Text=Text+"  -  linked to CHB Estimate";list.ReadOnly=true;Status();}
 void Status(){status.Text=Assign!=null&&!measure.Checked?"Select a row on CHB Estimate, then click its dimension or wall line here (or Measure 2 points): the width goes to that row and its Wall ID is written on the drawing. A blank new row gets a new Wall ID. "+Picked.Count+" measured.":measure.Checked?(firstX==null?"Measure: click the first end of the wall run (snaps to line ends).":"Measure: click the second end."):"Click a dimension or a wall line to give it a Wall ID. Mouse wheel = zoom, right or middle drag = pan. "+Picked.Count+" wall(s) picked.";}
 double ToM(double v){return Math.Round(WallDxfImport.ToMetres(v,drawing.Units),3);}
 string NextId(){var used=new HashSet<string>(usedIds.Concat(Picked.Select(w=>w.Id)),StringComparer.OrdinalIgnoreCase);return WallDxfImport.NextId(used);}
 /* picks a segment (dimension or wall line) as one wall; returns null when it is already picked */
 internal DxfWall Pick(DxfSeg seg){if(pickedSeg.Values.Contains(seg))return null;var taken=new HashSet<string>(Picked.Select(x=>x.Id),StringComparer.OrdinalIgnoreCase);
  var at=seg.Dimension?new PointF((float)seg.TextX,(float)seg.TextY):Mid(seg);string own=seg.OwnId;
  if(own==null){/* the nearest ID-like text label (within 1/5 of the drawing) */var ext=drawing.Extents();double reach=Math.Max(ext.Width,ext.Height)/5;
   var near=drawing.Texts.Select(t=>new{Id=WallDxfImport.AsId(t.Text),D=Math.Sqrt(Math.Pow(t.X-at.X,2)+Math.Pow(t.Y-at.Y,2))}).Where(t=>t.Id!=null&&t.D<=reach&&!taken.Contains(t.Id)&&!usedIds.Contains(t.Id)).OrderBy(t=>t.D).FirstOrDefault();if(near!=null)own=near.Id;}
  string id=own!=null&&!taken.Contains(own)?own:NextId();
  var w=new DxfWall{Id=id,IdFromDrawing=own!=null&&id==own,Layer=seg.Layer,LengthM=ToM(seg.Length),ThicknessMm=WallDxfImport.LayerThickness(seg.Layer)??defaultThickness,Source=seg.Dimension?"Dimension":"Wall line",X=at.X,Y=at.Y};
  if(Assign!=null&&!Assign(w))return null;Picked.Add(w);pickedSeg[w]=seg;Refresh2();return w;}
 internal DxfWall Measure(double x1,double y1,double x2,double y2){double len=Math.Sqrt((x2-x1)*(x2-x1)+(y2-y1)*(y2-y1));if(len<=0)return null;
  var w=new DxfWall{Id=NextId(),Layer="",LengthM=ToM(len),ThicknessMm=defaultThickness,Source="Measured",X=(x1+x2)/2,Y=(y1+y2)/2};if(Assign!=null&&!Assign(w))return null;Picked.Add(w);pickedSeg[w]=new DxfSeg{X1=x1,Y1=y1,X2=x2,Y2=y2,Layer="(measured)"};Refresh2();return w;}
 void Remove(DxfWall w){Picked.Remove(w);pickedSeg.Remove(w);Refresh2();}
 void Refresh2(){list.Rows.Clear();foreach(var w in Picked){int n=list.Rows.Add(w.Id,w.LengthM.ToString("0.###",CultureInfo.InvariantCulture),w.ThicknessMm.ToString("0",CultureInfo.InvariantCulture),w.Layer);list.Rows[n].Tag=w;}Status();canvas.Invalidate();}
 void ReadList(){foreach(DataGridViewRow r in list.Rows){var w=r.Tag as DxfWall;if(w==null)continue;string id=Convert.ToString(r.Cells[0].Value).Trim();if(id!="")w.Id=id;double v;
   if(double.TryParse(Convert.ToString(r.Cells[1].Value),NumberStyles.Float,CultureInfo.InvariantCulture,out v)&&v>0)w.LengthM=v;if(double.TryParse(Convert.ToString(r.Cells[2].Value),NumberStyles.Float,CultureInfo.InvariantCulture,out v)&&v>0)w.ThicknessMm=v;}canvas.Invalidate();}
 double LabelHeight(){var ext=drawing.Extents();return Math.Max(ext.Width,ext.Height)/60;}
 void SaveWithLabels(){list.EndEdit();ReadList();if(Picked.Count==0){MessageBox.Show(this,"Click the walls on the drawing first.",Text);return;}
  using(var dialog=new SaveFileDialog{Filter="AutoCAD DXF (*.dxf)|*.dxf",FileName=Path.GetFileNameWithoutExtension(SourceName)+" - Wall IDs.dxf"}){if(dialog.ShowDialog(this)!=DialogResult.OK)return;
   try{File.WriteAllText(dialog.FileName,drawing.WithLabels(Picked,LabelHeight()));MessageBox.Show(this,"Saved. Open it in AutoCAD: the Wall IDs are on layer "+AutoCadLink.LabelLayer+".",Text);}catch(Exception ex){MessageBox.Show(this,ex.Message,Text);}}}
 /* nearest dimension or wall line to a world point within the given world distance (dimensions first) */
 internal DxfSeg HitTest(double x,double y,double reach){DxfSeg best=null;double bestD=reach;foreach(var s in drawing.Segs.OrderBy(s=>s.Dimension?0:1)){var dl=s.DimLine();double d=Dist(new DxfSeg{X1=dl[0],Y1=dl[1],X2=dl[2],Y2=dl[3]},x,y);if(s.Dimension)d=Math.Min(d,Math.Sqrt(Math.Pow(s.TextX-x,2)+Math.Pow(s.TextY-y,2)));if(d<bestD-(s.Dimension?0:1e-9)){bestD=d;best=s;}}return best;}
 static double Dist(DxfSeg s,double x,double y){double dx=s.X2-s.X1,dy=s.Y2-s.Y1,l2=dx*dx+dy*dy;double t=l2<=0?0:Math.Max(0,Math.Min(1,((x-s.X1)*dx+(y-s.Y1)*dy)/l2));double px=s.X1+t*dx,py=s.Y1+t*dy;return Math.Sqrt((x-px)*(x-px)+(y-py)*(y-py));}
 internal PointF Snap(double x,double y,double reach){double bx=x,by=y,bd=reach;foreach(var s in drawing.Segs.Where(z=>!z.Dimension))foreach(var p in new[]{new[]{s.X1,s.Y1},new[]{s.X2,s.Y2}}){double d=Math.Sqrt(Math.Pow(p[0]-x,2)+Math.Pow(p[1]-y,2));if(d<bd){bd=d;bx=p[0];by=p[1];}}return new PointF((float)bx,(float)by);}
 sealed class Canvas:Panel {
  readonly WallDxfViewer owner;double scale=1,offX,offY;Point? drag;Point dragStart;double dragOffX,dragOffY;
  internal Canvas(WallDxfViewer owner){this.owner=owner;DoubleBuffered=true;BackColor=Color.FromArgb(30,30,30);
   MouseWheel+=(s,e)=>{var w=ToWorld(e.Location);scale*=e.Delta>0?1.2:1/1.2;offX=e.X-w.X*scale;offY=e.Y+w.Y*scale;Invalidate();};
   MouseDown+=(s,e)=>{Focus();if(e.Button==MouseButtons.Right||e.Button==MouseButtons.Middle){drag=e.Location;dragStart=e.Location;dragOffX=offX;dragOffY=offY;return;}if(e.Button==MouseButtons.Left)ClickAt(e.Location);};
   MouseMove+=(s,e)=>{if(drag!=null){offX=dragOffX+e.X-dragStart.X;offY=dragOffY+e.Y-dragStart.Y;Invalidate();}};
   MouseUp+=(s,e)=>{drag=null;};Resize+=(s,e)=>Invalidate();}
  internal void Fit(){var ext=owner.drawing.Extents();float pad=Math.Max(ext.Width,ext.Height)*0.08f;ext=new RectangleF(ext.X-pad,ext.Y-pad,ext.Width+2*pad,ext.Height+2*pad);if(Width<10||Height<10)return;scale=Math.Min((Width-40)/ext.Width,(Height-40)/ext.Height);offX=20-ext.X*scale+((Width-40)-ext.Width*scale)/2;offY=20+(ext.Y+ext.Height)*scale+((Height-40)-ext.Height*scale)/2;Invalidate();}
  PointF ToScreen(double x,double y){return new PointF((float)(offX+x*scale),(float)(offY-y*scale));}
  PointF ToWorld(Point p){return new PointF((float)((p.X-offX)/scale),(float)((offY-p.Y)/scale));}
  void ClickAt(Point p){var w=ToWorld(p);double reach=10/scale;
   if(owner.measure.Checked){var s=owner.Snap(w.X,w.Y,reach);if(owner.firstX==null){owner.firstX=s.X;owner.firstY=s.Y;owner.Status();Invalidate();return;}owner.Measure(owner.firstX.Value,owner.firstY.Value,s.X,s.Y);owner.firstX=owner.firstY=null;owner.Status();return;}
   var seg=owner.HitTest(w.X,w.Y,reach);if(seg!=null)owner.Pick(seg);}
  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;var picked=new HashSet<DxfSeg>(owner.pickedSeg.Values);
   foreach(var s in owner.drawing.Segs.OrderBy(z=>z.Dimension?1:0)){var dl=s.DimLine();var a=ToScreen(dl[0],dl[1]);var b=ToScreen(dl[2],dl[3]);
    if(s.Dimension)using(var ext=new Pen(Color.FromArgb(110,150,229,155),1f)){g.DrawLine(ext,ToScreen(s.X1,s.Y1),a);g.DrawLine(ext,ToScreen(s.X2,s.Y2),b);}double? t=WallDxfImport.LayerThickness(s.Layer);
    Color c=s.Dimension?Color.FromArgb(150,229,155):t==100?Color.FromArgb(255,209,102):t!=null?Color.FromArgb(92,200,255):Color.FromArgb(170,170,170);float width=s.Dimension?1f:t!=null?2.5f:1f;
    if(picked.Contains(s)){c=Color.FromArgb(255,140,60);width+=2;}
    using(var pen=new Pen(c,width)){if(s.Dimension)pen.DashStyle=DashStyle.Dash;g.DrawLine(pen,a,b);}
    if(s.Dimension&&s.Measured>0){var tp=ToScreen(s.TextX,s.TextY);using(var br=new SolidBrush(c))g.DrawString((s.OwnId!=null?s.OwnId+"  ":"")+s.Measured.ToString("0.###",CultureInfo.InvariantCulture),Font,br,tp);}}
   using(var br=new SolidBrush(Color.FromArgb(200,200,200)))foreach(var t in owner.drawing.Texts)g.DrawString(t.Text,Font,br,ToScreen(t.X,t.Y));
   using(var big=new Font("Segoe UI",11f,FontStyle.Bold))using(var tag=new SolidBrush(Color.FromArgb(255,143,171)))using(var back=new SolidBrush(Color.FromArgb(200,30,30,30)))
    foreach(var w in owner.Picked){var p=ToScreen(w.X,w.Y);string label=w.Id+" = "+w.LengthM.ToString("0.###",CultureInfo.InvariantCulture)+" m";var size=g.MeasureString(label,big);var r=new RectangleF(p.X-size.Width/2,p.Y-size.Height-4,size.Width,size.Height);g.FillRectangle(back,r);g.DrawString(label,big,tag,r.Location);}
   if(owner.firstX!=null){var p=ToScreen(owner.firstX.Value,owner.firstY.Value);using(var pen=new Pen(Color.Orange,2))g.DrawEllipse(pen,p.X-5,p.Y-5,10,10);}}
 }
}
partial class ProgramOfWorksForm {
 /* "Load walls from DXF": the drawing opens in its own window, linked to CHB Estimate (owner, 20 November 2026) */
 WallDxfViewer wallViewer;
 internal WallDxfViewer OpenWallDxfViewer(string path){string text=File.ReadAllText(path);var drawing=DxfDrawing.Parse(text);if(drawing.Segs.Count==0)throw new Exception("No lines or dimensions were found in this DXF (blocks are not exploded: explode the plan or use plain lines).");
  if(wallViewer!=null&&!wallViewer.IsDisposed)wallViewer.Close();
  var used=new HashSet<string>(walls.Rows.Cast<DataGridViewRow>().Where(r=>!r.IsNewRow).Select(r=>Cell(r,0)).Where(s=>s!=""),StringComparer.OrdinalIgnoreCase);
  wallViewer=new WallDxfViewer(drawing,Path.GetFileName(path),used,150){Assign=AssignFromViewer,StartPosition=FormStartPosition.Manual};wallViewer.Linked();
  var screen=Screen.FromControl(this).WorkingArea;wallViewer.Bounds=new Rectangle(screen.Left+screen.Width/2,screen.Top,screen.Width/2,screen.Height);
  wallViewer.Show(FindForm()??this);return wallViewer;}
 /* a measurement from the DXF viewer goes to the selected CHB Estimate row: width from the drawing, Wall ID and thickness from the row */
 internal bool AssignFromViewer(DxfWall w){walls.EndEdit();var row=walls.CurrentRow;bool before=filling;filling=true;
  try{if(row==null||row.IsNewRow){var works=walls as WorksDataGridView;row=works!=null?works.AddRowWithDefaults():walls.Rows[walls.Rows.Add()];
     var used=new HashSet<string>(walls.Rows.Cast<DataGridViewRow>().Where(r=>!r.IsNewRow&&r!=row).Select(r=>Cell(r,0)).Where(s=>s!=""),StringComparer.OrdinalIgnoreCase);
     if(Cell(row,0)==""||used.Contains(Cell(row,0)))row.Cells[0].Value=used.Contains(w.Id)?WallDxfImport.NextId(used):w.Id;if(Cell(row,1)=="")row.Cells[1].Value="From "+(wallViewer!=null?wallViewer.SourceName:"DXF");if(Cell(row,4)=="")row.Cells[4].Value="1";}
   if(Cell(row,0)=="")row.Cells[0].Value=w.Id;
   row.Cells[2].Value=w.LengthM.ToString("0.###",CultureInfo.InvariantCulture);if(Cell(row,19)!="")row.Cells[19].Value="";
   w.Id=Cell(row,0);double t;if(double.TryParse(Cell(row,18),NumberStyles.Float,CultureInfo.InvariantCulture,out t)&&t>0)w.ThicknessMm=t;
   int next=row.Index+1;if(next<walls.Rows.Count){try{walls.CurrentCell=walls.Rows[next].Cells[0].Visible?walls.Rows[next].Cells[0]:walls.Rows[next].Cells[2];}catch(InvalidOperationException){}}}
  finally{filling=before;}Recalculate();return true;}
}
}
