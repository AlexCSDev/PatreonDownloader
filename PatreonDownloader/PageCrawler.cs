using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;
using NLog;
using PuppeteerSharp;

namespace PatreonDownloader
{
    class ParsingResult
    {
        public List<GalleryEntry> entries;
        public string cursor;
    }
    class PageCrawler
    {
        private Browser _browser;
        public PageCrawler(Browser browser)
        {
            _browser = browser;
        }

        public async Task Crawl(long campaignId)
        {
            Log.Instance.Debug($"Starting crawling campaign #{campaignId}");
            var page = await _browser.NewPageAsync();
            string cursor = "";
            bool firstPage = true;
            while (!string.IsNullOrEmpty(cursor) || firstPage)
            {
                string pageUrl =
                    $"https://www.patreon.com/api/posts?include=user%2Cattachments%2Cuser_defined_tags%2Ccampaign%2Cpoll.choices%2Cpoll.current_user_responses.user%2Cpoll.current_user_responses.choice%2Cpoll.current_user_responses.poll%2Caccess_rules.tier.null%2Cimages.null%2Caudio.null&fields[post]=change_visibility_at%2Ccomment_count%2Ccontent%2Ccurrent_user_can_delete%2Ccurrent_user_can_view%2Ccurrent_user_has_liked%2Cembed%2Cimage%2Cis_paid%2Clike_count%2Cmin_cents_pledged_to_view%2Cpost_file%2Cpost_metadata%2Cpublished_at%2Cpatron_count%2Cpatreon_url%2Cpost_type%2Cpledge_url%2Cthumbnail_url%2Cteaser_text%2Ctitle%2Cupgrade_url%2Curl%2Cwas_posted_by_campaign_owner&fields[user]=image_url%2Cfull_name%2Curl&fields[campaign]=show_audio_post_download_links%2Cavatar_photo_url%2Cearnings_visibility%2Cis_nsfw%2Cis_monthly%2Cname%2Curl&fields[access_rule]=access_rule_type%2Camount_cents&fields[media]=id%2Cimage_urls%2Cdownload_url%2Cmetadata%2Cfile_name&fields[post]=change_visibility_at%2Ccomment_count%2Ccontent%2Ccurrent_user_can_delete%2Ccurrent_user_can_view%2Ccurrent_user_has_liked%2Cembed%2Cimage%2Cis_paid%2Clike_count%2Cmin_cents_pledged_to_view%2Cpost_file%2Cpost_metadata%2Cpublished_at%2Cpatron_count%2Cpatreon_url%2Cpost_type%2Cpledge_url%2Cthumbnail_url%2Cteaser_text%2Ctitle%2Cupgrade_url%2Curl%2Cwas_posted_by_campaign_owner&fields[user]=image_url%2Cfull_name%2Curl&fields[campaign]=show_audio_post_download_links%2Cavatar_photo_url%2Cearnings_visibility%2Cis_nsfw%2Cis_monthly%2Cname%2Curl&fields[access_rule]=access_rule_type%2Camount_cents&fields[media]=id%2Cimage_urls%2Cdownload_url%2Cmetadata%2Cfile_name&sort=-published_at&filter[campaign_id]={campaignId}&filter[is_draft]=false&filter[contains_exclusive_posts]=true&json-api-use-default-includes=false&json-api-version=1.0";
                if (!firstPage)
                {
                    pageUrl += $"&page[cursor]={cursor}";
                }

                Response response = await page.GoToAsync(pageUrl);
                string json = await response.TextAsync();

                ParsingResult result = await ParsePage(json);

                if (firstPage)
                    firstPage = false;
            }
        }

