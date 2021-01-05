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
using System.Timers;


//CURRENT TASK:

//Debugging Issues: 

///Completed
///Added ranking by both messages and vctime
///Moved commands to seperate class
///made it so you can change the time interval between when user roles assigned
///made it so saves roles when they are changed



///FUTURE TASKS:
///Make it get the guild it's a part of on start up AKA make it so it deals with multiple guilds at once
///get specific day of week/month stats
///change it so averages/totals can take in a range of days
///Make it get the guild it's a part of on start up AKA make it so it deals with multiple guilds at once
///create roles to organize users into
///change so takes username and then if there is more than one user with that name prompts you for a discriminator. Also deals with nicknames.
///Allow people to also search for userstats with an ID
///When user changes their name this bots connection to the guild doesn't realise this AKA I still get the old name if I ask it to print the SocketGuildUser name. Is fixed when I restart the Bot. Same thing occurs with nickname. If I think a user has changed their name reset connetion?
///Bug: when role re-created reference to color is lost
///Make it so that re-made roles are placed in correct spot
///make it so that they can specify how many roles they want (3, 4, or 5)
///look up total/average time for users in given role
///allow admin to change number of max users per role. allow people to get how many max users per role
///call assign roles function every 24 hours
///make so can rank users by messages or by hours in chat or both(default)
///can rank users by month, week, or day
///convert iDToUserStat into just a userStat list
///REWRITE EVERYTHING NOW THAT YOU CAN GET OFFLINE USERS
///Get best users based off of messages + voice chat
///add admin limitations to commands
///When deleted role re-created ensure that it has correct position
/////add commands (see "help" command)

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

        

        //dictionaries that record users and their corresponding UserStats
        public Dictionary<ulong, UserStatTracker> guildUserIDToStatIndex;
        public Dictionary<string, ulong> guildUserNameToIDIndex;

        public event Func<SocketUser, Task> UserJoinedAVoiceChat;
        public event Func<SocketUser, Task> UserLeftAllVoiceChats;

        private bool trackBotStats = true;

        //TODO: Make these singletons
        public SaveHandler saveHandlerRef;
        public UserStatsRoles userStatRolesRef;
        private CommandHandler commandHandlerRef;

        //TODO: rank config settings to save
        //TODO: save list of rankedUsers in UserStatsRoles

        //assign roles timer
        private System.Timers.Timer assignRolesTimer;
        private TimeSpan assignRolesTimeSpan;

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

            //For record user stats
            client.MessageReceived += RecordMessageSent;
            UserJoinedAVoiceChat += StartRecordingVCTime;
            UserLeftAllVoiceChats += StopRecordingVCTime;

            client.RoleUpdated += SaveRoles;

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
                    //get guilds this bot is a part of
                    //... then makes a instance of this bot with a new directory for each?

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
            if(saveHandlerRef == null)
            saveHandlerRef = new SaveHandler();
            if(userStatRolesRef == null)
            userStatRolesRef = new UserStatsRoles(this);
            if (commandHandlerRef == null)
            {
                commandHandlerRef = new CommandHandler(this, guildRef);
                client.MessageReceived += commandHandlerRef.CommandHandlerFunc; //adds CommandHandler func to MessageRecieved event delegate. Therefore CommandHandler will be executed anytime a message is posted on the discord server
                commandHandlerRef.wasBotCommand = false;
            }
                



            //LOADING
            //set up user dictionaries and load any info that already exists
            saveHandlerRef.LoadDictionary(out guildUserIDToStatIndex, nameof(guildUserIDToStatIndex)); //out keyword passes by reference instead of value
            saveHandlerRef.LoadDictionary(out guildUserNameToIDIndex, nameof(guildUserNameToIDIndex));

            if (guildUserNameToIDIndex == null)
            {
                guildUserNameToIDIndex = new Dictionary<string, ulong>();
                //Console.WriteLine("New dictionary to save usernames and IDs made.");
            }
            if (guildUserIDToStatIndex == null)
            {
                guildUserIDToStatIndex = new Dictionary<ulong, UserStatTracker>();
                //Console.WriteLine("New dictionary to save IDs and UserStats made.");
            }

            //load/create roles
            saveHandlerRef.LoadArray(out userStatRolesRef.rankRoles, userStatRolesRef.rolesSaveFileName);
            if(userStatRolesRef.rankRoles == null)
            {
                userStatRolesRef.CreateDefaultRolesArray();
            }
            userStatRolesRef.CreateRoles(guildRef);
            //save roles
            userStatRolesRef.SaveRoles(guildRef, saveHandlerRef);



            //calculate/assign user roles

            userStatRolesRef.AssignRoles(guildRef);

            //default
            assignRolesTimeSpan = new TimeSpan(0, 0, 36);
            AssignRolesTimer(assignRolesTimeSpan);

            SocketGuildUser user;
            IEnumerator<SocketGuildUser> userE = guildRef.Users.GetEnumerator();

            Console.WriteLine("GUILD USERS:");
            while (userE.MoveNext())
            {
                user = userE.Current;

                Console.WriteLine($@"   User in guild: {user.Username}");
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

        
        public Task StartRecordingVCTime(SocketUser user)
        {
            //Get User stat class
            UserStatTracker tempUserStatsRef = GetUserStats(GetUserNamePlusDiscrim((SocketGuildUser)user));
            //Record the time the player entered chat in their assossiated userStat class
            tempUserStatsRef.RecordGuildUserEnterVoiceChatTime();

            return Task.CompletedTask;
        }

        public Task StopRecordingVCTime(SocketUser user)
        {
            UserStatTracker tempUserStatsRef = GetUserStats(GetUserNamePlusDiscrim((SocketGuildUser)user));
            tempUserStatsRef.RecordGuildUserLeaveVoiceChatTime();
            saveHandlerRef.SaveDictionary(guildUserIDToStatIndex, nameof(guildUserIDToStatIndex));

            return Task.CompletedTask;
        }

        private Task RecordMessageSent(SocketMessage message)
        {
            //don't count bot commands as messages
            if (commandHandlerRef.wasBotCommand)
            {
                commandHandlerRef.wasBotCommand = false;
                return Task.CompletedTask;
            }

            UserStatTracker tempUserStatsRef;

            if(guildUserIDToStatIndex.TryGetValue(message.Author.Id, out tempUserStatsRef))
            {
                tempUserStatsRef.RecordThisUserSentAMessage(message);

                saveHandlerRef.SaveDictionary(guildUserIDToStatIndex, nameof(guildUserIDToStatIndex));
            }

            return Task.CompletedTask;
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
                guildUserIDToStatIndex.Add(user.Id, new UserStatTracker(this, usernamePlusDiscrim, user.Id));
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
                guildUserIDToStatIndex.Add(user.Id, new UserStatTracker(this, usernamePlusDiscrim, user.Id));

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
        public UserStatTracker GetUserStats(string userName)
        {
            UserStatTracker userStatInst = null;

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
        public ulong GetUserIDFromName(string userName)
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

        public bool UserIsInChat(SocketUser userInQuestion)
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
                        //Console.WriteLine("Matching user in chat");
                        return true;
                    }
                }
            }

            //Console.WriteLine("No matching user in chat");
            return false;
        }

        private void AssignRolesTimer(TimeSpan timeTillNextAssignRoles)
        {
            assignRolesTimer = new System.Timers.Timer(timeTillNextAssignRoles.TotalMilliseconds);
            Console.WriteLine($@"Timer interval is {assignRolesTimer.Interval} milliseconds");

            assignRolesTimer.Elapsed += AssignRolesTimerCallback;
            assignRolesTimer.AutoReset = true;
            assignRolesTimer.Enabled = true;
        }

        private void AssignRolesTimerCallback(Object source, ElapsedEventArgs e)
        {
            userStatRolesRef.AssignRoles(guildRef);
        }

        public void ChangeAssignRolesInterval(TimeSpan interval)
        {
            assignRolesTimer.Interval = interval.TotalMilliseconds;
            userStatRolesRef.AssignRoles(guildRef);
            Console.WriteLine("Assign timer interval changed");
        }

        private Task SaveRoles(SocketRole roleBefore, SocketRole roleAfter)
        {
            //save roles
            userStatRolesRef.SaveRoles(guildRef, saveHandlerRef);

            Console.WriteLine("Roles Saved");

            return Task.CompletedTask;
        }

        #endregion

        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        #endregion
    }
}