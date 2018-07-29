using System;

namespace TrackerBot
{
	class Program
	{
		static void Main ( string[] args )
		{
			new Bot ().Start ().GetAwaiter ().GetResult ();
		}
	}
}
