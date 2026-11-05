using System;using System.Collections.Generic;using System.Globalization;using System.Linq;using System.Text;using System.Windows.Forms;
namespace BeamScheduleFormatter {
partial class ProgramOfWorksForm {
 readonly Dictionary<string,DataGridView> masonryGrids=new Dictionary<string,DataGridView>();
 static readonly string[] MasonrySections={"MasonryLoadBearing","MasonryLouver","MasonryAAC100","MasonryAAC150"};
 static readonly string[] MasonryNames={"Load Bearing CHB","Louver Blocks","AAC Blocks 100 mm","AAC Blocks 150 mm"};
 IEnumerable<DataGridView> MasonryGrids(){return new[]{walls}.Concat(masonryGrids.Values);}
 IEnumerable<WallItem> RegularWalls {get{return wallItems.Where(w=>w.Material=="CHB");}}
 static bool IsMasonrySection(string section){return section=="Walls"||MasonrySections.Contains(section);}
 void NormalizeMasonryRow(DataGridViewRow row){var key=masonryGrids.FirstOrDefault(x=>x.Value==row.DataGridView).Key;if(key=="MasonryAAC100")row.Cells[18].Value="100";if(key=="MasonryAAC150")row.Cells[18].Value="150";}
 string WallMaterial(DataGridView grid){var entry=masonryGrids.FirstOrDefault(x=>x.Value==grid);int index=Array.IndexOf(MasonrySections,entry.Key);return index<0?"CHB":MasonryNames[index];}
 void AddMasonryMaterials(){for(int i=0;i<MasonrySections.Length;i++){int index=i;var grid=WallGrid();masonryGrids.Add(MasonrySections[i],grid);grid.Columns[9].HeaderText="Net area m2";grid.Columns[18].HeaderText="Thickness mm";foreach(int c in new[]{12,15,16,17})grid.Columns[c].Visible=false;if(i>0)grid.Columns[18].Visible=false;
   Action<DataGridViewRow> defaults=row=>{if(index>=2)row.Cells[18].Value=index==2?"100":"150";};grid.DefaultValuesNeeded+=(s,e)=>defaults(e.Row);grid.RowsAdded+=(s,e)=>{if(!filling)for(int n=e.RowIndex;n<e.RowIndex+e.RowCount;n++)defaults(grid.Rows[n]);};
   AddGrid(MasonryNames[i],grid,"Area takeoff in m2. Enter Width x Height x Qty OR total gross area, then deduct selected openings. Material totals are kept separate. Load-bearing classification does not perform a structural design check.");
  }
  foreach(int c in new[]{12,15,16,17})walls.Columns[c].Visible=false;
 }
 Dictionary<string,DataGridView> WithMasonry(Dictionary<string,DataGridView> tables){foreach(var entry in masonryGrids)tables.Add(entry.Key,entry.Value);return tables;}
 void AppendMasonryAreas(StringBuilder b,bool detailed){b.AppendLine();b.AppendLine("MASONRY WORKS | NET WALL AREAS");foreach(int thickness in new[]{100,150})b.AppendLine(string.Format(CultureInfo.InvariantCulture,"CHB {0} mm TOTAL: {1:0.00} m2",thickness,RegularWalls.Where(w=>w.ThicknessMm==thickness).Sum(w=>w.Net(wallOpenings))));foreach(string material in MasonryNames)b.AppendLine(string.Format(CultureInfo.InvariantCulture,"{0} TOTAL: {1:0.00} m2",material,wallItems.Where(w=>w.Material==material).Sum(w=>w.Net(wallOpenings))));if(!detailed)return;foreach(var wall in wallItems){b.AppendLine();b.AppendLine(wall.Material+" | "+wall.Id+" | "+wall.Description);b.AppendLine(string.Format(CultureInfo.InvariantCulture,"Gross {0:0.00} - openings {1:0.00} = net {2:0.00} m2",wall.Gross,wall.Deduction(wallOpenings),wall.Net(wallOpenings)));foreach(var opening in wallOpenings.Where(o=>string.Equals(o.Wall,wall.Id,StringComparison.OrdinalIgnoreCase)))b.AppendLine(string.Format(CultureInfo.InvariantCulture,"  Less {0}: {1:0.00} x {2:0.00} x {3} = {4:0.00} m2",opening.Id,opening.Width,opening.Height,opening.Quantity,opening.Area));}}
 void MasonryExcel(List<WorksExcelTable> tables){var summary=new List<object[]>();foreach(int thickness in new[]{100,150})summary.Add(new object[]{"CHB "+thickness+" mm",RegularWalls.Where(w=>w.ThicknessMm==thickness).Sum(w=>w.Net(wallOpenings)),"m2"});for(int i=0;i<MasonryNames.Length;i++){string material=MasonryNames[i];var wallsOfType=wallItems.Where(w=>w.Material==material).ToList();summary.Add(new object[]{material,wallsOfType.Sum(w=>w.Net(wallOpenings)),"m2"});tables.Add(new WorksExcelTable(new[]{"POW Load Bearing CHB","POW Louver Blocks","POW AAC 100 mm","POW AAC 150 mm"}[i],new[]{"Wall ID","Description","Gross m2","Openings m2","Net area m2"},wallsOfType.Select(w=>new object[]{w.Id,w.Description,w.Gross,w.Deduction(wallOpenings),w.Net(wallOpenings)}).ToList()));}tables.Add(new WorksExcelTable("POW Masonry Summary",new[]{"Material","Net area","Unit"},summary));}
}
}
