using MvvmCross.Core.ViewModels;
using System.Windows.Input;
using MvvmCross.Plugins.Messenger;
using BSM.Core.Services;
using MvvmCross.Plugins.File;
using System;
using BSM.Core.Messages;
using System.Threading.Tasks;
using System.Net.Http;
using System.Collections.Generic;

using BSM.Core.AuditEngine;
using BSM.Core.ConnectionLibrary;
using System.Linq;
using MvvmCross.Core;
using MvvmCross.Platform;
using MvvmCross.Platform.Core;
using MvvmCross.Platform.Platform;

namespace BSM.Core.ViewModels
{
	public class DashboardViewModel : BaseViewModel
    {
		#region Member Variables
		private readonly ILastSessionService _sessionService;
		private readonly IInspectionReportService _reportService;
		private readonly IDataService _dataservice;
		private readonly IDashBoardNotificationService _notification;
		private readonly IHourCalculatorService _hourCalcServive;
		private readonly ISyncService _syncService;
		private readonly IHosAlertService _hosalert;
		private readonly IBreakTimerService _breakTimerservice;
		private readonly ILanguageService _languageService;
		private readonly ITimeLogService _timelogService;
		private readonly IEmployeeService _employeeService;
		private readonly ICoWorkerService _coworkerService;
		private readonly IBSMBoxWifiService _bsmBoxWifiService;
		private readonly IMvxMessenger _messenger;

		private readonly MvxSubscriptionToken _breakTimerUpdateMessage;
		private readonly MvxSubscriptionToken _driverStatusMessage;
		private readonly MvxSubscriptionToken _breakTimerCompleteMessage;
		private readonly MvxSubscriptionToken _breakTimerCancelMessage;
		private readonly MvxSubscriptionToken _refreshVehicle;
		private readonly MvxSubscriptionToken _cycleSuccess;
		#endregion

		#region ctors
		public DashboardViewModel(IDataService dataservice,IDashBoardNotificationService notification, ISyncService syncService,IHosAlertService hosalert,IBreakTimerService  breakTimerservice, ILanguageService languageService,IBSMBoxWifiService bsmBoxWifiService,
			ITimeLogService timelogservice,IEmployeeService employeeservice,ICoWorkerService coworkerservice,IMvxMessenger messenger,IInspectionReportService reportService,ILastSessionService sessionService)
		{
			_sessionService = sessionService;
			_reportService = reportService;
			_timelogService = timelogservice;
			_employeeService = employeeservice;
			_coworkerService = coworkerservice;
			_bsmBoxWifiService = bsmBoxWifiService;
			_breakTimerservice = breakTimerservice;
			_hosalert = hosalert;
			_messenger = messenger;
			_dataservice = dataservice;
			_notification = notification;
			_syncService = syncService;
			_languageService = languageService;
			GetNotifications ();
			_hourCalcServive = Mvx.Resolve<IHourCalculatorService> ();
			_hourCalcServive.runHourCalculatorTimerNow ();
			GenerateGraphValues ();
			UpdateStatus ();

			_driverStatusMessage = _messenger.SubscribeOnMainThread<UpdateDriverStatusMessage>((message) =>
				{
					MvxSingleton<IMvxMainThreadDispatcher>.Instance.RequestMainThreadAction(()=>{						
						SelectedDriverStatus = message.driverStatusType;
					}).DisposeIfDisposable();
				});

			_breakTimerUpdateMessage = _messenger.SubscribeOnMainThread<BreakTimerUpdateMessage>((message) =>
				{
					if(SelectedDriverStatus != null){
						MvxSingleton<IMvxMainThreadDispatcher>.Instance.RequestMainThreadAction(()=>{
							GC.Collect();
							TimeSpan ts = TimeSpan.FromSeconds (message.Remaining);
							_selectedDriverStatus.driverStatusTimeText = ts.ToString (@"hh\:mm\:ss");
							Mvx.Trace (MvxTraceLevel.Diagnostic,ts.ToString (@"hh\:mm\:ss"));
						});
					}
				});

			_breakTimerCompleteMessage = _messenger.Subscribe<BreakTimerCompleteMessage>((message) =>
				{
					BreakCompleteAlert = true;
				});

			_breakTimerCancelMessage = _messenger.Subscribe<BreakTimerCancelMessage>((message) =>
				{
					BreakCancel = true;
				});

			_refreshVehicle = _messenger.SubscribeOnMainThread<RefeshVehicleStatusMessage>((message) =>
				{
					if (_dataservice.GetAssetBoxId () == -1) {
						DefectLevel = 2;
						HasInspectionDoneIn24Hrs = 2;
					} else {
						DefectLevel = _reportService.HasMajorDefects24Hrs (_dataservice.GetAssetBoxId ());
						HasInspectionDoneIn24Hrs = _reportService.HasInspectionDoneIn24Hrs (_dataservice.GetAssetBoxId ());
					}
				});

			_cycleSuccess = _messenger.Subscribe<UpdateCycleMessage>((message) =>
				{
					var currentEmployee = EmployeeDetail();
					CycleDescription =string.Join(" ",CycleList.FirstOrDefault (p=>p.Id == currentEmployee.Cycle).CycleDescription.Split('_'));
					SelectedExcemptions = message.Excemptions;
					if(SelectedExcemptions != null && SelectedExcemptions.Count > 0){
						EnableExcemption = true;
						ExcemptionDescription =SelectedExcemptions[0];
						if (SelectedExcemptions.Count == 1) {
							RightEnable = false;
							LeftEnable = false;
						} else {
							LeftEnable = false;
							RightEnable = true;
						}
					}else{
						ExcemptionDescription = string.Empty;
						EnableExcemption = false;
					}
				});
			_sessionService.SaveLastSession(_dataservice.GetCurrentDriverId(),_dataservice.GetAssetBoxId(),_dataservice.GetAssetBoxDescription(),_dataservice.GetCurrentLogStatus(),_dataservice.GetScannedTimeStamp());
		}


