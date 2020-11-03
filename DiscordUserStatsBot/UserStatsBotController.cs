using Discord;
using Discord.Commands;
using Discord.Net.Queue;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordUserStatsBot
{
    class UserStatsBotController
    {
        public static void Main(string[] args)
        => new UserStatsBotController().MainAsync().GetAwaiter().GetResult();


        #region VARIABLES
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
        private string ignoreAfterCommandString = "IACSn0ll";

        //playerStatIndex: a dictionary with user ids as the key and a userStatIndex struct(?)
        private Dictionary<ulong, UserStats> guildUserIDToStatIndex;
        private Dictionary<string, ulong> guildUserNameToIDIndex;

        UserStats beingProcessedUserStats;

        private event Func<SocketUser, Task> UserJoinedAVoiceChat;
        private event Func<SocketUser, Task> UserLeftAllVoiceChats;


        //--------------------------------------------------------------------------------------------------------------------------------------------------------------- 
        #endregion


        #region FUNCTIONS
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        #region InitFunctions
        public async Task MainAsync()
        {
            DiscordSocketConfig config = new DiscordSocketConfig();
            config.AlwaysDownloadUsers = true;

            client = new DiscordSocketClient(config);

            //restClient = new DiscordRestClient();
            
            
            //adds CommandHandler func to MessageRecieved delegate. Therefore CommandHandler will be executed anytime a message is posted on the discord server
            client.Log += Log;
            client.MessageReceived += CommandHandler;
            client.MessageReceived += DownloadMembers;
            client.UserVoiceStateUpdated += VoiceChatChange;
            client.Ready += BotSetUp; //Ready is fired when the bot connects to server
            client.GuildMembersDownloaded += DownloadMessage;
            client.MessageReceived += AddNewUserToStatBotIndex;
            UserJoinedAVoiceChat += AddNewUserToStatBotIndex;


            //TODO:
            //client.JoinedGuild += BotSetUp;

            //TODO: try this
            //BaseSocketClient baseClient;
            //baseClient.DownloadUsersAsync

            //discord people/bots/objects have a "token" AKA ID that is a password/username
            // not secure to hardcode token so instead will get it from saved file (under TomsDiscordBot->bin->Debug->netcoreapp3.1)
            var token = File.ReadAllText("token.txt");

            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            // wait for an indefinite amount of time
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
                    //TODO: get the guild it just connected to and set as guildRef. Something about Context?
                }

                Console.WriteLine($"Set up new guild reference to {guildRef.Name}");

                //set up user list
                guildUserIDToStatIndex = new Dictionary<ulong, UserStats>();
            }

            if (devMode)
            {
                //get debug channels
                debugTextChannelRef = guildRef.GetTextChannel(TBE_DEBUG_TEXT_CHANNEL_ID);
                debugVoiceChannelRef = guildRef.GetVoiceChannel(TBE_DEBUG_VOICE_CHANNEL_ID);
            }

            return Task.CompletedTask;
        }

        private async Task DownloadMembers(SocketMessage sm)
        {
            Console.WriteLine("try downloading users");

            //await guildRef.DownloadUsersAsync();

            Console.WriteLine("users downloaded?");
        }
        

        private Task DownloadMessage(SocketGuild guild)
        {
            Console.WriteLine("Offline users finished downloading event fired");

            Console.WriteLine($@"Number of downloaded members: {guildRef.DownloadedMemberCount}");

            return Task.CompletedTask;
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        #endregion

        #region VoiceChatFunctions
        private Task VoiceChatChange(SocketUser user, SocketVoiceState PreviousVoiceChat, SocketVoiceState CurrentVoiceChat)
        {
            if (CurrentVoiceChat.VoiceChannel != null)
            {
                UserJoinedAVoiceChat((SocketGuildUser)user);
            }
            else
            {
                UserLeftAllVoiceChats((SocketGuildUser)user);
            }

            return Task.CompletedTask;
        }

        /*
        private void UserJoinedAVoiceChat(SocketGuildUser guildUser)
        {
            //Get User stat class
            beingProcessedUserStats = GetOtherwiseCreateUserStats(guildUser);
            //Record the time the player entered chat in their assossiated userStat class
            beingProcessedUserStats.RecordGuildUserEnterVoiceChatTime();

        }

        private void UserLeftAllVoiceChats(SocketGuildUser guildUser)
        {
            //Get User stat class
            beingProcessedUserStats = GetOtherwiseCreateUserStats(guildUser);
            //Record the time the player entered chat in their assossiated userStat class
            beingProcessedUserStats.RecordGuildUserLeaveVoiceChatTime();
            //record how much time they were in chat
            beingProcessedUserStats.CalculateAndUpdateUserStats();
        }
        */
        #endregion

        #region CommandFunctions
        private Task CommandHandler(SocketMessage message)          //REMEMBER ALL COMMANDS MUST BE LOWERCASE
        {
            #region MessageFilter
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
            #endregion

            #region GetMessageCommandString
            //--------------------------------------------------------------------------------------------------
            string command = "";
            int lengthOfCommand = -1;

            //Only will take first word of command
            if (message.Content.Contains(' '))
            {
                //includes '!' in command length
                lengthOfCommand = message.Content.IndexOf(' ');
            }
            else
            {
                lengthOfCommand = message.Content.Length;
            }

            //                        Substring: you take specified section of string recieved. ToLower: makes string lowercase
            command = message.Content.Substring(1, lengthOfCommand - 1).ToLower();

            //--------------------------------------------------------------------------------------------------
            #endregion

            //COMMANDS 
            //REMEMBER: NO SPACES OR CAPITALS ALLOWED IN COMMANDS
            //--------------------------------------------------------------------------------------------------

            //COMMANDS HERE
            //------------------------------------------
            if (command.Equals("hi"))
            {
                message.Channel.SendMessageAsync("Hello fellow user.");
                return Task.CompletedTask;
            }
            //------------------------------------------

            //COMMANDS INVOLVING AFTER-COMMAND STRING HERE
            //------------------------------------------
            string stringAfterCommand = GetStringAfterCommand(message, lengthOfCommand).Result;

            //TODO: TEST AGAIN
            if (command.Equals("prefix"))
            {
                Console.WriteLine("Prefix command called");
                //if nothing after prefix then print out prefix otherwise set the prefix to 1st character after space
                if (stringAfterCommand.Equals(ignoreAfterCommandString))
                {
                    message.Channel.SendMessageAsync($@"The command prefix for UserStat bot is {botCommandPrefix}");
                    Console.WriteLine("Told user prefix");
                }
                else
                {
                    ChangePrefixCommand(message, stringAfterCommand);
                    Console.WriteLine("Set new prefix");
                }

                return Task.CompletedTask;
            }

            //get the total voice chat time of user
            //!totalchattime <username#0000> AKA <tag> //OBSELETE: OR !totalchattime userID
            if (command.Equals("totalchattime"))
            {

                //Enumerable example
                /*Console.WriteLine($@"Has all members? {guildRef.HasAllMembers}");

                Console.WriteLine($@"Number of downloaded members: {guildRef.DownloadedMemberCount}");


                SocketRole role;

                IEnumerator<SocketRole> roleE = guildRef.Roles.GetEnumerator();

                //get every user for @ everyone role
                while(roleE.MoveNext())
                {
                    role = roleE.Current;

                    Console.WriteLine($@"Role: {role.Name} Role members: {role.Members.Count()}");
                }
                */


                if (stringAfterCommand.Contains('#'))
                {
                    //split username#0000 and do GetUser(username, #0000);
                    int hashtagIndex = stringAfterCommand.IndexOf('#');
                    string usernameMinusDiscrim = stringAfterCommand.Substring(0, hashtagIndex).Trim();
                    string userDiscriminator = stringAfterCommand.Substring(hashtagIndex + 1, stringAfterCommand.Length - (hashtagIndex + 1)).Trim();
                    //to get rid of any space between username and discriminator
                    string fullUserName = usernameMinusDiscrim + userDiscriminator;

                    UserStats tempUserStat = GetUserStats(fullUserName);

                    if (fullUserName == null)
                    {
                        message.Channel.SendMessageAsync($@"Sorry there is no data on {fullUserName}. Maybe you misspelled it?");
                    }
                    else
                    {
                        message.Channel.SendMessageAsync($@"{usernameMinusDiscrim}'s total chat time is {tempUserStat.TotalVoiceChatTime}!");
                    }
                }
                else
                {
                    message.Channel.SendMessageAsync($@"Sorry that isn't a valid username#0000. Did you remember the discriminator?");
                }
            }
            //------------------------------------------

            return Task.CompletedTask;
        }

        private Task ChangePrefixCommand(SocketMessage message, string stringAfterCommand)
        {
            //only users who have manage guild permission can change the prefix

            //need to cast user to get var that tells me whether user can manage guild
            SocketGuildUser userGuild = (SocketGuildUser)(message.Author);

            if (userGuild.GuildPermissions.ManageGuild)
            {
                botCommandPrefix = stringAfterCommand[0];
                if(stringAfterCommand.Length > 1)
                {
                    message.Channel.SendMessageAsync($@"The command prefix for UserStat bot has been set to the first character typed {botCommandPrefix}");
                }
                else
                {
                    message.Channel.SendMessageAsync($@"The command prefix for UserStat bot has been set to {botCommandPrefix}");
                }
                
            }
            else
            {
                message.Channel.SendMessageAsync($@"Sorry, you need the Manage Guild permission in order to do this");
            }

            return Task.CompletedTask;
        }

        //returns ignoreAfterCommandString if nothing after commmand
        private Task<string> GetStringAfterCommand(SocketMessage message, int lengthOfCommand)
        {
            string stringAfterCommand = ignoreAfterCommandString;

            //if there is more content after the ' ' then set stringAfterCommand to that
            //lengthOfCommand includes prefix character, +1 Accounts for a " " after command
            if (message.Content.Length > lengthOfCommand + 1)
            {
                stringAfterCommand = message.Content.Substring(lengthOfCommand, message.Content.Length - (lengthOfCommand));
            }

            return Task<string>.FromResult(stringAfterCommand.Trim());
        }
        #endregion


        private Task AddNewUserToStatBotIndex(SocketUser user)
        {
            //if user not already in playerNameIndex
            if (!guildUserNameToIDIndex.ContainsKey(((SocketGuildUser)user).Username))
            {   
                //add them
                guildUserNameToIDIndex.Add((((SocketGuildUser)user).Username), user.Id);
                //make them a UserStat class instance and store it
                guildUserIDToStatIndex.Add(user.Id, new UserStats((SocketGuildUser)user));

                Console.WriteLine($@"Created a new userStat for {((SocketGuildUser)user).Username}");
            }

            return Task.CompletedTask;
        }

        private Task AddNewUserToStatBotIndex(SocketMessage message)
        {
            AddNewUserToStatBotIndex(message.Author);
            return Task.CompletedTask;
        }

        //returns null if no user in the dictionary
        private UserStats GetUserStats(string guildUserName)
        {
            UserStats userStatInst = null;

            //if user in guildUserNameIndex
            if (guildUserNameToIDIndex.ContainsKey(guildUserName))
            {
                userStatInst = guildUserIDToStatIndex[guildUserNameToIDIndex[guildUserName]];
            }
            else
            {
                Console.WriteLine("The bot has no recording of a user with that name");
            }

            return userStatInst;
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        #endregion
    }
}
