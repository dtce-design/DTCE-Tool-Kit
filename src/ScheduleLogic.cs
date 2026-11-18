using System;using System.Collections.Generic;using System.Globalization;using System.Linq;
namespace BeamScheduleFormatter {
// Realistic construction logic (owner, 18 November 2026: "gusto ko yung makatotohanan na construction schedule at
// manpower"). Instead of running every item of a phase at the same time, each pay item is given a trade (clearing,
// excavation, lean concrete, rebar, formworks, footing concrete, frame concrete, backfill, masonry, steel, roofing,
// plaster, ceiling, tiles, doors, painting ...) and linked to the trades it depends on with the usual precedence types:
//   FS lag  - starts lag days after the predecessor finishes (e.g. trusses 7 days after the frame is poured: curing)
//   SS lag  - starts lag days after the predecessor starts (overlap, e.g. formworks once part of the rebar is done)
//   FF lag  - finishes at least lag days after the predecessor finishes (e.g. frame pours end after the last forms)
// SS lags are a share of the predecessor's duration. Then the critical path method gives ES / EF / LS / LF / total float
// (ES of the first activity = 0), and the manpower is levelled: in the order of the network each non-critical item is
// started, within its float, on the day that keeps the daily labour lowest, so the project duration does not change.
// Plumbing and electrical rough-in come before plaster, ceiling and tiles; lean concrete, footing pours, roof insulation
// and roofing accessories are not moved by the levelling (they go with the trade before them).
sealed class ScheduleLink {internal PdmNode From,To;internal string Type;internal int Lag;
 internal string Label{get{return Type+(Lag>0?" +"+Lag:Lag<0?" "+Lag:"");}}}
static class ScheduleLogic {
 /* trades in network order: a rule only refers to trades earlier in this list */
 internal static readonly string[] Order={"CLR","PILE","EXC","LEAN","RB","FW","CFTG","CFRAME","BACK","POIS","GRAV","CSOF","MAS","STEEL","PAINTM","ROOF","ROOFACC","INSUL","WP","PLUMB","ELEC","PLAST","CEIL","TILE","FLOOR","DOOR","HW","RAIL","PAINT","PAINTW","PAVE","OTHER"};
 internal static readonly Dictionary<string,string> Names=new Dictionary<string,string>{{"CLR","Site clearing and removal"},{"PILE","Piling"},{"EXC","Excavation"},{"LEAN","Lean concrete"},{"RB","Reinforcing steel"},{"FW","Formworks"},
  {"CFTG","Footing concrete"},{"CFRAME","Frame concrete (columns, beams, slabs, walls)"},{"BACK","Backfill / embankment"},{"POIS","Soil poisoning"},{"GRAV","Gravel fill / bedding"},{"CSOF","Slab on fill"},{"MAS","Masonry"},
  {"STEEL","Structural steel"},{"PAINTM","Metal painting"},{"ROOF","Roofing"},{"ROOFACC","Roofing accessories"},{"INSUL","Roof insulation"},{"WP","Waterproofing"},{"PLUMB","Plumbing"},{"ELEC","Electrical"},
  {"PLAST","Plastering"},{"CEIL","Ceiling"},{"TILE","Tiles"},{"FLOOR","Floor finish"},{"DOOR","Doors and windows"},{"HW","Finishing hardware"},{"RAIL","Railings"},{"PAINT","Masonry / concrete painting"},
  {"PAINTW","Wood painting"},{"PAVE","Site works / pavement"},{"OTHER","Other works"}};
 sealed class Cand {internal string Trade,Type;internal double Lag;}
 static Cand C(string trade,string type,double lag){return new Cand{Trade=trade,Type=type,Lag=lag};}
 /* each rule = alternatives in order of preference; the first trade present in the project is used */
 static readonly Dictionary<string,Cand[][]> Rules=new Dictionary<string,Cand[][]>{
  {"PILE",new[]{new[]{C("CLR","FS",0)}}},
  {"EXC",new[]{new[]{C("PILE","FS",0),C("CLR","FS",0)}}},
  {"LEAN",new[]{new[]{C("EXC","SS",.3),C("CLR","FS",0)},new[]{C("EXC","FF",1)}}},
  {"RB",new[]{new[]{C("LEAN","SS",.3),C("EXC","SS",.3),C("PILE","FS",0),C("CLR","FS",0)}}},
  {"FW",new[]{new[]{C("RB","SS",.2),C("LEAN","SS",.3),C("EXC","SS",.3),C("CLR","FS",0)}}},
  {"CFTG",new[]{new[]{C("FW","SS",.15),C("RB","SS",.2),C("LEAN","SS",.3),C("EXC","SS",.3),C("CLR","FS",0)}}},
  {"CFRAME",new[]{new[]{C("CFTG","FS",0),C("FW","SS",.2),C("RB","SS",.2),C("EXC","FS",0),C("CLR","FS",0)},new[]{C("FW","FF",1)},new[]{C("RB","FF",1)}}},
  {"BACK",new[]{new[]{C("CFTG","FS",1),C("CFRAME","FS",1),C("EXC","FS",0),C("CLR","FS",0)}}},
  {"POIS",new[]{new[]{C("BACK","FS",0),C("EXC","FS",0),C("CLR","FS",0)}}},
  {"GRAV",new[]{new[]{C("POIS","FS",0),C("BACK","FS",0),C("EXC","FS",0),C("CLR","FS",0)}}},
  {"CSOF",new[]{new[]{C("GRAV","FS",0),C("POIS","FS",0),C("BACK","FS",0),C("CFTG","FS",0),C("CLR","FS",0)}}},
  {"MAS",new[]{new[]{C("CFRAME","SS",2),C("CFTG","FS",0),C("CSOF","FS",0),C("CLR","FS",0)}}},
  {"STEEL",new[]{new[]{C("CFRAME","FS",7),C("CFTG","FS",7),C("MAS","SS",.3),C("CLR","FS",0)}}},
  {"PAINTM",new[]{new[]{C("STEEL","SS",.5),C("CFRAME","FS",0),C("CLR","FS",0)},new[]{C("STEEL","FF",1)}}},
  {"ROOF",new[]{new[]{C("STEEL","FS",0),C("CFRAME","FS",7),C("MAS","SS",.5),C("CLR","FS",0)}}},
  {"ROOFACC",new[]{new[]{C("ROOF","SS",.5),C("STEEL","FS",0),C("CFRAME","FS",7),C("CLR","FS",0)},new[]{C("ROOF","FF",1)}}},
  {"INSUL",new[]{new[]{C("ROOF","SS",0),C("STEEL","FS",0),C("CFRAME","FS",7),C("CLR","FS",0)},new[]{C("ROOF","FF",0)}}},
  {"WP",new[]{new[]{C("CSOF","FS",7),C("CFRAME","FS",7),C("MAS","SS",.5),C("CLR","FS",0)}}},
  {"PLUMB",new[]{new[]{C("MAS","SS",.2),C("CFRAME","FS",0),C("CFTG","FS",0),C("CLR","FS",0)}}},
  {"ELEC",new[]{new[]{C("MAS","SS",.2),C("CFRAME","FS",0),C("CFTG","FS",0),C("CLR","FS",0)}}},
  {"PLAST",new[]{new[]{C("MAS","SS",.3),C("CFRAME","FS",7),C("CLR","FS",0)},new[]{C("MAS","FF",2)},new[]{C("PLUMB","SS",.3)},new[]{C("ELEC","SS",.3)}}},
  {"CEIL",new[]{new[]{C("ROOF","FS",0),C("STEEL","FS",0),C("CFRAME","FS",7),C("CLR","FS",0)},new[]{C("PLAST","SS",.3),C("MAS","SS",.5)},new[]{C("ELEC","SS",.5)},new[]{C("PLUMB","SS",.5)}}},
  {"TILE",new[]{new[]{C("PLAST","SS",.5),C("MAS","SS",.5),C("CSOF","FS",7),C("CLR","FS",0)},new[]{C("WP","FS",0)},new[]{C("PLUMB","FF",1)}}},
  {"FLOOR",new[]{new[]{C("PLAST","SS",.5),C("CSOF","FS",7),C("MAS","SS",.5),C("CLR","FS",0)}}},
  {"DOOR",new[]{new[]{C("PLAST","SS",.5),C("MAS","FS",0),C("CFRAME","FS",7),C("CLR","FS",0)},new[]{C("PLAST","FF",2)}}},
  {"HW",new[]{new[]{C("DOOR","SS",.5),C("PLAST","SS",.5),C("MAS","FS",0),C("CLR","FS",0)},new[]{C("DOOR","FF",1)}}},
  {"RAIL",new[]{new[]{C("TILE","SS",.5),C("PLAST","SS",.5),C("CFRAME","FS",7),C("CLR","FS",0)}}},
  {"PAINT",new[]{new[]{C("PLAST","SS",.5),C("MAS","FS",0),C("CFRAME","FS",7),C("CLR","FS",0)},new[]{C("PLAST","FF",3)}}},
  {"PAINTW",new[]{new[]{C("CEIL","SS",.5),C("DOOR","SS",.5),C("PLAST","SS",.5),C("CLR","FS",0)},new[]{C("CEIL","FF",2)}}},
  {"PAVE",new[]{new[]{C("ROOF","FS",0),C("CFRAME","FS",7),C("MAS","SS",.5),C("CLR","FS",0)}}}};
 /* items of no known trade follow the work phase of their item no. */
 static readonly Cand[][] OtherByPhase={new Cand[0],new[]{C("CLR","FS",0)},new[]{C("CLR","FS",0)},new[]{C("EXC","SS",.3),C("CLR","FS",0)},new[]{C("CFRAME","SS",.5),C("CFTG","FS",0),C("CLR","FS",0)},
  new[]{C("MAS","SS",.3),C("CFRAME","FS",0),C("CLR","FS",0)},new[]{C("PLAST","SS",.5),C("MAS","SS",.5),C("CLR","FS",0)},new[]{C("PLAST","SS",.5),C("MAS","FS",0),C("CLR","FS",0)}};
 static int Number(string code,out string rest){string c=(code??"").Trim();int n=0,i=0;while(i<c.Length&&char.IsDigit(c[i])){n=n*10+(c[i]-'0');i++;}rest=c.Substring(i);return i==0?-1:n;}
 internal static string Trade(ScheduleRow r){string rest;int n=Number(r.Code,out rest);string d=(r.Description??"").ToLowerInvariant();
  if(n==800||n==801||n==802)return "CLR";if(n==1052||n==808||n==1049||n==1717||n==400)return "PILE";if(n==803||n==102||n==103||n==1600)return "EXC";if(n==901)return "LEAN";if(n==902||n==404)return "RB";if(n==903)return "FW";
  if(n==900||n==405){if(d.Contains("footing")||d.Contains("pile cap"))return "CFTG";if(d.Contains("slab-on-fill")||d.Contains("slab on fill")||d.Contains("slab on grade")||d.Contains("slab-on-grade"))return "CSOF";return "CFRAME";}
  if(n==804)return d.Contains("gravel")||rest.StartsWith("(7)")?"GRAV":"BACK";if(n==1000)return "POIS";if(n==1046||n==506)return "MAS";if(n==1047||n==1048)return "STEEL";
  if(n==1014)return "ROOF";if(n==1013)return "ROOFACC";if(n==1038)return "INSUL";if(n==1016)return "WP";if(n==1001||n==1002)return "PLUMB";if(n>=1100&&n<1200)return "ELEC";if(n==1027)return "PLAST";if(n==1003)return "CEIL";
  if(n==1018||n==1019||n==1020)return "TILE";if(n==1021||n==1022)return "FLOOR";if(n==1004)return "HW";if(n>=1005&&n<=1012||n==1043)return "DOOR";if(n==1051)return "RAIL";
  if(n==1032){if(d.Contains("wood"))return "PAINTW";if(d.Contains("metal")||d.Contains("steel"))return "PAINTM";return "PAINT";}
  if(n>=300&&n<700)return "PAVE";return "OTHER";}
 /* started as early as possible, not moved by the levelling (work tied to the trade before it) */
 static readonly HashSet<string> NoLevel=new HashSet<string>{"LEAN","CFTG","INSUL","ROOFACC"};
 static int LagDays(Cand c,int predDays){if(c.Type!="SS"||c.Lag>=1)return (int)Math.Round(c.Lag);return c.Lag<=0?0:Math.Max(1,(int)Math.Round(c.Lag*predDays));}

