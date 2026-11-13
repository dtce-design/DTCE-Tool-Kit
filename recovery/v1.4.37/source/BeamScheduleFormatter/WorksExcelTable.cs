using System;
using System.Collections.Generic;

namespace BeamScheduleFormatter
{
	internal class WorksExcelTable
	{
		public readonly int StartRow;

		public readonly int StartColumn;

		public readonly string Name;

		public readonly string[] Headers;

		public readonly object[,] Rows;

		public WorksExcelTable(string name, string[] headers, List<object[]> rows, int startRow = 1, int startColumn = 1)
		{
			StartRow = startRow;
			StartColumn = startColumn;
			Name = name;
			Headers = headers;
			Rows = new object[rows.Count, headers.Length];
			for (int i = 0; i < rows.Count; i++)
			{
				if (rows[i].Length != headers.Length)
				{
					throw new ArgumentException("Workbook row/header mismatch: " + name);
				}
				for (int j = 0; j < headers.Length; j++)
				{
					Rows[i, j] = rows[i][j];
				}
			}
		}
	}
}
