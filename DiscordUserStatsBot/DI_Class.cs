using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordUserStatsBot
{
    //Dependency Injection - Creates and stores references to classes. 
    public class DI_Class
    {
        #region VARIABLES
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        
        private DiscordSocketClient clientRef;
        private SocketGuild guildRef;

        //BotComponents
        private LogClass logRef;
        private UserStatsBotController contRef;
        //private SaveHandler saveRef; //is a singleton
        private UserStatRoles rolesRef;
        private CommandHandler commandHandRef;
        private UserStatConfig configRef;
        private CommandExecute commandExecRef;
        
        public DiscordSocketClient Client { get { return clientRef; } }
        public SocketGuild GuildRef { get { return guildRef; } }

        public LogClass LogRef
        {
            get
            {
                if (logRef == null)
                { BuildWithDIRef(out logRef); }
                return logRef;
            }
        }
        public UserStatsBotController ContRef   { get { if (contRef == null)        BuildWithDIRef(out contRef);        return contRef; } }
        public UserStatRoles RolesRef           { get { if (rolesRef == null)       BuildWithDIRef(out rolesRef);       return rolesRef; } }
        public CommandHandler CommandHandRef    { get { if (commandHandRef == null) BuildWithDIRef(out commandHandRef); return commandHandRef; } }
        public CommandExecute CommandExecRef    { get { if (commandExecRef == null) BuildWithDIRef(out commandExecRef); return commandExecRef; } }
        public UserStatConfig ConfigRef         { get { if (configRef == null)      BuildWithDIRef(out configRef);      return configRef; } }

        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        #endregion

        //constructor
        public DI_Class(DiscordSocketClient client, SocketGuild guild)
        {
            //set instances
            clientRef = client;
            guildRef = guild;

            BuildWithDIRef(out logRef);
            logRef.Log($"Set up new guild reference to {guildRef.Name}");

            //start running bot
            BuildWithDIRef(out contRef);

            /*
            if (contRef == null)
            {
                contRef = new UserStatsBotController(this);
            }
            if (rolesRef == null)
                rolesRef = new UserStatRoles(this);
            if (commandHandRef == null)
            {
                commandHandRef = new CommandHandler(this);
                commandHandRef.wasBotCommand = false;
            }
            */
        }

        public void Reset(SocketGuild guild)
        {
            guildRef = guild;
            contRef.BotSetUp();
        }

        private T BuildWithDIRef<T>(out T classRef) where T : BotComponent, new()
        {   
            classRef = new T();

            classRef.SetDI(this);

            classRef.Init();

            return classRef;
        }
    }

    public class BotComponent
    {
        protected DI_Class dIRef;

        public void SetDI(DI_Class dI)
        {
            dIRef = dI;
        }

        virtual public void Init() { }
    }

}
