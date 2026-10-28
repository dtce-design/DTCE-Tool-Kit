using System;using System.Collections.Generic;
namespace BeamScheduleFormatter {
class WorksExcelTable {
 public readonly int StartRow,StartColumn;public readonly string Name;public readonly string[] Headers;public readonly object[,] Rows;
 public WorksExcelTable(string name,string[] headers,List<object[]> rows,int startRow=1,int startColumn=1){StartRow=startRow;StartColumn=startColumn;Name=name;Headers=headers;Rows=new object[rows.Count,headers.Length];for(int r=0;r<rows.Count;r++){if(rows[r].Length!=headers.Length)throw new ArgumentException("Workbook row/header mismatch: "+name);for(int c=0;c<headers.Length;c++)Rows[r,c]=rows[r][c];}}
}
}
