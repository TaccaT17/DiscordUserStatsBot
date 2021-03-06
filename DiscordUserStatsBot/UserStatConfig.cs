﻿//Copyright Tom Crammond 2021

using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordUserStatsBot
{
    public class UserStatConfig : BotComponent
    {
        //rank config
        public RankConfig rankConfig;

        public void DefaultRankConfig()
        {
            //by default ranks users by average messages and vcTime in the past month
            rankConfig.rankType = RankConfig.RankType.msgAndVCT;
            rankConfig.rankBy = RankConfig.RankByType.average;
            rankConfig.rankTime = RankConfig.RankTimeType.month;
            rankConfig.minAvgDays = ((int)rankConfig.rankTime / 2);
            rankConfig.initialized = true;
            dIRef.LogRef.Log($@"RankConfig initialized");
        }

        #region CHANGE CRITERIA
        //TODO: way to make these one function?
        public void ChangeRankCriteria(RankConfig.RankType newRankType, UserStatsBotController contRef)
        {
            rankConfig.rankType = newRankType;
            SaveHandler.S.SaveObject(rankConfig, nameof(rankConfig), dIRef.GuildRef);
        }
        public void ChangeRankCriteria(RankConfig.RankByType newRankByType, UserStatsBotController contRef)
        {
            rankConfig.rankBy = newRankByType;
            SaveHandler.S.SaveObject(rankConfig, nameof(rankConfig), dIRef.GuildRef);
        }
        public void ChangeRankCriteria(RankConfig.RankTimeType newRankTimeType, UserStatsBotController contRef)
        {
            rankConfig.rankTime = newRankTimeType;
            rankConfig.minAvgDays = (int)rankConfig.rankTime / 2;
            SaveHandler.S.SaveObject(rankConfig, nameof(rankConfig), dIRef.GuildRef);
            dIRef.LogRef.Log(new Discord.LogMessage(Discord.LogSeverity.Debug, this.ToString(), $"minAvgDays is {rankConfig.minAvgDays}"));
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
