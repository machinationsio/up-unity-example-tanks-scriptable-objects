using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml;
using MachinationsUP.Engines.Unity.GameComms;
using MachinationsUP.GameEngineAPI.Game;
using MachinationsUP.GameEngineAPI.GameObject;
using MachinationsUP.GameEngineAPI.States;
using MachinationsUP.SyncAPI;
using MachinationsUP.Integration.Binder;
using MachinationsUP.Integration.Elements;
using MachinationsUP.Integration.GameObject;
using MachinationsUP.Integration.Inventory;
using SocketIO;
using UnityEngine;

namespace MachinationsUP.Engines.Unity
{
    
    //TODO: this class name is no longer appropriate.
    /// <summary>
    /// The Machinations Game Layer is a Singleton that handles communication with the Machinations back-end.
    /// </summary>
    public class MachinationsGameLayer : MonoBehaviour, IMachiDiagram, IGameLifecycleSubscriber, IGameObjectLifecycleSubscriber
    {
        
        #region Editor-Defined
        
        /// <summary>
        /// Name of the directory where to store the Cache. If defined, then the MGL will store all values received from
        /// Machinations within this directory inside an XML file. Upon startup, if the connection to the Machinations Back-end
        /// is not operational, the Cache will be used. This system can also be used to provide versioning between different
        /// snapshots of data received from Machinations.
        /// </summary>
        public string cacheDirectoryName;
        
        #endregion

        #region Variables

        #region Public

        private IGameLifecycleProvider _gameLifecycleProvider;

        /// <summary>
        /// Used by MachinationsGameAwareObjects to query Game State.
        /// </summary>
        public IGameLifecycleProvider GameLifecycleProvider
        {
            set => _gameLifecycleProvider = value;
        }

        /// <summary>
        /// Global Event Handler for any incoming update from Machinations back-end.
        /// </summary>
        static public EventHandler OnMachinationsUpdate;

        /// <summary>
        /// Will throw exceptions if values are not found in Offline mode.
        /// </summary>
        static public bool StrictOfflineMode = false;

        #endregion

        #region Private

        /// <summary>
        /// This Dictionary contains ALL Machinations Diagram Elements that can possibly be retrieved
        /// during the lifetime of the game. This is generated based on ALL the
        /// <see cref="MachiObjectManifest"/> declared in the game.
        ///
        /// New MachinationElements are created from the ones in this Dictionary.
        ///
        /// Dictionary of the <see cref="MachinationsUP.Integration.Inventory.DiagramMapping"/> indicating where in the Diagram to find a
        /// Game Object Property Name and the <see cref="ElementBase"/> that will serve as a Source value
        /// to all values that will be created by the MachinationsGameLayer.
        /// </summary>
        static readonly private Dictionary<DiagramMapping, ElementBase> _sourceElements =
            new Dictionary<DiagramMapping, ElementBase>();

        /// <summary>
        /// Disk XML cache.
        /// See <see cref="MachinationsUP.Integration.Inventory.MCache"/>.
        /// </summary>
        static private MCache _cache = new MCache();

        /// <summary>
        /// List with of all registered MachinationsGameObject.
        /// </summary>
        static readonly private List<MachinationsGameObject> _gameObjects = new List<MachinationsGameObject>();

        /// <summary>
        /// List with of all registered MachinationsGameAwareObject.
        /// </summary>
        static readonly private List<MachinationsGameAwareObject> _gameAwareObjects = new List<MachinationsGameAwareObject>();

        /// <summary>
        /// Dictionary with Scriptable Objects and their associated Binders (per Game Object Property name).
        /// </summary>
        static readonly private Dictionary<IMachinationsScriptableObject, EnrolledScriptableObject> _scriptableObjects =
            new Dictionary<IMachinationsScriptableObject, EnrolledScriptableObject>();

        #endregion

        #endregion

        #region Implementation of IMachiSceneLayer

        public string DiagramName => "MainGame";

        public IMachinationsService MachinationsService { get; set; }
        public List<MachinationsGameObject> GameObjects => _gameObjects;
        public List<MachinationsGameAwareObject> GameAwareObjects => _gameAwareObjects;
        public Dictionary<DiagramMapping, ElementBase> SourceElements => _sourceElements;
        public Dictionary<IMachinationsScriptableObject, EnrolledScriptableObject> ScriptableObjects => _scriptableObjects;

