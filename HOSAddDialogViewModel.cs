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

namespace BSM.Core.ViewModels

{
	public class HOSAddDialogViewModel: BaseViewModel
	{
		private readonly ICommunicationService _communicationService;
		private readonly IDataService _dataService;
		private readonly IGeocoder _geocoder;
		private readonly ITimeLogService _timeLogService;
		private TimeLogModel addingTLM = null;
		private readonly ISettingsService _settings;
		private bool b_miles = false;
		IList<Address> address;
		private readonly IMvxMessenger _messenger;
		private readonly ILanguageService _languageService;

		#region ctors
		public HOSAddDialogViewModel(ICommunicationService communicationService, IDataService dataService, ITimeLogService timeLogService, ISettingsService settings, IMvxMessenger messenger, ILanguageService languageService)
		{	
			
			_communicationService = communicationService;
			_dataService = dataService;
			_timeLogService = timeLogService;
			_settings = settings;
			_geocoder = Mvx.Resolve<IGeocoder> ();
			_messenger = messenger;
			_languageService = languageService;
			EnableDelete = false;
			EnableEdit = false;
			EnableAdd = true;

			DriverStatusTypes = new List<DriverStatusTypeClass>();
			DriverStatusTypes.Add(new DriverStatusTypeClass(101, "OFF DUTY", "","", "OFF"));
			DriverStatusTypes.Add(new DriverStatusTypeClass(104, "ON DUTY", "","", "ON"));
			DriverStatusTypes.Add(new DriverStatusTypeClass(103, "DRIVING", "","", "D"));
			DriverStatusTypes.Add(new DriverStatusTypeClass(102, "SLEEPING", "", "","SB"));

			SelectedDriverStatus = DriverStatusTypes [0];

		}
		#endregion

		public void Init(DateTime SelectedDate) {
			PlottingDate = SelectedDate;
			addingTLM = new TimeLogModel ();
			LocalizeTimeLog (ref addingTLM);
			addingTLM.LogTime = PlottingDate;
			TxtTime = addingTLM.LogTime;
			try {
				SettingsModel sm = _settings.GetSettingsByName(Constants.SETTINGS_MI_ODO_ENABLED);
				string mi_odo_enabled = sm.SettingsValue;
				if (mi_odo_enabled != null && mi_odo_enabled.Equals("1")) {
					b_miles = true;
				}
			} catch (Exception e) {
			}
			if (b_miles)
				OdomTitle = _languageService.GetLocalisedString (Constants.str_mi);
			else
				OdomTitle = _languageService.GetLocalisedString(Constants.str_km);

			TitleText = _languageService.GetLocalisedString (Constants.str_add_event);
			HideLocation = true;
			HideDutyStatus = true;
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
			set { _plottingDate = value; RaisePropertyChanged(() => PlottingDate); }
		}

		private DateTime _txtTime;
		public DateTime TxtTime
		{
			get { return _txtTime; }
			set { _txtTime = value; RaisePropertyChanged(() => TxtTime); }
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
//				UserAddressString = addingTLM.Address != string.Empty ? addingTLM.Address : "";
				SearchNow (_userAddressString);
			}
		}

		private int _odomText;
		public int OdomText
		{
			get { return _odomText; }
			set { _odomText = value; RaisePropertyChanged(() => OdomText); }
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

		private MvxCommand _closePopup;
		public ICommand ClosePopup
		{
			get
			{
				_closePopup = _closePopup ?? new MvxCommand(ClosePop);
				return _closePopup;
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

		private void ClosePop()
		{
			Close(this);
		}

		private void CloseAddingEvent()
		{
			ShowViewModel<HOSEventsViewModel>(new { SelectedDate = PlottingDate });
		}

		public async void SearchNow(string searchTerm)
		{

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
//			if (searchTerm != null && searchTerm.Length > 0) {
//				CoWorkers = new ObservableCollection<string> (list.Where(p=>p.UserName.Contains(searchTerm)).Select (a => a.UserName).ToList ());
//				Invert ();
//			}
		}

		public ICommand AddSaveEvent
		{
			get
			{
				return new MvxCommand(() =>
					{	
						addingTLM.EquipmentID = _dataService.GetAssetBoxDescription();
						switch (SelectedDriverStatus.driverStatusType) {
						case 101:
							addingTLM.Logbookstopid = AuditLogic.OffDuty;
							addingTLM.LogStatus = (int)LOGSTATUS.OffDuty;
							addingTLM.Event = (int)LOGSTATUS.OffDuty;
							break;
						case 102:
							addingTLM.Logbookstopid = AuditLogic.Sleeping;
							addingTLM.Event = (int)LOGSTATUS.Sleeping;
							addingTLM.LogStatus = (int)LOGSTATUS.Sleeping;
							break;
						case 103:
							addingTLM.Logbookstopid = AuditLogic.Driving;
							addingTLM.Event = (int)LOGSTATUS.Driving;
							addingTLM.LogStatus = (int)LOGSTATUS.Driving;
							break;
						case 104:
							addingTLM.Logbookstopid = AuditLogic.OnDuty;
							addingTLM.Event = (int)LOGSTATUS.OnDuty;
							addingTLM.LogStatus = (int)LOGSTATUS.OnDuty;
							break;
//						case 105:
//							addingTLM.Logbookstopid = AuditLogic.ThirtyMinutesOffDutyStart;
//							addingTLM.Event = LOGSTATUS.Break;
//							break;
//						case 107:
//							addingTLM.Logbookstopid = AuditLogic.PERSONALUSE;
//							addingTLM.Event = LOGSTATUS.Personal;
//							break;
						default:
						break;
						}

						addingTLM.Odometer = OdomText;
						addingTLM.Comment = TxtComment;
						reverseGeoCode(_userAddressString);

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

		public ICommand SelectDutyStatusCommand {
			get {
				return new MvxCommand<DriverStatusTypeClass>((DutyStatus) => {
					SelectedDriverStatus = DutyStatus;
					HideDutyStatus = true;
				});
			}
		}

		public ICommand ShowDutyStatusCommand {
			get {
				return new MvxCommand (() => {
					HideDutyStatus = false;
				});
			}
		}

		public async void reverseGeoCode(string locAddress)
		{
			if ((UserAddressString != null && !UserAddressString.Equals (addingTLM.Address)) || addingTLM.Latitude == 0 || addingTLM.Longitude == 0) {
				if (UserAddressString == null) {
					UserAddressString = "";
				}
				addingTLM.Address = UserAddressString;
				try {
					address = await _geocoder.GetAddressesAsync (locAddress);
					if (address != null && address.Count > 0) {
						foreach (Address ad in address) {
							addingTLM.Latitude = ad.Latitude;
							addingTLM.Longitude = ad.Longitude;
						}
					}
				} catch (Exception) {
				}
			}
				_timeLogService.InsertOrUpdate(addingTLM);
			List<TimeLogModel> lsttlm = new List<TimeLogModel> ();
			lsttlm.Add (addingTLM);
			await _communicationService.SendTimeLogData(lsttlm);
			_messenger.Publish<RefreshMessage> (new RefreshMessage (this, PlottingDate));
			OnCloseView(new EventArgs());
			Close (this);
		}

		public event EventHandler CloseView;
		protected virtual void OnCloseView(EventArgs e)
		{
			if (CloseView != null)
			{
				CloseView(this, e);
			}
		}
	}
}
