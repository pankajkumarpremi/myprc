using System;
using MvvmCross.Core.ViewModels;
using BSM.Core.ViewModels;
using BSM.Core.Services;

using MvvmCross.Plugins.Messenger;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BSM.Core.ConnectionLibrary;
using Acr.MvvmCross.Plugins.Network;
//using MvxPlugins.Geocoder;
using BSM.Core.AuditEngine;
using System.Linq;
using BSM.Core.Messages;
using Acr.Notifications;
using MvvmCross.Core;
using MvvmCross.Platform;
using MvvmCross.Platform.Core;

namespace BSM.Core
{
	public class BaseViewModel : MvxViewModel
	{
		#region Member Variables
		private readonly ILastSessionService _sessionService;
		private readonly IDataService _dataService;
		private readonly IEmployeeService _employeeService;
		private readonly IInspectionReportService _reportService;
		private readonly ITimeLogService _timeLogService;
		private readonly IMvxMessenger _messenger;
		private readonly INetworkService _network;
		private readonly ISettingsService _settingServive;
		private readonly ISyncService _syncService;
		private readonly IBreakTimerService _breakTimerservice;
		private readonly IHourCalculatorService _hourCalculatorService;
		private readonly IBSMBoxWifiService _bsmBoxWifiService;
		private readonly IHosAlertService _hosAlertService;
		//private readonly IMessagingService _messagingService;
		private readonly ICoWorkerService _coworkerService;
		private readonly ILanguageService _languageService;
		private readonly IMessagingRepositoryService _messagingReposioryService;

		private readonly MvxSubscriptionToken _boxConnectivityMessage;
		private readonly MvxSubscriptionToken _boxDataMessage;
		private readonly MvxSubscriptionToken _networkStatusChanged;
		private readonly MvxSubscriptionToken _updatestatusformService;
		private readonly MvxSubscriptionToken _MsgSubscribeToken =  null;
		#endregion

		#region ctors
		public BaseViewModel ()
		{
			_sessionService = Mvx.Resolve<ILastSessionService> ();
			_dataService = Mvx.Resolve<IDataService> ();
			_employeeService = Mvx.Resolve<IEmployeeService> ();
			_reportService = Mvx.Resolve<IInspectionReportService>();
			_timeLogService = Mvx.Resolve<ITimeLogService>();
			_messenger = Mvx.Resolve<IMvxMessenger> ();
			_network = Mvx.Resolve<INetworkService> ();
			_settingServive = Mvx.Resolve<ISettingsService> ();
			_hourCalculatorService = Mvx.Resolve<IHourCalculatorService> ();
			_breakTimerservice = Mvx.Resolve<IBreakTimerService> ();
			_syncService = Mvx.Resolve<ISyncService> ();
			_bsmBoxWifiService = Mvx.Resolve<IBSMBoxWifiService> ();
			_hosAlertService = Mvx.Resolve<IHosAlertService> ();
			_coworkerService = Mvx.Resolve<ICoWorkerService> ();

			_languageService = Mvx.Resolve<ILanguageService> ();
			_languageService.LoadLanguage (Mvx.Resolve<IDeviceLocaleService> ().GetLocale());

			
			_messagingReposioryService = Mvx.Resolve<IMessagingRepositoryService> ();
			//_messagingService = Mvx.Resolve<IMessagingService> ();
			//_MsgSubscribeToken = _messenger.SubscribeOnThreadPoolThread<MessagingMessage> ((message) => {
			//	if(!message.MessageTopic.Equals("TheEnd"))
			//		NewMessagesCount++;
			//});
			//_messagingService.Start ();

			_updatestatusformService = _messenger.SubscribeOnMainThread<DriverStatusFromBoxWifiServiceMessage>((message) =>
				{
					if(!_dataService.GetShouldScreenLock()){
						if(_dataService.GetIsScreenLocked()){
							_breakTimerservice.stop30MinBreakTimer();
							_messenger.Publish<UpdateDriverStatusMessage>(new UpdateDriverStatusMessage(this){driverStatusType = DriverStatusTypes.FirstOrDefault(p=>p.driverStatusType == (int)LOGSTATUS.Driving)});
						}else{
							_bsmBoxWifiService.startBoxDataTimer();
							_messenger.Publish<UpdateDriverStatusMessage>(new UpdateDriverStatusMessage(this){driverStatusType = DriverStatusTypes.FirstOrDefault(p=>p.driverStatusType == _dataService.GetCurrentLogStatus())});
						}
					}
				});

			_boxConnectivityMessage = _messenger.SubscribeOnThreadPoolThread<BoxConnectivityMessage>((message) =>
				{
					if (_dataService.GetBSMBoxStatus() != (int)message.BoxStatus) {
					MvxSingleton<IMvxMainThreadDispatcher>.Instance.RequestMainThreadAction(()=>{						
						_dataService.SetBSMBoxStatus(message.BoxStatus);
						BsmBoxWifiStatus = _dataService.GetBSMBoxStatus();
					});
				}
				});
			_boxDataMessage = _messenger.SubscribeOnMainThread<BoxDataMessage>((message) =>
				{

					if (_dataService.GetShouldScreenLock()) {
						if (message.LogStatus == LOGSTATUS.Driving) {
							if (!_dataService.GetIsScreenLocked()) {						
								_dataService.SetIsScreenLocked(true);
								MvxSingleton<IMvxMainThreadDispatcher>.Instance.RequestMainThreadAction(()=>{
									_breakTimerservice.stop30MinBreakTimer();
									this.ShowViewModel<DrivingmodeViewModel>();
									UnSubScribeFromBaseViewModel();
									this.Close(this);
								}).DisposeIfDisposable();
							}
						}
					}
					else {
						if (message.LogStatus == LOGSTATUS.Driving) {
							if (!_dataService.GetIsScreenLocked()) {								
								_dataService.SetIsScreenLocked(true);
							}
						}
						else if (message.LogStatus == LOGSTATUS.OffDuty) {
							if (_dataService.GetIsScreenLocked()) {
								if (_dataService.GetCurrentLogStatus() == (int) LOGSTATUS.OffDuty)
									return;
								if (_dataService.GetCurrentLogStatus() == (int)LOGSTATUS.Driving) {
									var tlmGoingOffDutyQ = new TimeLogModel();
									_bsmBoxWifiService.LocalizeTimeLog(ref tlmGoingOffDutyQ);
									tlmGoingOffDutyQ.Event = (int)LOGSTATUS.OnDuty;
									tlmGoingOffDutyQ.LogStatus = (int)LOGSTATUS.OnDuty;
									tlmGoingOffDutyQ.Logbookstopid = (int)AuditLogic.OnDuty;
									_timeLogService.SaveTimeLog(tlmGoingOffDutyQ);
									_hourCalculatorService.runHourCalculatorTimer();
									_dataService.PersistCurrentLogStatus((int)LOGSTATUS.OnDuty);
									_messenger.Publish<UpdateDriverStatusMessage>(new UpdateDriverStatusMessage(this){driverStatusType = DriverStatusTypes.FirstOrDefault(p=>p.driverStatusType == (int)LOGSTATUS.OnDuty)});
								}
								_bsmBoxWifiService.stopBoxDataTimer();
								OnShowIginationOffAlert(new EventArgs());
							}
						}else if(message.LogStatus != LOGSTATUS.Driving && _dataService.GetIsScreenLocked()){							
							_dataService.SetIsScreenLocked (false);
							_bsmBoxWifiService.stopBoxDataTimer();
						}
					}
				});
			_networkStatusChanged = _messenger.Subscribe<NetworkStatusChangedMessage> ((message) => {
				if(!IsShowingConnectionLostAlert && _dataService.GetIsScreenLocked() && !_dataService.GetShouldScreenLock()){
					if( (message.Status.IsMobile || !message.Status.IsConnected)){
						IsShowingConnectionLostAlert = true;
						OnShowConnLostAlert(new EventArgs());
					}else if(!IsShowingConnectionLostAlert  && message.Status.IsConnected){
						IsShowingConnectionLostAlert = true;
						OnShowConnLostAlert(new EventArgs());
					}
				}else{
					if (message.Status.IsConnected) {
						IsOffline = false;
					}
					else {
						IsOffline = true;
					}
				}
			});
			UpdateBsmStatus ();
			GetCycleList ();
			UpdateCycleAndExcemption ();			
			BsmBoxWifiStatus = _dataService.GetBSMBoxStatus();
		}
		#endregion

