﻿using MachinationsUP.Engines.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;
using MachinationsUP.Logger;

namespace MachinationsUP.ExampleGames.MachinationsSupport
{
    /// <summary>
    /// Handles first-time initialization of a Scene.
    /// </summary>
    public class SampleSceneStartupHandler
    {

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnBeforeSceneLoadRuntimeMethod ()
        {
            QualitySettings.vSyncCount = 0; // VSync must be disabled
            Application.targetFrameRate = 20;

            L.D("SampleSceneStartupHandler OnBeforeSceneLoadRuntimeMethod.");
            //Get notifications about Scene Loads.
            SceneManager.sceneLoaded += SceneManagerOnsceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void OnAfterSceneLoadRuntimeMethod ()
        {
            L.D("SampleSceneStartupHandler OnAfterSceneLoadRuntimeMethod.");
        }

        static private void SceneManagerOnsceneLoaded (Scene arg0, LoadSceneMode arg1)
        {
            L.D("SampleSceneStartupHandler SceneManagerOnsceneLoaded CompleteMainScene.");
            //Provide the MachinationsGameLayer with an IGameLifecycleProvider.
            //This will usually be the Game Engine.
            MachinationsDataLayer.Instance.GameLifecycleProvider = new SampleGameEngine();
        }

        [RuntimeInitializeOnLoadMethod]
        static void OnRuntimeMethodLoad ()
        {
            L.D("SampleSceneStartupHandler OnRuntimeMethodLoad.");
        }

    }
}