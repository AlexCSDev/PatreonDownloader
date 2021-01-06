using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CG.Web.MegaApiClient;
using NLog;
using NLog.Fluent;
using PatreonDownloader.Interfaces.Models;

namespace PatreonDownloader.MegaDownloader
{
    internal enum MegaDownloadResult
    {
        Unknown,
        Failed,
        FileExists,
        Success
    }

    internal class MegaFolder
    {
        public string Name;
        public string ParentId;
        public string Path;
    }

    internal class MegaCredentials
    {
        public string Email;
        public string Password;

        public MegaCredentials(string email, string password)
        {
            Email = email;
            Password = password;
        }
    }

    internal class MegaDownloader : IDisposable
    {
        private MegaApiClient _client;

        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public MegaDownloader(MegaCredentials credentials = null)
        {
            _client = new MegaApiClient();
            if (credentials != null)
            {
                _client.Login(credentials.Email, credentials.Password);
            }
            else
            {
                _client.LoginAnonymous();
            }
        }

        public MegaDownloadResult DownloadUrl(CrawledUrl crawledUrl, string downloadPath, bool overwriteFiles = false)
        {
            var folders = new List<KeyValuePair<string, MegaFolder>>();
            Uri uri = new Uri(crawledUrl.Url);
            INode[] nodes = null;
            INodeInfo fileNode = null;
            try
            {
                nodes = _client.GetNodesFromLink(uri).ToArray(); // folder
            }
            catch (Exception ex) // not a folder
            {
                _logger.Debug($"[MEGA] Exception in nodes, probably not a folder: {crawledUrl.Url} - {ex}");
                fileNode = _client.GetNodeFromLink(uri);
            }
            if (nodes != null)
            {
                foreach (INode node in nodes)
                {
                    if (folders.Any(x => x.Key == node.Id) || node.Type == NodeType.File)
                    {
                        continue;
                    }

                    folders.Add(new KeyValuePair<string, MegaFolder>(node.Id,
                        new MegaFolder { Name = node.Name, ParentId = node.ParentId }));
                }

                foreach (var folder in folders)
                {
                    var path = folder.Value.Name;
                    var keyPath = folder.Key;
                    var parentId = folder.Value.ParentId;
                    while (parentId != null)
                    {
                        var parentFolder = folders.FirstOrDefault(x => x.Key == parentId);
                        path = parentFolder.Value.Name + "/" + path;
                        keyPath = parentFolder.Key + "/" + keyPath;
                        parentId = parentFolder.Value.ParentId;
                    }

                    folder.Value.Path = path;
                }

                var rootFolder = folders.FirstOrDefault(x => string.IsNullOrEmpty(x.Value.ParentId));

                foreach (INode node in nodes.Where(x => x.Type == NodeType.File))
                {
                    var path = Path.Combine(
                        downloadPath,
                        $"{crawledUrl.PostId}_{rootFolder.Key.Substring(0, 5)}_mega_{folders.FirstOrDefault(x => x.Key == node.ParentId).Value.Path}",
                        node.Name);
                    _logger.Info($"[MEGA] Downloading {path}");

                    path = Path.Combine(Environment.CurrentDirectory, path);
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(new FileInfo(path).DirectoryName);
                    }

                    if (File.Exists(path))
                    {
                        if (!overwriteFiles)
                        {
                            _logger.Warn($"[MEGA] FILE EXISTS: {crawledUrl.Url} - {path}");
                            continue;
                        }
                        else
                        {
                            File.Delete(path);
                        }
                    }

                    _client.DownloadFile(node, path);
                }
            }
            else
            {
                var path = Path.Combine(downloadPath, $"{crawledUrl.PostId}_{fileNode.Id.Substring(0, 5)}_mega_{fileNode.Name}");
                _logger.Debug($"[MEGA] Downloading {fileNode.Name} to {path}");

                if (File.Exists(path))
                {
                    if (!overwriteFiles)
                    {
                        _logger.Warn($"[MEGA] FILE EXISTS: {crawledUrl.Url} - {path}");
                        return MegaDownloadResult.FileExists;
                    }
                    else
                    {
                        File.Delete(path);
                    }
                }
                _client.DownloadFile(uri, path);

            }

            _logger.Debug($"[MEGA] Finished downloading {crawledUrl.Url}");
            return MegaDownloadResult.Success;
        }

        ~MegaDownloader()
        {
            ReleaseUnmanagedResources();
        }

        private void ReleaseUnmanagedResources()
        {
            _client.Logout();
            _client = null;
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }
    }
}
