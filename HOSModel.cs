using System;
using BSM.Core.AuditEngine;
using SQLite.Net.Attributes;

namespace BSM.Core
{
	public class HOSModel
	{
		public HOSModel ()
		{
		}
		[PrimaryKey,AutoIncrement]
		public int ID{ get; set;}
		public string EmployeeID{ get; set; }
		public DateTime LogTime{get;set;}
		public string JobRef{ get; set; }
		public string Permit{ get; set; }
		public string VLicense{ get; set; }
		public string VLicenseProvince{ get; set; }
		public string TrailerLicense{ get; set; }
		public string Coworker{ get; set; }
		public string VehicleDesc{ get; set; }
		public int BoxID{ get; set; }
		public DateTime OrigLogTime{ get; set; }
		public bool IsDeleted{ get; set; }
		public bool HaveSent{ get; set; }
	}

	public class LocationEventsModel
	{
		public LocationEventsModel ()
		{
		}
		[PrimaryKey,AutoIncrement]
		public int Id{ get; set;}
		public DateTime InsertTimeStamp{ get; set; }
		public float Latitude { get; set; }
		public float Longitude { get; set; }
		public float Speed { get; set; }
		public float Cog { get; set; }
	}

	public class TimeLogModel
	{
		/* CREATE TABLE TIMELOG (Id INTEGER PRIMARY KEY AUTOINCREMENT, LogStatus INTEGER, LogTime datetime, Logbookstopid INTEGER, Odometer INTEGER, DriverId ntext, HaveSent BOOLEAN, IsFromServer BOOLEAN, 
				Latitude, Longitude, BoxID INTEGER, EquipmentID ntext, Comment ntext, Address ntext, Signed BOOLEAN, Type INTEGER, OrigLogTime datetime, Editor INTEGER, TimeZone ntext, AppVersion ntext, DayLightSaving BOOLEAN); */

		public TimeLogModel ()
		{
			this.LogTime = Utils.GetDateTimeNow();
			this.OrigLogTime = Utils.GetDateTimeUtcNow();   // must be kept the same since the time it gets created
		}
		[PrimaryKey,AutoIncrement]
		public int Id{ get; set;}
		public int LogStatus { get; set; }
		public DateTime LogTime { get; set; }
		public int Logbookstopid { get; set; }
		public int Odometer { get; set; }
		public string DriverId { get; set; }
		public bool HaveSent { get; set; }
		public bool IsFromServer { get; set; }
		public double Latitude{ get; set; }
		public double Longitude{ get; set; }
		public int BoxID { get; set; }
		public string EquipmentID { get; set; }
		public string Comment { get; set; }
		public string Address { get; set; }
		public bool Signed { get; set; }
		public int Type { get; set; }
		public DateTime OrigLogTime { get; set; }
		public int Editor { get; set; }
		public string TimeZone { get; set; }
		public string AppVersion { get; set; }
		public bool DayLightSaving { get; set; }
		public string CoDriver { get; set; }
		public int Event { get; set; }
		public bool QualifyRadiusRule{ get; set;}
	}

	public class DeferHoursModel
	{
		public DeferHoursModel ()
		{
		}
		[PrimaryKey,AutoIncrement]
		public int Id { get; set;}
		public string DriverId { get; set;}
		public DateTime day1 { get; set;}
		public DateTime day2 { get; set;}
		public int deferMinutes { get; set;}
		public bool HaveSent { get; set;}
	}

	public class RecapModel
	{
		public RecapModel ()
		{
		}
		[PrimaryKey,AutoIncrement]
		public int Id { get; set;}
		public string DriverId { get; set;}
		public DateTime date { get; set;}
		public int today { get; set;}
		public int cycleTotal { get; set;}
		public int cycleAvailable { get; set;}
		public string cycle { get; set;}
		public bool HaveSent { get; set;}
		public int BoxID { get; set;}
	}

	public class RuleSelectionHistoryModel
	{
		/* CREATE TABLE RuleSelectionHistory (Id INTEGER PRIMARY KEY AUTOINCREMENT, DriverId ntext, ruleid INTEGER, selectTime datetime, country ntext, HaveSent BOOLEAN, HosExceptions INTEGER); */
		public RuleSelectionHistoryModel ()
		{
			this.Id = -1;
			this.ruleid = (int)HOSCYCLE.NONE;
			this.selectTime = DateTime.MinValue;
			this.country = "";
		}
		[PrimaryKey,AutoIncrement]
		public int Id { get; set;}
		[Indexed]
		public string DriverId { get; set;}
		public int ruleid { get; set;}
		[Indexed]
		public DateTime selectTime { get; set;}
		public string country { get; set;}
		public bool HaveSent { get; set;}
		public int HosExceptions { get; set;}
	}

	public class HosAlertModel
	{
		public HosAlertModel ()
		{
		}
		[PrimaryKey,AutoIncrement]
		public int Id { get; set;}
		public string DriverId { get; set;}
		public DateTime date { get; set;}
		public DateTime shiftStart { get; set;}
		public string drivingRuleViolated { get; set;}
		public string onDutyRuleViolated { get; set;}
		public int alertType { get; set;}
		public int drivingAvailable { get; set;}
		public int ondutyAvailable { get; set;}
		public int threshold { get; set;}
		public bool HaveSent { get; set;}
		public int BoxID { get; set;}
	}

	public class TripInfoModel
	{
		/* CREATE TABLE TRIPINFO (Id INTEGER PRIMARY KEY AUTOINCREMENT, DriverId ntext, BoxID ntext, Permit ntext, BLNumber ntext, TrailerLicense ntext, VehicleLicense ntext, VehicleLicenseProvince ntext, CoWorker ntext, Truck ntext, 
				LogTime datetime, HaveSent BOOLEAN, IsFromServer BOOLEAN, IsDeleted BOOLEAN, OrigLogTime datetime); */
		public TripInfoModel ()
		{
		}
		[PrimaryKey,AutoIncrement]
		public int Id { get; set;}
		public string DriverId { get; set;}
		public string BoxID { get; set;}
		public string Permit { get; set;}
		public string BLNumber { get; set;}
		public string TrailerLicense { get; set;}
		public string VehicleLicense { get; set;}
		public string VehicleLicenseProvince { get; set;}
		public string CoWorker { get; set;}
		public string Truck { get; set;}
		public DateTime LogTime { get; set;}
		public bool HaveSent { get; set;}
		public bool IsFromServer { get; set;}
		public bool IsDeleted { get; set;}
		public DateTime OrigLogTime { get; set;}
	}

	public class HosEventsModel
	{
		public HosEventsModel ()
		{
		}
		[PrimaryKey,AutoIncrement]
		public int Id { get; set;}
		public int StatusType { get; set;}
		public int EventType { get; set;}
		public DateTime CreatedDate { get; set;}
		public string Comments { get; set;}
	}

	// For view only
	public class EventRow
	{
		public TimeLogModel EventTimeLog { get; set;}
		public string EventItem { get; set;}
//		public string EventType { get; set;}
//		public string EventTime { get; set;}
//		public string EventLocation { get; set;}
		public bool IsSelected { get; set;}
	}

	public class Trip
	{
		public int Id { get; set;}
		public string TripDateTime { get; set;}
		public bool IsSelected { get; set;}
		public String TrailerLicense { get; set;}
		public String PermitNumber { get; set;}
		public String ShippingDocNumber{ get; set;}
	}

	public class HosCycleModel
	{
		public int Id { get; set;}
		public string CycleDescription { get; set;}
	}
}