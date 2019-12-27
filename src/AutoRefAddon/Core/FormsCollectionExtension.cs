﻿using System.Collections;
using System.Reflection;
using System.Windows.Forms;

namespace AutoRef
{
    internal static class FormsCollectionExtensions
    {
        #region Fields

        static readonly BindingFlags fieldBindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;

        #endregion

        #region Methods

        public static void AddListChangedEventHandler(this FormCollection collection, ListChangedEventHandler eventHandler, ListChangedType changedType)
        {
            var innerListlistFieldInfo = collection.GetType().BaseType.GetField("list", fieldBindingFlags);
            var innerlist = innerListlistFieldInfo?.GetValue(Application.OpenForms);

            if (!(innerlist is ObservableArrayList))
            {
                var newInnerList = new ObservableArrayList();

                foreach (var item in (innerlist as ArrayList))
                {
                    newInnerList.Add(item);
                }

                innerListlistFieldInfo.SetValue(Application.OpenForms, newInnerList);
            }

            if (innerListlistFieldInfo?.GetValue(Application.OpenForms) is ObservableArrayList currentInnerList)
            {
                switch (changedType)
                {
                    case ListChangedType.Added:
                        currentInnerList.Added += eventHandler;
                        break;
                    case ListChangedType.Removed:
                        currentInnerList.Removed += eventHandler;
                        break;
                }
            }
        }

        public static void RemoveListChangedEventHandler(this FormCollection collection, ListChangedEventHandler eventHandler, ListChangedType changedType)
        {
            var innerListlistFieldInfo = collection.GetType().BaseType.GetField("list", fieldBindingFlags);
            var innerlist = innerListlistFieldInfo?.GetValue(Application.OpenForms);

            if (!(innerlist is ObservableArrayList))
            {
                var newInnerList = new ObservableArrayList();
                foreach (var item in (innerlist as ArrayList))
                {
                    newInnerList.Add(item);
                }

                innerListlistFieldInfo.SetValue(Application.OpenForms, newInnerList);
            }

            if (innerListlistFieldInfo?.GetValue(Application.OpenForms) is ObservableArrayList currentInnerList)
            {
                switch (changedType)
                {
                    case ListChangedType.Added:
                        currentInnerList.Added -= eventHandler;
                        break;
                    case ListChangedType.Removed:
                        currentInnerList.Removed -= eventHandler;
                        break;
                }
            }
        }

        #endregion
    }
}