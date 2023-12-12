using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

[System.Serializable]
public struct McmcData
{
    public Vector3 position;
    public float density;
};

public class McmcSampler : MonoBehaviour
{
    public const int MAX_RESET_COUNT = 128;
    [SerializeField] private ComputeShader _mcmcCs;
    [SerializeField] private float _stdDev;
    [SerializeField] private Texture2D _probability;
    [SerializeField] private Shader _debugQuadShader;
    [SerializeField] private bool _destroyChildObjects;

    private GraphicsBuffer _resultBuffer;
    private McmcData[] _mcmcDatas;
    private Material _debugMaterial;

    private void Awake()
    {
        var type = GraphicsBuffer.Target.Structured;
        _resultBuffer = new GraphicsBuffer(type, 1, Marshal.SizeOf(typeof(McmcData)));
        _debugMaterial = new Material(_debugQuadShader);
        _resultBuffer.SetData(new McmcData[1]);
    }
    
    private void Update()
    {
        Sample();
    }

    Vector2 GetStdDev()
    {
        float aspect = (float)_probability.width / _probability.height;
        return new Vector2(_stdDev, _stdDev / aspect);
    }

    void Sample()
    {
        var seed = Random.Range(0, 65536 - 1);
        var kernalIdx = _mcmcCs.FindKernel("Reset");
        var threadGroupsX = MAX_RESET_COUNT / 8;
        _mcmcCs.SetInt("_Seed", seed);
        _mcmcCs.SetFloat("_Height", 1);
        _mcmcCs.SetVector("_StddevAspect", GetStdDev());
        _mcmcCs.SetTexture(kernalIdx, "_ProbMap", _probability);
        _mcmcCs.SetBuffer(kernalIdx, "_ResultBuffer", _resultBuffer);
        _mcmcCs.Dispatch(kernalIdx, threadGroupsX, 1, 1);

        kernalIdx = _mcmcCs.FindKernel("Sequence");
        threadGroupsX = 1 / 1;
        _mcmcCs.SetTexture(kernalIdx, "_ProbMap", _probability);
        _mcmcCs.SetBuffer(kernalIdx, "_ResultBuffer", _resultBuffer);
        _mcmcCs.Dispatch(kernalIdx, threadGroupsX, 1, 1);

        AsyncReadback(_resultBuffer);

        if (_destroyChildObjects)
        {
            ObjectHelper.Clear(transform);
            _destroyChildObjects = !_destroyChildObjects;
        }
    }

    void AsyncReadback(GraphicsBuffer buffer)
    {
        AsyncGPUReadback.Request(buffer, request =>
        {
            OnCompleteReadBack(request);
        });
    }

    void OnCompleteReadBack(AsyncGPUReadbackRequest request)
    {
        if (request.hasError)
        {
            Debug.Log("TouchedBuffer readback error detected.");
        }
        else if (request.done)
        {
            if (!Application.isPlaying) return;
            _mcmcDatas = request.GetData<McmcData>().ToArray();
            SpawnCube(new Vector3(_mcmcDatas[0].position.x, 0, _mcmcDatas[0].position.y));
        }
    }

    void SpawnCube(Vector3 pos)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.position = pos;
        cube.transform.localScale = new Vector3(0.005f, 0.25f, 0.005f);
        cube.transform.SetParent(transform, false);
    }

    void GetDataAndSpawn()
    {
        var mcmcDatas = new McmcData[1];
        _resultBuffer.GetData(mcmcDatas);
        SpawnCube(new Vector3(mcmcDatas[0].position.x, 0, mcmcDatas[0].position.y));
    }

    private void OnDestroy()
    {
        FBOUtility.DeleteBuffer(_resultBuffer);
        FBOUtility.DeleteMaterial(_debugMaterial);
        ObjectHelper.Clear(transform);
    }
}