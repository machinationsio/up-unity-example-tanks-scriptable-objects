using System.Collections.Generic;
using System;
using UnityEngine;

namespace MachinationsUP.Engines.Unity.EditorExtensions
{
    
    /// <summary>
    /// When the game runs, allows Machinations to call functions in the Main Thread.
    /// </summary>
    public class MachinationsMainThreadHook : MonoBehaviour
    {
        
        private double _servicePollTime = 1; //In seconds.
        private bool _isPlaying = false;
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static private void Initialize ()
        {
            DontDestroyOnLoad(new GameObject("MachinationsMainThreadHook").AddComponent<MachinationsMainThreadHook>().gameObject);
        }

        private void Update ()
        {
            //Wait until MachinationsService is operational.
            if (MachinationsDataLayer.Service == null) return;

            //Keep MachinationsService updated regarding game state.
            if (_isPlaying != Application.isPlaying)
            {
                _isPlaying = Application.isPlaying;
                MachinationsDataLayer.Service.IsGameRunning = _isPlaying;
            }
            
            //Making sure the Service Poll time will decrease even when the game is paused.
            _servicePollTime -= Math.Max(Time.deltaTime, 0.03);
            //Make sure that Machinations Service can schedule its work from the main thread.
            if (_servicePollTime < 0)
            {
                //Debug.Log("MachinationsMainThreadHook");
                _servicePollTime = 1;
                MachinationsDataLayer.Service.ProcessSchedule();
            }
        }

    }
}