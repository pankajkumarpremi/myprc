using System;
using MvvmCross.Core.ViewModels;
using System.Windows.Input;
using Sockets.Plugin;
using System.Threading.Tasks;
using BSM.Core.Services;
using MvvmCross.Plugins.Messenger;
using BSM.Core.Messages;
using BSM.Core.ConnectionLibrary;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using BSM.Core.AuditEngine;
using Acr.MvvmCross.Plugins.Network;
using MvvmCross.Platform;
using MvvmCross.Platform.Platform;

namespace BSM.Core.ViewModels
{
	public class LoginViewModel: MvxViewModel
	{
		#region Member Variables
		private readonly IMvxMessenger _messenger;
		private readonly ILoginService _loginservice;
		private readonly IDataService _dataservice;
		private readonly ILastSessionService _lastSession;
//		private MvxSubscriptionToken _loginSuccess;
//		private MvxSubscriptionToken _loginFailed;
		private readonly ILocationService _locationService;
		private readonly IEmployeeService _empService;
		private readonly ICoWorkerService _coworker;
		private readonly ICommunicationService _communicationService;
		private  IHourCalculatorService _hourCalcServive;
		private readonly ILanguageService _languageService;
		private readonly MvxSubscriptionToken _networkStatusChanged;
		private readonly IBSMBoxWifiService _bsmBoxWifiService;
		#endregion

		#region ctors
		public LoginViewModel (IMvxMessenger messenger, ILoginService loginservice, IDataService dataservice, IEmployeeService empService,ILocationService locationService,ICoWorkerService coworker,ICommunicationService communicationService,ILastSessionService lastsession, ILanguageService languageService,IBSMBoxWifiService bsmBoxWifiService)
		{
			_bsmBoxWifiService = bsmBoxWifiService;
			_lastSession = lastsession;
			_messenger = messenger;
			_loginservice = loginservice;
			_dataservice = dataservice;
			_locationService = locationService;
			_empService = empService;
			_coworker = coworker;
			_communicationService = communicationService;
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
			Users = _empService.GetAllRememberedEmployeeList ();
			_dataservice.ResetApplyWeightRule ();
			AutoLoginUsers = Users != null ? new ObservableCollection<string> (Users.Where(p=>p.Username != null && p.Username != string.Empty).Select(p=>p.Username)) : new ObservableCollection<string> ();
			_dataservice.ClearPersistedAssetBoxId ();
			_dataservice.ClearPersistedDriverId ();
			_dataservice.ResetBSMBoxStatus ();
			_dataservice.ClearPersistedAssetBoxDescription ();
		}

		public async void SyncTimelog()
		{
			var date = DateTime.Now;
			Mvx.Trace (MvxTraceLevel.Diagnostic,"Before Sync" + DateTime.Now);
			await _communicationService.SyncUser4TimeLog();
			Mvx.Trace (MvxTraceLevel.Diagnostic,"Before Sync" + DateTime.Now);
			var empModel = _empService.EmployeeDetailsById (_dataservice.GetCurrentDriverId ());
			empModel.Domain = Domain;
			empModel.Username = UserName;
			if(RememberMe)
				empModel.AutoLogin = RememberMe;			
			_empService.UpdateById(empModel);
			_coworker.Insert(new CoWorkerModel(){DriverName = empModel.DriverName,DriverID = empModel.Id,EmployeeID = empModel.Id,LoggedIn = false,LoginTime = Util.GetDateTimeNow(),UserName=UserName});
			_hourCalcServive = Mvx.Resolve<IHourCalculatorService> ();
			_hourCalcServive.runHourCalculatorTimerNow ();
			IsBusy = false;
			if(string.IsNullOrEmpty(empModel.Signature) || string.IsNullOrEmpty(empModel.HomeAddress) || empModel.Cycle < 0 ){
				Close(this);
				unSubscribe ();
				ShowViewModel<AddNewProfileViewModel>();
			}else{
				unSubscribe ();
				ShowViewModel<SelectAssetViewModel>();
			}
		}

        public async void SyncDefaultCategories()
        {
            bool x = await _communicationService.SyncDefaultCategories();
            _dataservice.PersistDefaultCategoriesSynced(x);
        }
        #endregion

        #region Properties
        private string _errorMessage = string.Empty;
		public string ErrorMessage{
			get{return _errorMessage; }
			set{_errorMessage = value;RaisePropertyChanged (()=>ErrorMessage); }
		}