        public void SyncComplete ()
        {
            Debug.Log("MGL.SyncComplete. MachinationsInitComplete.");
            Instance._gameLifecycleProvider?.MachinationsInitComplete();
            NotifyAboutMGLInitComplete();
            ReInitOngoing = true;
        }

        public void SyncFail ()
        {
            //Cache system active? Load Cache.
            if (!string.IsNullOrEmpty(cacheDirectoryName)) LoadCache();
        }

        /// <summary>
        /// Updates the <see cref="_sourceElements"/> with values from the Machinations Back-end. Only initializes those values
        /// that have been registered via <see cref="MachinationsUP.Integration.Inventory.DiagramMapping"/>. If entirely new
        /// values come from the back-end, will throw an Exception.
        /// </summary>
        /// <param name="elementsFromBackEnd">List of <see cref="JSONObject"/> received from the Socket IO Component.</param>
        /// <param name="updateFromDiagram">TRUE: update existing elements. If FALSE, will throw Exceptions on collisions.</param>
        public void UpdateWithValuesFromMachinations (List<JSONObject> elementsFromBackEnd, bool updateFromDiagram = false)
        {
            
            //Where should scriptable objects register?
            //Should this be bound to a certain Diagram? 
            //SOs should be allowed to get values from anywhere.
            
            //What about game objects?
            
            //They enroll and their manifests are added to Source Elements.
            //Really, currently there is no need to keep track of game objects or scriptable objects.
            //What the diagram layer cares is SOURCES. 
            
            //It also works with creating elements / binders. But this is a service-concern.
            
            //A request for more elements can come from/for multiple diagrams. The requests should not care about this.
            //They simply work with all the source elements being registered.
            
            //An object can add multiple Manifests. A Manifest is per diagram. So an object can belong to multiple diagrams.
            //So who really cares about having the diagram even represented in the game?
            //We come back to MGL actually being a MGL :).  A static location for all of a game's data.
            
            Debug.Log("MGL.UpdateWithValuesFromMachinations");
            //The response is an Array of key-value pairs PER Machination Diagram ID.
            //Each of these maps to a certain member of _sourceElements.
            foreach (JSONObject diagramElement in elementsFromBackEnd)
            {
                //Dictionary of SyncMsgs.JP_DIAGRAM_* and their value.
                var elementProperties = new Dictionary<string, string>();
                int i = 0;
                //Get all properties in a Dictionary, since their order is not guaranteed in a list.
                foreach (string machinationsPropertyName in diagramElement.keys)
                    elementProperties.Add(machinationsPropertyName, diagramElement[i++].ToString().Replace("\"", ""));

                //Find Diagram Mapping matching the provided Machinations Diagram ID.
                DiagramMapping diagramMapping = GetDiagramMappingForID(elementProperties["id"]);

                //Get the Element Base based on the dictionary of Element Properties.
                ElementBase elementBase = CreateElementFromProps(elementProperties);
                Debug.Log("ElementBase created for '" + diagramMapping + "' with Base Value of: " +
                          elementBase.BaseValue);

                //Element already exists but not in Update mode?
                if (_sourceElements[diagramMapping] != null && !updateFromDiagram)
                {   //Bark if a re-init wasn't what caused this duplication.
                    if (!ReInitOngoing)
                        throw new Exception(
                            "MGL.UpdateWithValuesFromMachinations: A Source Element already exists for this DiagramMapping: " +
                            diagramMapping +
                            ". Perhaps you wanted to update it? Then, invoke this function with update: true.");
                    //When re-initializing, go to the next element directly.
                    continue;
                }

                //This is the important line where the ElementBase is assigned to the Source Elements Dictionary, to be used for
                //cloning elements in the future.
                _sourceElements[diagramMapping] = elementBase;

                //Caching active? Update the Cache.
                if (!string.IsNullOrEmpty(cacheDirectoryName))
                {
                    diagramMapping.CachedElementBase = elementBase;
                    if (!_cache.DiagramMappings.Contains(diagramMapping)) _cache.DiagramMappings.Add(diagramMapping);
                }

                //When changes occur in the Diagram, the Machinations back-end will notify UP.
                if (updateFromDiagram) NotifyAboutMGLUpdate(diagramMapping, elementBase);
            }

            //If this was a re-initialization.
            if (ReInitOngoing)
            {
                ReInitOngoing = false;
                Debug.Log("MGL.UpdateWithValuesFromMachinations ReInitOngoing. MachinationsInitComplete.");
                //Notify Game Engine of Machinations Init Complete.
                Instance._gameLifecycleProvider?.MachinationsInitComplete();
            }

            //Send Update notification to all listeners.
            OnMachinationsUpdate?.Invoke(this, null);
            //Caching active? Save the cache now.
            if (!string.IsNullOrEmpty(cacheDirectoryName)) SaveCache();
        }
        
