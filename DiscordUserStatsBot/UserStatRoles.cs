//Copyright Tom Crammond 2021

using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;

namespace DiscordUserStatsBot
{
    public class UserStatRoles : BotComponent
    {
        #region VARIABLES
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------


        //NOTE: "Discord Role" refers to role that is saved by discord, "UserStatRole" refers to a struct that contains the relevant info to save for that role

        //roles ordered highest esteem to lowest
        public UserStatRole[] rankRoles;

        private Discord.Color[] defaultRoleColors;
        private string[] defaultRoleNames;
        private int[] defaultRoleMemberAmount;
        private int defaultNumberOfRoles = 5;

        public string rolesSaveFileName = "rolesSave";
        
        [Newtonsoft.Json.JsonProperty]
        private List<ulong> rankedUsers; //messageRank, chatRank, totalRank;

        //UserStatsBotController dIRef.ContRef;

        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        #endregion

        #region FUNCTIONS
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------

        public void CreateDefaultRolesArray()
        {
            defaultRoleColors = new Discord.Color[] { Discord.Color.Orange, Discord.Color.Purple, Discord.Color.Blue, Discord.Color.Green, Discord.Color.LighterGrey };
            defaultRoleNames = new string[] { "Apexian Loudmouth", "Babbling Baroness", "Insightful Inquisitor", "Earl of Eavesdropping", "Silent Sentinel" };
            //TODO: Change back to                3, 5, 10, 30, 100
            defaultRoleMemberAmount = new int[] { 1, 1, 1, 2, 3 };
            //make default array of UserStatRole struct
            rankRoles = new UserStatRole[defaultNumberOfRoles];
            for (int index = 0; index < rankRoles.Length; index++)
            {
                rankRoles[index] = new UserStatRole((ulong)0, defaultRoleNames[index], defaultRoleMemberAmount[index], defaultRoleColors[index], 0);
            }

            dIRef.LogRef.Log(new Discord.LogMessage(Discord.LogSeverity.Debug, this.ToString(), "Default Roles Created"));
        }

