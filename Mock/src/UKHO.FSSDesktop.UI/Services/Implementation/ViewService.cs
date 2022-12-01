using System;
using System.Linq;
using System.Windows;

namespace UKHO.FSSDesktop.UI.Services.Implementation
{
    internal class ViewService : IViewService
    {
        private readonly IServiceProvider _provider;

        public ViewService(IServiceProvider provider)
        {
            _provider = provider;
        }

        public T GetViewFor<T>(object model) where T : FrameworkElement
        {
            var modelTypeName = model.GetType().Name;

            string viewTypeName;

            if (modelTypeName.EndsWith("ViewModel"))
            {
                viewTypeName = modelTypeName.Replace("Model", "");
            }
            else
            {
                viewTypeName = modelTypeName.EndsWith("Model")
                    ? modelTypeName.Replace("Model", "View")
                    : $"{modelTypeName}View";
            }

            var viewType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .FirstOrDefault(x => x.Name == viewTypeName);

            if (viewType == null)
            {
                throw new ArgumentException($"Cannot find view for view type {model.GetType()}");
            }

            var view = (T) _provider.GetService(viewType)!;
            view.DataContext = model;

            return view;
        }
    }
}