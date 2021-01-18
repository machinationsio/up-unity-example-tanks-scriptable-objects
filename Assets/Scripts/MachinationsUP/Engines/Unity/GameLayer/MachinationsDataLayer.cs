﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml;
using MachinationsUP.Config;
using MachinationsUP.Engines.Unity.GameComms;
using MachinationsUP.GameEngineAPI.Game;
using MachinationsUP.GameEngineAPI.GameObject;
using MachinationsUP.GameEngineAPI.States;
using MachinationsUP.SyncAPI;
using MachinationsUP.Integration.Binder;
using MachinationsUP.Integration.Elements;
using MachinationsUP.Integration.GameObject;
using MachinationsUP.Integration.Inventory;
using MachinationsUP.Logger;
using UnityEditor;

namespace MachinationsUP.Engines.Unity
{
    /// <summary>
    /// The Machinations Game Layer is a Singleton that handles communication with the Machinations back-end.
    /// </summary>
    public class MachinationsDataLayer : IGameLifecycleSubscriber, IGameObjectLifecycleSubscriber
    {

        #region Editor-Defined

        /// <summary>
        /// Name of the directory where to store the Cache. If defined, then the MDL will store all values received from
        /// Machinations within this directory inside an XML file. Upon startup, if the connection to the Machinations Back-end
        /// is not operational, the Cache will be used. This system can also be used to provide versioning between different
        /// snapshots of data received from Machinations.
        /// TODO: add versioning in tandem with Machinations.
        /// </summary>
        private string cacheDirectoryName = "YourCache";

        #endregion

        #region Variables

        #region Public

        /// <summary>
        /// Machinations Service to use when dealing with the Back-end.
        /// </summary>
        static public MachinationsService Service;

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
        /// <see cref="MachinationsObjectManifest"/> declared in the game.
        ///
        /// New MachinationElements are created from the ones in this Dictionary.
        ///
        /// Dictionary of the <see cref="MachinationsUP.Integration.Inventory.DiagramMapping"/> indicating where in the Diagram to find a
        /// Game Object Property Name and the <see cref="ElementBase"/> that will serve as a Source value
        /// to all values that will be created by the MachinationsDataLayer.
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

        static public void SyncComplete ()
        {
            L.D("MDL.SyncComplete. MachinationsInitComplete.");
            NotifyAboutMGLInitComplete();
            Instance._gameLifecycleProvider?.MachinationsInitComplete();
            ReInitOngoing = true;
        }

        static public void SyncFail (bool loadCache)
        {
            //Cache system active? Load Cache.
            if (loadCache && !string.IsNullOrEmpty(Instance.cacheDirectoryName)) LoadCache();
        }

        static public JSONObject GetInitRequestData (string diagramToken)
        {
            //Init Request components will be stored as top level items in this Dictionary.
            var initRequest = new Dictionary<string, JSONObject>();
            bool noItems = true; //TRUE: nothing to get Init Request for.

            //Create individual JSON Objects for each Machination element to retrieve.
            //This is an Array because this is what the JSON Object Library expects.
            JSONObject[] keys = new JSONObject [_sourceElements.Keys.Count];
            int i = 0;
            foreach (DiagramMapping diagramMapping in _sourceElements.Keys)
            {
                if (_sourceElements[diagramMapping] != null)
                {
                    L.D("Skipping requsting info for already initialized Source Element: " + _sourceElements[diagramMapping]);
                    continue;
                }

                noItems = false;

                var item = new Dictionary<string, JSONObject>();
                item.Add("id", new JSONObject(diagramMapping.DiagramElementID));
                //Create JSON Objects for all props that we have to retrieve.
                string[] sprops =
                {
                    SyncMsgs.JP_DIAGRAM_LABEL, SyncMsgs.JP_DIAGRAM_ACTIVATION, SyncMsgs.JP_DIAGRAM_ACTION,
                    SyncMsgs.JP_DIAGRAM_RESOURCES, SyncMsgs.JP_DIAGRAM_CAPACITY, SyncMsgs.JP_DIAGRAM_OVERFLOW
                };
                List<JSONObject> props = new List<JSONObject>();
                foreach (string sprop in sprops)
                    props.Add(JSONObject.CreateStringObject(sprop));
                //Add props field.
                item.Add("props", new JSONObject(props.ToArray()));

                keys[i++] = new JSONObject(item);
            }

            if (noItems) return null;

            //Finalize request by adding all top level items.
            initRequest.Add(SyncMsgs.JK_AUTH_DIAGRAM_TOKEN, JSONObject.CreateStringObject(diagramToken));
            //Wrapping the keys Array inside a JSON Object.
            initRequest.Add(SyncMsgs.JK_INIT_MACHINATIONS_IDS, new JSONObject(keys));

            return new JSONObject(initRequest);
        }

