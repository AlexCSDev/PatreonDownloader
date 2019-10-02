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
using PuppeteerSharp;

namespace PatreonDownloader
{
    struct ParsingResult
    {
        public List<GalleryEntry> entries;
        public string next;
    }
    class PageCrawler
    {
        private Browser _browser;
        private CampaignInfoRetriever _campaignInfoRetriever;

        private string _campaignName;
        public PageCrawler(Browser browser)
        {
            _browser = browser;
            _campaignInfoRetriever = new CampaignInfoRetriever(browser);
        }

        public async Task Crawl(long campaignId)
        {
            Log.Instance.Info($"Starting crawling campaign #{campaignId}");
            Gallery gallery = new Gallery {Entries = new List<GalleryEntry>(), Name = "#PLACEHOLDER#"};

            Random rnd = new Random(Guid.NewGuid().GetHashCode());

            Log.Instance.Debug($"Retrieving campaign info");
            CampaignInfo campaignInfo = await _campaignInfoRetriever.RetrieveCampaignInfo(campaignId);

            Log.Instance.Info($"Campaign name: {campaignInfo.Name}");
            _campaignName = campaignInfo.Name;
            gallery.Name = campaignInfo.Name;

            var page = await _browser.NewPageAsync();
            string nextPage = $"https://www.patreon.com/api/posts?include=user%2Cattachments%2Cuser_defined_tags%2Ccampaign%2Cpoll.choices%2Cpoll.current_user_responses.user%2Cpoll.current_user_responses.choice%2Cpoll.current_user_responses.poll%2Caccess_rules.tier.null%2Cimages.null%2Caudio.null&fields[post]=change_visibility_at%2Ccomment_count%2Ccontent%2Ccurrent_user_can_delete%2Ccurrent_user_can_view%2Ccurrent_user_has_liked%2Cembed%2Cimage%2Cis_paid%2Clike_count%2Cmin_cents_pledged_to_view%2Cpost_file%2Cpost_metadata%2Cpublished_at%2Cpatron_count%2Cpatreon_url%2Cpost_type%2Cpledge_url%2Cthumbnail_url%2Cteaser_text%2Ctitle%2Cupgrade_url%2Curl%2Cwas_posted_by_campaign_owner&fields[user]=image_url%2Cfull_name%2Curl&fields[campaign]=show_audio_post_download_links%2Cavatar_photo_url%2Cearnings_visibility%2Cis_nsfw%2Cis_monthly%2Cname%2Curl&fields[access_rule]=access_rule_type%2Camount_cents&fields[media]=id%2Cimage_urls%2Cdownload_url%2Cmetadata%2Cfile_name&fields[post]=change_visibility_at%2Ccomment_count%2Ccontent%2Ccurrent_user_can_delete%2Ccurrent_user_can_view%2Ccurrent_user_has_liked%2Cembed%2Cimage%2Cis_paid%2Clike_count%2Cmin_cents_pledged_to_view%2Cpost_file%2Cpost_metadata%2Cpublished_at%2Cpatron_count%2Cpatreon_url%2Cpost_type%2Cpledge_url%2Cthumbnail_url%2Cteaser_text%2Ctitle%2Cupgrade_url%2Curl%2Cwas_posted_by_campaign_owner&fields[user]=image_url%2Cfull_name%2Curl&fields[campaign]=show_audio_post_download_links%2Cavatar_photo_url%2Cearnings_visibility%2Cis_nsfw%2Cis_monthly%2Cname%2Curl&fields[access_rule]=access_rule_type%2Camount_cents&fields[media]=id%2Cimage_urls%2Cdownload_url%2Cmetadata%2Cfile_name&sort=-published_at&filter[campaign_id]={campaignId}&filter[is_draft]=false&filter[contains_exclusive_posts]=true&json-api-use-default-includes=false&json-api-version=1.0";
            CookieParam[] browserCookies = null;

            while (!string.IsNullOrEmpty(nextPage))
            {
                Log.Instance.Debug($"New page");
                Response response = await page.GoToAsync(nextPage);

                if (browserCookies == null)
                {
                    Log.Instance.Debug("Retrieving cookies");
                    browserCookies = await page.GetCookiesAsync();
                }

                string json = await response.TextAsync();

                ParsingResult result = await ParsePage(json);

                if(result.entries.Count > 0)
                    gallery.Entries.AddRange(result.entries);

                nextPage = result.next;

                await Task.Delay(500 * rnd.Next(1, 3)); //0.5 - 1 second delay
            }

            Log.Instance.Info($"Starting download for #{campaignId}");

            Log.Instance.Debug("Filling cookies");
            CookieContainer cookieContainer = new CookieContainer();
            foreach(CookieParam browserCookie in browserCookies)
            {
                Log.Instance.Debug($"Adding cookie: {browserCookie.Name}");
                Cookie cookie = new Cookie(browserCookie.Name, browserCookie.Value, browserCookie.Path, browserCookie.Domain);
                cookieContainer.Add(cookie);
            }

            FileDownloader fileDownloader = new FileDownloader(cookieContainer);
            string downloadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "download", _campaignName);
            if (!Directory.Exists(downloadDirectory))
            {
                Directory.CreateDirectory(downloadDirectory);
            }
            //TODO: Save description for each file
            //TODO: Output progress (x/x files)
            //TODO: Download avatar and cover
            foreach (var entry in gallery.Entries)
            {
                Log.Instance.Info($"Downloading {entry.DownloadUrl}");
                await fileDownloader.DownloadFile(entry.DownloadUrl, Path.Combine(downloadDirectory, entry.Path));
            }

