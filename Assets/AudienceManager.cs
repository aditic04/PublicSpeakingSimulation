using UnityEngine;

public class AudienceManager : MonoBehaviour
{
    public Animator[] audienceAnimators;

    // Audience sits idle
    public void StopClapping()
    {
        foreach (Animator anim in audienceAnimators)
        {
            anim.SetBool("isClapping", false);
        }
    }

    // Audience claps
    public void StartClapping()
    {
        foreach (Animator anim in audienceAnimators)
        {
            anim.SetBool("isClapping", true);
        }
    }
}