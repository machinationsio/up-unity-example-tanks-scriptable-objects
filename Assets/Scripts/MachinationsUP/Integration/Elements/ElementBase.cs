using System;
using System.Runtime.Serialization;
using MachinationsUP.Engines.Unity;
using MachinationsUP.Integration.Binder;

namespace MachinationsUP.Integration.Elements
{
    /// <summary>
    /// Wraps a Machinations Value.
    /// </summary>
    [DataContract(Name = "MachinationsElementBase", Namespace = "http://www.machinations.io")]
    [KnownType(typeof(FormulaElement))]
    [Serializable]
    public class ElementBase
    {
        /// <summary>
        /// Permits the Unity Property inspector connection. Setting this manually will have no effect over <see cref="CurrentValue"/>.
        /// See <see cref="MachinationsUP.Engines.Unity.EditorExtensions.ElementBaseDrawer"/>
        /// </summary>
        public int _serializableValue;
        
        /// <summary>
        /// The value received from Machinations.
        /// </summary>
        [DataMember()]
        public int BaseValue { get; set; }

        /// <summary>
        /// Parent Element Binder.
        /// </summary>
        internal ElementBinder ParentElementBinder { get; }

        protected int _currentValue;

        /// <summary>
        /// INT value of this ElementBase.
        /// May be overrided in Child Classes.
        /// </summary>
        [DataMember()]
        virtual public int CurrentValue
        {
            get => _currentValue;
            protected set
            {
                _currentValue = value;
                _serializableValue = value;
            }
        }

        /// <summary>
        /// Top cap.
        /// </summary>
        [DataMember()]
        public int MaxValue { get; set; }

        /// <summary>
        /// Bottom cap. NOT USED YET.
        /// </summary>
        [DataMember()]
        public int MinValue { get; set; }

        //For Serialization only!
        private ElementBase ()
        {
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="parentBinder">Parent Element Binder.</param>
        /// <param name="baseValue">Value to start with.</param>
        public ElementBase (int baseValue = -1, ElementBinder parentBinder = null)
        {
            ParentElementBinder = parentBinder;
            BaseValue = baseValue;
            MaxValue = -1;
            Reset();
        }

        /// <summary>
        /// Initializes this ElementBase from another one.
        /// </summary>
        /// <param name="source"></param>
        public ElementBase (ElementBase source)
        {
            ParentElementBinder = source.ParentElementBinder;
            BaseValue = source.BaseValue;
            MaxValue = source.MaxValue;
            Reset();
        }

        /// <summary>
        /// Resets the Current Value to the Base Value.
        /// </summary>
        virtual protected void Reset ()
        {
            CurrentValue = BaseValue;
        }

        /// <summary>
        /// Makes sure the value doesn't exceed Min or Max caps.
        /// </summary>
        private void Clamp ()
        {
            if (MaxValue != -1 && CurrentValue > MaxValue) CurrentValue = MaxValue;
        }

        /// <summary>
        /// Changes the Int Value with the given amount.
        /// TODO: think how to support other than MInt.
        /// </summary>
        /// <param name="amount"></param>
        public void ChangeValueWith (int amount)
        {
            CurrentValue += amount;
            Clamp();
        }

        /// <summary>
        /// Changes the Value of the ElementBase to the given one.
        /// </summary>
        /// <param name="value">New Value.</param>
        public void ChangeValueTo (int value)
        {
            CurrentValue = value;
            Clamp();
        }

        /// <summary>
        /// Changes the value of this ElementBase to the given one.
        /// This function will also trigger an update towards the Machinations Back-End.
        /// </summary>
        /// <param name="value">New Value.</param>
        public void ChangeValueFromEditor (int value)
        {
            int previousValue = CurrentValue;
            ChangeValueTo(value);
            //TODO restore this to allow sync back to Machinations.
            //MachinationsGameLayer.Instance.EmitGameUpdateDiagramElementsRequest(this, previousValue);
        }

        /// <summary>
        /// Returns a duplicate of this Element Base. Required in <see cref="MachinationsUP.Engines.Unity.MachinationsGameLayer"/> in CreateElement.
        /// </summary>
        /// <param name="parentBinder">Parent Element Binder.</param>
        /// <returns></returns>
        virtual public ElementBase Clone (ElementBinder parentBinder)
        {
            return new ElementBase(BaseValue, parentBinder);
        }

        override public string ToString ()
        {
            return BaseValue.ToString();
        }

    }
}
