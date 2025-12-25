using UnityEngine;

public class PlayerDoorOpener : MonoBehaviour
{
    public float interactDistance = 2f;
    public KeyCode interactKey = KeyCode.E;
    public KeyCode openOutwardKey = KeyCode.F;
    public KeyCode openInwardKey = KeyCode.R;
    public bool autoDirectionOnInteract = false; // E键自动根据玩家所在侧选择方向
    public LayerMask doorLayer = ~0; // 默认所有层
    public Transform origin;

    void Awake()
    {
        if (origin == null) origin = transform;
    }

    void Update()
    {
        Collider[] hits = null;
        Door closestDoor = null;
        float closestSqr = float.MaxValue;

        void FindClosestDoor()
        {
            hits = Physics.OverlapSphere(origin.position, interactDistance, doorLayer);
            closestDoor = null;
            closestSqr = float.MaxValue;
            for (int i = 0; i < hits.Length; i++)
            {
                if (!hits[i].CompareTag("door")) continue;
                Door d = hits[i].GetComponentInParent<Door>();
                if (d == null) d = hits[i].GetComponent<Door>();
                if (d == null) continue;
                float sqr = (hits[i].transform.position - origin.position).sqrMagnitude;
                if (sqr < closestSqr)
                {
                    closestSqr = sqr;
                    closestDoor = d;
                }
            }
        }

        if (Input.GetKeyDown(interactKey))
        {
            FindClosestDoor();
            if (closestDoor == null) return;
            if (autoDirectionOnInteract) closestDoor.ToggleAuto(origin);
            else closestDoor.Toggle();
        }

        if (Input.GetKeyDown(openOutwardKey))
        {
            FindClosestDoor();
            if (closestDoor == null) return;
            closestDoor.ToggleWithDirection(Door.SwingDirection.Outward, origin);
        }

        if (Input.GetKeyDown(openInwardKey))
        {
            FindClosestDoor();
            if (closestDoor == null) return;
            closestDoor.ToggleWithDirection(Door.SwingDirection.Inward, origin);
        }
    }

    void OnDrawGizmosSelected()
    {
        Transform o = origin != null ? origin : transform;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(o.position, interactDistance);
    }
}