		#region Properties

		private bool isShowingConnectionLostAlert;
		public bool IsShowingConnectionLostAlert{
			get { return isShowingConnectionLostAlert; }
			set { 
				isShowingConnectionLostAlert = value; 
				RaisePropertyChanged(() => IsShowingConnectionLostAlert); 
			}
		}

		private bool breakCancel;
		public bool BreakCancel{
			get{return breakCancel; }
			set{ breakCancel = value;RaisePropertyChanged (()=>BreakCancel);}
		}

		private bool breakCompleteAlert;
		public bool BreakCompleteAlert{
			get{return breakCompleteAlert; }
			set{ breakCompleteAlert = value;RaisePropertyChanged (()=>BreakCompleteAlert);}
		}

		private bool _isOffline = false;
		public bool IsOffline
		{
			get { return _isOffline; }
			set { _isOffline = value; RaisePropertyChanged(() => IsOffline); }
		}

		private ObservableCollection<DriverStatusTypeClass> _driverStatusTypes = new ObservableCollection<DriverStatusTypeClass>();
		public ObservableCollection<DriverStatusTypeClass> DriverStatusTypes
		{
			get { return _driverStatusTypes; }
			set { _driverStatusTypes = value; RaisePropertyChanged(() => DriverStatusTypes); }
		}
		private int _assetId = -1;
		public int AssetId
		{ 
			get { return _assetId; }
			set {
				_assetId = value; 
				RaisePropertyChanged(() => AssetId); 
			}
		}

		private string _assetDescription = string.Empty;
		public string AssetDescription
		{ 
			get { return _assetDescription; }
			set { _assetDescription = value; RaisePropertyChanged(() => AssetDescription); }
		}

		private string _attachmentId = string.Empty;
		public string AttachmentId
		{
			get{return _attachmentId; }
			set{_attachmentId = value;RaisePropertyChanged (()=>AttachmentId); }
		}


		private string _attachmentDescription = string.Empty;
		public string AttachmentDescription
		{
			get{return _attachmentDescription; }
			set{_attachmentDescription = value;RaisePropertyChanged (()=>AttachmentDescription); }
		}

		private ObservableCollection<InspectionReportDefectModel> _defects = new ObservableCollection<InspectionReportDefectModel>();
		public ObservableCollection<InspectionReportDefectModel> Defects
		{
			get{return _defects; }
			set{_defects = value;RaisePropertyChanged (()=>Defects);}
		}


		private int _defectLevel;
		public int DefectLevel
		{
			get{return _defectLevel; }
			set{_defectLevel = value;RaisePropertyChanged (()=>DefectLevel); }
		
		
		}

		private int _hasInspectionDoneIn24Hrs;
		public int HasInspectionDoneIn24Hrs
		{
		
			get{return _hasInspectionDoneIn24Hrs; }
			set{
				_hasInspectionDoneIn24Hrs = value;RaisePropertyChanged (()=>HasInspectionDoneIn24Hrs);
			
			
			}
		}

		private int _odometer = 0;
		public int Odometer
		{ 
			get { return _odometer; }
			set {
				_odometer = value; 
				RaisePropertyChanged(() => Odometer); 
			}
		}

		private List<HosCycleModel> cycleList = new List<HosCycleModel>();
		public List<HosCycleModel> CycleList
		{
			get{return cycleList; }
			set{cycleList = value;RaisePropertyChanged (()=>CycleList); }
		}

		private bool hideCycle;
		public bool HideCycle
		{
			get{return hideCycle; }
			set{hideCycle = value;RaisePropertyChanged (()=>HideCycle); }
		}

		private string cycleDescription;
		public string CycleDescription{
			get{return cycleDescription; }
			set{cycleDescription = value;RaisePropertyChanged (()=>CycleDescription); }
		}

		private string excemptionDescription;
		public string ExcemptionDescription{
			get{return excemptionDescription; }
			set{excemptionDescription = value;RaisePropertyChanged (()=>ExcemptionDescription); }
		}

		public ObservableCollection<string> SelectedExcemptions{ get; set;}

		private bool rightEnable;
		public bool RightEnable{
			get{return rightEnable; }
			set{rightEnable = value; RaisePropertyChanged (()=>RightEnable); }
		}

		private bool leftEnable = false;
		public bool LeftEnable{
			get{return leftEnable; }
			set{leftEnable = value; RaisePropertyChanged (()=>LeftEnable); }
		}

		private bool enableExcemption;
		public bool EnableExcemption
		{
			get{return enableExcemption; }
			set{enableExcemption = value;RaisePropertyChanged (()=>EnableExcemption); }
		}

		public string DriverName {
			get;
			set;
		}

		private int _bsmBoxWifiStatus;
		public int BsmBoxWifiStatus
		{
			get{
				return _bsmBoxWifiStatus; 
			}
			set{
				_bsmBoxWifiStatus = value;
				RaisePropertyChanged (()=> BsmBoxWifiStatus); 
			}
		}

		private int _newMessagesCount;
		public int NewMessagesCount
		{
			get{
				return _newMessagesCount; 
			}
			set{
				_newMessagesCount = value;
				RaisePropertyChanged (()=> NewMessagesCount); 
			}
		}

		private bool logoutClick;
		public bool LogoutClick
		{
			get{return logoutClick; }
			set{logoutClick = value;RaisePropertyChanged (()=>LogoutClick); }
		}

		// for timedriver logic
		private TimeSpan _totalOnDutyforstatus = new TimeSpan(0);
		public TimeSpan TotalOnDutyforStatus
		{
			get { return _totalOnDutyforstatus; }
			set { 
				_totalOnDutyforstatus = value; 
				RaisePropertyChanged(() => TotalOnDutyforStatus); 
			}
		}

		private TimeSpan _totalDrivingforstatus = new TimeSpan(0);
		public TimeSpan TotalDrivingforStatus
		{
			get { return _totalDrivingforstatus; }
			set { 
				_totalDrivingforstatus = value; 
				RaisePropertyChanged(() => TotalDrivingforStatus); 
			}
		}

		private TimeSpan _totalSleepingforstatus = new TimeSpan(0);
		public TimeSpan TotalSleepingforStatus
		{
			get { return _totalSleepingforstatus; }
			set { 
				_totalSleepingforstatus = value; 
				RaisePropertyChanged(() => TotalSleepingforStatus); 
			}
		}

