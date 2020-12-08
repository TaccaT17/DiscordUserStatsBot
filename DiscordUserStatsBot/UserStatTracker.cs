using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace DiscordUserStatsBot
{
    class UserStatTracker
    {
        //Will log stats up to the last 30 days

        public enum Past
        {
            day = 1,
            week = 7,
            month = 30,
            year = 365
        }

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
        [Newtonsoft.Json.JsonProperty]
        private DateTime dateTrackerCreated;

        //records users stats for each day for the past 30 days. Goes from OldestDay -> NewestDay
        [Newtonsoft.Json.JsonProperty]
        private UserStatDay[] userStatsDays;
        [Newtonsoft.Json.JsonProperty]
        //private UserStatDay dayRecordingStatsFor;
        
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
        public UserStatTracker(UserStatsBotController controllerRef, string userName)
        {
            myController = controllerRef;
            usersFullName = userName;

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

            AddFakeData();

        }

        #region FUNCTIONS
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------

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

            PrintStatDaysArray();

            PrintData();

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

        private TimeSpan GetAverageChatTime(int pastNumberOfDays)
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
        private float GetAverageMessages(int pastNumberOfDays)
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
        private TimeSpan GetTotalChatTime(int pastNumberOfDays)
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
        private int GetTotalMessages(int pastNumberOfDays)
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
        private TimeSpan GetTotalAverageChatTime()
        {
            int daysSinceTrackerCreated = (DateTime.Now - dateTrackerCreated).Days;
            if (daysSinceTrackerCreated < 1)
            {
                daysSinceTrackerCreated = 1;
            }
            return totalVCTime / (float)daysSinceTrackerCreated;
        }
        private float GetTotalAverageMessages()
        {
            int daysSinceTrackerCreated = (DateTime.Now - dateTrackerCreated).Days;
            if (daysSinceTrackerCreated < 1)
            {
                daysSinceTrackerCreated = 1;
            }
            return totalMessagesSent / (float)daysSinceTrackerCreated;
        }
        #endregion

        #region DEBBUGGING
        //---------------------------------------------------------------------
        public void PrintStatDaysArray()
        {
            Console.WriteLine("");

            foreach (UserStatDay day in userStatsDays)
            {
                Console.WriteLine($@"Date: {day.date.ToString()}");
                Console.WriteLine($@"Messages: {day.messagesSent}");
                Console.WriteLine($@"VCTime: {day.vCTime.ToString()}");
            }

            return;
        }

        public void PrintData()
        {
            Console.WriteLine("");
            Console.WriteLine($@"Total chat time this past day: {GetTotalChatTime(1)}");
            Console.WriteLine($@"Total chat time this past week: {GetTotalChatTime(7)}");
            Console.WriteLine($@"Total chat time this past month: {GetTotalChatTime(30)}");
            Console.WriteLine($@"Total chat time all time: {totalVCTime}");

            Console.WriteLine($@"Average chat time this past day: {GetAverageChatTime(1)}");
            Console.WriteLine($@"Average chat time this past week: {GetAverageChatTime(7)}");
            Console.WriteLine($@"Average chat time this past month: {GetAverageChatTime(30)}");
            Console.WriteLine($@"Average chat time all time: {GetTotalAverageChatTime()}");

            Console.WriteLine($@"Total messages this past day: {GetTotalMessages(1)}");
            Console.WriteLine($@"Total messages this past week: {GetTotalMessages(7)}");
            Console.WriteLine($@"Total messages this past month: {GetTotalMessages(30)}");
            Console.WriteLine($@"Total messages all time: {totalMessagesSent}");

            Console.WriteLine($@"Average messages this past day: {GetAverageMessages(1)}");
            Console.WriteLine($@"Average messages this past week: {GetAverageMessages(7)}");
            Console.WriteLine($@"Average messages this past month: {GetAverageMessages(30)}");
            Console.WriteLine($@"Average messages all time: {GetTotalAverageMessages()}");
        }

        public void AddFakeData()
        {
            userStatsDays[27].messagesSent = 14;
            totalMessagesSent += userStatsDays[27].messagesSent;
            userStatsDays[27].vCTime = new TimeSpan(9, 36, 8);
            totalVCTime += userStatsDays[27].vCTime;

            userStatsDays[15].messagesSent = 72;
            totalMessagesSent += userStatsDays[15].messagesSent;
            userStatsDays[15].vCTime = new TimeSpan(2, 49, 4);
            totalVCTime += userStatsDays[15].vCTime;

            userStatsDays[3].messagesSent = 25;
            totalMessagesSent += userStatsDays[3].messagesSent;
            userStatsDays[3].vCTime = new TimeSpan(5, 7, 2);
            totalVCTime += userStatsDays[3].vCTime;
        }

        //---------------------------------------------------------------------
        #endregion
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        #endregion

        //STRUCT
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
