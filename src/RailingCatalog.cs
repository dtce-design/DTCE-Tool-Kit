using System;using System.Collections.Generic;using System.Globalization;using System.Linq;
namespace BeamScheduleFormatter {
// Railing sections from the Regan 2026 Product Handbook (owner upload, 17 November 2026), full tables:
// heavy gauge ERW pipe (printed p.15), light gauge TRI-R pipe (p.16), Strux lightweight tube (p.17), seamless
// Schedule 40 / 80 / 160 pipe (p.18-20), square tube
// (p.22-23), rectangular tube (p.24) and flat bar (p.12-14). The handbook has no plain round bar or square bar, so
// those stay computed from 7850 kg/m3. Kind values match SteelPainting so the painted perimeter is known.
static class RailingCatalog {
 static List<SteelProfile> all;
 static string F(double v){return v.ToString("0.##",CultureInfo.InvariantCulture);}
 internal static List<SteelProfile> All(){if(all!=null)return all;var list=new List<SteelProfile>();
  string[] inch={"1/2","3/4","1","1-1/4","1-1/2","2","2-1/2","3","4","5","6","8","10","12"};
  double[] od={21.3,26.7,33.4,42.2,48.3,60.3,73,88.9,114.3,141.3,168.3,219.1,273,323.8};
  double[] heavyT={2.8,2.9,3.4,3.6,3.7,4,5.2,5.5,6,6.6,7.1,8.2,9.3,10.3},heavyKg={1.277,1.702,2.515,3.427,4.069,5.553,8.694,11.312,16.024,21.923,28.224,42.647,60.476,76.628};
  double[] lightT={2,2.3,2.6,2.6,2.9,2.9,3.2,3.2,3.6,5,5,5.8,6.6,6.9},lightKg={.952,1.384,1.975,2.539,3.247,4.105,5.508,6.763,9.828,16.806,20.135,30.508,43.358,53.922};
  for(int i=0;i<inch.Length;i++){
   list.Add(new SteelProfile{Name="Pipe "+inch[i]+"in heavy gauge OD"+F(od[i])+"x"+F(heavyT[i])+"mm",Kind="HSS circle / pipe",H=od[i],B=od[i],T=heavyT[i],KgM=heavyKg[i],Source="heavy gauge ERW pipe (Supreme / Superior), PNS 26:2018"});
   list.Add(new SteelProfile{Name="Pipe "+inch[i]+"in light gauge OD"+F(od[i])+"x"+F(lightT[i])+"mm",Kind="HSS circle / pipe",H=od[i],B=od[i],T=lightT[i],KgM=lightKg[i],Source="light gauge ERW pipe (TRI-R), PNS 26:2018"});}
  // Seamless black / galvanized iron pipe, ASTM A-53 / API5L / A106 (printed p.18-20): nominal, OD, wall, kg/m.
  Seamless(list,"40","p.18",new object[]{"3/8",17.15,2.31,.85,"1/2",21.34,2.77,1.283,"3/4",26.67,2.87,1.75,"1",33.4,3.38,2.533,"1-1/4",42.2,3.56,3.433,"1-1/2",48.2,3.68,4.1,"2",60.3,3.91,5.517,"2-1/2",73.0,5.16,8.775,"3",88.9,5.49,11.467,"3-1/2",101.6,5.74,13.767,"4",114.3,6.02,16.333,"5",141.2,6.55,22.1,"6",168.8,7.11,28.667,"8",219.1,8.18,43.167,"10",273.0,9.27,61.3,"12",323.8,10.31,81.0});
  Seamless(list,"80","p.19",new object[]{"3/8",17.15,3.2,1.117,"1/2",21.34,3.73,1.65,"3/4",26.67,3.91,2.217,"1",33.4,4.55,3.267,"1-1/4",42.2,4.85,4.533,"1-1/2",48.2,5.08,5.483,"2",60.3,5.54,7.583,"2-1/2",73.0,7.01,11.583,"3",88.9,7.62,15.483,"3-1/2",101.6,8.08,19.0,"4",114.3,8.56,22.65,"5",141.2,9.52,31.417,"6",168.8,10.97,43.167,"8",219.1,12.7,65.6,"10",273.0,15.09,97.333,"12",323.8,17.48,133.917});
  Seamless(list,"160","p.20",new object[]{"1/2",21.34,4.75,1.967,"3/4",26.67,5.54,2.933,"1",33.4,6.35,4.3,"1-1/4",42.2,6.35,5.7,"1-1/2",48.2,7.14,7.367,"2",60.3,8.74,11.3,"2-1/2",73.0,9.52,15.133,"3",88.9,11.13,21.667,"4",114.3,13.49,34.067,"5",141.2,15.88,49.833,"6",168.8,18.26,68.633,"8",219.1,23.01,113.067,"10",273.0,28.58,175.067,"12",323.8,33.32,242.533});
  double[,] strux={{20.5,21.7,1.4,.657},{25.9,27.1,1.4,.843},{32.6,33.8,1.62,1.238},{41.4,42.6,1.71,1.673},{47.5,48.7,1.71,1.932},{59.7,60.9,1.8,2.57},{72.3,73.7,2.48,4.262},{88,89.8,2.48,5.22},{113.2,115.4,2.48,6.758}};
  for(int i=0;i<9;i++){double d=(strux[i,0]+strux[i,1])/2;list.Add(new SteelProfile{Name="Strux tube "+inch[i]+"in OD"+F(d)+"x"+F(strux[i,2])+"mm",Kind="HSS circle / pipe",H=d,B=d,T=strux[i,2],KgM=strux[i,3],Source="Strux lightweight steel tube, PNS 2145:2020 (OD "+F(strux[i,0])+"-"+F(strux[i,1])+" mm)"});}
  // Square tube: size mm, thickness, kg/m. 38x38x3 is omitted: the handbook prints 4.862 kg/m, the same as 4 mm (misprint).
  Tube(list,"SHS","p.22-23",new double[,]{{12.7,12.7,.9,.365},{12.7,12.7,1.2,.486},{12.7,12.7,1.5,.608},{15.9,15.9,.7,.355},{15.9,15.9,.9,.456},{15.9,15.9,1.2,.608},
   {19,19,.7,.425},{19,19,.9,.547},{19,19,1.2,.729},{19,19,1.5,.912},{19,19,2,1.216},{25,25,.7,.567},{25,25,.9,.729},{25,25,1.2,.972},{25,25,1.5,1.216},{25,25,2,1.621},{25,25,3,2.431},
   {31.8,31.8,.7,.709},{31.8,31.8,.9,.912},{31.8,31.8,1.2,1.216},{31.8,31.8,1.5,1.519},{31.8,31.8,2,2.026},{38,38,.7,.851},{38,38,.9,1.094},{38,38,1.2,1.459},{38,38,1.5,1.823},{38,38,2,2.431},{38,38,2.6,3.16},{38,38,4,4.862},
   {50,50,.7,1.134},{50,50,.9,1.459},{50,50,1.2,1.945},{50,50,1.5,2.431},{50,50,2,3.241},{50,50,2.6,4.214},{50,50,3,4.862},{50,50,4,6.483},{50,50,6,9.724},{60,60,1.5,3.039},{60,60,2,4.052},
   {75,75,1.5,3.647},{75,75,2,4.862},{75,75,3,7.293},{75,75,4,9.724},{75,75,6,14.586},{75,75,8,19.448},{100,100,2,6.483},{100,100,3,9.724},{100,100,4,12.965},{100,100,6,19.448},{100,100,8,25.931},
   {150,150,3,14.586},{150,150,4,19.448},{150,150,6,29.172},{150,150,8,38.896}});
  Tube(list,"RHS","p.24",new double[,]{{38,19,.7,.636},{38,19,.9,.818},{38,19,1.2,1.091},{38,19,1.5,1.364},{38,19,2,1.818},{38,19,3,2.728},{50,19,1.2,1.321},{50,19,1.5,1.651},{50,19,2,2.201},
   {50,25,.7,.837},{50,25,.9,1.093},{50,25,1.2,1.436},{50,25,1.5,1.795},{50,25,2,2.393},{50,25,3,3.589},{75,25,1.5,2.393},{75,25,2,3.19},{50,40,1.2,1.723},{50,40,1.5,2.153},{50,40,2,2.871},{50,40,2.6,3.733},{50,40,3,4.307},
   {75,40,1.5,2.752},{75,40,2,3.669},{75,40,3,5.503},{75,50,1.2,2.393},{75,50,1.5,2.991},{75,50,2,3.988},{75,50,2.6,5.184},{75,50,3,5.982},{75,50,4,7.976},{75,50,6,11.964},
   {100,50,1.2,2.871},{100,50,1.5,3.589},{100,50,2,4.785},{100,50,2.6,6.221},{100,50,3,7.178},{100,50,4,9.571},{100,50,5,12.155}});
  var flats=new List<SteelProfile>();FlatBarCatalog.AddTo(flats);list.AddRange(flats.Where(p=>p.H<=4*25.4+1e-9));
  foreach(int d in new[]{8,10,12,16,20,25})list.Add(new SteelProfile{Name="Round bar "+d+"mm",Kind="Round bar",H=d,B=d,T=d,KgM=Math.Round(Math.PI*d*d/4e6*SteelWeight.Density,3),Source="Computed from 7850 kg/m3"});
  foreach(int a in new[]{10,12,16,20,25})list.Add(new SteelProfile{Name="Square bar "+a+"x"+a+"mm",Kind="Square bar",H=a,B=a,T=a,KgM=Math.Round(a*a/1e6*SteelWeight.Density,3),Source="Computed from 7850 kg/m3"});
  all=list;return list;}
 static void Seamless(List<SteelProfile> list,string schedule,string page,object[] rows){for(int i=0;i<rows.Length;i+=4){double od=Convert.ToDouble(rows[i+1]),t=Convert.ToDouble(rows[i+2]);list.Add(new SteelProfile{Name="Pipe "+rows[i]+"in seamless Sch "+schedule+" OD"+F(od)+"x"+F(t)+"mm",Kind="HSS circle / pipe",H=od,B=od,T=t,KgM=Convert.ToDouble(rows[i+3]),Source="seamless pipe Schedule "+schedule+", ASTM A-53 / API5L / A106"});}}
 static void Tube(List<SteelProfile> list,string prefix,string page,double[,] rows){for(int i=0;i<rows.GetLength(0);i++)list.Add(new SteelProfile{Name=prefix+" "+F(rows[i,0])+"x"+F(rows[i,1])+"x"+F(rows[i,2])+"mm",Kind="HSS rectangle",H=rows[i,0],B=rows[i,1],T=rows[i,2],KgM=rows[i,3],Source="ASTM A-500 Gr. B / Q235"});}
}
}