		private TimeSpan _totalOffDutyforstatus = new TimeSpan(0);
		public TimeSpan TotalOffDutyforStatus
		{
			get { return _totalOffDutyforstatus; }
			set { 
				_totalOffDutyforstatus = value; 
				RaisePropertyChanged(() => TotalOffDutyforStatus); 
			}
		}
		#endregion

		public event EventHandler FeatureUnderDevelopment;
		protected virtual void OnFeatureUnderDevelopment(EventArgs e)
		{
			if (FeatureUnderDevelopment != null)
			{
				FeatureUnderDevelopment(this, e);
			}
		}

		public event EventHandler CloseLeftMenu;
		protected virtual void OnCloseLeftMenu(EventArgs e)
		{
			if (CloseLeftMenu != null)
			{
				CloseLeftMenu(this, e);
			}
		}

		public event EventHandler ShowCommentDialog;
		protected virtual void OnShowCommentDialog(EventArgs e)
		{
			if (ShowCommentDialog != null)
			{
				ShowCommentDialog(this, e);
			}
		}

		public event EventHandler ShowBreakAlert;
		protected virtual void OnShowBreakAlert(EventArgs e)
		{
			if (ShowBreakAlert != null)
			{
				ShowBreakAlert(this, e);
			}
		}

		public event EventHandler ShowCompleteBreakAlert;
		protected virtual void OnShowCompleteBreakAlert(EventArgs e)
		{
			if (ShowCompleteBreakAlert != null)
			{
				ShowCompleteBreakAlert(this, e);
			}
		}

		public event EventHandler ShowCoWorkerAlert;
		protected virtual void OnShowCoWorkerAlert(EventArgs e)
		{
			if (ShowCoWorkerAlert != null)
			{
				ShowCoWorkerAlert(this, e);
			}
		}

		public event EventHandler ShowIginationOffAlert;
		protected virtual void OnShowIginationOffAlert(EventArgs e)
		{
			if (ShowIginationOffAlert != null)
			{
				ShowIginationOffAlert(this, e);
			}
		}

		public event EventHandler ShowConnLostAlert;
		protected virtual void OnShowConnLostAlert(EventArgs e)
		{
			if (ShowConnLostAlert != null)
			{
				ShowConnLostAlert(this, e);
			}
		}
		#region Commands
		public IMvxCommand ShowNextExcemption
		{
			get{return new MvxCommand (()=> {
				var nextindex = SelectedExcemptions.IndexOf(excemptionDescription) + 1;
				if(nextindex < SelectedExcemptions.Count - 1){
					ExcemptionDescription = SelectedExcemptions[nextindex];
					RightEnable = true;
					LeftEnable = true;
				}else if(nextindex == SelectedExcemptions.Count - 1){
					ExcemptionDescription = SelectedExcemptions[nextindex];
					RightEnable = false;
					LeftEnable = true;
				}
			}); }
		}

		public IMvxCommand ShowPreviousExcemption
		{
			get
			{
				return new MvxCommand (()=> {
					var previousindex = SelectedExcemptions.IndexOf(excemptionDescription) - 1;
					if(previousindex >= 0){
						if(previousindex == 0) {					
							ExcemptionDescription = SelectedExcemptions[previousindex];
							RightEnable = true;
							LeftEnable = false;
						}else if(previousindex < SelectedExcemptions.Count - 1){
							ExcemptionDescription = SelectedExcemptions[previousindex];
							RightEnable = true;
							LeftEnable = true;
						}
					}
				});
			}
		}

		public IMvxCommand ShowDriverMode
		{
			get{return new MvxCommand (()=> {
				ShowViewModel<DriverModeViewModel>();
				UnSubScribeFromBaseViewModel();
			}); }
		}

		public IMvxCommand ShowExcemption
		{
			get{return new MvxCommand (()=> ShowViewModel<HOSCycleWithExcemptionViewModel>()); }
		}

		public IMvxCommand ShowCycle
		{
			get{return new MvxCommand (()=> HideCycle = !HideCycle); }
		}

		public IMvxCommand DashBoard
		{
			get{return new MvxCommand (()=>NavigateHome()); }	
		}

		public IMvxCommand HOS
		{
			get{return new MvxCommand (()=>NavigateHos()); }	
		}

		public IMvxCommand Settings
		{
			get{return new MvxCommand (()=>NavigateSettings()); }	
		}

		public IMvxCommand SendEmail
		{
			get{return new MvxCommand (()=>NavigateEmail()); }	
		}

		public IMvxCommand SwitchAsset
		{
			get{
				return new MvxCommand (()=>{
					NavigateAsset();
					Constants.LastActivity = string.Empty;
				});
			}	
		}

		public IMvxCommand Inspection
		{
			get{return new MvxCommand (()=>NavigateInspection()); }	
		}

		public IMvxCommand CoWorkerLogin
		{
			get{return new MvxCommand (()=>NavigateCoWorkerLogin()); }	
		}

		public IMvxCommand CoWorkerLogout
		{
			get{return new MvxCommand (()=>NavigateCoWorkerLogout()); }	
		}

		public IMvxCommand SwitchUser
		{
			get{return new MvxCommand (()=>NavigateSwitchUser()); }	
		}

		public IMvxCommand ShowMessages
		{
			get{
				return new MvxCommand (()=>{					
					ShowViewModel<MessagesViewModel>();
					NewMessagesCount = 0;
				}); 
			}	
		}

		public IMvxCommand Logout
		{
			get{
				return new MvxCommand (()=> {
				LogoutClick = true;
			}); 
			}	
		}


		public IMvxCommand GoOffDutyAndUpdateDriverStatus
		{
			get {
				return new MvxCommand(() => {
					var offdutyTimelog = new TimeLogModel();
					LocalizeTimeLog(ref offdutyTimelog);
					offdutyTimelog.LogStatus = (int) LOGSTATUS.OffDuty;
					offdutyTimelog.Event = (int)LOGSTATUS.OffDuty;
					offdutyTimelog.Logbookstopid = AuditLogic.OffDuty;
					_timeLogService.Insert(offdutyTimelog);
					_hourCalculatorService.runHourCalculatorTimer();
					_syncService.runTimerCallBackNow();
					_dataService.SetIsScreenLocked (false);
					_bsmBoxWifiService.startBoxDataTimer();
					_dataService.PersistCurrentLogStatus((int)LOGSTATUS.OffDuty);
					_messenger.Publish<UpdateDriverStatusMessage>(new UpdateDriverStatusMessage(this){driverStatusType = DriverStatusTypes.FirstOrDefault(p=>p.driverStatusType == (int)LOGSTATUS.OffDuty)});
					IsShowingConnectionLostAlert = false;
				});
			}
		}

		public IMvxCommand ChangeDriverStatus
		{
			get {
				return new MvxCommand(() => {
					_syncService.runTimerCallBackNow();
					_hourCalculatorService.runHourCalculatorTimer();
					_dataService.SetIsScreenLocked (false);
					_bsmBoxWifiService.startBoxDataTimer();
					IsShowingConnectionLostAlert = false;
				});
			}
		}



		public void NavigateSwitchUser(){
			if (_dataService.GetAssetBoxId () == -1) {
				OnShowCoWorkerAlert (new EventArgs ());
			} else {
				ShowViewModel<SwitchDriverViewModel> ();
				closeMenu ();
			}
		}

