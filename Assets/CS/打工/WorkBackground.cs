using UnityEngine;

public class WorkBackground : MonoBehaviour
{
    public static WorkBackground Instance { get; private set; }
    
    public Vector3 originPosition;
    public float moveRange = 30f;
    public float moveSpeed = 0.5f;
    private float bgMoveTimer = 0f;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            originPosition = transform.position;
            Debug.Log("[WorkBackground] 单例初始化完成");
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Update()
    {
        bgMoveTimer += Time.deltaTime * moveSpeed;
        float moveProgress = Mathf.PingPong(bgMoveTimer, 1f);

        transform.position = Vector3.Lerp(
            originPosition,
            originPosition - new Vector3(moveRange, 0, 0),
            moveProgress
        );
    }

    private void OnDestroy()
    {
        // 清空静态变量，避免指向已销毁的对象
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
