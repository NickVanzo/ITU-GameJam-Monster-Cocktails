using System;
using UnityEngine;

public class Swinger : MonoBehaviour
{
    public Transform Origin;
    public float RotationSpeed = 0.25f;
    public float RotationAngle = 1.5f;
    public float SwingSpeed = 0.25f;
    public float SwingAngle = 0.25f;

    void Start()
    {
        
    }

    void Update()
    {
        Vector3 target = Origin.transform.position;
        target.x += Mathf.Sin(Time.time * RotationSpeed) * RotationAngle;
        target.z += Mathf.Cos(Time.time * RotationSpeed) * RotationAngle;
        target.y += (MathF.Sin(Time.time * SwingSpeed) * 0.5f - 0.5f) * SwingAngle;
        transform.position = target;
    }
}
