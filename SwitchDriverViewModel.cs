using System;
using System.Windows.Input;
using MvvmCross.Core.ViewModels;
using BSM.Core.Services;
using MvvmCross.Plugins.Messenger;
using BSM.Core.Messages;
using BSM.Core.ConnectionLibrary;
using System.Collections.Generic;
using System.Linq;
using BSM.Core.ViewModels;
using System.Collections.ObjectModel;
using BSM.Core.AuditEngine;

using System.Threading.Tasks;

namespace BSM.Core.ViewModels
{
	public class SwitchDriverViewModel: BaseViewModel
	{
		private readonly IMvxMessenger _messenger;
		private readonly IDataService _dataService;
		private readonly ICommunicationService _communication;
		private readonly IEmployeeService _employee;
		private readonly ICoWorkerService _coworker;
		private ITimeLogService _timelogService;
		private readonly IHourCalculatorService _hourCalcService;
		public SwitchDriverViewModel (IDataService dataservice,ICommunicationService communication,IEmployeeService employee,ICoWorkerService coworker,IMvxMessenger messenger,ITimeLogService timelogService,IHourCalculatorService hourCalcService)
		{
			_hourCalcService = hourCalcService;
			_timelogService = timelogService;
			_messenger = messenger;
			_dataService = dataservice;
			_communication = communication;
			_employee = employee;
			_coworker = coworker;
			list = _coworker.CoWorkerList().Where(p=>p.LoggedIn == true).ToList();
			foreach(var user in list){
				CoWorkers.Add (user.UserName);
			}
			DriverBeforeSwitch = EmployeeDetail ();
		}
		public List<CoWorkerModel> list{ get; set;}
		private ObservableCollection<string> coWorkers = new ObservableCollection<string>();
		public ObservableCollection<string> CoWorkers
		{
			get{return coWorkers; }
			set{coWorkers = value;RaisePropertyChanged (()=>CoWorkers); }
		}

		private string username = string.Empty;
		public string UserName
		{
			get{return username; }
			set{
				username = value;
				RaisePropertyChanged (()=>UserName);
				searchNow (username);
			}
		}

		private string password = string.Empty;
		public string Password
		{
			get{return password; }
			set{password = value;RaisePropertyChanged (()=>Password); }
		}

		private bool _isBusy;
		public bool IsBusy
		{
			get { return _isBusy; }
			set { _isBusy = value; RaisePropertyChanged(() => IsBusy); }
		}

		public EmployeeModel DriverBeforeSwitch {
			get;
			set;
		}

		public EmployeeModel DriverAfetrSwitch {
			get;
			set;
		}



		public ICommand CancelSwitch
		{
			get {
				return new MvxCommand(() =>{
					OnCloseView(new EventArgs());
					Close(this);
				});
			}
		}

		public ICommand SwitchDriver
		{
			get {
				return new MvxCommand(() => DoSwitch());
			}
		}

		public async void DoSwitch(){

			var currentDriver = EmployeeDetail ();
			if (UserName.Trim ().Length == 0) {
				OnLoginError(new ErrorMessageEventArgs(){Message = "Username required!"});
			} else if (Password.Trim ().Length == 0) {
				OnLoginError(new ErrorMessageEventArgs(){Message = "Password required!"});
			}else if (currentDriver.Username == UserName.Trim () && currentDriver.Password == Password) {
				OnLoginError(new ErrorMessageEventArgs(){Message = "Already you are driver!"});
			}  else {
				IsBusy = true;
				var response = await _communication.syncUser (UserName,Password,_dataService.GetDomain());
				if (response) {
					DriverAfetrSwitch = _employee.EmployeeDetailsById(_dataService.GetCurrentDriverId());
					DriverAfetrSwitch.Username = UserName;
					DriverAfetrSwitch.Password = Password;
					DriverAfetrSwitch.Domain = _dataService.GetDomain();
					_employee.UpdateDomain(DriverAfetrSwitch);
					await Task.Run (() => {
						DoLogin ();
					});
				} else {
					var  currentEmployee = _employee.getEmployeeByUserAndPassword(UserName,Password);
					if(currentEmployee != null){
						_dataService.PersistCurrentDriverId(currentEmployee.Id);
						_dataService.PersistUserName (currentEmployee.Username);
						await Task.Run (() => {
							DoLogin();
						});
					}else{
						IsBusy = false;
						OnLoginError(new ErrorMessageEventArgs(){Message="Login failed!"});
					}
				}
			}
		}
		public async void DoLogin()
		{
			
			// put the DriverBeforeSwitch in to coworker as logded in true
			var changeCoworkerPrevious = _coworker.GetCoworkersByEmployeeId(DriverBeforeSwitch.Id);
			if(changeCoworkerPrevious != null){
				changeCoworkerPrevious.LoggedIn =true;
				changeCoworkerPrevious.LoginTime =Util.GetDateTimeUtcNow();
				changeCoworkerPrevious.UserName =changeCoworkerPrevious.UserName;
				_coworker.Insert(changeCoworkerPrevious);
			}
			//put the DriverBeforeSwitch in to coworker and make him as logged in false
			var changeCoworkerCurrent = _coworker.GetCoworkersByEmployeeId(_dataService.GetCurrentDriverId());
			if(changeCoworkerCurrent != null){
				changeCoworkerCurrent.LoggedIn =false;
				changeCoworkerCurrent.LoginTime =Util.GetDateTimeUtcNow();
				changeCoworkerCurrent.UserName = UserName;
				_coworker.Insert(changeCoworkerCurrent);
			}

			_dataService.PersistCoDriver(DriverBeforeSwitch.Id);
			var response = await _communication.SyncUser4TimeLog();
			IsBusy = false;
			var tlm = new TimeLogModel();
			LocalizeTimeLog(ref tlm);
			tlm.LogStatus =(int) LOGSTATUS.OnDuty;
			tlm.Logbookstopid = AuditLogic.OnDuty;
			tlm.Event = (int)LOGSTATUS.OnDuty;
			_timelogService.Insert(tlm);
			//SelectedDriverStatus = DriverStatusTypes.FirstOrDefault (p=>p.driverStatusType == (int)LOGSTATUS.OnDuty);

			_hourCalcService.runHourCalculatorTimerNow ();
			OnCloseView(new EventArgs());
			UnSubScribeFromBaseViewModel ();
			ShowViewModel<DashboardViewModel> ();
			this.Close(this);
		}

		public void searchNow(string searchTerm)
		{
			if (!string.IsNullOrEmpty(searchTerm)) {
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

		public ICommand SelectCoworker
		{
			get {
				return new MvxCommand<string>((selecteditem)=>{
					SelectedCoworker = selecteditem;
					Invert();
				});
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

		void Invert(){
			HideCoworkers = !HideCoworkers;
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
			if (CloseView != null)
			{
				CloseView(this, e);
			}
		}
		#endregion

		#region unsubscribe
		#endregion
	}
}

