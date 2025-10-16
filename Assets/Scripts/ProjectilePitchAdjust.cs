using Unity.VisualScripting;
using UnityEngine;

public class ProjectilePitchAdjust : MonoBehaviour
{
    public Rigidbody rigid;

    // Update is called once per frame
    void Update()
    {
        if (rigid.linearVelocity.magnitude >= 0.1)
        {
            transform.rotation = Quaternion.LookRotation(rigid.linearVelocity) * Quaternion.Euler(-90, 0, 0);
        }
    }
}
