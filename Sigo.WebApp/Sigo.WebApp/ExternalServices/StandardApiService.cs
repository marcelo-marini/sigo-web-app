using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Newtonsoft.Json;
using Sigo.WebApp.FileService;
using Sigo.WebApp.Models;

namespace Sigo.WebApp.ExternalServices
{
    public class StandardApiService : IStandardApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IFileService _fileService;

        public StandardApiService(IHttpClientFactory httpClientFactory,
            IHttpContextAccessor httpContextAccessor,
            IFileService fileService)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _fileService = fileService;
        }

        public async Task<IEnumerable<Standard>> GetStandardsAsync()
        {
            ////////////////////////
            // WAY 1 :

            var httpClient = _httpClientFactory.CreateClient("StandardApiClient");

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/standards");

            var response = await httpClient.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var movieList = JsonConvert.DeserializeObject<List<Standard>>(content);
            return movieList;

            ////////////////////////// //////////////////////// ////////////////////////
            //// WAY 2 :

            //// 1. "retrieve" our api credentials. This must be registered on Identity Server!
            //var apiClientCredentials = new ClientCredentialsTokenRequest
            //{
            //    Address = "https://localhost:5005/connect/token",

            //    ClientId = "movieClient",
            //    ClientSecret = "secret",

            //    // This is the scope our Protected API requires. 
            //    Scope = "movieAPI"
            //};

            //// creates a new HttpClient to talk to our IdentityServer (localhost:5005)
            //var client = new HttpClient();

            //// just checks if we can reach the Discovery document. Not 100% needed but..
            //var disco = await client.GetDiscoveryDocumentAsync("https://localhost:5005");
            //if (disco.IsError)
            //{
            //    return null; // throw 500 error
            //}

            //// 2. Authenticates and get an access token from Identity Server
            //var tokenResponse = await client.RequestClientCredentialsTokenAsync(apiClientCredentials);            
            //if (tokenResponse.IsError)
            //{
            //    return null;
            //}

            //// Another HttpClient for talking now with our Protected API
            //var apiClient = new HttpClient();

            //// 3. Set the access_token in the request Authorization: Bearer <token>
            //client.SetBearerToken(tokenResponse.AccessToken);

            //// 4. Send a request to our Protected API
            //var response = await client.GetAsync("https://localhost:5001/api/movies");
            //response.EnsureSuccessStatusCode();

            //var content = await response.Content.ReadAsStringAsync();

            //var movieList = JsonConvert.DeserializeObject<List<Movie>>(content);
            //return movieList;
        }

        public async Task<UpdateStandard> GetStandardByIdAsync(string id)
        {
            var httpClient = _httpClientFactory.CreateClient("StandardApiClient");

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/standards/{id}");

            var response = await httpClient.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<UpdateStandard>(content);
        }

        public async Task<Standard> CreateStandardAsync(CreateStandard standard)
        {
            standard.Url = await _fileService.UploadAsync(standard.File, standard.Code);

            var httpClient = _httpClientFactory.CreateClient("StandardApiClient");

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/standards");

            var json = JsonConvert.SerializeObject(standard, Formatting.Indented);
            request.Content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);

            var response = await httpClient.SendAsync(
                    request, HttpCompletionOption.ResponseHeadersRead)
                .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Standard>(content);
        }

        public async Task<Standard> UpdateStandardAsync(UpdateStandard standard)
        {
            if (standard.File != null)
            {
                standard.Url = await _fileService.UploadAsync(standard.File, standard.Code);
            }

            var httpClient = _httpClientFactory.CreateClient("StandardApiClient");

            var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"/standards");

            var json = JsonConvert.SerializeObject(standard, Formatting.Indented);
            request.Content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);

            var response = await httpClient.SendAsync(
                    request, HttpCompletionOption.ResponseHeadersRead)
                .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Standard>(content);
        }

        public async Task DeleteStandardAsync(string id)
        {
            var httpClient = _httpClientFactory.CreateClient("StandardApiClient");

            var request = new HttpRequestMessage(
                HttpMethod.Delete,
                $"/standards/{id}");

            var response = await httpClient.SendAsync(
                    request, HttpCompletionOption.ResponseHeadersRead)
                .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();
        }

        public async Task<UserInfoViewModel> GetUserInfoAsync()
        {
            var authClient = _httpClientFactory.CreateClient("AuthClient");

            var metaDataResponse = await authClient.GetDiscoveryDocumentAsync();

            if (metaDataResponse.IsError)
            {
                throw new HttpRequestException("Something went wrong while requesting the access token");
            }

            var accessToken = await _httpContextAccessor
                .HttpContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken);

            var userInfoResponse = await authClient.GetUserInfoAsync(
                new UserInfoRequest
                {
                    Address = metaDataResponse.UserInfoEndpoint,
                    Token = accessToken
                });

            if (userInfoResponse.IsError)
            {
                throw new HttpRequestException("Something went wrong while getting user info");
            }

            var userInfoDictionary = userInfoResponse.Claims.ToDictionary(claim => claim.Type, claim => claim.Value);

            return new UserInfoViewModel(userInfoDictionary);
        }
    }
}