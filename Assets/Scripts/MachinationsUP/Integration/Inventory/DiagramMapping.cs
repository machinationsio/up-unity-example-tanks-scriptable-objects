using System;
using System.Runtime.Serialization;
using MachinationsUP.GameEngineAPI.States;
using MachinationsUP.Integration.Binder;
using MachinationsUP.Integration.Elements;
using MachinationsUP.Integration.GameObject;
using MachinationsUP.Logger;
using MachinationsUP.SyncAPI;

namespace MachinationsUP.Integration.Inventory
{
    /// <summary>
    /// Summarizes information about how a Machinations Object Property is mapped to a Diagram.
    /// Since a single <see cref="StatesAssociation"/> is supported, these are the precise coordinates of a Machinations Object Property.
    /// See <see cref="MachinationsUP.Integration.Binder"/>
    /// </summary>
    [DataContract(Name = "MachinationsDiagramMapping", Namespace = "http://www.machinations.io")]
    public class DiagramMapping
    {

        private string _manifestName;

        /// <summary>
        /// The name of the Machinations Object that has this property.
        /// See <see cref="MnGameObject"/>
        /// See <see cref="MachinationsUP.Engines.Unity.IMnScriptableObject"/>
        /// </summary>
        [DataMember()]
        public string ManifestName
        {
            get => _manifestName;
            set => _manifestName = value;
        }

        private string _type;

        /// <summary>
        /// The type of Machinations Element this is. Currently taken as string, directly from the server response.
        /// TODO: convert to enum.
        /// </summary>
        [DataMember()]
        public string Type
        {
            get => _type;
            set => _type = value;
        }

        private string _label;

        /// <summary>
        /// String Label in the Diagram.
        /// </summary>
        [DataMember()]
        public string Label
        {
            get => _label;
            set => _label = value;
        }


        private string _propertyName;

        /// <summary>
        /// The name of this Property.
        /// </summary>
        [DataMember()]
        public string PropertyName
        {
            get => _propertyName;
            set => _propertyName = value;
        }

        private string _crossManifestName;

        /// <summary>
        /// When calling the Matches function, if a CrossManifestName has been specified, the Machinations Game Object Name will
        /// be IGNORED during the comparison. This functionality is used when the same DiagramElementsID is used in MULTIPLE Manifests.
        /// This is necessary because without it, this DiagramMapping CANNOT be used to match with the proper one over
        /// in MnDataLayer _sourceElements. This is because different Game Objects or Scriptable Objects may use different names inside
        /// their Manifests. This property allows us to define cross-Manifest names that remove this limitation while allowing us to
        /// keep a single array of unique Source Elements (which is also easier to manage & conceptualize). Then again, perhaps this comment
        /// is harder to conceptualize. But hey, life's tough, welcome to Axonn's over-engineered coding (or is it?).
        /// </summary>
        [DataMember()]
        public string CrossManifestName
        {
            get => _crossManifestName;
            set => _crossManifestName = value;
        }

        private StatesAssociation _statesAssociation;

        /// <summary>
        /// The States Association where this Property applies.
        /// </summary>
        [DataMember()]
        public StatesAssociation StatesAssoc
        {
            get => _statesAssociation;
            set => _statesAssociation = value;
        }

        private int _diagramElementID;

        /// <summary>
        /// Machinations Back-end Element ID.
        /// </summary>
        [DataMember()]
        public int DiagramElementID
        {
            get => _diagramElementID;
            set => _diagramElementID = value;
        }

        /// <summary>
        /// How to handle situations when different values come from the Diagram.
        /// //TODO: Not implemented yet.
        /// </summary>
        public OverwriteRules OvewriteRule { get; set; }

        /// <summary>
        /// When this Mapping is used inside a Scriptable Object, this will be the Element Base that is represented in the Editor.
        /// </summary>
        public ElementBase EditorElementBase { get; set; }

        /// <summary>
        /// A Default ElementBase, to be used only during OFFLINE mode.
        /// </summary>
        public ElementBase DefaultElementBase { get; set; }

