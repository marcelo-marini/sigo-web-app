using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Sigo.WebApp.Models
{
    public class CreateStandard
    {
        [Required(ErrorMessage = "Obrigatório")]
        [JsonProperty("description"), DisplayName("description")]
        public string Description { get; set; }

        [JsonProperty("url"), DisplayName("url")]
        public string Url { get; set; }

        [Required(ErrorMessage = "Obrigatório")]
        [JsonProperty("status"), DisplayName("status")]
        public string Status { get; set; }

        [Required(ErrorMessage = "Obrigatório")]
        [JsonProperty("type"), DisplayName("type")]
        public string Type { get; set; }

        [Required(ErrorMessage = "Obrigatório")]
        [JsonProperty("owner"), DisplayName("owner")]
        public string Owner { get; set; }

        [Required(ErrorMessage = "Obrigatório")]
        [JsonProperty("code"), DisplayName("code")]
        public string Code { get; set; }
        [Required(ErrorMessage = "Obrigatório")]
        public IFormFile File { get; set; }
    }
}