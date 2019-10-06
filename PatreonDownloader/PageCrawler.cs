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
using PatreonDownloader.Models;
using PuppeteerSharp;

namespace PatreonDownloader
{
    internal sealed class PageCrawler : IPageCrawler
    {
        private readonly Browser _browser;

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private CampaignInfo _campaignInfo;
        public PageCrawler(Browser browser)
        {
            _browser = browser ?? throw new ArgumentNullException(nameof(browser));
        }

        public async Task Crawl(CampaignInfo campaignInfo)
        {
            _logger.Info($"Starting crawling campaign {campaignInfo.Name}");
            List<GalleryEntry> galleryEntries = new List<GalleryEntry>();
            Random rnd = new Random(Guid.NewGuid().GetHashCode());
            _campaignInfo = campaignInfo;

            var page = await _browser.NewPageAsync();
            //TODO: Research possibility of not hardcoding this string
            string nextPage = $"https://www.patreon.com/api/posts?include=user%2Cattachments%2Cuser_defined_tags%2Ccampaign%2Cpoll.choices%2Cpoll.current_user_responses.user%2Cpoll.current_user_responses.choice%2Cpoll.current_user_responses.poll%2Caccess_rules.tier.null%2Cimages.null%2Caudio.null&fields[post]=change_visibility_at%2Ccomment_count%2Ccontent%2Ccurrent_user_can_delete%2Ccurrent_user_can_view%2Ccurrent_user_has_liked%2Cembed%2Cimage%2Cis_paid%2Clike_count%2Cmin_cents_pledged_to_view%2Cpost_file%2Cpost_metadata%2Cpublished_at%2Cpatron_count%2Cpatreon_url%2Cpost_type%2Cpledge_url%2Cthumbnail_url%2Cteaser_text%2Ctitle%2Cupgrade_url%2Curl%2Cwas_posted_by_campaign_owner&fields[user]=image_url%2Cfull_name%2Curl&fields[campaign]=show_audio_post_download_links%2Cavatar_photo_url%2Cearnings_visibility%2Cis_nsfw%2Cis_monthly%2Cname%2Curl&fields[access_rule]=access_rule_type%2Camount_cents&fields[media]=id%2Cimage_urls%2Cdownload_url%2Cmetadata%2Cfile_name&fields[post]=change_visibility_at%2Ccomment_count%2Ccontent%2Ccurrent_user_can_delete%2Ccurrent_user_can_view%2Ccurrent_user_has_liked%2Cembed%2Cimage%2Cis_paid%2Clike_count%2Cmin_cents_pledged_to_view%2Cpost_file%2Cpost_metadata%2Cpublished_at%2Cpatron_count%2Cpatreon_url%2Cpost_type%2Cpledge_url%2Cthumbnail_url%2Cteaser_text%2Ctitle%2Cupgrade_url%2Curl%2Cwas_posted_by_campaign_owner&fields[user]=image_url%2Cfull_name%2Curl&fields[campaign]=show_audio_post_download_links%2Cavatar_photo_url%2Cearnings_visibility%2Cis_nsfw%2Cis_monthly%2Cname%2Curl&fields[access_rule]=access_rule_type%2Camount_cents&fields[media]=id%2Cimage_urls%2Cdownload_url%2Cmetadata%2Cfile_name&sort=-published_at&filter[campaign_id]={_campaignInfo.Id}&filter[is_draft]=false&filter[contains_exclusive_posts]=true&json-api-use-default-includes=false&json-api-version=1.0";
            CookieParam[] browserCookies = null;

            while (!string.IsNullOrEmpty(nextPage))
            {
                _logger.Debug($"New page");
                Response response = await page.GoToAsync(nextPage);

                if (browserCookies == null)
                {
                    _logger.Debug("Retrieving cookies");
                    browserCookies = await page.GetCookiesAsync();
                }

                string json = await response.TextAsync();

                ParsingResult result = await ParsePage(json);

                if(result.Entries.Count > 0)
                    galleryEntries.AddRange(result.Entries);

                nextPage = result.NextPage;

                await Task.Delay(500 * rnd.Next(1, 3)); //0.5 - 1 second delay
            }

            _logger.Debug($"Closing page");
            await page.CloseAsync();

            _logger.Info($"Starting download for #{_campaignInfo.Name}");

            _logger.Debug("Filling cookies");
            CookieContainer cookieContainer = new CookieContainer();

            //TODO: Check that all required cookies were extracted
            if (browserCookies != null && browserCookies.Length > 0)
            {
                foreach (CookieParam browserCookie in browserCookies)
                {
                    _logger.Debug($"Adding cookie: {browserCookie.Name}");
                    Cookie cookie = new Cookie(browserCookie.Name, browserCookie.Value, browserCookie.Path, browserCookie.Domain);
                    cookieContainer.Add(cookie);
                }
            }
            else
            {
                _logger.Fatal("No cookies were extracted from browser, unable to proceed");
                return;
            }

            FileDownloader fileDownloader = new FileDownloader(cookieContainer);
            string downloadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "download", campaignInfo.Name);
            if (!Directory.Exists(downloadDirectory))
            {
                Directory.CreateDirectory(downloadDirectory);
            }

