using System;
using MvvmCross.Platform.Platform;
using System.Diagnostics;
using BSM.Core.ConnectionLibrary;

namespace BSM.Core
{
	public class MyClass : IMvxTrace
	{
		
		public void Trace(MvxTraceLevel level, string tag, Func<string> message)
		{
			if(Constants.Debug)
				Debug.WriteLine(tag + ":" + level + ":" + message());
		}

		public void Trace(MvxTraceLevel level, string tag, string message)
		{
			if(Constants.Debug)
				Debug.WriteLine(tag + ":" + level + ":" + message);
		}

		public void Trace(MvxTraceLevel level, string tag, string message, params object[] args)
		{
			if (Constants.Debug) {
				try {
					Debug.WriteLine (string.Format (tag + ":" + level + ":" + message, args));
				} catch (FormatException) {
					Trace (MvxTraceLevel.Error, tag, "Exception during trace of {0} {1} {2}", level, message);
				}
			}
		}
	}
}

