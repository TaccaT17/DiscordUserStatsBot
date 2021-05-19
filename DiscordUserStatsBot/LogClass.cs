using Discord;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DiscordUserStatsBot
{
    public class LogClass : BotComponent
    {
        bool devLogs = true;

        public void Log(string msg)
        {
            string outMsg = new LogMessage(LogSeverity.Info, this.ToString(), msg).ToString();
            LogOut(outMsg);
        }

        public void Log(LogMessage msg)
        {
            //ignore debugs if not in devmode
            if (msg.Severity.Equals(LogSeverity.Debug) && !devLogs)
            {
                return;
            }

            string outMsg = msg.ToString();
            LogOut(outMsg);
        }

        private void LogOut(string outMsg)
        {
            string guildName = $"{dIRef.GuildRef.Name} - ";

            string output = String.Format("{0,-30}{1,-10}", guildName, outMsg);

            using (System.IO.StreamWriter file =
            new System.IO.StreamWriter(MainClass.FilePath + Path.DirectorySeparatorChar + @"logs.txt", true))
            {
                file.WriteLine(output);
            }

            Console.WriteLine(output);
            return;
        }
    }
}
