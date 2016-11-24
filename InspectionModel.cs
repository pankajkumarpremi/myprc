using System;
using System.Collections.Generic;
using SQLite.Net.Attributes;

namespace BSM.Core
{
	public class InspectionModel
	{
		public InspectionModel ()
		{
		}
		public int InspectionItemId{ get; set;}
		public int CategoryID{ get; set;}
		public string Defect{ get; set;}
		public string DefectAbbr{ get; set;}
		public int DefectLevel{ get; set;}
		public int parentId{ get; set;}
		public int hasChildren{ get; set;}
		public string attID{ get; set;}
	}

	public class InspectionReportModel
	{

		/*CREATE TABLE INSPECTION_REPORT (Id INTEGER PRIMARY KEY AUTOINCREMENT, InspectionTime datetime, ModifiedDate datetime, EquipmentID ntext, Odometer INTEGER, DriverId ntext, DriverName ntext, 
			InspectionType INTEGER, HaveSent BOOLEAN, IsFromServer BOOLEAN, CheckedCategoryIds ntext, Latitude, Longitude, BoxID INTEGER, Signed BOOLEAN, attID ntext, CheckedAttCategoryIds ntext,
			TimeZone ntext, AppVersion ntext, DayLightSaving BOOLEAN);*/

		[PrimaryKey,AutoIncrement]
		public int Id{ get; set;}
		public DateTime InspectionTime{ get; set; }
		public DateTime ModifiedDate{ get; set; }
		public string EquipmentID{ get; set; }
		public int Odometer{ get; set; }
		public string DriverId{ get; set; }
		public string DriverName{ get; set; }
		public int InspectionType{ get; set; }
		public bool HaveSent{ get; set; }
		public bool IsFromServer{ get; set; }
		public string CheckedCategoryIds{ get; set; }
		public string AttachmentCheckedCategoryIds{ get; set; }
		public double Latitude{ get; set; }
		public double Longitude{ get; set; }
		public int BoxID{ get; set; }
		public int Signed{ get; set; }
		public string attID{ get; set; }

		public string TimeZone{ get; set;}
		public string AppVersion{ get; set;}
		public bool DayLightSaving{ get; set;}
		public DateTime StartReportTime{ get; set;}
	}

	public class InspectionItemModel {
		/*CREATE TABLE INSPECTION_ITEM (InspectionItemId INTEGER, CategoryID INTEGER, Defect ntext, DefectAbbr ntext, DefectLevel INTEGER, parentId INTEGER, hasChildren INTEGER);*/
		public InspectionItemModel() {
		}
		public InspectionItemModel(int _InspectionItemId, int _CategoryID, string _Defect, string _DefectAbbr, int _DefLevel, int _parentId, int _hasChildren, string _attID) {
			this.InspectionItemId = _InspectionItemId;
			this.CategoryID = _CategoryID;
			this.Defect = _Defect;
			this.DefectAbbr = _DefectAbbr;
			this.DefLevel = _DefLevel;
			this.parentId = _parentId;
			this.hasChildren = _hasChildren;
			this.attID = _attID;
		}
		//[PrimaryKey]
		public int InspectionItemId{ get; set;}
		public int CategoryID{ get; set;}
		public string Defect{ get; set; }
		public string DefectAbbr{ get; set; }
		public int DefLevel{ get; set; }
		public int parentId{ get; set; }
		public int hasChildren{ get; set; }
		public string attID{ get; set; }
		public string LngCode{ get; set;}

		// TODO Create new class with this new fields
		public bool SetStrikeThrough { get; set;}
		public bool hasMajorDefect{ get; set;}
		public bool hasMinorDefects{ get; set;}
		public bool IsAttachmentAvailble{ get; set;}
	}

	public class InspectionReportDefectModel {

		/*CREATE TABLE INSPECTION_REPORT_DEFFECTS (InspectionReportId INTEGER, InspectionItemId INTEGER, Comments ntext, MediaFiles ntext, attID ntext, Cleared BOOLEAN, ClearedDriverId ntext, ClearedDriverName ntext);*/

		public int InspectionReportId{ get; set; }
		public int InspectionItemId{ get; set; }
		public string Comments{ get; set; }
		public string ClrDriverID{ get; set; }
		public string ClrDriverName{ get; set; }
		public string attID{ get; set; }
		public string MFILES{ get; set; }

		/*public string Clr{ get; set; }*/
		public bool  Clr{ get; set; }
	}


	public class InspectionReportRow
	{
		public int irId{ get; set;}
		public int Id{ get; set;}
		public DateTime InspectionTime{ get; set; }
		public DateTime ModifiedDate{ get; set; }
		public string EquipmentID{ get; set; }
		public int Odometer{ get; set; }
		public string DriverId{ get; set; }
		public string DriverName{ get; set; }
		public int InspectionType{ get; set; }
		public bool HaveSent{ get; set; }
		public bool IsFromServer{ get; set; }
		public string CheckedCategoryIds{ get; set; }
		public string AttachmentCheckedCategoryIds{ get; set; }
		public double Latitude{ get; set; }
		public double Longitude{ get; set; }
		public int BoxID{ get; set; }
		public int Signed{ get; set; }
		public string attID{ get; set; }
		public string Comments{get;set;}
		public bool HasErrorAttachments { get; set; }
		public int DefectType { get; set; }

		public List<InspectionReportDefectModel> Defects{ get; set;}

	}
}

