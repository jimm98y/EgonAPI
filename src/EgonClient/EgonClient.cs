using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EgonAPI.API;
using EgonAPI.Model;

namespace EgonAPI
{
    public class EgonClient : IDisposable
    {
        private const int EGON_REPEAT_INTERVAL = 3000; // 3 seconds before repeating the query

        private readonly string _user;
        private readonly string _password;
        private readonly EgonWebModuleClient _client;

        private Dictionary<string, EgonDeviceModel> devices = new Dictionary<string, EgonDeviceModel>();

        public Dictionary<string, EgonDeviceModel> Devices
        {
            get { return devices; }
            private set { devices = value; }
        }

        private List<EgonGroupModel> groups = new List<EgonGroupModel>();

        public List<EgonGroupModel> Groups
        {
            get { return groups; }
            private set { groups = value; }
        }

        public EgonClient(EgonDescription description, string user, string password, bool isHttpsEnabled = false)
        {
            this._client = new EgonWebModuleClient(description, isHttpsEnabled);
            this._user = user;
            this._password = password;
        }

        public async Task<bool> InitializeAsync()
        {
            bool isAuthorized = false;
            int attempts = 0;
            const int maxAttempts = 10; // maximum number of attempts to make
            Dictionary<string, EgonDeviceModel> devices = new Dictionary<string, EgonDeviceModel>();
            List<EgonGroupModel> groups = new List<EgonGroupModel>();

            isAuthorized = await _client.AuthorizeAsync(_user, _password);
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
                            EgonDeviceModel device = EgonDeviceModel.FromData(e);
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

                                    EgonGroupModel egonGroup = EgonGroupModel.FromData(group, groupData.ElementStates);
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

                        this.Devices = devices;
                        this.Groups = groups;

                        // get realtime data
                        await RefreshAsync();

                        return true;
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

            return false;
        }

        public async Task RefreshAsync()
        {
            bool isAuthorized;
            isAuthorized = await _client.AuthorizeAsync(_user, _password);
            if (isAuthorized)
            {
                await _client.RefreshAsync();
                EgonData state = await _client.GetStateAsync();

                if (state != null)
                {
                    for (int i = 0; i < state.ElementStates.Length; i++)
                    {
                        EgonElementState currentElementState = state.ElementStates[i];
                        EgonDeviceModel foundDevice;

                        if (Devices.TryGetValue(currentElementState.Id, out foundDevice))
                        {
                            foundDevice.CurrentValue = currentElementState.Value;
                        }
                    }
                }
            }
        }

        public EgonConfigurationModel GetConfiguration()
        {
            EgonConfigurationModel configuration = new EgonConfigurationModel();
            configuration.Devices = this.Devices.Values.ToList();
            configuration.Groups = this.Groups;
            return configuration;
        }

        /// <summary>
        /// Execute an action.
        /// </summary>
        /// <returns><see cref="true"/> if successful, <see cref="false"/> otherwise.</returns>
        public async Task<bool> ExecuteActionAsync(string elementId, string action)
        {
            bool isAuthorized;
            isAuthorized = await _client.AuthorizeAsync(_user, _password);
            if (isAuthorized)
            {
                return await _client.ExecuteActionAsync(elementId, action);
            }

            return false;
        }

        public static Task<EgonDescription> DiscoverAsync(string networkIpAddress, int broadcastTimeout = 10000)
        {
            return EgonWebModuleClient.DiscoverAsync(networkIpAddress, broadcastTimeout);
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