		private string _userName = string.Empty;
		public string UserName
		{ 
			get { return _userName; }
			set { 
				_userName = value;
				if (_userName.Length > 0) {
					searchNow (_userName);
				}
				RaisePropertyChanged (() => UserName);
			}
		}

		private string _password = string.Empty;
		public string Password
		{ 
			get { return _password; }
			set { _password = value; RaisePropertyChanged(() => Password); }
		}

		private string _domain = string.Empty;
		public string Domain
		{ 
			get { return _domain; }
			set { _domain = value; RaisePropertyChanged(() => Domain); }
		}

		private bool _isRememberMe = false;
		public bool RememberMe
		{ 
			get { return _isRememberMe; }
			set { _isRememberMe = value; RaisePropertyChanged(() => RememberMe); }
		}

		private bool _canShowDomain = false;
		public bool ShowDomain
		{ 
			get { return _canShowDomain; }
			set { _canShowDomain = value; RaisePropertyChanged(() => ShowDomain); }
		}

		private bool _isBusy;
		public bool IsBusy
		{
			get { return _isBusy; }
			set { _isBusy = value; RaisePropertyChanged(() => IsBusy); }
		}

		private bool _isOffline = false;
		public bool IsOffline
		{
			get { return _isOffline; }
			set { _isOffline = value; RaisePropertyChanged(() => IsOffline); }
		}

		private bool _loginError;
		public bool HasLoginError
		{
			get { return _loginError; }
			set
			{
				_loginError = value;
				RaisePropertyChanged(() => HasLoginError);
			}
		}	

		private ObservableCollection<string> autologinUsers;
		public ObservableCollection<string> AutoLoginUsers{
			get { return autologinUsers; }
			set {
				autologinUsers = value;
				RaisePropertyChanged (() => AutoLoginUsers);
			}
		}

		public List<EmployeeModel> Users {
			get;
			set;
		}

		private bool hideloginList;
		public bool HideLoginList
		{
			get{return hideloginList; }
			set{
				hideloginList = value;
				RaisePropertyChanged (()=>HideLoginList);
			}
		}



		private object _selectedLogin;
		public object SelectedLogin
		{
			get{return _selectedLogin; }
			set{
				_selectedLogin = value;
				RaisePropertyChanged (()=>SelectedLogin);
			}
		}

		private string searchTerm;
		public string SearchTerm 
		{
			get{return searchTerm; }
			set{
				if (value == "") {
					searchTerm = null;
					AutoLoginUsers = new ObservableCollection<string> ();
					return;
				} else {
					searchTerm = value;	
				}
				if (value.Length < 2) {
					AutoLoginUsers = new ObservableCollection<string> ();
					return;
				}
				searchNow (value);
			}
		}

		public ICommand SelectAutoLogin
		{
			get {
				return new MvxCommand<string>((selecteditem)=>{
					UserName = selecteditem;
					Invert();
				});
			}
		}
		#endregion

		#region Events
		public event EventHandler LoginError;
		protected virtual void OnLoginError(EventArgs e)
		{
			if (LoginError != null)
			{
				HasLoginError = true;
				LoginError(this, e);
			}
		}

		public event EventHandler LoginSuccess;
		protected virtual void OnLoginSuccess(EventArgs e)
		{
			if (LoginSuccess != null)
			{
				HasLoginError = false;
				LoginSuccess(this, e);
			}
		}
		#endregion

		#region Commands
		public ICommand ExecuteLogin
		{
			get {
				return new MvxCommand(async () => await LoginCommand ());
			}
		}

