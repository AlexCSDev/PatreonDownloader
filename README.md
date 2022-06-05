
# PatreonDownloader
This application is designed for downloading content posted by creators on patreon.com. 

IMPORTANT: You need a valid patreon account to download both free and paid content. Paid content will only be downloaded if you have an active subscription to creator's page.

## Usage
#### Download all available files from creator
PatreonDownloader.App.exe --url #page url#. Page url should follow one of the following patterns:
* https://www.patreon.com/m/#numbers#/posts
* https://www.patreon.com/user?u=#numbers#
* https://www.patreon.com/user/posts?u=#numbers#
* https://www.patreon.com/#creator_name#/posts
#### Download all available files from creator into custom directory and save all possible data (post contents, embed metadata, cover and avatar, json responses)
PatreonDownloader.App.exe --url #page url# --download-directory c:\downloads --descriptions --embeds --campaign-images --json
#### Show available commands and their descriptions
PatreonDownloader.App.exe --help

## System requirements
Due to Cloudflare protection triggering on all connections with TLS version lower than 1.3 the application will only work on the following systems:
* Windows 10 1903 and newer
* Linux and other systems with OpenSSL 1.1.1 and newer

## Build instructions
See docs\BUILDING.md

## Supported features
* Tested under Windows and Linux. Should work on any platform supported by .NET Core and Chromium browser.
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
	* PatreonDownloader comes with the following plugins by default: Google Drive, Mega.nz
	
## Needs further testing
* Gallery posts

## Known not implemented or not tested features 
* Audio files
* Vimeo embedded videos
* YouTube external links
* imgur external links

## License
All files in this repository are licensed under the license listed in LICENSE.md file unless stated otherwise.

## Special thanks
We would like to say special thanks to [JetBrains](https://jb.gg/OpenSource) for providing software licenses to our core contributors as a part of their Open Source Support Program.
