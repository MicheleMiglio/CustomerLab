using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CLab.Views.Controls
{
    /// <summary>
    /// Impila i figli in verticale. Ogni figlio prende la sua altezza naturale;
    /// se la somma supera lo spazio disponibile, i figli piccoli restano intatti
    /// e lo spazio residuo viene diviso equamente tra quelli più grandi.
    /// </summary>
    public class FitHeightStackPanel : Panel
    {
        private double[] _desired = Array.Empty<double>();
        private double[] _heights = Array.Empty<double>();

        protected override Size MeasureOverride(Size available)
        {
            int n = InternalChildren.Count;
            _desired = new double[n];
            _heights = new double[n];

            for (int i = 0; i < n; i++)
            {
                var c = InternalChildren[i];
                c.Measure(new Size(available.Width, double.PositiveInfinity));
                _desired[i] = c.DesiredSize.Height;
            }

            _heights = Distribuisci(_desired, available.Height);

            double width = 0, total = 0;
            for (int i = 0; i < n; i++)
            {
                var c = InternalChildren[i];
                c.Measure(new Size(available.Width, _heights[i]));
                width = Math.Max(width, c.DesiredSize.Width);
                total += _heights[i];
            }

            return new Size(double.IsInfinity(available.Width) ? width : available.Width, total);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            double y = 0;
            for (int i = 0; i < InternalChildren.Count && i < _heights.Length; i++)
            {
                InternalChildren[i].Arrange(new Rect(0, y, finalSize.Width, _heights[i]));
                y += _heights[i];
            }
            return finalSize;
        }

        private static double[] Distribuisci(double[] desired, double disponibile)
        {
            var h = (double[])desired.Clone();
            if (double.IsInfinity(disponibile) || desired.Sum() <= disponibile)
                return h;

            double rimanente = disponibile;
            int count = desired.Length;
            foreach (int i in Enumerable.Range(0, desired.Length).OrderBy(i => desired[i]))
            {
                h[i] = Math.Min(desired[i], rimanente / count);
                rimanente -= h[i];
                count--;
            }
            return h;
        }
    }
}