 /* nodes (scheduled items with a duration), links and the CPM times */
 internal static List<ScheduleLink> Network(List<ScheduleRow> rows,out List<PdmNode> nodes){nodes=new List<PdmNode>();int id=0;
  foreach(var r in rows){id++;if(r.Days<=0)continue;nodes.Add(new PdmNode{Row=r,Id=id,Trade=Trade(r)});}
  var byTrade=nodes.GroupBy(n=>n.Trade).ToDictionary(g=>g.Key,g=>g.ToList());var links=new List<ScheduleLink>();
  foreach(var nd in nodes){Cand[][] rules;if(nd.Trade=="OTHER"){int p=Math.Max(0,Math.Min(OtherByPhase.Length-1,SchedulePhases.Of(nd.Row.Code)));rules=new[]{OtherByPhase[p]};}else if(!Rules.TryGetValue(nd.Trade,out rules))rules=new Cand[0][];
   foreach(var rule in rules){var hit=rule.FirstOrDefault(c=>byTrade.ContainsKey(c.Trade));if(hit==null)continue;foreach(var p in byTrade[hit.Trade])if(p!=nd)links.Add(new ScheduleLink{From=p,To=nd,Type=hit.Type,Lag=LagDays(hit,p.Row.Days)});}}
  /* network order: trade order, then Bill of Quantities order */
  nodes=nodes.OrderBy(n=>Array.IndexOf(Order,n.Trade)).ThenBy(n=>n.Id).ToList();
  foreach(var nd in nodes){int d=nd.Row.Days,es=0;foreach(var l in links.Where(x=>x.To==nd))es=Math.Max(es,Bound(l,d));nd.ES=es;nd.EF=es+d;}
  int total=nodes.Count==0?0:nodes.Max(n=>n.EF);
  for(int i=nodes.Count-1;i>=0;i--){var nd=nodes[i];int d=nd.Row.Days,lf=total;foreach(var l in links.Where(x=>x.From==nd)){var s=l.To;int c=l.Type=="FS"?s.LS-l.Lag:l.Type=="SS"?s.LS-l.Lag+d:s.LF-l.Lag;lf=Math.Min(lf,c);}nd.LF=lf;nd.LS=lf-d;nd.TF=nd.LS-nd.ES;}
  return links;}
 /* earliest start of the successor allowed by one link (predecessor at its ES / EF) */
 static int Bound(ScheduleLink l,int d){var p=l.From;return l.Type=="FS"?p.EF+l.Lag:l.Type=="SS"?p.ES+l.Lag:p.EF+l.Lag-d;}
 static int Bound(ScheduleLink l,int d,Dictionary<PdmNode,int> start){var p=l.From;int s=start[p],f=s+p.Row.Days;return l.Type=="FS"?f+l.Lag:l.Type=="SS"?s+l.Lag:f+l.Lag-d;}

