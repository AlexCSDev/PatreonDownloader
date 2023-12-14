using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;
using PatreonDownloader.Implementation.Enums;
using PatreonDownloader.Implementation.Models;
using PatreonDownloader.Implementation.Models.JSONObjects.Posts;
using UniversalDownloaderPlatform.Common.Events;
using UniversalDownloaderPlatform.Common.Interfaces;
using UniversalDownloaderPlatform.Common.Interfaces.Models;
using UniversalDownloaderPlatform.DefaultImplementations.Models;

namespace PatreonDownloader.Implementation
{
    internal sealed class PatreonPageCrawler : IPageCrawler
    {
        private readonly IWebDownloader _webDownloader;
        private readonly IPluginManager _pluginManager;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private PatreonDownloaderSettings _patreonDownloaderSettings;

        public event EventHandler<PostCrawlEventArgs> PostCrawlStart;
        public event EventHandler<PostCrawlEventArgs> PostCrawlEnd;
        public event EventHandler<NewCrawledUrlEventArgs> NewCrawledUrl;
        public event EventHandler<CrawlerMessageEventArgs> CrawlerMessage;

        //TODO: Research possibility of not hardcoding this string
        private const string CrawlStartUrl = "https://www.patreon.com/api/posts?" +
                                             "include=user%2Cattachments%2Ccampaign%2Cpoll.choices%2Cpoll.current_user_responses.user%2Cpoll.current_user_responses.choice%2Cpoll.current_user_responses.poll%2Caccess_rules.tier.null%2Cimages.null%2Caudio.null" +
                                             "&fields[post]=change_visibility_at%2Ccomment_count%2Ccontent%2Ccurrent_user_can_delete%2Ccurrent_user_can_view%2Ccurrent_user_has_liked%2Cembed%2Cimage%2Cis_paid%2Clike_count%2Cmin_cents_pledged_to_view%2Cpost_file%2Cpost_metadata%2Cpublished_at%2Cpatron_count%2Cpatreon_url%2Cpost_type%2Cpledge_url%2Cthumbnail_url%2Cteaser_text%2Ctitle%2Cupgrade_url%2Curl%2Cwas_posted_by_campaign_owner" +
                                             "&fields[user]=image_url%2Cfull_name%2Curl" +
                                             "&fields[campaign]=show_audio_post_download_links%2Cavatar_photo_url%2Cearnings_visibility%2Cis_nsfw%2Cis_monthly%2Cname%2Curl" +
                                             "&fields[access_rule]=access_rule_type%2Camount_cents" +
                                             "&fields[media]=id%2Cimage_urls%2Cdownload_url%2Cmetadata%2Cfile_name" +
                                             "&sort=-published_at" +
                                             "&filter[is_draft]=false&filter[contains_exclusive_posts]=true&json-api-use-default-includes=false&json-api-version=1.0";

        public PatreonPageCrawler(IWebDownloader webDownloader, IPluginManager pluginManager)
        {
            _webDownloader = webDownloader ?? throw new ArgumentNullException(nameof(webDownloader));
            _pluginManager = pluginManager ?? throw new ArgumentNullException(nameof(pluginManager));
        }

        public async Task BeforeStart(IUniversalDownloaderPlatformSettings settings)
        {
            _patreonDownloaderSettings = (PatreonDownloaderSettings) settings;
        }