            Log.Instance.Debug($"Done");
            //DOWNLOAD
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
                Log.Instance.Error($"HttpRequestException while trying to retrieve remote file name: {ex}");
            }

            string[] urlSplit = url.Split('?')[0].Split('/'); //TODO: replace with regex
            string filename = urlSplit[urlSplit.Length - 1];

            Log.Instance.Debug($"Content-Disposition failed, fallback to url extraction, extracted name: {filename}");
            return filename;
        }

        private async Task<ParsingResult> ParsePage(string json)
        {
            List<GalleryEntry> galleryEntries = new List<GalleryEntry>();

            RootObject jsonRoot = JsonConvert.DeserializeObject<RootObject>(json);

            Log.Instance.Info("Parsing data entries...");
            foreach (var jsonEntry in jsonRoot.data)
            {
                Log.Instance.Info($"Entry {jsonEntry.id}");
                if (jsonEntry.type != "post")
                {
                    Log.Instance.Error($"[{jsonEntry.id}] Invalid type for \"data\": {jsonEntry.type}, skipping");
                    continue;
                }

                Log.Instance.Info($"[{jsonEntry.id}] Is a post");
                if (!jsonEntry.attributes.current_user_can_view)
                {
                    Log.Instance.Warn($"[{jsonEntry.id}] Current user cannot view selected post");
                    continue;
                }

                GalleryEntry entry = new GalleryEntry();
                entry.Description = jsonEntry.attributes.content;
                entry.Author = _campaignName;
                entry.PageUrl = jsonEntry.attributes.url;
                entry.Name = jsonEntry.attributes.title;

                //TODO: EMBED SUPPORT
                if (jsonEntry.attributes.embed != null)
                {
                    Log.Instance.Fatal($"[{jsonEntry.id}] NOT IMPLEMENTED: POST EMBED");
                }

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(entry.Description);
                HtmlNodeCollection imgNodeCollection = doc.DocumentNode.SelectNodes("//img");
                if (imgNodeCollection != null)
                {
                    int cnt = 1;
                    foreach (var imgNode in imgNodeCollection)
                    {
                        GalleryEntry subEntry = (GalleryEntry)entry.Clone();
                        subEntry.DownloadUrl = imgNode.Attributes["src"].Value;
                        subEntry.Path = $"{jsonEntry.id}_extimg_{ await RetrieveRemoteFileName(subEntry.DownloadUrl) }"; // RetrieveRemoteFileName?
                        galleryEntries.Add(subEntry);
                        Log.Instance.Info($"[{jsonEntry.id}] New external (image) entry: {subEntry.Path} from {subEntry.DownloadUrl}");
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

                                GalleryEntry subEntry = (GalleryEntry)entry.Clone();
                                subEntry.DownloadUrl = url;
                                subEntry.Path = $"{jsonEntry.id}_extdropbox_{ await RetrieveRemoteFileName(url) }";
                                galleryEntries.Add(subEntry);
                                Log.Instance.Info($"[{jsonEntry.id}] New external (dropbox) entry: {subEntry.Path} from {subEntry.DownloadUrl}");
                            }
                            else if (url.IndexOf("drive.google.com/file/d/", StringComparison.Ordinal) != -1)
                            {
                                //TODO: GOOGLE DRIVE SUPPORT
                                Log.Instance.Fatal($"[{jsonEntry.id}] [NOT SUPPORTED] google drive link found for: {url}");
                            }
                            else if (url.StartsWith("https://mega.nz/"))
                            {
                                //TODO: MEGA SUPPORT
                                Log.Instance.Fatal($"[{jsonEntry.id}] [NOT SUPPORTED] MEGA link found for: {url}");
                            }
                            else
                            {
                                Log.Instance.Warn($"[{jsonEntry.id}] Unknown provider link found for, assuming it's direct url: {url}"); //pics.Add($"{RetrieveRemoteFileName(url)}:{url}");

                                GalleryEntry subEntry = (GalleryEntry)entry.Clone();
                                subEntry.DownloadUrl = url;
                                subEntry.Path = $"{jsonEntry.id}_extfile_{ await RetrieveRemoteFileName(url) }"; // RetrieveRemoteFileName?
                                galleryEntries.Add(subEntry);
                                Log.Instance.Info($"[{jsonEntry.id}] New external (file) entry: {subEntry.Path} from {subEntry.DownloadUrl}");
                                //cnt++;
                            }
                        }
                        else
                        {
                            Log.Instance.Warn($"[{jsonEntry.id}] link with invalid href for {jsonEntry.id}");
                        }
                    }
                }

                //Attachments
                if(jsonEntry.relationships.attachments?.data != null)
                {
                    foreach (var attachment in jsonEntry.relationships.attachments.data)
                    {
                        if (attachment.type != "attachment") //sanity check 
                        {
                            Log.Instance.Fatal($"[{jsonEntry.id}] invalid attachment type for {attachment.id}!!!");
                            continue;
                        }

                        var attachmentData =
                            jsonRoot.included
                                .FirstOrDefault(x => x.type == "attachment" && x.id == attachment.id);

                        if (attachmentData == null)
                        {
                            Log.Instance.Fatal($"[{jsonEntry.id}] attachment data not found for {attachment.id}!!!");
                            continue;
                        }

                        GalleryEntry subEntry = (GalleryEntry)entry.Clone();
                        subEntry.DownloadUrl = attachmentData.attributes.url;
                        subEntry.Path = $"{jsonEntry.id}_attachment_{attachmentData.attributes.name}";
                        galleryEntries.Add(subEntry);
                        Log.Instance.Info($"[{jsonEntry.id}] New attachment entry: {subEntry.Path} from {subEntry.DownloadUrl}");
                    }
                }

                //Media
                if (jsonEntry.relationships.images?.data != null)
                {
                    foreach (var image in jsonEntry.relationships.images.data)
                    {
                        if (image.type != "media") //sanity check 
                        {
                            Log.Instance.Fatal($"[{jsonEntry.id}] invalid media type for {image.id}!!!");
                            continue;
                        }

                        var imageData =
                            jsonRoot.included
                                .FirstOrDefault(x => x.type == "media" && x.id == image.id);

                        if (imageData == null)
                        {
                            Log.Instance.Fatal($"[{jsonEntry.id}] media data not found for {image.id}!!!");
                            continue;
                        }

                        GalleryEntry subEntry = (GalleryEntry)entry.Clone();
                        subEntry.DownloadUrl = imageData.attributes.download_url;
                        subEntry.Path = $"{jsonEntry.id}_media_{imageData.attributes.file_name}";
                        galleryEntries.Add(subEntry);
                        Log.Instance.Info($"[{jsonEntry.id}] New media entry: {subEntry.Path} from {subEntry.DownloadUrl}");
                    }
                }

                //Now parse the entry itself
                if (jsonEntry.attributes.post_file != null)
                {
                    entry.DownloadUrl = jsonEntry.attributes.post_file.url;
                    entry.Path = $"{jsonEntry.id}_post_{jsonEntry.attributes.post_file.name}";
                }
                else
                {
                    if (jsonEntry.attributes.image != null)
                    {
                        if (jsonEntry.attributes.image.large_url != null)
                        {
                            entry.DownloadUrl = jsonEntry.attributes.image.large_url;
                            entry.Path = $"{jsonEntry.id}_post_{ await RetrieveRemoteFileName(jsonEntry.attributes.image.large_url) }";
                        }
                        else if (jsonEntry.attributes.image.url != null)
                        {
                            entry.DownloadUrl = jsonEntry.attributes.image.url;
                            entry.Path = $"{jsonEntry.id}_post_{ await RetrieveRemoteFileName(jsonEntry.attributes.image.url) }";
                        }
                    }
                }

                if (!string.IsNullOrEmpty(entry.DownloadUrl) && !string.IsNullOrEmpty(entry.Path))
                {
                    Log.Instance.Info($"[{jsonEntry.id}] New entry: {entry.Path} from {entry.DownloadUrl}");
                    galleryEntries.Add(entry);
                }
                else
                {
                    Log.Instance.Warn($"[{jsonEntry.id}] Post entry doesn't have required values: {(entry.DownloadUrl != null ? entry.DownloadUrl : "no download url")}  {(entry.Path != null ? entry.Path : "no path")}");
                }
            }

            Log.Instance.Info("Checking if all included entries were added...");
            foreach (var jsonEntry in jsonRoot.included)
            {
                Log.Instance.Info($"Entry {jsonEntry.id}");
                if (jsonEntry.type != "attachment" && jsonEntry.type != "media")
                {
                    Log.Instance.Error($"[{jsonEntry.id}] Invalid type for \"included\": {jsonEntry.type}, skipping");
                    continue;
                }

                Log.Instance.Info($"[{jsonEntry.id}] Is a {jsonEntry.type}");

                if (jsonEntry.type == "attachment")
                {
                    if (!galleryEntries.Any(x => x.DownloadUrl == jsonEntry.attributes.url))
                    {
                        Log.Instance.Fatal($"[{jsonEntry.id}] Was not parsed!");
                        continue;
                    }
                }

                if (jsonEntry.type == "media")
                {
                    if (!galleryEntries.Any(x => x.DownloadUrl == jsonEntry.attributes.download_url))
                    {
                        Log.Instance.Fatal($"[{jsonEntry.id}] Was not parsed!");
                        continue;
                    }
                }

                Log.Instance.Info($"[{jsonEntry.id}] OK");
            }

            return new ParsingResult {entries = galleryEntries, next = jsonRoot.links?.next};
        }
    }
}
