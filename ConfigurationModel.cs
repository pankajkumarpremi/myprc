using System;
using SQLite.Net.Attributes;

namespace BSM.Core
{
	public class ConfigurationModel
	{
		public ConfigurationModel ()
		{
		}
		[PrimaryKey]
		public string EmployeeID{ get; set;}
		public int LoginFlag{ get; set; }
		public bool ScanFlag{ get; set; }
		public int InspHistDays{ get; set; }
		public int InspHistAmount{ get; set; }
		public int ImagesLimit{ get; set; }
		public int ViolationTH{ get; set; }
		public string SearchType{ get; set; }
		public int ScreenLock{ get; set; }
		public int OdoInput{ get; set; }
		public int WeightTH{ get; set; }
	}

	public class UVersionModel
	{
		public int version{ get; set; }
	}

	public class NotificationModel
	{
		public string Title{ get; set;}
		public string Description{ get; set;}
		public DateTime dateTime{ get; set;}
		public string DriverId{ get; set;}
		public string DriverName{ get; set;}
		public string Address{ get; set;}
	}
}

