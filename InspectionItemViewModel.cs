using System;
using System.Collections.Generic;
using MvvmCross.Core.ViewModels;
using System.Linq;
using MvvmCross.Plugins.Messenger;
using BSM.Core.Messages;
using System.Collections.ObjectModel;

using MvvmCross.Platform;
using MvvmCross.Plugins.Json;
using System.Windows.Input;
using MvvmCross.Platform.Platform;

namespace BSM.Core.ViewModels
{
	public class InspectionItemViewModel : BaseViewModel
	{
		private readonly IInspectionItemService itemService;
		private readonly IInspectionReportDefectService _defectService;
		private readonly IMvxMessenger _messenger;
		private MvxSubscriptionToken _defectUpdateMessage;
		private readonly ICategoryService _catservice;
		public void Init(string SerialiseDefects,int categoryId,int localReportId,bool formEnable,string attId,string parentDesc = "",string typedesc="",bool issupervisor = false){
			Attachment = attId;
			CategoryId = categoryId;
			_localReportId = localReportId;
			if(!string.IsNullOrEmpty(SerialiseDefects)){
				Defects =   Mvx.Resolve<IMvxJsonConverter>().DeserializeObject<ObservableCollection<InspectionReportDefectModel>>(SerialiseDefects);	
			}
			TypeDescription = typedesc;
			checkMajorDefects (itemService.GetInspectionItemByCategoryId (categoryId, attId),Defects);
			FormEnable = formEnable;
			ParentDescription = parentDesc;
			IsSuperViosor = issupervisor;
		}
		#region ctors
		public InspectionItemViewModel (IInspectionItemService _itemservice,IInspectionReportDefectService defectService,IMvxMessenger messenger,ICategoryService catservice)
		{
			Mvx.RegisterType<IMvxJsonConverter, MvxJsonConverter>();
			itemService = _itemservice;
			_defectService = defectService;
			_catservice = catservice;
			_messenger = messenger;
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
					checkMajorDefects (InspectionItems,Defects);
				});
		}
		#endregion

		#region properties
		private List<InspectionItemModel> _inspectionItems = new List<InspectionItemModel> ();
		public List<InspectionItemModel> InspectionItems
		{
			get{return _inspectionItems;}
			set{
				_inspectionItems = value;
				RaisePropertyChanged (()=>InspectionItems);
			}
		}

		private InspectionItemModel _selectedModel = new InspectionItemModel();
		public InspectionItemModel selectedModel
		{
			get{return _selectedModel; }
			set{_selectedModel = value;RaisePropertyChanged (()=>selectedModel);}
		}


		private int _localReportId;
		public int LocalReportId
		{
			get{return _localReportId; }
		}

		private bool _formEnable;
		public bool FormEnable
		{
			get{return _formEnable; }
			set{_formEnable = value;RaisePropertyChanged (()=>FormEnable); }
		}

		private int odometer;
		public int Odometer{
			get{return odometer; }
			set{odometer = value;RaisePropertyChanged (()=>Odometer); }
		}

		public bool IsSuperViosor{ get; set;}

		public string Attachment{ get; set;}
		public string TypeDescription{ get; set;}
		public string ParentDescription{ get; set;}
		public int ParentId{ get; set;}
		public int CategoryId{ get; set;}
		public int NextLevel{ get; set;}

		public int ItemId{ get; set;}

		public event EventHandler PopPrevious;
		protected virtual void OnPopPrevious(EventArgs e)
		{
			if (PopPrevious != null)
			{				
				PopPrevious(this, e);
			}
		}
		#endregion

		#region ctors
		public IMvxCommand ShowDefects
		{
			get
			{
				return new MvxCommand <InspectionItemModel>((modelfromui)=>{
					if(modelfromui != null && modelfromui.hasChildren == 1){
						NextLevel += 1;
						ParentId = modelfromui.InspectionItemId;
						ParentDescription = modelfromui.Defect;
						if(InspectionItems.Count > 0){
							checkMajorDefects (itemService.GetInspectionItemChildren (ParentId),Defects);
						}
					}else{
						var defectModel = Defects.FirstOrDefault(i=>i.InspectionItemId == modelfromui.InspectionItemId && i.attID == Attachment);
						var serialiseString =defectModel != null ? Mvx.Resolve<IMvxJsonConverter>().SerializeObject(defectModel) : string.Empty;
						if(FormEnable){
							if(defectModel == null){
								defectModel = new InspectionReportDefectModel();
								defectModel.InspectionItemId = modelfromui.InspectionItemId;
								defectModel.Comments ="";
								defectModel.attID = Attachment;
							}
							ShowViewModel<InspectionDefectViewModel>(new {SerialiseDefects =serialiseString,Desc =!string.IsNullOrEmpty(modelfromui.DefectAbbr) ? modelfromui.DefectAbbr : modelfromui.Defect ,inspectionitemId = modelfromui.InspectionItemId,formEnable = FormEnable,attId=Attachment,issupervisor = IsSuperViosor,localId=LocalReportId});
						}else{
							if(defectModel != null){
								ShowViewModel<InspectionDefectViewModel>(new {SerialiseDefects =serialiseString,Desc = !string.IsNullOrEmpty(modelfromui.DefectAbbr) ? modelfromui.DefectAbbr : modelfromui.Defect,formEnable = FormEnable,inspectionitemId = modelfromui.InspectionItemId,attId=Attachment,issupervisor = IsSuperViosor,localId = LocalReportId});
							}
						}
					}
				});
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

		public ICommand GoBack
		{
			get {
				return new MvxCommand (() => {
					OnCloseView(new EventArgs());
					Close(this);
					_messenger.Unsubscribe<DefectUpdateMessage>(_defectUpdateMessage);
				});
			}
		}
		public IMvxCommand ShowPrevParent
		{
			get
			{
				return new MvxCommand (()=>{
					NextLevel -= 1;
					if(NextLevel > 0){						
						if(InspectionItems.Count > 0)
							checkMajorDefects(new List<InspectionItemModel>(itemService.GetInspectionItemChildren (ParentId)),Defects);
					}else if(NextLevel == 0){						
						if(InspectionItems.Count > 0)
							checkMajorDefects(itemService.GetInspectionItemByCategoryId (CategoryId),Defects);
					}else{
						OnPopPrevious(new EventArgs());
					}
				});
			}
		}
		#endregion
		void checkMajorDefects (List<InspectionItemModel> lstItems,ObservableCollection<InspectionReportDefectModel> DefectsList){
			foreach(var item in lstItems){
				if (item.hasChildren == 1) {
					var childrenItems = itemService.GetInspectionItemChildren (item.InspectionItemId,Attachment);
					if (childrenItems != null && childrenItems.Count > 0) {
						var hasDefect = recursiveCheckInDefects (DefectsList, childrenItems);
						item.hasMajorDefect = hasDefect;
						if (hasDefect) {
							item.SetStrikeThrough = recursiveCheckInDefectsAllCleared (DefectsList, childrenItems, true);
							var defectModel = Defects.FirstOrDefault (i => i.InspectionItemId == ItemId && i.attID == Attachment);
							if (defectModel != null) {
								if (defectModel.Clr) {
									var defectDesc = item.Defect;
									if (item.Defect.Contains ("Cleared")) {
										defectDesc = defectDesc.Substring (0, defectDesc.IndexOf ("Cleared") - 2);
										defectDesc = defectDesc + "( Cleared By" + defectModel.ClrDriverName + " )";
									} else {
										defectDesc = defectDesc + "( Cleared By" + defectModel.ClrDriverName + " )";
									}
									item.Defect = defectDesc;
								}
								item.IsAttachmentAvailble = !string.IsNullOrEmpty (defectModel.MFILES) ? true : false;
							}
						}
					} else {
						item.hasMajorDefect = false;
						item.SetStrikeThrough = false;
						item.IsAttachmentAvailble = false;
						if (item.Defect.Contains ("Cleared By")) {
							var defectDesc1 = item.Defect;
							defectDesc1 = defectDesc1.Substring (0, defectDesc1.IndexOf ("Cleared") - 2);
							item.Defect = defectDesc1;
						}
					}
				} else {
					var modelfromdefects = Defects.FirstOrDefault (i => i.InspectionItemId == item.InspectionItemId && i.attID == Attachment);
					if (modelfromdefects != null) {
						item.hasMajorDefect = true;
						item.SetStrikeThrough = modelfromdefects.Clr;
						item.IsAttachmentAvailble = !string.IsNullOrEmpty (modelfromdefects.MFILES) ? true : false;
						if (modelfromdefects.Clr) {
							var defectDesc = item.Defect;
							if (item.Defect.Contains ("Cleared")) {
								defectDesc = defectDesc.Substring (0, defectDesc.IndexOf ("Cleared") - 2);
								defectDesc = defectDesc + "( Cleared By " + modelfromdefects.ClrDriverName + " )";
							} else {
								defectDesc = defectDesc + "( Cleared By " + modelfromdefects.ClrDriverName + " )";
							}
							item.Defect = defectDesc;
						}
					} else {
						item.hasMajorDefect = false;
						item.SetStrikeThrough = false;
						item.IsAttachmentAvailble = false;
						if (item.Defect.Contains ("Cleared By")) {
							var defectDesc1 = item.Defect;
							defectDesc1 = defectDesc1.Substring (0, defectDesc1.IndexOf ("Cleared") - 2);
							item.Defect = defectDesc1;
						}
					}
				}
			}
			InspectionItems = new List<InspectionItemModel> ();
			InspectionItems =new List<InspectionItemModel>(lstItems);
		}

		public void UnSubScribe(){
			_messenger.Unsubscribe<DefectUpdateMessage>(_defectUpdateMessage);
		}

		private bool recursiveCheckInDefects(ObservableCollection<InspectionReportDefectModel> defectList, List<InspectionItemModel> inspItem){
			bool rtnVal = false;
			if (defectList != null && inspItem != null) {
				foreach(var ii in inspItem){					
					var defectModel = defectList.FirstOrDefault (p=>p.InspectionItemId == ii.InspectionItemId && p.attID == Attachment);
					if(defectModel != null){
						return true;
					}
					if(ii.hasChildren == 1)
						rtnVal = rtnVal || recursiveCheckInDefects(defectList, itemService.GetInspectionItemChildren(ii.InspectionItemId, Attachment));
					if(rtnVal)
						return true;
				}
			}
			return rtnVal;
		}

		private bool recursiveCheckInDefectsAllCleared(ObservableCollection<InspectionReportDefectModel> defectList, List<InspectionItemModel> inspItem, bool curBoolVal){
			ItemId = -1;
			if (defectList != null && inspItem != null) {
				foreach(var iitem in inspItem){
					ItemId = iitem.InspectionItemId;
					if (iitem.hasChildren == 1) {						
						curBoolVal = curBoolVal && recursiveCheckInDefectsAllCleared (defectList, itemService.GetInspectionItemChildren (iitem.InspectionItemId, Attachment), curBoolVal);
					}
					else {						
						var defectModel = defectList.FirstOrDefault (p=>p.InspectionItemId == iitem.InspectionItemId && p.attID == Attachment);
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

