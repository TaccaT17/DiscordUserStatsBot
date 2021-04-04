//Copyright Tom Crammond 2021

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

namespace DiscordUserStatsBot
{
    class UserStatsBotController
    {
        #region VARIABLES
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------

        private DiscordSocketClient myClient;
        private SocketGuild guildRef;

        //dictionaries that record users and their corresponding UserStats
        public Dictionary<ulong, UserStatTracker> guildUserIDToStatIndex;
        public Dictionary<string, ulong> guildUserNameToIDIndex;

        public event Func<SocketUser, Task> UserJoinedAVoiceChat;
        public event Func<SocketUser, Task> UserLeftAllVoiceChats;

        private bool trackBotStats = false;

        public SaveHandler saveHandlerRef;
        public UserStatRoles userStatRolesRef;
        public CommandHandler commandHandlerRef;
        public UserStatConfig userStatConfigRef;

        //assign roles timer
        private System.Timers.Timer assignRolesTimer;
        private TimeSpan assignRolesTimeSpan;
        private DateTime assignRolesStartTime;

        public bool inactiveUsersLoseAllRoles = true;

        public SocketGuild GuildRef
        {
            get
            {
                return guildRef;
            }
        }

        List<SocketUser> usersInChat;

        bool devMode = true;

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------- 
        #endregion

        //CONSTRUCTOR
        public UserStatsBotController(DiscordSocketClient client, SocketGuild guild)
        {
            myClient = client;

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

            //get guild(AKA server)
            if (guildRef == null)
            {
                guildRef = guild;

                Log($"Set up new guild reference to {guildRef.Name}");

                //TODO: get relevant info AKA if this bot is new to the server make dictioneries, if not get dictionaries
            }

            //make save, roles, command classes
            if (saveHandlerRef == null)
                saveHandlerRef = new SaveHandler();
            if (userStatRolesRef == null)
                userStatRolesRef = new UserStatRoles(this);
            if (commandHandlerRef == null)
            {
                commandHandlerRef = new CommandHandler(this, guildRef);
                client.MessageReceived += commandHandlerRef.CommandHandlerFunc; //adds CommandHandler func to MessageRecieved event delegate. Therefore CommandHandler will be executed anytime a message is posted on the discord server
                //for recording user stats
                client.MessageReceived += RecordMessageSent;
                commandHandlerRef.wasBotCommand = false;
            }

            LoadAllBotInfo();

            //ensure has permissions
            if (!HasPermissions())
            {
                Log(new LogMessage(LogSeverity.Warning, this.ToString(), "Bot is lacking relevant permissions. "));
                guildRef.DefaultChannel.SendMessageAsync("WARNING: I don't have the permissions I need to work...");
                return;
            }
            
            //start timer
            AssignRolesTimer(assignRolesTimeSpan);

            //creates/calculates/assigns user roles
            userStatRolesRef.AssignRoles(guildRef);

            SaveAllBotInfo();

            Log("Bot set up");

            Connect();

            client.Ready += Connect;

        }

        #region FUNCTIONS
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        public void Log(string msg)
        {
            //string output = $"{guildRef.Name} - " + "\t" + new LogMessage(LogSeverity.Info, this.ToString(), msg).ToString();

            string output1 = $"{guildRef.Name} - ";
            string output2 = new LogMessage(LogSeverity.Info, this.ToString(), msg).ToString();

            string output = String.Format("{0,-30}{1,-10}", output1, output2);

            using (System.IO.StreamWriter file =
            new System.IO.StreamWriter(MainClass.FilePath + Path.DirectorySeparatorChar + @"logs.txt", true))
            {
                file.WriteLine(output);
            }

            Console.WriteLine(output);
            return;
        }

