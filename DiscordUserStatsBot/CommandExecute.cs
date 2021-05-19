using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordUserStatsBot
{
    public class CommandExecute : BotComponent
    {
        //private bool wasBotCommand = false;
        private CommandHandler cH;

        override public void Init()
        {
            cH = dIRef.CommandHandRef;
        }

        public Task ExecuteCommand(SocketMessage message)
        {
            //Command specific Permission Check
            if (//dIRef.GuildRef.CurrentUser != null &&
                (!dIRef.GuildRef.CurrentUser.GuildPermissions.SendMessages ||
                !dIRef.GuildRef.CurrentUser.GuildPermissions.ViewChannel ||
                !dIRef.GuildRef.CurrentUser.GuildPermissions.ReadMessageHistory))
            {
                dIRef.LogRef.Log(new LogMessage(LogSeverity.Error, this.ToString(), "Bot can't execute commands because lacks permissions."));
                return Task.CompletedTask;
            }

            #region MessageFilter
            //--------------------------------------------------------------------------------------------------
            //rule out messages that don't have bot prefix
            if (!message.Content.StartsWith(cH.BotCommandPrefix))
            {
                return Task.CompletedTask;
            }

            //rule out messages that bots (including itself) create
            else if (message.Author.IsBot)
            {
                return Task.CompletedTask;
            }

            //ignore if this message was not sent in this guild
            if (((SocketGuildChannel)(message.Channel)).Guild.Id.Equals(dIRef.GuildRef.Id))
            {
                dIRef.LogRef.Log(new LogMessage(LogSeverity.Debug, this.ToString(), "Command sent in THIS guild."));
            }
            else
            {
                //dIRef.LogRef.Log(new Discord.LogMessage(LogSeverity.Debug, this.ToString(), "Command sent in OTHER guild."));
                return Task.CompletedTask;
            }
            //--------------------------------------------------------------------------------------------------
            #endregion

            dIRef.CommandHandRef.wasBotCommand = true;

            //All permission check
            if (!dIRef.ContRef.HasPermissions())
            {
                dIRef.GuildRef.DefaultChannel.SendMessageAsync("Beware: Bot won't fully function because lacking permissions... " + cH.emoteSad + "\n" + dIRef.ContRef.MissingPermissions);
                
            }

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
            if (command.Equals(CommandHandler.greetCommand.ToLower()))
            {
                message.Channel.SendMessageAsync("Hello fellow user! \n" +
                    $"**Type '{cH.BotCommandPrefix}{CommandHandler.helpCommand}' for a list of bot commands.**");
                return Task.CompletedTask;
            }
            else if (command.Equals(CommandHandler.aboutCommand.ToLower()))
            {
                EmbedBuilder builder = new EmbedBuilder();
                builder.WithTitle("About User Stats Bot");

                builder.AddField("The UserStats bot creates a more organized sidebar by putting active users near the top. \n\n",
                    "**The bot does this by doing two things:** \n    **1.** It records users' guild (AKA server) activity over the past month (their time in chat + messages sent). \n" +
                                                 "    **2.** It assigns users a corresponding 'RankRole' based off of their activity.\n\n" +
                                                 "*You have the option to turn off the sidebar organization if you just want a bit of fun comparing stats with your friends.*");
                builder.AddField($"Type '{cH.BotCommandPrefix}{CommandHandler.helpCommand}' for a list of bot commands.", $"[Support My Creator {cH.emoteDonate}](https://ko-fi.com/tomthedoer)");
                //builder.WithFooter(footer => footer.Text = $"Type '{cH.BotCommandPrefix}{helpCommand}' for a list of bot commands.");

                message.Channel.SendMessageAsync("", false, builder.Build());

                return Task.CompletedTask;
            }
            else if (command.Equals(CommandHandler.helpCommand.ToLower()))
            {
                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle("User Stats Bot Commands: ");
                //builder.WithFooter(footer => footer.Text = $"current botPrefix is '{cH.BotCommandPrefix}'");
                builder.AddField(cH.BotCommandPrefix + CommandHandler.greetCommand, "The bot greets you.");
                builder.AddField(cH.BotCommandPrefix + CommandHandler.aboutCommand, "About this bot");
                builder.AddField(cH.BotCommandPrefix + CommandHandler.helpCommand, "Provides a list of bot commands.");
                if (((SocketGuildUser)(message.Author)).GuildPermissions.Administrator)
                {
                    builder.AddField(cH.BotCommandPrefix + CommandHandler.prefixCommand + " *(<newPrefix>)*", @"Get the botPrefix OR (optional) change it. *Admin only*");
                }
                else
                {
                    builder.AddField(cH.BotCommandPrefix + CommandHandler.prefixCommand, @"Get the botPrefix.");
                }
                builder.AddField(cH.BotCommandPrefix + CommandHandler.botInfoCommand, @"Gives relevant bot configuration information.");
                builder.AddField(cH.BotCommandPrefix + CommandHandler.getUserStatsCommand + " *<username(#0000)>*", "Get a given user's rank and stats.");
                builder.AddField(cH.BotCommandPrefix + CommandHandler.getRankStatsCommand + " *<RankRole>*", "Get a given RankRole's stats.");
                builder.AddField(cH.BotCommandPrefix + CommandHandler.showRanksCommand, "Get the top ranked users' and their stats.");
                builder.AddField(cH.BotCommandPrefix + CommandHandler.updateRanksCommand, "Recalculates everyones rank.");
                if (((SocketGuildUser)(message.Author)).GuildPermissions.Administrator)
                {
                    builder.AddField("------------------------------------------------", $"__**Admin Only**__: ");
                    builder.AddField(cH.BotCommandPrefix + CommandHandler.setRankTimeIntervalCommand + " *<hours>* ", "Change time interval between when users ranks are calculated. *By default is 24 hours*. This command resets the timer.");
                    builder.AddField(cH.BotCommandPrefix + CommandHandler.setRankMemberLimitCommand + " *<RankRole>, <Amount>* ", "changes the number of users in a RankRole to the given Amount. Use '0' to disable the role.");
                    builder.AddField(cH.BotCommandPrefix + CommandHandler.changeRankCriteriaCommand + " *<Criteria>*", "Sets what criteria people are ranked by.\n" +
                        "                     Criteria can be: messages(*Msg*), voice chat(*Vc*) or both(*Msg&Vc*). average (*Avg*) or totals(*Total*). month(*Month*), week(*Week*), or day(*Day*).\n");
                }

                builder.AddField("------------------------------------------------", $"***Beware**: Bot role must be above generated RankRoles for bot to function*");

                builder.AddField("------------------------------------------------", $"[Support My Creator {cH.emoteDonate}](https://ko-fi.com/tomthedoer)");

                message.Channel.SendMessageAsync("", false, builder.Build());

                return Task.CompletedTask;
            }
            else if (command.Equals(CommandHandler.botInfoCommand.ToLower()))
            {
                string roleNames = "";
                for (int index = 0; index < dIRef.RolesRef.rankRoles.Length; index++)
                {
                    roleNames += $"\n*Tier {index + 1}* : **__{dIRef.RolesRef.rankRoles[index].name}__** : \n                       - Member limit = **{dIRef.RolesRef.rankRoles[index].memberLimit}**";
                }

                string rankType = "";
                if (dIRef.ConfigRef.rankConfig.rankType.Equals(UserStatConfig.RankConfig.RankType.messages))
                {
                    rankType = "Messages";
                }
                else if (dIRef.ConfigRef.rankConfig.rankType.Equals(UserStatConfig.RankConfig.RankType.voiceChatTime))
                {
                    rankType = "Voice Chat Time";
                }
                else if (dIRef.ConfigRef.rankConfig.rankType.Equals(UserStatConfig.RankConfig.RankType.msgAndVCT))
                {
                    rankType = "Messages and Voice Chat Time";
                }

                string rankBy = "";
                if (dIRef.ConfigRef.rankConfig.rankBy.Equals(UserStatConfig.RankConfig.RankByType.average))
                {
                    rankBy = "Average";
                }
                else if (dIRef.ConfigRef.rankConfig.rankBy.Equals(UserStatConfig.RankConfig.RankByType.total))
                {
                    rankBy = "Total";
                }

                string rankTime = "";
                if (dIRef.ConfigRef.rankConfig.rankTime.Equals(UserStatConfig.RankConfig.RankTimeType.month))
                {
                    rankTime = "Month";
                }
                else if (dIRef.ConfigRef.rankConfig.rankTime.Equals(UserStatConfig.RankConfig.RankTimeType.week))
                {
                    rankTime = "Week";
                }
                else if (dIRef.ConfigRef.rankConfig.rankTime.Equals(UserStatConfig.RankConfig.RankTimeType.day))
                {
                    rankTime = "Day";
                }

                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle($"Bot Config Info:");
                builder.AddField($"{cH.BotCommandPrefix}", "Bot command prefix");
                builder.AddField($"{dIRef.ContRef.GetAssignRolesInterval().ToString(@"dd\.hh\:mm\:ss")}", "Assign ranks time interval");
                builder.AddField($"{(dIRef.ContRef.GetAssignRolesTimerStart() + dIRef.ContRef.GetAssignRolesInterval()).ToString() + " GMT"}", "When ranks will be recalculated");
                builder.AddField($"------------------------------------------------", $"Users ranked by: **{rankBy} {rankType}** in the past **{rankTime}**.");
                builder.AddField($"Rank roles", $"{roleNames}");

                message.Channel.SendMessageAsync("", false, builder.Build());

                return Task.CompletedTask;
            }
            else if (command.Equals(CommandHandler.showRanksCommand.ToLower()))
            {
                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle($"Top {cH.AmountShowRanks} Ranks:");

                List<ulong> topUsers = dIRef.RolesRef.GetTopUsers(cH.AmountShowRanks);


                UserStatTracker stats;
                int rankTime;

                for (int rank = 0; rank < topUsers.Count; rank++)
                {
                    stats = dIRef.ContRef.GetUserStats(topUsers[rank]);

                    if (stats == null)
                    {
                        dIRef.LogRef.Log(new Discord.LogMessage(LogSeverity.Error, this.ToString(), "Error: Top user not found on server."));
                        builder.AddField($"Rank {rank} : UserNotFound", "Impacts users that have left the guild.");
                        continue;
                    }

                    rankTime = stats.DetermineDays((int)dIRef.ConfigRef.rankConfig.rankTime);

                    //present averages or totals
                    if (dIRef.ConfigRef.rankConfig.rankBy.Equals(UserStatConfig.RankConfig.RankByType.average))
                    {
                        builder.AddField($"Rank {rank} : {stats.UsersFullName}", $"Days calculated: {rankTime} \n Avg Msgs: {stats.AverageMessages(rankTime).ToString("0.00")} \n Avg VC: {stats.AverageChatTime(rankTime).ToString(@"dd\.hh\:mm\:ss")}");
                    }
                    else if (dIRef.ConfigRef.rankConfig.rankBy.Equals(UserStatConfig.RankConfig.RankByType.total))
                    {
                        builder.AddField($"Rank {rank} : {stats.UsersFullName}", $"Days calculated: {rankTime} \n Total Msgs: {stats.TotalMessages(rankTime)} \n Total VC: {stats.TotalChatTime(rankTime).ToString(@"dd\.hh\:mm\:ss")}");
                    }
                    else
                    {
                        dIRef.LogRef.Log(new Discord.LogMessage(LogSeverity.Error, this.ToString(), "Forgot to account for new RankBy type in showRanksCommand."));
                    }
                }

                message.Channel.SendMessageAsync("", false, builder.Build());

                return Task.CompletedTask;
            }

            //------------------------------------------

            //COMMANDS INVOLVING AFTER-COMMAND STRING HERE
            //------------------------------------------
            string stringAfterCommand = cH.GetStringAfterCommand(message, lengthOfCommand).Result;

            if (command.Equals(CommandHandler.updateRanksCommand.ToLower()))
            {
                dIRef.LogRef.Log("Manually ");

                //TODO: update info of people currently in chat

                dIRef.RolesRef.AssignRoles(dIRef.GuildRef);
                message.AddReactionAsync(cH.emoteClap);
            }
            else if (command.Equals(CommandHandler.getUserStatsCommand.ToLower()))
            {

                string userName = cH.ReformatStringToUsername(stringAfterCommand).Result;

                if (userName == null)
                {
                    message.Channel.SendMessageAsync($@"Sorry that isn't a valid username");
                    return Task.CompletedTask;
                }
                else
                {
                    UserStatTracker tempUserStat = dIRef.ContRef.GetUserStats(userName);

                    if (tempUserStat == null)
                    {
                        message.Channel.SendMessageAsync($@"Sorry there is no data on {userName}. If there are multiple users with this username try again with the user's discriminator (#0000).");

                        return Task.CompletedTask;
                    }

                    //get rank
                    int rank = dIRef.RolesRef.GetUsersRank(dIRef.ContRef.GetUserIDFromName(tempUserStat.UsersFullName));

                    //if user in chat update chattime
                    SocketGuildUser guildUser = dIRef.GuildRef.GetUser(dIRef.ContRef.GetUserIDFromName(tempUserStat.UsersFullName));

                    if (dIRef.ContRef.UserIsInChat(guildUser))
                    {
                        dIRef.ContRef.StopRecordingVCTime(guildUser);
                        dIRef.ContRef.StartRecordingVCTime(guildUser);
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

                    int rankTime = tempUserStat.DetermineDays((int)dIRef.ConfigRef.rankConfig.rankTime);

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
            else if (command.Equals(CommandHandler.getRankStatsCommand.ToLower()))
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
                for (int index = 0; index < dIRef.RolesRef.rankRoles.Length; index++)
                {
                    if (dIRef.RolesRef.rankRoles[index].name.ToLower().Equals(roleName.ToLower()))
                    {
                        foundRole = true;
                        roleName = dIRef.RolesRef.rankRoles[index].name;

                        //get all the members in that role
                        List<SocketGuildUser> userList = dIRef.RolesRef.GetAllUsersInRank(dIRef.RolesRef.rankRoles[index], dIRef.GuildRef);

                        amountOfRankMembers = userList.Count;

                        //for each member in that role get their averages/totals
                        foreach (SocketGuildUser guildUser in userList)
                        {
                            tempStatTracker = dIRef.ContRef.GetUserStats(dIRef.ContRef.GetUserNamePlusDiscrim(guildUser));

                            if (tempStatTracker == null)
                            {
                                dIRef.LogRef.Log(new Discord.LogMessage(LogSeverity.Error, this.ToString(), $"Error: No info on {roleName} member '{dIRef.ContRef.GetUserNamePlusDiscrim(guildUser)}'"));
                            }
                            else
                            {
                                int tempDays = tempStatTracker.DetermineDays((int)dIRef.ConfigRef.rankConfig.rankTime);
                                if (daysCalculatedOver < tempDays)
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
                        index = dIRef.RolesRef.rankRoles.Length;
                    }
                }

                if (foundRole)
                {
                    TimeSpan VCAvg = cH.Average(VCAvgs);

                    message.Channel.SendMessageAsync($"__**{roleName} Stats**__:\n" +
                                                         $"  - Number of Members: **{amountOfRankMembers}**\n" +
                                                         $"Stats calculated using the past **{daysCalculatedOver} days**...\n" +
                                                         $"  - Total Meaningful Messages: **{messageTotal}**\n" +
                                                         $"  - Total Chattime: **{VCTotal.Days} days, " +
                                                                                $"{VCTotal.Hours} hours, " +
                                                                                $"{VCTotal.Minutes} minutes and " +
                                                                                $"{VCTotal.Seconds} seconds!**\n" +
                                                         $"  - Average Meaningful Messages: **{cH.Average(messageAvgs).ToString("0.00")}**\n" +
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

            //------------------------------------------

            //COMMANDS THAT REQUIRE PERMISSIONS
            //------------------------------------------
            //PREFIX
            else if (command.Equals(CommandHandler.prefixCommand.ToLower()))
            {
                //if nothing after prefix then print out prefix otherwise set the prefix to 1st character after space if admin
                if (stringAfterCommand.Equals(cH.IgnoreAfterCommandString))
                {
                    message.Channel.SendMessageAsync($@"The command prefix for UserStat bot is {cH.BotCommandPrefix}");
                }
                else
                {
                    if (!(((SocketGuildUser)(message.Author)).GuildPermissions.Administrator))
                    {
                        message.Channel.SendMessageAsync($@"Sorry, you need the {GuildPermission.Administrator} permission to do this");
                        return Task.CompletedTask;
                    }
                    cH.ChangePrefixCommand(message, stringAfterCommand);
                    dIRef.LogRef.Log("Set new prefix");
                    SaveHandler.S.SaveObject(cH.BotCommandPrefix, nameof(CommandHandler.BotCommandPrefix), dIRef.GuildRef);
                }

                return Task.CompletedTask;
            }
            else if (command.Equals(CommandHandler.setRankTimeIntervalCommand.ToLower()))
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
                    dIRef.ContRef.ChangeAssignRolesInterval(tP);
                    message.AddReactionAsync(cH.emoteClap);
                }

                return Task.CompletedTask;
            }
            else if (command.Equals(CommandHandler.changeRankCriteriaCommand.ToLower()))
            {
                if (!(((SocketGuildUser)(message.Author)).GuildPermissions.Administrator))
                {
                    message.Channel.SendMessageAsync($@"Sorry, you need the {GuildPermission.Administrator} permission to do this");
                    return Task.CompletedTask;
                }

                if (stringAfterCommand.ToLower().Equals("Msg".ToLower()))
                {
                    dIRef.ConfigRef.ChangeRankCriteria(UserStatConfig.RankConfig.RankType.messages, dIRef.ContRef);
                }
                else if (stringAfterCommand.ToLower().Equals("Vc".ToLower()))
                {
                    dIRef.ConfigRef.ChangeRankCriteria(UserStatConfig.RankConfig.RankType.voiceChatTime, dIRef.ContRef);
                }
                else if (stringAfterCommand.ToLower().Equals("Msg&Vc".ToLower()))
                {
                    dIRef.ConfigRef.ChangeRankCriteria(UserStatConfig.RankConfig.RankType.msgAndVCT, dIRef.ContRef);
                }
                else if (stringAfterCommand.ToLower().Equals("Avg".ToLower()))
                {
                    dIRef.ConfigRef.ChangeRankCriteria(UserStatConfig.RankConfig.RankByType.average, dIRef.ContRef);
                }
                else if (stringAfterCommand.ToLower().Equals("Total".ToLower()))
                {
                    dIRef.ConfigRef.ChangeRankCriteria(UserStatConfig.RankConfig.RankByType.total, dIRef.ContRef);
                }
                else if (stringAfterCommand.ToLower().Equals("Month".ToLower()))
                {
                    dIRef.ConfigRef.ChangeRankCriteria(UserStatConfig.RankConfig.RankTimeType.month, dIRef.ContRef);
                }
                else if (stringAfterCommand.ToLower().Equals("Week".ToLower()))
                {
                    dIRef.ConfigRef.ChangeRankCriteria(UserStatConfig.RankConfig.RankTimeType.week, dIRef.ContRef);
                }
                else if (stringAfterCommand.ToLower().Equals("Day".ToLower()))
                {
                    dIRef.ConfigRef.ChangeRankCriteria(UserStatConfig.RankConfig.RankTimeType.day, dIRef.ContRef);
                }
                else
                {
                    message.Channel.SendMessageAsync($@"Sorry, that was an invalid command. Not sure which one of us is the idiot.");
                    return Task.CompletedTask;
                }
                //update ranks to reflect changed rank criteria\
                dIRef.LogRef.Log("Changed rank criteria.");
                dIRef.RolesRef.AssignRoles(dIRef.GuildRef);

                message.AddReactionAsync(cH.emoteClap);

                return Task.CompletedTask;
            }
            else if (command.Equals(CommandHandler.setRankMemberLimitCommand.ToLower()))
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

                if (!(Int32.TryParse(newMemberLimitString, out newMemberLimit)))
                {
                    dIRef.LogRef.Log(new Discord.LogMessage(LogSeverity.Warning, this.ToString(), $@"Failed to change memberAmount (string not converted to int)"));
                    return Task.CompletedTask;
                }

                //if rolename is same as in rankRoles list...
                for (int index = 0; index < dIRef.RolesRef.rankRoles.Length; index++)
                {

                    if (dIRef.RolesRef.rankRoles[index].name.ToLower().Equals(roleName.ToLower()))
                    {
                        if (newMemberLimit < 0)
                        {
                            newMemberLimit = 0;
                            dIRef.LogRef.Log(new Discord.LogMessage(LogSeverity.Debug, this.ToString(), "MemberLimit amount cannot be negative. Changed to '0'"));
                        }

                        //...set new amount 
                        dIRef.RolesRef.rankRoles[index].memberLimit = newMemberLimit;
                        message.AddReactionAsync(cH.emoteClap);
                        dIRef.LogRef.Log($@"{dIRef.RolesRef.rankRoles[index].name} memberLimit changed to {newMemberLimit}.");
                    }
                }

                //update roles
                dIRef.LogRef.Log("Changed number of users in a role.");
                dIRef.RolesRef.AssignRoles(dIRef.GuildRef);
                //save roles
                dIRef.RolesRef.SaveRankRoles(dIRef.GuildRef, SaveHandler.S);

            }
            //------------------------------------------
            //--------------------------------------------------------------------------------------------------
            #endregion

            return Task.CompletedTask;
        }

    }
}