		public void Init(bool onrestore = false){
			if(onrestore){
				_bsmBoxWifiService.startBoxConnectivityTimer ();
				_bsmBoxWifiService.startBoxDataTimer ();
			}
		}

		public async void GenerateGraphValues () {
			IsBusy = true;
			IsGraphGenerated = false;
			await Task.Delay (1000);
			HourCalculator hourCalculator = _hourCalcServive.getHourCalculator ();
			if (hourCalculator != null) {
				int driveTime = (hourCalculator.AvaliableDrivingMinutes != int.MaxValue && hourCalculator.AvaliableDrivingMinutes != int.MinValue && hourCalculator.AvaliableDrivingMinutes > 0) ? hourCalculator.AvaliableDrivingMinutes : 0;
				int maxDriveTime = ((hourCalculator.MaxDriving > 0) ? hourCalculator.MaxDriving : 0) * 60; // Convert hours to minutes
				DrivingAngle = ((float)driveTime/(float)maxDriveTime);
				DrivingTimeStr = Util.HHmmFromMinutes (driveTime);

				int onDutyTime = (hourCalculator.AvaliableOnDutyMinutes != int.MaxValue && hourCalculator.AvaliableOnDutyMinutes != int.MinValue && hourCalculator.AvaliableOnDutyMinutes > 0) ? hourCalculator.AvaliableOnDutyMinutes : 0;
				int maxOnDutyTime = ((hourCalculator.MaxOnduty > 0) ? hourCalculator.MaxOnduty : 0) * 60; // Convert hours to minutes
				OnDutyAngle = ((float)onDutyTime/(float)maxOnDutyTime);
				OnDutyTimeStr = Util.HHmmFromMinutes (onDutyTime);

				int breakTime = _breakTimerservice.GetTotalBreakTimeInSec () - 1;
				int maxBreakTime = 1800;
				BreakAngle = ((float)breakTime/(float)maxBreakTime);
				BreakTimeStr = TimeSpan.FromSeconds (breakTime).ToString (@"hh\:mm");

				int cycleTime = (hourCalculator.AvaliableCycle > 0) ? hourCalculator.AvaliableCycle : 0;
				int maxCycleTime = ((hourCalculator.MaxCycle > 0) ? hourCalculator.MaxCycle : 0) * 60; // Convert hours to minutes
				CycleAngle = ((float)cycleTime/(float)maxCycleTime);
				CycleTimeStr = Util.HHmmFromMinutes (cycleTime);
				IsGraphGenerated = true;
			}
			IsBusy = false;
		}
		#endregion

