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
// Reviewed item by item for the whole DUPA catalogue (owner, 18 November 2026: "ireview lahat ng pay items ko para kahit
// anong pay items man ang maisama tama pa din"): gravel bedding before the lean concrete and footings, pile tests before
// the pile caps, the roof deck as the last frame pour, under-slab sanitary piping before soil poisoning, gravel and the
// slab on fill, masonry finishing after the frame, rough-in before plaster closes, tiles and floor finishes 7 days after
// their slab and under a roof, painting 14 days after the plaster, vinyl and floor coatings on a cured slab after the
// painting, road subgrade / subbase / pavement after the building is roofed, signs and markings last.
// Survey, lean concrete, footing pours, the roof deck, pile tests, roof insulation and roofing accessories are not moved
// by the levelling (they go with the trade before them).
sealed class ScheduleLink {internal PdmNode From,To;internal string Type;internal int Lag;
 internal string Label{get{return Type+(Lag>0?" +"+Lag:Lag<0?" "+Lag:"");}}}
static class ScheduleLogic {
 /* trades in network order: a rule only refers to trades earlier in this list (owner, 18 November 2026: "ireview ang
    buong pay items ko, para kahit anong pay items man ang mailagay tama pa din") - every item of the DUPA catalogue has
    a trade below; an item of no known trade follows the work phase of its item no. */
 internal static readonly string[] Order={"PRE","CLR","INVEST","GIMP","PILEF","PILE","PTEST","SPOIL","PEXC","PPIPE","PBACK","EXC","FFILL","GRAVF","LEAN","RB","FW","CFTG","FWS","CFRAME","CROOF","ANCHOR","RWALL","BACK",
  "SANIT","POIS","GRAV","VB","CSOF","DRAIN","SITEW","MAS","JAMB","CSEC","GLASSB","STEEL","PURL","WPRES","PAINTM","ROOF","ROOFACC","STORM","INSUL","PLUMB","ELEC","FPROT","WP","PLAST","CLAD","PART","CEIL","TRIM",
  "ELEC2","TILE","FLOOR","DOOR","HW","RAIL","PAINTMF","PAINT","PAINTW","FCOAT","CAB","FIXT","LIGHT","EQUIP","MECH","LIFT","FITOUT","SWEXC","SUBG","SUBB","PAVE","MARK","LAND","OTHER"};
 internal static readonly Dictionary<string,string> Names=new Dictionary<string,string>{{"PRE","General requirements (offices, billboard, safety)"},{"CLR","Site clearing and removal"},{"INVEST","Site investigation, survey and staking"},
  {"GIMP","Ground improvement"},{"PILEF","Piles furnished"},{"PILE","Piling"},{"PTEST","Pile / load tests"},{"SPOIL","Surplus excavation"},{"PEXC","Pipeline trench excavation"},{"PPIPE","Pipeline pipes"},{"PBACK","Pipeline backfill"},
  {"EXC","Structure excavation"},{"FFILL","Foundation fill"},{"GRAVF","Gravel bedding (footings, tie beams)"},{"LEAN","Lean / seal concrete"},{"RB","Reinforcing steel"},{"FW","Formworks"},{"CFTG","Footing concrete"},
  {"FWS","Slab forms, steel decking, falsework"},{"CFRAME","Frame concrete (columns, beams, slabs, walls)"},{"CROOF","Roof deck / roof slab concrete"},{"ANCHOR","Anchor bolts"},{"RWALL","Stone masonry / gabion walls"},
  {"BACK","Backfill / embankment"},{"SANIT","Sanitary drain, waste and vent piping"},{"POIS","Soil poisoning"},{"GRAV","Gravel bed (slab on fill, other areas)"},{"VB","Damp proofing / vapour barrier"},{"CSOF","Slab on fill"},
  {"DRAIN","Canal, catch basin, sewer and septic"},{"SITEW","Riprap and slope protection"},{"MAS","Masonry"},{"JAMB","Door jambs and frames"},{"CSEC","Stiffeners, lintels, coping, mouldings"},{"GLASSB","Glass block walls"},
  {"STEEL","Structural steel / roof framing"},{"PURL","Purlins, sagrods and bracing"},{"WPRES","Wood preservative"},{"PAINTM","Metal painting (steel framing)"},{"ROOF","Roofing"},{"ROOFACC","Roofing accessories / fascia"},
  {"STORM","Storm drainage and downspouts"},{"INSUL","Roof insulation"},{"PLUMB","Water supply piping"},{"ELEC","Electrical rough-in"},{"FPROT","Fire sprinkler piping"},{"WP","Waterproofing"},{"PLAST","Plastering"},
  {"CLAD","Claddings and wall finishes"},{"PART","Drywall partitions"},{"CEIL","Ceiling"},{"TRIM","Wood mouldings / cornice"},{"ELEC2","Wires and wiring devices"},{"TILE","Tiles"},{"FLOOR","Cement floor finish / topping"},
  {"DOOR","Doors and windows"},{"HW","Finishing hardware"},{"RAIL","Railings"},{"PAINTMF","Metal finish painting (doors, grilles)"},{"PAINT","Masonry / concrete painting"},{"PAINTW","Wood painting"},
  {"FCOAT","Vinyl flooring / floor coating"},{"CAB","Cabinets and countertops"},{"FIXT","Plumbing fixtures"},{"LIGHT","Lighting fixtures"},{"EQUIP","Electrical equipment and systems"},{"MECH","Mechanical and fire protection"},
  {"LIFT","Elevators"},{"FITOUT","Toilet, modular and folding partitions"},{"SWEXC","Roadway excavation / embankment"},{"SUBG","Subgrade preparation"},{"SUBB","Aggregate subbase"},{"PAVE","Pavement / sidewalk / curb"},
  {"MARK","Road signs, markings and fences"},{"LAND","Softscape"},{"OTHER","Other works"}};
 sealed class Cand {internal string Trade,Type;internal double Lag;}
 static Cand C(string trade,string type,double lag){return new Cand{Trade=trade,Type=type,Lag=lag};}
 static Cand[] Any(params Cand[] c){return c;}
 /* each rule = alternatives in order of preference; the first trade present in the project is used; the rules of one
    trade all apply (a trade missing from the project is skipped) */
 static readonly Dictionary<string,Cand[][]> Rules=new Dictionary<string,Cand[][]>{
  {"INVEST",new[]{Any(C("CLR","SS",.5))}},
  {"GIMP",new[]{Any(C("CLR","FS",0)),Any(C("INVEST","FS",0))}},
  {"PILEF",new[]{Any(C("CLR","SS",0)),Any(C("INVEST","FS",0))}},
  {"PILE",new[]{Any(C("GIMP","FS",0),C("CLR","FS",0)),Any(C("PILEF","SS",.3)),Any(C("INVEST","FS",0))}},
  /* tests on the first piles 7 days after they are driven / cast, the last one 7 days after the last pile */
  {"PTEST",new[]{Any(C("PILE","SS",7),C("GIMP","SS",3),C("CLR","FS",0)),Any(C("PILE","FF",7),C("GIMP","FF",3))}},
  {"SPOIL",new[]{Any(C("CLR","FS",0))}},
  {"PEXC",new[]{Any(C("CLR","FS",0))}},
  {"PPIPE",new[]{Any(C("PEXC","SS",.3),C("CLR","FS",0)),Any(C("PEXC","FF",1))}},
  {"PBACK",new[]{Any(C("PPIPE","SS",.3),C("PEXC","SS",.5),C("CLR","FS",0)),Any(C("PPIPE","FF",1),C("PEXC","FF",1))}},
  {"EXC",new[]{Any(C("PILE","FS",0),C("GIMP","FS",0),C("CLR","FS",0)),Any(C("INVEST","FS",0))}},
  {"FFILL",new[]{Any(C("EXC","SS",.3),C("CLR","FS",0)),Any(C("EXC","FF",1)),Any(C("INVEST","FS",0))}},
  /* gravel bedding under the footings and tie beams: placed in the excavated pits before the lean concrete */
  {"GRAVF",new[]{Any(C("FFILL","SS",.3),C("EXC","SS",.3),C("CLR","FS",0)),Any(C("FFILL","FF",1),C("EXC","FF",1))}},
  {"LEAN",new[]{Any(C("GRAVF","SS",.3),C("FFILL","SS",.3),C("EXC","SS",.3),C("CLR","FS",0)),Any(C("GRAVF","FF",1),C("FFILL","FF",1),C("EXC","FF",1)),Any(C("INVEST","FS",0))}},
  {"RB",new[]{Any(C("LEAN","SS",.3),C("GRAVF","SS",.3),C("EXC","SS",.3),C("PILE","FS",0),C("CLR","FS",0)),Any(C("INVEST","FS",0))}},
  {"FW",new[]{Any(C("RB","SS",.2),C("LEAN","SS",.3),C("GRAVF","SS",.3),C("EXC","SS",.3),C("CLR","FS",0))}},
  /* footing / pile-cap pours after every pile test, and not finished before the lean concrete or bedding below */
  {"CFTG",new[]{Any(C("FW","SS",.15),C("RB","SS",.2),C("LEAN","SS",.3),C("GRAVF","SS",.3),C("EXC","SS",.3),C("CLR","FS",0)),Any(C("PTEST","FS",0)),Any(C("LEAN","FF",1),C("GRAVF","FF",1),C("FFILL","FF",1),C("EXC","FF",1)),Any(C("INVEST","FS",0))}},
  {"FWS",new[]{Any(C("CFTG","FS",0),C("FW","SS",.3),C("RB","SS",.2),C("EXC","FS",0),C("CLR","FS",0))}},
  {"CFRAME",new[]{Any(C("CFTG","FS",0),C("FW","SS",.2),C("RB","SS",.2),C("EXC","FS",0),C("CLR","FS",0)),Any(C("FW","FF",1)),Any(C("RB","FF",1)),Any(C("PTEST","FS",0)),Any(C("FWS","FF",1))}},
  /* the roof deck / roof slab is the last pour of the frame */
  {"CROOF",new[]{Any(C("CFRAME","SS",.7),C("CFTG","FS",0),C("CLR","FS",0)),Any(C("CFRAME","FF",0)),Any(C("FW","FF",1)),Any(C("RB","FF",1)),Any(C("FWS","FF",1))}},
  {"ANCHOR",new[]{Any(C("CFRAME","SS",.3),C("CFTG","SS",.3),C("CLR","FS",0)),Any(C("CFRAME","FF",0))}},
  {"RWALL",new[]{Any(C("EXC","SS",.3),C("CLR","FS",0)),Any(C("EXC","FF",1))}},
  {"BACK",new[]{Any(C("CFTG","FS",1),C("CFRAME","FS",1),C("EXC","FS",0),C("CLR","FS",0))}},
  /* under-slab sanitary lines go in with the backfill, before soil poisoning, gravel bed and slab on fill */
  {"SANIT",new[]{Any(C("BACK","SS",.5),C("CFTG","FS",0),C("EXC","FS",0),C("CFRAME","FS",0),C("CLR","FS",0))}},
  {"POIS",new[]{Any(C("BACK","FS",0),C("EXC","FS",0),C("CLR","FS",0)),Any(C("SANIT","SS",.5))}},
  {"GRAV",new[]{Any(C("POIS","FS",0),C("BACK","FS",0),C("EXC","FS",0),C("CLR","FS",0)),Any(C("SANIT","SS",.5))}},
  {"VB",new[]{Any(C("GRAV","FS",0),C("POIS","FS",0),C("BACK","FS",0),C("CFTG","FS",1),C("CFRAME","SS",.3),C("EXC","FS",0),C("CLR","FS",0)),Any(C("CFTG","FS",1),C("CFRAME","SS",.3),C("EXC","FS",0)),Any(C("GRAVF","FS",0)),Any(C("SANIT","SS",.5))}},
  {"CSOF",new[]{Any(C("VB","FS",0),C("GRAV","FS",0),C("POIS","FS",0),C("BACK","FS",0),C("CFTG","FS",0),C("CLR","FS",0)),Any(C("CFTG","FS",1),C("CFRAME","SS",.3),C("EXC","FS",0)),Any(C("GRAV","FS",0),C("GRAVF","FS",0)),Any(C("SANIT","SS",.5)),Any(C("SANIT","FF",1))}},
  {"DRAIN",new[]{Any(C("BACK","FS",0),C("CFTG","FS",0),C("EXC","FS",0),C("CLR","FS",0))}},
  {"SITEW",new[]{Any(C("BACK","FS",0),C("EXC","FS",0),C("SPOIL","FS",0),C("CLR","FS",0))}},
  /* walls from 30 % of the frame, the last walls (upper floors) 7 days after the last frame pour */
  {"MAS",new[]{Any(C("CFRAME","SS",.3),C("CFTG","FS",0),C("CSOF","FS",0),C("CLR","FS",0)),Any(C("CFRAME","FF",7))}},
  {"JAMB",new[]{Any(C("MAS","SS",.2),C("CFRAME","FS",3),C("CLR","FS",0))}},
  {"CSEC",new[]{Any(C("MAS","SS",.3),C("CFRAME","FS",3),C("CFTG","FS",0),C("CLR","FS",0)),Any(C("MAS","FF",2))}},
  {"GLASSB",new[]{Any(C("MAS","SS",.5),C("CFRAME","FS",7),C("CLR","FS",0))}},
  {"STEEL",new[]{Any(C("CFRAME","FS",7),C("CSEC","FS",7),C("MAS","FS",3),C("CFTG","FS",7),C("CLR","FS",0)),Any(C("CFTG","FS",7)),Any(C("ANCHOR","FS",7)),Any(C("CROOF","FS",7))}},
  {"PURL",new[]{Any(C("STEEL","SS",.5),C("CFRAME","FS",7),C("CSEC","FS",7),C("MAS","FS",3),C("CLR","FS",0)),Any(C("STEEL","FF",1))}},
  {"WPRES",new[]{Any(C("STEEL","SS",.5),C("PURL","SS",.5),C("CFRAME","FS",0),C("CLR","FS",0)),Any(C("PURL","FF",1),C("STEEL","FF",1))}},
  {"PAINTM",new[]{Any(C("STEEL","SS",.5),C("PURL","SS",.5),C("CFRAME","FS",0),C("CLR","FS",0)),Any(C("PURL","FF",1),C("STEEL","FF",1))}},
  /* roof sheets on painted purlins and trusses */
  {"ROOF",new[]{Any(C("PURL","SS",.5),C("STEEL","FS",0),C("CFRAME","FS",7),C("CSEC","FS",7),C("MAS","FS",3),C("CLR","FS",0)),Any(C("PURL","FF",1)),Any(C("STEEL","FS",0)),Any(C("PAINTM","SS",.5)),Any(C("PAINTM","FF",1))}},
  {"ROOFACC",new[]{Any(C("ROOF","SS",.5),C("STEEL","FS",0),C("CFRAME","FS",7),C("CLR","FS",0)),Any(C("ROOF","FF",1))}},
  /* storm lines in the ground early, the downspouts after the roofing */
  {"STORM",new[]{Any(C("BACK","FS",0),C("CFTG","FS",0),C("EXC","FS",0),C("CLR","FS",0)),Any(C("ROOFACC","FF",1),C("ROOF","FF",1))}},
  {"INSUL",new[]{Any(C("ROOF","SS",0),C("STEEL","FS",0),C("CFRAME","FS",7),C("CLR","FS",0)),Any(C("ROOF","FF",0))}},
  /* rough-in with the walls; without walls from 30 % / 20 % of the frame, finishing with the frame */
  {"PLUMB",new[]{Any(C("MAS","SS",.2),C("CFRAME","SS",.3),C("CFTG","FS",0),C("CLR","FS",0)),Any(C("MAS","SS",.2),C("CFRAME","FF",0))}},
  {"ELEC",new[]{Any(C("MAS","SS",.2),C("CFRAME","SS",.2),C("CFTG","FS",0),C("CLR","FS",0)),Any(C("MAS","SS",.2),C("CFRAME","FF",0))}},
  {"FPROT",new[]{Any(C("ROOF","FS",0),C("STEEL","FS",0),C("CFRAME","FS",7),C("CLR","FS",0)),Any(C("MAS","SS",.5))}},
  {"WP",new[]{Any(C("CROOF","FS",7),C("CFRAME","FS",7),C("CSOF","FS",7),C("MAS","SS",.5),C("CLR","FS",0)),Any(C("CSOF","FS",7)),Any(C("PLUMB","SS",.5)),Any(C("PLUMB","FF",1))}},
  /* plaster closes the walls after the pipes, conduits, jambs, lintels and glass blocks in them */
  {"PLAST",new[]{Any(C("MAS","SS",.3),C("CFRAME","FS",7),C("CLR","FS",0)),Any(C("MAS","FF",2)),Any(C("PLUMB","SS",.3)),Any(C("ELEC","SS",.3)),Any(C("CSEC","SS",.5)),Any(C("PLUMB","FF",1)),Any(C("ELEC","FF",1)),Any(C("CSEC","FF",1)),Any(C("JAMB","SS",.5)),Any(C("GLASSB","FF",2))}},
  {"CLAD",new[]{Any(C("PLAST","SS",.5),C("MAS","SS",.5),C("STEEL","FS",0),C("CFRAME","FS",7),C("CLR","FS",0)),Any(C("PLAST","FF",2)),Any(C("STEEL","FS",0))}},
  {"PART",new[]{Any(C("ROOF","FS",0),C("STEEL","FS",0),C("CFRAME","FS",7),C("CLR","FS",0)),Any(C("PLAST","SS",.3),C("MAS","SS",.5)),Any(C("ELEC","SS",.5)),Any(C("ELEC","FF",1)),Any(C("CROOF","FS",7))}},
  /* ceiling boards closed after the rough-in above them */
  {"CEIL",new[]{Any(C("ROOF","FS",0),C("STEEL","FS",0),C("CFRAME","FS",7),C("CLR","FS",0)),Any(C("PLAST","SS",.3),C("MAS","SS",.5)),Any(C("ELEC","SS",.5)),Any(C("PLUMB","SS",.5)),Any(C("PART","SS",.3)),Any(C("ELEC","FF",1)),Any(C("PLUMB","FF",1)),Any(C("SANIT","FF",1)),Any(C("FPROT","SS",.5)),Any(C("CROOF","FS",7))}},
  {"TRIM",new[]{Any(C("CEIL","SS",.5),C("PLAST","SS",.5),C("MAS","FS",0),C("CLR","FS",0)),Any(C("PLAST","FF",1))}},
  {"ELEC2",new[]{Any(C("ELEC","SS",.5),C("MAS","FS",0),C("CFRAME","FS",7),C("CLR","FS",0)),Any(C("ELEC","FF",1)),Any(C("PLAST","SS",.5)),Any(C("PLAST","FF",2)),Any(C("CEIL","SS",.3)),Any(C("CEIL","FF",1))}},
  /* tiles and floor finishes 7 days after their slab, under a roof, after the drains and waterproofing below them */
  {"TILE",new[]{Any(C("PLAST","SS",.5),C("MAS","SS",.5),C("ROOF","FS",0),C("CFRAME","FS",7),C("CLR","FS",0)),Any(C("CSOF","FS",7)),Any(C("CFRAME","FS",7)),Any(C("CROOF","FS",7)),Any(C("ROOF","SS",.5)),Any(C("WP","SS",.5)),Any(C("WP","FF",2)),Any(C("PLUMB","SS",.5)),Any(C("PLUMB","FF",1)),Any(C("PLAST","FF",2)),Any(C("SANIT","FS",0))}},
  {"FLOOR",new[]{Any(C("PLAST","SS",.5),C("CSOF","FS",7),C("MAS","SS",.5),C("ROOF","FS",0),C("CFRAME","FS",7),C("CLR","FS",0)),Any(C("CSOF","FS",7)),Any(C("CFRAME","FS",7)),Any(C("CROOF","FS",7)),Any(C("ROOF","SS",.5)),Any(C("PLAST","FF",2)),Any(C("CEIL","FF",2),C("ROOF","FF",2))}},
  {"DOOR",new[]{Any(C("PLAST","SS",.5),C("MAS","FS",0),C("CFRAME","FS",7),C("CLR","FS",0)),Any(C("PLAST","FF",2)),Any(C("JAMB","FS",0))}},
  {"HW",new[]{Any(C("DOOR","SS",.5),C("PLAST","SS",.5),C("MAS","FS",0),C("CLR","FS",0)),Any(C("DOOR","FF",1))}},
  {"RAIL",new[]{Any(C("TILE","SS",.5),C("PLAST","SS",.5),C("CFRAME","FS",7),C("CLR","FS",0))}},
  {"PAINTMF",new[]{Any(C("DOOR","SS",.5),C("RAIL","SS",.5),C("PLAST","SS",.5),C("STEEL","FS",0),C("MAS","FS",0),C("CFRAME","FS",7),C("CLR","FS",0)),Any(C("DOOR","FF",1)),Any(C("RAIL","FF",1))}},
  /* painting 14 days after the plaster (curing), under a roof, finishing after the ceiling, tiles and doors */
  {"PAINT",new[]{Any(C("PLAST","SS",.5),C("CLAD","SS",.5),C("MAS","FS",0),C("CFRAME","FS",7),C("CLR","FS",0)),Any(C("PLAST","SS",14)),Any(C("PLAST","FF",14)),Any(C("ROOF","FS",0),C("CROOF","FS",7),C("CFRAME","FS",14)),Any(C("CEIL","FF",2)),Any(C("TILE","FF",2)),Any(C("DOOR","FF",2)),Any(C("GLASSB","FF",3))}},
  {"PAINTW",new[]{Any(C("CEIL","SS",.5),C("PART","SS",.5),C("DOOR","SS",.5),C("PLAST","SS",.5),C("CLR","FS",0)),Any(C("CEIL","FF",2)),Any(C("PART","FF",2)),Any(C("DOOR","FF",2)),Any(C("TRIM","FF",1))}},
  /* vinyl and floor coatings on a dry slab (24 working days, about 28 days), after the painting and the ceiling */
  {"FCOAT",new[]{Any(C("PAINT","SS",.5),C("PAINTW","SS",.5),C("CEIL","SS",.5),C("PLAST","FS",0),C("MAS","FS",0),C("CLR","FS",0)),Any(C("PLAST","FS",0)),Any(C("CEIL","FF",2),C("ROOF","FS",0),C("STEEL","FS",0)),Any(C("FLOOR","FS",7)),Any(C("CSOF","FS",24)),Any(C("CFRAME","FS",24)),Any(C("PAINT","FF",2)),Any(C("PAINTW","FF",2))}},
  {"CAB",new[]{Any(C("TILE","SS",.5),C("PAINT","SS",.5),C("PLAST","FS",0),C("MAS","FS",0),C("CLR","FS",0)),Any(C("PAINT","FF",1))}},
  {"FIXT",new[]{Any(C("TILE","SS",.5),C("PAINT","SS",.5),C("PLAST","FS",0),C("PLUMB","FS",0),C("CLR","FS",0)),Any(C("PLUMB","FS",0)),Any(C("TILE","FF",1)),Any(C("CAB","FF",1)),Any(C("SANIT","FS",0))}},
  {"LIGHT",new[]{Any(C("CEIL","SS",.5),C("PAINT","SS",.5),C("PLAST","FS",0),C("ROOF","FS",0),C("CLR","FS",0)),Any(C("ELEC2","SS",.3),C("ELEC","FS",0)),Any(C("CEIL","FF",1)),Any(C("ROOF","FS",0),C("CROOF","FS",7),C("CFRAME","FS",7))}},
  {"EQUIP",new[]{Any(C("ELEC2","SS",.5),C("ELEC","SS",.5),C("ROOF","FS",0),C("CFRAME","FS",7),C("CLR","FS",0)),Any(C("ELEC2","FF",1),C("ELEC","FF",1)),Any(C("ROOF","FS",0),C("CROOF","FS",7),C("STEEL","FS",0),C("CFRAME","FS",7)),Any(C("CEIL","FF",1),C("PAINT","FF",1))}},
  {"MECH",new[]{Any(C("ELEC","SS",.5),C("PLUMB","SS",.5),C("ROOF","FS",0),C("MAS","SS",.5),C("CFRAME","FS",7),C("PPIPE","SS",.5),C("CLR","FS",0)),Any(C("ROOF","FS",0),C("CROOF","FS",7),C("CFRAME","FS",7)),Any(C("CEIL","FF",3),C("PLAST","FF",3),C("PLUMB","FF",3))}},
  /* elevators once the hoistway and machine room are complete, tested on permanent power */
  {"LIFT",new[]{Any(C("CROOF","FS",7),C("CFRAME","FS",7),C("STEEL","FS",0),C("MAS","FS",0),C("CFTG","FS",7),C("CLR","FS",0)),Any(C("EQUIP","FF",1),C("ELEC2","FF",1))}},
  {"FITOUT",new[]{Any(C("TILE","SS",.5),C("PAINT","SS",.5),C("PLAST","FS",0),C("MAS","FS",0),C("CLR","FS",0)),Any(C("PAINT","FF",1))}},
  /* roads, parking and walks after the building is roofed; on a road job right after clearing */
  {"SWEXC",new[]{Any(C("ROOF","FS",0),C("MAS","FS",0),C("CLR","FS",0))}},
  {"SUBG",new[]{Any(C("SWEXC","SS",.3),C("PEXC","FS",0),C("BACK","FS",0),C("EXC","FS",0),C("CLR","FS",0)),Any(C("SWEXC","FF",1)),Any(C("DRAIN","FS",0)),Any(C("PBACK","FS",0)),Any(C("ROOF","FS",0),C("CFRAME","FS",0),C("MAS","FS",0)),Any(C("STORM","FS",0))}},
  {"SUBB",new[]{Any(C("SUBG","SS",.3),C("SWEXC","FS",0),C("CLR","FS",0)),Any(C("SUBG","FF",1)),Any(C("DRAIN","FS",0)),Any(C("PBACK","FS",0)),Any(C("ROOF","FS",0),C("CFRAME","FS",0),C("MAS","FS",0)),Any(C("STORM","FS",0))}},
  {"PAVE",new[]{Any(C("SUBB","SS",.3),C("SUBG","SS",.3),C("ROOF","FS",0),C("CFRAME","FS",7),C("MAS","SS",.5),C("CLR","FS",0)),Any(C("SUBB","FF",1)),Any(C("ROOF","FS",0),C("CFRAME","FS",7)),Any(C("DRAIN","FS",0)),Any(C("PBACK","FS",0)),Any(C("STORM","FS",0))}},
  {"MARK",new[]{Any(C("PAVE","FS",0),C("ROOF","FS",0),C("CFRAME","FS",7),C("CLR","FS",0))}},
  {"LAND",new[]{Any(C("PAVE","SS",.5),C("ROOF","FS",0),C("CFRAME","FS",7),C("CLR","FS",0)),Any(C("PAVE","FF",1))}}};
 /* items of no known trade follow the work phase of their item no. */
 static readonly Cand[][] OtherByPhase={new Cand[0],new[]{C("CLR","FS",0)},new[]{C("CLR","FS",0)},new[]{C("EXC","SS",.3),C("CLR","FS",0)},new[]{C("CFRAME","SS",.5),C("CFTG","FS",0),C("CLR","FS",0)},
  new[]{C("MAS","SS",.3),C("CFRAME","FS",0),C("CLR","FS",0)},new[]{C("PLAST","SS",.5),C("MAS","SS",.5),C("CLR","FS",0)},new[]{C("PLAST","SS",.5),C("MAS","FS",0),C("CLR","FS",0)}};
 static int Number(string code,out string rest){string c=(code??"").Trim();int n=0,i=0;while(i<c.Length&&char.IsDigit(c[i])){n=n*10+(c[i]-'0');i++;}rest=c.Substring(i);return i==0?-1:n;}
 /* gravel of item 804(7): bedding under footings / tie beams (the automatic quantity) or a bed on the fill under a slab */
 static bool SlabGravel(string d){return d.Contains("slab")||d.Contains("floor")||d.Contains("on fill")||d.Contains("on grade")||d.Contains("walk")||d.Contains("other area");}
 static bool BeddingGravel(string d){return d.Contains("bedding")||d.Contains("footing")||d.Contains("tie beam")||d.Contains("foundation");}
 internal static string Trade(ScheduleRow r){string rest;int n=Number(r.Code,out rest);string d=(r.Description??"").ToLowerInvariant();string code=(r.Code??"").Trim().ToUpperInvariant();
  Func<string,bool> has=t=>d.Contains(t);
  if(n<0){/* general requirements A.x / B.x (written with or without the dots); survey, staking and soil investigation are site work */
   string k=new string(code.Where(c=>char.IsLetterOrDigit(c)||c=='(').ToArray());if(k.StartsWith("B19(")||k.StartsWith("B4(")||has("geotechnical")||has("staking")||has("soil investigation")||has("soil boring"))return "INVEST";
   return k.Length>1&&(k[0]=='A'||k[0]=='B')&&char.IsDigit(k[1])?"PRE":"OTHER";}
  if(n==800||n==801||n==100||n==101)return "CLR";if(n==802)return "SPOIL";if(n==102)return "SWEXC";if(n==104)return has("backfill")?"BACK":"SWEXC";if(n==105)return "SUBG";if(n==200||n==201||n==202)return "SUBB";
  if(n==103)return has("excavation")?"EXC":"FFILL";if(n==803)return "EXC";
  if(n==804){if(!has("gravel")&&!rest.StartsWith("(7)"))return "BACK";return SlabGravel(d)&&!BeddingGravel(d)?"GRAV":"GRAVF";}
  if(n==807)return "LAND";if(n==808||n==1049){return has("test")?"PTEST":"GIMP";}
  if(n==1052){if(has("test"))return "PTEST";if(has("furnished"))return "PILEF";return "PILE";}if(n==1717||n==400)return "PILE";
  if(n==1600)return "PEXC";if(n==1601||n==1061&&(has("pipeline")||has("backfill")))return "PBACK";if(n==1602||n==1603)return "PPIPE";if(n==1604)return "MECH";/* water treatment plant: packaged equipment */
  if(n==901)return "LEAN";if(n==902||n==404)return "RB";
  if(n==903){if(has("footing")||has("pile cap"))return "FW";if(has("deck")||has("suspended")||!has("formwork")&&(has("falsework")||has("scaffold")))return "FWS";return "FW";}
  if(n==900||n==405){if(rest.StartsWith("(5)")||has("seal concrete"))return "LEAN";if(has("footing")||has("pile cap"))return "CFTG";if(has("slab-on-fill")||has("slab on fill")||has("slab on grade")||has("slab-on-grade"))return "CSOF";
   if(has("stiffener")||has("lintel")||has("coping")||has("canopy")||has("countertop")||has("parapet"))return "CSEC";if(has("roof deck")||has("roofdeck")||has("roof slab")||has("gutter")&&has("roof"))return "CROOF";return "CFRAME";}
  if(n==1000)return has("wood")?"WPRES":"POIS";if(n==1034)return "VB";
  /* DPWH highway items: drainage 500-504, slope protection / walls 505-599, bridge works 401-415, road furniture 602-699 */
  if(n>=500&&n<=504)return "DRAIN";
  if(n>=505&&n<600){if(has("sheet pile"))return "PILE";if(has("culvert")||has("drain")||has("manhole")||has("inlet")||has("catch basin"))return "DRAIN";if(n==506||n==507)return "RWALL";if(n==511)return has("mattress")?"SITEW":"RWALL";return "SITEW";}
  if(n==401)return "RAIL";
  if(n>=402&&n<500&&n!=403){if(has("railing"))return "RAIL";if(has("paint"))return "PAINTM";if(has("waterproof"))return "WP";if(n==406||n==407||has("concrete"))return "CFRAME";return "STEEL";}
  if(n>=300&&n<400)return "PAVE";
  if(n>=600&&n<700){if(n==600||n==601||has("curb")||has("gutter")||has("sidewalk"))return "PAVE";if(n>=608&&n<=611||has("topsoil")||has("sod")||has("sprig")||has("tree")||has("grass"))return "LAND";return "MARK";}
  if(n==1046)return "MAS";
  if(n==1047||n==403){if(has("chemical")||has("epoxy")||has("adhesive")||has("post-installed")||has("post installed")||has("expansion anchor")||has("wedge anchor"))return "STEEL";/* drilled into hardened concrete with the steel */
   if(has("anchor"))return "ANCHOR";if(has("purlin")||has("sagrod")||has("brac")||has("turnbuckle")||has("tumbuckle"))return "PURL";return "STEEL";}if(n==1048)return "STEEL";
  if(n==1014)return "ROOF";if(n==1013)return has("roofing sheet")?"ROOF":"ROOFACC";if(n==1038)return "INSUL";if(n==1016)return "WP";
  if(n==1001){if(has("downspout")||has("storm")||has("roof drain"))return "STORM";if(has("canal")||has("catch basin")||has("sewer")||has("septic")||has("drainage")||has("manhole"))return "DRAIN";return "SANIT";}
  if(n==1002)return has("fixture")?"FIXT":"PLUMB";
  if(n==1100)return "ELEC";if(n==1101)return "ELEC2";if(n==1103)return "LIGHT";if(n>=1102&&n<1200)return "EQUIP";
  if(n==1202&&(has("sprinkler")||has("standpipe")||has("pipe")))return "FPROT";if(n==1203)return "LIFT";if(n>=1200&&n<1300)return "MECH";
  if(n==1027)return has("plaster")?"PLAST":"CLAD";if(n==1025||n==1039)return "CLAD";
  if(n==1003){if(rest.StartsWith("(5)")||rest.StartsWith("(6)"))return "STEEL";if(rest.StartsWith("(11)"))return "ROOFACC";if(rest.StartsWith("(15)"))return has("concrete")||has("precast")?"CSEC":"TRIM";
   if(rest.StartsWith("(2)"))return "PART";if(rest.StartsWith("(3)")||rest.StartsWith("(4)"))return "CAB";if(rest.StartsWith("(14)")||rest.StartsWith("(22)"))return "FITOUT";if(has("ceiling")||rest.StartsWith("(1)"))return "CEIL";return "OTHER";}
  if(n==1019)return has("ceiling")?"CEIL":"CLAD";
  if(n==1018)return has("countertop")||has("counter top")?"CAB":"TILE";if(n==1020)return "FCOAT";if(n==1021||n==1022)return has("epoxy")||has("coating")?"FCOAT":"FLOOR";
  if(n==1004)return "HW";if(n==1010&&(rest.StartsWith("(1)")||has("jamb")))return "JAMB";if(n==1012&&(has("glass block")||has("glassblock")))return "GLASSB";
  if(n>=1005&&n<=1012||n==1043)return "DOOR";if(n==1044)return "FITOUT";if(n==1051)return "RAIL";
  if(n==1032){if(has("floor"))return "FCOAT";if(has("automotive"))return "PAINTMF";if(rest.StartsWith("(1)c")||has("metal"))return "PAINTM";if(rest.StartsWith("(1)b")||rest.StartsWith("(2)")||rest.StartsWith("(4)")||has("wood"))return "PAINTW";return "PAINT";}
  return "OTHER";}
 /* trades that depend on the rest of the project: metal painting goes with the steel framing, or with the doors, grilles
    and railings when the project has no steel framing; 804(7) gravel is bedding under the footings when there are
    footings, otherwise a bed for the slab / other areas */
 static string Resolve(PdmNode nd,HashSet<string> present){
  if(nd.Trade=="PAINTM"&&!present.Contains("STEEL")&&!present.Contains("PURL"))return "PAINTMF";
  if(nd.Trade=="GRAVF"&&!BeddingGravel((nd.Row.Description??"").ToLowerInvariant())&&!present.Contains("LEAN")&&!present.Contains("CFTG"))return "GRAV";
  return nd.Trade;}
 /* started as early as possible, not moved by the levelling (work tied to the trade before it) */
 static readonly HashSet<string> NoLevel=new HashSet<string>{"PRE","INVEST","GRAVF","LEAN","CFTG","CROOF","PTEST","ANCHOR","INSUL","ROOFACC"};
 /* checks used by the tests: every rule refers only to earlier trades and every trade is named */
 internal static IEnumerable<string> RuleProblems(){foreach(var r in Rules){int i=Array.IndexOf(Order,r.Key);if(i<0)yield return "rule for unknown trade "+r.Key;foreach(var alt in r.Value)foreach(var c in alt){int j=Array.IndexOf(Order,c.Trade);if(j<0||j>=i)yield return r.Key+" refers to "+c.Trade;}}
  foreach(var t in Order)if(!Names.ContainsKey(t))yield return "no name for "+t;
  foreach(var alt in OtherByPhase)foreach(var c in alt)if(Array.IndexOf(Order,c.Trade)<0)yield return "phase fallback refers to "+c.Trade;foreach(var t in NoLevel)if(Array.IndexOf(Order,t)<0)yield return "unknown trade "+t+" not levelled";}
 static int LagDays(Cand c,int predDays){if(c.Type!="SS"||c.Lag>=1)return (int)Math.Round(c.Lag);return c.Lag<=0?0:Math.Max(1,(int)Math.Round(c.Lag*predDays));}

