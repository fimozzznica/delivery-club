using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class AttachTransform : MonoBehaviour
{
    [SerializeField] Transform m_RightAttachTransform;
    [SerializeField] Transform m_LeftAttachTransform;
    public XRGrabInteractable XRGrabAttach;
    public void Attachhand(HoverEnterEventArgs args)
    {
        var rightHand = (args.interactorObject.interactionLayers & (1 << 1)) == 0;
        var handString = rightHand ? "Right" : "Left";
        Debug.Log($"GrabWeapon {handString} hand");
        Transform attTransform = rightHand ? m_RightAttachTransform : m_LeftAttachTransform;
        XRGrabAttach.attachTransform = attTransform;
    }
}