        #endregion
        
        #region Implementation of IGameLifecycleSubscriber

        /// <summary>
        /// Returns the current Game State.
        /// </summary>
        public GameStates CurrentGameState { get; private set; }

        /// <summary>
        /// Informs an IGameLifecycleSubscriber about a new Game State.
        /// </summary>
        /// <param name="newGameState">New Game State.</param>
        public void OnGameStateChanged (GameStates newGameState)
        {
            if (newGameState == CurrentGameState) return;
            foreach (MachinationsGameAwareObject mgao in _gameAwareObjects)
                mgao.OnGameStateChanged(newGameState);
            CurrentGameState = newGameState;
        }

        /// <summary>
        /// Informs an IGameLifecycleSubscriber that a Game Event occured.
        /// </summary>
        /// <param name="evnt"></param>
        public void OnGameEvent (string evnt)
        {
        }

        /// <summary>
        /// Machinations -> Game commands. Intended for Future use.
        /// </summary>
        /// <param name="command"></param>
        public void GameCommand (MachinationsCommands command)
        {
        }

        #endregion

        #region Implementation of IGameObjectLifecycleSubscriber

        /// <summary>
        /// Returns the current Game Object State.
        /// </summary>
        public GameObjectStates CurrentGameObjectState { get; private set; }

        /// <summary>
        /// Informs an IGameObjectLifecycleSubscriber about a new Game Object State.
        /// </summary>
        /// <param name="newGameObjectState">New Game Object State.</param>
        public void OnGameObjectStateChanged (GameObjectStates newGameObjectState)
        {
            if (newGameObjectState == CurrentGameObjectState) return;
            foreach (MachinationsGameAwareObject mgao in _gameAwareObjects)
                mgao.OnGameObjectStateChanged(newGameObjectState);
            CurrentGameObjectState = newGameObjectState;
        }

        /// <summary>
        /// Informs an IGameObjectLifecycleSubscriber that a Game Object Event occured.
        /// </summary>
        /// <param name="evnt"></param>
        public void OnGameObjectEvent (string evnt)
        {
            throw new NotSupportedException("Not supported. This is only for MachinationsGameAwareObjects.");
        }

        #endregion
        
        #region Singleton

        /// <summary>
        /// Singleton instance.
        /// </summary>
        static private MachinationsGameLayer _instance;

        /// <summary>
        /// Singleton implementation.
        /// </summary>
        static public MachinationsGameLayer Instance
        {
            get
            {
                if (_instance != null) return _instance;
                _instance = new GameObject("MachinationsGameLayer").AddComponent<MachinationsGameLayer>();
                Debug.Log("MGL created by invocation. Hash is " + _instance.GetHashCode() + " and User Key is ???????????????????!!!!!!!!!");
                //The MGL must live on.
                DontDestroyOnLoad(_instance);
                return _instance;
            }
        }

        #endregion

        #region MonoBehaviour Overrides (ENTRY POINT is in Start)

        /// <summary>
        /// UNITY-SPECIFIC FUNCTION.
        /// Awake is used to initialize any variables or game state before the game starts.
        /// Awake is called only once during the lifetime of the script instance.
        /// Awake is called after all objects are initialized so you can safely speak to other objects
        /// or query them using for example GameObject.FindWithTag.
        /// </summary>
        private void Awake ()
        {
            if (_instance == null)
            {
                //If the MGL is added to the Scene as a prefab (as it should), then this function
                //will likely execute before Instance is ever accessed. Making sure that the instance is set.
                Debug.Log("MGL instance assignment. Hash is " + GetHashCode() + " and User Key is ???????????!!!!!!!!!!!!!!");
                _instance = this;
                //Register this Scene Layer.
                MachiGlobalLayer.AddScene(_instance);
            }

            Debug.Log("MGL Awake.");
        }