		#region Properties
		private bool _isBusy;
		public bool IsBusy
		{
			get { return _isBusy; }
			set { _isBusy = value; RaisePropertyChanged(() => IsBusy); }
		}

		private bool _isGraphGenerated;
		public bool IsGraphGenerated
		{
			get { return _isGraphGenerated; }
			set { _isGraphGenerated = value; RaisePropertyChanged(() => IsGraphGenerated); }
		}
		private double lat;
		public double Lat
		{
			get { return lat; }
			set
			{
				lat = value;
				RaisePropertyChanged(() => Lat);
			}
		}

		private double lng;
		public double Lng
		{
			get
			{
				return lng;
			}
			set
			{
				lng = value;
				RaisePropertyChanged(() => Lng);
			}
		}

		private string _cityName;
		public string CityName
		{
			get
			{
				return _cityName;
			}
			set
			{
				_cityName = value;
				RaisePropertyChanged(() => CityName);
			}
		}

		private string _temperature;
		public string Temperature
		{
			get
			{
				return _temperature;
			}
			set
			{
				_temperature = value;
				RaisePropertyChanged(() => Temperature);
			}
		}

		private string _weatherDescription;
		public string WeatherDescription
		{
			get
			{
				return _weatherDescription;
			}
			set
			{
				_weatherDescription = value;
				RaisePropertyChanged(() => WeatherDescription);
			}
		}

		private string _weatherIconUrl;
		public string WeatherIconUrl
		{
			get
			{
				return _weatherIconUrl;
			}
			set
			{
				_weatherIconUrl = value;
				RaisePropertyChanged(() => WeatherIconUrl);
			}
		}

		private List<HosAlertModel> notificationList = new List<HosAlertModel>();
		public List<HosAlertModel> NotificationList
		{
			get{return notificationList; }
			set{notificationList = value;RaisePropertyChanged (()=>NotificationList); }
		}

		private float _drivingAngle;
		public float DrivingAngle
		{
			get
			{
				return _drivingAngle;
			}
			set
			{
				_drivingAngle = value;
				RaisePropertyChanged(() => DrivingAngle);
			}
		}

		private string _drivingTimeStr = "";
		public string DrivingTimeStr
		{
			get
			{
				return _drivingTimeStr;
			}
			set
			{
				_drivingTimeStr = value;
				RaisePropertyChanged(() => DrivingTimeStr);
			}
		}

		private float _onDutyAngle;
		public float OnDutyAngle
		{
			get
			{
				return _onDutyAngle;
			}
			set
			{
				_onDutyAngle = value;
				RaisePropertyChanged(() => OnDutyAngle);
			}
		}

		private string _onDutyTimeStr = "";
		public string OnDutyTimeStr
		{
			get
			{
				return _onDutyTimeStr;
			}
			set
			{
				_onDutyTimeStr = value;
				RaisePropertyChanged(() => OnDutyTimeStr);
			}
		}

		private float _breakAngle;
		public float BreakAngle
		{
			get
			{
				return _breakAngle;
			}
			set
			{
				_breakAngle = value;
				RaisePropertyChanged(() => BreakAngle);
			}
		}

		private string _breakTimeStr = "";
		public string BreakTimeStr
		{
			get
			{
				return _breakTimeStr;
			}
			set
			{
				_breakTimeStr = value;
				RaisePropertyChanged(() => BreakTimeStr);
			}
		}

		private float _cycleAngle;
		public float CycleAngle
		{
			get
			{
				return _cycleAngle;
			}
			set
			{
				_cycleAngle = value;
				RaisePropertyChanged(() => CycleAngle);
			}
		}

