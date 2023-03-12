using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EgonAPI.API;
using EgonAPI.Model;

namespace EgonAPI
{
    /// <summary>
    /// ABB Egon client.
    /// </summary>
    public class EgonClient : IDisposable
    {
        private const int EGON_REPEAT_INTERVAL = 3000; // 3 seconds before repeating the query
        private const int EGON_BROADCAST_TIMEOUT = 10000; // 10 seconds broadcast timeout

        private readonly string _user;
        private readonly string _password;
        private readonly EgonWebModuleClient _client;
        private readonly SemaphoreSlim _refreshSlim = new SemaphoreSlim(1);

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="description"><see cref="EgonDescription"/>.</param>
        /// <param name="user">User name.</param>
        /// <param name="password">Password.</param>
        public EgonClient(EgonDescription description, string user, string password)
        {
            if(description == null) 
                throw new ArgumentNullException(nameof(description));

            this._client = new EgonWebModuleClient(description);
            this._user = user;
            this._password = password;
        }

        /// <summary>
        /// Get Egon configuration.
        /// </summary>
        /// <returns><see cref="EgonConfiguration"/>.</returns>
        public async Task<EgonConfiguration> GetConfigurationAsync()
        {
            int attempts = 0;
            const int maxAttempts = 10; // maximum number of attempts to make
            Dictionary<string, EgonDevice> devices = new Dictionary<string, EgonDevice>();
            List<Model.EgonGroup> groups = new List<Model.EgonGroup>();

            bool isAuthorized = await _client.AuthorizeAsync(_user, _password);
            if (isAuthorized)
            {
                EgonData data = null;
                while (data == null)
                {
                    data = await _client.GetConfigurationAsync();
                    if (data != null)
                    {
                        foreach (var e in data.Elements)
                        {
                            EgonDevice device = CreateDeviceModel(e);
                            devices.Add(device.Id, device);
                        }

                        foreach (var group in data.Groups)
                        {
                            EgonData groupData = null;
                            while (groupData == null)
                            {
                                groupData = await _client.GetStateAsync(group.Id);

                                if (groupData != null)
                                {
                                    if (groupData.ElementStates == null || groupData.ElementStates.Count() == 0)
                                        continue;

                                    Model.EgonGroup egonGroup = CreateGroupModel(group, groupData.ElementStates);
                                    groups.Add(egonGroup);
                                }
                                else
                                {
                                    await Task.Delay(EGON_REPEAT_INTERVAL);
                                    attempts++;
                                    if (attempts > maxAttempts)
                                        break;
                                }
                            }
                        }

                        EgonConfiguration configuration = new EgonConfiguration();
                        configuration.Devices = devices;
                        configuration.Groups = groups;
                        return configuration;
                    }
                    else
                    {
                        await Task.Delay(EGON_REPEAT_INTERVAL);
                        attempts++;
                        if (attempts > maxAttempts)
                            break;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Queries the current state of all devices and returns the list of changes.
        /// </summary>
        /// <param name="egonConfiguration"><see cref="EgonConfiguration"/>.</param>
        /// <returns>A <see cref="List{T}"/> of devices that had a state change since the last refresh.</returns>
        /// <exception cref="UnauthorizedAccessException"></exception>
        public async Task<IList<EgonDevice>> GetCurrentStateAsync(EgonConfiguration egonConfiguration)
        {
            await _refreshSlim.WaitAsync();

            try
            {
                bool isAuthorized = await _client.AuthorizeAsync(_user, _password);

                if (!isAuthorized)
                    throw new UnauthorizedAccessException();

                await _client.RefreshAsync();

                EgonData state = await _client.GetStateAsync();
                List<EgonDevice> updatedDevices = new List<EgonDevice>();

                if (state != null)
                {
                    for (int i = 0; i < state.ElementStates.Length; i++)
                    {
                        EgonElementState currentElementState = state.ElementStates[i];
                        EgonDevice foundDevice;

                        if (egonConfiguration.Devices.TryGetValue(currentElementState.Id, out foundDevice))
                        {
                            if (string.Compare(foundDevice.CurrentValue, currentElementState.Value) != 0)
                            {
                                // device state has changed
                                updatedDevices.Add(foundDevice);
                            }

                            foundDevice.CurrentValue = currentElementState.Value;
                        }
                    }
                }

                return updatedDevices;
            }
            finally
            {
                _refreshSlim.Release();
            }
        }

        /// <summary>
        /// Execute an action.
        /// </summary>
        /// <param name="elementId">Egon element ID.</param>
        /// <param name="action"><see cref="EgonActions"/>.</param>
        /// <returns><see cref="true"/> if successful, <see cref="false"/> otherwise.</returns>
        public async Task<bool> ExecuteActionAsync(string elementId, string action)
        {
            bool isAuthorized = await _client.AuthorizeAsync(_user, _password);
            return isAuthorized && await _client.ExecuteActionAsync(elementId, action);
        }

        /// <summary>
        /// Discover Egon in the local network.
        /// </summary>
        /// <param name="networkIpAddress">Network IP address, e.g. 192.168.1.255.</param>
        /// <param name="broadcastTimeout">Broadcast timeout. <see cref="EGON_BROADCAST_TIMEOUT"/>.</param>
        /// <returns></returns>
        public static Task<EgonDescription> DiscoverAsync(string networkIpAddress, int broadcastTimeout = EGON_BROADCAST_TIMEOUT)
        {
            return EgonWebModuleClient.DiscoverAsync(networkIpAddress, broadcastTimeout);
        }

        private static EgonDevice CreateDeviceModel(API.EgonElement e)
        {
            EgonDevice d = new EgonDevice();
            d.CurrentValue = e.Value;
            d.Id = e.Id;
            d.IsEnabled = "true".CompareTo(e.Enabled) == 0;
            d.Name = e.Name;
            d.DeviceType = e.Type;
            return d;
        }

        private static Model.EgonGroup CreateGroupModel(API.EgonGroup g, API.EgonElementState[] element_states)
        {
            Model.EgonGroup eg = new Model.EgonGroup();
            eg.Id = g.Id;
            eg.Name = g.Name;
            eg.Devices = element_states.Select(x => x.Id).ToList();
            return eg;
        }

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this._client.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion // IDisposable Support
    }
}