        /// <summary>
        /// UNITY-SPECIFIC FUNCTION.
        /// Start is called before the first frame update.
        /// </summary>
        private void Start ()
        {
            Debug.Log("MGL.Start: Pausing game during initialization. MachinationsInitStart.");

            foreach (MachinationsGameObject mgo in _gameObjects)
                AddTargets(mgo.Manifest.GetMachinationsDiagramTargets());

            //TODO: re-think re-init concept. Bind together the two calls below in one single Sync location. 
            ReInitOngoing = true;
            Instance._gameLifecycleProvider?.MachinationsInitStart();
            Instance.MachinationsService.ScheduleSync(_instance);
        }

        #endregion

        #region Internal Functionality

        /// <summary>
        /// Notifies all enrolled <see cref="MachinationsUP.Integration.GameObject.MachinationsGameObject"/> that
        /// the MGL is now initialized.
        /// </summary>
        /// <param name="isRunningOffline">TRUE: the MGL is running in offline mode.</param>
        static private void NotifyAboutMGLInitComplete (bool isRunningOffline = false)
        {
            Debug.Log("MGL NotifyAboutMGLInitComplete.");
            //Build Binders for Scriptable Objects.
            foreach (IMachinationsScriptableObject so in _scriptableObjects.Keys)
            {
                _scriptableObjects[so].Binders = CreateBindersForScriptableObject(_scriptableObjects[so]);
                //Notify Scriptable Objects that they are ready.
                so.MGLInitCompleteSO(_scriptableObjects[so].Binders);
            }

            //_gameObjects is cloned as a new Array because the collection MAY be modified during MachinationsGameObject.MGLInitComplete.
            //That's because Game Objects MAY create other Game Objects during MachinationsGameObject.MGLInitComplete.
            //These new Game Objects will then enroll with the MGL, which will add them to _gameObjects.
            List<MachinationsGameObject> gameObjectsToNotify = new List<MachinationsGameObject>(_gameObjects.ToArray());
            //Maintain a list of Game Objects that were notified.
            List<MachinationsGameObject> gameObjectsNotified = new List<MachinationsGameObject>();
            //Maintain a list of all Game Objects that were created during the notification loop.
            List<MachinationsGameObject> gameObjectsCreatedDuringNotificationLoop = new List<MachinationsGameObject>();

            do
            {
                Debug.Log("MGL NotifyAboutMGLInitComplete gameObjectsToNotify: " + gameObjectsToNotify.Count);

                //Notify Machinations Game Objects.
                foreach (MachinationsGameObject mgo in gameObjectsToNotify)
                {
                    //Debug.Log("MGL NotifyAboutMGLInitComplete: Notifying: " + mgo);

                    //This may create new Machinations Game Objects due to them subscribing to MachinationsGameObject.OnBindersUpdated which
                    //is called on MGLInitComplete. Game Objects may create other Game Objects at that point.
                    //For an example: See Unity Example Ruby's Adventure @ EnemySpawner.OnBindersUpdated [rev f99963842e9666db3e697da5446e47cb5f1c4225]
                    mgo.MGLInitComplete(isRunningOffline);
                    gameObjectsNotified.Add(mgo);
                }

                //Debug.Log("MGL NotifyAboutMGLInitComplete gameObjectsNotified: " + gameObjectsNotified.Count);

                //Clearing our task list of objects to notify.
                gameObjectsToNotify.Clear();

                //Check if any new Game Objects were created & enrolled during the above notification loop.
                foreach (MachinationsGameObject mgo in _gameObjects)
                    if (!gameObjectsNotified.Contains(mgo))
                    {
                        //DEBT: [working on MGO lifecycle] we've commented out adding new Game Objects to gameObjectsToNotify because
                        //we want to only trigger MGLInitComplete on items that were already created. If they create other items,
                        //they will instead receive MGLReady upon Enrolling.
                        //gameObjectsToNotify.Add(mgo);

                        //Keep track of how many new objects we created during the notification loop(s).
                        gameObjectsCreatedDuringNotificationLoop.Add(mgo);
                    }

                //Debug.Log("MGL NotifyAboutMGLInitComplete NEW gameObjectsToNotify: " + gameObjectsToNotify.Count);
            }
            //New objects were created.
            while (gameObjectsToNotify.Count > 0);

            Debug.Log("MGL NotifyAboutMGLInitComplete gameObjectsCreatedDuringNotificationLoop: " +
                      gameObjectsCreatedDuringNotificationLoop.Count);
        }

