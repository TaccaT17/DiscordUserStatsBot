using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordUserStatsBot
{
    class UserStatConfig
    {
        //rank config
        public RankConfig rankConfig;

        private UserStatsBotController myCont;

        /*public UserStatConfig()
        {
            if (!rankConfig.initialized)
            {
                DefaultRankConfig();
            }
        }*/

        public UserStatConfig(UserStatsBotController controller)
        {
            myCont = controller;
        }

        public void DefaultRankConfig()
        {
            //by default ranks users by average messages and vcTime in the past month
            rankConfig.rankType = UserStatConfig.RankConfig.RankType.msgAndVCT;
            rankConfig.rankBy = UserStatConfig.RankConfig.RankByType.average;
            rankConfig.rankTime = UserStatConfig.RankConfig.RankTimeType.month;
            rankConfig.minAvgDays = ((int)rankConfig.rankTime / 2);
            rankConfig.initialized = true;
            myCont.Log($@"RankConfig initialized");
        }

        #region CHANGE CRITERIA
        //TODO: way to make these one function?
        public void ChangeRankCriteria(UserStatConfig.RankConfig.RankType newRankType, UserStatsBotController contRef)
        {
            rankConfig.rankType = newRankType;
            contRef.saveHandlerRef.SaveObject(rankConfig, nameof(rankConfig), contRef.GuildRef);
        }
        public void ChangeRankCriteria(UserStatConfig.RankConfig.RankByType newRankByType, UserStatsBotController contRef)
        {
            rankConfig.rankBy = newRankByType;
            contRef.saveHandlerRef.SaveObject(rankConfig, nameof(rankConfig), contRef.GuildRef);
        }
        public void ChangeRankCriteria(UserStatConfig.RankConfig.RankTimeType newRankTimeType, UserStatsBotController contRef)
        {
            rankConfig.rankTime = newRankTimeType;
            contRef.saveHandlerRef.SaveObject(rankConfig, nameof(rankConfig), contRef.GuildRef);
            rankConfig.minAvgDays = (int)rankConfig.rankTime / 2;
        }
        #endregion

        //STRUCT
        public struct RankConfig
        {
            public enum RankType
            {
                messages,
                voiceChatTime,
                msgAndVCT
            }
            public enum RankByType
            {
                average,
                total
            }
            public enum RankTimeType
            {
                day = 1,
                week = 7,
                month = 30
            }

            public bool initialized;
            public RankType rankType;
            public RankByType rankBy;
            public RankTimeType rankTime;
            public int minAvgDays; //cannot be higher than rankTime
        }
    }
}
