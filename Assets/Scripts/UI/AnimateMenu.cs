using UnityEngine;

public class AnimateMenu : MonoBehaviour
{
    [SerializeField]public Animator animator;

    public void OpenMenu()
    {
        animator.SetBool("MenuOpen", true);
        animator.SetBool("MenuClose", false);
    }

    public void StayOpenMenu()
    {
        animator.SetBool("StayOpen", true);
        animator.SetBool("MenuOpen", false);
    }

    public void CloseMenu()
    {
        animator.SetBool("MenuClose", true);
        animator.SetBool("MenuOpen", false);
        animator.SetBool("StayOpen", false);
    }
}
