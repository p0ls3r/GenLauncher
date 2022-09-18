using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace GenLauncherNet
{
    public class UpdateButton : Button
    {
        public bool IsBlinking { get { return (bool)GetValue(IsBlinkingProperty); } set { SetValue(IsBlinkingProperty, value); } }
        public static readonly DependencyProperty IsBlinkingProperty = DependencyProperty.Register("IsBlinking", typeof(bool), typeof(UpdateButton), new PropertyMetadata(false, (d, e) => { (d as UpdateButton).IsBlinkingChanged(); }));
        public int BlinkCount { get { return (int)GetValue(BlinkCountProperty); } set { SetValue(BlinkCountProperty, value); } }
        public static readonly DependencyProperty BlinkCountProperty = DependencyProperty.Register("BlinkCount", typeof(int), typeof(UpdateButton), new PropertyMetadata(10));

        Border blinkBorder;

        public UpdateButton()
        {
            blinkBorder = new Border();

            blinkBorder.Background = EntryPoint.Colors.GenLauncherDarkFillColor;

            blinkBorder.Opacity = 0;
            Loaded += delegate
            {
                try
                {
                    ContentPresenter cp = FindChild<ContentPresenter>(this);
                    if (cp != null)
                    {
                        Decorator decor = cp.Parent as Decorator;
                        Grid grid = new Grid();
                        decor.Child = grid;
                        grid.Children.Add(cp);
                        grid.Children.Add(blinkBorder);
                    }
                }
                catch { }
            };
        }

        void IsBlinkingChanged()
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;
            if (IsBlinking)
            {
                DoubleAnimation blinkAnime = new DoubleAnimation(0.5, new Duration(TimeSpan.FromMilliseconds(500)), FillBehavior.Stop);
                blinkAnime.AutoReverse = true;
                blinkAnime.RepeatBehavior = new RepeatBehavior(BlinkCount);
                blinkBorder?.BeginAnimation(OpacityProperty, blinkAnime);
            }
            else
                blinkBorder.BeginAnimation(OpacityProperty, null);
        }

        T FindChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is T) return (T)child;
                else
                {
                    T childOfChild = FindChild<T>(child);
                    if (childOfChild != null) return childOfChild;
                }
            }
            return null;
        }
    }
}
