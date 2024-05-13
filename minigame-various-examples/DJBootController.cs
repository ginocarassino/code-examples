using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiniGame.FollowMeow
{
    public class DJBootController : MonoBehaviour
    {
        [SerializeField] private List<Transform> m_DJBootButtons = new List<Transform>();
        private string[] animationTriggers = { "press_Red", "press_Blue", "press_Green", "press_Yellow" };

        [Header("Button movement")]
        [SerializeField] private float moveAmount = 0.01f;
        [SerializeField] private float duration = 0.15f;
        [SerializeField] private float targetEmissionStrength = 1.8f;
        [SerializeField] private float originalEmissionStrength = .5f;

        public Animator catAnimator;

        [SerializeField] private FMeow_EnvironmentController environmentCtr;

        [Header("Audio")]
        [SerializeField] List<AK.Wwise.Event> audioButtonEvents;

        public void PlayButton(int index)
        {
            PlayCatAnim(index);

            DOVirtual.DelayedCall(0.5f, () =>
            {
                audioButtonEvents[index].Post(gameObject);
                environmentCtr.PlayEnvironmentEffect(index);

                Material buttonMaterial = m_DJBootButtons[index].GetComponent<Renderer>().material;
                Vector3 originalPosition = m_DJBootButtons[index].position;
                Vector3 newPosition = originalPosition - new Vector3(0, moveAmount, 0);

                DOTween.To(() => buttonMaterial.GetFloat("_EmissionStrengh"),
                           x => buttonMaterial.SetFloat("_EmissionStrengh", x), targetEmissionStrength, duration);

                m_DJBootButtons[index].DOMove(newPosition, duration).OnComplete(() =>
                {
                    m_DJBootButtons[index].DOMove(originalPosition, duration);
                    DOTween.To(() => buttonMaterial.GetFloat("_EmissionStrengh"),
                               x => buttonMaterial.SetFloat("_EmissionStrengh", x), originalEmissionStrength, duration);
                });
            });
        }

        public void PlayCatAnim(int index)
        {
            if (index >= 0 && index < animationTriggers.Length)
                catAnimator.SetTrigger(animationTriggers[index]);
            else
                Debug.LogWarning("Index out of bounds for PlayCatAnim: " + index);
        }
    }
}