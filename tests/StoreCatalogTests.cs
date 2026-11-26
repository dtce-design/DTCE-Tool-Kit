using System;using BeamScheduleFormatter;
// Owner (26 November 2026): "e di pa ako inuupdate or ninonotify e may available ng update sa microsoft store" - the app reads the version
// the Store has published from the public Store catalog (sample below as the catalog returned it for both editions).
static class StoreCatalogTests {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok){Console.WriteLine("FAIL: "+why);Environment.Exit(1);}}
 static void Main(){
  string user="{\"Product\":{\"DisplaySkuAvailabilities\":[{\"Sku\":{\"Properties\":{\"Packages\":[{\"PackageFullName\":\"DTCE.DTCEToolKit_10.0.18.0_x64__ysag2e3cand8j\",\"Version\":\"2814749768286208\"},{\"PackageFullName\":\"DTCE.DTCEToolKit_7.2.39.0_x64__ysag2e3cand8j\"}]}}}]}}";
  string admin="{\"Packages\":[{\"PackageFullName\" : \"DTCE.DTCEToolKit-Admin_10.0.18.0_x64__ysag2e3cand8j\"}]}";
  Check(StoreUpdateQuery.CatalogVersion(user,"DTCE.DTCEToolKit")==new Version(10,0,18,0),"User edition version from the catalog (newest package)");
  Check(StoreUpdateQuery.CatalogVersion(admin,"DTCE.DTCEToolKit-Admin")==new Version(10,0,18,0),"Admin edition version");
  Check(StoreUpdateQuery.CatalogVersion(admin,"DTCE.DTCEToolKit")==null,"the User identity does not match the Admin package");
  Check(StoreUpdateQuery.CatalogVersion("{}","DTCE.DTCEToolKit")==null,"no package listed");
  Check(StoreUpdateQuery.Running==new Version(Version.Parse(Engine.Version).Major,Version.Parse(Engine.Version).Minor,Version.Parse(Engine.Version).Build,0),"running version as x.y.z.0");
  Check(new Version(10,0,18,0)>new Version(7,2,39,0),"10.0.18 is newer than 7.2.39");
  Console.WriteLine("StoreCatalogTests passed: "+checks+" checks");}
}
