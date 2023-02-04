using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace EgonAPI.API
{
    /// <summary>
    /// ABB Egon client that facilitates communication with the web module. 
    /// </summary>
    internal class EgonWebModuleClient : IDisposable
    {
        private class UdpState
        {
            public UdpClient Client { get; set; }
            public IPEndPoint Endpoint { get; set; }
            public TaskCompletionSource<EgonDescription> Result { get; set; }
        }

        private const string EGON_BROADCAST_MESSAGE = "EGO-N?";
        private const int EGON_BROADCAST_PORT = 2007;
        private const int EGON_BROADCAST_TIMEOUT = 10000;

        private HttpClient _client;
        private bool _isHttpsEnabled = false;
        private string _deviceId = string.Empty;
        private EgonData _configuration = null;

        public EgonDescription Description { get; private set; }

        public EgonWebModuleClient(EgonDescription description, bool isHttpsEnabled = false)
        {
            _client = new HttpClient();
            _isHttpsEnabled = isHttpsEnabled;
            Description = description;
            RegisterEncodingProvider();
        }

        private void RegisterEncodingProvider()
        {
            // this 'RegisterEncodingProvider' call is enough once per process - necessary for 1250 encoding support
            var provider = CodePagesEncodingProvider.Instance;
            Encoding.RegisterProvider(provider);
        }

        /// <summary>
        /// Login to the Egon device using supplied credentials.
        /// </summary>
        /// <param name="user">User name.</param>
        /// <param name="password">Password.</param>
        /// <returns>Authorization response that serves as device ID.</returns>
        public async Task<bool> AuthorizeAsync(string user, string password)
        {
            if (Description == null)
                throw new ArgumentNullException(nameof(Description));

            try
            {
                _deviceId = await AuthorizeAsync(_client, Description, user, password, _isHttpsEnabled);
            }
            catch (Exception)
            {
                // most probably the Internet connection is not available
                return false;
            }

            return !string.IsNullOrEmpty(_deviceId) && "device=0".CompareTo(_deviceId) != 0;
        }

        /// <summary>
        /// Get Egon configuration.
        /// </summary>
        /// <returns>Parsed <see cref="EgonData"/> response that contains all devices exposed through the web interface.</returns>
        public async Task<EgonData> GetConfigurationAsync()
        {
            if (Description == null)
                throw new ArgumentNullException(nameof(Description));

            if (string.IsNullOrEmpty(_deviceId))
                throw new ArgumentNullException(nameof(_deviceId));

            _configuration = await GetConfigurationAsync(_client, Description, _deviceId, _isHttpsEnabled);

            return _configuration;
        }

        /// <summary>
        /// Execute an action.
        /// </summary>
        /// <param name="elementId">ID of the egon device on which we want to execute an action.</param>
        /// <param name="action">Action to execute.</param>
        /// <returns><see cref="true"/> if successful, <see cref="false"/> otherwise.</returns>
        public Task<bool> ExecuteActionAsync(string elementId, string action)
        {
            if (Description == null)
                throw new ArgumentNullException(nameof(Description));

            if (string.IsNullOrEmpty(_deviceId))
                throw new ArgumentNullException(nameof(_deviceId));

            return ExecuteActionAsync(_client, Description, _deviceId, elementId, action, _isHttpsEnabled);
        }

        /// <summary>
        /// Refresh connection - called just to make sure the authorization does not expire.
        /// </summary>
        /// <returns><see cref="true"/> if successful, <see cref="false"/> otherwise.</returns>
        public Task<bool> RefreshAsync()
        {
            if (Description == null)
                throw new ArgumentNullException(nameof(Description));

            if (string.IsNullOrEmpty(_deviceId))
                throw new ArgumentNullException(nameof(_deviceId));

            return RefreshAsync(_client, Description, _deviceId, _isHttpsEnabled);
        }

        /// <summary>
        /// Get state of all devices.
        /// </summary>
        /// <param name="groupId">Optional group ID returns only the state of devices in that particular group.</param>
        /// <returns><see cref="EgonData"/> that contains state of all devices.</returns>
        public Task<EgonData> GetStateAsync(string groupId = null)
        {
            if (Description == null)
                throw new ArgumentNullException(nameof(Description));

            if (string.IsNullOrEmpty(_deviceId))
                throw new ArgumentNullException(nameof(_deviceId));

            return GetStateAsync(_client, Description, _deviceId, groupId, _isHttpsEnabled);
        }

        private static async Task<EgonData> ParseEgonData(HttpResponseMessage response)
        {
            try
            {
                XmlSerializer dcs = new XmlSerializer(typeof(EgonData));

                using (MemoryStream ms = new MemoryStream())
                {
                    string str = string.Empty;
                    var responseBuffer = await response.Content.ReadAsByteArrayAsync();

                    // Egon uses 1250 text encoding
                    str = Encoding.GetEncoding(1250).GetString(responseBuffer);

                    using (TextReader tr = new StringReader(str))
                    {
                        EgonData data = dcs.Deserialize(tr) as EgonData;
                        return data;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return null;
            }
        }

        private static string GetProtocol(bool isHttpsEnabled)
        {
            return isHttpsEnabled ? "https" : "http";
        }

        private static string GetPort(bool isHttpsEnabled)
        {
            return isHttpsEnabled ? ":4536" : "";
        }

        #region Static API

        /// <summary>
        /// Discover Egon devices on the current network segment.
        /// </summary>
        /// <returns>Description of the found Egon device or null if there aren't any on the current network segment.</returns>
        public static async Task<EgonDescription> DiscoverAsync(string networkIpAddress, int broadcastTimeout = EGON_BROADCAST_TIMEOUT)
        {
            EgonDescription result = null;
            TaskCompletionSource<EgonDescription> tcs = new TaskCompletionSource<EgonDescription>();
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, EGON_BROADCAST_PORT + 1);
            IPEndPoint broadcastEndpoint = new IPEndPoint(IPAddress.Parse(networkIpAddress), EGON_BROADCAST_PORT);

            try
            {
                using (UdpClient client = new UdpClient(endPoint))
                {
                    UdpState s = new UdpState();
                    s.Endpoint = endPoint;
                    s.Client = client;
                    s.Result = tcs;

                    client.BeginReceive(MessageReceived, s);

                    byte[] message = Encoding.ASCII.GetBytes(EGON_BROADCAST_MESSAGE);
                    await client.SendAsync(message, message.Count(), broadcastEndpoint);

                    // make sure we do not wait forever
                    IList<Task> tasks = new List<Task>() { tcs.Task, Task.Delay(broadcastTimeout) };
                    await Task.WhenAny(tasks);

                    if (tcs.Task.IsCompleted)
                        result = tcs.Task.Result;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return result;
        }

        private static void MessageReceived(IAsyncResult result)
        {
            UdpClient client = ((UdpState)result.AsyncState).Client;
            IPEndPoint endpoint = ((UdpState)result.AsyncState).Endpoint;
            TaskCompletionSource<EgonDescription> resultTaskCompletionSource = ((UdpState)result.AsyncState).Result;
            byte[] receiveBytes = client.EndReceive(result, ref endpoint);
            string message = Encoding.ASCII.GetString(receiveBytes);

            if (string.Compare(message, EGON_BROADCAST_MESSAGE) != 0)
            {
                try
                {
                    EgonDescription description = EgonDescription.FromBroadcastResponse(message);
                    resultTaskCompletionSource.TrySetResult(description);
                    return;
                }
                catch (NotSupportedException)
                {
                    // ignore and keep the socket opened, waiting for another message
                }
            }

            client.BeginReceive(MessageReceived, result.AsyncState);
        }

        /// <summary>
        /// Login to the Egon device using supplied credentials.
        /// </summary>
        /// <param name="client"><see cref="HttpClient"/> to use.</param>
        /// <param name="description">Egon device description.</param>
        /// <param name="user">User name.</param>
        /// <param name="password">Password.</param>
        /// <returns>Authorization response that serves as device ID.</returns>
        public static async Task<string> AuthorizeAsync(HttpClient client, EgonDescription description, string user, string password, bool isHttpsEnabled = false)
        {
            string authorizationRequest = string.Format(
                "{0}://{1}{2}/authorize.html?password={3}&user={4}",
                GetProtocol(isHttpsEnabled),
                description.IpAddr,
                GetPort(isHttpsEnabled),
                password,
                user);

            try
            {
                using (HttpResponseMessage authorizationResponse = await client.GetAsync(new Uri(authorizationRequest, UriKind.Absolute)))
                {
                    return await authorizationResponse.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Get Egon configuration.
        /// </summary>
        /// <param name="client"><see cref="HttpClient"/> to use.</param>
        /// <param name="description">Egon device description.</param>
        /// <param name="device">Authorization response that serves as device ID.</param>
        /// <returns>Parsed <see cref="EgonData"/> response that contains all devices exposed through the web interface.</returns>
        public static async Task<EgonData> GetConfigurationAsync(HttpClient client, EgonDescription description, string device, bool isHttpsEnabled = false)
        {
            string configRequest = string.Format(
                "{0}://{1}{2}/config.html?{3}",
                GetProtocol(isHttpsEnabled),
                description.IpAddr,
                GetPort(isHttpsEnabled),
                device);

            try
            {
                using (HttpResponseMessage configResponse = await client.GetAsync(new Uri(configRequest, UriKind.Absolute)))
                {
                    return await ParseEgonData(configResponse);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Execute an action.
        /// </summary>
        /// <param name="client"><see cref="HttpClient"/> to use.</param>
        /// <param name="description">Egon device description.</param>
        /// <param name="device">Authorization response that serves as device ID.</param>
        /// <param name="elementId">ID of the egon device on which we want to execute an action.</param>
        /// <param name="action">Action to execute.</param>
        /// <returns><see cref="true"/> if successful, <see cref="false"/> otherwise.</returns>
        public static async Task<bool> ExecuteActionAsync(HttpClient client, EgonDescription description, string device, string elementId, string action, bool isHttpsEnabled = false)
        {
            string actionRequest = string.Format(
                "{0}://{1}{2}/action.html?action={3}&{4}&id={5}",
                GetProtocol(isHttpsEnabled),
                description.IpAddr,
                GetPort(isHttpsEnabled),
                action,
                device,
                elementId);

            try
            {
                using (HttpResponseMessage actionResponse = await client.GetAsync(new Uri(actionRequest, UriKind.Absolute)))
                {
                    string result = await actionResponse.Content.ReadAsStringAsync();
                    return string.Compare("OK", result) == 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Refresh connection - called just to make sure the authorization does not expire.
        /// </summary>
        /// <param name="client"><see cref="HttpClient"/> to use.</param>
        /// <param name="description">Egon device description.</param>
        /// <param name="device">Authorization response that serves as device ID.</param>
        /// <returns><see cref="true"/> if successful, <see cref="false"/> otherwise.</returns>
        public static async Task<bool> RefreshAsync(HttpClient client, EgonDescription description, string device, bool isHttpsEnabled = false)
        {
            string refreshRequest = string.Format(
                "{0}://{1}{2}/refresh.html?{3}",
                GetProtocol(isHttpsEnabled),
                description.IpAddr,
                GetPort(isHttpsEnabled),
                device);

            try
            {
                using (HttpResponseMessage refreshResponse = await client.GetAsync(new Uri(refreshRequest, UriKind.Absolute)))
                {
                    string result = await refreshResponse.Content.ReadAsStringAsync();
                    return string.Compare("OK", result) == 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Get state of all devices.
        /// </summary>
        /// <param name="client"><see cref="HttpClient"/> to use.</param>
        /// <param name="description">Egon device description.</param>
        /// <param name="device">Authorization response that serves as device ID.</param>
        /// <param name="groupId">Optional group ID returns only the state of devices in that particular group.</param>
        /// <returns><see cref="EgonData"/> that contains state of all devices.</returns>
        public static async Task<EgonData> GetStateAsync(HttpClient client, EgonDescription description, string device, string groupId = null, bool isHttpsEnabled = false)
        {
            string stateRequest = string.Format(
                "{0}://{1}{2}/state.html?{3}{4}",
                GetProtocol(isHttpsEnabled),
                description.IpAddr,
                GetPort(isHttpsEnabled),
                device,
                string.IsNullOrEmpty(groupId) ? string.Empty : "&group=" + groupId);

            try
            {
                using (HttpResponseMessage stateResponse = await client.GetAsync(new Uri(stateRequest, UriKind.Absolute)))
                {
                    return await ParseEgonData(stateResponse);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return null;
            }
        }

        #endregion // Static API

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _client.Dispose();
                    _client = null;
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
