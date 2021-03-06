﻿using Newtonsoft.Json;

namespace BasecampApiNet.Models
{
    public class BucketModel
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("app_url")]
        public string AppUrl { get; set; }

    }
}
