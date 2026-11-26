using System;using System.Linq;using BeamScheduleFormatter;
/* Live Loads (NSCP 2015) per space of government buildings */
class LiveLoadsTests {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 [STAThread]static void Main(){
  foreach(var b in NscpLiveLoads.Buildings)foreach(var s in b.Value)Check(s.Row==""||NscpLiveLoads.Table.Any(r=>r.Id==s.Row),"row of "+b.Key+" / "+s.Space+": "+s.Row);
  Check(NscpLiveLoads.Table.Select(r=>r.Id).Distinct().Count()==NscpLiveLoads.Table.Length,"row ids unique");
  foreach(var name in new[]{"National Government Office (central / regional / district)","Barangay Hall","Evacuation Center","Birthing Center / Lying-in Clinic","Senior Citizen Center","Gymnasium","Covered Court"})Check(NscpLiveLoads.Buildings.ContainsKey(name),"building type "+name);
  var off=NscpLiveLoads.Row("13c");Check(NscpLiveLoads.Table.Select(r=>r.Category.Split(' ')[0]).Distinct().Count()==23,"23 categories as printed");Check(NscpLiveLoads.Row("13a").Uniform==2.9&&NscpLiveLoads.Row("9b").Uniform==2.4&&NscpLiveLoads.Row("12a").Uniform==6.0&&NscpLiveLoads.Row("22b").Concentrated==13.4&&NscpLiveLoads.Row("15b").Uniform==2.9,"printed values: call centers 2.9, public parking 2.4, light manufacturing 6.0, wholesale 13.4 kN, exterior balconies 2.9");Check(off.Uniform==2.4&&off.Concentrated==9.0,"offices 2.4 kPa / 9.0 kN");Check(NscpLiveLoads.Row("19a").Uniform==1.9&&NscpLiveLoads.Row("19a").Concentrated==4.5,"classrooms 1.9 kPa / 4.5 kN");
  var gym=NscpLiveLoads.Buildings["Gymnasium"].First(s=>s.Space.StartsWith("Playing"));Check(NscpLiveLoads.Uniform(gym)==4.8&&NscpLiveLoads.F1(gym)=="1.0"&&NscpLiveLoads.Reducible(gym).StartsWith("No"),"gym floor 4.8, f1 1.0, not reducible");
  var ward=NscpLiveLoads.Buildings["Birthing Center / Lying-in Clinic"].First(s=>s.Space.StartsWith("Recovery"));Check(NscpLiveLoads.Uniform(ward)==1.9&&NscpLiveLoads.F1(ward)=="0.5"&&NscpLiveLoads.Reducible(ward)=="Yes","ward 1.9, f1 0.5, reducible");
  var store=NscpLiveLoads.Buildings["Evacuation Center"].First(s=>s.Space.StartsWith("Relief"));Check(NscpLiveLoads.Uniform(store)==6.0&&NscpLiveLoads.F1(store)=="1.0"&&NscpLiveLoads.Reducible(store).StartsWith("No"),"storage 6.0 > 4.8: f1 1.0, not reducible");
  var wc=NscpLiveLoads.Buildings["Barangay Hall"].First(s=>s.Space=="Toilets");Check(NscpLiveLoads.Uniform(wc)==2.4,"toilets 2.4");
  System.Windows.Forms.Application.SetUnhandledExceptionMode(System.Windows.Forms.UnhandledExceptionMode.CatchException);AccessCodes.Granted=true;
  using(var form=new LiveLoadsForm()){form.Show();System.Windows.Forms.Application.DoEvents();var rows=form.Rows().ToList();int all=rows.Count;Check(all>100&&all<NscpLiveLoads.Buildings.Values.Sum(v=>v.Length)&&rows.Select(r=>r[0]+"|"+r[5]).Distinct().Count()==all,"one list, each space once (owner: huwag igroup per building): "+all);Check(rows.Select(r=>r[0]).SequenceEqual(rows.Select(r=>r[0]).OrderBy(x=>x,StringComparer.OrdinalIgnoreCase)),"sorted by space");
   int toilets=form.SearchSpaces("toilet");Check(toilets>=2&&toilets<all,"search 'toilet': "+toilets);Check(form.SearchSpaces("bleachers 4.8")>=1&&form.SearchSpaces("bleachers 4.8")<toilets+5,"search with two words");form.SearchSpaces("toilet");var shown=form.Rows().Where(r=>string.Join(" ",r).IndexOf("toilet",StringComparison.OrdinalIgnoreCase)>=0).ToList();
   Check(!form.Rows().Any(form.Included),"every space starts unchecked (owner: ako ang mag check)");
   form.SetIncluded(shown,true);Check(form.Rows().Count(form.Included)==shown.Count&&!form.Rows().Where(r=>r[0]=="Classrooms").Any(form.Included),"only the checked spaces go to the report");form.SetIncluded(shown,false);Check(!form.Rows().Any(form.Included),"unchecked again");
   form.SearchSpaces("");form.Close();}
  Console.WriteLine("LiveLoadsTests passed: "+checks+" checks");}
}
