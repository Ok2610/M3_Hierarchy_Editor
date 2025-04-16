/// <summary>
/// Represents a Tag object used to categorize or identify elements such as nodes in a hierarchy.
/// A tag contains a name, a unique ID, a tag type, and an associated tag set.
/// </summary>
[System.Serializable]
public class Tag
{
    /// <summary>
    /// The display name of the tag.
    /// </summary>
    public string name;

    /// <summary>
    /// The unique identifier of the tag.
    /// </summary>
    public int id;

    /// <summary>
    /// The identifier of the tag type this tag belongs to.
    /// </summary>
    public int tagTypeId;

    /// <summary>
    /// The identifier of the tag set that groups this tag.
    /// </summary>
    public int tagsetId;
}