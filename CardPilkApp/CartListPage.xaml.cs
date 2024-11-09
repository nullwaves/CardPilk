using CardPilkApp.ViewModels;

namespace CardPilkApp;

public partial class CartListPage : ContentPage
{
	private CartListViewModel _viewmodel;
	public CartListPage()
	{
        InitializeComponent();
		_viewmodel = new(App.CardLib.GetManager());
		BindingContext = _viewmodel;
	}
}