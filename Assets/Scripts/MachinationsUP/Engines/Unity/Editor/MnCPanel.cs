using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using MachinationsUP.Config;
using MachinationsUP.Engines.Unity.Editor.Graphics;
using MachinationsUP.Integration.Inventory;
using UnityEditor;
using UnityEngine;
using MachinationsUP.Logger;

namespace MachinationsUP.Engines.Unity.Editor
{
    public class MnCPanel : EditorWindow
    {

        private string _APIURL = "wss://api.machinations.io/socket.io/?EIO=4&transport=websocket";
        private string _userKey = "<<ENTER YOUR USER KEY>>";
        private string _gameName = "Brave New Game";
        private string _diagramToken = "<<ENTER YOUR DIAGRAM TOKEN>>";

        /// <summary>
        /// If settings were restored from the settings file.
        /// </summary>
        private bool _restoredFromSettings;

        /// <summary>
        /// Used to determine where to place the PopupSearchList when the Import button is pressed.
        /// </summary>
        Rect _btnImportElementsRect;

        /// <summary>
        /// Graphics to be used in the Control Panel.
        /// </summary>
        private MnCPanelGraphics _cpGraphics;

        /// <summary>
        /// Types of 
        /// </summary>
        private Dictionary<string, MnElementTypeTagProvider> _usedTypes;

        /// <summary>
        /// Used to check if a string value coming from Machinations can be used as a variable name.
        /// </summary>
        CodeDomProvider _cdp = CodeDomProvider.CreateProvider("C#");

        /// <summary>
        /// Default Constructor.
        /// </summary>
        public MnCPanel ()
        {
            PopupSearchList.OnButtonPressed += GenerateCodeFromPopupSearchListSelection;
        }

        #region Menu

        [MenuItem("Tools/Machinations/Open Machinations.io Control Panel")]
        static public void ShowWindow ()
        {
            GetWindow(typeof(MnCPanel), false, "Machinations.io");
        }

        [MenuItem("Tools/Machinations/Pause Sync")]
        static public void PauseSync ()
        {
            MnDataLayer.Service.PauseSync();
        }

        [MenuItem("Tools/Machinations/Launch Machinations.io")]
        static public void Launch ()
        {
            System.Diagnostics.Process.Start("http://my.machinations.io");
        }

        #endregion

