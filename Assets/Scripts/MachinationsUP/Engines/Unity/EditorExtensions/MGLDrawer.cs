using MachinationsUP.Integration.GameObject;
using MachinationsUP.Integration.Inventory;
using UnityEditor;
using UnityEngine;

namespace MachinationsUP.Engines.Unity.EditorExtensions
{
    
    //TODO: Useless, remove me.
    [CustomEditor(typeof(MachinationsGameLayer))]
    public class MGLDrawer : Editor
    {

        override public void OnInspectorGUI ()
        {
            DrawDefaultInspector();

            MachinationsGameLayer mgl = (MachinationsGameLayer) target;
            
            if(GUILayout.Button("Emit Event"))
            {
                MachinationsGameObject mgo = new MachinationsGameObject(new MachinationsGameObjectManifest {GameObjectName = "Some Game Object"}, null);
                mgl.EmitGameEvent(mgo, "Some Event");
            }
            
            //Application.isPlaying
        }

    }
}