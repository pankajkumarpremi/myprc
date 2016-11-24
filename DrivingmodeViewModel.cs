using System;
using MvvmCross.Core.ViewModels;
using MvvmCross.Plugins.Messenger;
using BSM.Core.Services;
using BSM.Core.Messages;
using BSM.Core.AuditEngine;
using BSM.Core.ViewModels;
using System.Linq;
using Acr.MvvmCross.Plugins.Network;
using System.Threading.Tasks;
using MvvmCross.Core;
using System.Diagnostics;

using MvvmCross.Platform;
using MvvmCross.Platform.Core;
using MvvmCross.Platform.Platform;

namespace BSM.Core
{
	public class DrivingmodeViewModel : BaseViewModel
	{
		private readonly IDataService _dataService;
		private readonly IMvxMessenger _messenger;
		private readonly IBSMBoxWifiService _bsmBoxWifiService;
		private readonly IHourCalculatorService _hourCalculatorService;
		private readonly ITimeLogService _timeLogService;
		private readonly ISyncService _syncService;
		private readonly IBSMTimerService _drivingTimerService;
		private readonly MvxSubscriptionToken _boxDataMessage;
		private readonly MvxSubscriptionToken _networkStatusChanged;
		private readonly MvxSubscriptionToken _drivingTickToken;
		private readonly MvxSubscriptionToken _updatestatusformService;
		HourCalculator hc;
		public DrivingmodeViewModel (IMvxMessenger messenger, IDataService dataService, IBSMBoxWifiService bsmBoxWifiService, IHourCalculatorService hourCalculatorService, ITimeLogService timeLogService, ISyncService syncService, IBSMTimerService drivingTimerService)
		{
			_messenger = messenger;
			_dataService = dataService;
			_bsmBoxWifiService = bsmBoxWifiService;
			_hourCalculatorService = hourCalculatorService;
			_timeLogService = timeLogService;
			_syncService = syncService;
			_drivingTimerService = drivingTimerService;

			_drivingTickToken = messenger.SubscribeOnMainThread<DrivingTickMessage>(OnDrivingTick);

			_updatestatusformService = _messenger.SubscribeOnMainThread<DriverStatusFromBoxWifiServiceMessage>((message) =>
				{
					if(_dataService.GetShouldScreenLock() && !_dataService.GetIsScreenLocked()){
						MvxSingleton<IMvxMainThreadDispatcher>.Instance.RequestMainThreadAction(()=>{							
							this.ShowViewModel<DashboardViewModel>();
							_hourCalculatorService.runHourCalculatorTimer();
							_syncService.runTimerCallBackNow();
							_bsmBoxWifiService.startBoxDataTimer();
							Unsubscribe();
							UnSubScribeFromBaseViewModel();
							Close (this);
						});		
					}
				});

			_boxDataMessage = _messenger.SubscribeOnMainThread<BoxDataMessage>((message) =>
				{
					
					if (message.LogStatus == LOGSTATUS.Driving) {
						
					}else if (message.LogStatus == LOGSTATUS.OffDuty) {
						if (_dataService.GetIsScreenLocked()) {
//							System.Diagnostics.Debug.WriteLine("_dataService.GetCurrentLogStatus() ---- {}", _dataService.GetCurrentLogStatus().ToString());
							// if (_dataService.GetCurrentLogStatus() == (int) LOGSTATUS.OffDuty || (!_bsmBoxWifiService.getApplyWeightRule() && _dataService.GetCurrentLogStatus() == (int) LOGSTATUS.OnDuty))
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
							}
							_bsmBoxWifiService.stopBoxDataTimer();
							MvxSingleton<IMvxMainThreadDispatcher>.Instance.RequestMainThreadAction(()=>{
								OnCloseScreenLockAlert(new EventArgs());
							});
						}
					}else if(message.LogStatus != LOGSTATUS.Driving && _dataService.GetIsScreenLocked()){
						_dataService.SetIsScreenLocked (false);
						_bsmBoxWifiService.stopBoxDataTimer();

					}

				});

