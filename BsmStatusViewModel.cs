using MvvmCross.Plugins.Messenger;
using BSM.Core.Messages;

using MvvmCross.Core;
using BSM.Core.AuditEngine;
using BSM.Core.Services;
using System;
using System.Linq;
using BSM.Core.ConnectionLibrary;
using MvvmCross.Platform;
using MvvmCross.Platform.Core;
using MvvmCross.Platform.Platform;

namespace BSM.Core.ViewModels
{
	public class BsmStatusViewModel : BaseViewModel
	{
		private readonly MvxSubscriptionToken _driverStatusMessage;
		private readonly IMvxMessenger _messenger;
		private readonly ITimeLogService _timelogService;
		private readonly IDataService _dataservice;
		private readonly IBreakTimerService _breakTimerservice;
		private readonly IEmployeeService _employeeService;
		private readonly ICoWorkerService _coworkerService;
		private readonly IHourCalculatorService _hourCalcService;
		private readonly ISyncService _syncService;
		private readonly IBSMBoxWifiService _bsmboxWifiService;
		private readonly MvxSubscriptionToken _breakTimerUpdateMessage;
		private readonly MvxSubscriptionToken _refreshVehicle;
		private readonly IInspectionReportService _reportService;
		private readonly MvxSubscriptionToken _cycleSuccess;
		private readonly MvxSubscriptionToken breakTimerCompleteMessage;
		private readonly MvxSubscriptionToken breakTimerCancelMessage;

		public BsmStatusViewModel() {
			_messenger = Mvx.Resolve<IMvxMessenger> ();
			_timelogService = Mvx.Resolve<ITimeLogService> ();
			_dataservice = Mvx.Resolve<IDataService> ();
			_breakTimerservice = Mvx.Resolve<IBreakTimerService> ();
			_syncService = Mvx.Resolve<ISyncService> ();
			_employeeService = Mvx.Resolve<IEmployeeService> ();
			_coworkerService = Mvx.Resolve<ICoWorkerService> ();
			_hourCalcService = Mvx.Resolve<IHourCalculatorService> ();
			_bsmboxWifiService = Mvx.Resolve<IBSMBoxWifiService> ();
			_reportService = Mvx.Resolve<IInspectionReportService> ();
			_driverStatusMessage = _messenger.SubscribeOnMainThread<UpdateDriverStatusMessage>((message) =>
				{
					MvxSingleton<IMvxMainThreadDispatcher>.Instance.RequestMainThreadAction(()=>{						
						SelectedDriverStatus = message.driverStatusType;
						_messenger.Publish<UpdateGraphEventsiOSMessage>(new UpdateGraphEventsiOSMessage(this));
					}).DisposeIfDisposable();
				});
			UpdateStatus ();
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

			breakTimerCompleteMessage = _messenger.Subscribe<BreakTimerCompleteMessage>((message) =>
				{
					BreakCompleteAlert = true;
				});

			breakTimerCancelMessage = _messenger.Subscribe<BreakTimerCancelMessage>((message) =>
				{
					BreakCancel = true;
				});
		}

		public BsmStatusViewModel(IMvxMessenger messenger,ITimeLogService timelogservice,IDataService dataservice,IBreakTimerService breaktimerservice,IEmployeeService employeeservice,ICoWorkerService coworkerservice,IHourCalculatorService hourcalcservice,ISyncService syncservice,IBSMBoxWifiService bsmboxwifiservice){
			_messenger = messenger;
			_timelogService = timelogservice;
			_dataservice = dataservice;
			_breakTimerservice = breaktimerservice;
			_syncService = syncservice;
			_employeeService = employeeservice;
			_coworkerService = coworkerservice;
			_hourCalcService = hourcalcservice;
			_bsmboxWifiService = bsmboxwifiservice;
		}
		void UpdateStatus(){
			var lastTimelog = _timelogService.GetLast (_dataservice.GetCurrentDriverId());
			if (_dataservice.GetCurrentLogStatus () == (int)LOGSTATUS.Break30Min) {
				if (!_breakTimerservice.isTimerRunning ()) {
					try {
						var totalBreakInsec = Convert.ToInt32 ((Utils.GetDateTimeNow () - lastTimelog.LogTime.ToUniversalTime ()).TotalSeconds);
						_breakTimerservice.SettotalBreakTimeInSec (totalBreakInsec);
					} catch (Exception exp) {
						Mvx.Trace (MvxTraceLevel.Error,exp.ToString ());
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
			SelectedDriverStatus = DriverStatusTypes.FirstOrDefault (p=>p.driverStatusType == lastEvent);
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

				switch(_selectedDriverStatus.driverStatusType)
				{
				case (int)LOGSTATUS.OffDuty:
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
				case (int)LOGSTATUS.Sleeping:
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
				case (int)LOGSTATUS.Driving:
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
					_bsmboxWifiService.Check4AlertsOnDriving(tlr.LogTime);
					break;
				case (int)LOGSTATUS.OnDuty:
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
				case (int)LOGSTATUS.Break30Min:
					if (prevStatus == (int)LOGSTATUS.Break30Min)
						break;
					isbreak = true;
					break;
				case (int)LOGSTATUS.Emergency:
					if (prevStatus == (int)LOGSTATUS.Emergency)
						break;
					OnShowCommentDialog (new EventArgs());
					break;
				case (int)LOGSTATUS.PersonalUse:
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
					_hourCalcService.runHourCalculatorTimer();
					_syncService.runTimerCallBackNow();
					_messenger.Publish<UpdateGraphMessage> (new UpdateGraphMessage(this));
				}
				if(isbreak){
					OnShowBreakAlert (new EventArgs());
				}
				RaisePropertyChanged(() => SelectedDriverStatus);
			}
		}
		public void UnSubScribe(){
			_messenger.Unsubscribe <UpdateDriverStatusMessage>(_driverStatusMessage);
			_messenger.Unsubscribe <BreakTimerUpdateMessage>(_breakTimerUpdateMessage);
			_messenger.Unsubscribe<RefeshVehicleStatusMessage> (_refreshVehicle);
			_messenger.Unsubscribe<UpdateCycleMessage> (_cycleSuccess);
			_messenger.Unsubscribe<BreakTimerCompleteMessage> (breakTimerCompleteMessage);
			_messenger.Unsubscribe<BreakTimerCancelMessage> (breakTimerCancelMessage);

		}
	}
}