using MachinationsUP.Engines.Unity;
using MachinationsUP.GameEngineAPI.Game;
using UnityEngine;
using MachinationsUP.Logger;

namespace MachinationsUP.ExampleGames.MachinationsSupport
{
    /// <summary>
    /// Sample Game Engine class.
    /// </summary>
    public class SampleGameEngine : IGameLifecycleProvider
    {

        #region Implementation of IGameLifecycleProvider
        
        public GameStates GetGameState ()
        {
            return GameStates.Exploring;
        }

        public void MachinationsInitStart ()
        {
            L.D("--- PAUSING GAME ---");
            //Won't touch anything if the game isn't even in running state.
            if (!MnDataLayer.Service.IsGameRunning) return;
            AudioListener.pause = true;
            Time.timeScale = 0;
        }

        public void MachinationsInitComplete ()
        {
            L.D("--- RESUMING GAME ---");
            //Won't touch anything if the game isn't even in running state.
            if (!MnDataLayer.Service.IsGameRunning) return;
            AudioListener.pause = true;
            Time.timeScale = 1;
        }

        #endregion
        
    }
}