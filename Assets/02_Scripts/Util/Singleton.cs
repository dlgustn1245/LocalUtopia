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
				instance = FindAnyObjectByType<T>();
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

#if UNITY_EDITOR
// 프로젝트 창 우클릭 > Create > Scripting > Singleton. 입력한 이름으로 Singleton<T> 를 상속한 스크립트를 만든다.
// 클래스명은 이름 편집이 끝난 뒤에야 알 수 있어서, 내장 스크립트 생성과 같은 EndNameEditAction 으로 파일을 쓴다.
static class SingletonScriptCreator
{
	const string Template =
@"using UnityEngine;

public class #NAME# : Singleton<#NAME#>
{

}
";

	[UnityEditor.MenuItem("Assets/Create/Scripting/Singleton", priority = 81)]
	static void Create()
	{
		var icon = UnityEditor.EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D;
		UnityEditor.ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
			default, ScriptableObject.CreateInstance<CreateAction>(), "NewSingleton.cs", icon, null);
	}

	class CreateAction : UnityEditor.ProjectWindowCallback.AssetCreationEndAction
	{
		public override void Action(EntityId instanceId, string pathName, string resourceFile)
		{
			string className = System.IO.Path.GetFileNameWithoutExtension(pathName).Replace(" ", "");
			System.IO.File.WriteAllText(pathName, Template.Replace("#NAME#", className));
			UnityEditor.AssetDatabase.ImportAsset(pathName);
			UnityEditor.ProjectWindowUtil.ShowCreatedAsset(UnityEditor.AssetDatabase.LoadAssetAtPath<Object>(pathName));
		}
	}
}
#endif
