namespace Console.Containers.Matroska.Elements;

using System.Collections;
using Console.Containers.Matroska.EBML;
using Types;

internal class TopElements : IEnumerable<KeyValuePair<VInt, long>>
{
    private readonly long _seekHeadPosition;
    private readonly Dictionary<VInt, long> _topElements = new();

    public TopElements(long seekHeadPosition) => _seekHeadPosition = seekHeadPosition;

    public long this[VInt cuesId] => _topElements[cuesId];

    public IEnumerator<KeyValuePair<VInt, long>> GetEnumerator() => _topElements.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Add(VInt id, long position)
    {
        if (_topElements.ContainsKey(id))
            throw new Exception("Top element already exists");

        _topElements.Add(id, position + _seekHeadPosition);
    }

    public bool Contains(ElementType type) => _topElements.ContainsKey(type.Id);
}