        public async Task<List<ICrawledUrl>> Crawl(ICrawlTargetInfo crawlTargetInfo)
        {
            PatreonCrawlTargetInfo patreonCrawlTargetInfo = (PatreonCrawlTargetInfo)crawlTargetInfo;
            if (patreonCrawlTargetInfo.Id < 1)
                throw new ArgumentException("Campaign ID cannot be less than 1");
            if (string.IsNullOrEmpty(patreonCrawlTargetInfo.Name))
                throw new ArgumentException("Campaign name cannot be null or empty");

            _logger.Debug($"Starting crawling campaign {patreonCrawlTargetInfo.Name}");
            List<ICrawledUrl> crawledUrls = new List<ICrawledUrl>();
            Random rnd = new Random(Guid.NewGuid().GetHashCode());

            if (_patreonDownloaderSettings.SaveAvatarAndCover)
            {
                _logger.Debug("Adding avatar and cover...");
                if(!string.IsNullOrWhiteSpace(patreonCrawlTargetInfo.AvatarUrl))
                    crawledUrls.Add(new PatreonCrawledUrl { PostId = "0", Url = patreonCrawlTargetInfo.AvatarUrl, UrlType = PatreonCrawledUrlType.AvatarFile });
                if (!string.IsNullOrWhiteSpace(patreonCrawlTargetInfo.CoverUrl))
                    crawledUrls.Add(new PatreonCrawledUrl { PostId = "0", Url = patreonCrawlTargetInfo.CoverUrl, UrlType = PatreonCrawledUrlType.CoverFile });
            }

            string nextPage = CrawlStartUrl + $"&filter[campaign_id]={patreonCrawlTargetInfo.Id}";

            int page = 0;
            while (!string.IsNullOrEmpty(nextPage))
            {
                page++;
                _logger.Debug($"Page #{page}: {nextPage}");
                string json = await _webDownloader.DownloadString(nextPage);

                if(_patreonDownloaderSettings.SaveJson)
                    await File.WriteAllTextAsync(Path.Combine(_patreonDownloaderSettings.DownloadDirectory, $"page_{page}.json"),
                        json);

                ParsingResult result = await ParsePage(json);

                if(result.CrawledUrls.Count > 0)
                    crawledUrls.AddRange(result.CrawledUrls);

                nextPage = result.NextPage;

                await Task.Delay(500 * rnd.Next(1, 3)); //0.5 - 1 second delay
            }

            _logger.Debug("Finished crawl");

            return crawledUrls;
        }

