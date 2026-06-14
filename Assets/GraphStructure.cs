using System;
using System.Collections.Generic;

[Serializable]
public class Node
{
    public string id;       // Identifiant unique (ex: "QR_ENTREE", "BUR_101")
    public float x;         // Position X en mètres (Gauche / Droite)
    public float y;         // Position Y (souvent 0 si tout est sur le même étage)
    public float z;         // Position Z en mètres (Avant / Arrière)
    public string name;     // Nom lisible pour l'utilisateur (ex: "Bureau 101")
}

[Serializable]
public class Edge
{
    public string from;     // ID du nœud de départ
    public string to;       // ID du nœud d'arrivée
}

[Serializable]
public class BuildingGraph
{
    public List<Node> nodes;
    public List<Edge> edges;
}