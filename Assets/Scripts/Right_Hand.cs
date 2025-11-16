using UnityEngine;

public enum HandSide1 { Left, Right, Unknown }

public class ControllerHand : MonoBehaviour
{
    public HandSide hand = HandSide.Unknown;

    void Awake()
    {
        if (hand != HandSide.Unknown) return;

        var n = gameObject.name.ToLower();
        if (n.Contains("left")) hand = HandSide.Left;
        else if (n.Contains("right")) hand = HandSide.Right;
        else hand = HandSide.Unknown;
    }
}