		public async Task LoginCommand() {
			var errorMessage = string.Empty;
			if (UserName.Length == 0) {
				errorMessage = _languageService.GetLocalisedString(Constants.str_username);
			} else if (Password.Length == 0) {
				errorMessage = _languageService.GetLocalisedString(Constants.str_password);
			} else if(Domain.Length == 0) {
				errorMessage = _languageService.GetLocalisedString(Constants.str_domain_req);
			}
			if(!string.IsNullOrEmpty(errorMessage)){
				ErrorMessage = errorMessage;
				return;
				//OnLoginError (new ErrorMessageEventArgs(){Message = errorMessage});
			}
			IsBusy = true;
			bool isLoggedIn = await _loginservice.Login(UserName, Password, Domain);
			if (isLoggedIn) {
				_dataservice.PersistDomain(Domain);
				_dataservice.PersistUserName (UserName);
				_dataservice.PersistLastLogin (Util.GetDateTimeUtcNow());
				OnLoginSuccess(new EventArgs());
				await Task.Run (() => {
					SyncTimelog ();
                    SyncDefaultCategories();
                });
			}
			else {
				var localmodel = _empService.getEmployeeByUserAndPassword(UserName,Password);
				if(localmodel != null && localmodel.Domain == Domain){
					_dataservice.PersistCurrentDriverId(localmodel.Id);
					_dataservice.PersistUserName (localmodel.Username);
					_dataservice.PersistLastLogin (Util.GetDateTimeUtcNow());
					await Task.Run (() => {
						SyncTimelog();
                        SyncDefaultCategories();
					});
				}else{
					IsBusy = false;
					ErrorMessage = _languageService.GetLocalisedString (Constants.str_login);
					//OnLoginError(new ErrorMessageEventArgs(){Message = "Login failed!"});
				}
			}
		}

		public ICommand GoToForgotPasswordView
		{
			get {
				return new MvxCommand(() =>
					{
                        //ShowViewModel<ForgotPasswordViewModel>();
                        ShowViewModel<CoworkerOnBreakViewModel>();
                    });
			}
		}

		public ICommand GoToTakeATour
		{
			get {
				return new MvxCommand(() =>
					{
						ShowViewModel<TakeatourViewModel>();
						//unSubscribe ();
						//Close(this);
					});
			}
		}


		public event EventHandler FeatureUnderDevelopment;
		protected virtual void OnFeatureUnderDevelopment(EventArgs e)
		{
			if (FeatureUnderDevelopment != null)
			{
				FeatureUnderDevelopment(this, e);
			}
		}


		public event EventHandler VersionUpdate;
		protected virtual void OnVersionUpdate(EventArgs e)
		{
			if (VersionUpdate != null)
			{
				VersionUpdate(this, e);
			}
		}
		#endregion

		#region unsubscribe
		public void unSubscribe() {
//			_messenger.Unsubscribe<LoggedInMessage>(_loginSuccess);
//			_messenger.Unsubscribe<LogInError>(_loginFailed);
			_messenger.Unsubscribe<NetworkStatusChangedMessage> (_networkStatusChanged);
		}
		#endregion

		public void searchNow(string searchTerm)
		{
			if (!string.IsNullOrEmpty (searchTerm)) {
				if (AutoLoginUsers.Contains (searchTerm)) {
					Password = Users.FirstOrDefault (p => p.Username != null && p.Username.ToLower () == searchTerm.ToLower ()).Password;
					Domain = Users.FirstOrDefault (p => p.Username != null && p.Username.ToLower () == searchTerm.ToLower ()).Domain;
					//RememberMe = Users.FirstOrDefault (p => p.Username != null && p.Username.ToLower () == searchTerm.ToLower ()).AutoLogin;
					return;
				}
				AutoLoginUsers = new ObservableCollection<string> (Users.Where (p => p.Username.ToLower ().Contains (searchTerm.ToLower ())).Select (a => a.Username).ToList ());
				if (AutoLoginUsers.Count > 0) {
					HideLoginList = true;	
				} else {
					HideLoginList = false;
				}
			} else {
				HideLoginList = false;
			}
		}

		void Invert(){			
			HideLoginList = !HideLoginList;
		}
		public void CheckLastSession(){
			Constants.LastActivity = string.Empty;
			var	HaveLastSession = _lastSession.ResumeLastSession ();
			if(HaveLastSession){
				_lastSession.DeleteAllSessions ();
				RestoreLastSession.Execute (null);
			}
			else {
				if(!Constants.NewDownloadedPkgPath.Equals(string.Empty))
				{
					//re-Install the application using the downloaded pkg file
					OnVersionUpdate(new EventArgs());
				}
			}
		}

		public ICommand RestoreLastSession
		{
			get {
				return new MvxCommand(() =>
					{
						_bsmBoxWifiService.stopBoxDataTimer ();
						_bsmBoxWifiService.stopBoxConnectivityTimer ();
						if(_dataservice.GetAssetBoxId() == -1){							
							ShowViewModel<SelectAssetViewModel>();
						}else{							
							ShowViewModel<DashboardViewModel>(new {onrestore = true});
						}
						unSubscribe();
					});
			}
		}
	}
}

