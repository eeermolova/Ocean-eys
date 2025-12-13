using UnityEngine;
using UnityEngine.UI;

public class FixButtonPosition : MonoBehaviour
{
    [ContextMenu("Fix All Buttons Position")]
    void FixButtons()
    {
        Button[] buttons = FindObjectsOfType<Button>(true);

        foreach (Button button in buttons)
        {
            // —брос позиции
            button.transform.position = Vector3.zero;
            button.transform.localPosition = Vector3.zero;

            // —брос масштаба
            button.transform.localScale = Vector3.one;

            // ƒл€ RectTransform кнопки в UI
            RectTransform rt = button.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(200, 80);
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
            }

            // ¬ключить кнопку
            button.gameObject.SetActive(true);

            Debug.Log($"»справлена кнопка: {button.name}");
        }
    }

    void Start()
    {
        // јвтоматически исправить при запуске
        FixButtons();
    }
}
