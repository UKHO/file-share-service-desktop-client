namespace UKHO.FSSDesktop.UI.Navigation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Windows.Data;
    using ModernWpf.Controls;

    public class NavigationItem : NavigationViewItem, INavigationItem
    {
        private readonly ObservableCollection<NavigationItem> _items;
        private readonly Type _modelType;
        private readonly IEnumerable<string> _claims;

        public NavigationItem(Type modelType, string title, IconElement icon, IEnumerable<string> claims, bool selectOnInvoke)
        {
            _modelType = modelType;
            _claims = claims;

            Icon = icon;
            Content = title;
            SelectsOnInvoked = selectOnInvoke;

            _items = new ObservableCollection<NavigationItem>();

            var binding = new Binding
            {
                Source = _items
            };

            SetBinding(MenuItemsSourceProperty, binding);
        }

        public Type ModelType => _modelType;

        public string Title => (string) Content;

        public IEnumerable<string> Claims => _claims;

        public INavigationItem AddNavigation(Type modelType, string title, IconElement icon, IEnumerable<string> claims, bool selectOnInvoke = true)
        {
            var item = new NavigationItem(modelType, title, icon, claims, selectOnInvoke);

            _items.Add(item);
            return item;
        }
    }
}