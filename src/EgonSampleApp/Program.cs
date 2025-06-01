using EgonAPI;
using System;

var device = await EgonClient.DiscoverAsync("192.168.1.255");
if (device != null)
    Console.WriteLine($"Device found");
else
    Console.WriteLine($"No device found");