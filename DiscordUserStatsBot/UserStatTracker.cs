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

        private UserStatsBotController myController;
        [Newtonsoft.Json.JsonProperty] //JSON by default only serializes public members. This is the work around
        private string usersFullName;
        [Newtonsoft.Json.JsonProperty]
        public ulong usersID;
        [Newtonsoft.Json.JsonProperty]
        private TimeSpan totalVCTime;
        [Newtonsoft.Json.JsonProperty]
        private int totalMessagesSent;
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

        //rank config
        public static RankConfig rankConfig;

        //these are only used when trying to sort by both messages and vcTime
        public int messageRankPosition;
        public int vcTimeRankPosition;

        //save role ID?

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
        public UserStatTracker(UserStatsBotController controllerRef, string userName, ulong userID)
        {
            myController = controllerRef;
            usersFullName = userName;
            usersID = userID;

            //set defaults to zero
            totalVCTime = TimeSpan.Zero;
            totalMessagesSent = 0;

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

            if (!rankConfig.initialized)
            {
                DefaultRankConfig();
            }
            

            //AddFakeData();

        }

        #region FUNCTIONS
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------

        public static void DefaultRankConfig()
        {
            //by default ranks users by average messages and vcTime in the past month
            rankConfig.rankType = RankConfig.RankType.msgAndVCT;
            rankConfig.rankBy = RankConfig.RankByType.average;
            rankConfig.rankTime = RankConfig.RankTimeType.month;
            rankConfig.initialized = true;
            Console.WriteLine($@"rankConfig initialized");
        }

        //TODO: Getting called too often?
        public void RecordGuildUserEnterVoiceChatTime()
        {
            lastTimeEnteredVC = DateTime.Now;
            //Console.WriteLine($"User entered chat at {lastTimeEnteredVC.ToString()}");
        }

        public void RecordGuildUserLeaveVoiceChatTime()
        {
            lastTimeLeftVC = DateTime.Now;
            if (totalVCTime == null)
            {
                totalVCTime = new TimeSpan();
            }
            totalVCTime += lastTimeLeftVC - lastTimeEnteredVC;

            userStatsDays[GetIndexOfDay(DateTime.Now)].vCTime += lastTimeLeftVC - lastTimeEnteredVC;
            //Console.WriteLine($"User left all chats at {lastTimeLeftVC.ToString()}");
        }

        public Task RecordThisUserSentAMessage(SocketMessage message)
        {
            //Console.WriteLine($@"Recorded {myGuildUser} sent a Message");

            totalMessagesSent++;

            userStatsDays[GetIndexOfDay(DateTime.Now)].messagesSent++;

            //PrintStatDaysArray();

            //PrintData();

            return Task.CompletedTask;
        }

        private int GetIndexOfDay(DateTime dateToChange)
        {
            //Future proofing XD
            if(dateToChange.Date > DateTime.Now)
            {
                Console.WriteLine("You should not be searching for stat info on a day that has yet to occur...");
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
                    //Console.WriteLine($@"Found today in userStatDays at index {dayToModIndex}");
                    day = -1; //end loop
                }
            }

            //if todays day not in list...
            if (!dayInArray)
            {
                //Console.WriteLine($@"Shifting day array");

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

        public TimeSpan GetAverageChatTime(int pastNumberOfDays)
        {
            if (pastNumberOfDays > userStatsDays.Length)
            {
                Console.WriteLine($@"Beware: UserStat only records the past {userStatsDays.Length} days.");
                pastNumberOfDays = userStatsDays.Length;
            }
            //if # of days searching for exceeds # of days since this stat tracker got made, instead return average since stat tracker created
            else if ((pastNumberOfDays > (DateTime.Now - dateTrackerCreated).Days))
            {
                return GetTotalAverageChatTime();
            }

            return GetTotalChatTime(pastNumberOfDays) / pastNumberOfDays;
        }
        public float GetAverageMessages(int pastNumberOfDays)
        {
            if (pastNumberOfDays > userStatsDays.Length)
            {
                Console.WriteLine($@"Beware: UserStat only records the past {userStatsDays.Length} days.");
                pastNumberOfDays = userStatsDays.Length;
            }
            //if # of days searching for exceeds # of days since this stat tracker got made, instead return average since stat tracker created
            else if ((pastNumberOfDays > (DateTime.Now - dateTrackerCreated).Days))
            {
                return GetTotalAverageMessages();
            }

            return GetTotalMessages(pastNumberOfDays) / (float)pastNumberOfDays;
        }
        public TimeSpan GetTotalChatTime(int pastNumberOfDays)
        {
            if (pastNumberOfDays > userStatsDays.Length)
            {
                Console.WriteLine($@"Beware: UserStat only records the past {userStatsDays.Length} days.");
                pastNumberOfDays = userStatsDays.Length;
            }
            //if # of days searching for exceeds # of days since this stat tracker got made, instead return average since stat tracker created
            else if ((pastNumberOfDays > (DateTime.Now - dateTrackerCreated).Days))
            {
                return totalVCTime;
            }
            else if(pastNumberOfDays < 1)
            {
                //get total chattime instead
                return totalVCTime;
            }

            TimeSpan totalCT = TimeSpan.Zero;

            int todaysIndex = GetIndexOfDay(DateTime.Now);
            for (int dayIndex = todaysIndex; dayIndex >= 0 && dayIndex > (todaysIndex - pastNumberOfDays); dayIndex--)
            {
                totalCT += userStatsDays[dayIndex].vCTime;
            }

            return totalCT;
        }
        public int GetTotalMessages(int pastNumberOfDays)
        {
            if (pastNumberOfDays > userStatsDays.Length)
            {
                Console.WriteLine($@"Beware: UserStat only records the past {userStatsDays.Length} days.");
                pastNumberOfDays = userStatsDays.Length;
            }
            //if # of days searching for exceeds # of days since this stat tracker got made, instead return average since stat tracker created
            else if ((pastNumberOfDays > (DateTime.Now - dateTrackerCreated).Days))
            {
                return totalMessagesSent;
            }
            else if (pastNumberOfDays < 1)
            {
                //get total chattime instead
                return totalMessagesSent;
            }

            int totalMessages = 0;

            int todaysIndex = GetIndexOfDay(DateTime.Now);
            for (int dayIndex = todaysIndex; dayIndex >= 0 && dayIndex > (todaysIndex - pastNumberOfDays); dayIndex--)
            {
                totalMessages += userStatsDays[dayIndex].messagesSent;
            }

            return totalMessages;
        }
        public TimeSpan GetTotalAverageChatTime()
        {
            int daysSinceTrackerCreated = (DateTime.Now - dateTrackerCreated).Days;
            if (daysSinceTrackerCreated < 1)
            {
                daysSinceTrackerCreated = 1;
            }
            return totalVCTime / (float)daysSinceTrackerCreated;
        }
        public float GetTotalAverageMessages()
        {
            int daysSinceTrackerCreated = (DateTime.Now - dateTrackerCreated).Days;
            if (daysSinceTrackerCreated < 1)
            {
                daysSinceTrackerCreated = 1;
            }
            return totalMessagesSent / (float)daysSinceTrackerCreated;
        }
        #endregion

        //TODO: way to make these one function?
        public static void ChangeRankCriteria(RankConfig.RankType newRankType)
        {
            rankConfig.rankType = newRankType;
            //gets saved b/c AssignRoles() called
        }
        public static void ChangeRankCriteria(RankConfig.RankByType newRankByType)
        {
            rankConfig.rankBy = newRankByType;
            //gets saved b/c AssignRoles() called
        }
        public static void ChangeRankCriteria(RankConfig.RankTimeType newRankTimeType)
        {
            rankConfig.rankTime = newRankTimeType;
            //gets saved b/c AssignRoles() called
        }


        //INTERFACE IMPLEMENTATION
        //---------------------------------------------------------------------

        //default compares both messages and chat by month
        public int CompareTo(UserStatTracker other)
        {
            //rank by average messages
            if (rankConfig.rankType.Equals(RankConfig.RankType.messages) && rankConfig.rankBy.Equals(RankConfig.RankByType.average))
            {
                if(this.GetAverageMessages((int)rankConfig.rankTime) > other.GetAverageMessages((int)rankConfig.rankTime))
                {
                    //higher rank
                    return -1;
                }
                else if(this.GetAverageMessages((int)rankConfig.rankTime) < other.GetAverageMessages((int)rankConfig.rankTime))
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
            else if (rankConfig.rankType.Equals(RankConfig.RankType.voiceChatTime) && rankConfig.rankBy.Equals(RankConfig.RankByType.average))
            {
                if (this.GetAverageChatTime((int)rankConfig.rankTime) > other.GetAverageChatTime((int)rankConfig.rankTime))
                {
                    return -1;
                }
                else if (this.GetAverageChatTime((int)rankConfig.rankTime) < other.GetAverageChatTime((int)rankConfig.rankTime))
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
            //rank by total messages
            else if (rankConfig.rankType.Equals(RankConfig.RankType.messages) && rankConfig.rankBy.Equals(RankConfig.RankByType.total))
            {
                if (this.GetTotalMessages((int)rankConfig.rankTime) > other.GetTotalMessages((int)rankConfig.rankTime))
                {
                    return -1;
                }
                else if (this.GetTotalMessages((int)rankConfig.rankTime) < other.GetTotalMessages((int)rankConfig.rankTime))
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
            //rank by total voice chat time
            else if (rankConfig.rankType.Equals(RankConfig.RankType.voiceChatTime) && rankConfig.rankBy.Equals(RankConfig.RankByType.total))
            {
                if (this.GetTotalChatTime((int)rankConfig.rankTime) > other.GetTotalChatTime((int)rankConfig.rankTime))
                {
                    return -1;
                }
                else if (this.GetTotalChatTime((int)rankConfig.rankTime) < other.GetTotalChatTime((int)rankConfig.rankTime))
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
            //rank by average both
            if (rankConfig.rankType.Equals(RankConfig.RankType.msgAndVCT))
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
        }
    }
}
