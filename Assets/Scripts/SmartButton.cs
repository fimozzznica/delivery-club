using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Bhaptics.SDK2; // если не используешь bhaptics, закомментируй вызовы

public enum HandSide { Left, Right, Unknown }

[RequireComponent(typeof(Button))]
public class SmartButtonUniversal : MonoBehaviour, IPointerClickHandler
{
    [Header("Haptics")]
    public string leftEffect = "left_select";
    public string rightEffect = "right_select";
    public bool debug = true;

    // Последняя обнаруженная рука (обновляется при кликах)
    HandSide lastHand = HandSide.Unknown;

    // --- IPointerClickHandler ---
    public void OnPointerClick(PointerEventData eventData)
    {
        try
        {
            // 1) Сначала пробуем получить interactor через XRUIInputModule (reflection)
            Component interactorComponent = TryGetInteractorComponentFromXRUI(eventData.pointerId);
            if (interactorComponent != null)
            {
                if (debug) Debug.Log("[SmartButtonUniversal] Got interactor component: " + DescribeTransform(interactorComponent.transform));
                lastHand = DetermineHandFromTransform(interactorComponent.transform);
            }
            else
            {
                // 2) Фолбэк: используем pointerPressRaycast.gameObject (UI hit)
                if (eventData.pointerPressRaycast.gameObject != null)
                {
                    if (debug) Debug.Log("[SmartButtonUniversal] XRUI GetInteractor failed - using pointerPressRaycast.gameObject: " + eventData.pointerPressRaycast.gameObject.name);
                    lastHand = DetermineHandFromTransform(eventData.pointerPressRaycast.gameObject.transform);
                }
                else
                {
                    lastHand = HandSide.Unknown;
                }
            }

            // Лог и воспроизведение хаптика
            if (lastHand == HandSide.Left)
            {
                if (debug) Debug.Log("[SmartButtonUniversal] Playing LEFT haptic");
                PlayHaptic(leftEffect);
            }
            else if (lastHand == HandSide.Right)
            {
                if (debug) Debug.Log("[SmartButtonUniversal] Playing RIGHT haptic");
                PlayHaptic(rightEffect);
            }
            else
            {
                if (debug) Debug.LogWarning("[SmartButtonUniversal] Hand could not be determined -> no haptic");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[SmartButtonUniversal] Exception in OnPointerClick: " + e);
        }
    }

    // --- Попытаться получить Component интерактора через XRUIInputModule.GetInteractor(pointerId)
    Component TryGetInteractorComponentFromXRUI(int pointerId)
    {
        var es = EventSystem.current;
        if (es == null)
        {
            if (debug) Debug.LogWarning("[SmartButtonUniversal] EventSystem.current is null");
            return null;
        }

        // Ищем компонент на EventSystem с именем "XRUIInputModule" (может быть в пространстве имён UnityEngine.XR.Interaction.Toolkit.UI)
        Component module = null;
        var comps = es.GetComponents<Component>();
        foreach (var c in comps)
        {
            if (c == null) continue;
            var tname = c.GetType().Name;
            if (tname == "XRUIInputModule" || tname.Contains("XRUIInputModule"))
            {
                module = c;
                break;
            }
        }

        if (module == null)
        {
            if (debug) Debug.Log("[SmartButtonUniversal] XRUIInputModule component not found on EventSystem.");
            return null;
        }

        // Попробуем найти метод GetInteractor(int) через reflection
        var modType = module.GetType();
        MethodInfo getInteractorMethod = modType.GetMethod("GetInteractor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(int) }, null);

        if (getInteractorMethod == null)
        {
            if (debug) Debug.Log("[SmartButtonUniversal] XRUIInputModule.GetInteractor(int) not found via reflection.");
            return null;
        }

        try
        {
            object interactorObj = getInteractorMethod.Invoke(module, new object[] { pointerId });
            if (interactorObj == null)
            {
                if (debug) Debug.Log("[SmartButtonUniversal] GetInteractor returned null for pointerId " + pointerId);
                return null;
            }

            // Обычно возвращаемый объект реализует IUIInteractor и при этом является Component (реализация в Unity)
            if (interactorObj is Component comp)
                return comp;

            // Если не Component, пробуем смотреть на свойства (в редком случае)
            var asComponentProp = interactorObj.GetType().GetProperty("gameObject", BindingFlags.Instance | BindingFlags.Public);
            if (asComponentProp != null)
            {
                object goObj = asComponentProp.GetValue(interactorObj);
                if (goObj is GameObject go)
                {
                    return go.transform as Component;
                }
            }

            if (debug) Debug.Log("[SmartButtonUniversal] Interactor object is not a Component: " + interactorObj.GetType().FullName);
            return null;
        }
        catch (Exception ex)
        {
            if (debug) Debug.LogWarning("[SmartButtonUniversal] Exception invoking GetInteractor: " + ex.Message);
            return null;
        }
    }

    // Определяем руку, просматривая вверх по трансформам (ищем ControllerHand или 'left'/'right' в именах)
    HandSide DetermineHandFromTransform(Transform t)
    {
        if (t == null) return HandSide.Unknown;

        // Если на одном из родителей есть компонент ControllerHand — используем его
        const int maxDepth = 30;
        int depth = 0;
        Transform cur = t;
        while (cur != null && depth < maxDepth)
        {
            // 1) Ищем компонент с именем "ControllerHand" (если у тебя есть такой)
            var comp = cur.GetComponent("ControllerHand");
            if (comp != null)
            {
                // пробуем получить поле или свойство 'hand' или 'isLeft' через reflection
                var compType = comp.GetType();
                var handField = compType.GetField("hand", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (handField != null)
                {
                    object val = handField.GetValue(comp);
                    if (val != null)
                    {
                        string s = val.ToString().ToLower();
                        if (s.Contains("left")) return HandSide.Left;
                        if (s.Contains("right")) return HandSide.Right;
                    }
                }

                var isLeftProp = compType.GetProperty("isLeft", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (isLeftProp != null)
                {
                    object v = isLeftProp.GetValue(comp);
                    if (v is bool b)
                        return b ? HandSide.Left : HandSide.Right;
                }

                // Если не можем распарсить поля — пробуем по имени объекта
                var n = cur.name.ToLower();
                if (n.Contains("left")) return HandSide.Left;
                if (n.Contains("right")) return HandSide.Right;
            }

            // 2) По имени объекта (популярные варианты)
            var name = cur.name.ToLower();
            if (name.Contains("left") || name.Contains("lhand") || name.Contains("lh") || name.Contains("hand_l"))
                return HandSide.Left;
            if (name.Contains("right") || name.Contains("rhand") || name.Contains("rh") || name.Contains("hand_r"))
                return HandSide.Right;

            cur = cur.parent;
            depth++;
        }

        // 3) Доп. попытка: посмотреть сам корень transform.root.name
        var rootName = t.root != null ? t.root.name.ToLower() : "";
        if (!string.IsNullOrEmpty(rootName))
        {
            if (rootName.Contains("left")) return HandSide.Left;
            if (rootName.Contains("right")) return HandSide.Right;
        }

        if (debug) Debug.Log("[SmartButtonUniversal] DetermineHandFromTransform failed to find left/right in hierarchy. Starting transform: " + DescribeTransform(t));
        return HandSide.Unknown;
    }

    // Воспроизведение хаптика (обернуто исключениями)
    void PlayHaptic(string effect)
    {
        if (string.IsNullOrEmpty(effect))
        {
            if (debug) Debug.LogWarning("[SmartButtonUniversal] Empty haptic effect");
            return;
        }

        try
        {
            // Примитивный вызов bhaptics. Подставь свою сигнатуру, если отличается.
            BhapticsLibrary.Play(effect, 0, 1, 1, 0, 0);
        }
        catch (Exception e)
        {
            if (debug) Debug.LogWarning("[SmartButtonUniversal] Bhaptics Play failed: " + e.Message);
        }
    }

    // Помощник — печатает путь от корня до transform
    string DescribeTransform(Transform t)
    {
        if (t == null) return "null";
        string s = t.name;
        Transform cur = t.parent;
        while (cur != null)
        {
            s = cur.name + "/" + s;
            cur = cur.parent;
        }
        return s;
    }
}
