using System;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.Win32;

namespace SproutGames.PlayerPrefManager
{
    public class PPMPrefDataHandler
    {
        private List<PrefData> m_loadedPrefData;
        private List<PrefData> m_filteredPrefData;

        public PPMPrefDataHandler()
        {
            m_loadedPrefData = new List<PrefData>();
            m_filteredPrefData = new List<PrefData>();
            FetchAllPlayerPrefs();
        }

        private void FetchAllPlayerPrefs()
        {
            string company = Application.companyName;
            string product = Application.productName;

            string registryPath = $@"Software\Unity\UnityEditor\{company}\{product}";

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(registryPath))
            {
                if (key == null)
                {
                    Debug.LogWarning($"No PlayerPrefs found at: {registryPath}");
                    return;
                }

                foreach (var valueName in key.GetValueNames())
                {
                    PrefData prefData = new PrefData();
                    object value = key.GetValue(valueName);
                    prefData.keyname = valueName.Substring(0, valueName.IndexOf("_"));

                    bool valueFound = false;
                    switch (value)
                    {
                        case System.Int32 i:
                            prefData.keyType = typeof(int);
                            prefData.value = PlayerPrefs.GetInt(prefData.keyname);
                            valueFound = true;
                            break;
                        case System.Int64 f:
                            prefData.keyType = typeof(float);
                            prefData.value = PlayerPrefs.GetFloat(prefData.keyname);
                            valueFound = true;
                            break;
                        case System.Byte[] s:
                            prefData.keyType = typeof(string);
                            prefData.value = PlayerPrefs.GetString(prefData.keyname);
                            valueFound = true;
                            break;
                        case null:
                            Debug.Log("It's null");
                            break;
                        default:
                            Debug.Log("Unknown type: " + value.GetType());
                            break;
                    }

                    if (valueFound) m_loadedPrefData.Add(prefData);

                }
            }
            ApplyFilterPref("", "All");
        }

        public void SavePrefData(int m_SelectedIndex, string m_PrefValue)
        {
            PrefData prefData = m_filteredPrefData[m_SelectedIndex];
            Type type = prefData.keyType;
            if (type == typeof(int))
            {
                int savedata_i;
                if (int.TryParse(m_PrefValue, out savedata_i))
                {
                    PlayerPrefs.SetInt(prefData.keyname, savedata_i);
                    prefData.value = savedata_i;
                }
                else
                {
                    Debug.LogError("Entered value in not an integer");
                    return;
                }
            }
            if (type == typeof(float))
            {
                float savedata_f;
                if (float.TryParse(m_PrefValue, out savedata_f))
                {
                    PlayerPrefs.SetFloat(prefData.keyname, savedata_f);
                    prefData.value = savedata_f;
                }
                else
                {
                    Debug.LogError("Entered value in not a float");
                    return;
                }
            }
            if (type == typeof(string))
            {
                string savedata_s = m_PrefValue;
                PlayerPrefs.SetString(prefData.keyname, savedata_s);
                prefData.value = savedata_s;
            }

            int index = m_loadedPrefData.FindIndex(e => e.keyname == prefData.keyname);
            m_loadedPrefData[index] = prefData;
            m_filteredPrefData.Clear();
            m_filteredPrefData.AddRange(m_loadedPrefData);
        }

        public void ApplyFilterPref(string m_FilterSearchKey, string m_FilterTypeKey)
        {
            m_filteredPrefData.Clear();

            if (m_FilterSearchKey == "")
            {
                m_filteredPrefData.AddRange(m_loadedPrefData);
            }
            else
            {
                m_filteredPrefData.AddRange(m_loadedPrefData.FindAll(e => e.keyname.Contains(m_FilterSearchKey)));
            }

            List<PrefData> tempPrefOfType = new List<PrefData>();
            switch (m_FilterTypeKey)
            {
                case "All":
                    tempPrefOfType.AddRange(m_filteredPrefData);
                    break;
                case "int":
                    tempPrefOfType.AddRange(m_filteredPrefData.FindAll(e => e.keyType == typeof(int)));
                    break;
                case "float":
                    tempPrefOfType.AddRange(m_filteredPrefData.FindAll(e => e.keyType == typeof(float)));
                    break;
                case "string":
                    tempPrefOfType.AddRange(m_filteredPrefData.FindAll(e => e.keyType == typeof(string)));
                    break;
                default:
                    break;
            }

            m_filteredPrefData.Clear();
            m_filteredPrefData.AddRange(tempPrefOfType);
        }

        public List<PrefData> GetFilteredPrefData()
        {
            return m_filteredPrefData;
        }

        public void AddNewPlayerPref(string m_keyname, string m_type, string m_value)
        {
            PrefData newPrefData = new PrefData();
            newPrefData.keyname = m_keyname;

            switch (m_type)
            {
                case "int":
                    int savedata_i;
                    if (int.TryParse(m_value, out savedata_i))
                    {
                        PlayerPrefs.SetInt(m_keyname, savedata_i);
                        newPrefData.keyType = typeof(int);
                        newPrefData.value = savedata_i;
                    }
                    else
                    {
                        Debug.LogError("Entered value in not an integer");
                        return;
                    }
                    break;
                case "float":
                    float savedata_f;
                    if (float.TryParse(m_value, out savedata_f))
                    {
                        PlayerPrefs.SetFloat(m_keyname, savedata_f);
                        newPrefData.keyType = typeof(float);
                        newPrefData.value = savedata_f;
                    }
                    else
                    {
                        Debug.LogError("Entered value in not an integer");
                        return;
                    }
                    break;
                case "string":
                    string savedata_s = m_value;
                    PlayerPrefs.SetString(m_keyname, savedata_s);
                    newPrefData.keyType = typeof(string);
                    newPrefData.value = savedata_s;
                    break;
                default:
                    Debug.LogError("Unidentified option selected");
                    return;
            }

            m_loadedPrefData.Add(newPrefData);
            m_filteredPrefData.Clear();
            m_filteredPrefData.AddRange(m_loadedPrefData);

        }

        public void ClearAllPlayerPrefs()
        {
            PlayerPrefs.DeleteAll();
            m_filteredPrefData.Clear();
            m_loadedPrefData.Clear();
            FetchAllPlayerPrefs();
        }

        public void DeletePlayerPref(int m_selectedindex)
        {
            string _keyname = m_loadedPrefData[m_selectedindex].keyname;
            m_filteredPrefData.RemoveAt(m_selectedindex);
            m_loadedPrefData.RemoveAll(e => e.keyname == _keyname);
            PlayerPrefs.DeleteKey(_keyname);
        }

    }
}
