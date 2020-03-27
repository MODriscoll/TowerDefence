using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(InputField))]
public class PlayerPrefInputField : MonoBehaviour
{
    [SerializeField] protected string m_prefKey;        // The key to use for storing input in PlayerPrefs

    private InputField m_inputField;    // Field for inputting name

    public string PrefKey { get { return m_prefKey; } }     // Key used for player preferences

    void Start()
    {
        m_inputField = GetComponent<InputField>();
        if (m_inputField)
        {
            m_inputField.text = getKeyValue(false);
            onKeyValueSet(m_inputField.text);
        }
    }

    /// <summary>
    /// Sets the value for player pref key that has been set
    /// </summary>
    /// <param name="value">Value to set</param>
    public void setKeyValue(string value)
    {
        if (isValidKey() && isValidValue(value))
        {
            PlayerPrefs.SetString(m_prefKey, value);
            onKeyValueSet(value);
        }
    }

    /// <summary>
    /// Get the current value of the player prefs key
    /// </summary>
    /// <param name="alreadyExists">If key must already exist in order to get value</param>
    /// <returns>Value under key</returns>
    public string getKeyValue(bool alreadyExists)
    {
        if (isValidKey(alreadyExists))
            return PlayerPrefs.GetString(m_prefKey, "");

        return "InvalidKey";
    }

    /// <summary>
    /// Event is called when value has been updated
    /// </summary>
    /// <param name="value">Value that was set</param>
    protected virtual void onKeyValueSet(string value)
    {
        // Do nothing by default
    }

    /// <summary>
    /// Called before setting the value. Should check if value is valid for key
    /// </summary>
    /// <param name="value">Value to check</param>
    /// <returns>If value is valid</returns>
    public virtual bool isValidValue(string value)
    {
        return true;
    }

    private bool isValidKey(bool alreadyExists = false)
    {
        bool isValid = !string.IsNullOrEmpty(m_prefKey);
        isValid |= !alreadyExists || PlayerPrefs.HasKey(m_prefKey);
        return isValid;
    }
}
