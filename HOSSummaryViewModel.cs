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
using MvvmCross.Platform;
using MvvmCross.Core;
using System.Threading.Tasks;
using MvvmCross.Platform.Platform;

namespace BSM.Core.ViewModels
{
	public class HOSSummaryViewModel : BaseViewModel
	{
		#region Member Variables
		private readonly IDataService _dataService;
		private readonly ITimeLogService _timeLogService;
		private readonly IHosAlertService _hosAlertService;
		private readonly ICommunicationService _communicationService;
		private List<HosAlertModel> hosAlerts;
		private List<TimeLogModel> lstTimeLog;
		private readonly IMvxMessenger _messenger;
		private readonly ISyncService _syncService;
		private readonly IEmployeeService _employeeService;
		private readonly IBSMBoxWifiService _bsmBoxWifiService;
		#endregion

		#region ctors
		public HOSSummaryViewModel(IDataService dataService, ITimeLogService timeLogService, IHosAlertService hosAlertService, IMvxMessenger messenger, ICommunicationService communicationService,ISyncService syncService,IEmployeeService employeeService,IBSMBoxWifiService bsmBoxWifiService)
		{
			_bsmBoxWifiService = bsmBoxWifiService;
			_syncService = syncService;
			_dataService = dataService;
			_timeLogService = timeLogService;
			_hosAlertService = hosAlertService;
			_communicationService = communicationService;
			_messenger = messenger;
			_employeeService = employeeService;

			SummaryData_14_Days = new RecapSumData ();
			SummaryData_8_Days = new RecapSumData ();
			SummaryData_7_Days = new RecapSumData ();

			UnSubScribeFromBaseViewModel ();
		}

