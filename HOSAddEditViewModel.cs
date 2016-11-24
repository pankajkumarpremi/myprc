using MvvmCross.Core.ViewModels;
using System.Windows.Input;
using MvvmCross.Plugins.Messenger;
using BSM.Core.Services;
using MvvmCross.Plugins.File;
using System;
using System.Collections.Generic;
using BSM.Core.Messages;
using System.Collections.ObjectModel;
using BSM.Core.AuditEngine;
using BSM.Core.ConnectionLibrary;

using MvxPlugins.Geocoder;
using MvvmCross.Platform;
using System.Threading.Tasks;

namespace BSM.Core.ViewModels {
	public class HOSAddEditViewModel: BaseViewModel {
		private readonly ICommunicationService _communicationService;
		private readonly IDataService _dataService;
		private readonly IGeocoder _geocoder;
		private readonly ITimeLogService _timeLogService;
		private readonly ISettingsService _settings;
		private readonly IEmployeeService _employeeService;
		private readonly ISyncService _syncService;
		private bool b_miles;
		IList<Address> address;
		private readonly IMvxMessenger _messenger;
		private readonly ILanguageService _languageService;

		#region ctors
		public HOSAddEditViewModel(ICommunicationService communicationService, IDataService dataService, ITimeLogService timeLogService, ISettingsService settings, IMvxMessenger messenger, IEmployeeService employeeService,ISyncService syncService, ILanguageService languageService)
		{	
			_communicationService = communicationService;
			_dataService = dataService;
			_timeLogService = timeLogService;
			_settings = settings;
			_employeeService = employeeService;
			_syncService = syncService;
			_geocoder = Mvx.Resolve<IGeocoder> ();
			_messenger = messenger;
			_languageService = languageService;
			DriverStatusTypes = new List<DriverStatusTypeClass>();
			DriverStatusTypes.Add(new DriverStatusTypeClass(101, "OFF DUTY", "","", "OFF"));
			DriverStatusTypes.Add(new DriverStatusTypeClass(104, "ON DUTY", "","", "ON"));
			DriverStatusTypes.Add(new DriverStatusTypeClass(103, "DRIVING", "","", "D"));
			DriverStatusTypes.Add(new DriverStatusTypeClass(102, "SLEEPING", "","", "SB"));
			HideDutyStatus = true;
			HideLocation = true;
			_prevEventTime = DateTime.MinValue;
			_nextEventTime = DateTime.MaxValue;
		}
		#endregion

