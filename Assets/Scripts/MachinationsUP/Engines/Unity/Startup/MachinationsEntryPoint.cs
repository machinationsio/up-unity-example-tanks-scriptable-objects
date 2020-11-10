using System.Timers;
using MachinationsUP.Config;
using MachinationsUP.Engines.Unity.BackendConnection;
using MachinationsUP.Engines.Unity.GameComms;
using UnityEditor;

namespace MachinationsUP.Engines.Unity.Startup
{
    /// <summary>
    /// Handles Machinations Initialization.
    /// </summary>
    static public class MachinationsEntryPoint
    {

        static private MachinationsService _machinationsService;
        static private SocketIOClient _socketClient;
        static private Timer _socketPulse;
        

        [InitializeOnLoadMethod]
        static public void InitMachinations ()
        {
            //Cannot operate until settings have been defined for Machinations.
            if (!MachinationsConfig.LoadSettings()) 
                return;
            
            _machinationsService = MachinationsDataLayer.Service = new MachinationsService();
            _socketClient = new SocketIOClient(
                _machinationsService,
                MachinationsConfig.Instance.APIURL,
                MachinationsConfig.Instance.UserKey,
                MachinationsConfig.Instance.GameName, MachinationsConfig.Instance.DiagramToken);
            _machinationsService.UseSocket(_socketClient);
            
            _socketPulse = new Timer(1000);
            _socketPulse.Elapsed += SocketPulseOnElapsed;
            _socketPulse.Start();
        }

        static private void SocketPulseOnElapsed (object sender, ElapsedEventArgs e)
        {
            _socketClient.ExecuteThread();
        }

    }
}