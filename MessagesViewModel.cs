using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Collections.ObjectModel;
using MvvmCross.Plugins.Messenger;
using MvvmCross.Core.ViewModels;
using MvvmCross.Core;
using BSM.Core.Messages;
using BSM.Core.Services;
using BSM.Core.AuditEngine;
using BSM.Core.ConnectionLibrary;
using MvvmCross.Platform.Core;

namespace BSM.Core
{
	public class MessagesViewModel : BaseViewModel
	{
		private readonly IMvxMessenger _messenger;
		private readonly IMessagingService _messagingService;
		private readonly IDataService _dataService;
		private readonly IMessagingRepositoryService _messagingRepositoryService;
		private MvxSubscriptionToken _MsgSubscribeToken;	

		private string curUserID = string.Empty;
		private int load_history_index = 1;

		public MessagesViewModel (IMvxMessenger messenger, IMessagingService messagingService, IDataService dataService, IMessagingRepositoryService messagingRepositoryService)
		{
			_messenger = messenger;
			_MsgSubscribeToken = _messenger.Subscribe<MessagingMessage>((message) =>{
				if(!IsLoadingMore){
					if(!message.MessageTopic.Equals("TheEnd")){
						BSMMessage messageObj = new BSMMessage ();
						messageObj.Text = message.Message;
						messageObj.Sender = message.UserName;
						messageObj.Type = BSMMessageType.Incoming;
						messageObj.IsMessageSent = false;
						messageObj.MsgDateTime = Utils.GetLocalFromUtc(message.MsgDateTime);

						//MvxMainThreadDispatcher.Instance.RequestMainThreadAction(() => {
						MvxSingleton<IMvxMainThreadDispatcher>.Instance.RequestMainThreadAction(() => {
							Messages.Add (messageObj);
						});
					}
				}else{
					if(message.MessageTopic.Equals("TheEnd")){
						//MvxMainThreadDispatcher.Instance.RequestMainThreadAction(() => {
						MvxSingleton<IMvxMainThreadDispatcher>.Instance.RequestMainThreadAction(() => {
							LoadMessageList (load_history_index);
							IsLoadingMore = false;
						});
					}
				}
			});

			_messagingService = messagingService;
			_dataService = dataService;
			_messagingRepositoryService = messagingRepositoryService;

			curUserID = _dataService.GetCurrentDriverId ();
			LoadMessageList (1);

			IsLoadingMore = false;
		}

		private void LoadMessageList(int load_index){
			Messages = new ObservableCollection<BSMMessage> ();

			List<MessagingModel> lastMsgs = _messagingRepositoryService.GetLastMessages (load_index*15);
			lastMsgs.Reverse ();
			if (lastMsgs != null && lastMsgs.Count > 0) {
				foreach (MessagingModel mm in lastMsgs) {
					BSMMessage m = new BSMMessage ();
					m.Text = mm.Message;
					m.Sender = mm.UserName;
					if (curUserID.Equals (mm.UserID)) {
						m.Type = BSMMessageType.Outgoing;
						m.IsMessageSent = true;
					} else {
						m.Type = BSMMessageType.Incoming;
						m.IsMessageSent = false;
					}
					m.MsgDateTime = Utils.GetLocalFromUtc(new DateTime(mm.MsgTimeStampTicks));
					Messages.Add (m);
				}
			}
		}

		public IMvxCommand CloseMessages
		{
			get {
				return new MvxCommand(()=>{
					unSubscribe();
					OnCloseView(new EventArgs());
					Close(this);
					//MvxMainThreadDispatcher.Instance.RequestMainThreadAction(() => {
					MvxSingleton<IMvxMainThreadDispatcher>.Instance.RequestMainThreadAction(() => {					
						NewMessagesCount = 0;
					});
				});
			}
		}

		public IMvxCommand SendMessage
		{
			get {
				return new MvxCommand(()=>{
					IsBusy = true;
					HideKeyBoard = false;
					//MvxMainThreadDispatcher.Instance.RequestMainThreadAction(() => {
					MvxSingleton<IMvxMainThreadDispatcher>.Instance.RequestMainThreadAction(() => {
						Validate();
						IsBusy = false;
						BodyText = string.Empty;
						HideKeyBoard = true;
						CannedListVisible = false;
					});
				});
			}
		}

		public IMvxCommand ShowCannedMsgs
		{
			get {
				return new MvxCommand(()=>{
					//MvxMainThreadDispatcher.Instance.RequestMainThreadAction(() => {
					MvxSingleton<IMvxMainThreadDispatcher>.Instance.RequestMainThreadAction(() => {						
						CannedListVisible = true;
					});
				});
			}
		}

