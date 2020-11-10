using MachinationsUP.GameEngineAPI.Game;
using UnityEngine;

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
            Debug.Log("--- PAUSING GAME ---");
            Time.timeScale = 0;
            AudioListener.pause = true;
        }

        public void MachinationsInitComplete ()
        {
            Debug.Log("--- RESUMING GAME ---");
            Time.timeScale = 1;
            AudioListener.pause = true;
        }

        #endregion
        
    }
}