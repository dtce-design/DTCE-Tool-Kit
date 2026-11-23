using System;using System.Collections.Generic;using System.Globalization;using System.Linq;
namespace BeamScheduleFormatter {
/* The PHIVOLCS fault lines joined into faults and measured along their main trace (owner, 22 November 2026: the Wells and Coppersmith
   rupture length on the Philippine fault lines). The lines are mapped in pieces, so:
   - pieces whose ends meet (within 0.15 km) are joined;
   - pieces in line with each other (ends pointing at each other within 30 degrees) are joined across gaps up to 2 km (dashed /
     approximate traces), and pieces of at least 1 km within 15 degrees across gaps up to 5 km (fault steps that a rupture can cross);
   - L of a fault is its main trace: the longest route through the joined pieces (side branches and parallel lines are not added, as
     the Wells and Coppersmith rupture length is one continuous rupture). The other pieces stay as branches: they count for the
     distance to the site and can be picked on the map, but not for L. */
static class PhivolcsFaultNetwork {
 const double SnapKm=0.15,GapKm=2.0,LongKm=5.0,LongMinKm=1.0;
 internal static List<FaultTrace> Build(List<FaultTrace> lines){
  int n=lines.Count;if(n==0)return lines;var len=lines.Select(t=>t.LengthKm).ToArray();
  /* graph: node 2i / 2i+1 = start / end of line i */
  var adj=new List<KeyValuePair<int,double>>[2*n];for(int i=0;i<2*n;i++)adj[i]=new List<KeyValuePair<int,double>>();
  Action<int,int,double> edge=(a,b,w)=>{adj[a].Add(new KeyValuePair<int,double>(b,w));adj[b].Add(new KeyValuePair<int,double>(a,w));};
  for(int i=0;i<n;i++)edge(2*i,2*i+1,len[i]);
  var pt=new double[2*n][];var dir=new double[2*n][];
  for(int i=0;i<n;i++){var p=lines[i].Points;pt[2*i]=p[0];pt[2*i+1]=p[p.Count-1];dir[2*i]=Out(p,true);dir[2*i+1]=Out(p,false);}
  var grid=new Dictionary<long,List<int>>();const double cell=0.05;Func<double,double,long> key=(x,y)=>((long)Math.Floor(x/cell)<<32)^(uint)(int)Math.Floor(y/cell);
  for(int k=0;k<2*n;k++){long g=key(pt[k][0],pt[k][1]);List<int> l;if(!grid.TryGetValue(g,out l))grid[g]=l=new List<int>();l.Add(k);}
  var links=new Dictionary<long,double[]>();/* gap links: key a*2n+b -> the two points */
  for(int a=0;a<2*n;a++){int gx=(int)Math.Floor(pt[a][0]/cell),gy=(int)Math.Floor(pt[a][1]/cell);
   for(int xx=-1;xx<=1;xx++)for(int yy=-1;yy<=1;yy++){List<int> l;if(!grid.TryGetValue(((long)(gx+xx)<<32)^(uint)(gy+yy),out l))continue;
    foreach(int b in l){if(b<=a||b/2==a/2)continue;double d=FaultTraces.Km(pt[a][1],pt[a][0],pt[b][1],pt[b][0]);if(d>LongKm)continue;
     if(d<=SnapKm){edge(a,b,d);continue;}
     double kx=Math.Cos(pt[a][1]*Math.PI/180),vx=(pt[b][0]-pt[a][0])*kx,vy=pt[b][1]-pt[a][1],vl=Math.Sqrt(vx*vx+vy*vy);vx/=vl;vy/=vl;
     double ca=dir[a][0]*vx+dir[a][1]*vy,cb=-(dir[b][0]*vx+dir[b][1]*vy);double lim=d<=GapKm?Math.Cos(30*Math.PI/180):Math.Cos(15*Math.PI/180);
     if(ca<lim||cb<lim)continue;if(d>GapKm&&(len[a/2]<LongMinKm||len[b/2]<LongMinKm))continue;
     edge(a,b,d);links[(long)a*2*n+b]=new[]{pt[a][0],pt[a][1],pt[b][0],pt[b][1]};}}}
  /* faults (connected parts) and their main trace */
  var comp=new int[2*n];for(int i=0;i<2*n;i++)comp[i]=-1;int nc=0;var members=new List<List<int>>();
  for(int s=0;s<2*n;s++){if(comp[s]>=0)continue;var list=new List<int>();var st=new Stack<int>();st.Push(s);comp[s]=nc;while(st.Count>0){int u=st.Pop();list.Add(u);foreach(var e in adj[u])if(comp[e.Key]<0){comp[e.Key]=nc;st.Push(e.Key);}}members.Add(list);nc++;}
  var result=new List<FaultTrace>();var order=Enumerable.Range(0,nc).OrderByDescending(c=>members[c].Max(u=>pt[u][1])).ToList();int number=0;
  foreach(int c in order){number++;string name="PHIVOLCS fault line "+number.ToString(CultureInfo.InvariantCulture);var nodes=members[c];
   int far1=Farthest(adj,nodes[0],null);var prev=new Dictionary<int,int>();int far2=Farthest(adj,far1,prev);
   var onPath=new HashSet<int>();var pathLinks=new List<double[]>();for(int u=far2;prev.ContainsKey(u);u=prev[u]){int v=prev[u];if(u/2==v/2)onPath.Add(u/2);else{int a=Math.Min(u,v),b=Math.Max(u,v);double[] lk;if(links.TryGetValue((long)a*2*n+b,out lk))pathLinks.Add(lk);}}
   foreach(int i in nodes.Where(u=>u%2==0).Select(u=>u/2)){var t=lines[i];t.Name=name;t.Branch=!onPath.Contains(i);result.Add(t);}
   foreach(var lk in pathLinks){var first=lines[nodes[0]/2];result.Add(new FaultTrace{Name=name,Catalog=first.Catalog,SlipType="",Category=first.Category,TraceType="gap",Points=new List<double[]>{new[]{lk[0],lk[1]},new[]{lk[2],lk[3]}}});}}
  return result;}
 /* the outward direction at an end of a line (from about 0.5 km inside), as a unit vector in local km */
 static double[] Out(List<double[]> p,bool start){var e=start?p[0]:p[p.Count-1];double kx=Math.Cos(e[1]*Math.PI/180);double[] q=null;double acc=0;
  for(int k=1;k<p.Count;k++){var a=start?p[k-1]:p[p.Count-k];var b=start?p[k]:p[p.Count-1-k];acc+=FaultTraces.Km(a[1],a[0],b[1],b[0]);q=b;if(acc>=0.5)break;}
  double vx=(e[0]-q[0])*kx,vy=e[1]-q[1],l=Math.Sqrt(vx*vx+vy*vy);return l>0?new[]{vx/l,vy/l}:new[]{0.0,0.0};}
 /* Dijkstra from s; the farthest node (and the route back, when prev is given) */
 static int Farthest(List<KeyValuePair<int,double>>[] adj,int s,Dictionary<int,int> prev){var dist=new Dictionary<int,double>{{s,0}};var heap=new SortedSet<Tuple<double,int>>{Tuple.Create(0.0,s)};
  while(heap.Count>0){var top=heap.Min;heap.Remove(top);double d=top.Item1;int u=top.Item2;if(d>dist[u])continue;
   foreach(var e in adj[u]){double nd=d+e.Value;double old;if(!dist.TryGetValue(e.Key,out old)||nd<old-1e-12){if(dist.ContainsKey(e.Key))heap.Remove(Tuple.Create(old,e.Key));dist[e.Key]=nd;if(prev!=null)prev[e.Key]=u;heap.Add(Tuple.Create(nd,e.Key));}}}
  int best=s;double bd=-1;foreach(var kv in dist)if(kv.Value>bd){bd=kv.Value;best=kv.Key;}return best;}
}
}
