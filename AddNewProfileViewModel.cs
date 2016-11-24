using System;
using MvvmCross.Core.ViewModels;
using System.Windows.Input;
using Sockets.Plugin;
using System.Threading.Tasks;
using BSM.Core.Services;
using MvvmCross.Plugins.Messenger;
using BSM.Core.Messages;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BSM.Core.AuditEngine;
using System.Linq;
using BSM.Core.ConnectionLibrary;

using Acr.MvvmCross.Plugins.Network;
using MvvmCross.Platform;

namespace BSM.Core.ViewModels
{
	public class AddNewProfileViewModel: MvxViewModel
	{
		#region Member Variables
		private readonly IMvxMessenger _messenger;
		private readonly ILoginService _loginservice;
		private readonly IEmployeeService _empService;
		private readonly IDataService _dataservice;
		private readonly ICommunicationService _communication;
		private readonly IRuleSelectionHistoryService _ruleselection;
		private readonly ISyncService _syncService;
		private readonly ILanguageService _languageService;
		private readonly MvxSubscriptionToken _networkStatusChanged;
		#endregion

		#region ctors
		public AddNewProfileViewModel (IMvxMessenger messenger, ILoginService loginservice, IDataService dataservice, IEmployeeService empService,ICommunicationService communication,IRuleSelectionHistoryService ruleselection,ISyncService syncService, ILanguageService languageService)
		{
			_syncService = syncService;
			_communication = communication;
			_messenger = messenger;
			_loginservice = loginservice;
			_empService = empService;
			_dataservice = dataservice;
			_ruleselection = ruleselection;
			_languageService = languageService;
			_languageService.LoadLanguage (Mvx.Resolve<IDeviceLocaleService> ().GetLocale());
			_networkStatusChanged = _messenger.Subscribe<NetworkStatusChangedMessage> ((message) => {
				if (message.Status.IsConnected) {
					IsOffline = false;
				}
				else {
					IsOffline = true;
				}
			});

			GoBack();
			LoadData ();
		}
		#endregion

		#region Properties
		private bool _isOffline = false;
		public bool IsOffline
		{
			get { return _isOffline; }
			set { _isOffline = value; RaisePropertyChanged(() => IsOffline); }
		}

		private string _fullName;
		public string FullName
		{ 
			get { return _fullName; }
			set { _fullName = value; RaisePropertyChanged(() => FullName); }
		}

		private string _email;
		public string Email
		{ 
			get { return _email; }
			set { _email = value; RaisePropertyChanged(() => Email); }
		}

		private string _phoneNumber;
		public string PhoneNumber
		{ 
			get { return _phoneNumber; }
			set { _phoneNumber = value; RaisePropertyChanged(() => PhoneNumber); }
		}

		private string _driversLicenceNumber;
		public string DriversLicenceNumber
		{ 
			get { return _driversLicenceNumber; }
			set { _driversLicenceNumber = value; RaisePropertyChanged(() => DriversLicenceNumber); }
		}

		private string _address;
		public string Address
		{ 
			get { return _address; }
			set { _address = value; RaisePropertyChanged(() => Address); }
		}

		private string _homeTerminal;
		public string HomeTerminal
		{ 
			get { return _homeTerminal; }
			set { _homeTerminal = value; RaisePropertyChanged(() => HomeTerminal); }
		}

		private string _timeZone;
		public string TimeZone
		{ 
			get { return _timeZone; }
			set { _timeZone = value; RaisePropertyChanged(() => TimeZone); }
		}