        /// <summary>
        /// Concatenates a Dictionary of Unique Machination IDs and their associated ElementBase to
        /// the Game Layer's repository. All of the ElementBase in the repository will be initialized upon
        /// game startup.
        /// </summary>
        /// <param name="targets"></param>
        static private void AddTargets (Dictionary<DiagramMapping, ElementBase> targets)
        {
            foreach (DiagramMapping diagramMapping in targets.Keys)
            {
                //Only add new targets.
                if (_sourceElements.ContainsKey(diagramMapping)) continue;
                _sourceElements.Add(diagramMapping, targets[diagramMapping]);
            }
        }

        /// <summary>
        /// Creates <see cref="ElementBinder"/> for each Game Object Property provided in the <see cref="EnrolledScriptableObject"/>'s
        /// <see cref="MachiObjectManifest"/>.
        /// </summary>
        /// <returns>Dictionary of Game Object Property Name and ElementBinder.</returns>
        static private Dictionary<string, ElementBinder> CreateBindersForScriptableObject (EnrolledScriptableObject eso)
        {
            var ret = new Dictionary<string, ElementBinder>();
            MachiObjectManifest manifest = eso.Manifest;
            foreach (DiagramMapping dm in manifest.DiagramMappings)
            {
                ElementBinder eb = new ElementBinder(eso, dm); //The Binder will NOT have any Parent Game Object.
                //Ask the Binder to create Elements for all possible States Associations.
                var statesAssociations = manifest.GetStatesAssociationsForPropertyName(dm.PropertyName);
                //If no States Associations were defined.
                if (statesAssociations.Count == 0)
                    eb.CreateElementBaseForStateAssoc();
                else
                    foreach (StatesAssociation statesAssociation in statesAssociations)
                        eb.CreateElementBaseForStateAssoc(statesAssociation);
                //Save the Binder for later use.
                dm.Binder = eb;
                //Store the new Binder in the Dictionary to return.
                ret[dm.PropertyName] = eb;
            }

            return ret;
        }

        /// <summary>
        /// Returns a string that can be later decomposed in order to find an element in a Machinations Diagram.
        /// </summary>
        /// <param name="binder"></param>
        /// <param name="statesAssociation"></param>
        /// <returns></returns>
        virtual protected string GetMachinationsUniqueID (ElementBinder binder, StatesAssociation statesAssociation)
        {
            return (binder.ParentGameObject != null ? binder.ParentGameObject.Name : "!NoParent!") + "." +
                   binder.GameObjectPropertyName + "." +
                   (statesAssociation != null ? statesAssociation.Title : "N/A");
        }

        /// <summary>
        /// Finds an ElementBase within the Diagram Mappings.
        /// </summary>
        /// <param name="elementBinder">The ElementBinder that should match the ElementBase.</param>
        /// <param name="statesAssociation">The StatesAssociation to search with.</param>
        private ElementBase FindSourceElement (ElementBinder elementBinder,
            StatesAssociation statesAssociation = null)
        {
            ElementBase ret = null; //ElementBase to return.
            DiagramMapping dm = null; //Matching Diagram Mapping.
            bool found = false;
            //Search all Diagram Mappings to see which one matches the provided Binder and States Association.
            foreach (DiagramMapping diagramMapping in _sourceElements.Keys)
                //Check if we got a match with the requested Binder-per-State. If using cache, will check States Associations as Strings.
                if (diagramMapping.Matches(elementBinder, statesAssociation, HasCache))
                {
                    ret = _sourceElements[diagramMapping];
                    found = true;
                    dm = diagramMapping; //Save the Diagram Mapping that was found to match this Binder-per-State.
                    break;
                }

            //A DiagramMapping MUST have been found for this element.
            if (!found)
                throw new Exception("MGL.FindSourceElement: machinationsUniqueID '" +
                                    GetMachinationsUniqueID(elementBinder, statesAssociation) +
                                    "' not found in _sourceElements.");

            //If there is an Override defined in the DiagramMapping, immediately returning that ElementBase instead of the one we found.
            if (dm?.OverrideElementBase != null)
            {
                Debug.LogWarning("Value for " + GetMachinationsUniqueID(elementBinder, statesAssociation) + " has been overriden!");
                return dm.OverrideElementBase;
            }

            //If no ElementBase was found.
            if (ret == null)
            {
                //Search the Cache.
                if (IsInOfflineMode && HasCache)
                    foreach (DiagramMapping diagramMapping in _cache.DiagramMappings)
                        if (diagramMapping.Matches(elementBinder, statesAssociation, true))
                            return diagramMapping.CachedElementBase;

                //Nothing found? Throw!
                if (!isInOfflineMode || (IsInOfflineMode && StrictOfflineMode))
                    throw new Exception("MGL.FindSourceElement: machinationsUniqueID '" +
                                        GetMachinationsUniqueID(elementBinder, statesAssociation) +
                                        "' has not been initialized.");
            }

            return ret;
        }

