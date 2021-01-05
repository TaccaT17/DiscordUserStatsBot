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
            helpCommand = "help",
            prefixCommand = "Prefix",
            totalChatTimeCommand = "TotalChatTime",
            totalMessagesSentCommand = "TotalMessagesSent",
            setRankTimeIntervalCommand = "SetRankTimeInterval",
            setUsersPerRankCommand = "UsersPerRank";

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

            if (command.Equals(helpCommand.ToLower()))
            {
                message.Channel.SendMessageAsync("Commands are: \n" +
                    greetCommand + " - the bot greets you\n" +
                    prefixCommand + " (<newPrefix>) - get the botPrefix or (optionally) set it\n" +
                    totalChatTimeCommand + " <username#0000> - get the given users total recorded guild time chat\n" +
                    totalMessagesSentCommand + " <username#0000> - get the users total recorded guild messages sent\n" +
                    setRankTimeIntervalCommand + " <hours> - change interval between when users are ranked and assigned the appropriate role. This command resets the timer. \n" +
                    //TODO:
                    //set number of users per given role
                    //set rank users by chat, messages or both
                    //get users rank (is rankedUsers index + 1)
                    //update user ranks
                    "");
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
                    return Task.CompletedTask;
                }
                else
                {
                    TimeSpan tP = TimeSpan.FromHours(hours);
                    myCont.ChangeAssignRolesInterval(tP);
                }

                return Task.CompletedTask;
            }
            else if (command.Equals(setUsersPerRankCommand.ToLower()))
            {
                wasBotCommand = true;

                //TODO:
                //Parse role rank and integer inputs
                //Get rank, set amount of users to integer

                return Task.CompletedTask;
            }
            //get the total voice chat time of user
            //!totalchattime <username#0000> AKA <tag> //OBSELETE: OR !totalchattime userID
            else if (command.Equals(totalChatTimeCommand.ToLower()))
            {
                wasBotCommand = true;

                string fullUserName = ReformatStringToUsername(stringAfterCommand).Result;

                //if not valid string
                if (fullUserName == null)
                {
                    message.Channel.SendMessageAsync($@"Sorry that isn't a valid username#0000. Did you remember the discriminator?");
                    return Task.CompletedTask;
                }
                else
                {
                    UserStatTracker tempUserStat = myCont.GetUserStats(fullUserName);

                    if (tempUserStat == null)
                    {
                        message.Channel.SendMessageAsync($@"Sorry there is no data on {fullUserName}.");
                        //This could possibly be a logic error: one of the two dictionaries is lacking an entry for this user

                        return Task.CompletedTask;
                    }

                    if (myCont.GetUserIDFromName(fullUserName) != 0 && (guildRef.GetUser(myCont.GetUserIDFromName(fullUserName)) != null))
                    {
                        SocketGuildUser guildUser = guildRef.GetUser(myCont.GetUserIDFromName(fullUserName));

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
            //get the total messages sent by user
            else if (command.Equals(totalMessagesSentCommand.ToLower()))
            {
                wasBotCommand = true;

                string fullUserName = ReformatStringToUsername(stringAfterCommand).Result;

                //if not valid string
                if (fullUserName == null)
                {
                    message.Channel.SendMessageAsync($@"Sorry that isn't a valid username#0000. Did you remember the discriminator?");
                    return Task.CompletedTask;
                }
                else
                {
                    UserStatTracker tempUserStat = myCont.GetUserStats(fullUserName);

                    if (tempUserStat == null)
                    {
                        message.Channel.SendMessageAsync($@"Sorry there is no data on {fullUserName}.");
                        //This could possibly be a logic error: one of the two dictionaries is lacking an entry for this user

                        return Task.CompletedTask;
                    }

                    message.Channel.SendMessageAsync($@"{tempUserStat.UsersFullName} has sent {tempUserStat.TotalMessagesSent} meaningful messages!");
                }
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
                Console.WriteLine("Invalid string. Cannot convert to a username");
            }
            return Task<string>.FromResult(fullUserName);
        }

        #endregion


    }
}
