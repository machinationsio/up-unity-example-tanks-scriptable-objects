using System;
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

namespace MachinationsUP.Engines.Unity
{
    /// <summary>
    /// The Machinations Data Layer is a Singleton that handles organization of Machinations-related data.
    /// TODO: better separation between ElementBinder and MnDataLayer
    /// </summary>
    public class MnDataLayer : IGameLifecycleSubscriber, IGameObjectLifecycleSubscriber
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
        static public MnService Service;

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
        /// <see cref="MnObjectManifest"/> declared in the game.
        ///
        /// New MachinationElements are created from the ones in this Dictionary.
        ///
        /// Dictionary of the <see cref="MachinationsUP.Integration.Inventory.DiagramMapping"/> indicating where in the Diagram to find a
        /// Game Object Property Name and the <see cref="ElementBase"/> that will serve as a Source value
        /// to all values that will be created by the MachinationsDataLayer.
        /// </summary>
        static private Dictionary<DiagramMapping, ElementBase> _sourceElements =
            new Dictionary<DiagramMapping, ElementBase>();

        /// <summary>
        /// Disk XML cache.
        /// See <see cref="MnCache"/>.
        /// </summary>
        static private MnCache _cache = new MnCache();

        /// <summary>
        /// List with of all registered MachinationsGameObject.
        /// </summary>
        static readonly private List<MnGameObject> _gameObjects = new List<MnGameObject>();

        /// <summary>
        /// List with of all registered MachinationsGameAwareObject.
        /// </summary>
        static readonly private List<MnGameAwareObject> _gameAwareObjects = new List<MnGameAwareObject>();

        /// <summary>
        /// Dictionary with Scriptable Objects and their associated <see cref="EnrolledScriptableObject"/> (which contains
        /// Binders per Game Object Property name for the Scriptable Object).
        /// </summary>
        static readonly private Dictionary<IMnScriptableObject, EnrolledScriptableObject> _scriptableObjects =
            new Dictionary<IMnScriptableObject, EnrolledScriptableObject>();

        #endregion

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
            foreach (MnGameAwareObject mgao in _gameAwareObjects)
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
            foreach (MnGameAwareObject mgao in _gameAwareObjects)
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
        static private MnDataLayer _instance;

        /// <summary>
        /// Singleton implementation.
        /// </summary>
        static public MnDataLayer Instance
        {
            get
            {
                if (_instance != null) return _instance;
                _instance = new MnDataLayer();
                L.D("MDL created by invocation. Hash is " + _instance.GetHashCode() +
                    " and User Key is " + MnConfig.Instance.UserKey);
                return _instance;
            }
        }

        #endregion

        #region Internal Functionality

