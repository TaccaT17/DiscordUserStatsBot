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
///

///FUTURE TASKS:
///Make it get the guild it's a part of on start up AKA make it so it deals with multiple guilds at once
///get specific day of week/month stats
///change so takes username and then if there is more than one user with that name prompts you for a discriminator. Also deals with nicknames.
///Allow people to also search for userstats with an ID
///When user changes their name this bots connection to the guild doesn't realise this AKA I still get the old name if I ask it to print the SocketGuildUser name. Is fixed when I restart the Bot. Same thing occurs with nickname. If I think a user has changed their name reset connetion?
///Bug: when role re-created reference to color is lost
///Make it so that re-made roles are placed in correct spot
///make it so that they can specify how many roles they want (3, 4, or 5)
///look up total/average voice chat time/messages for all users in a given role
///look up average voice chat time/messages for specific user
///allow admin to change number of max users per role.
///convert iDToUserStat into just a userStat list
///REWRITE EVERYTHING NOW THAT YOU CAN GET OFFLINE USERS
///add admin limitations to commands
///account for internet dropping out
///command that stops users from being organized in the sidebar by the rankRoles
///TODO: Compress GetUserStats and GetUserIDFromName
///put all JSON save files into same folder
///Ensure that when any save file deleted bot can deal with it

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
        private DateTime assignRolesStartTime;

        [Newtonsoft.Json.JsonProperty]
        private UserStatTracker.RankConfig rankConfigSave;

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

            //Times when the bot will create an entry for a user
            //client.UserJoined += AddNewUserToStatBotIndex;
            client.MessageReceived += AddNewUserToStatBotIndex;
            UserJoinedAVoiceChat += AddNewUserToStatBotIndex;

            client.Ready += BotSetUp; //Ready is fired when the bot connects to server

            //For record user stats
            UserJoinedAVoiceChat += StartRecordingVCTime;
            UserLeftAllVoiceChats += StopRecordingVCTime;

            client.RoleUpdated += SaveRolesSub;

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

            //make save, roles, command classes
            if(saveHandlerRef == null)
            saveHandlerRef = new SaveHandler();
            if(userStatRolesRef == null)
            userStatRolesRef = new UserStatsRoles(this);
            if (commandHandlerRef == null)
            {
                commandHandlerRef = new CommandHandler(this, guildRef);
                client.MessageReceived += commandHandlerRef.CommandHandlerFunc; //adds CommandHandler func to MessageRecieved event delegate. Therefore CommandHandler will be executed anytime a message is posted on the discord server
                //for recording user stats
                client.MessageReceived += RecordMessageSent;
                commandHandlerRef.wasBotCommand = false;
            }


            LoadAllBotInfo();

            //create roles
            userStatRolesRef.CreateRoles(guildRef);

            //start timer
            AssignRolesTimer(assignRolesTimeSpan);

            //calculate/assign user roles
            userStatRolesRef.AssignRoles(guildRef);

            SaveAllBotInfo();

            /*SocketGuildUser user;
            IEnumerator<SocketGuildUser> userE = guildRef.Users.GetEnumerator();

            Console.WriteLine("GUILD USERS:");
            while (userE.MoveNext())
            {
                user = userE.Current;

                Console.WriteLine($@"   User in guild: {user.Username}");
            }*/

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

        /// <summary>
        /// returns null if no user in the dictionary
        /// </summary>
        public UserStatTracker GetUserStats(string userName)
        {
            UserStatTracker userStatInst = null;

            //TODO: make this function more efficient
            //if user in guildUserNameIndex
            if (guildUserNameToIDIndex.ContainsKey(userName))
            {
                userStatInst = guildUserIDToStatIndex[guildUserNameToIDIndex[userName]];
            }
            else
            {
                int usersWithName = 0;
                string foundUser = "";

                //see if they just weren't capitalizing correctly or were excluding discriminator
                foreach(KeyValuePair<string, ulong> item in guildUserNameToIDIndex)
                {
                    //Console.WriteLine($@"   Name attempt: {item.Key.ToLower().Substring(0, item.Key.Length - 5)}");

                    //Console.WriteLine($@"User name is {(item.Key.ToLower().Substring(0, item.Key.Length - 5))}");
                    //Console.WriteLine($@"userName is {userName}");

                    if (item.Key.ToLower().Equals(userName.ToLower()) || (item.Key.ToLower().Substring(0, item.Key.Length - 5)).Equals(userName.ToLower()))
                    {
                        foundUser = item.Key;
                        usersWithName++;
                        //Console.WriteLine($@"       Found user with name {userName}");
                    }
                }

                if (usersWithName > 1)
                {
                    Console.WriteLine("Found multiple users with same name. : getuserstat");
                    return userStatInst;
                }
                else
                {
                    userName = foundUser;
                }

                if (guildUserNameToIDIndex.ContainsKey(userName))
                {
                    userStatInst = guildUserIDToStatIndex[guildUserNameToIDIndex[userName]];
                }
                else
                {
                    Console.WriteLine("The bot has no recording of a user with that name: userstat");
                }
            }

            return userStatInst;
        }

        /// <summary>
        /// returns 0 if no corresponding id in dictionary
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public ulong GetUserIDFromName(string userName)
        {
            ulong userID = 0;

            //TODO: make more efficient
            //if user in guildUserNameIndex
            if (guildUserNameToIDIndex.ContainsKey(userName))
            {
                userID = guildUserNameToIDIndex[userName];
            }
            else
            {
                int usersWithName = 0;
                string foundUser = "";

                //see if they just weren't capitalizing correctly or lacking discriminator
                foreach (KeyValuePair<string, ulong> item in guildUserNameToIDIndex)
                {
                    if (item.Key.ToLower().Equals(userName.ToLower()) || item.Key.ToLower().Substring(0, item.Key.Length - 5).Equals(userName.ToLower()))
                    {
                        userName = item.Key;
                        usersWithName++;
                        //Console.WriteLine($@"Found user with name {userName}");
                    }
                }

                if (usersWithName > 1)
                {
                    //Console.WriteLine("Found multiple users with same name. : getuserstat");
                    return userID;
                }
                else
                {
                    userName = foundUser;
                }
                
                if (guildUserNameToIDIndex.ContainsKey(userName))
                {
                    userID = guildUserNameToIDIndex[userName];
                }
                else
                {
                    Console.WriteLine("The bot has no recording of a user with that name: id");
                }
            }

            return userID;
        }

        #endregion

        #region TimerFunctions
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
            assignRolesStartTime = DateTime.Now;

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
            assignRolesTimeSpan = interval;
            assignRolesTimer.Interval = interval.TotalMilliseconds;
            userStatRolesRef.AssignRoles(guildRef);
            assignRolesStartTime = DateTime.Now;
            //save assignRolesTimer
            saveHandlerRef.SaveObject(assignRolesTimeSpan, nameof(assignRolesTimeSpan));
            Console.WriteLine("Assign timer interval changed");
        }

        public TimeSpan GetAssignRolesInterval()
        {
            return assignRolesTimeSpan;
        }
        public DateTime GetAssignRolesTimerStart()
        {
            return assignRolesStartTime;
        }
        #endregion

        #region MiscFunctions
        private string GetUserNamePlusDiscrim(SocketGuildUser guildUser)
        {
            return guildUser.Username + '#' + guildUser.Discriminator;
        }

        private Task SaveRolesSub(SocketRole roleBefore, SocketRole roleAfter)
        {
            //save roles
            userStatRolesRef.SaveRankRoles(guildRef, saveHandlerRef);

            //Console.WriteLine("Roles Saved");

            return Task.CompletedTask;
        }

        /// <summary>
        /// Loads bot config and data
        /// </summary>
        private void LoadAllBotInfo()
        {
            //LOADING
            
            //load roles
            saveHandlerRef.LoadArray(out userStatRolesRef.rankRoles, userStatRolesRef.rolesSaveFileName);
            if (userStatRolesRef.rankRoles == null)
            {
                userStatRolesRef.CreateDefaultRolesArray();
            }

            //load roles timer
            saveHandlerRef.LoadObject(out assignRolesTimeSpan, nameof(assignRolesTimeSpan));
            if (assignRolesTimeSpan.Equals(default(TimeSpan)))
            {
                assignRolesTimeSpan = new TimeSpan(24, 0, 0);
            }

            //TODO: TEST THIS
            //load rank Config
            //saveHandlerRef.LoadObject(out UserStatTracker.rankConfig, nameof(rankConfigSave));
            saveHandlerRef.LoadObject(out UserStatTracker.rankConfig, nameof(UserStatTracker.rankConfig));
            if (!(UserStatTracker.rankConfig.initialized))
            {
                UserStatTracker.DefaultRankConfig();
            }

            userStatRolesRef.LoadRankedUsers();

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

        }

        /// <summary>
        /// Saves bot config and data
        /// </summary>
        private void SaveAllBotInfo()
        {
            //save roles
            userStatRolesRef.SaveRankRoles(guildRef, saveHandlerRef);

            //save timer time 
            saveHandlerRef.SaveObject(assignRolesTimeSpan, nameof(assignRolesTimeSpan));

            //rank config save
            //rankConfigSave = UserStatTracker.rankConfig;
            //saveHandlerRef.SaveObject(rankConfigSave, nameof(rankConfigSave));
            saveHandlerRef.SaveObject(UserStatTracker.rankConfig, nameof(UserStatTracker.rankConfig));

            userStatRolesRef.SaveRankedUsers();

            saveHandlerRef.SaveDictionary(guildUserIDToStatIndex, nameof(guildUserIDToStatIndex));
            saveHandlerRef.SaveDictionary(guildUserNameToIDIndex, nameof(guildUserNameToIDIndex));
        }

        #endregion

        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        #endregion
    }
}