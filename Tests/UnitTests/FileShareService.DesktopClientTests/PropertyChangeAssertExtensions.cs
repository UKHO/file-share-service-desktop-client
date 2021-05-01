using System;
using System.Collections.Generic;
using System.ComponentModel;
using NUnit.Framework;

namespace FileShareService.DesktopClientTests
{
    public static class PropertyChangeAssertExtensions
    {
        public static void AssertPropertiesChanged(
            this INotifyPropertyChanged systemUnderTest,
            Action testCode,
            params string[] propertyNames)
        {
            Assert.NotNull(systemUnderTest);
            CollectionAssert.IsNotEmpty(propertyNames);

            var propertyChangesFired = new HashSet<string>();

            void PropertyChangedEventHandler(object? sender, PropertyChangedEventArgs args)
            {
                if (args.PropertyName != null) propertyChangesFired.Add(args.PropertyName);
            }

            systemUnderTest.PropertyChanged += PropertyChangedEventHandler;
            try
            {
                testCode();
                CollectionAssert.IsSubsetOf(propertyNames, propertyChangesFired,
                        $"One or more PropertyChanged events did not fire when executing testCode");
            }
            finally
            {
                systemUnderTest.PropertyChanged -= PropertyChangedEventHandler;
            }
        } 
        public static void AssertPropertyChanged(
            this INotifyPropertyChanged systemUnderTest,
            string propertyName,
            Action testCode)
        {
            Assert.NotNull(systemUnderTest);
            var propertyChangeHappened = false;

            void PropertyChangedEventHandler(object? sender, PropertyChangedEventArgs args)
            {
                if (string.IsNullOrEmpty(args.PropertyName) || propertyName.Equals(args.PropertyName))
                    propertyChangeHappened = true;
            }

            systemUnderTest.PropertyChanged += PropertyChangedEventHandler;
            try
            {
                testCode();
                if (!propertyChangeHappened)
                    Assert.Fail(
                        $"PropertyChanged event did not fire for property \"{propertyName}\" when executing testCode");
            }
            finally
            {
                systemUnderTest.PropertyChanged -= PropertyChangedEventHandler;
            }
        }

        public static void AssertPropertyChangedNotFired(
            this INotifyPropertyChanged systemUnderTest,
            string propertyName,
            Action testCode)
        {
            Assert.NotNull(systemUnderTest);
            var propertyChangeHappened = false;

            void PropertyChangedEventHandler(object? sender, PropertyChangedEventArgs args)
            {
                if (string.IsNullOrEmpty(args.PropertyName) || propertyName.Equals(args.PropertyName))
                    propertyChangeHappened = true;
            }

            systemUnderTest.PropertyChanged += PropertyChangedEventHandler;
            try
            {
                testCode();
                if (propertyChangeHappened)
                    Assert.Fail(
                        $"PropertyChanged event fired for property \"{propertyName}\" when executing testCode");
            }
            finally
            {
                systemUnderTest.PropertyChanged -= PropertyChangedEventHandler;
            }
        }
    }
}