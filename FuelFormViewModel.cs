using System;
using System.Windows.Input;
using MvvmCross.Core.ViewModels;
using BSM.Core.Services;
using System.Collections.Generic;
using BSM.Core.ConnectionLibrary;

namespace BSM.Core.ViewModels
{
	public class FuelFormViewModel : BaseViewModel
	{
		private readonly ISettingsService _settings;

		public FuelFormViewModel (ISettingsService settings)
		{
			_settings = settings;
			var odoMeterSetting = _settings.GetSettingsByName (Constants.SETTINGS_MI_ODO_ENABLED);
			if(odoMeterSetting != null){
				OdoMeterSwitch = odoMeterSetting.SettingsValue == "true" ? true : false;
			}
			DateValue = Util.GetDateTimeNow ();
		}

		public void Init(int fuelFormId,string desc){
		// check here new form or existing form
			InspectionDescription = desc;
		}

		public string InspectionDescription {
			get;
			set;
		}
		private int fuelAmount;
		public int FuelAmount{
			get{return fuelAmount; }
			set{fuelAmount = value;RaisePropertyChanged (()=>FuelAmount); }
		}
		private bool _fuelAmountSwitch;
		public bool FuelAmountSwitch
		{
			get{return _fuelAmountSwitch; }
			set{_fuelAmountSwitch = value;RaisePropertyChanged (()=>_fuelAmountSwitch); }
		}

		private DateTime _dateValue;
		public DateTime DateValue
		{
			get{return _dateValue; }
			set{_dateValue = value;RaisePropertyChanged (()=>DateValue); }
		}

		private bool odoMeterSwitch;
		public bool OdoMeterSwitch
		{
			get{return odoMeterSwitch; }
			set{odoMeterSwitch = value;RaisePropertyChanged (()=>OdoMeterSwitch); }
		}

		private string address;
		public string Address
		{
			get{return address; }
			set{address = value;RaisePropertyChanged (()=>Address); }

		}

		private List<string> attachments;
		public List<string> Attachments
		{
			get{return attachments; }
			set{attachments = value;RaisePropertyChanged (()=>Attachments); }
		}

		public IMvxCommand CancelFuelForm
		{
			//get{return new MvxCommand (()=>Close(this)); }

			get{return new MvxCommand (()=> {
				OnCloseView(new EventArgs());
				UnSubScribeFromBaseViewModel();
				Close(this);

			}); }

		}
        //test-command
        public ICommand GoBack
        {
            get
            {
                return new MvxCommand(() => {
                  
                    OnCloseView(new EventArgs());
                    Close(this);
                });
            }
        }
        public IMvxCommand OpenGallery
		{
			//get{return new MvxCommand (()=>Close(this)); }

			get{return new MvxCommand (()=> {
				OnFeatureUnderDevelopment(new EventArgs());
			}); }

		}

		public IMvxCommand OpenCamera
		{
			//get{return new MvxCommand (()=>Close(this)); }

			get{return new MvxCommand (()=> {
				OnFeatureUnderDevelopment(new EventArgs());
			}); }

		}

		public IMvxCommand SaveFuelForm
		{
			get{return new MvxCommand (()=> {
				OnFeatureUnderDevelopment(new EventArgs());
			}); }
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

