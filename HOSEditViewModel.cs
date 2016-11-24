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
using MvvmCross.Platform.Platform;

namespace BSM.Core.ViewModels

{
	public class HOSEditViewModel: BaseViewModel
	{
		private readonly ICommunicationService _communicationService;
		private readonly IDataService _dataService;
		private readonly IGeocoder _geocoder;
		private readonly ITimeLogService _timeLogService;
		private TimeLogModel editingTLM = null;
		private TimeLogModel originalTLM = null;
		private readonly ISettingsService _settings;
		private bool b_miles = false;
		IList<Address> address;
		private int eventIndex = -1;
		private List<TimeLogModel> allTimeLogData;
		private readonly IMvxMessenger _messenger;

		#region ctors
		public HOSEditViewModel(ICommunicationService communicationService, IDataService dataService, ITimeLogService timeLogService, ISettingsService settings, IMvxMessenger messenger)
		{	
			
			_communicationService = communicationService;
			_dataService = dataService;
			_timeLogService = timeLogService;
			_settings = settings;
			_geocoder = Mvx.Resolve<IGeocoder> ();
			_messenger = messenger;

			DriverStatusTypes = new List<DriverStatusTypeClass>();
			DriverStatusTypes.Add(new DriverStatusTypeClass(101, "OFF DUTY", "","", "OFF"));
			DriverStatusTypes.Add(new DriverStatusTypeClass(104, "ON DUTY", "","", "ON"));
			DriverStatusTypes.Add(new DriverStatusTypeClass(103, "DRIVING", "", "","D"));
			DriverStatusTypes.Add(new DriverStatusTypeClass(102, "SLEEPING", "","", "SB"));
//			DriverStatusTypes.Add(new DriverStatusTypeClass(105, "PERSONAL", ""));
//			DriverStatusTypes.Add(new DriverStatusTypeClass(107, "BREAK",""));

			editingTLM = new TimeLogModel ();
			LocalizeTimeLog (ref editingTLM);
			try {
				SettingsModel sm = _settings.GetSettingsByName(Constants.SETTINGS_MI_ODO_ENABLED);
				string mi_odo_enabled = sm.SettingsName;
				if (mi_odo_enabled != null && mi_odo_enabled.Equals("1")) {
					b_miles = true;
				}
			} catch (Exception e) {
			}
			if (b_miles)
				OdomTitle = "ODOMETER (Mi): ";
			else
				OdomTitle = "ODOMETER (Km): ";
			EnableDelete = true;
			EnableAdd = false;
			EnableEdit = true;
		}
		#endregion

