using System;
using MvvmCross.Core.ViewModels;
using MvvmCross.Plugins.Messenger;
using BSM.Core.Services;
using BSM.Core.AuditEngine;
using System.Collections.Generic;
using System.Linq;
using BSM.Core.ConnectionLibrary;
using MvxPlugins.Geocoder;

using System.Collections.ObjectModel;
using BSM.Core.Messages;
using MvvmCross.Platform;
using Acr.MvvmCross.Plugins.Network;
using MvvmCross.Platform.Platform;

namespace BSM.Core.ViewModels
{
	public class MyProfileViewModel: MvxViewModel
	{
		#region Member Variables
		private readonly IMvxMessenger _messenger;
		private readonly IDataService _dataservice;
		private readonly IEmployeeService _employeeService;
		private readonly ICommunicationService _commnication;
		private readonly IRuleSelectionHistoryService _ruleSelection;
		private readonly IGeocoder _geocoder;
		IList<Address> address;
		private readonly ISyncService _syncService;
		private readonly MvxSubscriptionToken _boxConnectivityMessage;
		private readonly MvxSubscriptionToken _networkStatusChanged;
		#endregion

		#region ctors
		public MyProfileViewModel (IMvxMessenger messenger, IDataService dataservice, IEmployeeService employeeService,ICommunicationService communication,IRuleSelectionHistoryService ruleSelection,ISyncService syncService)
		{
			_syncService = syncService;
			_commnication = communication;
			_messenger = messenger;
			_dataservice = dataservice;
			_employeeService = employeeService;
			_ruleSelection = ruleSelection;
			var currentDriverId = _dataservice.GetCurrentDriverId ();
			EmployeeData = _employeeService.EmployeeDetailsById (currentDriverId);
			OldPassword = EmployeeData.Password;
			PassWord = EmployeeData.Password;
			GetCycleList ();
			if (string.IsNullOrEmpty (EmployeeData.TimeZone)) {
				EmployeeData.TimeZone = TimeZoneInfo.Local != null ? TimeZoneInfo.Local.BaseUtcOffset.TotalHours.ToString () : "-5.0";
				EmployeeData.DayLightSaving= DateTime.Now.IsDaylightSavingTime();
			}
			TimeZoneList = Constants.TimeZoneNames.ToList ();
			foreach(var timezone in TimeZoneList){
				if (timezone.IndexOf (EmployeeData.TimeZone) > -1) {
					SelectedTimeZone = timezone;
					break;
				}	
			}
			_userAddressString = EmployeeData.HomeAddress;
			_geocoder = Mvx.Resolve<IGeocoder> ();
			_boxConnectivityMessage = _messenger.SubscribeOnThreadPoolThread<BoxConnectivityMessage>((message) =>
				{
					if (_dataservice.GetBSMBoxStatus() != (int)message.BoxStatus) {					
						_dataservice.SetBSMBoxStatus(message.BoxStatus);
						BsmBoxWifiStatus = _dataservice.GetBSMBoxStatus();
					}
				});
			_networkStatusChanged = _messenger.Subscribe<NetworkStatusChangedMessage> ((message) => {
				if (message.Status.IsConnected) {
					IsOffline = false;
				}
				else {
					IsOffline = true;
				}
			});
			BsmBoxWifiStatus = _dataservice.GetBSMBoxStatus ();
		}
		#endregion

		#region Properties
		private bool _isOffline = false;
		public bool IsOffline
		{
			get { return _isOffline; }
			set { _isOffline = value; RaisePropertyChanged(() => IsOffline); }
		}

		private int _bsmBoxWifiStatus;
		public int BsmBoxWifiStatus
		{
			get{
				return _bsmBoxWifiStatus; 
			}
			set{
				_bsmBoxWifiStatus = value;
				RaisePropertyChanged (()=> BsmBoxWifiStatus); 
			}
		}

		private bool _isBusy;
		public bool IsBusy
		{
			get { return _isBusy; }
			set { _isBusy = value; RaisePropertyChanged(() => IsBusy); }
		}
		private EmployeeModel _employeeData;
		public EmployeeModel EmployeeData
		{ 
			get { return _employeeData; }
			set { _employeeData = value; RaisePropertyChanged(() => EmployeeData); }
		}

		private bool _changePassword;
		public bool ChangePassword
		{
			get{return _changePassword; }
			set{
				_changePassword = value;
				RaisePropertyChanged (()=>ChangePassword);
				if(!ChangePassword){
					PassWord = OldPassword;
					ConfirmPassword = string.Empty;
					NewPassword = string.Empty;
				}
			}
		}


