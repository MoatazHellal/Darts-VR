using System.Collections.Generic;
using UnityEngine;

public class DartTint : MonoBehaviour
{
    [SerializeField] private Color tintColor = Color.green;
    
    
    private string colorProperty = "_BaseColor";
 
    private Renderer[] renderers;
    private Color[] baseColors;
    private MaterialPropertyBlock block;
    private int colorId;
 
    private void Awake()
    {
        colorId = Shader.PropertyToID(colorProperty);
        block = new MaterialPropertyBlock();
 
        var validRenderers = new List<Renderer>();
        var colors = new List<Color>();
        foreach (var dartRenderer in GetComponentsInChildren<Renderer>())
        {
            var material = dartRenderer.sharedMaterial;
            if (material == null || !material.HasProperty(colorId)) continue;
            validRenderers.Add(dartRenderer);
            colors.Add(material.GetColor(colorId));
        }
 
        renderers = validRenderers.ToArray();
        baseColors = colors.ToArray();
    }
 
    /// <summary>
    /// Blends the dart's color toward the tint color
    /// </summary>
    /// <param name="amount">0 = original color, 1 = full tint color</param>
    public void SetTint(float amount)
    {
        if (amount <= 0f)
        {
            ClearTint();
            return;
        }
 
        amount = Mathf.Clamp01(amount);
        for (var i = 0; i < renderers.Length; i++)
        {
            block.Clear();
            block.SetColor(colorId, Color.Lerp(baseColors[i], tintColor, amount));
            renderers[i].SetPropertyBlock(block);
        }
    }
 
    /// <summary>
    /// Removes the override and restores the material's own color
    /// </summary>
    public void ClearTint()
    {
        foreach (var dartRenderer in renderers)
            dartRenderer.SetPropertyBlock(null);
    }
}
