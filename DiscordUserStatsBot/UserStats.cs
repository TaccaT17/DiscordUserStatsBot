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
        //Will log stats up to the last 30 days

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
        private DateTime dateUserStatCreated;

        //records users stats for each day for the past 30 days. Goes from OldestDay -> NewestDay
        [Newtonsoft.Json.JsonProperty]
        private UserStatDay[] userStatsDays;
        [Newtonsoft.Json.JsonProperty]
        //private UserStatDay dayRecordingStatsFor;

        ///make an array that has up to 30 entries (days)
        ///those entries contain
        ///1 how many messages this user sent that day 
        ///2 how long this user was in voice chat that day
        ///3 what day of the week it was

        ///Averages:
        ///week chattime
        ///week messages
        ///day chattime
        ///day messages
        ///specific day of the week chattime
        ///specific day of the week messages
        ///Average chattime last 30 days
        ///Average messages last 30 days

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

            userStatsDays = new UserStatDay[30];
            //give each day a date
            DateTime dTSetUp = DateTime.Now;
            dTSetUp = dTSetUp.Date;
            Console.WriteLine($@"dtSetup is {dTSetUp}");
            for (int day = 0; day < userStatsDays.Length; day++)
            {
                userStatsDays[day] = new UserStatDay(dTSetUp);

                Console.WriteLine($@"Date for day {day} is {userStatsDays[day].date.ToString()}");

                dTSetUp = dTSetUp.AddDays(1);
            }

            dateUserStatCreated = DateTime.Now;
        }

        #region FUNCTIONS
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void CalculateAndUpdateUserVCTime()
        {
            if(totalVCTime == null)
            {
                totalVCTime = new TimeSpan();
            }
            totalVCTime += lastTimeLeftVC - lastTimeEnteredVC;

            userStatsDays[GetIndexOfToday()].vCTime += lastTimeLeftVC - lastTimeEnteredVC;

            PrintStatDaysArray();
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
            CalculateAndUpdateUserVCTime();
            //Console.WriteLine($"User left all chats at {lastTimeLeftVC.ToString()}");
        }

        public Task RecordThisUserSentAMessage(SocketMessage message)
        {
            //Console.WriteLine($@"Recorded {myGuildUser} sent a Message");

            totalMessagesSent++;

            userStatsDays[GetIndexOfToday()].messagesSent++;

            PrintStatDaysArray();

            return Task.CompletedTask;
        }

        //TODO: make this more efficient by starting from back of array?
        private int GetIndexOfToday()
        {
            DateTime today = DateTime.Now.Date;
            //today = today.AddDays(40);
            //Console.WriteLine($@"40 days from now is {today}");


            //find matching date to today
            bool dayInArray = false;
            //used to get index of UserStatDay that is being modified
            int dayToModIndex = 0;

            //find todays day in list
            for (int day = 0; day < userStatsDays.Length; day++)
            {
                if (userStatsDays[day].date.Date.Equals(today.Date))
                {
                    dayInArray = true;
                    dayToModIndex = day;
                    Console.WriteLine($@"Found today in userStatDays at index {dayToModIndex}");
                    //end loop
                    day = userStatsDays.Length;
                }
            }

            //if todays day not in list...
            if (!dayInArray)
            {
                Console.WriteLine($@"Shifting day array");

                //get difference of days between today and last entry in list
                TimeSpan tS = today - userStatsDays[userStatsDays.Length - 1].date;
                int daysDifferenceLastEntryAndToday = tS.Days;

                //
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

        //FOR DEBBUGGING
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
        
        public Task UpdateUsersName(SocketGuildUser correspondingGuildUser)
        {
            usersFullName = correspondingGuildUser.Username + '#' + correspondingGuildUser.Discriminator;

            return Task.CompletedTask;
        }

        private void AddMessageToDayEntry()
        {
            for (int day = 0; day < userStatsDays.Length; day++)
            {
                //if found same day update that entry

                //otherwise delete oldest dates (earlier index entries) scoot everything over the approppriate amount of days and create new dates appropriately

            }
        }

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