		public string OldPassword {
			get;
			set;
		}


		private string password = string.Empty;
		public string PassWord{
			get{return password; }
			set{ password = value;
				RaisePropertyChanged (()=>PassWord);}
		}

		private string _newPassword = string.Empty;
		public string NewPassword
		{
			get{return _newPassword; }
			set{_newPassword = value;RaisePropertyChanged (()=>NewPassword); }
		}

		private string _confirmPassword = string.Empty;
		public string ConfirmPassword
		{
			get{return _confirmPassword; }
			set{_confirmPassword = value;RaisePropertyChanged (()=>ConfirmPassword); }
		}

		private bool changeSignature;
		public bool ChangeSignature
		{
			get{return changeSignature; }
			set{changeSignature = value;RaisePropertyChanged (()=>ChangeSignature); }
		}
		private HosCycleModel selectedCycle;
		public HosCycleModel SelectedCycle
		{
			get{return selectedCycle; }
			set{
				selectedCycle = value;
				RaisePropertyChanged (()=>SelectedCycle);
				HideCycle = !HideCycle;
				EmployeeData.Cycle = selectedCycle.Id;
				EmployeeData.HaveSent = false;
				_employeeService.UpdateById (EmployeeData);
				var ruleModel = new RuleSelectionHistoryModel();
				if(EmployeeData.Cycle != selectedCycle.Id || EmployeeData.HosExceptions != -1){					
					ruleModel.ruleid = selectedCycle.Id;
					ruleModel.country = (selectedCycle.CycleDescription.ToString().ToLower().StartsWith("us") ? "US" : "CA");
					ruleModel.selectTime = Util.GetDateTimeUtcNow ();
					ruleModel.HosExceptions = EmployeeData.HosExceptions;
					ruleModel.DriverId = EmployeeData.Id;
					_ruleSelection.Insert (ruleModel);
				}
			}
		}
		public string NewSignature {
			get;
			set;
		}
		public List<string> TimeZoneList {
			get;
			set;
		}

		private string selectedTimeZone;
		public string SelectedTimeZone
		{
			get{return selectedTimeZone; }
			set{
				selectedTimeZone = value;
				RaisePropertyChanged (()=>SelectedTimeZone);
				HideZone = !HideZone;
			}
		}