		private string _cycleTimeStr = "";
		public string CycleTimeStr
		{
			get
			{
				return _cycleTimeStr;
			}
			set
			{
				_cycleTimeStr = value;
				RaisePropertyChanged(() => CycleTimeStr);
			}
		}
		private bool _isCycle = false;
		public bool IsCycle{
			get
			{
				return _isCycle;
			}
			set
			{
				_isCycle = value;
				RaisePropertyChanged(() => IsCycle);
			}
		}
		#endregion

		#region Commands
		public ICommand AddPreForm
		{
			get{return new MvxCommand (()=>NaviGateForms(1)); }
		}

		public ICommand AddPostForm
		{
			get{return new MvxCommand (()=>NaviGateForms(2)); }
		}

		public ICommand AddFuelForm
		{
			get{return new MvxCommand (()=>NaviGateForms(3)); }

		}

		public ICommand AddShippingDoc
		{
			get {
				return new MvxCommand (() => NaviGateShippingDoc());
			}
		}
        //test Command
        public ICommand GoToSelectAssetView
        {
            get
            {
                return new MvxCommand(() =>
                {
                    ShowViewModel<SelectAssetViewModel>();
                });
            }
        }
        //tes-command
        public ICommand GoToInspectionListView
        {
            get
            {
                return new MvxCommand(() =>
                {
                    ShowViewModel<InspectionListViewModel>();
                });
            }
        }

        private void OnWeatherMessage(WeatherMessage weatherMessage)
		{
			this.CityName = weatherMessage.CityName;
			this.Temperature = weatherMessage.Temperature;
			this.WeatherDescription = weatherMessage.Description;
			this.WeatherIconUrl = weatherMessage.WeatherIconUrl;
		}
		#endregion

		#region unsubscribe
		public void Unsubscribe()
		{
			_messenger.Unsubscribe<BreakTimerUpdateMessage> (_breakTimerUpdateMessage);
			_messenger.Unsubscribe<UpdateDriverStatusMessage> (_driverStatusMessage);
			_messenger.Unsubscribe<BreakTimerCompleteMessage> (_breakTimerCompleteMessage);
			_messenger.Unsubscribe<BreakTimerCancelMessage> (_breakTimerCancelMessage);
			_messenger.Unsubscribe<UpdateCycleMessage> (_cycleSuccess);
			_messenger.Unsubscribe<RefeshVehicleStatusMessage> (_refreshVehicle);
			_messenger.RequestPurgeAll ();
		}

		public void UnsubscribeIOS()
		{
			_messenger.Unsubscribe<BreakTimerUpdateMessage> (_breakTimerUpdateMessage);
			_messenger.Unsubscribe<UpdateDriverStatusMessage> (_driverStatusMessage);
			_messenger.Unsubscribe<RefeshVehicleStatusMessage> (_refreshVehicle);
			_messenger.Unsubscribe<UpdateCycleMessage> (_cycleSuccess);
			_messenger.RequestPurgeAll ();
		}
		#endregion
		void ShowDialog()
		{
			if (_dataservice.GetAssetBoxId () != -1) {
				OnFeatureUnderDevelopment (new ErrorMessageEventArgs (){Message = "Feature Under Developement"});
			} else {
				OnFeatureUnderDevelopment (new ErrorMessageEventArgs (){Message = _languageService.GetLocalisedString(Constants.str_vehicle)});
			}

		}

		void NaviGateForms(int formType)
		{
			if (_dataservice.GetAssetBoxId () != -1) {
				object _sync = new object ();
				lock (_sync) {
					ShowViewModel<InspectionListViewModel> (new {formType = formType,pagefrom = "DashBoard"});
				}	
			} else {
				_dataservice.PersistAssetBoxId (_dataservice.GetAssetBoxId() == -1 ? 0 : _dataservice.GetAssetBoxId());
				ShowViewModel<SelectAssetViewModel> (new {formType = formType});
			}
			UnSubScribeFromBaseViewModel ();
			this.Close (this);
		}

