﻿using System;
using MvvmCross.Core.ViewModels;
using System.Windows.Input;
using System.Threading.Tasks;
using BSM.Core.Services;
using MvvmCross.Plugins.Messenger;
using BSM.Core.Messages;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BSM.Core.AuditEngine;
using BSM.Core.ConnectionLibrary;
using System.Linq;

using Acr.MvvmCross.Plugins.Network;
using MvvmCross.Platform;

namespace BSM.Core.ViewModels
{
	public class AddAttachmentViewModel: MvxViewModel
	{
		#region Member Variables
		private readonly IMvxMessenger _messenger;
		private readonly IDataService _dataservice;
		private readonly ICommunicationService _communicationService;
		private readonly IAssetService _assetService;
		private readonly ICategoryVehicleService _categoryvehicleService;
		private readonly IBSMBoxWifiService _boxWiFiService;
		private readonly ISettingsService _settings;
		private readonly ISyncService _syncService;
		private readonly ITimeLogService _timelogService;
		private MvxSubscriptionToken _syncSuccess;
		private MvxSubscriptionToken _syncFailed;
		private readonly ILastSessionService _sessionService;
		private readonly IEmployeeService _employeeService;
		private readonly ILanguageService _languageService;
		private readonly MvxSubscriptionToken _networkStatusChanged;

		private object _sync = new object();
		#endregion
		public void Init(bool isAttach){
			//			_hasAttachment = isAttach;
			HasAttachment = isAttach;
		}
		#region ctors
		public AddAttachmentViewModel (IMvxMessenger messenger, IDataService dataservice, ICommunicationService communicationService, IAssetService assetService, ICategoryVehicleService categoryvehicleService
			,IBSMBoxWifiService boxService,ISettingsService settings,ITimeLogService timelog,ISyncService syncService,ILastSessionService sessionService,IEmployeeService employee, ILanguageService languageService)
		{
			_employeeService = employee;
			_sessionService = sessionService;
			_syncService = syncService;
			_timelogService = timelog;
			_messenger = messenger;
			_dataservice = dataservice;
			_communicationService = communicationService;
			_assetService = assetService;
			_categoryvehicleService = categoryvehicleService;
			_boxWiFiService = boxService;
			_settings = settings;
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

			SearchTypes = new List<SearchType>();
			ConfigSearchType = "3";
			var searchSettings = _settings.GetSettingsByName (Constants.SETTINGS_SEARCH_TYPE);
			if(searchSettings != null){
				ConfigSearchType = searchSettings.SettingsValue;
			}
			if(!string.IsNullOrEmpty(ConfigSearchType)){				
				if (ConfigSearchType.Length == 1) {
					var searchtype = new SearchType ();
					searchtype.searchTypeText = _languageService.GetLocalisedString (Constants.str_description);
					searchtype.searchType = 3;
					SearchTypes.Add (searchtype);
					SelectedSearchType = searchtype;
				} else {
					var searchArray = ConfigSearchType.Split (',').ToList<string> ();
					foreach(var id in searchArray){						
						if(id=="0"){
							var searchtype = new SearchType ();
							searchtype.searchTypeText = _languageService.GetLocalisedString (Constants.str_sap);
							searchtype.searchType = 0;
							SearchTypes.Add (searchtype);
						}else if(id=="1"){
							var searchtype = new SearchType ();
							searchtype.searchTypeText = _languageService.GetLocalisedString (Constants.str_legacy);
							searchtype.searchType = 1;
							SearchTypes.Add (searchtype);
						}else if(id=="2"){
							var searchtype = new SearchType ();
							searchtype.searchTypeText = _languageService.GetLocalisedString (Constants.str_serial);
							searchtype.searchType = 2;
							SearchTypes.Add (searchtype);
						}else if(id=="3"){
							var searchtype = new SearchType ();
							searchtype.searchTypeText =  _languageService.GetLocalisedString (Constants.str_description);
							searchtype.searchType = 3;
							SearchTypes.Add (searchtype);
						}else if(id=="4"){
							var searchtype = new SearchType ();
							searchtype.searchTypeText = _languageService.GetLocalisedString (Constants.str_licence);
							searchtype.searchType = 4;
							SearchTypes.Add (searchtype);
						}else if(id=="5"){
							var searchtype = new SearchType ();
							searchtype.searchTypeText = _languageService.GetLocalisedString (Constants.str_box_id);
							searchtype.searchType = 5;
							SearchTypes.Add (searchtype);
						}
					}
					SelectedSearchType = SearchTypes.FirstOrDefault(p=>p.searchType ==Convert.ToInt16(searchArray[0]));
				}
			}
			var scanSettings = _settings.GetSettingsByName (Constants.SETTINGS_SCAN_BARCODE);
			if(scanSettings != null){
				IsMustScan =Convert.ToBoolean(scanSettings.SettingsValue);
			}
			issearch = true;
			_syncSuccess = _messenger.Subscribe<SyncSuccessMessage>((message) =>
				{
					IsBusy = false;
                    unSubscribe();
                    if (_dataservice.GetIsSelectAttachment()) {
						_messenger.Publish<AttachmentSuccessMessage> (new AttachmentSuccessMessage(this));
						_dataservice.ClearIsSelectAttachment();
						OnCloseView(new EventArgs());
                        ViewShouldClose = true;
                    }
					Close(this);
					//var dispatcher = Mvx.Resolve<MvvmCross.Platform.Core.IMvxMainThreadDispatcher>(); 
					//dispatcher.RequestMainThreadAction(() => {
					//    this.Close(this);
					//});                    
				});
			_syncFailed = _messenger.Subscribe<SyncFailedMessage>((message) =>
				{
					IsBusy = false;
					OnSyncError(new EventArgs());
					// TODO: Show the message sync failed
				});
		}
        #endregion

