using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;
using NLog;
using PatreonDownloader.Helpers;
using PatreonDownloader.Models;
using PatreonDownloader.Wrappers.Browser;
using PuppeteerSharp;

namespace PatreonDownloader
{
    internal sealed class PageCrawler : IPageCrawler
    {
        private readonly IWebDownloader _webDownloader;
        private readonly IRemoteFilenameRetriever _remoteFilenameRetriever;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private CampaignInfo _campaignInfo;
        private CookieContainer _cookieContainer;
        private string _downloadDirectory;

        public PageCrawler(IWebDownloader webDownloader, IRemoteFilenameRetriever remoteFilenameRetriever)
        {
            _webDownloader = webDownloader ?? throw new ArgumentNullException(nameof(webDownloader));
            _remoteFilenameRetriever = remoteFilenameRetriever ?? throw new ArgumentNullException(nameof(remoteFilenameRetriever));
        }

        public async Task Crawl(CampaignInfo campaignInfo)
        {
            _campaignInfo = campaignInfo; //TODO: check if all values are valid

            _logger.Info($"Starting crawling campaign {campaignInfo.Name}");
            List<GalleryEntry> galleryEntries = new List<GalleryEntry>();
            Random rnd = new Random(Guid.NewGuid().GetHashCode());

            _logger.Debug("Creating download directory");
            _downloadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "download", campaignInfo.Name);
            if (!Directory.Exists(_downloadDirectory))
            {
                Directory.CreateDirectory(_downloadDirectory);
            }

            //TODO: Research possibility of not hardcoding this string
            string nextPage = $"https://www.patreon.com/api/posts?include=user%2Cattachments%2Cuser_defined_tags%2Ccampaign%2Cpoll.choices%2Cpoll.current_user_responses.user%2Cpoll.current_user_responses.choice%2Cpoll.current_user_responses.poll%2Caccess_rules.tier.null%2Cimages.null%2Caudio.null&fields[post]=change_visibility_at%2Ccomment_count%2Ccontent%2Ccurrent_user_can_delete%2Ccurrent_user_can_view%2Ccurrent_user_has_liked%2Cembed%2Cimage%2Cis_paid%2Clike_count%2Cmin_cents_pledged_to_view%2Cpost_file%2Cpost_metadata%2Cpublished_at%2Cpatron_count%2Cpatreon_url%2Cpost_type%2Cpledge_url%2Cthumbnail_url%2Cteaser_text%2Ctitle%2Cupgrade_url%2Curl%2Cwas_posted_by_campaign_owner&fields[user]=image_url%2Cfull_name%2Curl&fields[campaign]=show_audio_post_download_links%2Cavatar_photo_url%2Cearnings_visibility%2Cis_nsfw%2Cis_monthly%2Cname%2Curl&fields[access_rule]=access_rule_type%2Camount_cents&fields[media]=id%2Cimage_urls%2Cdownload_url%2Cmetadata%2Cfile_name&fields[post]=change_visibility_at%2Ccomment_count%2Ccontent%2Ccurrent_user_can_delete%2Ccurrent_user_can_view%2Ccurrent_user_has_liked%2Cembed%2Cimage%2Cis_paid%2Clike_count%2Cmin_cents_pledged_to_view%2Cpost_file%2Cpost_metadata%2Cpublished_at%2Cpatron_count%2Cpatreon_url%2Cpost_type%2Cpledge_url%2Cthumbnail_url%2Cteaser_text%2Ctitle%2Cupgrade_url%2Curl%2Cwas_posted_by_campaign_owner&fields[user]=image_url%2Cfull_name%2Curl&fields[campaign]=show_audio_post_download_links%2Cavatar_photo_url%2Cearnings_visibility%2Cis_nsfw%2Cis_monthly%2Cname%2Curl&fields[access_rule]=access_rule_type%2Camount_cents&fields[media]=id%2Cimage_urls%2Cdownload_url%2Cmetadata%2Cfile_name&sort=-published_at&filter[campaign_id]={_campaignInfo.Id}&filter[is_draft]=false&filter[contains_exclusive_posts]=true&json-api-use-default-includes=false&json-api-version=1.0";