        private async Task<ParsingResult> ParsePage(string json)
        {
            List<PatreonCrawledUrl> crawledUrls = new List<PatreonCrawledUrl>();
            List<string> skippedIncludesList = new List<string>(); //List for all included data which current account doesn't have access to

            Root jsonRoot = JsonConvert.DeserializeObject<Root>(json);

            _logger.Debug("Parsing data entries...");
            foreach (var jsonEntry in jsonRoot.Data)
            {
                OnPostCrawlStart(new PostCrawlEventArgs(jsonEntry.Id, true));
                _logger.Info($"-> {jsonEntry.Id}");
                if (jsonEntry.Type != "post")
                {
                    string msg = $"Invalid type for \"data\": {jsonEntry.Type}, skipping";
                    _logger.Error($"[{jsonEntry.Id}] {msg}");
                    OnPostCrawlEnd(new PostCrawlEventArgs(jsonEntry.Id, false, msg));
                    OnCrawlerMessage(new CrawlerMessageEventArgs(CrawlerMessageType.Error, msg, jsonEntry.Id));
                    continue;
                }

                _logger.Debug($"[{jsonEntry.Id}] Is a post");
                if (!jsonEntry.Attributes.CurrentUserCanView)
                {
                    _logger.Warn($"[{jsonEntry.Id}] Current user cannot view this post");

                    string[] skippedAttachments = jsonEntry.Relationships.Attachments?.Data.Select(x => x.Id).ToArray() ?? Array.Empty<string>();
                    string[] skippedMedia = jsonEntry.Relationships.Images?.Data.Select(x => x.Id).ToArray() ?? Array.Empty<string>();
                    _logger.Debug($"[{jsonEntry.Id}] Adding {skippedAttachments.Length} attachments and {skippedMedia.Length} media items to skipped list");

                    skippedIncludesList.AddRange(skippedAttachments);
                    skippedIncludesList.AddRange(skippedMedia);

                    OnPostCrawlEnd(new PostCrawlEventArgs(jsonEntry.Id, false, "Current user cannot view this post"));
                    OnCrawlerMessage(new CrawlerMessageEventArgs(CrawlerMessageType.Warning, "Current user cannot view this post", jsonEntry.Id));
                    continue;
                }

                if (_patreonDownloaderSettings.PublishedAfter != null && jsonEntry.Attributes.PublishedAt < _patreonDownloaderSettings.PublishedAfter)
                {
                    string msg = $"   -Not crawling because published at {jsonEntry.Attributes.PublishedAt}";
                    _logger.Info(msg);

                    OnPostCrawlEnd(new PostCrawlEventArgs(jsonEntry.Id, false, msg));
                    OnCrawlerMessage(new CrawlerMessageEventArgs(CrawlerMessageType.Info, msg, jsonEntry.Id));
                    continue;
                }

                if (_patreonDownloaderSettings.PublishedBefore != null && jsonEntry.Attributes.PublishedAt > _patreonDownloaderSettings.PublishedBefore)
                {
                    string msg = $"   -Not crawling because published at {jsonEntry.Attributes.PublishedAt}";
                    _logger.Info(msg);

                    OnPostCrawlEnd(new PostCrawlEventArgs(jsonEntry.Id, false, msg));
                    OnCrawlerMessage(new CrawlerMessageEventArgs(CrawlerMessageType.Info, msg, jsonEntry.Id));
                    continue;
                }

                PatreonCrawledUrl entry = new PatreonCrawledUrl
                {
                    PostId = jsonEntry.Id,
                    Title = jsonEntry.Attributes.Title,
                    PublishedAt = jsonEntry.Attributes.PublishedAt
                };

                string additionalFilesSaveDirectory = _patreonDownloaderSettings.DownloadDirectory;
                if (_patreonDownloaderSettings.IsUseSubDirectories &&
                    (_patreonDownloaderSettings.SaveDescriptions ||
                     (jsonEntry.Attributes.Embed != null && _patreonDownloaderSettings.SaveEmbeds)
                     )
                    )
                {
                    additionalFilesSaveDirectory = Path.Combine(_patreonDownloaderSettings.DownloadDirectory,
                        PostSubdirectoryHelper.CreateNameFromPattern(entry, _patreonDownloaderSettings.SubDirectoryPattern, _patreonDownloaderSettings.MaxSubdirectoryNameLength));
                    if (!Directory.Exists(additionalFilesSaveDirectory))
                        Directory.CreateDirectory(additionalFilesSaveDirectory);
                }

                if (_patreonDownloaderSettings.SaveDescriptions)
                {
                    try
                    {
                        string filename = "description.html";
                        if (!_patreonDownloaderSettings.IsUseSubDirectories)
                            filename = $"{jsonEntry.Id}_{filename}";

                        await File.WriteAllTextAsync(Path.Combine(additionalFilesSaveDirectory, filename),
                            jsonEntry.Attributes.Content);
                    }
                    catch (Exception ex)
                    {
                        string msg = $"Unable to save description: {ex}";
                        _logger.Error($"[{jsonEntry.Id}] {msg}");
                        OnCrawlerMessage(new CrawlerMessageEventArgs(CrawlerMessageType.Error, msg, jsonEntry.Id));
                    }
                }

                if (jsonEntry.Attributes.Embed != null)
                {
                    if (_patreonDownloaderSettings.SaveEmbeds)
                    {
                        _logger.Debug($"[{jsonEntry.Id}] Embed found, metadata will be saved");
                        try
                        {
                            string filename = "embed.txt";
                            if (!_patreonDownloaderSettings.IsUseSubDirectories)
                                filename = $"{jsonEntry.Id}_{filename}";

                            await File.WriteAllTextAsync(
                                Path.Combine(additionalFilesSaveDirectory, filename),
                                jsonEntry.Attributes.Embed.ToString());
                        }
                        catch (Exception ex)
                        {
                            string msg = $"Unable to save embed metadata: {ex}";
                            _logger.Error($"[{jsonEntry.Id}] {msg}");
                            OnCrawlerMessage(new CrawlerMessageEventArgs(CrawlerMessageType.Error, msg,
                                jsonEntry.Id));
                        }
                    }

                    PatreonCrawledUrl subEntry = (PatreonCrawledUrl)entry.Clone();
                    subEntry.Url = jsonEntry.Attributes.Embed.Url;
                    subEntry.UrlType = PatreonCrawledUrlType.ExternalUrl;
                    crawledUrls.Add(subEntry);
                    _logger.Info(
                        $"[{jsonEntry.Id}] New embed entry: {subEntry.Url}");

                    OnNewCrawledUrl(new NewCrawledUrlEventArgs((CrawledUrl)subEntry.Clone()));
                }

                //External urls via plugins (including direct via default plugin)
                List<string> pluginUrls = await _pluginManager.ExtractSupportedUrls(jsonEntry.Attributes.Content);
                foreach (string url in pluginUrls)
                {
                    PatreonCrawledUrl subEntry = (PatreonCrawledUrl)entry.Clone();
                    subEntry.Url = url;
                    subEntry.UrlType = PatreonCrawledUrlType.ExternalUrl;
                    crawledUrls.Add(subEntry);
                    _logger.Info($"[{jsonEntry.Id}] New external entry: {subEntry.Url}");
                    OnNewCrawledUrl(new NewCrawledUrlEventArgs((CrawledUrl)subEntry.Clone()));
                }

                _logger.Debug($"[{jsonEntry.Id}] Scanning attachment data");
                //Attachments
                if(jsonEntry.Relationships.Attachments?.Data != null)
                {
                    foreach (var attachment in jsonEntry.Relationships.Attachments.Data)
                    {
                        _logger.Debug($"[{jsonEntry.Id} A-{attachment.Id}] Scanning attachment");
                        if (attachment.Type != "attachment") //sanity check 
                        {
                            string msg = $"Invalid attachment type for {attachment.Id}!!!";
                            _logger.Fatal($"[{jsonEntry.Id}] {msg}");
                            OnCrawlerMessage(new CrawlerMessageEventArgs(CrawlerMessageType.Error, msg, jsonEntry.Id));
                            continue;
                        }

                        var attachmentData = jsonRoot.Included.FirstOrDefault(x => x.Type == "attachment" && x.Id == attachment.Id);

                        if (attachmentData == null)
                        {
                            string msg = $"Attachment data not found for {attachment.Id}!!!";
                            _logger.Fatal($"[{jsonEntry.Id}] {msg}");
                            OnCrawlerMessage(new CrawlerMessageEventArgs(CrawlerMessageType.Error, msg, jsonEntry.Id));
                            continue;
                        }

                        PatreonCrawledUrl subEntry = (PatreonCrawledUrl)entry.Clone(); ;
                        subEntry.Url = attachmentData.Attributes.Url;
                        subEntry.Filename = attachmentData.Attributes.Name;
                        subEntry.UrlType = PatreonCrawledUrlType.PostAttachment;
                        subEntry.FileId = attachmentData.Id;
                        crawledUrls.Add(subEntry);
                        _logger.Info($"[{jsonEntry.Id} A-{attachment.Id}] New attachment entry: {subEntry.Url}");
                        OnNewCrawledUrl(new NewCrawledUrlEventArgs((CrawledUrl)subEntry.Clone()));
                    }
                }

                _logger.Debug($"[{jsonEntry.Id}] Scanning media data");
                //Media
                if (jsonEntry.Relationships.Images?.Data != null)
                {
                    foreach (var image in jsonEntry.Relationships.Images.Data)
                    {
                        _logger.Debug($"[{jsonEntry.Id} M-{image.Id}] Scanning media");
                        if (image.Type != "media") //sanity check 
                        {
                            string msg = $"invalid media type for {image.Id}!!!";
                            _logger.Fatal($"[{jsonEntry.Id}] {msg}");
                            OnCrawlerMessage(new CrawlerMessageEventArgs(CrawlerMessageType.Error, msg, jsonEntry.Id));
                            continue;
                        }

                        var imageData = jsonRoot.Included.FirstOrDefault(x => x.Type == "media" && x.Id == image.Id);

                        if (imageData == null)
                        {
                            string msg = $"media data not found for {image.Id}!!!";
                            _logger.Fatal($"[{jsonEntry.Id}] {msg}");
                            OnCrawlerMessage(new CrawlerMessageEventArgs(CrawlerMessageType.Error, msg, jsonEntry.Id));
                            continue;
                        }

                        _logger.Debug($"[{jsonEntry.Id} M-{image.Id}] Searching for download url");
                        string downloadUrl = imageData.Attributes.DownloadUrl;

                        _logger.Debug($"[{jsonEntry.Id} M-{image.Id}] Download url is: {downloadUrl}");

                        PatreonCrawledUrl subEntry = (PatreonCrawledUrl)entry.Clone(); ;
                        subEntry.Url = downloadUrl;
                        subEntry.Filename = imageData.Attributes.FileName;
                        subEntry.UrlType = PatreonCrawledUrlType.PostMedia;
                        subEntry.FileId = image.Id;
                        crawledUrls.Add(subEntry);
                        _logger.Info($"[{jsonEntry.Id} M-{image.Id}] New media entry from {subEntry.Url}");
                        OnNewCrawledUrl(new NewCrawledUrlEventArgs((CrawledUrl)subEntry.Clone()));
                    }
                }

                _logger.Debug($"[{jsonEntry.Id}] Parsing base entry");
                //Now parse the entry itself, url type is set just before adding entry into list
                if (jsonEntry.Attributes.PostFile != null)
                {
                    _logger.Debug($"[{jsonEntry.Id}] Found file data");
                    entry.Url = jsonEntry.Attributes.PostFile.Url;
                    entry.Filename = jsonEntry.Attributes.PostFile.Name;
                }
                else
                {
                    _logger.Debug($"[{jsonEntry.Id}] No file data, fallback to image data");
                    if (jsonEntry.Attributes.Image != null)
                    {
                        _logger.Debug($"[{jsonEntry.Id}] Found image data");
                        if (jsonEntry.Attributes.Image.LargeUrl != null)
                        {
                            _logger.Debug($"[{jsonEntry.Id}] Found large url");
                            entry.Url = jsonEntry.Attributes.Image.LargeUrl;
                        }
                        else if (jsonEntry.Attributes.Image.Url != null)
                        {
                            _logger.Debug($"[{jsonEntry.Id}] Found regular url");
                            entry.Url = jsonEntry.Attributes.Image.Url;

                        }
                        else
                        {
                            _logger.Warn($"[{jsonEntry.Id}] No valid image data found");
                            OnCrawlerMessage(new CrawlerMessageEventArgs(CrawlerMessageType.Warning, "No valid image data found", jsonEntry.Id));
                        }
                    }
                }

                if (!string.IsNullOrEmpty(entry.Url))
                {
                    entry.UrlType = PatreonCrawledUrlType.PostFile;
                    _logger.Info($"[{jsonEntry.Id}] New entry: {entry.Url}");
                    crawledUrls.Add(entry);
                    OnNewCrawledUrl(new NewCrawledUrlEventArgs((CrawledUrl)entry.Clone()));
                }
                else
                {
                    _logger.Warn($"[{jsonEntry.Id}] Post entry doesn't have download url");
                    OnCrawlerMessage(new CrawlerMessageEventArgs(CrawlerMessageType.Warning, "Post entry doesn't have download url"));
                }
            }

            _logger.Debug("Checking if all included entries were added...");
            foreach (var jsonEntry in jsonRoot.Included)
            {
                _logger.Debug($"[{jsonEntry.Id}] Verification: Started");
                if (jsonEntry.Type != "attachment" && jsonEntry.Type != "media")
                {
                    if (jsonEntry.Type != "user" &&
                        jsonEntry.Type != "campaign" &&
                        jsonEntry.Type != "access-rule" &&
                        jsonEntry.Type != "reward" &&
                        jsonEntry.Type != "poll_choice" &&
                        jsonEntry.Type != "poll_response")
                    {
                        string msg = $"Verification for {jsonEntry.Id}: Unknown type for \"included\": {jsonEntry.Type}";
                        _logger.Error(msg);
                        OnCrawlerMessage(new CrawlerMessageEventArgs(CrawlerMessageType.Error, msg));
                    }
                    continue;
                }

                _logger.Debug($"[{jsonEntry.Id}] Is a {jsonEntry.Type}");

                if (jsonEntry.Type == "attachment")
                {
                    if (!skippedIncludesList.Any(x => x == jsonEntry.Id) && !crawledUrls.Any(x => x.Url == jsonEntry.Attributes.Url))
                    {
                        string msg =
                            $"Verification for {jsonEntry.Id}: Parsing verification failure! Attachment with this id might not referenced by any post.";
                        _logger.Warn(msg);
                        OnCrawlerMessage(new CrawlerMessageEventArgs(CrawlerMessageType.Warning, msg));
                        continue;
                    }
                }

                if (jsonEntry.Type == "media")
                {
                    if (!skippedIncludesList.Any(x=>x == jsonEntry.Id) && !crawledUrls.Any(x => x.Url == jsonEntry.Attributes.DownloadUrl/* || x.DownloadUrl == jsonEntry.Attributes.ImageUrls.Original || x.DownloadUrl == jsonEntry.Attributes.ImageUrls.Default*/))
                    {
                        string msg =
                            $"Verification for {jsonEntry.Id}: Parsing verification failure! Media with this id might not be referenced by any post.";
                        _logger.Warn(msg);
                        OnCrawlerMessage(new CrawlerMessageEventArgs(CrawlerMessageType.Warning, msg));
                        continue;
                    }
                }

                _logger.Debug($"[{jsonEntry.Id}] Verification: OK");
                OnPostCrawlEnd(new PostCrawlEventArgs(jsonEntry.Id, true));
            }

            return new ParsingResult {CrawledUrls = crawledUrls, NextPage = jsonRoot.Links?.Next};
        }

        private void OnPostCrawlStart(PostCrawlEventArgs e)
        {
            EventHandler<PostCrawlEventArgs> handler = PostCrawlStart;
            handler?.Invoke(this, e);
        }

        private void OnPostCrawlEnd(PostCrawlEventArgs e)
        {
            EventHandler<PostCrawlEventArgs> handler = PostCrawlEnd;
            handler?.Invoke(this, e);
        }

        private void OnNewCrawledUrl(NewCrawledUrlEventArgs e)
        {
            EventHandler<NewCrawledUrlEventArgs> handler = NewCrawledUrl;
            handler?.Invoke(this, e);
        }

        private void OnCrawlerMessage(CrawlerMessageEventArgs e)
        {
            EventHandler<CrawlerMessageEventArgs> handler = CrawlerMessage;
            handler?.Invoke(this, e);
        }
    }
}
