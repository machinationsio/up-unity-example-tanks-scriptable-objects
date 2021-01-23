using System;
using System.Collections.Generic;
using System.IO;
using MachinationsUP.Config;
using MachinationsUP.Engines.Unity.Editor.Graphics;
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
                //The Popup will be shown immediately next to the button.
                PopupWindow.Show(_btnImportElementsRect, new PopupSearchList(Screen.width,
                    new List<SearchListItem>()
                    {
                        new SearchListItem {ID = 1, Name = "Hit Points", TagProvider = _usedTypes["Pool"]},
                        new SearchListItem {ID = 2, Name = "Hit Points Converter", TagProvider = _usedTypes["Register"]},
                        new SearchListItem {ID = 3, Name = "Damage Drain", TagProvider = _usedTypes["Drain"]},
                        new SearchListItem {ID = 4, Name = "Friction Drain", TagProvider = _usedTypes["Drain"]},
                        new SearchListItem {ID = 5, Name = "Move To Teleporter", TagProvider = _usedTypes["State Connection"]},
                        new SearchListItem {ID = 6, Name = "Moved To Gateway", TagProvider = _usedTypes["State Connection"]},
                        new SearchListItem {ID = 7, Name = "Transfer of Gold", TagProvider = _usedTypes["Resource Connection"]},
                        new SearchListItem {ID = 8, Name = "Transfer of Ethereum", TagProvider = _usedTypes["Resource Connection"]},
                        new SearchListItem {ID = 9, Name = "Defeated Enemies", TagProvider = _usedTypes["End Condition"]},
                        new SearchListItem {ID = 10, Name = "Captured Civilians", TagProvider = _usedTypes["End Condition"]},
                        new SearchListItem {ID = 11, Name = "Delay 1", TagProvider = _usedTypes["Delay"]},
                        new SearchListItem {ID = 12, Name = "Delay 2", TagProvider = _usedTypes["Delay"]},
                        new SearchListItem {ID = 13, Name = "Delay 3", TagProvider = _usedTypes["Delay"]},
                        new SearchListItem {ID = 14, Name = "Register 1", TagProvider = _usedTypes["Register"]},
                        new SearchListItem {ID = 15, Name = "Register 2", TagProvider = _usedTypes["Register"]},
                        new SearchListItem {ID = 16, Name = "Register 3", TagProvider = _usedTypes["Register"]},
                        new SearchListItem {ID = 17, Name = "Ripple Converter", TagProvider = _usedTypes["Converter"]},
                        new SearchListItem {ID = 18, Name = "Electricity Converter", TagProvider = _usedTypes["Converter"]},
                        new SearchListItem {ID = 19, Name = "Stomach", TagProvider = _usedTypes["Converter"]},
                        new SearchListItem {ID = 20, Name = "Boiling Pot", TagProvider = _usedTypes["Converter"]},
                        new SearchListItem {ID = 21, Name = "Seaman Joe", TagProvider = _usedTypes["Trader"]},
                        new SearchListItem {ID = 22, Name = "Bartender Ally", TagProvider = _usedTypes["Trader"]},
                        new SearchListItem {ID = 23, Name = "Mysterious Sue", TagProvider = _usedTypes["Trader"]},
                        new SearchListItem {ID = 24, Name = "Gate To Heaven", TagProvider = _usedTypes["Gate"]},
                        new SearchListItem {ID = 25, Name = "Door 4", TagProvider = _usedTypes["Gate"]},
                        new SearchListItem {ID = 26, Name = "Paying Job", TagProvider = _usedTypes["Source"]},
                        new SearchListItem {ID = 27, Name = "Begging In The Bus", TagProvider = _usedTypes["Source"]},
                        new SearchListItem {ID = 28, Name = "Universal Source", TagProvider = _usedTypes["Source"]},
                        new SearchListItem {ID = 29, Name = "Armor Points", TagProvider = _usedTypes["Pool"]},
                        new SearchListItem {ID = 30, Name = "Armor Points Converter", TagProvider = _usedTypes["Register"]},
                        new SearchListItem {ID = 31, Name = "Crazy Register", TagProvider = _usedTypes["Register"]},
                    }, new List<ITagProvider>(_usedTypes.Values)));
            }

            //Save buttonRect position so that we know where to place the PopupSearchList.
            //GetLastRect will return the Rect of the last-painted element, which in this case is the call to GUILayout.Button above.
            if (Event.current.type == EventType.Repaint) _btnImportElementsRect = GUILayoutUtility.GetLastRect();

            EditorGUILayout.Separator();

            /* FUTURE FUNCTIONALITY - CODE GENERATION.
            EditorGUILayout.Space();
            EditorGUILayout.Separator();
            EditorGUILayout.Space();
            _index = EditorGUILayout.Popup(_index, new [] {"Player Tank", "Player Tank Shell", "Enemy Tank", "Enemy Tank Shell"});
            if (GUILayout.Button("Create"))
                CreateMachinationsCode();
            */

            if (GUI.changed)
            {
                SaveMachinationsConfig();
            }
        }

        /*
        /// <summary>
        /// Responsible for triggering Machinations Code Generation.
        /// </summary>
        void CreateMachinationsCode ()
        {
            switch (_index)
            {
                case 0:
                    string template = File.ReadAllText(Path.Combine(Application.dataPath, "MachinationsTemplates", "Template.cst"));
                    template = template.Replace("<<ClassName>>", "NewObject");
                    File.WriteAllText(Path.Combine(Application.dataPath, "MachinationsOut", "NewObject.cs"), template);
                    break;
                case 1:
                    break;
            }
        }
        */

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