			_networkStatusChanged = _messenger.SubscribeOnMainThread<NetworkStatusChangedMessage>((message) =>
				{
					if(!IsShowingAlert && (message.Status.IsMobile || !message.Status.IsConnected)){
						IsShowingAlert = true;
						OnNetworkChange(new EventArgs());
						_bsmBoxWifiService.stopBoxDataTimer();
					}else if(!IsShowingAlert && message.Status.IsConnected) {
						IsShowingAlert = true;
						OnNetworkChange(new EventArgs());
						_bsmBoxWifiService.stopBoxDataTimer();
					}
				});
			CheckValuesFromHc = 0;
			Mvx.Trace (MvxTraceLevel.Diagnostic,"Before get hour calculator "+ DateTime.Now);
			hc = _hourCalculatorService.getHourCalculator ();
			Mvx.Trace (MvxTraceLevel.Diagnostic,"After get hour calculator "+ DateTime.Now);
			if (hc != null) {
				DriveTime = TimeSpan.FromSeconds (hc != null && hc.AvaliableDrivingMinutes > 0 ? hc.AvaliableDrivingMinutes : 0);
				DutyCycle = TimeSpan.FromSeconds (hc != null && hc.AvaliableCycle > 0 ? hc.AvaliableCycle : 0);
				Mvx.Trace (MvxTraceLevel.Diagnostic,"Got Drivetime and dutycycle"+ DateTime.Now);
				if (DriveTime >= TimeSpan.MinValue && DriveTime <= TimeSpan.MaxValue) {
					DriveTimeStr = new DateTime (DriveTime.Ticks).ToString ("HH:mm:ss");
					CheckValuesFromHc += 1;
				}
				if (DutyCycle >= TimeSpan.MinValue && DutyCycle <= TimeSpan.MaxValue) {
					DutyCycleStr = new DateTime (DutyCycle.Ticks).ToString ("HH:mm:ss");
					CheckValuesFromHc += 1;
				}
				if (CheckValuesFromHc == 2) {					
					// hcTimer.Dispose ();
					Mvx.Trace (MvxTraceLevel.Diagnostic,"startDrivingTimer triggered"+ DateTime.Now);
					_drivingTimerService.startDrivingTimer();
				}
			}
		}

		/*public async Task Init() {
			Debug.WriteLine ("Init started {0}", DateTime.Now);
			IsBusy = false;
			IsBusy = true;
			await Task.Delay (700);
			await Task.Run (() => {
				CheckValuesFromHc = 0;
				Debug.WriteLine ("Before get hour calculator {0}", DateTime.Now);
				hc = _hourCalculatorService.getHourCalculator ();
				Debug.WriteLine ("After get hour calculator {0}", DateTime.Now);
				if (hc != null) {
					DriveTime = TimeSpan.FromSeconds (hc != null && hc.AvaliableDrivingMinutes > 0 ? hc.AvaliableDrivingMinutes : 0);
					DutyCycle = TimeSpan.FromSeconds (hc != null && hc.AvaliableCycle > 0 ? hc.AvaliableCycle : 0);
					Debug.WriteLine ("Got Drivetime and dutycycle {0}", DateTime.Now);
					if (DriveTime >= TimeSpan.MinValue && DriveTime <= TimeSpan.MaxValue) {
						DriveTimeStr = new DateTime (DriveTime.Ticks).ToString ("HH:mm:ss");
						CheckValuesFromHc += 1;
					}
					if (DutyCycle >= TimeSpan.MinValue && DutyCycle <= TimeSpan.MaxValue) {
						DutyCycleStr = new DateTime (DutyCycle.Ticks).ToString ("HH:mm:ss");
						CheckValuesFromHc += 1;
					}
					if (CheckValuesFromHc == 2) {					
						// hcTimer.Dispose ();
						Debug.WriteLine ("startDrivingTimer triggered {0}", DateTime.Now);
						_drivingTimerService.startDrivingTimer();
					}
				}
			});
			await Task.Delay (100);
			IsBusy = false;
			Debug.WriteLine ("Init complete {0}", DateTime.Now);
		}*/

		#region properties
		public int MaxDring{ get; set;}
		public int MaxCyCle{ get; set;}

		private bool _isBusy;
		public bool IsBusy
		{
			get { return _isBusy; }
			set { _isBusy = value; RaisePropertyChanged(() => IsBusy); }
		}

		private bool isShowingAlert;
		public bool IsShowingAlert{
			get { return isShowingAlert; }
			set { 
				isShowingAlert = value; 
				RaisePropertyChanged(() => IsShowingAlert); 
			}
		}
		private TimeSpan _driveTime;
		public TimeSpan DriveTime
		{
			get { return _driveTime; }
			set { 
				_driveTime = value; 
				RaisePropertyChanged(() => DriveTime); 
			}
		}

