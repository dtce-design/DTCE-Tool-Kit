using System;using System.Linq;using System.Collections.Generic;
namespace BeamScheduleFormatter {
static class ExcelRangeBatches {
 internal static IEnumerable<string> Pack(IEnumerable<string> ranges,int limit=200){if(limit<1||limit>255)throw new ArgumentOutOfRangeException("limit");string batch="";foreach(string range in ranges){if(string.IsNullOrWhiteSpace(range))continue;if(range.Length>limit)throw new Exception("Excel range address is too long.");if(batch.Length>0&&batch.Length+1+range.Length>limit){yield return batch;batch="";}batch=batch.Length==0?range:batch+","+range;}if(batch.Length>0)yield return batch;}
}
}
