-- Create Settings table
CREATE TABLE IF NOT EXISTS Settings (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Volume INTEGER NOT NULL
);

-- Create Playlists table
CREATE TABLE IF NOT EXISTS Playlists (
    PlaylistId INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL
);

-- Create Songs table
CREATE TABLE IF NOT EXISTS Songs (
    Id TEXT PRIMARY KEY,
    Title TEXT NOT NULL,
    ChannelTitle TEXT,
    ChannelId TEXT,
    DurationMiliseconds REAL,
    Url TEXT
);

-- Create PlaylistSongs table
CREATE TABLE IF NOT EXISTS PlaylistSongs (
    PlaylistId INTEGER NOT NULL,
    SongId TEXT NOT NULL,
    [Order] INTEGER NOT NULL,
    FOREIGN KEY (PlaylistId) REFERENCES Playlists (PlaylistId),
    FOREIGN KEY (SongId) REFERENCES Songs (Id),
    PRIMARY KEY (PlaylistId, SongId)
);