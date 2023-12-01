using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;
using System.Net;

public class NodeStructureHandler : MonoBehaviour
{
    // information about THIS node
    public string node_url;
    public bool expanded = false;
    public bool is_cycle = false;
    public int downloadProgress = 0;
    public bool scanning = false;

    public string scanError = null;

    // things that THIS node will need in its "gameplay loop"
    public GameObject node_mould_object;
    private VarHolder vars;
    public List<GameObject> connections;
    public LineRenderer linerenderer;
    public NodePhysicsHandler PhysicsHandler;
    public NodeColorHandler ColorHandler;
public void OnProgress(UnityWebRequest webRequest){
        var k = Mathf.FloorToInt(webRequest.downloadProgress * 100f);
        this.downloadProgress = k;
        if (k == 100) {
            scanning = false;
        }
    }

    public static string pattern = @"<a\s+(?:[^>]*?\s+)?href=""([^""]*)""";

    public void OnComplete(UnityWebRequest webRequest){
        scanning = false;

        if (webRequest.isNetworkError || webRequest.isHttpError){
            Debug.LogError(webRequest.error);
            scanError = webRequest.error;
            return;
        }
        
        string html = webRequest.downloadHandler.text;
        List<string> links = ParseHTMLForLinks(html);
        AttachUrls(links);
    }

    private void Awake()
    {
        connections.Clear();
        expanded = false;
        is_cycle = false;

        vars = GameObject.Find("Main Camera").GetComponent<VarHolder>();
        linerenderer = gameObject.GetComponent<LineRenderer>();
        node_mould_object = GameObject.Find("mould");
        PhysicsHandler = gameObject.GetComponent<NodePhysicsHandler>();
        ColorHandler = gameObject.GetComponent<NodeColorHandler>();
    }

    private void Update()
    {
        for (int x = 0; x < connections.Count; x++)
        {
            if (connections[x] != null)
            {
                Vector3 positionA = transform.position;
                Vector3 positionB = connections[x].GetComponent<Transform>().position;

                linerenderer.SetPosition(x * 2, positionA);
                linerenderer.SetPosition(x * 2 + 1, positionB);
            }
            else
            {
                linerenderer.SetPosition(x * 2, Vector3.zero);
                linerenderer.SetPosition(x * 2 + 1, Vector3.zero);
            }
        }
    }

    public static string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36";

    public void TaskDownload(string url) {
        scanError = null;
        StartCoroutine(DownloadNodeData(url));
    }

    private IEnumerator DownloadNodeData(string url)
    {
        UnityWebRequest webRequest = UnityWebRequest.Get(url);
        webRequest.SetRequestHeader("User-Agent", userAgent);
        
        webRequest.downloadProgressChanged += OnProgress;
        webRequest.completed += OnComplete;

        yield return webRequest.SendWebRequest();
    }

    private List<string> ParseHTMLForLinks(string html)
    {
        MatchCollection matches = Regex.Matches(html, pattern, RegexOptions.IgnoreCase);
        List<string> links = new List<string>();

        foreach (Match match in matches)
        {
            string href = match.Groups[1].Value;

            if (!string.IsNullOrEmpty(href))
            {
                string absoluteUrl = new Uri(new Uri(node_url), href).AbsoluteUri;
                links.Add(absoluteUrl);
            }
        }
        return links;
    }

    public void AttachUrls(List<string> connected_urls){
        expanded = true;
        ColorHandler.UpdateColors();

        foreach (string url in connected_urls)
        {
            if (!IsSamePage(url, node_url))
            {
                if (!vars.AllNodeUrls.Contains(url))
                {
                    GameObject current_node = Instantiate(node_mould_object, transform);
                    current_node.transform.position = transform.position + new Vector3(UnityEngine.Random.Range(-vars.InitGenRange, vars.InitGenRange),
                                                                                       UnityEngine.Random.Range(-vars.InitGenRange, vars.InitGenRange),
                                                                                       UnityEngine.Random.Range(-vars.InitGenRange, vars.InitGenRange));
                    current_node.GetComponent<NodeStructureHandler>().node_url = url;
                    current_node.name = url;
                    vars.AllNodeUrls += url + " \n ";
                    connections.Add(current_node);
                }
                else
                {
                    connections.Add(GameObject.Find(url));
                }
            }
            else
            {
                is_cycle = true;
            }
        }
        connections.RemoveAll(item => item == null);
        linerenderer.positionCount = connections.Count * 2;

        PhysicsHandler.connections = connections;
    }

    public void ExpandNode()
    {
        if (expanded) return;
        scanning = true;

        if (!string.IsNullOrEmpty(node_url) && !node_url.Contains("://"))
        {
            var separations = new List<string>(node_url.Split('/'));

            if (separations.Count >= 2)
            {
                string host = separations[0];
                separations.Remove(host);

                IPHostEntry dnslookup;
                dnslookup = Dns.GetHostEntry(host);

                if (dnslookup.AddressList.Length == 0)
                {
                    scanError = "Invalid hostname";
                    return;
                }
                node_url = $"https://{host}/{String.Join("/", separations)}/";
            }
            else
            {
                node_url = $"https://{separations[0].Trim('/')}/";
            }
        }
        TaskDownload(node_url);
    }
}
