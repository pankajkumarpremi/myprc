using System;

namespace BSM.Core
{
	public class CategoryVehicleModel
	{
		public CategoryVehicleModel ()
		{
		}

		public CategoryVehicleModel (int _BoxId, string _GroupIDs, DateTime _updateTS)
		{
			this.BoxId = _BoxId;
			this.GroupIDs = _GroupIDs;
			this.updateTS = _updateTS;
		}

		public int BoxId{ get; set; }
		public string GroupIDs{ get; set; }
		public DateTime updateTS{ get; set; }
	}
}

