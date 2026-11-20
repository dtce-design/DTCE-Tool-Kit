using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Drawing2D;using System.Globalization;using System.Linq;
namespace BeamScheduleFormatter {
/* Beam elevation and sections for the printed bar bending schedule (owner, 19 November 2026: "Hindi mo sinama yung section ng
   beam at elevation sa report? Para ma visualize clearly? Gamit ng A3 para ok"). Drawn in points on A3: the elevation of a beam
   line with supports, every bar on its own lane with its mark, hooks, laps, allowed splice zones, hoop zones and spacing, cut-off
   and span dimensions; then the sections at the left face, midspan and right face of every span at a standard scale. */
static class BeamDetailPaper {
 static readonly CultureInfo Inv=CultureInfo.InvariantCulture;
 const float SectionBoxW=118,SectionBoxH=208,ElevationH=300;
 static bool Longitudinal(CutPiece p){return BeamCutting.Longitudinal(p);}
 static Color BarColor(CutPiece p){if(p.Shape=="REVIEW")return Color.Firebrick;if(p.Location.Contains("continuous"))return p.Top?Color.FromArgb(25,90,160):Color.FromArgb(20,120,90);return p.Top?Color.FromArgb(95,135,200):Color.FromArgb(70,160,115);}
 static string F0(double v){return v.ToString("0",Inv);}
 /* section cuts of a line: left face, midspan, right face of every span (metres along the line) */
 internal static List<KeyValuePair<string,double>> Cuts(CutLineGeometry geo){var list=new List<KeyValuePair<string,double>>();for(int j=0;j<geo.Ln.Length;j++){double d=geo.Depth[j],a=geo.FaceLeft[j],b=geo.FaceRight[j];double off=Math.Min(Math.Max(.15,d/2),(b-a)/4);
   list.Add(new KeyValuePair<string,double>((j+1)+"A",a+off));list.Add(new KeyValuePair<string,double>((j+1)+"B",(a+b)/2));list.Add(new KeyValuePair<string,double>((j+1)+"C",b-off));}return list;}
 /* long beam lines (owner, 19 November 2026: "Pano if 10-20 spans ang beam") are drawn in strips of whole spans of about
    21 m or less, so that the elevation stays near 1:55 on A3; strips meet at match lines */
 internal static List<int[]> Strips(CutLineGeometry geo,double maxLength=21.5){var list=new List<int[]>();int n=geo.Ln.Length,from=0;
  for(int j=0;j<n;j++){double len=geo.SupportStart[j+1]+geo.SupportWidth[j+1]-geo.SupportStart[from];if(j>from&&len>maxLength){list.Add(new[]{from,j-1});from=j;}}list.Add(new[]{from,n-1});return list;}
 /* identical sections (same beam, bars, hoops and position) are drawn once and marked typical */
 internal sealed class SectionGroup {public string Name;public double X;public List<string> Same=new List<string>();}
 /* "1A, 2A, 3A, 5A" -> "1A to 3A, 5A" */
 internal static string Compact(List<string> names){var parts=new List<string>();foreach(var byLetter in names.GroupBy(n=>n.Substring(n.Length-1))){var nums=byLetter.Select(n=>int.Parse(n.Substring(0,n.Length-1),Inv)).OrderBy(v=>v).ToList();int i=0;while(i<nums.Count){int k=i;while(k+1<nums.Count&&nums[k+1]==nums[k]+1)k++;parts.Add(k-i>=2?nums[i]+byLetter.Key+" to "+nums[k]+byLetter.Key:string.Join(", ",nums.Skip(i).Take(k-i+1).Select(v=>v+byLetter.Key)));i=k+1;}}return string.Join(", ",parts);}
 internal static List<SectionGroup> Groups(CutResult r,CutLineGeometry geo){var pieces=r.Pieces.Where(p=>p.Line==geo.Line).ToList();var list=new List<SectionGroup>();var index=new Dictionary<string,SectionGroup>();
  foreach(var c in Cuts(geo)){int j=0;for(int q=0;q<geo.Ln.Length;q++)if(c.Value>=geo.SupportStart[q])j=q;var here=pieces.Where(p=>(Longitudinal(p)||p.Location.StartsWith("Side-face"))&&p.Start<=c.Value+1e-9&&p.End>=c.Value-1e-9).ToList();
   var zone=j<geo.Ties.Count?geo.Ties[j].FirstOrDefault(z=>c.Value-geo.FaceLeft[j]>=z.From-1e-9&&c.Value-geo.FaceLeft[j]<=z.To+1e-9):null;
   string key=c.Key.Substring(c.Key.Length-1)+"|"+geo.Marks[j]+"|"+F0(geo.Width[j]*1000)+"x"+F0(geo.Depth[j]*1000)+"|"+F0(geo.Slab[j]*1000)+"|"+string.Join(",",here.GroupBy(p=>(p.Top?"T":p.Location.StartsWith("Side-face")?"S":"B")+p.Diameter).OrderBy(q=>q.Key).Select(q=>q.Key+"x"+q.Sum(p=>p.PerLine)))+"|"+(zone==null?"-":zone.Legs+"L"+zone.Dia+"@"+F0(zone.Spacing*1000));
   SectionGroup grp;if(index.TryGetValue(key,out grp)){grp.Same.Add(c.Key);continue;}grp=new SectionGroup{Name=c.Key,X=c.Value};grp.Same.Add(c.Key);index[key]=grp;list.Add(grp);}return list;}
 /* height of the figure for a line at the given width */
 internal static float Height(CutLineGeometry geo,float width){int perRow=Math.Max(1,(int)(width/SectionBoxW));int rows=(Cuts(geo).Count+perRow-1)/perRow;return ElevationH+rows*SectionBoxH+10;}
 internal static int SectionRows(CutResult r,CutLineGeometry geo,float width){int perRow=Math.Max(1,(int)(width/SectionBoxW));return (Groups(r,geo).Count+perRow-1)/perRow;}
 internal static float SectionsHeight(int rows){return rows*SectionBoxH+10;}
 static Font Small(){return new Font("Segoe UI",7f,FontStyle.Regular,GraphicsUnit.Point);}
 static Font Bold(){return new Font("Segoe UI",7.5f,FontStyle.Bold,GraphicsUnit.Point);}
 static void Center(Graphics g,string t,Font f,Brush b,float cx,float y){var sz=g.MeasureString(t,f);g.DrawString(t,f,b,cx-sz.Width/2,y);}
 static void DimH(Graphics g,Pen pen,Font f,Brush b,float a,float c,float y,string text){g.DrawLine(pen,a,y,c,y);g.DrawLine(pen,a,y-3,a,y+3);g.DrawLine(pen,c,y-3,c,y+3);var sz=g.MeasureString(text,f);if(c-a>sz.Width+2)g.DrawString(text,f,b,(a+c)/2-sz.Width/2,y-sz.Height);else g.DrawString(text,f,b,(a+c)/2-sz.Width/2,y+1);}
 /* ---------------- elevation ---------------- */
 public static void DrawElevation(Graphics g,RectangleF area,CutResult r,CutLineGeometry geo,int fromSpan=0,int toSpan=-1){if(toSpan<0)toSpan=geo.Ln.Length-1;double w0=geo.SupportStart[fromSpan],w1=geo.SupportStart[toSpan+1]+geo.SupportWidth[toSpan+1];bool whole=fromSpan==0&&toSpan==geo.Ln.Length-1;var pieces=r.Pieces.Where(p=>p.Line==geo.Line).ToList();g.SmoothingMode=SmoothingMode.AntiAlias;
  using(var small=Small())using(var bold=Bold())using(var ink=new SolidBrush(PowTheme.Ink))using(var muted=new SolidBrush(PowTheme.Muted))using(var dim=new Pen(PowTheme.Muted,.6f)){
   g.DrawString("ELEVATION - BEAM LINE "+geo.Line+(whole?"":"  -  SPANS "+(fromSpan+1)+" TO "+(toSpan+1)+" OF "+geo.Ln.Length)+"  ("+string.Join(", ",geo.Marks.Skip(fromSpan).Take(toSpan-fromSpan+1).Distinct())+")",bold,ink,area.Left+6,area.Top+4);
   g.DrawString("Horizontal scale 1:"+F0((w1-w0)*1000/((area.Width-60)/72*25.4))+" on A3; depth drawn larger for clarity. Bar marks per the schedule.",small,muted,area.Left+6,area.Top+16);
   /* lanes */
   var lane=new Dictionary<CutPiece,int>();int[] lanes=new int[2];foreach(bool top in new[]{true,false}){var ends=new List<double>();foreach(var p in pieces.Where(x=>Longitudinal(x)&&x.Top==top).OrderBy(x=>x.Location.Contains("continuous")?0:1).ThenBy(x=>x.Start)){int k=ends.FindIndex(e=>e<p.Start-1e-6);if(k<0){ends.Add(p.End);k=ends.Count-1;}else ends[k]=p.End;lane[p]=k;}lanes[top?0:1]=ends.Count;}
   float x0=area.Left+30,width=area.Width-60;Func<double,float> X=x=>(float)(x0+(x-w0)/(w1-w0)*width);var outerClip=g.Clip;g.SetClip(new RectangleF(X(w0)-18,area.Top,X(w1)-X(w0)+36,area.Height));
   /* bar labels (owner, 20 November 2026: "overcrowded yung labels ng beam elevation"): mark and bars only (cut lengths are in the
      schedule), each placed in the nearest label row where it does not touch another label */
   var shown=pieces.Where(x=>Longitudinal(x)&&x.End>w0+1e-6&&x.Start<w1-1e-6).ToList();var labelText=new Dictionary<CutPiece,string>();var labelRow=new Dictionary<CutPiece,int>();var labelX=new Dictionary<CutPiece,float>();int[] rows=new int[2];
   foreach(bool top in new[]{true,false}){var used=new List<List<float[]>>();/* lapped pieces of one bar share one label, as in the RCDC drawings: T1+T2-5NOS-T16 */
     foreach(var run in shown.Where(x=>x.Top==top).GroupBy(x=>x.Id.Contains(".")?x.Id.Substring(0,x.Id.LastIndexOf('.')):x.Id).Select(q=>q.OrderBy(x=>x.Start).ToList()).OrderBy(q=>(Math.Max(q[0].Start,w0)+Math.Min(q[0].End,w1))/2)){var p0=run[0];string t=string.Join("+",run.Select(x=>x.Short))+"-"+p0.PerLine+"NOS-T"+p0.Diameter;float tw=g.MeasureString(t,small).Width;float vm=(Math.Max(X(p0.Start),X(w0))+Math.Min(X(p0.End),X(w1)))/2;float lx=Math.Max(area.Left+2,Math.Min(area.Right-tw-2,vm-tw/2));
     int row=used.FindIndex(list=>list.All(q=>lx>q[1]+8||lx+tw<q[0]-8));if(row<0){used.Add(new List<float[]>());row=used.Count-1;}used[row].Add(new[]{lx,lx+tw});foreach(var p in run){labelText[p]=p==p0?t:null;labelRow[p]=row;labelX[p]=lx;}}rows[top?0:1]=used.Count;}
   float labelRowsTop=Math.Max(1,rows[0])*11,beamTop=area.Top+58+labelRowsTop,beamH=Math.Max(64,Math.Min(96,(lanes[0]+lanes[1])*7+40)),beamBottom=beamTop+beamH;
   Func<CutPiece,float> laneY=p=>{int k;lane.TryGetValue(p,out k);float off=7+k*6;return p.Top?beamTop+off:beamBottom-off;};
   /* supports */
   using(var hatch=new HatchBrush(HatchStyle.BackwardDiagonal,Color.FromArgb(150,150,150),Color.FromArgb(238,238,238)))using(var edge=new Pen(Color.Gray,.8f))for(int i=fromSpan;i<=toSpan+1;i++){float a=X(geo.SupportStart[i]),b=X(geo.SupportStart[i]+geo.SupportWidth[i]);g.FillRectangle(hatch,a,beamTop-22,b-a,beamH+44);g.DrawRectangle(edge,a,beamTop-22,b-a,beamH+44);Center(g,"S"+(i+1),bold,ink,(a+b)/2,beamTop-labelRowsTop-26);}
   /* allowed splice zones */
   using(var zone=new SolidBrush(Color.FromArgb(45,60,170,90)))foreach(var z in geo.TopZones.Concat(geo.BottomZones).GroupBy(z=>z[0].ToString("0.000",Inv)+"|"+z[1].ToString("0.000",Inv)).Select(x=>x.First()))g.FillRectangle(zone,X(z[0]),beamTop,X(z[1])-X(z[0]),beamH);
   using(var outline=new Pen(PowTheme.Ink,1.1f))g.DrawRectangle(outline,X(w0),beamTop,X(w1)-X(w0),beamH);
   for(int j=0;j<geo.Slab.Length;j++)if(geo.Slab[j]>0){float ts=(float)(geo.Slab[j]/geo.Depth[j]*beamH);using(var slab=new Pen(PowTheme.Muted,.6f){DashStyle=DashStyle.Dash})g.DrawLine(slab,X(geo.FaceLeft[j]),beamTop+ts,X(geo.FaceRight[j]),beamTop+ts);}
   /* hoops: ticks at their spacing; zone labels below the beam */
   float below=beamBottom+4+Math.Max(1,rows[1])*11;var hoopLabels=new List<object[]>();
   using(var tie=new Pen(Color.FromArgb(150,150,150),.5f))for(int j=fromSpan;j<geo.Ties.Count&&j<=toSpan;j++)foreach(var z in geo.Ties[j]){if(z.Count<=0||z.Spacing<=0)continue;for(int k=0;k<z.Count;k++){double x=z.Name=="left"?geo.FaceLeft[j]+Math.Max(z.First>0?z.First:.05,z.From)+k*z.Spacing:z.Name=="right"?geo.FaceLeft[j]+z.To-(z.First>0?z.First:.05)-k*z.Spacing:geo.FaceLeft[j]+z.From+(k+1)*z.Spacing;g.DrawLine(tie,X(x),beamTop+2,X(x),beamBottom-2);}
     float a=X(geo.FaceLeft[j]+z.From),b=X(geo.FaceLeft[j]+z.To);var hp=pieces.FirstOrDefault(x=>x.ShapeCode=="HOOP"&&x.Location.StartsWith("Stirrups, span "+(j+1)+" "+z.Name+" "));string t=hp!=null?hp.Short+"-"+hp.PerLine+"NOS-T"+z.Dia+"-@"+F0(z.Spacing*1000)+" ("+z.Legs+"L)":z.Legs+"L-Ø"+z.Dia+" @ "+F0(z.Spacing*1000);hoopLabels.Add(new object[]{a,b,t});}
   /* hoop zones: dimension lines in one row, their labels (RCDC style S13-15NOS-T10-@100) in as many rows as they need above it */
   float hoopLine;{var used=new List<List<float[]>>();var place=new List<object[]>();foreach(var hl in hoopLabels){float a=(float)hl[0],b=(float)hl[1];string t=(string)hl[2];float tw=g.MeasureString(t,small).Width;float lx=(a+b)/2-tw/2;int row=used.FindIndex(list=>list.All(q=>lx>q[1]+6||lx+tw<q[0]-6));if(row<0){used.Add(new List<float[]>());row=used.Count-1;}used[row].Add(new[]{lx,lx+tw});place.Add(new object[]{a,b,t,lx,row});}
    hoopLine=below+Math.Max(1,used.Count)*11+4;foreach(var pl in place){float a=(float)pl[0],b=(float)pl[1];DimH(g,dim,small,muted,a,b,hoopLine,"");int row=(int)pl[4];float ty=hoopLine-12-(used.Count-1-row)*11;g.DrawString((string)pl[2],small,muted,(float)pl[3],ty);}}
   /* extra hoops over laps (418.6.3.3) */
   using(var extra=new Pen(Color.FromArgb(200,110,20),.6f))foreach(var p in pieces.Where(x=>x.Location.StartsWith("Extra hoops over lap"))){for(int k=0;k<p.PerLine;k++){double x=p.Start+(p.End-p.Start)*(k+.5)/p.PerLine;g.DrawLine(extra,X(x),beamTop+2,X(x),beamBottom-2);}}
   /* layer spacer bars (end view across the beam) between the first and second lane of the face */
   using(var sb=new SolidBrush(Color.FromArgb(150,110,60)))foreach(var p in pieces.Where(x=>x.Location.StartsWith("Layer spacer")&&x.End>w0&&x.Start<w1)){int n2=Math.Max(2,p.PerLine);float y=p.Top?beamTop+10:beamBottom-10;for(int k=0;k<n2;k++){double x=p.Start+(p.End-p.Start)*k/(n2-1);g.FillEllipse(sb,X(x)-1.8f,y-1.8f,3.6f,3.6f);}}
   /* bars with leaders to their labels */
   foreach(var p in pieces.Where(x=>Longitudinal(x)&&x.End>w0+1e-6&&x.Start<w1-1e-6)){float y=laneY(p),a=X(p.Start),b=X(p.End);using(var pen=new Pen(BarColor(p),1.4f)){g.DrawLine(pen,a,y,b,y);if(p.ShapeCode=="HOOK1"||p.ShapeCode=="HOOK2"||p.ShapeCode=="CRANKHOOK"){float hook=p.Top?12:-12;if(p.Start<=geo.Cover+1e-6)g.DrawLine(pen,a,y,a,y+hook);if(p.End>=geo.Total-geo.Cover-1e-6)g.DrawLine(pen,b,y,b,y+hook);}}
    int k=labelRow[p];string t=labelText[p];if(t==null)continue;var sz=g.MeasureString(t,small);float va=Math.Max(a,X(w0)),vb=Math.Min(b,X(w1)),vm=(va+vb)/2;float lx=labelX[p],ly=p.Top?beamTop-13-k*11:beamBottom+2+k*11;vm=Math.Max(lx+2,Math.Min(lx+sz.Width-2,vm));
    using(var lead=new Pen(Color.FromArgb(140,BarColor(p)),.5f))g.DrawLine(lead,vm,y,vm,p.Top?ly+sz.Height-2:ly+2);using(var bg=new SolidBrush(Color.FromArgb(225,255,255,255)))g.FillRectangle(bg,lx,ly,sz.Width,sz.Height-2);using(var tb=new SolidBrush(BarColor(p)))g.DrawString(t,small,tb,lx,ly-1);}
   /* spans left out of the estimate: greyed, only the dowels of the spans beside them */
   if(geo.Included!=null)for(int j=fromSpan;j<=toSpan&&j<geo.Included.Length;j++)if(!geo.Included[j]){float a=X(geo.FaceLeft[j]),b=X(geo.FaceRight[j]);var box=new RectangleF(a,beamTop-4,b-a,beamH+8);using(var veil=new SolidBrush(Color.FromArgb(150,236,238,240)))g.FillRectangle(veil,box);using(var hatch=new HatchBrush(HatchStyle.WideUpwardDiagonal,Color.FromArgb(70,120,120,120),Color.Transparent))g.FillRectangle(hatch,box);using(var dash=new Pen(Color.FromArgb(150,150,150),.6f){DashStyle=DashStyle.Dash})g.DrawRectangle(dash,box.X,box.Y,box.Width,box.Height);
    string t="SPAN "+(j+1)+" NOT INCLUDED - dowels (abang) only";var sz=g.MeasureString(t,bold);if(sz.Width<box.Width-6)using(var bg=new SolidBrush(Color.FromArgb(230,255,255,255))){float tx=a+(b-a-sz.Width)/2,ty=beamTop+beamH/2-sz.Height/2;g.FillRectangle(bg,tx-2,ty,sz.Width+4,sz.Height);g.DrawString(t,bold,Brushes.DimGray,tx,ty);}}
   /* laps */
   using(var lapPen=new Pen(Color.FromArgb(230,140,20),.9f){DashStyle=DashStyle.Dash})foreach(var c in r.Splices.Where(c=>c.Line==geo.Line&&c.To>w0&&c.From<w1)){var piece=pieces.FirstOrDefault(p=>p.Id.StartsWith(c.Bar+".")&&Math.Abs(p.End-c.To)<1e-6);bool top=piece!=null&&piece.Top;int n=Math.Max(2,top?lanes[0]:lanes[1]);float y1=top?beamTop+2:beamBottom-2,y2=top?beamTop+7+n*6:beamBottom-7-n*6;float a=X(c.From),b=X(c.To);
    using(var fill=new SolidBrush(c.Ok?Color.FromArgb(60,240,170,40):Color.FromArgb(80,220,40,40)))g.FillRectangle(fill,a,Math.Min(y1,y2),b-a,Math.Abs(y2-y1));g.DrawRectangle(lapPen,a,Math.Min(y1,y2),b-a,Math.Abs(y2-y1));{string lt="LAP "+F0(c.Lap*1000)+(c.Ok?"":" NG");var lsz=g.MeasureString(lt,small);using(var bg=new SolidBrush(Color.FromArgb(235,255,255,255)))g.FillRectangle(bg,(a+b)/2-lsz.Width/2,(y1+y2)/2-5,lsz.Width,lsz.Height-2);Center(g,lt,small,c.Ok?Brushes.DarkOrange:Brushes.Firebrick,(a+b)/2,(y1+y2)/2-5);}}
   /* section cuts */
   using(var cut=new Pen(Color.FromArgb(200,40,40),.9f){DashStyle=DashStyle.DashDot})foreach(var c in Cuts(geo).Where(q=>q.Value>w0&&q.Value<w1)){float px=X(c.Value);g.DrawLine(cut,px,beamTop-8,px,beamBottom+8);using(var b=new SolidBrush(Color.FromArgb(200,40,40)))g.DrawString(c.Key,small,b,px+1,beamBottom-10);}
   /* dimensions: supports and clear spans, 2h hoop zones, overall */
   float dy=hoopLine+16;for(int i=fromSpan;i<=toSpan+1;i++)DimH(g,dim,small,muted,X(geo.SupportStart[i]),X(geo.SupportStart[i]+geo.SupportWidth[i]),dy,F0(geo.SupportWidth[i]*1000));
   for(int j=fromSpan;j<=toSpan;j++){DimH(g,dim,small,muted,X(geo.FaceLeft[j]),X(geo.FaceRight[j]),dy,"Ln "+F0(geo.Ln[j]*1000)+"  ("+geo.Marks[j]+" "+F0(geo.Width[j]*1000)+"x"+F0(geo.Depth[j]*1000)+")");
    double h2=2*geo.Depth[j];if(geo.Ln[j]>2*h2){DimH(g,dim,small,muted,X(geo.FaceLeft[j]),X(geo.FaceLeft[j]+h2),dy+14,"2h "+F0(h2*1000));DimH(g,dim,small,muted,X(geo.FaceRight[j]-h2),X(geo.FaceRight[j]),dy+14,"2h "+F0(h2*1000));}}
   DimH(g,dim,small,muted,X(w0),X(w1),dy+30,whole?"overall "+F0(geo.Total*1000):"S"+(fromSpan+1)+" to S"+(toSpan+2)+": "+F0((w1-w0)*1000)+"  (line overall "+F0(geo.Total*1000)+")");
   g.Clip=outerClip;
   /* match lines where a strip meets the next one */
   using(var match=new Pen(Color.FromArgb(120,60,160),1.2f){DashStyle=DashStyle.DashDotDot}){if(fromSpan>0){float mx=X(w0+geo.SupportWidth[fromSpan]/2);g.DrawLine(match,mx,beamTop-40,mx,dy+50);g.DrawString("MATCH LINE S"+(fromSpan+1)+" (previous strip)",small,new SolidBrush(Color.FromArgb(120,60,160)),mx+3,dy+40);}
    if(toSpan<geo.Ln.Length-1){float mx=X(w1-geo.SupportWidth[toSpan+1]/2);g.DrawLine(match,mx,beamTop-40,mx,dy+50);var t="MATCH LINE S"+(toSpan+2)+" (next strip)";var sz=g.MeasureString(t,small);g.DrawString(t,small,new SolidBrush(Color.FromArgb(120,60,160)),mx-sz.Width-3,dy+40);}}
   /* legend */
   float lx2=area.Left+8,ly2=area.Bottom-14;foreach(var item in new[]{new{T="Top continuous",C=Color.FromArgb(25,90,160)},new{T="Top extra",C=Color.FromArgb(95,135,200)},new{T="Bottom continuous",C=Color.FromArgb(20,120,90)},new{T="Bottom extra",C=Color.FromArgb(70,160,115)},new{T="Lap (Class B)",C=Color.FromArgb(230,140,20)},new{T="Extra hoops over lap",C=Color.FromArgb(200,110,20)},new{T="Allowed splice zone",C=Color.FromArgb(150,200,160)},new{T="Layer spacer",C=Color.FromArgb(150,110,60)},new{T="Section cut",C=Color.FromArgb(200,40,40)}}){using(var b=new SolidBrush(item.C))g.FillRectangle(b,lx2,ly2+3,12,6);g.DrawString(item.T,small,muted,lx2+15,ly2);lx2+=g.MeasureString(item.T,small).Width+28;}}}
 /* ---------------- sections ---------------- */
 static readonly int[] Scales={5,10,15,20,25,30,40,50};
 public static void DrawSections(Graphics g,RectangleF area,CutResult r,CutLineGeometry geo,int firstRow,int rowCount){var cuts=Groups(r,geo);int perRow=Math.Max(1,(int)(area.Width/SectionBoxW));float boxW=area.Width/perRow;
  double maxB=geo.Width.Max()*1000,maxH=geo.Depth.Max()*1000;double ptPerMm=72/25.4;int scale=Scales.FirstOrDefault(n=>maxB*ptPerMm/n<=boxW-34&&maxH*ptPerMm/n<=SectionBoxH-92);if(scale==0)scale=Scales.Last();float k=(float)(ptPerMm/scale);
  var pieces=r.Pieces.Where(p=>p.Line==geo.Line).ToList();
  for(int i=firstRow*perRow;i<Math.Min(cuts.Count,(firstRow+rowCount)*perRow);i++){int row=i/perRow-firstRow,col=i%perRow;var box=new RectangleF(area.Left+col*boxW,area.Top+row*SectionBoxH,boxW,SectionBoxH);DrawSection(g,box,geo,pieces,cuts[i],cuts[i].X,k,scale);}}
 static void DrawSection(Graphics g,RectangleF box,CutLineGeometry geo,List<CutPiece> pieces,SectionGroup grp,double x,float k,int scale){string name=grp.Name;bool typ=grp.Same.Count>1;g.SmoothingMode=SmoothingMode.AntiAlias;
  int j=0;for(int q=0;q<geo.Ln.Length;q++)if(x>=geo.SupportStart[q])j=q;double b=geo.Width[j]*1000,h=geo.Depth[j]*1000,cover=geo.Cover*1000;
  var zone=j<geo.Ties.Count?geo.Ties[j].FirstOrDefault(z=>x-geo.FaceLeft[j]>=z.From-1e-9&&x-geo.FaceLeft[j]<=z.To+1e-9):null;int tieDia=zone!=null?zone.Dia:10;int legs=zone!=null?zone.Legs:2;
  using(var small=Small())using(var bold=Bold())using(var ink=new SolidBrush(PowTheme.Ink))using(var muted=new SolidBrush(PowTheme.Muted))using(var dim=new Pen(PowTheme.Muted,.6f)){
   string where=name.EndsWith("B")?"midspan":name.EndsWith("A")?"near left face":"near right face";Center(g,"SECTION "+name+(typ?" (TYP.)":""),bold,ink,box.Left+box.Width/2,box.Top+2);Center(g,(typ?"":"span "+(j+1)+" ")+where+" - 1:"+scale,small,muted,box.Left+box.Width/2,box.Top+13);
   var here=pieces.Where(p=>Longitudinal(p)&&p.Start<=x+1e-9&&p.End>=x-1e-9).ToList();var topBars=here.Where(p=>p.Top).ToList();var botBars=here.Where(p=>!p.Top).ToList();
   Func<List<CutPiece>,string> label=list=>string.Join(" + ",list.GroupBy(p=>p.Diameter).OrderByDescending(q=>q.Key).Select(q=>q.Sum(p=>p.PerLine)+"-Ø"+q.Key));
   float w=(float)(b*k),hh=(float)(h*k),x0=box.Left+(box.Width-w)/2+4,y0=box.Top+40;
   var topLayers=BeamCutting.SectionLayers(geo,x,true);var botLayers=BeamCutting.SectionLayers(geo,x,false);
   Center(g,topLayers.Count>0?BeamCutting.LayersText(topLayers):label(topBars),small,new SolidBrush(Color.FromArgb(25,90,160)),x0+w/2,y0-12);
   using(var concrete=new SolidBrush(Color.FromArgb(236,236,232)))g.FillRectangle(concrete,x0,y0,w,hh);using(var edge=new Pen(PowTheme.Ink,.9f))g.DrawRectangle(edge,x0,y0,w,hh);
   if(geo.Slab[j]>0){float ts=(float)(geo.Slab[j]*1000*k);using(var slab=new Pen(PowTheme.Muted,.5f){DashStyle=DashStyle.Dash}){g.DrawLine(slab,x0-12,y0+ts,x0,y0+ts);g.DrawLine(slab,x0+w,y0+ts,x0+w+12,y0+ts);}}
   float c=(float)((cover+tieDia/2.0)*k);float rad=(float)Math.Max(1.5,2*tieDia*k);using(var tie=new Pen(Color.FromArgb(90,90,90),(float)Math.Max(.6,tieDia*k))){var path=new GraphicsPath();float L=x0+c,T=y0+c,R=x0+w-c,B=y0+hh-c;path.AddArc(L,T,2*rad,2*rad,180,90);path.AddArc(R-2*rad,T,2*rad,2*rad,270,90);path.AddArc(R-2*rad,B-2*rad,2*rad,2*rad,0,90);path.AddArc(L,B-2*rad,2*rad,2*rad,90,90);path.CloseFigure();g.DrawPath(tie,path);path.Dispose();
    float hk=(float)Math.Max(4,75*k);g.DrawLine(tie,L+rad,T+rad*.3f,L+rad+hk*.7f,T+rad*.3f+hk*.7f);g.DrawLine(tie,L+rad*.3f,T+rad,L+rad*.3f+hk*.7f,T+rad+hk*.7f);
    for(int q=1;q<legs-1;q++){float lx=L+(R-L)*q/(legs-1);g.DrawLine(tie,lx,T,lx,B);}}
   foreach(bool top in new[]{true,false}){var layers=top?topLayers:botLayers;if(layers.Count>0){/* as scheduled, layer by layer */int cont=(top?topBars:botBars).Where(p=>p.Location.Contains("continuous")).Sum(p=>p.PerLine);int drawn=0;double offMm=cover+tieDia;
     for(int r=0;r<layers.Count;r++){var layer=layers[r];int ldmax=layer.Max();double linner=b-2*cover-2*tieDia;for(int q=0;q<layer.Count;q++){double cxMm=layer.Count==1?b/2:cover+tieDia+ldmax/2.0+(linner-ldmax)*q/(layer.Count-1);double cyMm=offMm+ldmax/2.0;float cx=x0+(float)(cxMm*k),cy=top?y0+(float)(cyMm*k):y0+hh-(float)(cyMm*k);float d=(float)Math.Max(2.4,layer[q]*k);bool isCont=drawn<cont;drawn++;
       using(var bar=new SolidBrush(top?(isCont?Color.FromArgb(25,90,160):Color.FromArgb(110,150,210)):(isCont?Color.FromArgb(20,120,90):Color.FromArgb(90,170,130))))g.FillEllipse(bar,cx-d/2,cy-d/2,d,d);}
      offMm+=ldmax;if(r<layers.Count-1){var spacerPiece=pieces.FirstOrDefault(p=>p.Location.StartsWith("Layer spacer")&&p.Top==top&&p.Start<=x+1e-9&&p.End>=x-1e-9);int lgap=spacerPiece!=null?spacerPiece.Diameter:25;double cyGap=offMm+lgap/2.0;float gy=top?y0+(float)(cyGap*k):y0+hh-(float)(cyGap*k);float sw=(float)Math.Max(1.2,lgap*k);using(var sp=new Pen(Color.FromArgb(150,110,60),sw))g.DrawLine(sp,x0+(float)((cover+tieDia)*k),gy,x0+w-(float)((cover+tieDia)*k),gy);using(var sb=new SolidBrush(Color.FromArgb(150,110,60)))g.DrawString("spacer Ø"+lgap,small,sb,x0+w+2,gy-6);offMm+=lgap;}}
     continue;}
    var bars=top?topBars:botBars;var spots=new List<CutPiece>();foreach(var p in bars.OrderByDescending(p=>p.Location.Contains("continuous")))for(int q=0;q<p.PerLine;q++)spots.Add(p);if(spots.Count==0)continue;
    int dmax=spots.Max(p=>p.Diameter);double inner=b-2*cover-2*tieDia;var spacer=pieces.FirstOrDefault(p=>p.Location.StartsWith("Layer spacer")&&p.Top==top&&p.Start<=x+1e-9&&p.End>=x-1e-9);int gapMm=spacer!=null?spacer.Diameter:25;int perRow=Math.Min(spots.Count,BeamCutting.PerLayer(inner,dmax,20));
    for(int q=0;q<spots.Count;q++){int row=q/perRow,col=q%perRow;int inRow=Math.Min(perRow,spots.Count-row*perRow);var p=spots[q];double cxMm=inRow==1?b/2:cover+tieDia+dmax/2.0+(inner-dmax)*col/(inRow-1);double off=cover+tieDia+dmax/2.0+row*(dmax+gapMm);float cx=x0+(float)(cxMm*k),cy=top?y0+(float)(off*k):y0+hh-(float)(off*k);float d=(float)Math.Max(2.4,p.Diameter*k);using(var bar=new SolidBrush(BarColor(p)))g.FillEllipse(bar,cx-d/2,cy-d/2,d,d);}
    /* spacer bar across the beam between the layers */
    int rows=(spots.Count+perRow-1)/perRow;for(int gap=1;gap<rows;gap++){double off=cover+tieDia+dmax+(gap-1)*(dmax+gapMm)+gapMm/2.0;float cy=top?y0+(float)(off*k):y0+hh-(float)(off*k);float sw=(float)Math.Max(1.2,gapMm*k);using(var sp=new Pen(Color.FromArgb(150,110,60),sw))g.DrawLine(sp,x0+(float)((cover+tieDia)*k),cy,x0+w-(float)((cover+tieDia)*k),cy);
     using(var sb=new SolidBrush(Color.FromArgb(150,110,60)))g.DrawString("spacer Ø"+gapMm,small,sb,x0+w+2,cy-6);}}
   foreach(var side in pieces.Where(p=>p.Location.StartsWith("Side-face")&&p.Start<=x+1e-9&&p.End>=x-1e-9)){int per=Math.Max(1,side.PerLine/2);for(int q=0;q<per;q++){float cy=y0+hh*(q+1)/(per+1);float d=(float)Math.Max(2,side.Diameter*k);using(var bar=new SolidBrush(Color.FromArgb(120,120,120))){g.FillEllipse(bar,x0+c-d/2,cy-d/2,d,d);g.FillEllipse(bar,x0+w-c-d/2,cy-d/2,d,d);}}}
   Center(g,botLayers.Count>0?BeamCutting.LayersText(botLayers):label(botBars),small,new SolidBrush(Color.FromArgb(20,120,90)),x0+w/2,y0+hh+2);
   /* dimensions */
   DimH(g,dim,small,muted,x0,x0+w,y0+hh+22,F0(b));{float vx=x0-8;g.DrawLine(dim,vx,y0,vx,y0+hh);g.DrawLine(dim,vx-3,y0,vx+3,y0);g.DrawLine(dim,vx-3,y0+hh,vx+3,y0+hh);var st=g.Save();g.TranslateTransform(vx-2,y0+hh/2);g.RotateTransform(-90);var sz=g.MeasureString(F0(h),small);g.DrawString(F0(h),small,muted,-sz.Width/2,-sz.Height);g.Restore(st);}
   string tieText=zone!=null?legs+"L-Ø"+tieDia+" @ "+F0(zone.Spacing*1000)+(legs>2?" (crossties)":""):"in support";Center(g,tieText,small,ink,x0+w/2-4,y0+hh+30);
   Center(g,"cover "+F0(cover)+", hoops 135°",small,muted,x0+w/2-4,y0+hh+40);
   var marks=typ?"Typ. at "+Compact(grp.Same)+" (bar marks per the elevation)":string.Join(", ",here.Select(p=>p.Short));var rect=new RectangleF(box.Left+4,y0+hh+51,box.Width-8,box.Bottom-(y0+hh+51));g.DrawString(marks,small,muted,rect);
   using(var frame=new Pen(PowTheme.Line,.5f))g.DrawRectangle(frame,box.Left+1,box.Top,box.Width-2,box.Height-4);}}
}
}
