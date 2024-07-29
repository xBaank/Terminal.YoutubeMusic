# Terminal.YoutubeMusic [WIP]
Listen to youtube music from the terminal

![imagen](https://github.com/user-attachments/assets/f588ad79-3fae-4f8c-b7d3-e4570b01bdb3)

## TODO
- [ ] Implement login with cookies (either via browser or by manually grabbing them)
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

## Download
> [!WARNING]\
> Expect bugs, crashes, sound glitches, etc.
- There is no stable release yet but you can download the latest [commit](https://nightly.link/xBaank/Terminal.YoutubeMusic/workflows/dotnet/main/YoutubeConsole.zip)

## How to run
- Run `dotnet Console.dll`

## Dependencies
- Terminal.gui v2
- YoutubeExplode ([Forked version](https://github.com/xBaank/YoutubeExplode/tree/music))
- Opentk
- Concentus
