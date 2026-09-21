using UnityEngine;

/// <summary>
/// 单例模式基类
/// </summary>
public abstract class SingleCase<T> : MonoBehaviour
    where T : SingleCase<T>
{
    private static T m_instance;

    public static T Instance
    {
        get
        {
            // 检查实例是否为 null 或已被销毁（Unity 的“假 null”）。
            if (m_instance == null || !m_instance)
            {
                m_instance = FindObjectOfType<T>();
            }
            return m_instance;
        }
    }

    /// <summary>
    /// 当前对象是否是已注册的唯一实例。
    /// 派生类如重写 Awake，调用 base.Awake() 后应在继续初始化前检查此值。
    /// </summary>
    protected bool IsPrimaryInstance => m_instance == this;

    /// <summary>
    /// 注册唯一实例。重复对象会在 Awake 阶段销毁，避免其 Start、OnEnable
    /// 或跨场景逻辑与已有服务并行运行。
    /// </summary>
    protected virtual void Awake()
    {
        if (m_instance != null && m_instance != this)
        {
            Debug.LogWarning($"[SingleCase<{typeof(T).Name}>] 检测到重复实例，销毁: {gameObject.name}", this);
            // Destroy 会在帧末执行；先禁用以避免重复对象在这一帧继续收到 OnEnable/Start。
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        m_instance = this as T;
    }

    public virtual void init() { }

    protected virtual void OnApplicationQuit()
    {
        if (m_instance == this)
            m_instance = null;
    }

    /// <summary>
    /// 对象销毁时仅清空指向自身的引用，不能影响后来注册的新实例。
    /// </summary>
    protected virtual void OnDestroy()
    {
        if (m_instance == this)
        {
            m_instance = null;
        }
    }
}
