using System;

[System.Serializable]
public class NodeData
{
    public string id;
    public string name;
    public float x;
    public float z;
}

[System.Serializable]
public class EdgeData
{
    public string from;
    public string to;
}

[System.Serializable]
public class GraphWrapper
{
    public NodeData[] nodes;
    public EdgeData[] edges;
}