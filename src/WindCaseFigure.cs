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
  L.Top.Add(new Row{Color=WwColor,Label="WW"+t.Travel,Text="WIND LOAD "+WindEdgeLoads.Spec(back,false,1)+" TYPE "+t.Ww+" YR "+top,Note=Side(back)+" faces, pushed in, "+Pressure(t.WwInt.Count>0?t.WwInt.Min():0,t.WwInt.Count>0?t.WwInt.Max():0)});
  L.Top.Add(new Row{Color=LwColor,Label="LW"+t.Travel,Text="WIND LOAD "+WindEdgeLoads.Spec(t.Travel,true,1)+" TYPE "+t.Lw+" YR "+top,Note=Side(t.Travel)+" faces, suction, "+Pressure(t.Leeward,t.Leeward)});
  L.Top.Add(new Row{Color=SwColor,Label="SW"+t.Travel,Text="WIND LOAD "+WindEdgeLoads.Spec("+"+other,true,1)+" and "+WindEdgeLoads.Spec("-"+other,true,1)+" TYPE "+t.Sw+" YR "+top,Note=other+"-min and "+other+"-max faces, suction, "+Pressure(t.Side,t.Side)});
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
/* the four design wind load cases of NSCP 2015 Figure 207B.4-8 on the building's plan (owner, 27 November 2026: "pano ang loading neto?"):
   one panel per case for wind along +X (and +Z for Cases 3 / 4) - windward pushed in, leeward pulled out, each with its factor; side walls
   only in Case 1; the torsion MT of Cases 2 / 4 as a curved arrow with the couple of joint loads at the level shown. Values are those of
   one level; every level is in the report table. */
