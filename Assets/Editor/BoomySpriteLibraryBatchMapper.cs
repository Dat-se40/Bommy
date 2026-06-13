#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D.Animation;

public class BoomyCharacterSpriteLibraryGenerator : EditorWindow
{
    private const int FramesPerRow = 8;

    [SerializeField] private string outputFolder = "Assets/ScriptableObjects/Characters/Skin colors";
    [SerializeField] private string assetNamePrefix = "Mimi";
    [SerializeField] private string categoryName = "Body";

    [Header("Auto Slice Support")]
    [SerializeField] private float rowTolerance = 24f;

    [Header("Options")]
    [SerializeField] private bool overwriteExisting = true;
    [SerializeField] private bool pingCreatedAsset = true;

    private readonly RowMap[] rowMaps =
    {
        new RowMap(1, "IdleDown"),
        new RowMap(5, "WalkDown"),
        new RowMap(6, "WalkUp"),
        new RowMap(7, "WalkRight"),
        new RowMap(8, "WalkLeft")
    };

    [MenuItem("Tools/Boomy/Generate Character Sprite Library")]
    private static void Open()
    {
        GetWindow<BoomyCharacterSpriteLibraryGenerator>("Character Sprite Library");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Boomy Character Sprite Library Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
        assetNamePrefix = EditorGUILayout.TextField("Asset Name Prefix", assetNamePrefix);
        categoryName = EditorGUILayout.TextField("Category Name", categoryName);

        EditorGUILayout.Space();

        rowTolerance = EditorGUILayout.FloatField("Row Tolerance", rowTolerance);

        EditorGUILayout.Space();

        overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing", overwriteExisting);
        pingCreatedAsset = EditorGUILayout.Toggle("Ping Created Asset", pingCreatedAsset);

        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "Chọn PNG spritesheet đã slice trong Project, không chọn SpriteLibraryAsset.\n\n" +
            "Tool hỗ trợ cả Grid Slice và Auto Slice.\n\n" +
            "Mapping:\n" +
            "Hàng 1: IdleDown\n" +
            "Hàng 5: WalkDown\n" +
            "Hàng 6: WalkUp\n" +
            "Hàng 7: WalkRight\n" +
            "Hàng 8: WalkLeft\n\n" +
            "Mỗi hàng lấy 8 frame: Label_0 đến Label_7.\n" +
            "Tổng label tạo ra: 40.",
            MessageType.Info
        );

        Texture2D[] selectedTextures = Selection.GetFiltered<Texture2D>(SelectionMode.Assets);

        EditorGUILayout.LabelField("Selected PNG Spritesheets: " + selectedTextures.Length);

        using (new EditorGUI.DisabledScope(selectedTextures.Length == 0))
        {
            if (GUILayout.Button("Generate From Selected PNG Spritesheets"))
            {
                GenerateFromSelectedTextures(selectedTextures);
            }
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Verify Selected Sprite Library Asset"))
        {
            VerifySelectedSpriteLibrary();
        }
    }

    /// <summary>
    /// Tạo SpriteLibraryAsset cho các spritesheet đang được chọn.
    /// </summary>
    private void GenerateFromSelectedTextures(Texture2D[] textures)
    {
        EnsureFolder(outputFolder);

        foreach (Texture2D texture in textures)
        {
            GenerateLibrary(texture);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[FLOW:SETUP] Sprite library generation completed. Count=" + textures.Length);
    }

    /// <summary>
    /// Tạo một SpriteLibraryAsset từ spritesheet đã slice.
    /// Hỗ trợ Auto Slice bằng cách gom frame theo hàng.
    /// </summary>
    private void GenerateLibrary(Texture2D texture)
    {
        string texturePath = AssetDatabase.GetAssetPath(texture);
        List<Sprite> sprites = LoadSlicedSprites(texturePath);

        if (sprites.Count == 0)
        {
            Debug.LogWarning("[FLOW:SETUP] No sliced sprites found. Texture=" + texturePath);
            return;
        }

        List<SpriteRow> rows = BuildRows(sprites, rowTolerance);

        if (rows.Count < 8)
        {
            Debug.LogWarning(
                "[FLOW:SETUP] Not enough detected rows. " +
                "Texture=" + texture.name +
                " Rows=" + rows.Count +
                " Required=8. Try increasing Row Tolerance or check slicing."
            );

            LogDetectedRows(rows);
            return;
        }

        SpriteLibraryAsset library = CreateInstance<SpriteLibraryAsset>();

        int labelCount = 0;

        foreach (RowMap rowMap in rowMaps)
        {
            labelCount += AddMappedRow(library, rows, rowMap);
        }

        string assetPath = GetOutputAssetPath(texture.name);

        if (overwriteExisting && AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(assetPath) != null)
        {
            AssetDatabase.DeleteAsset(assetPath);
        }

        AssetDatabase.CreateAsset(library, assetPath);
        EditorUtility.SetDirty(library);

        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(assetPath);

        if (pingCreatedAsset)
        {
            Selection.activeObject = library;
            EditorGUIUtility.PingObject(library);
        }

        Debug.Log(
            "[FLOW:SETUP] Created SpriteLibraryAsset: " + assetPath +
            " | category=" + categoryName +
            " | detectedRows=" + rows.Count +
            " | labels=" + labelCount
        );

        LogDetectedRows(rows);
    }

    /// <summary>
    /// Thêm 8 frame từ hàng được map vào SpriteLibraryAsset.
    /// Hàng trong mapping là 1-based: hàng 1 là hàng trên cùng.
    /// </summary>
    private int AddMappedRow(SpriteLibraryAsset library, List<SpriteRow> rows, RowMap rowMap)
    {
        int rowIndex = rowMap.RowNumber - 1;

        if (rowIndex < 0 || rowIndex >= rows.Count)
        {
            Debug.LogWarning(
                "[FLOW:SETUP] Row not found. Row=" + rowMap.RowNumber +
                " LabelPrefix=" + rowMap.LabelPrefix
            );

            return 0;
        }

        SpriteRow row = rows[rowIndex];

        if (row.Sprites.Count < FramesPerRow)
        {
            Debug.LogWarning(
                "[FLOW:SETUP] Row has less than 8 frames. " +
                "Row=" + rowMap.RowNumber +
                " LabelPrefix=" + rowMap.LabelPrefix +
                " Frames=" + row.Sprites.Count
            );
        }

        int addedCount = 0;
        int frameCount = Mathf.Min(FramesPerRow, row.Sprites.Count);

        for (int frame = 0; frame < frameCount; frame++)
        {
            Sprite sprite = row.Sprites[frame];
            string label = rowMap.LabelPrefix + "_" + frame;

            library.AddCategoryLabel(sprite, categoryName, label);
            addedCount++;
        }

        return addedCount;
    }

    /// <summary>
    /// Load toàn bộ sub-sprite của một texture.
    /// </summary>
    private static List<Sprite> LoadSlicedSprites(string texturePath)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(texturePath);

        return assets
            .OfType<Sprite>()
            .ToList();
    }

