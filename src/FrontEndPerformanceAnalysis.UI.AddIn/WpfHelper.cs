using System;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Windows;
using Omnifactotum;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn
{
    //// WpfHelper is borrowed from Omnifactotum.Wpf (being developed).
    internal static class WpfHelper
    {
        #region Public Methods

        public static bool IsInDesignMode()
        {
            try
            {
                return (bool)DesignerProperties
                    .IsInDesignModeProperty
                    .GetMetadata(typeof(DependencyObject))
                    .DefaultValue;
            }
            catch (Exception ex)
            {
                if (ex.IsFatal())
                {
                    throw;
                }

                return false;
            }
        }

        #endregion

        #region For<T> Class

        public static class For<T>
        {
            #region Public Methods

            public static DependencyProperty RegisterDependencyProperty<TProperty>(
                Expression<Func<T, TProperty>> propertyGetterExpression,
                PropertyMetadata typeMetadata = null,
                ValidateValueCallback validateValueCallback = null)
            {
                var propertyInfo = Factotum.For<T>.GetPropertyInfo(propertyGetterExpression);

                if (propertyInfo.DeclaringType != typeof(T))
                {
                    throw new ArgumentException(
                        @"Inconsistency between property expression and declaring object type.",
                        "propertyGetterExpression");
                }

                return DependencyProperty.Register(
                    propertyInfo.Name,
                    propertyInfo.PropertyType,
                    propertyInfo.DeclaringType.EnsureNotNull(),
                    typeMetadata,
                    validateValueCallback);
            }

            public static DependencyPropertyKey RegisterReadOnlyDependencyProperty<TProperty>(
                Expression<Func<T, TProperty>> propertyGetterExpression,
                PropertyMetadata typeMetadata = null,
                ValidateValueCallback validateValueCallback = null)
            {
                var propertyInfo = Factotum.For<T>.GetPropertyInfo(propertyGetterExpression);

                if (propertyInfo.DeclaringType != typeof(T))
                {
                    throw new ArgumentException(
                        @"Inconsistency between property expression and declaring object type.",
                        "propertyGetterExpression");
                }

                return DependencyProperty.RegisterReadOnly(
                    propertyInfo.Name,
                    propertyInfo.PropertyType,
                    propertyInfo.DeclaringType.EnsureNotNull(),
                    typeMetadata,
                    validateValueCallback);
            }

            #endregion
        }

        #endregion
    }
}