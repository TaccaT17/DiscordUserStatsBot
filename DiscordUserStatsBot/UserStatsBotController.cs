using Discord;
using Discord.Commands;
using Discord.Net.Queue;
using Discord.Rest;
using Discord.WebSocket;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;


//CURRENT TASK:

//Debugging Issues: set up so records/calculates averages, records/calculates totals, etc.

///Completed


///FUTURE TASKS:
///set up so records specific days of the week, averages, etc.
///Make it get the guild it's a part of on start up
///create roles to organize users into
///change so takes username and then if there is more than one user with that name prompts you for a discriminator. Also deals with nicknames.
///Allow people to also search for userstats with an ID
///When user changes their name this bots connection to the guild doesn't realise this AKA I still get the old name if I ask it to print the SocketGuildUser name. Is fixed when I restart the Bot. Same thing occurs with nickname. If I think a user has changed their name reset connetion?

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

        //dictionaries that record users and their corresponding UserStats
        public Dictionary<ulong, UserStats> guildUserIDToStatIndex;
        public Dictionary<string, ulong> guildUserNameToIDIndex;

        public event Func<SocketUser, Task> UserJoinedAVoiceChat;
        public event Func<SocketUser, Task> UserLeftAllVoiceChats;

        private bool trackBotStats = true;

        private SaveHandler saveHandlerRef;

        //bool to stop commands for this bot from being recorded in UserStat
        private bool wasBotCommand;

        //Commands
        private string GreetCommand = "Hi", 
            prefixCommand = "Prefix", 
            totalChatTimeCommand = "TotalChatTime", 
            totalMessagesSentCommand = "TotalMessagesSent";

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
            
            client.Log += Log;

            client.UserVoiceStateUpdated += VoiceChatChange;

            client.Ready += BotSetUp; //Ready is fired when the bot connects to server

            //Times when the bot will create an entry for a user
            client.UserJoined += AddNewUserToStatBotIndex;
            client.MessageReceived += AddNewUserToStatBotIndex;
            UserJoinedAVoiceChat += AddNewUserToStatBotIndex;

            client.MessageReceived += CommandHandler; //adds CommandHandler func to MessageRecieved event delegate. Therefore CommandHandler will be executed anytime a message is posted on the discord server

            //For record user stats
            client.MessageReceived += RecordMessageSent;
            UserJoinedAVoiceChat += StartRecordingVCTime;
            UserLeftAllVoiceChats += StopRecordingVCTime;

            //discord people/bots/objects have a "token" AKA ID that is a password/username
            // not secure to hardcode token so instead will get it from saved file (under TomsDiscordBot->bin->Debug->netcoreapp3.1)
            var token = File.ReadAllText("token.txt");

            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            wasBotCommand = false;

            // wait for an indefinite amount of time
            await Task.Delay(-1);
        }
     
        private Task BotSetUp()
        {
            //TODO:
            //called when bot either joins guild, when bot comes online. 
            //Will check to see if bot set up and up to date. If not will update.

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

                //TODO: get relevant info AKA if this bot is new to the server make dictioneries, if not get dictionaries
            }

            if (devMode)
            {
                //get debug channels
                debugTextChannelRef = guildRef.GetTextChannel(TBE_DEBUG_TEXT_CHANNEL_ID);
                debugVoiceChannelRef = guildRef.GetVoiceChannel(TBE_DEBUG_VOICE_CHANNEL_ID);
            }

            //make save class
            saveHandlerRef = new SaveHandler();

            //set up user dictionaries and load any info that already exists
            saveHandlerRef.LoadDictionary(out guildUserIDToStatIndex, nameof(guildUserIDToStatIndex)); //out keyword passes by reference instead of value
            saveHandlerRef.LoadDictionary(out guildUserNameToIDIndex, nameof(guildUserNameToIDIndex));

            if (guildUserNameToIDIndex == null)
            {
                guildUserNameToIDIndex = new Dictionary<string, ulong>();
                Console.WriteLine("New dictionary to save usernames and IDs made.");
            }
            if (guildUserIDToStatIndex == null)
            {
                guildUserIDToStatIndex = new Dictionary<ulong, UserStats>();
                Console.WriteLine("New dictionary to save IDs and UserStats made.");
            }

            Console.WriteLine("Bot set up");

            return Task.CompletedTask;
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        #endregion

        #region RecordStatsFunctions
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

        //TODO: Shouldn't these be inside the UserStats class?
        private Task StartRecordingVCTime(SocketUser user)
        {
            //Get User stat class
            UserStats tempUserStatsRef = GetUserStats(GetUserNamePlusDiscrim((SocketGuildUser)user));
            //Record the time the player entered chat in their assossiated userStat class
            tempUserStatsRef.RecordGuildUserEnterVoiceChatTime();

            return Task.CompletedTask;
        }

        private Task StopRecordingVCTime(SocketUser user)
        {
            UserStats tempUserStatsRef = GetUserStats(GetUserNamePlusDiscrim((SocketGuildUser)user));
            tempUserStatsRef.RecordGuildUserLeaveVoiceChatTime();
            saveHandlerRef.SaveDictionary(guildUserIDToStatIndex, nameof(guildUserIDToStatIndex));

            return Task.CompletedTask;
        }

        private Task RecordMessageSent(SocketMessage message)
        {
            //don't count bot commands as messages
            if (wasBotCommand)
            {
                wasBotCommand = false;
                return Task.CompletedTask;
            }

            UserStats tempUserStatsRef;

            if(guildUserIDToStatIndex.TryGetValue(message.Author.Id, out tempUserStatsRef))
            {
                tempUserStatsRef.RecordThisUserSentAMessage(message);

                saveHandlerRef.SaveDictionary(guildUserIDToStatIndex, nameof(guildUserIDToStatIndex));
            }

            return Task.CompletedTask;
        }

        #endregion

        #region CommandFunctions
        private Task CommandHandler(SocketMessage message)          //REMEMBER ALL COMMANDS MUST BE LOWERCASE
        {
            #region MessageFilter
            //--------------------------------------------------------------------------------------------------
            //rule out messages that don't have bot prefix
            if (!message.Content.StartsWith(botCommandPrefix))
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

            #region COMMANDS
            //REMEMBER: NO SPACES ALLOWED IN COMMANDS
            //--------------------------------------------------------------------------------------------------

            //COMMANDS
            //------------------------------------------
            if (command.Equals(GreetCommand.ToLower()))
            {
                wasBotCommand = true;
                message.Channel.SendMessageAsync("Hello fellow user.");
                return Task.CompletedTask;
            }
            //------------------------------------------

            //COMMANDS INVOLVING AFTER-COMMAND STRING HERE
            //------------------------------------------
            string stringAfterCommand = GetStringAfterCommand(message, lengthOfCommand).Result;

            //TODO: TEST AGAIN
            if (command.Equals(prefixCommand.ToLower()))
            {
                wasBotCommand = true;

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
            else if (command.Equals(totalChatTimeCommand.ToLower()))
            {
                wasBotCommand = true;

                string fullUserName = ReformatStringToUsername(stringAfterCommand).Result;

                //if not valid string
                if (fullUserName == null)
                {
                    message.Channel.SendMessageAsync($@"Sorry that isn't a valid username#0000. Did you remember the discriminator?");
                    return Task.CompletedTask;
                }
                else
                {
                    UserStats tempUserStat = GetUserStats(fullUserName);

                    if (tempUserStat == null)
                    {
                        message.Channel.SendMessageAsync($@"Sorry there is no data on {fullUserName}.");
                        //This could possibly be a logic error: one of the two dictionaries is lacking an entry for this user

                        return Task.CompletedTask;
                    }

                    if(GetUserID(fullUserName) != 0 && (guildRef.GetUser(GetUserID(fullUserName)) != null))
                    {
                        SocketGuildUser guildUser = guildRef.GetUser(GetUserID(fullUserName));

                        //if user in a chat update their time before sending message, otherwise just send message
                        if (UserIsInChat(guildUser))
                        {
                            StopRecordingVCTime(guildUser);
                            message.Channel.SendMessageAsync($@"{tempUserStat.UsersFullName}'s total chat time is " +
                                                             $@"{tempUserStat.TotalVoiceChatTime.Days} days, " +
                                                             $@"{tempUserStat.TotalVoiceChatTime.Hours} hours, " +
                                                             $@"{tempUserStat.TotalVoiceChatTime.Minutes} minutes and " +
                                                             $@"{tempUserStat.TotalVoiceChatTime.Seconds} seconds!");
                            StartRecordingVCTime(guildUser);
                        }
                        else
                        {
                            message.Channel.SendMessageAsync($@"{tempUserStat.UsersFullName}'s total chat time is " +
                                                             $@"{tempUserStat.TotalVoiceChatTime.Days} days, " +
                                                             $@"{tempUserStat.TotalVoiceChatTime.Hours} hours, " +
                                                             $@"{tempUserStat.TotalVoiceChatTime.Minutes} minutes and " +
                                                             $@"{tempUserStat.TotalVoiceChatTime.Seconds} seconds!");
                        }
                    }
                }
            }
            //get the total messages sent by user
            else if (command.Equals(totalMessagesSentCommand.ToLower()))
            {
                wasBotCommand = true;

                string fullUserName = ReformatStringToUsername(stringAfterCommand).Result;

                //if not valid string
                if (fullUserName == null)
                {
                    message.Channel.SendMessageAsync($@"Sorry that isn't a valid username#0000. Did you remember the discriminator?");
                    return Task.CompletedTask;
                }
                else
                {
                    UserStats tempUserStat = GetUserStats(fullUserName);

                    if (tempUserStat == null)
                    {
                        message.Channel.SendMessageAsync($@"Sorry there is no data on {fullUserName}.");
                        //This could possibly be a logic error: one of the two dictionaries is lacking an entry for this user

                        return Task.CompletedTask;
                    }

                    message.Channel.SendMessageAsync($@"{tempUserStat.UsersFullName} has sent {tempUserStat.TotalMessagesSent} meaningful messages!");
                }
            }

            //------------------------------------------
            //--------------------------------------------------------------------------------------------------
            #endregion

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
                if (stringAfterCommand.Length > 1)
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

        private Task<string> ReformatStringToUsername(string unformattedUsernameString)
        {
            string fullUserName = null;

            if (unformattedUsernameString.Contains('#'))
            {
                //get username string from stringAfterCommand
                int hashtagIndex = unformattedUsernameString.IndexOf('#');
                string usernameMinusDiscrim = unformattedUsernameString.Substring(0, hashtagIndex).Trim();
                string userDiscriminator = unformattedUsernameString.Substring(hashtagIndex + 1, unformattedUsernameString.Length - (hashtagIndex + 1)).Trim();

                //to get rid of any space between username and discriminator
                fullUserName = usernameMinusDiscrim + '#' + userDiscriminator;
            }
            else
            {
                Console.WriteLine("Invalid string. Cannot convert to a username");
            }
            return Task<string>.FromResult(fullUserName);
        }

        #endregion

        #region UserStatFunctions
        //TODO: way to deal with when user changes their name

        private Task AddNewUserToStatBotIndex(SocketUser user)
        {

            //track bot stats?
            if (!trackBotStats && user.IsBot)
            {
                return Task.CompletedTask;
            }

            //dictionary stores users using username + discriminator as key
            string usernamePlusDiscrim = GetUserNamePlusDiscrim((SocketGuildUser)user);

            //if there is an entry in both dictionaries you're good
            if (guildUserNameToIDIndex.ContainsKey(usernamePlusDiscrim) && guildUserIDToStatIndex.ContainsKey(user.Id))
            {
                return Task.CompletedTask;
            }
            //if doesn't have entry in either dictionary make new entry for both
            else if (!(guildUserNameToIDIndex.ContainsKey(usernamePlusDiscrim)) && !(guildUserIDToStatIndex.ContainsKey(user.Id)))
            {
                guildUserNameToIDIndex.Add(usernamePlusDiscrim, user.Id);
                guildUserIDToStatIndex.Add(user.Id, new UserStats(this, usernamePlusDiscrim));
                Console.WriteLine($@"Created a new stat tracker for {usernamePlusDiscrim}");
                //save dictionariess
                saveHandlerRef.SaveDictionary(guildUserNameToIDIndex, nameof(guildUserNameToIDIndex));
                saveHandlerRef.SaveDictionary(guildUserIDToStatIndex, nameof(guildUserIDToStatIndex));
                return Task.CompletedTask;
            }
            //if just doesn't have id/stat entry make a new one (prior info unfortunately lost) 
            else if (guildUserNameToIDIndex.ContainsKey(usernamePlusDiscrim) && !(guildUserIDToStatIndex.ContainsKey(user.Id)))
            {
                //caused by JSON dictionary having been deleted. Make a new id/user entry  
                guildUserIDToStatIndex.Add(user.Id, new UserStats(this, usernamePlusDiscrim));

                saveHandlerRef.SaveDictionary(guildUserIDToStatIndex, nameof(guildUserIDToStatIndex));

                Console.WriteLine($@"ID/UserStat dictionary was at some point deleted. Therefore making a fresh ID/UserStat entry for {usernamePlusDiscrim}.");

                return Task.CompletedTask;
            }
            //just doesn't have username/id entry...
            else
            {
                //caused by JSON dictionary having been deleted OR user changed their name OR both
                //...need to either update username, remake JSON dictionary or both

                bool userChangedTheirName = false;

                //create loop that looks for a corresponding ID entry in username/ID dictionary
                foreach (var item in guildUserNameToIDIndex)
                {
                    //if you find the same ID...
                    if (!userChangedTheirName && item.Value.Equals(user.Id))
                    {
                        //...user changed their name:
                        userChangedTheirName = true;

                        //replace that entry with current username
                        guildUserNameToIDIndex.Remove(item.Key);
                        guildUserNameToIDIndex.Add(usernamePlusDiscrim, user.Id);

                        //update userstat username to reflect this
                        guildUserIDToStatIndex[user.Id].UpdateUsersName((SocketGuildUser)user);

                        Console.WriteLine($@"{usernamePlusDiscrim} changed their username! Replaced their Username/ID entry {item.Key}, {item.Value} with {usernamePlusDiscrim}, {user.Id}");
                    }
                }

                //...otherwise JSON dict has been deleted so create fresh entry
                if (!userChangedTheirName)
                {
                    guildUserNameToIDIndex.Add(usernamePlusDiscrim, user.Id);
                    Console.WriteLine($@"Username/ID dictionary was at some point deleted. Therefore making a fresh Username/ID entry for {usernamePlusDiscrim}.");
                }

                saveHandlerRef.SaveDictionary(guildUserNameToIDIndex, nameof(guildUserNameToIDIndex));

                return Task.CompletedTask;
            }
        }

        private Task AddNewUserToStatBotIndex(SocketMessage message)
        {
            AddNewUserToStatBotIndex(message.Author);
            return Task.CompletedTask;
        }

        //returns null if no user in the dictionary
        private UserStats GetUserStats(string userName)
        {
            UserStats userStatInst = null;

            //if user in guildUserNameIndex
            if (guildUserNameToIDIndex.ContainsKey(userName))
            {
                userStatInst = guildUserIDToStatIndex[guildUserNameToIDIndex[userName]];
            }
            else
            {
                Console.WriteLine("The bot has no recording of a user with that name: userstat");
            }

            return userStatInst;
        }

        //returns 0 if no corresponding id in dictionary
        private ulong GetUserID(string userName)
        {
            ulong userID = 0;

            //if user in guildUserNameIndex
            if (guildUserNameToIDIndex.ContainsKey(userName))
            {
                userID = guildUserNameToIDIndex[userName];
            }
            else
            {
                Console.WriteLine("The bot has no recording of a user with that name: id");
            }

            return userID;
        }

        #endregion

        #region MiscFunctions
        private string GetUserNamePlusDiscrim(SocketGuildUser guildUser)
        {
            return guildUser.Username + '#' + guildUser.Discriminator;
        }

        private bool UserIsInChat(SocketUser userInQuestion)
        {
            //loop through active channels and users in them 
            //Note: contrary to the documentation guildRef.VoiceChannels only gets channels that are being actively used
            //Note: contrary to the documentation VoiceChannel.Users only gets users currently in that channel

            SocketVoiceChannel voiceChannel;
            IEnumerator<SocketVoiceChannel> voiceChannelE = guildRef.VoiceChannels.GetEnumerator();

            while (voiceChannelE.MoveNext())
            {
                voiceChannel = voiceChannelE.Current;

                SocketUser userInVC;

                IEnumerator<SocketUser> userInVC_E = voiceChannel.Users.GetEnumerator();

                while (userInVC_E.MoveNext())
                {
                    userInVC = userInVC_E.Current;

                    //Console.WriteLine($@"Chat: {voiceChannel.Name} Chat user username: {userLoop.Username}");

                    //check to see if the user in chat is the same as the user being asked about
                    if (userInVC.Id.Equals(userInQuestion.Id))
                    {
                        Console.WriteLine("Matching user in chat");
                        return true;
                    }
                }
            }

            Console.WriteLine("No matching user in chat");
            return false;
        }

        #endregion

        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        #endregion
    }
}