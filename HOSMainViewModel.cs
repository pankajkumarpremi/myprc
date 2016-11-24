using System;
using MvvmCross.Core.ViewModels;
using System.Windows.Input;
using MvvmCross.Plugins.Messenger;
using BSM.Core.Services;
using MvvmCross.Plugins.File;
using System.Collections.Generic;
using BSM.Core.Messages;
using System.Threading.Tasks;
using BSM.Core;
using System.Globalization;
using BSM.Core.ConnectionLibrary;
using System.Collections.ObjectModel;
using MvvmCross.Core;
using BSM.Core.AuditEngine;
using System.Linq;

using MvvmCross.Platform;
using MvvmCross.Platform.Core;
using MvvmCross.Platform.Platform;

namespace BSM.Core.ViewModels

{
	public class HOSMainViewModel : BaseViewModel
	{
		private readonly IInspectionReportService _reportService;
		private readonly IDataService _dataService;
		private readonly ITimeLogService _timeLogService;
		private readonly ICommunicationService _communicationService;
		private readonly IMvxMessenger _messenger;
		private MvxSubscriptionToken _refreshCalendarItems;
		private MvxSubscriptionToken _refreshPage;
		private MvxSubscriptionToken _refreshSummaryOptionSelection;
		private MvxSubscriptionToken _loadEventsTabMessage;
		private MvxSubscriptionToken _addEventsTabMessage;
		private MvxSubscriptionToken _breakTimerUpdateMessage;
		private readonly IBreakTimerService _breakTimerservice;
		private readonly IHourCalculatorService _hourcalcService;
		private readonly ISyncService _syncService;
		private readonly ICoWorkerService _coworkerService;
		private readonly IEmployeeService _employeeService;
		private readonly IBSMBoxWifiService _bsmBoxWifiService;
		private readonly MvxSubscriptionToken _driverStatusMessage;
		private readonly MvxSubscriptionToken _breakTimerCompleteMessage;
		private readonly MvxSubscriptionToken _breakTimerCancelMessage;
		private readonly MvxSubscriptionToken _refreshVehicle;
		private readonly MvxSubscriptionToken _cycleSuccess;
		private readonly MvxSubscriptionToken _updategrapheventsiOS;

