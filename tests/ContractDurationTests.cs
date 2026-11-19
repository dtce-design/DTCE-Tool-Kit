using System;using System.Collections.Generic;using System.Linq;using BeamScheduleFormatter;
// Owner (19 November 2026): Contract Duration tab, checked against the owner's "COMPUTATION OF ESTIMATED PROJECT DURATION" sheet.
class ContractDurationTests {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 static void Main(){
  /* the owner's PAGASA table (rainfall above 7.5 mm), years 2020-2026 */
  var rec=new Dictionary<int,int?[]>{
   {2020,new int?[]{null,null,null,null,null,null,null,null,null,null,9,13}},
   {2021,new int?[]{8,3,2,0,0,7,6,4,4,6,5,5}},{2022,new int?[]{3,2,1,1,6,4,5,12,5,15,4,16}},{2023,new int?[]{5,7,0,1,3,3,9,5,5,6,8,7}},
   {2024,new int?[]{5,0,0,0,2,2,6,5,11,9,13,8}},{2025,new int?[]{10,6,2,3,4,0,11,7,11,8,14,10}},{2026,new int?[]{9,1,0,1,0,2,9,8,null,null,null,null}}};
  int[] used;var avg=ContractDurationCalc.Averages(rec,out used);
  Check(avg.SequenceEqual(new[]{6,3,1,1,3,2,8,7,7,9,9,9}),"monthly averages of the last five years: "+string.Join(",",avg));
  /* the sheet: start May 3, 2027, 72 working days, holidays in range: Eid'l Adha May 17 and June 12 -> Aug 17, 2027, 107 days */
  var holidays=new[]{new DateTime(2027,5,1),new DateTime(2027,5,17),new DateTime(2027,6,12)};
  var r=ContractDurationCalc.Compute(new DateTime(2027,5,3),72,holidays,avg);
  Check(r.End==new DateTime(2027,8,17),"end date "+r.End.ToString("yyyy-MM-dd"));
  Check(r.Sundays==15&&r.Holidays==2&&r.RainyDays==18&&r.ContractDuration==107,"totals "+r.Sundays+"/"+r.Holidays+"/"+r.RainyDays+"/"+r.ContractDuration);
  var m=r.Months.Select(x=>new[]{x.Working,x.Sundays,x.Holidays,x.Rainy,x.RevisedRainy,x.TotalDays,x.Weeks}).ToList();
  Check(m.Count==4&&m[0].SequenceEqual(new[]{21,4,1,3,3,29,5})&&m[1].SequenceEqual(new[]{23,4,1,2,2,30,5})&&m[2].SequenceEqual(new[]{20,4,0,8,7,31,5})&&m[3].SequenceEqual(new[]{8,3,0,7,6,17,3}),"month rows as in the sheet");
  /* the app's national holiday list gives the same result as the sheet */
  var withStandard=ContractDurationCalc.Compute(new DateTime(2027,5,3),72,ContractDurationCalc.Standard(2027,2028).Select(h=>h.Date),avg);Check(withStandard.End==new DateTime(2027,8,17)&&withStandard.ContractDuration==107,"national holiday list: "+withStandard.End.ToString("yyyy-MM-dd")+" / "+withStandard.ContractDuration);
  /* standard holidays: fixed, Easter-based and National Heroes Day */
  var std=ContractDurationCalc.Standard(2027,2027);
  Check(std.Any(h=>h.Date==new DateTime(2027,3,25)&&h.Name=="Maundy Thursday")&&std.Any(h=>h.Date==new DateTime(2027,3,26)&&h.Name=="Good Friday")&&std.Any(h=>h.Date==new DateTime(2027,8,30)&&h.Name=="National Heroes Day")&&std.Any(h=>h.Date==new DateTime(2027,12,30))&&std.Any(h=>h.Date==new DateTime(2027,6,12)),"national holidays of 2027");
  Check(ContractDurationCalc.Easter(2026)==new DateTime(2026,4,5)&&ContractDurationCalc.Easter(2028)==new DateTime(2028,4,16),"Easter dates");
  /* a holiday on a Sunday is not counted twice */
  var sunday=ContractDurationCalc.Months(new DateTime(2027,6,1),new DateTime(2027,6,30),new HashSet<DateTime>{new DateTime(2027,6,13)},new int[12]);Check(sunday[0].Sundays==4&&sunday[0].Holidays==0,"holiday on a Sunday");
  int[] u2;Check(ContractDurationCalc.Averages(ContractDurationCalc.AparriRecords(),out u2).SequenceEqual(avg),"built-in Aparri records give the sheet averages");
  Console.WriteLine("PASS: "+checks+" contract duration checks");}
}
