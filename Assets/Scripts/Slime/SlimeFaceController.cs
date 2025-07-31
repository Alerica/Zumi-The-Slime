using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class SlimeFaceController : MonoBehaviour
{
    public Renderer slimeRenderer;
    public string faceTextureProperty = "_facetex";

    public List<Texture> faceTextures = new List<Texture>();

    private Dictionary<string, Texture> faceDict = new Dictionary<string, Texture>();

    private void Awake()
    {
        foreach (var tex in faceTextures)
        {
            if (tex != null)
                faceDict[tex.name.ToLower()] = tex;
        }
    }

    private void Start()
    {
        // StartCoroutine(RandomRoutine()); // For Random face changes
    }

    public void SetFace(string faceName)
    {
        Debug.Log("Setting face to: " + faceName);
        if (faceDict.TryGetValue(faceName.ToLower(), out Texture faceTex))
        {
            slimeRenderer.material.SetTexture(faceTextureProperty, faceTex);
        }
        else
        {
            Debug.LogWarning($"Face '{faceName}' not found!");
        }
    }
    


    IEnumerator RandomRoutine()
    {
        while (true)
        {
            SetFace(faceTextures[Random.Range(0, faceTextures.Count)].name);
            yield return new WaitForSeconds(2f);
        }
    }
}
