
using MvvmCross.Core.ViewModels;
using System.Windows.Input;
using MvvmCross.Plugins.Messenger;
using BSM.Core.Services;
using MvvmCross.Plugins.File;
using System;
using System.Collections.Generic;
using BSM.Core.Messages;
using System.Collections.ObjectModel;

using MvvmCross.Platform;
using MvvmCross.Plugins.Json;
using BSM.Core.AuditEngine;
using System.Linq;
using BSM.Core.ConnectionLibrary;
using System.Threading.Tasks;
using MvvmCross.Platform.Platform;

namespace BSM.Core.ViewModels

{
	public class HOSEventsViewModel : BaseViewModel
	{
//		private readonly ICommunicationService _communicationService;
		private readonly IDataService _dataService;
		private readonly ITimeLogService _timeLogService;
		private List<TimeLogModel> allTimeLogData;
		private TimeLogModel lastPrvDayEvent = null;
		private readonly IMvxMessenger _messenger;
		private string licensePlate;
		private readonly IAssetService _assetService;
		private readonly ISyncService _syncService;
		private MvxSubscriptionToken _refreshEvents;
		private readonly ILanguageService _languageService;


		#region ctors
		public HOSEventsViewModel(IDataService dataService, ITimeLogService timeLogService, IMvxMessenger messenger, IAssetService assetService,ISyncService syncService , ILanguageService languageService)
		{
			Mvx.RegisterType<IMvxJsonConverter, MvxJsonConverter>();
//			_communicationService = communicationService;
			_dataService = dataService;
			_timeLogService = timeLogService;
			_messenger = messenger;
			_assetService = assetService;
			_syncService = syncService;
			_languageService = languageService;
			_languageService.LoadLanguage (Mvx.Resolve<IDeviceLocaleService> ().GetLocale());
			_refreshEvents = _messenger.Subscribe<UpdateGraphMessage> (async (message)=>{
				if(PlottingDate.Date == Utils.GetDateTimeNow().Date)
					await PrepareEventRowData ();
				IsBusy = false;
			});
		}
		public async Task Init(DateTime SelectedDate,  bool EnableSendMail) {
			IsBusy = false;
			IsBusy = true;
			await Task.Delay (700);
			PlottingDate = DateTime.SpecifyKind(SelectedDate,DateTimeKind.Utc);
			await PrepareEventRowData ();
			EnableSendMailE = EnableSendMail;
			await Task.Delay (100);
			IsBusy = false;
		}
		#endregion

		private bool _isBusy;
		public bool IsBusy
		{
			get { return _isBusy; }
			set { _isBusy = value; RaisePropertyChanged(() => IsBusy); }
		}

		private DateTime _plottingDate;
		public DateTime PlottingDate
		{
			get { return _plottingDate; }
			set { _plottingDate = value; RaisePropertyChanged(() => PlottingDate); }
		}

		private List<EventRow> _eventList = new List<EventRow>();
		public List<EventRow> EventList
		{
			get { return _eventList; }
			set { _eventList = value; RaisePropertyChanged(() => EventList); }
		}

		private bool _enableSendMailE;
		public bool EnableSendMailE
		{
			get { return _enableSendMailE; }
			set { _enableSendMailE = value; RaisePropertyChanged(() => EnableSendMailE); }
		}

		private EventRow _selectedEvent;
		public EventRow SelectedEvent
		{
			get { return _selectedEvent; }
			set { _selectedEvent = value;
				RaisePropertyChanged(() => SelectedEvent); 
			}
		}

		private bool _enableDelete;
		public bool EnableDelete
		{
			get { return _enableDelete; }
			set { _enableDelete = value; RaisePropertyChanged(() => EnableDelete); }
		}

		private bool _enableSign = false;
		public bool EnableSign
		{
			get { return _enableSign; }
			set { _enableSign = value; RaisePropertyChanged(() => EnableSign); }
		}

		private bool _enableEdit;
		public bool EnableEdit
		{
			get { return _enableEdit; }
			set { _enableEdit = value; RaisePropertyChanged(() => EnableEdit); }
		}

		private List<EventRow> _selectedEvents = new List<EventRow>();
		public List<EventRow> SelectedEvents
		{
			get { return _selectedEvents; }
			set { _selectedEvents = value; RaisePropertyChanged(() => SelectedEvents); }
		}
		public async Task PrepareEventRowData() {
			if (!IsBusy)
				IsBusy = true;
			await Task.Run (() => {
				List<TimeLogModel> primaryTimeLogData = _timeLogService.GetAllForDate (PlottingDate, _dataService.GetCurrentDriverId ());
				allTimeLogData = primaryTimeLogData;

				AssetModel asm = _assetService.GetAssetByBoxId (_dataService.GetAssetBoxId ());
				licensePlate = asm == null ? "" : asm.VehicleLicense;

				if (allTimeLogData.Count == 0) {
					lastPrvDayEvent = _timeLogService.GetLastBeforeDate (_dataService.GetCurrentDriverId (), PlottingDate);
				}
				List<EventRow> tempEventList = new List<EventRow> ();
				foreach (TimeLogModel logModel in primaryTimeLogData) {
					tempEventList.Add (GeneratePendingEventRowData (logModel));
				}
				if (tempEventList.Count > 0) {
					EventList = new List<EventRow>(tempEventList);
				}
			});
			SetSignBtnVisibility ();
		}

