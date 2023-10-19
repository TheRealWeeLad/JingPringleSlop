using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HealthBar : MonoBehaviour
{
    Enemy _anchor;
    Camera _mainCamera;
    LineRenderer _lineRenderer;
    LineRenderer _outlineLineRenderer;
    RectTransform _canvas;
    List<RectTransform> _icons = new();
    const float PERCEIVEDSIZE = 0.1f;
    const float MAXSIZE = 2f;
    const float CONSTANTSIZE = 0.5f;
    const float OFFSET = 0.2f;
    // Values below determined experimentally
    const float outlineXOffset = 0.0025f;
    const float outlineYOffset = 0.046f;
    const float iconWidth = 0.04f;
    const float iconHeight = 0.07f;
    const float iconOffset = 0.06f;
    const float numFont = 0.04f;

    // Start is called before the first frame update
    void Start()
    {
        _mainCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        _lineRenderer = GetComponent<LineRenderer>();
        _outlineLineRenderer = transform.GetChild(1).GetComponent<LineRenderer>();
        _canvas = transform.GetChild(2).GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        if (_anchor != null)
        {
            // Always Face Camera
            float distToCamera = (_mainCamera.transform.position - transform.position).magnitude;
            float offset = distToCamera * PERCEIVEDSIZE;
            if (offset > MAXSIZE) offset = MAXSIZE;
            // UNCOMMENT FOR CONSTANT SIZE
            /*float offset = CONSTANTSIZE;*/
            Vector3 cameraRight = _mainCamera.transform.right;
            Vector3 left = transform.position - cameraRight * offset;
            // Update Size to Reflect Health
            float size = _anchor.Health / _anchor.MAXHEALTH * offset * 2;
            Vector3 right = left + cameraRight * size;

            Vector3[] positions = { left, right };
            _lineRenderer.SetPositions(positions);

            // Update Outline
            Vector3[] outlinePositions = new Vector3[4];
            Vector3 maxRight = left + 2 * offset * cameraRight;
            Vector3 cameraUp = _mainCamera.transform.up;
            Vector3 yOffset = outlineYOffset * cameraUp;
            Vector3 xOffset = outlineXOffset * cameraRight;
            outlinePositions[0] = positions[0] - xOffset + yOffset;
            outlinePositions[1] = maxRight + xOffset + yOffset;
            outlinePositions[2] = maxRight + xOffset - yOffset;
            outlinePositions[3] = positions[0] - xOffset - yOffset;
            _outlineLineRenderer.SetPositions(outlinePositions);

            // Update Status Icons
            _canvas.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, offset * 2);

            for (int i = 0; i < _icons.Count; i++)
            {
                // Update size
                RectTransform icon = _icons[i];
                icon.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, iconWidth + iconWidth * 2 * offset);
                icon.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, iconHeight + iconHeight * 2 * offset);

                TextMeshProUGUI number = icon.GetChild(0).GetComponent<TextMeshProUGUI>();
                number.fontSize = numFont + numFont * offset * 2;

                // Update position
                icon.anchoredPosition = new(iconOffset * i + iconOffset * i * 2 * offset, 0);
            }
        }
    }

    public void AddIcon(RectTransform img) { _icons.Add(img); }

    public void SetAnchor(Enemy enemy)
    {
        _anchor = enemy;
        // Every enemy should have empty child object positioned at highest point on enemy
        Transform topOfEnemy = enemy.transform.GetChild(enemy.transform.childCount - 2);
        transform.position = enemy.transform.position + Vector3.up * (topOfEnemy.transform.localPosition.y + OFFSET);
    }
}
