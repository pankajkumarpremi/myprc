using System;
using System.ComponentModel;
using MvvmCross.Core.ViewModels;
using BSM.Core.ViewModels;

namespace BSM.Core
{
	public class CategoryModel 
	{
		/*  CREATE TABLE CATEGORY (CategoryId INTEGER, Description ntext, GroupID INTEGER, Barcode ntext, Location ntext);  */ 

		public CategoryModel ()
		{
		}

		public CategoryModel (int _CategoryId, string _Description, string _attID, int _GroupID, string _BarCodeID)
		{
			this.CategoryId = _CategoryId;
			this.Description = _Description;
			this.attID = _attID;
			this.GroupID = _GroupID;
			this.BarCodeID = _BarCodeID;
			this.Location = "";
		}
		public int CategoryId{ get; set; }
		public string Description{ get; set; }
		public string attID{ get; set; }
		public int GroupID{ get; set; }
		public string Location{ get; set; }
		public string BarCodeID{ get; set; }
		public string LngCode { get; set;}
	}

	public class CategoryModelRow : INotifyPropertyChanged
	{
		InspectionCategoryViewModel _parent;
		public CategoryModelRow(InspectionCategoryViewModel parent){
			_parent = parent;
		}

		public int CategoryId{ get; set; }
		public string Description{ get; set; }
		public string attID{ get; set; }
		public int GroupID{ get; set; }
		public string Location{ get; set; }
		public string BarCodeID{ get; set; }
		public string LngCode { get; set;}

		public bool HasNoSubItems{ get; set;}

		private bool bsmAddOn_HasDefect = false;
		public bool BsmAddOn_HasDefect{
			get{return bsmAddOn_HasDefect; } 
			set{
				if (value != this.bsmAddOn_HasDefect) {
					this.bsmAddOn_HasDefect = value;
					var handler = this.PropertyChanged;
					if (handler != null) {
						handler (this, new PropertyChangedEventArgs ("BsmAddOn_HasDefect"));
					}
				} 
			}
		}

		public bool setStrikeThrough;
		public bool SetStrikeThrough {
			get { return setStrikeThrough; } 
			set {
				if (value != this.setStrikeThrough) {
					this.setStrikeThrough = value;
					var handler = this.PropertyChanged;
					if (handler != null) {
						handler (this, new PropertyChangedEventArgs ("SetStrikeThrough"));
					}
				} 
			}
		}

		private bool isChecked;
		public bool IsChecked{ 
			get {
				return this.isChecked;
			}
			set {
				if (value != this.isChecked) {
					this.isChecked = value;
					var handler = this.PropertyChanged;
					if (handler != null) {
						handler (this, new PropertyChangedEventArgs ("IsChecked"));
					}
				}
			}
		}

		private bool isScanned;
		public bool IsScanned{
			get {
				return this.isScanned;
			}
			set {
				if (value != this.isScanned) {
					this.isScanned = value;
					var handler = this.PropertyChanged;
					if (handler != null) {
						handler (this, new PropertyChangedEventArgs ("IsScanned"));
					}
				}
			}
		}

		private bool hasBarcode;
		public bool HasBarcode{
			get{return hasBarcode; }
			set{
				if (value != this.hasBarcode) {
					this.hasBarcode = value;
					var handler = this.PropertyChanged;
					if (handler != null) {
						handler (this, new PropertyChangedEventArgs ("HasBarcode"));
					}
				}
			}
		}


		public IMvxCommand ChangeCheckImage {
			get {
				return new MvxCommand (() => _parent.ChangeImage (this));
			}
		}

		#region INotifyPropertyChanged implementation

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
	}
}