        /// <summary>
        /// Notifies all enrolled <see cref="MnGameObject"/> that
        /// the MDL is now initialized.
        /// </summary>
        /// <param name="isRunningOffline">TRUE: the MDL is running in offline mode.</param>
        static private void NotifyAboutMGLInitComplete (bool isRunningOffline = false)
        {
            L.D("MDL NotifyAboutMGLInitComplete.");
            L.D("MDL NotifyAboutMGLInitComplete scriptableObjectsToNotify: " + _scriptableObjects.Keys.Count);
            //Build Binders for Scriptable Objects.
            foreach (IMnScriptableObject so in _scriptableObjects.Keys)
            {
                _scriptableObjects[so].Binders = CreateBindersForScriptableObject(_scriptableObjects[so]);
                //Notify Scriptable Objects that they are ready.
                so.MDLInitCompleteSO(_scriptableObjects[so].Binders);
            }

            //_gameObjects is cloned as a new Array because the collection MAY be modified during MachinationsGameObject.MGLInitComplete.
            //That's because Game Objects MAY create other Game Objects during MachinationsGameObject.MGLInitComplete.
            //These new Game Objects will then enroll with the MDL, which will add them to _gameObjects.
            List<MnGameObject> gameObjectsToNotify = new List<MnGameObject>(_gameObjects.ToArray());
            //Maintain a list of Game Objects that were notified.
            List<MnGameObject> gameObjectsNotified = new List<MnGameObject>();
            //Maintain a list of all Game Objects that were created during the notification loop.
            List<MnGameObject> gameObjectsCreatedDuringNotificationLoop = new List<MnGameObject>();

            do
            {
                L.D("MDL NotifyAboutMGLInitComplete gameObjectsToNotify: " + gameObjectsToNotify.Count);

                //Notify Machinations Game Objects.
                foreach (MnGameObject mgo in gameObjectsToNotify)
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
                foreach (MnGameObject mgo in _gameObjects)
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
                //Check for empty Diagram Mappings.
                DiagramMapping emptyDM = null;
                foreach (DiagramMapping sourceDM in _sourceElements.Keys)
                    //Found an empty Diagram Mapping. First of all, it can't even reach here with the same DiagramElementID (it
                    //should 'continue' above). Secondly, there is no PropertyName, which is a nonpossible situation :).
                    if (sourceDM.DiagramElementID == diagramMapping.DiagramElementID && string.IsNullOrEmpty(sourceDM.PropertyName))
                    {
                        L.D("Empty DM found for " + sourceDM);
                        emptyDM = sourceDM;
                        break;
                    }

                //No Empty DiagramMapping exists. We can add normaly.
                if (emptyDM == null) _sourceElements.Add(diagramMapping, targets[diagramMapping]);
                //When there is an Empty DiagramMapping, switch ElementBase from empty DiagramMapping source, to the incoming source.
                else
                {
                    //Copy the ElementBase from the key at the Empty DiagramMapping, to the incoming key...
                    _sourceElements.Add(diagramMapping, _sourceElements[emptyDM]);
                    //Transfer back-end properties to the proper DiagramMapping.
                    diagramMapping.Label = emptyDM.Label;
                    diagramMapping.Type = emptyDM.Type;
                    //.. and removing the empty key.
                    _sourceElements.Remove(emptyDM);
                }
            }
        }

