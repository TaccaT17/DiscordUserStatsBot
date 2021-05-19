//Copyright Tom Crammond 2021

using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordUserStatsBot
{
    public class CommandHandler : BotComponent
    {
        #region VARIABLES
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------

        private char botCommandPrefix = '!';
        private string ignoreAfterCommandString = "IACSn0ll";
        public string IgnoreAfterCommandString { get { return ignoreAfterCommandString; } }
        public bool wasBotCommand; //stops messages that are commands for this bot from being recorded

        public char BotCommandPrefix
        {
            get { return botCommandPrefix;  }
            set { botCommandPrefix = value; }
        }

        //Commands
        public const string greetCommand = "Hi",
            aboutCommand = "About",
            helpCommand = "Help",
            prefixCommand = "Prefix",
            getUserStatsCommand = "UserStats",
            getRankStatsCommand = "RankStats",
            setRankTimeIntervalCommand = "SetRankTimeInterval",
            setRankMemberLimitCommand = "SetRankMemberLimit",
            changeRankCriteriaCommand = "RankBy",
            updateRanksCommand = "UpdateRanks", 
            botInfoCommand = "BotInfo",
            showRanksCommand = "ShowRanks";

        private int amountShowRanks = 10;
        public int AmountShowRanks { get { return amountShowRanks; } }

        public IEmote emoteClap = new Emoji("👏"), emoteDonate = new Emoji("🤗"), emoteSad = new Emoji("😢");
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        #endregion

        #region CommandFunctions
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        
        /// <summary>
        /// Intro message to get users started with the bot.
        /// </summary>
        /// <param name="guild"></param>
        /// <returns></returns>
        public Task IntroMessage(SocketGuild guild)
        {
            guild.DefaultChannel.SendMessageAsync($"Hi! Type '{BotCommandPrefix}{helpCommand}' for a list of bot commands.");

            return Task.CompletedTask;
        }

        public Task ChangePrefixCommand(SocketMessage message, string stringAfterCommand)
        {
            //only users who have manage guild permission can change the prefix

            //need to cast user to get var that tells me whether user can manage guild
            botCommandPrefix = stringAfterCommand[0];
            if (stringAfterCommand.Length > 1)
            {
                message.Channel.SendMessageAsync($@"The command prefix for UserStat bot has been set to the first character typed {botCommandPrefix}");
            }
            else
            {
                message.AddReactionAsync(emoteClap);
            }
            return Task.CompletedTask;
        }

        //returns ignoreAfterCommandString if nothing after commmand
        public Task<string> GetStringAfterCommand(SocketMessage message, int lengthOfCommand)
        {
            string stringAfterCommand = ignoreAfterCommandString;

            //if there is more content after the ' ' then set stringAfterCommand to that
            //lengthOfCommand includes prefix character, +1 Accounts for a " " after command
            if (message.Content.Length > lengthOfCommand + 1)
            {
                stringAfterCommand = message.Content.Substring(lengthOfCommand, message.Content.Length - (lengthOfCommand));
            }

            return Task<string>.FromResult(stringAfterCommand.Trim());
        }

        public Task<string> ReformatStringToUsername(string unformattedUsernameString)
        {
            string fullUserName = null;

            if (unformattedUsernameString.Contains('#'))
            {
                //get username string from stringAfterCommand
                int hashtagIndex = unformattedUsernameString.IndexOf('#');
                string usernameMinusDiscrim = unformattedUsernameString.Substring(0, hashtagIndex).Trim();
                string userDiscriminator = unformattedUsernameString.Substring(hashtagIndex + 1, unformattedUsernameString.Length - (hashtagIndex + 1)).Trim();

                //to get rid of any space between username and discriminator
                fullUserName = usernameMinusDiscrim + '#' + userDiscriminator;
            }
            else
            {
                fullUserName = unformattedUsernameString.Trim();
                //Console.WriteLine("Invalid string. Cannot convert to a username");
            }
            return Task<string>.FromResult(fullUserName);
        }

        public float Average(List<float> list)
        {
            float average = default;

            if (list.Count == 0)
            {
                return average;
            }

            foreach (float item in list)
            {
                average = average + item;
            }

            average = average / (float)list.Count;

            return average;
        }

        public TimeSpan Average(List<TimeSpan> list)
        {
            TimeSpan average = default;

            if (list.Count == 0)
            {
                return average;
            }

            foreach (TimeSpan item in list)
            {
                average = average + item;
            }

            average = average / (float)list.Count;

            return average;
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        #endregion


    }
}
