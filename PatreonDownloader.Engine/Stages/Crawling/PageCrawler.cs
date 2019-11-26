using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;
using NLog;
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

        public PageCrawler(IWebDownloader webDownloader)
        {
            _webDownloader = webDownloader ?? throw new ArgumentNullException(nameof(webDownloader));
        }

        public async Task<List<CrawledUrl>> Crawl(CampaignInfo campaignInfo, PatreonDownloaderSettings settings)
        {
            if(settings == null)
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
                _logger.Info($"-> {jsonEntry.Id}");
                if (jsonEntry.Type != "post")
                {
                    _logger.Error($"[{jsonEntry.Id}] Invalid type for \"data\": {jsonEntry.Type}, skipping");
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
                        _logger.Error($"[{jsonEntry.Id}] Unable to save description: {ex}");
                    }
                }

                CrawledUrl entry = new CrawledUrl
                {
                    PostId = Convert.ToInt64((string) jsonEntry.Id)
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
                            _logger.Error($"[{jsonEntry.Id}] Unable to save embed: {ex}");
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
                        }
                        else
                        {
                            _logger.Warn($"[{jsonEntry.Id}] link with invalid href for {jsonEntry.Id}");
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
                            _logger.Fatal($"[{jsonEntry.Id} A-{attachment.Id}] invalid attachment type!!!");
                            continue;
                        }

                        var attachmentData = jsonRoot.Included.FirstOrDefault(x => x.Type == "attachment" && x.Id == attachment.Id);

                        if (attachmentData == null)
                        {
                            _logger.Fatal($"[{jsonEntry.Id} A-{attachment.Id}] attachment data not found!!!");
                            continue;
                        }

                        CrawledUrl subEntry = (CrawledUrl)entry.Clone(); ;
                        subEntry.Url = attachmentData.Attributes.Url;
                        subEntry.Filename = attachmentData.Attributes.Name;
                        subEntry.UrlType = CrawledUrlType.PostAttachment;
                        galleryEntries.Add(subEntry);
                        _logger.Info($"[{jsonEntry.Id} A-{attachment.Id}] New attachment entry: {subEntry.Filename} from {subEntry.Url}");
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
                            _logger.Fatal($"[{jsonEntry.Id}] invalid media type for {image.Id}!!!");
                            continue;
                        }

                        var imageData = jsonRoot.Included.FirstOrDefault(x => x.Type == "media" && x.Id == image.Id);

                        if (imageData == null)
                        {
                            _logger.Fatal($"[{jsonEntry.Id}] media data not found for {image.Id}!!!");
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
                        _logger.Info($"[{jsonEntry.Id} M-{image.Id}] New media entry: {subEntry.Filename} from {subEntry.Url}");
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
                            _logger.Debug($"[{jsonEntry.Id}] No valid image data found");
                        }
                    }
                }

                if (!string.IsNullOrEmpty(entry.Url))
                {
                    entry.UrlType = CrawledUrlType.PostFile;
                    _logger.Info($"[{jsonEntry.Id}] New entry: {entry.Filename} from {entry.Url}");
                    galleryEntries.Add(entry);
                }
                else
                {
                    _logger.Warn($"[{jsonEntry.Id}] Post entry doesn't have download url");
                }
            }

            _logger.Debug("Checking if all included entries were added...");
            foreach (var jsonEntry in jsonRoot.Included)
            {
                _logger.Debug($"[{jsonEntry.Id}] Verification: Started");
                if (jsonEntry.Type != "attachment" && jsonEntry.Type != "media")
                {
                    _logger.Error($"[{jsonEntry.Id}] Verification: Invalid type for \"included\": {jsonEntry.Type}, skipping");
                    continue;
                }

                _logger.Debug($"[{jsonEntry.Id}] Is a {jsonEntry.Type}");

                if (jsonEntry.Type == "attachment")
                {
                    if (!skippedIncludesList.Any(x => x == jsonEntry.Id) && !galleryEntries.Any(x => x.Url == jsonEntry.Attributes.Url))
                    {
                        _logger.Warn($"[{jsonEntry.Id}] Verification: Parsing verification failure! Attachment with this id might not referenced by any post.");
                        continue;
                    }
                }

                if (jsonEntry.Type == "media")
                {
                    if (!skippedIncludesList.Any(x=>x == jsonEntry.Id) && !galleryEntries.Any(x => x.Url == jsonEntry.Attributes.DownloadUrl/* || x.DownloadUrl == jsonEntry.Attributes.ImageUrls.Original || x.DownloadUrl == jsonEntry.Attributes.ImageUrls.Default*/))
                    {
                        _logger.Warn($"[{jsonEntry.Id}] Verification: Parsing verification failure! Media with this id might not be referenced by any post.");
                        continue;
                    }
                }

                _logger.Debug($"[{jsonEntry.Id}] Verification: OK");
            }

            return new ParsingResult {Entries = galleryEntries, NextPage = jsonRoot.Links?.Next};
        }
    }
}
