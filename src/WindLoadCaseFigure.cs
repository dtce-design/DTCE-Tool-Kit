using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Drawing2D;using System.Globalization;using System.Linq;using System.Text.RegularExpressions;
namespace BeamScheduleFormatter {
/* one figure per wind load case (owner, 27 November 2026: "isa isahin mo based din kung ilan wind load cases"): the plan of the STAAD model
   with the loading of that case read from its own STAAD text - WIND LOAD lines as arrows (direction and factor), the recessed walls of its
   XR / ZR lines tagged R1, R2 ..., the torsion couple of its JOINT LOAD at the joints, the roof lines as one legend row. All values are in
   the legend; the plan carries only coloured arrows and short tags, so nothing overlaps. */
sealed class WindLoadCaseFigure {
 static readonly CultureInfo Ci=CultureInfo.InvariantCulture;
 public string Key="",Title="",Text="";public WindPlanLevel Plan;public StaadModel Model;/* the largest plan height (the report keeps two or three cases per page) */public float MaxPlan=float.MaxValue;
 static string F(double v){return Math.Round(v,3).ToString("0.###",Ci);}
 static Font Font(float size,bool bold){return new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel);}
 static readonly Color Purple=Color.FromArgb(110,60,150),Roof=Color.FromArgb(60,120,110);
 /* one figure per case key of the case texts, in the order of the load cases; the plan is the typical floor (the outline shared by most levels) */
 internal static List<WindLoadCaseFigure> Build(StaadModel model,Dictionary<string,string> texts,Dictionary<string,string> titles,double top,double baseY=0){var list=new List<WindLoadCaseFigure>();if(model==null||texts==null||texts.Count==0)return list;
  string note;var plans=WindEdgeLoads.Plans(model,baseY,top,out note).Where(p=>p.Y>baseY+1e-3).ToList();if(plans.Count==0)return list;
  var plan=plans.GroupBy(p=>p.Signature).OrderByDescending(g=>g.Count()).ThenByDescending(g=>g.Max(p=>p.Y)).First().OrderBy(p=>p.Y).Last();
  Func<string,int> order=k=>{var m=Regex.Match(k,@"^(1B|[234])?");string c=m.Groups[1].Value;return c==""?0:c=="1B"?1:c=="2"?2:c=="3"?3:4;};
  foreach(var k in texts.Keys.OrderBy(order).ThenBy(k=>Array.IndexOf(texts.Keys.ToArray(),k))){string title;if(titles==null||!titles.TryGetValue(k,out title))title="Wind case "+k;list.Add(new WindLoadCaseFigure{Key=k,Title=title,Text=texts[k],Plan=plan,Model=model});}
  return list;}
 /* ---- the case read from its STAAD text ---- */
 sealed class Load {public string Dir;public double Factor;public int Type;public bool Range;public double At,A1,A2;public char Axis;public string Line,Role,Tag;}
 sealed class Couple {public double Y,Mt,D;public int J1,J2;public double F1,F2;}
 List<Load> Loads(){var list=new List<Load>();foreach(var raw in Text.Replace("\r","").Split('\n')){var line=raw.Trim();var m=Regex.Match(line,@"^WIND LOAD (-?[XZ]) (-?[0-9.]+) TYPE (\d+)(.*)$");if(!m.Success)continue;
   var l=new Load{Dir=m.Groups[1].Value,Factor=double.Parse(m.Groups[2].Value,Ci),Type=int.Parse(m.Groups[3].Value),Line=line};var rest=m.Groups[4].Value;
   var xr=Regex.Match(rest,@"XR (-?[0-9.]+) (-?[0-9.]+)");var zr=Regex.Match(rest,@"ZR (-?[0-9.]+) (-?[0-9.]+)");
   if(xr.Success&&zr.Success){l.Range=true;double x1=double.Parse(xr.Groups[1].Value,Ci),x2=double.Parse(xr.Groups[2].Value,Ci),z1=double.Parse(zr.Groups[1].Value,Ci),z2=double.Parse(zr.Groups[2].Value,Ci);
    if(x2-x1<z2-z1){l.Axis='X';l.At=(x1+x2)/2;l.A1=z1+0.05;l.A2=z2-0.05;}else{l.Axis='Z';l.At=(z1+z2)/2;l.A1=x1+0.05;l.A2=x2-0.05;}}
   list.Add(l);}
  /* roles: the travel axis is the axis of the windward (factor > 0) lines; a negative factor on that axis is leeward, on the other a side wall */
  var axes=new HashSet<char>(list.Where(l=>l.Factor>0).Select(l=>l.Dir[l.Dir.Length-1]));
  foreach(var l in list){char a=l.Dir[l.Dir.Length-1];l.Role=l.Factor>0?"WW":axes.Contains(a)?"LW":"SW";}
  int r=0;foreach(var l in list.Where(x=>x.Range))l.Tag="R"+(++r);return list;}
 List<Couple> Couples(){var list=new List<Couple>();var lines=Text.Replace("\r","").Split('\n').Select(x=>x.Trim()).ToList();
  for(int i=0;i<lines.Count;i++){var m=Regex.Match(lines[i],@"^\* level (-?[0-9.]+) m: MT = (-?[0-9.]+) kN-m, d = ([0-9.]+) m");if(!m.Success||i+2>=lines.Count)continue;
   var a=Regex.Match(lines[i+1],@"^(\d+) FX (-?[0-9.]+)");var b=Regex.Match(lines[i+2],@"^(\d+) FX (-?[0-9.]+)");if(!a.Success||!b.Success)continue;
   list.Add(new Couple{Y=double.Parse(m.Groups[1].Value,Ci),Mt=double.Parse(m.Groups[2].Value,Ci),D=double.Parse(m.Groups[3].Value,Ci),J1=int.Parse(a.Groups[1].Value),F1=double.Parse(a.Groups[2].Value,Ci),J2=int.Parse(b.Groups[1].Value),F2=double.Parse(b.Groups[2].Value,Ci)});}
  return list;}
 string RoofSummary(){int floor=Regex.Matches(Text,@"\bFLOAD\b").Count,member=Regex.Matches(Text,@"\bUNI Y\b").Count;if(floor+member==0)return "";
  bool up=Text.Contains("+GCpi, uplift");return (floor>0?floor+" FLOOR LOAD lines":"")+(floor>0&&member>0?" and ":"")+(member>0?member+" MEMBER LOAD lines (truss top chords)":"")+" - roof "+(up?"uplift, +GCpi / Cp branch 1":"-GCpi / Cp branch 2")+"; overhangs included";}
 /* ---- layout ---- */
 sealed class Row {public Color Color;public string Label,Body;public float H;}
 sealed class Layout {public float Pad,TitleH,TopH,BottomH,Margin,PlanH,PlanW,Scale,Total,RowH,LabelW;public List<string> TitleLines=new List<string>();public List<Row> Top=new List<Row>(),Bottom=new List<Row>();}
 static Color ColorOf(string role){return role=="WW"?WindCaseFigure.WwColor:role=="LW"?WindCaseFigure.LwColor:WindCaseFigure.SwColor;}
 Layout Make(Graphics g,float width,float fs){var L=new Layout{Pad=fs*0.8f,RowH=fs*1.45f};var loads=Loads();
  using(var tf=Font(fs*1.2f,true)){var words=(Title+"  -  "+Summary()).Split(' ');string cur="";foreach(var w in words){string t=cur==""?w:cur+" "+w;if(g.MeasureString(t,tf).Width>width&&cur!=""){L.TitleLines.Add(cur);cur=w;}else cur=t;}if(cur!="")L.TitleLines.Add(cur);L.TitleH=L.TitleLines.Count*g.MeasureString("X",tf).Height+L.Pad*0.4f;}
  foreach(var l in loads.Where(x=>!x.Range))L.Top.Add(new Row{Color=ColorOf(l.Role),Label=l.Role,Body=l.Line});
  var roof=RoofSummary();if(roof!="")L.Top.Add(new Row{Color=Roof,Label="Roof",Body=roof});
  foreach(var c in Couples())L.Top.Add(new Row{Color=Purple,Label="Torsion",Body="level "+F(c.Y)+" m: MT = "+F(c.Mt)+" kN·m = "+F(Math.Abs(c.F1))+" kN × "+F(c.D)+" m - JOINT LOAD "+c.J1+" FX "+F(c.F1)+", "+c.J2+" FX "+F(c.F2)});
  foreach(var l in loads.Where(x=>x.Range))L.Bottom.Add(new Row{Color=ColorOf(l.Role),Label=l.Tag+" "+l.Role,Body=l.Line});
  using(var hf=Font(fs,true))using(var bf=Font(fs,false)){L.LabelW=L.Top.Concat(L.Bottom).Select(r=>g.MeasureString(r.Label,hf).Width).DefaultIfEmpty(0).Max()+fs*1.8f;
   foreach(var r in L.Top.Concat(L.Bottom)){var h=g.MeasureString(r.Body,bf,new SizeF(Math.Max(10,width-L.LabelW-fs),1000)).Height;r.H=Math.Max(L.RowH,h+fs*0.3f);}}
  L.TopH=L.Top.Sum(r=>r.H)+L.Pad*0.5f;L.BottomH=L.Bottom.Count==0?0:L.Bottom.Sum(r=>r.H)+L.Pad*0.5f;
  var p=Plan;double sx=Math.Max(p.Xs.Last()-p.Xs.First(),1e-3),sz=Math.Max(p.Zs.Last()-p.Zs.First(),1e-3);L.Margin=fs*3.6f;L.Scale=(float)Math.Min(Math.Min((width-2*L.Margin)/sx,width*0.42/sz),MaxPlan/sz);L.PlanW=(float)(sx*L.Scale);L.PlanH=(float)(sz*L.Scale);
  L.Total=L.TitleH+L.TopH+L.Margin*2+L.PlanH+L.BottomH+L.Pad;return L;}
 /* the case in words, from its key */
 string Summary(){var m=Regex.Match(Key,@"^(1B|[234])?((?:[+-][XZ]){1,2})([+-]T)?$");if(!m.Success)return "";string c=m.Groups[1].Value,d=m.Groups[2].Value,t=m.Groups[3].Value;
  return c==""?"Case 1: full pressures toward "+d+", side walls":c=="1B"?"Case 1, -GCpi: full pressures toward "+d+", roof -GCpi":c=="2"?"Case 2: 75 % toward "+d+", torsion "+t:c=="3"?"Case 3: 75 % toward "+d.Substring(0,2)+" and "+d.Substring(2):"Case 4: 56.3 % toward "+d.Substring(0,2)+" and "+d.Substring(2)+", torsion "+t;}
 internal float Height(Graphics g,float width,float fs){return Make(g,width,fs).Total;}
 internal float ReportHeight(float width,float fs){using(var bmp=new Bitmap(1,1)){bmp.SetResolution(72,72);using(var g=Graphics.FromImage(bmp))return Height(g,width,fs);}}
 static void Arrow(Graphics g,Color c,float w,PointF a,PointF b){using(var pen=new Pen(c,w))g.DrawLine(pen,a,b);double ang=Math.Atan2(b.Y-a.Y,b.X-a.X);float h=w*3.2f+2.5f;
  var p1=new PointF(b.X-(float)(Math.Cos(ang-.42)*h),b.Y-(float)(Math.Sin(ang-.42)*h));var p2=new PointF(b.X-(float)(Math.Cos(ang+.42)*h),b.Y-(float)(Math.Sin(ang+.42)*h));using(var br=new SolidBrush(c))g.FillPolygon(br,new[]{b,p1,p2});}
 void Rows(Graphics g,Layout L,List<Row> rows,float x,float y,float width,float fs){using(var hf=Font(fs,true))using(var bf=Font(fs,false))foreach(var r in rows){using(var b=new SolidBrush(r.Color)){g.FillRectangle(b,x,y+L.RowH*0.25f,fs*0.9f,L.RowH*0.5f);g.DrawString(r.Label,hf,b,x+fs*1.2f,y);g.DrawString(r.Body,bf,b,new RectangleF(x+L.LabelW,y,width-L.LabelW-fs,r.H));}y+=r.H;}}
 internal void Draw(Graphics g,RectangleF rc,float fs){var L=Make(g,rc.Width,fs);var p=Plan;g.SmoothingMode=SmoothingMode.AntiAlias;float x0=rc.X,y=rc.Y;var ink=PowTheme.Ink;
  using(var tf=Font(fs*1.2f,true))using(var br=new SolidBrush(ink)){float lh=g.MeasureString("X",tf).Height;foreach(var t in L.TitleLines){g.DrawString(t,tf,br,x0,y);y+=lh;}}y+=L.Pad*0.4f;
  Rows(g,L,L.Top,x0+fs*0.3f,y,rc.Width,fs);y+=L.TopH+L.Margin;
  float px=x0+(rc.Width-L.PlanW)/2,py=y;double X0=p.Xs.First(),Z0=p.Zs.First();Func<double,double,PointF> P=(xx,zz)=>new PointF(px+(float)((xx-X0)*L.Scale),py+(float)((zz-Z0)*L.Scale));
  using(var fill=new SolidBrush(Color.FromArgb(236,240,238)))for(int i=0;i+1<p.Xs.Count;i++)for(int j=0;j+1<p.Zs.Count;j++)if(p.Inside[i,j]){var a=P(p.Xs[i],p.Zs[j]);var b=P(p.Xs[i+1],p.Zs[j+1]);g.FillRectangle(fill,a.X,a.Y,b.X-a.X+0.5f,b.Y-a.Y+0.5f);}
  var loads=Loads();float len=fs*2.6f,step=fs*2.2f;
  foreach(var f in p.Faces){var a=f.Normal[1]=='X'?P(f.At,f.A1):P(f.A1,f.At);var b=f.Normal[1]=='X'?P(f.At,f.A2):P(f.A2,f.At);
   Load load=null;if(f.Shielded){load=loads.FirstOrDefault(l=>l.Range&&l.Axis==f.Normal[1]&&Math.Abs(l.At-f.At)<0.02&&l.A1<f.A2-1e-3&&l.A2>f.A1+1e-3);}
   else{/* the outer WIND LOAD line that reaches this face: wind blowing toward -normal reaches it with its direction = -normal */string dir=(f.Normal[0]=='+'?"-":"")+f.Normal[1];load=loads.FirstOrDefault(l=>!l.Range&&l.Dir==dir);}
   using(var pen=new Pen(f.Shielded&&load!=null?ColorOf(load.Role):ink,f.Shielded&&load!=null?fs*0.36f:fs*0.12f))g.DrawLine(pen,a,b);if(load==null)continue;
   var col=ColorOf(load.Role);var n=f.Normal=="+X"?new PointF(1,0):f.Normal=="-X"?new PointF(-1,0):f.Normal=="+Z"?new PointF(0,1):new PointF(0,-1);
   float l0=(float)(len*Math.Min(1,Math.Abs(load.Factor)));if(f.Shielded&&!double.IsInfinity(f.Free))l0=(float)Math.Min(l0,f.Free*L.Scale*0.42);l0=Math.Max(l0,fs*0.7f);
   float fl=(float)Math.Sqrt((b.X-a.X)*(b.X-a.X)+(b.Y-a.Y)*(b.Y-a.Y));int count=Math.Max(1,(int)(fl/step));
   for(int k=0;k<count;k++){float u=(k+0.5f)/count;var q=new PointF(a.X+(b.X-a.X)*u,a.Y+(b.Y-a.Y)*u);var o=new PointF(q.X+n.X*l0,q.Y+n.Y*l0);var s=new PointF(q.X+n.X*fs*0.12f,q.Y+n.Y*fs*0.12f);
    if(load.Factor>0)Arrow(g,col,fs*0.11f,o,s);else Arrow(g,col,fs*0.11f,s,o);}
   if(f.Shielded&&load.Tag!=null)using(var hf=Font(fs*0.95f,true))using(var br=new SolidBrush(col)){var sz=g.MeasureString(load.Tag,hf);var mid=new PointF((a.X+b.X)/2,(a.Y+b.Y)/2);g.DrawString(load.Tag,hf,br,mid.X-n.X*(fs*0.5f+sz.Width/2)-sz.Width/2,mid.Y-n.Y*(fs*0.5f+sz.Height/2)-sz.Height/2);}}
  /* the torsion couple at the joints of this plan's level (or the nearest level listed): FX arrows at both joints, a tag with the joint number,
     and the turning sense as an arc at the middle of the plan (screen y = Z, so clockwise on the page = -sum z FX > 0) */
  var cs=Couples();if(cs.Count>0&&Model!=null){var c=cs.OrderBy(q=>Math.Abs(q.Y-p.Y)).First();StaadJoint j1,j2;
   if(Model.Joints.TryGetValue(c.J1,out j1)&&Model.Joints.TryGetValue(c.J2,out j2)){double tau=0;float al=fs*3.2f;
    foreach(var jf in new[]{Tuple.Create(j1,c.F1),Tuple.Create(j2,c.F2)}){var q=P(Model.P1(jf.Item1),Model.P2(jf.Item1));tau+=-Model.P2(jf.Item1)*jf.Item2;float d=jf.Item2>=0?1:-1;
     Arrow(g,Purple,fs*0.2f,new PointF(q.X-d*al,q.Y),q);using(var br=new SolidBrush(Purple))g.FillEllipse(br,q.X-fs*0.28f,q.Y-fs*0.28f,fs*0.56f,fs*0.56f);
     using(var hf=Font(fs*0.9f,true))using(var br=new SolidBrush(Purple)){string tag="J"+jf.Item1.No;var sz=g.MeasureString(tag,hf);bool top=Model.P2(jf.Item1)<=(p.Zs.First()+p.Zs.Last())/2;g.DrawString(tag,hf,br,q.X-d*al/2-sz.Width/2,top?q.Y+fs*0.35f:q.Y-sz.Height-fs*0.35f);/* inside the plan, clear of the wind arrows outside */}}
    var cen=P((p.Xs.First()+p.Xs.Last())/2,(p.Zs.First()+p.Zs.Last())/2);float rr=Math.Max(fs*1.6f,Math.Min(L.PlanW,L.PlanH)*0.14f);float sweep=tau>=0?250:-250;
    using(var pen=new Pen(Purple,fs*0.16f))g.DrawArc(pen,cen.X-rr,cen.Y-rr,2*rr,2*rr,200,sweep);double e=(200+sweep)*Math.PI/180;var tip=new PointF(cen.X+(float)(Math.Cos(e)*rr),cen.Y+(float)(Math.Sin(e)*rr));
    float sg=sweep>0?1:-1;var tan=new PointF((float)(-sg*Math.Sin(e)),(float)(sg*Math.Cos(e)));Arrow(g,Purple,fs*0.16f,new PointF(tip.X-tan.X*fs*0.8f,tip.Y-tan.Y*fs*0.8f),tip);
    using(var hf=Font(fs,true))using(var br=new SolidBrush(Purple)){var sz=g.MeasureString("MT",hf);g.DrawString("MT",hf,br,cen.X-sz.Width/2,cen.Y-sz.Height/2);}}}
  y=py+L.PlanH+L.Margin;if(L.Bottom.Count>0)Rows(g,L,L.Bottom,x0+fs*0.3f,y,rc.Width,fs);}
}
}
