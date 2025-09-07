using UnityEngine;
using UnityEngine.EventSystems;

public class AnimationManager : MonoBehaviour
{
    [SerializeField]
    public Animator animator;

    public void Spin()
    {
        if (animator)
        {
            Debug.Log("Spin");
            animator.SetTrigger("Spin");
        }
    }

    public void Squirt()
    {
        if (animator)
        {
            Debug.Log("Squirt");
            animator.SetTrigger("Straw_Back");
        }
    }

    public void Unsquirt()
    {
        if (animator)
        {
            Debug.Log("Unsquirt");
            animator.SetTrigger("Straw_Forward");
        }
    }

    public void Scoop() 
    {
        if (animator)
        {
            Debug.Log("Scoop");
            animator.SetTrigger("Scoop");
        }
    }

    public void Descoop() 
    {
        if (animator)
        {
            Debug.Log("Descoop");
            animator.SetTrigger("Descoop");
        }
    }

    public void Zipline(bool ziplineOrNot)
    {
        if (animator)
        {
            Debug.Log("Zipline state change: " + ziplineOrNot);
            animator.SetBool("Zipline", ziplineOrNot);
        }
    }
}
