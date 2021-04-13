using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Newtonsoft.Json;
using Sigo.WebApp.FileService;
using Sigo.WebApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace Sigo.WebApp.ExternalServices
{
    public class StandardApiService : IStandardApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IFileService _fileService;
        private readonly IConfiguration _configuration;
        private readonly string _apiGatewayUrl;

        public StandardApiService(IHttpClientFactory httpClientFactory,
            IHttpContextAccessor httpContextAccessor,
            IFileService fileService,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _fileService = fileService;
            _configuration = configuration;
            _apiGatewayUrl = _configuration.GetSection("BaseUrls:ApiGateway").Value;
        }

        public async Task<IEnumerable<Standard>> GetStandardsAsync()
        {
            var apiClient = await GetClient();

            var response = await apiClient.GetAsync($"{_apiGatewayUrl}standards");
            response.EnsureSuccessStatusCode();

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return new List<Standard>();
            }

            var content = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<List<Standard>>(content);
        }

        public async Task<UpdateStandard> GetStandardByIdAsync(string id)
        {
            var request = new HttpRequestMessage(
                HttpMethod.Get,
               $"{_apiGatewayUrl}standards/{id}");

            var apiClient = await GetClient();

            var response = await apiClient.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<UpdateStandard>(content);
        }

        public async Task<Standard> CreateStandardAsync(CreateStandard standard)
        {
            standard.Url = await _fileService.UploadAsync(standard.File, standard.Code);

            var apiClient = await GetClient();

            var request = new HttpRequestMessage(
                HttpMethod.Post,
               $"{_apiGatewayUrl}standards");

            var json = JsonConvert.SerializeObject(standard, Formatting.Indented);
            request.Content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);

            var response = await apiClient.SendAsync(
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

            var apiClient = await GetClient();

            var request = new HttpRequestMessage(
                HttpMethod.Put,
               $"{_apiGatewayUrl}standards");

            var json = JsonConvert.SerializeObject(standard, Formatting.Indented);
            request.Content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);

            var response = await apiClient.SendAsync(
                    request, HttpCompletionOption.ResponseHeadersRead)
                .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Standard>(content);
        }

        public async Task DeleteStandardAsync(string id)
        {
            var apiClient = await GetClient();

            var request = new HttpRequestMessage(
                HttpMethod.Delete,
                $"{_apiGatewayUrl}standards/{id}");

            var response = await apiClient.SendAsync(
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

        private async Task<TokenResponse> GenerateToken()
        {
            var authBaseUrl = _configuration.GetSection("BaseUrls:Auth").Value;
            var apiClientCredentials = new ClientCredentialsTokenRequest
            {
                Address = $"{authBaseUrl}/connect/token",

                ClientId = _configuration.GetSection("StandardCredentials:ClientId").Value,
                ClientSecret = _configuration.GetSection("StandardCredentials:ClientSecret").Value,
                GrantType = _configuration.GetSection("StandardCredentials:GrantType").Value,
                Scope = _configuration.GetSection("StandardCredentials:Scope").Value
            };

            var client = new HttpClient();
            var disco = await client.GetDiscoveryDocumentAsync(authBaseUrl);

            if (disco.IsError)
            {
                return null;
            }

            var tokenResponse = await client.RequestClientCredentialsTokenAsync(apiClientCredentials);

            if (tokenResponse.IsError)
            {
                return null;
            }

            return tokenResponse;
        }

        private async Task<HttpClient> GetClient()
        {
            var tokenResponse = await GenerateToken();
            var apiClient = new HttpClient();
            apiClient.SetBearerToken(tokenResponse.AccessToken);

            return apiClient;
        }
    }
}