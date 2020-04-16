using System.Collections;
using System.Collections.Generic;
using UnityEngine;

    // This script is for writing functions related to creating effect events using the camera.

public class CameraEffectsController : MonoBehaviour
{

    public static CameraEffectsController instance;         // Singleton access to this script

    private float cameraShakePower, cameraShakeLength, cameraShakeFade, shakeRotation, shakeRotationMultiplier;


    // Start is called before the first frame update
    void Start()
    {
        instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        // Set camera position in update so that shake does not permenantly displace camera
        transform.localPosition = new Vector3(0, 0, 0);
    }

    // LateUpdate is called after Update
    private void LateUpdate()
    {
        if(cameraShakeLength > 0)
        {
            cameraShakeLength -= Time.deltaTime;

            float x = Random.Range(-1f, 1f) * cameraShakePower;
            float y = Random.Range(-1f, 1f) * cameraShakePower;

            transform.position += new Vector3(x, y, 0);

            // Multipliers returning values to zero over time to fade the shake effect
            cameraShakePower = Mathf.MoveTowards(cameraShakePower, 0, cameraShakeFade * Time.deltaTime);
            shakeRotation = Mathf.MoveTowards(shakeRotation, 0, cameraShakeFade * shakeRotationMultiplier * Time.deltaTime);
        }

        // Do a rotation shake based on a multiplier
        transform.rotation = Quaternion.Euler(0, 0, shakeRotation * Random.Range(-1f, 1f));

    }

    public void CameraShake(float power, float length, float rotation)
    {
        cameraShakePower = power;
        cameraShakeLength = length;
        shakeRotationMultiplier = rotation;

        // Math to control shake intensity
        cameraShakeFade = power / length;
        shakeRotation = power * rotation;
    }
}