        /// <summary>
        /// Creates <see cref="ElementBinder"/> for each Game Object Property provided in the <see cref="EnrolledScriptableObject"/>'s
        /// <see cref="MnObjectManifest"/>.
        /// </summary>
        /// <returns>Dictionary of Game Object Property Name and ElementBinder.</returns>
        static private Dictionary<string, ElementBinder> CreateBindersForScriptableObject (EnrolledScriptableObject eso)
        {
            var ret = new Dictionary<string, ElementBinder>();
            MnObjectManifest manifest = eso.Manifest;
            L.D("Creating binders for " + eso.Manifest);
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
        /// Returns a string that details data about a Machinations Element.
        /// </summary>
        /// <param name="binder"></param>
        /// <param name="statesAssociation"></param>
        /// <returns></returns>
        virtual protected string GetMachinationsElementDescription (ElementBinder binder, StatesAssociation statesAssociation)
        {
            return "Binder: [Parent: '" + (binder.ParentGameObject != null ? binder.ParentGameObject.Name : "!NoParent!") + "' " +
                   "Property: '" + binder.GameObjectPropertyName + "' " +
                   "SAssoc: '" + (statesAssociation != null ? statesAssociation.Title : "N/A") + "'] " +
                   "[Diagram Mapping: '" + binder.DiagMapping + "']";
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
            bool foundMapping = false;
            //Search all Diagram Mappings to see which one matches the provided Binder and States Association.
            foreach (DiagramMapping diagramMapping in _sourceElements.Keys)
            {
                //Check if we got a match with the requested Binder-per-State. If using cache, will check States Associations as Strings.
                if (diagramMapping.Matches(elementBinder, statesAssociation, HasCache, diagramMapping.CrossManifestName))
                {
                    ret = _sourceElements[diagramMapping];
                    foundMapping = true;
                    dm = diagramMapping; //Save the Diagram Mapping that was found to match this Binder-per-State.
                    break;
                }
            }

            //A DiagramMapping MUST have been found for this element.
            if (!foundMapping)
                throw new Exception("MDL.FindSourceElement: _sourceElements has no Diagram Mapping key for '" +
                                    GetMachinationsElementDescription(elementBinder, statesAssociation) + ".");

            //If there is an Override defined in the DiagramMapping, immediately returning that ElementBase instead of the one we found.
            if (dm?.OverrideElementBase != null)
            {
                L.W("Value for '" + GetMachinationsElementDescription(elementBinder, statesAssociation) + "' has been overriden!");
                return dm.OverrideElementBase;
            }

            //If no ElementBase was found.
            if (ret == null)
            {
                //Search the Cache, if it is enabled.
                if (IsInOfflineMode && HasCache)
                    foreach (DiagramMapping diagramMapping in _cache.DiagramMappings)
                        if (diagramMapping.Matches(elementBinder, statesAssociation, true))
                            return diagramMapping.CachedElementBase;

                //Nothing found? Throw!
                if (!isInOfflineMode || (IsInOfflineMode && StrictOfflineMode))
                    throw new Exception("MDL.FindSourceElement could not find any ElementBase in _sourceElements for: '" +
                                        GetMachinationsElementDescription(elementBinder, statesAssociation) +
                                        "'.");
            }

            return ret;
        }

        /// <summary>
        /// Goes through registered <see cref="MnGameObject"/> and <see cref="IMnScriptableObject"/>
        /// and notifies those Objects that are affected by the update.
        /// </summary>
        /// <param name="diagramMapping">Diagram Mapping that has been updated.</param>
        /// <param name="elementBase">The <see cref="ElementBase"/> parsed from the back-end update.</param>
        static private void NotifyAboutMGLUpdate (DiagramMapping diagramMapping, ElementBase elementBase)
        {
            //Notify Scriptable Objects that are affected by what changed in this update.
            foreach (IMnScriptableObject imso in _scriptableObjects.Keys)
            {
                bool foundCrossManifestMapping = false;
                //When the Diagram Mapping is for a cross-manifest item, we have to check if the current Scriptable Object has it.
                if (diagramMapping.CrossManifestName != null)
                    foreach (DiagramMapping dm in imso.Manifest.DiagramMappings)
                        if (diagramMapping.CrossManifestName == dm.CrossManifestName)
                        {
                            foundCrossManifestMapping = true;
                            break;
                        }

                //Skip to the next Scriptable Object if this one has nothing to do with the provided Manifest.
                if (!foundCrossManifestMapping && imso.Manifest.Name != elementBase.DiagMapping.ManifestName)
                    continue;

                if (_scriptableObjects[imso].Binders == null)
                {
                    L.W("There are no Binders defined yet for Scriptable Object with Manifest " + imso.Manifest +
                        ". Will attempt to create them now.");
                    _scriptableObjects[imso].Binders = CreateBindersForScriptableObject(_scriptableObjects[imso]);
                }

                //Find matching Binder by checking Machinations Object Property names.
                //Reminder: _scriptableObjects[imso] is a Dictionary of a Machinations Scriptable Object's Game Property Names and the associated Binder.
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

                        imso.MDLUpdateSO(diagramMapping, elementBase);
                    }
            }

            //Notify all registered Machinations Game Objects, if they have 
            foreach (MnGameObject mgo in _gameObjects)
                //When we find a registered Game Object that matches this Diagram Mapping asking it to update its Binder.
                if (mgo.Name == diagramMapping.ManifestName)
                    mgo.UpdateBinder(diagramMapping, elementBase);
        }

        /// <summary>
        /// Retrieves the <see cref="DiagramMapping"/> for the requested Machinations Diagram ID.
        /// </summary>
        static private DiagramMapping GetDiagramMappingForID (string machinationsDiagramID)
        {
            foreach (DiagramMapping dm in _sourceElements.Keys)
                if (dm.DiagramElementID == int.Parse(machinationsDiagramID))
                {
                    return dm;
                }

            //Couldn't find any Source Element for this Machinations Diagram ID.
            //This doesn't mean it won't be used, this may just mean it hasn't been requested yet.
            L.T("MDL.GetDiagramMappingForID: Got from the back-end a Machinations Diagram ID (" +
                machinationsDiagramID + ") for which there is no DiagramMapping. Creating an Empty DiagramMapping.");
            //If any object comes looking for this element ID, this Diagram Mapping will be replaced with that of the requesting element.
            //See function AddTargets.
            return new DiagramMapping(int.Parse(machinationsDiagramID));
        }

