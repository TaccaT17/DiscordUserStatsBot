//Copyright Tom Crammond 2021

using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DiscordUserStatsBot
{
    class MainClass
    {

        private static string filePath;
        private DiscordSocketClient client; //         <--------------------------------THIS IS YOUR REFERENCE TO EVERYTHING
        
        //For controlling instances of bot per guild
        private bool guildInstancesInitialized;
        private List<DI_Class> guildControllers;

        public static string FilePath
        {
            get { return filePath; }
        }

        public static void Main(string[] args)
        => new MainClass().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            guildInstancesInitialized = false;

            DiscordSocketConfig config = new DiscordSocketConfig();
            config.AlwaysDownloadUsers = true;

            filePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if(client == null)
            {
                await Log(new LogMessage(LogSeverity.Info, this.ToString(), "Client null, making new client"));
                client = new DiscordSocketClient(config);
            }
            
            client.Log += Log;

            client.Ready += BootUpBot; //Ready is fired when the bot comes online and is connected to discord

            //discord people/bots/objects have a "token" AKA ID that is a password/username
            // not secure to hardcode token so instead will get it from saved file
            Console.WriteLine("token path: " + filePath);

            var token = File.ReadAllText(filePath + Path.DirectorySeparatorChar + @"token.txt");

            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            guildControllers = new List<DI_Class>();

            client.JoinedGuild += SetUpNewGuildInstance;

            // wait for an indefinite amount of time
            await Task.Delay(-1);
        }

        private Task Log(LogMessage msg)
        {
            using (System.IO.StreamWriter file =
            new System.IO.StreamWriter(filePath + Path.DirectorySeparatorChar + @"logs.txt", true))
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
                Log(new LogMessage(LogSeverity.Info, this.ToString(), "     Creating guild instances."));

                //for each guild create controller
                SocketGuild guild;
                IEnumerator<SocketGuild> guildE = client.Guilds.GetEnumerator();
                while (guildE.MoveNext())
                {
                    guild = guildE.Current;

                    DI_Class tempDIRef = new DI_Class(client, guild);

                    guildControllers.Add(tempDIRef);
                }

                guildInstancesInitialized = true;
            }

            return Task.CompletedTask;
        }

        private Task SetUpNewGuildInstance(SocketGuild newGuild)
        {
            DI_Class tempDIRef = GuildHasExistingInstance(newGuild);

            Console.WriteLine("tempDI: " + tempDIRef);

            //make sure doesn't already have instance
            if (tempDIRef != null)
            {
                //reset
                tempDIRef.Reset(newGuild);

                return Task.CompletedTask;
            }

            tempDIRef = new DI_Class(client, newGuild);

            tempDIRef.CommandHandRef.IntroMessage(newGuild);

            Log(new LogMessage(LogSeverity.Info, this.ToString(), $"     Created new guild instance for '{newGuild.Name}'"));

            return Task.CompletedTask;
        }

        private DI_Class GuildHasExistingInstance(SocketGuild guildToCheck)
        {
            foreach (DI_Class dI in guildControllers)
            {
                if (dI.GuildRef.Id.Equals(guildToCheck.Id))
                {
                    return dI;
                }
            }
            return null;
        }
    }
}
