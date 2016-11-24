using System;
using MvvmCross.Core.ViewModels;
using System.Text.RegularExpressions;
using BSM.Core.ViewModels;
using BSM.Core.Services;
using System.Linq;
using BSM.Core.AuditEngine;


namespace BSM.Core
{
	public class SendEmailViewModel : BaseViewModel
	{	
		private readonly IDataService _dataservice;
		private readonly IEmailService _emailservice;
		private readonly ITimeLogService _timelog;
		private readonly ISyncService _syncservice;
		private readonly ICommunicationService _communication;
		public SendEmailViewModel(IDataService dataservice,IEmailService emailservice,ITimeLogService timelog,ISyncService syncservice,ICommunicationService communication){
			_dataservice = dataservice;
			_emailservice = emailservice;
			_timelog = timelog;
			_syncservice = syncservice;
			_communication = communication;
			FromSelected = Utils.GetDateTimeNow ().Date;
			ToSelected = Utils.GetDateTimeNow ().Date;
			TodayDate = ToSelected;
			EmailAddress = "";
			MaxDate = Utils.GetDateTimeNow ().AddDays(1).Date;
			MinDate = Utils.GetDateTimeNow ().AddDays(-14).Date;
		}

		public void Init(DateTime? SelectedDate) {
			if (SelectedDate != null) {
				FromSelected = Convert.ToDateTime(SelectedDate).Date;
			} else {
				FromSelected = Utils.GetDateTimeNow ().Date;
			}
		}


		const string RegulerExpression = @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
			@"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$";
		private string emailaddress;
		public string EmailAddress{
			get{return emailaddress; }
			set{emailaddress = value;RaisePropertyChanged (()=>EmailAddress); }
		}

		private bool checkLogsheet;
		public bool CheckLogsheet{
			get{return checkLogsheet; }
			set{checkLogsheet = value;RaisePropertyChanged (()=>CheckLogsheet); }
		}

		private bool checkPretrip;
		public bool CheckPretrip{
			get{return checkPretrip; }
			set{checkPretrip = value;RaisePropertyChanged (()=>CheckPretrip); }
		}

		private bool checkPosttrip;
		public bool CheckPosttrip{
			get{return checkPosttrip; }
			set{checkPosttrip = value;RaisePropertyChanged (()=>CheckPosttrip); }
		}

		public DateTime MaxDate {
			get;
			set;
		}

		public DateTime MinDate {
			get;
			set;
		}

		private DateTime fromSelected;
		public DateTime FromSelected
		{
			get{return fromSelected; }
			set{fromSelected = value;RaisePropertyChanged (()=>FromSelected); }
		}

		private DateTime toSelected;
		public DateTime ToSelected
		{
			get{return toSelected; }
			set{toSelected = value;RaisePropertyChanged (()=>ToSelected); }
		}

		public DateTime TodayDate {
			get;
			set;
		}

		public IMvxCommand EmailSend
		{
			
			get{return new MvxCommand (()=> {ValidateAndSendEmail();});}
		}

		public IMvxCommand CloseMessages
		{
			get {
				return new MvxCommand(()=>{
					OnCloseView(new EventArgs());
					Close(this);
				});
			}
		}

		void ValidateAndSendEmail(){
			
			var errorMessage = string.Empty;
			if (EmailAddress.Length == 0 || !Regex.IsMatch (EmailAddress, RegulerExpression,RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds (500))) {
				errorMessage = "Valid Email Required";
			} else if(!CheckLogsheet && !CheckPretrip && !CheckPosttrip){
				errorMessage = "Attachment is required";
			}
			if (errorMessage.Length != 0) {
				OnSendError (new ErrorMessageEventArgs(){Message = errorMessage});	
			} else {
				var emailModel = new EmailModel ();
				emailModel.EmailAddress = EmailAddress;
				emailModel.DriverId = _dataservice.GetCurrentDriverId ();
				emailModel.FromDate = FromSelected.Date;
				emailModel.ToDate = ToSelected.Date;
				emailModel.HaveSent = false;
				emailModel.CreatedDate = Utils.GetDateTimeUtcNow ();
				emailModel.Type =string.Join(",",((CheckLogsheet ? "0,":",") + (CheckPretrip ? "1,":",") + (CheckPosttrip ? "2,":",")).Split(',').Where(p=>!string.IsNullOrWhiteSpace(p)));
				if (CheckLogsheet) {
					var lstTimelog = _timelog.GetAllNotSignedForDatesDriver (FromSelected,ToSelected,_dataservice.GetCurrentDriverId());	
					if (lstTimelog != null && lstTimelog.Count > 0) {
						OnShowAlert (new EventArgs ());
					} else {
						InsertEmail (emailModel);
					}
				} else {
					InsertEmail (emailModel);
				}
			}
		}

		public void SignAndSend(bool send = true){
			if (send) {
				var lstTimelog = _timelog.GetAllNotSignedForDatesDriver (FromSelected, ToSelected, _dataservice.GetCurrentDriverId ());	
				if (lstTimelog != null && lstTimelog.Count > 0) {
					foreach (var timelog in lstTimelog) {
						timelog.HaveSent = false;
						timelog.Signed = true;
						_timelog.InsertOrUpdate (timelog);
					}
				}
				var emailModel = new EmailModel ();
				emailModel.EmailAddress = EmailAddress;
				emailModel.DriverId = _dataservice.GetCurrentDriverId ();
				emailModel.FromDate = FromSelected.Date;
				emailModel.ToDate = ToSelected.Date;
				emailModel.HaveSent = false;
				emailModel.Type = string.Join (",", ((CheckLogsheet ? "0," : ",") + (CheckPretrip ? "1," : ",") + (CheckPosttrip ? "2," : ",")).Split (',').Where (p => !string.IsNullOrWhiteSpace (p)));
				InsertEmail (emailModel);
			} else {
				ShowViewModel<HOSMainViewModel> (new {selectedDate = FromSelected,pageFrom="email"});
			}
		}
		void InsertEmail(EmailModel modeltoInsert){
			_emailservice.Insert (modeltoInsert);
			_syncservice.runTimerCallBackNow ();
			OnShowSuccessAlert (new ErrorMessageEventArgs(){Message = "Email was sent!"});
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

		public event EventHandler SendError;
		protected virtual void OnSendError(EventArgs e)
		{
			if (SendError != null)
			{
				SendError(this, e);
			}
		}

		public event EventHandler ShowAlert;
		protected virtual void OnShowAlert(EventArgs e)
		{
			if (ShowAlert != null)
			{
				ShowAlert(this, e);
			}
		}

		public event EventHandler ShowSuccessAlert;
		protected virtual void OnShowSuccessAlert(EventArgs e)
		{
			if (ShowSuccessAlert != null)
			{
				ShowSuccessAlert(this, e);
			}
		}
		#endregion
	}
}

