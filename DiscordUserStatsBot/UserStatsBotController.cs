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
    public class UserStatsBotController : BotComponent
    {
        #region VARIABLES
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------

        public DI_Class DIRef { get { return dIRef; } }

        //private DiscordSocketClient myClient;
        //private SocketGuild dIRef.GuildRef;

        //dictionaries that record users and their corresponding UserStats
        public Dictionary<ulong, UserStatTracker> guildUserIDToStatIndex;
        public Dictionary<string, ulong> guildUserNameToIDIndex;

        public event Func<SocketUser, Task> UserJoinedAVoiceChat;
        public event Func<SocketUser, Task> UserLeftAllVoiceChats;

        private bool trackBotStats = false;

        //assign roles timer
        private System.Timers.Timer assignRolesTimer;
        private TimeSpan assignRolesTimeSpan;
        private DateTime assignRolesStartTime;

        public bool inactiveUsersLoseAllRoles = true;

        public string MissingPermissions;

        /*
        public SocketGuild GuildRef
        {
            get
            {
                return dIRef.GuildRef;
            }
        }*/

        List<SocketUser> usersInChat;

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------- 
        #endregion

        override public void Init()
        {
            SubscribeToEvents();

            //get guild(AKA server)


            //TODO: get relevant info AKA if this bot is new to the server make dictioneries, if not get dictionaries

            /*
            //make save, roles, command classes
            if (dIRef.RolesRef == null)
                dIRef.RolesRef = new UserStatRoles(this);
            if (commandHandlerRef == null)
            {
                commandHandlerRef = new CommandHandler(this, dIRef.GuildRef);
                dIRef.Client.MessageReceived += commandHandlerRef.CommandHandlerFunc; //adds CommandHandler func to MessageRecieved event delegate. Therefore CommandHandler will be executed anytime a message is posted on the discord server
                //for recording user stats
                dIRef.Client.MessageReceived += RecordMessageSent;
                commandHandlerRef.wasBotCommand = false;
            }
            */
            BotSetUp();
            

            

        }

        #region FUNCTIONS
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        
        public void BotSetUp()
        {
            LoadAllBotInfo();

            //ensure has permissions
            if (!HasPermissions())
            {
                dIRef.LogRef.Log(new LogMessage(LogSeverity.Warning, this.ToString(), "Bot is lacking relevant permissions. "));
                dIRef.GuildRef.DefaultChannel.SendMessageAsync("Beware: Bot won't fully function because lacking permissions... \n" + MissingPermissions);
                return;
            }

            //start timer
            AssignRolesTimer(assignRolesTimeSpan);

            //creates/calculates/assigns user roles
            dIRef.RolesRef.AssignRoles(dIRef.GuildRef);

            SaveAllBotInfo();

            dIRef.LogRef.Log("Bot set up");

            Connect();
        }

        private void SubscribeToEvents()
        {
            dIRef.Client.UserVoiceStateUpdated += VoiceChatChange;

            //Times when the bot will create an entry for a user
            //dIRef.Client.UserJoined += AddNewUserToStatBotIndex;
            dIRef.Client.MessageReceived += AddNewUserToStatBotIndex;
            UserJoinedAVoiceChat += AddNewUserToStatBotIndex;

            //For record user stats
            UserJoinedAVoiceChat += StartRecordingVCTime;
            UserLeftAllVoiceChats += StopRecordingVCTime;

            dIRef.Client.RoleUpdated += SaveRolesSub;

            dIRef.Client.Disconnected += Disconnect;

            dIRef.Client.MessageReceived += dIRef.CommandExecRef.ExecuteCommand; //adds CommandHandler func to MessageRecieved event delegate. Therefore CommandHandler will be executed anytime a message is posted on the discord server
            dIRef.Client.MessageReceived += RecordMessageSent;
            dIRef.CommandHandRef.wasBotCommand = false;

            dIRef.Client.Ready += Connect;
        }

        public bool HasPermissions()
        {
            bool hasPermissions = true;
            MissingPermissions = "Missing Permissions: ";

            //goes out of sync???
            //CurrentUser is null 

            if (!dIRef.GuildRef.CurrentUser.GuildPermissions.ManageRoles)
            {
                dIRef.LogRef.Log(new LogMessage(LogSeverity.Warning, this.ToString(), "ManageRoles"));
                hasPermissions = false;
                MissingPermissions += "\n   - ManageRoles";
            }
            if (!dIRef.GuildRef.CurrentUser.GuildPermissions.AddReactions)
            {
                dIRef.LogRef.Log(new LogMessage(LogSeverity.Warning, this.ToString(), "AddReactions"));
                hasPermissions = false;
                MissingPermissions += "\n   - AddReactions";
            }
            if (!dIRef.GuildRef.CurrentUser.GuildPermissions.ViewChannel)
            {
                dIRef.LogRef.Log(new LogMessage(LogSeverity.Warning, this.ToString(), "ViewChannel"));
                hasPermissions = false;
                MissingPermissions += "\n   - ViewChannel";
            }
            if (!dIRef.GuildRef.CurrentUser.GuildPermissions.SendMessages)
            {
                dIRef.LogRef.Log(new LogMessage(LogSeverity.Warning, this.ToString(), "SendMessages"));
                hasPermissions = false;
                MissingPermissions += "\n   - SendMessages";
            }
            if (!dIRef.GuildRef.CurrentUser.GuildPermissions.UseVAD)
            {
                dIRef.LogRef.Log(new LogMessage(LogSeverity.Warning, this.ToString(), "UseVAD"));
                hasPermissions = false;
                MissingPermissions += "\n   - UseVAD";
            }
            if (!dIRef.GuildRef.CurrentUser.GuildPermissions.ReadMessageHistory)
            {
                dIRef.LogRef.Log(new LogMessage(LogSeverity.Warning, this.ToString(), "ReadMessageHistory"));
                hasPermissions = false;
                MissingPermissions += "\n   - ReadMessageHistory";
            }
            if (!hasPermissions)
            {
                dIRef.LogRef.Log(new LogMessage(LogSeverity.Warning, this.ToString(), "^ Missing Permissions ^"));
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

            //dIRef.LogRef.Log($@"There are {usersInChat.Count} users in chat for {dIRef.GuildRef.Name}");

            return Task.CompletedTask;
        }

        #region RecordStatsFunctions
        private Task VoiceChatChange(SocketUser user, SocketVoiceState PreviousVoiceChat, SocketVoiceState CurrentVoiceChat)
        {
            //return if not correct guild
            if (PreviousVoiceChat.VoiceChannel != null && !((PreviousVoiceChat.VoiceChannel).Guild.Id.Equals(dIRef.GuildRef.Id)))
            {
                return Task.CompletedTask;
            }
            else if (CurrentVoiceChat.VoiceChannel != null && !((CurrentVoiceChat.VoiceChannel).Guild.Id.Equals(dIRef.GuildRef.Id)))
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
            SaveHandler.S.SaveDictionary(guildUserIDToStatIndex, nameof(guildUserIDToStatIndex), dIRef.GuildRef);

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
            if (!(((SocketGuildChannel)message.Channel).Guild.Id.Equals(dIRef.GuildRef.Id)))
            {
                return Task.CompletedTask;
            }

            //don't count bot commands as messages
            if (dIRef.CommandHandRef.wasBotCommand)
            {
                dIRef.CommandHandRef.wasBotCommand = false;
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

                SaveHandler.S.SaveDictionary(guildUserIDToStatIndex, nameof(guildUserIDToStatIndex), dIRef.GuildRef);
            }

            return Task.CompletedTask;
        }

        #endregion

        #region UserStatFunctions
        //TODO: way to deal with when user changes their name

        /// <summary>
        /// Called whenever message in guild or connects or voice chat happens
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        private Task AddNewUserToStatBotIndex(SocketUser user)
        {
            //dIRef.LogRef.Log("Add new user: " + user.Username);

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
                dIRef.LogRef.Log($@"Created a new stat tracker for {usernamePlusDiscrim}");
                //save dictionariess
                SaveHandler.S.SaveDictionary(guildUserNameToIDIndex, nameof(guildUserNameToIDIndex), dIRef.GuildRef);
                SaveHandler.S.SaveDictionary(guildUserIDToStatIndex, nameof(guildUserIDToStatIndex), dIRef.GuildRef);
                return Task.CompletedTask;
            }
            //if just doesn't have id/stat entry make a new one (prior info unfortunately lost) 
            else if (guildUserNameToIDIndex.ContainsKey(usernamePlusDiscrim) && !(guildUserIDToStatIndex.ContainsKey(user.Id)))
            {
                //caused by JSON dictionary having been deleted. Make a new id/user entry  
                guildUserIDToStatIndex.Add(user.Id, new UserStatTracker(this, usernamePlusDiscrim, user.Id));

                SaveHandler.S.SaveDictionary(guildUserIDToStatIndex, nameof(guildUserIDToStatIndex), dIRef.GuildRef);

                dIRef.LogRef.Log(new LogMessage(LogSeverity.Debug, this.ToString(), $@"ID/UserStat dictionary was at some point deleted. Therefore making a fresh ID/UserStat entry for {usernamePlusDiscrim}."));

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

                        dIRef.LogRef.Log(new LogMessage(LogSeverity.Debug, this.ToString(), $@"{usernamePlusDiscrim} changed their username! Replaced their Username/ID entry {item.Key}, {item.Value} with {usernamePlusDiscrim}, {user.Id}"));
                    }
                }

                //...otherwise JSON dict has been deleted so create fresh entry
                if (!userChangedTheirName)
                {
                    guildUserNameToIDIndex.Add(usernamePlusDiscrim, user.Id);
                    dIRef.LogRef.Log(new LogMessage(LogSeverity.Warning, this.ToString(), $@"Username/ID dictionary was at some point deleted. Therefore making a fresh Username/ID entry for {usernamePlusDiscrim}."));
                }

                SaveHandler.S.SaveDictionary(guildUserNameToIDIndex, nameof(guildUserNameToIDIndex), dIRef.GuildRef);

                return Task.CompletedTask;
            }
        }

        private Task AddNewUserToStatBotIndex(SocketMessage message)
        {
            if (!(((SocketGuildChannel)(message.Channel)).Guild.Id.Equals(dIRef.GuildRef.Id)))
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
                    //dIRef.LogRef.Log($@"       Found user with name {userName}");
                }
            }

            if (usersWithName > 1)
            {
                dIRef.LogRef.Log(new LogMessage(LogSeverity.Debug, this.ToString(), $"Found multiple users with same name '{foundUser}'"));
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
                dIRef.LogRef.Log(new LogMessage(LogSeverity.Debug, this.ToString(), $"Search for UserStats: The bot has no recording of a user with that name '{userName}'"));
            }

            return userStatInst;
        }

        /// <summary>
        /// returns null if no user in the dictionary
        /// </summary>
        public UserStatTracker GetUserStats(ulong userID)
        {
            //Console.WriteLine($"GetUserStats called. Looking for '{userID}'");
            if (guildUserIDToStatIndex.ContainsKey(userID))
            {
                return guildUserIDToStatIndex[userID];
            }
            else
            {
                return null;
            }
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
                dIRef.LogRef.Log(new LogMessage(LogSeverity.Debug, this.ToString(), $"Found multiple entries with same id '{foundUser}'"));
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
                dIRef.LogRef.Log(new LogMessage(LogSeverity.Debug, this.ToString(), $"Search for ID: The bot has no recording of a user with that name: '{userName}'"));
            }

            return userID;
        }

        /// <summary>
        /// Searches for user who changed their name. Stops and returns true if it finds a user who changed their name.
        /// </summary>
        /// <returns></returns>
        private bool UpdateOnceNameToID()
        {
            if (dIRef.GuildRef == null)
            {
                dIRef.LogRef.Log(new LogMessage(LogSeverity.Warning, this.ToString(), "Error: Could not update usernameToID list because guild is null."));
                return false;
            }

            foreach (var item in guildUserNameToIDIndex)
            {
                SocketGuildUser guildUser = dIRef.GuildRef.GetUser(item.Value);

                if (guildUser != null)
                {
                    //if name not same name update dictionaries
                    if (GetUserNamePlusDiscrim(guildUser) != item.Key)
                    {
                        guildUserNameToIDIndex.Remove(item.Key);
                        guildUserNameToIDIndex.Add(GetUserNamePlusDiscrim(guildUser), guildUser.Id);

                        guildUserIDToStatIndex[guildUser.Id].UpdateUsersName(guildUser);


                        dIRef.LogRef.Log(new LogMessage(LogSeverity.Debug, this.ToString(), $"{guildUser.Username} changed their name. Updating dictionary entries."));
                        return true;
                    }
                }
                else
                {
                    //delete that entry (user not part of guild)
                    dIRef.LogRef.Log(new LogMessage(LogSeverity.Debug, this.ToString(), "User left the guild. Deleting info."));
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
            //Note: contrary to the documentation dIRef.GuildRef.VoiceChannels only gets channels that are being actively used
            //Note: contrary to the documentation VoiceChannel.Users only gets users currently in that channel

            usersInChatList = new List<SocketUser>();

            //iterate through users in channels
            SocketVoiceChannel voiceChannel;
            IEnumerator<SocketVoiceChannel> voiceChannelE = dIRef.GuildRef.VoiceChannels.GetEnumerator();

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

            //dIRef.LogRef.Log($@"There are {usersInChatList.Count} users in chat");
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
            dIRef.LogRef.Log("Timer ended so ");
            dIRef.RolesRef.AssignRoles(dIRef.GuildRef);
        }

        public void ChangeAssignRolesInterval(TimeSpan interval)
        {
            assignRolesTimeSpan = interval;
            assignRolesTimer.Interval = interval.TotalMilliseconds;
            dIRef.LogRef.Log("Changed time interval so ");
            dIRef.RolesRef.AssignRoles(dIRef.GuildRef);
            assignRolesStartTime = DateTime.Now;
            //save assignRolesTimer
            SaveHandler.S.SaveObject(assignRolesTimeSpan, nameof(assignRolesTimeSpan), dIRef.GuildRef);
            dIRef.LogRef.Log("Assign timer interval changed");
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
            if (!(roleBefore.Guild.Id.Equals(dIRef.GuildRef.Id)))
            {
                return Task.CompletedTask;
            }

            //save roles
            dIRef.RolesRef.SaveRankRoles(dIRef.GuildRef, SaveHandler.S);

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
            SaveHandler.S.LoadArray(out dIRef.RolesRef.rankRoles, dIRef.RolesRef.rolesSaveFileName, dIRef.GuildRef);
            if (dIRef.RolesRef.rankRoles == null)
            {
                dIRef.RolesRef.CreateDefaultRolesArray();
            }

            //load roles timer
            SaveHandler.S.LoadObject(out assignRolesTimeSpan, nameof(assignRolesTimeSpan), dIRef.GuildRef);
            if (assignRolesTimeSpan.Equals(default(TimeSpan)))
            {
                assignRolesTimeSpan = new TimeSpan(24, 0, 0);
            }

            //if userStatConfig null make new one
            /*if (dIRef.ConfigRef == null)
            {
                dIRef.ConfigRef = new UserStatConfig(this);
            }
            */

            //load rank Config
            SaveHandler.S.LoadObject(out dIRef.ConfigRef.rankConfig, nameof(dIRef.ConfigRef.rankConfig), dIRef.GuildRef);
            if (!(dIRef.ConfigRef.rankConfig.initialized))
            {
                dIRef.ConfigRef.DefaultRankConfig();
            }

            //set up user dictionaries and load any info that already exists
            SaveHandler.S.LoadDictionary(out guildUserIDToStatIndex, nameof(guildUserIDToStatIndex), dIRef.GuildRef); //out keyword passes by reference instead of value
            SaveHandler.S.LoadDictionary(out guildUserNameToIDIndex, nameof(guildUserNameToIDIndex), dIRef.GuildRef);

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

            dIRef.RolesRef.LoadRankedUsers();

            //load prefix
            char savedBCP;
            SaveHandler.S.LoadObject(out savedBCP, nameof(CommandHandler.BotCommandPrefix), dIRef.GuildRef);
            if (savedBCP.Equals(default))
            {
                savedBCP = '!';
                dIRef.LogRef.Log(new LogMessage(LogSeverity.Debug, this.ToString(), "Using default prefix."));
            }
            dIRef.CommandHandRef.BotCommandPrefix = savedBCP;
        }

        /// <summary>
        /// Saves bot config and data
        /// </summary>
        private void SaveAllBotInfo()
        {
            //save roles
            dIRef.RolesRef.SaveRankRoles(dIRef.GuildRef, SaveHandler.S);

            //save timer time 
            SaveHandler.S.SaveObject(assignRolesTimeSpan, nameof(assignRolesTimeSpan), dIRef.GuildRef);

            //rank config save
            SaveHandler.S.SaveObject(dIRef.ConfigRef.rankConfig, nameof(dIRef.ConfigRef.rankConfig), dIRef.GuildRef);

            dIRef.RolesRef.SaveRankedUsers();

            SaveHandler.S.SaveDictionary(guildUserIDToStatIndex, nameof(guildUserIDToStatIndex), dIRef.GuildRef);
            SaveHandler.S.SaveDictionary(guildUserNameToIDIndex, nameof(guildUserNameToIDIndex), dIRef.GuildRef);
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