		private void NaviGateShippingDoc () {
			ShowViewModel<HOSMainViewModel> (new { pageFrom = "Dashboard"});
			UnSubScribeFromBaseViewModel ();
			this.Close (this);
		}

		void GetNotifications()
		{			
			NotificationList = _hosalert.GetHosAlerts();
		}

		private DriverStatusTypeClass _selectedDriverStatus = new DriverStatusTypeClass(-1, "", "","", "");
		public DriverStatusTypeClass SelectedDriverStatus
		{
			get { return _selectedDriverStatus; }
			set {
				if (value == null || (value != null && _dataservice.GetCurrentLogStatus () == value.driverStatusType)) {
					_selectedDriverStatus = value;
					RaisePropertyChanged (() => SelectedDriverStatus);
					return;
				}
				_selectedDriverStatus = value;
				var isbreak = false;
				var prevStatus = _dataservice.GetCurrentLogStatus ();
				var tlr = new TimeLogModel ();
				//If we were in Emergency mode and now a new status is selected; let's end the emergency use
				if( prevStatus ==(int) LOGSTATUS.Emergency && DriverStatusTypes.IndexOf(_selectedDriverStatus) != 5){ // 5 is Emergency	
					LocalizeTimeLog(ref tlr);
					tlr.Event = _timelogService.GetLastByLogbookstopid(_dataservice.GetCurrentDriverId(), AuditLogic.EmergencyUseStart).Event;
					tlr.LogStatus = _timelogService.GetLastByLogbookstopid(_dataservice.GetCurrentDriverId(), AuditLogic.EmergencyUseStart).Event; //We end emergency mode with status that we started with
					tlr.Logbookstopid = AuditLogic.EmergencyUseEnd;
					_timelogService.Insert (tlr);
				}
				//If we were in PersonalUse mode and now a new status is selected; let's end the PersonalUse
				if( prevStatus ==(int) LOGSTATUS.PersonalUse && DriverStatusTypes.IndexOf(_selectedDriverStatus) != 6){ // 6 is PersonalUse
					LocalizeTimeLog(ref tlr);
					tlr.Event =(int) LOGSTATUS.OffDuty;
					tlr.LogStatus =(int) LOGSTATUS.OffDuty;
					tlr.Logbookstopid = AuditLogic.PersonalUseEnd;
					_timelogService.Insert (tlr);
					BSMBoxWifiService.personalUsageLog = null;
				}
				//If we were in 30 MIN BREAK mode and now a new status is selected; let's end the 30 MIN BREAK timer
				if( prevStatus ==(int) LOGSTATUS.Break30Min && DriverStatusTypes.IndexOf(_selectedDriverStatus) != 4){ // 4 is 30 MIN BREAK
					_breakTimerservice.stop30MinBreakTimer();
					var employeeList = _employeeService.EmployeeList ().Where(p=>p.Domain == _dataservice.GetDomain()).ToList();
					var coworkerList = _coworkerService.CoWorkerList ();
					var joinList = (from emp in employeeList
						join worker in coworkerList on emp.Id equals worker.EmployeeID
						select emp).ToList<EmployeeModel>();
					if(joinList != null && joinList.Count > 0){
						foreach(var er in joinList){
							var	timelog = _timelogService.GetLast (er.Id);
							if (tlr.Event == (int)LOGSTATUS.OffDuty && tlr.Logbookstopid == AuditLogic.ThirtyMinutesOffDutyStart) {
								var tLog = new TimeLogModel ();
								LocalizeTimeLog(ref tLog);
								tLog.DriverId = er.Id;
								tLog.CoDriver = "";
								tLog.Event =(int) LOGSTATUS.OnDuty;
								tLog.LogStatus =(int) LOGSTATUS.OnDuty;
								tLog.Logbookstopid = AuditLogic.OnDuty;
								_timelogService.Insert (tLog);
							}
						}
					}
				}

				switch(DriverStatusTypes.IndexOf(_selectedDriverStatus))
				{
				case (int)LOGSTATUS.OffDuty-101:
					if (prevStatus == (int)LOGSTATUS.OffDuty)
						break;
					tlr = new TimeLogModel ();
					LocalizeTimeLog (ref tlr);
					tlr.Event = (int)LOGSTATUS.OffDuty;
					tlr.Logbookstopid = AuditLogic.OffDuty;
					tlr.LogStatus = (int)LOGSTATUS.OffDuty;
					_timelogService.Insert (tlr);
					_dataservice.PersistCurrentLogStatus ((int)LOGSTATUS.OffDuty);
					//_selectedDriverStatus.driverStatusTimeText = TotalOffDutyforStatus.ToString (@"hh\:mm");
					break;
				case (int)LOGSTATUS.Sleeping-101:
					if(prevStatus ==(int) LOGSTATUS.Sleeping)
						break;
					tlr = new TimeLogModel ();
					LocalizeTimeLog(ref tlr);
					tlr.Event =(int) LOGSTATUS.Sleeping;
					tlr.LogStatus = (int)LOGSTATUS.Sleeping;
					tlr.Logbookstopid = AuditLogic.Sleeping;
					_timelogService.Insert (tlr);
					_dataservice.PersistCurrentLogStatus ((int)LOGSTATUS.Sleeping);
					//_selectedDriverStatus.driverStatusTimeText = TotalSleepingforStatus.ToString (@"hh\:mm");
					break;
				case (int)LOGSTATUS.Driving-101:					
					if(prevStatus ==(int) LOGSTATUS.Driving)
						break;
					tlr = new TimeLogModel ();
					LocalizeTimeLog(ref tlr);
					tlr.Event =(int) LOGSTATUS.Driving;
					tlr.Logbookstopid = AuditLogic.Driving;
					tlr.LogStatus = (int)LOGSTATUS.Driving;
					_timelogService.Insert (tlr);
					_dataservice.PersistCurrentLogStatus ((int)LOGSTATUS.Driving);
					//_selectedDriverStatus.driverStatusTimeText = TotalDrivingforStatus.ToString (@"hh\:mm");
					//Check for Alerts on Driving status we have to check
					_bsmBoxWifiService.Check4AlertsOnDriving(tlr.LogTime);
					break;
				case (int)LOGSTATUS.OnDuty-101:
					if(prevStatus ==(int) LOGSTATUS.OnDuty)
						break;
					tlr = new TimeLogModel ();
					LocalizeTimeLog(ref tlr);
					tlr.Event =(int) LOGSTATUS.OnDuty;
					tlr.Logbookstopid = AuditLogic.OnDuty;
					tlr.LogStatus = (int)LOGSTATUS.OnDuty;
					_timelogService.Insert (tlr);
					_dataservice.PersistCurrentLogStatus ((int)LOGSTATUS.OnDuty);
					//_selectedDriverStatus.driverStatusTimeText = TotalOnDutyforStatus.ToString (@"hh\:mm");
					break;
				case (int)LOGSTATUS.Break30Min-101:
					if (prevStatus == (int)LOGSTATUS.Break30Min)
						break;
					isbreak = true;
					break;
				case (int)LOGSTATUS.Emergency-101:					
					if (prevStatus == (int)LOGSTATUS.Emergency)
						break;
					OnShowCommentDialog (new EventArgs());
					break;
				case (int)LOGSTATUS.PersonalUse-101:
					if(prevStatus ==(int) LOGSTATUS.PersonalUse)
						break;
					if (EmployeeDetail().Cycle.ToString ().ToLower ().StartsWith ("ca")) {
						var	curPersonalUsage = _timelogService.GetPersonalUsageForDateDriver (Util.GetDateTimeNow().Date, _dataservice.GetCurrentDriverId());
						if (curPersonalUsage > 75) {
							/*Display no personal usage left for today
							Toast.MakeText (this.ctx, this.ctx.Resources.GetString(Resource.String.str_no_personal_usage_left), ToastLength.Short).Show ();*/
							BSMBoxWifiService.personalUsageLog = null;
						} else {
							BSMBoxWifiService.personalUsageLog = new BSMBoxWifiService.PersonalUseStatusLog ();
							BSMBoxWifiService.personalUsageLog.StartDateTime = Utils.GetDateTimeNow ();
							BSMBoxWifiService.personalUsageLog.PrevOdo = _dataservice.GetOdometer ();
							BSMBoxWifiService.personalUsageLog.BoxId = _dataservice.GetAssetBoxId ();
							BSMBoxWifiService.personalUsageLog.Distance = _timelogService.GetPersonalUsageForDateDriver (Utils.GetDateTimeNow ().Date, _dataservice.GetCurrentDriverId ());
							tlr = new TimeLogModel ();
							LocalizeTimeLog(ref tlr);
							tlr.Event =(int) LOGSTATUS.OffDuty;
							tlr.LogStatus =(int) LOGSTATUS.OffDuty;
							tlr.Logbookstopid = AuditLogic.PERSONALUSE;
							_timelogService.Insert (tlr);
							_dataservice.PersistCurrentLogStatus((int)LOGSTATUS.PersonalUse);
						}
					} else {
						_dataservice.ClearStartDateTime();
						_dataservice.ClearDistance();
						tlr = new TimeLogModel ();
						LocalizeTimeLog(ref tlr);
						tlr.Event =(int) LOGSTATUS.OffDuty;
						tlr.LogStatus =(int) LOGSTATUS.OffDuty;
						tlr.Logbookstopid = AuditLogic.PERSONALUSE;
						_timelogService.Insert (tlr);
						_dataservice.PersistCurrentLogStatus((int)LOGSTATUS.PersonalUse);
					}
					break;
				}
				if(prevStatus != -1 && prevStatus != _dataservice.GetCurrentLogStatus()){
					_hourCalcServive.runHourCalculatorTimer();
					_syncService.runTimerCallBackNow();
					if(prevStatus == (int)LOGSTATUS.Driving && _dataservice.GetIsScreenLocked()){
						_dataservice.SetIsScreenLocked (false);
						_bsmBoxWifiService.setLastEcmStatus ();
					}
				}
				if(isbreak){
					OnShowBreakAlert (new EventArgs());
				}
				RaisePropertyChanged(() => SelectedDriverStatus);
			}
		}
		void UpdateStatus(){
			var lastTimelog = _timelogService.GetLast (_dataservice.GetCurrentDriverId());
			if (_dataservice.GetCurrentLogStatus () == (int)LOGSTATUS.Break30Min) {
				if (!_breakTimerservice.isTimerRunning ()) {
					try {
						var totalBreakInsec = Convert.ToInt32 ((Utils.GetDateTimeNow () - lastTimelog.LogTime.ToUniversalTime ()).TotalSeconds);
						_breakTimerservice.SettotalBreakTimeInSec (totalBreakInsec);
					} catch (Exception exp) {
						Mvx.Trace (MvxTraceLevel.Error,"While Updating The BreakTimer"+exp.ToString());
					}
					_breakTimerservice.start30MinBreakTimer ();
				}
			} else {
				_breakTimerservice.stop30MinBreakTimer ();
			}
			var curPersonalUsage1 = _timelogService.GetPersonalUsageForDateDriver (Utils.GetDateTimeNow().Date, _dataservice.GetCurrentDriverId());
			if (curPersonalUsage1 > 0) {
				/*this.view.FindViewById<LinearLayout>(Resource.Id.hos_personal_distance).Visibility = ViewStates.Visible;
					this.view.FindViewById<View>(Resource.Id.hos_personal_distance_divider).Visibility = ViewStates.Visible;
					this.view.FindViewById<TextView> (Resource.Id.txtV_hos_personal_distance_lable).Visibility = ViewStates.Visible;
					txtPersonalUsage = this.view.FindViewById<TextView> (Resource.Id.txtV_hos_personal_distance);
					txtPersonalUsage.Visibility = ViewStates.Visible;
					txtPersonalUsage.Text = curPersonalUsage.ToString ();*/
			}
			var lastEvent = _dataservice.GetCurrentLogStatus ();
			_selectedDriverStatus = DriverStatusTypes.FirstOrDefault (p=>p.driverStatusType == lastEvent);
		}

    }
}
