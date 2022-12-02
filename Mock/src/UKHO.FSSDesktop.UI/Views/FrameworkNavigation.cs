namespace UKHO.FSSDesktop.UI.Views
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Messaging;
    using FSSDesktop.Messages.Security;
    using Messages;
    using ModernWpf.Controls;
    using Navigation;

    internal class FrameworkNavigation : ObservableObject, IRecipient<AuthenticationMessage>
    {
        private readonly List<INavigationItem> _allNavigationItems;
        private readonly List<INavigationItem> _filteredNavigationItems;


        public FrameworkNavigation()
        {
            _allNavigationItems = new List<INavigationItem>();
            _filteredNavigationItems = new List<INavigationItem>();

            WeakReferenceMessenger.Default.Register(this);
        }

        public IEnumerable<INavigationItem> NavigationItems => _filteredNavigationItems;

        public INavigationItem AddNavigation(Type modelType, string title, IconElement icon, IEnumerable<string> claims, bool selectOnInvoke = true)
        {
            var item = new NavigationItem(modelType, title, icon, claims, selectOnInvoke);

            _allNavigationItems.Add(item);

            return item;
        }

        public void Receive(AuthenticationMessage message)
        {
            _filteredNavigationItems.Clear();

            if (message.Context != null)
            {
                foreach (var item in _allNavigationItems)
                {
                    var commonClaims = item.Claims.Where(x => message.Context.Claims.Select(c => c.Name).Contains(x));

                    if (commonClaims.Any())
                    {
                        _filteredNavigationItems.Add(item);
                    }
                }
            }
        }
    }
}