		public void Init(int SelectedId, DateTime SelectedDate) {
			PlottingDate = DateTime.SpecifyKind(SelectedDate,DateTimeKind.Utc);
			if (SelectedId > 0) {
				EnableDelete = true;
				EnableAdd = false;
				EnableEdit = true;
				TimeLogList = _timeLogService.GetAllForDate (PlottingDate, _dataService.GetCurrentDriverId ());

				int i = 0;
				foreach (TimeLogModel tlObj in TimeLogList) {
					if (tlObj.Id == SelectedId) {
						if(tlObj.Editor != 1)
							OrigTimeLogModel = tlObj;


						AddEditTimeLogModel = new TimeLogModel();
						AddEditTimeLogModel = _timeLogService.CopyModel (tlObj, AddEditTimeLogModel);
						break;
					}
					i++;
				}

				TxtTime = AddEditTimeLogModel.LogTime;

				if (i > 0)
					PrevEventTime = TimeLogList [i - 1].LogTime;
				if ((i + 1) < TimeLogList.Count)
					NextEventTime = TimeLogList [i + 1].LogTime;

				if (AddEditTimeLogModel.LogStatus != 0) {
					int eventIndex = AddEditTimeLogModel.LogStatus - 101;
					if (eventIndex > -1 && eventIndex < 4) {
						foreach (DriverStatusTypeClass driverStatusTypeObj in DriverStatusTypes) {
							if (driverStatusTypeObj.driverStatusType == AddEditTimeLogModel.LogStatus) {
								SelectedDriverStatus = driverStatusTypeObj;
							}
						}
					}
				}

				TitleText = _languageService.GetLocalisedString (Constants.str_edit_event);

				UserAddressString = !string.IsNullOrEmpty(AddEditTimeLogModel.Address) ? AddEditTimeLogModel.Address : "";

				if(!string.IsNullOrEmpty(AddEditTimeLogModel.Comment)) {
					if (AddEditTimeLogModel.Comment.StartsWith ("[modified]"))
						TxtComment = AddEditTimeLogModel.Comment.Replace ("[modified]", "");
					else
						TxtComment = AddEditTimeLogModel.Comment;
				} else {
					TxtComment = "";
				}

			} else {
				EnableDelete = false;
				EnableAdd = true;
				EnableEdit = false;
				TimeLogModel tlModel = new TimeLogModel ();
				LocalizeTimeLog (ref tlModel);
				AddEditTimeLogModel = tlModel;
				AddEditTimeLogModel.EquipmentID = _dataService.GetAssetBoxDescription ();
				AddEditTimeLogModel.LogTime = PlottingDate;

				SelectedDriverStatus = DriverStatusTypes [0];
				TitleText = _languageService.GetLocalisedString (Constants.str_add_event);
				TxtTime = PlottingDate;
			}
			// Set Odometer Label
			EmployeeModel empModel = _employeeService.EmployeeDetailsById (_dataService.GetCurrentDriverId ());
			if (empModel.Domain.ToLower ().IndexOf ("kiewit") > -1) {
				OdomTitle = _languageService.GetLocalisedString (Constants.str_engine_hour);
			} else {
				SettingsModel settingsModel = _settings.GetSettingsByName (Constants.SETTINGS_MI_ODO_ENABLED);
				if (settingsModel != null && settingsModel.SettingsValue == "1") {
					b_miles = true;
					OdomTitle = _languageService.GetLocalisedString (Constants.str_mi);
				} else {
					b_miles = false;
					OdomTitle = _languageService.GetLocalisedString (Constants.str_km);
				}
			}

			B_add_modified = false;
			var manulaOdoSettings = _settings.GetSettingsByName (Constants.SETTINGS_MANUAL_ODO_INPUT);
			if(manulaOdoSettings != null && manulaOdoSettings.SettingsValue == "1"){
				EnableOdo = true;
			}
			OdomText = (b_miles ? Convert.ToInt32 (AddEditTimeLogModel.Odometer * 0.621371) : AddEditTimeLogModel.Odometer);
		}

		#region Properties
		private bool _hideLocation;
		public bool HideLocation
		{
			get { return _hideLocation; }
			set { _hideLocation = value; RaisePropertyChanged(() => HideLocation); }
		}

		private bool _hideDutyStatus;
		public bool HideDutyStatus
		{
			get { return _hideDutyStatus; }
			set { _hideDutyStatus = value; RaisePropertyChanged(() => HideDutyStatus); }
		}

		private bool _b_add_modified;
		public bool B_add_modified
		{
			get { return _b_add_modified; }
			set { _b_add_modified = value; RaisePropertyChanged(() => B_add_modified); }
		}

		private string _titleText;
		public string TitleText
		{
			get { return _titleText; }
			set { _titleText = value; RaisePropertyChanged(() => TitleText); }
		}

		private DateTime _plottingDate;
		public DateTime PlottingDate
		{
			get { return _plottingDate; }
			set { _plottingDate = value; 
				RaisePropertyChanged(() => PlottingDate); }
		}

		private DateTime _txtTime;
		public DateTime TxtTime
		{
			get { return _txtTime; }
			set { _txtTime = value;
				RaisePropertyChanged(() => TxtTime);
			}
		}

		private string _txtComment;
		public string TxtComment
		{
			get { return _txtComment; }
			set { _txtComment = value; RaisePropertyChanged(() => TxtComment); }
		}

		private string _odomTitle;
		public string OdomTitle
		{
			get { return _odomTitle; }
			set { _odomTitle = value; RaisePropertyChanged(() => OdomTitle); }
		}

		private int _odomText;
		public int OdomText
		{
			get { return _odomText; }
			set { _odomText = value; RaisePropertyChanged(() => OdomText); }
		}

		private List<DriverStatusTypeClass> _driverStatusTypes;
		public List<DriverStatusTypeClass> DriverStatusTypes
		{
			get { return _driverStatusTypes; }
			set { _driverStatusTypes = value; RaisePropertyChanged(() => DriverStatusTypes); }
		}

