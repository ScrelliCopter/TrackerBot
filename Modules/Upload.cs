using System.Text;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;

namespace TrackerBot.Modules
{
    class Upload : ModuleBase
    {
		private readonly ConfigService m_cfgSrv;

		public Upload ( ConfigService a_cfgSrv )
		{
			m_cfgSrv = a_cfgSrv;
		}

		[Command ( "add" )]
		public async Task Add ( string 
    }
}
