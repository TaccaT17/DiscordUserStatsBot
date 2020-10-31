using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace DiscordUserStatsBot
{
    class UserStats
    {
        //Will log stats up to a year

        #region VARIABLES
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------

        private SocketGuildUser myGuildUser;
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
        public UserStats(SocketGuildUser guildUser)
        {
            myGuildUser = guildUser;
        }

        //FUNCTION START
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        public void CalculateAndUpdateUserStats()
        {
            if(totalVCTime == null)
            {
                totalVCTime = new TimeSpan();
            }
            totalVCTime += lastTimeLeftVC - lastTimeEnteredVC;
        }

        public void RecordGuildUserEnterVoiceChatTime()
        {
            lastTimeEnteredVC = DateTime.UtcNow;
            Console.WriteLine($"User entered chat at {lastTimeEnteredVC.ToString()}");
        }

        public void RecordGuildUserLeaveVoiceChatTime()
        {
            lastTimeLeftVC = DateTime.UtcNow;
            Console.WriteLine($"User entered chat at {lastTimeLeftVC.ToString()}");
        }



        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        //FUNCTION END

    }
}