		public void NavigateCoWorkerLogout(){
			if (_dataService.GetAssetBoxId () == -1) {
				OnShowCoWorkerAlert (new EventArgs ());
			} else {				
				ShowViewModel<CoWorkerLogoutViewModel> ();
				closeMenu ();
			}
		}

		public void NavigateHome(){
			closeMenu ();
			UnSubScribeFromBaseViewModel ();
			ShowViewModel<DashboardViewModel> ();
			this.Close(this);
		}

		public void NavigateHos(){
			AssetId = _dataService.GetAssetBoxId ();
			AssetDescription = _dataService.GetAssetBoxDescription ();				
			closeMenu ();
			UnSubScribeFromBaseViewModel ();
			ShowViewModel<HOSMainViewModel> ();
			this.Close(this);
		}

		public void NavigateSettings(){			
			closeMenu ();
			UnSubScribeFromBaseViewModel ();
			ShowViewModel<SettingsViewModel> ();
			this.Close(this);
		}

		public void NavigateEmail(){			
			closeMenu ();
			ShowViewModel<SendEmailViewModel>();
		}

		public void NavigateAsset(){			
			closeMenu ();
			_dataService.PersistAssetBoxId (_dataService.GetAssetBoxId() == -1 ? 0 : _dataService.GetAssetBoxId());
			ShowViewModel<SelectAssetViewModel> ();
			UnSubScribeFromBaseViewModel ();
			Close (this);
		}

		public void NavigateInspection(){
			AssetId = _dataService.GetAssetBoxId ();
			AssetDescription = _dataService.GetAssetBoxDescription ();
			if (AssetId != -1) {				
				closeMenu ();
				UnSubScribeFromBaseViewModel ();
				ShowViewModel<InspectionListViewModel> ();
				this.Close(this);
			} else {
				NavigateAsset ();
			}
		}

		public void NavigateCoWorkerLogin(){
			if(_dataService.GetAssetBoxId() == -1){
				OnShowCoWorkerAlert (new EventArgs());
			}else{
				closeMenu ();
				ShowViewModel<CoWorkerLoginViewModel> ();
			}
		}
		public void closeMenu() {
			OnCloseLeftMenu (new EventArgs ());
		}
		#endregion

		public EmployeeModel EmployeeDetail () {
			EmployeeModel currentEmployee = _employeeService.EmployeeDetailsById (_dataService.GetCurrentDriverId ());
			return currentEmployee;
		}

		public void SaveEmergencyComment(string comment){
			if (comment == null) {
				_messenger.Publish<UpdateDriverStatusMessage> (new UpdateDriverStatusMessage(this){driverStatusType = DriverStatusTypes.FirstOrDefault(p=>p.driverStatusType == _dataService.GetCurrentLogStatus())});
			} else {
				var tlr = new TimeLogModel ();
				LocalizeTimeLog (ref tlr);
				tlr.Event = _dataService.GetCurrentLogStatus() ==(int) LOGSTATUS.Driving ? (int)LOGSTATUS.Driving :(int) LOGSTATUS.OnDuty;
				tlr.LogStatus = _dataService.GetCurrentLogStatus() ==(int) LOGSTATUS.Driving ? (int)LOGSTATUS.Driving :(int) LOGSTATUS.OnDuty;
				tlr.Logbookstopid = AuditLogic.EmergencyUseStart;
				tlr.Comment = comment;
				_timeLogService.Insert (tlr);
				_dataService.PersistCurrentLogStatus((int)LOGSTATUS.Emergency);
			}
		}
		public void SaveBreakState(bool gobreak = false){			
			var tlr = new TimeLogModel ();
			LocalizeTimeLog(ref tlr);
			tlr.Event =(int) LOGSTATUS.OffDuty;
			tlr.LogStatus = (int)LOGSTATUS.OffDuty;
			tlr.Logbookstopid = AuditLogic.ThirtyMinutesOffDutyStart;
			_timeLogService.Insert (tlr);
			_hourCalculatorService.runHourCalculatorTimer();
			_dataService.PersistCurrentLogStatus((int)LOGSTATUS.Break30Min);

			var date1 = Utils.GetDateTimeNow ().ToUniversalTime ();
			//Also if in Canadian cycle initiate the personalUsage object for ECM status service to track millage for viloation
			if (EmployeeDetail().Cycle.ToString ().ToLower ().StartsWith ("ca")) {
				var personalusagelog = new BSM.Core.Services.BSMBoxWifiService.PersonalUseStatusLog ();
				personalusagelog.StartDateTime =date1;
				personalusagelog.PrevOdo = _dataService.GetOdometer ();
				personalusagelog.BoxId = _dataService.GetAssetBoxId ();
				personalusagelog.Distance = _timeLogService.GetPersonalUsageForDateDriver (date1.Date, _dataService.GetCurrentDriverId ());
				_bsmBoxWifiService.setPersonalUseStatusLog (personalusagelog);
				/*Global.personalUsageLog = new PersonalUseStatusLog ();
				Global.personalUsageLog.StartDateTime = NowDateTime;
				Global.personalUsageLog.PrevOdo = GlobalInstance.Odometer;
				Global.personalUsageLog.BoxId = GlobalInstance.BoxID;
				Global.personalUsageLog.Distance = TimeLogRepository.GetPersonalUsageForDateDriver (NowDateTime.Date, GlobalInstance.CurrentDriverId);
				_dataService.SetStartDateTime();
				_dataService.SetDistance (_timeLogService.GetPersonalUsageForDateDriver(Utils.GetDateTimeNow().Date,_dataService.GetCurrentDriverId()));*/
			} else {
				BSMBoxWifiService.personalUsageLog = null;
				//Global.personalUsageLog = null;
			}
			_breakTimerservice.stop30MinBreakTimer ();
			_breakTimerservice.start30MinBreakTimer ();
			var cowrokers = _coworkerService.CoWorkerList ().Where (p => p.LoggedIn).ToList ();
			if(cowrokers != null && cowrokers.Count > 0)
				ShowViewModel<CoworkerOnBreakViewModel> ();
		}

