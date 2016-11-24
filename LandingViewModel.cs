using MvvmCross.Core.ViewModels;
using System.Windows.Input;
using MvvmCross.Plugins.Messenger;
using BSM.Core.Services;
using MvvmCross.Plugins.File;

namespace BSM.Core.ViewModels
{
	public class LandingViewModel : MvxViewModel
    {
		#region Member Variables
//		private readonly IMvxMessenger _messenger;
		private readonly IDataService _dataservice;
//		private readonly ILocationService _locationservice;
		#endregion

		#region ctors
		public LandingViewModel()
//		public LandingViewModel (IMvxMessenger messenger, IDataService dataservice, ILocationService locationservice, IMvxFileStore filesystem)
		{
//			_messenger = messenger;
//			_dataservice = dataservice;
//			_locationservice = locationservice;
		}
		#endregion

		#region Commands
		public ICommand GoToLoginView
		{
			get {
				return new MvxCommand(() =>
					{
						ShowViewModel<LoginViewModel>();
					});
			}
		}
		#endregion
    }
}
