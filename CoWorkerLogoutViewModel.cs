using System;
using System.Windows.Input;
using MvvmCross.Core.ViewModels;
using BSM.Core.Services;
using MvvmCross.Plugins.Messenger;
using BSM.Core.Messages;
using BSM.Core.ConnectionLibrary;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BSM.Core.ViewModels;
using BSM.Core.AuditEngine;

using Acr.MvvmCross.Plugins.Network;
using MvvmCross.Platform;

namespace BSM.Core.ViewModels
{
	public class CoWorkerLogoutViewModel : MvxViewModel
	{
		private readonly IMvxMessenger _messenger;
		private readonly IDataService _dataService;
		private readonly ICommunicationService _communication;
		private readonly IEmployeeService _employee;
		private readonly ICoWorkerService _coworker;
		private readonly ITimeLogService _timelog;
		private readonly IHourCalculatorService _hourCalcService;
		private readonly IBSMBoxWifiService _bsmBoxWifiService;
		private readonly ILanguageService _languageService;
		private readonly MvxSubscriptionToken _networkStatusChanged;

		public CoWorkerLogoutViewModel (IDataService dataService,ICommunicationService communication,IMvxMessenger messenger,IEmployeeService employee,ICoWorkerService coworker,ITimeLogService timelog,IHourCalculatorService hourCalcService,IBSMBoxWifiService bsmBoxWifiService, ILanguageService languageService)
		{
			_hourCalcService = hourCalcService;
			_messenger = messenger;
			_dataService = dataService;
			_communication = communication;
			_employee = employee;
			_coworker = coworker;
			_timelog = timelog;
			_bsmBoxWifiService = bsmBoxWifiService;
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

			list = _coworker.CoWorkerList().Where(p=>p.LoggedIn == true).ToList();
			SelectedCow = new CoWorkerModel ();
		}

		private bool _isOffline = false;
		public bool IsOffline
		{
			get { return _isOffline; }
			set { _isOffline = value; RaisePropertyChanged(() => IsOffline); }
		}

		private bool _isBusy;
		public bool IsBusy
		{
			get { return _isBusy; }
			set { _isBusy = value; RaisePropertyChanged(() => IsBusy); }
		}

		private List<CoWorkerModel> _list;
		public List<CoWorkerModel> list
		{ 
			get { return _list; }
			set { _list = value; RaisePropertyChanged (()=>list); }
		}


		public CoWorkerModel SelectedCow { 
			get;
			set;
		}

		private bool isEnabled;
		public bool IsEnabled
		{
			get{return isEnabled; }
			set{
				isEnabled = value;
				RaisePropertyChanged (()=> IsEnabled);
			}
		}

		private string textPassword = string.Empty;
		public string TextPassword
		{
			get{return textPassword; }
			set{
				textPassword = value;
				RaisePropertyChanged (()=> TextPassword);
			}
		}

		public ICommand SelectCheckbox
		{
			get {
				return new MvxCommand<CoWorkerModel> ((selectedCoworker) => {
					if(selectedCoworker.IsSelected){
						var tempList = list;
						foreach(var coworker in tempList){
							coworker.IsSelected = false;
						}
						SelectedCow = new CoWorkerModel();
						list = new List<CoWorkerModel>(tempList);
					}else{
						var tempList1 = list;
						foreach(var item in tempList1){
							if(selectedCoworker == item){
								item.IsSelected = true;
								SelectedCow = selectedCoworker;
							}else{
								item.IsSelected = false;
							}
						}
						list = new List<CoWorkerModel>(tempList1);
					}

					if(SelectedCow != null && !string.IsNullOrEmpty(SelectedCow.UserName)){
						IsEnabled = true;
					}else{
						IsEnabled = false;
						TextPassword = string.Empty;
					}
				});

			}
		}

		public ICommand CancelLogout
		{
			get {
				return new MvxCommand(() =>{
					OnCloseView(new EventArgs());
					Close(this);
				});
			}
		}

		public ICommand CoWorkerLogoutContinue
		{
			get {
				return new MvxCommand(() =>ValidateBefore());
			}
		}
	