		public void Init(String editingVM, DateTime SelectedDate, String allTimeLog) {
			PlottingDate = SelectedDate;
			B_add_modified = false;

			if(!string.IsNullOrEmpty(editingVM)){
				editingTLM = Mvx.Resolve<IMvxJsonConverter>().DeserializeObject<TimeLogModel>(editingVM);
				allTimeLogData = Mvx.Resolve<IMvxJsonConverter>().DeserializeObject<List<TimeLogModel>>(allTimeLog);
			}
			if(editingTLM.Editor != 1)
				originalTLM = editingTLM;
			
			TxtTime = editingTLM.LogTime;

			eventIndex = (int)editingTLM.Event - 101;
			if (eventIndex > -1 && eventIndex < 4) {
				SelectedDriverStatus = DriverStatusTypes [eventIndex];
				B_add_modified = false;
			}
			//SelectedDriverStatus = DriverStatusTypes.Find(x => x.driverStatusType == (int)editingTLM.Event);
			OdomText = editingTLM.Odometer;
			UserAddressString = editingTLM.Address;
			if (editingTLM.Comment.StartsWith ("[modified]"))
				TxtComment = editingTLM.Comment.Replace ("[modified]", "");
			else
				TxtComment = editingTLM.Comment;
			
			TitleText = "EDIT EVENT";
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

		public event EventHandler DeleteEventDialog;
		protected virtual void OnDeleteEventDialog(EventArgs e)
		{
			if (DeleteEventDialog != null)
			{
				DeleteEventDialog(this, e);
			}
		}

		public event EventHandler NeedCommentDialog;
		protected virtual void OnNeedCommentDialog(EventArgs e)
		{
			if (NeedCommentDialog != null)
			{
				NeedCommentDialog(this, e);
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

		private MvxCommand _deleteEventCommand;
		public ICommand DeleteEventCommand
		{
			get
			{
				return new MvxCommand(() =>
					{	
						//do delete action and redirect to the main eventsviewmodel. called after pressing ok within dialog
						editingTLM.Comment = "[modified]" + TxtComment;
						editingTLM.Event = (int)LOGSTATUS.DeleteStatus;
						editingTLM.HaveSent = false;
						editingTLM.Signed = false;
						_timeLogService.InsertOrUpdate(editingTLM);

						//If this the last event of the day also update the next Auto type event at Mid-night with last timelog event before the one we are deleting
						if(editingTLM.OrigLogTime == allTimeLogData.ToArray()[allTimeLogData.Count-1].OrigLogTime){
							TimeLogModel tmpTL = _timeLogService.GetByDrvIdLogTime(_dataService.GetCurrentDriverId(), PlottingDate.AddDays(1).Date);
							if(tmpTL != null && tmpTL.Type == (int)TimeLogType.Auto){
								TimeLogModel tmpTL2 = _timeLogService.GetLastBeforeDate(_dataService.GetCurrentDriverId(), editingTLM.LogTime);
								if(tmpTL2 != null)
									tmpTL.Event = tmpTL2.Event;
								else
									tmpTL.Event = (int)LOGSTATUS.OffDuty;
								tmpTL.Signed = false;
								tmpTL.HaveSent = false;
								_timeLogService.InsertOrUpdate(tmpTL);
							}
						}
						List<TimeLogModel> lsttlm = new List<TimeLogModel> ();
						lsttlm.Add (editingTLM);
						sendTimeLog(lsttlm);
					});
			}
		}

		private MvxCommand _deleteEvent;
		public ICommand DeleteEvent
		{
			get
			{
				return new MvxCommand(() =>
					{	
						//called when delete is clicked
						OnDeleteEventDialog(new EventArgs());

					});
			}
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
					B_add_modified = true;
				}
			}
			catch(Exception){
			}
		}

		public ICommand EditEvent
		{
			get
			{
				return new MvxCommand(() =>
					{
					
						editingTLM.EquipmentID = _dataService.GetAssetBoxDescription();
						switch (SelectedDriverStatus.driverStatusType) {
						case 101:
							editingTLM.Logbookstopid = AuditLogic.OffDuty;
							editingTLM.Event = (int)LOGSTATUS.OffDuty;
							editingTLM.LogStatus = (int)LOGSTATUS.OffDuty;
							break;
						case 102:
							editingTLM.Logbookstopid = AuditLogic.Sleeping;
							editingTLM.Event = (int)LOGSTATUS.Sleeping;
							editingTLM.LogStatus = (int)LOGSTATUS.Sleeping;
							break;
						case 103:
							editingTLM.Logbookstopid = AuditLogic.Driving;
							editingTLM.Event = (int)LOGSTATUS.Driving;
							editingTLM.LogStatus = (int)LOGSTATUS.Driving;
							break;
						case 104:
							editingTLM.Logbookstopid = AuditLogic.OnDuty;
							editingTLM.Event = (int)LOGSTATUS.OnDuty;
							editingTLM.LogStatus = (int)LOGSTATUS.OnDuty;
							break;
						default:
							break;
						}
						if(eventIndex != (SelectedDriverStatus.driverStatusType-101)) {
							B_add_modified = true;
						}

						if(TxtComment.Length > 0){
							//When timelog is changed, we need to create a new record(the new record’s editor filed is still 1), 
							//also keep the original record and change the TimeLogType to Modified. The original record also needs to be sent to server
							if(originalTLM != null){ //If originalTLR is not null that means we have edited this record before so a TimeLogType.Modified record is created already
								originalTLM.Type = (int)TimeLogType.Modified;
								originalTLM.HaveSent = false;
								originalTLM.Signed = false;
								_timeLogService.InsertOrUpdate(originalTLM);

								editingTLM.Id = -1; //We only create a new record with Editor=1 if TimeLogType.Modified record is not created yet otherwise just update the Editor=1 record 
							}

							//We have edited this timelog and we want to resend it to server as a new timelog record 
							editingTLM.HaveSent = false;
							editingTLM.Signed = false;
							if(B_add_modified){
								editingTLM.Editor = 1;
								editingTLM.Comment = "[modified]" + TxtComment;
							}else{
								editingTLM.Comment = TxtComment;
							}
							editingTLM.Odometer = OdomText;

							//GeoCode the address to its lat/lng before saving if the address has been changed
							if(!UserAddressString.Equals(editingTLM.Address) || editingTLM.Latitude == 0 || editingTLM.Longitude == 0)
							{
								editingTLM.Address = UserAddressString;
								reverseGeoCode(UserAddressString);

							}

								_timeLogService.InsertOrUpdate(editingTLM);

								//If this the last event of the day also update the next Auto type event at Mid-night
								if(editingTLM.OrigLogTime == allTimeLogData.ToArray()[allTimeLogData.Count-1].OrigLogTime) {
									TimeLogModel tmpTL = _timeLogService.GetByDrvIdLogTime(_dataService.GetCurrentDriverId (), PlottingDate.AddDays(1).Date);
									if(tmpTL != null && tmpTL.Type == (int)TimeLogType.Auto){
										tmpTL.Event = editingTLM.Event;
										tmpTL.Signed = false;
										tmpTL.HaveSent = false;
										_timeLogService.InsertOrUpdate(tmpTL);
									}
								}

							List<TimeLogModel> lsttlm = new List<TimeLogModel> ();
							lsttlm.Add (editingTLM);
							sendTimeLog(lsttlm);

						} else {
							//show comment required dialog
							OnNeedCommentDialog(new EventArgs());
						}

					});
			}
		}

		public async void reverseGeoCode(string locAddress)
		{
			try{
				address = await _geocoder.GetAddressesAsync(locAddress);
				if(address != null && address.Count > 0){
					foreach(Address ad in address){
						editingTLM.Latitude = ad.Latitude;
						editingTLM.Longitude = ad.Longitude;
					}
				}
			}catch(Exception){
			}

		}

		public async void sendTimeLog(List<TimeLogModel> lsttimelog) {
			await _communicationService.SendTimeLogData(lsttimelog);
			_messenger.Publish<RefreshMessage> (new RefreshMessage (this, PlottingDate));
		}


	}


}
