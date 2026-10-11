using System;
using UnityEngine;

public class Swinger : MonoBehaviour
{
    public Transform Origin;
    public float RotationSpeed = 0.25f;
    public float RotationAngle = 1.5f;
    public float SwingSpeed = 0.25f;
    public float SwingAngle = 0.25f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 target = Origin.transform.position;
        target.x += Mathf.Sin(Time.time * RotationSpeed) * RotationAngle;
        target.z += Mathf.Cos(Time.time * RotationSpeed) * RotationAngle;
        target.y += (MathF.Sin(Time.time * SwingSpeed) * 0.5f - 0.5f) * SwingAngle;
        transform.position = target;
        //transform.RotateAround(Origin.position, Vector3.up, 1.5f);    
    }
}