        /// <summary>
        /// Assigns users their appropriate role. Called when bot started and once every day
        /// </summary>
        /// <param name="guildRef"></param>
        public async void AssignRoles(SocketGuild guildRef)
        {
            dIRef.LogRef.Log("Assigning Roles");

            //ensure permission
            if (!guildRef.CurrentUser.GuildPermissions.ManageRoles || 
                !guildRef.CurrentUser.GuildPermissions.AddReactions)
            {
                dIRef.LogRef.Log(new Discord.LogMessage(Discord.LogSeverity.Error, this.ToString(), "Can't assign roles because lacks permissions."));
                return;
            }

            //check to make sure have all roles
            CreateUnmadeRoles(guildRef);


            //if ranked users not initialized then make list from dictionary
            if(rankedUsers == null)
            {
                rankedUsers = new List<ulong>();
                foreach(KeyValuePair<ulong, UserStatTracker> item in dIRef.ContRef.guildUserIDToStatIndex)
                {
                    rankedUsers.Add(item.Key);
                }
            }

            //ensure all recorded ids in rankedUsers list
            foreach (KeyValuePair<ulong, UserStatTracker> item in dIRef.ContRef.guildUserIDToStatIndex)
            {
                if (!(rankedUsers.Contains(item.Key)))
                {
                    //if not in list add it
                    rankedUsers.Add(item.Key);
                }
            }

            //if rankedUsers has userID that statTracker doesn't delete it
            for (int iD = rankedUsers.Count - 1; iD >= 0; iD--)
            {
                if (!(dIRef.ContRef.guildUserIDToStatIndex.ContainsKey(rankedUsers[iD])))
                {
                    //Console.WriteLine("Doesn't have key");
                    //also remove any rank roles that user might have
                    SocketGuildUser guildUser = guildRef.GetUser(rankedUsers[iD]);
                    if(guildUser != null)
                    {
                        RemoveUsersRankRoles(guildRef, guildUser);
                    }

                    rankedUsers.RemoveAt(iD);
                }
                else
                {
                    //Console.WriteLine("Does have key: " + rankedUsers[iD]);
                }
            }

            RankUsers();

            //get highest position bot role
            int maxBotRolePos = 0;
            SocketRole botRole;
            IEnumerator<SocketRole> botRoleE = guildRef.CurrentUser.Roles.GetEnumerator();
            while (botRoleE.MoveNext())
            {
                botRole = botRoleE.Current;

                if (maxBotRolePos < botRole.Position)
                {
                    maxBotRolePos = botRole.Position;
                }
            }

            //go through roleList and give each role appropriate users
            int rankedUserIndex = 0;
            for (int rankRole = 0; rankRole < rankRoles.Length; rankRole++)
            {
                //ensure bot has high enough position to move this RankRole
                //if position of this bot role is less than the role about to impact then skip this role
                if(rankRoles[rankRole].position > maxBotRolePos)
                {
                    dIRef.LogRef.Log(new Discord.LogMessage(Discord.LogSeverity.Error, this.ToString(), $"BEWARE: Bot role not above generated RankRole: {rankRoles[rankRole].name}"));
                    await guildRef.DefaultChannel.SendMessageAsync($"Beware: Bot role must be above '{rankRoles[rankRole].name}' role for bot to fully function.");
                    continue;
                }
                for (int rankMemberAmountIteration = 0; rankMemberAmountIteration < rankRoles[rankRole].memberLimit; rankMemberAmountIteration++)
                {
                    if(rankedUserIndex >= rankedUsers.Count || rankedUsers.Count < 1)
                    {
                        //Log("Ran out of users to assign roles to", Discord.LogSeverity.Debug);
                        return;
                    }
                    if (guildRef.GetUser(rankedUsers[rankedUserIndex]) == null)
                    {
                        dIRef.LogRef.Log( new Discord.LogMessage(Discord.LogSeverity.Error, this.ToString(), "Failed to find guild user using SocketGuild.GetUser(ID). ID: " + rankedUsers[rankedUserIndex]));

                        return;
                    }
                    
                    //iterate though given users roles
                    SocketRole usersExistingRole;
                    IEnumerator<SocketRole> userExistingRoleE = guildRef.GetUser(rankedUsers[rankedUserIndex]).Roles.GetEnumerator();
                    bool alreadyHasRole = false;
                    userExistingRoleE.MoveNext();  //skip @everyone role
                    bool userActive = UserIsActive(rankedUsers[rankedUserIndex]);
                    while (userExistingRoleE.MoveNext())
                    {
                        usersExistingRole = userExistingRoleE.Current;

                        if (usersExistingRole.Position > maxBotRolePos)
                        {
                            //skip here too (bot not high enough role position)
                            continue;
                        }

                        //if user already has role you are good and is active
                        if (usersExistingRole.Id.Equals(rankRoles[rankRole].Id) && userActive)
                        {
                            alreadyHasRole = true;
                        }
                        //if the user has another ranked role or is inactive remove that role
                        //TODO: is iterationg more times than necessary
                        else
                        {
                            for (int role = 0; role < rankRoles.Length; role++)
                            {
                                if (usersExistingRole.Id.Equals(rankRoles[role].Id))
                                {
                                    await guildRef.GetUser(rankedUsers[rankedUserIndex]).RemoveRoleAsync(guildRef.GetRole(rankRoles[role].Id));
                                }
                            }
                        }
                    }

                    if (userActive)
                    {
                        if (!alreadyHasRole)
                        {
                            //if user doesnt have role assign it
                            await guildRef.GetUser(rankedUsers[rankedUserIndex]).AddRoleAsync(guildRef.GetRole(rankRoles[rankRole].Id));
                        }
                    }
                    else
                    {
                        //don't apply roles to inactive members
                        //dIRef.LogRef.Log(new Discord.LogMessage(Discord.LogSeverity.Debug, this.ToString(), "This user is inactive therefore not applying a role."));
                        rankMemberAmountIteration--;
                    }

                    rankedUserIndex++;
                }
            }

            //if there are more users to go through and delete any rankRoles they have
            for (; rankedUserIndex < rankedUsers.Count; rankedUserIndex++)       //iterate through rest of users
            {
                SocketGuildUser guildUser = guildRef.GetUser(rankedUsers[rankedUserIndex]);

                RemoveUsersRankRoles(guildRef, guildUser);
            }

            //save rankedUsers list
            SaveRankedUsers();

        }

