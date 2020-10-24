using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;

namespace DiscordUserStatsBot
{
    class UserStats
    {
        //Will log stats up to a year

        //VARIABLES START
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        private float totalVoiceChatTime;
        private int totalMessagesSent;

        public int TotalMessagesSent { get; }

        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        //VARIABLES END

        //FUNCTION START
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        public void UpdateUserStats()
        {

        }
        
        public string GetTotalVoiceChatTime()
        {
            string totalVoiceChatTime = "00:00:00";

            return totalVoiceChatTime;
        }
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        //FUNCTION END

    }
}