        /// <summary>
        /// Goes through registered <see cref="MachinationsGameObject"/> and <see cref="IMachinationsScriptableObject"/>
        /// and notifies those Objects that are affected by the update.
        /// </summary>
        /// <param name="diagramMapping">Diagram Mapping that has been updated.</param>
        /// <param name="elementBase">The <see cref="ElementBase"/> parsed from the back-end update.</param>
        private void NotifyAboutMGLUpdate (DiagramMapping diagramMapping, ElementBase elementBase)
        {
            //Notify Scriptable Objects that are affected by what changed in this update.
            foreach (IMachinationsScriptableObject imso in _scriptableObjects.Keys)
            {
                //Find matching Binder by checking Game Object Property names.
                //Reminder: _scriptableObjects[imso] is a Dictionary of that Machinations Scriptable Object's Game Property Names.
                foreach (string gameObjectPropertyName in _scriptableObjects[imso].Binders.Keys)
                    if (gameObjectPropertyName == diagramMapping.PropertyName)
                    {
                        //TODO: must update instead of create here.
                        _scriptableObjects[imso].Binders[gameObjectPropertyName]
                            .CreateElementBaseForStateAssoc(diagramMapping.StatesAssoc, true);
                        imso.MGLUpdateSO(diagramMapping, elementBase);
                    }
            }

            //Notify all registered Machinations Game Objects, if they have 
            foreach (MachinationsGameObject mgo in _gameObjects)
                //When we find a registered Game Object that matches this Diagram Mapping asking it to update its Binder.
                if (mgo.Name == diagramMapping.Name)
                    mgo.UpdateBinder(diagramMapping, elementBase);
        }

        /// <summary>
        /// Retrieves the <see cref="DiagramMapping"/> for the requested Machinations Diagram ID.
        /// </summary>
        static private DiagramMapping GetDiagramMappingForID (string machinationsDiagramID)
        {
            DiagramMapping diagramMapping = null;
            foreach (DiagramMapping dm in _sourceElements.Keys)
                if (dm.DiagramElementID == int.Parse(machinationsDiagramID))
                {
                    return dm;
                }

            //Couldn't find any Binding for this Machinations Diagram ID.
            if (diagramMapping == null)
                throw new Exception("MGL.UpdateWithValuesFromMachinations: Got from the back-end a Machinations Diagram ID (" +
                                    machinationsDiagramID + ") for which there is no DiagramMapping.");
            return null;
        }

        /// <summary>
        /// Creates an <see cref="ElementBase"/> based on the provided properties from the Machinations Back-end.
        /// </summary>
        /// <param name="elementProperties">Dictionary of Machinations-specific properties.</param>
        private ElementBase CreateElementFromProps (Dictionary<string, string> elementProperties)
        {
            ElementBase elementBase;
            //Populate value inside Machinations Element.
            int iValue;
            string sValue;
            try
            {
                iValue = int.Parse(elementProperties["resources"]);
                elementBase = new ElementBase(iValue);
                //Set MaxValue, if we have from.
                if (elementProperties.ContainsKey("capacity") && int.TryParse(elementProperties["capacity"],
                    out iValue) && iValue != -1 && iValue != 0)
                {
                    elementBase.MaxValue = iValue;
                }
            }
            catch
            {
                sValue = elementProperties["label"];
                elementBase = new FormulaElement(sValue, false);
            }

            return elementBase;
        }