        private async void RemoveUsersRankRoles(SocketGuild guildRef, SocketGuildUser guildUser)
        {
            SocketRole usersExistingRoles;
            IEnumerator<SocketRole> userExistingRoleE = guildUser.Roles.GetEnumerator(); //Null reference here. Bot broke because user offline? Mobile
            while (userExistingRoleE.MoveNext())            //iterate through users roles
            {
                usersExistingRoles = userExistingRoleE.Current;

                foreach (UserStatRole rankRole in rankRoles)     //iterate through rankRoles
                {
                    if (usersExistingRoles.Id.Equals(rankRole.Id))
                    {
                        await guildUser.RemoveRoleAsync(guildRef.GetRole(rankRole.Id));
                    }
                }

            }
        }

        /// <summary>
        /// returns true if user sent message or participated in voice chat in the past *rankTime* days
        /// </summary>
        /// <param name="iD"></param>
        /// <returns></returns>
        private bool UserIsActive(ulong iD)
        {
            if (dIRef.ContRef.inactiveUsersLoseAllRoles)
            {
                UserStatTracker stats = dIRef.ContRef.GetUserStats(iD);

                if (stats != null && (stats.TotalChatTime((int)dIRef.ConfigRef.rankConfig.rankTime) > TimeSpan.Zero || stats.TotalMessages((int)dIRef.ConfigRef.rankConfig.rankTime) > 0))
                {
                    return true;
                }

                return false;
            }
            else
            {
                return true;
            }
            
        }

        /// <summary>
        /// Reorders rankedUsers list so sorted highest -> lowest rank
        /// Can modify rank sorting options using static RankConfig struct in UserStatTracker
        /// </summary>
        public void RankUsers()
        {
            //generate ordered list of userStatTrackers from ranked iD list. I use rankedID list for this because it should be already pretty close to being in the correct order.
            List<UserStatTracker> userStatTrackersList = new List<UserStatTracker>();
            for (int iD = 0; iD < rankedUsers.Count; iD++)
            {
                userStatTrackersList.Add(dIRef.ContRef.guildUserIDToStatIndex[rankedUsers[iD]]);
            }


            //TODO: make this more efficient by calling recurring function (funciton calls itself)
            //if sorting by both messages and voice chat time
            if (dIRef.ConfigRef.rankConfig.rankType.Equals(UserStatConfig.RankConfig.RankType.msgAndVCT))
            {
                //create list that is ranked by messages
                //create list that is ranked by vctime
                List<UserStatTracker> sortedByMessages = new List<UserStatTracker>();
                List<UserStatTracker> sortedByVCTime= new List<UserStatTracker>();
                for (int index = 0; index < userStatTrackersList.Count; index++)
                {
                    sortedByMessages.Add(userStatTrackersList[index]);
                    sortedByVCTime.Add(userStatTrackersList[index]);
                }

                dIRef.ConfigRef.rankConfig.rankType = UserStatConfig.RankConfig.RankType.messages;
                sortedByMessages.Sort();

                dIRef.ConfigRef.rankConfig.rankType = UserStatConfig.RankConfig.RankType.voiceChatTime;
                sortedByVCTime.Sort();

                for (int index = 0; index < userStatTrackersList.Count; index++)
                {
                    //give rank position based off of index
                    sortedByMessages[index].messageRankPosition = index;
                    sortedByVCTime[index].vcTimeRankPosition = index;
                }

                dIRef.ConfigRef.rankConfig.rankType = UserStatConfig.RankConfig.RankType.msgAndVCT;
                //now when userStatTrackerList sorts will have accurate data
            }

            //NOTE:
            //will use default comparer AKA "CompareTo()" function in UserStatTrackerClass
            //comparer configured by static struct "rankConfig" in UserStatTrackerClass
            userStatTrackersList.Sort();

            //generate new ranked iD list
            rankedUsers = new List<ulong>();
            for (int index = 0; index < userStatTrackersList.Count; index++)
            {
                rankedUsers.Add(userStatTrackersList[index].usersID);
            }

            return;
        }        

