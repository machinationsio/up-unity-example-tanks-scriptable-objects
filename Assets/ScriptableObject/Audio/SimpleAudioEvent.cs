using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MachinationsUP.Engines.Unity;
using MachinationsUP.GameEngineAPI.Game;
using MachinationsUP.GameEngineAPI.GameObject;
using MachinationsUP.GameEngineAPI.States;
using MachinationsUP.Integration.Binder;
using MachinationsUP.Integration.Elements;
using MachinationsUP.Integration.GameObject;
using MachinationsUP.Integration.Inventory;
using UnityEditor;
using Random = UnityEngine.Random;

[CreateAssetMenu(menuName = "Audio Events/Simple")]
public class SimpleAudioEvent : AudioEvent, IMachinationsScriptableObject
{

    public AudioClip[] clips;

    public RangedFloat volume;

    [MinMaxRange(0, 2)] public RangedFloat pitch;

    //Machinations.

    //Tracked Machinations Elements.
    private const string M_HEALTH = "Health";
    private const string M_SPEED = "Speed";
    //Binders used to transfer information to this SO.
    private Dictionary<string, ElementBinder> _binders;
    //Manifest that defines what the SO uses from Machinations.
    static readonly private MachinationsGameObjectManifest _manifest = new MachinationsGameObjectManifest
    {
        PropertiesToSync = new List<DiagramMapping>
        {
            new DiagramMapping
            {
                GameObjectPropertyName = M_HEALTH,
                DiagramElementID = 19,
                DefaultElementBase = new ElementBase(105)
            },
            new DiagramMapping
            {
                GameObjectPropertyName = M_SPEED,
                DiagramElementID = 102,
                DefaultElementBase = new ElementBase(25)
            }
        },
        /*
        CommonStatesAssociations = new List<StatesAssociation>
        {
            new StatesAssociation("Exploring", new List<GameStates>() {GameStates.Exploring})
        }
        */
    };

    public void Awake ()
    {
        Debug.Log("SO SimpleAudioEvent Hash[" + GetHashCode() + "] Awake.");
    }

    public void OnEnable ()
    {
        Debug.Log("SO SimpleAudioEvent OnEnable.");
        MachinationsGameLayer.DeclareManifest(_manifest, this);
    }

    /// <summary>
    /// Map Machinations values to SO values.
    /// </summary>
    public void MGLInitCompleteSO ()
    {
        if (MachinationsGameLayer.IsInitialized || MachinationsGameLayer.IsInOfflineMode)
        {
            _binders = MachinationsGameLayer.CreateBindersForManifest(_manifest);
            //_binders[M_SPEED].UpdateStates(GameStates.Exploring, GameObjectStates.Undefined);
            //_binders[M_HEALTH].UpdateStates(GameStates.Exploring, GameObjectStates.Undefined);
            MGLUpdateSO();
        }
    }

    public void MGLUpdateSO (DiagramMapping diagramMapping = null, ElementBase elementBase = null)
    {
        volume.minValue = _binders[M_SPEED].Value / 100f;
        volume.maxValue = _binders[M_HEALTH].Value / 100f;
    }

    static public void UpdateFromMachinations ()
    {
        Debug.Log("On Update from Machinations");
    }

    override public void Play (AudioSource source)
    {
        if (clips.Length == 0) return;

        source.clip = clips[Random.Range(0, clips.Length)];
        source.volume = Random.Range(volume.minValue, volume.maxValue);
        source.pitch = Random.Range(pitch.minValue, pitch.maxValue);
        source.Play();
    }

}