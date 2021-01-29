using System.IO;
using MachinationsUP.Config;
using MachinationsUP.Engines.Unity.BackendConnection;
using MachinationsUP.Engines.Unity.GameComms;
using MachinationsUP.Logger;
using UnityEditor;
using UnityEngine;

namespace MachinationsUP.Engines.Unity.Startup
{
    /// <summary>
    /// Handles Machinations Initialization.
    /// </summary>
    static public class MnEntryPoint
    {

        /// <summary>
        /// Machinations Service instance.
        /// </summary>
        static private MnService _mnService;

        /// <summary>
        /// SocketIOClient to use for communication with the Machinations back-end.
        /// </summary>
        static private SocketIOClient _socketClient;

        [InitializeOnLoadMethod]
        static public void InitMachinations ()
        {
            L.Level = LogLevel.Trace;
            L.LogFilePath = Application.dataPath + "//MachinationsService-Error.log";
            
            //Since Application.dataPath cannot be accessed from other threads (and we need that), storing it in MDL.
            MnDataLayer.AssetsPath = Application.dataPath;
            
            //Cannot operate until settings have been defined for Machinations.
            if (!MnConfig.LoadSettings())
            {
                MnConfig.HasSettings = false;
                L.W("Machinations Settings do not exist. Please configure Machinations using Tools -> Machinations -> Open Machinations.io Control Panel.");
                return;
            }
            
            //Bootstrap.
            _mnService = MnDataLayer.Service = new MnService();
            _socketClient = new SocketIOClient(
                _mnService,
                MnConfig.Instance.APIURL,
                MnConfig.Instance.UserKey,
                MnConfig.Instance.DiagramToken,
                MnConfig.Instance.GameName);
            _mnService.UseSocket(_socketClient);
        }

    }
}