		private bool hideZone;
		public bool HideZone
		{
			get{return hideZone; }
			set{hideZone = value;RaisePropertyChanged (()=>HideZone); }
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

		private ObservableCollection<string> _userAddress = new ObservableCollection<string>();
		public ObservableCollection<string> UserAddress 
		{
			get{ return _userAddress; }
			set{ _userAddress = value; RaisePropertyChanged (()=>UserAddress); }
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

		private bool _hideLocation;
		public bool HideLocation
		{
			get { return _hideLocation; }
			set { _hideLocation = value; RaisePropertyChanged(() => HideLocation); }
		}

		private List<HosCycleModel> cycleList;
		public List<HosCycleModel> CycleList
		{
			get{return cycleList; }
			set{cycleList = value;RaisePropertyChanged (()=>CycleList); }
		}

		private bool hideCycle;
		public bool HideCycle
		{
			get{return hideCycle; }
			set{hideCycle = value;RaisePropertyChanged (()=>HideCycle); }
		}

		#endregion

		public IMvxCommand SelectLocationCommand {
			get {
				return new MvxCommand<string>((Location) => {
					SelectedLocation = Location;
					HideLocation = true;
				});
			}
		}

		public IMvxCommand ShowCycle
		{
			get{return new MvxCommand (()=> HideCycle = !HideCycle); }
		}

		public IMvxCommand ShowTimeZone
		{
			get{return new MvxCommand (()=> HideZone = !HideZone); }
		}

		public IMvxCommand ShowPasswordFields
		{
			get{return new MvxCommand (()=>ChangePassword = !ChangePassword); }
		}

		public IMvxCommand ShowSignaturePad
		{
			get{return new MvxCommand (()=>ChangeSignature = !ChangeSignature); }
		}

		public IMvxCommand SaveEmployee
		{
			get{return new MvxCommand (()=>Save()); }
		}
		public void Save()
		{
			EmployeeData.HaveSent = false;
			ValidateFormAndSave ();
		}


		public void ValidateFormAndSave()
		{
			var errorMessage = string.Empty;
			if(EmployeeData.DriverName.Trim().Length == 0){
				errorMessage = "Please Add DriverName";
			}else if(EmployeeData.License == null || EmployeeData.License.Trim().Length == 0){
				errorMessage = "Please Add DriverLicence";
			}else if(UserAddressString == null || UserAddressString.Length == 0){
				errorMessage = "Please Add Address";
			}else if(EmployeeData.TimeZone == null || EmployeeData.TimeZone.Trim().Length == 0){
				errorMessage = "Please Add TimeZone";
			}else if(PassWord.Trim().Length == 0){
				errorMessage = "Password mismatch. Update new password or cancel password change";
			}else if(ChangePassword && (NewPassword.Trim().Length == 0 || ConfirmPassword.Trim().Length == 0 || NewPassword.Trim() == OldPassword.Trim() || NewPassword.Trim() != ConfirmPassword.Trim())){				
					errorMessage = "Password mismatch. Update new password or cancel password change";
			}else if(ChangeSignature){
				OnGetSignature (new EventArgs());
				if(NewSignature == null || NewSignature.Trim().Length == 0){
					errorMessage = "Please Add Signature";
				}
			}else if(EmployeeData.Signature == null || EmployeeData.Signature.Trim().Length == 0){
				errorMessage = "Please Add Signature";
			}
			if (errorMessage.Length == 0) {
				if (ChangeSignature) {
					OnGetSignature (new EventArgs());
					EmployeeData.Signature = NewSignature;
				}
				if(ChangePassword){
					EmployeeData.Password = NewPassword;
				}
				EmployeeData.HomeAddress = UserAddressString;
				EmployeeData.TimeZone = SelectedTimeZone.Substring(3, SelectedTimeZone.IndexOf(" ")-3);
				EmployeeData.Cycle = SelectedCycle.Id;
				EmployeeData.HaveSent = false;
				_employeeService.UpdateById (EmployeeData);
				_syncService.runTimerCallBackNow ();
				OnSuccessMessage (new ErrorMessageEventArgs(){Message = errorMessage});
			} else {				
				OnSaveError (new ErrorMessageEventArgs(){Message = errorMessage});
			}
		}

		public bool ComparePasswords(string newpass,string confirm){
			bool passValidation = false;
			if(!string.IsNullOrEmpty(EmployeeData.Password) && OldPassword == EmployeeData.Password ){
				if (!string.IsNullOrEmpty (newpass) && !string.IsNullOrEmpty (confirm)) {
					if(newpass.Trim() == confirm.Trim()){
						passValidation = true;
					}	
				}
			}
			return passValidation;
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
			catch(Exception Ex){
				Mvx.Trace (MvxTraceLevel.Error,"Error When Retriving Address" +Ex.ToString());
			}
		}

		public event EventHandler SaveError;
		protected virtual void OnSaveError(EventArgs e)
		{
			if (SaveError != null)
			{				
				SaveError(this, e);
			}
		}

		public event EventHandler Success;
		protected virtual void OnSuccessMessage(EventArgs e)
		{
			if (Success != null)
			{				
				Success(this, e);
			}
		}

		public event EventHandler GetSignature;
		protected virtual void OnGetSignature(EventArgs e)
		{
			if (GetSignature != null)
			{				
				GetSignature(this, e);
			}
		}

		public void CloseProfile(){
			unSubscribe ();
			Close (this);
		}

		public void unSubscribe() {
			_messenger.Unsubscribe<NetworkStatusChangedMessage> (_networkStatusChanged);
			_messenger.Unsubscribe<BoxConnectivityMessage> (_boxConnectivityMessage);
		}

		void GetCycleList(){
			CycleList = new List<HosCycleModel> ();
			var hosCycleNames = Enum.GetNames(typeof(HOSCYCLE));
			var hosCycleValues = Enum.GetValues(typeof(HOSCYCLE));
			for(int i=0; i<hosCycleNames.Length; i++) {
				var hoscycle = new HosCycleModel ();
				hoscycle.Id = (int)hosCycleValues.GetValue (i);
				hoscycle.CycleDescription = hosCycleNames [i];
				CycleList.Add (hoscycle);
			}
			SelectedCycle = CycleList.FirstOrDefault (p=>p.Id == EmployeeData.Cycle);
		}


		public IMvxCommand ShowMessages
		{

			get{return new MvxCommand (()=>ShowViewModel<MessagesViewModel>()); }	
		}
	}
}