        /// <summary>
        /// Draws the Machionations Control Panel GUI.
        /// </summary>
        void OnGUI ()
        {
            if (MnConfig.Instance == null && MnConfig.HasSettings) return;
            if (!_restoredFromSettings && MnConfig.HasSettings && MnConfig.Instance != null)
            {
                _restoredFromSettings = true;
                _APIURL = MnConfig.Instance.APIURL;
                _userKey = MnConfig.Instance.UserKey;
                _gameName = MnConfig.Instance.GameName;
                _diagramToken = MnConfig.Instance.DiagramToken;
            }

            EditorGUI.BeginChangeCheck();
            GUILayout.Label("Machinations.io Connection Settings", EditorStyles.boldLabel);
            _APIURL = EditorGUILayout.TextField("API URL", _APIURL);
            _userKey = EditorGUILayout.TextField("User Key", _userKey);
            _gameName = EditorGUILayout.TextField("Game Name", _gameName);
            _diagramToken = EditorGUILayout.TextField("Diagram Token", _diagramToken);

            PopupSearchList.DrawLineInInspector(Color.black, 2, 10);

            //If it's not there yet, get Scriptable Object with Control Panel Graphics.
            if (_cpGraphics == null)
            {
                string assetGUID = AssetDatabase.FindAssets("t:MnCPanelGraphics")[0];
                string path = AssetDatabase.GUIDToAssetPath(assetGUID);
                _cpGraphics = (MnCPanelGraphics) AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            }
            //Now we can initialize the list of MachinationsElementType (which inherit from ITagProvider, used to provide custom
            //graphics to the PopupSearchList component).
            else
            {
                //Initialize only if required.
                if (_usedTypes == null)
                {
                    _usedTypes = new Dictionary<string, MnElementTypeTagProvider>
                    {
                        {"Pool", new MnElementTypeTagProvider {Aspect = _cpGraphics.Pool, Name = "Pool"}},
                        {"Drain", new MnElementTypeTagProvider {Aspect = _cpGraphics.Drain, Name = "Drain"}},
                        {"State Connection", new MnElementTypeTagProvider {Aspect = _cpGraphics.StateConnection, Name = "State Connection"}},
                        {"Resource Connection", new MnElementTypeTagProvider {Aspect = _cpGraphics.ResourceConnection, Name = "Resource Connection"}},
                        {"End Condition", new MnElementTypeTagProvider {Aspect = _cpGraphics.EndCondition, Name = "End Condition"}},
                        {"Delay", new MnElementTypeTagProvider {Aspect = _cpGraphics.Delay, Name = "Delay"}},
                        {"Register", new MnElementTypeTagProvider {Aspect = _cpGraphics.Register, Name = "Register"}},
                        {"Converter", new MnElementTypeTagProvider {Aspect = _cpGraphics.Converter, Name = "Converter"}},
                        {"Trader", new MnElementTypeTagProvider {Aspect = _cpGraphics.Trader, Name = "Trader"}},
                        {"Gate", new MnElementTypeTagProvider {Aspect = _cpGraphics.Gate, Name = "Gate"}},
                        {"Source", new MnElementTypeTagProvider {Aspect = _cpGraphics.Source, Name = "Source"}},
                    };
                }
            }

            //Setup PopupSearchList for importing Machinations Elements.
            if (_usedTypes != null && GUILayout.Button("Import Elements", GUILayout.Width(200)))
            {
                List<DiagramMapping> currentMappings = MnDataLayer.GetRegisteredMappings();
                List<SearchListItem> searchListItems = new List<SearchListItem>();
                foreach (DiagramMapping dm in currentMappings)
                {
                    //No type specified? Cannot show such items.
                    if (dm.Type == null) continue;
                    //Handling NULL labels.
                    if (dm.Label == null) dm.Label = "[no label]";
                    var sli = new SearchListItem {ID = dm.DiagramElementID, Name = dm.Label, TagProvider = _usedTypes[dm.Type], AttachedObject = dm};
                    searchListItems.Add(sli);
                }

                //The Popup will be shown immediately next to the button.
                PopupWindow.Show(_btnImportElementsRect,
                    new PopupSearchList(Screen.width, searchListItems, new List<ITagProvider>(_usedTypes.Values)));
            }

            //Save buttonRect position so that we know where to place the PopupSearchList.
            //GetLastRect will return the Rect of the last-painted element, which in this case is the call to GUILayout.Button above.
            if (Event.current.type == EventType.Repaint) _btnImportElementsRect = GUILayoutUtility.GetLastRect();

            EditorGUILayout.Separator();

            if (GUI.changed)
            {
                SaveMachinationsConfig();
            }
        }

