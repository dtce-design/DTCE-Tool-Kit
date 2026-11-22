using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Imaging;using System.Linq;using System.Runtime.InteropServices;
namespace BeamScheduleFormatter {
/* Fault lines measured on a PHIVOLCS active fault map KMZ (owner, 22 November 2026: "hindi ba niya pwede sukatin yung nasa inupload
   kong kmz file?"). The KMZ holds only georeferenced images, so the red active fault lines (solid, dashed, dotted offshore
   projections) are traced from the most detailed tiles: red pixels are marked on a grid of about 150 m, the small gaps are closed
   (dotted lines), the lines are thinned to their centre and followed into polylines, and each dash is linked to the next one in line
   with it (up to about 2 km) so a dashed fault is one line.
   Each connected line is one fault trace; its length is measured along the polyline. The legend samples (short horizontal lines)
   and filled red symbols (the logo) are left out. Black "potentially active" faults are not traced: black is also the label text. */
static class KmzFaultTracer {
 internal static Action<byte[],int,int,string> Stage;/* test hook: the grid after each step */
 internal static bool IsRed(int r,int g,int b,int a){return a>100&&r>=150&&r-Math.Max(g,b)>=70;}
 internal static List<FaultTrace> Trace(List<KeyValuePair<RectangleF,Image>> tiles){var result=new List<FaultTrace>();if(tiles==null||tiles.Count==0)return result;
  /* the most detailed tiles: the smallest degrees per pixel */
  double best=tiles.Min(t=>Math.Abs((double)t.Key.Width)/Math.Max(1,t.Value.Width));var use=tiles.Where(t=>Math.Abs((double)t.Key.Width)/Math.Max(1,t.Value.Width)<=best*1.02).ToList();
  double W=use.Min(t=>(double)t.Key.Left),E=use.Max(t=>(double)t.Key.Right),N=use.Max(t=>(double)Math.Max(t.Key.Top,t.Key.Bottom)),S=use.Min(t=>(double)Math.Min(t.Key.Top,t.Key.Bottom));
  var t0=use[0];double dx=Math.Abs((double)t0.Key.Width)/t0.Value.Width,dy=Math.Abs((double)t0.Key.Height)/t0.Value.Height;
  double midLat=(N+S)/2,pxKm=dx*111.32*Math.Cos(midLat*Math.PI/180);int f=Math.Max(1,(int)Math.Round(0.15/Math.Max(pxKm,1e-6)));if(f>8)f=8;
  int pw=(int)Math.Round((E-W)/dx),ph=(int)Math.Round((N-S)/dy);/* a large map (e.g. the whole country) on a coarser grid */while((long)((pw+f-1)/f)*((ph+f-1)/f)>40000000L)f++;int gw=(pw+f-1)/f,gh=(ph+f-1)/f;if(gw<3||gh<3)return result;
  double cellKm=pxKm*f;var count=new byte[gw*gh];
  foreach(var t in use){int ox=(int)Math.Round((t.Key.Left-W)/dx),oy=(int)Math.Round((N-Math.Max(t.Key.Top,t.Key.Bottom))/dy);
   using(var bmp=new Bitmap(t.Value)){var data=bmp.LockBits(new Rectangle(0,0,bmp.Width,bmp.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);try{var row=new byte[data.Stride];
    for(int y=0;y<bmp.Height;y++){Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),row,0,data.Stride);int gy=(oy+y)/f;if(gy<0||gy>=gh)continue;
     for(int x=0;x<bmp.Width;x++){int k=x*4;if(!IsRed(row[k+2],row[k+1],row[k],row[k+3]))continue;int gx=(ox+x)/f;if(gx<0||gx>=gw)continue;int i=gy*gw+gx;if(count[i]<255)count[i]++;}}}finally{bmp.UnlockBits(data);}}}
  var mask=new byte[gw*gh];for(int i=0;i<mask.Length;i++)mask[i]=(byte)(count[i]>0?1:0);if(Stage!=null)Stage(mask,gw,gh,"red");
  /* close the small gaps (the dotted offshore projections), thin to the centre lines */int r=Math.Max(1,(int)Math.Round(0.25/cellKm));mask=Morph(Morph(mask,gw,gh,r,true),gw,gh,r,false);if(Stage!=null)Stage(mask,gw,gh,"closed");
  Thin(mask,gw,gh);if(Stage!=null)Stage(mask,gw,gh,"thin");
  /* the pieces: dashes and solid lines (8-neighbour), each followed into paths */
  var seen=new bool[gw*gh];var pieces=new List<List<List<int>>>();
  for(int s=0;s<mask.Length;s++){if(mask[s]==0||seen[s])continue;var comp=new List<int>();var stack=new Stack<int>();stack.Push(s);seen[s]=true;
   while(stack.Count>0){int p=stack.Pop();comp.Add(p);int px=p%gw,py=p/gw;for(int yy=-1;yy<=1;yy++)for(int xx=-1;xx<=1;xx++){int qx=px+xx,qy=py+yy;if(qx<0||qy<0||qx>=gw||qy>=gh)continue;int q=qy*gw+qx;if(mask[q]!=0&&!seen[q]){seen[q]=true;stack.Push(q);}}}
   var paths=comp.Count==1?new List<List<int>>{comp}:Paths(new HashSet<int>(comp),gw,gh);if(paths.Count>0)pieces.Add(paths);}
  /* the ends of the pieces with their direction (outwards); a short dash or dot has none */
  var ends=new List<double[]>();/* x, y, dirX, dirY (NaN = none), piece */
  double reach0=2.0/cellKm;var dashSeg=new Dictionary<int,double[]>();/* a dash measured end to end */
  for(int k=0;k<pieces.Count;k++){var pc=pieces[k];var cells=new HashSet<int>(pc.SelectMany(q=>q));
   int extent=Math.Max(cells.Max(q=>q%gw)-cells.Min(q=>q%gw),cells.Max(q=>q/gw)-cells.Min(q=>q/gw));
   if(cells.Count<6||extent<reach0){/* a dash: its direction is the long axis of its red pixels, an end each way */int x0=cells.Min(q=>q%gw)-1,x1=cells.Max(q=>q%gw)+1,y0=cells.Min(q=>q/gw)-1,y1=cells.Max(q=>q/gw)+1;double sx=0,sy=0,sxx=0,syy=0,sxy=0;int m=0;
    for(int y=Math.Max(0,y0);y<=Math.Min(gh-1,y1);y++)for(int x=Math.Max(0,x0);x<=Math.Min(gw-1,x1);x++)if(count[y*gw+x]>0){double w8=count[y*gw+x];m++;sx+=w8*x;sy+=w8*y;sxx+=w8*x*x;syy+=w8*y*y;sxy+=w8*x*y;}
    double cxp=cells.Average(q=>(double)(q%gw)),cyp=cells.Average(q=>(double)(q/gw));double ax=double.NaN,ay=double.NaN;
    if(m>=3){double wsum=0;for(int y=Math.Max(0,y0);y<=Math.Min(gh-1,y1);y++)for(int x=Math.Max(0,x0);x<=Math.Min(gw-1,x1);x++)wsum+=count[y*gw+x];double mx=sx/wsum,my=sy/wsum,vxx=sxx/wsum-mx*mx,vyy=syy/wsum-my*my,vxy=sxy/wsum-mx*my;
     /* only an elongated dash has a direction */double tr=vxx+vyy,det=vxx*vyy-vxy*vxy,l1=tr/2+Math.Sqrt(Math.Max(0,tr*tr/4-det)),l2=tr/2-Math.Sqrt(Math.Max(0,tr*tr/4-det));if(l1>2.5*Math.Max(l2,0.05)){double th=0.5*Math.Atan2(2*vxy,vxx-vyy);ax=Math.Cos(th);ay=Math.Sin(th);}}
    if(double.IsNaN(ax)){ends.Add(new double[]{cxp,cyp,double.NaN,double.NaN,k});continue;}
    /* the ends at the outermost red pixels of the dash (thinning shortens it), within its own width */double pmin=0,pmax=0;for(int y=Math.Max(0,y0);y<=Math.Min(gh-1,y1);y++)for(int x=Math.Max(0,x0);x<=Math.Min(gw-1,x1);x++){if(count[y*gw+x]==0)continue;double along=(x-cxp)*ax+(y-cyp)*ay,across=Math.Abs(-(x-cxp)*ay+(y-cyp)*ax);if(across>2)continue;pmin=Math.Min(pmin,along);pmax=Math.Max(pmax,along);}
    dashSeg[k]=new[]{cxp+pmin*ax,cyp+pmin*ay,cxp+pmax*ax,cyp+pmax*ay};ends.Add(new double[]{cxp+pmin*ax,cyp+pmin*ay,-ax,-ay,k});ends.Add(new double[]{cxp+pmax*ax,cyp+pmax*ay,ax,ay,k});continue;}
   foreach(var path in pc){for(int e=0;e<2;e++){if(e==1&&path.Count<2)continue;int end=e==0?path[0]:path[path.Count-1];int nb=0;int ex=end%gw,ey=end/gw;for(int yy=-1;yy<=1;yy++)for(int xx=-1;xx<=1;xx++)if((xx!=0||yy!=0)&&cells.Contains((ey+yy)*gw+ex+xx))nb++;if(nb>1)continue;
     double ddx=double.NaN,ddy=double.NaN;if(path.Count>=4){int back=Math.Min(6,path.Count-1);int b=e==0?path[back]:path[path.Count-1-back];double vx=ex-b%gw,vy=ey-b/gw,L=Math.Sqrt(vx*vx+vy*vy);if(L>0){ddx=vx/L;ddy=vy/L;}}
     ends.Add(new double[]{ex,ey,ddx,ddy,k});}}}
  /* link each end to the next piece in line with it (the gaps of the dashed lines, up to about 1.5 km) */
  var parent=Enumerable.Range(0,pieces.Count).ToArray();Func<int,int> root=null;root=q=>{while(parent[q]!=q){parent[q]=parent[parent[q]];q=parent[q];}return q;};
  double reach=2.0/cellKm,reachLong=5.0/cellKm;var cand=new List<double[]>();var hash=new Dictionary<long,List<int>>();int hs=Math.Max(1,(int)Math.Ceiling(reachLong));
  /* the length of each piece (cells): a longer jump (up to 5 km, e.g. across a map label on the line) is taken only between pieces of
     at least 1 km lying in line with each other (within 15 degrees) */
  var pieceLen=new double[pieces.Count];for(int k=0;k<pieces.Count;k++){var cs=pieces[k].SelectMany(q=>q).ToList();pieceLen[k]=Math.Max(cs.Max(q=>q%gw)-cs.Min(q=>q%gw),cs.Max(q=>q/gw)-cs.Min(q=>q/gw));}
  for(int e=0;e<ends.Count;e++){long key=((long)((int)ends[e][0]/hs)<<32)|(uint)((int)ends[e][1]/hs);List<int> l;if(!hash.TryGetValue(key,out l))hash[key]=l=new List<int>();l.Add(e);}
  Func<double,double,double,double,double> angle=(ax,ay,bx,by)=>double.IsNaN(ax)?25:Math.Acos(Math.Max(-1,Math.Min(1,ax*bx+ay*by)))*180/Math.PI;
  for(int e=0;e<ends.Count;e++){int cx=(int)ends[e][0]/hs,cy=(int)ends[e][1]/hs;for(int yy=-1;yy<=1;yy++)for(int xx=-1;xx<=1;xx++){List<int> l;if(!hash.TryGetValue(((long)(cx+xx)<<32)|(uint)(cy+yy),out l))continue;
    foreach(var g in l){if(g<=e||ends[g][4]==ends[e][4])continue;double vx=ends[g][0]-ends[e][0],vy=ends[g][1]-ends[e][1],d=Math.Sqrt(vx*vx+vy*vy);if(d>reachLong||d<1e-9)continue;bool far=d>reach;vx/=d;vy/=d;
     double ae=angle(ends[e][2],ends[e][3],vx,vy),ag=angle(ends[g][2],ends[g][3],-vx,-vy);if(far&&(double.IsNaN(ends[e][2])||double.IsNaN(ends[g][2])||ae>15||ag>15||pieceLen[(int)ends[e][4]]*cellKm<1||pieceLen[(int)ends[g][4]]*cellKm<1))continue;if(ae>35||ag>35||(double.IsNaN(ends[e][2])&&double.IsNaN(ends[g][2])&&d>reach/2))continue;cand.Add(new double[]{d*(1+(ae+ag)/30),e,g});}}}
  var linked=new bool[ends.Count];var links=new List<double[]>();
  foreach(var c in cand.OrderBy(c=>c[0])){int e=(int)c[1],g=(int)c[2];if(linked[e]||linked[g])continue;linked[e]=linked[g]=true;parent[root((int)ends[e][4])]=root((int)ends[g][4]);links.Add(new[]{ends[e][0],ends[e][1],ends[g][0],ends[g][1],ends[e][4]});}
  /* the fault lines: linked pieces with their links */
  Func<double,double,double[]> geo=(x,y)=>new[]{W+(x*f+f/2.0)*dx,N-(y*f+f/2.0)*dy};
  var byRoot=new Dictionary<int,List<FaultTrace>>();Func<int,List<FaultTrace>> of=q=>{List<FaultTrace> l;int rt=root(q);if(!byRoot.TryGetValue(rt,out l))byRoot[rt]=l=new List<FaultTrace>();return l;};
  for(int k=0;k<pieces.Count;k++){double[] ds;if(dashSeg.TryGetValue(k,out ds)){var tr=new FaultTrace{Catalog="KMZ",SlipType=""};tr.Points.Add(geo(ds[0],ds[1]));tr.Points.Add(geo(ds[2],ds[3]));if(tr.LengthKm>0)of(k).Add(tr);continue;}foreach(var path in pieces[k]){if(path.Count<2)continue;var simple=Simplify(path.Select(p=>new double[]{p%gw,p/gw}).ToList(),1.0);var tr=new FaultTrace{Catalog="KMZ",SlipType=""};foreach(var c in simple)tr.Points.Add(geo(c[0],c[1]));of(k).Add(tr);}}
  foreach(var l in links){var tr=new FaultTrace{Catalog="KMZ",SlipType=""};tr.Points.Add(geo(l[0],l[1]));tr.Points.Add(geo(l[2],l[3]));of((int)l[4]).Add(tr);}
  var groups=new List<List<FaultTrace>>();int n=0;
  foreach(var traces in byRoot.Values){double len=traces.Sum(t=>t.LengthKm);if(len<1.0)continue;
   double lo0=traces.Min(t=>t.Points.Min(p=>p[0])),lo1=traces.Max(t=>t.Points.Max(p=>p[0])),la0=traces.Min(t=>t.Points.Min(p=>p[1])),la1=traces.Max(t=>t.Points.Max(p=>p[1]));
   int minX=(int)((lo0-W)/dx/f),maxX=(int)((lo1-W)/dx/f),minY=(int)((N-la1)/dy/f),maxY=(int)((N-la0)/dy/f);
   /* the legend samples: short, exactly horizontal */if(maxY-minY<=2&&len<30)continue;
   /* filled symbols (compact, far more red than a line of this length) */double spanKm=Math.Max(maxX-minX,maxY-minY)*cellKm;if(spanKm<12){int red=0;for(int y=Math.Max(0,minY-2);y<=Math.Min(gh-1,maxY+2);y++)for(int x=Math.Max(0,minX-2);x<=Math.Min(gw-1,maxX+2);x++)red+=count[y*gw+x];if(red>len/pxKm*12)continue;}
   groups.Add(traces);}
  /* numbered north to south */foreach(var g in groups.OrderByDescending(g=>g.Max(t=>t.Points.Max(p=>p[1])))){n++;foreach(var t in g){t.Name="PHIVOLCS map fault line "+n;result.Add(t);}}
  return result;}
 /* square dilation (grow) or erosion, separable */
 static byte[] Morph(byte[] m,int w,int h,int r,bool grow){var a=new byte[m.Length];var b=new byte[m.Length];var pre=new int[Math.Max(w,h)+1];
  for(int y=0;y<h;y++){int o=y*w;for(int x=0;x<w;x++)pre[x+1]=pre[x]+m[o+x];for(int x=0;x<w;x++){int lo=Math.Max(0,x-r),hi=Math.Min(w-1,x+r);int c=pre[hi+1]-pre[lo];a[o+x]=(byte)(grow?(c>0?1:0):(c==hi-lo+1?1:0));}}
  for(int x=0;x<w;x++){for(int y=0;y<h;y++)pre[y+1]=pre[y]+a[y*w+x];for(int y=0;y<h;y++){int lo=Math.Max(0,y-r),hi=Math.Min(h-1,y+r);int c=pre[hi+1]-pre[lo];b[y*w+x]=(byte)(grow?(c>0?1:0):(c==hi-lo+1?1:0));}}
  return b;}
 /* Zhang-Suen thinning */
 static void Thin(byte[] m,int w,int h){var cand=new List<int>();for(int i=0;i<m.Length;i++){int x=i%w,y=i/w;if(m[i]!=0){if(x==0||y==0||x==w-1||y==h-1)m[i]=0;else cand.Add(i);}}
  var del=new List<int>();bool changed=true;int[] nb=new int[8];
  while(changed){changed=false;for(int step=0;step<2;step++){del.Clear();foreach(var i in cand){if(m[i]==0)continue;nb[0]=m[i-w];nb[1]=m[i-w+1];nb[2]=m[i+1];nb[3]=m[i+w+1];nb[4]=m[i+w];nb[5]=m[i+w-1];nb[6]=m[i-1];nb[7]=m[i-w-1];
     int B=0,A=0;for(int k=0;k<8;k++){B+=nb[k];if(nb[k]==0&&nb[(k+1)%8]!=0)A++;}if(B<2||B>6||A!=1)continue;
     if(step==0){if(nb[0]*nb[2]*nb[4]!=0||nb[2]*nb[4]*nb[6]!=0)continue;}else{if(nb[0]*nb[2]*nb[6]!=0||nb[0]*nb[4]*nb[6]!=0)continue;}del.Add(i);}
    foreach(var i in del)m[i]=0;if(del.Count>0)changed=true;}cand=cand.Where(i=>m[i]!=0).ToList();}}
 /* the skeleton of one line followed into paths: from the ends first, then whatever is left (loops) */
 static List<List<int>> Paths(HashSet<int> set,int w,int h){var used=new HashSet<int>();var paths=new List<List<int>>();
  Func<int,IEnumerable<int>> nbs=p=>{int px=p%w,py=p/w;var l=new List<int>();foreach(var d in new[]{new[]{1,0},new[]{-1,0},new[]{0,1},new[]{0,-1},new[]{1,1},new[]{-1,1},new[]{1,-1},new[]{-1,-1}}){int qx=px+d[0],qy=py+d[1];if(qx<0||qy<0||qx>=w||qy>=h)continue;int q=qy*w+qx;if(set.Contains(q))l.Add(q);}return l;};
  Action<int> walk=start=>{var path=new List<int>();int cur=start;/* join to an already followed neighbour so branches stay connected */foreach(var q in nbs(start))if(used.Contains(q)){path.Add(q);break;}
   while(true){used.Add(cur);path.Add(cur);int next=-1;foreach(var q in nbs(cur))if(!used.Contains(q)){next=q;break;}if(next<0)break;cur=next;}if(path.Count>=2)paths.Add(path);};
  foreach(var p in set.Where(p=>nbs(p).Count()==1).ToList())if(!used.Contains(p))walk(p);
  foreach(var p in set.ToList())if(!used.Contains(p))walk(p);
  return paths;}
 /* Douglas-Peucker */
 static List<double[]> Simplify(List<double[]> pts,double tol){if(pts.Count<3)return pts;var keep=new bool[pts.Count];keep[0]=keep[pts.Count-1]=true;var st=new Stack<int[]>();st.Push(new[]{0,pts.Count-1});
  while(st.Count>0){var s=st.Pop();int a=s[0],b=s[1];double ax=pts[a][0],ay=pts[a][1],bx=pts[b][0],by=pts[b][1],L=Math.Sqrt((bx-ax)*(bx-ax)+(by-ay)*(by-ay));double dmax=0;int imax=-1;
   for(int i=a+1;i<b;i++){double d=L<1e-9?Math.Sqrt((pts[i][0]-ax)*(pts[i][0]-ax)+(pts[i][1]-ay)*(pts[i][1]-ay)):Math.Abs((bx-ax)*(ay-pts[i][1])-(ax-pts[i][0])*(by-ay))/L;if(d>dmax){dmax=d;imax=i;}}
   if(imax>0&&dmax>tol){keep[imax]=true;st.Push(new[]{a,imax});st.Push(new[]{imax,b});}}
  var list=new List<double[]>();for(int i=0;i<pts.Count;i++)if(keep[i])list.Add(pts[i]);return list;}
 internal static RectangleF Bounds(List<KeyValuePair<RectangleF,Image>> tiles){if(tiles==null||tiles.Count==0)return RectangleF.Empty;float w=tiles.Min(t=>t.Key.Left),e=tiles.Max(t=>t.Key.Right),n=tiles.Max(t=>Math.Max(t.Key.Top,t.Key.Bottom)),s=tiles.Min(t=>Math.Min(t.Key.Top,t.Key.Bottom));return RectangleF.FromLTRB(w,s,e,n);}
}
}
