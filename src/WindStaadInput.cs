using System;using System.Collections.Generic;using System.Linq;
namespace BeamScheduleFormatter {
class WindStaadHeightRow {public string Direction,Exposure;public double Height,Kz,Kzt,Qz;}
static class WindStaadInput {
 internal static List<WindStaadHeightRow> Profile(WindBuildingInput building,double speed,double kd,IEnumerable<WindDirection> directions,double interval=3){building.Validate();if(!WindTin.Finite(interval)||interval<=0||!WindTin.Finite(speed)||speed<=0||!WindTin.Finite(kd)||kd<=0||kd>1)throw new Exception("Check height increment and wind parameters.");var heights=new SortedSet<double>{0,building.Eave,building.Height,building.Apex};for(double z=interval;z<building.Apex;z+=interval){if(heights.Count>1000)throw new Exception("Too many height stations.");heights.Add(z);}var rows=new List<WindStaadHeightRow>();foreach(var direction in directions)foreach(double z in heights){double kz=WindBuildingLoads.Kz(direction.Exposure,z),kzt=WindBuildingLoads.TopographyAt(direction,z);rows.Add(new WindStaadHeightRow{Direction=direction.Name,Exposure=direction.Exposure,Height=z,Kz=kz,Kzt=kzt,Qz=.613*kz*kzt*kd*Math.Pow(speed/3.6,2)/1000});}return rows;}
}
}
