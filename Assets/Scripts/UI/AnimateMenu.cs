using UnityEngine;

public class AnimateMenu : MonoBehaviour
{
    [SerializeField]public Animator animator;

    public void OpenMenu()
    {
        animator.SetBool("MenuOpen", true);
        animator.SetBool("MenuClose", false);
    }

    public void CloseMenu()
    {
        animator.SetBool("MenuClose", true);
        animator.SetBool("MenuOpen", false);
    }
}
