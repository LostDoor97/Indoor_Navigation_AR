using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;

public class GraphManager : MonoBehaviour
{
    public GameObject pointPrefab;
    private LineRenderer lineRenderer;
    private GraphWrapper graph;

    [Header("Configuration AR")]
    [SerializeField] private Transform arSessionOrigin; // Objet "XR Origin"
    [SerializeField] private Transform arCamera;        // "Main Camera"

    private GameObject worldAnchorContainer;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }
        ConfigurerLineRendererParDefaut();

        if (arSessionOrigin == null || arCamera == null)
        {
            Debug.LogError("[GraphManager] Erreur : arSessionOrigin ou arCamera manquants !");
            return;
        }

        // Création du conteneur d'ancrage
        worldAnchorContainer = new GameObject("AR_World_Anchor_Container");
        worldAnchorContainer.transform.SetParent(arSessionOrigin, false);

        // On attache le LineRenderer au conteneur pour qu'il suive le recalibrage
        lineRenderer.transform.SetParent(worldAnchorContainer.transform, false);
        lineRenderer.useWorldSpace = false;

        StartCoroutine(LoadGraphAndroidCompatible());
    }

    IEnumerator LoadGraphAndroidCompatible()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "carte.json").Replace("\\", "/");
        if (!filePath.Contains("://")) filePath = "file://" + filePath;

        using (UnityWebRequest webRequest = UnityWebRequest.Get(filePath))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                graph = JsonUtility.FromJson<GraphWrapper>(webRequest.downloadHandler.text);
                if (graph != null && graph.nodes != null)
                {
                    Debug.LogWarning($"[GraphManager] Graphe chargé avec succès ({graph.nodes.Length} nœuds).");

                    // Instanciation des points dans le conteneur
                    foreach (NodeData node in graph.nodes)
                    {
                        Vector3 positionNode = new Vector3(node.x, 0.01f, node.z);
                        if (pointPrefab != null)
                        {
                            GameObject newPoint = Instantiate(pointPrefab, worldAnchorContainer.transform);
                            newPoint.name = node.name;
                            newPoint.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
                            newPoint.transform.localPosition = positionNode;
                        }
                    }
                }
            }
        }
    }

    public void RecalibratePosition(string qrCodeId)
    {
        if (graph == null || graph.nodes == null) return;

        NodeData targetNode = null;
        foreach (NodeData node in graph.nodes)
        {
            if (node.id.Equals(qrCodeId, StringComparison.OrdinalIgnoreCase))
            {
                targetNode = node;
                break;
            }
        }

        if (targetNode == null) return;

        // --- RECALIBRAGE DE LA POSITION ET DE LA ROTATION ---

        // 1. Alignement de la rotation (Lacet / Axe Y uniquement)
        // On fait pivoter le conteneur pour que l'axe Z du graphe s'aligne avec la direction de la caméra
        float camYaw = arCamera.eulerAngles.y;
        worldAnchorContainer.transform.rotation = Quaternion.Euler(0f, camYaw, 0f);

        // 2. Alignement de la position
        Vector3 camPosition = arCamera.position;
        Vector3 nodeLocalPos = new Vector3(targetNode.x, 0f, targetNode.z);

        // On applique le décalage en tenant compte de la nouvelle rotation du conteneur
        Vector3 worldOffset = camPosition - worldAnchorContainer.transform.TransformDirection(nodeLocalPos);
        worldOffset.y = camPosition.y - 1.4f; // Fixe la hauteur de la carte environ 1m40 sous la caméra (au sol)

        worldAnchorContainer.transform.position = worldOffset;

        Debug.LogWarning($"[Recalibration] Position recalibrée sur le QR code !");

        // Calcul du chemin A* de l'entrée vers le Bureau 101 (dernier nœud)
        NodeData destinationNode = graph.nodes[graph.nodes.Length - 1];
        List<NodeData> nouveauChemin = Pathfinding.FindPath(targetNode, destinationNode, graph);

        if (nouveauChemin != null && nouveauChemin.Count > 0)
        {
            DessinerLigneDeGuidage(nouveauChemin);
        }
    }

    private void DessinerLigneDeGuidage(List<NodeData> chemin)
    {
        lineRenderer.positionCount = chemin.Count;
        for (int i = 0; i < chemin.Count; i++)
        {
            // Positionnement des points légèrement surélevés (2cm) par rapport au sol du conteneur
            Vector3 pointPosition = new Vector3(chemin[i].x, 0.02f, chemin[i].z);
            lineRenderer.SetPosition(i, pointPosition);
        }
    }

    private void ConfigurerLineRendererParDefaut()
    {
        lineRenderer.startWidth = 0.08f;
        lineRenderer.endWidth = 0.08f;

        // CORRECTION SHADER MOBILE/URP : Utilisation d'un shader non-éclairé standard
        Shader mobileShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Mobile/Unlit (Supports Lightmap)") ?? Shader.Find("Sprites/Default");
        lineRenderer.material = new Material(mobileShader);

        lineRenderer.startColor = Color.green;
        lineRenderer.endColor = Color.green;
    }
}