        /// <summary>
        /// Creates an <see cref="ElementBase"/> based on the provided properties from the Machinations Back-end.
        /// </summary>
        /// <param name="elementProperties">Dictionary of Machinations-specific properties.</param>
        /// <param name="mapping">The DiagramMapping for which the Element is created.</param>
        static private ElementBase CreateSourceElementBaseFromProps (Dictionary<string, string> elementProperties, DiagramMapping mapping)
        {
            ElementBase elementBase;

            //Initialize Diagram Mapping properties.
            if (elementProperties.ContainsKey(SyncMsgs.JP_DIAGRAM_ELEMENT_TYPE))
                mapping.Type = elementProperties[SyncMsgs.JP_DIAGRAM_ELEMENT_TYPE];
            if (elementProperties.ContainsKey(SyncMsgs.JP_DIAGRAM_LABEL)) mapping.Label = elementProperties[SyncMsgs.JP_DIAGRAM_LABEL];
            //Numeric elements get their Value from Resources.
            if (elementProperties.ContainsKey(SyncMsgs.JP_DIAGRAM_RESOURCES))
            {
                //Populate value inside Machinations Element.
                int iValue = int.Parse(elementProperties[SyncMsgs.JP_DIAGRAM_RESOURCES]);
                elementBase = new ElementBase(iValue, mapping);
                //Set MaxValue, if we have from.
                if (elementProperties.ContainsKey(SyncMsgs.JP_DIAGRAM_CAPACITY) && int.TryParse(
                    elementProperties[SyncMsgs.JP_DIAGRAM_CAPACITY],
                    out iValue) && iValue != -1 && iValue != 0)
                {
                    elementBase.MaxValue = iValue;
                }
            }
            //These Element Types have values inside Formulas.
            else if (mapping.Type == SyncMsgs.JP_ELETYPE_STATE_CONNECTION
                     || mapping.Type == SyncMsgs.JP_ELETYPE_RESOURCE_CONNECTION || mapping.Type == SyncMsgs.JP_ELETYPE_REGISTER)
            {
                if (!elementProperties.ContainsKey(SyncMsgs.JP_DIAGRAM_FORMULA))
                    return null;
                string sValue = elementProperties[SyncMsgs.JP_DIAGRAM_FORMULA];
                try
                {
                    elementBase = new FormulaElement(sValue, mapping, false);
                }
                catch
                {
                    return null;
                }
            }
            else return null;

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

            DataContractSerializer dcs = new DataContractSerializer(typeof(MnCache));
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
            DataContractSerializer dcs = new DataContractSerializer(typeof(MnCache));
            FileStream fs = new FileStream(cacheFilePath, FileMode.Open);
            _cache = (MnCache) dcs.ReadObject(fs);

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
        /// Returns all Diagram Mappings that are currently registered in the MDL.
        /// </summary>
        static public List<DiagramMapping> GetRegisteredMappings ()
        {
            return _sourceElements.Keys.ToList();
        }

        /// <summary>
        /// Resets the MDL's data pool, so that new data may come fresh.
        /// </summary>
        static public void ResetData ()
        {
            _sourceElements = new Dictionary<DiagramMapping, ElementBase>();
            foreach (MnGameObject mgo in _gameObjects)
                AddTargets(mgo.Manifest.GetMachinationsDiagramTargets());
            foreach (EnrolledScriptableObject eso in _scriptableObjects.Values)
                AddTargets(eso.Manifest.GetMachinationsDiagramTargets());
        }

        static public void Prepare ()
        {
            L.D("MDL.Start: Pausing game during initialization. MachinationsInitStart.");
            ResetData();
            //TODO: re-think re-init concept. Bind together the two calls below in one single Sync location. 
            ReInitOngoing = true;
            Instance._gameLifecycleProvider?.MachinationsInitStart();
            Service.ScheduleSync();

            L.D("MDL Awake.");
        }

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

        /// <summary>
        /// Creates a JSON object which contains all data that we want to request from Machinations.
        /// </summary>
        /// <param name="diagramToken">The DiagramToken we want to retrieve elements from.</param>
        /// <param name="selectiveGet">TRUE: get only elements that we don't already have.</param>
        /// <returns>A JSON object that contains what we want to request from Machinations.</returns>
        static public JSONObject GetInitRequestData (string diagramToken, bool selectiveGet)
        {
            //Init Request components will be stored as top level items in this Dictionary.
            var initRequest = new Dictionary<string, JSONObject>();
            bool noItems = true; //TRUE: nothing to get Init Request for.

            initRequest.Add(SyncMsgs.JK_AUTH_DIAGRAM_TOKEN, JSONObject.CreateStringObject(diagramToken));

            //If only certain items need to be requested, checking the _sourceElements Map for elements that haven't yet been initialized.
            if (selectiveGet)
            {
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
                    item.Add(SyncMsgs.JP_DIAGRAM_ID, new JSONObject(diagramMapping.DiagramElementID));
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
                //Add the items to the request.
                initRequest.Add(SyncMsgs.JK_INIT_MACHINATIONS_IDS, new JSONObject(keys));
            }

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
            L.D("MDL.UpdateWithValuesFromMachinations: number of elementsFromBackEnd received: " + elementsFromBackEnd.Count);
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
                DiagramMapping diagramMapping = GetDiagramMappingForID(elementProperties[SyncMsgs.JP_DIAGRAM_ID]);
                
                //Get the Element Base based on the dictionary of Element Properties.
                ElementBase elementBase = CreateSourceElementBaseFromProps(elementProperties, diagramMapping);

                //This is the important line (oh so important :D) where the ElementBase is assigned to the Source Elements Dictionary, to be used for
                //cloning elements in the future.
                _sourceElements[diagramMapping] = elementBase;

                //Some elements received from the Back-End may be invalid, especially due to the way Formulas currently work (there is no
                //way to know if a label is a Formula or just a text). See MnFormula constructor.
                if (elementBase == null)
                {
                    L.T("MDL.UpdateSourcesWithValuesFromMachinations: Failed to create ElementBase for '" + diagramMapping +
                        "'. Its value is possibly incompatible.");
                    //But does this mean that _sourceElements will have no ElementBase for this diagramMapping? [_sourceElements[diagramMapping] = null]
                    //Yes, it does. And if anybody comes requesting for it, there's going to be an error, which is EXACTLY what the expected behavior is.
                    continue;
                }

                L.D("MDL.UpdateSourcesWithValuesFromMachinations: ElementBase created for '" + diagramMapping + "' with Base Value of: " +
                    elementBase.BaseValue);

                //This element already exists and we're not in update mode.
                if (_sourceElements.ContainsKey(diagramMapping) && !updateFromDiagram)
                {
                    continue;
                }

                //Caching active? Update the Cache.
                if (!string.IsNullOrEmpty(Instance.cacheDirectoryName))
                {
                    diagramMapping.CachedElementBase = elementBase;
                    if (!_cache.DiagramMappings.Contains(diagramMapping)) _cache.DiagramMappings.Add(diagramMapping);
                }

                //When changes occur in the Diagram, the Machinations back-end will notify UP. In this case, we will notify the associated objects.
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
            L.D("MDL.UpdateWithValuesFromMachinations: number of source elements: " + _sourceElements.Count);
        }

        /// <summary>
        /// Registers a <see cref="MnObjectManifest"/> to make sure that during Initialization, the MDL
        /// (aka <see cref="MnDataLayer"/> retrieves all the Manifest's necessary data so that
        /// any Game Objects that use this Manifest can query the MDL for the needed values.
        /// </summary>
        static public void DeclareManifest (MnObjectManifest manifest)
        {
            L.D("MDL DeclareManifest: " + manifest);
            //Add all of this Manifest's targets to the list that we will have to Initialize & monitor.
            AddTargets(manifest.GetMachinationsDiagramTargets());
        }

        /// <summary>
        /// Registers a <see cref="IMnScriptableObject"/> along with its Manifest.
        /// This is used to make sure that all Game Objects are ready for use after MDL Initialization.
        /// </summary>
        /// <param name="imso">The IMachinationsScriptableObject to add.</param>
        /// <param name="manifest">Its <see cref="MnObjectManifest"/>.</param>
        static public void EnrollScriptableObject (IMnScriptableObject imso,
            MnObjectManifest manifest)
        {
            L.D("MDL EnrollScriptableObject: enrolling: " + manifest);
            DeclareManifest(manifest);

            //Restore previously-serialized values.
            foreach (DiagramMapping dm in manifest.DiagramMappings)
                if (dm.EditorElementBase != null)
                {
                    L.D("MDL EnrollScriptableObject [" + imso.GetType() + "] : Restoring Serialized Value for '" + dm + "' to " +
                        dm.EditorElementBase._serializableValue);
                    dm.EditorElementBase.AssignDiagramMapping(dm);
                    dm.EditorElementBase.MaxValue = null;
                    dm.EditorElementBase.ChangeValueTo(dm.EditorElementBase._serializableValue);
                }

            //Save this in the Scriptable Objects database.
            if (!_scriptableObjects.ContainsKey(imso))
            {
                _scriptableObjects[imso] = new EnrolledScriptableObject {MScriptableObject = imso, Manifest = manifest};
                /*
                if (imso.Manifest.DiagramMappings.Count > 0)
                    //
                    foreach (DiagramMapping dm in _sourceElements.Keys)
                    {
                        //No ElementBase created yet for this DiagramMapping!
                        if (_sourceElements[dm] == null) continue;
                        //
                        if (dm.DiagramElementID == imso.Manifest.DiagramMappings[0].DiagramElementID)
                        {
                            _scriptableObjects[imso].Binders = CreateBindersForScriptableObject(_scriptableObjects[imso]);
                            break;
                        }
                    }
                    */
            }

            //Schedule a sync for any new addition.
            Service?.ScheduleSync();
        }

        /// <summary>
        /// Registers a MachinationsGameObject that the Game Layer can keep track of.
        /// </summary>
        /// <param name="mnGameObject"></param>
        static public void EnrollGameObject (MnGameObject mnGameObject)
        {
            _gameObjects.Add(mnGameObject);
            if (mnGameObject is MnGameAwareObject gameAwareObject)
                _gameAwareObjects.Add(gameAwareObject);
            //WARN: This may crash due to recent refactoring.
            mnGameObject.MGLInitComplete();

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
        /// in the <see cref="MnDataLayer"/> Init Request.</param>
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
                GetMachinationsElementDescription(elementBinder, statesAssociation) + "'.");

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
        /// Emits the 'Game Update Request' Socket event. Called by <see cref="ElementBase"/> in function ChangeValueFromEditor.
        /// </summary>
        static public void EmitGameUpdateDiagramElementsRequest (ElementBase sourceElement, int previousValue)
        {
            //Update Request components will be stored as top level items in this Dictionary.
            var updateRequest = new Dictionary<string, JSONObject>();

            //The item will be a Dictionary comprising of "id" and "props". The "props" will contain the properties to update.
            var item = new Dictionary<string, JSONObject>();
            item.Add(SyncMsgs.JP_DIAGRAM_ID, new JSONObject(sourceElement.DiagMapping.DiagramElementID));
            item.Add(SyncMsgs.JP_DIAGRAM_ELEMENT_TYPE, JSONObject.CreateStringObject(SyncMsgs.JP_DIAGRAM_RESOURCES));
            item.Add("timeStamp", new JSONObject(DateTime.Now.Ticks));
            item.Add("parameter", JSONObject.CreateStringObject("number"));
            item.Add("previous", new JSONObject(previousValue));
            item.Add(sourceElement is FormulaElement ? "formula" : "value", new JSONObject(sourceElement.CurrentValue));

            JSONObject[] keys = new JSONObject [1];
            keys[0] = new JSONObject(item);

            //Finalize request by adding all top level items.
            updateRequest.Add(SyncMsgs.JK_AUTH_DIAGRAM_TOKEN, JSONObject.CreateStringObject(MnConfig.Instance.DiagramToken));
            //Wrapping the keys Array inside a JSON Object.
            updateRequest.Add(SyncMsgs.JK_INIT_MACHINATIONS_IDS, new JSONObject(keys));

            L.D("MDL.EmitMachinationsUpdateElementsRequest.");

            //When an element is changed from the editor, making sure that any Scriptable Object related to it
            //also gets an update notification, because game objects may listen to the update event.
            bool found = false;
            foreach (EnrolledScriptableObject so in _scriptableObjects.Values)
            {
                foreach (DiagramMapping dm in so.MScriptableObject.Manifest.DiagramMappings)
                    //TODO: why does this not work directly with the ElementBase and instead requires Diagram Mapping comparison?
                    if (dm.EditorElementBase != null && dm.EditorElementBase.DiagMapping == sourceElement.DiagMapping)
                    {
                        so.MScriptableObject.MDLUpdateSO(dm, sourceElement);
                        found = true;
                        break;
                    }

                if (found) break;
            }

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