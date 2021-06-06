﻿using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine;

namespace MachinationsUP.Engines.Unity.EditorExtensions
{
    /// <summary>
    /// When the game runs, allows Machinations to call functions in the Main Thread.
    /// </summary>
    public class MnMainThreadHook : MonoBehaviour
    {

        private double _servicePollTime = 1; //In seconds.
        private bool _isPlaying;
        private bool _hookedPlayModeEvent;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static private void Initialize ()
        {
            DontDestroyOnLoad(new GameObject("MachinationsMainThreadHook").AddComponent<MnMainThreadHook>().gameObject);
        }

        private void EditorApplicationOnplayModeStateChanged (PlayModeStateChange obj)
        {
            if (obj == PlayModeStateChange.EnteredEditMode || obj == PlayModeStateChange.ExitingPlayMode)
            {
                _isPlaying = false;
                MnDataLayer.Service.IsGameRunning = _isPlaying;
                Debug.Log("Changed playstate to: " + _isPlaying);
            }
        }

        private void Update ()
        {
            //Wait until MachinationsService is operational.
            if (MnDataLayer.Service == null) return;

            //Keep MachinationsService updated regarding game state.
            if (_isPlaying != Application.isPlaying)
            {
                _isPlaying = Application.isPlaying;
                MnDataLayer.Service.IsGameRunning = _isPlaying;
            }

            //Making sure the Service Poll time will decrease even when the game is paused.
            _servicePollTime -= Math.Max(Time.deltaTime, 0.03);
            //Make sure that Machinations Service can schedule its work from the main thread.
            if (_servicePollTime < 0)
            {
                //L.D("MachinationsMainThreadHook");
                _servicePollTime = 1;
                MnDataLayer.Service.ProcessSchedule();
            }

            if (!_hookedPlayModeEvent)
            {
                _hookedPlayModeEvent = true;
                EditorApplication.playModeStateChanged += EditorApplicationOnplayModeStateChanged;
            }
        }

    }
}