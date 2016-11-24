using MvvmCross.Core.ViewModels;
using System.Windows.Input;
using MvvmCross.Plugins.Messenger;
using BSM.Core.Services;
using MvvmCross.Plugins.File;
using System;
using System.Collections.Generic;
using BSM.Core.Messages;
using BSM.Core.AuditEngine;
using BSM.Core.ConnectionLibrary;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace BSM.Core.ViewModels

{
	public class HOSLogSheetViewModel : BaseViewModel
	{
		private readonly ICommunicationService _communicationService;
		private readonly ITimeLogService _timeLogService;
		private readonly IDataService _dataService;
		private List<TimeLogModel> allTimeLogData;
		private List<TripInfoModel> tripInfoList;
		private TimeLogModel lastPrvDayEvent = null;
		private readonly IHourCalculatorService _hourCalculatorService;
		private readonly ISettingsService _settingsService;
		private readonly ITripInfoService _tripInfoService;
		private readonly IAssetService _assetService;
		private bool b_miles = false;
		HourCalculator hc;
		private readonly IMvxMessenger _messenger;
		private readonly ILanguageService _languageService;

		#region ctors
		public HOSLogSheetViewModel(ICommunicationService communicationService, ITimeLogService timeLogService, IDataService dataService, IHourCalculatorService hourCalculatorService, 
			ISettingsService settingsService, ITripInfoService tripInfoService, IAssetService assetService, IMvxMessenger messenger, ILanguageService languageService ) {
			_communicationService = communicationService;
			_timeLogService = timeLogService;
			_dataService = dataService;
			_hourCalculatorService = hourCalculatorService;
			_settingsService = settingsService;
			hc = _hourCalculatorService.getHourCalculator ();
			_tripInfoService = tripInfoService;
			_assetService = assetService;
			_messenger = messenger;
			_languageService = languageService;

			CoDriver = _dataService.GetCoDriver();
			OnDutyLeft = TimeSpan.FromMinutes (Convert.ToDouble ((hc!= null && hc.AvaliableOnDutyMinutes > 0) ? hc.AvaliableOnDutyMinutes : 0)).ToString (@"hh\:mm");
			Drive = TotalDrivingStr;

			PermitNumber = "";
//			txtOrgName = this.view.FindViewById<TextView> (Resource.Id.txtV_hos_org_name);
//			txtOrgName.Text = GlobalInstance.currentEmployee.OrgName; 
//
//			txtOrgAddr = this.view.FindViewById<TextView> (Resource.Id.txtV_hos_org_addr);
//			txtOrgAddr.Text = GlobalInstance.currentEmployee.OrgAddr; 
			EmployeeModel emp = EmployeeDetail();
			OrgAddress = emp.OrgAddr;
			HomeAddress = emp.HomeAddress;
			ShiftStart = (hc != null && hc.ShiftStart != null) ? ((DateTime)hc.ShiftStart).ToString("f") : "";
			DrivingLeft = TimeSpan.FromMinutes (Convert.ToDouble ((hc != null && hc.AvaliableDrivingMinutes > 0) ? hc.AvaliableDrivingMinutes : 0)).ToString (@"hh\:mm");

			if (hc != null && hc.MaxCycle > 0)
				Cycle = TimeSpan.FromMinutes (Convert.ToDouble (hc.MaxCycle - hc.AvaliableCycle)).ToString (@"hh\:mm");
			StartOdometer = "";
			TimeZone = TimeZoneInfo.Local != null ? TimeZoneInfo.Local.StandardName : "EST";
			OrgName = emp.OrgName;

			Equipment = "";
			LicensePlate = "";
			LicenseProvince = "";

			try {
				SettingsModel sm = _settingsService.GetSettingsByName(Constants.SETTINGS_MI_ODO_ENABLED);
				string mi_odo_enabled = sm.SettingsValue;
				if (mi_odo_enabled != null && mi_odo_enabled.Equals("1")) {
					b_miles = true;
				}
			} catch (Exception e) {
			}
			if (b_miles)
				OdomTitle = _languageService.GetLocalisedString (Constants.str_mi);
			else
				OdomTitle = _languageService.GetLocalisedString (Constants.str_km);

			SettingsModel sm_man = _settingsService.GetSettingsByName(Constants.SETTINGS_MANUAL_ODO_INPUT);
			if (sm_man.SettingsValue != null) {
					Odometer = _dataService.GetOdometer() != 0 ? (b_miles ? Convert.ToInt32 (_dataService.GetOdometer() * 0.621371).ToString () : _dataService.GetOdometer().ToString()) : "N/A";	
				if(sm_man.SettingsValue.Equals ("Yes")) {
					//enable edittext
				} else if (sm_man.SettingsValue.Equals ("No")) {
					//disable edittext
				}
			}
			StartOdometer = (hc != null && hc.ShiftStartOdometer != 0) ? 
				(b_miles ? Convert.ToInt32(hc.ShiftStartOdometer*0.621371).ToString () : hc.ShiftStartOdometer.ToString()) : 
				(sm_man.SettingsValue.Equals ("Yes") ? Odometer : Odometer);

		}

		public async Task Init(DateTime SelectedDate, bool EnableSendMail) {
			IsBusy = false;
			IsBusy = true;
			await Task.Delay (700);
			PlottingDate = SelectedDate;
			await PrepareLogData ();
			EnableSendMailL = EnableSendMail;
			await Task.Delay (100);
			IsBusy = false;
		}
		#endregion

		#region properties
		private bool _isBusy;
		public bool IsBusy
		{
			get { return _isBusy; }
			set { _isBusy = value; RaisePropertyChanged(() => IsBusy); }
		}

		private bool _enableSendMailL;
		public bool EnableSendMailL
		{
			get { return _enableSendMailL; }
			set { _enableSendMailL = value; RaisePropertyChanged(() => EnableSendMailL); }
		}

		private string _coDriver;
		public string CoDriver
		{
			get { return _coDriver; }
			set { _coDriver = value; RaisePropertyChanged(() => CoDriver); }
		}

		private string _shiftStart;
		public string ShiftStart
		{
			get { return _shiftStart; }
			set { _shiftStart = value; RaisePropertyChanged(() => ShiftStart); }
		}

		private string _onDutyLeft;
		public string OnDutyLeft
		{
			get { return _onDutyLeft; }
			set { _onDutyLeft = value; RaisePropertyChanged(() => OnDutyLeft); }
		}

		private string _drivingLeft;
		public string DrivingLeft
		{
			get { return _drivingLeft; }
			set { _drivingLeft = value; RaisePropertyChanged(() => DrivingLeft); }
		}

		private string _cycle;
		public string Cycle
		{
			get { return _cycle; }
			set { _cycle = value; RaisePropertyChanged(() => Cycle); }

		}

		private string _drive;
		public string Drive
		{
			get { return _drive; }
			set { _drive = value; RaisePropertyChanged(() => Drive); }
		}

		private string _equipment;
		public string Equipment
		{
			get { return _equipment; }
			set { _equipment = value; RaisePropertyChanged (() => Equipment); }
		}

		private string _odometer;
		public string Odometer
		{
			get { return _odometer; }
			set { _odometer = value; RaisePropertyChanged(() => Odometer); }

		}

		private string _odomTitle;
		public string OdomTitle
		{
			get { return _odomTitle; }
			set { _odomTitle = value; RaisePropertyChanged(() => OdomTitle); }
		}

		private string _startOdometer;
		public string StartOdometer
		{
			get { return _startOdometer; }
			set { _startOdometer = value; RaisePropertyChanged(() => StartOdometer); }
		}

		private string _distanceDriven;
		public string DistanceDriven
		{
			get { return _distanceDriven; }
			set { _distanceDriven = value; RaisePropertyChanged(() => DistanceDriven); }
		}

		private string _timeZone;
		public string TimeZone
		{
			get { return _timeZone; }
			set { _timeZone = value; RaisePropertyChanged(() => TimeZone); }
		}

		private string _homeAddress;
		public string HomeAddress
		{
			get { return _homeAddress; }
			set { _homeAddress = value; RaisePropertyChanged(() => HomeAddress); }
		}

		private string _licensePlate;
		public string LicensePlate
		{
			get { return _licensePlate; }
			set { _licensePlate = value; RaisePropertyChanged(() => LicensePlate); }
		}	

		private string _licenseProvince;
		public string LicenseProvince
		{
			get { return _licenseProvince; }
			set { _licenseProvince = value; RaisePropertyChanged(() => LicenseProvince); }
		}

		private string _shippingDocNumber="";
		public string ShippingDocNumber
		{
			get { return _shippingDocNumber; }
			set { _shippingDocNumber = value; RaisePropertyChanged(() => ShippingDocNumber); }
		}

		private string _permitNumber="";
		public string PermitNumber
		{
			get { return _permitNumber; }
			set { _permitNumber = value; RaisePropertyChanged(() => PermitNumber); }
		}

		private string _shippingDocNumberEdit="";
		public string ShippingDocNumberEdit
		{
			get { return _shippingDocNumberEdit; }
			set { _shippingDocNumberEdit = value; RaisePropertyChanged(() => ShippingDocNumberEdit); }
		}

		private string _permitNumberEdit="";
		public string PermitNumberEdit
		{
			get { return _permitNumberEdit; }
			set { _permitNumberEdit = value; RaisePropertyChanged(() => PermitNumberEdit); }
		}

		private string _trailerLicenseEdit;
		public string TrailerLicenseEdit
		{
			get { return _trailerLicenseEdit; }
			set { _trailerLicenseEdit = value; RaisePropertyChanged(() => TrailerLicenseEdit); }
		}

		private string _trailerLicense;
		public string TrailerLicense
		{
			get { return _trailerLicense; }
			set { _trailerLicense = value; RaisePropertyChanged(() => TrailerLicense); }
		}

		private string _orgName;
		public string OrgName
		{
			get { return _orgName; }
			set { _orgName = value; RaisePropertyChanged(() => OrgName); }
		}

		private string _orgAddress;
		public string OrgAddress
		{
			get { return _orgAddress; }
			set { _orgAddress = value; RaisePropertyChanged(() => OrgAddress); }
		}

		private DateTime _plottingDate;
		public DateTime PlottingDate
		{
			get { return _plottingDate; }
			set { _plottingDate = value; RaisePropertyChanged(() => PlottingDate); }
		}

		private List<TimeLogModel> _timeLogList;
		public List<TimeLogModel> TimeLogList
		{
			get { return _timeLogList; }
			set { _timeLogList = value; RaisePropertyChanged(() => TimeLogList); }
		}

		private TimeSpan _totalOffDuty = new TimeSpan(0);
		public TimeSpan TotalOffDuty
		{
			get { return _totalOffDuty; }
			set { 
				_totalOffDuty = value; 
				RaisePropertyChanged(() => TotalOffDuty); 
			}
		}

		private TimeSpan _totalOnDuty = new TimeSpan(0);
		public TimeSpan TotalOnDuty
		{
			get { return _totalOnDuty; }
			set { 
				_totalOnDuty = value; 
				RaisePropertyChanged(() => TotalOnDuty); 
			}
		}

		private TimeSpan _totalDriving = new TimeSpan(0);
		public TimeSpan TotalDriving
		{
			get { return _totalDriving; }
			set { 
				_totalDriving = value; 
				RaisePropertyChanged(() => TotalDriving); 
			}
		}

		private TimeSpan _totalSleeping = new TimeSpan(0);
		public TimeSpan TotalSleeping
		{
			get { return _totalSleeping; }
			set { 
				_totalSleeping = value; 
				RaisePropertyChanged(() => TotalSleeping); 
			}
		}

		private string _totalOffDutyStr = "00:00";
		public string TotalOffDutyStr
		{
			get { return _totalOffDutyStr; }
			set { 
				_totalOffDutyStr = value; 
				RaisePropertyChanged(() => TotalOffDutyStr); 
			}
		}

		private string _totalOnDutyStr = "00:00";
		public string TotalOnDutyStr
		{
			get { return _totalOnDutyStr; }
			set { 
				_totalOnDutyStr = value; 
				RaisePropertyChanged(() => TotalOnDutyStr); 
			}
		}

		private string _totalDrivingStr = "00:00";
		public string TotalDrivingStr
		{
			get { return _totalDrivingStr; }
			set { 
				_totalDrivingStr = value; 
				RaisePropertyChanged(() => TotalDrivingStr); 
			}
		}

		private string _totalSleepingStr = "00:00";
		public string TotalSleepingStr
		{
			get { return _totalSleepingStr; }
			set { 
				_totalSleepingStr = value; 
				RaisePropertyChanged(() => TotalSleepingStr); 
			}
		}

		private TimeSpan _total = new TimeSpan(0);
		public TimeSpan Total
		{
			get { return _total; }
			set { 
				_total = value; 
				RaisePropertyChanged(() => Total); 
			}
		}

		private string _totalStr;
		public string TotalStr
		{
			get { return _totalStr; }
			set { _totalStr = value; RaisePropertyChanged(() => TotalStr); }
		}

		private int _selectedTimeLogIndexOffSet = 0;
		public int SelectedTimeLogIndexOffSet
		{
			get { return _selectedTimeLogIndexOffSet; }
			set { _selectedTimeLogIndexOffSet = value; RaisePropertyChanged(() => SelectedTimeLogIndexOffSet); }
		}

		private List<Trip> _tripList = new List<Trip>();
		public List<Trip> TripList
		{
			get { return _tripList; }
			set { _tripList = value; RaisePropertyChanged(() => TripList); }
		}

		private Trip _selectedTrip;
		public Trip SelectedTrip
		{
			get { return _selectedTrip; }
			set { _selectedTrip = value;
				RaisePropertyChanged(() => SelectedTrip); 
			}
		}

		private List<Trip> _selectedTrips = new List<Trip>();
		public List<Trip> SelectedTrips
		{
			get { return _selectedTrips; }
			set { _selectedTrips = value; RaisePropertyChanged(() => SelectedTrips); }
		}

/*
		private bool _isEditable=false;
		public bool IsEditable
		{
			get { return _isEditable; }
			set { _isEditable = value; RaisePropertyChanged(() => IsEditable); }
		}
*/

		private bool _isAddEditable=true;
		public bool IsAddEditable
		{
			get { return _isAddEditable; }
			set { _isAddEditable = value; RaisePropertyChanged(() => IsAddEditable); }
		}

		private bool _canDelete=false;
		public bool CanDelete
		{
			get { return _canDelete; }
			set { _canDelete = value; RaisePropertyChanged(() => CanDelete); }
		}

//		private bool _closeMenu=false;
//		public bool CloseMenu
//		{
//			get { return _closeMenu; }
//			set { _closeMenu = value; RaisePropertyChanged(() => CloseMenu); }
//		}

		private bool isexpanded;
		public bool IsExpanded
		{
			get{ return isexpanded;}
			set{isexpanded = value;RaisePropertyChanged (()=>IsExpanded); }
		}

		#endregion

		#region Commands
		public ICommand SelectCheckbox
		{
			get {
				return new MvxCommand<Trip> ((selectedTrip) => {
					if(selectedTrip != null){
						if(TripList.Contains(selectedTrip)) {
							Trip er = TripList.Find(x => x==selectedTrip);
							er.IsSelected = !er.IsSelected;
						}
						ObservableCollection<Trip> tempColl = new ObservableCollection<Trip >(TripList);
						TripList = new List<Trip>(tempColl);

						if(!SelectedTrips.Contains(selectedTrip)){
							SelectedTrips.Add(selectedTrip);
						}else{
							SelectedTrips.Remove(selectedTrip);
						}
						if(SelectedTrips.Count==1) {
//							IsEditable = true; 
							IsAddEditable = true;
							SelectedTrip = SelectedTrips[0];
							ShippingDocNumberEdit = SelectedTrip.ShippingDocNumber;
							PermitNumberEdit = SelectedTrip.PermitNumber;
							TrailerLicenseEdit = SelectedTrip.TrailerLicense;
							CanDelete = true;
						} else {
//							IsEditable = false;
							IsAddEditable = false;
							SelectedTrip = null;
							ShippingDocNumberEdit = "";
							PermitNumberEdit = "";
							TrailerLicenseEdit = "";
							if(SelectedTrips.Count==0) {
								CanDelete = false;
							} else {
								CanDelete = true;
							}
						}

						if(SelectedTrips.Count==0) {
							IsAddEditable = true;
						}
					}

				});

			}
		}

		public ICommand AddOrEditTrip
		{
			get
			{
				return new MvxCommand (() => {
					TripInfoModel trm= new TripInfoModel();
					trm.CoWorker="";
					trm.Truck = Equipment;
					trm.BoxID = _dataService.GetAssetBoxId().ToString();
					trm.DriverId = _dataService.GetCurrentDriverId();
					trm.OrigLogTime = Utils.GetDateTimeUtcNow();
					trm.LogTime = Util.GetDateTimeNow();
					trm.VehicleLicense = LicensePlate;
					trm.VehicleLicenseProvince = LicenseProvince;
					trm.TrailerLicense = TrailerLicenseEdit;
					trm.BLNumber = ShippingDocNumberEdit;
					trm.Permit = PermitNumberEdit;

					if(SelectedTrip!=null) {
						trm.Id = SelectedTrip.Id;
						_tripInfoService.Update(trm);
					} else {
						trm.Id = -1;
						_tripInfoService.Insert(trm);
					}
					//Close menu, reload trip list, clear fields
					ShippingDocNumberEdit = "";
					PermitNumberEdit="";
					TrailerLicenseEdit="";
//					List<TripInfoModel> lsttrips = new List<TripInfoModel>();
//					lsttrips.Add(trm);
					sendTimeLog();
					//LoadTrips();
					//CloseMenu = true;
					SelectedTrips.Clear();
					SelectedTrip = null;

				});
			}
		}

		public ICommand EditbuttonClicked
		{
			get {
				return new MvxCommand(() => {
					ShippingDocNumberEdit = SelectedTrip.ShippingDocNumber;
					PermitNumberEdit = SelectedTrip.PermitNumber;
					TrailerLicenseEdit = SelectedTrip.TrailerLicense;
					//CloseMenu = true;
				});
			}
		}

		public ICommand ShowMore
		{
			get {
				return new MvxCommand(() => 
					Invert()
				);
			}
		}
		public ICommand ShowLess
		{
			get {
				return new MvxCommand(() => 
					Invert()
				);
			}
		}

		private MvxCommand _deleteTrip;
		public ICommand DeleteTrip
		{
			get
			{
				return new MvxCommand(() =>
					{	
						//called when delete is clicked
						OnDeleteTripDialog(new EventArgs());

					});
			}
		}

		private MvxCommand _deleteTripCommand;

		public ICommand DeleteTripCommand
		{
			get
			{
				return new MvxCommand(() =>
					{	
						_tripInfoService.DeleteSelected(SelectedTrips);
						_timeLogService.SetSignForDate(false, _dataService.GetCurrentDriverId(), PlottingDate);
						sendTimeLog();
						//CloseMenu = true;
						SelectedTrips.Clear();
						SelectedTrip = null;
						IsAddEditable = true;
						CanDelete = false;
					});
			}
		}
		#endregion

		#region EventHandlers
		public event EventHandler DeleteTripDialog;
		protected virtual void OnDeleteTripDialog(EventArgs e)
		{
			if (DeleteTripDialog != null)
			{
				DeleteTripDialog(this, e);
			}
		}
		#endregion

		#region methods
		public async Task PrepareLogData() {
			if (!IsBusy)
				IsBusy = true;
			await Task.Run (() => {
			List<TimeLogModel> primaryTimeLogData = _timeLogService.GetAllForDate (PlottingDate, _dataService.GetCurrentDriverId ());
			allTimeLogData = primaryTimeLogData;
			if (allTimeLogData.Count == 0) {
				lastPrvDayEvent = _timeLogService.GetLastBeforeDate (_dataService.GetCurrentDriverId (), PlottingDate);
			}
			TimeLogList = ValidateLogData (primaryTimeLogData);
			prepareTotalHours (TimeLogList);

			DistanceDriven = (hc != null && hc.ShiftStart != null) ? 
				(b_miles ? Convert.ToInt32 ((_timeLogService.GetDistanceAfterDateTime ((DateTime)hc.ShiftStart, _dataService.GetCurrentDriverId ())) * 0.621371).ToString () : 
					(_timeLogService.GetDistanceAfterDateTime ((DateTime)hc.ShiftStart, _dataService.GetCurrentDriverId ())).ToString ()) : "N/A";
			
			LoadTrips ();

			//Load equipment, licenseplate and licenseprovince
			Equipment = _dataService.GetAssetBoxDescription ();
			AssetModel asm = _assetService.GetAssetByBoxId (_dataService.GetAssetBoxId ());
			LicensePlate = asm == null ? "" : asm.VehicleLicense;
			LicenseProvince = asm == null ? "" : asm.VehicleLicenseProvince;
			});
		}

		public Trip GeneratePendingTripRowData (TripInfoModel logModel) {
			Trip tRow = new Trip();
			tRow.Id = logModel.Id;
			tRow.IsSelected = false;
			tRow.TripDateTime = string.Format ("{0:dddd, MMM dd, yyyy tt}", logModel.LogTime) +" | "+ string.Format ("{0:HH:mm:ss tt}", logModel.LogTime);
			tRow.TrailerLicense = logModel.TrailerLicense;
			tRow.ShippingDocNumber = logModel.BLNumber;
			tRow.PermitNumber = logModel.Permit;
			return tRow;
		}

		public void LoadTrips() {
			tripInfoList = _tripInfoService.GetAllTripInfo (_dataService.GetCurrentDriverId(), PlottingDate);
			if (tripInfoList != null) {
				foreach (TripInfoModel tir in tripInfoList) {
					if (ShippingDocNumber.Equals(""))
						ShippingDocNumber = tir.BLNumber;
					else
						ShippingDocNumber += ", " + tir.BLNumber;

					if (PermitNumber.Equals(""))
						PermitNumber = tir.Permit;
					else
						PermitNumber += ", " + tir.Permit;
				}

				List<Trip> tempTripList = new List<Trip> ();
				foreach (TripInfoModel logModel in tripInfoList) {
					tempTripList.Add (GeneratePendingTripRowData (logModel));
				}
				if (tempTripList.Count >= 0) {
					TripList = new List<Trip>(tempTripList);
				}
			}
		}

		private List<TimeLogModel> ValidateLogData(List<TimeLogModel> data)
		{
			List<TimeLogModel> logdata = new List<TimeLogModel>();

			if (data == null)
				return null;

			//If there are no events to show, just add last known event at mid-night to view it as current duty stauts
			if (data.Count == 0) {
				TimeLogModel lastTLR = _timeLogService.GetLastBeforeDate (_dataService.GetCurrentDriverId (), PlottingDate);
				// TimeLogRow lastTLR = TimeLogRepository.GetLastBeforeDate (SentinelMobile.Shared.Communication.GlobalInstance.CurrentDriverId, plottingDate);
				if (lastTLR != null) {
					lastTLR.LogTime = PlottingDate.Date;
					data.Add (lastTLR);
				} else {
					//return null;
					TimeLogModel tempLog = new TimeLogModel();
					LocalizeTimeLog(ref tempLog);
					tempLog.Type = (int)TimeLogType.Auto;
					logdata.Add (tempLog);
					return logdata;
				}
			}

			logdata.AddRange(data);

			TimeLogModel tlr = logdata[logdata.Count - 1];

			//Add a Auto TimeLog with the last event till Mid-Night or current time if viewing for today
			TimeLogModel t = new TimeLogModel();
			LocalizeTimeLog(ref t);
			t.LogStatus = tlr.LogStatus;
			t.Latitude = tlr.Latitude;
			t.Longitude = tlr.Longitude;
			t.Type = (int)TimeLogType.Auto;
			DateTime tmpNowDate = Util.GetDateTimeNow ();
			if (tlr.LogTime.Date == tmpNowDate.Date)
				t.LogTime = tmpNowDate;
			else
				t.LogTime = tlr.LogTime.Date.AddDays(1);

			logdata.Add(t);

			//Also if the first node doesn't start at mid-night -> check last event before mid-night and add that event log at mid-night
			//if no last event exist use the first log event and insert one at mid-night
			tlr = logdata[0];
			if (tlr.LogTime != tlr.LogTime.Date) {
				t = new TimeLogModel ();
				LocalizeTimeLog(ref t);
				t.Latitude = tlr.Latitude;
				t.Longitude = tlr.Longitude;
				t.Type = (int)TimeLogType.Auto;
				t.LogTime = tlr.LogTime.Date;

				TimeLogModel lastBeforeMidNight = _timeLogService.GetLastBeforeDate (_dataService.GetCurrentDriverId (), tlr.LogTime.Date);
				// TimeLogRow lastBeforeMidNight = TimeLogRepository.GetLastBeforeDate (SentinelMobile.Shared.Communication.GlobalInstance.CurrentDriverId, tlr.LogTime.Date);
				if (lastBeforeMidNight != null) { 
					t.LogStatus = lastBeforeMidNight.LogStatus;
					t.Event = lastBeforeMidNight.LogStatus;
				} else {
					t.LogStatus = tlr.LogStatus;
					t.Event = lastBeforeMidNight.LogStatus;
				}

				logdata.Insert (0, t);
				//If we are adding an Auto event for midnight let's change ouroff set to 1 so that correct portion gets selected based on the actual timelog list that user sees
				SelectedTimeLogIndexOffSet = 1;
			}

			return logdata;
		}

		public void prepareTotalHours(List<TimeLogModel> data) {
			if (data == null || data.Count == 0) 
				return;
			for (int i = 1; i < data.Count; i++)
			{
				TimeSpan ts = new TimeSpan (data [i].LogTime.Ticks - data [i - 1].LogTime.Ticks);
				switch (data[i - 1].LogStatus)
				{
				case (int)LOGSTATUS.OffDuty:
					TotalOffDuty = TotalOffDuty.Add (ts);
					TotalOffDutyStr = TotalOffDuty.ToString (@"hh\:mm");
					if (TotalOffDutyStr == "00:00") {
						TotalOffDutyStr = "24:00";
					}
					break;
				case (int)LOGSTATUS.OnDuty:
					TotalOnDuty = TotalOnDuty.Add(ts);
					TotalOnDutyStr = TotalOnDuty.ToString (@"hh\:mm");
					if (TotalOnDutyStr == "00:00") {
						TotalOnDutyStr = "24:00";
					}
					break;
				case (int)LOGSTATUS.Driving:
					TotalDriving = TotalDriving.Add(ts);
					TotalDrivingStr = TotalDriving.ToString (@"hh\:mm");
					if (TotalDrivingStr == "00:00") {
						TotalDrivingStr = "24:00";
					}
					break;
				case (int)LOGSTATUS.Sleeping:
					TotalSleeping = TotalSleeping.Add(ts);
					TotalSleepingStr = TotalSleeping.ToString (@"hh\:mm");
					if (TotalSleepingStr == "00:00") {
						TotalSleepingStr = "24:00";
					}
					break;
				}
			}
			Total = TotalDriving + TotalOffDuty + TotalOnDuty + TotalSleeping;
			if (Total.Days == 1) {
				TotalStr = "24:00";
			} else {
				TotalStr = Total.ToString (@"hh\:mm");
			}

		}

		public async void sendTimeLog() {
			List<TripInfoModel> list_unsent_trip_info = _tripInfoService.GetAllUnSent ();
			bool ack = await _communicationService.SendTripInfo(list_unsent_trip_info);
			if (ack) {
				LoadTrips ();
				_tripInfoService.SetHaveSentDeleteOlder ();
			}
			//_messenger.Publish<RefreshMessage> (new RefreshMessage (this, PlottingDate));
		}

/*		List<TripInfoRow> list_unsent_trip_info = TripInfoRepository.GetAllUnSent ();
		if (list_unsent_trip_info != null && list_unsent_trip_info.Count > 0) 
		{
			SetTCPinUse(true);
			TCPCommunicationUtility tcp = new TCPCommunicationUtility ();
			bool rv=tcp.SendTripInfo(list_unsent_trip_info);
			postTripInfo_cb(rv);
		}
*/
		public void Invert(){
			IsExpanded = !IsExpanded;
		}

		#endregion
	}

}

