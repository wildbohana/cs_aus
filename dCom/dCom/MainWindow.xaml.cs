using dCom.Exceptions;
using dCom.ViewModel;
using System;
using System.Windows;
using System.Windows.Input;

namespace dCom
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		// Inicijalizacija glavnog prozora
		public MainWindow()
		{
			try
			{
				InitializeComponent();
				this.Closed += Window_Closed;
				DataContext = new MainViewModel();				
			}
			catch (ConfigurationException confEx)
			{
				MessageBox.Show($"{confEx.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				this.Close();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Unexpected error occured!\nStack trace:\n{ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				this.Close();
			}
		}

		// Dvoklik otvara novi prozor za pregled stavke
		private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			ControlWindow cw = new ControlWindow(dgPoints.SelectedItem as BasePointItem);
			cw.Owner = this;
			cw.ShowDialog();
		}

		// Poziv Dispose() metode prilikom zatvaranja prozora
		private void Window_Closed(object sender, EventArgs e)
		{
			(DataContext as IDisposable).Dispose();
		}
	}
}