		private DriverStatusTypeClass _selectedDriverStatus;
		public DriverStatusTypeClass SelectedDriverStatus
		{
			get { return _selectedDriverStatus; }
			set {
				_selectedDriverStatus = value;
				RaisePropertyChanged(() => SelectedDriverStatus);
			}
		}

		private string _userAddressString;
		public string UserAddressString
		{
			get { return _userAddressString; }
			set { _userAddressString = value;
				HideLocation = false;
				RaisePropertyChanged(() => UserAddressString);
				SearchNow (_userAddressString);
			}
		}

		private ObservableCollection<string> _userAddress = new ObservableCollection<string>();
		public ObservableCollection<string> UserAddress 
		{
			get{ return _userAddress; }
			set{ _userAddress = value; RaisePropertyChanged (()=>UserAddress); }
		}

		private string _selectedLocation;
		public string SelectedLocation
		{
			get{ return _selectedLocation; }
			set{
				_selectedLocation = value;
				RaisePropertyChanged (() => SelectedLocation);
			}
		}

		private bool _enableOdo;
		public bool EnableOdo
		{
			get { return _enableOdo; }
			set { _enableOdo = value; RaisePropertyChanged(() => EnableOdo); }
		}

		private bool _enableDelete;
		public bool EnableDelete
		{
			get { return _enableDelete; }
			set { _enableDelete = value; RaisePropertyChanged(() => EnableDelete); }
		}

		private bool _enableAdd;
		public bool EnableAdd
		{
			get { return _enableAdd; }
			set { _enableAdd = value; RaisePropertyChanged(() => EnableAdd); }
		}

		private bool _enableEdit;
		public bool EnableEdit
		{
			get { return _enableEdit; }
			set { _enableEdit = value; RaisePropertyChanged(() => EnableEdit); }
		}

		private List<TimeLogModel> _timeLogList;
		public List<TimeLogModel> TimeLogList
		{
			get { return _timeLogList; }
			set { _timeLogList = value; RaisePropertyChanged(() => TimeLogList); }
		}

		private TimeLogModel _origTimeLogModel;
		public TimeLogModel OrigTimeLogModel
		{
			get { return _origTimeLogModel; }
			set { _origTimeLogModel = value; RaisePropertyChanged(() => OrigTimeLogModel); }
		}

		private TimeLogModel _addEditTimeLogModel;
		public TimeLogModel AddEditTimeLogModel
		{
			get { return _addEditTimeLogModel; }
			set { _addEditTimeLogModel = value; RaisePropertyChanged(() => AddEditTimeLogModel); }
		}

		private DateTime _prevEventTime;
		public DateTime PrevEventTime
		{
			get { return _prevEventTime; }
			set { 
				_prevEventTime = value;
				RaisePropertyChanged(() => PrevEventTime);
			}
		}

		private DateTime _nextEventTime;
		public DateTime NextEventTime
		{
			get { return _nextEventTime; }
			set { 
				_nextEventTime = value;
				RaisePropertyChanged(() => NextEventTime);
			}
		}

		#endregion

		#region ICommands

		public ICommand SelectDutyStatusCommand {
			get {
				return new MvxCommand<DriverStatusTypeClass>((DutyStatus) => {
					SelectedDriverStatus = DutyStatus;
					HideDutyStatus = !HideDutyStatus;
				});
			}
		}

		public ICommand ShowDutyStatusCommand {
			get {
				return new MvxCommand (() => {
					HideDutyStatus = !HideDutyStatus;
				});
			}
		}

		public ICommand SelectLocationCommand {
			get {
				return new MvxCommand<string>((Location) => {
					SelectedLocation = Location;
					HideLocation = true;
				});
			}
		}

		private MvxCommand _cancelCommand;
		public ICommand CancelCommand
		{
			get
			{
				_cancelCommand = _cancelCommand ?? new MvxCommand(CloseAddingEvent);
				return _cancelCommand;
			}
		}

		/******** <Add Event> *****/ 
		public ICommand AddEvent
		{
			get
			{
				return new MvxCommand(async () => await DoAddCommand());
			}
		}

