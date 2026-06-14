using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking; // Gère les requêtes web sur Android

namespace IndoorNavigation.Database
{
    public class NavigationDatabase : MonoBehaviour
    {
        private static NavigationDatabase s_Instance;

        private string m_DatabasePath;
        private const string DATABASE_NAME = "navigation_database.db";

        private Dictionary<string, Navigation.NavigationNode> m_NodeCache;
        private List<Navigation.NavigationNode> m_AllNodes;

        public static NavigationDatabase Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = FindObjectOfType<NavigationDatabase>();
                    if (s_Instance == null)
                    {
                        var go = new GameObject("NavigationDatabase");
                        s_Instance = go.AddComponent<NavigationDatabase>();
                    }
                }
                return s_Instance;
            }
        }

        private void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeDatabase();
        }

        public void InitializeDatabase()
        {
            m_NodeCache = new Dictionary<string, Navigation.NavigationNode>();
            m_AllNodes = new List<Navigation.NavigationNode>();
            m_DatabasePath = Path.Combine(Application.persistentDataPath, DATABASE_NAME);
            Debug.Log($"[Navigation Database] Initialized at: {m_DatabasePath}");
        }

        public Navigation.NavigationGraph LoadGraphFromJSON(string jsonFileName)
        {
            // Initialisation des listes si le mode Éditeur court-circuite Awake
            if (m_NodeCache == null)
                m_NodeCache = new Dictionary<string, Navigation.NavigationNode>();
            if (m_AllNodes == null)
                m_AllNodes = new List<Navigation.NavigationNode>();

            if (!jsonFileName.EndsWith(".json"))
                jsonFileName += ".json";

            string jsonPath = Path.Combine(Application.streamingAssetsPath, jsonFileName);
            string jsonContent = "";

            // --- DEBUT DU PATCH ANDROID (UnityWebRequest) ---
            if (jsonPath.Contains("://") || jsonPath.Contains(":///"))
            {
                // Lecture depuis l'APK compressé (Android)
                using (UnityWebRequest webRequest = UnityWebRequest.Get(jsonPath))
                {
                    // Force l'attente synchrone de la requête
                    var operation = webRequest.SendWebRequest();
                    while (!operation.isDone) { }

                    if (webRequest.result == UnityWebRequest.Result.Success)
                    {
                        jsonContent = webRequest.downloadHandler.text;
                        Debug.Log($"[Navigation Database] Read {jsonContent.Length} chars via WebRequest from {jsonFileName}");
                    }
                    else
                    {
                        Debug.LogError($"[Navigation Database] Failed to load JSON via WebRequest. Error: {webRequest.error} at path: {jsonPath}");
                        return null;
                    }
                }
            }
            else
            {
                // Lecture standard (Éditeur Unity sur PC / Mac)
                if (File.Exists(jsonPath))
                {
                    jsonContent = File.ReadAllText(jsonPath);
                    Debug.Log($"[Navigation Database] Read {jsonContent.Length} chars via File System from {jsonFileName}");
                }
                else
                {
                    Debug.LogError($"[Navigation Database] JSON file not found in local file system: {jsonPath}");
                    return null;
                }
            }
            // --- FIN DU PATCH ANDROID ---

            try
            {
                JObject root = JObject.Parse(jsonContent);

                string buildingName = root["BuildingName"]?.ToString() ?? "Unknown Building";
                var graph = new Navigation.NavigationGraph(buildingName);
                graph.Nodes = new List<Navigation.NavigationNode>();

                // Read metadata Dictionary
                if (root["GraphMetadata"] != null)
                {
                    graph.GraphMetadata = new Dictionary<string, string>();
                    foreach (var prop in root["GraphMetadata"].Children<JProperty>())
                        graph.GraphMetadata[prop.Name] = prop.Value.ToString();
                }

                // Read nodes
                var nodesArray = root["Nodes"] as JArray;
                if (nodesArray != null)
                {
                    foreach (JObject nodeJson in nodesArray)
                    {
                        var posJson = nodeJson["Position"];
                        Vector3 position = new Vector3(
                            posJson?["x"]?.Value<float>() ?? 0f,
                            posJson?["y"]?.Value<float>() ?? 0f,
                            posJson?["z"]?.Value<float>() ?? 0f
                        );

                        var node = new Navigation.NavigationNode(
                            nodeJson["Id"]?.ToString() ?? "",
                            nodeJson["Name"]?.ToString() ?? "",
                            position
                        );

                        node.MarkerId = nodeJson["MarkerId"]?.ToString() ?? "";
                        node.IsPointOfInterest = nodeJson["IsPointOfInterest"]?.Value<bool>() ?? false;
                        node.Category = nodeJson["Category"]?.ToString() ?? "";

                        node.Metadata = new Dictionary<string, string>();
                        if (nodeJson["Metadata"] != null)
                        {
                            foreach (var prop in nodeJson["Metadata"].Children<JProperty>())
                                node.Metadata[prop.Name] = prop.Value.ToString();
                        }

                        node.ConnectedNodeIds = new List<string>();
                        var connectedIds = nodeJson["ConnectedNodeIds"] as JArray;
                        if (connectedIds != null)
                            foreach (var id in connectedIds)
                                node.ConnectedNodeIds.Add(id.ToString());

                        node.EdgeCosts = new List<float>();
                        var edgeCosts = nodeJson["EdgeCosts"] as JArray;
                        if (edgeCosts != null)
                            foreach (var cost in edgeCosts)
                                node.EdgeCosts.Add(cost.Value<float>());

                        graph.AddNode(node);
                    }
                }

                if (!graph.ValidateIntegrity())
                    Debug.LogWarning($"[Navigation Database] Graph integrity check failed for {jsonFileName}");

                CacheGraph(graph);
                Debug.Log($"[Navigation Database] Loaded graph '{graph.BuildingName}' with {graph.Nodes.Count} nodes");

                return graph;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Navigation Database] Error parsing graph: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        public bool SaveGraphToJSON(Navigation.NavigationGraph graph, string jsonFileName)
        {
            // Note importante : Application.streamingAssetsPath est en LECTURE SEULE sur un vrai téléphone Android.
            // Cette méthode fonctionnera parfaitement dans l'Éditeur Unity pour générer votre fichier de configuration.
            try
            {
                if (!jsonFileName.EndsWith(".json"))
                    jsonFileName += ".json";

                string jsonPath = Path.Combine(Application.streamingAssetsPath, jsonFileName);

                JsonSerializerSettings settings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };

                string jsonContent = JsonConvert.SerializeObject(graph, settings);
                File.WriteAllText(jsonPath, jsonContent);

                Debug.Log($"[Navigation Database] Graph saved to: {jsonPath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Navigation Database] Error saving graph: {ex.Message}");
                return false;
            }
        }

        private void CacheGraph(Navigation.NavigationGraph graph)
        {
            m_NodeCache.Clear();
            m_AllNodes.Clear();

            if (graph.Nodes == null)
            {
                ThreadSafeDebugWarning("[Navigation Database] Graph nodes list is null");
                return;
            }

            foreach (var node in graph.Nodes)
            {
                if (node == null) continue;
                m_NodeCache[node.Id] = node;
                m_AllNodes.Add(node);
            }
        }

        // Wrapper de secours pour l'appel de Debug depuis un thread secondaire potentiel lors du cache
        private void ThreadSafeDebugWarning(string message)
        {
            if (Screen.width > 0) // Simple vérification pour savoir si on est sur le thread principal
                Debug.LogWarning(message);
        }

        public Navigation.NavigationNode GetNodeById(string nodeId)
        {
            if (m_NodeCache.TryGetValue(nodeId, out var node))
                return node;
            return null;
        }

        public List<Navigation.NavigationNode> GetAllPointsOfInterest()
        {
            return m_AllNodes.FindAll(n => n.IsPointOfInterest);
        }

        public List<Navigation.NavigationNode> GetPointsOfInterestByCategory(string category)
        {
            return m_AllNodes.FindAll(n => n.IsPointOfInterest && n.Category == category);
        }

        public Navigation.NavigationNode FindNodeByMarkerId(string markerId)
        {
            return m_AllNodes.Find(n => n.MarkerId == markerId);
        }

        public void SaveNode(Navigation.NavigationNode node)
        {
            m_NodeCache[node.Id] = node;
            if (!m_AllNodes.Exists(n => n.Id == node.Id))
                m_AllNodes.Add(node);
        }

        public void DeleteNode(string nodeId)
        {
            m_NodeCache.Remove(nodeId);
            m_AllNodes.RemoveAll(n => n.Id == nodeId);
        }

        public List<Navigation.NavigationNode> GetAllNodes()
        {
            return new List<Navigation.NavigationNode>(m_AllNodes);
        }
    }
}