		public void Validate(){
			if(!string.IsNullOrEmpty(BodyText)){
				MessagingMessage myMsg = new MessagingMessage (this);

				myMsg.BoxId = _dataService.GetAssetBoxId ();
				myMsg.Type = MessageType.Driver;
				myMsg.UserID = curUserID;
				myMsg.UserName = DriverName;
				myMsg.Lat = _dataService.GetLatitude ();
				myMsg.Lng = _dataService.GetLongitude ();
				myMsg.MsgDateTime =  Utils.GetDateTimeUtcNow ();
				myMsg.Message = BodyText;

				_messagingService.ReplyToLastSender (myMsg);

				BSMMessage messageObj = new BSMMessage ();
				messageObj.Text = BodyText;
				messageObj.Sender = DriverName;
				messageObj.Type = BSMMessageType.Outgoing;
				messageObj.IsMessageSent = true;
				messageObj.MsgDateTime = Utils.GetDateTimeNow ();
				Messages.Add (messageObj);
			}
		}

		public void LoadMoreMessages(){			
			if (Messages.Count > 0) {
				IsLoadingMore = true;
				load_history_index++;
				DateTime _olderDate = DateTime.MinValue;
				MessagingModel lastBeforeCurOldest = _messagingRepositoryService.GetLastBeforeDate (Messages [0].MsgDateTime.Ticks);
				if (lastBeforeCurOldest != null)
					_olderDate = new DateTime (lastBeforeCurOldest.MsgTimeStampTicks);

				_messagingService.LoadNextRange (Messages [0].MsgDateTime, _olderDate);
			}
		}

		public IMvxCommand GetCannedItem
		{
			get {
				return new MvxCommand<CannedItem>((item)=>{
					if(item != null)
						BodyText=item.CannedItemText;
				});
			}
		}

		public IMvxCommand GetItemText
		{
			get {
				return new MvxCommand<BSMMessage>((item)=>{
					if(item != null)
						CopiedText = item.Text;
				});
			}
		}

		private string _textBody;
		public string BodyText
		{
			get{return _textBody; }
			set{_textBody = value;RaisePropertyChanged (()=>BodyText); }
		}

		private string _copiedText;
		public string CopiedText
		{
			get{return _copiedText; }
			set{_copiedText = value;RaisePropertyChanged (()=>CopiedText); }
		}

		private List<CannedItem> _cannedList = new List<CannedItem>();
		public List<CannedItem> CannedList
		{
			get{return _cannedList; }
			set{ _cannedList = value; RaisePropertyChanged (()=> CannedList); }
		}

		private ObservableCollection<BSMMessage> _messages;
		public ObservableCollection<BSMMessage> Messages
		{
			get{return _messages; }
			set{_messages = value; RaisePropertyChanged (()=>Messages); }
		}

		private bool keypadEnable;
		public bool KeypadEnable
		{
			get{return keypadEnable; }
			set{keypadEnable = value;RaisePropertyChanged (()=>KeypadEnable); }
		}

		private bool sendEnable;
		public bool SendEnable
		{
			get{return sendEnable; }
			set{sendEnable = value;RaisePropertyChanged (()=>SendEnable); }
		}

		private bool _isBusy;
		public bool IsBusy
		{
			get { return _isBusy; }
			set { _isBusy = value; RaisePropertyChanged(() => IsBusy); }
		}

		private bool _hideKeyBoard;
		public bool HideKeyBoard
		{
			get { return _hideKeyBoard; }
			set { _hideKeyBoard = value; RaisePropertyChanged(() => HideKeyBoard); }
		}

		private bool _cannedListVisible;
		public bool CannedListVisible
		{
			get { return _cannedListVisible; }
			set { _cannedListVisible = value; RaisePropertyChanged(() => CannedListVisible); }
		}

		private bool _isLoadingMore;
		public bool IsLoadingMore
		{
			get { return _isLoadingMore; }
			set { _isLoadingMore = value; RaisePropertyChanged(() => IsLoadingMore); }
		}

		#region Events
		public event EventHandler CloseView;
		protected virtual void OnCloseView(EventArgs e)
		{
			if (CloseView != null){				
				CloseView(this, e);
			}
			unSubscribe ();
		}
		#endregion

		#region unsubscribe
		public void unSubscribe() {
			_messenger.Unsubscribe<MessagingMessage>(_MsgSubscribeToken);
		}
		#endregion
	}
}

