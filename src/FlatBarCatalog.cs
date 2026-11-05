using System;using System.Collections.Generic;using System.Globalization;
namespace BeamScheduleFormatter {
static class FlatBarCatalog {
 public static void AddTo(List<SteelProfile> profiles){
  foreach(int width in new[]{2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,18,20})foreach(double t in new double[]{3,4.5,6,8,9,9.3,10,12,15,18,20,24}){
   int page=width<=7?20:width<=13?21:22;double mm=width*25.4;
   profiles.Add(new SteelProfile{Name="FB "+width+" in x "+t.ToString("0.###",CultureInfo.InvariantCulture)+" mm",Kind="Flat bar",H=mm,B=mm,T=t,KgM=Math.Round(mm*t*.00785,2,MidpointRounding.AwayFromZero),Source="Regan 2026 handbook PDF p."+page+", printed p."+(page-8)+"; ASTM A36"});
  }
 }
}
}
