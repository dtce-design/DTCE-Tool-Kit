using System;using System.Collections.Generic;
namespace BeamScheduleFormatter {
// Permanent public Excel contract. Never move/reuse a cell; append new metrics separately.
static class WorksSummaryLayout {
 public const string Sheet="DTCE Tool Kit - POW",Schema="DTCEPOWSummaryV1";public const int LastRow=32;
 public static readonly string[][] Map={
  new[]{"Grade40","Grade 40 rebar TOTAL TO BUY","5","kg"},new[]{"Grade60","Grade 60 rebar TOTAL TO BUY","6","kg"},
  new[]{"BeamConcrete","Beam concrete (gross)","9","m3"},new[]{"SlabConcrete","Slab concrete (gross)","10","m3"},new[]{"ConcreteTotal","Total concrete (gross)","11","m3"},
  new[]{"BeamForms","Beam forms","14","m2"},new[]{"SlabForms","Slab forms","15","m2"},new[]{"FormsTotal","Total forms","16","m2"},
  new[]{"CHBArea","Net CHB wall area","19","m2"},new[]{"CHBCount","CHB blocks","20","pcs"},new[]{"Plaster","Net plaster","21","m2"},new[]{"MasonryPaint","Masonry paint","22","m2"},
  new[]{"CHB100Area","CHB 100 mm net area","23","m2"},new[]{"CHB150Area","CHB 150 mm net area","24","m2"},
  new[]{"WoodPaint","Wood paint including jamb","25","m2"},new[]{"Leversets","Leversets","26","pcs"},new[]{"Hinges","Hinges","27","pcs"},
  new[]{"WallTiles","Wall tiles","30","m2"},new[]{"CeilingFinishes","Ceiling finishes","31","m2"},new[]{"FloorFinishes","Floor finishes","32","m2"}};
 public static WorksExcelTable Build(IDictionary<string,double> values){var rows=new List<object[]>();for(int n=2;n<=LastRow;n++)rows.Add(new object[]{null,null,null});rows[0][0]="PROGRAM OF WORKS";rows[1][0]="Fixed summary cells | "+Schema;foreach(var item in Map){int n=int.Parse(item[2])-2;double v;if(!values.TryGetValue(item[0],out v)){if(item[0]=="CHB100Area"||item[0]=="CHB150Area")v=0;else throw new ArgumentException("Missing fixed summary value: "+item[0]);}rows[n]=new object[]{item[1],v,item[3]};}return new WorksExcelTable(Sheet,new[]{"Item","Quantity","Unit"},rows);}
 public static WorksExcelTable CellMap(FinishTypeRegistry registry=null){var rows=new List<object[]>();foreach(var item in Map)rows.Add(new object[]{item[0],item[1],Sheet,"B"+item[2],item[3]});rows.AddRange(FinishTypeCatalog.Map());if(registry!=null)foreach(var item in registry.Rows)rows.Add(new object[]{"FinishType:"+item.Key,item.Name,FinishTypeRegistry.Sheet,"B"+item.Row,"m2"});return new WorksExcelTable("POW Cell Map",new[]{"Key","Value","Worksheet","Fixed cell","Unit"},rows);}
}
}
