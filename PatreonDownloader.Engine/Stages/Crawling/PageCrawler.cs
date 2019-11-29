using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;
using NLog;
using PatreonDownloader.Engine.Events;
using PatreonDownloader.Engine.Models;
using PatreonDownloader.Engine.Models.JSONObjects;
using PatreonDownloader.Engine.Models.JSONObjects.Posts;
using PatreonDownloader.Engine.Stages.Downloading;
using PatreonDownloader.Interfaces.Models;

namespace PatreonDownloader.Engine.Stages.Crawling
{
    internal sealed class PageCrawler : IPageCrawler
    {
        private readonly IWebDownloader _webDownloader;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public event EventHandler<PostCrawlEventArgs> PostCrawlStart;
        public event EventHandler<PostCrawlEventArgs> PostCrawlEnd; 
        public event EventHandler<NewCrawledUrlEventArgs> NewCrawledUrl;
        public event EventHandler<CrawlerMessageEventArgs> CrawlerMessage; 

        public PageCrawler(IWebDownloader webDownloader)
        {
            _webDownloader = webDownloader ?? throw new ArgumentNullException(nameof(webDownloader));
        }

        public async Task<List<CrawledUrl>> Crawl(CampaignInfo campaignInfo, PatreonDownloaderSettings settings)
        {
            if(campaignInfo.Id < 1)
                throw new ArgumentException("Campaign ID cannot be less than 1");
            if (string.IsNullOrEmpty(campaignInfo.Name))
                throw new ArgumentException("Campaign name cannot be null or empty");
            if (string.IsNullOrEmpty(campaignInfo.CoverUrl))
                throw new ArgumentException("Campaign cover url cannot be null or empty");
            if (string.IsNullOrEmpty(campaignInfo.AvatarUrl))
                throw new ArgumentException("Campaign name cannot be null or empty");
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            _logger.Debug($"Starting crawling campaign {campaignInfo.Name}");
            List<CrawledUrl> crawledUrls = new List<CrawledUrl>();
            Random rnd = new Random(Guid.NewGuid().GetHashCode());

            if (settings.SaveAvatarAndCover)
            {
                _logger.Debug("Adding avatar and cover...");
                crawledUrls.Add(new CrawledUrl { PostId = 0, Url = campaignInfo.AvatarUrl, UrlType = CrawledUrlType.AvatarFile });
                crawledUrls.Add(new CrawledUrl { PostId = 0, Url = campaignInfo.CoverUrl, UrlType = CrawledUrlType.CoverFile });
            }

            //TODO: Research possibility of not hardcoding this string
            string nextPage = $"https://www.patreon.com/api/posts?include=user%2Cattachments%2Cuser_defined_tags%2Ccampaign%2Cpoll.choices%2Cpoll.current_user_responses.user%2Cpoll.current_user_responses.choice%2Cpoll.current_user_responses.poll%2Caccess_rules.tier.null%2Cimages.null%2Caudio.null&fields[post]=change_visibility_at%2Ccomment_count%2Ccontent%2Ccurrent_user_can_delete%2Ccurrent_user_can_view%2Ccurrent_user_has_liked%2Cembed%2Cimage%2Cis_paid%2Clike_count%2Cmin_cents_pledged_to_view%2Cpost_file%2Cpost_metadata%2Cpublished_at%2Cpatron_count%2Cpatreon_url%2Cpost_type%2Cpledge_url%2Cthumbnail_url%2Cteaser_text%2Ctitle%2Cupgrade_url%2Curl%2Cwas_posted_by_campaign_owner&fields[user]=image_url%2Cfull_name%2Curl&fields[campaign]=show_audio_post_download_links%2Cavatar_photo_url%2Cearnings_visibility%2Cis_nsfw%2Cis_monthly%2Cname%2Curl&fields[access_rule]=access_rule_type%2Camount_cents&fields[media]=id%2Cimage_urls%2Cdownload_url%2Cmetadata%2Cfile_name&fields[post]=change_visibility_at%2Ccomment_count%2Ccontent%2Ccurrent_user_can_delete%2Ccurrent_user_can_view%2Ccurrent_user_has_liked%2Cembed%2Cimage%2Cis_paid%2Clike_count%2Cmin_cents_pledged_to_view%2Cpost_file%2Cpost_metadata%2Cpublished_at%2Cpatron_count%2Cpatreon_url%2Cpost_type%2Cpledge_url%2Cthumbnail_url%2Cteaser_text%2Ctitle%2Cupgrade_url%2Curl%2Cwas_posted_by_campaign_owner&fields[user]=image_url%2Cfull_name%2Curl&fields[campaign]=show_audio_post_download_links%2Cavatar_photo_url%2Cearnings_visibility%2Cis_nsfw%2Cis_monthly%2Cname%2Curl&fields[access_rule]=access_rule_type%2Camount_cents&fields[media]=id%2Cimage_urls%2Cdownload_url%2Cmetadata%2Cfile_name&sort=-published_at&filter[campaign_id]={campaignInfo.Id}&filter[is_draft]=false&filter[contains_exclusive_posts]=true&json-api-use-default-includes=false&json-api-version=1.0";

            int page = 0;
            while (!string.IsNullOrEmpty(nextPage))
            {
                page++;
                _logger.Debug($"Page #{page}: {nextPage}");
                string json = await _webDownloader.DownloadString(nextPage);

                if(settings.SaveJson)
                    await File.WriteAllTextAsync(Path.Combine(settings.DownloadDirectory, $"page_{page}.json"),
                        json);

                ParsingResult result = await ParsePage(json, settings);

                if(result.Entries.Count > 0)
                    crawledUrls.AddRange(result.Entries);

                nextPage = result.NextPage;

                await Task.Delay(500 * rnd.Next(1, 3)); //0.5 - 1 second delay
            }

            _logger.Debug("Finished crawl");

            return crawledUrls;
        }

