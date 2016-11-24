using System;
using MvvmCross.Core.ViewModels;
using BSM.Core.Services;
using BSM.Core.ConnectionLibrary;
using BSM.Core.ViewModels;
using System.Windows.Input;

namespace BSM.Core.ViewModels
{ 
	public class SettingsViewModel : BaseViewModel
	{
		private readonly IDataService _dataService;
		private readonly ISettingsService _settings;

		public SettingsViewModel (IDataService dataService,ISettingsService settingService)
		{
			_dataService = dataService;
			_settings = settingService;
			IpAddress = "65.110.160.142";
			var tcpipSetting = _settings.GetSettingsByName (Constants.SETTINGS_TCP_ENABLED);
			if(tcpipSetting != null){
				TcpSetting = tcpipSetting;
				TcpipSwitchOn = tcpipSetting.SettingsValue == "1" ? true : false;
			}

			var gpsSetting = _settings.GetSettingsByName (Constants.SETTINGS_BOX_GPS_ENABLED);
			if(gpsSetting != null){
				GpsSetting = gpsSetting;
				GpsSwitchOn = gpsSetting.SettingsValue == "1" ? true : false;
			}

			var odoSetting = _settings.GetSettingsByName (Constants.SETTINGS_MI_ODO_ENABLED);
			if(odoSetting != null){
				OdoMeterSetting = odoSetting;
				OdoSwitchOn = odoSetting.SettingsValue == "1" ? true : false;
			}

			var odoSettingsText = _settings.GetSettingsByName (Constants.SETTINGS_MANUAL_ODO_INPUT);
			if(odoSettingsText != null ){
				CanInputOdometer = odoSettingsText.SettingsValue == "1" ? "Yes" :"No";
			}

			var loginSettings = _settings.GetSettingsByName (Constants.SETTINGS_LOGIN);
			if(loginSettings != null ){
				LoginFlag =Convert.ToInt16(loginSettings.SettingsValue);
			}

			var scanSetting = _settings.GetSettingsByName (Constants.SETTINGS_SCAN_BARCODE);
			if(scanSetting != null ){
				MustScan = scanSetting.SettingsValue;
			}

			var historyDay = _settings.GetSettingsByName (Constants.SETTINGS_INSPECTION_HISTORY_DAY);
			if(historyDay != null ){
				InsHistoryDay = historyDay.SettingsValue;
			}

			var historyAmount = _settings.GetSettingsByName (Constants.SETTINGS_INSPECTION_HISTORY_AMOUNT);
			if(historyAmount != null ){
				InsHistoryCount = historyAmount.SettingsValue;
			}

			var imagelimitSetting = _settings.GetSettingsByName (Constants.SETTINGS_IMAGE_LIMIT);
			if(imagelimitSetting != null ){
				ImageAttachment = imagelimitSetting.SettingsValue;
			}

			var hoursThreshold = _settings.GetSettingsByName (Constants.SETTINGS_VIOLATION_THRESHOLD);
			if(hoursThreshold != null ){
				HoursThresold = hoursThreshold.SettingsValue;
			}

			var lockScreen = _settings.GetSettingsByName (Constants.SETTINGS_SCREEN_LOCK);
			if(lockScreen != null ){
				LockScreen = lockScreen.SettingsValue == "1" ? "Yes" : "No";
			}

			var searchTypeSetting = _settings.GetSettingsByName (Constants.SETTINGS_SEARCH_TYPE);
			if(searchTypeSetting != null ){
				SearchTypes = searchTypeSetting.SettingsValue;
			}
			AppVersion =Util.getAppVersion();
		}
		public string IpAddress {
			get;
			set;
		}

		private int loginFlag;
		public int LoginFlag
		{
			get{return loginFlag; }
			set{loginFlag = value;RaisePropertyChanged (()=>LoginFlag); }
		}

