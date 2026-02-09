using TMPro;
using TMPro.EditorUtilities;
using UnityEngine;

public class updateText : MonoBehaviour
{
    [SerializeField] TMP_Text text;

    public void updateTheText(string tx)
    {
        text.text = tx;
    }
}
