using Mcmc;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SampleTest : MonoBehaviour
{
    public int   limitObjectCount = 10000;
    public float stdDev;
    public float sleepInterval = 0.1f;
    public int   nInitialize = 10;
    public int   nSamples = 5;
    public Texture2D probability;
    public bool destroyChildObjects;

    private MCMC _mcmc;
    private List<Transform> _objects;

    void Start()
    {
        _mcmc = new MCMC(probability, stdDev);
        _objects = new List<Transform>();
        StartCoroutine(Pinning(sleepInterval, nSamples));
    }
    void Update()
    {
        if(destroyChildObjects)
        {
            ObjectHelper.Clear(transform);
            destroyChildObjects = !destroyChildObjects;
        }
    }

    IEnumerator Pinning(float interval, int count)
    {
        while (true)
        {
            yield return new WaitForSeconds(interval);

            foreach (var uv in _mcmc.SequenceTex(nInitialize, count))
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.position = new Vector3(uv.x, 0, uv.y);
                cube.transform.localScale = new Vector3(0.005f, 0.25f, 0.005f);
                cube.transform.SetParent(transform, false);
            }

            if (_objects.Count >= limitObjectCount)
            {
                foreach (var p in _objects)
                    Destroy(p.gameObject);
                _objects.Clear();
                yield return new WaitForSeconds(2f);
            }
        }
    }
}