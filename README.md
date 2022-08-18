# LoLCumulativeStats
A small tool for streamers to get information on their total kills/deaths/assists/towerdmg/kda for the day.

<br>
<h1>Usage</h1>
In order to use this software, you will need an API key from Riot. To do this, go to https://developer.riotgames.com/, log in and request an API key. 
<br>
When you load the app, it will ask you for your key.
<br>
To use in OBS, open a Text source, select "Read from file" and select the text file with the information you need.

<br>
<br>
<h1>Files</h1>
Files are stored in the same folder as the executable.
<h4>OBS file location</h4>
Kill total is stored in Kills.txt<br>
Death total is stored in Deaths.txt<br>
Assists total is stored in Assists.txt<br>
Tower damage total is stored in TowerDmg.txt<br>
KDA is stored in KDA.txt<br>
<br>
<h4>Logistics files</h4>
Information on the player (Riot name, region) is stored in PlayerInfo.txt<br>
The user's API key is stored in API-Key.txt (This is important as the app currently has no means of changing the API key)<br>
