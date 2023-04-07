# EgonAPI
Client to control the ABB Ego-n smart home system. The client communicates directly with the web module over HTTP.

## Discover
Call `DiscoverAsync` to get the web module description:
```cs
EgonDescription discovery = await EgonClient.DiscoverAsync("192.168.1.255");
```

## EgonClient
Create the client:
```cs
var egonClient = new EgonClient(discovery, "admin", "123456789");
```

Retrive all devices and groups from the configuration:
```cs
var egonConfig = await egonClient.GetConfigurationAsync();
```

Query the current state of all devices (polling):
```cs
var updated = await egonClient.GetCurrentStateAsync(egonConfig);
```

## Control devices
To turn on the first device:
```cs
await egonClient.ExecuteActionAsync(egonConfig.Devices.First().Value.Id, EgonActions.On);
```