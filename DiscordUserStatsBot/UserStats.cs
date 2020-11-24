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

        public SocketGuildUser myGuildUser;
        public UserStatsBotController myController;
        private TimeSpan totalVCTime;
        private int totalMessagesSent = 0;

        //VC = voice chat
        private DateTime lastTimeEnteredVC;
        private DateTime lastTimeLeftVC;

        //dictionary 

        public int TotalMessagesSent {
            get {return totalMessagesSent; }
            set
            {
                totalMessagesSent = value;
            }
        }

        public TimeSpan TotalVoiceChatTime { 
            get { return totalVCTime; }
            set
            {
                totalVCTime = value;
            }
        }

        public DateTime LastTimeEnteredVoiceChat
        {
            set
            {
                lastTimeEnteredVC = value;
            }
        }

        public DateTime LastTimeLeftVoiceChat
        {
            set
            {
                lastTimeLeftVC = value;
            }
        }


        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        #endregion

        //CONSTRUCTOR
        public UserStats(SocketGuildUser guildUser, UserStatsBotController controllerRef)
        {
            myGuildUser = guildUser;

            myController = controllerRef;
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

        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        #endregion

    }
}
