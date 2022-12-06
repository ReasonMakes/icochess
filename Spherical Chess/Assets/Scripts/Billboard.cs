using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Billboard : MonoBehaviour
{
    void Update()
    {
        //transform.LookAt(Camera.main.transform.position, -Vector3.up);

        Vector3 cameraRelativeToBillboard = transform.position - Camera.main.transform.position;
        transform.localRotation = Quaternion.LookRotation(cameraRelativeToBillboard);
    }
}