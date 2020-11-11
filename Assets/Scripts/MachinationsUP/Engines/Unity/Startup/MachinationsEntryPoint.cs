using MachinationsUP.Config;
using MachinationsUP.Engines.Unity.BackendConnection;
using MachinationsUP.Engines.Unity.GameComms;
using UnityEditor;
using UnityEngine;

namespace MachinationsUP.Engines.Unity.Startup
{
    /// <summary>
    /// Handles Machinations Initialization.
    /// </summary>
    static public class MachinationsEntryPoint
    {

        /// <summary>
        /// Machinations Service instance.
        /// </summary>
        /// 
        static private MachinationsService _machinationsService;

        /// <summary>
        /// SocketIOClient to use for communication with the Machinations back-end.
        /// </summary>
        static private SocketIOClient _socketClient;

        [InitializeOnLoadMethod]
        static public void InitMachinations ()
        {
            //Cannot operate until settings have been defined for Machinations.
            if (!MachinationsConfig.LoadSettings())
            {
                Debug.LogWarning(
                    "Machinations Settings do not exist. Please configure Machinations using Tools -> Machinations -> Open Machinations.io Control Panel.");
                return;
            }

            //Since Application.dataPath cannot be accessed from other threads (and we need that), storing it in MDL.
            MachinationsDataLayer.AssetsPath = Application.dataPath;
            
            //Bootstrap.
            _machinationsService = MachinationsDataLayer.Service = new MachinationsService();
            _socketClient = new SocketIOClient(
                _machinationsService,
                MachinationsConfig.Instance.APIURL,
                MachinationsConfig.Instance.UserKey,
                MachinationsConfig.Instance.DiagramToken,
                MachinationsConfig.Instance.GameName);
            _machinationsService.UseSocket(_socketClient);
        }

    }
}