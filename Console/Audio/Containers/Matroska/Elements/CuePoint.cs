namespace Console.Audio.Containers.Matroska.Elements;

internal readonly record struct CuePoint(double Time, IEnumerable<CueTrackPosition> TrackPositions);

internal readonly record struct CueTrackPosition(long ClusterPosition, int Track);
