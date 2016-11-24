using MvvmCross.Core.ViewModels;
using System.Windows.Input;
using MvvmCross.Plugins.Messenger;
using BSM.Core.Services;
using MvvmCross.Plugins.File;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using BSM.Core.Messages;
using BSM.Core.ConnectionLibrary;
using System.Threading.Tasks;

using MvvmCross.Platform;
using MvvmCross.Plugins.Json;
using MvvmCross.Platform.Platform;

namespace BSM.Core.ViewModels
{
	public class InspectionCategoryViewModel : BaseViewModel
    {
		#region Member Variables
		private readonly ISettingsService _settings;
		private readonly ISyncService _syncService;
		private readonly ICategoryService _catService;
		private readonly IDataService _dataService;
		private readonly ILocationService _locationService;
		private readonly ICommunicationService _communicationService;
		private readonly IInspectionReportService _report;
		private readonly IInspectionItemService itemService;
		private readonly IInspectionReportDefectService _defectService;
		private readonly IAssetService _assetService;
		private readonly IMvxMessenger _messenger;
		private MvxSubscriptionToken _attachmentMessage;
        private MvxSubscriptionToken _attachmentUpdateMessage;
        private MvxSubscriptionToken _defectUpdateMessage;
		#endregion
		public void Init(int inspectiontype,string inspectionDescription,string attachmentId,int ReportId){
			InspectionType = inspectiontype;
			InspectionDescription = inspectionDescription.ToUpper();
			ReportID = ReportId;
			AttachmentId = attachmentId;
			if (ReportID != 0) {
				EditReport ();
			} else {
				NewReport ();
			}
			Odometer = ReportID != 0 ? ReportModel.Odometer : _dataService.GetOdometer();
			Odometer = (b_miles ? Convert.ToInt32(Odometer*0.621371): Odometer);
			CheckIsSuperVisorAndFormEnable ();
		}
		#region ctors
		public InspectionCategoryViewModel(ICategoryService catService,IDataService dataService,IInspectionItemService _itemservice,IMvxMessenger messenger,ILocationService locationService,IInspectionReportService report,ICommunicationService communicationService,
			IInspectionReportDefectService defectService, IAssetService assetService,ISyncService syncService,ISettingsService settings)
		{
			_settings = settings;
			Mvx.RegisterType<IMvxJsonConverter, MvxJsonConverter>();
			_syncService = syncService;
			_defectService = defectService;
			itemService = _itemservice;
			_catService = catService;
			_assetService = assetService;
			_dataService = dataService;
			_locationService = locationService;
			_report = report;
			_communicationService = communicationService;
			_messenger = messenger;
			CurrentEmployee = EmployeeDetail();
			_attachmentMessage = _messenger.Subscribe<AttachmentSuccessMessage>((message) =>
				{
					AttachmentId = _dataService.GetAttachmentId();
					AttachmentDescription = _dataService.GetAttachmentDescription();
					AllAttachmentCategories =ConvertModel(_catService.GetAllCategoryDetails(Convert.ToInt32(AttachmentId)));
					HasAttachment = true;
					ButtonDescription = "Remove";
					InvertAttachment.Execute(null);
				});
            _attachmentUpdateMessage = _messenger.Subscribe<AttachmentUpdateMessage>((message) =>
            {
              
                if (message.localAssetId<0 && !string.IsNullOrEmpty(AttachmentId) && HasAttachment &&
                        AttachmentId == message.localAssetId.ToString())
                {
                    AttachmentId = _dataService.GetAttachmentId();
                    AttachmentDescription = _dataService.GetAttachmentDescription();
                    ButtonDescription = "Remove";
                }
              
               
            });
            _defectUpdateMessage = _messenger.Subscribe<DefectUpdateMessage>((message) =>
				{
					var ModelToUpdate = Defects.FirstOrDefault(i=>i.InspectionItemId == message.model.InspectionItemId && i.attID == message.model.attID);
					var ModelFromMessenger = message.model;
					if(ModelToUpdate == null){
						Defects.Add(ModelFromMessenger);
					}else{
						Defects.Remove(ModelToUpdate);
						if(!message.removeDefect)
						Defects.Add(ModelFromMessenger);
					}
					if(!string.IsNullOrEmpty(message.model.attID)){
						checkForDefects(AllAttachmentCategories,Defects,message.model.attID);
					}else{
						checkForDefects(AllVehcileCategory,Defects);
					}

				});


			if (CurrentEmployee.Domain.ToLower ().IndexOf ("kiewit") > -1) {
				PrefixText = "Engine Hours:";
			} else {
				var mi_odo_enabled = _settings.GetSettingsByName (Constants.SETTINGS_MI_ODO_ENABLED);
				if (mi_odo_enabled != null && mi_odo_enabled.SettingsValue == "1")
					b_miles = true;
				if (b_miles)
					PrefixText = "ODOMETER(Mi):";
				else
					PrefixText ="ODOMETER(Km):";
			}
			var enableOdo = _settings.GetSettingsByName (Constants.SETTINGS_MANUAL_ODO_INPUT);
			if(enableOdo != null && enableOdo.SettingsValue == "1"){
				EnableOdo = true;
			}
		}
		#endregion

