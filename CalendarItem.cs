using System;
using System.ComponentModel;

namespace BSM.Core
{
	public class CalendarItem : INotifyPropertyChanged
    {
        public string Day { get; set; }
		public string Date { get; set; }
		public string Month { get; set; }
		public string Status_Header { get; set; }
		public string Status_Footer { get; set; }
		public DateTime date { get; set; }
//		public bool isItemSelected { get; set; }
//		public bool isHOSLogSheetNotSigned { get; set;}
//		public bool violationExists{ get; set;}

		private bool _isItemSelected;
		public bool isItemSelected {
			get{return _isItemSelected; }
			set{
				if (value != this._isItemSelected) {
					this._isItemSelected = value;
					var handler = this.PropertyChanged;
					if (handler != null) {
						handler (this, new PropertyChangedEventArgs ("isItemSelected"));
					}
				}
			}
		}

		private bool _isHOSLogSheetNotSigned;
		public bool isHOSLogSheetNotSigned {
			get{return _isHOSLogSheetNotSigned; }
			set{
				if (value != this._isHOSLogSheetNotSigned) {
					this._isHOSLogSheetNotSigned = value;
					var handler = this.PropertyChanged;
					if (handler != null) {
						handler (this, new PropertyChangedEventArgs ("isHOSLogSheetNotSigned"));
					}
				}
			}
		}

		private bool _violationExists;
		public bool violationExists {
			get{return _violationExists; }
			set{
				if (value != this._violationExists) {
					this._violationExists = value;
					var handler = this.PropertyChanged;
					if (handler != null) {
						handler (this, new PropertyChangedEventArgs ("violationExists"));
					}
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
    }
}

