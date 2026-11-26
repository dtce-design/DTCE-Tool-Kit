using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Drawing2D;using System.Globalization;using System.Linq;
namespace BeamScheduleFormatter {
/* the figure of a wind load case (owner, 26 November 2026: "isama mo din yung figure sa wind load loading. pero nagooverlap yung texts"):
   the floor plan from the STAAD model as STAAD's top view (X to the right, Z down), every outline face with its arrows - windward (WW)
   pushed in, leeward (LW) and side walls (SW) pulled out - the recessed walls drawn thick with their own WIND LOAD line. Owner ("colored lang yung text at arrows basta di nagooverlap yung texts. lagyan na lang ng legend"): no boxes or leader lines -
   the outer WIND LOAD lines are in the legend at the top, each recessed wall has a short coloured tag (R1, R2 ...) placed clear of the
   others and its line in the legend under the plan. One figure per wind case and per floor outline (levels with the same plan share one). */
sealed class WindCaseFigure {
 static readonly CultureInfo Ci=CultureInfo.InvariantCulture;
 internal static readonly Color WwColor=Color.FromArgb(31,95,191),LwColor=Color.FromArgb(200,58,38),SwColor=Color.FromArgb(214,128,0);
 public string Title="";public List<WindPlanLevel> Levels;public WindFaceTypes Types;public List<WindRecessLine> Lines=new List<WindRecessLine>();public double BaseY;
 internal WindPlanLevel Plan{get{return Levels[0];}}
 static string F(double v){return Math.Round(v,3).ToString("0.###",Ci);}
 /* the figures of every wind case of the link (travel -> packed types) on the model's plans; titles by travel ("LOAD 9 WL+X") */
 internal static List<WindCaseFigure> Build(StaadModel model,Dictionary<string,string> link,Dictionary<string,string> titles,double baseY=0){var list=new List<WindCaseFigure>();if(model==null||link==null)return list;
  foreach(var travel in new[]{"+X","-X","+Z","-Z"}){string packed;if(!link.TryGetValue(travel,out packed))continue;var t=WindFaceTypes.Unpack(travel,packed);if(t==null)continue;
   string note;var plans=WindEdgeLoads.Plans(model,baseY,t.Top,out note);if(plans.Count==0)continue;
   /* the ground beams at the base close no storey: their plan is not drawn when there are levels above */var shown=plans.Count>1&&plans[0].Y<=baseY+1e-3?plans.Skip(1).ToList():plans;var faces=plans.SelectMany(p=>p.Faces).ToList();var lines=WindEdgeLoads.RecessLines(faces,t);
   string title;if(titles==null||!titles.TryGetValue(travel,out title))title="WL"+travel;
   foreach(var g in shown.GroupBy(p=>p.Signature)){var levels=g.OrderBy(p=>p.Y).ToList();var ys=new HashSet<double>(levels.Select(p=>p.Y));
    var keys=new HashSet<string>(levels[0].Faces.Select(f=>f.Key));
    list.Add(new WindCaseFigure{Title=title,Levels=levels,Types=t,BaseY=baseY,Lines=lines.Where(l=>keys.Contains(l.Face.Key)&&l.Levels.Any(ys.Contains)).ToList()});}}
  return list;}
 /* ---- layout ---- */
 /* a legend row: colour, label, text, note; Wrap puts the note under the text when the row is too wide */
 sealed class Row {public Color Color;public string Label,Text,Note;public bool Wrap;public float H;}
 sealed class Tag {public WindRecessLine Line;public string Name;public Color Color;}
 sealed class Layout {public float W,Pad,TitleH,TopH,BottomH,Margin,PlanH,PlanW,Scale,Total,ArrowLen,RowH,LabelW;public double X0,Z0;public List<Row> Top=new List<Row>(),Bottom=new List<Row>();public List<Tag> Tags=new List<Tag>();}
 static Font Font(float size,bool bold){return new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel);}
 string Side(string normal){return normal=="-X"?"X-min":normal=="+X"?"X-max":normal=="-Z"?"Z-min":"Z-max";}
 string Pressure(double a,double b){return Math.Abs(a-b)<1e-9?F(a)+" kPa":F(Math.Min(a,b))+"–"+F(Math.Max(a,b))+" kPa";}
 internal static string RoleOf(string normal,string travel){char ax=travel[1];bool along=normal[1]==ax;if(!along)return "SW";return normal[0]==travel[0]?"LW":"WW";}
 internal static Color ColorOf(string role){return role.StartsWith("WW")?WwColor:role.StartsWith("LW")?LwColor:SwColor;}
 Layout Make(Graphics g,float width,float fs){var L=new Layout{W=width,Pad=fs*0.8f,ArrowLen=fs*2.4f,RowH=fs*1.45f};var t=Types;char ax=t.Travel[1];string back=(t.Travel[0]=='+'?"-":"+")+ax;
  string other=ax=='X'?"Z":"X";string top=F(BaseY)+" "+F(t.Top);
  L.Top.Add(new Row{Color=WwColor,Label="WW"+t.Travel,Text="WIND LOAD "+(t.Travel[0]=='+'?"":"-")+ax+" 1 TYPE "+t.Ww+" YR "+top,Note=Side(back)+" faces, pushed in, "+Pressure(t.WwInt.Count>0?t.WwInt.Min():0,t.WwInt.Count>0?t.WwInt.Max():0)});
  L.Top.Add(new Row{Color=LwColor,Label="LW"+t.Travel,Text="WIND LOAD "+(t.Travel[0]=='+'?"-":"")+ax+" -1 TYPE "+t.Lw+" YR "+top,Note=Side(t.Travel)+" faces, suction, "+Pressure(t.Leeward,t.Leeward)});
  L.Top.Add(new Row{Color=SwColor,Label="SW"+t.Travel,Text="WIND LOAD "+other+" -1 and -"+other+" -1 TYPE "+t.Sw+" YR "+top,Note=other+"-min and "+other+"-max faces, suction, "+Pressure(t.Side,t.Side)});
  /* recessed walls: a tag on the plan (R1, R2 ...) and a legend row under it */
  int k=0;foreach(var l in Lines){var tag="R"+(++k);L.Tags.Add(new Tag{Line=l,Name=tag,Color=ColorOf(l.Role)});L.Bottom.Add(new Row{Color=ColorOf(l.Role),Label=tag+"  "+l.Role,Text=l.Text,Note=l.Comment});}
  using(var tf=Font(fs*1.25f,true))L.TitleH=g.MeasureString("X",tf).Height+L.Pad*0.6f;
  using(var hf=Font(fs,true))using(var bf=Font(fs,false))using(var mf=Font(fs*0.92f,false)){
   L.LabelW=L.Top.Concat(L.Bottom).Max(r=>g.MeasureString(r.Label,hf).Width)+fs*1.8f;
   Action<List<Row>,float> fit=(rows,avail)=>{float tw=rows.Count==0?0:rows.Max(r=>g.MeasureString(r.Text,bf).Width)+fs;foreach(var r in rows){r.Wrap=L.LabelW+tw+g.MeasureString(r.Note,mf).Width>avail;r.H=L.RowH*(r.Wrap?2:1);}};
   fit(L.Top,width-fs*7f);fit(L.Bottom,width);}
  L.TopH=Math.Max(L.RowH+L.Top.Sum(r=>r.H),fs*6f)+L.Pad*0.5f;L.BottomH=L.Bottom.Count==0?0:L.RowH*1.2f+L.Bottom.Sum(r=>r.H);
  var p=Plan;double xmin=p.Xs.First(),xmax=p.Xs.Last(),zmin=p.Zs.First(),zmax=p.Zs.Last();L.X0=xmin;L.Z0=zmin;L.Margin=L.ArrowLen+fs*1.2f;
  double sx=Math.Max(xmax-xmin,1e-3),sz=Math.Max(zmax-zmin,1e-3);float avail2=width-2*L.Margin;L.Scale=(float)Math.Min(avail2/sx,width*0.5/sz);L.PlanW=(float)(sx*L.Scale);L.PlanH=(float)(sz*L.Scale);
  L.Total=L.TitleH+L.TopH+L.Margin*2+L.PlanH+L.BottomH+L.Pad;return L;}
 internal float Height(Graphics g,float width,float fs){return Make(g,width,fs).Total;}
 /* the height for a report figure (72 dpi raster, report points) */
 internal float ReportHeight(float width,float fs){using(var bmp=new Bitmap(1,1)){bmp.SetResolution(72,72);using(var g=Graphics.FromImage(bmp))return Height(g,width,fs);}}
 static void Arrow(Graphics g,Color c,float w,PointF a,PointF b){using(var pen=new Pen(c,w))g.DrawLine(pen,a,b);double ang=Math.Atan2(b.Y-a.Y,b.X-a.X);float h=w*3.2f+2.5f;
  var p1=new PointF(b.X-(float)(Math.Cos(ang-.42)*h),b.Y-(float)(Math.Sin(ang-.42)*h));var p2=new PointF(b.X-(float)(Math.Cos(ang+.42)*h),b.Y-(float)(Math.Sin(ang+.42)*h));using(var br=new SolidBrush(c))g.FillPolygon(br,new[]{b,p1,p2});}
 void Rows(Graphics g,Layout L,List<Row> rows,float x0,float y,float fs){using(var hf=Font(fs,true))using(var bf=Font(fs,false))using(var mf=Font(fs*0.92f,false))using(var muted=new SolidBrush(PowTheme.Muted)){
   float tw=rows.Count==0?0:rows.Max(r=>g.MeasureString(r.Text,bf).Width)+fs;
   foreach(var r in rows){using(var b=new SolidBrush(r.Color)){g.FillRectangle(b,x0,y+L.RowH*0.25f,fs*0.9f,L.RowH*0.5f);g.DrawString(r.Label,hf,b,x0+fs*1.2f,y);g.DrawString(r.Text,bf,b,x0+L.LabelW,y);}
    if(r.Wrap)g.DrawString(r.Note,mf,muted,x0+L.LabelW,y+L.RowH);else g.DrawString(r.Note,mf,muted,x0+L.LabelW+tw,y+fs*0.05f);y+=r.H;}}}
 internal void Draw(Graphics g,RectangleF rc,float fs){var L=Make(g,rc.Width,fs);var p=Plan;var t=Types;g.SmoothingMode=SmoothingMode.AntiAlias;
  float x0=rc.X,y=rc.Y;var ink=PowTheme.Ink;
  using(var tf=Font(fs*1.25f,true))using(var br=new SolidBrush(ink))g.DrawString(Title+"   wind toward "+t.Travel+"   (plan, levels "+string.Join(", ",Levels.Select(l=>F(l.Y)))+" m)",tf,br,x0,y);
  y+=L.TitleH;
  /* top legend: the wind, the outer WIND LOAD lines; axes on the right */
  using(var hf=Font(fs,true))using(var mf=Font(fs*0.92f,false))using(var muted=new SolidBrush(PowTheme.Muted)){
   var dir=Normal(t.Travel);float ax=x0+fs*0.5f,acy=y+L.RowH/2;Arrow(g,WwColor,fs*0.28f,new PointF(ax+fs*1.5f-dir.X*fs*1.3f,acy-dir.Y*fs*0.75f),new PointF(ax+fs*1.5f+dir.X*fs*1.3f,acy+dir.Y*fs*0.75f));
   using(var b=new SolidBrush(WwColor))g.DrawString("WIND toward "+t.Travel,hf,b,ax+fs*3.6f,y);
   float tx=x0+rc.Width-fs*5.5f,ty=y+fs*0.8f;Arrow(g,ink,fs*0.12f,new PointF(tx,ty),new PointF(tx+fs*3.2f,ty));Arrow(g,ink,fs*0.12f,new PointF(tx,ty),new PointF(tx,ty+fs*3.2f));
   using(var b=new SolidBrush(ink)){g.DrawString("X",hf,b,tx+fs*3.3f,ty-fs*0.6f);g.DrawString("Z",hf,b,tx-fs*0.4f,ty+fs*3.3f);g.DrawString("top view",mf,muted,tx-fs*1.2f,ty+fs*4.4f);}}
  Rows(g,L,L.Top,x0+fs*0.5f,y+L.RowH,fs);
  y+=L.TopH+L.Margin;float px=x0+(rc.Width-L.PlanW)/2,py=y;
  Func<double,double,PointF> P=(xx,zz)=>new PointF(px+(float)((xx-L.X0)*L.Scale),py+(float)((zz-L.Z0)*L.Scale));
  using(var fill=new SolidBrush(Color.FromArgb(236,240,238)))for(int i=0;i+1<p.Xs.Count;i++)for(int j=0;j+1<p.Zs.Count;j++)if(p.Inside[i,j]){var a=P(p.Xs[i],p.Zs[j]);var b=P(p.Xs[i+1],p.Zs[j+1]);g.FillRectangle(fill,a.X,a.Y,b.X-a.X,b.Y-a.Y);}
  using(var pen=new Pen(Color.FromArgb(185,192,189),Math.Max(0.5f,fs*0.05f)))foreach(var bm in p.Beams)g.DrawLine(pen,P(bm[0],bm[1]),P(bm[2],bm[3]));
  float step=fs*2.4f;
  foreach(var f in p.Faces){string role=RoleOf(f.Normal,t.Travel);var col=ColorOf(role);var a=f.Normal[1]=='X'?P(f.At,f.A1):P(f.A1,f.At);var b=f.Normal[1]=='X'?P(f.At,f.A2):P(f.A2,f.At);
   using(var pen=new Pen(f.Shielded?(f.Covered?col:Color.Gray):ink,f.Shielded?fs*0.42f:fs*0.14f))g.DrawLine(pen,a,b);
   if(f.Shielded&&!f.Covered)continue;var n=Normal(f.Normal);float len=L.ArrowLen;if(f.Shielded&&!double.IsInfinity(f.Free))len=(float)Math.Min(len,f.Free*L.Scale*0.42);if(len<fs*0.8f)len=fs*0.8f;
   float flen=(float)Math.Sqrt((b.X-a.X)*(b.X-a.X)+(b.Y-a.Y)*(b.Y-a.Y));int count=Math.Max(1,(int)(flen/step));
   for(int k=0;k<count;k++){float u=(k+0.5f)/count;var q=new PointF(a.X+(b.X-a.X)*u,a.Y+(b.Y-a.Y)*u);var o=new PointF(q.X+n.X*len,q.Y+n.Y*len);
    if(role=="WW")Arrow(g,col,fs*0.13f,o,new PointF(q.X+n.X*fs*0.15f,q.Y+n.Y*fs*0.15f));else Arrow(g,col,fs*0.13f,new PointF(q.X+n.X*fs*0.15f,q.Y+n.Y*fs*0.15f),o);}}
  /* tags of the recessed walls: beside the wall on the floor side, moved along the wall until they clear the others */
  using(var hf=Font(fs,true)){var placed=new List<RectangleF>();
   foreach(var tg in L.Tags){var f=tg.Line.Face;var n=Normal(f.Normal);var sz=g.MeasureString(tg.Name,hf);bool alongZ=f.Normal[1]=='X';
    var a=alongZ?P(f.At,f.A1):P(f.A1,f.At);var b=alongZ?P(f.At,f.A2):P(f.A2,f.At);    float off=fs*0.5f;RectangleF r=RectangleF.Empty;bool ok=false;
    foreach(float u in new[]{0.5f,0.25f,0.75f,0.1f,0.9f}){var q=new PointF(a.X+(b.X-a.X)*u,a.Y+(b.Y-a.Y)*u);float cx=q.X-n.X*(off+sz.Width/2),cy=q.Y-n.Y*(off+sz.Height/2);/* on the floor side of the wall, clear of the arrows */
     if(n.X==0)cx=q.X+off+sz.Width/2;if(n.Y==0)cy=q.Y-off-sz.Height/2;
     r=new RectangleF(cx-sz.Width/2,cy-sz.Height/2,sz.Width,sz.Height);if(!placed.Any(x=>x.IntersectsWith(r))){ok=true;break;}}
    if(!ok)while(placed.Any(x=>x.IntersectsWith(r)))r.Offset(0,sz.Height*0.9f);
    placed.Add(r);using(var bg=new SolidBrush(Color.FromArgb(225,Color.White)))g.FillRectangle(bg,r);using(var br=new SolidBrush(tg.Color))g.DrawString(tg.Name,hf,br,r.X,r.Y);}}
  /* bottom legend: the recessed walls */
  if(L.Bottom.Count>0){float by=py+L.PlanH+L.Margin;using(var hf=Font(fs,true))using(var br=new SolidBrush(ink))g.DrawString("Recessed walls (own WIND LOAD limited by an XR / ZR / YR range around the wall):",hf,br,x0+fs*0.5f,by);Rows(g,L,L.Bottom,x0+fs*0.5f,by+L.RowH*1.2f,fs);}}
 static PointF Normal(string n){return n=="+X"?new PointF(1,0):n=="-X"?new PointF(-1,0):n=="+Z"?new PointF(0,1):new PointF(0,-1);}
}
}