		private TimeSpan _dutyCycle;
		public TimeSpan DutyCycle
		{
			get { return _dutyCycle; }
			set { 
				_dutyCycle = value; 
				RaisePropertyChanged(() => DutyCycle); 
			}
		}

		private string _driveTimeStr = "00:00:00";
		public string DriveTimeStr
		{
			get { return _driveTimeStr; }
			set { 
				_driveTimeStr = value; 
				RaisePropertyChanged(() => DriveTimeStr); 
			}
		}

		private string _dutyCycleStr = "00:00:00";
		public string DutyCycleStr
		{
			get { return _dutyCycleStr; }
			set { 
				_dutyCycleStr = value; 
				RaisePropertyChanged(() => DutyCycleStr); 
			}
		}

		private DateTime _startTime;
		public DateTime StartTime
		{
			get { return _startTime; }
			set { 
				_startTime = value; 
				RaisePropertyChanged(() => StartTime); 
			}
		}

		private DateTime _currentTime;
		public DateTime CurrentTime
		{
			get { return _currentTime; }
			set { 
				_currentTime = value; 
				RaisePropertyChanged(() => CurrentTime); 
			}
		}

		public int CheckValuesFromHc {
			get;
			set;
		}

		public bool IsTimerRunning{ get; set;}

		private bool isShowViolation;
		public bool IsShowViolation{
			get { return isShowViolation; }
			set { 
				isShowViolation = value; 
				RaisePropertyChanged(() => IsShowViolation); 
			}
		}

		#endregion

//		public void StartHCTimer() {
////			await Task.Delay (1000);
//			var timerDelegate = new TimerCallback(CheckHCStatus);
//			hcTimer = new Timer (timerDelegate, null, 0, 60000);
//		}

//		public void StartTimer() {
//			await Task.Delay (1000);
//			var timerDelegate = new TimerCallback(CheckStatus);
//			CycleTimer = new Timer(timerDelegate, null, 100, 1000);

//		}

//		public void CheckHCStatus(Object state) {
//			CheckValuesFromHc = 0;
//			hc = _hourCalculatorService.getHourCalculator ();
////			await Task.Delay (1000);
//			if (hc != null) {
//				DriveTime = TimeSpan.FromSeconds (hc != null && hc.AvaliableDrivingMinutes > 0 ? hc.AvaliableDrivingMinutes : 0);
//				DutyCycle = TimeSpan.FromSeconds (hc != null && hc.AvaliableCycle > 0? hc.AvaliableCycle : 0);
//				if (DriveTime >= TimeSpan.MinValue && DriveTime <= TimeSpan.MaxValue) {
//					DriveTimeStr = new DateTime (DriveTime.Ticks).ToString ("HH:mm:ss");
//					CheckValuesFromHc += 1;
//				}
//				if (DutyCycle >= TimeSpan.MinValue && DutyCycle <= TimeSpan.MaxValue) {
//					DutyCycleStr = new DateTime (DutyCycle.Ticks).ToString ("HH:mm:ss");
//					CheckValuesFromHc += 1;
//				}
//				if(CheckValuesFromHc == 2){					
//					hcTimer.Dispose();
//				}
//			}
//		}

		private void OnDrivingTick(DrivingTickMessage obj)
		{
			Mvx.Trace (MvxTraceLevel.Diagnostic,"OnDrivingTick"+ DateTime.Now);
			if (hc != null && CheckValuesFromHc == 2 && !IsTimerRunning) {
				if (DriveTime != TimeSpan.FromSeconds (0)) {
					DriveTime = new TimeSpan (DriveTime.Ticks) - new TimeSpan (0, 0, 1);
					DriveTimeStr = DriveTime.ToString (@"hh\:mm\:ss");
				} else {
					if (!IsShowViolation) {
						IsShowViolation = true;
						DriveTimeStr = DriveTime.ToString (@"hh\:mm\:ss");
					}
				}
				if (DutyCycle != TimeSpan.FromSeconds (0)) {
					DutyCycle = new TimeSpan (DutyCycle.Ticks) - new TimeSpan (0, 0, 1);
					DutyCycleStr = DutyCycle.ToString (@"hh\:mm\:ss");
				} else {
					DutyCycleStr = DutyCycle.ToString (@"hh\:mm\:ss");
				}
				IsTimerRunning = false;
			}
		}

