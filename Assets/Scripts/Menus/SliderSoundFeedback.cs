using UnityEngine;
using UnityEngine.EventSystems;

public class SliderSoundFeedback : MonoBehaviour, IPointerUpHandler
{
    public void OnPointerUp(PointerEventData eventData)
    {
        AudioManager.audioManagerInstance.PlaySFX(AudioManager.audioManagerInstance.button); 
    }
}