		private string mustScan;
		public string MustScan
		{
			get{return mustScan; }
			set{mustScan = value;RaisePropertyChanged (()=>MustScan); }
		}
		private string insHistoryDay;
		public string InsHistoryDay
		{
			get{return insHistoryDay; }
			set{insHistoryDay = value;RaisePropertyChanged (()=>InsHistoryDay); }
		}

		private string insHistoryCount;
		public string InsHistoryCount
		{
			get{return insHistoryCount; }
			set{insHistoryCount = value;RaisePropertyChanged (()=>InsHistoryCount); }
		}

		private string imageAttachment;
		public string ImageAttachment
		{
			get{return imageAttachment; }
			set{imageAttachment = value;RaisePropertyChanged (()=>ImageAttachment); }
		}

		private string hoursThresold;
		public string HoursThresold
		{
			get{return hoursThresold; }
			set{hoursThresold = value;RaisePropertyChanged (()=>HoursThresold); }
		}

		private string searchTypes;
		public string SearchTypes
		{
			get{return searchTypes; }
			set{searchTypes = value;RaisePropertyChanged (()=>SearchTypes); }
		}

		private string lockScreen;
		public string LockScreen
		{
			get{return lockScreen; }
			set{lockScreen = value;RaisePropertyChanged (()=>LockScreen); }
		}

		private string canInputOdometer;
		public string CanInputOdometer
		{
			get{return canInputOdometer; }
			set{canInputOdometer = value;RaisePropertyChanged (()=>CanInputOdometer); }
		}

		private string appVersion;
		public string AppVersion
		{
			get{return appVersion; }
			set{appVersion = value;RaisePropertyChanged (()=>AppVersion); }
		}

		private bool tcpipSwitchOn;
		public bool TcpipSwitchOn
		{
			get{return tcpipSwitchOn; }
			set{
				tcpipSwitchOn = value;
				RaisePropertyChanged (()=>TcpipSwitchOn);
				TcpSetting.SettingsValue = value ? "1" : "0";
				//_settings.Insert (TcpSetting);
			}
		}

		private bool gpsSwitchOn;
		public bool GpsSwitchOn
		{
			get{return gpsSwitchOn; }
			set
			{
				gpsSwitchOn = value;
				RaisePropertyChanged (()=>GpsSwitchOn);

				if (GpsSetting == null) {
					GpsSetting = new SettingsModel ();
					GpsSetting.SettingsName = Constants.SETTINGS_BOX_GPS_ENABLED;
					GpsSetting.SettingsValue = value ? "1" : "0";
				} else {
					GpsSetting.SettingsValue = value ? "1" : "0";
				}
				_settings.Insert (GpsSetting);
			}
		}


		private bool odoSwitchOn;
		public bool OdoSwitchOn
		{
			get{return odoSwitchOn; }
			set
			{
				odoSwitchOn = value;
				RaisePropertyChanged (()=>OdoSwitchOn);
				if (OdoMeterSetting == null) {
					OdoMeterSetting = new SettingsModel ();
					OdoMeterSetting.SettingsName = Constants.SETTINGS_MI_ODO_ENABLED;
					OdoMeterSetting.SettingsValue = value ? "1" : "0";
				} else {
					OdoMeterSetting.SettingsValue = value ? "1" : "0";
				}
				_settings.Insert (OdoMeterSetting);
			}
		}
		public SettingsModel TcpSetting {
			get;
			set;
		}

		public SettingsModel GpsSetting {
			get;
			set;
		}

		public SettingsModel OdoMeterSetting {
			get;
			set;
		}

		public IMvxCommand showMyprofile
		{
			get{
				return new MvxCommand(() =>showProfile());
			}
		}

		public void showProfile()
		{
			ShowViewModel<MyProfileViewModel> ();
		}

		public ICommand GoBackCommand
		{
			get
			{
				return new MvxCommand (() => Close (this));
			}
		}

		private void GoBack()
		{
			Close(this);
		}

	}
}

