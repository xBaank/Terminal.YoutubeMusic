# Terminal.YoutubeMusic [WIP]
Listen to youtube music from the terminal

![imagen](https://github.com/user-attachments/assets/2be7eb04-6baa-43b0-938d-8a6354a2f387)

## TODO
- [ ] Save current playlist locally
- [X] Implement login with cookies (either via browser or by manually grabbing them)
- [X] Implement Youtube music suggestions
- [X] Repeat song or list of songs
- [X] Seek time with only keyboard
- [X] Improve queue so you can go to any video without skiping one by one

## Requirements
- .NET 8
- Terminal
- Windows, Mac or Linux

## Features
- Almost no CPU usage 
- Small memory usage
- No external dependencies (No MPV, no yt-dlp or youtube-dl)

## Login
To login you can extract the cookies from music.youtube.com using:
- Chrome or Edge [get cookies.txt LOCALLY](https://chrome.google.com/webstore/detail/get-cookiestxt-locally/cclelndahbckbenkjhflpdbgdldlbecc)
- Firefox [cookies.txt](https://addons.mozilla.org/en-US/firefox/addon/cookies-txt/)
- Opera [edit this cookie](https://addons.opera.com/en/extensions/details/edit-this-cookie)

and then running `dotnet .\Console.dll --cookies-path path_to_cookies.txt`

## Download
> [!WARNING]\
> Expect bugs, crashes, sound glitches, etc.
- There is no stable release yet but you can download the latest [commit](https://nightly.link/xBaank/Terminal.YoutubeMusic/workflows/dotnet/main/YoutubeConsole.zip)

## How to run
- Run `dotnet Console.dll`

## Dependencies
- Terminal.gui v2
- YoutubeExplode
- Opentk
- Concentus