		public async Task Init(int SummaryType) {
			IsBusy = false;
			IsBusy = true;
			await Task.Delay (700);
			await Task.Run (() => {
				hosAlerts = _hosAlertService.GetHosAlerts ();

				PopulateSummaryData (_timeLogService.GetAllForDriver (_dataService.GetCurrentDriverId ()));

				SummaryData_8_Days.recapItems.AddRange (SummaryData_14_Days.recapItems);
				SummaryData_7_Days.recapItems.AddRange (SummaryData_14_Days.recapItems);

				for (int i = 7; i < SummaryData_14_Days.recapItems.Count; i++) {
					SummaryData_7_Days.recapItems.RemoveAt (SummaryData_7_Days.recapItems.Count - 1);
					if (i > 7)
						SummaryData_8_Days.recapItems.RemoveAt (SummaryData_8_Days.recapItems.Count - 1);
				}
			});
			switch (SummaryType) {
			case 7:
				SummaryList = SummaryData_7_Days.recapItems;
				TotalOnDutyStr = Util.HHmmFromMinutes ((int)SummaryData_7_Days.tlOnDuty7);
				TotalSleepingStr = Util.HHmmFromMinutes ((int)SummaryData_7_Days.tlSleepDuty7);
				TotalDrivingStr = Util.HHmmFromMinutes ((int)SummaryData_7_Days.tlDriveDuty7);
				TotalOffDutyStr = Util.HHmmFromMinutes ((int)SummaryData_7_Days.tlOffDuty7);
				TotalStr = Util.HHmmFromMinutes(7 * 24 * 60);
				break;
			case 8:
				SummaryList = SummaryData_8_Days.recapItems;
				TotalOnDutyStr = Util.HHmmFromMinutes ((int)SummaryData_8_Days.tlOnDuty8);
				TotalSleepingStr = Util.HHmmFromMinutes ((int)SummaryData_8_Days.tlSleepDuty8);
				TotalDrivingStr = Util.HHmmFromMinutes ((int)SummaryData_8_Days.tlDriveDuty8);
				TotalOffDutyStr = Util.HHmmFromMinutes ((int)SummaryData_8_Days.tlOffDuty8);
				TotalStr = Util.HHmmFromMinutes(8 * 24 * 60);
				break;
			case 14:
				SummaryList = SummaryData_14_Days.recapItems;
				TotalOnDutyStr = Util.HHmmFromMinutes ((int)SummaryData_14_Days.tlOnDuty14);
				TotalSleepingStr = Util.HHmmFromMinutes ((int)SummaryData_14_Days.tlSleepDuty14);
				TotalDrivingStr = Util.HHmmFromMinutes ((int)SummaryData_14_Days.tlDriveDuty14);
				TotalOffDutyStr = Util.HHmmFromMinutes ((int)SummaryData_14_Days.tlOffDuty14);
				TotalStr = Util.HHmmFromMinutes(14 * 24 * 60);
				break;
			}
			await Task.Delay (100);
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

		private List<RecapSumDataItem> _summaryList;
		public List<RecapSumDataItem> SummaryList
		{
			get { return _summaryList; }
			set { _summaryList = value; RaisePropertyChanged(() => SummaryList); }
		}

		private RecapSumData _summaryData_14_Days;
		public RecapSumData SummaryData_14_Days
		{
			get { return _summaryData_14_Days; }
			set { _summaryData_14_Days = value; RaisePropertyChanged(() => SummaryData_14_Days); }
		}

		private RecapSumData _summaryData_8_Days;
		public RecapSumData SummaryData_8_Days
		{
			get { return _summaryData_8_Days; }
			set { _summaryData_8_Days = value; RaisePropertyChanged(() => SummaryData_8_Days); }
		}

		private RecapSumData _summaryData_7_Days;
		public RecapSumData SummaryData_7_Days
		{
			get { return _summaryData_7_Days; }
			set { _summaryData_7_Days = value; RaisePropertyChanged(() => SummaryData_7_Days); }
		}

		private string _totalOffDutyStr;
		public string TotalOffDutyStr
		{
			get { return _totalOffDutyStr; }
			set { _totalOffDutyStr = value; RaisePropertyChanged(() => TotalOffDutyStr); }
		}

		private string _totalOnDutyStr;
		public string TotalOnDutyStr
		{
			get { return _totalOnDutyStr; }
			set { _totalOnDutyStr = value; RaisePropertyChanged(() => TotalOnDutyStr); }
		}

		private string _totalDrivingStr;
		public string TotalDrivingStr
		{
			get { return _totalDrivingStr; }
			set { _totalDrivingStr = value; RaisePropertyChanged(() => TotalDrivingStr); }
		}

		private string _totalSleepingStr;
		public string TotalSleepingStr
		{
			get { return _totalSleepingStr; }
			set { _totalSleepingStr = value; RaisePropertyChanged(() => TotalSleepingStr); }
		}

		private string _totalStr;
		public string TotalStr
		{
			get { return _totalStr; }
			set { _totalStr = value; RaisePropertyChanged(() => TotalStr); }
		}

		private RecapSumDataItem _recapItem;
		public RecapSumDataItem RecapItem
		{
			get { return _recapItem; }
			set { _recapItem = value; RaisePropertyChanged(() => RecapItem); }
		}

		#endregion

		#region Events
		#endregion

		#region Commands
		public ICommand Show14DaySummaryCommand
		{
			get
			{
				return new MvxCommand(() =>
					{	
						SummaryList = SummaryData_14_Days.recapItems;
						TotalOnDutyStr = Util.HHmmFromMinutes ((int)SummaryData_14_Days.tlOnDuty14);
						TotalSleepingStr = Util.HHmmFromMinutes ((int)SummaryData_14_Days.tlSleepDuty14);
						TotalDrivingStr = Util.HHmmFromMinutes ((int)SummaryData_14_Days.tlDriveDuty14);
						TotalOffDutyStr = Util.HHmmFromMinutes ((int)SummaryData_14_Days.tlOffDuty14);
						TotalStr = Util.HHmmFromMinutes(14 * 24 * 60);
					});
			}
		}

		public ICommand Show8DaySummaryCommand
		{
			get
			{
				return new MvxCommand(() =>
					{	
						SummaryList = SummaryData_8_Days.recapItems;
						TotalOnDutyStr = Util.HHmmFromMinutes ((int)SummaryData_8_Days.tlOnDuty8);
						TotalSleepingStr = Util.HHmmFromMinutes ((int)SummaryData_8_Days.tlSleepDuty8);
						TotalDrivingStr = Util.HHmmFromMinutes ((int)SummaryData_8_Days.tlDriveDuty8);
						TotalOffDutyStr = Util.HHmmFromMinutes ((int)SummaryData_8_Days.tlOffDuty8);
						TotalStr = Util.HHmmFromMinutes(8 * 24 * 60);
					});
			}
		}

		public ICommand Show7DaySummaryCommand
		{
			get
			{
				return new MvxCommand(() =>
					{	
						SummaryList = SummaryData_7_Days.recapItems;
						TotalOnDutyStr = Util.HHmmFromMinutes ((int)SummaryData_7_Days.tlOnDuty7);
						TotalSleepingStr = Util.HHmmFromMinutes ((int)SummaryData_7_Days.tlSleepDuty7);
						TotalDrivingStr = Util.HHmmFromMinutes ((int)SummaryData_7_Days.tlDriveDuty7);
						TotalOffDutyStr = Util.HHmmFromMinutes ((int)SummaryData_7_Days.tlOffDuty7);
						TotalStr = Util.HHmmFromMinutes(7 * 24 * 60);
					});
			}
		}

		private IMvxCommand _signBtnClick;
		public IMvxCommand SignBtnClick {
			get {
				_signBtnClick = _signBtnClick ?? new MvxCommand<RecapSumDataItem> (btnSignClick);
				return _signBtnClick;
			}
		}

		public void btnSignClick (RecapSumDataItem item)
		{
			RecapItem = item;
			if(RecapItem != null && RecapItem.ShowSign){
				OnSignLogDialog (new EventArgs());
			}
		}

		public void SignLogMeth(RecapSumDataItem item) {
			TimeLogModel lastPrvDayEvent = null;
			List<TimeLogModel> primaryTimeLogData = _timeLogService.GetAllForDate (item.dateT, _dataService.GetCurrentDriverId ());
			lstTimeLog = primaryTimeLogData;

			if (lstTimeLog.Count == 0) {
				lastPrvDayEvent = _timeLogService.GetLastBeforeDate (_dataService.GetCurrentDriverId (), item.dateT);
			}

			//Do Something
			List<TimeLogModel> lsttlm = new List<TimeLogModel>();

			EmployeeModel currentEmployee =  _employeeService.EmployeeDetailsById (_dataService.GetCurrentDriverId ());
			//Check to see if currentEmployee has signature, if not, take them to MyProfile add signature
			if(currentEmployee.Signature != null && currentEmployee.Signature.Length > 1){
				foreach(TimeLogModel TLrow in lstTimeLog)
				{
					if (!TLrow.Signed) {
						TLrow.Signed = true;
						TLrow.HaveSent = false;
						_timeLogService.Update(TLrow);
					}
				}
				//If no timelogs and trying to sign, add one off duty if no events previously otherwise add one with last previous event
				if(lstTimeLog.Count == 0){
					TimeLogModel tempLog = new TimeLogModel();
					_bsmBoxWifiService.LocalizeTimeLog(ref tempLog);
					tempLog.Type =  (int)TimeLogType.Auto;
					tempLog.LogTime = item.dateT;
					tempLog.Signed = true;
					if (lastPrvDayEvent != null) {
						tempLog.Event = lastPrvDayEvent.Event;
						tempLog.LogStatus = lastPrvDayEvent.Event;
					} else {
						tempLog.Event = (int)LOGSTATUS.OffDuty;
						tempLog.LogStatus = (int)LOGSTATUS.OffDuty;
					}
					_timeLogService.Insert(tempLog);
					lstTimeLog.Add(tempLog);
				}
				//Global.runTimerCallBackNow();
				item.ShowSign = false;
				_syncService.runTimerCallBackNow();
				// TODO : Referesh the calender Items
				_messenger.Publish<CalendarMessage>(new CalendarMessage(this));

			} else{
				OnNoSignDialog(new EventArgs());
			}

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

//		private void SetSignBtnVisibility()
//		{
//			bool logSheetSigned = true;
//			foreach (TimeLogModel logrow in allTimeLogData) {
//				if (!logrow.Signed) {
//					logSheetSigned = false;
//					break;
//				}
//			}
//			//If no events for current day show sign button based on last event of previous day
//			if(allTimeLogData.Count == 0 && lastPrvDayEvent != null && !lastPrvDayEvent.Signed)
//				logSheetSigned = false;
//
//			if (logSheetSigned) {
//				EnableSign = false;
//			} else {
//				EnableSign = true;
//			}
//		}

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

		private IMvxCommand _violationBtnClick;

		public IMvxCommand ViolationBtnClick {
			get {
				_violationBtnClick = _violationBtnClick ?? new MvxCommand<RecapSumDataItem> (btnViolationClick);
				return _violationBtnClick;
			}
		}

		public void btnViolationClick (RecapSumDataItem item)
		{
			// publish a message to mainviewmodel where you need to trigger ShowGraphViewOnDateChangeCommand
			_messenger.Publish<LoadEventsTabMessage> (new LoadEventsTabMessage (this, item.dateT));
		}

		#endregion

		#region unsubscribe
		#endregion

		public void PopulateSummaryData(List<TimeLogModel> list)
		{
			const int startTime = 0;
			try
			{
				for (int index = 0; index <= 13; index++)
				{
					DateTime sd = Utils.GetDateTimeNow().Date.AddDays(-1 * index).AddMinutes(startTime);
					DateTime ed = sd.AddHours(24);
					if (ed > Utils.GetDateTimeNow())
						ed = Utils.GetDateTimeNow();
					RecapSumDataItem item = GetRecapSummaryItem (sd, ed, index, list, hosAlerts);
					SummaryData_14_Days.addData(item);
				}
			}
			catch (Exception ex) {
				if(GlobalInstance.Debug)
					MvxTrace.Trace(MvxTraceLevel.Error, "GetSummary() Exception: " + ex.ToString());
			}
		}

		private RecapSumDataItem GetRecapSummaryItem (DateTime sd, DateTime ed, int lblIndex, List<TimeLogModel> list, List<HosAlertModel> hosAlerts)
		{
			RecapSumDataItem item = new RecapSumDataItem (this);
			TimeLogModel preTlr = null;
			double offDuty = 0;
			double sleepDuty = 0;
			double driveDuty = 0;
			double onDuty = 0;

			if (list != null)
			{
				for (int index = list.Count - 1; index >= 0; index--)
				{
					if (list[index].LogTime > ed) continue;
					else
					{
						if (list[index].LogTime > sd)
						{
							if (list[index].Event == (int)LOGSTATUS.OnDuty)
							{
								if (preTlr == null)
								{
									onDuty = onDuty + ed.Subtract(list[index].LogTime).TotalMinutes;
								}
								else
								{
									onDuty = onDuty + preTlr.LogTime.Subtract(list[index].LogTime).TotalMinutes;
								}
							}
							else if (list[index].Event == (int)LOGSTATUS.Driving) {
								if (preTlr == null)
								{
									driveDuty = driveDuty + ed.Subtract(list[index].LogTime).TotalMinutes;
								}
								else
								{
									driveDuty = driveDuty + preTlr.LogTime.Subtract(list[index].LogTime).TotalMinutes;
								}
							}
							else if (list[index].Event == (int)LOGSTATUS.Sleeping) {
								if (preTlr == null)
								{
									sleepDuty = sleepDuty + ed.Subtract(list[index].LogTime).TotalMinutes;
								}
								else
								{
									sleepDuty = sleepDuty + preTlr.LogTime.Subtract(list[index].LogTime).TotalMinutes;
								}
							}
							preTlr = list[index];
						}
						else
						{
							if (list[index].Event == (int)LOGSTATUS.OnDuty)
							{
								if (preTlr == null)
								{
									onDuty = onDuty + ed.Subtract(sd).TotalMinutes;
								}
								else
								{
									onDuty = onDuty + preTlr.LogTime.Subtract(sd).TotalMinutes;
								}
							}
							else if (list[index].Event == (int)LOGSTATUS.Driving) {
								if (preTlr == null)
								{
									driveDuty = driveDuty + ed.Subtract(sd).TotalMinutes;
								}
								else
								{
									driveDuty = driveDuty + preTlr.LogTime.Subtract(sd).TotalMinutes;
								}
							}
							else if (list[index].Event == (int)LOGSTATUS.Sleeping) {
								if (preTlr == null)
								{
									sleepDuty = sleepDuty + ed.Subtract(sd).TotalMinutes;
								}
								else
								{
									sleepDuty = sleepDuty + preTlr.LogTime.Subtract(sd).TotalMinutes;
								}
							}
							break;
						}
					}

				}
			}
			onDuty = (int)onDuty;
			offDuty = (24 * 60) - (int)onDuty - (int)driveDuty - (int)sleepDuty;

			if (lblIndex >= 0 && lblIndex <= 6)
			{
				SummaryData_7_Days.tlOffDuty7 = SummaryData_7_Days.tlOffDuty7 + offDuty;
				SummaryData_7_Days.tlSleepDuty7 = SummaryData_7_Days.tlSleepDuty7 + sleepDuty;
				SummaryData_7_Days.tlDriveDuty7 = SummaryData_7_Days.tlDriveDuty7 + driveDuty;
				SummaryData_7_Days.tlOnDuty7 = SummaryData_7_Days.tlOnDuty7 + onDuty;
			}
			if (lblIndex >= 0 && lblIndex <= 7)
			{
				SummaryData_8_Days.tlOffDuty8 = SummaryData_8_Days.tlOffDuty8 + offDuty;
				SummaryData_8_Days.tlSleepDuty8 = SummaryData_8_Days.tlSleepDuty8 + sleepDuty;
				SummaryData_8_Days.tlDriveDuty8 = SummaryData_8_Days.tlDriveDuty8 + driveDuty;
				SummaryData_8_Days.tlOnDuty8 = SummaryData_8_Days.tlOnDuty8 + onDuty;
			}
			SummaryData_14_Days.tlOffDuty14 = SummaryData_14_Days.tlOffDuty14 + offDuty;
			SummaryData_14_Days.tlSleepDuty14 = SummaryData_14_Days.tlSleepDuty14 + sleepDuty;
			SummaryData_14_Days.tlDriveDuty14 = SummaryData_14_Days.tlDriveDuty14 + driveDuty;
			SummaryData_14_Days.tlOnDuty14 = SummaryData_14_Days.tlOnDuty14 + onDuty;

			string curDate = String.Format("{0:dddd - dd/MMMM}", Utils.GetDateTimeNow().Date.AddDays(-1 * lblIndex));

			item.date = curDate;
			item.dateT = Utils.GetDateTimeNow().Date.AddDays(-1 * lblIndex);
			item.onDuty = (int)onDuty;
			item.sleepDuty = (int)sleepDuty;
			item.driveDuty = (int)driveDuty;
			item.offDuty = (int)offDuty;
			item.TotalOnDutyStr = Util.HHmmFromMinutes ((int)onDuty);
			item.TotalSleepingStr = Util.HHmmFromMinutes ((int)sleepDuty);
			item.TotalDrivingStr = Util.HHmmFromMinutes ((int)driveDuty);
			item.TotalOffDutyStr = Util.HHmmFromMinutes ((int)offDuty);

			foreach (var lstTime in list) { 
				if(!lstTime.Signed) {
					if ((Utils.GetDateTimeNow().Date.AddDays(-1 * lblIndex)).ToString("yyyy-MM-dd").CompareTo (lstTime.LogTime.ToString ("yyyy-MM-dd")) == 0) {
						item.ShowSign = true;
					}
				}
			}

			/*foreach (var alert in hosAlerts) {
				if((Utils.GetDateTimeNow().Date.AddDays(-1 * lblIndex)).ToString("yyyy-MM-dd").CompareTo(alert.date.ToString ("yyyy-MM-dd")) == 0){
					item.ShowViolation = true;
				}
			}*/

			return item;
		}
	}
}
