using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheesePulse : MonoBehaviour
{
    // Grow parameters
    [SerializeField] private float approachSpeedGrowth = 0.08f;
    [SerializeField] private float approachSpeedShrink = 0.04f;
    [SerializeField] private float growthBound = 1.5f;
    [SerializeField] private float shrinkBound = 1f;
    [SerializeField] private float currentRatio = 1;


    private Vector3 m_originalScale = Vector3.one;      // Original scale in local space, recorded once in Awake()
    private Coroutine m_pulseRoutine = null;            // If valid, we are currently pulsing

    void Awake()
    {
        m_originalScale = transform.localScale;
    }

    /// <summary>
    /// Will have the cheese pulse once. If already pulsing, will reset
    /// </summary>
    public void pulseOnce()
    {
        if (m_pulseRoutine != null)
            StopCoroutine(m_pulseRoutine);

        m_pulseRoutine = StartCoroutine(pulseRoutine(1));
    }

    private IEnumerator pulseRoutine(int iterations)
    {
        this.transform.localScale = m_originalScale;

        // Notice we do a post-decrement here
        while (iterations-- > 0)
        {
            // Get bigger for a few seconds
            while (this.currentRatio != this.growthBound)
            {
                // Determine the new ratio to use
                currentRatio = Mathf.MoveTowards(currentRatio, growthBound, approachSpeedGrowth);

                this.transform.localScale = m_originalScale * currentRatio;

                yield return null;
            }

            // Shrink for a few seconds
            while (this.currentRatio != this.shrinkBound)
            {
                // Determine the new ratio to use
                currentRatio = Mathf.MoveTowards(currentRatio, shrinkBound, approachSpeedShrink);

                this.transform.localScale = m_originalScale * currentRatio;

                yield return null;
            }

            // Make sure we are fully back to where we were
            this.transform.localScale = m_originalScale;
        }

        m_pulseRoutine = null;
    }
}