 /* CPM, then manpower levelling within the float; sets Start / Finish (working days, 1-based) and Critical */
 internal static void Schedule(List<ScheduleRow> rows){List<PdmNode> nodes;var links=Network(rows,out nodes);int total=nodes.Count==0?0:nodes.Max(n=>n.EF);
  var load=new int[Math.Max(1,total)];var start=new Dictionary<PdmNode,int>();
  foreach(var nd in nodes){int d=nd.Row.Days,lo=0;foreach(var l in links.Where(x=>x.To==nd))lo=Math.Max(lo,Bound(l,d,start));int hi=Math.Max(lo,nd.LS);int men=ScheduleManpower.Labor(nd.Row);int best=lo;if(NoLevel.Contains(nd.Trade))hi=lo;
   if(men>0&&hi>lo){long bestKey=long.MaxValue;for(int s=lo;s<=hi;s++){int peak=0;long sq=0;for(int k=s;k<s+d&&k<load.Length;k++){int v=load[k]+men;peak=Math.Max(peak,v);sq+=(long)v*v-(long)load[k]*load[k];}
     long key=(long)peak*1000000000L+sq*100+(s-lo);if(key<bestKey){bestKey=key;best=s;}}}
   start[nd]=best;for(int k=best;k<best+d&&k<load.Length;k++)load[k]+=men;}
  foreach(var r in rows){r.Start=1;r.Finish=0;r.Critical=false;}
  foreach(var nd in nodes){nd.Row.Start=start[nd]+1;nd.Row.Finish=start[nd]+nd.Row.Days;nd.Row.Critical=nd.TF==0;nd.Row.Predecessors=string.Join(", ",links.Where(l=>l.To==nd).Select(l=>l.From.Id+" "+l.Label));}}
}
}