		void UpdateCycleAndExcemption()
		{
			var employee = EmployeeDetail();
			if(employee != null){
				DriverName = employee.DriverName;
				SelectedExcemptions = new ObservableCollection<string> ();
				CycleDescription =string.Join(" ",CycleList.FirstOrDefault (p=>p.Id == employee.Cycle).CycleDescription.Split('_'));
				if ((employee.HosExceptions & (int)RuleExceptions.USA_24_hour_cycle_reset) == (int)RuleExceptions.USA_24_hour_cycle_reset) {
					var execmption =string.Join(" ",Enum.GetName (typeof(RuleExceptions),(int)RuleExceptions.USA_24_hour_cycle_reset).Split('_').Skip(1));
					if(SelectedExcemptions != null && !SelectedExcemptions.Contains(execmption)){
						SelectedExcemptions.Add (execmption);
					}
				}					
				if ((employee.HosExceptions & (int)RuleExceptions.USA_Oilfield_waiting_time) == (int)RuleExceptions.USA_Oilfield_waiting_time) {
					var execmption =string.Join(" ",Enum.GetName (typeof(RuleExceptions),(int)RuleExceptions.USA_Oilfield_waiting_time).Split('_').Skip(1));
					if(SelectedExcemptions != null && !SelectedExcemptions.Contains(execmption)){
						SelectedExcemptions.Add (execmption);
					}
				}					
				if ((employee.HosExceptions & (int)RuleExceptions.USA_100_air_mile_radius) == (int)RuleExceptions.USA_100_air_mile_radius) {
					var execmption =string.Join(" ",Enum.GetName (typeof(RuleExceptions),(int)RuleExceptions.USA_100_air_mile_radius).Split('_').Skip(1));
					if(SelectedExcemptions != null && !SelectedExcemptions.Contains(execmption)){
						SelectedExcemptions.Add (execmption);
					}
				}					
				if ((employee.HosExceptions & (int)RuleExceptions.USA_150_air_mile_radius) == (int)RuleExceptions.USA_150_air_mile_radius) {
					var execmption =string.Join(" ",Enum.GetName (typeof(RuleExceptions),(int)RuleExceptions.USA_150_air_mile_radius).Split('_').Skip(1));
					if(SelectedExcemptions != null && !SelectedExcemptions.Contains(execmption)){
						SelectedExcemptions.Add (execmption);
					}
				}					
				if ((employee.HosExceptions & (int)RuleExceptions.USA_Transportation_construction_Materialsandequipment) == (int)RuleExceptions.USA_Transportation_construction_Materialsandequipment) {
					var execmption =string.Join(" ",Enum.GetName (typeof(RuleExceptions),(int)RuleExceptions.USA_Transportation_construction_Materialsandequipment).Split('_').Skip(1));
					if(SelectedExcemptions != null && !SelectedExcemptions.Contains(execmption)){
						SelectedExcemptions.Add (execmption);
					}
				}
				if(SelectedExcemptions.Count == 0){
					var execmption = Enum.GetName (typeof(RuleExceptions),employee.HosExceptions);
					if(SelectedExcemptions != null && !SelectedExcemptions.Contains(execmption)){
						SelectedExcemptions.Add (execmption);
					}
				}

				if (SelectedExcemptions != null && SelectedExcemptions.Count > 0) {
					EnableExcemption = true;
					ExcemptionDescription = SelectedExcemptions [0];
					if (SelectedExcemptions.Count == 1) {
						RightEnable = false;
						LeftEnable = false;
					} else {
						RightEnable = true;
						LeftEnable = false;
					}
				} else {
					EnableExcemption = false;
				}
			}
		}
		void GetCycleList(){
			var hosCycleNames = Enum.GetNames(typeof(HOSCYCLE));
			var hosCycleValues = Enum.GetValues(typeof(HOSCYCLE));
			for(int i=0; i<hosCycleNames.Length; i++) {
				var hoscycle = new HosCycleModel ();
				hoscycle.Id = (int)hosCycleValues.GetValue (i);
				hoscycle.CycleDescription = hosCycleNames [i];
				CycleList.Add (hoscycle);
			}
		}
		public void UpdateBsmStatus(){
			
			AssetId = _dataService.GetAssetBoxId ();
			AssetDescription = _dataService.GetAssetBoxDescription ();
			if (_dataService.GetAssetBoxId () == -1) {
				DefectLevel = 2;
				HasInspectionDoneIn24Hrs = 2;
			} else {
				DefectLevel = _reportService.HasMajorDefects24Hrs (AssetId);
				HasInspectionDoneIn24Hrs = _reportService.HasInspectionDoneIn24Hrs (AssetId);
			}
			DriverStatusTypes.Add(new DriverStatusTypeClass(101, "OFF DUTY - ", "00:00","00:00", "OFF"));
			DriverStatusTypes.Add(new DriverStatusTypeClass(102, "SLEEPING - ", "00:00","00:00", "SB"));
			DriverStatusTypes.Add(new DriverStatusTypeClass(103, "DRIVING - ", "00:00","00:00", "D"));
			DriverStatusTypes.Add(new DriverStatusTypeClass(104, "ON DUTY - ", "00:00","00:00", "ON"));
			DriverStatusTypes.Add(new DriverStatusTypeClass(105, "30 MIN BRK - ", "30:00","30:00", "OFF"));
			DriverStatusTypes.Add(new DriverStatusTypeClass(106, "EMERGENCY - ", "00:00","00:00", "OFF"));
			DriverStatusTypes.Add(new DriverStatusTypeClass(107, "PERSONAL - ", "00:00","00:00", "OFF"));

				if(!string.IsNullOrEmpty(_dataService.GetCurrentDriverId()) && _dataService.GetCurrentDriverId() != "-1"){
					UpdateLastTimeLog ();
				}
				var tcpModel = _settingServive.GetSettingsByName (Constants.SETTINGS_TCP_ENABLED);
				var settingRow = new SettingsModel (){SettingsName=Constants.SETTINGS_TCP_ENABLED};
				if (_network.IsConnected) {
					if (tcpModel == null) {
						settingRow.SettingsValue = "1";
						_settingServive.Insert (settingRow);
					} else {
						tcpModel.SettingsValue = "1";
						_settingServive.Update (tcpModel);
					}
				} else {
					if (tcpModel == null) {					
						settingRow.SettingsValue = "0";
						_settingServive.Insert (settingRow);
					} else {
						tcpModel.SettingsValue = "0";
						_settingServive.Update (tcpModel);
					}
				}
		}

		void UpdateLastTimeLog()
		{
			var lastTimelog = _timeLogService.GetLast (_dataService.GetCurrentDriverId ());
			if (lastTimelog == null) {
				var tlr = new TimeLogModel ();
				LocalizeTimeLog (ref tlr);
				tlr.Event = (int)LOGSTATUS.OffDuty;
				tlr.LogStatus = (int)LOGSTATUS.OffDuty;
				tlr.Logbookstopid = AuditLogic.OffDuty;
				_timeLogService.Insert (tlr);
				_dataService.PersistCurrentLogStatus ((int)LOGSTATUS.OffDuty);
			} else {
				_dataService.PersistCurrentLogStatus (lastTimelog.Event);
				if (lastTimelog.Event == (int)LOGSTATUS.OffDuty) {
					var date = Utils.GetDateTimeNow ().ToUniversalTime ();
					if (lastTimelog.Logbookstopid == AuditLogic.ThirtyMinutesOffDutyStart) {
						_dataService.PersistCurrentLogStatus ((int)LOGSTATUS.Break30Min);
						if (EmployeeDetail ().Cycle.ToString ().ToLower ().StartsWith ("ca")) {							
							BSMBoxWifiService.personalUsageLog = new BSMBoxWifiService.PersonalUseStatusLog ();
							BSMBoxWifiService.personalUsageLog.StartDateTime = date;
							BSMBoxWifiService.personalUsageLog.PrevOdo = _dataService.GetOdometer ();
							BSMBoxWifiService.personalUsageLog.BoxId = _dataService.GetAssetBoxId ();
							BSMBoxWifiService.personalUsageLog.Distance = _timeLogService.GetPersonalUsageForDateDriver (date.Date, _dataService.GetCurrentDriverId ());
						} else {
							BSMBoxWifiService.personalUsageLog = null;
						}
					} else if (lastTimelog.Logbookstopid == AuditLogic.PERSONALUSE) {
						_dataService.PersistCurrentLogStatus ((int)LOGSTATUS.PersonalUse);
						if (EmployeeDetail ().Cycle.ToString ().ToLower ().StartsWith ("ca") && BSMBoxWifiService.personalUsageLog == null) { 
							var curPersonalUsage = _timeLogService.GetPersonalUsageForDateDriver (date.Date, _dataService.GetCurrentDriverId ());
							if (curPersonalUsage > 75) {
								//Display no personal usage left for today
								//Toast.MakeText (this.ctx, this.ctx.Resources.GetString(Resource.String.str_no_personal_usage_left), ToastLength.Short).Show ();
								_dataService.PersistCurrentLogStatus ((int)LOGSTATUS.OffDuty);
								BSMBoxWifiService.personalUsageLog = null;
							} else {								
								BSMBoxWifiService.personalUsageLog = new BSMBoxWifiService.PersonalUseStatusLog ();
								BSMBoxWifiService.personalUsageLog.StartDateTime =date ;
								BSMBoxWifiService.personalUsageLog.PrevOdo = _dataService.GetOdometer ();
								BSMBoxWifiService.personalUsageLog.BoxId = _dataService.GetAssetBoxId ();
								BSMBoxWifiService.personalUsageLog.Distance = _timeLogService.GetPersonalUsageForDateDriver (date.Date, _dataService.GetCurrentDriverId ());
							}
						}
					}
				}
			}
			var primaryTimeLogData = _timeLogService.GetAllForDate (DateTime.Now, _dataService.GetCurrentDriverId ());
			var primaryTimeLogData1 = ValidateLogData (primaryTimeLogData, Utils.GetDateTimeNow ().ToUniversalTime());
			prepareTotalHoursForBsmStatus (primaryTimeLogData1);
		}

