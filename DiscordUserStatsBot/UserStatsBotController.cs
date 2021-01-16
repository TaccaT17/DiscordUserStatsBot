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
///Fixed voice recording null errors
///merged userstat commands into one
///added list that keeps track of users in chat - bot starts/stops recording their stats appropriately
///fixed so bot messages/vc not recorded based off of bool
///fixed so if rankedRoles discrepancy bot still works

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

//on startup
//any users that is currently in voice chat
///1. if user leaves chat and the enter time is not recorded AKA default AKA 0
///2. 
///a
///a
///a
///a
///Person enters chat. They're start time is recorded.
///bot disconnects
///person disconnects
///person reconnects much later
///bot connects
///person leaves. Disconnect time recorded.
///a
///a
///a
///Basically the problem: what do I do if bot doesn't see a user disconnect or reconnect?
///Solution: 
///1. when bot connects start record vctime for any users currently in chat
///2. when bot disconnects stop recording time for any users in chat

namespace DiscordUserStatsBot
{
    class UserStatsBotController
    {
        #region VARIABLES
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        //private bool devMode = true;

        //TODO: make these generic
        //private const ulong TBE_GUILD_ID = 767845719570382900;
        //private const ulong TBE_DEBUG_TEXT_CHANNEL_ID = 767845719570382903;
        //private const ulong TBE_DEBUG_VOICE_CHANNEL_ID = 767845719570382904;

        private SocketGuild guildRef;
        //private SocketTextChannel debugTextChannelRef;
        //private SocketVoiceChannel debugVoiceChannelRef;

        //dictionaries that record users and their corresponding UserStats
        public Dictionary<ulong, UserStatTracker> guildUserIDToStatIndex;
        public Dictionary<string, ulong> guildUserNameToIDIndex;

        public event Func<SocketUser, Task> UserJoinedAVoiceChat;
        public event Func<SocketUser, Task> UserLeftAllVoiceChats;

        private bool trackBotStats = false;

        public SaveHandler saveHandlerRef;
        public UserStatsRoles userStatRolesRef;
        private CommandHandler commandHandlerRef;

        //assign roles timer
        private System.Timers.Timer assignRolesTimer;
        private TimeSpan assignRolesTimeSpan;
        private DateTime assignRolesStartTime;

        //[Newtonsoft.Json.JsonProperty]
        //private UserStatTracker.RankConfig rankConfigSave;

        public SocketGuild GuildRef
        {
            get
            {
                return guildRef;
            }
        }

        List<SocketUser> usersInChat;

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------- 
        #endregion