            while (!string.IsNullOrEmpty(nextPage))
            {
                _logger.Debug($"New page");
                string json = await _webDownloader.DownloadString(nextPage);

                ParsingResult result = await ParsePage(json);

                if(result.Entries.Count > 0)
                    galleryEntries.AddRange(result.Entries);

                nextPage = result.NextPage;

                await Task.Delay(500 * rnd.Next(1, 3)); //0.5 - 1 second delay
            }

            _logger.Info($"Starting download for #{_campaignInfo.Name}");

            //TODO: Download avatar and cover
            _logger.Info("Downloading avatar and cover");
            _logger.Debug("Retrieving avatar extension...");
            string avatarExt = await _remoteFilenameRetriever.RetrieveRemoteFileName(campaignInfo.AvatarUrl);
            if (avatarExt != null)
            {
                avatarExt = Path.GetExtension(avatarExt);

                _logger.Debug("Downloading avatar...");
                await _webDownloader.DownloadFile(campaignInfo.AvatarUrl, Path.Combine(_downloadDirectory, $"avatar{avatarExt}"));
            }
            else
            {
                _logger.Error("Unable to retrieve remote filename for avatar!");
            }

            _logger.Debug("Retrieving cover extension...");
            string coverExt = await _remoteFilenameRetriever.RetrieveRemoteFileName(campaignInfo.CoverUrl);
            if (coverExt != null)
            {
                coverExt = Path.GetExtension(coverExt);

                _logger.Debug("Downloading cover...");
                await _webDownloader.DownloadFile(campaignInfo.CoverUrl, Path.Combine(_downloadDirectory, $"cover{coverExt}"));
            }
            else
            {
                _logger.Error("Unable to retrieve remote filename for cover!");
            }

            for (int i = 0; i < galleryEntries.Count; i++)
            {
                GalleryEntry entry = galleryEntries[i];
                _logger.Info($"Downloading {i+1}/{galleryEntries.Count}: {entry.DownloadUrl}");

                string filename = entry.Path;

                _logger.Debug($"Sanitizing filename: {filename}");
                foreach (char c in System.IO.Path.GetInvalidFileNameChars())
                {
                    filename = filename.Replace(c, '_');
                }

                await _webDownloader.DownloadFile(entry.DownloadUrl, Path.Combine(_downloadDirectory, filename));
            }

            _logger.Debug($"Finished crawl");
        }

