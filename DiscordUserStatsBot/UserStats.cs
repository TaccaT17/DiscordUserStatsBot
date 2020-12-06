using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace DiscordUserStatsBot
{
    class UserStats
    {
        //Will log stats up to a year

        #region VARIABLES
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------

        private UserStatsBotController myController;
        [Newtonsoft.Json.JsonProperty] //JSON by default only serializes public members. This is the work around
        private string usersFullName;
        [Newtonsoft.Json.JsonProperty]
        private TimeSpan totalVCTime;
        [Newtonsoft.Json.JsonProperty]
        private int totalMessagesSent;
        //VC = voice chat
        [Newtonsoft.Json.JsonProperty]
        private DateTime lastTimeEnteredVC;
        [Newtonsoft.Json.JsonProperty]
        private DateTime lastTimeLeftVC;

        //public getters
        public int TotalMessagesSent {
            get {return totalMessagesSent; }
        }
        public TimeSpan TotalVoiceChatTime { 
            get { return totalVCTime; }
        }
        public DateTime LastTimeEnteredVoiceChat
        {
            get { return lastTimeEnteredVC; }
        }
        public DateTime LastTimeLeftVoiceChat
        {
            get { return lastTimeLeftVC; }
        }
        public string UsersFullName
        {
            get { return usersFullName; }
        }


        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        #endregion

        //CONSTRUCTOR
        public UserStats(UserStatsBotController controllerRef, string userName)
        {
            myController = controllerRef;
            usersFullName = userName;

            //set defaults to zero
            totalVCTime = TimeSpan.Zero;
            totalMessagesSent = 0;
        }

        #region FUNCTIONS
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        public void CalculateAndUpdateUserVCTime()
        {
            if(totalVCTime == null)
            {
                totalVCTime = new TimeSpan();
            }
            totalVCTime += lastTimeLeftVC - lastTimeEnteredVC;
            //Console.WriteLine($"Calculations performed. Total is {totalVCTime}");
        }

        //TODO: Getting called too often?
        public void RecordGuildUserEnterVoiceChatTime()
        {
            lastTimeEnteredVC = DateTime.UtcNow;
            //Console.WriteLine($"User entered chat at {lastTimeEnteredVC.ToString()}");
        }

        public void RecordGuildUserLeaveVoiceChatTime()
        {
            lastTimeLeftVC = DateTime.UtcNow;
            //Console.WriteLine($"User left all chats at {lastTimeLeftVC.ToString()}");
        }

        public Task RecordThisUserSentAMessage(SocketMessage message)
        {
            //Console.WriteLine($@"Recorded {myGuildUser} sent a Message");

            totalMessagesSent++;

            return Task.CompletedTask;
        }

        public Task UpdateUsersName(SocketGuildUser correspondingGuildUser)
        {
            usersFullName = correspondingGuildUser.Username + '#' + correspondingGuildUser.Discriminator;

            return Task.CompletedTask;
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        #endregion

    }
}
