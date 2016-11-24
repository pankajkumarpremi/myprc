using System;
using System.Windows.Input;
using MvvmCross.Core.ViewModels;
using BSM.Core.Services;
using MvvmCross.Plugins.Messenger;
using BSM.Core.Messages;
using BSM.Core.ConnectionLibrary;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;
using BSM.Core.AuditEngine;

using Acr.MvvmCross.Plugins.Network;
using MvvmCross.Platform;

namespace BSM.Core.ViewModels
{
	public class CoWorkerLoginViewModel : MvxViewModel
	{
		private readonly IMvxMessenger _messenger;
		private readonly IDataService _dataService;
		private readonly ICommunicationService _communication;
		private readonly IEmployeeService _employee;
		private readonly ICoWorkerService _coworker;
		private readonly ITimeLogService _timelog;
		private readonly IHourCalculatorService _hourCalcService;
		private readonly IBSMBoxWifiService _bsmBoxWiFiService;
		private readonly ILanguageService _languageService;
		private readonly MvxSubscriptionToken _networkStatusChanged;

		public  CoWorkerLoginViewModel (IDataService dataService,ICommunicationService communication,IMvxMessenger messenger,IEmployeeService employee,ICoWorkerService coworker,ITimeLogService timelog,IHourCalculatorService hourCalcService,IBSMBoxWifiService bsmBoxWifiService,ILanguageService languageService)
		{
			_hourCalcService = hourCalcService;
			_messenger = messenger;
			_dataService = dataService;
			_communication = communication;
			_employee = employee;
			_coworker = coworker;
			_timelog = timelog;
			_bsmBoxWiFiService = bsmBoxWifiService;
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

			list = _coworker.CoWorkerList();
			foreach(var user in list){
				CoWorkers.Add (user.UserName);
			}
		}

		private bool _isOffline = false;
		public bool IsOffline
		{
			get { return _isOffline; }
			set { _isOffline = value; RaisePropertyChanged(() => IsOffline); }
		}

		private string _userName = string.Empty;
		public string UserName
		{ 
			get { return _userName; }
			set { 
				_userName = value; 
				RaisePropertyChanged(() => UserName);
				searchNow (_userName);
			}
		}
		private string _password = string.Empty;
		public string Password
		{ 
			get { return _password; }
			set { _password = value; RaisePropertyChanged(() => Password); }
		}
		private bool _isBusy;
		public bool IsBusy
		{
			get { return _isBusy; }
			set { _isBusy = value; RaisePropertyChanged(() => IsBusy); }
		}
		public List<CoWorkerModel> list{ get; set;}
		private ObservableCollection<string> coWorkers = new ObservableCollection<string>();
		public ObservableCollection<string> CoWorkers
		{
			get{return coWorkers; }
			set{coWorkers = value;RaisePropertyChanged (()=>CoWorkers); }
		}
		private string _selectedCoworker;
		public string SelectedCoworker
		{
			get{return _selectedCoworker; }
			set{
				_selectedCoworker = value;
				UserName = _selectedCoworker;
				RaisePropertyChanged (()=>SelectedCoworker);
			}
		}

		private bool hideCoworkers;
		public bool HideCoworkers
		{
			get{return hideCoworkers; }
			set{
				hideCoworkers = value;
				RaisePropertyChanged (()=>HideCoworkers);
			}
		}

		public ICommand CancelLogin
		{
			get {
				return new MvxCommand(() =>{
					OnCloseView(new EventArgs());
					Close(this);
				});
			}
		}

		public ICommand SelectCoworker
		{
			get {
				return new MvxCommand<string>((selecteditem)=>{
					SelectedCoworker = selecteditem;
					Invert();
				});
			}
		}

		public ICommand CoWorkerLoginContinue
		{
			get {
				return new MvxCommand(() =>DoLogin());
			}
		}
		void Invert(){
			HideCoworkers = !HideCoworkers;
		}


