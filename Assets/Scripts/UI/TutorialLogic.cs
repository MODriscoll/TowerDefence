using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialLogic : MonoBehaviour
{
    public TextMeshProUGUI m_pageNumberText;
    private int m_pageNumber = 1;

    public Sprite[] m_tutorialImages;
    public Image m_displayImage;

    private void Awake()
    {
        UpdateDisplayImage();
    }

    public void NextPage()
    {
        if (m_pageNumber < m_tutorialImages.Length)
        {
            m_pageNumber++;
            m_pageNumberText.text = m_pageNumber + " / " + m_tutorialImages.Length;
            UpdateDisplayImage();
        }
    }

    public void PreviousPage()
    {
        if (m_pageNumber > 1)
        {
            m_pageNumber--;
            m_pageNumberText.text = m_pageNumber + " / " + m_tutorialImages.Length;
            UpdateDisplayImage();
        }
    }

    public void UpdateDisplayImage()
    {
        m_displayImage.sprite = m_tutorialImages[m_pageNumber - 1];
    }
}