    /// <summary>
    /// Gom các sprite thành từng hàng dựa trên vị trí centerY.
    /// Sau đó sort hàng từ trên xuống, frame trong hàng từ trái sang phải.
    /// </summary>
    private static List<SpriteRow> BuildRows(List<Sprite> sprites, float tolerance)
    {
        List<SpriteRow> rows = new();

        List<Sprite> sortedByY = sprites
            .OrderByDescending(GetCenterY)
            .ToList();

        foreach (Sprite sprite in sortedByY)
        {
            float centerY = GetCenterY(sprite);
            SpriteRow targetRow = null;

            for (int i = 0; i < rows.Count; i++)
            {
                if (Mathf.Abs(rows[i].CenterY - centerY) <= tolerance)
                {
                    targetRow = rows[i];
                    break;
                }
            }

            if (targetRow == null)
            {
                targetRow = new SpriteRow();
                rows.Add(targetRow);
            }

            targetRow.Add(sprite);
        }

        rows = rows
            .OrderByDescending(row => row.CenterY)
            .ToList();

        foreach (SpriteRow row in rows)
        {
            row.SortLeftToRight();
        }

        return rows;
    }

    private static float GetCenterY(Sprite sprite)
    {
        return sprite.rect.y + sprite.rect.height * 0.5f;
    }

    private static float GetCenterX(Sprite sprite)
    {
        return sprite.rect.x + sprite.rect.width * 0.5f;
    }

    private string GetOutputAssetPath(string textureName)
    {
        string cleanTextureName = textureName.Trim();

        string assetName = string.IsNullOrWhiteSpace(assetNamePrefix)
            ? cleanTextureName + "_Library.asset"
            : assetNamePrefix + "_" + cleanTextureName + "_Library.asset";

        return outputFolder.TrimEnd('/') + "/" + assetName;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        Directory.CreateDirectory(folderPath);
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// In thông tin hàng detect được để kiểm tra Auto Slice có đúng không.
    /// </summary>
    private static void LogDetectedRows(List<SpriteRow> rows)
    {
        for (int i = 0; i < rows.Count; i++)
        {
            string spriteNames = string.Join(", ", rows[i].Sprites.Select(sprite => sprite.name));

            Debug.Log(
                "[FLOW:SETUP] Detected Row " + (i + 1) +
                " | frames=" + rows[i].Sprites.Count +
                " | centerY=" + rows[i].CenterY.ToString("0.00") +
                " | sprites=[" + spriteNames + "]"
            );
        }
    }

    /// <summary>
    /// Kiểm tra SpriteLibraryAsset đang chọn có category và label hay không.
    /// </summary>
    private static void VerifySelectedSpriteLibrary()
    {
        SpriteLibraryAsset library = Selection.activeObject as SpriteLibraryAsset;

        if (library == null)
        {
            Debug.LogWarning("[FLOW:SETUP] Selected object is not a SpriteLibraryAsset.");
            return;
        }

        IEnumerable<string> categories = library.GetCategoryNames();

        int totalLabels = 0;

        foreach (string category in categories)
        {
            List<string> labels = library.GetCategoryLabelNames(category).ToList();
            totalLabels += labels.Count;

            Debug.Log(
                "[FLOW:SETUP] Category=" + category +
                " Labels=" + labels.Count +
                " [" + string.Join(", ", labels) + "]"
            );
        }

        Debug.Log("[FLOW:SETUP] Verify SpriteLibraryAsset done. Total labels=" + totalLabels);
    }

    private readonly struct RowMap
    {
        public readonly int RowNumber;
        public readonly string LabelPrefix;

        public RowMap(int rowNumber, string labelPrefix)
        {
            RowNumber = rowNumber;
            LabelPrefix = labelPrefix;
        }
    }

    private sealed class SpriteRow
    {
        public readonly List<Sprite> Sprites = new();

        public float CenterY { get; private set; }

        public void Add(Sprite sprite)
        {
            Sprites.Add(sprite);
            RecalculateCenterY();
        }

        public void SortLeftToRight()
        {
            Sprites.Sort((a, b) => GetCenterX(a).CompareTo(GetCenterX(b)));
        }

        private void RecalculateCenterY()
        {
            if (Sprites.Count == 0)
            {
                CenterY = 0f;
                return;
            }

            float total = 0f;

            for (int i = 0; i < Sprites.Count; i++)
            {
                total += GetCenterY(Sprites[i]);
            }

            CenterY = total / Sprites.Count;
        }
    }
}
#endif
