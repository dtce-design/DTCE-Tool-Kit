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
 /* trades in network order: a rule only refers to trades earlier in this list (owner, 18 November 2026: "ireview ang
    buong pay items ko, para kahit anong pay items man ang mailagay tama pa din") - every item of the DUPA catalogue has
    a trade below; an item of no known trade follows the work phase of its item no. */
 internal static readonly string[] Order={"PRE","CLR","GIMP","PILEF","PILE","PTEST","SWEXC","PEXC","PPIPE","PBACK","EXC","FFILL","LEAN","RB","FW","CFTG","CFRAME","ANCHOR","BACK","POIS","GRAV","VB","CSOF","DRAIN","SITEW",
  "MAS","CSEC","STEEL","PURL","PAINTM","ROOF","ROOFACC","INSUL","WP","PLUMB","ELEC","PLAST","CLAD","PART","CEIL","TRIM","ELEC2","TILE","FLOOR","DOOR","HW","RAIL","PAINT","PAINTW","FIXT","LIGHT","EQUIP","MECH","FITOUT","SUBG","SUBB","PAVE","LAND","OTHER"};
 internal static readonly Dictionary<string,string> Names=new Dictionary<string,string>{{"PRE","General requirements (billboard, survey, investigation)"},{"CLR","Site clearing and removal"},{"GIMP","Ground improvement"},{"PILEF","Piles furnished"},{"PILE","Piling"},{"PTEST","Pile / load tests"},
  {"SWEXC","Roadway / surplus excavation"},{"PEXC","Pipeline trench excavation"},{"PPIPE","Pipeline pipes"},{"PBACK","Pipeline backfill"},{"EXC","Structure excavation"},{"FFILL","Foundation fill"},{"LEAN","Lean / seal concrete"},{"RB","Reinforcing steel"},{"FW","Formworks"},
  {"CFTG","Footing concrete"},{"CFRAME","Frame concrete (columns, beams, slabs, walls)"},{"ANCHOR","Anchor bolts"},{"BACK","Backfill / embankment"},{"POIS","Soil poisoning"},{"GRAV","Gravel fill / bedding"},{"VB","Damp proofing / vapour barrier"},{"CSOF","Slab on fill"},
  {"DRAIN","Drainage, canal, sewer and septic"},{"SITEW","Stone masonry / slope protection"},{"MAS","Masonry"},{"CSEC","Stiffeners, lintels, coping, canopy"},{"STEEL","Structural steel / roof framing"},{"PURL","Purlins, sagrods and bracing"},{"PAINTM","Metal painting / wood preservative"},{"ROOF","Roofing"},
  {"ROOFACC","Roofing accessories / fascia"},{"INSUL","Roof insulation"},{"WP","Waterproofing"},{"PLUMB","Plumbing pipes"},{"ELEC","Electrical rough-in"},{"PLAST","Plastering"},{"CLAD","Claddings and wall finishes"},{"PART","Drywall partitions"},{"CEIL","Ceiling"},
  {"TRIM","Mouldings"},{"ELEC2","Wires and wiring devices"},{"TILE","Tiles"},{"FLOOR","Floor finish / coating"},{"DOOR","Doors and windows"},{"HW","Finishing hardware"},{"RAIL","Railings"},{"PAINT","Masonry / concrete painting"},{"PAINTW","Wood painting"},
  {"FIXT","Plumbing fixtures"},{"LIGHT","Lighting fixtures"},{"EQUIP","Electrical equipment and systems"},{"MECH","Mechanical, fire protection and elevator"},{"FITOUT","Cabinets, countertops and partitions"},{"SUBG","Subgrade preparation"},{"SUBB","Aggregate subbase"},
  {"PAVE","Pavement / sidewalk"},{"LAND","Softscape"},{"OTHER","Other works"}};
 sealed class Cand {internal string Trade,Type;internal double Lag;}
 static Cand C(string trade,string type,double lag){return new Cand{Trade=trade,Type=type,Lag=lag};}
 static Cand[] Any(params Cand[] c){return c;}
 /* each rule = alternatives in order of preference; the first trade present in the project is used */
 static readonly Dictionary<string,Cand[][]> Rules=new Dictionary<string,Cand[][]>{
  {"GIMP",new[]{Any(C("CLR","FS",0))}},
  {"PILEF",new[]{Any(C("CLR","SS",0))}},
  {"PILE",new[]{Any(C("GIMP","FS",0),C("CLR","FS",0)),Any(C("PILEF","SS",.3))}},
  {"PTEST",new[]{Any(C("PILE","FS",7),C("GIMP","FS",3))}},
  {"SWEXC",new[]{Any(C("CLR","FS",0))}},
  {"PEXC",new[]{Any(C("CLR","FS",0))}},
  {"PPIPE",new[]{Any(C("PEXC","SS",.3),C("CLR","FS",0)),Any(C("PEXC","FF",1))}},
  {"PBACK",new[]{Any(C("PPIPE","SS",.3),C("PEXC","SS",.5),C("CLR","FS",0)),Any(C("PPIPE","FF",1))}},
  {"EXC",new[]{Any(C("PILE","FS",0),C("GIMP","FS",0),C("CLR","FS",0))}},
  {"FFILL",new[]{Any(C("EXC","SS",.3),C("CLR","FS",0)),Any(C("EXC","FF",1))}},
  {"LEAN",new[]{Any(C("FFILL","SS",.3),C("EXC","SS",.3),C("CLR","FS",0)),Any(C("FFILL","FF",1),C("EXC","FF",1))}},
  {"RB",new[]{Any(C("LEAN","SS",.3),C("EXC","SS",.3),C("PILE","FS",0),C("CLR","FS",0))}},
  {"FW",new[]{Any(C("RB","SS",.2),C("LEAN","SS",.3),C("EXC","SS",.3),C("CLR","FS",0))}},
  {"CFTG",new[]{Any(C("FW","SS",.15),C("RB","SS",.2),C("LEAN","SS",.3),C("EXC","SS",.3),C("CLR","FS",0))}},
  {"CFRAME",new[]{Any(C("CFTG","FS",0),C("FW","SS",.2),C("RB","SS",.2),C("EXC","FS",0),C("CLR","FS",0)),Any(C("FW","FF",1)),Any(C("RB","FF",1))}},
  {"ANCHOR",new[]{Any(C("CFRAME","SS",.3),C("CFTG","SS",.3),C("CLR","FS",0)),Any(C("CFRAME","FF",0))}},
  {"BACK",new[]{Any(C("CFTG","FS",1),C("CFRAME","FS",1),C("EXC","FS",0),C("CLR","FS",0))}},
  {"POIS",new[]{Any(C("BACK","FS",0),C("EXC","FS",0),C("CLR","FS",0))}},
  {"GRAV",new[]{Any(C("POIS","FS",0),C("BACK","FS",0),C("EXC","FS",0),C("CLR","FS",0))}},
  {"VB",new[]{Any(C("GRAV","FS",0),C("POIS","FS",0),C("BACK","FS",0),C("CLR","FS",0))}},
  {"CSOF",new[]{Any(C("VB","FS",0),C("GRAV","FS",0),C("POIS","FS",0),C("BACK","FS",0),C("CFTG","FS",0),C("CLR","FS",0))}},
  {"DRAIN",new[]{Any(C("BACK","FS",0),C("CFTG","FS",0),C("EXC","FS",0),C("CLR","FS",0))}},
  {"SITEW",new[]{Any(C("BACK","FS",0),C("EXC","FS",0),C("SWEXC","FS",0),C("CLR","FS",0))}},
  {"MAS",new[]{Any(C("CFRAME","SS",2),C("CFTG","FS",0),C("CSOF","FS",0),C("CLR","FS",0))}},
  {"CSEC",new[]{Any(C("MAS","SS",.3),C("CFRAME","FS",3),C("CLR","FS",0)),Any(C("MAS","FF",2))}},
  {"STEEL",new[]{Any(C("CFRAME","FS",7),C("CFTG","FS",7),C("MAS","SS",.3),C("CLR","FS",0)),Any(C("ANCHOR","FS",7))}},
  {"PURL",new[]{Any(C("STEEL","SS",.5),C("CFRAME","FS",7),C("MAS","SS",.5),C("CLR","FS",0)),Any(C("STEEL","FF",1))}},
  {"PAINTM",new[]{Any(C("STEEL","SS",.5),C("PURL","SS",.5),C("CFRAME","FS",0),C("CLR","FS",0)),Any(C("PURL","FF",1),C("STEEL","FF",1))}},
  {"ROOF",new[]{Any(C("PURL","SS",.5),C("STEEL","FS",0),C("CFRAME","FS",7),C("MAS","SS",.5),C("CLR","FS",0)),Any(C("PURL","FF",1)),Any(C("STEEL","FS",0))}},
  {"ROOFACC",new[]{Any(C("ROOF","SS",.5),C("STEEL","FS",0),C("CFRAME","FS",7),C("CLR","FS",0)),Any(C("ROOF","FF",1))}},
  {"INSUL",new[]{Any(C("ROOF","SS",0),C("STEEL","FS",0),C("CFRAME","FS",7),C("CLR","FS",0)),Any(C("ROOF","FF",0))}},
  {"WP",new[]{Any(C("CSOF","FS",7),C("CFRAME","FS",7),C("MAS","SS",.5),C("CLR","FS",0))}},
  {"PLUMB",new[]{Any(C("MAS","SS",.2),C("CFRAME","FS",0),C("CFTG","FS",0),C("CLR","FS",0))}},
  {"ELEC",new[]{Any(C("MAS","SS",.2),C("CFRAME","FS",0),C("CFTG","FS",0),C("CLR","FS",0))}},
  {"PLAST",new[]{Any(C("MAS","SS",.3),C("CFRAME","FS",7),C("CLR","FS",0)),Any(C("MAS","FF",2)),Any(C("PLUMB","SS",.3)),Any(C("ELEC","SS",.3)),Any(C("CSEC","SS",.5))}},
  {"CLAD",new[]{Any(C("PLAST","SS",.5),C("MAS","SS",.5),C("STEEL","FS",0),C("CFRAME","FS",7),C("CLR","FS",0)),Any(C("PLAST","FF",2))}},
  {"PART",new[]{Any(C("ROOF","FS",0),C("STEEL","FS",0),C("CFRAME","FS",7),C("CLR","FS",0)),Any(C("PLAST","SS",.3),C("MAS","SS",.5)),Any(C("ELEC","SS",.5))}},
  {"CEIL",new[]{Any(C("ROOF","FS",0),C("STEEL","FS",0),C("CFRAME","FS",7),C("CLR","FS",0)),Any(C("PLAST","SS",.3),C("MAS","SS",.5)),Any(C("ELEC","SS",.5)),Any(C("PLUMB","SS",.5)),Any(C("PART","SS",.3))}},
  {"TRIM",new[]{Any(C("CEIL","SS",.5),C("PLAST","SS",.5),C("MAS","FS",0),C("CLR","FS",0)),Any(C("PLAST","FF",1))}},
  {"ELEC2",new[]{Any(C("ELEC","SS",.5),C("MAS","FS",0),C("CFRAME","FS",7),C("CLR","FS",0)),Any(C("PLAST","SS",.5)),Any(C("CEIL","SS",.3))}},
  {"TILE",new[]{Any(C("PLAST","SS",.5),C("MAS","SS",.5),C("CSOF","FS",7),C("CLR","FS",0)),Any(C("WP","FS",0)),Any(C("PLUMB","FF",1))}},
  {"FLOOR",new[]{Any(C("PLAST","SS",.5),C("CSOF","FS",7),C("MAS","SS",.5),C("CLR","FS",0))}},
  {"DOOR",new[]{Any(C("PLAST","SS",.5),C("MAS","FS",0),C("CFRAME","FS",7),C("CLR","FS",0)),Any(C("PLAST","FF",2))}},
  {"HW",new[]{Any(C("DOOR","SS",.5),C("PLAST","SS",.5),C("MAS","FS",0),C("CLR","FS",0)),Any(C("DOOR","FF",1))}},
  {"RAIL",new[]{Any(C("TILE","SS",.5),C("PLAST","SS",.5),C("CFRAME","FS",7),C("CLR","FS",0))}},
  {"PAINT",new[]{Any(C("PLAST","SS",.5),C("CLAD","SS",.5),C("MAS","FS",0),C("CFRAME","FS",7),C("CLR","FS",0)),Any(C("PLAST","FF",3))}},
  {"PAINTW",new[]{Any(C("CEIL","SS",.5),C("PART","SS",.5),C("DOOR","SS",.5),C("PLAST","SS",.5),C("CLR","FS",0)),Any(C("CEIL","FF",2),C("PART","FF",2))}},
  {"FIXT",new[]{Any(C("TILE","SS",.5),C("PAINT","SS",.5),C("PLAST","FS",0),C("PLUMB","FS",0),C("CLR","FS",0)),Any(C("PLUMB","FS",0)),Any(C("TILE","FF",1))}},
  {"LIGHT",new[]{Any(C("CEIL","SS",.5),C("PAINT","SS",.5),C("PLAST","FS",0),C("ROOF","FS",0),C("CLR","FS",0)),Any(C("ELEC2","SS",.3),C("ELEC","FS",0)),Any(C("CEIL","FF",1))}},
  {"EQUIP",new[]{Any(C("ELEC2","SS",.5),C("ELEC","SS",.5),C("ROOF","FS",0),C("CFRAME","FS",7),C("CLR","FS",0)),Any(C("ELEC2","FF",1))}},
  {"MECH",new[]{Any(C("CEIL","SS",.3),C("ROOF","FS",0),C("MAS","SS",.5),C("CFRAME","FS",7),C("CLR","FS",0))}},
  {"FITOUT",new[]{Any(C("TILE","SS",.5),C("PAINT","SS",.5),C("PLAST","FS",0),C("MAS","FS",0),C("CLR","FS",0)),Any(C("PAINT","FF",1))}},
  {"SUBG",new[]{Any(C("SWEXC","SS",.3),C("PEXC","FS",0),C("BACK","FS",0),C("EXC","FS",0),C("CLR","FS",0)),Any(C("SWEXC","FF",1)),Any(C("DRAIN","FS",0)),Any(C("PBACK","FS",0))}},
  {"SUBB",new[]{Any(C("SUBG","SS",.3),C("SWEXC","FS",0),C("CLR","FS",0)),Any(C("SUBG","FF",1))}},
  {"PAVE",new[]{Any(C("SUBB","SS",.3),C("SUBG","SS",.3),C("ROOF","FS",0),C("CFRAME","FS",7),C("MAS","SS",.5),C("CLR","FS",0)),Any(C("SUBB","FF",1)),Any(C("ROOF","FS",0),C("CFRAME","FS",7)),Any(C("DRAIN","FS",0))}},
  {"LAND",new[]{Any(C("PAVE","SS",.5),C("ROOF","FS",0),C("CFRAME","FS",7),C("CLR","FS",0)),Any(C("PAVE","FF",1))}}};
 /* items of no known trade follow the work phase of their item no. */
 static readonly Cand[][] OtherByPhase={new Cand[0],new[]{C("CLR","FS",0)},new[]{C("CLR","FS",0)},new[]{C("EXC","SS",.3),C("CLR","FS",0)},new[]{C("CFRAME","SS",.5),C("CFTG","FS",0),C("CLR","FS",0)},
  new[]{C("MAS","SS",.3),C("CFRAME","FS",0),C("CLR","FS",0)},new[]{C("PLAST","SS",.5),C("MAS","SS",.5),C("CLR","FS",0)},new[]{C("PLAST","SS",.5),C("MAS","FS",0),C("CLR","FS",0)}};
 static int Number(string code,out string rest){string c=(code??"").Trim();int n=0,i=0;while(i<c.Length&&char.IsDigit(c[i])){n=n*10+(c[i]-'0');i++;}rest=c.Substring(i);return i==0?-1:n;}
 internal static string Trade(ScheduleRow r){string rest;int n=Number(r.Code,out rest);string d=(r.Description??"").ToLowerInvariant();string code=(r.Code??"").Trim().ToUpperInvariant();
  Func<string,bool> has=t=>d.Contains(t);
  if(n<0)return code.StartsWith("A.")||code.StartsWith("B.")?"PRE":"OTHER";/* general requirements: billboard, survey, investigation ... */
  if(n==800||n==801)return "CLR";if(n==802||n==102)return "SWEXC";if(n==105)return "SUBG";if(n==200||n==201||n==202)return "SUBB";
  if(n==103)return has("excavation")?"EXC":"FFILL";if(n==803)return "EXC";if(n==804)return has("gravel")||rest.StartsWith("(7)")?"GRAV":"BACK";
  if(n==807)return "LAND";if(n==808||n==1049){return has("test")?"PTEST":"GIMP";}
  if(n==1052){if(has("test"))return "PTEST";if(has("furnished"))return "PILEF";return "PILE";}if(n==1717||n==400)return "PILE";
  if(n==1600)return "PEXC";if(n==1601)return "PBACK";if(n==1602||n==1603||n==1604)return "PPIPE";
  if(n==901)return "LEAN";if(n==902||n==404)return "RB";if(n==903)return "FW";
  if(n==900||n==405){if(rest.StartsWith("(5)")||has("seal concrete"))return "LEAN";if(has("footing")||has("pile cap"))return "CFTG";if(has("slab-on-fill")||has("slab on fill")||has("slab on grade")||has("slab-on-grade"))return "CSOF";
   if(has("stiffener")||has("lintel")||has("coping")||has("canopy")||has("countertop")||has("parapet"))return "CSEC";return "CFRAME";}
  if(n==1000)return has("wood")?"PAINTM":"POIS";if(n==1034)return "VB";
  if(n==506||n==510||n==505||n==508)return "SITEW";if(n>=300&&n<700)return "PAVE";
  if(n==1046)return "MAS";
  if(n==1047){if(has("anchor"))return "ANCHOR";if(has("purlin")||has("sagrod")||has("brac")||has("turnbuckle")||has("tumbuckle"))return "PURL";return "STEEL";}if(n==1048)return "STEEL";
  if(n==1014)return "ROOF";if(n==1013)return has("roofing sheet")?"ROOF":"ROOFACC";if(n==1038)return "INSUL";if(n==1016)return "WP";
  if(n==1001){if(has("canal")||has("catch basin")||has("sewer")||has("storm")||has("septic")||has("drainage")||has("manhole"))return "DRAIN";return "PLUMB";}
  if(n==1002)return has("fixture")?"FIXT":"PLUMB";
  if(n==1100)return "ELEC";if(n==1101)return "ELEC2";if(n==1103)return "LIGHT";if(n>=1102&&n<1200)return "EQUIP";if(n>=1200&&n<1300)return "MECH";
  if(n==1027)return has("plaster")?"PLAST":"CLAD";if(n==1025||n==1039)return "CLAD";
  if(n==1003){if(rest.StartsWith("(5)")||rest.StartsWith("(6)"))return "STEEL";if(rest.StartsWith("(11)"))return "ROOFACC";if(rest.StartsWith("(15)"))return "TRIM";
   if(rest.StartsWith("(2)"))return "PART";if(rest.StartsWith("(3)")||rest.StartsWith("(4)")||rest.StartsWith("(14)")||rest.StartsWith("(22)"))return "FITOUT";if(has("ceiling")||rest.StartsWith("(1)"))return "CEIL";return "OTHER";}
  if(n==1019)return has("ceiling")?"CEIL":"CLAD";
  if(n==1018){if(has("countertop"))return "FITOUT";return "TILE";}if(n==1020)return "TILE";if(n==1021||n==1022)return "FLOOR";
  if(n==1004)return "HW";if(n>=1005&&n<=1012||n==1043)return "DOOR";if(n==1044)return "FITOUT";if(n==1051)return "RAIL";
  if(n==1032){if(has("floor coating"))return "FLOOR";if(rest.StartsWith("(1)c")||has("metal"))return "PAINTM";if(rest.StartsWith("(1)b")||rest.StartsWith("(2)")||rest.StartsWith("(4)")||has("wood"))return "PAINTW";return "PAINT";}
  return "OTHER";}
 /* started as early as possible, not moved by the levelling (work tied to the trade before it) */
 static readonly HashSet<string> NoLevel=new HashSet<string>{"PRE","LEAN","CFTG","ANCHOR","INSUL","ROOFACC"};
 /* checks used by the tests: every rule refers only to earlier trades and every trade is named */
 internal static IEnumerable<string> RuleProblems(){foreach(var r in Rules){int i=Array.IndexOf(Order,r.Key);if(i<0)yield return "rule for unknown trade "+r.Key;foreach(var alt in r.Value)foreach(var c in alt){int j=Array.IndexOf(Order,c.Trade);if(j<0||j>=i)yield return r.Key+" refers to "+c.Trade;}}
  foreach(var t in Order)if(!Names.ContainsKey(t))yield return "no name for "+t;}
 static int LagDays(Cand c,int predDays){if(c.Type!="SS"||c.Lag>=1)return (int)Math.Round(c.Lag);return c.Lag<=0?0:Math.Max(1,(int)Math.Round(c.Lag*predDays));}

 /* nodes (scheduled items with a duration), links and the CPM times */
 internal static List<ScheduleLink> Network(List<ScheduleRow> rows,out List<PdmNode> nodes){nodes=new List<PdmNode>();int id=0;
  foreach(var r in rows){id++;if(r.Days<=0)continue;nodes.Add(new PdmNode{Row=r,Id=id,Trade=Trade(r)});}
  var byTrade=nodes.GroupBy(n=>n.Trade).ToDictionary(g=>g.Key,g=>g.ToList());var links=new List<ScheduleLink>();
  foreach(var nd in nodes){Cand[][] rules;if(nd.Trade=="OTHER"){int p=Math.Max(0,Math.Min(OtherByPhase.Length-1,SchedulePhases.Of(nd.Row.Code)));rules=new[]{OtherByPhase[p]};}else if(!Rules.TryGetValue(nd.Trade,out rules))rules=new Cand[0][];
   foreach(var rule in rules){var hit=rule.FirstOrDefault(c=>byTrade.ContainsKey(c.Trade));if(hit==null)continue;foreach(var p in byTrade[hit.Trade])if(p!=nd)links.Add(new ScheduleLink{From=p,To=nd,Type=hit.Type,Lag=LagDays(hit,p.Row.Days)});}}
  /* network order: trade order, then Bill of Quantities order */
  nodes=nodes.OrderBy(n=>Array.IndexOf(Order,n.Trade)).ThenBy(n=>n.Id).ToList();
  /* drop relations already implied by other relations (every type is "start of s >= start of p + c", c = D(p) + lag for FS,
     lag for SS, D(p) + lag - D(s) for FF; a link is implied when another path from p to s gives at least the same c), so the
     PDM and the PERT network show only the relations that matter; the schedule is not changed by this */
  Reduce(nodes,links);
  foreach(var nd in nodes){int d=nd.Row.Days,es=0;foreach(var l in links.Where(x=>x.To==nd))es=Math.Max(es,Bound(l,d));nd.ES=es;nd.EF=es+d;}
  int total=nodes.Count==0?0:nodes.Max(n=>n.EF);
  for(int i=nodes.Count-1;i>=0;i--){var nd=nodes[i];int d=nd.Row.Days,lf=total;foreach(var l in links.Where(x=>x.From==nd)){var s=l.To;int c=l.Type=="FS"?s.LS-l.Lag:l.Type=="SS"?s.LS-l.Lag+d:s.LF-l.Lag;lf=Math.Min(lf,c);}nd.LF=lf;nd.LS=lf-d;nd.TF=nd.LS-nd.ES;}
  return links;}
 static int Offset(ScheduleLink l){int dp=l.From.Row.Days,ds=l.To.Row.Days;return l.Type=="FS"?dp+l.Lag:l.Type=="SS"?l.Lag:dp+l.Lag-ds;}
 static void Reduce(List<PdmNode> nodes,List<ScheduleLink> links){var rank=new Dictionary<PdmNode,int>();for(int i=0;i<nodes.Count;i++)rank[nodes[i]]=i;
  foreach(var l in links.OrderByDescending(x=>rank[x.To]-rank[x.From]).ToList()){int c=Offset(l);
   /* longest path from l.From to l.To without l (network order is topological) */
   var best=new Dictionary<PdmNode,int>{{l.From,0}};foreach(var nd in nodes.Where(n=>rank[n]>rank[l.From]&&rank[n]<=rank[l.To])){int v=int.MinValue;foreach(var k in links)if(k!=l&&k.To==nd){int b;if(best.TryGetValue(k.From,out b))v=Math.Max(v,b+Offset(k));}if(v>int.MinValue)best[nd]=v;}
   int via;if(best.TryGetValue(l.To,out via)&&via>=c)links.Remove(l);}}
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
