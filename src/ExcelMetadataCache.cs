using System;using System.Linq;using System.Collections.Generic;
namespace BeamScheduleFormatter {
sealed partial class ExcelLiveLink {
 Dictionary<string,int> backupNameRows;Dictionary<string,string> backupNameSignatures;HashSet<string> registeredItemSlots;
 void LoadBackupNameSnapshot(){backupNameRows=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);backupNameSignatures=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);foreach(dynamic name in book.Names){string key=Convert.ToString(name.Name);if(key.Contains("!"))key=key.Substring(key.LastIndexOf('!')+1);if(!key.StartsWith("_DTCE_BACKUP_",StringComparison.Ordinal))continue;dynamic range;try{range=name.RefersToRange;}catch{throw new Exception("A saved BACKUP mapping is broken: "+key+". Repair the named range in Excel.");}if(Convert.ToString(range.Worksheet.Name)!="BACKUP"||Convert.ToInt32(range.Column)!=55)throw new Exception("A BACKUP mapping points outside its total column: "+key);backupNameRows[key]=Convert.ToInt32(range.Row);backupNameSignatures[key]=Convert.ToString(name.Comment);}if(nameIdentityCache==null)nameIdentityCache=new Dictionary<string,string>();foreach(var pair in backupNameSignatures)nameIdentityCache[pair.Key]=pair.Value;}
 Dictionary<string,int> CachedBackupNames(string prefix,Dictionary<string,string> identities){if(backupNameRows==null)LoadBackupNameSnapshot();var result=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);foreach(var pair in backupNameRows)if(pair.Key.StartsWith(prefix,StringComparison.Ordinal)){result[pair.Key]=pair.Value;if(identities!=null)identities[pair.Key]=backupNameSignatures[pair.Key];}return result;}
 void RememberBackupName(string name,int row,string identity){if(backupNameRows!=null){backupNameRows[name]=row;backupNameSignatures[name]=identity;}}
 void ForgetBackupName(string name){if(backupNameRows!=null){backupNameRows.Remove(name);backupNameSignatures.Remove(name);}}
}
}