            //TODO: Download avatar and cover
            _logger.Info("Downloading avatar and cover");
            _logger.Debug("Retrieving avatar extension...");
            string avatarExt = await RetrieveRemoteFileName(campaignInfo.AvatarUrl);
            avatarExt = Path.GetExtension(avatarExt);

            _logger.Debug("Retrieving cover extension...");
            string coverExt = await RetrieveRemoteFileName(campaignInfo.CoverUrl);
            coverExt = Path.GetExtension(coverExt);

            _logger.Debug("Downloading avatar...");
            await fileDownloader.DownloadFile(campaignInfo.AvatarUrl, Path.Combine(downloadDirectory, $"avatar{avatarExt}"));

            _logger.Debug("Downloading cover...");
            await fileDownloader.DownloadFile(campaignInfo.CoverUrl, Path.Combine(downloadDirectory, $"cover{coverExt}"));

            for (int i = 0; i < galleryEntries.Count; i++)
            {
                GalleryEntry entry = galleryEntries[i];
                _logger.Info($"Downloading {i+1}/{galleryEntries.Count}: {entry.DownloadUrl}");
                try
                {
                    await File.WriteAllTextAsync(Path.Combine(downloadDirectory, $"{entry.Path}_desc.txt"),
                        entry.Description);
                }
                catch (Exception ex)
                {
                    _logger.Error($"Unable to write description for {entry.Path}: {ex}");
                }

                await fileDownloader.DownloadFile(entry.DownloadUrl, Path.Combine(downloadDirectory, entry.Path));
            }

            _logger.Debug($"Finished crawl");
        }

        private async Task<string> RetrieveRemoteFileName(string url)
        {
            try
            {
                HttpClient httpClient = new HttpClient();
                var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

                if (response.Content.Headers.ContentDisposition?.FileName != null)
                    return response.Content.Headers.ContentDisposition.FileName.Replace("\"","");
            }
            catch (HttpRequestException ex)
            {
                _logger.Error($"HttpRequestException while trying to retrieve remote file name: {ex}");
            }

            string[] urlSplit = url.Split('?')[0].Split('/'); //TODO: replace with regex
            string filename = urlSplit[urlSplit.Length - 1];

            _logger.Debug($"Content-Disposition failed, fallback to url extraction, extracted name: {filename}");
            return filename;
        }

        private async Task<ParsingResult> ParsePage(string json)
        {
            List<GalleryEntry> galleryEntries = new List<GalleryEntry>();

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
                    continue;
                }

