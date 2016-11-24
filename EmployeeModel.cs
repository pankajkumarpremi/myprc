using System;
using SQLite.Net.Attributes;
using BSM.Core.AuditEngine;

namespace BSM.Core
{
	public class EmployeeModel
	{
		public EmployeeModel ()
		{
		}
		public string Id{ get; set;}
		public string DriverName{ get; set;}
		public string Username{ get; set;}
		public string Password{ get; set;}
		public string License{ get; set;}
		public bool AutoLogin{ get; set;}
		public string HomeAddress{ get; set;}
		public bool IsSupervisor{ get; set;}
		public string Signature{ get; set;}
		public string Domain{ get; set;}
		public int Cycle{ get; set;}
		public bool HaveSent{ get; set;}
		public bool TimeLogSynced{ get; set;}
		public string OrgName{ get; set;}
		public string OrgAddr{ get; set;}
		public int HosExceptions{ get; set;}
		public string TimeZone{ get; set;}
		public bool DayLightSaving{ get; set;}
		//TODO:to validate
		public string Country{ get; set;}
		public bool isFromServer{ get; set;}
		public string State{ get; set;}
		public int ApplyDTS{ get; set;}
	}

	public class EmailModel
	{
		[PrimaryKey,AutoIncrement]
		public int Id { get; set;}
		public string DriverId { get; set;}
		public string EmailAddress { get; set;}
		public DateTime FromDate { get; set;}
		public DateTime ToDate { get; set;}
		public string Type { get; set;}
		public bool HaveSent { get; set;}
		public DateTime CreatedDate { get; set;}

		public EmailModel()
		{
			this.CreatedDate = Utils.GetDateTimeUtcNow();
		}
	}

	public class SettingsModel
	{
		[PrimaryKey,AutoIncrement]
		public int Id{ get; set;}
		public string SettingsName{ get; set;}
		public string SettingsValue{ get; set;}
	}

	// TODO: Revalidate this later
	public class CoWorkerModel
	{
		public string EmployeeID{ get; set; }
		public string DriverID{ get; set; }
		public string DriverName{ get; set; }
		public DateTime LoginTime{ get; set; }
		public bool LoggedIn{ get; set; }
		// adding the username of driver to show in coworkerlogin and logout if needed
		public string UserName{ get; set;}
		public bool IsSelected { get; set; }
	}

	public class VehicleOdoModel
	{
		public int BoxID{ get; set; }
		public string EmployeeID{ get; set; }
		public DateTime InsertTS{ get; set; }
		public float OdoV{ get; set; }
		public int Type{ get; set; }
	}

	public class SensorFailureModel
	{
		[PrimaryKey,AutoIncrement]
		public int Id { get; set;}
		public string DriverId { get; set;}
		public int BoxID { get; set;}
		public int Code { get; set;}
		public DateTime CreatedDate { get; set;}
		public bool HaveSent { get; set;}
		public string Comment { get; set;}
	}

	public class LastSessionModel
	{
		[PrimaryKey,AutoIncrement]
		public int Id { get; set;}
		public string DriverId { get; set;}
		public int BoxID { get; set;}
		public string BoxDescription { get; set;}
		public int LogStatus { get; set;}
		public DateTime ScanTime { get; set;}
	}
}
