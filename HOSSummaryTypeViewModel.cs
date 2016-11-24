using MvvmCross.Core.ViewModels;
using System.Windows.Input;
using MvvmCross.Plugins.Messenger;
using BSM.Core.Services;
using MvvmCross.Plugins.File;
using System;
using System.Collections.Generic;
using BSM.Core.Messages;
using BSM.Core.AuditEngine;
using BSM.Core.ConnectionLibrary;
using MvvmCross.Platform;

namespace BSM.Core.ViewModels
{
	public class HOSSummaryTypeViewModel : MvxViewModel
	{
		#region Member Variables
		private readonly IMvxMessenger _messenger;
		#endregion

		#region ctors
		public HOSSummaryTypeViewModel(IMvxMessenger messenger)
		{
			_messenger = messenger;
			SummaryOptions.Add (1);
			SummaryOptions.Add (7);
			SummaryOptions.Add (8);
			SummaryOptions.Add (14);
		}
		#endregion

		#region Properties
		private List<int> _summaryOptions = new List<int> ();
		public List<int> SummaryOptions
		{
			get { return _summaryOptions; }
			set
			{
				_summaryOptions = value;
				RaisePropertyChanged(() => SummaryOptions);
			}
		}
		#endregion

		#region Events
		#endregion

		#region Commands
		public ICommand SelectSummaryTypeSummaryCommand
		{
			get
			{
				return new MvxCommand<int>((SummaryType) =>
					{	
						_messenger.Publish<RefreshSummaryMessage>(new RefreshSummaryMessage(this)
							{
								SummaryType = SummaryType
							});
						OnCloseView(new EventArgs());
					});
			}
		}

		public IMvxCommand CloseSummaryType
		{
			get {
				return new MvxCommand(() => {
					OnCloseView(new EventArgs());
				});
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
		#endregion

		#region unsubscribe
		#endregion
	}
}
