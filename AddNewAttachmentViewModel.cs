using Acr.MvvmCross.Plugins.Network;
using BSM.Core.AuditEngine;
using BSM.Core.ConnectionLibrary;
using BSM.Core.Messages;
using BSM.Core.Services;

using MvvmCross.Plugins.Messenger;
using MvvmCross.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using MvvmCross.Platform;
using MvvmCross.Platform.Platform;

namespace BSM.Core.ViewModels
{
    public class AddNewAttachmentViewModel : MvxViewModel
    {
        #region Member Variables

        private readonly IAssetService _assetService;
        private readonly ISyncService _syncService;
        private readonly ICommunicationService _communicationService;
        private readonly IDataService _dataservice;
        private readonly IMvxMessenger _messenger;

        #endregion
        #region ctors
        public AddNewAttachmentViewModel(IAssetService assetService, ISyncService syncService, IDataService dataService, IMvxMessenger messenger,  ICommunicationService communicationService)
        {
            _assetService = assetService;
            _syncService = syncService;
            _communicationService = communicationService;
            _dataservice = dataService;
            _messenger = messenger;       
            StatesList = Constants.ProvStatesList.ToList();
            SelectedState = StatesList[0];
            WeigthUnitList = Constants.WeigthUnitList.ToList();
            SelectedWeightUnit = WeigthUnitList[0];
        }
        #endregion

        #region Properties
        private string _description;
        public string Description
        {
            get { return _description; }
            set { _description = value; RaisePropertyChanged(() => Description); }
        }
        private string _licensePlate;
        public string LicensePlate
        {
            get { return _licensePlate; }
            set { _licensePlate = value; RaisePropertyChanged(() => LicensePlate); }
        }

        private List<String> _statesList;
        public List<String> StatesList
        {
            get { return _statesList; }
            set { _statesList = value; RaisePropertyChanged(() => StatesList); }
        }        
        private string _selectedState;
        public string SelectedState
        {
            get { return _selectedState; }
            set { _selectedState = value; RaisePropertyChanged(() => SelectedState); }
        }
        private List<String> _weigthUnitList;
        public List<String> WeigthUnitList
        {
            get { return _weigthUnitList; }
            set { _weigthUnitList = value; RaisePropertyChanged(() => WeigthUnitList); }
        }
        private string _selectedWeightUnit;
        public string SelectedWeightUnit
        {
            get { return _selectedWeightUnit; }
            set { _selectedWeightUnit = value; RaisePropertyChanged(() => SelectedWeightUnit); }
        }
        private int _weight;
        public int Weight
        {
            get { return _weight; }
            set { _weight = value; RaisePropertyChanged(() => Weight); }
        }
        private bool _isBusy;
        public bool IsBusy
        {
            get { return _isBusy; }
            set { _isBusy = value; RaisePropertyChanged(() => IsBusy); }
        }
        #endregion

        #region Command
        public ICommand SaveNewTrailer
        {
            get
            {
                return new MvxCommand(() => SaveTrailerAttachment());
            }
        }
     
        public ICommand CancelNewTrailer
        {
            get
            {
               return new MvxCommand(() => CloseAddNewAttachment());             
            }
        }
        public async void SaveTrailerAttachment()
        {
           
           int boxId= _assetService.SaveTrailer(_dataservice.GetCurrentDriverId(),  Description, Weight, LicensePlate, SelectedState, Utils.GetDateTimeNow());

            if (boxId != 0)
            {
                //persist attachment Id and description
                _dataservice.PersistAttachmentId(boxId.ToString());
                _dataservice.PersistAttachmentDescription(Description);
                
                //Sync new trailer additions to server
                var list_unsent_trailer = _assetService.GetAssetWithNoBoxid(_dataservice.GetCurrentDriverId());
                if (list_unsent_trailer != null && list_unsent_trailer.Count > 0)
                {
                    IsBusy = true;
                    bool rv = await _communicationService.SyncNewTrailer(list_unsent_trailer, Constants.DefaultWeightUnit);
                    if(!rv)
                        _syncService.runTimerCallBack();
                }                
                CloseAddNewAttachment();
                _dataservice.PersistIsSelectAttachment(true);
                _messenger.Publish<SyncSuccessMessage>(new SyncSuccessMessage(this));
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

        public void CloseAddNewAttachment()
        {                  
            OnCloseView(new EventArgs());
            this.Close(this);
        }
        #endregion
    }
}
