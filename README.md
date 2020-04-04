# PatreonDownloader
This application is designed for downloading content posted by creators on patreon.com. 

IMPORTANT: You need a valid patreon account to download both free and paid content. Paid content will only be downloaded if you have an active subscription to creator's page.

## Usage
#### Download all available files from creator
PatreonDownloader.exe --creator #creatorname#. Creator name can be obtained by looking at their page url: https://www.patreon.com/#creator_name_here#/posts
#### Download all available files from creator into custom directory and save all possible data (post contents, embed metadata, cover and avatar, json responses)
PatreonDownloader.exe --creator #creatorname# --download-directory c:\downloads --descriptions --embeds --campaign-images --json
#### Show available commands and their descriptions
PatreonDownloader.exe --help

## Build instructions
See BUILDING.md

## Supported features
* Downloading files from posts
* Downloading files from attachments
* Saving html contents of posts
* Saving metadata of embedded content
* Saving api responses (mostly for troubleshooting purposes)
* External links extraction from post
	* C# plugin support (see below)
	* Limited/dumb direct link support (PatreonDownloader will attempt to download any file with valid extension if no suitable plugin is installed)
	* Dropbox support
	* Blacklist (configured in settings.json)
* Plugins (via C#)
	* Custom downloaders for adding download support for websites which need custom download logic
	* PatreonDownloader comes with the following plugins by default: Google Drive
	
## Needs further testing
* Gallery posts

## Known not implemented or not tested features 
* Audio files
* Vimeo embedded videos
* Mega.nz external links
* YouTube external links
* imgur external links
* Running under operating systems other than Windows is not tested

## License
All files in this repository are licensed under the license listed in LICENSE.md file unless stated otherwise.