using MvvmCross.Core.ViewModels;
using System;
using MvvmCross.Plugins.Messenger;
using BSM.Core.Messages;

namespace BSM.Core.ViewModels
{
	public class DriverModeViewModel : BaseViewModel
	{
		IMvxMessenger _messenger;
		public DriverModeViewModel (IMvxMessenger messenger)
		{
			_messenger = messenger;
		}

		public IMvxCommand SelectDriveMode
		{
			get {
				return new MvxCommand<DriverStatusTypeClass>((DriverStatus) => {
					_messenger.Publish<UpdateDriverStatusMessage> (new UpdateDriverStatusMessage(this){driverStatusType = DriverStatus});
					OnCloseView(new EventArgs());
				});
			}
		}

		public IMvxCommand CloseDriveMode
		{
			get {
				return new MvxCommand(() => {
					OnCloseView(new EventArgs());
				});
			}
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