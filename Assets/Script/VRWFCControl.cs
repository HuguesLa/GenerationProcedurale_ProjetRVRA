using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Xml;
using System.IO;
using System.Linq;
using TMPro;

public class VRWFCControl : MonoBehaviour
{
    [Header("UI References")]
    public ScrollRect scrollView; // Le ScrollView contenant la liste
    public GameObject tileRowPrefab; // Prefab de ligne (TileRow.prefab)
    public Button regenerateButton; // Bouton "Régénérer"
    public TextAsset xmlAsset; // Assign ton XML (Trainer.xml) dans l'Inspector

    [Header("WFC Reference")]
    public SimpleTiledWFC wfcGenerator; // Drag ton GameObject avec SimpleTiledWFC

    private List<TileInfo> tiles = new List<TileInfo>(); // Stocke nom, weight actuel
    private string xmlPath; // Chemin du fichier XML (ex. Assets/Resources/Trainer.xml)
    private XmlDocument xmlDoc;

    [System.Serializable]
    public class TileInfo
    {
        public string name;
        public string weight; // Format "X,Y"
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

        xmlPath = Path.Combine(Application.dataPath, "Resources", xmlAsset.name + ".xml"); // Ajuste si chemin différent
        LoadAndParseXML();

        regenerateButton.onClick.AddListener(RegenerateEnvironment);

        // Écoute les changements sur les sliders (dans BuildUI)
    }

    void LoadAndParseXML()
    {
        xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(xmlAsset.text);

        // Parse les <tile> dans <tiles>
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
        foreach (Transform child in content) Destroy(child.gameObject);

        foreach (TileInfo tile in tiles)
        {
            GameObject row = Instantiate(tileRowPrefab, content);
            TMP_Text nameText = row.transform.Find("NameText")?.GetComponent<TMP_Text>();
            Slider weightSlider = row.transform.Find("WeightSlider")?.GetComponent<Slider>();
            TMP_Text weightText = row.transform.Find("WeightText")?.GetComponent<TMP_Text>();

            if (!nameText || !weightSlider || !weightText)
            {
                Debug.LogError("Prefab TileRow mal configuré ! Vérifie les noms et composants.");
                continue;
            }

            nameText.text = tile.name;
            float mainWeight = float.Parse(tile.weight.Split(',')[0]);
            weightSlider.value = mainWeight;
            weightText.text = tile.weight;

            weightSlider.onValueChanged.AddListener((float v) =>
            {
                tile.weight = Mathf.RoundToInt(v) + ",0";
                weightText.text = tile.weight;
                UpdateXML();
                SaveXML();
            });

            tile.nameText = nameText;
            tile.weightSlider = weightSlider;
            tile.weightText = weightText;
            tile.rowObject = row;
        }
    }

    void OnWeightChanged(TileInfo tile, float newValue)
    {
        // Update le weight dans la liste (format "X,0" en gardant Y=0 pour simplicité)
        tile.weight = Mathf.RoundToInt(newValue).ToString() + ",0";
        tile.weightText.text = tile.weight;

        // Update XML et save
        UpdateXML();
        SaveXML();
    }

    void UpdateXML()
    {
        // Met à jour tous les <tile> avec les nouveaux weights
        XmlNodeList tileNodes = xmlDoc.SelectNodes("//tile");
        int index = 0;
        foreach (XmlNode node in tileNodes)
        {
            if (index < tiles.Count)
            {
                XmlAttribute weightAttr = node.Attributes["weight"];
                if (weightAttr != null)
                {
                    weightAttr.Value = tiles[index].weight;
                }
                index++;
            }
        }
    }

    void SaveXML()
    {
        xmlDoc.Save(xmlPath);
        Debug.Log("XML sauvegardé : " + xmlPath);

        // Recharge l'asset si besoin (en éditeur, refresh ; en build, recharge via Resources.LoadTextAsset)
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

    void RegenerateEnvironment()
    {
        if (wfcGenerator == null)
        {
            Debug.LogError("Assign le SimpleTiledWFC dans l'Inspector !");
            return;
        }

        // Force le reload du XML mis à jour
        wfcGenerator.xml = (TextAsset)Resources.Load(xmlAsset.name, typeof(TextAsset)); // Recharge

        // Détruit l'ancien output
        if (wfcGenerator.output != null)
        {
            foreach (Transform child in wfcGenerator.output.transform)
            {
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
        }

        // Relance la génération
        wfcGenerator.Generate();
        wfcGenerator.Run();

        Debug.Log("Environnement régénéré !");
    }
}