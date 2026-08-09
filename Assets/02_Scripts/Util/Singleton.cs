using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
	static T instance;

	public static T Instance
	{
		get
		{
			if (instance is null)
			{
				instance = FindFirstObjectByType<T>();
				if (instance is not null)
				{
					DontDestroyOnLoad(instance.gameObject);
				}
				else
				{
					return null;
				}
			}
			return instance;
		}
	}

	protected virtual void Awake()
	{
		if (instance != null && instance != this)
		{
			Destroy(gameObject);
			return;
		}

		instance = this as T;
		DontDestroyOnLoad(gameObject);
	}
}