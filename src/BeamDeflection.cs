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
class PhysicalBeamInput {public string Name,Members,DeadCases,LiveCases,Criterion,IeBasis="",SpanType="Between supports";public int Start,End;public double RhoPrime,StiffnessFactor=1,Xi=2,CustomRatio;public string Vertical="Y";public bool ServiceBasis,SupportsVerified;}
class BeamDeflectionStation {public int Node;public double Station,Dead,Live,Relative;}
// One NSCP 2015 serviceability check of one physical beam.
class BeamDeflectionResult {
 public string Beam,Members,Criterion,Check,Formula,Clause,Status,SpanType,Vertical,DeadCases,LiveCases,IeBasis="";public bool Exceeds,Qualified;
 public double Span,LimitSpan,LimitRatio,LimitMm,MaximumMm,SignedMaximumMm,At,LargestSegment,DeadFactor,LiveFactor,Lambda,Xi,RhoPrime,StiffnessFactor;public int GoverningNode,StartNode,EndNode;
 public readonly List<BeamDeflectionStation> Stations=new List<BeamDeflectionStation>();
 public double Utilization{get{return LimitMm>0?MaximumMm/LimitMm:0;}}
 public string Ratio{get{return MaximumMm<=1e-9?"L/∞":"L/"+Math.Floor(LimitSpan*1000/MaximumMm).ToString("0",CultureInfo.InvariantCulture);}}
}
// Deflection criteria. Reinforced concrete: NSCP 2015 Table 424.2.2 with time-dependent deflection per Section 424.2.4.
// Steel and other members: NSCP 2015 Chapter 1 roof/floor member limits (live load and dead + live load).
class DeflectionCriterion {public string Name,Material,Clause,Row;public string[] Kinds;public double[] Ratios;}
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
 internal const string Custom="Custom limit L/n - sum of the listed dead and live cases";
 internal static readonly DeflectionCriterion[] Criteria={
  new DeflectionCriterion{Name="RC flat roof not supporting or attached to nonstructural elements likely to be damaged",Material="RC",Clause="NSCP 2015 Table 424.2.2",Row="Row 1: immediate deflection due to roof live load Lr (and rain/snow if any)",Kinds=new[]{"Live"},Ratios=new[]{180.0}},
  new DeflectionCriterion{Name="RC floor not supporting or attached to nonstructural elements likely to be damaged",Material="RC",Clause="NSCP 2015 Table 424.2.2",Row="Row 2: immediate deflection due to live load L",Kinds=new[]{"Live"},Ratios=new[]{360.0}},
  new DeflectionCriterion{Name="RC roof or floor supporting or attached to nonstructural elements likely to be damaged",Material="RC",Clause="NSCP 2015 Table 424.2.2",Row="Row 3: part of the total deflection after attachment of nonstructural elements",Kinds=new[]{"After"},Ratios=new[]{480.0}},
  new DeflectionCriterion{Name="RC roof or floor supporting or attached to nonstructural elements not likely to be damaged",Material="RC",Clause="NSCP 2015 Table 424.2.2",Row="Row 4: part of the total deflection after attachment of nonstructural elements",Kinds=new[]{"After"},Ratios=new[]{240.0}},
  new DeflectionCriterion{Name="Steel or other floor member",Material="Steel",Clause="NSCP 2015 Chapter 1 deflection limits",Row="Floor members: L/360 for live load, L/240 for dead + live load",Kinds=new[]{"Live","Total"},Ratios=new[]{360.0,240}},
  new DeflectionCriterion{Name="Steel or other roof member supporting plaster or stucco ceiling",Material="Steel",Clause="NSCP 2015 Chapter 1 deflection limits",Row="Roof members supporting plaster ceiling: L/360 live, L/240 dead + live",Kinds=new[]{"Live","Total"},Ratios=new[]{360.0,240}},
  new DeflectionCriterion{Name="Steel or other roof member supporting nonplaster ceiling",Material="Steel",Clause="NSCP 2015 Chapter 1 deflection limits",Row="Roof members supporting nonplaster ceiling: L/240 live, L/180 dead + live",Kinds=new[]{"Live","Total"},Ratios=new[]{240.0,180}},
  new DeflectionCriterion{Name="Steel or other roof member not supporting ceiling",Material="Steel",Clause="NSCP 2015 Chapter 1 deflection limits",Row="Roof members not supporting ceiling: L/180 live, L/120 dead + live",Kinds=new[]{"Live","Total"},Ratios=new[]{180.0,120}},
  new DeflectionCriterion{Name=Custom,Material="Custom",Clause="Project specification",Row="User-entered limit",Kinds=new[]{"Combined"},Ratios=new[]{0.0}}};
 internal static readonly string[] CriterionNames=Criteria.Select(c=>c.Name).ToArray();
 internal static DeflectionCriterion Find(string name){var c=Criteria.FirstOrDefault(x=>x.Name==name);if(c==null)throw new Exception("Select a deflection criterion.");return c;}
 // Time-dependent factor ξ (NSCP 2015 Table 424.2.4.1.3).
 internal static readonly string[] Durations={"5 years or more (ξ = 2.0)","12 months (ξ = 1.4)","6 months (ξ = 1.2)","3 months (ξ = 1.0)"};
 internal static double XiFor(string duration){int i=Array.IndexOf(Durations,duration);if(i<0)throw new Exception("Select the sustained load duration.");return new[]{2.0,1.4,1.2,1.0}[i];}
 internal static double Lambda(double xi,double rhoPrime){return xi/(1+50*rhoPrime);}
 static List<string> CaseList(string text){return (text??"").Split(new[]{',',';',' ','+'},StringSplitOptions.RemoveEmptyEntries).Select(t=>t.Trim()).Where(t=>t!="").ToList();}
 static string Match(List<string> all,string request){var found=all.FirstOrDefault(c=>c.Equals(request,StringComparison.OrdinalIgnoreCase))??all.FirstOrDefault(c=>CaseId(c).Equals(request,StringComparison.OrdinalIgnoreCase));if(found==null)throw new Exception("Load case "+request+" is not in the displacement results.");return found;}
 // Support-chord relative displacement (m) at each path node, summed over the listed cases (linear superposition).
 static double[] Relative(BeamDeflectionData data,List<BeamNode> path,IEnumerable<string> cases,string axis,bool cantilever){double span=Distance(path[0],path.Last());var sum=new double[path.Count];
  foreach(string name in cases){var byNode=new Dictionary<int,BeamDisplacement>();foreach(var row in data.Displacements.Where(d=>d.Case.Equals(name,StringComparison.OrdinalIgnoreCase))){if(byNode.ContainsKey(row.Node))throw new Exception("Node "+row.Node+" appears twice in load case "+name+".");byNode.Add(row.Node,row);}
   var missing=path.Where(n=>!byNode.ContainsKey(n.Id)).Select(n=>n.Id.ToString(CultureInfo.InvariantCulture)).ToList();if(missing.Count>0)throw new Exception("Load case "+name+" has no displacement for node(s) "+string.Join(", ",missing)+". Export all nodes of the beam, including supports and intermediate nodes.");
   double first=Component(byNode[path[0].Id],axis),last=Component(byNode[path.Last().Id],axis);
   for(int i=0;i<path.Count;i++){double x=Distance(path[0],path[i]),chord=cantilever?first:first+(last-first)*x/span,value=Component(byNode[path[i].Id],axis)-chord;if(!Finite(value))throw new Exception("Non-finite displacement at node "+path[i].Id+".");sum[i]+=value;}}
  return sum;}
 internal static List<BeamDeflectionResult> Calculate(BeamDeflectionData data,PhysicalBeamInput input){
  var criterion=Find(input.Criterion);bool cantilever=input.SpanType==Cantilever;if(!SpanTypes.Contains(input.SpanType))throw new Exception("Select the span type.");
  if(!new[]{"X","Y","Z"}.Contains(input.Vertical))throw new Exception("Select the global displacement direction.");
  bool rc=criterion.Material=="RC";
  if(rc){if(!Finite(input.RhoPrime)||input.RhoPrime<0||input.RhoPrime>.08)throw new Exception("ρ' (compression steel ratio As'/bd) must be between 0 and 0.08.");if(!Finite(input.StiffnessFactor)||input.StiffnessFactor<1||input.StiffnessFactor>20)throw new Exception("Ig/Ie must be at least 1.0 (1.0 = STAAD stiffness already effective/cracked).");if(!new[]{2.0,1.4,1.2,1.0}.Contains(input.Xi))throw new Exception("Select the sustained load duration ξ.");}
  if(criterion.Material=="Custom"&&(!Finite(input.CustomRatio)||input.CustomRatio<=0))throw new Exception("Enter n for the custom L/n limit.");
  var path=Chain(data,input);double span=Distance(path[0],path.Last());
  if(Math.Abs(Along(path[0],path.Last(),input.Vertical))/span>.999)throw new Exception("Global "+input.Vertical+" is along the beam axis; select a transverse (normally vertical) direction.");
  var all=data.Cases().ToList();if(all.Count==0)throw new Exception("No node displacement results are loaded.");
  var dead=CaseList(input.DeadCases).Select(c=>Match(all,c)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();var live=CaseList(input.LiveCases).Select(c=>Match(all,c)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
  if(dead.Intersect(live,StringComparer.OrdinalIgnoreCase).Any())throw new Exception("A load case is listed as both dead and live.");
  if(criterion.Kinds.Any(kd=>kd!="Combined")&&live.Count==0)throw new Exception("Enter the live load case(s) (service, unfactored).");
  if(criterion.Kinds.Any(kd=>kd=="After"||kd=="Total")&&dead.Count==0)throw new Exception("Enter the dead / sustained load case(s) (service, unfactored).");
  if(criterion.Material=="Custom"&&dead.Count+live.Count==0)throw new Exception("Enter at least one load case.");
  var relD=Relative(data,path,dead,input.Vertical,cantilever);var relL=Relative(data,path,live,input.Vertical,cantilever);
  double largest=Enumerable.Range(1,path.Count-1).Max(i=>Distance(path[i-1],path[i]));double k=rc?input.StiffnessFactor:1,lambda=rc?Lambda(input.Xi,input.RhoPrime):0;
  // Steel/other cantilevers: L is taken as twice the cantilever length.
  double limitSpan=cantilever&&criterion.Material=="Steel"?2*span:span;
  var results=new List<BeamDeflectionResult>();
  for(int c=0;c<criterion.Kinds.Length;c++){string kind=criterion.Kinds[c];double n=criterion.Material=="Custom"?input.CustomRatio:criterion.Ratios[c];double fd,fl;string check,formula;
   if(kind=="Live"){fd=0;fl=k;check=rc?"Immediate live-load deflection":"Live-load deflection";formula=rc?"δ = (Ig/Ie)·δL = "+N(k,2)+"·δL":"δ = δL";}
   else if(kind=="After"){fd=k*lambda;fl=k;check="Deflection after attachment of nonstructural elements";formula="δ = λΔ·δD,i + δL,i = "+N(lambda,3)+"·("+N(k,2)+"·δD) + "+N(k,2)+"·δL";}
   else if(kind=="Total"){fd=1;fl=1;check="Dead + live load deflection";formula="δ = δD + δL";}
   else{fd=1;fl=1;check="Listed cases combined";formula="δ = Σ δ(listed cases)";}
   var r=new BeamDeflectionResult{Beam=input.Name,Members=input.Members,Criterion=criterion.Name,Clause=criterion.Clause+" - "+criterion.Row,Check=check,Formula=formula,SpanType=input.SpanType,Vertical=input.Vertical,DeadCases=string.Join(", ",dead),LiveCases=string.Join(", ",live),StartNode=path[0].Id,EndNode=path.Last().Id,Span=span,LimitSpan=limitSpan,LimitRatio=n,LimitMm=limitSpan*1000/n,LargestSegment=largest,DeadFactor=fd,LiveFactor=fl,Lambda=lambda,Xi=input.Xi,RhoPrime=input.RhoPrime,StiffnessFactor=k,IeBasis=rc?(input.IeBasis??""):""};
   for(int i=0;i<path.Count;i++){double value=(fd*relD[i]+fl*relL[i])*1000;r.Stations.Add(new BeamDeflectionStation{Node=path[i].Id,Station=Distance(path[0],path[i]),Dead=relD[i]*1000,Live=relL[i]*1000,Relative=value});if(Math.Abs(value)>r.MaximumMm){r.MaximumMm=Math.Abs(value);r.SignedMaximumMm=value;r.At=r.Stations[i].Station;r.GoverningNode=path[i].Id;}}
   if(r.GoverningNode==0){r.GoverningNode=path[0].Id;}
   r.Exceeds=r.MaximumMm>r.LimitMm+1e-9;
   if(!cantilever&&path.Count<3)r.Status="NOT CHECKED - no intermediate node between the supports. Add analysis nodes in STAAD (for example at quarter points) and re-export.";
   else if(r.Exceeds)r.Status="EXCEEDS - "+N(r.MaximumMm,2)+" mm > L/"+N(n,0)+" = "+N(r.LimitMm,2)+" mm";
   else if(!input.ServiceBasis)r.Status="REVIEW - within limit; confirm the cases are unfactored service loads";
   else if(!input.SupportsVerified)r.Status="REVIEW - within limit; confirm Support A/B are the real supports";
   else{r.Qualified=true;r.Status="OK - "+N(r.MaximumMm,2)+" mm ≤ L/"+N(n,0)+" = "+N(r.LimitMm,2)+" mm";}
   results.Add(r);}
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
