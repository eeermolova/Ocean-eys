using UnityEngine;
using UnityEngine.UI;

public class VolumeController : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private AudioSource audioSource; // опционально
    [SerializeField] private string volumeKey = "MasterVolume";

    [Header("Визуализация")]
    [SerializeField] private Text volumeText;
    [SerializeField] private Image volumeIcon;
    [SerializeField] private Sprite[] volumeSprites; // 0: mute, 1: low, 2: medium, 3: high

    void Start()
    {
        InitializeSlider();
    }

    void InitializeSlider()
    {
        if (volumeSlider == null)
            volumeSlider = GetComponent<Slider>();

        // Загружаем сохраненное значение или ставим 1.0
        float savedVolume = PlayerPrefs.GetFloat(volumeKey, 1f);

        // Устанавливаем значение слайдера
        volumeSlider.value = savedVolume;

        // Применяем громкость
        ApplyVolume(savedVolume);

        // Добавляем слушатель
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
    }

    public void OnVolumeChanged(float value)
    {
        ApplyVolume(value);
        UpdateVisuals(value);
        SaveVolume(value);
    }

    void ApplyVolume(float volume)
    {
        // Глобальная громкость для всех звуков
        AudioListener.volume = volume;

        // Или для конкретного AudioSource
        if (audioSource != null)
            audioSource.volume = volume;
    }

    void UpdateVisuals(float volume)
    {
        // Обновляем текст
        if (volumeText != null)
            volumeText.text = Mathf.RoundToInt(volume * 100) + "%";

        // Обновляем иконку
        if (volumeIcon != null && volumeSprites != null && volumeSprites.Length >= 4)
        {
            if (volume == 0)
                volumeIcon.sprite = volumeSprites[0]; // Mute
            else if (volume < 0.33f)
                volumeIcon.sprite = volumeSprites[1]; // Low
            else if (volume < 0.66f)
                volumeIcon.sprite = volumeSprites[2]; // Medium
            else
                volumeIcon.sprite = volumeSprites[3]; // High
        }
    }

    void SaveVolume(float volume)
    {
        PlayerPrefs.SetFloat(volumeKey, volume);
        PlayerPrefs.Save();
    }

    // Дополнительные методы управления
    public void Mute()
    {
        volumeSlider.value = 0;
    }

    public void SetMaxVolume()
    {
        volumeSlider.value = 1;
    }

    public void IncreaseVolume(float amount = 0.1f)
    {
        volumeSlider.value = Mathf.Clamp01(volumeSlider.value + amount);
    }

    public void DecreaseVolume(float amount = 0.1f)
    {
        volumeSlider.value = Mathf.Clamp01(volumeSlider.value - amount);
    }

    void OnDestroy()
    {
        // Отписываемся от события
        if (volumeSlider != null)
            volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
    }
}