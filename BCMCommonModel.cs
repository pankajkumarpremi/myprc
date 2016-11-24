using System;
using System.Collections.Generic;
using MvvmCross.Core.ViewModels;
using BSM.Core.ViewModels;
using System.ComponentModel;

namespace BSM.Core
{
	public class BCMCommonModel
	{
		public BCMCommonModel ()
		{
		}
	}

	public class SearchType
	{
		public SearchType(){
			
		}
		public SearchType (int _searchType, string _searchTypeText)
		{
			this.searchType = _searchType;
			this.searchTypeText = _searchTypeText;
		}
		public int searchType { get; set; }
		public string searchTypeText { get; set; }
	}

	public class Filter
	{
		public Filter (int _filterDays, string _filterString)
		{
			this.filterDays  = _filterDays;
			this.filterString = _filterString;
		}
		public int filterDays { get; set; }
		public string filterString { get; set; }
	}

	public class InspectionTypeClass
	{
		public InspectionTypeClass (int _inspectionType, string _inspectionTypeText)
		{
			this.inspectionType = _inspectionType;
			this.inspectionTypeText = _inspectionTypeText;
		}
		public int inspectionType { get; set; }
		public string inspectionTypeText { get; set; }
	}

	public class DriverStatusTypeClass : INotifyPropertyChanged
	{
		public DriverStatusTypeClass (int _driverStatusType, string _driverStatusTypeText, string _driverStatusTimeText,string _driverStatusTimeTextDropDown, string _driverStatusSuffixText)
		{
			this.driverStatusType = _driverStatusType;
			this.driverStatusTypeText = _driverStatusTypeText;
			this.driverStatusTimeText = _driverStatusTimeText;
			this.driverStatusTimeTextDropDown = _driverStatusTimeTextDropDown;
			this.driverStatusSuffixText = _driverStatusSuffixText;
		}
		//public int driverStatusType { get; set; }

		private int _driverStatusType;
		public int driverStatusType{
			get{return _driverStatusType; }
			set{
				if (value != this._driverStatusType) {
					this._driverStatusType = value;
					var handler = this.PropertyChanged;
					if (handler != null) {
						handler (this, new PropertyChangedEventArgs ("driverStatusType"));
					}
				}
			}
		}



		//public string driverStatusTypeText { get; set; }
		private string _driverStatusTypeText;
		public string driverStatusTypeText{
			get{return _driverStatusTypeText; }
			set{
				if (value != this._driverStatusTypeText) {
					this._driverStatusTypeText = value;
					var handler = this.PropertyChanged;
					if (handler != null) {
						handler (this, new PropertyChangedEventArgs ("driverStatusTypeText"));
					}
				}
			}
		}
		//public string driverStatusTimeText { get; set; }
		//public string driverStatusSuffixText { get; set; }
		private string _driverStatusSuffixText;
		public string driverStatusSuffixText{
			get{return _driverStatusSuffixText; }
			set{
				if (value != this._driverStatusSuffixText) {
					this._driverStatusSuffixText = value;
					var handler = this.PropertyChanged;
					if (handler != null) {
						handler (this, new PropertyChangedEventArgs ("driverStatusSuffixText"));
					}
				}
			}
		}


		private string _driverStatusTimeTextDropDown;
		public string driverStatusTimeTextDropDown{
			get{return _driverStatusTimeTextDropDown; }
			set{
				if (value != this._driverStatusTimeTextDropDown) {
					this._driverStatusTimeTextDropDown = value;
					var handler = this.PropertyChanged;
					if (handler != null) {
						handler (this, new PropertyChangedEventArgs ("driverStatusTimeTextDropDown"));
					}
				}
			}
		}



