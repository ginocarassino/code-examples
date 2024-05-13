using UnityEngine;
namespace MiniGame
{
    public class InGameAudio : MonoBehaviour
    {
        public bool debugEnabled = false;

        [SerializeField] AK.Wwise.Event musicEvent;
        [SerializeField] AK.Wwise.Event atmoEvent;
        [SerializeField] AK.Wwise.Event musicEventPause;
        [SerializeField] AK.Wwise.Event musicEventResume;

        private GameManager gameManager;

        public void Start()
        {
            PlayMusicEventTrigger();
        }

        public void OnEnable()
        {

            if (gameManager == null)
                gameManager = FindAnyObjectByType<GameManager>();

            if (gameManager)
            {
                gameManager.OnStartGame += OnStartGame;
                gameManager.OnEndGame += OnEndGame;
            }

            if (musicEventPause != null && gameManager)
            {
                gameManager.OnPauseGame += PauseMusicEventTrigger;
                gameManager.OnResumeGame += ResumeMusicEventTrigger;
                gameManager.OnExitGame += StopMusicEventTrigger;
            }
        }

        public void OnDisable()
        {

            if (gameManager == null)
                gameManager = FindAnyObjectByType<GameManager>();

            if (musicEventPause != null && gameManager)
            {
                gameManager.OnPauseGame -= PauseMusicEventTrigger;
                gameManager.OnResumeGame -= ResumeMusicEventTrigger;
                gameManager.OnExitGame -= StopMusicEventTrigger;
            }

            if (gameManager)
            {
                gameManager.OnStartGame -= OnStartGame;
                gameManager.OnEndGame -= OnEndGame;
            }
        }

        public void PlayMusicEventTrigger()
        {
            if (musicEvent != null)
            {
                if (debugEnabled) { Debug.Log("Play Music Trigger"); }
                musicEvent.Post(gameObject);
            }
        }

        public void PauseMusicEventTrigger()
        {
            if (musicEvent != null)
            {
                if (debugEnabled) { Debug.Log("Pause Music Trigger"); }
                musicEvent.ExecuteAction(gameObject, AkActionOnEventType.AkActionOnEventType_Pause, 0, AkCurveInterpolation.AkCurveInterpolation_Linear);
            }
        }

        public void ResumeMusicEventTrigger()
        {
            if (musicEvent != null)
            {
                if (debugEnabled) { Debug.Log("Resume Music Trigger"); }
                musicEvent.ExecuteAction(gameObject, AkActionOnEventType.AkActionOnEventType_Resume, 0, AkCurveInterpolation.AkCurveInterpolation_Linear);
            }
        }

        public void StopMusicEventTrigger()
        {
            if (musicEvent != null)
            {
                if (debugEnabled) { Debug.Log("Play Music Trigger"); }
                musicEvent.Stop(gameObject, 0, AkCurveInterpolation.AkCurveInterpolation_Linear);
            }
        }

        private void OnEndGame()
        {
            atmoEvent.Stop(gameObject, 0, AkCurveInterpolation.AkCurveInterpolation_Linear);
        }

        private void OnStartGame()
        {
            atmoEvent.Post(gameObject);
        }
    }
}