		bool isRefreshed = false;
		IHosAlertService _hosAlertService;
		#region ctors
		public HOSMainViewModel(IDataService dataService, ITimeLogService timeLogService, ICommunicationService communicationService, IMvxMessenger messenger, IHosAlertService hosAlertService,IBreakTimerService breakTimerservice,
			IHourCalculatorService hourcalcService,ISyncService syncService,ICoWorkerService coworkerservice,IEmployeeService employeeservice,IBSMBoxWifiService boxBoxWifiService,IInspectionReportService reportService)
		{
			_reportService = reportService;
			_bsmBoxWifiService = boxBoxWifiService;
			_employeeService = employeeservice;
			_coworkerService = coworkerservice;
			_syncService = syncService;
			_hourcalcService = hourcalcService;
			_breakTimerservice = breakTimerservice;
			_communicationService = communicationService;
			_timeLogService = timeLogService;
			_dataService = dataService;
			_messenger = messenger;
			_hosAlertService = hosAlertService;

			_updategrapheventsiOS = _messenger.SubscribeOnMainThread<UpdateGraphEventsiOSMessage>((message) =>
				{
					var currentDate = Util.GetDateTimeNow ();
					var lsttimelog = _timeLogService.GetAllForDateRange(currentDate.AddDays(-30), currentDate, _dataService.GetCurrentDriverId());
					var hosalerts = _hosAlertService.GetHosAlerts ();
					UpdateCalendarOnly(lsttimelog, hosalerts);
					if(_isTabGraphSelected || _isTabEventsSelected){
						_messenger.Publish<UpdateGraphMessage>(new UpdateGraphMessage(this));	
					}
				});

			_refreshVehicle = _messenger.SubscribeOnMainThread<RefeshVehicleStatusMessage>((message) =>
				{
					if (_dataService.GetAssetBoxId () == -1) {
						DefectLevel = 2;
						HasInspectionDoneIn24Hrs = 2;
					} else {
						DefectLevel = _reportService.HasMajorDefects24Hrs (_dataService.GetAssetBoxId ());
						HasInspectionDoneIn24Hrs = _reportService.HasInspectionDoneIn24Hrs (_dataService.GetAssetBoxId ());
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

			_refreshCalendarItems = _messenger.Subscribe<CalendarMessage> ((SummaryType)=>{
				var currentDate = Util.GetDateTimeNow ();
				List<TimeLogModel> lsttimelog = _timeLogService.GetAllForDateRange(currentDate.AddDays(-30), currentDate, _dataService.GetCurrentDriverId());
				List<HosAlertModel> hosalerts = _hosAlertService.GetHosAlerts ();
				UpdateCalendarOnly(lsttimelog,hosalerts);
			});
			_refreshPage = _messenger.Subscribe<RefreshMessage> (async (message)=>{
				if (!IsBusy)
					IsBusy = true;
				var LastTimelog = _timeLogService.GetLast (_dataService.GetCurrentDriverId());
				if(LastTimelog.Event != _dataService.GetCurrentLogStatus()){
					_dataService.PersistCurrentLogStatus (LastTimelog.Event);
					SelectedDriverStatus = DriverStatusTypes.FirstOrDefault(p=>p.driverStatusType == LastTimelog.Event);
				}
				await Task.Run (() => {
					SelectedCalendaritem = CalendarItems.FirstOrDefault (p=>p.date.Date == message._plottingDate.Date);
					isRefreshed = true;
					var currentDate = Util.GetDateTimeNow ();
					List<TimeLogModel> lsttimelog = _timeLogService.GetAllForDateRange(currentDate.AddDays(-30), currentDate, _dataService.GetCurrentDriverId());
					List<HosAlertModel> hosalerts = _hosAlertService.GetHosAlerts ();
					UpdateCalendar(lsttimelog,hosalerts);
					HideOtherTabs = false;
					IsBusy = false;
				});
			});

			_refreshSummaryOptionSelection = _messenger.Subscribe<RefreshSummaryMessage> ((Message)=>{
				switch (Message.SummaryType) {
				case 1:
					SelectedFilter = FilterList[0];
					break;
				case 7:
					SelectedFilter = FilterList[1];
					break;
				case 8:
					SelectedFilter = FilterList[2];
					break;
				case 14:
					SelectedFilter = FilterList[3];
					break;
				default:
				break;
				}
			});

			_loadEventsTabMessage = _messenger.Subscribe<LoadEventsTabMessage> ((message) => {
				SelectedCalendaritem = CalendarItems.FirstOrDefault (p=>p.date.Date == message._dateT.Date);
				foreach (var cl in CalendarItems) {
					if (cl.date.CompareTo(SelectedCalendaritem.date)==0) {
						cl.isItemSelected = true;
					} else {
						cl.isItemSelected = false;
					}
				}
				SelectedSummaryOption = 1;
				// In Android We are binding selected filter to radio group
				ChangeSelectedFilter = false;
				SelectedFilter = FilterList[0];
				ChangeSelectedFilter = true;
				this.ShowEventsCommand.Execute(null);
			});
			_addEventsTabMessage = _messenger.Subscribe<AddEventsTabMessage> ((message) => {
				IsTabGraphSelected = false;
				IsTabLogsSelected = false;
				IsTabEventsSelected = true;
				IsTabSummarySelected = false;
			});

			SummaryOptions.Add (1);
			SummaryOptions.Add (7);
			SummaryOptions.Add (8);
			SummaryOptions.Add (14);
			HideOtherTabs = false;
			FilterList.Add (new Filter(1,"DAILY"));
			FilterList.Add (new Filter(7,"7 DAYS"));
			FilterList.Add (new Filter(8,"8 DAYS"));
			FilterList.Add (new Filter(14,"14 DAYS"));


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

			_driverStatusMessage = _messenger.SubscribeOnMainThread<UpdateDriverStatusMessage>((message) =>
				{
					MvxSingleton<IMvxMainThreadDispatcher>.Instance.RequestMainThreadAction(()=>{						
						SelectedDriverStatus = message.driverStatusType;
						// Here We have to update the graph and calender
						var currentDate = Util.GetDateTimeNow ();
						var lsttimelog = _timeLogService.GetAllForDateRange(currentDate.AddDays(-30), currentDate, _dataService.GetCurrentDriverId());
						var hosalerts = _hosAlertService.GetHosAlerts ();
						UpdateCalendarOnly(lsttimelog, hosalerts);
						if(_isTabGraphSelected || _isTabEventsSelected){
							_messenger.Publish<UpdateGraphMessage>(new UpdateGraphMessage(this));	
						}
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
			GenerateCalendarItems ();
		}
		public async Task Init(DateTime selectedDate,string pageFrom) {
			if (selectedDate != DateTime.MinValue) {
				SelectedCalendaritem.date = selectedDate;
			}
			if (!string.IsNullOrEmpty (pageFrom)) {
				PageFrom = pageFrom;
			} else {
				PageFrom = "";
			}
			await Task.Run (() => {
				SyncTimeLog ();
			});
		}
		#endregion

		#region properties

		public bool ChangeSelectedFilter = true;


		private bool _isBusy;
		public bool IsBusy
		{
			get { return _isBusy; }
			set { _isBusy = value; RaisePropertyChanged(() => IsBusy); }
		}

		private bool _showToday;
		public bool ShowToday
		{
			get { return _showToday; }
			set { _showToday = value; RaisePropertyChanged(() => ShowToday); }
		}

		private bool _isTabGraphSelected;
		public bool IsTabGraphSelected
		{
			get { return _isTabGraphSelected; }
			set { _isTabGraphSelected = value; RaisePropertyChanged(() => IsTabGraphSelected); }
		}

		private bool _isTabEventsSelected;
		public bool IsTabEventsSelected
		{
			get { return _isTabEventsSelected; }
			set { _isTabEventsSelected = value; RaisePropertyChanged(() => IsTabEventsSelected); }
		}

		private bool _isTabLogsSelected;
		public bool IsTabLogsSelected
		{
			get { return _isTabLogsSelected; }
			set { _isTabLogsSelected = value; RaisePropertyChanged(() => IsTabLogsSelected); }
		}

		private bool _isTabSummarySelected;
		public bool IsTabSummarySelected
		{
			get { return _isTabSummarySelected; }
			set { _isTabSummarySelected = value; RaisePropertyChanged(() => IsTabSummarySelected); }
		}

		// Comment By Subbarao
		/*private int _defectLevel;
		public int DefectLevel
		{
			get{return _defectLevel; }
			set{_defectLevel = value;RaisePropertyChanged (()=>DefectLevel); }
		}*/

		private List<CalendarItem> _calendaritems = new List<CalendarItem> ();
		public List<CalendarItem> CalendarItems
		{
			get { return _calendaritems; }
			set
			{
				_calendaritems = value;
				RaisePropertyChanged(() => CalendarItems);
			}
		}

		private CalendarItem _selectedCalendaritem = new CalendarItem ();
		public CalendarItem SelectedCalendaritem
		{
			get { return _selectedCalendaritem; }
			set
			{
				_selectedCalendaritem = value;
				if ((DateTime.Now.Subtract (SelectedCalendaritem.date)).TotalDays <= 15) {
					EnableSendMailM = true;
				} else {
					EnableSendMailM = false;
				}
				RaisePropertyChanged(() => SelectedCalendaritem);
			}
		}

		private bool _enableSendMailM = false;
		public bool EnableSendMailM
		{
			get { return _enableSendMailM; }
			set
			{
				_enableSendMailM = value;
				RaisePropertyChanged(() => EnableSendMailM);
			}
		}

		private List<int> _summaryOptions = new List<int> ();
		public List<int> SummaryOptions
		{
			get { return _summaryOptions; }
			set
			{
				_summaryOptions = value;
				RaisePropertyChanged(() => SummaryOptions);
			}
		}

		private int _selectedSummaryOption = 1;
		public int SelectedSummaryOption
		{
			get { return _selectedSummaryOption; }
			set
			{
				_selectedSummaryOption = value;
				RaisePropertyChanged(() => SelectedSummaryOption);
			}
		}

		private bool _hideOtherTabs = false;
		public bool HideOtherTabs
		{
			get { return _hideOtherTabs; }
			set
			{
				_hideOtherTabs = value;
				RaisePropertyChanged(() => HideOtherTabs);
			}
		}

		private bool _scrollToLast = false;
		public bool ScrollToLast
		{
			get { return _scrollToLast; }
			set
			{
				_scrollToLast = value;
				RaisePropertyChanged(() => ScrollToLast);
			}
		}

		private List<Filter> _filterList = new List<Filter> ();
		public List<Filter> FilterList
		{
			get { return _filterList; }
			set
			{
				_filterList = value;
				RaisePropertyChanged(() => FilterList);
			}
		}

		private Filter _selectedFilter;
		public Filter SelectedFilter
		{
			get { return _selectedFilter; }
			set
			{
				_selectedFilter = value;
				if(ChangeSelectedFilter)
				showSummary (SelectedFilter.filterDays);
				RaisePropertyChanged(() => SelectedFilter);
			}
		}

		public string PageFrom {
			get;
			set;
		}

		#endregion

		#region Commands

		public async void SyncTimeLog() {
			if (!IsBusy)
				IsBusy = true;
			// Mvx.Trace (MvxTraceLevel.Diagnostic,"SyncTimeLog Intialized..");
			// await _communicationService.SyncUser4TimeLog ();
			var currentDate = Util.GetDateTimeNow ();
			List<TimeLogModel> lsttimelog = _timeLogService.GetAllForDateRange(currentDate.AddDays(-30), currentDate, _dataService.GetCurrentDriverId());
			List<HosAlertModel> hosalerts = _hosAlertService.GetHosAlerts ();
			UpdateCalendar(lsttimelog, hosalerts);
			// Mvx.Trace (MvxTraceLevel.Diagnostic,"SyncTimeLog Completed..");
			IsBusy = false;
		}

		public void unSubscribe() {			
			_messenger.Unsubscribe<CalendarMessage> (_refreshCalendarItems);
			_messenger.Unsubscribe<RefreshMessage> (_refreshPage);
			_messenger.Unsubscribe<RefreshSummaryMessage> (_refreshSummaryOptionSelection);
			_messenger.Unsubscribe<LoadEventsTabMessage> (_loadEventsTabMessage);
			_messenger.Unsubscribe<AddEventsTabMessage> (_addEventsTabMessage);
			_messenger.Unsubscribe<BreakTimerUpdateMessage> (_breakTimerUpdateMessage);
			_messenger.Unsubscribe<UpdateDriverStatusMessage> (_driverStatusMessage);
			_messenger.Unsubscribe<BreakTimerCompleteMessage> (_breakTimerCompleteMessage);
			_messenger.Unsubscribe<BreakTimerCancelMessage> (_breakTimerCancelMessage);
			_messenger.Unsubscribe<UpdateCycleMessage> (_cycleSuccess);
			_messenger.Unsubscribe<RefeshVehicleStatusMessage> (_refreshVehicle);
			_messenger.Unsubscribe<UpdateGraphEventsiOSMessage> (_updategrapheventsiOS);
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

		public ICommand ShowTodayCommand {
			get {
				return new MvxCommand(() => {
					SelectedCalendaritem = CalendarItems.LastOrDefault<CalendarItem>();
					this.ShowGraphViewOnDateChangeCommand.Execute(SelectedCalendaritem);
					ScrollToLast = true;
				});
			}
		}

		public ICommand ShowGraphViewOnDateChangeCommand
		{
			get
			{
				return new MvxCommand<CalendarItem>((SelectedItem) => 
					{
						SelectedCalendaritem = SelectedItem;
						foreach (var cl in CalendarItems) {
							if (cl.date.CompareTo(SelectedItem.date)==0) {
								cl.isItemSelected = true;
							} else {
								cl.isItemSelected = false;
							}
						}
//						ObservableCollection<CalendarItem> tempColl = new ObservableCollection<CalendarItem>(CalendarItems);
//						CalendarItems = new List<CalendarItem>(tempColl);

						//show viewmodel based on the selected tab. 
						if(IsTabEventsSelected)
							ShowViewModel<HOSEventsViewModel>(new { SelectedDate = SelectedCalendaritem.date , EnableSendMail = EnableSendMailM});
						else if (IsTabGraphSelected)
							ShowViewModel<HOSGraphViewModel>(new { SelectedDate = SelectedCalendaritem.date , EnableSendMail = EnableSendMailM});
						else if (IsTabLogsSelected)
							ShowViewModel<HOSLogSheetViewModel>(new { SelectedDate = SelectedCalendaritem.date, EnableSendMail = EnableSendMailM });
						else
							ShowGraphViewcommand.Execute(null);
					});
			}
		}
			
		public ICommand ShowGraphViewcommand
		{
			get
			{
				return new MvxCommand(() => 
					{
						SelectedSummaryOption = 1;
						IsTabGraphSelected = true;
						IsTabLogsSelected = false;
						IsTabEventsSelected = false;
						IsTabSummarySelected = false;
						HideOtherTabs = false;
						ShowViewModel<HOSGraphViewModel>(new { SelectedDate = SelectedCalendaritem.date, EnableSendMail = EnableSendMailM });
					});
			}
		}


		public ICommand ShowEventsCommand
		{
			get
			{
				return new MvxCommand(() =>
					{	
						IsTabGraphSelected = false;
						IsTabLogsSelected = false;
						IsTabEventsSelected = true;
						IsTabSummarySelected = false;
						HideOtherTabs = false;
						ShowViewModel<HOSEventsViewModel>(new { SelectedDate =  SelectedCalendaritem.date, EnableSendMail = EnableSendMailM });
					});
			}
		}

		public ICommand ShowLogSheetsCommand
		{
			get
			{
				return new MvxCommand(() =>
					{	
						IsTabGraphSelected = false;
						IsTabLogsSelected = true;
						IsTabEventsSelected = false;
						IsTabSummarySelected = false;
						HideOtherTabs = false;
						ShowViewModel<HOSLogSheetViewModel>(new { SelectedDate = SelectedCalendaritem.date, EnableSendMail = EnableSendMailM });

					});
			}
		}

		public ICommand ShowSelectedSummaryCommand
		{
			get
			{
				return new MvxCommand<int>((SelectedSummary) =>
					{	
						SelectedSummaryOption = SelectedSummary;
						IsTabGraphSelected = false;
						IsTabLogsSelected = false;
						IsTabEventsSelected = false;
						IsTabSummarySelected = true;
					//	HideOtherTabs = !HideOtherTabs;
						HideOtherTabs = true;

						ShowViewModel<HOSSummaryViewModel>(new {SummaryType = SelectedSummaryOption });
					});
			}
		}

		public ICommand ShowSummaryOptionsCommand
		{
			get
			{
				return new MvxCommand (() => {
					ShowViewModel<HOSSummaryTypeViewModel>();
				});
			}
		}

		#endregion

		public void GenerateCalendarItems() {
//			IsBusy = true;
//			await Task.Run (() => {
				Mvx.Trace (MvxTraceLevel.Diagnostic,"Initializing calendar list populating process..");
				DateTime dt = Util.GetDateTimeNow ();
				// List<CalendarItem> CalendarItems = new List<CalendarItem> ();
				for (int i = -30; i <= 0 ; i++) {
					//set date, day and month and default header and footer colors
					CalendarItem cl = new CalendarItem ();
					cl.Day = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedDayName(dt.AddDays(i).DayOfWeek);
					cl.Date = dt.AddDays(i).Date.ToString("dd");
					cl.Month = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(dt.AddDays(i).Month);
					cl.Status_Header = "#FFFFFF";
					cl.Status_Footer = "#00D487";
					cl.date = dt.AddDays(i);
					cl.isItemSelected = i==0 ? true : false;
					CalendarItems.Add (cl);
				}
			Mvx.Trace (MvxTraceLevel.Diagnostic,"CalenderItems Loaded"); //Elapsed time : " + DateTime.Now.Subtract(dt));
//			});
		}

		public async void UpdateCalendar(List<TimeLogModel> lstTimeLog, List<HosAlertModel> lstAlerts)
		{
			DateTime currentDate = Util.GetDateTimeNow ();
			await Task.Run (() => {
				Mvx.Trace (MvxTraceLevel.Diagnostic,"Updating calendar list after fetching data from the api/sqlite..");

				foreach(var calenderItem in CalendarItems){
					MvxSingleton<IMvxMainThreadDispatcher>.Instance.RequestMainThreadAction(()=>{
						calenderItem.isHOSLogSheetNotSigned = false;
						calenderItem.violationExists = false;
					});

					if(lstTimeLog != null && lstTimeLog.Count > 0){
						var modelIsSigned = lstTimeLog.FirstOrDefault(p=>p.LogTime.Date == calenderItem.date.Date);
						if(modelIsSigned != null){
							MvxSingleton<IMvxMainThreadDispatcher>.Instance.RequestMainThreadAction(()=>{
								calenderItem.isHOSLogSheetNotSigned = true;
							});
						}
					}
					if(lstAlerts != null && lstAlerts.Count > 0){
						var model = lstAlerts.FirstOrDefault(p=>p.date.Date == calenderItem.date.Date);
						if(model != null){
							MvxSingleton<IMvxMainThreadDispatcher>.Instance.RequestMainThreadAction(()=>{
								calenderItem.violationExists = true;
							});
						}
					}
				}
			});
			if (isRefreshed) {				
				isRefreshed = false;
				Mvx.Trace (MvxTraceLevel.Diagnostic,"Calendar items updated.. ");
//				IsTabGraphSelected = true;
//				IsTabLogsSelected = false;
//				IsTabEventsSelected = false;
//Commented by anudeep				ShowGraphViewOnDateChangeCommand.Execute (SelectedCalendaritem);
				SelectedFilter = FilterList[0];
			} else {
				SelectedCalendaritem = CalendarItems.FirstOrDefault (p=>p.date.Date == currentDate.Date);
				ScrollToLast = true;
				Mvx.Trace (MvxTraceLevel.Diagnostic,"Calendar items updated.. "); //Elapsed time : " + DateTime.Now.Subtract(currentDate));
				if (PageFrom == "Dashboard") {
					IsTabGraphSelected = false;
					IsTabEventsSelected = false;
					IsTabLogsSelected = true;
					IsTabSummarySelected = false;
				} else if (PageFrom == "email") {
					IsTabGraphSelected = false;
					IsTabEventsSelected = true;
					IsTabLogsSelected = false;
					IsTabSummarySelected = false;
				} else {
					IsTabGraphSelected = true;
					IsTabLogsSelected = false;
					IsTabEventsSelected = false;
					IsTabSummarySelected = false;
				}
//Commented by anudeep				ShowGraphViewOnDateChangeCommand.Execute (SelectedCalendaritem);
				SelectedFilter = FilterList[0];
			}
		}



		public async void UpdateCalendarOnly(List<TimeLogModel> lstTimeLog, List<HosAlertModel> lstAlerts)
		{
			await Task.Run (() => {
				foreach(var calenderItem in CalendarItems){
					MvxSingleton<IMvxMainThreadDispatcher>.Instance.RequestMainThreadAction(()=>{
						calenderItem.isHOSLogSheetNotSigned = false;
						calenderItem.violationExists = false;
					});
					if(lstTimeLog != null && lstTimeLog.Count > 0){
						var modelIsSigned = lstTimeLog.FirstOrDefault(p=>p.LogTime.Date == calenderItem.date.Date);
						if(modelIsSigned != null){
							MvxSingleton<IMvxMainThreadDispatcher>.Instance.RequestMainThreadAction(()=>{
								calenderItem.isHOSLogSheetNotSigned = true;
							});
						}
					}
					if(lstAlerts != null && lstAlerts.Count > 0){
						var model = lstAlerts.FirstOrDefault(p=>p.date.Date == calenderItem.date.Date);
						if(model != null){
							MvxSingleton<IMvxMainThreadDispatcher>.Instance.RequestMainThreadAction(()=>{
								calenderItem.violationExists = true;
							});
						}
					}
				}
			});
		}

		public void showSummary(int SelectedSummary) {
			SelectedSummaryOption = SelectedSummary;
			if (SelectedSummary == 1) {
				HideOtherTabs = false;
				//SelectedFilter = FilterList[0];
				IsTabGraphSelected = true;
				IsTabLogsSelected = false;
				IsTabEventsSelected = false;
				IsTabSummarySelected = false;
				ShowGraphViewOnDateChangeCommand.Execute (SelectedCalendaritem);
			} else {
				HideOtherTabs = true;
				IsTabGraphSelected = false;
				IsTabLogsSelected = false;
				IsTabEventsSelected = false;
				IsTabSummarySelected = true;
				ShowViewModel<HOSSummaryViewModel> (new {SummaryType = SelectedSummaryOption });
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
					tlr.Event = _timeLogService.GetLastByLogbookstopid(_dataService.GetCurrentDriverId(), AuditLogic.EmergencyUseStart).Event;
					tlr.LogStatus = _timeLogService.GetLastByLogbookstopid(_dataService.GetCurrentDriverId(), AuditLogic.EmergencyUseStart).Event; //We end emergency mode with status that we started with
					tlr.Logbookstopid = AuditLogic.EmergencyUseEnd;
					_timeLogService.Insert (tlr);
				}
				//If we were in PersonalUse mode and now a new status is selected; let's end the PersonalUse
				if( prevStatus ==(int) LOGSTATUS.PersonalUse && DriverStatusTypes.IndexOf(_selectedDriverStatus) != 6){ // 6 is PersonalUse
					LocalizeTimeLog(ref tlr);
					tlr.Event =(int) LOGSTATUS.OffDuty;
					tlr.LogStatus =(int) LOGSTATUS.OffDuty;
					tlr.Logbookstopid = AuditLogic.PersonalUseEnd;
					_timeLogService.Insert (tlr);
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
							var	timelog = _timeLogService.GetLast (er.Id);
							if (tlr.Event == (int)LOGSTATUS.OffDuty && tlr.Logbookstopid == AuditLogic.ThirtyMinutesOffDutyStart) {
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
					_timeLogService.Insert (tlr);
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
					_timeLogService.Insert (tlr);
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
					_timeLogService.Insert (tlr);
					_dataService.PersistCurrentLogStatus ((int)LOGSTATUS.Driving);
					//SelectedDriverStatus.driverStatusTimeText = TotalDrivingforStatus.ToString (@"hh\:mm");
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
					_timeLogService.Insert (tlr);
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
						var	curPersonalUsage = _timeLogService.GetPersonalUsageForDateDriver (Util.GetDateTimeNow().Date, _dataService.GetCurrentDriverId());
						if (curPersonalUsage > 75) {
							/*Display no personal usage left for today
							Toast.MakeText (this.ctx, this.ctx.Resources.GetString(Resource.String.str_no_personal_usage_left), ToastLength.Short).Show ();*/
							BSMBoxWifiService.personalUsageLog = null;
						} else {
							BSMBoxWifiService.personalUsageLog = new BSMBoxWifiService.PersonalUseStatusLog ();
							BSMBoxWifiService.personalUsageLog.StartDateTime = Utils.GetDateTimeNow ();
							BSMBoxWifiService.personalUsageLog.PrevOdo = _dataService.GetOdometer ();
							BSMBoxWifiService.personalUsageLog.BoxId = _dataService.GetAssetBoxId ();
							BSMBoxWifiService.personalUsageLog.Distance = _timeLogService.GetPersonalUsageForDateDriver (Utils.GetDateTimeNow ().Date, _dataService.GetCurrentDriverId ());
							tlr = new TimeLogModel ();
							LocalizeTimeLog(ref tlr);
							tlr.Event =(int) LOGSTATUS.OffDuty;
							tlr.LogStatus =(int) LOGSTATUS.OffDuty;
							tlr.Logbookstopid = AuditLogic.PERSONALUSE;
							_timeLogService.Insert (tlr);
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
						_timeLogService.Insert (tlr);
						_dataService.PersistCurrentLogStatus((int)LOGSTATUS.PersonalUse);
					}
					break;
				}
				if(prevStatus != -1 && prevStatus != _dataService.GetCurrentLogStatus()){
					_hourcalcService.runHourCalculatorTimer();
					_syncService.runTimerCallBackNow();
					// Here We have to Update the calender also
					var currentDate = Util.GetDateTimeNow();
					var lsttimelog = _timeLogService.GetAllForDateRange(currentDate.AddDays(-30), currentDate, _dataService.GetCurrentDriverId());
					var hosalerts = _hosAlertService.GetHosAlerts ();
					UpdateCalendarOnly(lsttimelog,hosalerts);
					_messenger.Publish<UpdateGraphMessage> (new UpdateGraphMessage(this));
					if(prevStatus == (int)LOGSTATUS.Driving && _dataService.GetIsScreenLocked()){
						_dataService.SetIsScreenLocked (false);
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
			var lastTimelog = _timeLogService.GetLast (_dataService.GetCurrentDriverId());
			if (_dataService.GetCurrentLogStatus () == (int)LOGSTATUS.Break30Min) {
				if (!_breakTimerservice.isTimerRunning ()) {
					try {
						var totalBreakInsec = Convert.ToInt32 ((Utils.GetDateTimeNow () - lastTimelog.LogTime.ToUniversalTime ()).TotalSeconds);
						_breakTimerservice.SettotalBreakTimeInSec (totalBreakInsec);
					} catch (Exception exp) {
						Mvx.Trace (MvxTraceLevel.Error,"While Updating Break"+exp.ToString());
					}
					_breakTimerservice.start30MinBreakTimer ();
				}
			} else {
				_breakTimerservice.stop30MinBreakTimer ();
			}
			var curPersonalUsage1 = _timeLogService.GetPersonalUsageForDateDriver (Utils.GetDateTimeNow().Date, _dataService.GetCurrentDriverId());
			if (curPersonalUsage1 > 0) {
				/*this.view.FindViewById<LinearLayout>(Resource.Id.hos_personal_distance).Visibility = ViewStates.Visible;
					this.view.FindViewById<View>(Resource.Id.hos_personal_distance_divider).Visibility = ViewStates.Visible;
					this.view.FindViewById<TextView> (Resource.Id.txtV_hos_personal_distance_lable).Visibility = ViewStates.Visible;
					txtPersonalUsage = this.view.FindViewById<TextView> (Resource.Id.txtV_hos_personal_distance);
					txtPersonalUsage.Visibility = ViewStates.Visible;
					txtPersonalUsage.Text = curPersonalUsage.ToString ();*/
			}
			var lastEvent = _dataService.GetCurrentLogStatus ();
			SelectedDriverStatus = DriverStatusTypes.FirstOrDefault (p=>p.driverStatusType == lastEvent);
		}
	}
}
