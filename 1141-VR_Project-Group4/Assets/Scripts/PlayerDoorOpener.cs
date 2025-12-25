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
                // 支持 door、door1、door2 标签
                if (!(hits[i].CompareTag("door") || hits[i].CompareTag("door1") || hits[i].CompareTag("door2"))) continue;
                // 优先寻找同层或父层的Door组件
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
            // 若是上锁门，走 TryToggle；否则正常 Toggle
            LockedDoor maybeLocked = closestDoor.GetComponent<LockedDoor>();
            if (maybeLocked != null)
            {
                maybeLocked.TryToggle();
            }
            else
            {
                if (autoDirectionOnInteract) closestDoor.ToggleAuto(origin);
                else closestDoor.Toggle();
            }
        }

        if (Input.GetKeyDown(openOutwardKey))
        {
            FindClosestDoor();
            if (closestDoor == null) return;
            LockedDoor maybeLocked = closestDoor.GetComponent<LockedDoor>();
            if (maybeLocked != null) maybeLocked.TryToggle();
            else closestDoor.ToggleWithDirection(Door.SwingDirection.Outward, origin);
        }

        if (Input.GetKeyDown(openInwardKey))
        {
            FindClosestDoor();
            if (closestDoor == null) return;
            LockedDoor maybeLocked = closestDoor.GetComponent<LockedDoor>();
            if (maybeLocked != null) maybeLocked.TryToggle();
            else closestDoor.ToggleWithDirection(Door.SwingDirection.Inward, origin);
        }
    }

    void OnDrawGizmosSelected()
    {
        Transform o = origin != null ? origin : transform;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(o.position, interactDistance);
    }
}