        private async Task<ParsingResult> ParsePage(string json)
        {
            //TODO: COMMAND LINE TOGGLE FOR JSON DUMPING (DEBUG PURPOSES)
            List<GalleryEntry> galleryEntries = new List<GalleryEntry>();
            List<string> skippedIncludesList = new List<string>(); //List for all included data which current account doesn't have access to

            Models.JSONObjects.Posts.Root jsonRoot = JsonConvert.DeserializeObject<Models.JSONObjects.Posts.Root>(json);

            _logger.Info("Parsing data entries...");
            foreach (var jsonEntry in jsonRoot.Data)
            {
                _logger.Info($"Entry {jsonEntry.Id}");
                if (jsonEntry.Type != "post")
                {
                    _logger.Error($"[{jsonEntry.Id}] Invalid type for \"data\": {jsonEntry.Type}, skipping");
                    continue;
                }

                _logger.Info($"[{jsonEntry.Id}] Is a post");
                if (!jsonEntry.Attributes.CurrentUserCanView)
                {
                    _logger.Warn($"[{jsonEntry.Id}] Current user cannot view selected post");

                    string[] skippedAttachments = jsonEntry.Relationships.Attachments?.Data.Select(x => x.Id).ToArray() ?? new string[0];
                    string[] skippedMedia = jsonEntry.Relationships.Images?.Data.Select(x => x.Id).ToArray() ?? new string[0];
                    _logger.Debug($"[{jsonEntry.Id}] Adding {skippedAttachments.Length} attachments and {skippedMedia.Length} media items to skipped list");

                    skippedIncludesList.AddRange(skippedAttachments);
                    skippedIncludesList.AddRange(skippedMedia);
                    continue;
                }

                //TODO: COMMAND LINE TOGGLE FOR THIS
                try
                {
                    await File.WriteAllTextAsync(Path.Combine(_downloadDirectory, $"{jsonEntry.Id}_description.txt"),
                        jsonEntry.Attributes.Content);
                }
                catch (Exception ex)
                {
                    _logger.Error($"Unable to write description for {jsonEntry.Id}: {ex}");
                }

                GalleryEntry entry = new GalleryEntry
                {
                    Author = _campaignInfo.Name,
                    PageUrl = jsonEntry.Attributes.Url,
                    Name = jsonEntry.Attributes.Title
                };

                //TODO: EMBED SUPPORT
                if (jsonEntry.Attributes.Embed != null)
                {
                    _logger.Fatal($"[{jsonEntry.Id}] NOT IMPLEMENTED: POST EMBED");
                }

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(jsonEntry.Attributes.Content);
                HtmlNodeCollection imgNodeCollection = doc.DocumentNode.SelectNodes("//img");
                if (imgNodeCollection != null)
                {
                    foreach (var imgNode in imgNodeCollection)
                    {
                        string url = imgNode.Attributes["src"].Value;
                        if (UrlChecker.IsValidUrl(url))
                        {
                            string filename =
                                await _remoteFilenameRetriever.RetrieveRemoteFileName(url);

                            if (filename != null)
                            {
                                GalleryEntry subEntry = entry;
                                subEntry.DownloadUrl = url;
                                subEntry.Path =
                                    $"{jsonEntry.Id}_extimg_{filename}"; // RetrieveRemoteFileName?
                                galleryEntries.Add(subEntry);
                                _logger.Info(
                                    $"[{jsonEntry.Id}] New external (image) entry: {subEntry.Path} from {subEntry.DownloadUrl}");
                            }
                            else
                            {
                                _logger.Error($"[{jsonEntry.Id}] Unable to retrieve name for external (image) entry: {url}");
                            }
                        }
                        else
                        {
                            _logger.Error($"[{jsonEntry.Id}] Invalid or blacklisted external (image) entry: {url}");
                        }
                    }
                }

                HtmlNodeCollection linkNodeCollection = doc.DocumentNode.SelectNodes("//a");
                if (linkNodeCollection != null)
                {
                    int cnt = 1;
                    foreach (var linkNode in linkNodeCollection)
                    {
                        if (linkNode.Attributes["href"] != null)
                        {
                            var url = linkNode.Attributes["href"].Value;
                            if (url.IndexOf("dropbox.com/", StringComparison.Ordinal) != -1)
                            {
                                if (!url.EndsWith("?dl=1"))
                                {
                                    if (url.EndsWith("?dl=0"))
                                        url = url.Replace("?dl=0", "?dl=1");
                                    else
                                        url = $"{url}?dl=1";
                                }

                                string filename =
                                    await _remoteFilenameRetriever.RetrieveRemoteFileName(url);

                                if (filename != null)
                                {
                                    GalleryEntry subEntry = entry;
                                    subEntry.DownloadUrl = url;
                                    subEntry.Path = $"{jsonEntry.Id}_extdropbox_{ filename }";
                                    galleryEntries.Add(subEntry);
                                    _logger.Info($"[{jsonEntry.Id}] New external (dropbox) entry: {subEntry.Path} from {subEntry.DownloadUrl}");
                                }
                                else
                                {
                                    _logger.Error($"[{jsonEntry.Id}] Unable to retrieve name for external (dropbox) entry: {url}");
                                }

                            }
                            else if (url.IndexOf("drive.google.com/file/d/", StringComparison.Ordinal) != -1)
                            {
                                //TODO: GOOGLE DRIVE SUPPORT
                                _logger.Fatal($"[{jsonEntry.Id}] [NOT SUPPORTED] Google Drive link found: {url}");
                            }
                            else if (url.StartsWith("https://mega.nz/"))
                            {
                                //TODO: MEGA SUPPORT
                                _logger.Fatal($"[{jsonEntry.Id}] [NOT SUPPORTED] MEGA link found: {url}");
                            }
                            else if (url.IndexOf("youtube.com/watch?v=", StringComparison.Ordinal) != -1 || url.IndexOf("youtu.be/", StringComparison.Ordinal) != -1)
                            {
                                //TODO: YOUTUBE SUPPORT?
                                _logger.Fatal($"[{jsonEntry.Id}] [NOT SUPPORTED] YOUTUBE link found: {url}");
                            }
                            else if (url.IndexOf("imgur.com/", StringComparison.Ordinal) != -1)
                            {
                                //TODO: IMGUR SUPPORT
                                _logger.Fatal($"[{jsonEntry.Id}] [NOT SUPPORTED] IMGUR link found: {url}");
                            }
                            else
                            {
                                _logger.Warn($"[{jsonEntry.Id}] Unknown provider link found for, assuming it's direct url: {url}");
                                if (UrlChecker.IsValidUrl(url))
                                {
                                    string filename =
                                        await _remoteFilenameRetriever.RetrieveRemoteFileName(url);

                                    if (filename != null)
                                    {
                                        GalleryEntry subEntry = entry;
                                        subEntry.DownloadUrl = url;
                                        subEntry.Path = $"{jsonEntry.Id}_direct_{ filename }";
                                        galleryEntries.Add(subEntry);
                                        _logger.Info($"[{jsonEntry.Id}] New external (direct) entry: {subEntry.Path} from {subEntry.DownloadUrl}");
                                    }
                                    else
                                    {
                                        _logger.Error($"[{jsonEntry.Id}] Unable to retrieve name for external (direct) entry: {url}");
                                    }
                                }
                                else
                                {
                                    _logger.Error($"[{jsonEntry.Id}] Direct url is invalid or blacklisted, skipping: {url}");
                                }
                            }
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
                            _logger.Fatal($"[{jsonEntry.Id} A-{attachment.Id}] invalid attachment type for!!!");
                            continue;
                        }

                        var attachmentData =
                            jsonRoot.Included
                                .FirstOrDefault(x => x.Type == "attachment" && x.Id == attachment.Id);

                        if (attachmentData == null)
                        {
                            _logger.Fatal($"[{jsonEntry.Id} A-{attachment.Id}] attachment data not found!!!");
                            continue;
                        }

                        GalleryEntry subEntry = entry;
                        subEntry.DownloadUrl = attachmentData.Attributes.Url;
                        subEntry.Path = $"{jsonEntry.Id}_attachment_{attachmentData.Attributes.Name}";
                        galleryEntries.Add(subEntry);
                        _logger.Info($"[{jsonEntry.Id} A-{attachment.Id}] New attachment entry: {subEntry.Path} from {subEntry.DownloadUrl}");
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

                        var imageData =
                            jsonRoot.Included
                                .FirstOrDefault(x => x.Type == "media" && x.Id == image.Id);

                        if (imageData == null)
                        {
                            _logger.Fatal($"[{jsonEntry.Id}] media data not found for {image.Id}!!!");
                            continue;
                        }

                        _logger.Debug($"[{jsonEntry.Id} M-{image.Id}] Searching for download url");
                        string downloadUrl = imageData.Attributes.DownloadUrl;

                        /*if (string.IsNullOrEmpty(downloadUrl))
                        {
                            if (!string.IsNullOrEmpty(imageData.Attributes.ImageUrls.Original))
                                downloadUrl = imageData.Attributes.ImageUrls.Original;
                            else if (!string.IsNullOrEmpty(imageData.Attributes.ImageUrls.Default))
                                downloadUrl = imageData.Attributes.ImageUrls.Default;
                            else
                            {
                                _logger.Fatal($"[{jsonEntry.Id} M-{image.Id}] No valid download url found");
                                continue;
                            }
                        }*/

                        _logger.Debug($"[{jsonEntry.Id} M-{image.Id}] Download url is: {downloadUrl}");

                        GalleryEntry subEntry = entry;
                        subEntry.DownloadUrl = downloadUrl;
                        subEntry.Path = $"{jsonEntry.Id}_media_{imageData.Attributes.FileName}";
                        galleryEntries.Add(subEntry);
                        _logger.Info($"[{jsonEntry.Id} M-{image.Id}] New media entry: {subEntry.Path} from {subEntry.DownloadUrl}");
                    }
                }

                _logger.Debug($"[{jsonEntry.Id}] Parsing base entry");
                //Now parse the entry itself
                if (jsonEntry.Attributes.PostFile != null)
                {
                    _logger.Debug($"[{jsonEntry.Id}] Found file data");
                    entry.DownloadUrl = jsonEntry.Attributes.PostFile.Url;
                    entry.Path = $"{jsonEntry.Id}_post_{jsonEntry.Attributes.PostFile.Name}";
                }
                else
                {
                    _logger.Debug($"[{jsonEntry.Id}] No file data, fallback to image data");
                    if (jsonEntry.Attributes.Image != null)
                    {
                        _logger.Debug($"[{jsonEntry.Id}] Found image data");
                        if (jsonEntry.Attributes.Image.LargeUrl != null)
                        {
                            string filename =
                                await _remoteFilenameRetriever.RetrieveRemoteFileName(jsonEntry.Attributes.Image.LargeUrl);

                            if (filename != null)
                            {
                                _logger.Debug($"[{jsonEntry.Id}] Found large url");
                                entry.DownloadUrl = jsonEntry.Attributes.Image.LargeUrl;
                                entry.Path = $"{jsonEntry.Id}_post_{ filename }";
                            }
                            else
                            {
                                _logger.Error($"[{jsonEntry.Id}] Unable to retrieve name for large url: {jsonEntry.Attributes.Image.LargeUrl}");
                            }
                        }
                        else if (jsonEntry.Attributes.Image.Url != null)
                        {
                            string filename =
                                await _remoteFilenameRetriever.RetrieveRemoteFileName(jsonEntry.Attributes.Image.Url);

                            if (filename != null)
                            {
                                _logger.Debug($"[{jsonEntry.Id}] Found regular url");
                                entry.DownloadUrl = jsonEntry.Attributes.Image.Url;
                                entry.Path = $"{jsonEntry.Id}_post_{ filename }";
                            }
                            else
                            {
                                _logger.Error($"[{jsonEntry.Id}] Unable to retrieve name for regular url: {jsonEntry.Attributes.Image.Url}");
                            }
                        }
                        else
                        {
                            _logger.Debug($"[{jsonEntry.Id}] No valid image data found");
                        }
                    }
                }

                if (!string.IsNullOrEmpty(entry.DownloadUrl) && !string.IsNullOrEmpty(entry.Path))
                {
                    _logger.Info($"[{jsonEntry.Id}] New entry: {entry.Path} from {entry.DownloadUrl}");
                    galleryEntries.Add(entry);
                }
                else
                {
                    _logger.Warn($"[{jsonEntry.Id}] Post entry doesn't have required values: {(entry.DownloadUrl != null ? entry.DownloadUrl : "no download url")}  {(entry.Path != null ? entry.Path : "no path")}");
                }
            }

            _logger.Info("Checking if all included entries were added...");
            foreach (var jsonEntry in jsonRoot.Included)
            {
                _logger.Info($"Entry {jsonEntry.Id}");
                if (jsonEntry.Type != "attachment" && jsonEntry.Type != "media")
                {
                    _logger.Error($"[{jsonEntry.Id}] Invalid type for \"included\": {jsonEntry.Type}, skipping");
                    continue;
                }

                _logger.Info($"[{jsonEntry.Id}] Is a {jsonEntry.Type}");

                if (jsonEntry.Type == "attachment")
                {
                    if (!skippedIncludesList.Any(x => x == jsonEntry.Id) && !galleryEntries.Any(x => x.DownloadUrl == jsonEntry.Attributes.Url))
                    {
                        _logger.Warn($"[{jsonEntry.Id}] Attachment was not parsed! Attachment not referenced by any post?");
                        continue;
                    }
                }

                if (jsonEntry.Type == "media")
                {
                    if (!skippedIncludesList.Any(x=>x == jsonEntry.Id) && !galleryEntries.Any(x => x.DownloadUrl == jsonEntry.Attributes.DownloadUrl/* || x.DownloadUrl == jsonEntry.Attributes.ImageUrls.Original || x.DownloadUrl == jsonEntry.Attributes.ImageUrls.Default*/))
                    {
                        _logger.Warn($"[{jsonEntry.Id}] Media was not parsed! Media not referenced by any post?");
                        continue;
                    }
                }

                _logger.Info($"[{jsonEntry.Id}] OK");
            }

            return new ParsingResult {Entries = galleryEntries, NextPage = jsonRoot.Links?.Next};
        }
    }
}
