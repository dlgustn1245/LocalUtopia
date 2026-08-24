using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SceneFader : MonoBehaviour
{
	public Graphic graphic;
	public float fadeInTime = 1f;
	void Awake()
	{
		if (graphic == null)
		{
			graphic = GetComponent<Graphic>();
		}
	}

	public void Start()
	{
		StartCoroutine(SetRayCastDisable());
		Color color = graphic.color;
		color.a = 1f;
		graphic.color = color;
		
		FadeIn();
	}

	IEnumerator SetRayCastDisable()
	{
		graphic.raycastTarget = true;
		yield return new WaitForSeconds(0.5f);
		graphic.raycastTarget = false;
	}

	void FadeIn(float startAlpha = 1f, float endAlpha = 0f)
	{
		graphic.enabled = true;
		graphic.canvasRenderer.SetAlpha(startAlpha);
		graphic.CrossFadeAlpha(endAlpha, fadeInTime, false);
	}
}