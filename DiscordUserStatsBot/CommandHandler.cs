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

        #region VARIABLES
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
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
            getUserStatsCommand = "UserStats",
            getRankStatsCommand = "RankStats",
            setRankTimeIntervalCommand = "SetRankTimeInterval",
            setRankMemberLimitCommand = "SetRankMemberLimit",
            changeRankCriteria = "RankBy",
            updateRanksCommand = "UpdateRanks", 
            botInfoCommand = "BotInfo",
            showRanksCommand = "ShowRanks";

        int amountShowRanks = 10;

        //bool to stop commands for this bot from being recorded in UserStat
        public bool wasBotCommand;

        IEmote emoteClap;
        IEmote emoteDonate;
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        #endregion


        //CONSTRUCTOR
        public CommandHandler(UserStatsBotController myController, SocketGuild guildReference)
        {
            myCont = myController;
            guildRef = guildReference;

            emoteClap = new Emoji("👏");
            emoteDonate = new Emoji("🤗");
        }

        #region CommandFunctions
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        public Task CommandHandlerFunc(SocketMessage message)          //REMEMBER ALL COMMANDS MUST BE LOWERCASE
        {
            #region MessageFilter
            //--------------------------------------------------------------------------------------------------
            //rule out messages that don't have bot prefix
            if (!message.Content.StartsWith(botCommandPrefix))
            {
                return Task.CompletedTask;
            }

            //rule out messages that bots (including itself) create
            else if (message.Author.IsBot)
            {
                return Task.CompletedTask;
            }

            //ignore if this message was not sent in this guild
            if (((SocketGuildChannel)(message.Channel)).Guild.Id.Equals(guildRef.Id))
            {
                myCont.Log(new Discord.LogMessage(LogSeverity.Debug, this.ToString(), "Command sent in THIS guild."));
            }
            else
            {
                //myCont.Log(new Discord.LogMessage(LogSeverity.Debug, this.ToString(), "Command sent in OTHER guild."));
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
                message.Channel.SendMessageAsync("Hello fellow user! \n" +
                    $"**Type '{botCommandPrefix}{helpCommand}' for a list of bot commands.** \n" +
                    $"Support My Creator @ https://ko-fi.com/tomthedoer {emoteDonate}");
                return Task.CompletedTask;
            }
            else if (command.Equals(aboutCommand.ToLower()))
            {
                EmbedBuilder builder = new EmbedBuilder();
                builder.WithTitle("About User Stats Bot");

                builder.AddField("The StatTracker bot creates a more organized sidebar by putting active users near the top. \n\n",
                    "**The bot does this by doing two things:** \n    **1.** It records users' guild (AKA server) activity over the past month (their time in chat + messages sent). \n" +
                                                 "    **2.** It assigns users a corresponding 'RankRole' based off of their activity.\n\n" +
                                                 "*You have the option to turn off the sidebar organization if you just want a bit of fun comparing stats with your friends.*");
                builder.AddField($"Type '{botCommandPrefix}{helpCommand}' for a list of bot commands.", $"[Support My Creator {emoteDonate}](https://ko-fi.com/tomthedoer)");
                //builder.WithFooter(footer => footer.Text = $"Type '{botCommandPrefix}{helpCommand}' for a list of bot commands.");

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
                builder.AddField(botCommandPrefix + getRankStatsCommand + " *<RankRole>*", "Get a given RankRole's stats.");
                builder.AddField(botCommandPrefix + showRanksCommand, "Get the top ranked users' and their stats.");
                builder.AddField(botCommandPrefix + updateRanksCommand, "Recalculates everyones rank.");
                if (((SocketGuildUser)(message.Author)).GuildPermissions.Administrator)
                {
                    builder.AddField(botCommandPrefix + setRankTimeIntervalCommand + " *<hours>* ", "Change time interval between when users ranks are calculated. *By default is 24 hours*. This command resets the timer. *Admin only*");
                    builder.AddField(botCommandPrefix + setRankMemberLimitCommand + " *<RankRole>, <Amount>* ", "changes the number of users in a RankRole to the given Amount. Use '0' to disable the role. *Admin only*");
                    builder.AddField(botCommandPrefix + changeRankCriteria + " *<Criteria>*", "Sets what criteria people are ranked by. *Admin only*\n" +
                        "                     Criteria can be: messages(*Msg*), voice chat(*Vc*) or both(*Msg&Vc*). average (*Avg*) or totals(*Total*). month(*Month*), week(*Week*), or day(*Day*).\n");
                }

                builder.AddField("------------------------------------------------", $"[Support My Creator {emoteDonate}](https://ko-fi.com/tomthedoer)");

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
                if (myCont.userStatConfigRef.rankConfig.rankType.Equals(UserStatConfig.RankConfig.RankType.messages))
                {
                    rankType = "Messages";
                }
                else if (myCont.userStatConfigRef.rankConfig.rankType.Equals(UserStatConfig.RankConfig.RankType.voiceChatTime))
                {
                    rankType = "Voice Chat Time";
                }
                else if (myCont.userStatConfigRef.rankConfig.rankType.Equals(UserStatConfig.RankConfig.RankType.msgAndVCT))
                {
                    rankType = "Messages and Voice Chat Time";
                }

                string rankBy = "";
                if (myCont.userStatConfigRef.rankConfig.rankBy.Equals(UserStatConfig.RankConfig.RankByType.average))
                {
                    rankBy = "Average";
                }
                else if (myCont.userStatConfigRef.rankConfig.rankBy.Equals(UserStatConfig.RankConfig.RankByType.total))
                {
                    rankBy = "Total";
                }

                string rankTime = "";
                if (myCont.userStatConfigRef.rankConfig.rankTime.Equals(UserStatConfig.RankConfig.RankTimeType.month))
                {
                    rankTime = "Month";
                }
                else if (myCont.userStatConfigRef.rankConfig.rankTime.Equals(UserStatConfig.RankConfig.RankTimeType.week))
                {
                    rankTime = "Week";
                }
                else if (myCont.userStatConfigRef.rankConfig.rankTime.Equals(UserStatConfig.RankConfig.RankTimeType.day))
                {
                    rankTime = "Day";
                }

                /*
                //returns bot info
                message.Channel.SendMessageAsync($"Bot info: \n" +
                    $"- Bot command prefix: **{botCommandPrefix}** \n" +
                    $"- Assign ranks time interval: **{myCont.GetAssignRolesInterval().ToString(@"dd\.hh\:mm\:ss")}** \n" +
                    $"- When ranks will be recalculated: **{(myCont.GetAssignRolesTimerStart() + myCont.GetAssignRolesInterval()).ToString()}** \n" +
                    $"- Users ranked by: **{rankBy} {rankType}** in the past **{rankTime}**.\n" + 
                    $"- Rank roles: {roleNames}");
                */

                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle($"Bot Config Info:");
                builder.AddField($"{botCommandPrefix}", "Bot command prefix");
                builder.AddField($"{myCont.GetAssignRolesInterval().ToString(@"dd\.hh\:mm\:ss")}", "Assign ranks time interval");
                builder.AddField($"{(myCont.GetAssignRolesTimerStart() + myCont.GetAssignRolesInterval()).ToString()}", "When ranks will be recalculated");
                builder.AddField($"------------------------------------------------", $"Users ranked by: **{rankBy} {rankType}** in the past **{rankTime}**.");
                builder.AddField($"Rank roles", $"{roleNames}");

                message.Channel.SendMessageAsync("", false, builder.Build());

                return Task.CompletedTask;
            }
            else if (command.Equals(showRanksCommand.ToLower()))
            {
                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle($"Top {amountShowRanks} Ranks:");

                List<ulong> topUsers = myCont.userStatRolesRef.GetTopUsers(amountShowRanks);


                UserStatTracker stats;
                int rankTime;

                for (int rank = 0; rank < topUsers.Count; rank++)
                {
                    stats = myCont.GetUserStats(topUsers[rank]);

                    if(stats == null)
                    {
                        myCont.Log(new Discord.LogMessage(LogSeverity.Error, this.ToString(), "Error: Top user not found on server."));
                        builder.AddField($"Rank {rank} : UserNotFound", "Impacts users that have left the guild.");
                        continue;
                    }

                    //TODO: if totals use those instead

                    rankTime = stats.DetermineDays((int)myCont.userStatConfigRef.rankConfig.rankTime);

                    //if avg
                    if (myCont.userStatConfigRef.rankConfig.rankBy.Equals(UserStatConfig.RankConfig.RankByType.average))
                    {
                        builder.AddField($"Rank {rank} : {stats.UsersFullName}", $"Days calculated: {rankTime} \n Avg Msgs: {stats.AverageMessages(rankTime).ToString("0.00")} \n Avg VC: {stats.AverageChatTime(rankTime).ToString(@"dd\.hh\:mm\:ss")}");
                    }
                    else if (myCont.userStatConfigRef.rankConfig.rankBy.Equals(UserStatConfig.RankConfig.RankByType.total))
                    {
                        builder.AddField($"Rank {rank} : {stats.UsersFullName}", $"Days calculated: {rankTime} \n Total Msgs: {stats.TotalMessages(rankTime)} \n Total VC: {stats.TotalChatTime(rankTime).ToString(@"dd\.hh\:mm\:ss")}");
                    }
                    else
                    {
                        myCont.Log(new Discord.LogMessage(LogSeverity.Error, this.ToString(), "Forgot to account for new RankBy type in showRanksCommand."));
                    }
                }

                message.Channel.SendMessageAsync("", false, builder.Build());

                return Task.CompletedTask;
            }

            //------------------------------------------

            //COMMANDS INVOLVING AFTER-COMMAND STRING HERE
            //------------------------------------------
            string stringAfterCommand = GetStringAfterCommand(message, lengthOfCommand).Result;

            if (command.Equals(updateRanksCommand.ToLower()))
            {
                Console.Write("Manually ");

                //TODO: update info of people currently in chat

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

                    int rankTime = tempUserStat.DetermineDays((int)myCont.userStatConfigRef.rankConfig.rankTime);

                    int totalMsgs = tempUserStat.TotalMessages(rankTime);
                    TimeSpan totalVCTime = tempUserStat.TotalChatTime(rankTime);
                    float avgMsgs = tempUserStat.AverageMessages(rankTime);
                    TimeSpan avgVCTime = tempUserStat.AverageChatTime(rankTime);

                    message.Channel.SendMessageAsync($"__**{tempUserStat.UsersFullName} Stats**__:\n" +
                                                     $"  - Rank: **{rank + 1}{postRank}**\n" +
                                                     $"Stats calculated using the past **{rankTime} days**...\n" +
                                                     $"  - Total Meaningful Messages: **{totalMsgs}**\n" +
                                                     $"  - Total Chattime: **{totalVCTime.Days} days, " +
                                                                            $"{totalVCTime.Hours} hours, " +
                                                                            $"{totalVCTime.Minutes} minutes and " +
                                                                            $"{totalVCTime.Seconds} seconds!**\n" +
                                                     $"  - Average Meaningful Messages: **{avgMsgs.ToString("0.00")}**\n" +
                                                     $"  - Average Chattime: **{avgVCTime.Days} days, " +
                                                                            $"{avgVCTime.Hours} hours, " +
                                                                            $"{avgVCTime.Minutes} minutes and " +
                                                                            $"{avgVCTime.Seconds} seconds!**");


                }

            }
            else if (command.Equals(getRankStatsCommand.ToLower()))
            {
                string roleName = stringAfterCommand.Trim();

                UserStatTracker tempStatTracker;
                
                List<float> messageAvgs = new List<float>();
                int messageTotal = 0;
                List<TimeSpan> VCAvgs = new List<TimeSpan>();
                TimeSpan VCTotal = TimeSpan.Zero;
                int daysCalculatedOver = 0;
                int amountOfRankMembers = 0;

                bool foundRole = false;

                //find role
                for (int index = 0; index < myCont.userStatRolesRef.rankRoles.Length; index++)
                {
                    if (myCont.userStatRolesRef.rankRoles[index].name.ToLower().Equals(roleName.ToLower()))
                    {
                        foundRole = true;
                        roleName = myCont.userStatRolesRef.rankRoles[index].name;

                        //get all the members in that role
                        List<SocketGuildUser> userList = myCont.userStatRolesRef.GetAllUsersInRank(myCont.userStatRolesRef.rankRoles[index], guildRef);

                        amountOfRankMembers = userList.Count;

                        //for each member in that role get their averages/totals
                        foreach (SocketGuildUser guildUser in userList)
                        {
                            tempStatTracker = myCont.GetUserStats(myCont.GetUserNamePlusDiscrim(guildUser));

                            if(tempStatTracker == null)
                            {
                                myCont.Log(new Discord.LogMessage(LogSeverity.Error, this.ToString(), $"Error: No info on {roleName} member '{myCont.GetUserNamePlusDiscrim(guildUser)}'"));
                            }
                            else
                            {
                                int tempDays = tempStatTracker.DetermineDays((int)myCont.userStatConfigRef.rankConfig.rankTime);
                                if(daysCalculatedOver < tempDays)
                                {
                                    daysCalculatedOver = tempDays;
                                }

                                messageAvgs.Add(tempStatTracker.AverageMessages(tempDays));
                                messageTotal += tempStatTracker.TotalMessages(tempDays);
                                VCAvgs.Add(tempStatTracker.AverageChatTime(tempDays));
                                VCTotal += tempStatTracker.TotalChatTime(tempDays);
                            }

                            
                        }

                        //end loop because you found the role
                        index = myCont.userStatRolesRef.rankRoles.Length;
                    }
                }
                
                if (foundRole)
                {
                TimeSpan VCAvg = Average(VCAvgs);

                message.Channel.SendMessageAsync($"__**{roleName} Stats**__:\n" +
                                                     $"  - Number of Members: **{amountOfRankMembers}**\n" +
                                                     $"Stats calculated using the past **{daysCalculatedOver} days**...\n" +
                                                     $"  - Total Meaningful Messages: **{messageTotal}**\n" +
                                                     $"  - Total Chattime: **{VCTotal.Days} days, " +
                                                                            $"{VCTotal.Hours} hours, " +
                                                                            $"{VCTotal.Minutes} minutes and " +
                                                                            $"{VCTotal.Seconds} seconds!**\n" +
                                                     $"  - Average Meaningful Messages: **{Average(messageAvgs).ToString("0.00")}**\n" +
                                                     $"  - Average Chattime: **{VCAvg.Days} days, " +
                                                                            $"{VCAvg.Hours} hours, " +
                                                                            $"{VCAvg.Minutes} minutes and " +
                                                                            $"{VCAvg.Seconds} seconds!**");
                }
                //Error if doesnt find any role with given name
                else
                {
                    message.Channel.SendMessageAsync($@"Sorry there is no rankrole named '{roleName}'.");
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
                //if nothing after prefix then print out prefix otherwise set the prefix to 1st character after space
                if (stringAfterCommand.Equals(ignoreAfterCommandString))
                {
                    message.Channel.SendMessageAsync($@"The command prefix for UserStat bot is {botCommandPrefix}");
                }
                else
                {
                    ChangePrefixCommand(message, stringAfterCommand);
                    myCont.Log("Set new prefix");
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
                    myCont.userStatConfigRef.ChangeRankCriteria(UserStatConfig.RankConfig.RankType.messages, myCont);
                }
                else if (stringAfterCommand.ToLower().Equals("Vc".ToLower()))
                {
                    myCont.userStatConfigRef.ChangeRankCriteria(UserStatConfig.RankConfig.RankType.voiceChatTime, myCont);
                }
                else if (stringAfterCommand.ToLower().Equals("Msg&Vc".ToLower()))
                {
                    myCont.userStatConfigRef.ChangeRankCriteria(UserStatConfig.RankConfig.RankType.msgAndVCT, myCont);
                }
                else if (stringAfterCommand.ToLower().Equals("Avg".ToLower()))
                {
                    myCont.userStatConfigRef.ChangeRankCriteria(UserStatConfig.RankConfig.RankByType.average, myCont);
                }
                else if (stringAfterCommand.ToLower().Equals("Total".ToLower()))
                {
                    myCont.userStatConfigRef.ChangeRankCriteria(UserStatConfig.RankConfig.RankByType.total, myCont);
                }
                else if (stringAfterCommand.ToLower().Equals("Month".ToLower()))
                {
                    myCont.userStatConfigRef.ChangeRankCriteria(UserStatConfig.RankConfig.RankTimeType.month, myCont);
                }
                else if (stringAfterCommand.ToLower().Equals("Week".ToLower()))
                {
                    myCont.userStatConfigRef.ChangeRankCriteria(UserStatConfig.RankConfig.RankTimeType.week, myCont);
                }
                else if (stringAfterCommand.ToLower().Equals("Day".ToLower()))
                {
                    myCont.userStatConfigRef.ChangeRankCriteria(UserStatConfig.RankConfig.RankTimeType.day, myCont);
                }
                else
                {
                    message.Channel.SendMessageAsync($@"Sorry, that was an invalid command. Not sure which one of us is the idiot.");
                    return Task.CompletedTask;
                }
                //update ranks to reflect changed rank criteria\
                myCont.Log("Changed rank criteria.");
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
                    myCont.Log(new Discord.LogMessage(LogSeverity.Warning, this.ToString(), $@"Failed to change memberAmount (string not converted to int)"));
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
                            myCont.Log(new Discord.LogMessage(LogSeverity.Debug, this.ToString(), "MemberLimit amount cannot be negative. Changed to '0'"));
                        }

                        //...set new amount 
                        myCont.userStatRolesRef.rankRoles[index].memberLimit = newMemberLimit;
                        message.AddReactionAsync(emoteClap);
                        myCont.Log($@"{myCont.userStatRolesRef.rankRoles[index].name} memberLimit changed to {newMemberLimit}.");
                    }
                }

                //update roles
                myCont.Log("Changed number of users in a role.");
                myCont.userStatRolesRef.AssignRoles(guildRef);
                //save roles
                myCont.userStatRolesRef.SaveRankRoles(guildRef, myCont.saveHandlerRef);

            }
            //------------------------------------------
            //--------------------------------------------------------------------------------------------------
            #endregion

            return Task.CompletedTask;
        }

        /// <summary>
        /// Intro message to get users started with the bot.
        /// </summary>
        /// <param name="guild"></param>
        /// <returns></returns>
        public Task IntroMessage(SocketGuild guild)
        {
            guild.DefaultChannel.SendMessageAsync($"Hi! Type '{BotCommandPrefix}{helpCommand}' for a list of bot commands. \n [Support My Creator {emoteDonate}](https://ko-fi.com/tomthedoer)");

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
