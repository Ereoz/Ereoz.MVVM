using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Ereoz.MVVM
{
    /// <summary>
    /// Методы расширения <see cref="DependencyObject" />, позволяющие выполнять поиск по дереву элементов.
    /// </summary>
    public static class TreeHelper
    {
        /// <summary>
        /// Рекурсивно ищет ближайший родительский элемент заданного типа, по дереву родительских элементов.
        /// </summary>
        /// <typeparam name="T">Тип родительского элемента.</typeparam>
        /// <param name="child">Целевой объект.</param>
        /// <returns>Родительский элемент заданного типа или <see langword="null" />, если в дереве родительских элементов
        /// отсутствует родительский элемент заданного типа.</returns>
        public static T TryFindParent<T>(this DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = child.GetParentObject();

            if (parentObject == null) return null;

            T parent = parentObject as T;
            if (parent != null)
            {
                return parent;
            }
            else
            {
                return TryFindParent<T>(parentObject);
            }
        }

        /// <summary>
        /// Возвращает родительский элемент.
        /// </summary>
        /// <param name="child">Целевой объект</param>
        /// <returns>Родительский элемент.</returns>
        public static DependencyObject GetParentObject(this DependencyObject child)
        {
            if (child == null) return null;

            ContentElement contentElement = child as ContentElement;
            if (contentElement != null)
            {
                DependencyObject parent = ContentOperations.GetParent(contentElement);
                if (parent != null) return parent;

                FrameworkContentElement fce = contentElement as FrameworkContentElement;
                return fce != null ? fce.Parent : null;
            }

            FrameworkElement frameworkElement = child as FrameworkElement;
            if (frameworkElement != null)
            {
                DependencyObject parent = frameworkElement.Parent;
                if (parent != null) return parent;
            }

            return VisualTreeHelper.GetParent(child);
        }

        /// <summary>
        /// Рекурсивно ищет все дочерние элементы заданного типа, по дереву дочерних элементов.
        /// </summary>
        /// <typeparam name="T">Тип дочернего элемента.</typeparam>
        /// <param name="source">Целевой объект.</param>
        /// <returns>Все найденные дочерние элементы заданного типа.</returns>
        public static IEnumerable<T> FindChildren<T>(this DependencyObject source) where T : DependencyObject
        {
            if (source != null)
            {
                var childs = source.GetChildObjects();

                foreach (DependencyObject child in childs)
                {
                    if (child != null && child is T)
                        yield return (T)child;

                    foreach (T descendant in FindChildren<T>(child))
                        yield return descendant;
                }
            }
        }

        /// <summary>
        /// Возвращает дочерние элементы.
        /// </summary>
        /// <param name="parent">Целевой объект.</param>
        /// <returns>Дочерние элементы.</returns>
        public static IEnumerable<DependencyObject> GetChildObjects(this DependencyObject parent)
        {
            if (parent == null)
                yield break;

            if (parent is ContentElement || parent is FrameworkElement)
            {
                foreach (object obj in LogicalTreeHelper.GetChildren(parent))
                {
                    var depObj = obj as DependencyObject;

                    if (depObj != null)
                        yield return (DependencyObject)obj;
                }
            }
            else
            {
                int count = VisualTreeHelper.GetChildrenCount(parent);

                for (int i = 0; i < count; i++)
                    yield return VisualTreeHelper.GetChild(parent, i);
            }
        }
    }
}
