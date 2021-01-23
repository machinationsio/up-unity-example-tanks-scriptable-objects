using MachinationsUP.Engines.Unity;
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
            //Provide the MachinationsDataLayer with an IGameLifecycleProvider.
            //This will usually be the Game Engine.
            SampleGameEngine engine = new SampleGameEngine();
            MnDataLayer.Instance.GameLifecycleProvider = engine;
            engine.MachinationsInitStart();

            L.D("SampleSceneStartupHandler OnBeforeSceneLoadRuntimeMethod.");
        }
        
    }
}