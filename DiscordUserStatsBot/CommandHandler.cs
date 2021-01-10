using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordUserStatsBot
{
    class CommandHandler
    {
        private UserStatsBotController myCont;
        private SocketGuild guildRef;

        private char botCommandPrefix = '!';
        private string ignoreAfterCommandString = "IACSn0ll";

        //Commands
        private string greetCommand = "Hi",
            aboutCommand = "About",
            helpCommand = "Help",
            prefixCommand = "Prefix",
            totalChatTimeCommand = "TotalChatTime",
            totalMessagesCommand = "TotalMessages",
            setRankTimeIntervalCommand = "SetRankTimeInterval",
            setRankMemberLimitCommand = "SetRankMemberLimit",
            rankMemberLimitCommand = "RankMemberLimit",
            changeRankCriteria = "RankBy",
            getUserRankCommand = "UserRank",
            updateRanksCommand = "UpdateRanks", 
            botInfoCommand = "BotInfo";

        //bool to stop commands for this bot from being recorded in UserStat
        public bool wasBotCommand;

        //CONSTRUCTOR
        public CommandHandler(UserStatsBotController myController, SocketGuild guildReference)
        {
            myCont = myController;
            guildRef = guildReference;
        }



        #region CommandFunctions
        public Task CommandHandlerFunc(SocketMessage message)          //REMEMBER ALL COMMANDS MUST BE LOWERCASE
        {
            #region MessageFilter
            //--------------------------------------------------------------------------------------------------
            //rule out messages that don't have bot prefix
            if (!message.Content.StartsWith(botCommandPrefix))
            {
                //Console.WriteLine("Message is not a bot command");
                return Task.CompletedTask;
            }

            //rule out messages that bots (including itself) create
            else if (message.Author.IsBot)
            {
                //Console.WriteLine("Message is from a bot");
                return Task.CompletedTask;
            }
            //--------------------------------------------------------------------------------------------------
            #endregion

            #region GetMessageCommandString
            //--------------------------------------------------------------------------------------------------
            string command = "";
            int lengthOfCommand = -1;

            //Only will take first word of command
            if (message.Content.Contains(' '))
            {
                //includes '!' in command length
                lengthOfCommand = message.Content.IndexOf(' ');
            }
            else
            {
                lengthOfCommand = message.Content.Length;
            }

            //                        Substring: you take specified section of string recieved. ToLower: makes string lowercase
            command = message.Content.Substring(1, lengthOfCommand - 1).ToLower();

            //--------------------------------------------------------------------------------------------------
            #endregion

            #region COMMANDS
            //REMEMBER: NO SPACES ALLOWED IN COMMANDS
            //--------------------------------------------------------------------------------------------------

            //COMMANDS
            //------------------------------------------
            //HI
            if (command.Equals(greetCommand.ToLower()))
            {
                wasBotCommand = true;
                message.Channel.SendMessageAsync("Hello fellow user.");
                message.Channel.SendMessageAsync("Whalecome...");
                return Task.CompletedTask;
            }

            if (command.Equals(aboutCommand.ToLower()))
            {
                wasBotCommand = true;
                message.Channel.SendMessageAsync($@"The goal of the StatTracker bot is to create a more organized sidebar so active users appear near the top." +
                                                 $@"The bot does this by doing two things: " + 
                                                 $@"1. It records active users' guild (AKA server) activity over the past month (their time in chat + messages sent) " + 
                                                 $@"2. It assigns users a rank and corresponding role based off of their activity." +
                                                 $@"You have the option to turn off the sidebar organization if you just want a bit of fun comparing stats with you're friends");
                return Task.CompletedTask;
            }

            else if (command.Equals(helpCommand.ToLower()))
            {
                message.Channel.SendMessageAsync("Commands are: \n" +
                    greetCommand + " - the bot greets you\n" +
                    prefixCommand + " (<newPrefix>) - get the botPrefix or (optionally) set it\n" +
                    totalChatTimeCommand + " <username#0000> - get a given users total recorded guild time chat\n" +
                    totalMessagesCommand + " <username#0000> - get a given users total recorded guild messages sent\n" +
                    updateRanksCommand + " - recalculates everyones rank\n" +
                    setRankTimeIntervalCommand + " <hours> - change time interval between when users ranks are calculated. This command resets the timer. \n" +
                    changeRankCriteria + " <Msg, Vc, or Msg&Vc> - set whether people are ranked by messages, voice chat or both. \n" +
                    getUserRankCommand + " <username#0000> - get a given users rank\n" +
                    setRankMemberLimitCommand + " <RankRole>, <Amount> - changes the number of users in a RankRole to the given Amount\n" +
                    botInfoCommand + " gives relevant bot information" +
                    //TODO:
                    //set number of users per given role
                    "");

                return Task.CompletedTask;
            }

            else if (command.Equals(botInfoCommand.ToLower()))
            {
                string roleNames = "";
                for (int index = 0; index < myCont.userStatRolesRef.rankRoles.Length; index++)
                {
                    roleNames += $"\n       {myCont.userStatRolesRef.rankRoles[index].name} : \n                Member limit = {myCont.userStatRolesRef.rankRoles[index].memberLimit}";
                }

                string rankBy = "";
                if (UserStatTracker.rankConfig.rankType.Equals(UserStatTracker.RankConfig.RankType.messages))
                {
                    rankBy = "Messages";
                }
                else if (UserStatTracker.rankConfig.rankType.Equals(UserStatTracker.RankConfig.RankType.voiceChatTime))
                {
                    rankBy = "Voice Chat Time";
                }
                else if (UserStatTracker.rankConfig.rankType.Equals(UserStatTracker.RankConfig.RankType.msgAndVCT))
                {
                    rankBy = "Messages and Voice Chat time";
                }

                //returns bot info
                message.Channel.SendMessageAsync($"Bot info: \n" +
                    $"Assign ranks time interval: {myCont.GetAssignRolesInterval().ToString(@"dd\.hh\:mm\:ss")} \n" +
                    $"What time ranks will be recalculated: {(myCont.GetAssignRolesTimerStart() + myCont.GetAssignRolesInterval()).ToString()} \n" +
                    $"Users ranked by: {rankBy}\n" + 
                    $"Rank roles: {roleNames}");

                return Task.CompletedTask;
            }

            //------------------------------------------

            //COMMANDS INVOLVING AFTER-COMMAND STRING HERE
            //------------------------------------------
            string stringAfterCommand = GetStringAfterCommand(message, lengthOfCommand).Result;
            //PREFIX
            if (command.Equals(prefixCommand.ToLower()))
            {
                wasBotCommand = true;

                Console.WriteLine("Prefix command called");
                //if nothing after prefix then print out prefix otherwise set the prefix to 1st character after space
                if (stringAfterCommand.Equals(ignoreAfterCommandString))
                {
                    message.Channel.SendMessageAsync($@"The command prefix for UserStat bot is {botCommandPrefix}");
                    Console.WriteLine("Told user prefix");
                }
                else
                {
                    ChangePrefixCommand(message, stringAfterCommand);
                    Console.WriteLine("Set new prefix");
                }

                return Task.CompletedTask;
            }
            else if (command.Equals(setRankTimeIntervalCommand.ToLower()))
            {
                wasBotCommand = true;

                float hours;
                if (!(float.TryParse(stringAfterCommand, out hours)))       //TryParse returns false if not an int
                {
                    message.Channel.SendMessageAsync($@"Sorry, but '{stringAfterCommand}' is not a number.");
                    return Task.CompletedTask;
                }
                else
                {
                    TimeSpan tP = TimeSpan.FromHours(hours);
                    myCont.ChangeAssignRolesInterval(tP);
                }

                return Task.CompletedTask;
            }
            else if (command.Equals(changeRankCriteria.ToLower()))
            {
                wasBotCommand = true;

                if (stringAfterCommand.ToLower().Equals("Msg".ToLower()))
                {
                    UserStatTracker.ChangeRankType(UserStatTracker.RankConfig.RankType.messages);
                }
                else if (stringAfterCommand.ToLower().Equals("Vc".ToLower()))
                {
                    UserStatTracker.ChangeRankType(UserStatTracker.RankConfig.RankType.voiceChatTime);
                }
                else if (stringAfterCommand.ToLower().Equals("Msg&Vc".ToLower()))
                {
                    UserStatTracker.ChangeRankType(UserStatTracker.RankConfig.RankType.msgAndVCT);
                }
                else
                {
                    message.Channel.SendMessageAsync($@"Sorry, that was an invalid command. Not sure which one of us is the idiot.");
                }
                //update ranks to reflect changed rank criteria
                myCont.userStatRolesRef.AssignRoles(guildRef);

                return Task.CompletedTask;
            }
            else if (command.Equals(updateRanksCommand.ToLower()))
            {
                wasBotCommand = true;

                myCont.userStatRolesRef.AssignRoles(guildRef);

            }
            else if (command.Equals(rankMemberLimitCommand.ToLower()))
            {
                wasBotCommand = true;

                for (int index = 0; index < myCont.userStatRolesRef.rankRoles.Length; index++)
                {
                    if (myCont.userStatRolesRef.rankRoles[index].name.ToLower().Equals(stringAfterCommand.ToLower()))
                    {
                        message.Channel.SendMessageAsync($@"{myCont.userStatRolesRef.rankRoles[index].name} memberLimit is {myCont.userStatRolesRef.rankRoles[index].memberLimit}.");
                    }
                }

            }
            else if (command.Equals(getUserRankCommand.ToLower()))
            {
                wasBotCommand = true;

                string userName = ReformatStringToUsername(stringAfterCommand).Result;

                if (userName == null)
                {
                    message.Channel.SendMessageAsync($@"Sorry that isn't a valid username");
                    return Task.CompletedTask;
                }
                else
                {
                    UserStatTracker tempUserStat = myCont.GetUserStats(userName);

                    if (tempUserStat == null)
                    {
                        message.Channel.SendMessageAsync($@"Sorry there is no data on {userName}.");
                        //This could possibly be a logic error: one of the two dictionaries is lacking an entry for this user

                        return Task.CompletedTask;
                    }

                    //get rank
                    int rank = myCont.userStatRolesRef.GetUsersRank(myCont.GetUserIDFromName(userName));
                    message.Channel.SendMessageAsync($@"{userName} is ranked {rank + 1}th");

                }

            }
            else if (command.Equals(totalChatTimeCommand.ToLower()))
            {
                wasBotCommand = true;

                string userName = ReformatStringToUsername(stringAfterCommand).Result;

                //if not valid string
                if (userName == null)
                {
                    message.Channel.SendMessageAsync($@"Sorry that isn't a valid username.");
                    return Task.CompletedTask;
                }
                else
                {
                    UserStatTracker tempUserStat = myCont.GetUserStats(userName);

                    if (tempUserStat == null)
                    {
                        message.Channel.SendMessageAsync($@"Sorry there is no data on {userName}.");
                        //This could possibly be a logic error: one of the two dictionaries is lacking an entry for this user

                        return Task.CompletedTask;
                    }

                    if (myCont.GetUserIDFromName(userName) != 0 && (guildRef.GetUser(myCont.GetUserIDFromName(userName)) != null))
                    {
                        SocketGuildUser guildUser = guildRef.GetUser(myCont.GetUserIDFromName(userName));

                        //if user in a chat update their time before sending message, otherwise just send message
                        if (myCont.UserIsInChat(guildUser))
                        {
                            myCont.StopRecordingVCTime(guildUser);
                            message.Channel.SendMessageAsync($@"{tempUserStat.UsersFullName}'s total chat time is " +
                                                             $@"{tempUserStat.TotalVoiceChatTime.Days} days, " +
                                                             $@"{tempUserStat.TotalVoiceChatTime.Hours} hours, " +
                                                             $@"{tempUserStat.TotalVoiceChatTime.Minutes} minutes and " +
                                                             $@"{tempUserStat.TotalVoiceChatTime.Seconds} seconds!");
                            myCont.StartRecordingVCTime(guildUser);
                        }
                        else
                        {
                            message.Channel.SendMessageAsync($@"{tempUserStat.UsersFullName}'s total chat time is " +
                                                             $@"{tempUserStat.TotalVoiceChatTime.Days} days, " +
                                                             $@"{tempUserStat.TotalVoiceChatTime.Hours} hours, " +
                                                             $@"{tempUserStat.TotalVoiceChatTime.Minutes} minutes and " +
                                                             $@"{tempUserStat.TotalVoiceChatTime.Seconds} seconds!");
                        }
                    }
                }
            }
            else if (command.Equals(totalMessagesCommand.ToLower()))
            {
                wasBotCommand = true;

                string userName = ReformatStringToUsername(stringAfterCommand).Result;

                //if not valid string
                if (userName == null)
                {
                    message.Channel.SendMessageAsync($@"Sorry that isn't a valid username.");
                    return Task.CompletedTask;
                }
                else
                {
                    UserStatTracker tempUserStat = myCont.GetUserStats(userName);

                    if (tempUserStat == null)
                    {
                        message.Channel.SendMessageAsync($@"Sorry there is no data on {userName}.");
                        //This could possibly be a logic error: one of the two dictionaries is lacking an entry for this user

                        return Task.CompletedTask;
                    }

                    message.Channel.SendMessageAsync($@"{tempUserStat.UsersFullName} has sent {tempUserStat.TotalMessagesSent} meaningful messages!");
                }
            }
            else if (command.Equals(setRankMemberLimitCommand.ToLower()))
            {
                wasBotCommand = true;

                if (!(stringAfterCommand.Contains(',')))
                {
                    message.Channel.SendMessageAsync($@"Invalid input. Use a comma.");
                    return Task.CompletedTask;
                }

                //Parse role rank and integer inputs
                int comma = stringAfterCommand.IndexOf(',');
                int lengthAfterComma = stringAfterCommand.Length - (comma + 1);

                string roleName = stringAfterCommand.Substring(0, comma);
                roleName.Trim();
                string newMemberLimitString = stringAfterCommand.Substring(comma + 1, lengthAfterComma);
                newMemberLimitString.Trim();

                int newMemberLimit = -1;

                if(!(Int32.TryParse(newMemberLimitString, out newMemberLimit)))
                {
                    Console.WriteLine($@"Failed to convert amount string into integer.");
                    return Task.CompletedTask;
                }

                //if rolename is same as in rankRoles list...
                for (int index = 0; index < myCont.userStatRolesRef.rankRoles.Length; index++)
                {
                    if (myCont.userStatRolesRef.rankRoles[index].name.ToLower().Equals(roleName.ToLower()))
                    {
                        //...set new amount 
                        myCont.userStatRolesRef.rankRoles[index].memberLimit = newMemberLimit;
                        Console.WriteLine($@"{myCont.userStatRolesRef.rankRoles[index].name} memberLimit changed to {newMemberLimit}.");
                    }
                }

                //update roles
                myCont.userStatRolesRef.AssignRoles(guildRef);
                //save roles
                myCont.userStatRolesRef.SaveRoles(guildRef, myCont.saveHandlerRef);

            }
            //------------------------------------------
            //--------------------------------------------------------------------------------------------------
            #endregion

            return Task.CompletedTask;
        }

        private Task ChangePrefixCommand(SocketMessage message, string stringAfterCommand)
        {
            //only users who have manage guild permission can change the prefix

            //need to cast user to get var that tells me whether user can manage guild
            SocketGuildUser userGuild = (SocketGuildUser)(message.Author);

            if (userGuild.GuildPermissions.ManageGuild)
            {
                botCommandPrefix = stringAfterCommand[0];
                if (stringAfterCommand.Length > 1)
                {
                    message.Channel.SendMessageAsync($@"The command prefix for UserStat bot has been set to the first character typed {botCommandPrefix}");
                }
                else
                {
                    message.Channel.SendMessageAsync($@"The command prefix for UserStat bot has been set to {botCommandPrefix}");
                }

            }
            else
            {
                message.Channel.SendMessageAsync($@"Sorry, you need the Manage Guild permission in order to do this");
            }

            return Task.CompletedTask;
        }

        //returns ignoreAfterCommandString if nothing after commmand
        private Task<string> GetStringAfterCommand(SocketMessage message, int lengthOfCommand)
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

        private Task<string> ReformatStringToUsername(string unformattedUsernameString)
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

        #endregion


    }
}
