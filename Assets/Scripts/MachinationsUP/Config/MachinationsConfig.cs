using System;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;

namespace MachinationsUP.Config
{
    [Serializable]
    public class MachinationsConfig
    {

        public string APIURL { get; set; }

        public string UserKey { get; set; }

        public string GameName { get; set; }

        public string DiagramToken { get; set; }

        static public MachinationsConfig Instance { get; private set; }

        static public bool LoadSettings ()
        {
            string cacheFilePath = Path.Combine(Application.dataPath, "MachinationsSettings.xml");
            if (!File.Exists(cacheFilePath))
            {
                Debug.LogWarning("Machinations Settings do not exist. Please configure Machinations using Tools -> Machinations -> Open Machinations.io Control Panel.");
                return false;
            }

            FileStream fs = new FileStream(cacheFilePath, FileMode.Open);
            XmlSerializer xs = new XmlSerializer(typeof(MachinationsConfig));
            MachinationsConfig config = (MachinationsConfig) xs.Deserialize(fs);
            fs.Close();
            Instance = config;
            return true;
        }

        static public void SaveSettings ()
        {
            if (Instance == null) Instance = new MachinationsConfig();
            string cacheFilePath = Path.Combine(Application.dataPath, "MachinationsSettings.xml");
            FileStream fs = new FileStream(cacheFilePath, FileMode.Create);
            XmlSerializer xs = new XmlSerializer(typeof(MachinationsConfig));
            xs.Serialize(fs, Instance);
            fs.Close();
        }

    }
}