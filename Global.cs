using System;
using System.IO;
using Newtonsoft.Json;

namespace TrackerBot
{
	static class Global
	{
		public static Config Config { get { return m_cfg; } }

		public static void LoadConfig ()
		{
			try
			{
				var file = File.ReadAllText ( "config.json" );
				m_cfg = JsonConvert.DeserializeObject<Config> ( file );
			}
			catch ( Exception ex )
			{
				throw new Exception ( "Error loading conf.json", ex );
			}
		}

		public static void SaveConfig ()
		{
			try
			{
				File.WriteAllText ( "config.json", JsonConvert.SerializeObject ( Config, Formatting.Indented ) );
			}
			catch ( Exception ex )
			{
				throw new Exception ( "Error loading conf.json", ex );
			}
		}

		private static Config m_cfg;
	}
}