        public void Log(LogMessage msg)
        {
            //ignore debugs if not in devmode
            if(msg.Severity.Equals(LogSeverity.Debug) && !devMode)
            {
                return;
            }

            string output1 = $"{guildRef.Name} - ";
            string output2 = msg.ToString();

            string output = String.Format("{0,-30}{1,-10}", output1, output2);

            using (System.IO.StreamWriter file =
            new System.IO.StreamWriter(MainClass.FilePath + Path.DirectorySeparatorChar + @"logs.txt", true))
            {
                file.WriteLine(output);
            }

            Console.WriteLine(output);
            return;
        }

        public bool HasPermissions()
        {
            bool hasPermissions = true;

            if (!guildRef.CurrentUser.GuildPermissions.ManageRoles)
            {
                Log(new LogMessage(LogSeverity.Warning, this.ToString(), "ManageRoles"));
                hasPermissions = false;
            }
            if (!guildRef.CurrentUser.GuildPermissions.AddReactions)
            {
                Log(new LogMessage(LogSeverity.Warning, this.ToString(), "AddReactions"));
                hasPermissions = false;
            }
            if (!guildRef.CurrentUser.GuildPermissions.ViewChannel)
            {
                Log(new LogMessage(LogSeverity.Warning, this.ToString(), "ViewChannel"));
                hasPermissions = false;
            }
            if (!guildRef.CurrentUser.GuildPermissions.SendMessages)
            {
                Log(new LogMessage(LogSeverity.Warning, this.ToString(), "SendMessages"));
                hasPermissions = false;
            }
            if (!guildRef.CurrentUser.GuildPermissions.UseVAD)
            {
                Log(new LogMessage(LogSeverity.Warning, this.ToString(), "UseVAD"));
                hasPermissions = false;
            }
            if (!guildRef.CurrentUser.GuildPermissions.ReadMessageHistory)
            {
                Log(new LogMessage(LogSeverity.Warning, this.ToString(), "ReadMessageHistory"));
                hasPermissions = false;
            }
            if (!hasPermissions)
            {
                Log(new LogMessage(LogSeverity.Warning, this.ToString(), "^ Missing Permissions ^"));
            }

            return hasPermissions;
        }

        private Task Connect()
        {
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

            return Task.CompletedTask;
        }

        private Task Disconnect(Exception exception) 
        {
            if(usersInChat == null)
            {
                return Task.CompletedTask;
            }

            //go through users in chat list and call stopRecord()
            for (int userIndex = usersInChat.Count - 1; userIndex >= 0; userIndex--)
            {
                StopRecordingVCTime(usersInChat[userIndex]);
            }

            //Log($@"There are {usersInChat.Count} users in chat for {guildRef.Name}");

            return Task.CompletedTask;
        }

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

            if (CurrentVoiceChat.VoiceChannel != null && PreviousVoiceChat.VoiceChannel == null)
            {
                UserJoinedAVoiceChat((SocketGuildUser)user);
            }
            else if (CurrentVoiceChat.VoiceChannel == null && PreviousVoiceChat.VoiceChannel != null)
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
            if (usersInChat != null && !(usersInChat.Contains(user)))
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
            if (usersInChat != null && usersInChat.Contains(user))
            {
                usersInChat.Remove(user);
            }

            return Task.CompletedTask;
        }

