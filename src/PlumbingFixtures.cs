using System;using System.Collections.Generic;using System.Globalization;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* Plumbing fixtures in Plumbing Works (owner, 21 November 2026: "sa plumbing works include plumbing fixture ... paki check if pwede
   automatic yung kapag nag-input ako ng urinal kasama na yung flush valve etcetera, if may kasamang bidet automatic na yung two way
   angle valve"). One row per fixture: category, item from the owner's list, quantity. With "Automatic accessories?" ticked the
   fittings that come with it are added:
   - water closet: one-way angle valve; concealed-cistern closet: also the concealed cistern front panel push button;
   - urinal: exposed 2.80 LPF urinal flush valve;
   - bidet spray: two-way angle valve, which takes the place of the one-way angle valve of a water closet at the same location;
   - wash basin / vanity: single-line basin faucet (elongated for vessel basins), brass P-trap, pop-up with overflow, one-way angle valve;
   - kitchen sink: single-line kitchen sink faucet with angle valve and flexible hose, brass P-trap.
   The Bill of Quantities gets one Plumbing Fixture (1002(4), ls) line with every item and its number. */
static class FixtureCatalog {
 internal const string OneWay="One Way Angle Valve",TwoWay="Two Way Angle Valve",PTrap="Brass P-trap",PopUp="Push Button Pop Up w/ Overflow Function",
  BasinFaucet="Single Line Basin Faucet Supplied with Complete Fittings",TallBasinFaucet="Single Line Elongated Basin Faucet Supplied with Complete Fittings",
  SinkFaucet="Single Line Kitchen Sink Faucet Supplied with Angle Valve and Flexible Hose",UrinalValve="Exposed Type 2.80 LPF Urinal Flush Valve Supplied with Bushing Reducer",
  CisternPanel="Frameless Concealed Cistern Dual Flush with Chrome Finish Front Panel Push Button";
 internal static readonly KeyValuePair<string,string[]>[] Items={
  K("Water closet","One Piece Dual Flush Push Button Rimless Water Closet","Close Coupled Push Button Rimless Water Closet","Floor-Mount Concealed Cistern Rimless Water Closet"),
  K("Water closet flushing","AC/DC Concealed Water Closet Flush Sensor with Manual Override Button",CisternPanel),
  K("Urinal","Wall-Mount Back Inlet Urinal Supplied with Spreader","Wall-Mount Top Inlet Urinal Supplied with Spreader"),
  K("Urinal flushing","AC/DC Concealed Urinal Flush Sensor with Manual Override Button",UrinalValve,"Exposed Type 1.9 LPF Urinal Push Valve"),
  K("Hose bibb / bib tap","Brass Chrome Polished Hose Bibb","Brass Hose Bibb","Single Line Wall Mounted Bib Tap","Single Line Wall Mounted Elongated Bib Tap"),
  K("Faucet",BasinFaucet,TallBasinFaucet,SinkFaucet,"Single Line Bath Faucet with Hand Shower","Wash Basin Mixing Faucet Supplied with Complete Fittings","Elongated Wash Basin Mixing Faucet Supplied with Complete Fittings","Kitchen Sink Mixing Faucet Supplied with Angle Valve and Flexible Hose","Bath Mixing Faucet with Hand Shower","Bath Mixing Faucet Supplied with Hand Shower and Shower Rail with Holder"),
  K("Bidet","Stainless Steel Bidet Spray Supplied with Flexible Hose and Holder"),
  K("Wash basin","Rectangular Vessel Type Wash Basin","Square Vessel Type Wash Basin","Round Vessel Type Wash Basin","35mm Single Hole Vessel Type Wash Basin (L600 x W420 x H130mm)","35mm Single Hole Vessel Type Wash Basin (L500 x W420 x H130mm)","35mm Single Hole Countertop Wash Basin (L556 x W403 x H157mm)","35mm Single Hole Countertop Wash Basin (L565 x W399 x H157mm)","35mm Single-Hole Wall-Hung Wash Basin (L585 x W425 x H190mm)","35mm Single-Hole Wall-Hung Wash Basin with Short Pedestal (L585 x W425 x H500mm)","35mm Single-Hole Wall-Hung Wash Basin with Long Pedestal (L585 x W425 x H782mm)","Wall-Hung Vanity Cabinet with Ceramic Counter (L580 x W410 x H590 mm)"),
  K("Trap / pop-up / valve",PTrap,PopUp,OneWay,TwoWay),
  K("Bathroom accessory","Towel Bar","Toilet Paper Holder with Cover","Grille-Type Soap Holder","Stainless Steel Floor Drain (100x100mm)"),
  K("Grab bar","300mm Anti-Bacterial ABS Straight Type Grab Bar","400mm Anti-Bacterial ABS Straight Type Grab Bar","450mm Anti-Bacterial ABS Straight Type Grab Bar","600mm Anti-Bacterial ABS Straight Type Grab Bar","450x450mm Anti-Bacterial ABS L-Type Grab Bar","600x600mm Anti-Bacterial ABS L-Type Grab Bar","500x500mm Anti-Bacterial ABS 120° Type Grab Bar","Anti-Bacterial ABS Wall-to-Wall Type Grab Bar","Anti-Bacterial ABS Floor-to-Wall, L-Type, Grab Bar","Anti-Bacterial ABS 3U Urinal Grab Bar with Leg","Anti-Bacterial ABS H-Type Urinal Grab Bar","Anti-Bacterial ABS U2-Type Grab Bar","700mm Anti-Bacterial ABS Flip-Up Type Grab Bar","Anti-Bacterial ABS Flip-Up Type Grab Bar with Leg"),
  K("Kitchen sink","805x435x170mm Crown Stainless Steel 304 Single Bowl Kitchen Sink with Drainboard","860x440x160mm Crown Stainless Steel 304 Single Bowl Kitchen Sink with Drainboard","1180x510x150mm Crown Stainless Steel 304 Double Bowl Kitchen Sink with Drainboard","510x510x200x0.80mm Franke Bell Series Stainless Steel 304 Single Bowl Kitchen Sink","790x480x180x0.80mm Franke Bell Series Stainless Steel 304 Double Bowl Kitchen Sink"),
  K("Vanity mirror","14\" x 18\" x 4mm thk Wall Vanity Mirror with backplate, tox and screw","18\" x 24\" x 4mm thk Wall Vanity Mirror with backplate, tox and screw","20\" x 30\" x 4mm thk Wall Vanity Mirror with backplate, tox and screw","24\" x 36\" x 4mm thk Wall Vanity Mirror with backplate, tox and screw"),
  K("Hand dryer / soap dispenser","Automatic Jet Hand Dryer (AC220V 50/60Hz) with DC Brushless Motor, Double HEPA Filter System and UV Light","Automatic Jet Hand Dryer (AC220V 50/60Hz) with Brush Motor and HEPA Filter System","Automatic Jet Hand Dryer Mini (AC220V 50/60Hz) with Carbon Brush Motor and HEPA Filter","ABS Plastic Mini Automatic Hand Dryer (AC220V 50/60Hz) with Brush Motor","Brushed Stainless Steel Automatic Hand Dryer (AC220V 50/60Hz) with Carbon Brush Motor","Brushed Stainless Steel Automatic Hand Dryer (AC220V 50/60Hz) with Carbon Brush Motor and HEPA Filter","Automatic Soap Dispenser, Battery Powered: DC 4.5V, AA x 2, 900 mL Capacity","Automatic Soap Dispenser, Battery Powered: DC 4.5V, AA x 6, 700 mL Capacity","Wall Mount Soap Dispenser, 350 mL Capacity"),
  K("Water heater","3.5kW Single-point Water Heater with 3Way Shower Head","3.5kW Single-point Water Heater with 1Way Shower Head","4.5kW Single-point Water Heater with 1Way Shower Head","6.0kW Multi-point Water Heater","8.0kW Multi-point Water Heater","10.0kW Multi-point Water Heater"),
  K("Grease trap","5gpm Grease Trap")};
 static KeyValuePair<string,string[]> K(string c,params string[] items){return new KeyValuePair<string,string[]>(c,items);}
 internal static string[] Categories{get{return Items.Select(i=>i.Key).ToArray();}}
 internal static string[] AllItems{get{return Items.SelectMany(i=>i.Value).ToArray();}}
 internal static string CategoryOf(string item){foreach(var c in Items)if(c.Value.Contains(item))return c.Key;return "";}
 /* the fittings that come with one fixture (before the bidet takes over a water closet's one-way angle valve) */
 internal static List<KeyValuePair<string,int>> Accessories(string item){var list=new List<KeyValuePair<string,int>>();string c=CategoryOf(item);Action<string> add=a=>list.Add(new KeyValuePair<string,int>(a,1));
  if(c=="Water closet"){add(OneWay);if(item.Contains("Concealed Cistern"))add(CisternPanel);}
  else if(c=="Urinal")add(UrinalValve);
  else if(c=="Bidet")add(TwoWay);
  else if(c=="Wash basin"){add(item.Contains("Vessel")?TallBasinFaucet:BasinFaucet);add(PTrap);add(PopUp);add(OneWay);}
  else if(c=="Kitchen sink"){add(SinkFaucet);add(PTrap);}
  return list;}
}
sealed class FixtureLine {public string Id,Location,Item;public int Quantity;public readonly Dictionary<string,int> Accessories=new Dictionary<string,int>();}
partial class ProgramOfWorksForm {
 ExtraWorkGrid fixtureGrid;readonly List<FixtureLine> fixtures=new List<FixtureLine>();
 void AddPlumbingFixtures(){
  fixtureGrid=NewExtraGrid("PlumbingFixtures","Plumbing Fixtures","Plumbing Works",new[]{"Fixture ID","Location / toilet","Category","Fixture / material","Qty","Automatic accessories?","Accessories added","Status"},6,new Dictionary<int,string>{{4,"1"},{5,"True"}},
   "One row per fixture or material. Choose the category, then the item, and the quantity. With Automatic accessories? ticked the fittings that come with it are added: water closet - one-way angle valve (concealed cistern: also the front panel push button); urinal - 2.80 LPF exposed flush valve; bidet spray - two-way angle valve, replacing the one-way angle valve of a water closet at the same location; wash basin - basin faucet (elongated for vessel basins), P-trap, pop-up with overflow and one-way angle valve; kitchen sink - kitchen sink faucet with angle valve and flexible hose and P-trap. Untick it to enter the fittings yourself. The Bill of Quantities gets one Plumbing Fixture (ls) line listing every item and number.");
  var g=fixtureGrid.Grid;ExtraChoice(g,2,FixtureCatalog.Categories);ExtraChoice(g,3,FixtureCatalog.AllItems);ExtraCheck(g,5);g.Columns[1].Width=170;g.Columns[2].Width=190;g.Columns[3].Width=430;g.Columns[6].Width=430;g.Columns[6].DefaultCellStyle.WrapMode=DataGridViewTriState.True;g.DataError+=(s,e)=>{e.ThrowException=false;};
  /* the item list follows the category; choosing an item sets its category */
  g.CellValueChanged+=(s,e)=>{if(filling||e.RowIndex<0)return;var row=g.Rows[e.RowIndex];if(e.ColumnIndex==3){string c=FixtureCatalog.CategoryOf(Cell(row,3));if(c!=""&&Cell(row,2)!=c){bool before=filling;filling=true;try{row.Cells[2].Value=c;}finally{filling=before;}}}PrepareFixtureItems(row);};}
 void PrepareFixtureItems(DataGridViewRow row){var cell=row.Cells[3] as DataGridViewComboBoxCell;if(cell==null)return;var cat=FixtureCatalog.Items.FirstOrDefault(i=>i.Key==Cell(row,2));var names=cat.Value??FixtureCatalog.AllItems;string current=Cell(row,3);cell.Items.Clear();cell.Items.AddRange(names);if(current!=""&&!names.Contains(current))cell.Items.Add(current);}
 void RecalculatePlumbingFixtures(){fixtures.Clear();if(fixtureGrid==null)return;var g=fixtureGrid.Grid;var ids=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  foreach(DataGridViewRow row in g.Rows){if(row.IsNewRow)continue;row.Tag=null;row.Cells[6].Value="";row.Cells[7].Value="";if(Empty(row))continue;
   try{if(Cell(row,0)==""||!ids.Add(Cell(row,0)))throw new Exception("Enter a unique fixture ID.");if(Cell(row,3)=="")throw new Exception("Choose the fixture / material.");int qty=Count(row,4);
    var line=new FixtureLine{Id=Cell(row,0),Location=Cell(row,1).Trim(),Item=Cell(row,3),Quantity=qty};bool auto=Cell(row,5)==""||bool.Parse(Cell(row,5));
    if(auto)foreach(var a in FixtureCatalog.Accessories(line.Item))line.Accessories[a.Key]=(line.Accessories.ContainsKey(a.Key)?line.Accessories[a.Key]:0)+a.Value*qty;
    fixtures.Add(line);row.Tag=line;}
   catch(Exception ex){row.Cells[7].Value=ex.Message;errors.Add("Plumbing fixture "+Cell(row,0)+": "+ex.Message);}}
  /* a bidet's two-way angle valve feeds the water closet beside it: the closet's one-way angle valve at the same location is not needed */
  foreach(var bidets in fixtures.Where(f=>FixtureCatalog.CategoryOf(f.Item)=="Bidet"&&f.Accessories.ContainsKey(FixtureCatalog.TwoWay)).GroupBy(f=>f.Location,StringComparer.OrdinalIgnoreCase)){int take=bidets.Sum(b=>b.Accessories[FixtureCatalog.TwoWay]);
   foreach(var wc in fixtures.Where(f=>FixtureCatalog.CategoryOf(f.Item)=="Water closet"&&string.Equals(f.Location,bidets.Key,StringComparison.OrdinalIgnoreCase)&&f.Accessories.ContainsKey(FixtureCatalog.OneWay))){if(take<=0)break;int n=Math.Min(take,wc.Accessories[FixtureCatalog.OneWay]);wc.Accessories[FixtureCatalog.OneWay]-=n;take-=n;if(wc.Accessories[FixtureCatalog.OneWay]==0)wc.Accessories.Remove(FixtureCatalog.OneWay);}}
  foreach(DataGridViewRow row in g.Rows){var line=row.Tag as FixtureLine;if(line==null)continue;row.Cells[6].Value=line.Accessories.Count==0?"-":string.Join("; ",line.Accessories.Select(a=>a.Value+" x "+a.Key));row.Cells[7].Value="Estimated";}}
 /* every item with its number: the fixtures and the fittings added with them */
 internal List<KeyValuePair<string,int>> FixtureMaterials(){var totals=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);Action<string,int> add=(n,q)=>{if(q>0)totals[n]=(totals.ContainsKey(n)?totals[n]:0)+q;};
  foreach(var f in fixtures){add(f.Item,f.Quantity);foreach(var a in f.Accessories)add(a.Key,a.Value);}return totals.OrderBy(t=>Array.IndexOf(FixtureCatalog.AllItems,t.Key)<0?999:Array.IndexOf(FixtureCatalog.AllItems,t.Key)).ToList();}
 internal List<BillQuantity> FixtureQuantities(){var list=new List<BillQuantity>();var items=FixtureMaterials();if(items.Count==0)return list;
  var ls=new BillQuantity{Section="Plumbing Works",Description="Plumbing Fixture",Unit="ls",Quantity=1,PayCode="1002(4)",PayDescription="Plumbing Fixture"};
  foreach(var i in items)ls.Sources.Add(new BillQuantity{Description=i.Key,Quantity=i.Value,Unit="pcs"});list.Add(ls);return list;}
}
}
