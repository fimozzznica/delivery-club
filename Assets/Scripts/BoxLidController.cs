using UnityEngine;

public class BoxLidController : MonoBehaviour
{
    [Header("Верхние створки")]
    public Transform topLeftLid;
    public Transform topRightLid;
    public float topOpenAngle = 120f;
    public Vector3 topRotationAxis = Vector3.right;

    [Header("Нижние створки")]
    public Transform bottomFrontLid;
    public Transform bottomBackLid;
    public float bottomOpenAngle = 120f;
    public Vector3 bottomRotationAxis = Vector3.forward;

    [Header("Общие настройки")]
    public float openSpeed = 1f;
    public float delayBetweenTopAndBottom = 1f;

    private Quaternion topLeftInitial, topRightInitial;
    private Quaternion bottomFrontInitial, bottomBackInitial;

    private Quaternion topLeftOpen, topRightOpen;
    private Quaternion bottomFrontOpen, bottomBackOpen;

    private float openTimer = 0f;
    private bool isOpening = false;
    private bool isClosing = false;

    void Start()
    {
        topLeftInitial = topLeftLid.localRotation;
        topRightInitial = topRightLid.localRotation;
        bottomFrontInitial = bottomFrontLid.localRotation;
        bottomBackInitial = bottomBackLid.localRotation;

        topLeftOpen = topLeftInitial * Quaternion.AngleAxis(-topOpenAngle, topRotationAxis);
        topRightOpen = topRightInitial * Quaternion.AngleAxis(topOpenAngle, topRotationAxis);

        bottomFrontOpen = bottomFrontInitial * Quaternion.AngleAxis(-bottomOpenAngle, bottomRotationAxis);
        bottomBackOpen = bottomBackInitial * Quaternion.AngleAxis(bottomOpenAngle, bottomRotationAxis);
    }

    void Update()
    {
        if (isOpening)
        {
            openTimer += Time.deltaTime * openSpeed;
        }
        else if (isClosing)
        {
            openTimer -= Time.deltaTime * openSpeed;
        }

        openTimer = Mathf.Clamp(openTimer, 0f, delayBetweenTopAndBottom * 2f);

        // Верхние створки — плавный переход
        float topT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(openTimer / delayBetweenTopAndBottom));
        topLeftLid.localRotation = Quaternion.Slerp(topLeftInitial, topLeftOpen, topT);
        topRightLid.localRotation = Quaternion.Slerp(topRightInitial, topRightOpen, topT);

        // Нижние створки — с задержкой, тоже плавный переход
        float bottomT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((openTimer - delayBetweenTopAndBottom) / delayBetweenTopAndBottom));
        bottomFrontLid.localRotation = Quaternion.Slerp(bottomFrontInitial, bottomFrontOpen, bottomT);
        bottomBackLid.localRotation = Quaternion.Slerp(bottomBackInitial, bottomBackOpen, bottomT);

        // Когда закрытие закончилось
        if (isClosing && openTimer <= 0f)
        {
            isClosing = false;
            Debug.Log("Closing complete");
        }
        // Когда открытие закончилось
        if (isOpening && openTimer >= delayBetweenTopAndBottom * 2f)
        {
            isOpening = false;
            Debug.Log("Opening complete");
        }
    }

    public void OpenBox()
    {
        isOpening = true;
        isClosing = false;
    }

    public void CloseBox()
    {
        isClosing = true;
        isOpening = false;
        openTimer = delayBetweenTopAndBottom * 2f;
    }
}




