using System;using System.Linq;using System.Reflection;using System.Windows.Forms;using BeamScheduleFormatter;
/* v6.0.44: plumbing fixtures with automatic accessories (owner, 21 November 2026) */
class PlumbingFixtureTests {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Public;
 [STAThread]static void Main(){AccessCodes.Granted=true;var pow=new ProgramOfWorksForm();var t=typeof(ProgramOfWorksForm);
  var item=t.GetField("fixtureGrid",F).GetValue(pow);var g=(DataGridView)item.GetType().GetField("Grid",F).GetValue(item);
  Check(FixtureCatalog.AllItems.Length>=80&&FixtureCatalog.AllItems.Contains("5gpm Grease Trap"),"the owner's list is in the catalogue ("+FixtureCatalog.AllItems.Length+")");
  Action<string,string,string,string> add=(id,loc,name,qty)=>{int r=g.Rows.Add();g.Rows[r].Cells[0].Value=id;g.Rows[r].Cells[1].Value=loc;g.Rows[r].Cells[2].Value=FixtureCatalog.CategoryOf(name);g.Rows[r].Cells[3].Value=name;g.Rows[r].Cells[4].Value=qty;g.Rows[r].Cells[5].Value=true;};
  add("F1","Toilet 1","One Piece Dual Flush Push Button Rimless Water Closet","2");add("F2","Toilet 1","Stainless Steel Bidet Spray Supplied with Flexible Hose and Holder","1");
  add("F3","Men's toilet","Wall-Mount Back Inlet Urinal Supplied with Spreader","3");add("F4","Toilet 1","Round Vessel Type Wash Basin","2");add("F5","Kitchen","805x435x170mm Crown Stainless Steel 304 Single Bowl Kitchen Sink with Drainboard","1");
  add("F6","Toilet 2","Floor-Mount Concealed Cistern Rimless Water Closet","1");
  t.GetMethod("Recalculate",F,null,Type.EmptyTypes,null).Invoke(pow,null);
  var m=((System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string,int>>)t.GetMethod("FixtureMaterials",F).Invoke(pow,null)).ToDictionary(x=>x.Key,x=>x.Value);
  Func<string,int> n=k=>m.ContainsKey(k)?m[k]:0;
  Check(n(FixtureCatalog.UrinalValve)==3,"each urinal brings its 2.80 LPF flush valve");
  Check(n(FixtureCatalog.TwoWay)==1,"the bidet brings a two-way angle valve");
  Check(n(FixtureCatalog.OneWay)==1+2+1,"one-way angle valves: water closets 2 - 1 taken over by the bidet, + 2 basins, + 1 concealed-cistern closet = "+n(FixtureCatalog.OneWay));
  Check(n(FixtureCatalog.TallBasinFaucet)==2&&n(FixtureCatalog.PopUp)==2&&n(FixtureCatalog.PTrap)==3,"vessel basins: elongated faucet, pop-up and P-trap; the sink also has a P-trap");
  Check(n(FixtureCatalog.SinkFaucet)==1&&n(FixtureCatalog.CisternPanel)==1,"kitchen sink faucet; concealed cistern push-button panel");
  var bill=(System.Collections.Generic.List<BillQuantity>)t.GetMethod("FixtureQuantities",F).Invoke(pow,null);Check(bill.Count==1&&bill[0].PayCode=="1002(4)"&&bill[0].Unit=="ls"&&bill[0].Sources.Any(s=>s.Description==FixtureCatalog.UrinalValve&&s.Quantity==3),"one Plumbing Fixture (ls) line lists every item");
  g.Rows[2].Cells[5].Value=false;t.GetMethod("Recalculate",F,null,Type.EmptyTypes,null).Invoke(pow,null);m=((System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string,int>>)t.GetMethod("FixtureMaterials",F).Invoke(pow,null)).ToDictionary(x=>x.Key,x=>x.Value);Check(!m.ContainsKey(FixtureCatalog.UrinalValve),"untick Automatic accessories to enter them yourself");
  var xml=new System.Xml.XmlDocument();Console.WriteLine("PASS: "+checks+" plumbing fixture checks");Environment.Exit(0);}
}