		public void BreakComplete(bool isbreakComplete){
			if (isbreakComplete) {				
				var tlr = new TimeLogModel ();
				LocalizeTimeLog(ref tlr);
				tlr.Event =(int) LOGSTATUS.OnDuty;
				tlr.LogStatus =(int) LOGSTATUS.OnDuty;
				tlr.Logbookstopid = AuditLogic.OnDuty;
				_timeLogService.Insert (tlr);
				_dataService.PersistCurrentLogStatus ((int)LOGSTATUS.OnDuty);
				_breakTimerservice.stop30MinBreakTimer ();
				_hourCalculatorService.runHourCalculatorTimerNow ();
				if(DriverStatusTypes != null && DriverStatusTypes.Count > 0){
					_messenger.Publish<UpdateDriverStatusMessage> (new UpdateDriverStatusMessage(this){driverStatusType = DriverStatusTypes.FirstOrDefault(p=>p.driverStatusType == (int)LOGSTATUS.OnDuty)});
				}
				var employeeList = _employeeService.EmployeeList ().Where(p=>p.Domain == _dataService.GetDomain()).ToList();
				var coworkerList = _coworkerService.CoWorkerList ();
				var joinList = (from emp in employeeList
					join worker in coworkerList on emp.Id equals worker.EmployeeID
					select emp).ToList<EmployeeModel>();
				if(joinList != null && joinList.Count > 0){
					foreach(var er in joinList){
						var	timelog = _timeLogService.GetLast (er.Id);
						if (timelog.Event ==(int) LOGSTATUS.OffDuty && timelog.Logbookstopid == AuditLogic.ThirtyMinutesOffDutyStart) {
							var tLog = new TimeLogModel ();
							LocalizeTimeLog(ref tLog);
							tLog.DriverId = er.Id;
							tLog.CoDriver = "";
							tLog.Event =(int) LOGSTATUS.OnDuty;
							tLog.LogStatus =(int) LOGSTATUS.OnDuty;
							tLog.Logbookstopid = AuditLogic.OnDuty;
							_timeLogService.Insert (tLog);
						}
					}
				}
			} else {
				var tlr = new TimeLogModel ();
				LocalizeTimeLog(ref tlr);
				tlr.Event =(int) LOGSTATUS.OffDuty;
				tlr.LogStatus =(int) LOGSTATUS.OffDuty;
				tlr.Logbookstopid = AuditLogic.OffDuty;
				_timeLogService.Insert (tlr);
				_breakTimerservice.stop30MinBreakTimer ();
				_dataService.PersistCurrentLogStatus ((int)LOGSTATUS.OffDuty);
				var employeeList = _employeeService.EmployeeList ().Where(p=>p.Domain == _dataService.GetDomain()).ToList();
				var coworkerList = _coworkerService.CoWorkerList ();
				var joinList = (from emp in employeeList
					join worker in coworkerList on emp.Id equals worker.EmployeeID
					select emp).ToList<EmployeeModel>();
				if(joinList != null && joinList.Count > 0){
					foreach(var er in joinList){
						var	timelog = _timeLogService.GetLast (er.Id);
						if (timelog.Event == (int)LOGSTATUS.OffDuty && timelog.Logbookstopid == AuditLogic.ThirtyMinutesOffDutyStart) {
							var tLog = new TimeLogModel ();
							LocalizeTimeLog(ref tLog);
							tLog.DriverId = er.Id;
							tLog.CoDriver = "";
							tLog.Event =(int) LOGSTATUS.OnDuty;
							tLog.LogStatus =(int) LOGSTATUS.OnDuty;
							tLog.Logbookstopid = AuditLogic.OnDuty;
							_timeLogService.Insert (tLog);
						}
					}
				}

				if(DriverStatusTypes != null && DriverStatusTypes.Count > 0){
					_messenger.Publish<UpdateDriverStatusMessage> (new UpdateDriverStatusMessage(this){driverStatusType = DriverStatusTypes.FirstOrDefault(p=>p.driverStatusType == (int)LOGSTATUS.OffDuty)});
				}
				_hourCalculatorService.runHourCalculatorTimerNow ();
			}
		}

		public void LocalizeTimeLog(ref TimeLogModel timeLogModel)
		{
			timeLogModel.Id = -1;
			timeLogModel.Latitude = _dataService.GetLatitude();
			timeLogModel.Longitude = _dataService.GetLongitude();
			timeLogModel.Odometer = _dataService.GetOdometer ();
			timeLogModel.BoxID = _dataService.GetAssetBoxId ();
			timeLogModel.DriverId = _dataService.GetCurrentDriverId ();
			timeLogModel.CoDriver = _dataService.GetCoDriver ();
			timeLogModel.EquipmentID = _dataService.GetAssetBoxDescription ();
			timeLogModel.LogTime = Utils.GetDateTimeNow ().ToUniversalTime();
			timeLogModel.OrigLogTime = Utils.GetDateTimeUtcNow ();

			EmployeeModel currentEmployee = _employeeService.EmployeeDetailsById (_dataService.GetCurrentDriverId ());
			if(currentEmployee != null && !string.IsNullOrEmpty(currentEmployee.TimeZone)){
				timeLogModel.TimeZone = currentEmployee.TimeZone;
				timeLogModel.DayLightSaving = currentEmployee.DayLightSaving;
			} else{
				timeLogModel.TimeZone = TimeZoneInfo.Local != null ? TimeZoneInfo.Local.BaseUtcOffset.TotalHours.ToString () : "-5.0";
				timeLogModel.DayLightSaving = DateTime.Now.IsDaylightSavingTime();
			}
			timeLogModel.AppVersion = Util.getAppVersion ();

			if (timeLogModel.Event < 200) {
				timeLogModel.QualifyRadiusRule = _hourCalculatorService.getHourCalculator() != null ? Convert.ToBoolean(_hourCalculatorService.getHourCalculator().QualifyRadiusRule) : false;
				//Let's keep track of last RadiusRule qualification value in Global so that if it changes we changed the lastTimeLog's RadiusRule qualification
				_dataService.SetLastTimeLogRadiusRule (timeLogModel.QualifyRadiusRule);
			}
		}

