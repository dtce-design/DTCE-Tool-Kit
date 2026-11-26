using System;using System.Collections.Generic;using System.Linq;
namespace BeamScheduleFormatter {
/* NSCP 2015 Section 205 live loads (owner, 26 November 2026: "include all Live Loads common used in Government Buildings all spaces and
   Rooms, including Barangay Halls, Evacuation Centers, Birthing Center, Senior Citizen Center, Gymnasiums, Covered Court etc. summarize
   them per space. based on NSCP 2015"). Table 205-1 rows (uniform kPa, concentrated kN) and the spaces of each building type with the
   row that governs them. Check the rows against the printed code. */
sealed class LiveLoadRow {public string Id,Category,Description,Note="";public double Uniform=double.NaN,Concentrated=double.NaN;
 public string UniformText{get{return double.IsNaN(Uniform)?(Note!=""?"see note":"-"):Uniform.ToString("0.0#");}}public string ConcentratedText{get{return double.IsNaN(Concentrated)?"-":Concentrated.ToString("0.0#");}}}
sealed class LiveLoadSpace {public string Space,Row,Remark="";public double Override=double.NaN;}
static class NscpLiveLoads {
 static LiveLoadRow R(string id,string cat,string desc,double u,double c=double.NaN,string note=""){return new LiveLoadRow{Id=id,Category=cat,Description=desc,Uniform=u,Concentrated=c,Note=note};}
 const double N=double.NaN;
 /* Table 205-1 Minimum uniform and concentrated live loads; the concentrated load acts on 750 mm x 750 mm unless stated */
 internal static readonly LiveLoadRow[] Table={
  R("1a","1 Access floor systems","Office use",2.4,9.0),R("1b","1 Access floor systems","Computer use",4.8,9.0),
  R("2","2 Armories","",7.2),
  R("3a","3 Theaters, assembly areas and auditoriums","Fixed seats fastened to the floor",2.9),R("3b","3 Theaters, assembly areas and auditoriums","Lobbies",4.8),R("3c","3 Theaters, assembly areas and auditoriums","Movable seats",4.8),R("3d","3 Theaters, assembly areas and auditoriums","Platforms (assembly)",4.8),R("3e","3 Theaters, assembly areas and auditoriums","Stage areas",7.2),
  R("4","4 Bowling alleys, poolrooms and similar recreational areas","",3.6),
  R("5","5 Catwalks for maintenance access","",1.9,1.3),
  R("6","6 Cornices and marquees","",3.6),
  R("7","7 Dance halls and ballrooms","",4.8),
  R("8","8 Dining rooms and restaurants","",4.8),
  R("9","9 Exit facilities","Corridors serving 10 or more persons, exterior exit balconies, stairways, fire escapes and similar",4.8),
  R("10a","10 Garages","General storage and / or repair",4.8,N,"trucks and buses: by the actual wheel loads"),R("10b","10 Garages","Private or pleasure-type motor vehicle storage",1.9),
  R("11","11 Gymnasiums","Main floors and balconies",4.8),
  R("12a","12 Hospitals","Operating rooms, laboratories",2.9,4.5),R("12b","12 Hospitals","Private rooms",1.9,4.5),R("12c","12 Hospitals","Wards",1.9,4.5),R("12d","12 Hospitals","Corridors above ground floor",3.8,4.5),
  R("13a","13 Libraries","Reading rooms",2.9,4.5),R("13b","13 Libraries","Stack rooms",7.2,4.5),R("13c","13 Libraries","Corridors above ground floor",3.8,4.5),
  R("14a","14 Manufacturing","Light",6.0,9.0),R("14b","14 Manufacturing","Heavy",12.0,13.4),
  R("15a","15 Office use","Lobbies and ground floor corridors",4.8,9.0),R("15b","15 Office use","Offices",2.4,9.0),R("15c","15 Office use","Corridors above ground floor",3.8,9.0),R("15d","15 Office use","File and computer rooms",N,N,"designed for the heavier loads of the expected use"),
  R("16a","16 Penal institutions","Cell blocks",1.9),R("16b","16 Penal institutions","Corridors",4.8),
  R("17a","17 Printing plants","Press rooms",7.2,11.0),R("17b","17 Printing plants","Composing and linotype rooms",4.8,9.0),
  R("18a","18 Residential","One and two family: uninhabitable attics without storage",0.5),R("18b","18 Residential","One and two family: uninhabitable attics with storage",1.0),R("18c","18 Residential","One and two family: habitable attics and sleeping areas",1.4),R("18d","18 Residential","One and two family: all other areas",1.9),
  R("18e","18 Residential","Hotels and multifamily: private rooms and corridors serving them",1.9),R("18f","18 Residential","Hotels and multifamily: public rooms and corridors serving them",4.8),
  R("19","19 Restrooms","",N,N,"same as the occupancy served, need not exceed 2.4 kPa"),
  R("20","20 Reviewing stands, grandstands and bleachers","",4.8),
  R("21","21 Roof decks","",N,N,"same as the area served or the occupancy accommodated"),
  R("22a","22 Schools","Classrooms",1.9,4.5),R("22b","22 Schools","Corridors above ground floor",3.8,4.5),R("22c","22 Schools","Ground floor corridors",4.8,4.5),
  R("23","23 Scuttles, skylight ribs and accessible ceilings","",N,0.9),
  R("24","24 Sidewalks, vehicular driveways and yards subject to trucking","",12.0,35.6),
  R("25a","25 Stadiums and arenas","Bleachers",4.8),R("25b","25 Stadiums and arenas","Fixed seats fastened to the floor",2.9),
  R("26a","26 Stairs and exits","One and two family dwellings",1.9),R("26b","26 Stairs and exits","All others",4.8),
  R("27a","27 Storage warehouses","Light",6.0),R("27b","27 Storage warehouses","Heavy",12.0),
  R("28a","28 Stores","Retail, first floor",4.8,4.5),R("28b","28 Stores","Retail, upper floors",3.6,4.5),R("28c","28 Stores","Wholesale, all floors",6.0,4.5),
  R("29","29 Yards and terraces, pedestrians","",4.8),
  R("30a","30 Balconies (exterior)","",4.8),R("30b","30 Balconies (exterior)","One and two family, not over 9.3 m²",2.9),
  R("31","31 Walkways and elevated platforms (other than exitways)","",2.9),
  R("32","32 Elevator machine room grating","on 2,580 mm²",N,1.3)};
 internal static LiveLoadRow Row(string id){return Table.First(r=>r.Id==id);}
 /* Table 205-3 Minimum roof live loads, Method 1 (kPa, by tributary area) */
 internal static readonly string[][] RoofTable={
  new[]{"1 Flat or rise less than 4 in 12 (33 %); arch or dome with rise less than 1/8 of the span","1.00","0.75","0.60"},
  new[]{"2 Rise 4 in 12 to less than 12 in 12; arch or dome with rise 1/8 to less than 3/8 of the span","0.75","0.70","0.60"},
  new[]{"3 Rise 12 in 12 and greater; arch or dome with rise 3/8 of the span or greater","0.60","0.60","0.60"},
  new[]{"4 Awnings, except cloth covered","0.25","0.25","0.25"},new[]{"5 Greenhouses, lath houses and agricultural buildings","0.50","0.50","0.50"}};
 static LiveLoadSpace S(string space,string row,string remark=""){return new LiveLoadSpace{Space=space,Row=row,Remark=remark};}
 static LiveLoadSpace T(string space,double kpa,string remark){return new LiveLoadSpace{Space=space,Row="19",Override=kpa,Remark=remark};}
 const string Toilet="as the area served, not over 2.4 kPa";const string Roof="roof: Table 205-3 by slope and tributary area";
 internal static readonly Dictionary<string,LiveLoadSpace[]> Buildings=new Dictionary<string,LiveLoadSpace[]>{
  {"Barangay Hall",new[]{S("Offices (punong barangay, secretary, treasurer, staff)","15b"),S("Session hall (Sangguniang Barangay)","3c","movable seats"),S("Lupon / conference / multi-purpose room","3c","people gather: assembly, movable seats"),
   S("Lobby, waiting area and ground floor corridors","15a"),S("Corridors above ground floor","15c"),S("Records / file room","15d","use Light storage 6.0 kPa unless the files are lighter"),S("Barangay health station / clinic room","12b"),
   S("Day care / child development room","22a"),T("Toilets",2.4,Toilet),S("Stairs and exits","26b"),S("Roof deck (accessible)","21","as the use on the deck")}},
  {"Evacuation Center",new[]{S("Evacuation hall / family and sleeping areas","3c","crowded use: assembly, movable seats"),S("Women- and child-friendly space, breastfeeding room","3c"),S("Community kitchen","8","heavy equipment by its weight"),S("Dining area","8"),
   S("Relief goods storage","27a","stacked sacks of rice / water: check Heavy storage 12.0 kPa"),S("Clinic / first aid / isolation room","12c"),S("Administration / command post office","15b"),
   T("Toilets and bathing areas",2.4,Toilet),S("Corridors and exits","9"),S("Stairs","26b"),S("Roof deck used as refuge (flooding)","3c","as the use on the deck: assembly")}},
  {"Birthing Center / Lying-in Clinic",new[]{S("Delivery room","12a"),S("Labor room","12a"),S("Laboratory","12a"),S("Recovery / postpartum ward","12c"),S("Private patient rooms","12b"),S("Newborn care / nursery","12c"),
   S("Consultation / examination rooms","12b"),S("Pharmacy / supply room","27a"),S("Waiting area / lobby","15a"),S("Offices / nurse station","15b"),S("Corridors, ground floor","9"),S("Corridors above ground floor","12d"),T("Toilets",2.4,Toilet),S("Stairs and exits","26b")}},
  {"Senior Citizen Center",new[]{S("Activity / function hall","3c"),S("Exercise / dance area","7"),S("Dining area","8"),S("Kitchen","8"),S("Reading room / library","13a"),S("Clinic / therapy room","12b"),
   S("Resting / sleeping room","18e"),S("Offices","15b"),S("Lobby and ground floor corridors","15a"),S("Corridors above ground floor","15c"),T("Toilets",2.4,Toilet),S("Stairs and exits","26b")}},
  {"Gymnasium",new[]{S("Playing court / main floor","11"),S("Balconies","11"),S("Bleachers","20"),S("Fixed seats","25b"),S("Stage","3e"),S("Lobby / concourse","3b"),T("Locker and shower rooms",2.4,Toilet),
   S("Equipment storage","27a"),S("Offices","15b"),S("Catwalks for maintenance","5"),S("Stairs and exits","26b"),S("Roof","","Table 205-3 by slope and tributary area")}},
  {"Covered Court",new[]{S("Court floor","11","usually a slab on grade"),S("Bleachers","20"),S("Stage","3e"),S("Equipment storage","27a"),T("Toilets",2.4,Toilet),S("Catwalks for maintenance","5"),S("Roof","","Table 205-3 by slope and tributary area")}},
  {"Multi-Purpose Hall",new[]{S("Hall, movable seats","3c"),S("Hall, fixed seats","3a"),S("Stage","3e"),S("Lobby","3b"),S("Kitchen / pantry","8"),S("Storage","27a"),T("Toilets",2.4,Toilet),S("Stairs and exits","26b"),S("Roof","","Table 205-3 by slope and tributary area")}},
  {"Municipal / City / Provincial Hall",new[]{S("Offices","15b"),S("Session hall","3c"),S("Gallery with fixed seats","3a"),S("Lobby and ground floor corridors","15a"),S("Corridors above ground floor","15c"),
   S("Records / archives","15d","Light storage 6.0 kPa; compact shelving: Heavy 12.0 kPa"),S("Computer / server room","1b"),S("Conference rooms","3c"),S("Canteen","8"),T("Toilets",2.4,Toilet),S("Stairs and exits","26b"),S("Roof deck","21")}},
  {"School Building",new[]{S("Classrooms","22a"),S("Corridors above ground floor","22b"),S("Ground floor corridors","22c"),S("Library reading room","13a"),S("Library stack room","13b"),S("Science / computer laboratory","12a","laboratories"),
   S("Faculty / principal's office","15b"),S("Canteen","8"),S("Stage / assembly","3e"),T("Toilets",2.4,Toilet),S("Stairs and exits","26b")}},
  {"Rural Health Unit / Health Center",new[]{S("Consultation / treatment rooms","12b"),S("Laboratory","12a"),S("Ward / observation","12c"),S("Pharmacy / storage","27a"),S("Waiting area","15a"),S("Offices","15b"),
   S("Corridors above ground floor","12d"),S("Ground floor corridors","9"),T("Toilets",2.4,Toilet),S("Stairs and exits","26b")}},
  {"Child Development / Day Care Center",new[]{S("Activity / classroom","22a"),S("Indoor play area","3c","children gathering: assembly"),S("Sleeping room","18c"),S("Kitchen / dining","8"),S("Office","15b"),T("Toilets",2.4,Toilet),S("Stairs and exits","26b")}},
  {"Public Market",new[]{S("Market stalls, ground floor (retail)","28a"),S("Stalls, upper floors (retail)","28b"),S("Wholesale area","28c"),S("Cold storage / warehouse","27b"),S("Food court","8"),S("Aisles / walkways","9"),S("Loading area / driveway","24"),T("Toilets",2.4,Toilet)}},
  {"Police / Fire Station",new[]{S("Offices","15b"),S("Detention cells","16a"),S("Cell corridors","16b"),S("Barracks / sleeping quarters","18e"),S("Apparatus bay (fire trucks)","10a","fire trucks: by the actual wheel loads"),S("Garage (patrol cars)","10b"),
   S("Armory","2"),S("Lobby","15a"),T("Toilets and showers",2.4,Toilet),S("Stairs and exits","26b")}},
  {"Public Library",new[]{S("Reading rooms","13a"),S("Stack rooms","13b"),S("Corridors above ground floor","13c"),S("Lobby","15a"),S("Offices","15b"),T("Toilets",2.4,Toilet),S("Stairs and exits","26b")}}};
 /* the load of a space: the table row, or the space's own value */
 internal static double Uniform(LiveLoadSpace s){if(!double.IsNaN(s.Override))return s.Override;if(s.Row==""||s.Row=="21"||s.Row=="19")return double.NaN;var r=Row(s.Row);if(!double.IsNaN(r.Uniform))return r.Uniform;return s.Row=="15d"?6.0:double.NaN;}
 internal static double Concentrated(LiveLoadSpace s){if(s.Row==""||s.Row=="19")return double.NaN;return Row(s.Row).Concentrated;}
 /* f1 of Eq. 203-3 / 203-5: 1.0 for places of public assembly, live loads over 4.8 kPa and garages; 0.5 otherwise */
 internal static string F1(LiveLoadSpace s){double u=Uniform(s);if(s.Row==""||double.IsNaN(u))return "-";var cat=Row(s.Row).Category;bool one=cat.StartsWith("3 ")||cat.StartsWith("7 ")||cat.StartsWith("11 ")||cat.StartsWith("20 ")||cat.StartsWith("25 ")||cat.StartsWith("10 ")||u>4.8;return one?"1.0":"0.5";}
 /* live load reduction (Section 205.5): not for places of public assembly, not for live loads over 4.8 kPa (except as allowed) */
 internal static string Reducible(LiveLoadSpace s){if(s.Row=="")return "-";var cat=Row(s.Row).Category;if(cat.StartsWith("3 ")||cat.StartsWith("7 ")||cat.StartsWith("11 ")||cat.StartsWith("20 ")||cat.StartsWith("25 "))return "No (assembly)";double u=Uniform(s);if(u>4.8)return "No (over 4.8 kPa)";return double.IsNaN(u)?"-":"Yes";}
 internal static string Basis(LiveLoadSpace s){if(s.Row=="")return "Table 205-3 (roof)";var r=Row(s.Row);return "Table 205-1, "+r.Category+(r.Description!=""?" - "+r.Description:"");}
}
}
