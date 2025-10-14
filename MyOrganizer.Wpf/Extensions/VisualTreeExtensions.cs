using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace MyOrganizer.Wpf.Extensions
{
    internal static class VisualTreeExtensions
    {
        public static IEnumerable<DependencyObject> GetVisualDescendants(this DependencyObject root)
        {
            if (root == null) yield break;

            var queue = new Queue<DependencyObject>();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                int count = VisualTreeHelper.GetChildrenCount(current);

                for (int i = 0; i < count; i++)
                {
                    var child = VisualTreeHelper.GetChild(current, i);
                    yield return child;
                    queue.Enqueue(child);
                }
            }
        }
    }

}
