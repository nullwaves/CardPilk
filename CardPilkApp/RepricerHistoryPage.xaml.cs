using CardPilkApp.ViewModels;

namespace CardPilkApp;

public partial class RepricerHistoryPage : ContentPage
{
	RepricerHistoryViewModel viewmodel;

	public RepricerHistoryPage()
	{
		InitializeComponent();
		viewmodel = new(App.CardLib.GetManager());
		BindingContext = viewmodel;
	}
}