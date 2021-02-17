using System;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;

namespace MachinationsUP.Config
{
    
    /// <summary>
    /// Saves Machinations-specific configuration. 
    /// </summary>
    [Serializable]
    public class MnConfig
    {

        /// <summary>
        /// TRUE: Settings loaded.
        /// </summary>
        static public bool HasSettings { get; set; } = true;
        
        /// <summary>
        /// The URL where the Machinations API resides.
        /// </summary>
        public string APIURL { get; set; }

        /// <summary>
        /// User Key (API key) to use when connecting to the back-end.
        /// </summary>
        public string UserKey { get; set; }

        /// <summary>
        /// Diagram Token to make requests to.
        /// </summary>
        public string DiagramToken { get; set; }
        
        /// <summary>
        /// Game name.
        /// </summary>
        public string GameName { get; set; }

        /// <summary>
        /// Currently used configuration.
        /// </summary>
        static public MnConfig Instance { get; set; }

        /// <summary>
        /// Loads settings from XML.
        /// </summary>
        static public bool LoadSettings ()
        {
            string cacheFilePath = Path.Combine(Application.dataPath, "MachinationsSettings.xml");
            //Cannot work until Machinations Settings have been defined.
            if (!File.Exists(cacheFilePath)) return false;

            FileStream fs = new FileStream(cacheFilePath, FileMode.Open);
            XmlSerializer xs = new XmlSerializer(typeof(MnConfig));
            try
            {
                MnConfig config = (MnConfig) xs.Deserialize(fs);
                fs.Close();
                Instance = config;
            }
            catch 
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Saves settings to XML.
        /// </summary>
        static public void SaveSettings ()
        {
            if (Instance == null) Instance = new MnConfig();
            string cacheFilePath = Path.Combine(Application.dataPath, "MachinationsSettings.xml");
            FileStream fs = new FileStream(cacheFilePath, FileMode.Create);
            XmlSerializer xs = new XmlSerializer(typeof(MnConfig));
            xs.Serialize(fs, Instance);
            fs.Close();
        }

    }
}