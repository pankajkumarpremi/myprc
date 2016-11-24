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
	public class ForgotPasswordViewModel: MvxViewModel
	{
		#region Member Variables
		private readonly IMvxMessenger _messenger;
		private readonly ILoginService _loginservice;
		private readonly MvxSubscriptionToken _networkStatusChanged;
		#endregion

		#region ctors
		public ForgotPasswordViewModel (IMvxMessenger messenger, ILoginService loginservice)
		{
			_messenger = messenger;
			_loginservice = loginservice;
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

		private string _authorizationCode;
		public string AuthorizationCode
		{ 
			get { return _authorizationCode; }
			set { _authorizationCode = value; RaisePropertyChanged(() => AuthorizationCode); }
		}

		private string _newPassword;
		public string NewPassword
		{ 
			get { return _newPassword; }
			set { _newPassword = value; RaisePropertyChanged(() => NewPassword); }
		}

		private string _retypePassword;
		public string RetypePassword
		{ 
			get { return _retypePassword; }
			set { _retypePassword = value; RaisePropertyChanged(() => RetypePassword); }
		}

		private bool _isBusy;
		public bool IsBusy
		{
			get { return _isBusy; }
			set { _isBusy = value; RaisePropertyChanged(() => IsBusy); }
		}
		#endregion

		#region Commands
		public ICommand Cancel
		{
			get {
				return new MvxCommand(() => {
					OnCloseView(new EventArgs());
					Close(this);
				});
			}
		}

		public ICommand ResetPasswordCommand
		{
			get {
				return new MvxCommand(() => ResetPassword());
			}
		}

		public void ResetPassword() {
			OnFeatureUnderDevelopment(new EventArgs());
		}
		#endregion

		#region Events
		public event EventHandler CloseView;
		protected virtual void OnCloseView(EventArgs e)
		{
			unSubscribe ();
			if (CloseView != null)
			{
				CloseView(this, e);
			}
		}

		public event EventHandler FeatureUnderDevelopment;
		protected virtual void OnFeatureUnderDevelopment(EventArgs e)
		{
			if (FeatureUnderDevelopment != null)
			{
				FeatureUnderDevelopment(this, e);
			}
		}
		#endregion

		#region unsubscribe
		public void unSubscribe() {
			_messenger.Unsubscribe<NetworkStatusChangedMessage> (_networkStatusChanged);
		}
		#endregion
	}
}