        private static string RetrieveRemoteFileName(string url)
        {
            WebRequest request = WebRequest.Create(url);
            using (WebResponse response = request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            {
                return new ContentDisposition(response.Headers["Content-Disposition"]).FileName;
            }
        }

        private async Task<ParsingResult> ParsePage(string json)
        {
            List<GalleryEntry> galleryEntries = new List<GalleryEntry>();

            RootObject jsonRoot = JsonConvert.DeserializeObject<RootObject>(json);

            foreach (var jsonEntry in jsonRoot.data)
            {
                Log.Instance.Info($"Entry {jsonEntry.id}");
                if (jsonEntry.type != "post")
                {
                    Log.Instance.Debug($"[{jsonEntry.id}] Invalid type for \"data\": {jsonEntry.type}, skipping");
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
                entry.Author = "TODO: NOT IMPLEMENTED";
                entry.PageUrl = jsonEntry.attributes.url;
                entry.Name = jsonEntry.attributes.title;

                //TODO: EMBED SUPPORT
                if (jsonEntry.attributes.embed != null)
                {
                    Log.Instance.Fatal($"[{jsonEntry.id}] NOT IMPLEMENTED: POST EMBED");
                }

                HtmlDocument doc = new HtmlDocument();
                HtmlNodeCollection imgNodeCollection = doc.DocumentNode.SelectNodes("//img");
                if (imgNodeCollection != null)
                {
                    int cnt = 1;
                    foreach (var imgNode in imgNodeCollection)
                    {
                        GalleryEntry subEntry = (GalleryEntry)entry.Clone();
                        subEntry.DownloadUrl = imgNode.Attributes["src"].Value;
                        subEntry.Path = jsonEntry.id + "_" + cnt; // RetrieveRemoteFileName?
                        galleryEntries.Add(subEntry);
                        Log.Instance.Info($"[{jsonEntry.id}] New entry: {entry.Path} from {entry.DownloadUrl}");
                        cnt++;
                    }
                }

                HtmlNodeCollection linkNodeCollection = doc.DocumentNode.SelectNodes("//a");
                if (linkNodeCollection != null)
                {
                    int cnt = 1;
                    foreach (var linkNode in linkNodeCollection)
                    {
                        if (linkNode.Attributes["src"] != null)
                        {
                            var url = linkNode.Attributes["src"].Value;
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
                                subEntry.Path = RetrieveRemoteFileName(url);
                                galleryEntries.Add(subEntry);
                                Log.Instance.Info($"[{jsonEntry.id}] New entry: {entry.Path} from {entry.DownloadUrl}");
                                cnt++;
                            }
                            else if (url.IndexOf("drive.google.com/file/d/", StringComparison.Ordinal) != -1)
                            {
                                //TODO: GOOGLE DRIVE SUPPORT
                                Log.Instance.Warn($"[{jsonEntry.id}] [NOT SUPPORTED] google drive link found for {jsonEntry.id}: {url}");
                                cnt++;
                            }
                            else
                                Log.Instance.Warn($"[{jsonEntry.id}] [NOT SUPPORTED] unknown provider link found for {jsonEntry.id}: {url}"); //pics.Add($"{RetrieveRemoteFileName(url)}:{url}");
                        }
                        else
                        {
                            Log.Instance.Warn($"[{jsonEntry.id}] link with invalid src for {jsonEntry.id}");
                        }
                    }
                }

                //Now parse the entry itself
                if (jsonEntry.attributes.post_file != null)
                {
                    entry.DownloadUrl = jsonEntry.attributes.post_file.url;
                    entry.Path = jsonEntry.attributes.post_file.name;
                }
                else
                {
                    if (jsonEntry.attributes.image != null)
                    {
                        if (jsonEntry.attributes.image.large_url != null)
                        {
                            entry.DownloadUrl = jsonEntry.attributes.image.large_url;
                            entry.Path = RetrieveRemoteFileName(jsonEntry.attributes.image.large_url);
                        }
                        else if (jsonEntry.attributes.image.url != null)
                        {
                            entry.DownloadUrl = jsonEntry.attributes.image.url;
                            entry.Path = RetrieveRemoteFileName(jsonEntry.attributes.image.url);
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
                    Log.Instance.Fatal($"[{jsonEntry.id}] Download url or path is invalid: {(entry.DownloadUrl != null ? entry.DownloadUrl : "download url")}  {(entry.Path != null ? entry.Path : "path")}");
                }
            }

            return null;
        }
    }
}