        #region FUNCTIONS
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        #region InitFunctions
        
     
        public UserStatsBotController(DiscordSocketClient client, SocketGuild guild)
        {
            //EVENTS
            //-------------------------------------------------------------------------------------------------------------
            client.UserVoiceStateUpdated += VoiceChatChange;

            //Times when the bot will create an entry for a user
            //client.UserJoined += AddNewUserToStatBotIndex;
            client.MessageReceived += AddNewUserToStatBotIndex;
            UserJoinedAVoiceChat += AddNewUserToStatBotIndex;

            //For record user stats
            UserJoinedAVoiceChat += StartRecordingVCTime;
            UserLeftAllVoiceChats += StopRecordingVCTime;

            client.RoleUpdated += SaveRolesSub;

            client.Disconnected += Disconnect;
            //-------------------------------------------------------------------------------------------------------------

            //TODO:
            //called when bot either joins guild, when bot comes online. 
            //Will check to see if bot set up and up to date. If not will update.

            //get guild(AKA server)
            if (guildRef == null)
            {
                guildRef = guild;

                Console.WriteLine($"Set up new guild reference to {guildRef.Name}");

                //TODO: get relevant info AKA if this bot is new to the server make dictioneries, if not get dictionaries
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

            //get all users currently in chat and put their id's in userInChat list
            //go through userInChat list and call startRecord()

            GetUsersInChat(out usersInChat);

            foreach (SocketUser user in usersInChat)
            {
                if (!(guildUserIDToStatIndex.ContainsKey(user.Id)))
                {
                    AddNewUserToStatBotIndex(user);
                }

                StartRecordingVCTime(user);
            }

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
        }

        private Task Disconnect(Exception exception) //what about crashes???
        {
            //go through users in chat list and call stopRecord()
            for (int userIndex = usersInChat.Count - 1; userIndex >= 0; userIndex--)
            {
                StopRecordingVCTime(usersInChat[userIndex]);
            }

            Console.WriteLine($@"There are {usersInChat.Count} users in chat.");

            return Task.CompletedTask;
        }

        

        #endregion

        #region RecordStatsFunctions
        private Task VoiceChatChange(SocketUser user, SocketVoiceState PreviousVoiceChat, SocketVoiceState CurrentVoiceChat)
        {
            //return if not correct guild
            if (PreviousVoiceChat.VoiceChannel != null && !((PreviousVoiceChat.VoiceChannel).Guild.Id.Equals(guildRef.Id)))
            {
                return Task.CompletedTask;
            }
            else if (CurrentVoiceChat.VoiceChannel != null && !((CurrentVoiceChat.VoiceChannel).Guild.Id.Equals(guildRef.Id)))
            {
                return Task.CompletedTask;
            }

            //track bot stats?
            if (!trackBotStats && user.IsBot)
            {
                return Task.CompletedTask;
            }

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

            //if user not in userInChat list add them
            if (!(usersInChat.Contains(user)))
            {
                usersInChat.Add(user);
            }

            return Task.CompletedTask;
        }

        public Task StopRecordingVCTime(SocketUser user)
        {
            UserStatTracker tempUserStatsRef = GetUserStats(GetUserNamePlusDiscrim((SocketGuildUser)user));
            tempUserStatsRef.RecordGuildUserLeaveVoiceChatTime();
            saveHandlerRef.SaveDictionary(guildUserIDToStatIndex, nameof(guildUserIDToStatIndex), guildRef);

            //if user in userInChat list remove them
            if (usersInChat.Contains(user))
            {
                usersInChat.Remove(user);
            }

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
            //track bot stats?
            if (!trackBotStats && message.Author.IsBot)
            {
                return Task.CompletedTask;
            }

            UserStatTracker tempUserStatsRef;

            if(guildUserIDToStatIndex.TryGetValue(message.Author.Id, out tempUserStatsRef))
            {
                tempUserStatsRef.RecordThisUserSentAMessage(message);

                saveHandlerRef.SaveDictionary(guildUserIDToStatIndex, nameof(guildUserIDToStatIndex), guildRef);
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
                saveHandlerRef.SaveDictionary(guildUserNameToIDIndex, nameof(guildUserNameToIDIndex), guildRef);
                saveHandlerRef.SaveDictionary(guildUserIDToStatIndex, nameof(guildUserIDToStatIndex), guildRef);
                return Task.CompletedTask;
            }
            //if just doesn't have id/stat entry make a new one (prior info unfortunately lost) 
            else if (guildUserNameToIDIndex.ContainsKey(usernamePlusDiscrim) && !(guildUserIDToStatIndex.ContainsKey(user.Id)))
            {
                //caused by JSON dictionary having been deleted. Make a new id/user entry  
                guildUserIDToStatIndex.Add(user.Id, new UserStatTracker(this, usernamePlusDiscrim, user.Id));

                saveHandlerRef.SaveDictionary(guildUserIDToStatIndex, nameof(guildUserIDToStatIndex), guildRef);

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

                saveHandlerRef.SaveDictionary(guildUserNameToIDIndex, nameof(guildUserNameToIDIndex), guildRef);

                return Task.CompletedTask;
            }
        }

        private Task AddNewUserToStatBotIndex(SocketMessage message)
        {
            if (!(((SocketGuildChannel)(message.Channel)).Guild.Id.Equals(guildRef.Id)))
            {
                return Task.CompletedTask;
            }

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
                    Console.WriteLine($"Search for ID: The bot has no recording of a user with that name: {userName}");
                }
            }

            return userID;
        }

