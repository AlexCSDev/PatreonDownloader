# PatreonDownloader
This application is designed for downloading content posted by creators on patreon.com. It is only able to download content which your account has access to.

## Usage
#### Login into your account (will open patreon login page in embedded chrome browser):
PatreonDownloader.exe -login
#### Download all available files from creator:
PatreonDownloader.exe -download=https://www.patreon.com/#####/posts
#### Show available commands and their description
PatreonDownloader.exe -help

## Supported features
* Downloading files from posts
* Downloading files from attachments
* Saving html contents of posts
* External links extraction from post
	* Limited/dumb direct link support (any url that is not parsed by specific parsers and ends with a valid extension is considered a valid url to download)
	* Dropbox support
	* Blacklist (configured in settings.json)
	
## Needs further testing
* Gallery posts

## Known not implemented or not tested features 
* Embedded data (Embed attribute)
* Audio files
* Vimeo embedded videos
* Google drive external links
* Mega.nz external links
* YouTube external links
* imgur external links
* Running under operating systems other than Windows is not tested