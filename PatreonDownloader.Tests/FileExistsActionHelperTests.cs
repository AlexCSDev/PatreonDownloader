using System;
using System.IO;
using UniversalDownloaderPlatform.Common.Helpers;
using Xunit;

namespace PatreonDownloader.Tests
{
    public class FileExistsActionHelperTests : IDisposable
    {
        private readonly string _tempDirectory;

        public FileExistsActionHelperTests()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), $"PatreonDownloader.Tests_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDirectory);
        }

        [Fact]
        public void ResolveExistingFilePath_ExactPathExists_ReturnsExactPath()
        {
            string expectedPath = Path.Combine(_tempDirectory, "media_123.png");
            File.WriteAllText(expectedPath, "test");

            string resolvedPath = FileExistsActionHelper.ResolveExistingFilePath(expectedPath);

            Assert.Equal(expectedPath, resolvedPath);
        }

        [Fact]
        public void ResolveExistingFilePath_MatchingBasenameWithDifferentExtensionExists_ReturnsConvertedFilePath()
        {
            string requestedPath = Path.Combine(_tempDirectory, "media_123.png");
            string convertedPath = Path.Combine(_tempDirectory, "media_123.mp3");
            File.WriteAllText(convertedPath, "test");

            string resolvedPath = FileExistsActionHelper.ResolveExistingFilePath(requestedPath);

            Assert.Equal(convertedPath, resolvedPath);
        }

        [Fact]
        public void ResolveExistingFilePath_PartialNameMatchOnly_ReturnsNull()
        {
            string requestedPath = Path.Combine(_tempDirectory, "media_123.png");
            File.WriteAllText(Path.Combine(_tempDirectory, "media_123_extra.mp3"), "test");

            string resolvedPath = FileExistsActionHelper.ResolveExistingFilePath(requestedPath);

            Assert.Null(resolvedPath);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDirectory))
                Directory.Delete(_tempDirectory, true);
        }
    }
}
