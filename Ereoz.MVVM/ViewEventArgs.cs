using System;
using System.Windows.Controls;

namespace Ereoz.MVVM
{
    public class ViewEventArgs : EventArgs
    {
        public Page View { get; set; }
    }
}
