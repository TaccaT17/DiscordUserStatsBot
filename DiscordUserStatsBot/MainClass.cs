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

        private string filePath;
        private DiscordSocketClient client; //         <--------------------------------THIS IS YOUR REFERENCE TO EVERYTHING
        private bool guildInstancesInitialized;

        public static void Main(string[] args)
        => new MainClass().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            Console.WriteLine("Main called.");

            guildInstancesInitialized = false;

            DiscordSocketConfig config = new DiscordSocketConfig();
            config.AlwaysDownloadUsers = true;

            filePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if(client == null)
            {
                Console.WriteLine("Client null, making new client");
                client = new DiscordSocketClient(config);
            }
            
            client.Log += Log;

            client.Ready += BootUpBot; //Ready is fired when the bot comes online and is connected to discord

            //discord people/bots/objects have a "token" AKA ID that is a password/username
            // not secure to hardcode token so instead will get it from saved file (under TomsDiscordBot->bin->Debug->netcoreapp3.1)
            Console.WriteLine("token path: " + filePath);

            var token = File.ReadAllText(filePath + @"\token.txt");

            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            // wait for an indefinite amount of time
            await Task.Delay(-1);
        }

        private Task Log(LogMessage msg)
        {
            using (System.IO.StreamWriter file =
            new System.IO.StreamWriter(filePath + @"\logs.txt", true))
            {
                file.WriteLine(msg.ToString());
            }

            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private Task BootUpBot()
        {
            if (!guildInstancesInitialized)
            {
                Console.WriteLine("     Creating guild instances.");

                //for each guild create controller
                SocketGuild guild;
                IEnumerator<SocketGuild> guildE = client.Guilds.GetEnumerator();
                while (guildE.MoveNext())
                {
                    guild = guildE.Current;

                    UserStatsBotController tempControllerRef = new UserStatsBotController(client, guild);
                }

                guildInstancesInitialized = true;
            }

            return Task.CompletedTask;
        }

    }
}
