using Duende.IdentityModel.Client;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CheapHelpers.WebServices
{
    public abstract class WebServiceBase : IWebServiceBase
    {
        public WebServiceBase(string servicename, HttpClient httpClient, bool createHub = false)
        {
            try
            {
                ServiceName = servicename;
                _httpclient = httpClient;

                Debug.WriteLine("Created Api Url: " + ApiUrl);
                if (createHub)
                {
                    Debug.WriteLine("Created Hub Url: " + HubUrl);
                    //as long as the server version < 2012 use longpolling! websockets is not implemented on server 2008R2 & IIS 7.5, yet signalr will try to use websockets (automatic fallback doesn work)
                    HubConnection = new HubConnectionBuilder().WithUrl(HubUrl, HttpTransportType.WebSockets).Build();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }
        }

        private HttpClient _httpclient;
        public HubConnection HubConnection { get; private set; }
        public string ServiceName { get; private set; }
        //public string HubUrl => $@"{Endpoint}{ServiceName}/";
        public string Endpoint => $@"https://mecamgroup.com/"; //TODO: no more mecam
        public string HubUrl => $@"{Endpoint}hub/{ServiceName}/";
        public string ApiEndpoint => $@"{Endpoint}api/";
        public string ApiUrl => $@"{ApiEndpoint}{ServiceName}/";


        public async Task StartAsync()
        {
            try
            {
                await HubConnection.StartAsync();
                Debug.WriteLine("Started Connection on Hub: " + HubUrl);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public async Task StopAsync()
        {
            await HubConnection.StopAsync();
            Debug.WriteLine("Stopped Connection on Hub: " + HubUrl);
        }

        public async Task DisposeAsync()
        {
            if (HubConnection != null)
            {
                await HubConnection.DisposeAsync();
            }
        }

        internal async Task Request(string method, object input = null, string endpoint = "")
        {
            string result = await Request<string>(method, input, endpoint);
            Debug.WriteLine($"Request result: \n {result}");
        }

        /// <summary>
        /// uses access token
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        internal async Task<TClass> PutRequest<TClass>(object content, string endpoint = "") where TClass : class
        {
            try
            {
                // call api
                using (var apiClient = new HttpClient())
                {
                    apiClient.Timeout = TimeSpan.FromMinutes(5);
                    //apiClient.SetBearerToken(await RequestAccessToken());

                    using (HttpResponseMessage response = await apiClient.PutAsync($@"{ApiUrl}{endpoint}", new StringContent(content.ToJson(), Encoding.UTF8, MimeTypes.Application.Json)))
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            throw new Exception(response.StatusCode.ToString());
                        }
                        else
                        {
                            return (await response.Content.ReadAsStringAsync()).FromJson<TClass>();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }
        }
        /// <summary>
        /// uses access token
        /// </summary>
        /// <returns></returns>
        internal async Task<TClass> GetRequest<TClass>(string endpoint = "") where TClass : class
        {
            try
            {
                Debug.WriteLine($"Starting GET request to: {ApiUrl}{endpoint}");

                Debug.WriteLine("Sending request...");
                using (HttpResponseMessage response = await _httpclient.GetAsync($@"{ApiUrl}{endpoint}"))
                {
                    Debug.WriteLine($"Response status code: {response.StatusCode}");

                    if (!response.IsSuccessStatusCode)
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        Debug.WriteLine($"Error response content: {errorContent}");
                        throw new Exception($"Status: {response.StatusCode}, Content: {errorContent}");
                    }
                    else
                    {
                        string content = await response.Content.ReadAsStringAsync();
                        Debug.WriteLine($"Response content length: {content.Length}");
                        return content.FromJson<TClass>();
                    }
                }
            }
            catch (HttpRequestException hre)
            {
                Debug.WriteLine($"HTTP Request error: {hre.Message}");
                if (hre.InnerException != null)
                    Debug.WriteLine($"Inner exception: {hre.InnerException.Message}");
                throw;
            }
            catch (TaskCanceledException tce)
            {
                Debug.WriteLine($"Request timed out: {tce.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetRequest: {ex.GetType().Name}: {ex.Message}");
                throw;
            }
        }

        private async Task<string> RequestAccessToken()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    //var disco = await client.GetDiscoveryDocumentAsync(ApiEndpoint);
                    //if (disco.IsError)
                    //{
                    //    throw new Exception(disco.Error);
                    //}

                    var tokenResponse = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
                    {
                        Address = @"https://login.microsoftonline.com/11d6a2ed-37dc-4c0e-9dcc-295dab323ff7/oauth2/v2.0/token",
                        ClientId = "clientid",
                        ClientSecret = "clientsecret",
                        Scope = "api://3918aa93-577f-4602-9ec3-76ebe4d13515/.default",
                    });

                    if (tokenResponse.IsError)
                    {
                        throw new Exception(tokenResponse.ErrorDescription);
                    }

                    Debug.WriteLine(tokenResponse.Json.ToString());

                    return tokenResponse.AccessToken;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }
        }

        /// <summary>
        /// doesn not use an access token
        /// </summary>
        /// <typeparam name="TClass"></typeparam>
        /// <param name="method"></param>
        /// <param name="input"></param>
        /// <param name="endpoint"></param>
        /// <param name="apiUrl"></param>
        /// <returns></returns>
        internal async Task<TClass> Request<TClass>(string method, object input = null, string endpoint = "", bool apiUrl = true) where TClass : class
        {
            try
            {
                string prefix = Endpoint;
                if (apiUrl)
                {
                    prefix = ApiUrl;
                }

                WebRequest req = WebRequest.Create($@"{prefix}{endpoint}");
                req.Method = method;
                req.ContentType = MimeTypes.Application.Json;
                req.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);

                if (input != null)
                {
                    using (Stream stream = await req.GetRequestStreamAsync())
                    {
                        using (StreamWriter writer = new StreamWriter(stream))
                        {
                            writer.Write(input.ToJson());
                        }
                    }
                }

                using (WebResponse rep = await req.GetResponseAsync())
                {
                    using (Stream responseStream = rep.GetResponseStream())
                    {
                        using (StreamReader streamReader = new StreamReader(responseStream))
                        {
                            string result = await streamReader.ReadToEndAsync();
                            if (typeof(TClass) == typeof(string) || typeof(TClass).IsValueType)
                            {
                                return result as TClass;
                            }

                            return JsonConvert.DeserializeObject<TClass>(result, new JsonSerializerSettings
                            {
                                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Webservicebase request failed");
                Debug.WriteLine(ex.Message);
                throw;
            }
        }
    }
}