using System;using System.Linq;using System.Collections.Generic;
namespace BeamScheduleFormatter {
static class FinishTypeCatalog {
 public const string Schema="DTCEFinishCatalogV1";public const int HeaderRow=35,FirstRow=36,LastRow=56;
 public static readonly string[] Names={
 "Two-Component Cementitious Waterproofing System, 3 Coats",
 "One-Component Polyurethane-Acrylic Liquid Applied Flexible Membrane, 3 Coats",
 "4mm thk. Slated Bituminous Membrane Waterproofing",
 "3mm thk. Sanded Bituminous Membrane Waterproofing",
 "Porcelain Glazed Tiles and Trims",
 "Porcelain Unglazed Tiles",
 "Granite Stone Countertop",
 "Textured Unglazed Tiles (Yellow Warning Tactile Tiles)",
 "Vinyl Floor Tile",
 "Plain Cement Floor Finish",
 "Plain Cement Floor Finish with Floor Hardener",
 "Floor Topping (Trowelled Finish)",
 "Cement Broom Finish",
 "Nonslip Finish",
 "Bush Hammered Finish",
 "Cement Plaster Finish (Paper Finish)",
 "Cement Plaster Finish (Float Finish)",
 "Decorative Stone (Bricks)",
 "Damp Proofing (Polyethylene Sheet)",
 "Moisture Barrier and Pest Control Film, M168",
 "Aluminum Cladding"};
 public static string KindFor(string name){int index=Array.FindIndex(Names,n=>FinishTypeRegistry.Key(n)==FinishTypeRegistry.Key(name));if(index<0)return null;return index==4||index==5||index==7?"Tiles":"Other";}
 public static bool IsPlaster(string name){string key=FinishTypeRegistry.Key(name);return key==FinishTypeRegistry.Key(Names[15])||key==FinishTypeRegistry.Key(Names[16]);}
 public static WorksExcelTable Table(List<FinishTypeTotal> totals){var rows=new List<object[]>();for(int n=0;n<Names.Length;n++){var total=totals.FirstOrDefault(t=>t.Key==FinishTypeRegistry.Key(Names[n]));rows.Add(new object[]{Names[n],total==null?0:total.Area,"m2"});}return new WorksExcelTable(WorksSummaryLayout.Sheet,new[]{"FINISH / WATERPROOFING TYPE","TOTAL AREA m2","UNIT"},rows,HeaderRow,4);}
 public static IEnumerable<object[]> Map(){for(int n=0;n<Names.Length;n++)yield return new object[]{"FinishType"+(n+1).ToString("00"),Names[n],WorksSummaryLayout.Sheet,"E"+(FirstRow+n),"m2"};}
}
}
