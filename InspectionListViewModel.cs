using MvvmCross.Core.ViewModels;
using System.Windows.Input;
using MvvmCross.Plugins.Messenger;
using BSM.Core.Services;
using MvvmCross.Plugins.File;
using System;
using System.Collections.Generic;
using BSM.Core.Messages;
using BSM.Core.ConnectionLibrary;
using System.Threading.Tasks;
using MvvmCross.Core;
using BSM.Core.AuditEngine;
using System.Linq;

using MvvmCross.Platform;
using MvvmCross.Platform.Core;
using MvvmCross.Platform.Platform;

namespace BSM.Core.ViewModels

{
	public class InspectionListViewModel : BaseViewModel
	{
		#region Member Variables
		private IDataService _dataService;
		private readonly IMvxMessenger _messenger;
		private IInspectionReportService _inspReportService;
		private MvxSubscriptionToken _refreshInspectionReports;
		private readonly ISettingsService _settings;
		private readonly ITimeLogService _timelogService;
		private readonly MvxSubscriptionToken _breakTimerUpdateMessage;
		private readonly IEmployeeService _employeeService;
		private readonly ICoWorkerService _coworkerService;
		private readonly IBreakTimerService _breakTimerservice;
		private readonly IBSMBoxWifiService _bsmboxWifiService;
		private readonly ISyncService _syncService;
		private readonly IHourCalculatorService _hourCalcService;
		private readonly MvxSubscriptionToken _driverStatusMessage;
		private readonly MvxSubscriptionToken _breakTimerCompleteMessage;
		private readonly MvxSubscriptionToken _breakTimerCancelMessage;
		private readonly MvxSubscriptionToken _refreshVehicle;
		private readonly MvxSubscriptionToken _cycleSuccess;
		#endregion

