using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using PatreonDownloader.Implementation.Models.JSONObjects.IgnorePosts;
using UniversalDownloaderPlatform.Common.Interfaces.Models;

namespace PatreonDownloader.Implementation;

public class PatreonCrawledUrlFilter
{
    private static PatreonCrawledUrlFilter _instance;
        private const string IgnorePostsFileName = "ignorePosts.json";
        private readonly List<IgnorePost> _ignorePosts ;
        private readonly IUniversalDownloaderPlatformSettings _settings;
        
        private PatreonCrawledUrlFilter(IUniversalDownloaderPlatformSettings settings)
        {
            _settings = settings;
            _ignorePosts = GetIgnorePostsFromJson();
        }
        
        public static PatreonCrawledUrlFilter GetInstance()
        {
            if(_instance == null) throw new Exception("Instance not initialized");
            return _instance;
        }
        
        public static PatreonCrawledUrlFilter GetInstance(IUniversalDownloaderPlatformSettings settings)
        {
            return _instance ??= new PatreonCrawledUrlFilter(settings);
        }
        
        private string GetIgnorePostsFilePath()
        {
            return $"{_settings.DownloadDirectory}/{IgnorePostsFileName}";
        }

        private List<IgnorePost> GetIgnorePostsFromJson()
        {
            if (!File.Exists(GetIgnorePostsFilePath())) return new List<IgnorePost>();
            
            string json = File.ReadAllText(GetIgnorePostsFilePath());
            List<IgnorePost> jsonRoot = JsonConvert.DeserializeObject<List<IgnorePost>>(json);
            return jsonRoot;
        }
        
        public void SaveIgnorePostsToJson()
        {
            string json = JsonConvert.SerializeObject(_ignorePosts);
            File.WriteAllText(GetIgnorePostsFilePath(), json);
        }
        
        public void FilterOutPages(List<PatreonCrawledUrl> crawledUrls)
        {
            crawledUrls.RemoveAll(x => _ignorePosts.Any(y => y.Id == x.PostId));
        }

        public void AddIgnorePost(IgnorePost ignorePost)
        {
            _ignorePosts.Add(ignorePost);
        }
}