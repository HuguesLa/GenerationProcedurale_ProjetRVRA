using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Xml;
using System.IO;
using TMPro;

public class VRWFCControl : MonoBehaviour
{
    [Header("UI References")]
    public ScrollRect scrollView;
    public GameObject tileRowPrefab;
    public Button regenerateButton;
    public TextAsset xmlAsset;

    [Header("WFC Reference")]
    public SimpleTiledWFC wfcGenerator;

    private List<TileInfo> tiles = new List<TileInfo>();
    private string xmlPath;
    private XmlDocument xmlDoc;

    // Pour sauvegarde différée
    private Coroutine saveCoroutine;
    private const float SAVE_DELAY = 0.5f;

    [System.Serializable]
    public class TileInfo
    {
        public string name;
        public string weight;
        public Slider weightSlider;
        public TMP_Text nameText;
        public TMP_Text weightText;
        public GameObject rowObject;
    }

    void Start()
    {
        if (xmlAsset == null)
        {
            Debug.LogError("Assign le XML dans l'Inspector !");
            return;
        }

        xmlPath = Path.Combine(Application.dataPath, "Resources", xmlAsset.name + ".xml");
        LoadAndParseXML();

        if (regenerateButton != null)
            regenerateButton.onClick.AddListener(RegenerateEnvironment);
    }

    void LoadAndParseXML()
    {
        xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(xmlAsset.text);

        tiles.Clear();
        XmlNodeList tileNodes = xmlDoc.SelectNodes("//tile");
        foreach (XmlNode node in tileNodes)
        {
            string name = node.Attributes["name"]?.Value;
            string weight = node.Attributes["weight"]?.Value ?? "1,0";
            if (!string.IsNullOrEmpty(name))
            {
                tiles.Add(new TileInfo { name = name, weight = weight });
            }
        }
        BuildUI();
    }

    void BuildUI()
    {
        if (scrollView == null || scrollView.content == null)
        {
            Debug.LogError("ScrollView ou Content manquant !");
            return;
        }

        Transform content = scrollView.content;
        foreach (Transform child in content)
            Destroy(child.gameObject);

        foreach (TileInfo tile in tiles)
        {
            GameObject row = Instantiate(tileRowPrefab, content);
            TMP_Text nameText = row.transform.Find("NameText")?.GetComponent<TMP_Text>();
            Slider weightSlider = row.transform.Find("WeightSlider")?.GetComponent<Slider>();
            TMP_Text weightText = row.transform.Find("WeightText")?.GetComponent<TMP_Text>();

            if (!nameText || !weightSlider || !weightText)
            {
                Debug.LogError("Prefab TileRow mal configuré ! Vérifie les noms et composants.");
                Destroy(row);
                continue;
            }

            // Configuration du slider
            nameText.text = tile.name;
            float mainWeight = float.Parse(tile.weight.Split(',')[0]);
            weightSlider.wholeNumbers = true;
            weightSlider.minValue = 0;
            weightSlider.maxValue = 20;
            weightSlider.value = mainWeight;
            weightText.text = tile.weight;

            // Écouteur avec sauvegarde différée
            weightSlider.onValueChanged.AddListener((float v) =>
            {
                int newWeight = Mathf.RoundToInt(v);
                tile.weight = newWeight + ",0";
                weightText.text = tile.weight;
                UpdateXML();

                // Sauvegarde différée
                if (saveCoroutine != null)
                    StopCoroutine(saveCoroutine);
                saveCoroutine = StartCoroutine(DelayedSave());
            });

            // Stockage des références
            tile.nameText = nameText;
            tile.weightSlider = weightSlider;
            tile.weightText = weightText;
            tile.rowObject = row;
        }
    }

    void UpdateXML()
    {
        XmlNodeList tileNodes = xmlDoc.SelectNodes("//tile");
        for (int i = 0; i < tileNodes.Count && i < tiles.Count; i++)
        {
            XmlAttribute weightAttr = tileNodes[i].Attributes["weight"];
            if (weightAttr != null)
            {
                weightAttr.Value = tiles[i].weight;
            }
        }
    }

    private IEnumerator DelayedSave()
    {
        yield return new WaitForSeconds(SAVE_DELAY);
        SaveXML();
    }

    void SaveXML()
    {
        try
        {
            xmlDoc.Save(xmlPath);
            Debug.Log("XML sauvegardé : " + xmlPath);

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
        }
        catch (System.Exception e)
        {
            Debug.LogError("Erreur sauvegarde XML : " + e.Message);
        }
    }

    void RegenerateEnvironment()
    {
        if (wfcGenerator == null)
        {
            Debug.LogError("Assign le SimpleTiledWFC dans l'Inspector !");
            return;
        }

        // Recharge le XML modifié
        TextAsset updatedXml = Resources.Load<TextAsset>(xmlAsset.name);
        if (updatedXml != null)
            wfcGenerator.xml = updatedXml;

        // Nettoie l'ancien output
        if (wfcGenerator.output != null)
        {
            foreach (Transform child in wfcGenerator.output.transform)
            {
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }

        // Régénère
        wfcGenerator.Generate();
        wfcGenerator.Run();

        Debug.Log("Environnement régénéré !");
    }

    // Optionnel : rechargement manuel avec R
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            LoadAndParseXML();
        }
    }
}