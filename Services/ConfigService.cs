using System;
using System.IO;
using Newtonsoft.Json;
using LiteDB;

namespace TrackerBot
{
	public class ConfigService
	{
		private LiteDatabase m_db;

		public ConfigService ()
		{
			m_db = new LiteDatabase ( @"modules.db" );
		}

		public

		#region Config

		public Config Config { get { return m_cfg; } }
		private Config m_cfg;

		public void LoadConfig ()
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

		public void SaveConfig ()
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

		#endregion
	}
}