		public TimeLogModel LocalizeAndReturnTimeLog(TimeLogModel timeLogModel)
		{
			timeLogModel.Latitude = _dataService.GetLatitude();
			timeLogModel.Longitude = _dataService.GetLongitude();
			timeLogModel.Odometer = _dataService.GetOdometer ();
			timeLogModel.BoxID = _dataService.GetAssetBoxId ();
			timeLogModel.DriverId = _dataService.GetCurrentDriverId ();
			timeLogModel.CoDriver = _dataService.GetCoDriver ();
			timeLogModel.EquipmentID = _dataService.GetAssetBoxDescription ();
			timeLogModel.LogTime = Util.GetDateTimeNow ();
			timeLogModel.OrigLogTime = Utils.GetDateTimeUtcNow ();

			EmployeeModel currentEmployee = _employeeService.EmployeeDetailsById (_dataService.GetCurrentDriverId ());
			if(currentEmployee != null && !string.IsNullOrEmpty(currentEmployee.TimeZone)){
				timeLogModel.TimeZone = currentEmployee.TimeZone;
				timeLogModel.DayLightSaving = currentEmployee.DayLightSaving;
			} else{
				timeLogModel.TimeZone = TimeZoneInfo.Local != null ? TimeZoneInfo.Local.BaseUtcOffset.TotalHours.ToString () : "-5.0";
				timeLogModel.DayLightSaving = DateTime.Now.IsDaylightSavingTime();
			}
			timeLogModel.AppVersion = Util.getAppVersion ();

			if (timeLogModel.Event < 200) {
				timeLogModel.QualifyRadiusRule = _hourCalculatorService.getHourCalculator() != null ? Convert.ToBoolean(_hourCalculatorService.getHourCalculator().QualifyRadiusRule) : false;
				//Let's keep track of last RadiusRule qualification value in Global so that if it changes we changed the lastTimeLog's RadiusRule qualification
				_dataService.SetLastTimeLogRadiusRule (timeLogModel.QualifyRadiusRule);
			}
			return timeLogModel;
		}

		public bool IsinOffDuty(){
			var status = true;
			if(_dataService.GetCurrentLogStatus() == (int)LOGSTATUS.OffDuty){
				status = false;
			}
			return status;
		}

		public void DoLogout(bool changestatus = false,bool showLogin=false){
			if(changestatus){
				var tlm = new TimeLogModel ();
				LocalizeTimeLog (ref tlm);
				tlm.EquipmentID = AssetDescription;
				tlm.Event = (int)LOGSTATUS.OffDuty;
				tlm.LogStatus = (int)LOGSTATUS.OffDuty;
				tlm.Logbookstopid = (int)AuditLogic.OffDuty;
				_timeLogService.InsertOrUpdate (tlm);
				_syncService.runTimerCallBackNow ();
				if (_hourCalculatorService != null && _hourCalculatorService.getHourCalculator().ShiftStart != null &&
					_timeLogService.LogStatusExistsAfterDate ((DateTime)_hourCalculatorService.getHourCalculator().ShiftStart, _dataService.GetCurrentDriverId(), _dataService.GetAssetBoxId(), LOGSTATUS.Driving) &&
					_reportService.ReportExistsInPeriod (_dataService.GetAssetBoxId(), (DateTime)_hourCalculatorService.getHourCalculator().ShiftStart,Utils.GetDateTimeNow (), InspectionType.PostTrip)) {						
					if(_hourCalculatorService.getHourCalculator() != null && _hourCalculatorService.getHourCalculator().ShiftStart != null && 
						_timeLogService.LogStatusExistsAfterDate ((DateTime)_hourCalculatorService.getHourCalculator().ShiftStart, _dataService.GetCurrentDriverId(), _dataService.GetAssetBoxId(), LOGSTATUS.Driving) &&
						_reportService.ReportExistsInPeriod (_dataService.GetAssetBoxId(), (DateTime)_hourCalculatorService.getHourCalculator().ShiftStart, Utils.GetDateTimeNow(), InspectionType.PostTrip)) {
						int HourViolationThreshold = 0;
						var hoursThreshold = _settingServive.GetSettingsByName (Constants.SETTINGS_VIOLATION_THRESHOLD);
						if(hoursThreshold != null ){
							HourViolationThreshold = Util.str2int (hoursThreshold.SettingsValue);
						}
						//Save alert and if this was the first time this alert was generated in this driver's shift then show a notification
						if(_hosAlertService.SaveHosAlert (_hosAlertService.GetNewHosAlert (AlertTypes.PostTripInspectionAlert, _hourCalculatorService.getHourCalculator(), HourViolationThreshold))) {
//							var dispatcher = Mvx.Resolve<IMvxMainThreadDispatcher>();
//							dispatcher.RequestMainThreadAction (() => {
							//MvxMainThreadDispatcher.Instance.RequestMainThreadAction(() => {
							MvxSingleton<IMvxMainThreadDispatcher>.Instance.RequestMainThreadAction(() => {
								Notifications.Instance.Send(_languageService.GetLocalisedString(Constants.str_violation), AlertTypes.PostTripInspectionAlert.ToString());
							});
//							Global.ViewViolationAlert(AlertTypes.PostTripInspectionAlert);
						}
					}
				}
			}
			_bsmBoxWifiService.stopBoxConnectivityTimer ();
			_bsmBoxWifiService.stopBoxDataTimer ();
			_breakTimerservice.stop30MinBreakTimer ();
			_hourCalculatorService.removeHourCalculatedEvent ();
			_sessionService.DeleteAllSessions ();
			_dataService.ClearLoginCredentials();
			closeMenu ();
			UnSubScribeFromBaseViewModel ();
			Close(this);
			ShowViewModel<LoginViewModel>();
			//_messagingService.Stop ();
		}

