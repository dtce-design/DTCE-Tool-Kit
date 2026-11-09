using System;
using System.Threading.Tasks;
namespace BeamScheduleFormatter {
enum StoreUpdateResult { Unavailable, Current, Required, NotPackaged }
// Only a successful Store response can restrict access. No stale disk lock offline.
sealed class StoreUpdatePolicy {
 public bool Blocked {get;private set;}
 public bool Checking {get;private set;}
 public DateTime NextCheckUtc {get;private set;}
 public async Task<bool> CheckAsync(Func<Task<StoreUpdateResult>> query,DateTime now,bool force=false){
  if(Checking||(!force&&now<NextCheckUtc))return false;
  Checking=true;
  try{StoreUpdateResult result;try{result=await query();}catch{result=StoreUpdateResult.Unavailable;}
   Blocked=result==StoreUpdateResult.Required;
   NextCheckUtc=now.AddMinutes(result==StoreUpdateResult.Current?360:result==StoreUpdateResult.Required?5:15);
   return true;
  }finally{Checking=false;}
 }
}
static class StoreUpdateState {public static bool Blocked;}
}
