using MvvmCross.Core.ViewModels;
using System.Windows.Input;
using MvvmCross.Plugins.Messenger;
using BSM.Core.Services;
using MvvmCross.Plugins.File;
using System;
using System.Collections.Generic;
using BSM.Core.Messages;
using System.Threading.Tasks;
using BSM.Core.ConnectionLibrary;
using BSM.Core.AuditEngine;
using MvvmCross.Platform;
using System.Linq;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace BSM.Core.ViewModels

{
	public class HOSGraphViewModel : BaseViewModel
	{
		private readonly ISyncService _syncService;
//		private readonly ICommunicationService _communicationService;
		private readonly ITimeLogService _timeLogService;
		private readonly IDataService _dataService;
		private readonly IMvxMessenger _messenger;
		private readonly MvxSubscriptionToken _refreshGraph;
		private List<TimeLogModel> allTimeLogData;
		private TimeLogModel lastPrvDayEvent = null;

		#region ctors
		public HOSGraphViewModel(IMvxMessenger messenger, ITimeLogService timeLogService, IDataService dataService,ISyncService syncService)
		{
			_syncService = syncService;
			_timeLogService = timeLogService;
			_dataService = dataService;
			_messenger = messenger;
			_refreshGraph = _messenger.Subscribe<UpdateGraphMessage> (async (message)=>{
				if(PlottingDate.Date == Util.GetDateTimeNow().Date) {
					await PrepareLogData();
				}
				IsBusy = false;
			});
		}

		public async Task Init(DateTime SelectedDate, bool EnableSendMail) {
//			Debug.WriteLine ("init start {0}", DateTime.Now);
			IsBusy = false;
			IsBusy = true;
			await Task.Delay (700);
			PlottingDate = SelectedDate;
			await PrepareLogData ();
			EnableSendMailG = EnableSendMail;
			await Task.Delay (100);
			IsBusy = false;
//			Debug.WriteLine ("init end {0}", DateTime.Now);
		}
		#endregion

		private bool _isBusy;
		public bool IsBusy
		{
			get { return _isBusy; }
			set { _isBusy = value; RaisePropertyChanged(() => IsBusy); }
		}

		private bool _enableSendMailG;
		public bool EnableSendMailG
		{
			get { return _enableSendMailG; }
			set { _enableSendMailG = value; RaisePropertyChanged(() => EnableSendMailG); }
		}

		private int _isEmergencyUseStart = -1;
		public int IsEmergencyUseStart
		{
			get { return _isEmergencyUseStart; }
			set { _isEmergencyUseStart = value; RaisePropertyChanged(() => IsEmergencyUseStart); }
		}

		private int _selectedTimeLogIndexOffSet = 0;
		public int SelectedTimeLogIndexOffSet
		{
			get { return _selectedTimeLogIndexOffSet; }
			set { _selectedTimeLogIndexOffSet = value; RaisePropertyChanged(() => SelectedTimeLogIndexOffSet); }
		}


		private DateTime _plottingDate;
		public DateTime PlottingDate
		{
			get { return _plottingDate; }
			set { _plottingDate = value; RaisePropertyChanged(() => PlottingDate); }
		}

		private ObservableCollection<TimeLogModel> _timeLogList;
		public ObservableCollection<TimeLogModel> TimeLogList
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

		private bool _enableSign;
		public bool EnableSign
		{
			get { return _enableSign; }
			set { _enableSign = value; RaisePropertyChanged(() => EnableSign); }
		}

		public event EventHandler SignLogDialog;
		protected virtual void OnSignLogDialog(EventArgs e)
		{
			if (SignLogDialog != null)
			{
				SignLogDialog(this, e);
			}
		}

		public event EventHandler NoSignDialog;
		protected virtual void OnNoSignDialog(EventArgs e)
		{
			if (NoSignDialog != null)
			{
				NoSignDialog(this, e);
			}
		}

		private MvxCommand _signLogCommand;
		public ICommand SignLogCommand
		{
			get
			{
				return new MvxCommand(() =>
					{	
						EmployeeModel currentEmployee = EmployeeDetail();
						//Check to see if currentEmployee has signature, if not, take them to MyProfile add signature
						if(currentEmployee.Signature != null && currentEmployee.Signature.Length > 1){
							var notSignedLog = allTimeLogData.Where(p=>p.Signed == false).ToList<TimeLogModel>();

							if(notSignedLog != null && notSignedLog.Count > 0){
								foreach(TimeLogModel TLrow in notSignedLog)
								{
										TLrow.Signed = true;
										TLrow.HaveSent = false;
										_timeLogService.Update(TLrow);
								}
							}
							//If no timelogs and trying to sign, add one off duty if no events previously otherwise add one with last previous event
							if(allTimeLogData.Count == 0){
								TimeLogModel tempLog = new TimeLogModel();
								LocalizeTimeLog(ref tempLog);
								tempLog.Type =  (int)TimeLogType.Auto;
								tempLog.LogTime = PlottingDate;
								tempLog.Signed = true;
								if(lastPrvDayEvent != null){
									tempLog.Event = lastPrvDayEvent.Event;
									tempLog.LogStatus = lastPrvDayEvent.LogStatus;
								}
								else{
									tempLog.Event =  (int)LOGSTATUS.OffDuty;
									tempLog.LogStatus =  (int)LOGSTATUS.OffDuty;
								}
								_timeLogService.Insert(tempLog);
							}
							//Global.runTimerCallBackNow();
							_syncService.runTimerCallBackNow();
							_messenger.Publish<RefreshMessage> (new RefreshMessage (this, PlottingDate));
						} else{
							OnNoSignDialog(new EventArgs());
						}
					});
			}
		}

		private void SetSignBtnVisibility()
		{
			bool logSheetSigned = true;
			foreach (TimeLogModel logrow in allTimeLogData) {
				if (!logrow.Signed) {
					logSheetSigned = false;
					break;
				}
			}
			//If no events for current day show sign button based on last event of previous day
			if(allTimeLogData.Count == 0 && lastPrvDayEvent != null && !lastPrvDayEvent.Signed)
				logSheetSigned = false;

			if (logSheetSigned) {
				EnableSign = false;
			} else {
				EnableSign = true;
			}
		}

		private MvxCommand _signLog;
		public ICommand SignLog
		{
			get
			{
				return new MvxCommand(() =>
					{	
						//called when sign is clicked
						OnSignLogDialog(new EventArgs());

					});
			}
		}

		public ICommand AddEvent
		{
			get
			{
				return new MvxCommand(() =>
					{	
						var editVM = string.Empty;
						var allTLog = allTimeLogData;
						_messenger.Publish<AddEventsTabMessage> (new AddEventsTabMessage (this, PlottingDate));
						ShowViewModel<HOSAddEditViewModel>(new { editingVM = editVM, SelectedDate = PlottingDate, allTimeLog = allTLog});
					});
			}
		}	

		public ICommand SendEmailG
		{
			get
			{
				return new MvxCommand(() =>
					{	
						ShowViewModel<SendEmailViewModel>(new { SelectedDate = PlottingDate});
					});
			}
		}	

		public async Task PrepareLogData() {
			if (!IsBusy)
				IsBusy = true;
			await Task.Run (() => {
				IsEmergencyUseStart = AuditLogic.EmergencyUseStart;
				List<TimeLogModel> primaryTimeLogData = _timeLogService.GetAllForDate (PlottingDate, _dataService.GetCurrentDriverId ());
				allTimeLogData = primaryTimeLogData;
				if (allTimeLogData.Count == 0) {
					lastPrvDayEvent = _timeLogService.GetLastBeforeDate (_dataService.GetCurrentDriverId (), PlottingDate);
				}
				TimeLogList = new ObservableCollection<TimeLogModel> (ValidateLogData (primaryTimeLogData));
				prepareTotalHours (TimeLogList);
			});
			SetSignBtnVisibility ();
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
			t.Event = tlr.LogStatus;
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
					t.Event = tlr.LogStatus;
				}

				logdata.Insert (0, t);
				//If we are adding an Auto event for midnight let's change ouroff set to 1 so that correct portion gets selected based on the actual timelog list that user sees
				SelectedTimeLogIndexOffSet = 1;
			}

//			if(logdata != null && logdata.Count > 0){
//				logdata = logdata.OrderBy(p=>p.LogTime).ToList<TimeLogModel>();
//			}
			return logdata.OrderBy(p=>p.LogTime).ToList();
		}

		public void prepareTotalHours(ObservableCollection<TimeLogModel> data) {
			if (data == null || data.Count == 0) 
				return;
			for (int i = 1; i < data.Count; i++)
			{
				TimeSpan ts = new TimeSpan (data [i].LogTime.Ticks - data [i - 1].LogTime.Ticks);
				switch (data[i - 1].LogStatus)
				{
				case (int)LOGSTATUS.OffDuty:
					TotalOffDuty = TotalOffDuty.Add (ts);
					break;
				case (int)LOGSTATUS.OnDuty:
					TotalOnDuty = TotalOnDuty.Add(ts);
					break;
				case (int)LOGSTATUS.Driving:
					TotalDriving = TotalDriving.Add(ts);
					break;
				case (int)LOGSTATUS.Sleeping:
					TotalSleeping = TotalSleeping.Add(ts);
					break;
				}
			}
			if (data.Count == 1) {
				switch (data[0].Event)
				{
				case (int)LOGSTATUS.OffDuty:
					TotalOffDuty = new TimeSpan (24, 0, 0);
					break;
				case (int)LOGSTATUS.OnDuty:
					TotalOnDuty = new TimeSpan (24, 0, 0);
					break;
				case (int)LOGSTATUS.Driving:
					TotalDriving = new TimeSpan (24, 0, 0);
					break;
				case (int)LOGSTATUS.Sleeping:
					TotalSleeping = new TimeSpan (24, 0, 0);
					break;
				}
			}
			TotalOffDutyStr = timeSpanToStr (TotalOffDuty);
			TotalOnDutyStr = timeSpanToStr (TotalOnDuty);
			TotalSleepingStr = timeSpanToStr (TotalSleeping);
			TotalDrivingStr = timeSpanToStr (TotalDriving);
			Total = TotalDriving + TotalOffDuty + TotalOnDuty + TotalSleeping;
			if (Total.Days == 1 && Total.TotalMinutes == 1440) {
				TotalStr = "24:00";
			} else {
				TotalStr = Total.ToString (@"hh\:mm");
			}
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

		public void UnSubScribe(){
			_messenger.Unsubscribe<UpdateGraphMessage> (_refreshGraph);
		}
	}
}

