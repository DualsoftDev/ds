namespace Engine.Core;

public interface ITagSREContainer {
    Action<IEnumerable<Tag>> AddTagsFunc { get; }
}
internal class TagSREContainer : ITagSREContainer
{
    TagDic _starts = new();
    TagDic _resets = new();
    TagDic _ends   = new();
    Action<IEnumerable<Tag>> _addTagsFunc;

    public IEnumerable<Tag> TagsStart => _starts.Values;
    public IEnumerable<Tag> TagsReset => _resets.Values;
    public IEnumerable<Tag> TagsEnd => _ends.Values;

    public TagSREContainer()
    {
        _addTagsFunc = new Action<IEnumerable<Tag>>(tags => AddTags(tags.ToArray()));
    }

    public Action<IEnumerable<Tag>> AddTagsFunc => _addTagsFunc;

    public void AddStartTags(params Tag[] tags)
    {
        foreach (var tag in tags)
            _starts[tag.Name] = tag;
    }

    public void AddResetTags(params Tag[] tags)
    {
        foreach (var tag in tags)
            _resets[tag.Name] = tag;
    }

    public void AddEndTags(params Tag[] tags)
    {
        foreach (var tag in tags)
            _ends[tag.Name] = tag;
    }

    public void AddTags(params Tag[] tags)
    {
        foreach (var tag in tags)
        {
            TagDic dic = null;
            if (tag.Type.HasFlag(TagType.Start))
                dic = _starts;
            else if (tag.Type.HasFlag(TagType.Reset))
                dic = _resets;
            else if (tag.Type.HasFlag(TagType.End))
                dic = _ends;
            else
                throw new Exception("Tag type is not supported.");

            dic[tag.Name] = tag;
        }
    }

}