		public void prepareTotalHoursForBsmStatus(List<TimeLogModel> data) {
			if (data == null || data.Count == 0) 
				return;
			for (int i = 1; i < data.Count; i++)
			{
				var ts = new TimeSpan (data [i].LogTime.Ticks - data [i - 1].LogTime.Ticks);
				switch (data[i - 1].LogStatus)
				{
				case (int)LOGSTATUS.OffDuty:
					TotalOffDutyforStatus = TotalOffDutyforStatus.Add (ts);
					break;
				case (int)LOGSTATUS.OnDuty:
					TotalOnDutyforStatus = TotalOnDutyforStatus.Add(ts);
					break;
				case (int)LOGSTATUS.Driving:
					TotalDrivingforStatus = TotalDrivingforStatus.Add(ts);
					break;
				case (int)LOGSTATUS.Sleeping:
					TotalSleepingforStatus = TotalSleepingforStatus.Add(ts);
					break;
				}
			}
			if (data.Count == 1) {
				switch (data[0].Event)
				{
				case (int)LOGSTATUS.OffDuty:
					TotalOffDutyforStatus = new TimeSpan (24, 0, 0);
					break;
				case (int)LOGSTATUS.OnDuty:
					TotalOnDutyforStatus = new TimeSpan (24, 0, 0);
					break;
				case (int)LOGSTATUS.Driving:
					TotalDrivingforStatus = new TimeSpan (24, 0, 0);
					break;
				case (int)LOGSTATUS.Sleeping:
					TotalSleepingforStatus = new TimeSpan (24, 0, 0);
					break;
				}
			}
			var TotalOffDutyforStatusString = timeSpanToStr(TotalOffDutyforStatus);
			var TotalOnDutyforStatusString = timeSpanToStr(TotalOnDutyforStatus);
			var TotalSleepingforStatusString = timeSpanToStr(TotalSleepingforStatus);
			var TotalDrivingforStatusString = timeSpanToStr(TotalDrivingforStatus);
			DriverStatusTypes.First (p => p.driverStatusType == (int)LOGSTATUS.OffDuty).driverStatusTimeText = TotalOffDutyforStatusString;
			DriverStatusTypes.First (p => p.driverStatusType == (int)LOGSTATUS.OnDuty).driverStatusTimeText = TotalOnDutyforStatusString;
			DriverStatusTypes.First (p => p.driverStatusType == (int)LOGSTATUS.Driving).driverStatusTimeText = TotalDrivingforStatusString;
			DriverStatusTypes.First (p => p.driverStatusType == (int)LOGSTATUS.Sleeping).driverStatusTimeText = TotalSleepingforStatusString;
			DriverStatusTypes.First (p => p.driverStatusType == (int)LOGSTATUS.OffDuty).driverStatusTimeTextDropDown = TotalOffDutyforStatusString;
			DriverStatusTypes.First (p => p.driverStatusType == (int)LOGSTATUS.OnDuty).driverStatusTimeTextDropDown = TotalOnDutyforStatusString;
			DriverStatusTypes.First (p => p.driverStatusType == (int)LOGSTATUS.Driving).driverStatusTimeTextDropDown = TotalDrivingforStatusString;
			DriverStatusTypes.First (p => p.driverStatusType == (int)LOGSTATUS.Sleeping).driverStatusTimeTextDropDown = TotalSleepingforStatusString;

		}
		string timeSpanToStr(TimeSpan ts)
		{
			var m = (int)ts.TotalMinutes;
			if (ts.TotalDays == 1 && m == 1440) {
				return "24:00";
			}
			if (ts.Seconds > 30) {
				m++;
				var ts1 = TimeSpan.FromMinutes (m);
				return ts1.ToString (@"hh\:mm");
			}
			return ts.ToString (@"hh\:mm");
		}

		public List<TimeLogModel> ValidateLogData(List<TimeLogModel> data,DateTime PlottingDate)
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
					tempLog.Event = (int)LOGSTATUS.OffDuty;
					tempLog.LogStatus = (int)LOGSTATUS.OffDuty;
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
			t.Event = tlr.Event;
			t.Type = (int)TimeLogType.Auto;
			var tmpNowDate = Util.GetDateTimeNow ();
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
					t.Event = lastBeforeMidNight.Event;
				} else {
					t.LogStatus = tlr.LogStatus;
					t.Event = tlr.Event;
				}

				logdata.Insert (0, t);
				//If we are adding an Auto event for midnight let's change ouroff set to 1 so that correct portion gets selected based on the actual timelog list that user sees
				//SelectedTimeLogIndexOffSet = 1;
			}
			return logdata.OrderBy(p=>p.LogTime).ToList();
		}

		public void UnSubScribeFromBaseViewModel()
		{
			_messenger.Unsubscribe<NetworkStatusChangedMessage> (_networkStatusChanged);
			_messenger.Unsubscribe<BoxConnectivityMessage> (_boxConnectivityMessage);
			_messenger.Unsubscribe<BoxDataMessage> (_boxDataMessage);
			_messenger.Unsubscribe<DriverStatusFromBoxWifiServiceMessage> (_updatestatusformService);
			_messenger.RequestPurgeAll ();
		}


		public void DeleteSessions(){
			_sessionService.DeleteAllSessions ();
		}

		public void UpdateSelectedStatus(string status){
			var tlmodel = new TimeLogModel ();
			LocalizeTimeLog (ref tlmodel);
			if (status == "OffDuty") {
				tlmodel.Event = (int)LOGSTATUS.OffDuty;
				tlmodel.LogStatus = (int)LOGSTATUS.OffDuty;
				tlmodel.HaveSent = false;
				tlmodel.Logbookstopid = AuditLogic.OffDuty;
				_timeLogService.Insert (tlmodel);
				_syncService.runTimerCallBackNow ();
				_hourCalculatorService.runHourCalculatorTimer ();
				_dataService.PersistCurrentLogStatus ((int)LOGSTATUS.OffDuty);
			} else if (status == "OnDuty") {
				tlmodel.Event = (int)LOGSTATUS.OnDuty;
				tlmodel.LogStatus = (int)LOGSTATUS.OnDuty;
				tlmodel.HaveSent = false;
				tlmodel.Logbookstopid = AuditLogic.OnDuty;
				_timeLogService.Insert (tlmodel);
				_syncService.runTimerCallBackNow ();
				_hourCalculatorService.runHourCalculatorTimer();
				_dataService.PersistCurrentLogStatus ((int)LOGSTATUS.OnDuty);
			} else {
				tlmodel.Event = (int)LOGSTATUS.Driving;
				tlmodel.LogStatus = (int)LOGSTATUS.Driving;
				tlmodel.HaveSent = false;
				tlmodel.Logbookstopid = AuditLogic.Driving;
				_timeLogService.Insert (tlmodel);
				_syncService.runTimerCallBackNow ();
				_hourCalculatorService.runHourCalculatorTimer();
				_bsmBoxWifiService.Check4AlertsOnDriving(tlmodel.LogTime);
				_dataService.PersistCurrentLogStatus ((int)LOGSTATUS.Driving);
			}
			_bsmBoxWifiService.startBoxDataTimer();
			_dataService.SetIsScreenLocked (false);
			_messenger.Publish<UpdateDriverStatusMessage> (new UpdateDriverStatusMessage(this){driverStatusType = DriverStatusTypes.FirstOrDefault(p=>p.driverStatusType == _dataService.GetCurrentLogStatus())});
			IsShowingConnectionLostAlert = false;
		}

		public void InsertOndutyTimelogWhenConnectionLost(){
			var tlmGoingOffDutyQ = new TimeLogModel();
			_bsmBoxWifiService.LocalizeTimeLog(ref tlmGoingOffDutyQ);
			tlmGoingOffDutyQ.Event = (int)LOGSTATUS.OnDuty;
			tlmGoingOffDutyQ.LogStatus = (int)LOGSTATUS.OnDuty;
			tlmGoingOffDutyQ.Logbookstopid = (int)AuditLogic.OnDuty;
			_timeLogService.SaveTimeLog(tlmGoingOffDutyQ);
			_hourCalculatorService.runHourCalculatorTimer();
			_dataService.PersistCurrentLogStatus((int)LOGSTATUS.OnDuty);
			_messenger.Publish<UpdateDriverStatusMessage>(new UpdateDriverStatusMessage(this){driverStatusType = DriverStatusTypes.FirstOrDefault(p=>p.driverStatusType == (int)LOGSTATUS.OnDuty)});
			_bsmBoxWifiService.stopBoxDataTimer();
		}
	}
}