		public async Task DoAddCommand () {
			List<TimeLogModel> tlModelList = new List<TimeLogModel> ();
			AddEditTimeLogModel.EquipmentID = _dataService.GetAssetBoxDescription();
			switch (SelectedDriverStatus.driverStatusType) {
			case 101:
				AddEditTimeLogModel.Logbookstopid = AuditLogic.OffDuty;
				AddEditTimeLogModel.LogStatus = (int)LOGSTATUS.OffDuty;
				AddEditTimeLogModel.Event = (int)LOGSTATUS.OffDuty;
				break;
			case 102:
				AddEditTimeLogModel.Logbookstopid = AuditLogic.Sleeping;
				AddEditTimeLogModel.Event = (int)LOGSTATUS.Sleeping;
				AddEditTimeLogModel.LogStatus = (int)LOGSTATUS.Sleeping;
				break;
			case 103:
				AddEditTimeLogModel.Logbookstopid = AuditLogic.Driving;
				AddEditTimeLogModel.Event = (int)LOGSTATUS.Driving;
				AddEditTimeLogModel.LogStatus = (int)LOGSTATUS.Driving;
				break;
			case 104:
				AddEditTimeLogModel.Logbookstopid = AuditLogic.OnDuty;
				AddEditTimeLogModel.Event = (int)LOGSTATUS.OnDuty;
				AddEditTimeLogModel.LogStatus = (int)LOGSTATUS.OnDuty;
				break;
			default:
				break;
			}
			AddEditTimeLogModel.Id = -1;
			var tmpOdo = 0;
			if (OdomText > 0 && int.TryParse (OdomText.ToString(), out tmpOdo)) {
				tmpOdo = b_miles ? int.Parse (Math.Round (1.60934 * tmpOdo, 0).ToString ()) : tmpOdo;
				AddEditTimeLogModel.Odometer = tmpOdo;
			} else {
				AddEditTimeLogModel.Odometer = OdomText;
			}
			AddEditTimeLogModel.Comment = TxtComment;
			AddEditTimeLogModel.LogTime = DateTime.SpecifyKind(TxtTime,DateTimeKind.Utc);
			await Task.Run (() => {
				reverseGeoCode(_userAddressString);
			});
			_timeLogService.SaveTimeLog (AddEditTimeLogModel);
			tlModelList.Add (AddEditTimeLogModel);

			_messenger.Publish<RefreshMessage> (new RefreshMessage (this, PlottingDate));
			OnCloseView(new EventArgs());
			Close (this);
			_syncService.runTimerCallBackNow ();
		}
		/******** </Add Event> *****/ 



		/******** <Edit Event> *****/ 
		public ICommand EditEvent
		{
			get
			{
				return new MvxCommand(async () => await DoEditCommand());
			}
		}

