
using MvvmCross.Core.ViewModels;

namespace BSM.Core.ViewModels
{
	public class RootViewModel  : MvxViewModel
	{
		public void Init(Parameters parametrs){
			id = parametrs.id;
		}
		int id;
		public RootViewModel()
		{
			Home = new LandingViewModel();
			Menu = new MenuViewModel();
		}
		private LandingViewModel _home;
		public LandingViewModel Home
		{
			get { return _home; }
			set { _home = value; RaisePropertyChanged(() => Home); }
		}

		private MenuViewModel _menu;
		public MenuViewModel Menu
		{
			get { return _menu; }
			set { _menu = value; RaisePropertyChanged(() => Menu); }
		}

		public class Parameters
		{
		public int id{get;set;}			
		}
	}
}