		public void ValidateBefore(){
			if (list != null && list.Count > 0) {
				var currentDriver = _employee.EmployeeDetailsById (_dataService.GetCurrentDriverId());
				var errorMessage = string.Empty;
				if (SelectedCow != null && string.IsNullOrEmpty(SelectedCow.UserName)) {
					errorMessage = "Please Select CoWorker To Logout";
				} else {			
					if (TextPassword.Trim ().Length == 0) {
						errorMessage = _languageService.GetLocalisedString (Constants.str_password);
					} else {
						if (currentDriver.Username == SelectedCow.UserName) {
							errorMessage = _languageService.GetLocalisedString (Constants.str_driver);
						}
					}
				}
				if(errorMessage.Length > 0){
					OnLoginError(new ErrorMessageEventArgs(){Message = errorMessage});
					return;
				}
				CheckUserExistAndLogout (SelectedCow);
			} else {
				OnLoginError(new ErrorMessageEventArgs(){Message = "No CoWorker Available"});
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
		public void SaveTimeLog(){
			var tlr = new TimeLogModel ();
			_bsmBoxWifiService.LocalizeTimeLog (ref tlr);
			tlr.Signed = true;
			tlr.DriverId = _dataService.GetCoDriver ();
			tlr.CoDriver = string.Empty;
			tlr.EquipmentID = _dataService.GetAssetBoxId ().ToString();
			tlr.Logbookstopid = AuditLogic.OffDuty;
			tlr.LogStatus =(int) LOGSTATUS.OffDuty;
			tlr.Event =(int) LOGSTATUS.OffDuty;
			_timelog.SetSignSentForDate(true,false,tlr.DriverId,Util.GetDateTimeUtcNow());
			_timelog.Insert (tlr);
			_hourCalcService.runHourCalculatorTimerNow ();
			OnCloseView(new EventArgs());
			Close(this);
		}
		public async void CheckUserExistAndLogout(CoWorkerModel selectedmodel)
		{
			if(selectedmodel != null){
				IsBusy = true;
				_dataService.PersistIsCoWorkerLogin (true);
				var response = await _communication.syncUser (selectedmodel.UserName, TextPassword, _dataService.GetDomain ());
				if (response) {
					LogoutSuccess ();
				} else {
					LogoutFailed ();
				}
			}
		}

		public void LogoutSuccess(){
			_dataService.PersistIsCoWorkerLogin (false);
			_dataService.ClearPersistedCoDriver();
			var employeeModel = _employee.getEmployeeByUserAndPassword(SelectedCow.UserName,TextPassword);
			IsBusy = false;
			var coworkerModel = new CoWorkerModel{DriverName = employeeModel.DriverName,DriverID = employeeModel.Id,EmployeeID = employeeModel.Id,LoggedIn = false,LoginTime = Util.GetDateTimeNow(),UserName = SelectedCow.UserName};
			_coworker.Insert(coworkerModel);
			SaveTimeLog();
		}

		public void LogoutFailed(){
			_dataService.PersistIsCoWorkerLogin (false);
			IsBusy = false;
			var employeeModel = _employee.getEmployeeByUserAndPassword(SelectedCow.UserName,TextPassword);
			if(employeeModel == null){
				OnLoginError(new ErrorMessageEventArgs(){Message = _languageService.GetLocalisedString (Constants.str_logout_failed)});
			}else{
				_dataService.ClearPersistedCoDriver();
				var coworkerModel = new CoWorkerModel{DriverName = employeeModel.DriverName,DriverID = employeeModel.Id,EmployeeID = employeeModel.Id,LoggedIn = false,LoginTime = Util.GetDateTimeNow(),UserName = SelectedCow.UserName};
				_coworker.Insert(coworkerModel);
				SaveTimeLog();
			}
		}

		#region unsubscribe
		public void unSubscribe() {
			_messenger.Unsubscribe<NetworkStatusChangedMessage> (_networkStatusChanged);
		}
		#endregion
	}
}