        private Task RecordMessageSent(SocketMessage message)
        {
            //if not correct guild
            if (!(((SocketGuildChannel)message.Channel).Guild.Id.Equals(guildRef.Id)))
            {
                return Task.CompletedTask;
            }

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
                Log($@"Created a new stat tracker for {usernamePlusDiscrim}");
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

                Log(new LogMessage(LogSeverity.Debug, this.ToString(), $@"ID/UserStat dictionary was at some point deleted. Therefore making a fresh ID/UserStat entry for {usernamePlusDiscrim}."));

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

                        Log(new LogMessage(LogSeverity.Debug, this.ToString(), $@"{usernamePlusDiscrim} changed their username! Replaced their Username/ID entry {item.Key}, {item.Value} with {usernamePlusDiscrim}, {user.Id}"));
                    }
                }

                //...otherwise JSON dict has been deleted so create fresh entry
                if (!userChangedTheirName)
                {
                    guildUserNameToIDIndex.Add(usernamePlusDiscrim, user.Id);
                    Log(new LogMessage(LogSeverity.Warning, this.ToString(), $@"Username/ID dictionary was at some point deleted. Therefore making a fresh Username/ID entry for {usernamePlusDiscrim}."));
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

            //if user in guildUserNameIndex get them
            if (guildUserNameToIDIndex.ContainsKey(userName))
            {
                userStatInst = guildUserIDToStatIndex[guildUserNameToIDIndex[userName]];
                return userStatInst;
            }
            //try updating names and if something changed try it again
            while (UpdateOnceNameToID())
            {
                if (guildUserNameToIDIndex.ContainsKey(userName))
                {
                    userStatInst = guildUserIDToStatIndex[guildUserNameToIDIndex[userName]];
                    return userStatInst;
                }
            }

            //see if they just weren't capitalizing correctly or were excluding discriminator
            int usersWithName = 0;
            string foundUser = "";

            foreach(KeyValuePair<string, ulong> item in guildUserNameToIDIndex)
            {

                if (item.Key.ToLower().Equals(userName.ToLower()) || (item.Key.ToLower().Substring(0, item.Key.Length - 5)).Equals(userName.ToLower()))
                {
                    foundUser = item.Key;
                    usersWithName++;
                    //Log($@"       Found user with name {userName}");
                }
            }

            if (usersWithName > 1)
            {
                Log(new LogMessage(LogSeverity.Debug, this.ToString(), $"Found multiple users with same name '{foundUser}'"));
                return userStatInst;
            }
            else if (usersWithName == 1)
            {
                userName = foundUser;
                if (guildUserNameToIDIndex.ContainsKey(userName))
                {
                    userStatInst = guildUserIDToStatIndex[guildUserNameToIDIndex[userName]];
                }
            }
            else
            {
                Log(new LogMessage(LogSeverity.Debug, this.ToString(), $"Search for UserStats: The bot has no recording of a user with that name '{userName}'"));
            }


            return userStatInst;
        }

        /// <summary>
        /// returns null if no user in the dictionary
        /// </summary>
        public UserStatTracker GetUserStats(ulong userID)
        {
            //Console.WriteLine($"GetUserStats called. Looking for '{userID}'");

            return guildUserIDToStatIndex[userID];
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
                return userID;
            }
            //try updating names and if something changed try it again
            while (UpdateOnceNameToID())
            {
                if (guildUserNameToIDIndex.ContainsKey(userName))
                {
                    userID = guildUserNameToIDIndex[userName];
                    return userID;
                }
            }
            
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
                Log(new LogMessage(LogSeverity.Debug, this.ToString(), $"Found multiple entries with same id '{foundUser}'"));
                return userID;
            }
            else if (usersWithName == 1)
            {
                userName = foundUser;
                if (guildUserNameToIDIndex.ContainsKey(userName))
                {
                    userID = guildUserNameToIDIndex[userName];
                }
            }
            else
            {
                Log(new LogMessage(LogSeverity.Debug, this.ToString(), $"Search for ID: The bot has no recording of a user with that name: '{userName}'"));
            }

            return userID;
        }

        /// <summary>
        /// Searches for user who changed their name. Stops and returns true if it finds a user who changed their name.
        /// </summary>
        /// <returns></returns>
        private bool UpdateOnceNameToID()
        {
            if (guildRef == null)
            {
                Log(new LogMessage(LogSeverity.Warning, this.ToString(), "Error: Could not update usernameToID list because guild is null."));
                return false;
            }

            foreach (var item in guildUserNameToIDIndex)
            {
                SocketGuildUser guildUser = guildRef.GetUser(item.Value);

                if (guildUser != null)
                {
                    //if name not same name update dictionaries
                    if (GetUserNamePlusDiscrim(guildUser) != item.Key)
                    {
                        guildUserNameToIDIndex.Remove(item.Key);
                        guildUserNameToIDIndex.Add(GetUserNamePlusDiscrim(guildUser), guildUser.Id);

                        guildUserIDToStatIndex[guildUser.Id].UpdateUsersName(guildUser);


                        Log(new LogMessage(LogSeverity.Debug, this.ToString(), $"{guildUser.Username} changed their name. Updating dictionary entries."));
                        return true;
                    }
                }
                else
                {
                    //delete that entry (user not part of guild)
                    Log(new LogMessage(LogSeverity.Debug, this.ToString(), "User left the guild. Deleting info."));
                    guildUserNameToIDIndex.Remove(item.Key);
                    guildUserIDToStatIndex.Remove(item.Value);
                }
            }

            return false;
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

            //Log($@"There are {usersInChatList.Count} users in chat");
        }

        public bool UserIsInChat(SocketUser userInQuestion)
        {
            bool userInChat = false;

            if (usersInChat == null)
            {
                return false;
            }

            for (int index = 0; index < usersInChat.Count; index++)
            {
                if (usersInChat[index].Id.Equals(userInQuestion.Id)) 
                {
                    userInChat = true;
                    index = usersInChat.Count;
                }
            }

            return userInChat;
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
            Log("Timer ended so ");
            userStatRolesRef.AssignRoles(guildRef);
        }

        public void ChangeAssignRolesInterval(TimeSpan interval)
        {
            assignRolesTimeSpan = interval;
            assignRolesTimer.Interval = interval.TotalMilliseconds;
            Log("Changed time interval so ");
            userStatRolesRef.AssignRoles(guildRef);
            assignRolesStartTime = DateTime.Now;
            //save assignRolesTimer
            saveHandlerRef.SaveObject(assignRolesTimeSpan, nameof(assignRolesTimeSpan), guildRef);
            Log("Assign timer interval changed");
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



        #region Save/Load Functions
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

            //if userStatConfig null make new one
            if (userStatConfigRef == null)
            {
                userStatConfigRef = new UserStatConfig(this);
            }

            //load rank Config
            saveHandlerRef.LoadObject(out userStatConfigRef.rankConfig, nameof(userStatConfigRef.rankConfig), guildRef);
            if (!(userStatConfigRef.rankConfig.initialized))
            {
                userStatConfigRef.DefaultRankConfig();
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

            //ensure StatTrackers linked to this controller
            foreach (KeyValuePair<ulong, UserStatTracker> item in guildUserIDToStatIndex)
            {
                item.Value.SetController(this);
            }

            userStatRolesRef.LoadRankedUsers();

            //load prefix
            char savedBCP;
            saveHandlerRef.LoadObject(out savedBCP, nameof(CommandHandler.BotCommandPrefix), guildRef);
            if (savedBCP.Equals(default))
            {
                savedBCP = '!';
                Log(new LogMessage(LogSeverity.Debug, this.ToString(), "Using default prefix."));
            }
            commandHandlerRef.BotCommandPrefix = savedBCP;

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
            saveHandlerRef.SaveObject(userStatConfigRef.rankConfig, nameof(userStatConfigRef.rankConfig), guildRef);

            userStatRolesRef.SaveRankedUsers();

            saveHandlerRef.SaveDictionary(guildUserIDToStatIndex, nameof(guildUserIDToStatIndex), guildRef);
            saveHandlerRef.SaveDictionary(guildUserNameToIDIndex, nameof(guildUserNameToIDIndex), guildRef);
        }

        #endregion
     
        public string GetUserNamePlusDiscrim(SocketGuildUser guildUser)
        {
            return guildUser.Username + '#' + guildUser.Discriminator;
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        #endregion
    }
}