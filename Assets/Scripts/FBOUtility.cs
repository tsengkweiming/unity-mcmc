using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Collections;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

//FrameBufferObject Utility
public static class FBOUtility
{
    public static void CreateBuffer(ref RenderTexture rt, int w, int h, RenderTextureFormat format, FilterMode filter, string name = "", bool isDepth = false, bool useInComputeshader = false, int antiAlias = 1)
    {
        var depth = isDepth ? 24 : 0;
        rt = new RenderTexture(w, h, 0, format, RenderTextureReadWrite.Linear) { 
            name = name, 
            depth = depth,
            filterMode = filter,
            wrapMode = TextureWrapMode.Clamp,
            enableRandomWrite = useInComputeshader,
            antiAliasing = antiAlias
        };
        rt.Create();
    }

    public static void CreateBuffer(ref RenderTexture[] rt, int w, int h, RenderTextureFormat format, FilterMode filter)
    {
        rt = new RenderTexture[2];
        for (var i = 0; i < rt.Length; i++)
        {
            rt[i] = new RenderTexture(w, h, 0, format, RenderTextureReadWrite.Linear);
            rt[i].filterMode = filter;
            rt[i].wrapMode = TextureWrapMode.Clamp;
            rt[i].Create();
        }
    }

    public static void CreateBuffer(ref Texture2D tex, int w, int h, TextureFormat format, bool mipmap = false, bool linear = false)
    {
        Destroy(ref tex);
        tex = new Texture2D(w, h, format, mipmap, linear);
    }
    
    public static Texture2D CreateBuffer(int w, int h, TextureFormat format, bool mipmap = false, bool linear = false)
    {
        return new Texture2D(w, h, format, mipmap, linear);
    }

    public static void Destroy(ref Texture2D tex)
    {
        if (tex != null)
        {
            DestroySelf(tex);
            tex = null;
        }
    }

    public static void DestroySelf(Object obj, float t = 0f)
    {
        if (obj != null)
        {
            if (Application.isPlaying)
                Object.Destroy(obj, t);
            else
                Object.DestroyImmediate(obj);
        }
    }

    public static void DeleteBuffer(RenderTexture rt)
    {
        if (rt != null)
        {
            if (Application.isEditor)
                RenderTexture.DestroyImmediate(rt);
            else
                RenderTexture.Destroy(rt);
            rt = null;
        }
    }

    public static void DeleteBuffer(RenderTexture[] rt)
    {
        if (rt != null)
        {
            for (var i = 0; i < rt.Length; i++)
            {
                if (rt[i] != null)
                {
                    if (Application.isEditor)
                        RenderTexture.DestroyImmediate(rt[i]);
                    else
                        RenderTexture.Destroy(rt[i]);
                    rt[i] = null;
                }
            }
        }
    }

    public static void ClearBuffer(RenderTexture rt, Color? clearColor = null)
    {
        Color c = clearColor.HasValue ? clearColor.Value : new Color(0, 0, 0, 0);

        RenderTexture temp = RenderTexture.active;
        Graphics.SetRenderTarget(rt);
        GL.Clear(false, true, c);
        Graphics.SetRenderTarget(temp);
    }

    public static void Swap(RenderTexture[] rt)
    {
        RenderTexture temp = rt[0];
        rt[0] = rt[1];
        rt[1] = temp;
    }

    public static void CreateMaterial(ref Material material, Shader shader)
    {
        if (material == null)
        {
            material = new Material(shader);
            material.hideFlags = HideFlags.DontSave;
        }
    }

    public static void DeleteMaterial(Material material)
    {
        if (material != null)
        {
            if (Application.isEditor)
                Material.DestroyImmediate(material);
            else
                Material.Destroy(material);
            material = null;
        }
    }

    public static void DeleteBuffer(GraphicsBuffer buffer)
    {
        if (buffer != null)
        {
            buffer.Release();
            buffer = null;
        }
    }

    public static void DeleteBuffer(ComputeBuffer buffer)
    {
        if (buffer != null)
        {
            buffer.Release();
            buffer = null;
        }
    }

    /// <summary>
    /// �}�b�v��쐬����
    /// �𑜓x���ω����Ă���΁A�}�b�v��쐬���Ȃ���
    /// </summary>
    /// <param name="rt"></param>
    /// <param name="w"></param>
    /// <param name="h"></param>
    /// <param name="format"></param>
    /// <param name="filter"></param>
    /// <param name="wrap"></param>
    /// <param name="useInComputeshader"></param>
    public static void CreateOrReCreateMap(ref RenderTexture rt, int w, int h, RenderTextureFormat format, FilterMode filter, bool useInComputeshader = false, string name = "")
    {
        if (rt == null)
        {
            CreateBuffer(ref rt, w, h, format, filter, name, false, useInComputeshader);
            ClearBuffer(rt);
        }
        else
        {
            if (w != rt.width || h != rt.height)
            {
                DeleteBuffer(rt);
                CreateBuffer(ref rt, w, h, format, filter, name, false, useInComputeshader);
                ClearBuffer(rt);
            }
        }
    }

#if UNITY_EDITOR
    [MenuItem("Assets/Save RenderTexture to file")]
    public static void SaveSelectedRTToFile()
    {
        RenderTexture rt = Selection.activeObject as RenderTexture;

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        RenderTexture.active = null;

        byte[] bytes;
        bytes = tex.EncodeToPNG();

        string path = AssetDatabase.GetAssetPath(rt) + ".png";
        System.IO.File.WriteAllBytes(path, bytes);
        AssetDatabase.ImportAsset(path);
        Debug.Log("Saved to " + path);
    }

