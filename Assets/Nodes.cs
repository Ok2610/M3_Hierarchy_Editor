using System.Collections.Generic;
using System;    

/// <summary>
/// Represents a node in a hierarchical structure, where each node can have children nodes.
/// The node contains an ID, a tag, and a hierarchy ID to describe its relationship within the structure.
/// </summary>
[System.Serializable]
public class Nodes
{
    /// <summary>
    /// The unique identifier of the node.
    /// </summary>
    public int id;

    /// <summary>
    /// The identifier for the associated tag of the node.
    /// </summary>
    public int tagId;

    /// <summary>
    /// The tag associated with this node. This could be a category or a label used for further processing.
    /// </summary>
    public Tag tag;

    /// <summary>
    /// The ID representing the node's position within a hierarchy.
    /// </summary>
    public int hierarchyId;

    /// <summary>
    /// The list of child nodes that belong to this node, representing a hierarchical tree structure.
    /// </summary>
    public List<Nodes> children = new List<Nodes>();
}
