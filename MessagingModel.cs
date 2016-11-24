using System;
using System.Collections.Generic;
using SQLite.Net.Attributes;
using  BSM.Core.AuditEngine;

namespace BSM.Core
{
	public enum MessageType
	{
		Driver,
		Admin,
	}

	public class MessagingModel
	{
		public MessagingModel()
		{
			MsgTimeStampTicks = Utils.GetDateTimeUtcNow ().Ticks;
		}

		[PrimaryKey,AutoIncrement]
		public int Id { get; set;}
		public MessageType Type { get; set;}
		public string UserID { get; set;}
		public string UserName { get; set;}
		public string Message { get; set;}
		public double Lat { get; set;}
		public double Lng { get; set;}
		public int BoxId { get; set;}
		public long MsgTimeStampTicks { get; set;}
		public string MessageTopic { get; set;}
		public string Images { get; set;}  //Comma separated image filenames
	}
}