        #region Properties
        private bool _viewShouldClose = false;
        public bool ViewShouldClose
        {
            get { return _viewShouldClose; }
            set { _viewShouldClose = value; RaisePropertyChanged(() => ViewShouldClose); }
        }

        private bool _isOffline = false;
		public bool IsOffline
		{
			get { return _isOffline; }
			set { _isOffline = value; RaisePropertyChanged(() => IsOffline); }
		}

		private bool _rescanEnabled;
		public bool RescanEnabled
		{
			get { return _rescanEnabled; }
			set {
				_rescanEnabled = value;
				RaisePropertyChanged(() => RescanEnabled);
			}
		}

		private string _searchTerm = string.Empty;
		public string SearchTerm
		{
			get { return _searchTerm; }
			set {
				_searchTerm = value;
				IsSearch = false;
				performSearch ();
				RaisePropertyChanged(() => SearchTerm);
			}
		}

		private ObservableCollection<AssetModel> _searchResults;
		public ObservableCollection<AssetModel> SearchResults
		{
			get { return _searchResults; }
			set {
				_searchResults = value;
				OnSearch = false;
				RaisePropertyChanged (() => SearchResults);
			}
		}

		private AssetModel _selectedSearchResult;
		public AssetModel SelectedSearchResult
		{
			get { return _selectedSearchResult; }
			set { _selectedSearchResult = value; RaisePropertyChanged(() => SelectedSearchResult); }
		}

		private List<SearchType> _searchTypes;
		public List<SearchType> SearchTypes
		{
			get { return _searchTypes; }
			set { _searchTypes = value; RaisePropertyChanged(() => SearchTypes); }
		}

		private SearchType _selectedSearchType;
		public SearchType SelectedSearchType
		{
			get { return _selectedSearchType; }
			set {
				_selectedSearchType = value;
				Invert ();
				performSearch ();
				RaisePropertyChanged(() => SelectedSearchType);
			}
		}

		private bool _isBusy;
		public bool IsBusy
		{
			get { return _isBusy; }
			set { _isBusy = value; RaisePropertyChanged(() => IsBusy); }
		}

