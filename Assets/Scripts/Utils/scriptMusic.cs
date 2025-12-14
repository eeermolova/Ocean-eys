using UnityEngine;
using UnityEngine.UI;

public class MusicToggleController : MonoBehaviour
{
    [Header("НАСТРОЙКИ МУЗЫКИ")]
    [Tooltip("Перетащите сюда AudioSource с музыкой")]
    [SerializeField] private AudioSource musicSource;

    [Tooltip("Перетащите сюда UI Toggle")]
    [SerializeField] private Toggle musicToggle;

    [Header("ВИЗУАЛЬНЫЕ НАСТРОЙКИ (ОПЦИОНАЛЬНО)")]
    [SerializeField] private Image toggleIcon;
    [SerializeField] private Sprite musicOnSprite;
    [SerializeField] private Sprite musicOffSprite;

    [SerializeField] private Text statusText;
    [SerializeField] private string musicOnText = "Музыка: ВКЛ";
    [SerializeField] private string musicOffText = "Музыка: ВЫКЛ";

    [Header("ОТЛАДКА")]
    [SerializeField] private bool showDebugLogs = true;

    // Ключ для сохранения
    private const string SAVE_KEY = "MusicEnabled";

    void Start()
    {
        InitializeMusicSystem();
    }

    // Инициализация системы музыки
    private void InitializeMusicSystem()
    {
        // Проверяем ссылки
        if (musicSource == null)
        {
            Debug.LogError("❌ MusicController: AudioSource не назначен!");
            return;
        }

        if (musicToggle == null)
        {
            Debug.LogError("❌ MusicController: Toggle не назначен!");
            return;
        }

        // Загружаем сохранённое состояние
        bool savedState = LoadMusicState();

        if (showDebugLogs)
        {
            Debug.Log($"🎵 Загружено состояние: {(savedState ? "ВКЛ" : "ВЫКЛ")}");
        }

        // Настраиваем Toggle
        SetupToggle(savedState);

        // Настраиваем музыку
        SetupMusic(savedState);

        // Настраиваем визуал
        UpdateVisuals(savedState);

        if (showDebugLogs)
        {
            Debug.Log("✅ Музыкальная система инициализирована");
        }
    }

    // Загрузка состояния из памяти
    private bool LoadMusicState()
    {
        // По умолчанию музыка включена (1)
        int savedValue = PlayerPrefs.GetInt(SAVE_KEY, 1);
        return savedValue == 1;
    }

    // Настройка Toggle
    private void SetupToggle(bool isMusicOn)
    {
        // Устанавливаем начальное состояние
        musicToggle.isOn = isMusicOn;

        // Удаляем старые подписчики (для безопасности)
        musicToggle.onValueChanged.RemoveAllListeners();

        // Подписываемся на изменение
        musicToggle.onValueChanged.AddListener(OnToggleValueChanged);

        if (showDebugLogs)
        {
            Debug.Log($"🎚 Toggle установлен: {(isMusicOn ? "ВКЛ" : "ВЫКЛ")}");
        }
    }

    // Настройка музыки
    private void SetupMusic(bool isMusicOn)
    {
        // Включаем/выключаем музыку
        musicSource.mute = !isMusicOn;

        // Если музыка должна быть включена, но не играет - запускаем
        if (isMusicOn && !musicSource.isPlaying)
        {
            musicSource.Play();
        }

        if (showDebugLogs)
        {
            Debug.Log($"🔊 Музыка: {(isMusicOn ? "играет" : "выключена")}");
        }
    }

    // Обновление визуальных элементов
    private void UpdateVisuals(bool isMusicOn)
    {
        // Обновляем иконку
        if (toggleIcon != null && musicOnSprite != null && musicOffSprite != null)
        {
            toggleIcon.sprite = isMusicOn ? musicOnSprite : musicOffSprite;
        }

        // Обновляем текст
        if (statusText != null)
        {
            statusText.text = isMusicOn ? musicOnText : musicOffText;
        }
    }