        /// <summary>
        /// The last value received from the Machinations back-end.
        /// </summary>
        [DataMember()]
        public ElementBase CachedElementBase { get; set; }

        /// <summary>
        /// An ElementBase that will override the one in the Diagram.
        /// </summary>
        public ElementBase OverrideElementBase { get; set; }

        /// <summary>
        /// Default constructor. Necessary for field-based initializations.
        /// </summary>
        public DiagramMapping ()
        {
        }

        /// <summary>
        /// Constructor that allows specifying the back-end Element ID. Required to place DiagramMappings on items that weren't yet
        /// requested by the game, but have come during a full diagram init request.
        /// <see cref="MachinationsUP.Engines.Unity.GameComms.MnService"/>
        /// <param name="diagramElementID">Machinations Back-end Element ID.</param>
        /// </summary>
        public DiagramMapping (int diagramElementID)
        {
            DiagramElementID = diagramElementID;
        }

        /// <summary>
        /// Verifies if this DiagramMapping matches the provided criteria.
        /// </summary>
        /// <param name="name">Game Object name to match.</param>
        /// /// <param name="propertyName">Game Object Property name to match.</param>
        /// <param name="statesAssociation">States Association to verify.</param>
        /// <param name="stringifyStatesAssociation">In the case of cached States Association, string values will be used for comparison.</param>
        public bool Matches (string name, string propertyName, StatesAssociation statesAssociation,
            bool stringifyStatesAssociation)
        {
            //A DiagramMapping matches some criteria IF:
            //The Manifest name is the same OR, there is a CrossManifestName for this property
            return (ManifestName == name || CrossManifestName != null && CrossManifestName == name)
                   //The property name matches.
                   && PropertyName == propertyName &&
                   (
                       //The StatesAssociation matches, either stringified or...
                       (stringifyStatesAssociation &&
                        (
                            (StatesAssoc == null && statesAssociation == null) ||
                            (StatesAssoc != null && statesAssociation != null && StatesAssoc.ToString() == statesAssociation.ToString())))
                       ||
                       //In Object form.
                       (!stringifyStatesAssociation && StatesAssoc == statesAssociation)
                   );
        }

        /// <summary>
        /// Verifies if this DiagramMapping matches the Game Object-specific properties of an ElementBinder and
        /// any given <see cref="StatesAssociation"/>.
        /// </summary>
        /// <param name="elementBinder">Element Binder to verify.</param>
        /// <param name="statesAssociation">States Association to verify.</param>
        /// <param name="stringifyStatesAssociation">In the case of cached States Association, string values will be used for comparison.</param>
        /// <param name="universalName"></param>
        public bool Matches (ElementBinder elementBinder, StatesAssociation statesAssociation, bool stringifyStatesAssociation,
            string universalName = null)
        {
            string nameMatch;
            if (universalName != null)
                nameMatch = universalName;
            else
                nameMatch = elementBinder.ParentGameObject != null
                    ? elementBinder.ParentGameObject.Name
                    : elementBinder.ParentScriptableObject?.Manifest.Name;

            return Matches(nameMatch, elementBinder.GameObjectPropertyName, statesAssociation, stringifyStatesAssociation);
        }

        /// <summary>
        /// Verifies if this DiagramMapping matches another DiagramMapping.
        /// </summary>
        /// <param name="diagramMapping">Diagram Mapping to verify.</param>
        /// <param name="stringifyStatesAssociation">In the case of cached States Association, string values will be used for comparison.</param>
        public bool Matches (DiagramMapping diagramMapping, bool stringifyStatesAssociation)
        {
            return Matches(diagramMapping.ManifestName, diagramMapping.PropertyName, diagramMapping.StatesAssoc,
                stringifyStatesAssociation);
        }

        override public string ToString ()
        {
            return "DiagramMapping for " + ManifestName + "." + PropertyName + "." +
                   (_statesAssociation != null ? _statesAssociation.Title : "N/A") +
                   " bound to DiagramID: " + DiagramElementID;
        }

    }
}