		private bool issearch;
		public bool IsSearch
		{
			get{ return issearch;}
			set{issearch = value;RaisePropertyChanged (()=>IsSearch); }
		}
		private string _scannedBarcode;
		public string ScannedBarcode
		{
			get { return _scannedBarcode; }
			set { 
				_scannedBarcode = value;
				if (!string.IsNullOrEmpty(_scannedBarcode)) {					
					var resultModel = new AssetModel ();
					if (SearchResults != null && SearchResults.Count > 0) {
						foreach(var result in SearchResults){
							if(_scannedBarcode == result.SearchValue && SelectedSearchType.searchType == result.SearchType){
								resultModel = result;
								break;
							}
						}	
					}
					if (string.IsNullOrEmpty (resultModel.AssetDescription)) {
						IsSacn = true;
						SearchTerm = _scannedBarcode;
					} else {
						CheckForAsset (resultModel);
					}
				}
				RaisePropertyChanged(() => ScannedBarcode);
			}
		}
		private bool _hasAttachment;
		public bool HasAttachment
		{
			get{return _hasAttachment; }
			set{_hasAttachment = value;RaisePropertyChanged (()=>HasAttachment);}
		}

		private bool isScan;
		public bool IsSacn
		{
			get{return isScan; }
			set{isScan = value;RaisePropertyChanged (()=>IsSacn); }
		}

		private bool doScan;
		public bool DoScan{
			get{return doScan; }
			set{doScan = value;RaisePropertyChanged (()=>DoScan); }
		}

		public string ConfigSearchType{ get; set;}
		public bool IsMustScan{ get; set;}

		public bool OnSearch;

		#endregion

		#region Events
		public event EventHandler SyncError;
		protected virtual void OnSyncError(EventArgs e)
		{
			if (SyncError != null)
			{
				//				HasError = true;
				SyncError(this, e);
			}
		}

