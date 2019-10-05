# PatreonDownloader
This application is designed for downloading content posted by creators on patreon.com. It is only able to download content which your account has access to.

## Usage
#### Login into your account:
dotnet PatreonDownloader -login
#### Download all creator's page:
dotnet PatreonDownloader.dll -download=https://www.patreon.com/#####/posts

## Supported features
* Downloading images from posts
* Downloading images from attachments
* External links extraction from post
	* Limited/dumb direct link support (any url that is not parsed by specific parsers is considered a valid url to download)
	* Dropbox support

## Known not implemented or not tested features 
* Embedded data (Embed attribute)
* Audio files
* Vimeo embedded videos
* Google drive external links
* Mega.nz external links