        /// <summary>
        /// Updates the <see cref="_sourceElements"/> with values from the Machinations Back-end. Only initializes those values
        /// that have been registered via <see cref="MachinationsUP.Integration.Inventory.DiagramMapping"/>. If entirely new
        /// values come from the back-end, will throw an Exception.
        /// </summary>
        /// <param name="elementsFromBackEnd">List of <see cref="JSONObject"/> received from the Socket IO Component.</param>
        /// <param name="updateFromDiagram">TRUE: update existing elements. If FALSE, will throw Exceptions on collisions.</param>
        static public void UpdateSourcesWithValuesFromMachinations (List<JSONObject> elementsFromBackEnd, bool updateFromDiagram = false)
        {
            L.D("MDL.UpdateWithValuesFromMachinations");
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
                ElementBase elementBase = CreateSourceElementBaseFromProps(elementProperties, diagramMapping);
                L.D("ElementBase created for '" + diagramMapping + "' with Base Value of: " +
                    elementBase.BaseValue);

                //Element already exists but not in Update mode?
                if (_sourceElements[diagramMapping] != null && !updateFromDiagram)
                {
                    //Bark if a re-init wasn't what caused this duplication.
                    if (!ReInitOngoing)
                        throw new Exception(
                            "MDL.UpdateWithValuesFromMachinations: A Source Element already exists for this DiagramMapping: " +
                            diagramMapping +
                            ". Perhaps you wanted to update it? Then, invoke this function with update: true.");
                    //When re-initializing, go to the next element directly.
                    continue;
                }

                //This is the important line where the ElementBase is assigned to the Source Elements Dictionary, to be used for
                //cloning elements in the future.
                _sourceElements[diagramMapping] = elementBase;

                //Caching active? Update the Cache.
                if (!string.IsNullOrEmpty(Instance.cacheDirectoryName))
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
                L.D("MDL.UpdateWithValuesFromMachinations ReInitOngoing. MachinationsInitComplete.");
                //Notify Game Engine of Machinations Init Complete.
                Instance._gameLifecycleProvider?.MachinationsInitComplete();
            }

            //Send Update notification to all listeners.
            OnMachinationsUpdate?.Invoke(Instance, null);
            //Caching active? Save the cache now.
            if (!string.IsNullOrEmpty(Instance.cacheDirectoryName)) SaveCache();
        }

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
        static private MachinationsDataLayer _instance;

        /// <summary>
        /// Singleton implementation.
        /// </summary>
        static public MachinationsDataLayer Instance
        {
            get
            {
                if (_instance != null) return _instance;
                _instance = new MachinationsDataLayer();
                L.D("MDL created by invocation. Hash is " + _instance.GetHashCode() +
                    " and User Key is ???????????????????!!!!!!!!!");
                return _instance;
            }
        }

        #endregion

        static public void Prepare ()
        {
            L.D("MDL.Start: Pausing game during initialization. MachinationsInitStart.");

            foreach (MachinationsGameObject mgo in _gameObjects)
                AddTargets(mgo.Manifest.GetMachinationsDiagramTargets());

            //TODO: re-think re-init concept. Bind together the two calls below in one single Sync location. 
            ReInitOngoing = true;
            Instance._gameLifecycleProvider?.MachinationsInitStart();
            Service.ScheduleSync();

            L.D("MDL Awake.");
        }

        #region Internal Functionality