		public async Task DoEditCommand () {
			if (TxtComment.Length > 0) {
				List<TimeLogModel> tlModelList = new List<TimeLogModel> ();

				if (SelectedDriverStatus.driverStatusType != AddEditTimeLogModel.LogStatus) {
					B_add_modified = true;
				}
//				if (TxtComment != AddEditTimeLogModel.Comment) {
//					B_add_modified = true;
//				}
				if (OdomText != AddEditTimeLogModel.Odometer) {
					B_add_modified = true;
				}

				switch (SelectedDriverStatus.driverStatusType) {
				case 101:
					AddEditTimeLogModel.Logbookstopid = AuditLogic.OffDuty;
					AddEditTimeLogModel.LogStatus = (int)LOGSTATUS.OffDuty;
					AddEditTimeLogModel.Event = (int)LOGSTATUS.OffDuty;
					break;
				case 102:
					AddEditTimeLogModel.Logbookstopid = AuditLogic.Sleeping;
					AddEditTimeLogModel.Event = (int)LOGSTATUS.Sleeping;
					AddEditTimeLogModel.LogStatus = (int)LOGSTATUS.Sleeping;
					break;
				case 103:
					AddEditTimeLogModel.Logbookstopid = AuditLogic.Driving;
					AddEditTimeLogModel.Event = (int)LOGSTATUS.Driving;
					AddEditTimeLogModel.LogStatus = (int)LOGSTATUS.Driving;
					break;
				case 104:
					AddEditTimeLogModel.Logbookstopid = AuditLogic.OnDuty;
					AddEditTimeLogModel.Event = (int)LOGSTATUS.OnDuty;
					AddEditTimeLogModel.LogStatus = (int)LOGSTATUS.OnDuty;
					break;
				default:
					break;
				}

				if(OrigTimeLogModel != null){ //If originalTLR is not null that means we have edited this record before so a TimeLogType.Modified record is created already
					OrigTimeLogModel.Type = (int)TimeLogType.Modified;
					OrigTimeLogModel.HaveSent = false;
					OrigTimeLogModel.Signed = false;
					_timeLogService.SaveTimeLog(OrigTimeLogModel);
					tlModelList.Add (OrigTimeLogModel);
					AddEditTimeLogModel.Id = -1; //We only create a new record with Editor=1 if TimeLogType.Modified record is not created yet otherwise just update the Editor=1 record 
				}

				AddEditTimeLogModel.HaveSent = false;
				AddEditTimeLogModel.Signed = false;

				if(B_add_modified){
					AddEditTimeLogModel.Editor = 1;
					AddEditTimeLogModel.Comment = "[modified]" + TxtComment.Trim();
				}else{
					AddEditTimeLogModel.Comment = TxtComment.Trim();
				}

				var tmpOdo = 0;
				if (OdomText > 0 && int.TryParse (OdomText.ToString(), out tmpOdo)) {
					tmpOdo = b_miles ? int.Parse (Math.Round (1.60934 * tmpOdo, 0).ToString ()) : tmpOdo;
					AddEditTimeLogModel.Odometer = tmpOdo;
				} else {
					AddEditTimeLogModel.Odometer = OdomText;
				}

				if (!UserAddressString.Equals (AddEditTimeLogModel.Address) || AddEditTimeLogModel.Latitude == 0 || AddEditTimeLogModel.Longitude == 0) {
					AddEditTimeLogModel.Address = UserAddressString;
					AddEditTimeLogModel.LogTime = DateTime.SpecifyKind(TxtTime,DateTimeKind.Utc);
					await Task.Run (() => {
						reverseGeoCode(UserAddressString);
					});
				}
				_timeLogService.SaveTimeLog(AddEditTimeLogModel);
				tlModelList.Add (AddEditTimeLogModel);


				if(AddEditTimeLogModel.OrigLogTime == TimeLogList.ToArray()[TimeLogList.Count-1].OrigLogTime) {
					DateTime currentGraphDate = Util.GetDateTimeNow();
					TimeLogModel tmpTLModel = _timeLogService.GetByDrvIdLogTime(_dataService.GetCurrentDriverId(),  currentGraphDate.AddDays(1).Date);
					if(tmpTLModel != null && tmpTLModel.Type == (int)TimeLogType.Auto){
						tmpTLModel.Event = AddEditTimeLogModel.Event;
						tmpTLModel.Signed = false;
						tmpTLModel.HaveSent = false;
						_timeLogService.SaveTimeLog(AddEditTimeLogModel);
						tlModelList.Add (AddEditTimeLogModel);
					}
				}
				_messenger.Publish<RefreshMessage> (new RefreshMessage (this, PlottingDate));
				OnCloseView(new EventArgs());
				Close (this);
				_syncService.runTimerCallBack ();
			} else {
				//show comment required dialog
				OnNeedCommentDialog(new EventArgs());
			}
		}
		/******** </Edit Event> *****/ 

		/******** <Delete Event>  *****/ 
		private MvxCommand _deleteEvent;
		public ICommand DeleteEvent {
			get
			{
				return new MvxCommand(() =>
					{	
						//called when delete is clicked
						if (TxtComment.Length > 0) {
							OnDeleteEventDialog(new EventArgs());
						}else{
							OnNeedCommentDialog(new EventArgs());
						}
					});
			}
		}

		private MvxCommand _deleteEventCommand;
		public ICommand DeleteEventCommand
		{
			get
			{
				return new MvxCommand(async () => await DoDeleteCommand());
			}
		}

