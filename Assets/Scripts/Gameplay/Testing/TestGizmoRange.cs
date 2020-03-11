using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestGizmoRange : MonoBehaviour
{
    [SerializeField] float rad = 2f;          //radius to be drawn around an object

    // Update is called once per frame
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, rad);
    }
}
