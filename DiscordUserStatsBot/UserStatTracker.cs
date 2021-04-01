//Copyright Tom Crammond 2021

using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace DiscordUserStatsBot
{
    class UserStatTracker: IComparable<UserStatTracker>
    {
        //Will log stats up to the last 30 days

        #region VARIABLES
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------

        private UserStatsBotController myCont;
        [Newtonsoft.Json.JsonProperty] //JSON by default only serializes public members. This is the work around
        private string usersFullName;
        [Newtonsoft.Json.JsonProperty]
        public ulong usersID;

        [Newtonsoft.Json.JsonProperty]
        private TimeSpan totalAllTimeVCTime;
        [Newtonsoft.Json.JsonProperty]
        private int totalAllTimeMessagesSent;

        [Newtonsoft.Json.JsonProperty]
        private TimeSpan totalVCTime;
        [Newtonsoft.Json.JsonProperty]
        private int totalMessagesSent;
        [Newtonsoft.Json.JsonProperty]
        private TimeSpan avgVCTime;
        [Newtonsoft.Json.JsonProperty]
        private float avgMsgsSent;

        //VC = voice chat
        [Newtonsoft.Json.JsonProperty]
        private DateTime lastTimeEnteredVC;
        [Newtonsoft.Json.JsonProperty]
        private DateTime lastTimeLeftVC;
        [Newtonsoft.Json.JsonProperty]
        private DateTime dateTrackerCreated;

        //records users stats for each day for the past 30 days. Goes from OldestDay -> NewestDay
        [Newtonsoft.Json.JsonProperty]
        private UserStatDay[] userStatsDays;

        //these are only used when trying to sort by both messages and vcTime
        public int messageRankPosition;
        public int vcTimeRankPosition;

        //public getters
        public string UsersFullName
        {
            get { return usersFullName; }
        }
        

        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        #endregion

        //CONSTRUCTOR
        public UserStatTracker(UserStatsBotController controllerRef, string userName, ulong userID)
        {
            myCont = controllerRef;
            usersFullName = userName;
            usersID = userID;

            //set defaults to zero
            totalAllTimeVCTime = TimeSpan.Zero;
            totalAllTimeMessagesSent = 0;

            userStatsDays = new UserStatDay[30];
            //give each day a date
            DateTime dTSetUp = DateTime.Now;
            dTSetUp = dTSetUp.Date;
            //populate array backwards (so that "today"/present is always at end of array)
            for (int day = (userStatsDays.Length - 1); day >= 0; day--)
            {
                userStatsDays[day] = new UserStatDay(dTSetUp);

                dTSetUp = dTSetUp.AddDays(-1);
            }

            dateTrackerCreated = DateTime.Now;
            //AddFakeData();
        }

        #region FUNCTIONS
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------

        public void SetController(UserStatsBotController controller)
        {
            myCont = controller;
        }

        //TODO: Getting called too often?
        public void RecordGuildUserEnterVoiceChatTime()
        {
            lastTimeEnteredVC = DateTime.Now;
            myCont.Log(new Discord.LogMessage(Discord.LogSeverity.Debug, this.ToString(), $"{usersFullName} entered chat at {lastTimeLeftVC}."));
        }

        public void RecordGuildUserLeaveVoiceChatTime()
        {
            lastTimeLeftVC = DateTime.Now;
            TimeSpan timeElapsed = lastTimeLeftVC - lastTimeEnteredVC;

            if (totalAllTimeVCTime == null)
            {
                totalAllTimeVCTime = new TimeSpan();
            }
            totalAllTimeVCTime += timeElapsed;

            userStatsDays[GetIndexOfDay(DateTime.Now)].vCTime += timeElapsed;
            myCont.Log(new Discord.LogMessage(Discord.LogSeverity.Debug, this.ToString(), $"{usersFullName} left chat at {lastTimeLeftVC}. Increased chattime by {timeElapsed.ToString(@"dd\.hh\:mm\:ss")}"));
        }

        public Task RecordThisUserSentAMessage(SocketMessage message)
        {
            totalAllTimeMessagesSent++;

            userStatsDays[GetIndexOfDay(DateTime.Now)].messagesSent++;

            myCont.Log(new Discord.LogMessage(Discord.LogSeverity.Debug, this.ToString(), $"{usersFullName} sent a message!"));

            return Task.CompletedTask;
        }

        private int GetIndexOfDay(DateTime dateToChange)
        {
            //Future proofing XD
            if(dateToChange.Date > DateTime.Now)
            {
                myCont.Log("You should not be searching for stat info on a day that has yet to occur...");
                dateToChange = DateTime.Now.Date;
            }

            //find matching date to dateToChange
            bool dayInArray = false;
            //used to get index of UserStatDay that is being modified
            int dayToModIndex = 0;

            //find dateToChange day in list
            for (int day = userStatsDays.Length - 1; day >= 0; day--)
            {
                if (userStatsDays[day].date.Date.Equals(dateToChange.Date))
                {
                    dayInArray = true;
                    dayToModIndex = day;
                    day = -1; //end loop
                }
            }

            //if todays day not in list...
            if (!dayInArray)
            {
                //get difference of days between today and last entry in list
                TimeSpan tS = dateToChange - userStatsDays[userStatsDays.Length - 1].date;
                int daysDifferenceLastEntryAndToday = tS.Days;

                //move days over
                int dayMoveTo = 0;

                for (int dayMoveFrom = (daysDifferenceLastEntryAndToday); dayMoveFrom < userStatsDays.Length;)
                {
                    UserStatDay tempDay = userStatsDays[dayMoveFrom];
                    userStatsDays[dayMoveTo] = tempDay;
                    dayMoveFrom++;
                    dayMoveTo++;
                }

                DateTime dTSetUp = userStatsDays[dayMoveTo - 1].date;

                for (; dayMoveTo < userStatsDays.Length; dayMoveTo++)
                {
                    dTSetUp = dTSetUp.AddDays(1);
                    userStatsDays[dayMoveTo] = new UserStatDay(dTSetUp);
                }

                dayToModIndex = userStatsDays.Length - 1;
            }

            return dayToModIndex;
        }
        
        public Task UpdateUsersName(SocketGuildUser correspondingGuildUser)
        {
            usersFullName = correspondingGuildUser.Username + '#' + correspondingGuildUser.Discriminator;

            return Task.CompletedTask;
        }

        #region AVERAGES AND TOTALS

        //TODO: Ask Ben about ways to merge these functions

        //Can get all time totals from member variables
        public int DetermineDays(int pastNumberOfDays)
        {
            if (pastNumberOfDays > userStatsDays.Length)
            {
                myCont.Log(new Discord.LogMessage(Discord.LogSeverity.Debug, this.ToString(), $@"Beware: This StatTracker has only recorded the past {userStatsDays.Length} days."));
                pastNumberOfDays = userStatsDays.Length;
            }
            //if # of days searching for exceeds # of days since this stat tracker got made, instead return average since stat tracker created or average since minimum days
            else if ((pastNumberOfDays > (DateTime.Now - dateTrackerCreated).Days))
            {
                pastNumberOfDays = (DateTime.Now - dateTrackerCreated).Days;

                if ((DateTime.Now - dateTrackerCreated).Days <= 0)
                {
                    pastNumberOfDays = 1;
                }

                //myCont.Log(new Discord.LogMessage(Discord.LogSeverity.Debug, this.ToString(), $"Number of days wanted is {pastNumberOfDays}"));

                if (pastNumberOfDays < myCont.userStatConfigRef.rankConfig.minAvgDays && myCont.userStatConfigRef.rankConfig.minAvgDays < (int)myCont.userStatConfigRef.rankConfig.rankTime)
                {
                    pastNumberOfDays = myCont.userStatConfigRef.rankConfig.minAvgDays;
                }
            }

            return pastNumberOfDays;
        }

        /// <summary>
        /// Update and get users average chat time
        /// </summary>
        /// <param name="pastNumberOfDays"></param>
        /// <returns></returns>
        public TimeSpan AverageChatTime(int pastNumberOfDays)
        {
            pastNumberOfDays = DetermineDays(pastNumberOfDays);

            avgVCTime = TotalChatTime(pastNumberOfDays) / pastNumberOfDays;

            return avgVCTime;
        }
        /// <summary>
        /// Update and get users average messages
        /// </summary>
        /// <param name="pastNumberOfDays"></param>
        /// <returns></returns>
        public float AverageMessages(int pastNumberOfDays)
        {
            pastNumberOfDays = DetermineDays(pastNumberOfDays);

            avgMsgsSent = TotalMessages(pastNumberOfDays) / (float)pastNumberOfDays;

            return avgMsgsSent;
        }
        /// <summary>
        /// Update and get users total chat time
        /// </summary>
        /// <param name="pastNumberOfDays"></param>
        /// <returns></returns>
        public TimeSpan TotalChatTime(int pastNumberOfDays)
        {
            if (pastNumberOfDays > userStatsDays.Length)
            {
                myCont.Log(new Discord.LogMessage(Discord.LogSeverity.Debug, this.ToString(), $@"Beware: This StatTracker has only recorded the past {userStatsDays.Length} days."));
                pastNumberOfDays = userStatsDays.Length;
            }
            //if # of days searching for exceeds # of days since this stat tracker got made, instead return total since stat tracker created
            else if ((pastNumberOfDays > (DateTime.Now - dateTrackerCreated).Days) || pastNumberOfDays < 1)
            {
                return totalAllTimeVCTime;
            }

            totalVCTime = TimeSpan.Zero;

            int todaysIndex = GetIndexOfDay(DateTime.Now);
            for (int dayIndex = todaysIndex; dayIndex >= 0 && dayIndex > (todaysIndex - pastNumberOfDays); dayIndex--)
            {
                totalVCTime += userStatsDays[dayIndex].vCTime;
            }

            return totalVCTime;
        }
        /// <summary>
        /// Update and get users total messages
        /// </summary>
        /// <param name="pastNumberOfDays"></param>
        /// <returns></returns>
        public int TotalMessages(int pastNumberOfDays)
        {
            if (pastNumberOfDays > userStatsDays.Length)
            {
                myCont.Log(new Discord.LogMessage(Discord.LogSeverity.Debug, this.ToString(), $@"Beware: This StatTracker has only recorded the past {userStatsDays.Length} days."));
                pastNumberOfDays = userStatsDays.Length;
            }
            //if # of days searching for exceeds # of days since this stat tracker got made, instead return total since stat tracker created
            else if ((pastNumberOfDays > (DateTime.Now - dateTrackerCreated).Days) || pastNumberOfDays < 1)
            {
                return totalAllTimeMessagesSent;
            }

            totalMessagesSent = 0;

            int todaysIndex = GetIndexOfDay(DateTime.Now);
            for (int dayIndex = todaysIndex; dayIndex >= 0 && dayIndex > (todaysIndex - pastNumberOfDays); dayIndex--)
            {
                totalMessagesSent += userStatsDays[dayIndex].messagesSent;
            }

            return totalMessagesSent;
        }
        public TimeSpan GetTotalAverageChatTime()
        {
            int daysSinceTrackerCreated = (DateTime.Now - dateTrackerCreated).Days;
            if (daysSinceTrackerCreated < 1)
            {
                daysSinceTrackerCreated = 1;
            }
            return totalAllTimeVCTime / (float)daysSinceTrackerCreated;
        }
        public float GetTotalAverageMessages()
        {
            int daysSinceTrackerCreated = (DateTime.Now - dateTrackerCreated).Days;
            if (daysSinceTrackerCreated < 1)
            {
                daysSinceTrackerCreated = 1;
            }
            return totalAllTimeMessagesSent / (float)daysSinceTrackerCreated;
        }
        #endregion

        

        //INTERFACE IMPLEMENTATION
        //---------------------------------------------------------------------

        //default compares both messages and chat by month
        public int CompareTo(UserStatTracker other)
        {
            //rank by average messages
            if (myCont.userStatConfigRef.rankConfig.rankType.Equals(UserStatConfig.RankConfig.RankType.messages) && myCont.userStatConfigRef.rankConfig.rankBy.Equals(UserStatConfig.RankConfig.RankByType.average))
            {
                if(this.AverageMessages((int)myCont.userStatConfigRef.rankConfig.rankTime) > other.AverageMessages((int)myCont.userStatConfigRef.rankConfig.rankTime))
                {
                    //higher rank
                    return -1;
                }
                else if(this.AverageMessages((int)myCont.userStatConfigRef.rankConfig.rankTime) < other.AverageMessages((int)myCont.userStatConfigRef.rankConfig.rankTime))
                {
                    //lower rank
                    return 1;
                }
                else
                {
                    //same rank
                    return 0;
                }
            }
            //rank by average voice chat time
            else if (myCont.userStatConfigRef.rankConfig.rankType.Equals(UserStatConfig.RankConfig.RankType.voiceChatTime) && myCont.userStatConfigRef.rankConfig.rankBy.Equals(UserStatConfig.RankConfig.RankByType.average))
            {
                if (this.AverageChatTime((int)myCont.userStatConfigRef.rankConfig.rankTime) > other.AverageChatTime((int)myCont.userStatConfigRef.rankConfig.rankTime))
                {
                    return -1;
                }
                else if (this.AverageChatTime((int)myCont.userStatConfigRef.rankConfig.rankTime) < other.AverageChatTime((int)myCont.userStatConfigRef.rankConfig.rankTime))
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
            //rank by total messages
            else if (myCont.userStatConfigRef.rankConfig.rankType.Equals(UserStatConfig.RankConfig.RankType.messages) && myCont.userStatConfigRef.rankConfig.rankBy.Equals(UserStatConfig.RankConfig.RankByType.total))
            {
                if (this.TotalMessages((int)myCont.userStatConfigRef.rankConfig.rankTime) > other.TotalMessages((int)myCont.userStatConfigRef.rankConfig.rankTime))
                {
                    return -1;
                }
                else if (this.TotalMessages((int)myCont.userStatConfigRef.rankConfig.rankTime) < other.TotalMessages((int)myCont.userStatConfigRef.rankConfig.rankTime))
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
            //rank by total voice chat time
            else if (myCont.userStatConfigRef.rankConfig.rankType.Equals(UserStatConfig.RankConfig.RankType.voiceChatTime) && myCont.userStatConfigRef.rankConfig.rankBy.Equals(UserStatConfig.RankConfig.RankByType.total))
            {
                if (this.TotalChatTime((int)myCont.userStatConfigRef.rankConfig.rankTime) > other.TotalChatTime((int)myCont.userStatConfigRef.rankConfig.rankTime))
                {
                    return -1;
                }
                else if (this.TotalChatTime((int)myCont.userStatConfigRef.rankConfig.rankTime) < other.TotalChatTime((int)myCont.userStatConfigRef.rankConfig.rankTime))
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
            //rank by average both
            if (myCont.userStatConfigRef.rankConfig.rankType.Equals(UserStatConfig.RankConfig.RankType.msgAndVCT))
            {
                //smaller number = higher rank
                if ((this.messageRankPosition + this.vcTimeRankPosition) < (other.messageRankPosition + other.vcTimeRankPosition))
                {
                    //this is higher rank
                    return -1;
                }
                else if ((this.messageRankPosition + this.vcTimeRankPosition) > (other.messageRankPosition + other.vcTimeRankPosition))
                {
                    //this is lower rank
                    return 1;
                }
                else
                {
                    //this is same rank
                    return 0;
                }
            }
            return 1;
        }

        //---------------------------------------------------------------------

        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        #endregion

        //STRUCTS
        private struct UserStatDay
        {
            public int messagesSent;
            public TimeSpan vCTime;
            public DateTime date;

            public UserStatDay(DateTime dayDate)
            {
                date = dayDate;
                messagesSent = 0;
                vCTime = TimeSpan.Zero;
            }
        }

    }
}
