using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DiscordUserStatsBot
{
    class UserStatsBotController
    {
        public static void Main(string[] args)
        => new UserStatsBotController().MainAsync().GetAwaiter().GetResult();


        //VARIABLES START
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------

        private DiscordSocketClient client; //         <--------------------------------THIS IS YOUR REFERENCE TO EVERYTHING

        private bool devMode = true;

        //TODO: make these generic
        private const ulong TBE_GUILD_ID = 767845719570382900;
        private const ulong TBE_DEBUG_TEXT_CHANNEL_ID = 767845719570382903;
        private const ulong TBE_DEBUG_VOICE_CHANNEL_ID = 767845719570382904;



        private SocketGuild guildRef;
        private SocketTextChannel debugTextChannelRef;
        private SocketVoiceChannel debugVoiceChannelRef;

        private char botCommandPrefix = '!';

        //playerStatIndex: a dictionary with user ids as the key and a userStatIndex struct(?)


        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        //VARIABLES END




        //FUNCTIONS START
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        public async Task MainAsync()
        {
            client = new DiscordSocketClient();

            //adds CommandHandler func to MessageRecieved delegate. Therefore CommandHandler will be executed anytime a message is posted on the discord server
            client.MessageReceived += CommandHandler;
            client.UserVoiceStateUpdated += VoiceChatChange;
            client.Ready += BotSetUp; //Ready is fired when the bot connects to server

            //TODO:
            //_client.JoinedGuild += BotSetUp;

            client.Log += Log;

            //discord people/bots/objects have a "token" AKA ID that is a password/username
            // not secure to hardcode token so instead will get it from saved file (under TomsDiscordBot->bin->Debug->netcoreapp3.1)
            var token = File.ReadAllText("token.txt");

            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private Task BotSetUp()
        {
            //TODO:
            //called when bot either joins guild, when bot comes online, . 
            //Will check to see if bot set up and up to date. If not will update.

            //Let people know bot updating
            Console.WriteLine("BotBeing set up");

            //get guild(AKA server)
            if (guildRef == null)
            {
                if (devMode)
                {
                    guildRef = client.GetGuild(TBE_GUILD_ID);
                }
                else
                {
                    //TODO: get the guild it just connected to and set as guildRef
                }

                Console.WriteLine($"Set up new guild reference to {guildRef.Name}");
            }

            if (devMode)
            {
                //get debug channels
                debugTextChannelRef = guildRef.GetTextChannel(TBE_DEBUG_TEXT_CHANNEL_ID);
                debugVoiceChannelRef = guildRef.GetVoiceChannel(TBE_DEBUG_VOICE_CHANNEL_ID);
            }


            return Task.CompletedTask;
        }

        private Task VoiceChatChange(SocketUser user, SocketVoiceState PreviousVoiceChat, SocketVoiceState CurrentVoiceChat)
        {

            if (CurrentVoiceChat.VoiceChannel != null)
            {
                UserJoinedAVoiceChat(user);
            }
            else
            {
                UserLeftAllVoiceChats(user);
            }


            return Task.CompletedTask;
        }

        private void UserJoinedAVoiceChat(SocketUser user)
        {
            //debugTextChannelRef.SendMessageAsync($"{user.Username} joined a chat!");

            //If user doesn't have a userStat struct assossciated with them then create one and add it to the userStatIndex dictionary
            //Record the time the player entered chat in their assossiated userStat struct

        }

        private void UserLeftAllVoiceChats(SocketUser user)
        {
            //debugTextChannelRef.SendMessageAsync($"{user.Username} left all voice chats.");

            //If user doesn't have a userStat struct assossciated with them then create one and add it to the userStatIndex dictionary
            //calculate amount of time player spent in voice and add it to their "totalTimeSpentInVoiceChat" stat in their assossciated userStat struct

        }

        private Task CommandHandler(SocketMessage message)          //REMEMBER ALL COMMANDS MUST BE LOWERCASE
        {
            //FILTER OUT MESSAGES WE DONT WANT TO ANALYZE
            //--------------------------------------------------------------------------------------------------
            //rule out messages that don't have bot prefix
            if (!message.Content.StartsWith(botCommandPrefix))                   //BOT PREFIX
            {
                //Console.WriteLine("Message is not a bot command");
                return Task.CompletedTask;
            }

            //rule out messages that bots (including itself) create
            else if (message.Author.IsBot)
            {
                //Console.WriteLine("Message is from a bot");
                return Task.CompletedTask;
            }
            //--------------------------------------------------------------------------------------------------

            //GET LOWERCASE MESSAGE STRING
            //--------------------------------------------------------------------------------------------------
            string command = "";
            int lengthOfCommand = -1;

            //Only will take first word of command
            if (message.Content.Contains(' '))
            {
                lengthOfCommand = message.Content.IndexOf(' ');
            }
            else
            {
                lengthOfCommand = message.Content.Length;
            }

            //                        Substring: you take specified section of string recieved. ToLower: makes string lowercase
            command = message.Content.Substring(1, lengthOfCommand - 1).ToLower();

            //--------------------------------------------------------------------------------------------------

            //COMMANDS 
            //REMEMBER: NO SPACES ALLOWED IN COMMANDS
            //--------------------------------------------------------------------------------------------------

            //TODO: TEST
            if (command.Equals("prefix"))
            {
                //if nothing after prefix then print out prefix otherwise set the prefix to 1st character after space
                if (message.Content.Length <= lengthOfCommand + 1)
                {
                    message.Channel.SendMessageAsync($@"The command prefix for UserStat bot is {botCommandPrefix}");
                    Console.WriteLine("Told user prefix");
                }
                else
                {
                    ChangePrefixCommand(message);
                    Console.WriteLine("Set new prefix");
                }

            }

            //--------------------------------------------------------------------------------------------------
            return Task.CompletedTask;
        }

        private Task ChangePrefixCommand(SocketMessage message)
        {
            //only users who have manage guild permission can change the prefix

            //need to cast user to get var that tells me whether user can manage guild
            SocketGuildUser userGuild = (SocketGuildUser)(message.Author);

            if (userGuild.GuildPermissions.ManageGuild)
            {
                int newPrefixIndex = message.Content.IndexOf(' ') + 1;
                botCommandPrefix = message.Content.ToCharArray()[newPrefixIndex];

                message.Channel.SendMessageAsync($@"The command prefix for UserStat bot has been set to {botCommandPrefix}");
            }
            else
            {
                message.Channel.SendMessageAsync($@"Sorry, you need the Manage Guild permission in order to do this");
            }

            return Task.CompletedTask;
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        //FUNCTIONS END
    }
}