		public async void DoLogin()
		{
			var currentDriver = _employee.EmployeeDetailsById(_dataService.GetCurrentDriverId());
			if (UserName.Trim ().Length == 0) {
				OnLoginError(new ErrorMessageEventArgs(){Message = _languageService.GetLocalisedString (Constants.str_username)});
			} else if (Password.Trim ().Length == 0) {
				OnLoginError(new ErrorMessageEventArgs(){Message = _languageService.GetLocalisedString (Constants.str_password)});
			} else if (currentDriver.Username == UserName.Trim () && currentDriver.Password == Password) {
				OnLoginError(new ErrorMessageEventArgs(){Message = _languageService.GetLocalisedString (Constants.str_driver)});
			} else {
				IsBusy = true;
				_dataService.PersistIsCoWorkerLogin (true);
				var response  = await _communication.syncUser (UserName.Trim(),Password.Trim(),_dataService.GetDomain());
				if (response) {
				// Login Success
					LoginSuccess();
				} else {
				// Login Falied
					LoginFalied();
				}
			}
		}

		public void searchNow(string searchTerm)
		{
			if (!string.IsNullOrEmpty(searchTerm) && list != null && list.Count > 0) {
				if (CoWorkers.Contains (searchTerm)) {					
					return;
				}
				CoWorkers = new ObservableCollection<string> (list.Where (p => p.UserName != null && p.UserName.ToLower ().Contains (searchTerm.ToLower ())).Select (a => a.UserName).ToList ());
				if (CoWorkers.Count > 0) {
					HideCoworkers = true;	
				} else {
					HideCoworkers = false;
				}
			} else {
				HideCoworkers = false;
			}
		}

		#region Events
		public event EventHandler LoginError;
		protected virtual void OnLoginError(ErrorMessageEventArgs e)
		{
			if (LoginError != null){
				LoginError(this, e);
			}
		}

		public event EventHandler CloseView;
		protected virtual void OnCloseView(EventArgs e)
		{
			unSubscribe ();
			if (CloseView != null)
			{
				CloseView(this, e);
			}
		}
		#endregion

		#region unsubscribe
		public void unSubscribe() {
			_messenger.Unsubscribe<NetworkStatusChangedMessage> (_networkStatusChanged);
		}
		#endregion

		public void SaveTimeLog(){
			var tlr = new TimeLogModel ();
			_bsmBoxWiFiService.LocalizeTimeLog(ref tlr);
			tlr.Signed = true;
			tlr.DriverId = _dataService.GetCoDriver ();
			tlr.CoDriver = string.Empty;
			tlr.EquipmentID = _dataService.GetAssetBoxId ().ToString();
			tlr.Logbookstopid = AuditLogic.OnDuty;
			tlr.LogStatus =(int)LOGSTATUS.OnDuty;
			tlr.Event = (int) LOGSTATUS.OnDuty;
			_timelog.Insert (tlr);
			//Start/Restart the HourCalculatorTimer to send the timeLog
			_hourCalcService.runHourCalculatorTimerNow ();
			OnCloseView(new EventArgs());
			Close(this);
		}

		public void LoginSuccess(){
			var empModel = _employee.EmployeeDetailsById(_dataService.GetCoDriver());
			empModel.Username = UserName;
			empModel.Password = Password;
			empModel.Domain = _dataService.GetDomain();
			_employee.UpdateDomain(empModel);
			var employeeModel = _employee.getEmployeeByUserAndPassword(UserName,Password);
			var coworkerModel = new CoWorkerModel{DriverName = employeeModel.DriverName,DriverID = employeeModel.Id,EmployeeID = employeeModel.Id,LoggedIn = true,LoginTime = Util.GetDateTimeNow(),UserName=UserName};
			_coworker.Insert(coworkerModel);
			_dataService.ClearIsCoWorkerLogin();
			IsBusy = false;
			SaveTimeLog();
		}

		public void LoginFalied(){
			_dataService.PersistIsCoWorkerLogin (false);
			IsBusy = false;
			var employeeModel = _employee.getEmployeeByUserAndPassword(UserName,Password);
			if(employeeModel == null){
				OnLoginError(new ErrorMessageEventArgs(){Message = _languageService.GetLocalisedString (Constants.str_login)});
			}else{						
				var coworkerModel = new CoWorkerModel{DriverName = employeeModel.DriverName,DriverID = employeeModel.Id,EmployeeID = employeeModel.Id,LoggedIn = true,LoginTime = Util.GetDateTimeNow(),UserName=UserName};
				_coworker.Insert(coworkerModel);
				SaveTimeLog();
			}
		}
	}
	public class ErrorMessageEventArgs : EventArgs
	{
		public string Message { get; set; }
	}
}

