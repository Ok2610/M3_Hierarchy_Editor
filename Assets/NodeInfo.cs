/// <summary>
/// Represents metadata information about a node, including its name, hierarchy,
/// tag associations, and optional parent node reference.
/// </summary>
public class NodeInfo
{
    /// <summary>
    /// The unique identifier of the node.
    /// </summary>
    public int id;

    /// <summary>
    /// The display name of the node.
    /// </summary>
    public string name;

    /// <summary>
    /// The identifier of the hierarchy this node belongs to.
    /// </summary>
    public int hierarchyId;

    /// <summary>
    /// The identifier of the tag type associated with the node.
    /// </summary>
    public int tagTypeId;

    /// <summary>
    /// The identifier of the tag set associated with the node.
    /// </summary>
    public int tagSetId;

    /// <summary>
    /// The optional ID of the parent node, if it exists.
    /// </summary>
    public int? parentnodeId;

    /// <summary>
    /// Constructs a new instance of <see cref="NodeInfo"/> with the provided values.
    /// </summary>
    /// <param name="id">The unique ID of the node.</param>
    /// <param name="name">The display name of the node.</param>
    /// <param name="hierarchyId">The hierarchy ID to which the node belongs.</param>
    /// <param name="tagTypeId">The tag type ID associated with this node.</param>
    /// <param name="tagSetId">The tag set ID associated with this node.</param>
    /// <param name="parentnodeId">The optional parent node ID, or null if there is no parent.</param>
    public NodeInfo(int id, string name, int hierarchyId, int tagTypeId, int tagSetId, int? parentnodeId)
    {
        this.id = id;
        this.name = name;
        this.hierarchyId = hierarchyId;
        this.tagTypeId = tagTypeId;
        this.tagSetId = tagSetId;
        this.parentnodeId = parentnodeId;
    }
}