                GalleryEntry entry = new GalleryEntry
                {
                    Description = jsonEntry.Attributes.Content,
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
                doc.LoadHtml(entry.Description);
                HtmlNodeCollection imgNodeCollection = doc.DocumentNode.SelectNodes("//img");
                if (imgNodeCollection != null)
                {
                    int cnt = 1;
                    foreach (var imgNode in imgNodeCollection)
                    {
                        GalleryEntry subEntry = entry;
                        subEntry.DownloadUrl = imgNode.Attributes["src"].Value;
                        subEntry.Path = $"{jsonEntry.Id}_extimg_{ await RetrieveRemoteFileName(subEntry.DownloadUrl) }"; // RetrieveRemoteFileName?
                        galleryEntries.Add(subEntry);
                        _logger.Info($"[{jsonEntry.Id}] New external (image) entry: {subEntry.Path} from {subEntry.DownloadUrl}");
                        cnt++;
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

                                GalleryEntry subEntry = entry;
                                subEntry.DownloadUrl = url;
                                subEntry.Path = $"{jsonEntry.Id}_extdropbox_{ await RetrieveRemoteFileName(url) }";
                                galleryEntries.Add(subEntry);
                                _logger.Info($"[{jsonEntry.Id}] New external (dropbox) entry: {subEntry.Path} from {subEntry.DownloadUrl}");
                            }
                            else if (url.IndexOf("drive.google.com/file/d/", StringComparison.Ordinal) != -1)
                            {
                                //TODO: GOOGLE DRIVE SUPPORT
                                _logger.Fatal($"[{jsonEntry.Id}] [NOT SUPPORTED] google drive link found for: {url}");
                            }
                            else if (url.StartsWith("https://mega.nz/"))
                            {
                                //TODO: MEGA SUPPORT
                                _logger.Fatal($"[{jsonEntry.Id}] [NOT SUPPORTED] MEGA link found for: {url}");
                            }
                            else
                            {
                                _logger.Warn($"[{jsonEntry.Id}] Unknown provider link found for, assuming it's direct url: {url}");

                                GalleryEntry subEntry = entry;
                                subEntry.DownloadUrl = url;
                                subEntry.Path = $"{jsonEntry.Id}_extfile_{ await RetrieveRemoteFileName(url) }";
                                galleryEntries.Add(subEntry);
                                _logger.Info($"[{jsonEntry.Id}] New external (file) entry: {subEntry.Path} from {subEntry.DownloadUrl}");
                            }
                        }
                        else
                        {
                            _logger.Warn($"[{jsonEntry.Id}] link with invalid href for {jsonEntry.Id}");
                        }
                    }
                }

                //Attachments
                if(jsonEntry.Relationships.Attachments?.Data != null)
                {
                    foreach (var attachment in jsonEntry.Relationships.Attachments.Data)
                    {
                        if (attachment.Type != "attachment") //sanity check 
                        {
                            _logger.Fatal($"[{jsonEntry.Id}] invalid attachment type for {attachment.Id}!!!");
                            continue;
                        }

                        var attachmentData =
                            jsonRoot.Included
                                .FirstOrDefault(x => x.Type == "attachment" && x.Id == attachment.Id);

                        if (attachmentData == null)
                        {
                            _logger.Fatal($"[{jsonEntry.Id}] attachment data not found for {attachment.Id}!!!");
                            continue;
                        }

                        GalleryEntry subEntry = entry;
                        subEntry.DownloadUrl = attachmentData.Attributes.Url;
                        subEntry.Path = $"{jsonEntry.Id}_attachment_{attachmentData.Attributes.Name}";
                        galleryEntries.Add(subEntry);
                        _logger.Info($"[{jsonEntry.Id}] New attachment entry: {subEntry.Path} from {subEntry.DownloadUrl}");
                    }
                }

                //Media
                if (jsonEntry.Relationships.Images?.Data != null)
                {
                    foreach (var image in jsonEntry.Relationships.Images.Data)
                    {
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

                        GalleryEntry subEntry = entry;
                        subEntry.DownloadUrl = imageData.Attributes.DownloadUrl;
                        subEntry.Path = $"{jsonEntry.Id}_media_{imageData.Attributes.FileName}";
                        galleryEntries.Add(subEntry);
                        _logger.Info($"[{jsonEntry.Id}] New media entry: {subEntry.Path} from {subEntry.DownloadUrl}");
                    }
                }

                //Now parse the entry itself
                if (jsonEntry.Attributes.PostFile != null)
                {
                    entry.DownloadUrl = jsonEntry.Attributes.PostFile.Url;
                    entry.Path = $"{jsonEntry.Id}_post_{jsonEntry.Attributes.PostFile.Name}";
                }
                else
                {
                    if (jsonEntry.Attributes.Image != null)
                    {
                        if (jsonEntry.Attributes.Image.LargeUrl != null)
                        {
                            entry.DownloadUrl = jsonEntry.Attributes.Image.LargeUrl;
                            entry.Path = $"{jsonEntry.Id}_post_{ await RetrieveRemoteFileName(jsonEntry.Attributes.Image.LargeUrl) }";
                        }
                        else if (jsonEntry.Attributes.Image.Url != null)
                        {
                            entry.DownloadUrl = jsonEntry.Attributes.Image.Url;
                            entry.Path = $"{jsonEntry.Id}_post_{ await RetrieveRemoteFileName(jsonEntry.Attributes.Image.Url) }";
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
                    if (!galleryEntries.Any(x => x.DownloadUrl == jsonEntry.Attributes.Url))
                    {
                        _logger.Fatal($"[{jsonEntry.Id}] Was not parsed!");
                        continue;
                    }
                }

                if (jsonEntry.Type == "media")
                {
                    if (!galleryEntries.Any(x => x.DownloadUrl == jsonEntry.Attributes.DownloadUrl))
                    {
                        _logger.Fatal($"[{jsonEntry.Id}] Was not parsed!");
                        continue;
                    }
                }

                _logger.Info($"[{jsonEntry.Id}] OK");
            }

            return new ParsingResult {Entries = galleryEntries, NextPage = jsonRoot.Links?.Next};
        }
    }
}
