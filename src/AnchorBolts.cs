using System;using System.Collections.Generic;using System.Globalization;using System.Linq;
namespace BeamScheduleFormatter {
/* Commercial anchor bolt sizes (owner, 19 November 2026: "Use this sizes sa anchor bolt" - Powersteel Anchor Bolt Specs Guide,
   A x B x C x D = diameter x length x hook x thread, inches). Inch bolts use the UNC tensile stress area and the heavy hex nut;
   metric M16-M36 stay available for other anchors. */
static class AnchorBolts {
 static readonly CultureInfo Inv=CultureInfo.InvariantCulture;
 internal sealed class Size {public string Dia;public double DiaMm,LengthIn,HookIn,ThreadIn;public string Name{get{return "Ø"+Dia+"\" x "+Fmt(LengthIn)+"\" (hook "+Fmt(HookIn)+"\", thread "+Fmt(ThreadIn)+"\")";}}}
 static string Fmt(double v){return v==.5?"1/2":v.ToString("0.##",Inv);}
 static readonly string[] Table={"3/8|6|1|0.5","3/8|8|1|0.5","3/8|10|1|0.5","3/8|12|1|0.5","1/2|8|2|4","1/2|10|2|4","1/2|12|2|4","1/2|18|4|4","5/8|8|2|4","5/8|10|2|4","5/8|12|3|4","5/8|18|4|6","5/8|20|4|6",
  "3/4|6|3|4","3/4|8|3|4","3/4|10|3|4","3/4|12|4|4","3/4|14|4|6","3/4|15|4|6","3/4|16|4|6","3/4|18|4|6","3/4|18|4|7","3/4|24|4|6","3/4|24|4|7","3/4|30|4|6","3/4|30|4|7","3/4|36|4|7","7/8|18|4|6",
  "1|12|4|4","1|18|4|6","1|24|4|6","1|36|4|6","1|36|4|7","1|36|4|12","1 1/4|36|5|12","1 1/4|42|6|7"};
 internal static double Inches(string f){double total=0;foreach(var part in f.Split(' ')){var q=part.Split('/');total+=q.Length==2?double.Parse(q[0],Inv)/double.Parse(q[1],Inv):double.Parse(q[0],Inv);}return total;}
 internal static readonly List<Size> Powersteel=Table.Select(t=>{var c=t.Split('|');return new Size{Dia=c[0],DiaMm=Inches(c[0])*25.4,LengthIn=double.Parse(c[1],Inv),HookIn=double.Parse(c[2],Inv),ThreadIn=double.Parse(c[3],Inv)};}).ToList();
 /* tensile stress area mm² and heavy hex nut width across flats mm */
 static readonly Dictionary<string,double[]> InchData=new Dictionary<string,double[]>{{"3/8",new[]{50.0,17.5}},{"1/2",new[]{91.5,22.2}},{"5/8",new[]{145.8,27.0}},{"3/4",new[]{215.5,31.8}},{"7/8",new[]{298.1,36.5}},{"1",new[]{391.0,41.3}},{"1 1/4",new[]{625.2,50.8}}};
 static readonly Dictionary<int,double[]> MetricData=new Dictionary<int,double[]>{{16,new[]{157.0,27}},{20,new[]{245.0,34}},{24,new[]{353.0,41}},{27,new[]{459.0,46}},{30,new[]{561.0,50}},{36,new[]{817.0,60}}};
 static double[] Data(double mm){foreach(var kv in InchData)if(Math.Abs(Inches(kv.Key)*25.4-mm)<.05)return kv.Value;foreach(var kv in MetricData)if(Math.Abs(kv.Key-mm)<.05)return kv.Value;throw new Exception("Use a Powersteel inch anchor bolt or an M16 to M36 anchor.");}
 internal static double StressArea(double mm){return Data(mm)[0];}
 internal static double NutFlats(double mm){return Data(mm)[1];}
 internal static double BearingArea(double mm){double f=NutFlats(mm);return Math.Sqrt(3)/2*f*f-Math.PI*mm*mm/4;}
 internal static string[] DiameterLabels{get{return InchData.Keys.Select(k=>k+" in ("+(Inches(k)*25.4).ToString("0.0",Inv)+" mm)").Concat(MetricData.Keys.Select(k=>"M"+k)).ToArray();}}
 internal static string Label(double mm){foreach(var k in InchData.Keys)if(Math.Abs(Inches(k)*25.4-mm)<.05)return k+" in ("+mm.ToString("0.0",Inv)+" mm)";return "M"+mm.ToString("0",Inv);}
 internal static double ParseDiameter(string label){label=(label??"").Trim();if(label.StartsWith("M"))return double.Parse(label.Substring(1),Inv);int i=label.IndexOf(" in");if(i<0)throw new Exception("Choose the anchor diameter.");return Inches(label.Substring(0,i))*25.4;}
 internal static string Short(double mm){foreach(var k in InchData.Keys)if(Math.Abs(Inches(k)*25.4-mm)<.05)return "Ø"+k+"\"";return "M"+mm.ToString("0",Inv);}
 /* what sticks out above the concrete: grout, plate, washer, heavy hex nut and two threads */
 internal static double Projection(double mm,double plateT,bool grout){return (grout?25:0)+plateT+Math.Max(3,.15*mm)+mm+6;}
 internal static double ThreadNeeded(double mm,double plateT){return plateT+Math.Max(3,.15*mm)+mm+6;}
}
}
