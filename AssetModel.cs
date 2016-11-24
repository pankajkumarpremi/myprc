using System;
using SQLite.Net.Attributes;

namespace BSM.Core
{
	public class AssetModel
	{
		/* CREATE TABLE ASSETS (EmployeeID ntext, AssetDescription ntext, AssetBoxId INTEGER, IsAttachment BOOLEAN, CatGroups ntext, CatLastUpdate datetime, VehicleLicense ntext, VehicleLicenseProvince ntext); */
		public AssetModel ()
		{
		}
		public string EmployeeID{ get; set; }
		[PrimaryKey]
		public int AssetBoxId{ get; set; }
		public string AssetDescription{ get; set; }
		public int Weight{ get; set; }
		public bool IsAttachment{ get; set; }
		public string VehicleLicense{ get; set; }
		public string VehicleLicenseProvince{ get; set; }
		public string DOT{ get; set; }
		public DateTime InsertTS{ get; set; }
		public string CatGroups{ get; set; }
		public DateTime CatLastUpdate{ get; set; }

		public string LngCode{ get; set;}
		public string SearchValue{ get; set;}
		public int SearchType{ get; set;}
	}
}

