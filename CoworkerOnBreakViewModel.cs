using System;
using MvvmCross.Core.ViewModels;
using MvvmCross.Plugins.Messenger;
using BSM.Core.Services;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using BSM.Core.AuditEngine;

using Acr.MvvmCross.Plugins.Network;
using MvvmCross.Platform;

namespace BSM.Core
{
	public class CoworkerOnBreakViewModel : MvxViewModel
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

		public CoworkerOnBreakViewModel (IDataService dataService,ICommunicationService communication,IMvxMessenger messenger,IEmployeeService employee,ICoWorkerService coworker,ITimeLogService timelog,IHourCalculatorService hourCalcService,IBSMBoxWifiService bsmBoxWifiService, ILanguageService languageService)
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
			SelectedCoworkers = new List<CoWorkerModel> ();
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
			set { _isBusy = value; RaisePropertyChanged (()=>IsBusy); }
		}


		private List<CoWorkerModel> _list;
		public List<CoWorkerModel> list
		{ 
			get { return _list; }
			set { _list = value; RaisePropertyChanged (()=>list); }
		}

		public List<CoWorkerModel> SelectedCoworkers { 
			get;
			set;
		}

		public ICommand SelectCheckbox
		{
			get {
				return new MvxCommand<CoWorkerModel> ((selectedCoworker) => {
					if(SelectedCoworkers != null && !SelectedCoworkers.Contains(selectedCoworker)){
						SelectedCoworkers.Add(selectedCoworker);
						list.FirstOrDefault(p=>p.DriverID == selectedCoworker.DriverID).IsSelected = true;
					}else{
						if(SelectedCoworkers != null  && SelectedCoworkers.Contains(selectedCoworker)){
							SelectedCoworkers.Remove(selectedCoworker);
							list.FirstOrDefault(p=>p.DriverID == selectedCoworker.DriverID).IsSelected = false;
						}
					}
					list=new List<CoWorkerModel>(list);
				});
			}
		}
		public ICommand ContinueToBreak
		{
			get {
				return new MvxCommand(() => {
					if(SelectedCoworkers != null && SelectedCoworkers.Count > 0){
						foreach(var coworker in SelectedCoworkers){
								var coworkertlr = new TimeLogModel();
								_bsmBoxWifiService.LocalizeTimeLog(ref coworkertlr);
								coworkertlr.Event =(int) LOGSTATUS.OffDuty;
								coworkertlr.LogStatus = (int)LOGSTATUS.OffDuty;
								coworkertlr.Logbookstopid = AuditLogic.ThirtyMinutesOffDutyStart;
								coworkertlr.CoDriver = coworker.DriverID;
								_timelog.Insert (coworkertlr);
							}
							_hourCalcService.runHourCalculatorTimer();
						}
					OnCloseView(new EventArgs());
					Close(this);
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

		public event EventHandler CloseView;
		protected virtual void OnCloseView(EventArgs e)
		{
			unSubscribe ();
			if (CloseView != null)
			{
				CloseView(this, e);
			}
		}

		#region unsubscribe
		public void unSubscribe() {
			_messenger.Unsubscribe<NetworkStatusChangedMessage> (_networkStatusChanged);
		}
		#endregion
	}
}

