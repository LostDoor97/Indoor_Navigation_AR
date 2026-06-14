using System.Collections.Generic;
using UnityEngine;

public class Pathfinding
{
    // Fonction principale qui retourne la liste des nœuds à suivre
    public static List<NodeData> FindPath(NodeData startNode, NodeData targetNode, GraphWrapper graph)
    {
        if (startNode == null || targetNode == null || graph == null) return null;

        // OPTIMISATION : Indexation rapide des nœuds par leur ID pour éviter de reparcourir le tableau
        Dictionary<string, NodeData> idToNodeMap = new Dictionary<string, NodeData>();
        foreach (var node in graph.nodes)
        {
            idToNodeMap[node.id] = node;
        }

        List<NodeData> openSet = new List<NodeData>();
        HashSet<NodeData> closedSet = new HashSet<NodeData>();
        openSet.Add(startNode);

        // Dictionnaires pour stocker les scores de l'algorithme
        Dictionary<NodeData, NodeData> parentMap = new Dictionary<NodeData, NodeData>();
        Dictionary<NodeData, float> gScore = new Dictionary<NodeData, float>();
        Dictionary<NodeData, float> fScore = new Dictionary<NodeData, float>();

        foreach (var node in graph.nodes)
        {
            gScore[node] = Mathf.Infinity;
            fScore[node] = Mathf.Infinity;
        }

        gScore[startNode] = 0;
        fScore[startNode] = GetDistance(startNode, targetNode);

        while (openSet.Count > 0)
        {
            // Trouver le nœud dans openSet avec le plus bas fScore
            NodeData currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (fScore[openSet[i]] < fScore[currentNode])
                {
                    currentNode = openSet[i];
                }
            }

            // Si on a atteint la destination, on reconstruit le chemin
            if (currentNode == targetNode)
            {
                return RetracePath(startNode, targetNode, parentMap);
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            // Analyser les voisins du nœud actuel
            foreach (NodeData neighbor in GetNeighborsOptimized(currentNode, graph, idToNodeMap))
            {
                if (closedSet.Contains(neighbor)) continue;

                float tentativeGScore = gScore[currentNode] + GetDistance(currentNode, neighbor);

                if (tentativeGScore < gScore[neighbor])
                {
                    parentMap[neighbor] = currentNode;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = gScore[neighbor] + GetDistance(neighbor, targetNode);

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        return null; // Aucun chemin trouvé
    }

    // Calcul de la distance euclidienne (Heuristique)
    private static float GetDistance(NodeData nodeA, NodeData nodeB)
    {
        return Mathf.Sqrt(Mathf.Pow(nodeB.x - nodeA.x, 2) + Mathf.Pow(nodeB.z - nodeA.z, 2));
    }

    // Récupère les voisins d'un nœud de manière optimisée grâce au Dictionnaire
    private static List<NodeData> GetNeighborsOptimized(NodeData node, GraphWrapper graph, Dictionary<string, NodeData> idToNodeMap)
    {
        List<NodeData> neighbors = new List<NodeData>();

        foreach (EdgeData edge in graph.edges)
        {
            if (edge.from == node.id)
            {
                // Accès direct et instantané O(1) au lieu de faire une boucle de recherche complète
                if (idToNodeMap.TryGetValue(edge.to, out NodeData neighbor))
                {
                    neighbors.Add(neighbor);
                }
            }
            else if (edge.to == node.id) // Permet de marcher dans les deux sens du couloir
            {
                if (idToNodeMap.TryGetValue(edge.from, out NodeData neighbor))
                {
                    neighbors.Add(neighbor);
                }
            }
        }
        return neighbors;
    }

    // Remonte la chaîne des parents pour créer la liste finale du chemin
    private static List<NodeData> RetracePath(NodeData startNode, NodeData endNode, Dictionary<NodeData, NodeData> parentMap)
    {
        List<NodeData> path = new List<NodeData>();
        NodeData currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = parentMap[currentNode];
        }
        path.Add(startNode);
        path.Reverse(); // Pour l'avoir du départ vers l'arrivée
        return path;
    }
}