using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;

namespace DiscordUserStatsBot
{

    //USE REST WHEN SENDING REQUEST, USE WEBSOCKET WHEN 

    //creates different roles 
    //bool for enabling or disabling roles


    class UserStatsRoles
    {

        #region VARIABLES
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------


        //NOTE: "Discord Role" refers to role that is saved by discord, "UserStat Role" refers to a struct that contains the relevant info to save for that role

        //roles ordered highest esteem to lowest
        public UserStatRole[] rankRoles;

        private Discord.Color[] defaultRoleColors;
        private string[] defaultRoleNames;
        private int[] defaultRoleMemberAmount;
        private int defaultNumberOfRoles = 5;

        public string rolesSaveFileName = "rolesSave";

        private List<ulong> rankedUsers; //messageRank, chatRank, totalRank;

        UserStatsBotController myCont;

        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        #endregion

        //Constructor
        public UserStatsRoles(UserStatsBotController myController)
        {
            myCont = myController;
        }

        #region FUNCTIONS
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------

        public void CreateDefaultRolesArray()
        {
            defaultRoleColors = new Discord.Color[] { Discord.Color.Orange, Discord.Color.Purple, Discord.Color.Blue, Discord.Color.Green, Discord.Color.LighterGrey };
            defaultRoleNames = new string[] { "Apexian Exarch", "High Baroness", "Earl", "Low Inquisitor", "Simple Sentinel" };
            //TODO: Change back to                3, 5, 10, 30, 100
            defaultRoleMemberAmount = new int[] { 1, 1, 1, 1, 1 };
            //make default array of UserStatRole struct
            rankRoles = new UserStatRole[defaultNumberOfRoles];
            for (int index = 0; index < rankRoles.Length; index++)
            {
                rankRoles[index] = new UserStatRole((ulong)0, defaultRoleNames[index], defaultRoleMemberAmount[index], defaultRoleColors[index], 0);
            }

            Console.WriteLine("Default Roles Created");
        }



        /// <summary>
        /// Assigns users their appropriate role. Called when bot started and once every day
        /// </summary>
        /// <param name="guildRef"></param>
        public async void AssignRoles(SocketGuild guildRef)
        {
            Console.WriteLine("Assign Roles Start");

            //check to make sure have all roles
            CreateRoles(guildRef);

            //if ranked users not initialized then make list from dictionary
            if(rankedUsers == null)
            {
                rankedUsers = new List<ulong>();
                foreach(KeyValuePair<ulong, UserStatTracker> item in myCont.guildUserIDToStatIndex)
                {
                    rankedUsers.Add(item.Key);
                }
            }

            RankUsers();

            //go through rankedList and give each user appropriate role

            int rankedUserIndex = 0;

            for (int rankRole = 0; rankRole < rankRoles.Length; rankRole++)
            {
                for (int index = 0; index < rankRoles[rankRole].memberLimit; index++)
                {
                    if(rankedUsers.Count < 1)
                    {
                        Console.WriteLine("No current users to assign roles to");
                        return;
                    }

                    if(rankedUserIndex >= rankedUsers.Count)
                    {
                        Console.WriteLine("Ran out of users to assign roles to");
                        return;
                    }

                    SocketRole usersExistingRoles;

                    IEnumerator<SocketRole> userExistingRoleE = guildRef.GetUser(rankedUsers[rankedUserIndex]).Roles.GetEnumerator();

                    bool hasRole = false;

                    while (userExistingRoleE.MoveNext())
                    {
                        usersExistingRoles = userExistingRoleE.Current;

                        //if user already has role you are good
                        if (usersExistingRoles.Id.Equals(rankRoles[rankRole].Id))
                        {
                            hasRole = true;
                        }
                        //if the user has another ranked role remove that one
                        //TODO: is iterationg more times than necessary
                        else
                        {
                            for (int role = 0; role < rankRoles.Length; role++)
                            {
                                if (usersExistingRoles.Id.Equals(rankRoles[role].Id))
                                {
                                    await guildRef.GetUser(rankedUsers[rankedUserIndex]).RemoveRoleAsync(guildRef.GetRole(rankRoles[role].Id));
                                }
                            }
                        }
                    }

                    if (!hasRole)
                    {
                        //if user doesnt have role assign it
                        await guildRef.GetUser(rankedUsers[rankedUserIndex]).AddRoleAsync(guildRef.GetRole(rankRoles[rankRole].Id));
                    }

                    rankedUserIndex++;
                }
            }

            Console.WriteLine("Assign Roles End");

        }

