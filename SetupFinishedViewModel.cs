using System;
using MvvmCross.Core.ViewModels;
using System.Windows.Input;
using Sockets.Plugin;
using System.Threading.Tasks;
using BSM.Core.Services;
using MvvmCross.Plugins.Messenger;
using BSM.Core.Messages;
using Acr.MvvmCross.Plugins.Network;

namespace BSM.Core.ViewModels
{
	public class SetupFinishedViewModel: MvxViewModel
	{
		#region Member Variables
		private readonly IMvxMessenger _messenger;
		private readonly IDataService _dataService;
		private readonly MvxSubscriptionToken _networkStatusChanged;
		#endregion

		#region ctors
		public SetupFinishedViewModel (IMvxMessenger messenger, IDataService dataService)
		{
			_messenger = messenger;
			_dataService = dataService;
			_networkStatusChanged = _messenger.Subscribe<NetworkStatusChangedMessage> ((message) => {
				if (message.Status.IsConnected) {
					IsOffline = false;
				}
				else {
					IsOffline = true;
				}
			});
		}
		#endregion

		#region Properties
		private bool _isOffline = false;
		public bool IsOffline
		{
			get { return _isOffline; }
			set { _isOffline = value; RaisePropertyChanged(() => IsOffline); }
		}
		#endregion

		#region Commands
		public ICommand Cancel
		{
			get {
				return new MvxCommand(() => Close(this));
			}
		}

		public ICommand TakeATourCommand
		{
			get {
				return new MvxCommand(() => TakeATour());
			}
		}

		public void TakeATour() {
			ShowViewModel<TakeatourViewModel>();
		}

        public ICommand SkipTourCommand
		{
			get {
				return new MvxCommand(() => SkipTour());
			}
		}

		public void SkipTour() {
			unSubscribe ();
			ShowViewModel<SelectAssetViewModel> ();
			/*AssetId = _dataService.GetAssetBoxId ();
			AssetDescription = _dataService.GetAssetBoxDescription ();
			if (AssetId != -1) {
				ShowViewModel<DashboardViewModel> ();
//				ShowViewModel<InspectionListViewModel> ();
			} else {
				ShowViewModel<SelectAssetViewModel> ();
			}*/
			Close (this);
		}
		#endregion

		#region unsubscribe
		public void unSubscribe() {
			_messenger.Unsubscribe<NetworkStatusChangedMessage> (_networkStatusChanged);
		}
		#endregion
	}
}

