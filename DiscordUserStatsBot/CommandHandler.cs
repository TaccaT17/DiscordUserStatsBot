using Discord;
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

        /// <summary>
        /// Prefix used to call bot commands.
        /// </summary>
        private char botCommandPrefix = '!';
        private string ignoreAfterCommandString = "IACSn0ll";

        public char BotCommandPrefix
        {
            get { return botCommandPrefix;  }
            set
            {
                botCommandPrefix = value;
            }
        }

        //Commands
        private string greetCommand = "Hi",
            aboutCommand = "About",
            helpCommand = "Help",
            prefixCommand = "StatPrefix",
            getUserStatsCommand = "UserInfo",
            setRankTimeIntervalCommand = "SetRankTimeInterval",
            setRankMemberLimitCommand = "SetRankMemberLimit",
            changeRankCriteria = "RankBy",
            updateRanksCommand = "UpdateRanks", 
            botInfoCommand = "BotInfo";

        //bool to stop commands for this bot from being recorded in UserStat
        public bool wasBotCommand;

        IEmote emoteClap;

        //CONSTRUCTOR
        public CommandHandler(UserStatsBotController myController, SocketGuild guildReference)
        {
            myCont = myController;
            guildRef = guildReference;

            emoteClap = new Emoji("👏");
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

            //ignore if this message was not sent in this guild
            if (((SocketGuildChannel)(message.Channel)).Guild.Id.Equals(guildRef.Id))
            {
                Console.WriteLine("Command sent in this guild: " + ((SocketGuildChannel)(message.Channel)).Guild.Name);
            }
            else
            {
                Console.WriteLine("Command sent in other guild: " + ((SocketGuildChannel)(message.Channel)).Guild.Name);
                return Task.CompletedTask;
            }
            //--------------------------------------------------------------------------------------------------
            #endregion

            wasBotCommand = true;

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
                message.Channel.SendMessageAsync("Hello fellow user.");
                message.Channel.SendMessageAsync("Whalecome...");
                message.Channel.SendMessageAsync($"Type '{botCommandPrefix}help' for a list of bot commands.");
                return Task.CompletedTask;
            }
            else if (command.Equals(aboutCommand.ToLower()))
            {
                EmbedBuilder builder = new EmbedBuilder();
                builder.WithTitle("About User Stats Bot");

                builder.AddField("The goal of the StatTracker bot is to create a more organized sidebar so active users appear near the top. \n\n",
                    "**The bot does this by doing two things:** \n    **1.** It records active users' guild (AKA server) activity over the past month (their time in chat + messages sent). \n" +
                                                 "    **2.** It assigns users a rank and corresponding role based off of their activity.\n\n" +
                                                 "*You have the option to turn off the sidebar organization if you just want a bit of fun comparing stats with your friends.*");
                builder.WithFooter(footer => footer.Text = $"Type '{botCommandPrefix}{helpCommand}' for a list of bot commands.");

                message.Channel.SendMessageAsync("", false, builder.Build());

                /*
                message.Channel.SendMessageAsync("The goal of the StatTracker bot is to create a more organized sidebar so active users appear near the top. \n" +
                                                 "The bot does this by doing two things: \n" + 
                                                 "    **1.** It records active users' guild (AKA server) activity over the past month (their time in chat + messages sent). \n" + 
                                                 "    **2.** It assigns users a rank and corresponding role based off of their activity.\n" +
                                                 "You have the option to turn off the sidebar organization if you just want a bit of fun comparing stats with your friends.\n" +
                                                 $"Type '{botCommandPrefix}{helpCommand}' for a list of bot commands.");
                */
                return Task.CompletedTask;
            }
            else if (command.Equals(helpCommand.ToLower()))
            {
                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle("User Stats Bot Commands: ");
                //builder.WithFooter(footer => footer.Text = $"current botPrefix is '{botCommandPrefix}'");
                builder.AddField(botCommandPrefix + greetCommand, "The bot greets you.");
                builder.AddField(botCommandPrefix + aboutCommand, "About this bot");
                builder.AddField(botCommandPrefix + helpCommand, "Provides a list of bot commands.");
                if (((SocketGuildUser)(message.Author)).GuildPermissions.Administrator)
                {
                    builder.AddField(botCommandPrefix + prefixCommand + " *(<newPrefix>)*", @"Get the botPrefix OR (optional) change it. *Admin only*");
                }
                else
                {
                    builder.AddField(botCommandPrefix + prefixCommand, @"Get the botPrefix.");
                }
                builder.AddField(botCommandPrefix + botInfoCommand, @"Gives relevant bot configuration information.");
                builder.AddField(botCommandPrefix + getUserStatsCommand + " *<username(#0000)>*", "Get a given user's rank and stats.");
                builder.AddField(botCommandPrefix + updateRanksCommand, "Recalculates everyones rank.");
                if (((SocketGuildUser)(message.Author)).GuildPermissions.Administrator)
                {
                    builder.AddField(botCommandPrefix + setRankTimeIntervalCommand + " *<hours>* ", "Change time interval between when users ranks are calculated. *By default is 24 hours*. This command resets the timer. *Admin only*");
                    builder.AddField(botCommandPrefix + setRankMemberLimitCommand + " *<RankRole>, <Amount>* ", "changes the number of users in a RankRole to the given Amount. Use '0' to disable the role. *Admin only*");
                    builder.AddField(botCommandPrefix + changeRankCriteria + " *<Criteria>*", "Sets what criteria people are ranked by. *Admin only*\n" +
                        "                     Criteria can be: messages(*Msg*), voice chat(*Vc*) or both(*Msg&Vc*). average (*Avg*) or totals(*Total*). month(*Month*), week(*Week*), or day(*Day*).\n");
                }

                message.Channel.SendMessageAsync("", false, builder.Build());
                /*
                message.Channel.SendMessageAsync("**Commands**:\n" +
                    $"*(current botPrefix is '{botCommandPrefix}')*\n" +
                    "--------------------------------------------------------\n" +
                    "- **" + greetCommand + "**" + " : the bot greets you.\n" +
                    "- **" + aboutCommand + "**" + " : about this bot.\n" +
                    "- **" + helpCommand + "**" + " : a list of bot commands.\n" +
                    "- **" + prefixCommand + "**" + " *(<newPrefix>)* : get the botPrefix OR (optional) change it. *Admin only*\n" +
                    "--------------------------------------------------------\n" +
                    "- **" + botInfoCommand + "**" + " : gives relevant bot configuration information.\n" +
                    "- **" + getUserStatsCommand + "**" + " *<username(#0000)>* : get a given user's rank and  stats.\n" +
                    "--------------------------------------------------------\n" +
                    "- **" + updateRanksCommand + "**" + " : recalculates everyones rank.\n" +
                    "- **" + setRankTimeIntervalCommand + "**" + " *<hours>* : change time interval between when users ranks are calculated. *By default is 1 hour*. This command resets the timer. *Admin only*\n" +
                    "- **" + setRankMemberLimitCommand + "**" + " *<RankRole>, <Amount>* : changes the number of users in a RankRole to the given Amount. Use '0' to disable the role. *Admin only*\n" +
                    "- **" + changeRankCriteria + "**" + " *<criteria>* : sets what criteria people are ranked by. *Admin only*\n" +
                    "                     Criteria can be: messages(*Msg*), voice chat(*Vc*) or both(*Msg&Vc*). average (*Avg*) or totals(*Total*). month(*Month*), week(*Week*), or day(*Day*).\n" +
                    "");
                */
                return Task.CompletedTask;
            }
            else if (command.Equals(botInfoCommand.ToLower()))
            {
                string roleNames = "";
                for (int index = 0; index < myCont.userStatRolesRef.rankRoles.Length; index++)
                {
                    roleNames += $"\n*Tier {index + 1}* : **__{myCont.userStatRolesRef.rankRoles[index].name}__** : \n                       - Member limit = **{myCont.userStatRolesRef.rankRoles[index].memberLimit}**";
                }

                string rankType = "";
                if (UserStatTracker.rankConfig.rankType.Equals(UserStatTracker.RankConfig.RankType.messages))
                {
                    rankType = "Messages";
                }
                else if (UserStatTracker.rankConfig.rankType.Equals(UserStatTracker.RankConfig.RankType.voiceChatTime))
                {
                    rankType = "Voice Chat Time";
                }
                else if (UserStatTracker.rankConfig.rankType.Equals(UserStatTracker.RankConfig.RankType.msgAndVCT))
                {
                    rankType = "Messages and Voice Chat Time";
                }

                string rankBy = "";
                if (UserStatTracker.rankConfig.rankBy.Equals(UserStatTracker.RankConfig.RankByType.average))
                {
                    rankBy = "Average";
                }
                else if (UserStatTracker.rankConfig.rankBy.Equals(UserStatTracker.RankConfig.RankByType.total))
                {
                    rankBy = "Total";
                }

                string rankTime = "";
                if (UserStatTracker.rankConfig.rankTime.Equals(UserStatTracker.RankConfig.RankTimeType.month))
                {
                    rankTime = "Month";
                }
                else if (UserStatTracker.rankConfig.rankTime.Equals(UserStatTracker.RankConfig.RankTimeType.week))
                {
                    rankTime = "Week";
                }
                else if (UserStatTracker.rankConfig.rankTime.Equals(UserStatTracker.RankConfig.RankTimeType.day))
                {
                    rankTime = "Day";
                }

                //returns bot info
                message.Channel.SendMessageAsync($"Bot info: \n" +
                    $"- Bot command prefix: **{botCommandPrefix}** \n" +
                    $"- Assign ranks time interval: **{myCont.GetAssignRolesInterval().ToString(@"dd\.hh\:mm\:ss")}** \n" +
                    $"- When ranks will be recalculated: **{(myCont.GetAssignRolesTimerStart() + myCont.GetAssignRolesInterval()).ToString()}** \n" +
                    $"- Users ranked by: **{rankBy} {rankType}** in the past **{rankTime}**.\n" + 
                    $"- Rank roles: {roleNames}");

                return Task.CompletedTask;
            }

            //------------------------------------------

            //COMMANDS INVOLVING AFTER-COMMAND STRING HERE
            //------------------------------------------
            string stringAfterCommand = GetStringAfterCommand(message, lengthOfCommand).Result;

            if (command.Equals(updateRanksCommand.ToLower()))
            {
                Console.Write("Manually ");
                myCont.userStatRolesRef.AssignRoles(guildRef);
                message.AddReactionAsync(emoteClap);
            }
            else if (command.Equals(getUserStatsCommand.ToLower()))
            {

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
                        message.Channel.SendMessageAsync($@"Sorry there is no data on {userName}. If there are multiple users with this username try again with the user's discriminator (#0000).");

                        return Task.CompletedTask;
                    }

                    //get rank
                    int rank = myCont.userStatRolesRef.GetUsersRank(myCont.GetUserIDFromName(tempUserStat.UsersFullName));

                    //if user in chat update chattime
                    SocketGuildUser guildUser = guildRef.GetUser(myCont.GetUserIDFromName(tempUserStat.UsersFullName));

                    if (myCont.UserIsInChat(guildUser))
                    {
                        myCont.StopRecordingVCTime(guildUser);
                        myCont.StartRecordingVCTime(guildUser);
                    }

                    string postRank = "th";
                    if ((rank + 1).Equals(1))
                    {
                        postRank = "st";
                    }
                    else if ((rank + 1).Equals(2))
                    {
                        postRank = "nd";
                    }
                    else if ((rank + 1).Equals(3))
                    {
                        postRank = "rd";
                    }

                    int rankTime = tempUserStat.DetermineDays((int)UserStatTracker.rankConfig.rankTime);

                    int totalMsgs = tempUserStat.TotalMessages(rankTime);
                    TimeSpan totalVCTime = tempUserStat.TotalChatTime(rankTime);
                    float avgMsgs = tempUserStat.AverageMessages(rankTime);
                    TimeSpan avgVCTime = tempUserStat.AverageChatTime(rankTime);


                    //stats over the past **{rankTime} days**
                    message.Channel.SendMessageAsync($"__**{tempUserStat.UsersFullName}**__:\n" +
                                                     $"  - Rank: **{rank + 1}{postRank}**\n" +
                                                     $"Stats calculated using the past **{rankTime} days**...\n" +
                                                     $"  - Total Meaningful Messages: **{totalMsgs}**\n" +
                                                     $"  - Total Chattime: **{totalVCTime.Days} days, " +
                                                                            $"{totalVCTime.Hours} hours, " +
                                                                            $"{totalVCTime.Minutes} minutes and " +
                                                                            $"{totalVCTime.Seconds} seconds!**" +
                                                     $"  - Average Meaningful Messages: **{avgMsgs}**\n" +
                                                     $"  - Average Chattime: **{avgVCTime.Days} days, " +
                                                                            $"{avgVCTime.Hours} hours, " +
                                                                            $"{avgVCTime.Minutes} minutes and " +
                                                                            $"{avgVCTime.Seconds} seconds!**");


                }

            }

            //COMMANDS THAT REQUIRE PERMISSIONS
            //PREFIX
            else if (command.Equals(prefixCommand.ToLower()))
            {
                if (!(((SocketGuildUser)(message.Author)).GuildPermissions.Administrator))
                {
                    message.Channel.SendMessageAsync($@"Sorry, you need the {GuildPermission.Administrator} permission to do this");
                    return Task.CompletedTask;
                }

                //Console.WriteLine("Prefix command called");
                //if nothing after prefix then print out prefix otherwise set the prefix to 1st character after space
                if (stringAfterCommand.Equals(ignoreAfterCommandString))
                {
                    message.Channel.SendMessageAsync($@"The command prefix for UserStat bot is {botCommandPrefix}");
                }
                else
                {
                    ChangePrefixCommand(message, stringAfterCommand);
                    Console.WriteLine("Set new prefix");
                    myCont.saveHandlerRef.SaveObject(botCommandPrefix, nameof(CommandHandler.BotCommandPrefix), guildRef);
                }

                return Task.CompletedTask;
            }
            else if (command.Equals(setRankTimeIntervalCommand.ToLower()))
            {
                if (!(((SocketGuildUser)(message.Author)).GuildPermissions.Administrator))
                {
                    message.Channel.SendMessageAsync($@"Sorry, you need the {GuildPermission.Administrator} permission to do this");
                    return Task.CompletedTask;
                }

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
                    message.AddReactionAsync(emoteClap);
                }

                return Task.CompletedTask;
            }
            else if (command.Equals(changeRankCriteria.ToLower()))
            {
                if (!(((SocketGuildUser)(message.Author)).GuildPermissions.Administrator))
                {
                    message.Channel.SendMessageAsync($@"Sorry, you need the {GuildPermission.Administrator} permission to do this");
                    return Task.CompletedTask;
                }

                if (stringAfterCommand.ToLower().Equals("Msg".ToLower()))
                {
                    UserStatTracker.ChangeRankCriteria(UserStatTracker.RankConfig.RankType.messages, myCont);
                }
                else if (stringAfterCommand.ToLower().Equals("Vc".ToLower()))
                {
                    UserStatTracker.ChangeRankCriteria(UserStatTracker.RankConfig.RankType.voiceChatTime, myCont);
                }
                else if (stringAfterCommand.ToLower().Equals("Msg&Vc".ToLower()))
                {
                    UserStatTracker.ChangeRankCriteria(UserStatTracker.RankConfig.RankType.msgAndVCT, myCont);
                }
                else if (stringAfterCommand.ToLower().Equals("Avg".ToLower()))
                {
                    UserStatTracker.ChangeRankCriteria(UserStatTracker.RankConfig.RankByType.average, myCont);
                }
                else if (stringAfterCommand.ToLower().Equals("Total".ToLower()))
                {
                    UserStatTracker.ChangeRankCriteria(UserStatTracker.RankConfig.RankByType.total, myCont);
                }
                else if (stringAfterCommand.ToLower().Equals("Month".ToLower()))
                {
                    UserStatTracker.ChangeRankCriteria(UserStatTracker.RankConfig.RankTimeType.month, myCont);
                }
                else if (stringAfterCommand.ToLower().Equals("Week".ToLower()))
                {
                    UserStatTracker.ChangeRankCriteria(UserStatTracker.RankConfig.RankTimeType.week, myCont);
                }
                else if (stringAfterCommand.ToLower().Equals("Day".ToLower()))
                {
                    UserStatTracker.ChangeRankCriteria(UserStatTracker.RankConfig.RankTimeType.day, myCont);
                }
                else
                {
                    message.Channel.SendMessageAsync($@"Sorry, that was an invalid command. Not sure which one of us is the idiot.");
                    return Task.CompletedTask;
                }
                //update ranks to reflect changed rank criteria\
                Console.Write("Changed rank criteria so ");
                myCont.userStatRolesRef.AssignRoles(guildRef);

                message.AddReactionAsync(emoteClap);

                return Task.CompletedTask;
            }
            else if (command.Equals(setRankMemberLimitCommand.ToLower()))
            {
                if (!(((SocketGuildUser)(message.Author)).GuildPermissions.Administrator))
                {
                    message.Channel.SendMessageAsync($@"Sorry, you need the {GuildPermission.Administrator} permission to do this");
                    return Task.CompletedTask;
                }

                if (!(stringAfterCommand.Contains(',')))
                {
                    message.Channel.SendMessageAsync($@"Invalid input. Use a comma!");
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
                        if(newMemberLimit < 0)
                        {
                            newMemberLimit = 0;
                            Console.WriteLine("MemberLimit amount cannot be negative. Changed to '0'");
                        }

                        //...set new amount 
                        myCont.userStatRolesRef.rankRoles[index].memberLimit = newMemberLimit;
                        message.AddReactionAsync(emoteClap);
                        Console.WriteLine($@"{myCont.userStatRolesRef.rankRoles[index].name} memberLimit changed to {newMemberLimit}.");
                    }
                }

                //update roles
                Console.Write("Changed number of users in a role so ");
                myCont.userStatRolesRef.AssignRoles(guildRef);
                //save roles
                myCont.userStatRolesRef.SaveRankRoles(guildRef, myCont.saveHandlerRef);

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
