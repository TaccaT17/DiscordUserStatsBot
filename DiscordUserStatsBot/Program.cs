//Copyright Tom Crammond 2020

using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;


namespace DiscordUserStatsBot
{

    //My first bot
    /*
        public class Program
        {
            public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

            private DiscordSocketClient _client; //         <--------------------------------THIS IS YOUR REFERENCE TO EVERYTHING

            //private bool devMode = true;

            //TODO: make these generic
            private const ulong GUILD_ID = 767845719570382900;
            private const ulong DEBUG_TEXT_CHANNEL_ID = 767845719570382903;
            private const ulong DEBUG_VOICE_CHANNEL_ID = 767845719570382904;



            private SocketGuild guildRef;
            private SocketTextChannel debugTextChannelRef;
            private SocketVoiceChannel debugVoiceChannelRef;

            private char botCommandPrefix = '!';


            public async Task MainAsync()
            {
                _client = new DiscordSocketClient();

                //adds CommandHandler func to MessageRecieved delegate. Therefore CommandHandler will be executed anytime a message is posted on the discord server
                _client.MessageReceived += CommandHandler;
                _client.UserVoiceStateUpdated += VoiceChatChange;
                _client.Ready += BotSetUp; //Ready is fired when the bot connects to server

                //TODO:
                //_client.JoinedGuild += BotSetUp;

                _client.Log += Log;

                //discord people/bots/objects have a "token" AKA ID that is a password/username
                // not secure to hardcode token so instead will get it from saved file (under TomsDiscordBot->bin->Debug->netcoreapp3.1)
                var token = File.ReadAllText("token.txt");

                await _client.LoginAsync(TokenType.Bot, token);
                await _client.StartAsync();

                // Block this task until the program is closed.
                await Task.Delay(-1);
            }

            private Task Log(LogMessage msg)
            {
                Console.WriteLine(msg.ToString());
                return Task.CompletedTask;
            }


            //TODO:
            //called when bot either joins guild, when bot comes online, . 
            //Will check to see if bot set up and up to date. If not will update.
            private Task BotSetUp()
            {
                //Let people know bot updating
                Console.WriteLine("BotBeing set up");

                //get guild(AKA server)
                if(guildRef == null)
                {
                    guildRef = _client.GetGuild(GUILD_ID);
                    Console.WriteLine($"Set up new guild reference to {guildRef.Name}");
                }

                //get debug channels
                debugTextChannelRef = guildRef.GetTextChannel(DEBUG_TEXT_CHANNEL_ID);
                debugVoiceChannelRef = guildRef.GetVoiceChannel(DEBUG_VOICE_CHANNEL_ID);


                //make dictionary of channels where each key is the name of the channel
                //Dictionary<string, SocketGuildChannel> channels = new Dictionary<string, SocketGuildChannel>();

                //foreach(SocketGuildChannel channel in guildRef.Channels)
                //{
                //    channels.Add(channel.Name, channel);
                //}




                return Task.CompletedTask;
            }



            //HANDLES ANY COMMANDS GIVEN TO IT BY USER
            private Task CommandHandler(SocketMessage message)
            {            
                //FILTER OUT MESSAGES WE DONT WANT TO ANALYZE
                //--------------------------------------------------------------------------------------------------
                //rule out messages that don't have bot prefix
                if (!message.Content.StartsWith(botCommandPrefix))                   //BOT PREFIX
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

                //GET LOWERCASE MESSAGE STRING
                //--------------------------------------------------------------------------------------------------
                string command = "";
                int lengthOfCommand = -1;

                //Only will take first word of command
                if(message.Content.Contains(' '))
                {
                    lengthOfCommand = message.Content.IndexOf(' ');
                }
                else
                {
                    lengthOfCommand = message.Content.Length;
                }

                //                        Substring: you take specified section of string recieved. ToLower: makes string lowercase
                command = message.Content.Substring(1, lengthOfCommand - 1).ToLower();

                //--------------------------------------------------------------------------------------------------

                //COMMANDS 
                //REMEMBER: NO SPACES ALLOWED IN COMMANDS
                //--------------------------------------------------------------------------------------------------

                if(command.Equals("welcome"))
                {
                    //                              $@ lets you put commands in string instead of doing "Hello " + message.Author.mention + "."
                    //                                                                                                              ^mention means will "@" the user
                    message.Channel.SendMessageAsync($@"Hello {message.Author.Mention}. As my creator I am at your service.");
                }

                else if (command.Equals("age"))
                {
                    //$: lets you put code inside string
                    //@ will take the string "verbatim" AKA instead of using \n you can just hit enter
                    message.Channel.SendMessageAsync($@"Your account was made {message.Author.CreatedAt.DateTime.Date}");
                }

                else if (command.Equals("muwahahahaha"))
                {
                    //                                  this is shorthand for "\:emojiName:emojiId". You get it by typing \emoji in discord channel. As far as I can tell this is the only way to get the ID
                    //                                  could also use "new Emoji ("\U0001F608")" AKA the unicode
                    message.AddReactionAsync(new Emoji("😈"));

                    //message.Channel.SendMessageAsync($@"Message activity is {message.Activity}.");
                    //message.Channel.SendMessageAsync($@"Message application is {message.Application}.");
                    //message.Channel.SendMessageAsync($@"Message source is {message.Source}.");



                }

                //--------------------------------------------------------------------------------------------------
                return Task.CompletedTask;
            }


            private Task VoiceChatChange(SocketUser user, SocketVoiceState PreviousVoiceChat, SocketVoiceState CurrentVoiceChat)
            {

                if (CurrentVoiceChat.VoiceChannel != null)
                {
                    UserJoinedAVoiceChat(user);
                }
                else
                {
                    UserLeftAllVoiceChats(user);
                }


                return Task.CompletedTask;
            }

            private void UserJoinedAVoiceChat(SocketUser user)
            {
                //debugTextChannelRef.SendMessageAsync($"{user.Username} joined a chat!");

                //Record the time the player entered chat 

            }

            private void UserLeftAllVoiceChats(SocketUser user)
            {
                //debugTextChannelRef.SendMessageAsync($"{user.Username} left all voice chats.");

                //stop the timer for this user

            }


         }


        */
}

