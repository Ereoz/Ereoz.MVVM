namespace Ereoz.MVVM
{
    public class ViewModelBase : NotifyPropertyChanged
    {
        public object View { get; set; }
        public object WindowDialogResult { get; set; }
        public virtual void ParametersReceived(params object[] parameters) { }
    }
}
