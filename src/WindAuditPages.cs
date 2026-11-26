using System;using System.Collections.Generic;using System.Drawing;using System.Linq;
namespace BeamScheduleFormatter {
// Drawn PDF/print pages for the exposure/Kzt explanation and STAAD wind data. Rows grow to fit wrapped text.
static class WindAuditPages {
 /* column widths scaled to the content width inside the 1 inch binding margin (owner screenshot: tables ran past the right margin) */
 static float[] Fit(params float[] w){float sum=w.Sum();return w.Select(x=>x/sum*CalculationDocument.CW).ToArray();}
 class Block {public string Title,Text;public string[] Headers;public float[] Widths;public List<string[]> Rows;public float FigureHeight;public Action<Graphics,RectangleF> Figure;}
 const float Left=CalculationDocument.L,Width=CalculationDocument.CW,Top=130,Bottom=790;
 /* each upwind ray from the site as a band coloured by the mapped surface roughness, with the B fetch, D fetch and D fetch + transition */
 static void RoughnessRays(Graphics g,RectangleF rc,List<List<WindSample>> rays,WindExposureAudit audit,string name,double bearing,WindTerrain terrain=null,int dirIndex=-1){
  /* the roughness circle with this direction's upwind sectors highlighted (owner: "siguro sa circle or sa map") */
  if(terrain!=null){float cs=Math.Min(rc.Height,172);WindRoughnessCircle.Draw(g,new RectangleF(rc.Left,rc.Top+(rc.Height-cs)/2,cs,cs),terrain,null,dirIndex,audit.Height,false);rc=new RectangleF(rc.Left+cs+12,rc.Top,rc.Width-cs-12,rc.Height);}
  using(var f=new Font("Segoe UI",7f,FontStyle.Regular,GraphicsUnit.Point))using(var b=new Font("Segoe UI",7f,FontStyle.Bold,GraphicsUnit.Point))using(var ink=new SolidBrush(PowTheme.Ink))using(var muted=new SolidBrush(PowTheme.Muted))using(var frame=new Pen(Color.FromArgb(150,160,170),.6f)){
   g.DrawString("Upwind rays - wind from "+name,b,ink,rc.Left,rc.Top);
   float labelW=62,top=rc.Top+32,bandH=13,gap=5,plotL=rc.Left+labelW,plotW=rc.Width-labelW-8;
   double far=Math.Max(rays.Count==0?0:rays.Max(r=>r.Count==0?0:r.Last().D),Math.Max(WindTin.Finite(audit.BFetch)?audit.BFetch:0,WindTin.Finite(audit.DFetch)?audit.DFetch+audit.Transition:0));if(far<=0)far=1000;
   Func<double,float> X=dist=>plotL+(float)(dist/far)*plotW;string[] offsets={"−45°","−22.5°","0°","+22.5°","+45°"};
   for(int k=0;k<rays.Count;k++){float y=top+k*(bandH+gap);var ray=rays[k];double brg=((bearing+(k-2)*22.5)%360+360)%360;
    g.DrawString((k<offsets.Length?offsets[k]:"")+" ("+brg.ToString("0.#")+"°)",f,ink,rc.Left,y+1);
    for(int i=0;i<ray.Count;i++){double d0=ray[i].D,d1=i+1<ray.Count?ray[i+1].D:ray[i].D+50;using(var fill=new SolidBrush(WindColors.Cover(ray[i].Cover)))g.FillRectangle(fill,X(d0),y,Math.Max(.5f,X(d1)-X(d0)),bandH);}
    g.DrawRectangle(frame,plotL,y,X(ray.Count==0?0:ray.Last().D+50)-plotL,bandH);}
   float bottom=top+rays.Count*(bandH+gap)-gap;
   /* distance axis */
   double step=far>4000?1000:far>2000?500:far>800?200:100;using(var axis=new Pen(PowTheme.Ink,.6f)){g.DrawLine(axis,plotL,bottom+3,plotL+plotW,bottom+3);for(double t=0;t<=far+1e-6;t+=step){g.DrawLine(axis,X(t),bottom+3,X(t),bottom+6);var txt=t.ToString("0");var sz=g.MeasureString(txt,f);g.DrawString(txt,f,ink,X(t)-sz.Width/2,bottom+6);}g.DrawString("distance upwind from the site (m)",f,muted,plotL+plotW/2-60,bottom+16);}
   /* fetch limits */
   var marks=new List<KeyValuePair<double,string>>();if(WindTin.Finite(audit.Transition)&&audit.Transition>0)marks.Add(new KeyValuePair<double,string>(audit.Transition,"D transition "+audit.Transition.ToString("0")+" m"));if(WindTin.Finite(audit.BFetch)&&audit.BFetch>0)marks.Add(new KeyValuePair<double,string>(audit.BFetch,"B fetch "+audit.BFetch.ToString("0")+" m"));if(WindTin.Finite(audit.DFetch)&&audit.DFetch>0)marks.Add(new KeyValuePair<double,string>(audit.DFetch,"D fetch "+audit.DFetch.ToString("0")+" m"));
   using(var dash=new Pen(Color.FromArgb(40,52,64),.9f){DashStyle=System.Drawing.Drawing2D.DashStyle.Dash})for(int m=0;m<marks.Count;m++){float x=X(marks[m].Key);g.DrawLine(dash,x,top-3,x,bottom+3);var sz=g.MeasureString(marks[m].Value,f);float lx=Math.Min(x+2,rc.Right-sz.Width);g.DrawString(marks[m].Value,f,ink,lx,rc.Top+11+(m%2)*9);}
   /* legend */
   float lgx=plotL,lgy=bottom+28;foreach(var item in new[]{new KeyValuePair<string,string>("B","B built-up / trees"),new KeyValuePair<string,string>("C","C open"),new KeyValuePair<string,string>("D","D water"),new KeyValuePair<string,string>("?","no data")}){using(var fill=new SolidBrush(WindColors.Roughness(item.Key)))g.FillRectangle(fill,lgx,lgy+1,12,8);g.DrawRectangle(frame,lgx,lgy+1,12,8);g.DrawString(item.Value,f,ink,lgx+15,lgy-1);lgx+=g.MeasureString(item.Value,f).Width+26;}
   {var se="Suggested exposure "+audit.Exposure;g.DrawString(se,b,ink,rc.Right-g.MeasureString(se,b).Width,rc.Top);}}}
 static float Measure(string text,float width,float size,bool bold){using(var bmp=new Bitmap(1,1))using(var g=Graphics.FromImage(bmp)){g.PageUnit=GraphicsUnit.Point;using(var font=new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Point))return g.MeasureString(text??"",font,(int)Math.Max(10,width)).Height;}}
 static float RowHeight(string[] row,float[] widths,bool bold){float h=0;for(int c=0;c<widths.Length;c++)h=Math.Max(h,Measure(c<row.Length?row[c]:"",widths[c]-8,7f,bold));return h+6;}
 internal static void Add(CalculationDocument doc,List<WindDirection> directions,List<WindStaadBand> bands,string commands,WindTerrain terrain=null,Dictionary<int,string> faces=null){
  /* with a building: the face directions first (in load case order), each titled with its face; the other assessed directions after,
     marked as reference (no building face is normal to them) */
  Func<WindDirection,int> idx=d=>terrain!=null?d.Index:Array.IndexOf(WindAssessment.Directions,d.Name);
  if(faces!=null&&faces.Count>0)directions=directions.OrderBy(d=>faces.ContainsKey(idx(d))?0:1).ThenBy(d=>{string f;return faces.TryGetValue(idx(d),out f)?f:"";}).ToList();
  var blocks=new List<Block>();
  foreach(var d in directions){if(d.ExposureAudit==null&&d.TopographicChecks.Count==0)continue;string face=null;if(faces!=null&&faces.Count>0&&!faces.TryGetValue(idx(d),out face))face="";blocks.Add(new Block{Title="EXPOSURE AND Kzt BASIS / WIND FROM "+d.Name+(face==null?"":face==""?" (REFERENCE, NOT A FACE)":" (LOAD CASE "+face.Substring(0,face.IndexOf(',')).ToUpperInvariant()+", BUILDING FACE)"),Text=(string.IsNullOrEmpty(face)?"":"Building face: "+face+". ")+(face==""?"Not normal to a building face; assessed for reference (diagonal wind / Kzt envelope). ":"")+"Exposure "+d.Exposure+"; Kzt = "+(WindTin.Finite(d.Kzt)?d.Kzt.ToString("0.###",System.Globalization.CultureInfo.InvariantCulture):"unresolved")+". "+(d.ExposureAudit==null?"":d.ExposureAudit.Reason)});
   /* surface roughness diagram of the five upwind rays (owner, 24 November 2026: "ipakita din yung diagram for surface roughness") */
   int dirIndex=terrain!=null?d.Index:Array.IndexOf(WindAssessment.Directions,d.Name);if(terrain!=null&&dirIndex>=0&&d.ExposureAudit!=null){var rays=WindAssessment.UpwindSector(terrain,dirIndex).ToList();var audit=d.ExposureAudit;string name=d.Name;double bearing=terrain.BearingOf(dirIndex);
    blocks.Add(new Block{FigureHeight=178,Figure=(g,rc)=>RoughnessRays(g,rc,rays,audit,name,bearing,terrain,dirIndex)});}
   if(d.ExposureAudit!=null)blocks.Add(new Block{Headers=new[]{"Exposure check","Value","Basis / decision"},Widths=Fit(130,120,273),Rows=d.ExposureAudit.Checks.Select(r=>r.ToArray()).ToList()});
   if(d.TopographicChecks.Count>0)blocks.Add(new Block{Headers=new[]{"Kzt check (207A.8)","Value","Basis / decision"},Widths=Fit(130,150,243),Rows=d.TopographicChecks.Select(r=>r.ToArray()).ToList()});}
  if(bands!=null&&bands.Count>0){blocks.Add(new Block{Title="STAAD WIND INPUT / WALL INTENSITIES",Text="Wall pressures by height band (windward qz·G·Cp at the band top, leeward qh·G·Cp). The STAAD commands define one wind type per face for each direction: WW (windward, by height), LW (leeward) and SW (side walls), named by the direction the wind blows in the model (WW+X, LW+X, SW+X ...) when the model orientation is set; INT is the external q·G·Cp as a positive magnitude. Roof pressures are applied separately. Do not apply G and Cp again in STAAD."});
   blocks.Add(new Block{Headers=new[]{"Wind from / axis","Band top z m","Windward kPa","Leeward kPa","INT kPa"},Widths=Fit(160,85,95,90,93),Rows=bands.Select(b=>new[]{b.Axis,b.Top.ToString("0.###"),b.Windward.ToString("0.###"),b.Leeward.ToString("0.###"),b.Combined.ToString("0.###")}).ToList()});
   blocks.Add(new Block{Headers=new[]{"STAAD DEFINE WIND LOAD (UNIT METER KN)"},Widths=new float[]{CalculationDocument.CW},Rows=commands.Split('\n').Select(l=>new[]{l}).ToList()});}
  // Paginate blocks into sheets; table rows never split.
  var pages=new List<List<Action<Graphics,float>>>();var current=new List<Action<Graphics,float>>();float y=Top;var offsets=new List<List<float>>();var currentOffsets=new List<float>();
  Action newPage=()=>{pages.Add(current);offsets.Add(currentOffsets);current=new List<Action<Graphics,float>>();currentOffsets=new List<float>();y=Top;};
  foreach(var block in blocks){
   if(block.Figure!=null){if(y>Top&&y+block.FigureHeight>Bottom)newPage();var fig=block;current.Add((g,at)=>fig.Figure(g,new RectangleF(Left,at,Width,fig.FigureHeight)));currentOffsets.Add(y);y+=block.FigureHeight+8;continue;}
   if(block.Title!=null){float th=Measure(block.Text,Width,7f,false)+40;if(y>Top&&y+th+60>Bottom)newPage();var captured=block;current.Add((g,at)=>{CalculationDocument.Section(g,captured.Title,at);CalculationDocument.Text(g,captured.Text,Left,at+32,Width,th-34,7f);});currentOffsets.Add(y);y+=th+4;}
   if(block.Rows!=null){float header=RowHeight(block.Headers,block.Widths,true);int start=0;while(start<block.Rows.Count){if(y+header+RowHeight(block.Rows[start],block.Widths,false)>Bottom)newPage();int end=start;float h=header;while(end<block.Rows.Count){float rh=RowHeight(block.Rows[end],block.Widths,false);if(y+h+rh>Bottom)break;h+=rh;end++;}var rows=block.Rows.Skip(start).Take(end-start).ToList();var b=block;current.Add((g,at)=>Table(g,b.Headers,rows,b.Widths,at));currentOffsets.Add(y);y+=h+10;start=end;}}
  }
  if(current.Count>0){pages.Add(current);offsets.Add(currentOffsets);}
  for(int p=0;p<pages.Count;p++){var actions=pages[p];var at=offsets[p];doc.Sheets.Add(g=>{for(int i=0;i<actions.Count;i++)actions[i](g,at[i]);});}
 }
 static void Table(Graphics g,string[] headers,List<string[]> rows,float[] widths,float y){float hh=RowHeight(headers,widths,true);float x=Left;for(int c=0;c<headers.Length;c++){using(var brush=new SolidBrush(PowTheme.Ink))g.FillRectangle(brush,x,y,widths[c],hh);Cell(g,headers[c],x,y,widths[c],hh,true,Color.White);x+=widths[c];}y+=hh;
  for(int r=0;r<rows.Count;r++){float rh=RowHeight(rows[r],widths,false);x=Left;using(var brush=new SolidBrush(r%2==0?Color.White:ModernUi.Soft))g.FillRectangle(brush,Left,y,widths.Sum(),rh);for(int c=0;c<widths.Length;c++){Cell(g,c<rows[r].Length?rows[r][c]:"",x,y,widths[c],rh,false,PowTheme.Ink);x+=widths[c];}using(var pen=new Pen(PowTheme.Line,.5f))g.DrawLine(pen,Left,y+rh,Left+widths.Sum(),y+rh);y+=rh;}}
 static void Cell(Graphics g,string text,float x,float y,float w,float h,bool bold,Color color){using(var font=new Font("Segoe UI",7f,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Point))using(var brush=new SolidBrush(color))g.DrawString(text??"",font,brush,new RectangleF(x+4,y+3,w-8,h-3));}
}
}