 /* nodes (scheduled items with a duration), links and the CPM times */
 /* milestones: when several items of one trade lead to several items of later trades by the same rule (type and lag), the
    relations go through one zero-duration milestone ("Reinforcing steel complete" ...) instead of every pair - the usual
    way to keep a network readable; the times are exactly the same: FS lag L: p -> M FS 0, M -> s FS L; FF lag L: p -> M
    FF 0, M -> s FF L; SS (lag depending on the predecessor): p -> M SS l(p), M -> s SS 0 */
 internal static List<ScheduleLink> Network(List<ScheduleRow> rows,out List<PdmNode> nodes,bool milestones=false){nodes=new List<PdmNode>();int id=0;
  foreach(var r in rows){id++;if(r.Days<=0)continue;nodes.Add(new PdmNode{Row=r,Id=id,Trade=Trade(r)});}
  var present=new HashSet<string>(nodes.Select(n=>n.Trade));foreach(var nd in nodes)nd.Trade=Resolve(nd,present);
  var byTrade=nodes.GroupBy(n=>n.Trade).ToDictionary(g=>g.Key,g=>g.ToList());var links=new List<ScheduleLink>();
  var uses=new Dictionary<string,KeyValuePair<Cand,List<PdmNode>>>();
  foreach(var nd in nodes){Cand[][] rules;if(nd.Trade=="OTHER"){int p=Math.Max(0,Math.Min(OtherByPhase.Length-1,SchedulePhases.Of(nd.Row.Code)));rules=new[]{OtherByPhase[p]};}else if(!Rules.TryGetValue(nd.Trade,out rules))rules=new Cand[0][];
   foreach(var rule in rules){var hit=rule.FirstOrDefault(c=>byTrade.ContainsKey(c.Trade));if(hit==null)continue;
    string key=hit.Trade+"|"+hit.Type+"|"+hit.Lag.ToString("R",CultureInfo.InvariantCulture);KeyValuePair<Cand,List<PdmNode>> u;if(!uses.TryGetValue(key,out u)){u=new KeyValuePair<Cand,List<PdmNode>>(hit,new List<PdmNode>());uses[key]=u;}u.Value.Add(nd);}}
  int mno=0;
  foreach(var u in uses.Values){var hit=u.Key;var preds=byTrade[hit.Trade];var succs=u.Value.Distinct().ToList();
   if(milestones&&preds.Count>=2&&succs.Count>=2&&preds.Count*succs.Count>preds.Count+succs.Count+1){mno++;
    string what=Names.ContainsKey(hit.Trade)?Names[hit.Trade]:hit.Trade;string cap=hit.Type=="SS"?what+" under way":what+" complete";
    var m=new PdmNode{Row=new ScheduleRow{Code="",Description=cap,Unit="",Days=0},Id=100000+mno,MilestoneNo=mno,Trade=hit.Trade};nodes.Add(m);
    foreach(var p in preds)links.Add(new ScheduleLink{From=p,To=m,Type=hit.Type,Lag=hit.Type=="SS"?LagDays(hit,p.Row.Days):0});
    foreach(var sn in succs)if(sn!=m)links.Add(new ScheduleLink{From=m,To=sn,Type=hit.Type,Lag=hit.Type=="SS"?0:LagDays(hit,0)});continue;}
   foreach(var sn in succs)foreach(var p in preds)if(p!=sn)links.Add(new ScheduleLink{From=p,To=sn,Type=hit.Type,Lag=LagDays(hit,p.Row.Days)});}
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
 internal static void Schedule(List<ScheduleRow> rows){List<PdmNode> nodes;var links=Network(rows,out nodes,false);int total=nodes.Count==0?0:nodes.Max(n=>n.EF);
  var load=new int[Math.Max(1,total)];var start=new Dictionary<PdmNode,int>();
  foreach(var nd in nodes){int d=nd.Row.Days,lo=0;foreach(var l in links.Where(x=>x.To==nd))lo=Math.Max(lo,Bound(l,d,start));int hi=Math.Max(lo,nd.LS);int men=ScheduleManpower.Labor(nd.Row);int best=lo;if(NoLevel.Contains(nd.Trade))hi=lo;
   if(men>0&&hi>lo){long bestKey=long.MaxValue;for(int s=lo;s<=hi;s++){int peak=0;long sq=0;for(int k=s;k<s+d&&k<load.Length;k++){int v=load[k]+men;peak=Math.Max(peak,v);sq+=(long)v*v-(long)load[k]*load[k];}
     long key=(long)peak*1000000000L+sq*100+(s-lo);if(key<bestKey){bestKey=key;best=s;}}}
   start[nd]=best;for(int k=best;k<best+d&&k<load.Length;k++)load[k]+=men;}
  foreach(var r in rows){r.Start=1;r.Finish=0;r.Critical=false;}
  foreach(var nd in nodes){nd.Row.Start=start[nd]+1;nd.Row.Finish=start[nd]+nd.Row.Days;nd.Row.Critical=nd.TF==0;nd.Row.Predecessors=string.Join(", ",links.Where(l=>l.To==nd).Select(l=>l.From.Id+" "+l.Label));}}
}
}
