using BSM.Core.Services;
using System;
using BSM.Core.ConnectionLibrary;
using MvvmCross.Core.ViewModels;
using MvvmCross.Platform.Plugins;
using MvvmCross.Platform;
using MvvmCross.Platform.IoC;

namespace BSM.Core
{
    public class App : MvxApplication
    {

		private ILoginService _loginservice;
		private IDataService _dataservice;
		private ILanguageService _languageservice;
		private IDeviceService _deviceservice;
		private ILoggerService _loggerservice;

		private BSMHelper.OS _os;
		private string _lang;
		private string _appVersion;
		/*public App(BSMHelper.OS os, string lang)
		{
			_os = os;
			_lang = lang;

			#if DEBUG 
			Constants.Debug = true;
			#else
			Constants.Debug = false;
			#endif
		}*/

		public App(BSMHelper.OS os, string appVersion)
		{
			_os = os;
			_appVersion = appVersion;

			#if DEBUG 
			Constants.Debug = true;
			#else
			Constants.Debug = false;
			#endif
		}

		public App ()
		{
			//TODO: Normal Constructor Initialize
			#if DEBUG 
			Constants.Debug = true;
			#else
			Constants.Debug = false;
			#endif
		}

		public override void LoadPlugins (IMvxPluginManager pluginManager)
		{
			base.LoadPlugins (pluginManager);
			//pluginManager.EnsurePlatformAdaptionLoaded<Acr.MvvmCross.Plugins.DeviceInfo.PluginLoader> ();
//			pluginManager.EnsurePlatformAdaptionLoaded<Acr.MvvmCross.Plugins.FileSystem.PluginLoader>();
		}

        public override void Initialize()
        {
            //Mvx.ConstructAndRegisterSingleton<ICheckDBVersion, CheckDBVersion>();
            Mvx.ConstructAndRegisterSingleton<IDataService, BSMDataService>();
            _dataservice = Mvx.Resolve<IDataService>();
            _dataservice.SetAppVersion(_appVersion);

            CreatableTypes()
                .EndingWith("Service")
                .AsInterfaces()
                .RegisterAsLazySingleton();

            // Mvx.ConstructAndRegisterSingleton<IDeviceService, DeviceService>();
            _deviceservice = Mvx.Resolve<IDeviceService>();
            _deviceservice.SetCurrentDevice(_os);

            // Trying to Initialize Table creation
            Mvx.ConstructAndRegisterSingleton<IDBConnection, DBConnection>();
            Mvx.ConstructAndRegisterSingleton<IInspectionReportService, InspectionReportService>();
			Mvx.ConstructAndRegisterSingleton<IInspectionItemService, InspectionItemService>();
			Mvx.ConstructAndRegisterSingleton<IInspectionReportDefectService, InspectionReportDefectService>();
			Mvx.ConstructAndRegisterSingleton<ITimeLogService, TimeLogService>();
			Mvx.ConstructAndRegisterSingleton<ICategoryVehicleService, CategoryVehicleService>();


			_loginservice = Mvx.Resolve<ILoginService>();
			DateTime lastLogin = _loginservice.LastLogin;

//			_dataservice = Mvx.Resolve<IDataService>();
			bool acceptedRememberMe = _dataservice.AcceptedRememberMe();

			if (acceptedRememberMe) {
				RegisterAppStart<ViewModels.LoginViewModel> ();
				return;
			} else {
				RegisterAppStart<ViewModels.LoginViewModel> ();
				return;
			}
        }
    }
}