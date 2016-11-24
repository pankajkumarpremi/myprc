using System;
using System.Linq;
using MvvmCross.Plugins.Messenger;
using BSM.Core.Messages;
using System.Collections.Generic;
using System.Windows.Input;
using MvvmCross.Core.ViewModels;
using BSM.Core.Services;

using MvvmCross.Platform;
using MvvmCross.Plugins.Json;
using BSM.Core.ConnectionLibrary;
using MvvmCross.Plugins.PictureChooser;
using MvvmCross.Plugins.File;
using System.IO;
using System.Collections.ObjectModel;
using MvvmCross.Platform.Platform;

namespace BSM.Core.ViewModels
{
	public class InspectionDefectViewModel : BaseViewModel
	{
		public void Init(string SerialiseDefects,string Desc,bool formEnable,int inspectionitemId,string attId,int localId,bool issupervisor = false){
			ReportId = localId;
			Attachment = attId;
			IsSuperVisor = issupervisor;
			if(!string.IsNullOrEmpty(SerialiseDefects)){
				Defect =Mvx.Resolve<IMvxJsonConverter>().DeserializeObject<InspectionReportDefectModel>(SerialiseDefects);
			}
			_comments = Defect != null ? Defect.Comments : string.Empty;
			ItemDescription = Desc;
			FormEnable =issupervisor || formEnable;

			ItemId = inspectionitemId;
			if(Defect != null && !string.IsNullOrEmpty(Defect.MFILES)){
				var media_files = Defect.MFILES;
				var filenames = media_files.Split (',');
				if (filenames != null && filenames.Count () > 0) {
					foreach (var file in filenames) {
						var photoModel = new PhotoModel (this);
						photoModel.PhotoName = file;
						photoModel.PhotoPath = _filesystem.NativePath (file);
						PhotoList.Add (photoModel);
					}
				}
			}
		}
		#region Member Variables
		private readonly IDataService _dataService;
		private readonly IInspectionReportDefectService defectService;
		private readonly IMvxMessenger _messenger;
		private readonly ISettingsService _settings;
		private readonly IMvxFileStore _filesystem;
		private readonly MvxSubscriptionToken _photoSubscribe;
		private readonly IPhotoService _photoservice;
		#endregion

		#region ctors
		public InspectionDefectViewModel(IInspectionReportDefectService _defectService,IMvxMessenger messenger,IDataService dataService,ISettingsService settings,IMvxFileStore filesysstem,IPhotoService photoservice)
		{
			Mvx.RegisterType<IMvxJsonConverter, MvxJsonConverter>();
			_photoservice = photoservice;
			_filesystem = filesysstem;
			_dataService = dataService;
			defectService = _defectService;	
			_messenger = messenger;
			_settings = settings;
			var imageSettings = _settings.GetSettingsByName (Constants.SETTINGS_IMAGE_LIMIT);
			if(imageSettings != null){
				ImageLimit =Convert.ToInt16(imageSettings.SettingsValue);
			}
			_photoSubscribe = _messenger.Subscribe<PictureAvailbleMessage>((message) =>
				{
					ProcessPicture(message.imageData);
				});
		}

		#endregion

		#region Properties
		public int ReportId{ get; set;}

		private string _comments;
		public string Comments
		{
			get{return _comments; }
			set{_comments = value;RaisePropertyChanged (()=>Comments); }
		}

		private string _itemDescription;
		public string ItemDescription
		{
			get{ return _itemDescription;}
			set{_itemDescription = value;RaisePropertyChanged (()=>ItemDescription); }
		}

		private InspectionReportDefectModel _defect ;
		public InspectionReportDefectModel Defect
		{
			get{return _defect; }
			set{_defect = value;RaisePropertyChanged (()=>Defect); }
		}

		public bool _formEnable;
		public bool FormEnable
		{
			get{return _formEnable; }
			set{_formEnable = value;RaisePropertyChanged (()=>FormEnable); }
		}

		private ObservableCollection<PhotoModel> _photoList = new ObservableCollection<PhotoModel>();
		public ObservableCollection<PhotoModel> PhotoList{
			get{return _photoList; }
			set{_photoList = value;RaisePropertyChanged (()=>PhotoList); }
		}

		private bool showPicture;
		public bool ShowPicture
		{
			get{return showPicture; }
			set{showPicture = value; RaisePropertyChanged (()=>ShowPicture); }
		}

		private bool thresholdLimit;
		public bool ThresholdLimit
		{
			get{return thresholdLimit; }
			set{thresholdLimit = value; RaisePropertyChanged (()=>ThresholdLimit); }
		}

		public PhotoModel SelectedPhotoModel{ get; set;}
		public bool IsSuperVisor{ get; set;}

	
		public int ItemId{ get; set;}
		public string Attachment{ get; set;}
		public int ImageLimit{ get; set;}
		#endregion 

