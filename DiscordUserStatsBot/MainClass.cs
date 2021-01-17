using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DiscordUserStatsBot
{

    //current problem: changing rank criteria for one server changes it for all servers

    class MainClass
    {
        private DiscordSocketClient client; //         <--------------------------------THIS IS YOUR REFERENCE TO EVERYTHING

        public static void Main(string[] args)
        => new MainClass().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            DiscordSocketConfig config = new DiscordSocketConfig();
            config.AlwaysDownloadUsers = true;

            client = new DiscordSocketClient(config);

            client.Log += Log;

            client.Ready += BootUpBot; //Ready is fired when the bot comes online and is connected to discord

            //discord people/bots/objects have a "token" AKA ID that is a password/username
            // not secure to hardcode token so instead will get it from saved file (under TomsDiscordBot->bin->Debug->netcoreapp3.1)
            string filePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            Console.WriteLine("token path: " + filePath);
            var token = File.ReadAllText(filePath + @"\token.txt");

            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            // wait for an indefinite amount of time
            await Task.Delay(-1);
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private Task BootUpBot()
        {
            //for each guild create controller
            SocketGuild guild;
            IEnumerator<SocketGuild> guildE = client.Guilds.GetEnumerator();
            while (guildE.MoveNext())
            {
                guild = guildE.Current;

                UserStatsBotController tempControllerRef = new UserStatsBotController(client, guild);
            }

            return Task.CompletedTask;
        }

    }
}