		#region Properties
		public bool b_miles;

		private bool _enableOdo;
		public bool EnableOdo
		{
			get{return _enableOdo; }
			set{
				_enableOdo = value;
				RaisePropertyChanged (()=>EnableOdo);
			}
		}

		private string _prefixText;
		public string PrefixText
		{
			get{return _prefixText; }
			set{
				_prefixText = value;
				RaisePropertyChanged (()=>PrefixText);
			}
		}

		private bool _isVechile = true;
		public bool IsVechicle
		{
			get{return _isVechile; }
			set{
				_isVechile = value;
				RaisePropertyChanged (()=>IsVechicle);
			}
		}

		private bool _hasAttachment;
		public bool HasAttachment
		{
			get{return _hasAttachment; }
			set{_hasAttachment = value;RaisePropertyChanged (()=>HasAttachment); }
		}

		private bool _isAttachmentList;
		public bool IsAttachmentList
		{
			get{return _isAttachmentList; }
			set{
				_isAttachmentList = value;
				RaisePropertyChanged (()=>IsAttachmentList);
			}
		}
		private List<CategoryModelRow> _allVehcileCategory = new List<CategoryModelRow>();
		public List<CategoryModelRow> AllVehcileCategory
		{
			get{return _allVehcileCategory; }
			set{_allVehcileCategory = value; RaisePropertyChanged (()=>AllVehcileCategory);}
		}
		private List<CategoryModelRow> _allAttachmentCategories = new List<CategoryModelRow>();
		public List<CategoryModelRow> AllAttachmentCategories
		{
			get{return _allAttachmentCategories; }
			set{
				_allAttachmentCategories = value;
				RaisePropertyChanged (()=>AllAttachmentCategories);
			}
		}
		private bool _checkAllVehcile;
		public bool CheckAllVehcile
		{
			get{ return _checkAllVehcile;}
			set{
				_checkAllVehcile = value;
				if(_checkAllVehcile){
					foreach(var item in AllVehcileCategory){
						item.IsChecked = _checkAllVehcile;
					}
				}else if(AllVehcileCategory.Where(p=>p.IsChecked == true).Count() == AllVehcileCategory.Count){
					foreach(var item in AllVehcileCategory){
						item.IsChecked = false;
						item.IsScanned = false;
					}
				}
				RaisePropertyChanged (()=>CheckAllVehcile);
			}
		}

		private bool _checkAllAttachment;
		public bool CheckAllAttachment
		{
			get{ return _checkAllAttachment;}
			set{
				_checkAllAttachment = value;
				if(_checkAllAttachment){
					foreach(var item in AllAttachmentCategories){
						item.IsChecked = _checkAllAttachment;
					}
				}else if(AllAttachmentCategories.Where(p=>p.IsChecked == true).Count() == AllAttachmentCategories.Count){
					foreach(var item in AllAttachmentCategories){
						item.IsChecked = false;
						item.IsScanned = false;
					}
				}
				RaisePropertyChanged (()=>CheckAllAttachment);
			}
		}
		private int _inspectionType;
		public int InspectionType
		{
			get{return _inspectionType; }
			set{_inspectionType = value; }
		}

		private string _inspectionDescription;
		public string InspectionDescription
		{
			get{return _inspectionDescription; }
			set{_inspectionDescription = value; }
		}

		private string _buttonDescription = "Add";
		public string ButtonDescription
		{
			get{return _buttonDescription; }
			set{_buttonDescription = value; RaisePropertyChanged (()=>ButtonDescription); }
		}

		private bool _isBusy;
		public bool IsBusy
		{
			get { return _isBusy; }
			set { _isBusy = value; RaisePropertyChanged(() => IsBusy); }
		}

		private bool _isNew;
		public bool IsNew
		{
			get { return _isNew; }
			set { _isNew = value; RaisePropertyChanged(() => IsNew); }
		}

		private int _reportID;
		public int ReportID
		{
			get{return _reportID; }
			set{_reportID = value;RaisePropertyChanged (()=>ReportID); }
		}

		private InspectionReportModel _reportModel;
		public InspectionReportModel ReportModel
		{
			get{return _reportModel; }
			set{_reportModel = value; }
		}

		private bool _enable;
		public bool Enable
		{
			get{return _enable; }
			set{_enable = value; RaisePropertyChanged (()=>Enable);}
		}

		private string errorMessage;
		public string ErrorMessage
		{
			get{return errorMessage; }
			set{errorMessage = value;RaisePropertyChanged (()=>ErrorMessage); }
		}

		private EmployeeModel _currentEmployee;
		public EmployeeModel CurrentEmployee
		{
			get{return _currentEmployee; }
			set{_currentEmployee = value; }
		}

