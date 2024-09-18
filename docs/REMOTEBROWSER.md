# Using remote browser
**This feature is for advanced users only and no support is provided for it. All issues asking for help will be closed unless you can prove that there is an issue with PatreonDownloader itself.**

PatreonDownloader has support for using remote browser for situations when using local browser is not possible. (gui-less servers, etc) 

In order to use this feature remote machine should be running compatible version of chromium browser. Required chromium version can be determined by running PatreonDownloader locally and checking `Chrome` subfolder.

Please note that login functionality is disabled while running remote browser mode. Before using remote browser with PatreonDownloader you will need to manually login into your patreon account.

Example usage:
* Remote side: 
```chrome.exe --headless --disable-gpu --remote-debugging-port=9222 --user-data-dir=C:\chromedata --user-agent="Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/85.0.4183.83 Safari/537.36"```
* PatreonDownloader side: 
```.\PatreonDownloader.App.exe --url https://www.patreon.com/mycreator/posts --remote-browser-address ws://127.0.0.1:9222```

Another example posted in the [issue #16](https://github.com/AlexCSDev/PatreonDownloader/issues/16#issuecomment-742842926 "issue #16"):
- SSH to your host, forwarding port `9222`: `ssh -L 9222:127.0.0.1:9222 <host>`
- Start Chrome with:
```
google-chrome-stable \
  --headless \
  --disable-gpu \
  --remote-debugging-port=9222 \
  --user-data-dir=(pwd)/chromedata \
  --user-agent='Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/85.0.4183.83 Safari/537.36'
```
- Open the Chrome remote debugger by opening Chrome on your local machine and navigating to http://127.0.0.1:9222
- Click the `about:blank` link you see
- You'll be shown a page that looks like Chrome's debug tools, but with an address bar at the top and a large display of the browser's screen. You can interact with the address bar, click things on the screen and type things with your keyboard.
- Enter `https://www.patreon.com` in the debugger's address bar and hit enter
- Use the keyboard and mouse to log in
- Use ```--remote-browser-address ws://127.0.0.1:9222``` parameter to let PatreonDownloader know that remote browser should be used
