using Ereoz.Abstractions.DI;
using Ereoz.Abstractions.MVVM;
using Ereoz.WindowManagement;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace Ereoz.MVVM
{
    public class NavigationManager : INavigationManager
    {
        private readonly Dictionary<Type, Type> _views;
        private readonly Dictionary<Type, Type> _viewModels;
        private IServiceContainer _container;

        private Type _lastRegisteredView;
        private Type _lastRegisteredViewModel;

        public NavigationManager(IServiceContainer container)
        {
            _container = container;
            _views = new Dictionary<Type, Type>();
            _viewModels = new Dictionary<Type, Type>();
        }

        public event EventHandler<EventArgs> ViewChanged;

        public AsSingletoneHandler RegisterViewWithViewModel<TView, TViewModel>() =>
            RegisterViewWithViewModel(typeof(TView), typeof(TViewModel));

        public AsSingletoneHandler RegisterViewWithViewModel(Type view, Type viewModel)
        {
            _viewModels.Add(view, viewModel);
            _views.Add(viewModel, view);

            _lastRegisteredView = view;
            _lastRegisteredViewModel = viewModel;

            return AsSingletone;
        }

        public void AutoRegisterAllViewsWithViewModels(List<Type> allTypes)
        {
            var views = allTypes
                .Where(t => t.IsSubclassOf(typeof(UserControl))
                         || t.IsSubclassOf(typeof(Page))
                         || t.IsSubclassOf(typeof(Window)));

            var viewModels = allTypes
                .Where(t => t.Namespace != null
                         && t.Namespace.Contains("ViewModels")
                         && t.Name.EndsWith("VM"));

            foreach (var view in views)
            {
                string viewName = view.Name;

                if (viewName.EndsWith("View") || viewName.EndsWith("Page"))
                    viewName = viewName.Substring(0, viewName.Length - 4);
                else if (viewName.EndsWith("Window"))
                    viewName = viewName.Substring(0, viewName.Length - 6);

                if (viewModels.FirstOrDefault(it => it.Name.Substring(0, it.Name.Length - 2) == viewName) is Type viewModel)
                    RegisterViewWithViewModel(view, viewModel);
            }
        }

        public T CreateMainWindow<T>(WindowLocation appSettings = null)
        {
            var view = _container.Resolve<T>();

            if (_viewModels.TryGetValue(typeof(T), out Type vm))
                (view as Window).DataContext = (ViewModelBase)_container.Resolve(vm);

            SetDataContextUserControls(view as Window);
            new WindowStateManager(view as Window, appSettings);

            return view;
        }

        public object Navigate<T>(object sender, params object[] parameters) =>
            Navigate(sender, typeof(T), parameters);

        public object Navigate(object sender, Type viewOrViewModel, params object[] parameters)
        {
            object view = null;
            ViewModelBase viewModel = null;

            if (viewOrViewModel.IsSubclassOf(typeof(ViewModelBase)))
            {
                view = _container.Resolve(_views[viewOrViewModel]);
                viewModel = (ViewModelBase)_container.Resolve(viewOrViewModel);
            }
            else
            {
                view = _container.Resolve(viewOrViewModel);
                
                if (_viewModels.TryGetValue(viewOrViewModel, out Type vm))
                    viewModel = (ViewModelBase)_container.Resolve(vm);
            }

            SetDataContextUserControls((DependencyObject)view);

            if (viewModel != null)
            {
                viewModel.View = view;

                if (parameters != null)
                    viewModel.ParametersReceived(parameters);
            }

            if (view is Page page)
            {
                if (viewModel != null)
                    page.DataContext = viewModel;

                ViewChanged?.Invoke(this, new ViewEventArgs { View = page });
            }
            else if (view is Window window)
            {
                if (viewModel != null)
                    window.DataContext = viewModel;

                if (sender is ViewModelBase callingViewModel)
                {
                    if (callingViewModel.View is Window directOwner)
                        window.Owner = directOwner;
                    else if (TreeHelper.TryFindParent<Window>((DependencyObject)callingViewModel.View) is Window nearestOwner)
                        window.Owner = nearestOwner;
                    else
                        window.Owner = Application.Current.MainWindow;
                }
                else
                {
                    window.Owner = Application.Current.MainWindow;
                }

                if (parameters?.Any(it => it is string str && str.ToLower() == "showdialog") == true)
                {
                    return window.ShowDialog() == true ? viewModel?.WindowDialogResult : null;
                }
                else
                    window.Show();
            }
            else
            {
                throw new Exception($"Не поддерживаемый для навигации тип представления: {view.GetType()}");
            }

            return null;
        }

        private void SetDataContextUserControls(DependencyObject view)
        {
            foreach (var userControl in TreeHelper.FindChildren<UserControl>(view))
            {
                if (_viewModels.TryGetValue(userControl.GetType(), out Type userControlViewModel))
                    userControl.DataContext = userControlViewModel;
            }
        }

        private void AsSingletone()
        {
            _container.Register(_lastRegisteredViewModel).AsSingletone();

            if (!_lastRegisteredView.IsSubclassOf(typeof(Window)))
                _container.Register(_lastRegisteredView).AsSingletone();
        }
    }
}