        /// <summary>
        /// Notifies all enrolled <see cref="MachinationsUP.Integration.GameObject.MachinationsGameObject"/> that
        /// the MDL is now initialized.
        /// </summary>
        /// <param name="isRunningOffline">TRUE: the MDL is running in offline mode.</param>
        static private void NotifyAboutMGLInitComplete (bool isRunningOffline = false)
        {
            L.D("MDL NotifyAboutMGLInitComplete.");
            L.D("MDL NotifyAboutMGLInitComplete scriptableObjectsToNotify: " + _scriptableObjects.Keys.Count);
            //Build Binders for Scriptable Objects.
            foreach (IMachinationsScriptableObject so in _scriptableObjects.Keys)
            {
                _scriptableObjects[so].Binders = CreateBindersForScriptableObject(_scriptableObjects[so]);
                //Notify Scriptable Objects that they are ready.
                so.MGLInitCompleteSO(_scriptableObjects[so].Binders);
            }

            //_gameObjects is cloned as a new Array because the collection MAY be modified during MachinationsGameObject.MGLInitComplete.
            //That's because Game Objects MAY create other Game Objects during MachinationsGameObject.MGLInitComplete.
            //These new Game Objects will then enroll with the MDL, which will add them to _gameObjects.
            List<MachinationsGameObject> gameObjectsToNotify = new List<MachinationsGameObject>(_gameObjects.ToArray());
            //Maintain a list of Game Objects that were notified.
            List<MachinationsGameObject> gameObjectsNotified = new List<MachinationsGameObject>();
            //Maintain a list of all Game Objects that were created during the notification loop.
            List<MachinationsGameObject> gameObjectsCreatedDuringNotificationLoop = new List<MachinationsGameObject>();

            do
            {
                L.D("MDL NotifyAboutMGLInitComplete gameObjectsToNotify: " + gameObjectsToNotify.Count);

                //Notify Machinations Game Objects.
                foreach (MachinationsGameObject mgo in gameObjectsToNotify)
                {
                    L.D("MDL NotifyAboutMGLInitComplete: Notifying: " + mgo);

                    //This may create new Machinations Game Objects due to them subscribing to MachinationsGameObject.OnBindersUpdated which
                    //is called on MGLInitComplete. Game Objects may create other Game Objects at that point.
                    //For an example: See Unity Example Ruby's Adventure @ EnemySpawner.OnBindersUpdated [rev f99963842e9666db3e697da5446e47cb5f1c4225]
                    mgo.MGLInitComplete(isRunningOffline);
                    gameObjectsNotified.Add(mgo);
                }

                L.D("MDL NotifyAboutMGLInitComplete gameObjectsNotified: " + gameObjectsNotified.Count);

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

                L.D("MDL NotifyAboutMGLInitComplete NEW gameObjectsToNotify: " + gameObjectsToNotify.Count);
            }
            //New objects were created.
            while (gameObjectsToNotify.Count > 0);

            L.D("MDL NotifyAboutMGLInitComplete gameObjectsCreatedDuringNotificationLoop: " +
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
        /// <see cref="MachinationsObjectManifest"/>.
        /// </summary>
        /// <returns>Dictionary of Game Object Property Name and ElementBinder.</returns>
        static private Dictionary<string, ElementBinder> CreateBindersForScriptableObject (EnrolledScriptableObject eso)
        {
            var ret = new Dictionary<string, ElementBinder>();
            MachinationsObjectManifest manifest = eso.Manifest;
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
                throw new Exception("MDL.FindSourceElement: machinationsUniqueID '" +
                                    GetMachinationsUniqueID(elementBinder, statesAssociation) +
                                    "' not found in _sourceElements.");

            //If there is an Override defined in the DiagramMapping, immediately returning that ElementBase instead of the one we found.
            if (dm?.OverrideElementBase != null)
            {
                L.W("Value for " + GetMachinationsUniqueID(elementBinder, statesAssociation) + " has been overriden!");
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
                    throw new Exception("MDL.FindSourceElement: machinationsUniqueID '" +
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
        static private void NotifyAboutMGLUpdate (DiagramMapping diagramMapping, ElementBase elementBase)
        {
            //Notify Scriptable Objects that are affected by what changed in this update.
            foreach (IMachinationsScriptableObject imso in _scriptableObjects.Keys)
            {
                //Find matching Binder by checking Machinations Object Property names.
                //Reminder: _scriptableObjects[imso] is a Dictionary of that Machinations Scriptable Object's Game Property Names.
                foreach (string gameObjectPropertyName in _scriptableObjects[imso].Binders.Keys)
                    //Found the incoming Property Name.
                    if (gameObjectPropertyName == diagramMapping.PropertyName)
                    {
                        ElementBinder targetBinder = _scriptableObjects[imso].Binders[gameObjectPropertyName];
                        //If the current element in this Binder matches the given one, overwrite it with the incoming values. 
                        if (targetBinder.CurrentElement.DiagMapping.Matches(diagramMapping, true))
                            targetBinder.CurrentElement.Overwrite(elementBase);
                        else
                        //TODO: during Game State Awareness implementation, consider implementing a function that also overwrites if the current element is NOT the incoming one.
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
                throw new Exception("MDL.UpdateWithValuesFromMachinations: Got from the back-end a Machinations Diagram ID (" +
                                    machinationsDiagramID + ") for which there is no DiagramMapping.");
            return null;
        }

        /// <summary>
        /// Creates an <see cref="ElementBase"/> based on the provided properties from the Machinations Back-end.
        /// </summary>
        /// <param name="elementProperties">Dictionary of Machinations-specific properties.</param>
        /// <param name="mapping">The DiagramMapping for which the Element is created.</param>
        static private ElementBase CreateSourceElementBaseFromProps (Dictionary<string, string> elementProperties, DiagramMapping mapping)
        {
            ElementBase elementBase;
            //Populate value inside Machinations Element.
            int iValue;
            string sValue;
            try
            {
                iValue = int.Parse(elementProperties["resources"]);
                elementBase = new ElementBase(iValue, mapping);
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
                elementBase = new FormulaElement(sValue, mapping, false);
            }

            return elementBase;
        }

        /// <summary>
        /// Saves the Cache.
        /// </summary>
        static private void SaveCache ()
        {
            string cachePath = Path.Combine(AssetsPath, "MachinationsCache", Instance.cacheDirectoryName);
            string cacheFilePath = Path.Combine(cachePath, "Cache.xml");
            Directory.CreateDirectory(cachePath);
            L.D("MDL.SaveCache using file: " + cacheFilePath);

            DataContractSerializer dcs = new DataContractSerializer(typeof(MCache));
            var settings = new XmlWriterSettings {Indent = true, NewLineOnAttributes = true};
            XmlWriter xmlWriter = XmlWriter.Create(cacheFilePath, settings);
            dcs.WriteObject(xmlWriter, _cache);
            xmlWriter.Close();
        }

        /// <summary>
        /// Loads the Cache located at <see cref="cacheDirectoryName"/>. Applies it over <see cref="_sourceElements"/>.
        /// </summary>
        static private void LoadCache ()
        {
            string cacheFilePath = Path.Combine(AssetsPath, "MachinationsCache", Instance.cacheDirectoryName, "Cache.xml");
            if (!File.Exists(cacheFilePath))
            {
                L.D("MDL.LoadCache DOES NOT EXIST: " + cacheFilePath);
                _cache = null;
                return;
            }

            L.D("MDL.LoadCache using file: " + cacheFilePath);

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
            OnMachinationsUpdate?.Invoke(Instance, null);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Registers a <see cref="MachinationsObjectManifest"/> to make sure that during Initialization, the MDL
        /// (aka <see cref="MachinationsDataLayer"/> retrieves all the Manifest's necessary data so that
        /// any Game Objects that use this Manifest can query the MDL for the needed values.
        /// </summary>
        static public void DeclareManifest (MachinationsObjectManifest manifest)
        {
            L.D("MDL DeclareManifest: " + manifest);
            //Add all of this Manifest's targets to the list that we will have to Initialize & monitor.
            AddTargets(manifest.GetMachinationsDiagramTargets());
        }

        /// <summary>
        /// Registers a <see cref="IMachinationsScriptableObject"/> along with its Manifest.
        /// This is used to make sure that all Game Objects are ready for use after MDL Initialization.
        /// </summary>
        /// <param name="imso">The IMachinationsScriptableObject to add.</param>
        /// <param name="manifest">Its <see cref="MachinationsObjectManifest"/>.</param>
        static public void EnrollScriptableObject (IMachinationsScriptableObject imso,
            MachinationsObjectManifest manifest)
        {
            L.D("MDL EnrollScriptableObject: " + manifest);
            DeclareManifest(manifest);

            //Restore previously-serialized values.
            foreach (DiagramMapping dm in manifest.DiagramMappings)
                if (dm.GameElementBase != null)
                {
                    L.D("EBBS Restoring Serialized Value for '" + dm + "' to " + dm.GameElementBase._serializableValue);
                    dm.GameElementBase.MaxValue = null;
                    dm.GameElementBase.ChangeValueTo(dm.GameElementBase._serializableValue);
                }

            //Save this in the Scriptable Objects database.
            if (!_scriptableObjects.ContainsKey(imso))
                _scriptableObjects[imso] = new EnrolledScriptableObject {MScriptableObject = imso, Manifest = manifest};

            //Schedule a sync for any new addition.
            Service?.ScheduleSync();
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

            //Schedule a sync for any new addition.
            Service.ScheduleSync();
        }

        /// <summary>
        /// Creates an <see cref="MachinationsUP.Integration.Elements.ElementBase"/>.
        /// </summary>
        /// <param name="elementBinder">The <see cref="MachinationsUP.Integration.Binder.ElementBinder"/> for which
        /// the <see cref="MachinationsUP.Integration.Elements.ElementBase"/> is to be created.</param>
        /// <param name="statesAssociation">OPTIONAL. The StatesAssociation for which the ElementBase is to be created.
        /// If this is not provided, the default value of NULL means that the ElementBase will use "N/A" as Title
        /// in the <see cref="MachinationsDataLayer"/> Init Request.</param>
        public ElementBase CreateElement (ElementBinder elementBinder, StatesAssociation statesAssociation = null)
        {
            ElementBase sourceElement = FindSourceElement(elementBinder, statesAssociation);
            //Not found elements are accepted in Offline Mode.
            if (sourceElement == null)
            {
                //Offline mode allows not finding a Source Element.
                if (isInOfflineMode) return null;
                throw new Exception("MDL.CreateElement: Unhandled null Source Element.");
            }

            //Initialize the ElementBase by cloning it from the sourceElement.
            var newElement = sourceElement.Clone(elementBinder);

            L.D("MDL.CreateValue complete for machinationsUniqueID '" +
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
                L.D("MDL Returning DefaultElementBase for " + diagramMapping);
                return diagramMapping.DefaultElementBase;
            }

            throw new Exception("MDL.GetSourceElementBase: cannot find any Source Element Base for " + diagramMapping);
        }

        static public void PerformInitRequest ()
        {
            //Notify Game Engine of Machinations Init Start.
            L.D("MDL.PerformInitRequest: Pausing game during initialization. MachinationsInitStart.");
            Instance._gameLifecycleProvider?.MachinationsInitStart();
            //Instance.EmitMachinationsInitRequest();
            //Make sure that when the Init request will be handled as re-initialization.
            ReInitOngoing = true;
        }

        /// <summary>
        /// Emits the 'Game Init Request' Socket event.
        /// </summary>
        static public void EmitGameUpdateDiagramElementsRequest (ElementBase sourceElement, int previousValue)
        {
            //Update Request components will be stored as top level items in this Dictionary.
            var updateRequest = new Dictionary<string, JSONObject>();

            //The item will be a Dictionary comprising of "id" and "props". The "props" will contain the properties to update.
            var item = new Dictionary<string, JSONObject>();
            item.Add("id", new JSONObject(sourceElement.ParentElementBinder.DiagMapping.DiagramElementID));
            item.Add("type", JSONObject.CreateStringObject("resources"));
            item.Add("timeStamp", new JSONObject(DateTime.Now.Ticks));
            item.Add("parameter", JSONObject.CreateStringObject("number"));
            item.Add("previous", new JSONObject(previousValue));
            item.Add("value", new JSONObject(sourceElement.CurrentValue));

            JSONObject[] keys = new JSONObject [1];
            keys[0] = new JSONObject(item);

            //Finalize request by adding all top level items.
            updateRequest.Add(SyncMsgs.JK_AUTH_DIAGRAM_TOKEN, JSONObject.CreateStringObject(MachinationsConfig.Instance.DiagramToken));
            //Wrapping the keys Array inside a JSON Object.
            updateRequest.Add(SyncMsgs.JK_INIT_MACHINATIONS_IDS, new JSONObject(keys));

            L.D("MDL.EmitMachinationsUpdateElementsRequest.");

            Service.EmitGameUpdateDiagramElementsRequest(new JSONObject(updateRequest));
        }

        #endregion

        #region Properties

        static private bool isInOfflineMode;

        /// <summary>
        /// MDL is running in offline mode.
        /// </summary>
        static public bool IsInOfflineMode
        {
            set
            {
                isInOfflineMode = value;
                if (value)
                {
                    L.D("MachinationsDataLayer is now in Offline Mode!");
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
                throw new Exception("MDL no IGameLifecycleProvider available.");
            return Instance._gameLifecycleProvider.GetGameState();
        }

        /// <summary>
        /// Returns if the MDL has any cache loaded.
        /// </summary>
        static public bool HasCache => !string.IsNullOrEmpty(Instance.cacheDirectoryName) && _cache != null;

        /// <summary>
        /// The path where the Project's Assets are located. Can be set to Application.dataPath.
        /// We need Application.dataPath in other contexts than the main thread.
        /// </summary>
        static public string AssetsPath { get; set; }

        #endregion

    }
}