		public event EventHandler SyncSuccess;
		protected virtual void OnSyncSuccess(EventArgs e)
		{
			if (SyncSuccess != null)
			{
				//				HasError = false;
				SyncSuccess(this, e);
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

		private async  void CheckForAsset(AssetModel asset) {
			if (asset != null) {
				SelectedSearchResult = asset;
				SelectedSearchResult.InsertTS = DateTime.UtcNow;
				SelectedSearchResult.DOT = "";
				SelectedSearchResult.EmployeeID = _dataservice.GetCurrentDriverId ();
				SelectedSearchResult.SearchType = SelectedSearchType.searchType;
				SelectedSearchResult.SearchValue = SearchTerm;
				_assetService.SaveVehicleAssignment (_dataservice.GetCurrentDriverId (), asset.AssetBoxId, asset.AssetDescription, asset.Weight, HasAttachment, SelectedSearchType.searchType, SearchTerm,Utils.GetDateTimeNow(), asset.VehicleLicense);
				if (!string.IsNullOrEmpty (asset.VehicleLicense) || !string.IsNullOrEmpty (asset.VehicleLicenseProvince))
					_assetService.UpdateVehicleInfo (asset.AssetBoxId, asset.VehicleLicense, asset.VehicleLicenseProvince);
				_scannedBarcode = String.Empty;
				if(!HasAttachment){
					_dataservice.PersistAssetBoxId(SelectedSearchResult.AssetBoxId);
					_dataservice.PersistAssetBoxDescription(SelectedSearchResult.AssetDescription);
					_dataservice.SetScannedTimeStamp ();
					PutDriverOnDutyAfterScann ();
					/*var tlr = new TimeLogModel ();
					tlr.Event =(int) LOGSTATUS.AssignDriverToVehicle;
					tlr.LogStatus =(int) LOGSTATUS.AssignDriverToVehicle;
					_boxWiFiService.LocalizeTimeLog (ref tlr);
					_timelogService.InsertOrUpdate (tlr);*/
					_syncService.runTimerCallBackNow ();

				} else {
					_dataservice.PersistAttachmentId(SelectedSearchResult.AssetBoxId.ToString());
					_dataservice.PersistAttachmentDescription(SelectedSearchResult.AssetDescription);
					_dataservice.PersistIsSelectAttachment(true);
				}
				var catVehicleData = _categoryvehicleService.GetVehicleCategoryByBoxId(SelectedSearchResult.AssetBoxId);
				if (catVehicleData != null) {
					_communicationService.User4CategoriesRequest(SyncType.User, catVehicleData.updateTS, SelectedSearchResult.AssetBoxId, _dataservice.GetCurrentDriverId ());
				}
				else {					
					_communicationService.User4CategoriesRequest(SyncType.User, new DateTime (1970, 01, 01), SelectedSearchResult.AssetBoxId, _dataservice.GetCurrentDriverId ());
				}
			}
		}

		#region Commands
		public ICommand ShowAssetSearchType
		{
			get {
				return new MvxCommand(() => 
					Invert()
				);
			}
		}

		public ICommand AddTrailerCommand
		{
			get
			{
				return new MvxCommand(() => {
                    ShowViewModel<AddNewAttachmentViewModel>();
                });
			}
		}
		
        public ICommand Cancel
		{
			get {
				return new MvxCommand(() =>{
					_dataservice.ClearIsSelectAttachment();
					Close(this);
					OnCloseView(new EventArgs());
					unSubscribe();
				});
			}
		}

		public ICommand SelectAssetCommand
		{
			get {
				return new MvxCommand<AssetModel> (async(SearchResult) => {
					if(!IsMustScan){
						await SelectAsset(SearchResult);
					}else{						
						DoScan = true;
					}
				});
			}
		}

		public ICommand NotInVehicleCommand
		{
			get {
				return new MvxCommand(() => {
					_dataservice.ClearPersistedAssetBoxId();
					_dataservice.ClearPersistedAssetBoxDescription();
					_boxWiFiService.stopBoxDataTimer ();
					_boxWiFiService.stopBoxConnectivityTimer ();
					ShowViewModel<DashboardViewModel>();
					unSubscribe();
					Close(this);
				});
			}
		}

		public async Task SelectAsset(AssetModel SearchResult) {
			SelectedSearchResult = SearchResult;
			IsBusy = true;
			SearchResult.InsertTS = DateTime.UtcNow;
			SearchResult.DOT = "";
			SearchResult.SearchType = SelectedSearchType.searchType;
			SearchResult.SearchValue = SearchTerm;
			SearchResult.EmployeeID = _dataservice.GetCurrentDriverId();
			_assetService.SaveVehicleAssignment (_dataservice.GetCurrentDriverId (), SearchResult.AssetBoxId, SearchResult.AssetDescription, SearchResult.Weight, HasAttachment, SelectedSearchType.searchType, SearchTerm,Utils.GetDateTimeNow(), SearchResult.VehicleLicense);
			if (!string.IsNullOrEmpty (SearchResult.VehicleLicense) || !string.IsNullOrEmpty (SearchResult.VehicleLicenseProvince))
				_assetService.UpdateVehicleInfo (SearchResult.AssetBoxId, SearchResult.VehicleLicense, SearchResult.VehicleLicenseProvince);
			if(!HasAttachment){
				_dataservice.PersistAssetBoxId(SelectedSearchResult.AssetBoxId);
				_dataservice.PersistAssetBoxDescription(SelectedSearchResult.AssetDescription);
				if(IsSacn){
					_dataservice.SetScannedTimeStamp();
					PutDriverOnDutyAfterScann();
					/*var tlr = new TimeLogModel ();
					tlr.Event =(int) LOGSTATUS.AssignDriverToVehicle;
					tlr.LogStatus =(int) LOGSTATUS.AssignDriverToVehicle;
					_boxWifiService.LocalizeTimeLog(ref tlr);
					_timelogService.Insert (tlr);*/
					_syncService.runTimerCallBackNow ();
				}
			} else {
				_dataservice.PersistAttachmentId(SelectedSearchResult.AssetBoxId.ToString());
				_dataservice.PersistAttachmentDescription(SelectedSearchResult.AssetDescription);
				_dataservice.PersistIsSelectAttachment(true);
			}
			await Task.Run(() => {
				var catVehicleData = _assetService.GetAssetByBoxId(SelectedSearchResult.AssetBoxId);
				if (catVehicleData != null) {
					_communicationService.User4CategoriesRequest(SyncType.User, catVehicleData.CatLastUpdate, SelectedSearchResult.AssetBoxId, _dataservice.GetCurrentDriverId ());
				}
				else {
					// TODO - check this - may cause issues
					_communicationService.User4CategoriesRequest(SyncType.User, new DateTime (1970, 01, 01), SelectedSearchResult.AssetBoxId, _dataservice.GetCurrentDriverId ());
				}
			});
		}

		public async void performSearch() {
			if (IsSacn) {
				IsSacn = false;
				SearchResults = await _communicationService.SyncUser4Search (SearchTerm, SelectedSearchType.searchType, int.Parse (_dataservice.GetCurrentDriverId ()), 0);
				if(SearchResults.Count == 1){
					await SelectAsset (SearchResults[0]);
				}
			} else {
				if (SearchTerm != null && SearchTerm.Length > 0) {

					if (OnSearch)
						return;
					else {
						OnSearch = true;
						SearchResults = await _communicationService.SyncUser4Search (SearchTerm, SelectedSearchType.searchType, int.Parse (_dataservice.GetCurrentDriverId ()), 0);
					}
				} else if (SearchTerm != null && SearchTerm.Length == 0) {					
					SearchResults =_assetService.AssetListByEmployeeId (_dataservice.GetCurrentDriverId (),HasAttachment);
				} else {
					SearchResults = new ObservableCollection<AssetModel> ();
				}
			}
		}

		public void Invert(){
			IsSearch = !IsSearch;
		}

		public async void SyncTimelog()
		{
			await Task.Run (delegate {
				_communicationService.SyncUser4TimeLog();	
			});
		}


		public void PutDriverOnDutyAfterScann()
		{
			//If Driver not OnDuty already put him OnDuty after scan
			var employee = _employeeService.EmployeeDetailsById(_dataservice.GetCurrentDriverId());
			var tLogRow = _timelogService.GetLast (employee.Id);
			if (tLogRow == null) {
				tLogRow = new TimeLogModel ();
				_boxWiFiService.LocalizeTimeLog (ref tLogRow);
				tLogRow.Event =(int) LOGSTATUS.OffDuty;
				tLogRow.LogStatus = (int) LOGSTATUS.OffDuty;
				tLogRow.Logbookstopid = AuditLogic.OffDuty;
				_timelogService.InsertOrUpdate (tLogRow);

				tLogRow = new TimeLogModel ();
				_boxWiFiService.LocalizeTimeLog (ref tLogRow);
				tLogRow.Event =(int) LOGSTATUS.OnDuty;
				tLogRow.LogStatus = (int) LOGSTATUS.OnDuty;
				tLogRow.Logbookstopid = AuditLogic.OnDuty;
				_timelogService.InsertOrUpdate (tLogRow);
				_dataservice.PersistCurrentLogStatus ((int) LOGSTATUS.OnDuty);
				_syncService.runTimerCallBackNow ();
			}else{				
				if (tLogRow.Event != (int)LOGSTATUS.OnDuty) {
					tLogRow = new TimeLogModel ();
					_boxWiFiService.LocalizeTimeLog (ref tLogRow);
					//SentinelMobile.Shared.Data.Utils.LocalizeTimeLog (ref tLogRow);
					tLogRow.Event =(int) LOGSTATUS.OnDuty;
					tLogRow.LogStatus =(int) LOGSTATUS.OnDuty;
					tLogRow.Logbookstopid = AuditLogic.OnDuty;
					_timelogService.InsertOrUpdate (tLogRow);
					_dataservice.PersistCurrentLogStatus ((int) LOGSTATUS.OnDuty);
					_syncService.runTimerCallBackNow ();
				}
			}
		}
		#endregion

		#region unsubscribe
		public void unSubscribe() {
			_messenger.Unsubscribe<SyncSuccessMessage>(_syncSuccess);
			_messenger.Unsubscribe<SyncFailedMessage>(_syncFailed);
			_messenger.Unsubscribe<NetworkStatusChangedMessage> (_networkStatusChanged);
		}
		#endregion
	}
}