        /// <summary>
        /// Handles CODE GENERATION for items selected in the <see cref="PopupSearchList"/>.
        /// </summary>
        /// <param name="selecteditems">What items have been selected in the <see cref="PopupSearchList"/></param>
        private void GenerateCodeFromPopupSearchListSelection (List<SearchListItem> selecteditems)
        {
            if (selecteditems.Count == 0) return;
            //Read code generation template.
            string template = File.ReadAllText(Path.Combine(Application.dataPath, "MachinationsTemplates", "Template.cst"));
            //Create up a unique class name.
            string className = "GeneratedSO_" + DateTime.Now.ToString("yyyyMMdd_HHmmss").Replace(" ", "");
            template = template.Replace("<<CLASS NAME>>", className);

            //List of identifiers we already used in the code. On collision, the incoming identifier will be appended a number.
            List<string> currentlyUsedIdentifiers = new List<string>();
            //Used to replace things in the code generation template.
            string constants = "";
            string variables = "";
            string diagramMappings = "";
            string initFunction = "";

            //Now go through all items and generate code that will be inserted in the template.
            foreach (SearchListItem sli in selecteditems)
            {
                DiagramMapping dm = (DiagramMapping) sli.AttachedObject; //All code will be generated based on the selected Diagram Mappings.

                //Figure out what identifier name we should use for this selected item.
                //By default, the identifier name to use for generating this item is taken from the label.
                string identifierName = dm.Label.Replace(" ", "");
                //If, however, it is not a valid identifier, the fun starts.
                if (!_cdp.IsValidIdentifier(identifierName))
                {
                    string formingIdentifier = "";
                    int startPosition = 0;
                    //Get the first LETTER from the label that is a valid identifier.
                    while (!_cdp.IsValidIdentifier(formingIdentifier) && startPosition < identifierName.Length)
                        //Advance character by charater. When the first valid character is found, it will be taken as the start of the identifier name.
                        formingIdentifier = identifierName.Substring(startPosition++, 1);
                    //Step back to whatever character was found to be valid.
                    if (startPosition > 0) startPosition--;
                    //Now get the following characters for the identifier name, starting from where we left off.
                    int endPosition = startPosition + 1;
                    //Loop until the end.
                    while (endPosition < identifierName.Length)
                    {
                        string candidateIdentifier = identifierName.Substring(startPosition, endPosition++ - startPosition);
                        if (_cdp.IsValidIdentifier(candidateIdentifier)) formingIdentifier = candidateIdentifier;
                        else break;
                    }

                    if (_cdp.IsValidIdentifier(formingIdentifier)) identifierName = formingIdentifier;
                    else identifierName = "MElementID_" + dm.DiagramElementID + "_" + dm.Type.Replace(" ", "");
                }

                //Handle situations when this identifier already exists.
                string originalIdentifierName = identifierName;
                int nextAvailableName = 1;
                while (currentlyUsedIdentifiers.Contains(identifierName))
                    identifierName = originalIdentifierName + "_" + nextAvailableName++;
                //Memorize that this identifier was used.
                currentlyUsedIdentifiers.Add(identifierName);

                //Generate code using the above-computed identifier name.

                //Constants declaration:
                //private const string M_HEALTH = "Health";
                constants += "\tprivate const string M_" + identifierName.ToUpper() + " = " + "\"" + identifierName + " [" + dm.Label + "]\";\r\n";

                //Variables delcaration:
                //public ElementBase Health;
                variables += "\tpublic ElementBase " + identifierName + ";\r\n";

                //Diagram Mappings:
                //new DiagramMapping
                //{
                //    PropertyName = M_HEALTH,
                //    DiagramElementID = 215,
                //    DefaultElementBase = new ElementBase(105)
                //},
                if (diagramMappings.Length > 0) diagramMappings += ",\r\n";
                diagramMappings += "\tnew DiagramMapping\r\n";
                diagramMappings += "\t{\r\n";
                diagramMappings += "\t\tPropertyName = M_" + identifierName.ToUpper() + ",\r\n";
                diagramMappings += "\t\tDiagramElementID = " + dm.DiagramElementID + ",\r\n";
                diagramMappings += "\t}";

                if (dm.DiagramElementID == 911)
                {
                    L.D("salut");
                }
                //Init Function:
                //Health = binders[M_HEALTH].CurrentElement;
                initFunction += "\t" + identifierName + " = " + "binders[M_" + identifierName.ToUpper() + "].CurrentElement;\r\n";
            }

            template = template.Replace("<<CONSTANTS DECLARATION>>", constants);
            template = template.Replace("<<VARIABLES DECLARATION>>", variables);
            template = template.Replace("<<DIAGRAM MAPPINGS>>", diagramMappings);
            template = template.Replace("<<INIT FUNCTION>>", initFunction);

            //Make sure the directory exists & write file.
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "MachinationsOut"));
            File.WriteAllText(Path.Combine(Application.dataPath, "MachinationsOut", className + ".cs"), template);
            //Notify of GREAT SUCCESS.
            ShowNotification(new GUIContent("Class " + className + " created in your Assets/MachinationsOut directory."), 10);
        }

        void OnEnable ()
        {
            SaveMachinationsConfig();
        }

        void OnDestroy ()
        {
            SaveMachinationsConfig();
        }

        /// <summary>
        /// Saves any setting changed.
        /// </summary>
        private void SaveMachinationsConfig ()
        {
            //Only saving settings if there are any.
            if (!_restoredFromSettings && MnConfig.HasSettings) return;
            MnConfig.Instance = new MnConfig();
            MnConfig.Instance.APIURL = _APIURL;
            MnConfig.Instance.UserKey = _userKey;
            MnConfig.Instance.GameName = _gameName;
            MnConfig.Instance.DiagramToken = _diagramToken;
            MnConfig.SaveSettings();
        }

    }
}