    public static void SaveFloatRenderTexToFile(RenderTexture rt, string name = default(string), TextureFormat texFormat = TextureFormat.RGBA32, string path = default(string))
    {
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(rt.width, rt.height, texFormat, false, true);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        RenderTexture.active = null;

        byte[] bytes;
        bytes = ImageConversion.EncodeToEXR(tex, Texture2D.EXRFlags.OutputAsFloat);

        if (name == default(string)) name = "rt";
        if (path == default(string)) path = Application.dataPath;
        string filePath = System.IO.Path.Combine(path, name);
        System.IO.File.WriteAllBytes(filePath, bytes);
        AssetDatabase.ImportAsset(filePath);
        Debug.Log("Saved to " + filePath);
    }

    public static void SaveRenderTexToPNG(RenderTexture rt, string name = default(string), TextureFormat texFormat = TextureFormat.RGBA32, string path = default(string))
    {
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(rt.width, rt.height, texFormat, false, true);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        RenderTexture.active = null;

        byte[] bytes;
        bytes = tex.EncodeToPNG();

        if (name == default(string)) name = "rt";
        if (path == default(string)) path = Application.dataPath;
        string filePath = System.IO.Path.Combine(path, name);
        System.IO.File.WriteAllBytes(filePath, bytes);
        AssetDatabase.ImportAsset(filePath);
        Debug.Log("Saved to " + filePath);
    }

    public static void SaveRenderTexToFile(RenderTexture rt, TextureCreationFlags flags, string name = default(string), GraphicsFormat texFormat = GraphicsFormat.R16G16B16A16_SFloat, bool isFloatTex = true, string path = default(string))
    {
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(rt.width, rt.height, texFormat, flags);

        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        RenderTexture.active = null;
        tex.Apply();

        byte[] bytes;
        if (isFloatTex)
            bytes = ImageConversion.EncodeToEXR(tex, Texture2D.EXRFlags.OutputAsFloat);
        else
            bytes = tex.EncodeToPNG();

        if (name == default(string)) name = "rt";
        if (path == default(string)) path = Application.dataPath;
        string filePath = System.IO.Path.Combine(path, name);
        System.IO.File.WriteAllBytes(filePath, bytes);
        AssetDatabase.ImportAsset(filePath);
        Debug.Log("Saved to " + filePath);
    }

    public enum SaveTextureFileFormat
    {
        PNG,
        EXR,
        JPG,
        TGA
    }

    static public void SaveTextureToFile(Texture source,
                                         string filePath,
                                         int width,
                                         int height,
                                         SaveTextureFileFormat fileFormat = SaveTextureFileFormat.PNG,
                                         int jpgQuality = 95,
                                         bool asynchronous = true,
                                         System.Action<bool> done = null)
    {
        // check that the input we're getting is something we can handle:
        if (!(source is Texture2D || source is RenderTexture))
        {
            done?.Invoke(false);
            return;
        }

        // use the original texture size in case the input is negative:
        if (width < 0 || height < 0)
        {
            width = source.width;
            height = source.height;
        }

        // resize the original image:
        var resizeRT = RenderTexture.GetTemporary(width, height, 0);
        Graphics.Blit(source, resizeRT);

        // create a native array to receive data from the GPU:
        var narray = new NativeArray<byte>(width * height * 4, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

        // request the texture data back from the GPU:
        var request = AsyncGPUReadback.RequestIntoNativeArray(ref narray, resizeRT, 0, (AsyncGPUReadbackRequest request) =>
        {
            // if the readback was successful, encode and write the results to disk
            if (!request.hasError)
            {
                NativeArray<byte> encoded;

                switch (fileFormat)
                {
                    case SaveTextureFileFormat.EXR:
                        encoded = ImageConversion.EncodeNativeArrayToEXR(narray, resizeRT.graphicsFormat, (uint)width, (uint)height);
                        break;
                    case SaveTextureFileFormat.JPG:
                        encoded = ImageConversion.EncodeNativeArrayToJPG(narray, resizeRT.graphicsFormat, (uint)width, (uint)height, 0, jpgQuality);
                        break;
                    case SaveTextureFileFormat.TGA:
                        encoded = ImageConversion.EncodeNativeArrayToTGA(narray, resizeRT.graphicsFormat, (uint)width, (uint)height);
                        break;
                    default:
                        encoded = ImageConversion.EncodeNativeArrayToPNG(narray, resizeRT.graphicsFormat, (uint)width, (uint)height);
                        break;
                }

                System.IO.File.WriteAllBytes(filePath, encoded.ToArray());
                encoded.Dispose();
            }

            narray.Dispose();

            // notify the user that the operation is done, and its outcome.
            done?.Invoke(!request.hasError);
        });

        if (!asynchronous)
            request.WaitForCompletion();
    }
#endif
}
