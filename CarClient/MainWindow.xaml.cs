using ModelsDLL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace CarClient;

public partial class MainWindow : Window
{
	Command request;
	public ObservableCollection<Car> Cars { get; set; }

	private TcpClient _client;

	public Car Car
	{
		get { return (Car)GetValue(CarProperty); }
		set { SetValue(CarProperty, value); }
	}


	// Using a DependencyProperty as the backing store for Car.  This enables animation, styling, binding, etc...
	public static readonly DependencyProperty CarProperty =
		DependencyProperty.Register("Car", typeof(Car), typeof(MainWindow));


	public bool IsTextBoxAvialable
	{
		get { return (bool)GetValue(IsTextBoxAvialableProperty); }
		set { SetValue(IsTextBoxAvialableProperty, value); }
	}


	// Using a DependencyProperty as the backing store for IsTextBoxAvialable.  This enables animation, styling, binding, etc...
	public static readonly DependencyProperty IsTextBoxAvialableProperty =
		DependencyProperty.Register("IsTextBoxAvialable", typeof(bool), typeof(MainWindow));


	

	public MainWindow()
	{
		InitializeComponent();

		DataContext = this;
		IsTextBoxAvialable = false;
		request = new Command();
		Car = new();
		Cars = new();
		_client = new TcpClient("127.0.0.1", 45678);
	}

	private void Window_Loaded(object sender, RoutedEventArgs e)
		=> cbCommand.ItemsSource = Enum.GetValues(typeof(HttpMethods)).Cast<HttpMethods>();

	private void cbCommand_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (cbCommand.SelectedItem is HttpMethods method)
		{
			request.Method = method;

			switch (method)
			{
				case HttpMethods.GET:
				case HttpMethods.DELETE:
					foreach (var txt in requestGrid.Children.OfType<TextBox>())
					{
						if (txt.Name != "txtId")
							txt.Text = string.Empty;
					}

					IsTextBoxAvialable = false;
					break;

				case HttpMethods.POST:
				case HttpMethods.PUT:
					IsTextBoxAvialable = true;
					break;
			}
		}
	}

	private void Button_Click(object sender, RoutedEventArgs e)
	{
		if (cbCommand.SelectedItem is null)
		{
			MessageBox.Show("Please select any command...", "", MessageBoxButton.OK, MessageBoxImage.Warning);
			return;
		}

		if (cbCommand.SelectedItem is HttpMethods method)
			ExecuteServerCommand(method);
	}

	private async void ExecuteServerCommand(HttpMethods method)
	{
		var stream = _client.GetStream();
		var bw = new BinaryWriter(stream);
		var br = new BinaryReader(stream);

		switch (method)
		{
			case HttpMethods.GET:
				{
					if (Car.Id < 0)
					{
						MessageBox.Show("Entered id is invalid...", "", MessageBoxButton.OK, MessageBoxImage.Error);
						return;
					}

					request.Car = Car;
					var jsonStr = JsonSerializer.Serialize(request);

					bw.Write(jsonStr);

					await Task.Delay(50);

					if (Car.Id == 0)
					{
						var jsonCars = br.ReadString();
						var cars = JsonSerializer.Deserialize<List<Car>>(jsonCars);

						Cars.Clear();

						foreach (var c in cars)
							Cars.Add(c);

						return;
					}

					var jsonResponse = br.ReadString();
					var car = JsonSerializer.Deserialize<Car>(jsonResponse);

					if (car != null)
					{
						Cars.Clear();
						Cars.Add(car);
					}
					else
					{
						MessageBox.Show("Car with such id not found...", "", MessageBoxButton.OK, MessageBoxImage.Error);
						Cars.Clear();
					}

					break;
				}
			case HttpMethods.POST:
				{
					var sb = new StringBuilder();

					if (Car.Id <= 0)
						sb.Append("Entered id is invalid.");
					if (Car.Year < 1960 || Car.Year > DateTime.Now.Year)
						sb.Append("Entered year is invalid.");

					if (string.IsNullOrWhiteSpace(Car.Make)
						|| string.IsNullOrWhiteSpace(Car.Model)
						|| string.IsNullOrWhiteSpace(Car.VIN)
						|| string.IsNullOrEmpty(Car.Color))
						sb.Append("Please enter all required information.");

					if (sb.Length > 0)
					{
						MessageBox.Show(sb.ToString());
						return;
					}

					request.Car = Car;
					var jsonStr = JsonSerializer.Serialize(request);

					bw.Write(jsonStr);

					await Task.Delay(50);

					var isPosted = br.ReadBoolean();
					var resultText = string.Empty;

					if (isPosted)
						resultText = "Added succesfully...";
					else
						resultText = "Car with such id already Exists...";

					MessageBox.Show(resultText);
					Cars.Clear();

					break;
				}
			case HttpMethods.PUT:
				{
					var sb = new StringBuilder();

					if (Car.Id <= 0)
						sb.Append("Entered id is invalid.");
					if (Car.Year < 1960 || Car.Year > DateTime.Now.Year)
						sb.Append("Entered year is invalid.");

					if (string.IsNullOrWhiteSpace(Car.Make)
						|| string.IsNullOrWhiteSpace(Car.Model)
						|| string.IsNullOrWhiteSpace(Car.VIN)
						|| string.IsNullOrEmpty(Car.Color))
						sb.Append("Please enter all required information.");

					if (sb.Length > 0)
					{
						MessageBox.Show(sb.ToString());
						return;
					}

					request.Car = Car;
					var jsonStr = JsonSerializer.Serialize(request);

					bw.Write(jsonStr);

					await Task.Delay(50);

					var isPosted = br.ReadBoolean();
					var resultText = string.Empty;

					if (isPosted)
						resultText = "Updated succesfully...";
					else
						resultText = "Car with such id doesn't Exists...";

					MessageBox.Show(resultText);
					Cars.Clear();

					break;
				}
			case HttpMethods.DELETE:
				{
					if (Car.Id <= 0)
					{
						MessageBox.Show("Entered id is invalid...", "", MessageBoxButton.OK, MessageBoxImage.Error);
						return;
					}

					request.Car = Car;
					var jsonStr = JsonSerializer.Serialize(request);

					bw.Write(jsonStr);

					await Task.Delay(50);

					var isDeleted = br.ReadBoolean();

					var resultText = string.Empty;

					if (isDeleted)
						resultText = "Deleted succesfully...";
					else
						resultText = "Car with such id not found...";

					MessageBox.Show(resultText);
					Cars.Clear();
					break;
				}
		}
	}
}