        public void CreateUnmadeRoles(SocketGuild guildRef)
        {
            //if role not in array or discord doesn't have it make a whole new role
            for (int index = 0; index < rankRoles.Length; index++)
            {       //if role null or cant find roles in discord with same index
                if (guildRef.GetRole(rankRoles[index].Id) == null)                           
                {
                    Discord.Rest.RestRole tempRestRole = guildRef.CreateRoleAsync(rankRoles[index].name, null, rankRoles[index].color, true, true).Result;

                    dIRef.LogRef.Log($"RankRole Created: {rankRoles[index].name}");

                    //set roles ID to created discord role ID
                    rankRoles[index].Id = tempRestRole.Id;

                }
            }
            return;
        }

        /// <summary>
        /// Returns -1 if no users in ranked list with that ID.
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        public int GetUsersRank(ulong userID)
        {
            int rank = -1;

            if(rankedUsers != null)
            {
                rank = rankedUsers.IndexOf(userID);
            }
            else
            {
                dIRef.LogRef.Log(new Discord.LogMessage(Discord.LogSeverity.Error, this.ToString(), "Error: rankedUsers null"));
            }

            return rank;
        }

        public List<ulong> GetTopUsers(int numberOfUsers)
        {
            List<ulong> topUsers = new List<ulong>();

            for (int i = 0; i < rankedUsers.Count && i < numberOfUsers; i++)
            {
                topUsers.Add(rankedUsers[i]);
            }

            return topUsers;
        }

        public List<SocketGuildUser> GetAllUsersInRank(UserStatRole rankRole, SocketGuild guildRef)
        {
            List<SocketGuildUser> userList = new List<SocketGuildUser>();

            if (guildRef.GetRole(rankRole.Id) == null)
            {
                dIRef.LogRef.Log(new Discord.LogMessage(Discord.LogSeverity.Error, this.ToString(), "Error: No role in the guild with that ID."));
                return null;
            }

            SocketGuildUser guildUser;
            IEnumerator<SocketGuildUser> guildUserE = guildRef.GetRole(rankRole.Id).Members.GetEnumerator();

            while (guildUserE.MoveNext())
            {
                guildUser = guildUserE.Current;

                userList.Add(guildUser);
            }

            return userList;

        }

        #region SAVE/LOADING

        /// <summary>
        /// Ensures UserStat roles are in line with Discord roles and then saves them
        /// </summary>
        /// <param name="guildRef"></param>
        /// <param name="saveRef"></param>
        /// <returns></returns>
        public Task SaveRankRoles(SocketGuild guildRef, SaveHandler saveRef)
        {
            //make sure roles array is up to date and saved

            //iterate through guilds current roles

            SocketRole discordRole;
            IEnumerator<SocketRole> roleE = guildRef.Roles.GetEnumerator();

            while (roleE.MoveNext())
            {
                discordRole = roleE.Current;

                for (int userStatRole = 0; userStatRole < rankRoles.Length; userStatRole++)
                {

                    //if same ID as a the roles Array role ID update the roles Array role
                    if (rankRoles[userStatRole].Id.Equals(discordRole.Id))
                    {
                        //set so has same properties
                        rankRoles[userStatRole].name = discordRole.Name;
                        rankRoles[userStatRole].color = discordRole.Color;
                        rankRoles[userStatRole].position = discordRole.Position;
                        userStatRole = rankRoles.Length; //end nested loop
                    }
                }
            }

            //save roles array
            saveRef.SaveArray(rankRoles, rolesSaveFileName, guildRef);

            return Task.CompletedTask;
        }

        public void SaveRankedUsers()
        {
            SaveHandler.S.SaveObject(rankedUsers, nameof(rankedUsers), dIRef.GuildRef);
        }

        public void LoadRankedUsers()
        {
            SaveHandler.S.LoadObject(out rankedUsers, nameof(rankedUsers), dIRef.GuildRef);
            if(rankedUsers == null)
            {
                rankedUsers = new List<ulong>();
                foreach (KeyValuePair<ulong, UserStatTracker> item in dIRef.ContRef.guildUserIDToStatIndex)
                {
                    rankedUsers.Add(item.Key);
                }
            }
        }

        #endregion
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        #endregion
        public struct UserStatRole
        {
            public ulong Id;
            public string name;
            public int memberLimit;
            public Discord.Color color;
            public int position;
            public UserStatRole(ulong roleId, string roleName, int roleMemberLimit, Discord.Color roleColor, int rolePosition)
            {
                Id = roleId;
                name = roleName;
                memberLimit = roleMemberLimit;
                color = roleColor;
                position = rolePosition;
            }
        }
    }
}
