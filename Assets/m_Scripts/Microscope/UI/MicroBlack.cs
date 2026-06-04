using UnityEngine;

public class MicroBlack : MonoBehaviour
{
    public static bool ToBlack;
    public Animator animator;
    void Start()
    {
        animator = GetComponent<Animator>();
        ToBlack = false;
    }

    void Update()
    {
        if (ToBlack)
        {
            animator.SetTrigger("ToBlack");
            ToBlack = false;
        }
    }
}
