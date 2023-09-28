﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows;

namespace ReportYTracker
{
    public class ProgressBarSmoother
    {
        public static double GetSmoothValue(DependencyObject obj)
        {
            return (double)obj.GetValue(SmoothValueProperty);
        }

        public static void SetSmoothValue(DependencyObject obj, double value)
        {
            obj.SetValue(SmoothValueProperty, value);
        }

        public static readonly DependencyProperty SmoothValueProperty =
            DependencyProperty.RegisterAttached("SmoothValue", typeof(double), typeof(ProgressBarSmoother), new PropertyMetadata(0.0, OnSmoothChangingValue));

        private static void OnSmoothChangingValue(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var animation = new DoubleAnimation((double)e.OldValue, (double)e.NewValue, new TimeSpan(0, 0, 0, 0, 250));
            var pb = (ProgressBar)obj;
            if (pb == null) throw new ArgumentNullException(nameof(pb));
            pb.BeginAnimation(ProgressBar.ValueProperty, animation, HandoffBehavior.Compose);
        }
    }
}
