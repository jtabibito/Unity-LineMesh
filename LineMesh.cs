﻿using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public enum LineType {
    none,
    upperline,
    middleLine,
    underLine
}

public class Line {
    public Vector2 position = Vector2.zero;
    public Vector2 size = Vector2.one;
    public Vector2 pivot = Vector2.zero;
    public Color color = Color.black;

    public Line(Text text, LineType type, int index) {
        var characters = text.cachedTextGenerator.characters;
        var lines = text.cachedTextGenerator.lines[index];
        UICharInfo charInfo = characters[lines.startCharIdx];
        CanvasScaler cs = text.GetComponentInParent<CanvasScaler>();
        float scaleFactor = (Screen.width * cs.referenceResolution[1] / (Screen.height * cs.referenceResolution[0])) >= 1 ? cs.referenceResolution[1] / Screen.height : cs.referenceResolution[0] / Screen.width;
        position = CalculatePosition(text.alignment, text.rectTransform.rect.width,
            (charInfo.cursorPos.y - text.cachedTextGenerator.lines[index].height * GetTypeWeight(type)) * scaleFactor);
        size = new Vector2(GetWidth(characters, lines.startCharIdx, characters.Count - 1, text.rectTransform.rect.width),
            text.fontSize * 0.1f == 0 ? float.Epsilon : text.fontSize * 0.1f) * scaleFactor;
        color = text.color;
        pivot = Text.GetTextAnchorPivot(text.alignment);
    }

    // public Line(Text text, LineType type, int index, int start, int length) {
    //     UICharInfo charInfo = text.cachedTextGenerator.characters[start];
    //     position = new Vector2(charInfo.cursorPos.x, charInfo.cursorPos.y - text.cachedTextGenerator.lines[index].height * GetTypeWeight(type));
    //     size = new Vector2(GetWidth(text.cachedTextGenerator.characters, start, length - 1),
    //         text.fontSize * 0.1f == 0 ? float.Epsilon : text.fontSize * 0.1f);
    //     color = text.color;
    //     SetPivot(text.alignment);
    // }

    float GetTypeWeight(LineType type) {
        switch (type) {
        case LineType.upperline:
            return 0;
        case LineType.middleLine:
            return 0.5f;
        case LineType.underLine:
        default:
            return 1;
        }
    }

    float GetWidth(IList<UICharInfo> characters, int start, int end, float width) {
        float num = 0;
        int lastCharIdx = end - 1;
        for (int i = start; i < end; ++i) {
            if (characters[i].cursorPos[0] > characters[i + 1].cursorPos[0]) {
                num = characters[i].cursorPos[0] + characters[i].charWidth - characters[start].cursorPos[0];
                break;
            }

            if (i == lastCharIdx) {
                num = characters[i + 1].cursorPos[0] - characters[start].cursorPos[0];
                break;
            }
        }
        return num >= width ? width * 0.5f : num;
    }

    public Vector2 CalculatePosition(TextAnchor anchor, float width, float y) {
        switch (anchor) {
        case TextAnchor.MiddleCenter:
        case TextAnchor.UpperCenter:
        case TextAnchor.LowerCenter:
            return Vector2.up * y;
        case TextAnchor.MiddleRight:
        case TextAnchor.UpperRight:
        case TextAnchor.LowerRight:
            return new Vector2(width * 0.5f, y);
        default:
            return new Vector2(-width * 0.5f, y);
        }
    }
}

public class LineMesh : MonoBehaviour {
    private Text text;
    public LineType type = LineType.none;

    private bool isInitialize = false;

    private int characterCount = 0;
    private LineType currentType = LineType.none;
    private Color currentColor = Color.white;
    private FontStyle currentFontStyle;
    private TextAnchor currentAnchor;
    private float lineSpacing;

    private List<Image> lines;

    // 标签化正则, 暂时未实现
    // 删除线
    //private const string deleteLineEvaluator = @"~~[^~]+?~~";
    //private const string deleteLineContentEvaluator = @"(?<=~~)[^~]+?(?=~~)";

    // 上划线
    //private const string underLineEvaluator = @"<up>.*</up>";
    //private const string underLineContentEvaluator = @"(?<=<up>).*(?=</up>)";

    // 下划线
    //private const string underLineEvaluator = @"<un>.*</un>";
    //private const string underLineContentEvaluator = @"(?<=<un>).*(?=</un>)";

    private void Awake() {
        text = this.GetComponent<Text>();
        lines = new List<Image>();
    }

    // Start is called before the first frame update
    private void Start() {
        UpdateParams();
        isInitialize = true;
    }

    // Update is called once per frame
    private void Update() {
        if (isInitialize) {
            if (type == LineType.none && currentType != type) {
                currentType = type;
                ClearLines();
            } else if (currentType != type || characterCount != text.cachedTextGenerator.characterCount ||
                currentColor != text.color || currentFontStyle != text.fontStyle ||
                currentAnchor != text.alignment || lineSpacing != text.lineSpacing) {
                UpdateParams();
                List<Line> lines = GetLines(type);
                CreateLines(lines);
            }
        }
    }

    private void UpdateParams() {
        characterCount = text.cachedTextGenerator.characterCount;
        currentType = type;
        currentColor = text.color;
        currentFontStyle = text.fontStyle;
        currentAnchor = text.alignment;
        lineSpacing = text.lineSpacing;
    }

    private List<Line> GetLines(LineType type) {
        if (type == LineType.none) return null;
        List<Line> lineInfos = new List<Line>();
        for (int j = 0; j < text.cachedTextGenerator.lineCount; ++j) {
            Line line = new Line(text, type, j);
            if (line.size[0] == 0 || line.size[1] == 0)
                continue;
            lineInfos.Add(line);
        }
        return lineInfos;
    }

    private void ClearLines() {
        foreach (var line in lines)
            Destroy(line.gameObject);
        lines.Clear();
    }

    private void CreateLines(List<Line> lineInfos) {
        if (lineInfos == null) return;
        ClearLines();
        for (int i = 0; i < lineInfos.Count; ++i) {
            Image img = new GameObject().AddComponent<Image>();
            img.transform.SetParent(text.transform, false);
            img.name = $"line{i}";
            img.color = lineInfos[i].color;
            img.rectTransform.pivot = lineInfos[i].pivot;
            lines.Add(img);

            Texture2D texture = new Texture2D((int)lineInfos[i].size[0], (int)lineInfos[i].size[1], TextureFormat.ARGB32, false);
            var colors = texture.GetPixels();
            for (int j = 0; j < colors.Length; ++j)
                colors[j] = img.color;
            texture.SetPixels(colors);
            texture.Apply();

            img.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
            img.SetNativeSize();
            img.rectTransform.sizeDelta = img.rectTransform.sizeDelta;
            lines[i].rectTransform.anchoredPosition = lineInfos[i].position;
        }
    }
}