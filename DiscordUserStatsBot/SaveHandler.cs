using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using Discord.WebSocket;
using Newtonsoft.Json;


namespace DiscordUserStatsBot
{
    class SaveHandler
    {
        //VARIABLES
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        
        private string fileFolderPath;
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------

        //CONSTRUCTORS
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        public SaveHandler()
        {
            fileFolderPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + @"\saves"; //Gets path that .dll is in not path that .exe is in
        }
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------

        #region FUNCTIONS
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------

        public void SaveDictionary<K, T>(Dictionary<K, T> dictionaryToSave, string nameOfSaveFile, SocketGuild guild)
        {
            string jsonDataString = JsonConvert.SerializeObject(dictionaryToSave, Formatting.Indented);

            string saveFilePath = fileFolderPath + $@"\" + nameOfSaveFile + "_" + guild.Name + guild.Id.ToString() + ".json";

            File.WriteAllText(saveFilePath, jsonDataString);
        }

        public Dictionary<K, T> LoadDictionary<K, T>(out Dictionary<K, T> dictionaryToLoad, string nameOfSaveFile, SocketGuild guild)
        {
            string saveFilePath = fileFolderPath + $@"\" + nameOfSaveFile + "_" + guild.Name + guild.Id.ToString() + ".json";

            if (File.Exists(saveFilePath))
            {
                string jsonDataString = File.ReadAllText(saveFilePath);
                dictionaryToLoad = JsonConvert.DeserializeObject<Dictionary<K, T>>(jsonDataString);
                return dictionaryToLoad;
            }
            else
            {
                Console.WriteLine($@"Load file '{nameOfSaveFile}' doesn't exist");
                dictionaryToLoad = null;
                return dictionaryToLoad;
            }
        }

        public void SaveArray<T>(T[] arrayToSave, string nameOfSaveFile, SocketGuild guild)
        {
            string jsonDataString = JsonConvert.SerializeObject(arrayToSave, Formatting.Indented);

            string saveFilePath = fileFolderPath + $@"\" + nameOfSaveFile + "_" + guild.Name + guild.Id.ToString() + ".json";

            File.WriteAllText(saveFilePath, jsonDataString);

            //Console.WriteLine("Saved!");
        }

        public T[] LoadArray<T>(out T[] arrayToLoad, string nameOfSaveFile, SocketGuild guild)
        {
            string saveFilePath = fileFolderPath + $@"\" + nameOfSaveFile + "_" + guild.Name + guild.Id.ToString() + ".json";

            if (File.Exists(saveFilePath))
            {
                string jsonDataString = File.ReadAllText(saveFilePath);
                arrayToLoad = JsonConvert.DeserializeObject<T[]>(jsonDataString);
                //Console.WriteLine("Loaded!");
                return arrayToLoad;
            }
            else
            {
                Console.WriteLine($@"Load file '{nameOfSaveFile}' doesn't exist");
                arrayToLoad = null;
                return arrayToLoad;
            }
        }

        public void SaveObject<T>(T objectToSave, string nameOfSaveFile, SocketGuild guild)
        {
            string jsonDataString = JsonConvert.SerializeObject(objectToSave, Formatting.Indented);

            string saveFilePath = fileFolderPath + $@"\" + nameOfSaveFile + "_" + guild.Name + guild.Id.ToString() + ".json";

            File.WriteAllText(saveFilePath, jsonDataString);

            //Console.WriteLine("Saved!");
        }

        public void LoadObject<T>(out T objectToLoad, string nameOfSaveFile, SocketGuild guild)
        {
            string saveFilePath = fileFolderPath + $@"\" + nameOfSaveFile + "_" + guild.Name + guild.Id.ToString() + ".json";

            if (File.Exists(saveFilePath))
            {
                string jsonDataString = File.ReadAllText(saveFilePath);
                objectToLoad = JsonConvert.DeserializeObject<T>(jsonDataString);
                //Console.WriteLine("Loaded!");
                //return objectToLoad;
            }
            else
            {
                Console.WriteLine($@"Load file '{nameOfSaveFile}' doesn't exist");
                objectToLoad = default(T);
                //return objectToLoad;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        #endregion
    }
}