		#region Commands
		public ICommand SaveDefects
		{
			get {
				return new MvxCommand (() => {

					if(Defect != null && string.IsNullOrEmpty(Comments) && (PhotoList != null && PhotoList.Count == 0)){
						// We Have to remove the defect
						_messenger.Publish<DefectUpdateMessage>(new DefectUpdateMessage(this,Defect,true));
					}else if(!string.IsNullOrEmpty(Comments) || (PhotoList != null && PhotoList.Count > 0)){
						// then only save the defect
						if(Defect == null ){
							Defect = new InspectionReportDefectModel();
						}
						Defect.Comments = Comments;
						Defect.Clr = Defect.Clr ? Defect.Clr : false;						
						Defect.InspectionItemId = ItemId;
						Defect.attID = Attachment;
						Defect.MFILES =PhotoList != null && PhotoList.Count > 0 ? string.Join(",",PhotoList.Where(p=>p.PhotoName != null && p.PhotoName != "").Select(p=>p.PhotoName).ToList()) : string.Empty;
						_messenger.Publish<DefectUpdateMessage>(new DefectUpdateMessage(this,Defect));	
					}				
					OnCloseView(new EventArgs());
				});
			}
		}
		public ICommand ClearDefects
		{
			get
			{
				return new MvxCommand (()=>{
					if(Defect != null){
						var currentEmployee = EmployeeDetail();
						Defect.Comments = Comments;
						Defect.Clr=true;
						Defect.InspectionItemId = ItemId;
						Defect.ClrDriverID = currentEmployee.Id;
						Defect.ClrDriverName = currentEmployee.DriverName;
						Defect.attID = Attachment;
						Defect.MFILES =PhotoList != null && PhotoList.Count > 0 ? string.Join(",",PhotoList.Where(p=>p.PhotoName != null && p.PhotoName != "").Select(p=>p.PhotoName).ToList()) : string.Empty;
						_messenger.Publish<DefectUpdateMessage>(new DefectUpdateMessage(this,Defect));
					}
						OnCloseView(new EventArgs());
				});
			}
		}
		public ICommand GoBack
		{
			get {
				return new MvxCommand (() => {
					UnSubscribe();
					OnCloseView(new EventArgs());
					Close(this);
				});
			}
		}

		public ICommand GoBackAndroid
		{
			get {
				return new MvxCommand (() => {
					Close(this);
				});
			}
		}

		public void UnSubscribe(){
			_messenger.Unsubscribe<PictureAvailbleMessage> (_photoSubscribe);
		}


		public ICommand OpenGallery
		{
			get {
				return new MvxCommand (async () => {					
					if(PhotoList.Count < ImageLimit && FormEnable){
						try{
							var _pictureChooser = Mvx.Resolve<IMvxPictureChooserTask>();
							var result = await _pictureChooser.ChoosePictureFromLibrary(300,95);
							ProcessPicture(result);
						}catch(Exception exp){
							Mvx.Trace (MvxTraceLevel.Error,"While Opening Gallery"+exp.ToString());
						}
					}else{
						ThresholdLimit = true;
					}
				});
			}
		}


		public ICommand TakePictureAndroid{
			get {
				return new MvxCommand (() => {
					if(PhotoList.Count < ImageLimit && FormEnable){
						OnOpenCameraActivity(new EventArgs());
					}else{
						ThresholdLimit = true;
					}
				});
			}
		}

		public ICommand TakePicture
		{
			get {
				return new MvxCommand (async () => {
					if(PhotoList.Count < ImageLimit && FormEnable){
						try{
							var _pictureChooser = Mvx.Resolve<IMvxPictureChooserTask>();
							var result = await _pictureChooser.TakePictureAsync(300,95);
							ProcessPicture(result);
						}catch(Exception ex1){
							Mvx.Trace (MvxTraceLevel.Error,"While Capturing Image"+ex1.ToString());
						}
						// _photoservice.GetPhoto();
					}else{
						ThresholdLimit = true;
					}
				});
			}
		}

		public void ViewPhoto(PhotoModel model){
			if(model != null && FormEnable){
				SelectedPhotoModel = model;
				ShowPicture = true;
			}
		}


		public void RemovePhoto(PhotoModel model){
			if(model != null && FormEnable){
				var modelToRemove = PhotoList.FirstOrDefault (p=>p.PhotoName == model.PhotoName);
				if(modelToRemove != null){
					PhotoList.Remove (modelToRemove);
				}
			}
		}
		public	void ProcessPicture(Stream stream){			
			if(stream != null ){
				var filename = String.Format ("sentinelPhoto_{0}.jpg", Guid.NewGuid ());
				var memoryStream = new MemoryStream();
				stream.CopyTo(memoryStream);
				var bytes = memoryStream.ToArray();
				try{
					_filesystem.WriteFile(_filesystem.NativePath(filename),bytes);
					var photoModel = new PhotoModel(this);
					photoModel.PhotoName = filename;
					photoModel.PhotoPath = _filesystem.NativePath(filename);
					PhotoList.Add(photoModel);
				}catch(Exception exp){
					Mvx.Trace (MvxTraceLevel.Error,"While Processing Image"+exp.ToString());
				}
			}
		}

		public	void ProcessPictureAndroid(byte[] stream){			
			if(stream != null ){
				var filename = String.Format ("sentinelPhoto_{0}.jpg", Guid.NewGuid ());
				try{
					_filesystem.WriteFile(_filesystem.NativePath(filename),stream);
					var photoModel = new PhotoModel(this);
					photoModel.PhotoName = filename;
					photoModel.PhotoPath = _filesystem.NativePath(filename);
					PhotoList.Add(photoModel);
				}catch(Exception exp){
					Mvx.Trace (MvxTraceLevel.Error,"While Processing Image"+exp.ToString());
				}
			}
		}
		#endregion

		#region Events
		public event EventHandler CloseView;
		protected virtual void OnCloseView(EventArgs e)
		{
			if (CloseView != null)
			{
				CloseView(this, e);
			}
		}

		public event EventHandler OpenCameraActivity;
		protected virtual void OnOpenCameraActivity(EventArgs e)
		{
			if (OpenCameraActivity != null)
			{
				OpenCameraActivity(this, e);
			}
		}
		#endregion
	}
}

