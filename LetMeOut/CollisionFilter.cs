using RoR2;
using UnityEngine;

namespace LetMeOut;

public class CollisionFilter : MonoBehaviour
{
    public MeshCollider Collider;

    void OnCollisionEnter(Collision collision)
    {
        var player = collision.gameObject.GetComponent<PlayerCharacterMasterController>();
        if (player == null)
        {
            Physics.IgnoreCollision(Collider, collision.collider);
        }
    }
}