		private string _scannedBarcode;
		public string ScannedBarcode
		{
			get { return _scannedBarcode; }
			set { 
				_scannedBarcode = value;
				RaisePropertyChanged(() => ScannedBarcode);
		}
	    }
		private bool isSuperVisor;
		public bool IsSuperVisor{ 
			get{return isSuperVisor; }
			set{
				isSuperVisor = value;
				RaisePropertyChanged (()=>IsSuperVisor); 
			}
		}

		public List<InspectionReportDefectModel> AttachmentDefects{ get; set;}
		public bool ShowDefect{ get; set;}
		public CategoryModelRow ScannedCategory{ get; set;}
		public string CheckedVechileCategory{ get; set;}
		public string CheckedAttachmentCategories{ get; set;}
		#endregion

		#region Events
		public event EventHandler ScanSuccess;
		protected virtual void OnScanSuccess(EventArgs e)
		{
			if (ScanSuccess != null)
			{
				ScanSuccess(this, e);
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

		public event EventHandler Success;
		protected virtual void OnSuccessMessage(EventArgs e)
		{
			if (Success != null)
			{
				Success(this, e);
			}
		}

		public event EventHandler SaveError;
		protected virtual void OnSaveError(EventArgs e)
		{
			if (SaveError != null)
			{				
				SaveError(this, e);
			}
		}

		public event EventHandler ShowSacn;
		protected virtual void OnShowSacn(EventArgs e)
		{
			if (ShowSacn != null)
			{				
				ShowSacn(this, e);
			}
		}

		public event EventHandler ShowDefectPopUp;
		protected virtual void OnShowDefectPopUp(EventArgs e)
		{
			if (ShowDefectPopUp != null)
			{				
				ShowDefectPopUp(this, e);
			}
		}

		public event EventHandler ShowRemoveAttachmentPopUp;
		protected virtual void OnShowRemoveAttachmentPopUp(EventArgs e)
		{
			if (ShowRemoveAttachmentPopUp != null)
			{				
				ShowRemoveAttachmentPopUp(this, e);
			}
		}

		public event EventHandler ShowclearDefectConfirmation;
		protected virtual void OnShowclearDefectConfirmation(EventArgs e)
		{
			if (ShowclearDefectConfirmation != null)
			{				
				ShowclearDefectConfirmation(this, e);
			}
		}

		public event EventHandler ShowbackConfirmation;
		protected virtual void OnShowbackConfirmation(EventArgs e)
		{
			if (ShowbackConfirmation != null)
			{				
				ShowbackConfirmation(this, e);
			}
		}
		#endregion

		#region Commands

		public ICommand ClickBack
		{
			get {
				return new MvxCommand (() => {
					OnShowbackConfirmation(new EventArgs());
				});
			}
		}

		public void CloseAndroid(){
			_messenger.Publish<InspectionCategoryMessage>(new InspectionCategoryMessage(this));
			unSubscribe();
			UnSubScribeFromBaseViewModel ();
			OnCloseView (new EventArgs ());
			this.Close(this);
		}
		public ICommand SelectVehcileCommand
		{
			get {
				return new MvxCommand<CategoryModelRow> ((category) => {
					var itemModel =  itemService.GetInspectionItemByCategoryId (category.CategoryId);
					if(itemModel != null && itemModel.Count > 0){
						var defects = string.Empty;
						if(itemModel.Count == 1 && itemModel[0].hasChildren == 0){
							if(Defects != null && Defects.Count > 0 && Defects.FirstOrDefault(p=>p.InspectionItemId == itemModel[0].InspectionItemId) !=null){
								defects =  Mvx.Resolve<IMvxJsonConverter>().SerializeObject(Defects.FirstOrDefault(p=>p.InspectionItemId == itemModel[0].InspectionItemId));	
							}
							ShowViewModel<InspectionDefectViewModel>(new {inspectionitemId = itemModel[0].InspectionItemId,Desc =!string.IsNullOrEmpty(itemModel[0].DefectAbbr) ? itemModel[0].DefectAbbr : itemModel[0].Defect,SerialiseDefects = defects,formEnable = Enable,attId="",issupervisor = IsSuperVisor,localId=ReportID});
						}else {
							defects = Defects != null && Defects.Count > 0   ? Mvx.Resolve<IMvxJsonConverter>().SerializeObject(Defects) : string.Empty;
							ShowViewModel<InspectionItemViewModel>(new {categoryId = category.CategoryId,localReportId = ReportID,SerialiseDefects = defects,formEnable = Enable,attId="",parentDesc=category.Description,typedesc=InspectionDescription,issupervisor = IsSuperVisor});
						}
					}
				});
			}
		}
		public ICommand SelectAttachmentCommand
		{
			get {
				return new MvxCommand<CategoryModelRow> ((category) => {
					var itemModel =  itemService.GetInspectionItemByCategoryId (category.CategoryId);
					var defects = string.Empty;
					if(itemModel.Count == 1 && itemModel[0].hasChildren == 0){
						if(Defects != null && Defects.Count > 0 && Defects.FirstOrDefault(p=>p.InspectionItemId == itemModel[0].InspectionItemId) != null){
							defects =  Mvx.Resolve<IMvxJsonConverter>().SerializeObject(Defects.FirstOrDefault(p=>p.InspectionItemId == itemModel[0].InspectionItemId));	
						}
						ShowViewModel<InspectionDefectViewModel>(new {inspectionitemId = itemModel[0].InspectionItemId,Desc = !string.IsNullOrEmpty(itemModel[0].DefectAbbr) ? itemModel[0].DefectAbbr : itemModel[0].Defect,SerialiseDefects = defects,formEnable = Enable,attId=AttachmentId,localId=ReportID});
					}else {
						defects = Defects != null && Defects.Count > 0 ? Mvx.Resolve<IMvxJsonConverter>().SerializeObject(Defects) : string.Empty;
						ShowViewModel<InspectionItemViewModel>(new {categoryId = category.CategoryId,localReportId = ReportID,SerialiseDefects = defects,formEnable = Enable,attId=AttachmentId,parentDesc=category.Description,typedesc=InspectionDescription});
					}
				});
			}
		}
		public ICommand AddAttachment
		{
			get {
				return new MvxCommand (() => {
					if(!HasAttachment){
						ShowViewModel<AddAttachmentViewModel>(new {isAttach=true});	
					}else{
						OnShowRemoveAttachmentPopUp(new EventArgs());
					}
				});
			}
		}

		public ICommand AddAttachmentIos
		{
			get {
				return new MvxCommand (() => {
					if(!HasAttachment){						
						ShowViewModel<SelectAssetViewModel>(new {isAttach=true});	
					}else{
						OnShowRemoveAttachmentPopUp(new EventArgs());
					}
				});
			}
		}
		public ICommand SelectAfterScan
		{
			get {
				return new MvxCommand (() => {
					if (!string.IsNullOrEmpty (ScannedBarcode)) {
							if(IsAttachmentList){
							ScannedCategory = 	AllAttachmentCategories.FirstOrDefault (p => p.BarCodeID == ScannedBarcode);
							if(ScannedCategory != null){
								AllAttachmentCategories.FirstOrDefault (p => p.BarCodeID == ScannedBarcode).IsScanned = true;
								AllAttachmentCategories.FirstOrDefault (p => p.BarCodeID == ScannedBarcode).IsChecked = true;
							}
							}else{
							ScannedCategory = AllVehcileCategory.FirstOrDefault (p => p.BarCodeID == ScannedBarcode);
							if(ScannedCategory != null){								
								AllVehcileCategory.FirstOrDefault (p => p.BarCodeID == ScannedBarcode).IsScanned = true;
								AllVehcileCategory.FirstOrDefault (p => p.BarCodeID == ScannedBarcode).IsChecked = true;
							}
						}
						if(ScannedCategory != null){
							ShowDefect = true;
							OnShowDefectPopUp(new EventArgs());
						}else{
							ShowDefect = false;
						}
					}
				});
			}
		}


		public ICommand CancelDefectPopUp
		{
			get {
				return new MvxCommand (() => {
					if(IsAttachmentList){
						var previousCount= AllAttachmentCategories.Where(p=>p.IsChecked == true).Count();
						var scanedAttachment = AllAttachmentCategories.FirstOrDefault(p=>p.BarCodeID == ScannedBarcode);
						if(scanedAttachment != null && scanedAttachment.IsScanned){
							AllAttachmentCategories.FirstOrDefault(p=>p.BarCodeID == ScannedBarcode).IsChecked = true;
						}
						if(AllAttachmentCategories.Where(p=>p.IsChecked == true).Count() == AllAttachmentCategories.Count){
							CheckAllAttachment = true;
						}else{
							CheckAllAttachment = false;
						}
					}else{
						var previousCount= AllVehcileCategory.Where(p=>p.IsChecked == true).Count();
						var scanedVechile = AllVehcileCategory.FirstOrDefault(p=>p.BarCodeID == ScannedBarcode);
						if(scanedVechile != null && scanedVechile.IsScanned){
							AllVehcileCategory.FirstOrDefault(p=>p.BarCodeID == ScannedBarcode).IsChecked = true;
						}
						if(AllVehcileCategory.Where(p=>p.IsChecked == true).Count() == AllAttachmentCategories.Count){
							CheckAllAttachment = true;
						}else{
							CheckAllAttachment = false;
						}
					}
					ScannedBarcode = string.Empty;
				});
			}
		}

		public ICommand NavigateAfterScan
		{
			get {
				return new MvxCommand (() => {
					var category = ScannedCategory;
					var itemModel =  itemService.GetInspectionItemByCategoryId (category.CategoryId);
					var defects = string.Empty;
					if(itemModel.Count == 1 && itemModel[0].hasChildren == 0){
						if(Defects != null && Defects.Count > 0 && Defects.FirstOrDefault(p=>p.InspectionItemId == itemModel[0].InspectionItemId) != null){
							defects =  Mvx.Resolve<IMvxJsonConverter>().SerializeObject(Defects.FirstOrDefault(p=>p.InspectionItemId == itemModel[0].InspectionItemId));	
						}
						ShowViewModel<InspectionDefectViewModel>(new {inspectionitemId = itemModel[0].InspectionItemId,Desc = itemModel[0].DefectAbbr,SerialiseDefects = defects,formEnable = Enable,attId=AttachmentId,localId=ReportID});
					}else {
						defects = Defects != null && Defects.Count > 0 ? Mvx.Resolve<IMvxJsonConverter>().SerializeObject(Defects) : string.Empty;
						ShowViewModel<InspectionItemViewModel>(new {categoryId = category.CategoryId,localReportId = ReportID,SerialiseDefects = defects,formEnable = Enable,attId=AttachmentId,parentDesc=category.Description,typedesc=InspectionDescription});
					}
				});
			}
		}

		public ICommand ScanCategories
		{
			get {
				return new MvxCommand (() => {
					OnShowSacn(new EventArgs());
				});
			}
		}
		public ICommand ClearDefects
		{
			get {
				return new MvxCommand (() => {
					OnShowclearDefectConfirmation(new EventArgs());
				});
			}
		}

		public ICommand SaveForms
		{
			get
			{
				return new MvxCommand (()=>{
					if(IsSuperVisor){
						SaveOrUpdateForm();
					}else{
						if(AllVehcileCategory.Where(p=>p.IsChecked == true).Count() == AllVehcileCategory.Count){
							if(HasAttachment && AllAttachmentCategories.Where(p=>p.IsChecked).Count() == AllAttachmentCategories.Count){							
								SaveOrUpdateForm();
							}else if(!HasAttachment){
								SaveOrUpdateForm();
							}else{							
								OnSaveError(new EventArgs());
							}
						}else{
							OnSaveError(new EventArgs());
						}	
					}
				});
			}	
		}

		public ICommand SaveFormsTablet
		{
			get
			{
				return new MvxCommand (()=>{
					if(AllVehcileCategory.Where(p=>p.IsChecked == true).Count() == AllVehcileCategory.Count){
						if(HasAttachment && AllAttachmentCategories.Where(p=>p.IsChecked).Count() == AllAttachmentCategories.Count){							
							SaveOrUpdateForm();
						}else if(!HasAttachment){							
							SaveOrUpdateForm();
						}else{							
							OnSaveError(new EventArgs());
						}
					}else{						
						OnSaveError(new EventArgs());
					}
				});
			}	
		}
		public ICommand InvertVehicle
		{
			get {
				return new MvxCommand(() => {
					if(IsVechicle){
						IsVechicle = false;
						if(!string.IsNullOrEmpty(AttachmentId)){
							IsAttachmentList = true;
						}
					}else{
						IsVechicle = true;
						if(!string.IsNullOrEmpty(AttachmentId)){
							IsAttachmentList = false;
						}
					}
				});
			}
		}
		public ICommand InvertAttachment
		{
			get {
				return new MvxCommand(() => {
					if(IsAttachmentList){
						IsAttachmentList = false;
						IsVechicle = true;
					}else {
						IsVechicle = false;
						IsAttachmentList = true;
					}
				});
			}
		}
		public ICommand ToggleSelectAllVehicle
		{
			get {
				return new MvxCommand(() => {
					CheckAllVehcile = !CheckAllVehcile;
				});
			}
		}
		public ICommand ToggleSelectAllAttachment
		{
			get {
				return new MvxCommand(() => {
					CheckAllAttachment = !CheckAllAttachment;
				});
			}
		}
		public ICommand GoBack
		{
			get {
				return new MvxCommand (() => {
					unSubscribe();
					OnCloseView(new EventArgs());
					Close(this);
				});
			}
		}

		#endregion

		#region unsubscribe
		public void unSubscribe() {
            //_messenger.Unsubscribe<LocationUpdated> (_locationupdated);
            _messenger.Unsubscribe<AttachmentUpdateMessage>(_attachmentUpdateMessage);
            _messenger.Unsubscribe<AttachmentSuccessMessage>(_attachmentMessage);
			_messenger.Unsubscribe<InspectionCategoryMessage> (_defectUpdateMessage);
		}
		#endregion


		void SaveOrUpdateForm()
		{
			var model = new InspectionReportModel();
			model.Id = ReportID;
			model.DriverId = _dataService.GetCurrentDriverId();
			model.DriverName = CurrentEmployee.DriverName;
			model.BoxID = _dataService.GetAssetBoxId();
			model.EquipmentID = _dataService.GetAssetBoxDescription();
			model.CheckedCategoryIds = string.Empty;
			model.AttachmentCheckedCategoryIds = string.Empty;
			model.HaveSent = false;
			model.ModifiedDate = Util.GetDateTimeNow();
			model.StartReportTime = Util.GetDateTimeNow ();
			model.attID = AttachmentId;
			model.Longitude = _dataService.GetLongitude();
			model.Latitude = _dataService.GetLatitude();
			//Also if odo is inputed or requested from ECM set it for this inspection report
			var tmpOdo = 0;
			if (Odometer > 0 && int.TryParse (Odometer.ToString(), out tmpOdo)) {
				tmpOdo = b_miles ? int.Parse (Math.Round (1.60934 * tmpOdo, 0).ToString ()) : tmpOdo;
				model.Odometer = tmpOdo;
			} else {
				model.Odometer = Odometer;
			}
			model.InspectionType = InspectionType;
			model.IsFromServer = false;

			if (CurrentEmployee != null && !string.IsNullOrEmpty (CurrentEmployee.TimeZone)) {
				model.TimeZone = CurrentEmployee.TimeZone;
				model.DayLightSaving = CurrentEmployee.DayLightSaving;
			}
			else {
				model.TimeZone = TimeZoneInfo.Local != null ? TimeZoneInfo.Local.BaseUtcOffset.TotalHours.ToString () : "-5.0";
				model.DayLightSaving = DateTime.Now.IsDaylightSavingTime ();
			}
			model.AppVersion = Util.getAppVersion();

			foreach(var category in AllVehcileCategory){
				if(category.IsScanned){
					model.CheckedCategoryIds += model.CheckedCategoryIds.Length == 0 ? "+" + category.CategoryId.ToString () : (",+" + category.CategoryId.ToString ());
				}else if(category.IsChecked){
					model.CheckedCategoryIds += model.CheckedCategoryIds.Length == 0 ? category.CategoryId.ToString() : ("," +category.CategoryId.ToString());
				}
			}
			if (CurrentEmployee.Signature != null && CurrentEmployee.Signature.Length > 2) {
				model.Signed = 1;
			}
			if (!string.IsNullOrEmpty (AttachmentId)) {
				foreach(var attachment in AllAttachmentCategories){
					 if(attachment.IsScanned){
						model.AttachmentCheckedCategoryIds += model.AttachmentCheckedCategoryIds.Length == 0 ? "+"+attachment.CategoryId.ToString() : (",+" +attachment.CategoryId.ToString());
					}else if(attachment.IsChecked){
						model.AttachmentCheckedCategoryIds += model.AttachmentCheckedCategoryIds.Length == 0 ? attachment.CategoryId.ToString() : ("," +attachment.CategoryId.ToString());
					}
				}	
			} else {
				model.AttachmentCheckedCategoryIds = string.Empty;
			}
			if (model.Signed == 1) {
				if (model.Id == 0) {					
					model.InspectionTime = Util.GetDateTimeNow ().ToUniversalTime();
					_report.Insert (model);
					ReportID = model.Id;
					if (Defects.Count > 0) {
						foreach (var defect in Defects) {
							defect.InspectionReportId = model.Id;
						}
						_defectService.InsertDefectList (Defects.ToList<InspectionReportDefectModel> ());
					}
				} else {
					model.InspectionTime = ReportModel.InspectionTime;
					_report.Update (model);
					if (AttachmentDefects != null && AttachmentDefects.Count > 0) {
						_defectService.DeleteDefects (AttachmentDefects);
					}
					if (Defects != null && Defects.Count > 0) {
						foreach (var defect in Defects) {
							defect.InspectionReportId = model.Id;
						}
						_defectService.InsertDefectList (Defects.ToList<InspectionReportDefectModel> (),false);
					}
				}
				_syncService.runTimerCallBackNow ();
				_dataService.ClearPersistedAttachmentId ();
				_dataService.ClearPersistedAttachmentDescription ();
				//AttachmentId = string.Empty;
				//AttachmentDescription = "";
				OnSuccessMessage (new EventArgs ());
				_messenger.Publish<InspectionCategoryMessage> (new InspectionCategoryMessage (this));	
			} else {
				ErrorMessage = "Add Sign";
				OnSaveError(new EventArgs());
			}
		}
		void checkForDefects(List<CategoryModelRow> allCatrgoryList,ObservableCollection<InspectionReportDefectModel> defects,string attId="")
		{
			foreach(var category  in allCatrgoryList)
			{
				var itemModel =  itemService.GetInspectionItemByCategoryId (category.CategoryId,attId);
				if(itemModel != null && itemModel.Count > 0){
					var hasDefect = recursiveCheckInDefects (defects,itemModel);
					category.BsmAddOn_HasDefect = hasDefect;
					if(hasDefect){
						category.SetStrikeThrough = recursiveCheckInDefectsAllCleared (defects,itemModel,true);
					}
				}
			}
			if (!string.IsNullOrEmpty (attId)) {
				AllAttachmentCategories = allCatrgoryList;	
			} else {
				AllVehcileCategory = allCatrgoryList;
			}
		}
		void AddAttachmentCategories(string attachmentId){
			HasAttachment = true;
			ButtonDescription = "Remove";
			AllAttachmentCategories = ConvertModel(_catService.GetAllCategoryDetails(Convert.ToInt32(attachmentId)));
			if(!string.IsNullOrEmpty(CheckedAttachmentCategories)){
				var checkedAttachment = CheckedAttachmentCategories.Split (',').ToList<string>();
				if(AllAttachmentCategories != null && AllAttachmentCategories.Count >0 && checkedAttachment != null && checkedAttachment.Count > 0){
					foreach(var attachment in AllAttachmentCategories){
						if (checkedAttachment.Contains ("+" + attachment.CategoryId.ToString ())) {
							attachment.IsScanned = true;
							attachment.IsChecked = true;
						}
						else if (checkedAttachment.Contains (attachment.CategoryId.ToString ()))
							attachment.IsChecked = true;
					}
				}
			}
			if(AllAttachmentCategories.Where(p=>p.IsChecked == true).Count() == AllAttachmentCategories.Count){
				CheckAllAttachment = true;
			}												
			var _model = _assetService.GetAssetByBoxId (int.Parse(AttachmentId));
			AttachmentDescription = _model.AssetDescription;
			checkForDefects(AllAttachmentCategories,Defects,AttachmentId);
			InvertAttachment.Execute (null);
		}

		private async void EditReport(){
			IsNew = false;
			ReportModel = _report.GetReport (ReportID);
			CheckedVechileCategory = ReportModel.CheckedCategoryIds;
			CheckedAttachmentCategories = ReportModel.AttachmentCheckedCategoryIds;
			AllVehcileCategory =ConvertModel(_catService.GetAllCategoryDetails(_dataService.GetAssetBoxId()));
			if (!string.IsNullOrEmpty (CheckedVechileCategory)) {
				var CheckedList = CheckedVechileCategory.Split (',').ToList<string> ();
				foreach (var itemcategory in AllVehcileCategory) {
					if (CheckedList.Contains ("+" + itemcategory.CategoryId.ToString ())) {
						itemcategory.IsScanned = true;
						itemcategory.IsChecked = true;
					}
					else if (CheckedList.Contains (itemcategory.CategoryId.ToString ()))
						itemcategory.IsChecked = true;
				}
			}
			if(AllVehcileCategory.Where(p=>p.IsChecked).Count() == AllVehcileCategory.Count){
				CheckAllVehcile = true;
			}
			Defects = new  ObservableCollection<InspectionReportDefectModel>(_defectService.GetDeffectList(ReportID));
			checkForDefects(AllVehcileCategory,Defects);
			Odometer = ReportModel.Odometer;
			if (!string.IsNullOrEmpty (AttachmentId)) {
				var catGroup = _catService.GetCatGroup (Convert.ToInt32 (AttachmentId));
				if (catGroup == "0") {
					_dataService.PersistAttachmentId (AttachmentId);
					IsBusy = true;
					var datetime=_assetService.GetCatLastUpdate (Convert.ToInt32 (AttachmentId));
					bool isAck = await _communicationService.SyncUser4Att (AttachmentId,Convert.ToInt32(_dataService.GetCurrentDriverId()));
					if (isAck) {
						AddAttachmentCategories(AttachmentId);
					}
					IsBusy = false;
				} else {
					AddAttachmentCategories (AttachmentId);
				}
			}
		}
		void NewReport()
		{
			IsNew = true;
			IsAttachmentList = false;
			AllVehcileCategory = ConvertModel(_catService.GetAllCategoryDetails(_dataService.GetAssetBoxId()));
		}
		List<CategoryModelRow> ConvertModel(List<CategoryModel> lstItems,bool Checked = false){		
			var lstcategoryRow = new List<CategoryModelRow> ();
			if(lstItems != null && lstItems.Count > 0){
				foreach(var item in lstItems){
					var row = new CategoryModelRow (this);
					row.CategoryId = item.CategoryId;
					row.Description = item.Description;
					row.GroupID = item.GroupID;
					row.LngCode = item.LngCode;
					row.Location = item.Location;
					row.BarCodeID = item.BarCodeID;
					row.attID = item.attID;
					row.IsChecked = Checked;
					row.HasBarcode = string.IsNullOrEmpty (item.BarCodeID) ? false : true;
					/* They are not checking in the main level
					var itemModel = itemService.GetInspectionItemByCategoryId (item.CategoryId);
					if(itemModel != null && itemModel.Count > 0){						
						if(itemModel.Count == 1 && itemModel[0].hasChildren == 0){
							row.HasNoSubItems = false;
						}else {
							row.HasNoSubItems = true;	
						}
					}*/
					lstcategoryRow.Add (row);
				}
			}
			return lstcategoryRow;
		}
		void CheckIsSuperVisorAndFormEnable(){
			IsSuperVisor = CurrentEmployee.IsSupervisor;
			if (IsSuperVisor || ReportID == 0) {
				Enable = true;
			} else if (ReportID != 0) {
				if (ReportModel.DriverId == _dataService.GetCurrentDriverId () && ReportModel.InspectionTime >= Util.GetDateTimeNow ().AddHours (-24)) {
					Enable = true;
				} else {
					Enable = false;
				}
			}
		}

		public void reloadAfterSave(){
			IsNew = false;
			ReportModel = _report.GetReport (ReportID);
			CheckedVechileCategory = ReportModel.CheckedCategoryIds;
			CheckedAttachmentCategories = ReportModel.AttachmentCheckedCategoryIds;
			Odometer = ReportModel.Odometer;
			// Update the Defect with inspectionReportID
			if(Defects != null && Defects.Count > 0){
				foreach(var defect in Defects){
					defect.InspectionReportId = ReportModel.Id;
				}
			}
		}

		public void RemoveAttachment(){
			HasAttachment = false;
			_dataService.ClearPersistedAttachmentId();
			_dataService.ClearPersistedAttachmentDescription();
			if(Defects != null && Defects.Count > 0 ){
				AttachmentDefects = Defects.Where(p=>p.attID == AttachmentId).ToList();
				if(AttachmentDefects != null && AttachmentDefects.Count > 0){
					foreach(var defect in AttachmentDefects){
						Defects.Remove(defect);
					}
				}
			}
			AttachmentId = string.Empty;
			AttachmentDescription = "";
			ButtonDescription = "Add";
			IsVechicle = true;
			_isAttachmentList = false;
		}

		public void ClearAllDefects(){
			if(Defects.Count > 0){
				if(ReportID ==0){
					// Remove the Local Saved Images
					var fileStore = Mvx.Resolve<IMvxFileStore>();
					if(Defects != null && Defects.Count > 0){
						foreach(var defect in Defects){
							if(!string.IsNullOrEmpty(defect.MFILES)){
								var files = defect.MFILES.Split(',');
								foreach(var file in files){
									if(fileStore.Exists(fileStore.NativePath(file))){
										fileStore.DeleteFile(fileStore.NativePath(file));
									}		
								}
							}
						}
						Defects.Clear();
					}

				}else{
					foreach(var defect in Defects){
						defect.ClrDriverID = _dataService.GetCurrentDriverId();
						defect.ClrDriverName = DriverName;
						defect.Clr = true;
					}
				}
				checkForDefects (AllVehcileCategory,Defects);
				if(!string.IsNullOrEmpty(AttachmentId)){
					checkForDefects(AllAttachmentCategories,Defects,AttachmentId);	
				}
			}
		}
		public void ChangeImage(CategoryModelRow model){

			if (IsAttachmentList) {
				if (model.IsChecked) {
					model.IsChecked = false;	
					model.IsScanned = false;
                    // Changed to fix the bug
                    CheckAllAttachment = false;
				} else {
					model.IsChecked = true;
					AllAttachmentCategories.FirstOrDefault (p => p.CategoryId == model.CategoryId).IsChecked = true;
					var checkedCount = AllAttachmentCategories.Where (p => p.IsChecked || p.IsScanned).Count ();
					if(checkedCount == AllAttachmentCategories.Count){
						CheckAllAttachment = true;
					}
				}
			} else {
				if (model.IsChecked) {
					model.IsChecked = false;	
					model.IsScanned = false;
					CheckAllVehcile = false;
				} else {
					model.IsChecked = true;
					AllVehcileCategory.FirstOrDefault (p => p.CategoryId == model.CategoryId).IsChecked = true;
					var checkedCount = AllVehcileCategory.Where (p => p.IsChecked || p.IsScanned).Count ();
					if(checkedCount == AllVehcileCategory.Count){
						CheckAllVehcile = true;
					}
				}
			}
		}

		private bool recursiveCheckInDefects(ObservableCollection<InspectionReportDefectModel> defectList, List<InspectionItemModel> inspItem){
			bool rtnVal = false;
			if (defectList != null && inspItem != null) {
				foreach(var ii in inspItem){
					if(ii.hasChildren == 1)
						rtnVal = rtnVal || recursiveCheckInDefects(defectList, itemService.GetInspectionItemChildren(ii.InspectionItemId, ii.attID));
					if(rtnVal)
						return true;
					var defectModel = defectList.FirstOrDefault (p=>p.InspectionItemId == ii.InspectionItemId && p.attID == ii.attID);
					if(defectModel != null){
						return true;
					}						
				}
			}
			return rtnVal;
		}

		private bool recursiveCheckInDefectsAllCleared(ObservableCollection<InspectionReportDefectModel> defectList, List<InspectionItemModel> inspItem, bool curBoolVal){
			if (defectList != null && inspItem != null) {
				foreach(var iitem in inspItem){
					if (iitem.hasChildren == 1)
						curBoolVal = curBoolVal && recursiveCheckInDefectsAllCleared (defectList, itemService.GetInspectionItemChildren (iitem.InspectionItemId, iitem.attID), curBoolVal);
					else {
						var defectModel = defectList.FirstOrDefault (p=>p.InspectionItemId == iitem.InspectionItemId && p.attID == iitem.attID);
						if(defectModel != null){
							curBoolVal = curBoolVal && defectModel.Clr;
						}
					}
				}
			}
			return curBoolVal;
		}
	}
}