		private string _driverStatusTimeText;
		public string driverStatusTimeText{
			get{return _driverStatusTimeText; }
			set{
				if (value != this._driverStatusTimeText) {
					this._driverStatusTimeText = value;
					var handler = this.PropertyChanged;
					if (handler != null) {
						handler (this, new PropertyChangedEventArgs ("driverStatusTimeText"));
					}
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

	}

	public class RecapSumDataItem : INotifyPropertyChanged
	{
		HOSSummaryViewModel _parent;      

		public RecapSumDataItem (HOSSummaryViewModel parent)
		{
			_parent = parent;
			this.date = "";
			this.driveDuty = 0;
			this.TotalDrivingStr = "00:00";
			this.offDuty = 0;
			this.TotalOffDutyStr = "00:00";
			this.onDuty = 0;
			this.TotalOnDutyStr = "00:00";
			this.sleepDuty = 0;
			this.TotalSleepingStr = "00:00";
		}

		private bool showSign;
		public bool ShowSign{
			get{return showSign; }
			set{
				if (value != this.showSign) {
					this.showSign = value;
					var handler = this.PropertyChanged;
					if (handler != null) {
						handler (this, new PropertyChangedEventArgs ("ShowSign"));
					}
				}
			}
		}
		private bool showViolation = true;
		public bool ShowViolation{
			get{return showViolation; }
			set{
				if (value != this.showViolation) {
					this.showViolation = value;
					var handler = this.PropertyChanged;
					if (handler != null) {
						handler (this, new PropertyChangedEventArgs ("ShowViolation"));
					}
				}
			}
		}
		public IMvxCommand SignClick {
			get {
				return new MvxCommand (() => _parent.btnSignClick (this));
			}
		}

		public IMvxCommand ViolationClick {
			get {
				return new MvxCommand (() => _parent.btnViolationClick (this));
			}
		}


		public RecapSumDataItem Item{ get { return this; } }   

//		public RecapSumDataItem () {
//			this.date = "";
//			this.driveDuty = 0;
//			this.TotalDrivingStr = "00:00";
//			this.offDuty = 0;
//			this.TotalOffDutyStr = "00:00";
//			this.onDuty = 0;
//			this.TotalOnDutyStr = "00:00";
//			this.sleepDuty = 0;
//			this.TotalSleepingStr = "00:00";
//		}

		public string date { get; set; }
		public int offDuty { get; set; }
		public int sleepDuty { get; set; }
		public int driveDuty { get; set; }
		public int onDuty { get; set; }
		public string TotalOffDutyStr { get; set; }
		public string TotalSleepingStr { get; set; }
		public string TotalDrivingStr { get; set; }
		public string TotalOnDutyStr { get; set; }
		public DateTime dateT { get; set; }

		public event PropertyChangedEventHandler PropertyChanged;
	}

	public class RecapSumData
	{
		public RecapSumData () {
			this.tlOffDuty7 = 0;
			this.tlSleepDuty7 = 0;
			this.tlDriveDuty7 = 0;
			this.tlOnDuty7 = 0;

			this.tlOffDuty8 = 0;
			this.tlSleepDuty8 = 0;
			this.tlDriveDuty8 = 0;
			this.tlOnDuty8 = 0;

			this.tlOffDuty14 = 0;
			this.tlSleepDuty14 = 0;
			this.tlDriveDuty14 = 0;
			this.tlOnDuty14 = 0;
			this.recapItems = new List<RecapSumDataItem> ();
		}

		public double tlOffDuty7 { get; set; }
		public double tlSleepDuty7 { get; set; }
		public double tlDriveDuty7 { get; set; }
		public double tlOnDuty7 { get; set; }

		public double tlOffDuty8 { get; set; }
		public double tlSleepDuty8 { get; set; }
		public double tlDriveDuty8 { get; set; }
		public double tlOnDuty8 { get; set; }

		public double tlOffDuty14 { get; set; }
		public double tlSleepDuty14 { get; set; }
		public double tlDriveDuty14 { get; set; }
		public double tlOnDuty14 { get; set; }

		public List<RecapSumDataItem> recapItems { get; set; }

		public List<RecapSumDataItem> getData(){
			return recapItems;
		}
		public void addData(RecapSumDataItem item){
			if(recapItems != null)
				recapItems.Add(item);
		}
	}

	public class BSMMessage
	{
		public BSMMessageType Type { get; set; }
		public string Sender { get; set; }
		public string Text { get; set; }
		public bool IsMessageSent { get; set; }
		public DateTime MsgDateTime { get; set; }
	}

	public class CannedItem
	{
		public CannedItem (string _cannedItemText)
		{
			this.CannedItemText = _cannedItemText;
		}
		public string CannedItemText { get; set; }
	}

	public enum BSMMessageType
	{
		Incoming,
		Outgoing,
	}

	public enum TCPConnStates
	{
		Connecting,
		Requesting,
		WaitingOnAck,
		Connected
	}

	public enum BoxWiFiStatus
	{
		ONINIT,
		CONNECTING,
		CONNECTED,
		CONNECTION_FAILED,
		DISCONNECTED
	}

	public class PhotoModel{

		InspectionDefectViewModel _parent;
		public PhotoModel(InspectionDefectViewModel parent){
			_parent = parent;
		}

		public string PhotoName{ get; set;}
		public string PhotoPath{ get; set;}

		public IMvxCommand RemovePicture {
			get {
				return new MvxCommand (() => _parent.RemovePhoto (this));
			}
		}

		public IMvxCommand ViewPicture {
			get {
				return new MvxCommand (() => _parent.ViewPhoto (this));
			}
		}
	}

	//CREATE TABLE UVERSION (VERSION INTEGER)
	public class UVERSION{
		public int VERSION{ get; set;}
	}
}