sealed class WindDesignCasesFigure {
 static readonly CultureInfo Ci=CultureInfo.InvariantCulture;
 static string F(double v){return Math.Round(v,2).ToString("0.##",Ci);}
 public WindPlanLevel Plan;public WindFaceTypes X,Z;public WindEdgeLoads.TorsionLevel T2,T4;public double BaseY;
 internal static WindDesignCasesFigure Build(StaadModel model,Dictionary<string,string> link,double baseY=0){if(model==null||link==null)return null;string px,pz;if(!link.TryGetValue("+X",out px)||!link.TryGetValue("+Z",out pz))return null;
  var x=WindFaceTypes.Unpack("+X",px);var z=WindFaceTypes.Unpack("+Z",pz);if(x==null||z==null)return null;string note;var plans=WindEdgeLoads.Plans(model,baseY,Math.Max(x.Top,z.Top),out note);var shown=plans.Where(p=>p.Y>baseY+1e-3).ToList();if(shown.Count==0)return null;
  /* the level with the largest torsion (usually a typical upper floor) */
  var t2=WindEdgeLoads.Torsion(model,plans,new List<WindFaceTypes>{x},0.75,baseY);var t4=WindEdgeLoads.Torsion(model,plans,new List<WindFaceTypes>{x,z},0.563,baseY);if(t2.Count==0)return null;
  var top=t2.OrderByDescending(l=>l.Mt).First();var plan=shown.FirstOrDefault(p=>Math.Abs(p.Y-top.Y)<1e-6)??shown.Last();
  return new WindDesignCasesFigure{Plan=plan,X=x,Z=z,T2=top,T4=t4.FirstOrDefault(l=>Math.Abs(l.Y-top.Y)<1e-6),BaseY=baseY};}
 static Font Font(float size,bool bold){return new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel);}
 internal float Height(float width,float fs){float panelW=width/2;var p=Plan;double sx=p.Xs.Last()-p.Xs.First(),sz=p.Zs.Last()-p.Zs.First();float m=fs*3.4f;float scale=(float)Math.Min((panelW-2*m)/sx,panelW*0.5/sz);
  return fs*2.2f+2*(fs*3.4f+2*m+(float)(sz*scale)+fs*0.8f)+fs*3.2f;}
 internal float ReportHeight(float width,float fs){return Height(width,fs);}
 static void Arrow(Graphics g,Color c,float w,PointF a,PointF b){using(var pen=new Pen(c,w))g.DrawLine(pen,a,b);double ang=Math.Atan2(b.Y-a.Y,b.X-a.X);float h=w*3.2f+2.5f;
  var p1=new PointF(b.X-(float)(Math.Cos(ang-.42)*h),b.Y-(float)(Math.Sin(ang-.42)*h));var p2=new PointF(b.X-(float)(Math.Cos(ang+.42)*h),b.Y-(float)(Math.Sin(ang+.42)*h));using(var br=new SolidBrush(c))g.FillPolygon(br,new[]{b,p1,p2});}
 internal void Draw(Graphics g,RectangleF rc,float fs){g.SmoothingMode=SmoothingMode.AntiAlias;var ink=PowTheme.Ink;
  using(var tf=Font(fs*1.2f,true))using(var b=new SolidBrush(ink))g.DrawString("Design wind load cases, NSCP 2015 Figure 207B.4-8 (directional procedure) - plan at level "+F(Plan.Y)+" m, wind toward +X / +Z",tf,b,rc.X,rc.Y);
  float top=rc.Y+fs*2.2f;float panelW=rc.Width/2,panelH=(rc.Height-fs*2.2f-fs*3.2f)/2;
  string ww=F(X.WindwardAt(T2.Y)),lw=F(X.Leeward),wz=F(Z.WindwardAt(T2.Y)),lz=F(Z.Leeward);
  var cases=new[]{
   new{Title="Case 1  WL+X",Sub="PWX = "+ww+", PLX = "+lw+" kPa (full); side walls "+F(X.Side)+" kPa",Fx=1.0,Fz=0.0,Side=true,T=0},
   new{Title="Case 2  WL2+X±T",Sub="0.75 PWX, 0.75 PLX;  MT = 0.75 (PWX + PLX) BX h · (±0.15 BX) = ±"+F(T2.Mt)+" kN·m",Fx=0.75,Fz=0.0,Side=false,T=2},
   new{Title="Case 3  WL3+X+Z",Sub="0.75 PWX, 0.75 PLX and 0.75 PWZ, 0.75 PLZ at once (PWZ = "+wz+", PLZ = "+lz+" kPa)",Fx=0.75,Fz=0.75,Side=false,T=0},
   new{Title="Case 4  WL4+X+Z±T",Sub="0.563 of both axes;  MT = 0.563 [(PWX+PLX) BX h eX + (PWZ+PLZ) BZ h eZ] = ±"+F(T4!=null?T4.Mt:0)+" kN·m",Fx=0.563,Fz=0.563,Side=false,T=4}};
  for(int k=0;k<4;k++){var c=cases[k];float ox=rc.X+(k%2)*panelW,oy=top+(k/2)*panelH;var box=new RectangleF(ox+fs*0.3f,oy,panelW-fs*0.6f,panelH-fs*0.4f);
   using(var pen=new Pen(Color.FromArgb(205,212,209),Math.Max(0.6f,fs*0.06f)))g.DrawRectangle(pen,box.X,box.Y,box.Width,box.Height);
   using(var hf=Font(fs,true))using(var sf=Font(fs*0.92f,false))using(var b=new SolidBrush(ink))using(var muted=new SolidBrush(PowTheme.Muted)){g.DrawString(c.Title,hf,b,box.X+fs*0.4f,box.Y+fs*0.3f);g.DrawString(c.Sub,sf,muted,new RectangleF(box.X+fs*0.4f,box.Y+fs*1.6f,box.Width-fs*0.8f,fs*2.4f));}
   DrawPlan(g,new RectangleF(box.X,box.Y+fs*3.4f,box.Width,box.Height-fs*3.4f),fs,c.Fx,c.Fz,c.Side,c.T);}
  using(var sf=Font(fs*0.92f,false))using(var muted=new SolidBrush(PowTheme.Muted))g.DrawString("Blue: windward pushed in; red: leeward pulled out; orange: side walls (Case 1 only). Arrow length follows the factor. Torsion: curved arrow MT, applied in STAAD as the couple F at the two joints shown (JOINT LOAD FX, no net force); the −T cases reverse it. Cases for −X / −Z are the mirror images.",sf,muted,new RectangleF(rc.X,rc.Bottom-fs*3.0f,rc.Width,fs*3.0f));}
 void DrawPlan(Graphics g,RectangleF r,float fs,double fx,double fz,bool side,int torsion){var p=Plan;double x0=p.Xs.First(),x1=p.Xs.Last(),z0=p.Zs.First(),z1=p.Zs.Last();float m=fs*3.4f;
  float scale=(float)Math.Min((r.Width-2*m)/(x1-x0),(r.Height-2*m)/(z1-z0));float pw=(float)((x1-x0)*scale),ph=(float)((z1-z0)*scale);float px=r.X+(r.Width-pw)/2,py=r.Y+(r.Height-ph)/2;
  Func<double,double,PointF> P=(xx,zz)=>new PointF(px+(float)((xx-x0)*scale),py+(float)((zz-z0)*scale));
  using(var fill=new SolidBrush(Color.FromArgb(236,240,238)))for(int i=0;i+1<p.Xs.Count;i++)for(int j=0;j+1<p.Zs.Count;j++)if(p.Inside[i,j]){var a=P(p.Xs[i],p.Zs[j]);var b=P(p.Xs[i+1],p.Zs[j+1]);g.FillRectangle(fill,a.X,a.Y,b.X-a.X+0.5f,b.Y-a.Y+0.5f);}
  float len=fs*2.6f,step=fs*1.9f;
  foreach(var f in p.Faces){var a=f.Normal[1]=='X'?P(f.At,f.A1):P(f.A1,f.At);var b=f.Normal[1]=='X'?P(f.At,f.A2):P(f.A2,f.At);
   using(var pen=new Pen(PowTheme.Ink,fs*0.12f))g.DrawLine(pen,a,b);
   char axis=f.Normal[1];double fac=axis=='X'?fx:fz;string role=axis=='X'?(f.Normal[0]=='-'?"WW":"LW"):(fz>0?(f.Normal[0]=='-'?"WW":"LW"):"SW");if(axis=='Z'&&fz==0)fac=side?1:0;
   if(fac<=0||(role=="SW"&&!side))continue;if(f.Shielded&&!f.Covered)continue;
   var col=role=="WW"?WindCaseFigure.WwColor:role=="LW"?WindCaseFigure.LwColor:WindCaseFigure.SwColor;var n=f.Normal=="+X"?new PointF(1,0):f.Normal=="-X"?new PointF(-1,0):f.Normal=="+Z"?new PointF(0,1):new PointF(0,-1);
   float L=(float)(len*Math.Min(1,fac));if(f.Shielded&&!double.IsInfinity(f.Free))L=(float)Math.Min(L,f.Free*scale*0.42);L=Math.Max(L,fs*0.7f);
   if(f.Shielded)using(var pen=new Pen(col,fs*0.3f))g.DrawLine(pen,a,b);
   float fl=(float)Math.Sqrt((b.X-a.X)*(b.X-a.X)+(b.Y-a.Y)*(b.Y-a.Y));int count=Math.Max(1,(int)(fl/step));
   for(int k=0;k<count;k++){float u=(k+0.5f)/count;var q=new PointF(a.X+(b.X-a.X)*u,a.Y+(b.Y-a.Y)*u);var o=new PointF(q.X+n.X*L,q.Y+n.Y*L);
    if(role=="WW")Arrow(g,col,fs*0.11f,o,new PointF(q.X+n.X*fs*0.12f,q.Y+n.Y*fs*0.12f));else Arrow(g,col,fs*0.11f,new PointF(q.X+n.X*fs*0.12f,q.Y+n.Y*fs*0.12f),o);}}
  if(torsion==0)return;var t=torsion==2?T2:T4;if(t==null)return;
  /* torsion: curved arrow at the plan centre and the couple at the two joints (positive MT about +Y: with X right and Z down, +Y points at the viewer, so +T turns counterclockwise on the plan - the same sense as the couple +F at Z-max, -F at Z-min) */
  var cen=P((x0+x1)/2,(z0+z1)/2);float rr=Math.Min(pw,ph)*0.22f;var purple=Color.FromArgb(110,60,150);
  using(var pen=new Pen(purple,fs*0.18f))g.DrawArc(pen,cen.X-rr,cen.Y-rr,2*rr,2*rr,200,-250);
  double end=(200-250)*Math.PI/180;var tip=new PointF(cen.X+(float)(Math.Cos(end)*rr),cen.Y+(float)(Math.Sin(end)*rr));var tan=new PointF((float)Math.Sin(end),(float)-Math.Cos(end));
  Arrow(g,purple,fs*0.18f,new PointF(tip.X-tan.X*fs*0.6f,tip.Y-tan.Y*fs*0.6f),tip);
  using(var hf=Font(fs,true))using(var br=new SolidBrush(purple)){var lbl="MT ±"+F(t.Mt)+" kN·m";var sz=g.MeasureString(lbl,hf);g.DrawString(lbl,hf,br,cen.X+rr+fs*0.5f,cen.Y-sz.Height/2);
   /* the couple at its two joints, drawn just inside the floor so it stays clear of the wind arrows outside */
   double f=t.Mt/t.D;var j2=P(t.X2,t.Z2);var j1=P(t.X1,t.Z1);float d=fs*1.3f;
   using(var dot=new SolidBrush(purple)){g.FillEllipse(dot,j2.X-fs*0.25f,j2.Y-fs*0.25f,fs*0.5f,fs*0.5f);g.FillEllipse(dot,j1.X-fs*0.25f,j1.Y-fs*0.25f,fs*0.5f,fs*0.5f);}
   Arrow(g,purple,fs*0.16f,new PointF(j2.X-fs*1.8f,j2.Y-d),new PointF(j2.X+fs*1.8f,j2.Y-d));Arrow(g,purple,fs*0.16f,new PointF(j1.X+fs*1.8f,j1.Y+d),new PointF(j1.X-fs*1.8f,j1.Y+d));
   using(var sf=Font(fs*0.92f,false)){var s1="F = "+F(f)+" kN, joint "+t.J2;var z1s=g.MeasureString(s1,sf);float tx=Math.Min(Math.Max(j2.X-z1s.Width/2,px),px+pw-z1s.Width);g.DrawString(s1,sf,br,tx,j2.Y-d-fs*0.6f-z1s.Height);
    var s2="F = "+F(f)+" kN, joint "+t.J1;var z2s=g.MeasureString(s2,sf);tx=Math.Min(Math.Max(j1.X-z2s.Width/2,px),px+pw-z2s.Width);g.DrawString(s2,sf,br,tx,j1.Y+d+fs*0.6f);}}}
}
}
