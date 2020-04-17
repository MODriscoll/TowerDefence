using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheesePulse : MonoBehaviour
{

    // Grow parameters
    [SerializeField] private float approachSpeedGrowth = 0.04f;
    [SerializeField] private float approachSpeedShrink = 0.02f;
    [SerializeField] private float growthBound = 1.5f;
    [SerializeField] private float shrinkBound = 0.5f;
    [SerializeField] private float currentRatio = 1;


    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine("Pulse");
    }

    IEnumerator Pulse()
    {
   
        // Get bigger for a few seconds
        while (this.currentRatio != this.growthBound)
        {
            // Determine the new ratio to use
            currentRatio = Mathf.MoveTowards(currentRatio, growthBound, approachSpeedGrowth);

            // Update our text element
            this.transform.localScale = Vector3.one * currentRatio;

            yield return new WaitForEndOfFrame();
        }

        // Shrink for a few seconds
        while (this.currentRatio != this.shrinkBound)
        {
            // Determine the new ratio to use
            currentRatio = Mathf.MoveTowards(currentRatio, shrinkBound, approachSpeedShrink);

            // Update our text element
            this.transform.localScale = Vector3.one * currentRatio;


            yield return new WaitForEndOfFrame();
        }
        
    }
}