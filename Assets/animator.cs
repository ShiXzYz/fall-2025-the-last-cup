using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class animator : MonoBehaviour
{
    [SerializeField] public List<Material> fire_materials;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    int fireIter = 0;
    Stopwatch stopwatch;
    List<MeshRenderer> rends;

    void Start()
    {
        stopwatch = new Stopwatch();
        rends = new List<MeshRenderer>();
        stopwatch.Start();
        for (int i = 0; i < transform.Find("fire").childCount; i++) {
            rends.Add(transform.Find("fire").GetChild(i).gameObject.GetComponent<MeshRenderer>());
            //Debug.Log("aa");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (stopwatch.ElapsedMilliseconds > 50) {
            for (int e = 0; e < transform.Find("fire").childCount; e++) {
                rends[e].material = fire_materials[fireIter % fire_materials.Count];
            }
            fireIter += 1;
            stopwatch = new Stopwatch();
            stopwatch.Start();
        }
    }
}
