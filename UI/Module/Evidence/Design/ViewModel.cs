using System.Windows.Input;

namespace Evidence.Design
{
  internal sealed class ViewModel : IViewModel
  {
    public IBrowseViewModel BrowseViewModel => new BrowseViewModel();
    public IManageViewModel ManageViewModel => new ManageViewModel();
    public ICommand Import => throw new System.NotImplementedException();
  }
}
