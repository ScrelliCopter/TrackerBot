using System.Threading.Tasks;
using Discord.Commands;

namespace TrackerBot.Modules
{
    class Test : ModuleBase
    {
		[Command ( "ping" ), Summary ( "Pretty self explainatory." )]
		public async Task Ping ()
		{
			await Context.Channel.SendMessageAsync ( $"{Context.User.Mention} Pong." );
		}
    }
}