		public void GoToSelectedStatus(string status){
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
			} else if (status == "OnDuty") {
				tlmodel.Event = (int)LOGSTATUS.OnDuty;
				tlmodel.LogStatus = (int)LOGSTATUS.OnDuty;
				tlmodel.HaveSent = false;
				tlmodel.Logbookstopid = AuditLogic.OnDuty;
				_timeLogService.Insert (tlmodel);
				_syncService.runTimerCallBackNow ();
				_hourCalculatorService.runHourCalculatorTimer();
			} else {
				tlmodel.Event = (int)LOGSTATUS.Driving;
				tlmodel.LogStatus = (int)LOGSTATUS.Driving;
				tlmodel.HaveSent = false;
				tlmodel.Logbookstopid = AuditLogic.Driving;
				_timeLogService.Insert (tlmodel);
				_syncService.runTimerCallBackNow ();
				_hourCalculatorService.runHourCalculatorTimer();
				_bsmBoxWifiService.Check4AlertsOnDriving(tlmodel.LogTime);
			}
			 Unsubscribe();
			 UnSubScribeFromBaseViewModel();
			_dataService.SetIsScreenLocked (false);
			_bsmBoxWifiService.startBoxDataTimer();
			 ShowViewModel<DashboardViewModel> ();
			 Close (this);
		}

		public void InsertOndutyTimelog(){
			var tlmGoingOffDutyQ = new TimeLogModel();
			LocalizeTimeLog(ref tlmGoingOffDutyQ);
			tlmGoingOffDutyQ.Event = (int)LOGSTATUS.OnDuty;
			tlmGoingOffDutyQ.LogStatus = (int)LOGSTATUS.OnDuty;
			tlmGoingOffDutyQ.Logbookstopid = (int)AuditLogic.OnDuty;
			_timeLogService.SaveTimeLog(tlmGoingOffDutyQ);
			_hourCalculatorService.runHourCalculatorTimer();
			_dataService.PersistCurrentLogStatus((int)LOGSTATUS.OnDuty);
		}

		public IMvxCommand GoOffDutyAndCloseScreenLockCommand
		{
			get {
				return new MvxCommand(() => {
					var offdutyTimelog = new TimeLogModel();
					LocalizeTimeLog(ref offdutyTimelog);
					offdutyTimelog.LogStatus = (int) LOGSTATUS.OffDuty;
					offdutyTimelog.Event = (int)LOGSTATUS.OffDuty;
					offdutyTimelog.Logbookstopid = AuditLogic.OffDuty;
					_timeLogService.Insert(offdutyTimelog);
					OnCloseScreenLock(new EventArgs());
					Unsubscribe();
					UnSubScribeFromBaseViewModel();
					_dataService.SetIsScreenLocked (false);
					_bsmBoxWifiService.startBoxDataTimer();
					this.ShowViewModel<DashboardViewModel>();
					this.Close (this);
				});
			}
		}

		public IMvxCommand CloseScreenLockCommand
		{
			get {
				return new MvxCommand(() => {
					_syncService.runTimerCallBackNow();
					_hourCalculatorService.runHourCalculatorTimer();
					OnCloseScreenLock(new EventArgs());
					_dataService.SetIsScreenLocked (false);
					Unsubscribe();
					UnSubScribeFromBaseViewModel();
					_bsmBoxWifiService.startBoxDataTimer();
					this.ShowViewModel<DashboardViewModel>();
					Close (this);
				});
			}
		}

		#region Events
		public event EventHandler CloseScreenLock;
		protected virtual void OnCloseScreenLock(EventArgs e)
		{
			if (CloseScreenLock != null)
			{
				CloseScreenLock(this, e);
			}
		}

		public event EventHandler CloseScreenLockAlert;
		protected virtual void OnCloseScreenLockAlert(EventArgs e)
		{
			if (CloseScreenLockAlert != null)
			{
				CloseScreenLockAlert(this, e);
			}
		}

		public event EventHandler NetworkChange;
		protected virtual void OnNetworkChange(EventArgs e)
		{
			if (NetworkChange != null)
			{
				NetworkChange(this, e);
			}
		}
		#endregion

		#region unsubscribe
		private void Unsubscribe()
		{			
			_drivingTimerService.stopDrivingTimer ();
			_messenger.Unsubscribe<DrivingTickMessage> (_drivingTickToken);
			_messenger.Unsubscribe<BoxDataMessage> (_boxDataMessage);
			_messenger.Unsubscribe<NetworkStatusChangedMessage> (_networkStatusChanged);
			_messenger.Unsubscribe<DriverStatusFromBoxWifiServiceMessage> (_updatestatusformService);
		}
		#endregion
	}
}