        private async Task<ParsingResult> ParsePage(string json, PatreonDownloaderSettings settings)
        {
            List<CrawledUrl> galleryEntries = new List<CrawledUrl>();
            List<string> skippedIncludesList = new List<string>(); //List for all included data which current account doesn't have access to

            Models.JSONObjects.Posts.Root jsonRoot = JsonConvert.DeserializeObject<Models.JSONObjects.Posts.Root>(json);

            _logger.Debug("Parsing data entries...");
            foreach (var jsonEntry in jsonRoot.Data)
            {
                OnPostCrawlStart(new PostCrawlEventArgs(jsonEntry.IdInt64, true));
                _logger.Info($"-> {jsonEntry.Id}");
                if (jsonEntry.Type != "post")
                {
                    string msg = $"Invalid type for \"data\": {jsonEntry.Type}, skipping";
                    _logger.Error($"[{jsonEntry.Id}] {msg}");
                    OnPostCrawlEnd(new PostCrawlEventArgs(jsonEntry.IdInt64, false, msg));
                    OnCrawlerMessage(new CrawlerMessageEventArgs(CrawlerMessageType.Error, msg, jsonEntry.IdInt64));
                    continue;
                }

                _logger.Debug($"[{jsonEntry.Id}] Is a post");
                if (!jsonEntry.Attributes.CurrentUserCanView)
                {
                    _logger.Warn($"[{jsonEntry.Id}] Current user cannot view this post");

                    string[] skippedAttachments = jsonEntry.Relationships.Attachments?.Data.Select(x => x.Id).ToArray() ?? new string[0];
                    string[] skippedMedia = jsonEntry.Relationships.Images?.Data.Select(x => x.Id).ToArray() ?? new string[0];
                    _logger.Debug($"[{jsonEntry.Id}] Adding {skippedAttachments.Length} attachments and {skippedMedia.Length} media items to skipped list");

                    skippedIncludesList.AddRange(skippedAttachments);
                    skippedIncludesList.AddRange(skippedMedia);

                    OnPostCrawlEnd(new PostCrawlEventArgs(jsonEntry.IdInt64, false, "Current user cannot view this post"));
                    OnCrawlerMessage(new CrawlerMessageEventArgs(CrawlerMessageType.Warning, "Current user cannot view this post", jsonEntry.IdInt64));
                    continue;
                }

                if (settings.SaveDescriptions)
                {
                    try
                    {
                        await File.WriteAllTextAsync(Path.Combine(settings.DownloadDirectory, $"{jsonEntry.Id}_description.txt"),
                            jsonEntry.Attributes.Content); //TODO: WRITE TITLE, AND OTHER METADATA?
                    }
                    catch (Exception ex)
                    {
                        string msg = $"Unable to save description: {ex}";
                        _logger.Error($"[{jsonEntry.Id}] {msg}");
                        OnCrawlerMessage(new CrawlerMessageEventArgs(CrawlerMessageType.Error, msg, jsonEntry.IdInt64));
                    }
                }

                CrawledUrl entry = new CrawledUrl
                {
                    PostId = jsonEntry.IdInt64
                };

                if (settings.SaveEmbeds)
                {
                    if (jsonEntry.Attributes.Embed != null)
                    {
                        _logger.Debug($"[{jsonEntry.Id}] Embed found");
                        try
                        {
                            await File.WriteAllTextAsync(
                                Path.Combine(settings.DownloadDirectory, $"{jsonEntry.Id}_embed.txt"),
                                jsonEntry.Attributes.Embed.ToString());
                        }
                        catch (Exception ex)
                        {
                            string msg = $"Unable to save embed: {ex}";
                            _logger.Error($"[{jsonEntry.Id}] {msg}");
                            OnCrawlerMessage(new CrawlerMessageEventArgs(CrawlerMessageType.Error, msg, jsonEntry.IdInt64));
                        }
                    }
                }

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(jsonEntry.Attributes.Content);
                HtmlNodeCollection imgNodeCollection = doc.DocumentNode.SelectNodes("//img");
                if (imgNodeCollection != null)
                {
                    foreach (var imgNode in imgNodeCollection)
                    {
                        string url = imgNode.Attributes["src"].Value;

                        CrawledUrl subEntry = (CrawledUrl)entry.Clone();
                        subEntry.Url = url;
                        subEntry.UrlType = CrawledUrlType.ExternalUrl;
                        galleryEntries.Add(subEntry);
                        _logger.Info(
                            $"[{jsonEntry.Id}] New external (image) entry: {subEntry.Url}");

                        OnNewCrawledUrl(new NewCrawledUrlEventArgs((CrawledUrl)subEntry.Clone()));
                    }
                }

                //TODO: Implement link parsing as plugins?
                HtmlNodeCollection linkNodeCollection = doc.DocumentNode.SelectNodes("//a");
                if (linkNodeCollection != null)
                {
                    int cnt = 1;
                    foreach (var linkNode in linkNodeCollection)
                    {
                        if (linkNode.Attributes["href"] != null)
                        {
                            var url = linkNode.Attributes["href"].Value;

                            CrawledUrl subEntry = (CrawledUrl)entry.Clone();
                            subEntry.Url = url;
                            subEntry.UrlType = CrawledUrlType.ExternalUrl;
                            galleryEntries.Add(subEntry);
                            _logger.Info($"[{jsonEntry.Id}] New external (direct) entry: {subEntry.Url}");
                            OnNewCrawledUrl(new NewCrawledUrlEventArgs((CrawledUrl)subEntry.Clone()));
                        }
                        else
                        {
                            _logger.Warn($"[{jsonEntry.Id}] link with invalid href found, ignoring...");
                            OnCrawlerMessage(new CrawlerMessageEventArgs(CrawlerMessageType.Warning, "link with invalid href found, ignoring...", jsonEntry.IdInt64));
                        }
                    }
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
                            OnCrawlerMessage(new CrawlerMessageEventArgs(CrawlerMessageType.Error, msg, jsonEntry.IdInt64));
                            continue;
                        }

                        var attachmentData = jsonRoot.Included.FirstOrDefault(x => x.Type == "attachment" && x.Id == attachment.Id);

                        if (attachmentData == null)
                        {
                            string msg = $"Attachment data not found for {attachment.Id}!!!";
                            _logger.Fatal($"[{jsonEntry.Id}] {msg}");
                            OnCrawlerMessage(new CrawlerMessageEventArgs(CrawlerMessageType.Error, msg, jsonEntry.IdInt64));
                            continue;
                        }

                        CrawledUrl subEntry = (CrawledUrl)entry.Clone(); ;
                        subEntry.Url = attachmentData.Attributes.Url;
                        subEntry.Filename = attachmentData.Attributes.Name;
                        subEntry.UrlType = CrawledUrlType.PostAttachment;
                        galleryEntries.Add(subEntry);
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
                            OnCrawlerMessage(new CrawlerMessageEventArgs(CrawlerMessageType.Error, msg, jsonEntry.IdInt64));
                            continue;
                        }

                        var imageData = jsonRoot.Included.FirstOrDefault(x => x.Type == "media" && x.Id == image.Id);

                        if (imageData == null)
                        {
                            string msg = $"media data not found for {image.Id}!!!";
                            _logger.Fatal($"[{jsonEntry.Id}] {msg}");
                            OnCrawlerMessage(new CrawlerMessageEventArgs(CrawlerMessageType.Error, msg, jsonEntry.IdInt64));
                            continue;
                        }

                        _logger.Debug($"[{jsonEntry.Id} M-{image.Id}] Searching for download url");
                        string downloadUrl = imageData.Attributes.DownloadUrl;

                        _logger.Debug($"[{jsonEntry.Id} M-{image.Id}] Download url is: {downloadUrl}");

                        CrawledUrl subEntry = (CrawledUrl)entry.Clone(); ;
                        subEntry.Url = downloadUrl;
                        subEntry.Filename = imageData.Attributes.FileName;
                        subEntry.UrlType = CrawledUrlType.PostMedia;
                        galleryEntries.Add(subEntry);
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
                            OnCrawlerMessage(new CrawlerMessageEventArgs(CrawlerMessageType.Warning, "No valid image data found", jsonEntry.IdInt64));
                        }
                    }
                }

                if (!string.IsNullOrEmpty(entry.Url))
                {
                    entry.UrlType = CrawledUrlType.PostFile;
                    _logger.Info($"[{jsonEntry.Id}] New entry: {entry.Url}");
                    galleryEntries.Add(entry);
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
                    if (jsonEntry.Type != "user" && jsonEntry.Type != "campaign" && jsonEntry.Type != "access-rule")
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
                    if (!skippedIncludesList.Any(x => x == jsonEntry.Id) && !galleryEntries.Any(x => x.Url == jsonEntry.Attributes.Url))
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
                    if (!skippedIncludesList.Any(x=>x == jsonEntry.Id) && !galleryEntries.Any(x => x.Url == jsonEntry.Attributes.DownloadUrl/* || x.DownloadUrl == jsonEntry.Attributes.ImageUrls.Original || x.DownloadUrl == jsonEntry.Attributes.ImageUrls.Default*/))
                    {
                        string msg =
                            $"Verification for {jsonEntry.Id}: Parsing verification failure! Media with this id might not be referenced by any post.";
                        _logger.Warn(msg);
                        OnCrawlerMessage(new CrawlerMessageEventArgs(CrawlerMessageType.Warning, msg));
                        continue;
                    }
                }

                _logger.Debug($"[{jsonEntry.Id}] Verification: OK");
                OnPostCrawlEnd(new PostCrawlEventArgs(jsonEntry.IdInt64, true));
            }

            return new ParsingResult {Entries = galleryEntries, NextPage = jsonRoot.Links?.Next};
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