		private List<string> _timeZoneList;
		public List<string> TimeZoneList
		{
			get { return _timeZoneList; }
			set { _timeZoneList = value; RaisePropertyChanged(() => TimeZoneList); }
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

		private string _signature;
		public string Signature
		{
			get { return _signature; }
			set { _signature = value; RaisePropertyChanged(() => Signature); }
		}
		private bool _isScreen1Hidden;
		public bool IsScreen1Hidden
		{
			get { return _isScreen1Hidden; }
			set { _isScreen1Hidden = value; RaisePropertyChanged(() => IsScreen1Hidden); }
		}

		private bool _isScreen2Hidden;
		public bool IsScreen2Hidden
		{
			get { return _isScreen2Hidden; }
			set { _isScreen2Hidden = value; RaisePropertyChanged(() => IsScreen2Hidden); }
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
				if(EmployeeData != null){
					EmployeeData.Cycle = selectedCycle.Id;
					EmployeeData.HaveSent = false;
					_empService.UpdateById (EmployeeData);
					var ruleModel = new RuleSelectionHistoryModel();
					if(EmployeeData.Cycle != selectedCycle.Id || EmployeeData.HosExceptions != -1){
						ruleModel.ruleid = selectedCycle.Id;
						ruleModel.country = (selectedCycle.CycleDescription.ToString().ToLower().StartsWith("us") ? "US" : "CA");
						ruleModel.selectTime = Util.GetDateTimeUtcNow ();
						ruleModel.HosExceptions = EmployeeData.HosExceptions;
						ruleModel.DriverId = EmployeeData.Id;
						_ruleselection.Insert (ruleModel);
					}
				}
			}
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

		private bool hideZone;
		public bool HideZone
		{
			get{return hideZone; }
			set{hideZone = value;RaisePropertyChanged (()=>HideZone); }
		}

		public EmployeeModel EmployeeData{ get; set;}
		#endregion

		#region Commands

		public IMvxCommand ShowTimeZone
		{
			get{return new MvxCommand (()=> HideZone = !HideZone); }
		}

        public ICommand Forward
		{
			get {
				return new MvxCommand(() => GoForward());
			}
		}

		public ICommand Back
		{
			get {
				return new MvxCommand(() => GoBack());
			}
		}

		public ICommand SaveProfileCommand
		{
			get {
				return new MvxCommand(() => SaveProfile());
			}
		}

		public IMvxCommand ShowSignaturePad
		{
			get{return new MvxCommand (()=>ChangeSignature = !ChangeSignature); }
		}

		public IMvxCommand ShowCycle
		{
			get{return new MvxCommand (()=> HideCycle = !HideCycle); }
		}

		public void GoForward() {
			var errorMessage = DoValidationScreen1 ();
			if(!string.IsNullOrEmpty(errorMessage)){
				OnSaveError (new ErrorMessageEventArgs(){ Message=errorMessage });
				return;
			}
			IsScreen1Hidden = true;
			IsScreen2Hidden = false;
			if (string.IsNullOrEmpty (_signature)) {				
				ChangeSignature = true;
			} else {
				ChangeSignature = false;
			}
		}
		public void GoBack() {
			IsScreen2Hidden = true;
			IsScreen1Hidden = false;
		}
		public void SaveProfile() {
			var errorMessage = DoValidationScreen2 ();
			if (!string.IsNullOrEmpty (errorMessage)) {
				OnSaveError (new ErrorMessageEventArgs(){ Message = errorMessage });
			} else {
				UpdateProfile ();
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
		public event EventHandler GetSignature;
		protected virtual void OnGetSignature(EventArgs e)
		{
			if (GetSignature != null)
			{				
				GetSignature(this, e);
			}
		}
		#endregion

		#region unsubscribe
		public void unSubscribe() {
			_messenger.Unsubscribe<NetworkStatusChangedMessage> (_networkStatusChanged);
		}
		#endregion
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
		void LoadData(){
			EmployeeData = _empService.EmployeeDetailsById (_dataservice.GetCurrentDriverId());
			if(EmployeeData != null){
				_fullName = EmployeeData.DriverName;
				_driversLicenceNumber = EmployeeData.License;
				_address = EmployeeData.HomeAddress;
				_homeTerminal = EmployeeData.OrgAddr;
				_signature = EmployeeData.Signature;
				TimeZoneList = Constants.TimeZoneNames.ToList ();
				if (string.IsNullOrEmpty (EmployeeData.TimeZone)) {
					EmployeeData.TimeZone = TimeZoneInfo.Local != null ? TimeZoneInfo.Local.BaseUtcOffset.TotalHours.ToString () : "-5.0";
				}
				foreach(var timezone in TimeZoneList){
					if (timezone.IndexOf (EmployeeData.TimeZone) > -1) {
						SelectedTimeZone = timezone;
						break;
					}	
				}
				GetCycleList ();
			}

		}

		public string DoValidationScreen1 ()
		{
			var error = string.Empty;
			if(Address == null || Address.Trim().Length == 0){
				error = _languageService.GetLocalisedString (Constants.str_add_address);
			}else if(FullName == null || FullName.Trim().Length == 0){
				error = _languageService.GetLocalisedString (Constants.str_add_name);
			}else if(DriversLicenceNumber== null || DriversLicenceNumber.Trim().Length == 0){
				error = _languageService.GetLocalisedString (Constants.str_add_licence);
			}
			return error;
		}

		public string DoValidationScreen2()
		{
			OnGetSignature (new EventArgs());
			var error = string.Empty;
			if(HomeTerminal == null || HomeTerminal.Trim().Length == 0){
				error = _languageService.GetLocalisedString (Constants.str_add_home_address);
			}else if(SelectedTimeZone == null || SelectedTimeZone.Trim().Length == 0){
				error = _languageService.GetLocalisedString (Constants.str_timezone);
			}else if(string.IsNullOrEmpty(SelectedCycle.CycleDescription)){
				error = "Please Select Cycle";
			}else if(Signature == null || Signature.Trim().Length == 0){
				error = _languageService.GetLocalisedString (Constants.str_signature);
			}
			return error;
		}

		public void UpdateProfile()
		{
			OnGetSignature (new EventArgs());
			if(EmployeeData != null){
				EmployeeData.Cycle = SelectedCycle.Id;
				EmployeeData.DriverName = FullName;
				EmployeeData.HaveSent = false;
				EmployeeData.Signature = Signature;
				EmployeeData.HomeAddress = Address;
				EmployeeData.OrgAddr = HomeTerminal;
				EmployeeData.License = DriversLicenceNumber;
				EmployeeData.TimeZone = SelectedTimeZone.Substring(3, SelectedTimeZone.IndexOf(" ")-3);;
				_empService.UpdateById (EmployeeData);

				_syncService.runTimerCallBack ();
			}
			ShowViewModel<SetupFinishedViewModel>();
			unSubscribe ();
			Close (this);
		}
	}
}

