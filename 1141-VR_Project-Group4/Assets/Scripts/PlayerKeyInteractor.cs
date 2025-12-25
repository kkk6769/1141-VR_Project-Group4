using UnityEngine;

// 在玩家上挂载该脚本：按 E 从屏幕中心（相机正前方）射线检测钥匙并拾取。
[DisallowMultipleComponent]
public class PlayerKeyInteractor : MonoBehaviour
{
    public Camera playerCamera;
    public float interactDistance = 3.0f;
    public KeyCode pickupKey = KeyCode.E;
    public LayerMask pickupLayer = ~0; // 默认所有层

    void Awake()
    {
        if (playerCamera == null)
        {
            if (Camera.main != null) playerCamera = Camera.main;
            else playerCamera = GetComponentInChildren<Camera>();
        }
    }

    void Update()
    {
        if (!Input.GetKeyDown(pickupKey)) return;
        if (playerCamera == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, interactDistance, pickupLayer))
        {
            // 支持拾取 lv1-key、lv2-key-1、lv2-key-2
            bool isKey = hit.collider.CompareTag("lv1-key") || hit.collider.CompareTag("lv2-key-1") || hit.collider.CompareTag("lv2-key-2");
            if (!isKey) return;

            // 优先调用KeyPickup组件
            KeyPickup kp = hit.collider.GetComponent<KeyPickup>();
            if (kp == null) kp = hit.collider.GetComponentInParent<KeyPickup>();
            if (kp != null)
            {
                kp.Pickup();
                return;
            }

            // 如果没有KeyPickup，直接走最小流程：更新钥匙状态、隐藏对应提示并销毁对象
            if (hit.collider.CompareTag("lv2-key-1"))
            {
                KeyInventory.CollectLv2Key1();
                var looks = GameObject.FindGameObjectsWithTag("lv2-look-1");
                for (int i = 0; i < looks.Length; i++) if (looks[i] != null) looks[i].SetActive(false);
            }
            else if (hit.collider.CompareTag("lv2-key-2"))
            {
                KeyInventory.CollectLv2Key2();
                var looks = GameObject.FindGameObjectsWithTag("lv2-look-2");
                for (int i = 0; i < looks.Length; i++) if (looks[i] != null) looks[i].SetActive(false);
            }
            else
            {
                KeyInventory.CollectLv1Key();
                var looks = GameObject.FindGameObjectsWithTag("lv1-look");
                for (int i = 0; i < looks.Length; i++) if (looks[i] != null) looks[i].SetActive(false);
            }
            Object.Destroy(hit.collider.gameObject);
        }
    }
}
