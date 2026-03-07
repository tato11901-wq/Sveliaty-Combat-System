using UnityEngine;
using TMPro;
namespace SproutGames.PlayerPrefManager
{
    public class PPMDemo : MonoBehaviour
    {

        [SerializeField] TMP_InputField key_txt;
        [SerializeField] TMP_InputField value_txt;
        [SerializeField] TMP_Dropdown type_drp;

        public void OnSave()
        {
            switch (type_drp.value)
            {
                case 0:
                    {
                        int savedata_i;
                        if (int.TryParse(value_txt.text, out savedata_i))
                        {
                            PlayerPrefs.SetInt(key_txt.text, savedata_i);
                        }
                        else
                        {
                            Debug.LogError("Entered value in not an integer");
                            return;
                        }
                        break;
                    }
                case 1:
                    {
                        PlayerPrefs.SetString(key_txt.text, value_txt.text);
                        break;
                    }
                case 2:
                    {
                        float savedata_f;
                        if (float.TryParse(value_txt.text, out savedata_f))
                        {
                            PlayerPrefs.SetFloat(key_txt.text, savedata_f);
                        }
                        else
                        {
                            Debug.LogError("Entered value in not a float");
                            return;
                        }
                        break;
                    }
                default:
                    break;
            }

            Debug.Log("Playerpref saved.");
            Debug.Log("Open Playerpref Manager via \"Tools > SproutStudio > PlayerPrefs Manager\" \n New saved playerpref will be listed in left pane. click it to view and edit data ");
        }

    }
}