		public async Task DoDeleteCommand () {
				List<TimeLogModel> tlModelList = new List<TimeLogModel> ();
				//do delete action and redirect to the main eventsviewmodel. called after pressing ok within dialog
				AddEditTimeLogModel.Comment = "[modified]" + TxtComment;
				AddEditTimeLogModel.Event = (int)LOGSTATUS.DeleteStatus;
				AddEditTimeLogModel.LogStatus = (int)LOGSTATUS.DeleteStatus;
				AddEditTimeLogModel.HaveSent = false;
				AddEditTimeLogModel.Signed = false;
				_timeLogService.SaveTimeLog(AddEditTimeLogModel);
				tlModelList.Add (AddEditTimeLogModel);

				//If this the last event of the day also update the next Auto type event at Mid-night with last timelog event before the one we are deleting
				if(TimeLogList!=null) {
					if(AddEditTimeLogModel.OrigLogTime == TimeLogList.ToArray()[TimeLogList.Count-1].OrigLogTime) {
						TimeLogModel tmpTL = _timeLogService.GetByDrvIdLogTime(_dataService.GetCurrentDriverId(), PlottingDate.AddDays(1).Date);
						if(tmpTL != null && tmpTL.Type == (int)TimeLogType.Auto){
							TimeLogModel tmpTL2 = _timeLogService.GetLastBeforeDate(_dataService.GetCurrentDriverId(), AddEditTimeLogModel.LogTime);
						if (tmpTL2 != null) {
							tmpTL.Event = tmpTL2.Event;
							tmpTL.LogStatus = tmpTL2.Event;
						} else {
							tmpTL.Event = (int)LOGSTATUS.OffDuty;
							tmpTL.LogStatus = (int)LOGSTATUS.OffDuty;
						}
							tmpTL.Signed = false;
							tmpTL.HaveSent = false;
							_timeLogService.SaveTimeLog(tmpTL);
							tlModelList.Add (tmpTL);
						}
					}
				}
				_messenger.Publish<RefreshMessage> (new RefreshMessage (this, PlottingDate));
				OnCloseView(new EventArgs());
				Close (this);
				_syncService.runTimerCallBack ();

		}
		/******** </Delete Event> *****/ 
		#endregion

		#region Events
		public event EventHandler CloseView;
		protected virtual void OnCloseView(EventArgs e) {
			if (CloseView != null) {
				CloseView(this, e);
			}
		}


		public event EventHandler DeleteEventDialog;
		protected virtual void OnDeleteEventDialog(EventArgs e) {
			if (DeleteEventDialog != null) {
				DeleteEventDialog(this, e);
			}
		}

		public event EventHandler NeedCommentDialog;
		protected virtual void OnNeedCommentDialog(EventArgs e) {
			if (NeedCommentDialog != null) {
				NeedCommentDialog(this, e);
			}
		}

		#endregion

		public async void SearchNow(string searchTerm) {
			try{
				address = await _geocoder.GetAddressesAsync(UserAddressString);
				if(address != null && address.Count > 0){
					UserAddress.Clear();
					foreach(Address ad in address){
						string addr = ad.AddressLine != null ? ad.AddressLine : "";
						addr += ad.AdministrativeArea != null ? ", " + ad.AdministrativeArea : "";
						addr += ad.Country != null ? ", " + ad.Country : "";

						UserAddress.Add(addr);
					}
				}
			}
			catch(Exception){
			}
		}

		public async void reverseGeoCode(string locAddress) {
			List<TimeLogModel> lsttlm = new List<TimeLogModel> ();
			if ((UserAddressString != null && !UserAddressString.Equals (AddEditTimeLogModel.Address)) || AddEditTimeLogModel.Latitude == 0 || AddEditTimeLogModel.Longitude == 0) {
				if (UserAddressString == null) {
					UserAddressString = "";
				}
				AddEditTimeLogModel.Address = UserAddressString;
				try {
					address = await _geocoder.GetAddressesAsync (locAddress);
					if (address != null && address.Count > 0) {
						foreach (Address ad in address) {
							AddEditTimeLogModel.Latitude = ad.Latitude;
							AddEditTimeLogModel.Longitude = ad.Longitude;
						}
					}
				} catch (Exception) {
				}
			}
		}
		private void CloseAddingEvent() {
			_messenger.Publish<RefreshMessage> (new RefreshMessage (this, PlottingDate));
			OnCloseView(new EventArgs());
			UnSubScribeFromBaseViewModel ();
			Close (this);
		}
	}
}
