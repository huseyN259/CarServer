using ModelsDLL;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

List<Car> cars = null!;

if (File.Exists(@"..\..\..\Cars.json"))
	cars = JsonSerializer.Deserialize<List<Car>>(File.ReadAllText(@"..\..\..\Cars.json"))!;

cars ??= new();

var listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 45678);

listener.Start(10);

while (true)
{
	var client = await listener.AcceptTcpClientAsync();
	var sync = new object();

	Console.WriteLine($"Client {client.Client.RemoteEndPoint} accepted.");

	new Task(() =>
	{
		var stream = client.GetStream();
		var bw = new BinaryWriter(stream);
		var br = new BinaryReader(stream);

		while (true)
		{
			var jsonStr = br.ReadString();

			var command = JsonSerializer.Deserialize<Command>(jsonStr);

			if (command is null)
				continue;

			switch (command.Method)
			{
				case HttpMethods.GET:
					{
						var id = command.Car?.Id;
						if (id == 0)
						{
							var jsonCars = JsonSerializer.Serialize(cars);
							bw.Write(jsonCars);
							break;
						}

						Car? car = null;
						foreach (var c in cars)
						{
							if (c.Id == id)
							{
								car = c;
								break;
							}
						}

						var jsonResponse = JsonSerializer.Serialize(car);
						bw.Write(jsonResponse);
						break;
					}
				case HttpMethods.POST:
					{
						var id = command.Car?.Id;
						var canBePosted = true;
						foreach (var c in cars)
						{
							if (c.Id == id)
							{
								canBePosted = false;
								break;
							}
						}

						if (canBePosted)
						{
							if (command.Car is not null)
								lock (sync)
								{
									cars.Add(command.Car);
								}
						}

						bw.Write(canBePosted);

						break;
					}
				case HttpMethods.PUT:
					{
						var id = command.Car?.Id;
						var insertIndex = -1;
						var canBePuted = false;
						foreach (var c in cars)
						{
							if (c.Id == id)
							{
								canBePuted = true;
								lock (sync)
								{
									insertIndex = cars.IndexOf(c);
								}
								cars.Remove(c);
								break;
							}
						}

						if (canBePuted)
						{
							if (command.Car is not null)
								lock (sync)
								{
									cars.Insert(insertIndex, command.Car);
								}
						}

						bw.Write(canBePuted);

						break;
					}

				case HttpMethods.DELETE:
					{
						var isDeleted = false;
						var id = command.Car?.Id;
						foreach (var c in cars)
						{
							if (c.Id == id)
							{
								lock (sync)
								{
									cars.Remove(c);
								}
								isDeleted = true;
								break;
							}
						}
						bw.Write(isDeleted);
						break;
					}
			}

			lock (sync)
			{
				var jsonCars = JsonSerializer.Serialize(cars);
				File.WriteAllTextAsync(@"..\..\..\Cars.json", jsonCars);
			}
		}
	}).Start();
}