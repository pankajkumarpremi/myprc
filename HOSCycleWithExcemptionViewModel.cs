using System;
using MvvmCross.Core.ViewModels;
using BSM.Core.Services;
using BSM.Core.ConnectionLibrary;
using BSM.Core.ViewModels;
using System.Windows.Input;
using System.Linq;
using BSM.Core.AuditEngine;
using MvvmCross.Plugins.Messenger;
using BSM.Core.Messages;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BSM.Core
{
	public class HOSCycleWithExcemptionViewModel : BaseViewModel
	{
		private readonly IDataService _dataService;
		private readonly IRuleSelectionHistoryService _ruleselection;
		private readonly IEmployeeService _employeservice;
		private readonly ICommunicationService _communication;
		private readonly ISyncService _syncService;
		private readonly IMvxMessenger _messenger;
		public HOSCycleWithExcemptionViewModel (IDataService dataservice,IRuleSelectionHistoryService ruleselection,IEmployeeService employeservice,ICommunicationService communication,IMvxMessenger messenger, ISyncService syncService)
		{
			_communication = communication;
			_dataService = dataservice;
			_ruleselection = ruleselection;
			_employeservice = employeservice;
			_syncService = syncService;
			_messenger = messenger;
			CurrentEmployee = EmployeeDetail ();
			SelectedCycle = CycleList.FirstOrDefault (p=>p.Id == CurrentEmployee.Cycle);
			if ((CurrentEmployee.HosExceptions & (int)RuleExceptions.USA_24_hour_cycle_reset) == (int)RuleExceptions.USA_24_hour_cycle_reset)
				Chk_24_Cycle_Reset = true;
			if ((CurrentEmployee.HosExceptions & (int)RuleExceptions.USA_Oilfield_waiting_time) == (int)RuleExceptions.USA_Oilfield_waiting_time)
				Chk_Oilfield_Waiting_Time = true;
			if ((CurrentEmployee.HosExceptions & (int)RuleExceptions.USA_100_air_mile_radius) == (int)RuleExceptions.USA_100_air_mile_radius)
				Chk_100_Air_Mile_Radius = true;
			if ((CurrentEmployee.HosExceptions & (int)RuleExceptions.USA_150_air_mile_radius) == (int)RuleExceptions.USA_150_air_mile_radius)
				Chk_150_Air_Mile_Radius = true;
			if ((CurrentEmployee.HosExceptions & (int)RuleExceptions.USA_Transportation_construction_Materialsandequipment) == (int)RuleExceptions.USA_Transportation_construction_Materialsandequipment)
				Chk_Trans_Construct_Sandequipment = true;
		}

		private HosCycleModel selectedCycle;
		public HosCycleModel SelectedCycle
		{
			get{return selectedCycle; }
			set{
				selectedCycle = value;
				RaisePropertyChanged (()=>SelectedCycle);
				HideCycle = !HideCycle;
			}
		}

		private bool chk_24_cycle_reset;
		public bool Chk_24_Cycle_Reset
		{
			get{return chk_24_cycle_reset; }
			set{chk_24_cycle_reset = value;RaisePropertyChanged (()=>Chk_24_Cycle_Reset); }
		}

		private bool chk_oilfield_waiting_time;
		public bool Chk_Oilfield_Waiting_Time
		{
			get{return chk_oilfield_waiting_time; }
			set{chk_oilfield_waiting_time = value;RaisePropertyChanged (()=>Chk_Oilfield_Waiting_Time); }
		}

		private bool chk_100_air_mile_radius;
		public bool Chk_100_Air_Mile_Radius
		{
			get{return chk_100_air_mile_radius; }
			set{
				chk_100_air_mile_radius = value;
				RaisePropertyChanged (()=>Chk_100_Air_Mile_Radius);
				if (chk_100_air_mile_radius)
					Chk_150_Air_Mile_Radius = false;
			}
		}

		private bool chk_150_air_mile_radius;
		public bool Chk_150_Air_Mile_Radius
		{
			get{return chk_150_air_mile_radius; }
			set{
				chk_150_air_mile_radius = value;
				RaisePropertyChanged (()=>Chk_150_Air_Mile_Radius);
				if (chk_150_air_mile_radius)
					Chk_100_Air_Mile_Radius = false;
			}
		}

		private bool chk_trans_construct_sandequipment;
		public bool Chk_Trans_Construct_Sandequipment
		{
			get{return chk_trans_construct_sandequipment; }
			set{chk_trans_construct_sandequipment = value;RaisePropertyChanged (()=>Chk_Trans_Construct_Sandequipment); }
		}

		public EmployeeModel CurrentEmployee 
		{
			get;
			set;
		}

		public IMvxCommand SaveRules
		{
			get{return new MvxCommand (()=>Save()); }
		}

		public IMvxCommand CloseRules
		{
			get{return new MvxCommand (()=> {
				OnCloseView(new EventArgs());
				UnSubScribeFromBaseViewModel();
				Close(this);
			}); }
		}

		void Save()
		{
			var lstExcemptions = new ObservableCollection<string> ();
			int hosExceptions = 0;
			if(Chk_24_Cycle_Reset){
				hosExceptions |= (int)RuleExceptions.USA_24_hour_cycle_reset;
				var execmption = Enum.GetName (typeof(RuleExceptions),(int)RuleExceptions.USA_24_hour_cycle_reset);
				if(!lstExcemptions.Contains(execmption)){
					lstExcemptions.Add (execmption);
				}
			}
			if(Chk_Oilfield_Waiting_Time){
				hosExceptions |= (int)RuleExceptions.USA_Oilfield_waiting_time;
				var execmption = Enum.GetName (typeof(RuleExceptions),(int)RuleExceptions.USA_Oilfield_waiting_time);
				if(!lstExcemptions.Contains(execmption)){
					lstExcemptions.Add (execmption);
				}
			}
			if(Chk_100_Air_Mile_Radius){
				hosExceptions |= (int)RuleExceptions.USA_100_air_mile_radius;
				var execmption = Enum.GetName (typeof(RuleExceptions),(int)RuleExceptions.USA_100_air_mile_radius);
				if(!lstExcemptions.Contains(execmption)){
					lstExcemptions.Add (execmption);
				}
			}
			if(Chk_150_Air_Mile_Radius){
				hosExceptions |= (int)RuleExceptions.USA_150_air_mile_radius;
				var execmption = Enum.GetName (typeof(RuleExceptions),(int)RuleExceptions.USA_150_air_mile_radius);
				if(!lstExcemptions.Contains(execmption)){
					lstExcemptions.Add (execmption);
				}
			}
			if(Chk_Trans_Construct_Sandequipment){
				hosExceptions |= (int)RuleExceptions.USA_Transportation_construction_Materialsandequipment;
				var execmption = Enum.GetName (typeof(RuleExceptions),(int)RuleExceptions.USA_Transportation_construction_Materialsandequipment);
				if(!lstExcemptions.Contains(execmption)){
					lstExcemptions.Add (execmption);
				}
			}
			if(hosExceptions == 0){
				var execmption = Enum.GetName (typeof(RuleExceptions),hosExceptions);
				if(!lstExcemptions.Contains(execmption)){
					lstExcemptions.Add (execmption);
				}
			}
			//if a new cycle is selected then save it in RuleSelectionHistory table
			if(CurrentEmployee.Cycle != SelectedCycle.Id || CurrentEmployee.HosExceptions != hosExceptions){
				var rule = new RuleSelectionHistoryModel();
				rule.ruleid = SelectedCycle.Id;
				rule.country = (SelectedCycle.CycleDescription.ToString().ToLower().StartsWith("us") ? "US" : "CA");
				rule.selectTime = Util.GetDateTimeUtcNow ();
				rule.HosExceptions = hosExceptions;
				rule.DriverId = CurrentEmployee.Id;
				_ruleselection.Insert (rule);
			}
			CurrentEmployee.Cycle = SelectedCycle.Id;
			CurrentEmployee.HosExceptions = hosExceptions;
			CurrentEmployee.HaveSent = false;
			_employeservice.UpdateById (CurrentEmployee);
			_communication.sendEmployee ();

			_syncService.runTimerCallBackNow ();
			UnSubScribeFromBaseViewModel ();
			_messenger.Publish<UpdateCycleMessage>(new UpdateCycleMessage(this){Excemptions = lstExcemptions});
			OnCloseView(new EventArgs());
			Close (this);
		}

		#region Events
		public event EventHandler CloseView;
		protected virtual void OnCloseView(EventArgs e)
		{
			if (CloseView != null)
			{
				CloseView(this, e);
			}
		}
		#endregion
	}
}