		public void Init(int formType,string pagefrom,bool onrestore = false)
		{
			if(pagefrom != null && pagefrom == "DashBoard"){				
				SelectedInspectionType = InspectionTypes[formType];
				PageFrom = pagefrom;
			}else{
				SelectedInspectionType = InspectionTypes [0];
			}
			if(onrestore){
				_bsmboxWifiService.startBoxConnectivityTimer ();
				_bsmboxWifiService.startBoxDataTimer ();
			}
		}
		#region ctors
		public InspectionListViewModel(IDataService dataService, IInspectionReportService inspReportService,IMvxMessenger messenger,ISettingsService settings,ITimeLogService timelogservice,IEmployeeService employeeservice,ICoWorkerService coworkerService,IBreakTimerService breakTimerservice,
			IBSMBoxWifiService bsmboxWifiService,ISyncService syncService,IHourCalculatorService hourCalcService)
		{
			_hourCalcService = hourCalcService;
			_syncService = syncService;
			_bsmboxWifiService = bsmboxWifiService;
			_breakTimerservice = breakTimerservice;
			_coworkerService = coworkerService;
			_employeeService = employeeservice;
			_timelogService = timelogservice;
			_settings = settings;
			_messenger = messenger;
			_dataService = dataService;
			_inspReportService = inspReportService;

			InspectionTypes = new List<InspectionTypeClass>();
			InspectionTypes.Add(new InspectionTypeClass(-1, "All"));
			InspectionTypes.Add(new InspectionTypeClass(1, "Pre Trip"));
			InspectionTypes.Add(new InspectionTypeClass(2, "Post Trip"));
			InspectionTypes.Add(new InspectionTypeClass(3, "Fuel Form"));

			_refreshInspectionReports = _messenger.Subscribe<InspectionCategoryMessage> ((message)=>{
				fetchInspectionReports (SelectedInspectionType);
				_messenger.Publish<RefeshVehicleStatusMessage>(new RefeshVehicleStatusMessage (this));			
			});

			UpdateStatus ();
			_breakTimerUpdateMessage = _messenger.SubscribeOnMainThread<BreakTimerUpdateMessage>((message) =>
				{
					if(SelectedDriverStatus != null){
						MvxSingleton<IMvxMainThreadDispatcher>.Instance.RequestMainThreadAction(()=>{
							GC.Collect();
							TimeSpan ts = TimeSpan.FromSeconds (message.Remaining);
							SelectedDriverStatus.driverStatusTimeText = ts.ToString (@"hh\:mm\:ss");
							Mvx.Trace (MvxTraceLevel.Diagnostic,ts.ToString (@"hh\:mm\:ss"));
						});
					}
				});

			_driverStatusMessage = _messenger.SubscribeOnMainThread<UpdateDriverStatusMessage>((message) =>
				{
					MvxSingleton<IMvxMainThreadDispatcher>.Instance.RequestMainThreadAction(()=>{						
						SelectedDriverStatus = message.driverStatusType;
					}).DisposeIfDisposable();
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
					if (_dataService.GetAssetBoxId () == -1) {
						DefectLevel = 2;
						HasInspectionDoneIn24Hrs = 2;
					} else {
						DefectLevel = _inspReportService.HasMajorDefects24Hrs (_dataService.GetAssetBoxId ());
						HasInspectionDoneIn24Hrs = _inspReportService.HasInspectionDoneIn24Hrs (_dataService.GetAssetBoxId ());
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
		}
		#endregion

		#region Properties
		private List<InspectionTypeClass> _inspectionTypes;
		public List<InspectionTypeClass> InspectionTypes
		{
			get { return _inspectionTypes; }
			set { _inspectionTypes = value; RaisePropertyChanged(() => InspectionTypes); }
		}

		private InspectionTypeClass _selectedInspectionType;
		public InspectionTypeClass SelectedInspectionType
		{
			get { return _selectedInspectionType; }
			set {
				_selectedInspectionType = value;
				HideInspectionType = !HideInspectionType;
				fetchInspectionReports (SelectedInspectionType);
				RaisePropertyChanged(() => SelectedInspectionType);
			}
		}

		private List<InspectionReportRow> _inspectionReports;
		public List<InspectionReportRow> InspectionReports
		{
			get { return _inspectionReports; }
			set { _inspectionReports = value; RaisePropertyChanged(() => InspectionReports); }
		}

		private InspectionReportRow _selectedInspectionReport;
		public InspectionReportRow SelectedInspectionReport
		{
			get { return _selectedInspectionReport; }
			set { _selectedInspectionReport = value; RaisePropertyChanged(() => SelectedInspectionReport); }
		}

		private bool _changeStatus;
		public bool ChangeStatus
		{
			get{return _changeStatus; }
			set{_changeStatus = value;RaisePropertyChanged (()=> ChangeStatus); }
		}

		private bool _hideInspectionType;
		public bool HideInspectionType
		{
			get{return _hideInspectionType; }
			set{_hideInspectionType = value;RaisePropertyChanged (()=> HideInspectionType); }
		}

		private bool _isBusy;
		public bool IsBusy
		{
			get { return _isBusy; }
			set { _isBusy = value; RaisePropertyChanged(() => IsBusy); }
		}

		public string PageFrom {
			get;
			set;
		}
		#endregion

		#region Commands
		public ICommand ToggleInspectionTypeCommand {
			get {
				return new MvxCommand (() => {
					HideInspectionType = !HideInspectionType;
				});
			}
		}

		public ICommand AddInspectionCommand
		{
			get
			{
				return new MvxCommand(() =>
					{	
						AddForm();
					});
			}
		}

		public ICommand SelectInspectionCommand
		{
			get
			{
				return new MvxCommand<InspectionReportRow>((InspectionReport) =>
					{
						if(InspectionReport == null && InspectionReports != null && InspectionReports.Count > 0){
							InspectionReport = InspectionReports[0];
							SelectedInspectionReport = InspectionReport;
							OnRemoveBackStack(new EventArgs());
							ShowViewModel<InspectionCategoryViewModel>(new {ReportId=SelectedInspectionReport.Id,inspectiontype=SelectedInspectionReport.InspectionType,inspectionDescription = InspectionTypes[SelectedInspectionReport.InspectionType].inspectionTypeText,attachmentId = InspectionReport.attID});
						}else if(InspectionReports != null && InspectionReports.Count > 0){
							SelectedInspectionReport = InspectionReport;
							OnRemoveBackStack(new EventArgs());
							ShowViewModel<InspectionCategoryViewModel>(new {ReportId=SelectedInspectionReport.Id,inspectiontype=SelectedInspectionReport.InspectionType,inspectionDescription = InspectionTypes[SelectedInspectionReport.InspectionType].inspectionTypeText,attachmentId = InspectionReport.attID});
						}
					});
			}
		}

		private MvxCommand _goBackCommand;
		public ICommand GoBackCommand
		{
			get
			{
				_goBackCommand = _goBackCommand ?? new MvxCommand(GoBack);
				return _goBackCommand;
			}
		}

		private void GoBack()
		{
			Close(this);
		}

		public async void fetchInspectionReports(InspectionTypeClass _inspectionType) {
			IsBusy = true;
			if (_inspectionType.inspectionType != -1) {
				ChangeStatus = true;
			} else {
				ChangeStatus = false;
			}
			await Task.Run (() => {
				InspectionReports = _inspReportService.GetAllByTypeAndId (_inspectionType.inspectionType, _dataService.GetAssetBoxId ());
			});
			IsBusy = false;
		}
		#endregion

		#region Events
		public event EventHandler ChooseInspectionTypeError;
		protected virtual void OnChooseInspectionTypeError(EventArgs e)
		{
			if (ChooseInspectionTypeError != null)
			{
				ChooseInspectionTypeError(this, e);
			}
		}

		public event EventHandler RemoveBackStack;
		protected virtual void OnRemoveBackStack(EventArgs e)
		{
			if (RemoveBackStack != null)
			{
				RemoveBackStack(this, e);
			}
		}
		#endregion

		#region unsubscribe
		public void unSubscribe() {
			_messenger.Unsubscribe<InspectionCategoryMessage> (_refreshInspectionReports);
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

		public void AddForm(){
			if (SelectedInspectionType.inspectionType == -1) {
				OnChooseInspectionTypeError(new EventArgs());
			}
			else if (SelectedInspectionType.inspectionType == 3) {
				ShowViewModel<FuelFormViewModel>(new {desc = "Fuel Form"});
			}
			else {
					OnRemoveBackStack (new EventArgs());
				 ShowViewModel<InspectionCategoryViewModel>(new {ReportID = 0,inspectiontype=SelectedInspectionType.inspectionType,inspectionDescription = SelectedInspectionType.inspectionTypeText,attachmentId = ""});	
			}
		}

		private DriverStatusTypeClass _selectedDriverStatus = new DriverStatusTypeClass(-1, "", "","", "");
		public DriverStatusTypeClass SelectedDriverStatus
		{
			get { return _selectedDriverStatus; }
			set {
				if (value == null || (value != null && _dataService.GetCurrentLogStatus () == value.driverStatusType)) {
					_selectedDriverStatus = value;
					RaisePropertyChanged (() => SelectedDriverStatus);
					return;
				}
				_selectedDriverStatus = value;
				var isbreak = false;
				var prevStatus = _dataService.GetCurrentLogStatus ();
				var tlr = new TimeLogModel ();
				//If we were in Emergency mode and now a new status is selected; let's end the emergency use
				if( prevStatus ==(int) LOGSTATUS.Emergency && DriverStatusTypes.IndexOf(_selectedDriverStatus) != 5){ // 5 is Emergency	
					LocalizeTimeLog(ref tlr);
					tlr.Event = _timelogService.GetLastByLogbookstopid(_dataService.GetCurrentDriverId(), AuditLogic.EmergencyUseStart).Event;
					tlr.LogStatus = _timelogService.GetLastByLogbookstopid(_dataService.GetCurrentDriverId(), AuditLogic.EmergencyUseStart).Event; //We end emergency mode with status that we started with
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
					var employeeList = _employeeService.EmployeeList ().Where(p=>p.Domain == _dataService.GetDomain()).ToList();
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
					_dataService.PersistCurrentLogStatus ((int)LOGSTATUS.OffDuty);
					//SelectedDriverStatus.driverStatusTimeText = TotalOffDutyforStatus.ToString (@"hh\:mm");
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
					_dataService.PersistCurrentLogStatus ((int)LOGSTATUS.Sleeping);
					//SelectedDriverStatus.driverStatusTimeText = TotalSleepingforStatus.ToString (@"hh\:mm");
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
					_dataService.PersistCurrentLogStatus ((int)LOGSTATUS.Driving);
					//SelectedDriverStatus.driverStatusTimeText = TotalDrivingforStatus.ToString (@"hh\:mm");
					//Check for Alerts on Driving status we have to check
					_bsmboxWifiService.Check4AlertsOnDriving(tlr.LogTime);
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
					_dataService.PersistCurrentLogStatus ((int)LOGSTATUS.OnDuty);
					//SelectedDriverStatus.driverStatusTimeText = TotalOnDutyforStatus.ToString (@"hh\:mm");
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
						var	curPersonalUsage = _timelogService.GetPersonalUsageForDateDriver (Util.GetDateTimeNow().Date, _dataService.GetCurrentDriverId());
						if (curPersonalUsage > 75) {
							/*Display no personal usage left for today
							Toast.MakeText (this.ctx, this.ctx.Resources.GetString(Resource.String.str_no_personal_usage_left), ToastLength.Short).Show ();*/
							BSMBoxWifiService.personalUsageLog = null;
						} else {
							BSMBoxWifiService.personalUsageLog = new BSMBoxWifiService.PersonalUseStatusLog ();
							BSMBoxWifiService.personalUsageLog.StartDateTime = Utils.GetDateTimeNow ();
							BSMBoxWifiService.personalUsageLog.PrevOdo = _dataService.GetOdometer ();
							BSMBoxWifiService.personalUsageLog.BoxId = _dataService.GetAssetBoxId ();
							BSMBoxWifiService.personalUsageLog.Distance = _timelogService.GetPersonalUsageForDateDriver (Utils.GetDateTimeNow ().Date, _dataService.GetCurrentDriverId ());
							tlr = new TimeLogModel ();
							LocalizeTimeLog(ref tlr);
							tlr.Event =(int) LOGSTATUS.OffDuty;
							tlr.LogStatus =(int) LOGSTATUS.OffDuty;
							tlr.Logbookstopid = AuditLogic.PERSONALUSE;
							_timelogService.Insert (tlr);
							_dataService.PersistCurrentLogStatus((int)LOGSTATUS.PersonalUse);
						}
					} else {
						_dataService.ClearStartDateTime();
						_dataService.ClearDistance();
						tlr = new TimeLogModel ();
						LocalizeTimeLog(ref tlr);
						tlr.Event =(int) LOGSTATUS.OffDuty;
						tlr.LogStatus =(int) LOGSTATUS.OffDuty;
						tlr.Logbookstopid = AuditLogic.PERSONALUSE;
						_timelogService.Insert (tlr);
						_dataService.PersistCurrentLogStatus((int)LOGSTATUS.PersonalUse);
					}
					break;
				}
				if(prevStatus != -1 && prevStatus != _dataService.GetCurrentLogStatus()){
					_hourCalcService.runHourCalculatorTimer();
					_syncService.runTimerCallBackNow();
					if(prevStatus == (int)LOGSTATUS.Driving && _dataService.GetIsScreenLocked()){
						_dataService.SetIsScreenLocked (false);
						_bsmboxWifiService.setLastEcmStatus ();
					}
				}
				if(isbreak){
					OnShowBreakAlert (new EventArgs());
				}
				RaisePropertyChanged(() => SelectedDriverStatus);
			}
		}
		void UpdateStatus(){
			var lastTimelog = _timelogService.GetLast (_dataService.GetCurrentDriverId());
			if (_dataService.GetCurrentLogStatus () == (int)LOGSTATUS.Break30Min) {
				if (!_breakTimerservice.isTimerRunning ()) {
					try {
						var totalBreakInsec = Convert.ToInt32 ((Utils.GetDateTimeNow () - lastTimelog.LogTime.ToUniversalTime ()).TotalSeconds);
						_breakTimerservice.SettotalBreakTimeInSec (totalBreakInsec);
					} catch (Exception exp) {
						Mvx.Trace (MvxTraceLevel.Error,exp.ToString());
					}
					_breakTimerservice.start30MinBreakTimer ();
				}
			} else {
				_breakTimerservice.stop30MinBreakTimer ();
			}
			var curPersonalUsage1 = _timelogService.GetPersonalUsageForDateDriver (Utils.GetDateTimeNow().Date, _dataService.GetCurrentDriverId());
			if (curPersonalUsage1 > 0) {
				/*this.view.FindViewById<LinearLayout>(Resource.Id.hos_personal_distance).Visibility = ViewStates.Visible;
					this.view.FindViewById<View>(Resource.Id.hos_personal_distance_divider).Visibility = ViewStates.Visible;
					this.view.FindViewById<TextView> (Resource.Id.txtV_hos_personal_distance_lable).Visibility = ViewStates.Visible;
					txtPersonalUsage = this.view.FindViewById<TextView> (Resource.Id.txtV_hos_personal_distance);
					txtPersonalUsage.Visibility = ViewStates.Visible;
					txtPersonalUsage.Text = curPersonalUsage.ToString ();*/
			}
			var lastEvent = _dataService.GetCurrentLogStatus ();
			_selectedDriverStatus = DriverStatusTypes.FirstOrDefault (p=>p.driverStatusType == lastEvent);
		}
	}
}