        /// <summary>
        /// Reorders rankedUsers list so sorted highest -> lowest rank
        /// Can modify rank sorting options using static RankConfig struct in UserStatTracker
        /// </summary>
        public void RankUsers()
        {

            //ensure all recorded ids in rankedUsers list
            foreach(KeyValuePair<ulong, UserStatTracker> item in myCont.guildUserIDToStatIndex)
            {
                if (!(rankedUsers.Contains(item.Key)))
                {
                    //if not in list add it
                    rankedUsers.Add(item.Key);
                }
            }

            //generate ordered list of userStatTrackers from ranked iD list. I use rankedID list for this because it should be already pretty close to being in the correct order.
            List<UserStatTracker> userStatTrackersList = new List<UserStatTracker>();
            for (int iD = 0; iD < rankedUsers.Count; iD++)
            {
                userStatTrackersList.Add(myCont.guildUserIDToStatIndex[rankedUsers[iD]]);
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

        public void ChangeRoleMaxUsers(int newMaxUsers)
        {

        }

        public void CreateRoles(SocketGuild guildRef)
        {
            //if role not in array or discord doesn't have it make a whole new role
            for (int index = 0; index < rankRoles.Length; index++)
            {       //if role null or cant find roles in discord with same index
                if (guildRef.GetRole(rankRoles[index].Id) == null)                           //NULL CHECK? DONT NEED ONE CAUSE STRUCT?
                {
                    //create discord role using info in roles array
                    Discord.Rest.RestRole tempRestRole = guildRef.CreateRoleAsync(rankRoles[index].name, null, rankRoles[index].color, true, true).Result;

                    //set roles ID to created discord role ID
                    rankRoles[index].Id = tempRestRole.Id;

                    //Console.WriteLine($@"{tempRestRole.Name} role created.");
                }
                else
                {
                    //Console.WriteLine($@"{rankRoles[index].name} role found");
                }
            }
            return;
        }

        /// <summary>
        /// Ensures UserStat roles are in line with Discord roles and then saves them
        /// </summary>
        /// <param name="guildRef"></param>
        /// <param name="saveRef"></param>
        /// <returns></returns>
        public Task SaveRoles(SocketGuild guildRef, SaveHandler saveRef)
        {
            //make sure roles array is up to date and saved

            //iterate through guilds current roles

            SocketRole discordRole;
            IEnumerator<SocketRole> roleE = guildRef.Roles.GetEnumerator();

            while (roleE.MoveNext())
            {
                discordRole = roleE.Current;

                //Console.WriteLine($@"Discord role = {discordRole.Name}");

                for (int userStatRole = 0; userStatRole < rankRoles.Length; userStatRole++)
                {
                    //Console.WriteLine($@"   Array role = {rankRoles[userStatRole].name}");

                    //if same ID as a the roles Array role ID update the roles Array role
                    if (rankRoles[userStatRole].Id.Equals(discordRole.Id))                   //NULL CHECK? DONT NEED ONE CAUSE STRUCT?
                    {
                        //Console.WriteLine($@"       Same Role!");
                        //set so has same properties
                        rankRoles[userStatRole].name = discordRole.Name;
                        rankRoles[userStatRole].color = discordRole.Color;
                        rankRoles[userStatRole].position = discordRole.Position;
                        userStatRole = rankRoles.Length; //end nested loop
                    }
                }
            }

            //save roles array
            saveRef.SaveArray(rankRoles, rolesSaveFileName);

            //Console.WriteLine("Roles Saved");

            return Task.CompletedTask;
        }

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