        /// <summary>
        /// Saves the Cache.
        /// </summary>
        private void SaveCache ()
        {
            string cachePath = Path.Combine(Application.dataPath, "MachinationsCache", cacheDirectoryName);
            string cacheFilePath = Path.Combine(cachePath, "Cache.xml");
            Directory.CreateDirectory(cachePath);
            Debug.Log("MGL.SaveCache using file: " + cacheFilePath);

            DataContractSerializer dcs = new DataContractSerializer(typeof(MCache));
            var settings = new XmlWriterSettings {Indent = true, NewLineOnAttributes = true};
            XmlWriter xmlWriter = XmlWriter.Create(cacheFilePath, settings);
            dcs.WriteObject(xmlWriter, _cache);
            xmlWriter.Close();
        }

        /// <summary>
        /// Loads the Cache located at <see cref="cacheDirectoryName"/>. Applies it over <see cref="_sourceElements"/>.
        /// </summary>
        private void LoadCache ()
        {
            string cacheFilePath = Path.Combine(Application.dataPath, "MachinationsCache", cacheDirectoryName, "Cache.xml");
            if (!File.Exists(cacheFilePath))
            {
                Debug.Log("MGL.LoadCache DOES NOT EXIST: " + cacheFilePath);
                _cache = null;
                return;
            }

            Debug.Log("MGL.LoadCache using file: " + cacheFilePath);

            //Deserialize Cache.
            DataContractSerializer dcs = new DataContractSerializer(typeof(MCache));
            FileStream fs = new FileStream(cacheFilePath, FileMode.Open);
            _cache = (MCache) dcs.ReadObject(fs);

            //Applying Cache.
            foreach (DiagramMapping dm in _cache.DiagramMappings)
            {
                //Cloning list elements because we'll be tampering with the collection.
                List<DiagramMapping> sourceKeys = new List<DiagramMapping>(_sourceElements.Keys.ToArray());
                for (int i = 0; i < sourceKeys.Count; i++)
                {
                    DiagramMapping dms = sourceKeys[i];
                    if (dms.Matches(dm, true))
                        _sourceElements[dms] = dm.CachedElementBase;
                }
            }

            //Running in offline mode now.
            IsInOfflineMode = true;
            OnMachinationsUpdate?.Invoke(this, null);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Registers a <see cref="MachiObjectManifest"/> to make sure that during Initialization, the MGL
        /// (aka <see cref="MachinationsGameLayer"/> retrieves all the Manifest's necessary data so that
        /// any Game Objects that use this Manifest can query the MGL for the needed values.
        /// </summary>
        static public void DeclareManifest (MachiObjectManifest manifest)
        {
            Debug.Log("MGL DeclareManifest: " + manifest);
            //Add all of this Manifest's targets to the list that we will have to Initialize & monitor.
            AddTargets(manifest.GetMachinationsDiagramTargets());
        }

        /// <summary>
        /// Registers a <see cref="IMachinationsScriptableObject"/> along with its Manifest.
        /// This is used to make sure that all Game Objects are ready for use after MGL Initialization.
        /// </summary>
        /// <param name="imso">The IMachinationsScriptableObject to add.</param>
        /// <param name="manifest">Its <see cref="MachiObjectManifest"/>.</param>
        static public void EnrollScriptableObject (IMachinationsScriptableObject imso,
            MachiObjectManifest manifest)
        {
            Debug.Log("MGL EnrollScriptableObject: " + manifest);
            DeclareManifest(manifest);
            if (!_scriptableObjects.ContainsKey(imso))
                _scriptableObjects[imso] = new EnrolledScriptableObject {MScriptableObject = imso, Manifest = manifest};
            
            //TODO: re-design sync system around startup cycle.
            //Schedule a sync for every new addition.
            if (_instance != null)
                _instance.MachinationsService.ScheduleSync(_instance);
        }

        /// <summary>
        /// Registers a MachinationsGameObject that the Game Layer can keep track of.
        /// </summary>
        /// <param name="machinationsGameObject"></param>
        static public void EnrollGameObject (MachinationsGameObject machinationsGameObject)
        {
            _gameObjects.Add(machinationsGameObject);
            if (machinationsGameObject is MachinationsGameAwareObject gameAwareObject)
                _gameAwareObjects.Add(gameAwareObject);
            //WARN: This may crash due to recent refactoring.
            machinationsGameObject.MGLInitComplete();

            //Schedule a sync for every new addition.
            if (_instance != null)
                _instance.MachinationsService.ScheduleSync(_instance);
        }

        /// <summary>
        /// Creates an <see cref="MachinationsUP.Integration.Elements.ElementBase"/>.
        /// </summary>
        /// <param name="elementBinder">The <see cref="MachinationsUP.Integration.Binder.ElementBinder"/> for which
        /// the <see cref="MachinationsUP.Integration.Elements.ElementBase"/> is to be created.</param>
        /// <param name="statesAssociation">OPTIONAL. The StatesAssociation for which the ElementBase is to be created.
        /// If this is not provided, the default value of NULL means that the ElementBase will use "N/A" as Title
        /// in the <see cref="MachinationsUP.Engines.Unity.MachinationsGameLayer"/> Init Request.</param>
        public ElementBase CreateElement (ElementBinder elementBinder, StatesAssociation statesAssociation = null)
        {
            ElementBase sourceElement = FindSourceElement(elementBinder, statesAssociation);
            //Not found elements are accepted in Offline Mode.
            if (sourceElement == null)
            {
                //Offline mode allows not finding a Source Element.
                if (isInOfflineMode) return null;
                throw new Exception("MGL.CreateElement: Unhandled null Source Element.");
            }

            //Initialize the ElementBase by cloning it from the sourceElement.
            var newElement = sourceElement.Clone(elementBinder);

            Debug.Log("MGL.CreateValue complete for machinationsUniqueID '" +
                      GetMachinationsUniqueID(elementBinder, statesAssociation) + "'.");

            return newElement;
        }

        /// <summary>
        /// Returns the Source <see cref="MachinationsUP.Integration.Elements.ElementBase"/> found at the requested DiagramMapping.
        /// If Offline & caching active, will return the value previously loaded from the Cache.
        /// If Offline & caching inactive, returns the DefaultElementBase, if any was defined.
        /// Throws if cannot find any Source ElementBase.
        /// </summary>
        /// <param name="diagramMapping"><see cref="MachinationsUP.Integration.Inventory.DiagramMapping"/> whose
        /// <see cref="MachinationsUP.Integration.Elements.ElementBase"/> to return.</param>
        /// <returns></returns>
        static public ElementBase GetSourceElementBase (DiagramMapping diagramMapping)
        {
            if (_sourceElements.ContainsKey(diagramMapping) && _sourceElements[diagramMapping] != null)
                return _sourceElements[diagramMapping];
            //When Offline, we may return the Default Element Base.
            if (IsInOfflineMode && diagramMapping.DefaultElementBase != null)
            {
                Debug.Log("MGL Returning DefaultElementBase for " + diagramMapping);
                return diagramMapping.DefaultElementBase;
            }

            throw new Exception("MGL.GetSourceElementBase: cannot find any Source Element Base for " + diagramMapping);
        }

        static public void PerformInitRequest ()
        {
            //Notify Game Engine of Machinations Init Start.
            Debug.Log("MGL.PerformInitRequest: Pausing game during initialization. MachinationsInitStart.");
            Instance._gameLifecycleProvider?.MachinationsInitStart();
            //Instance.EmitMachinationsInitRequest();
            //Make sure that when the Init request will be handled as re-initialization.
            ReInitOngoing = true;
        }

        #endregion

        #region Properties

        static private bool isInOfflineMode;

        /// <summary>
        /// MGL is running in offline mode.
        /// </summary>
        static public bool IsInOfflineMode
        {
            set
            {
                isInOfflineMode = value;
                if (value)
                {
                    Debug.Log("MachinationsGameLayer is now in Offline Mode!");
                    NotifyAboutMGLInitComplete(isInOfflineMode);
                }
            }
            get => isInOfflineMode;
        }

        /// <summary>
        /// TRUE: a re-initialization request is ongoing.
        /// </summary>
        static private bool ReInitOngoing { get; set; }

        /// <summary>
        /// TRUE: new elements have been added.
        /// </summary>
        static private bool NewElementsAdded { get; set; }

        /// <summary>
        /// Returns the current Game State, if any <see cref="IGameLifecycleProvider"/> is avaialble.
        /// </summary>
        /// <returns></returns>
        static public GameStates GetGameState ()
        {
            if (Instance._gameLifecycleProvider == null)
                throw new Exception("MGL no IGameLifecycleProvider available.");
            return Instance._gameLifecycleProvider.GetGameState();
        }

        /// <summary>
        /// Returns if the MGL has any cache loaded.
        /// </summary>
        static public bool HasCache => !string.IsNullOrEmpty(Instance.cacheDirectoryName) && _cache != null;

        #endregion

    }
}