using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.Xaml.Interactivity;

namespace Spiffy.Actions;

public sealed class FocusElementAction : DependencyObject, IAction
{
    // Using a DependencyProperty as the backing store for TargetElement.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty TargetElementProperty =
        DependencyProperty.Register("TargetElement", typeof(UIElement), typeof(FocusElementAction), new PropertyMetadata(null));

    public UIElement TargetElement
    {
        get => (UIElement)GetValue(TargetElementProperty);
        set => SetValue(TargetElementProperty, value);
    }


    public object Execute(object sender, object parameter)
    {
        if (TargetElement != null)
        {
            TargetElement.Focus(FocusState.Programmatic);
            return true;
        }
        return false;
    }
}
