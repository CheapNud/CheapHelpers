using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CheapHelpers.Services
{
    public class AddressSearchService
    {
        public AddressSearchService(string key, string clientid, string endpoint)
        {
            _apikey = key;
            _clientid = clientid;
            _endpoint = endpoint;
        }

        private readonly string _apikey;
        private readonly string _clientid;
        private readonly string _endpoint;

        public async Task<List<Result>> FuzzyAddressSearch(string searchtext, CancellationToken token = default)
        {
            return await FuzzyAddressSearch(searchtext, default, default, token);
        }

        public async Task<List<Result>> FuzzyAddressSearch(string searchtext, string countrycodes = "BE,NL", bool typeahead = true, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(searchtext))
            {
                throw new ArgumentException($"'{nameof(searchtext)}' cannot be null or whitespace.", nameof(searchtext));
            }

            try
            {
                // Input and output languages are defined as parameters.
                string route = $@"/fuzzy/json?api-version=1.0&query={searchtext}&countryset={countrycodes}&typeahead={typeahead}&subscription-key={_apikey}";
                using (var client = new HttpClient())
                {
                    using (var request = new HttpRequestMessage())
                    {
                        // Build the request.
                        request.Method = HttpMethod.Get;
                        request.Headers.Add("x-ms-client-id", _clientid);
                        request.RequestUri = new Uri(_endpoint + route);
                        // Send the request and get response.
                        using (HttpResponseMessage response = await client.SendAsync(request, token).ConfigureAwait(false))
                        {
                            // Read response as a string.
                            string rawresult = await response.Content.ReadAsStringAsync(token);
                            var resultobj = rawresult.FromJson<Root>();
                            return resultobj.Results;
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
    }
}