    // Вызывается при изменении Toggle
    public void OnToggleValueChanged(bool isOn)
    {
        if (showDebugLogs)
        {
            Debug.Log($"🔄 Toggle изменён: {(isOn ? "ВКЛ" : "ВЫКЛ")}");
        }

        // Обновляем музыку
        musicSource.mute = !isOn;

        // Обновляем визуал
        UpdateVisuals(isOn);

        // Сохраняем состояние
        SaveMusicState(isOn);

        // Дополнительно: если музыка выключена, но источник играет - останавливаем
        if (!isOn && musicSource.isPlaying)
        {
            musicSource.Stop();
        }
        // Если музыка включена, но источник не играет - запускаем
        else if (isOn && !musicSource.isPlaying)
        {
            musicSource.Play();
        }
    }

    // Сохранение состояния
    private void SaveMusicState(bool isMusicOn)
    {
        int saveValue = isMusicOn ? 1 : 0;
        PlayerPrefs.SetInt(SAVE_KEY, saveValue);
        PlayerPrefs.Save();

        if (showDebugLogs)
        {
            Debug.Log($"💾 Сохранено: {saveValue}");
        }
    }

    // ====== ДОПОЛНИТЕЛЬНЫЕ МЕТОДЫ ======

    // Включить музыку (можно вызывать из других скриптов)
    public void EnableMusic()
    {
        SetMusicState(true);
    }

    // Выключить музыку (можно вызывать из других скриптов)
    public void DisableMusic()
    {
        SetMusicState(false);
    }

    // Переключить музыку (можно вызывать из других скриптов)
    public void ToggleMusic()
    {
        if (musicToggle != null)
        {
            musicToggle.isOn = !musicToggle.isOn;
        }
    }

    // Установить состояние музыки
    private void SetMusicState(bool enable)
    {
        if (musicToggle != null)
        {
            musicToggle.isOn = enable;
        }
        else
        {
            // Если Toggle нет, меняем напрямую
            musicSource.mute = !enable;
            UpdateVisuals(enable);
            SaveMusicState(enable);
        }
    }

    // Получить текущее состояние
    public bool IsMusicEnabled()
    {
        return !musicSource.mute;
    }

    // ====== МЕТОДЫ ДЛЯ ОТЛАДКИ В РЕДАКТОРЕ ======

    [ContextMenu("Включить музыку")]
    private void DebugEnableMusic()
    {
        EnableMusic();
        Debug.Log("🧪 Отладка: Музыка включена");
    }

    [ContextMenu("Выключить музыку")]
    private void DebugDisableMusic()
    {
        DisableMusic();
        Debug.Log("🧪 Отладка: Музыка выключена");
    }

    [ContextMenu("Показать состояние")]
    private void DebugShowState()
    {
        bool isEnabled = IsMusicEnabled();
        int savedValue = PlayerPrefs.GetInt(SAVE_KEY, -1);

        Debug.Log($"📊 Текущее состояние: {(isEnabled ? "ВКЛ" : "ВЫКЛ")}");
        Debug.Log($"💾 Сохранённое значение: {savedValue}");
        Debug.Log($"🎵 Источник играет: {musicSource.isPlaying}");
        Debug.Log($"🔇 Mute: {musicSource.mute}");
    }

    [ContextMenu("Сбросить сохранения")]
    private void DebugResetSave()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        PlayerPrefs.Save();
        Debug.Log("🗑️ Сохранения сброшены");
    }

    // ====== ОЧИСТКА ПРИ УНИЧТОЖЕНИИ ======

    void OnDestroy()
    {
        // Отписываемся от события при уничтожении объекта
        if (musicToggle != null)
        {
            musicToggle.onValueChanged.RemoveListener(OnToggleValueChanged);

            if (showDebugLogs)
            {
                Debug.Log("🔓 Отписались от события Toggle");
            }
        }
    }
}