        #endregion

        private void GetUsersInChat(out List<SocketUser> usersInChatList)
        {

            //loop through active channels and users in them 
            //Note: contrary to the documentation guildRef.VoiceChannels only gets channels that are being actively used
            //Note: contrary to the documentation VoiceChannel.Users only gets users currently in that channel

            usersInChatList = new List<SocketUser>();

            //iterate through users in channels
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

                    usersInChatList.Add(userInVC);
                }
            }

            Console.WriteLine($@"There are {usersInChatList.Count} users in chat");
        }

        public bool UserIsInChat(SocketUser userInQuestion)
        {
            bool userInChat = false;

            for (int index = 0; index < usersInChat.Count; index++)
            {
                if (usersInChat[index].Id.Equals(userInQuestion.Id)) 
                {
                    userInChat = true;
                    index = usersInChat.Count;
                }
            }

            return userInChat;

            /*SocketVoiceChannel voiceChannel;
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
            */
            //Console.WriteLine("No matching user in chat");
        }

        #region TimerFunctions
        private void AssignRolesTimer(TimeSpan timeTillNextAssignRoles)
        {
            assignRolesTimer = new System.Timers.Timer(timeTillNextAssignRoles.TotalMilliseconds);
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
            saveHandlerRef.SaveObject(assignRolesTimeSpan, nameof(assignRolesTimeSpan), guildRef);
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
            //return if not correct guild
            if (!(roleBefore.Guild.Id.Equals(guildRef.Id)))
            {
                return Task.CompletedTask;
            }

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
            saveHandlerRef.LoadArray(out userStatRolesRef.rankRoles, userStatRolesRef.rolesSaveFileName, guildRef);
            if (userStatRolesRef.rankRoles == null)
            {
                userStatRolesRef.CreateDefaultRolesArray();
            }

            //load roles timer
            saveHandlerRef.LoadObject(out assignRolesTimeSpan, nameof(assignRolesTimeSpan), guildRef);
            if (assignRolesTimeSpan.Equals(default(TimeSpan)))
            {
                assignRolesTimeSpan = new TimeSpan(24, 0, 0);
            }

            //TODO: TEST THIS
            //load rank Config
            //saveHandlerRef.LoadObject(out UserStatTracker.rankConfig, nameof(rankConfigSave));
            saveHandlerRef.LoadObject(out UserStatTracker.rankConfig, nameof(UserStatTracker.rankConfig), guildRef);
            if (!(UserStatTracker.rankConfig.initialized))
            {
                UserStatTracker.DefaultRankConfig();
            }

            //set up user dictionaries and load any info that already exists
            saveHandlerRef.LoadDictionary(out guildUserIDToStatIndex, nameof(guildUserIDToStatIndex), guildRef); //out keyword passes by reference instead of value
            saveHandlerRef.LoadDictionary(out guildUserNameToIDIndex, nameof(guildUserNameToIDIndex), guildRef);

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

            userStatRolesRef.LoadRankedUsers();

        }

        /// <summary>
        /// Saves bot config and data
        /// </summary>
        private void SaveAllBotInfo()
        {
            //save roles
            userStatRolesRef.SaveRankRoles(guildRef, saveHandlerRef);

            //save timer time 
            saveHandlerRef.SaveObject(assignRolesTimeSpan, nameof(assignRolesTimeSpan), guildRef);

            //rank config save
            //rankConfigSave = UserStatTracker.rankConfig;
            //saveHandlerRef.SaveObject(rankConfigSave, nameof(rankConfigSave));
            saveHandlerRef.SaveObject(UserStatTracker.rankConfig, nameof(UserStatTracker.rankConfig), guildRef);

            userStatRolesRef.SaveRankedUsers();

            saveHandlerRef.SaveDictionary(guildUserIDToStatIndex, nameof(guildUserIDToStatIndex), guildRef);
            saveHandlerRef.SaveDictionary(guildUserNameToIDIndex, nameof(guildUserNameToIDIndex), guildRef);
        }

        #endregion

        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        #endregion
    }
}