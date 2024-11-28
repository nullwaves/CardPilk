using Polly;
using Polly.RateLimit;
using RestSharp;

namespace CardPilkApp.Services
{
    public class RateLimitedRestClient
    {
        private readonly RestClient _client;
        private readonly IAsyncPolicy _ratePolicy;

        public RateLimitedRestClient(string baseUri, int msDelay)
        {
            _client = new RestClient(baseUri);
            _ratePolicy = Policy.RateLimitAsync(1, TimeSpan.FromMilliseconds(msDelay));
        }

        public async Task<RestResponse?> ExecuteAsync(RestRequest request)
        {
            while(true)
            {
                try
                {
                    return await _ratePolicy.ExecuteAsync(async () =>
                    {
                        return await _client.ExecuteAsync(request);
                    });
                }
                catch(RateLimitRejectedException ex)
                {
                    Thread.Sleep(ex.RetryAfter.Milliseconds+1);
                }
            }
        }
    }
}
