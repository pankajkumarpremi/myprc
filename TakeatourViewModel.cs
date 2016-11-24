using MvvmCross.Core.ViewModels;
using System;

namespace BSM.Core.ViewModels
{
	public class TakeatourViewModel : MvxViewModel
	{
		public event EventHandler CloseView;
		protected virtual void OnCloseView(EventArgs e)
		{
			if (CloseView != null)
			{
				
				CloseView(this, e);

			}
		}
		public IMvxCommand CloseTakeATour
		{
			get {
				return new MvxCommand(()=>{
					OnCloseView(new EventArgs());
					//ShowViewModel<LoginViewModel>();
					Close(this);
				});
			}
		}
	}
}

