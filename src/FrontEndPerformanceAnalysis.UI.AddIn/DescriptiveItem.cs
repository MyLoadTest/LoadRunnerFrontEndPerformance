using System;
using System.Collections.Generic;
using System.Linq;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn
{
    internal sealed class DescriptiveItem<T> : IEquatable<DescriptiveItem<T>>
    {
        #region Constants and Fields

        private static readonly EqualityComparer<T> ValueComparer = EqualityComparer<T>.Default;

        #endregion

        #region Constructors

        public DescriptiveItem(T value, string text)
        {
            Value = value;
            Text = text;
        }

        public DescriptiveItem(T value)
            : this(value, value.ToStringSafelyInvariant())
        {
            // Nothing to do
        }

        #endregion

        #region Public Properties

        public T Value
        {
            get;
        }

        public string Text
        {
            get;
        }

        #endregion

        #region Public Methods

        public override bool Equals(object obj)
        {
            return Equals(obj as DescriptiveItem<T>);
        }

        public override int GetHashCode()
        {
            return ValueComparer.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Text;
        }

        #endregion

        #region IEquatable<DescriptiveItem<T>> Members

        public bool Equals(DescriptiveItem<T> other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            if (ReferenceEquals(other, this))
            {
                return true;
            }

            return ValueComparer.Equals(other.Value, Value);
        }

        #endregion
    }
}