		public EventRow GeneratePendingEventRowData (TimeLogModel logModel) {
			AssetModel asm = _assetService.GetAssetByBoxId (logModel.BoxID);
			licensePlate = asm == null ? "" : asm.VehicleLicense;

			EventRow eRow = new EventRow ();
			eRow.EventTimeLog = logModel;
			eRow.IsSelected = false;
			eRow.EventItem = ((LOGSTATUS)logModel.LogStatus).ToString () + " - " + string.Format ("{0:hh:mm:ss tt}", logModel.LogTime) + " | " +
				((logModel.Address != null && logModel.Address.Length > 0) ? logModel.Address : (_languageService.GetLocalisedString(Constants.str_latitude) + logModel.Latitude + ", Longitude: " + logModel.Longitude)) + " | " +
			"Remarks: " + logModel.Comment + " | " + /*logic for showing [modified] string*/ 
			"Vehicle: " + logModel.EquipmentID + " " + logModel.BoxID + " LP - " + licensePlate + " | " +
			"Odometer: " + logModel.Odometer;
			
/*			Status (Exemption start or stop if applicable), 
			Time, 
			location without postal code.
			Vehicle: DOT/NSC# 12345 Asset# 45678 License Plate # LP1232 ( Jurisdiction/State ON) 
			Odometer: 1001 
			Trailer(if exist): DOT/NSC# 12345 Asset# ABC License Plate # LP1232 Shipping Document number (if exist)
			Remarks: driving (Edited or modifed tag if edited.)

			ON DUTY - 00:00:00 | 75 International Blvd, Toronto, ON | Remarks: Written driver comments | [modified]
			Vehicle: ID-123456 LP-ABC123(ON) Odo-10,001 Km, Trailer: ID-234567 LP-DEF456(ON)
*/
//			eRow.EventType = ((LOGSTATUS)logModel.LogStatus).ToString();//GetLogType(logModel.Event);
//			eRow.EventTime = string.Format ("{0:hh:mm:ss tt}", logModel.LogTime) +" | "+ logModel.EquipmentID + " | ";
//			if (logModel.Address != null && logModel.Address.Length > 0) {
//				eRow.EventLocation = logModel.Address;
//			} else {
//				eRow.EventLocation = "Latitude: " + logModel.Latitude + ", Longitude: " + logModel.Longitude;
//			}
			return eRow;
		}

		#region Commands
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
						List<TimeLogModel> lsttlm = new List<TimeLogModel>();
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
								if(lastPrvDayEvent != null)
								{
									tempLog.Event = lastPrvDayEvent.Event;
									tempLog.LogStatus = lastPrvDayEvent.LogStatus;
								}
								else
								{
									tempLog.Event =  (int)LOGSTATUS.OffDuty;
									tempLog.LogStatus = (int)LOGSTATUS.OffDuty;
								}
								_timeLogService.Insert(tempLog);
								allTimeLogData.Add(tempLog);
							}
							_messenger.Publish<RefreshMessage> (new RefreshMessage (this, PlottingDate));
							//Global.runTimerCallBackNow();
							_syncService.runTimerCallBackNow();
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

		public ICommand AddEditEvent
		{
			get
			{
				return new MvxCommand<EventRow>((selectedEvent) =>
					{	
						var selectedEventId = -100;
						if(selectedEvent != null) {
							SelectedEvent = selectedEvent;
							selectedEventId = SelectedEvent.EventTimeLog.Id;
						}
						ShowViewModel<HOSAddEditViewModel>(new { SelectedDate = PlottingDate, SelectedId = selectedEventId });
					});
			}
		}	
/*
 *  	Commented out the multiple selection for simplicity and lack of actionable menu items. Uncomment when more menu items added - Anudeep
		public ICommand SelectCheckbox
		{
			get {
				return new MvxCommand<EventRow> ((selectedEvent) => {
					if(selectedEvent != null){
						SelectedEvent = selectedEvent;
						if(EventList.Contains(selectedEvent)) {
							EventRow er = EventList.Find(x => x==selectedEvent);
							er.IsSelected = !er.IsSelected;
						}
						ObservableCollection<EventRow> tempColl = new ObservableCollection<EventRow >(EventList);
						EventList = new List<EventRow>(tempColl);

						if(!SelectedEvents.Contains(selectedEvent)){
							SelectedEvents.Add(selectedEvent);
						}else{
							SelectedEvents.Remove(selectedEvent);
						}
						if(SelectedEvents.Count==1) {
							EnableEdit = true; 
						} else {
							EnableEdit = false;
						}
					}

				});

			}
		}
		public ICommand AddEvent
		{
			get
			{
				return new MvxCommand(() =>
					{	
						ShowViewModel<HOSAddDialogViewModel>(new { SelectedDate = PlottingDate });
					});
			}
		}

		public ICommand EditEvent
		{
			get
			{
				return new MvxCommand(() =>
					{	
						var editVM = SelectedEvent.EventTimeLog  != null ? Mvx.Resolve<IMvxJsonConverter>().SerializeObject(SelectedEvent.EventTimeLog) : string.Empty;
						var allTLog = allTimeLogData != null ? Mvx.Resolve<IMvxJsonConverter>().SerializeObject(allTimeLogData) : string.Empty;
						ShowViewModel<HOSEditViewModel>(new { editingVM = editVM, SelectedDate = PlottingDate, allTimeLog = allTLog});
					});
			}
		}
*/
		public void UnSubScribe(){
			_messenger.Unsubscribe<UpdateGraphMessage> (_refreshEvents);
		}

		#endregion
	}
}
