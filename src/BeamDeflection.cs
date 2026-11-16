using System;using System.Linq;using System.Collections.Generic;using System.Globalization;
namespace BeamScheduleFormatter {
// Beam Deflection Check engine. Geometry is stored in metres and translations in metres; reports convert to mm.
class BeamNode {public int Id;public double X,Y,Z;}
class BeamMember {public int Id,A,B;}
class BeamDisplacement {public int Node;public string Case;public double X,Y,Z;}
class BeamDeflectionData {
 public bool ZUp;public string GeometryUnitSource="",DisplacementUnitSource="";
 public readonly Dictionary<int,BeamNode> Nodes=new Dictionary<int,BeamNode>();
 public readonly Dictionary<int,BeamMember> Members=new Dictionary<int,BeamMember>();
 public readonly List<BeamDisplacement> Displacements=new List<BeamDisplacement>();
 public IEnumerable<string> Cases(){return Displacements.Select(d=>d.Case).Distinct(StringComparer.OrdinalIgnoreCase);}
}
class PhysicalBeamInput {public string Name,Members,Cases,SpanType="Between supports";public int Start,End;public double LimitRatio=360;public double AbsoluteLimitMm;public string Vertical="Y";public bool ServiceBasis,SupportsVerified;}
class BeamDeflectionStation {public int Node;public double Station,Absolute,Chord,Relative;}
class BeamDeflectionResult {
 public string Beam,Case,Status,SpanType,Members,Vertical;public bool Exceeds,Qualified;public double Span,LimitRatio,LimitMm,AbsoluteLimitMm,MaximumMm,SignedMaximumMm,At,LargestSegment;public int GoverningNode,StartNode,EndNode;
 public readonly List<BeamDeflectionStation> Stations=new List<BeamDeflectionStation>();
 public double Utilization{get{return LimitMm>0?MaximumMm/LimitMm:0;}}
 public string Ratio{get{return MaximumMm<=1e-9?"L/∞":"L/"+Math.Floor(Span*1000/MaximumMm).ToString("0",CultureInfo.InvariantCulture);}}
}
static class BeamDeflection {
 internal const string Cantilever="Cantilever (fixed at Support A)";
 internal static readonly string[] SpanTypes={"Between supports",Cantilever};
 internal static double Distance(BeamNode a,BeamNode b){return Math.Sqrt(Math.Pow(a.X-b.X,2)+Math.Pow(a.Y-b.Y,2)+Math.Pow(a.Z-b.Z,2));}
 static bool Finite(double x){return !double.IsNaN(x)&&!double.IsInfinity(x);}
 internal static List<int> MemberIds(string text){var ids=new List<int>();foreach(string token in (text??"").Split(new[]{',',';',' ','\t'},StringSplitOptions.RemoveEmptyEntries)){var range=token.Split(new[]{'-'},StringSplitOptions.RemoveEmptyEntries);int a,b;if(range.Length==2&&int.TryParse(range[0],out a)&&int.TryParse(range[1],out b)&&a>0&&b>=a&&b-a<10000){for(int i=a;i<=b;i++)if(ids.Contains(i))throw new Exception("Member "+i+" is listed twice.");else ids.Add(i);continue;}int id;if(!int.TryParse(token,out id)||id<=0)throw new Exception("Member IDs must be positive integers (for example 12, 13, 14 or 12-14).");if(ids.Contains(id))throw new Exception("Member "+id+" is listed twice.");ids.Add(id);}if(ids.Count==0)throw new Exception("Enter the STAAD member IDs that form this physical beam.");return ids;}
 // Orders the selected members into one straight, open chain of nodes from Support A to Support B.
 internal static List<BeamNode> Chain(BeamDeflectionData data,PhysicalBeamInput input){
  var ids=MemberIds(input.Members);var edges=new Dictionary<int,List<int>>();
  foreach(int id in ids){BeamMember m;if(!data.Members.TryGetValue(id,out m))throw new Exception("Member "+id+" is not in the imported member incidences.");if(!data.Nodes.ContainsKey(m.A)||!data.Nodes.ContainsKey(m.B))throw new Exception("Member "+id+" refers to a node without coordinates.");if(m.A==m.B)throw new Exception("Member "+id+" starts and ends at the same node.");
   foreach(var pair in new[]{new[]{m.A,m.B},new[]{m.B,m.A}}){List<int> list;if(!edges.TryGetValue(pair[0],out list)){list=new List<int>();edges.Add(pair[0],list);}list.Add(pair[1]);if(list.Count>2)throw new Exception("Node "+pair[0]+" joins more than two selected members; select one straight beam line.");}}
  var ends=edges.Where(p=>p.Value.Count==1).Select(p=>p.Key).ToList();if(ends.Count!=2)throw new Exception("The selected members must form one open connected line with two end nodes.");
  int start=input.Start>0?input.Start:ends.Min();if(!ends.Contains(start))throw new Exception("Support A (node "+start+") is not an end node of the selected members. Split the beam at real supports.");int end=input.End>0?input.End:ends.Single(n=>n!=start);if(!ends.Contains(end)||start==end)throw new Exception("Support B (node "+end+") must be the other end node of the selected members.");
  var path=new List<BeamNode>();var seen=new HashSet<int>();int current=start,previous=-1;while(true){if(!seen.Add(current))throw new Exception("The selected members form a loop.");path.Add(data.Nodes[current]);if(current==end)break;int[] next=edges[current].Where(n=>n!=previous).ToArray();if(next.Length!=1)throw new Exception("The selected members are disconnected or ambiguous.");previous=current;current=next[0];}
  if(path.Count!=ids.Count+1||seen.Count!=edges.Count)throw new Exception("The selected members contain a disconnected segment.");
  if(path.Any(n=>!Finite(n.X)||!Finite(n.Y)||!Finite(n.Z)))throw new Exception("Non-finite node coordinates.");
  double length=Distance(path[0],path.Last());if(length<=1e-9)throw new Exception("Zero beam span.");double tolerance=Math.Max(.001,length*1e-5);
  double sum=Enumerable.Range(1,path.Count-1).Sum(i=>Distance(path[i-1],path[i]));if(Math.Abs(sum-length)>tolerance)throw new Exception("Selected members are not straight/collinear or they overlap; this sheet checks one straight physical beam.");
  foreach(var node in path){double t=((node.X-path[0].X)*(path.Last().X-path[0].X)+(node.Y-path[0].Y)*(path.Last().Y-path[0].Y)+(node.Z-path[0].Z)*(path.Last().Z-path[0].Z))/(length*length);double x=path[0].X+t*(path.Last().X-path[0].X),y=path[0].Y+t*(path.Last().Y-path[0].Y),z=path[0].Z+t*(path.Last().Z-path[0].Z);if(Math.Sqrt(Math.Pow(node.X-x,2)+Math.Pow(node.Y-y,2)+Math.Pow(node.Z-z,2))>tolerance)throw new Exception("Node "+node.Id+" is off the straight line between the supports.");}
  return path;
 }
 internal static string CaseId(string name){return (name??"").Trim().Split(new[]{':',' ','\t'},StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()??"";}
 static double Component(BeamDisplacement d,string axis){return axis=="X"?d.X:axis=="Y"?d.Y:d.Z;}
 static double Along(BeamNode a,BeamNode b,string axis){return axis=="X"?b.X-a.X:axis=="Y"?b.Y-a.Y:b.Z-a.Z;}
 internal static List<BeamDeflectionResult> Calculate(BeamDeflectionData data,PhysicalBeamInput input){
  bool cantilever=input.SpanType==Cantilever;if(!SpanTypes.Contains(input.SpanType))throw new Exception("Select the span type.");
  if(!Finite(input.LimitRatio)||input.LimitRatio<=0)throw new Exception("Enter a positive deflection limit ratio n for L/n.");if(!Finite(input.AbsoluteLimitMm)||input.AbsoluteLimitMm<0)throw new Exception("Absolute limit must be zero (not used) or positive mm.");
  if(!new[]{"X","Y","Z"}.Contains(input.Vertical))throw new Exception("Select the global displacement direction.");
  var path=Chain(data,input);double span=Distance(path[0],path.Last());
  if(Math.Abs(Along(path[0],path.Last(),input.Vertical))/span>.999)throw new Exception("Global "+input.Vertical+" is along the beam axis; select a transverse (normally vertical) direction.");
  var requested=new List<string>((input.Cases??"").Split(new[]{',',';',' '},StringSplitOptions.RemoveEmptyEntries));var all=data.Cases().ToList();
  if(all.Count==0)throw new Exception("No node displacement results are loaded.");
  foreach(string r in requested)if(!all.Any(c=>CaseId(c).Equals(r,StringComparison.OrdinalIgnoreCase)||c.Equals(r,StringComparison.OrdinalIgnoreCase)))throw new Exception("Load case "+r+" is not in the displacement results.");
  var cases=all.Where(c=>requested.Count==0||requested.Any(r=>CaseId(c).Equals(r,StringComparison.OrdinalIgnoreCase)||c.Equals(r,StringComparison.OrdinalIgnoreCase))).ToList();
  double largest=Enumerable.Range(1,path.Count-1).Max(i=>Distance(path[i-1],path[i]));
  var results=new List<BeamDeflectionResult>();
  foreach(string name in cases){
   var rows=data.Displacements.Where(d=>d.Case.Equals(name,StringComparison.OrdinalIgnoreCase)).ToList();var byNode=new Dictionary<int,BeamDisplacement>();foreach(var row in rows){if(byNode.ContainsKey(row.Node))throw new Exception("Node "+row.Node+" appears twice in load case "+name+".");byNode.Add(row.Node,row);}
   var missing=path.Where(n=>!byNode.ContainsKey(n.Id)).Select(n=>n.Id.ToString(CultureInfo.InvariantCulture)).ToList();if(missing.Count>0)throw new Exception("Load case "+name+" has no displacement for node(s) "+string.Join(", ",missing)+". Export all nodes of the beam, including supports and intermediate nodes.");
   double first=Component(byNode[path[0].Id],input.Vertical),last=Component(byNode[path.Last().Id],input.Vertical);
   double limit=span*1000/input.LimitRatio;if(input.AbsoluteLimitMm>0)limit=Math.Min(limit,input.AbsoluteLimitMm);
   var result=new BeamDeflectionResult{Beam=input.Name,Case=name,Members=input.Members,SpanType=input.SpanType,Vertical=input.Vertical,StartNode=path[0].Id,EndNode=path.Last().Id,Span=span,LimitRatio=input.LimitRatio,AbsoluteLimitMm=input.AbsoluteLimitMm,LimitMm=limit,LargestSegment=largest};
   foreach(var node in path){double station=Distance(path[0],node),absolute=Component(byNode[node.Id],input.Vertical),chord=cantilever?first:first+(last-first)*station/span,relative=(absolute-chord)*1000;if(!Finite(relative))throw new Exception("Non-finite displacement at node "+node.Id+".");
    result.Stations.Add(new BeamDeflectionStation{Node=node.Id,Station=station,Absolute=absolute*1000,Chord=chord*1000,Relative=relative});if(Math.Abs(relative)>result.MaximumMm){result.MaximumMm=Math.Abs(relative);result.SignedMaximumMm=relative;result.At=station;result.GoverningNode=node.Id;}}
   result.Exceeds=result.MaximumMm>result.LimitMm+1e-9;int interior=path.Count-2;
   if(!cantilever&&interior<1){result.Status="NOT CHECKED - no intermediate node between the supports. Add analysis nodes in STAAD (for example at quarter points) and re-export.";}
   else if(result.Exceeds)result.Status="EXCEEDS LIMIT - "+N(result.MaximumMm,2)+" mm > "+N(limit,2)+" mm";
   else if(!input.ServiceBasis)result.Status="WITHIN LIMIT AT NODES - confirm the selected cases are service (unfactored) loads";
   else if(!input.SupportsVerified)result.Status="WITHIN LIMIT AT NODES - confirm Support A/B are the real supports";
   else{result.Qualified=true;result.Status="OK - "+N(result.MaximumMm,2)+" mm ≤ "+N(limit,2)+" mm at analysis nodes";}
   results.Add(result);
  }
  return results;
 }
 internal static string N(double value,int decimals){return value.ToString("F"+decimals,CultureInfo.InvariantCulture);}
 // Suggests straight beam lines between supports: horizontal collinear chains split where a column/post or a node support meets them.
 internal static List<KeyValuePair<string,int[]>> SuggestGroups(BeamDeflectionData data,string vertical){
  Func<BeamMember,double[]> dir=m=>{var a=data.Nodes[m.A];var b=data.Nodes[m.B];double dx=b.X-a.X,dy=b.Y-a.Y,dz=b.Z-a.Z,l=Math.Sqrt(dx*dx+dy*dy+dz*dz);return l<1e-9?null:new[]{dx/l,dy/l,dz/l};};
  int axis=vertical=="X"?0:vertical=="Y"?1:2;var valid=data.Members.Values.Where(m=>data.Nodes.ContainsKey(m.A)&&data.Nodes.ContainsKey(m.B)&&dir(m)!=null).ToList();
  var horizontal=valid.Where(m=>Math.Abs(dir(m)[axis])<.05).ToList();var upright=valid.Where(m=>Math.Abs(dir(m)[axis])>.7).ToList();
  var supportNodes=new HashSet<int>(upright.SelectMany(m=>new[]{m.A,m.B}));
  var byNode=new Dictionary<int,List<BeamMember>>();foreach(var m in horizontal)foreach(int n in new[]{m.A,m.B}){List<BeamMember> list;if(!byNode.TryGetValue(n,out list)){list=new List<BeamMember>();byNode.Add(n,list);}list.Add(m);}
  Func<BeamMember,BeamMember,bool> collinear=(a,b)=>{var u=dir(a);var v=dir(b);return Math.Abs(Math.Abs(u[0]*v[0]+u[1]*v[1]+u[2]*v[2])-1)<1e-6;};
  var used=new HashSet<int>();var groups=new List<KeyValuePair<string,int[]>>();
  foreach(var seed in horizontal.OrderBy(m=>m.Id)){if(used.Contains(seed.Id))continue;var chain=new LinkedList<BeamMember>();chain.AddFirst(seed);used.Add(seed.Id);
   foreach(bool forward in new[]{true,false}){int node=forward?seed.B:seed.A;var current=seed;while(!supportNodes.Contains(node)){List<BeamMember> list;if(!byNode.TryGetValue(node,out list))break;var next=list.Where(m=>m.Id!=current.Id&&!used.Contains(m.Id)&&collinear(m,current)).ToList();if(next.Count!=1)break;current=next[0];used.Add(current.Id);if(forward)chain.AddLast(current);else chain.AddFirst(current);node=current.A==node?current.B:current.A;}}
   if(chain.Count>=2)groups.Add(new KeyValuePair<string,int[]>("B"+(groups.Count+1),chain.Select(m=>m.Id).ToArray()));}
  return groups;
 }
}
}
