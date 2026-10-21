using UnityEngine;

public class SoundManager : Singleton<SoundManager>
{
	readonly string bgmVolumeKey = "bgmVolume";
	readonly string sfxVolumeKey = "sfxVolume";
	readonly string muteKey = "mute";

	#region AudioClip
	public AudioClip menuBgm;
	public AudioClip endingBgm;

	public AudioClip selectSfx;
	public AudioClip territorySfx;
	public AudioClip completeSfx;
	public AudioClip failSfx;
	#endregion

	public float bgmVolume;
	public float sfxVolume;
	public bool isMuted;

	public AudioSource bgmSource;
	public AudioSource sfxSource;

	protected override void Awake()
	{
		base.Awake();

		if (Instance != this)
		{
			return;
		}

		bgmSource.loop = true;
		bgmSource.playOnAwake = false;

		sfxSource.loop = false;
		sfxSource.playOnAwake = false;

		bgmVolume = PlayerPrefs.GetFloat(bgmVolumeKey, 1.0f);
		sfxVolume = PlayerPrefs.GetFloat(sfxVolumeKey, 1.0f);
		isMuted = PlayerPrefs.GetInt(muteKey, 0) == 1;
		ApplyVolume();
	}

	public void PlayBGM(AudioClip audioClip)
	{
		if (bgmSource.clip == audioClip && bgmSource.isPlaying)
		{
			return;
		}

		bgmSource.clip = audioClip;
		bgmSource.Play();
	}

	public void StopBGM()
	{
		bgmSource.Stop();
	}

	public void PlaySFX(AudioClip audioClip)
	{
		sfxSource.PlayOneShot(audioClip);
	}

	// 슬라이더 드래그 중 매 프레임 불리므로 여기서는 Save 하지 않는다. 설정 팝업이 닫힐 때 GameSetting 이 저장한다.
	public void SetBgmVolume(float volume)
	{
		bgmVolume = volume;
		PlayerPrefs.SetFloat(bgmVolumeKey, volume);
		ApplyVolume();
	}

	public void SetSfxVolume(float volume)
	{
		sfxVolume = volume;
		PlayerPrefs.SetFloat(sfxVolumeKey, volume);
		ApplyVolume();
	}

	public void SetMute(bool muted)
	{
		isMuted = muted;
		PlayerPrefs.SetInt(muteKey, muted ? 1 : 0);
		PlayerPrefs.Save();
		ApplyVolume();
	}

	void ApplyVolume()
	{
		bgmSource.volume = bgmVolume;
		sfxSource.volume = sfxVolume;
		AudioListener.volume = isMuted ? 0f : 1f;
	}
}
