using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Platformer
{
    public class GameManager : MonoBehaviour
    {
        public int coinsCounter = 0;

        public GameObject playerGameObject;
        private PlayerController player;
        public GameObject deathPlayerPrefab;
        public Text coinText;
        [SerializeField] private Transform waterfallTransform;
        [SerializeField] private float maxWaterfallDistance = 20f;
        [SerializeField] private int coinCountDelayMs = 500;
        [SerializeField] private int coinCountStepIntervalMs = 100;
        [SerializeField] private float birdChirpMinInterval = 4f;
        [SerializeField] private float birdChirpMaxInterval = 10f;

        private bool deathSequenceRunning;
        private bool coinCountLerping;
        private Coroutine deathSequenceRoutine;
        private bool oscInitialized;
        private Coroutine musicStartRoutine;
        private Coroutine birdChirpRoutine;
        private float lastSentWaterfallDistance = float.NaN;
        private DayNightHandler dayNightHandler;

        void Start()
        {
            player = GameObject.Find("Player").GetComponent<PlayerController>();
            dayNightHandler = GameObject.Find("DayNightHandler").GetComponent<DayNightHandler>();
            InitializeOsc();
            SendMusicTrigger(true);

            if (musicStartRoutine != null)
            {
                StopCoroutine(musicStartRoutine);
            }

            musicStartRoutine = StartCoroutine(ResendMusicTriggerAfterSceneStart());
            if (birdChirpRoutine != null)
            {
                StopCoroutine(birdChirpRoutine);
            }

            birdChirpRoutine = StartCoroutine(BirdChirpRoutine());
            SyncWaterfallDistanceOsc();
        }

        private IEnumerator ResendMusicTriggerAfterSceneStart()
        {
            yield return null;
            SendMusicTrigger(true);
        }

        private IEnumerator BirdChirpRoutine()
        {
            while (!deathSequenceRunning)
            {
                float minInterval = Mathf.Max(0.1f, birdChirpMinInterval);
                float maxInterval = Mathf.Max(minInterval, birdChirpMaxInterval);
                float waitTime = Random.Range(minInterval, maxInterval);

                yield return new WaitForSeconds(waitTime);

                if (!deathSequenceRunning && dayNightHandler != null && dayNightHandler.IsDay())
                {
                    OSCHandler.Instance.SendMessageToClient("pd", "/unity/birdtrig", 1);
                }
            }
        }

        private void InitializeOsc()
        {
            try
            {
                OSCHandler.Instance.Init();
                oscInitialized = true;
            }
            catch (System.Exception ex)
            {
                oscInitialized = false;
                Debug.LogError("OSC init failed: " + ex.Message);
            }
        }

        private void SendMusicTrigger(bool musicOn)
        {
            if (!oscInitialized)
            {
                return;
            }

            OSCHandler.Instance.SendMessageToClient("pd", musicOn ? "/unity/oscmusicon" : "/unity/oscmusicoff", 1);
        }

        void Update()
        {
            if (!coinCountLerping)
            {
                coinText.text = coinsCounter.ToString();
            }

            SyncWaterfallDistanceOsc();

            if(player.deathState == true)
            {
                StartDeathSequence();
            }
        }

        private void SyncWaterfallDistanceOsc()
        {
            if (!oscInitialized || deathSequenceRunning || waterfallTransform == null || playerGameObject == null)
            {
                return;
            }

            float normalizedDistance = GetNormalizedWaterfallDistance();
            if (!float.IsNaN(lastSentWaterfallDistance) && Mathf.Abs(normalizedDistance - lastSentWaterfallDistance) < 0.01f)
            {
                return;
            }

            OSCHandler.Instance.SendMessageToClient("pd", "/unity/waterfalldistance", normalizedDistance);
            lastSentWaterfallDistance = normalizedDistance;
        }

        private float GetNormalizedWaterfallDistance()
        {
            float maxDistance = Mathf.Max(0.001f, maxWaterfallDistance);
            float distance = Vector3.Distance(playerGameObject.transform.position, waterfallTransform.position);
            return 1f - Mathf.Clamp01(distance / maxDistance);
        }

        private void StartDeathSequence()
        {
            if (deathSequenceRunning)
            {
                return;
            }

            deathSequenceRunning = true;
            SendMusicTrigger(false);
            if (birdChirpRoutine != null)
            {
                StopCoroutine(birdChirpRoutine);
                birdChirpRoutine = null;
            }
            playerGameObject.SetActive(false);
            GameObject deathPlayer = (GameObject)Instantiate(deathPlayerPrefab, playerGameObject.transform.position, playerGameObject.transform.rotation);
            deathPlayer.transform.localScale = new Vector3(playerGameObject.transform.localScale.x, playerGameObject.transform.localScale.y, playerGameObject.transform.localScale.z);
            player.deathState = false;

            if (deathSequenceRoutine != null)
            {
                StopCoroutine(deathSequenceRoutine);
            }

            deathSequenceRoutine = StartCoroutine(DeathSequenceRoutine());
        }

        private IEnumerator DeathSequenceRoutine()
        {
            yield return new WaitForSeconds(coinCountDelayMs / 1000f);

            coinCountLerping = true;

            int displayedCoins = coinsCounter;
            coinText.text = displayedCoins.ToString();
            int stepIntervalMs = Mathf.Max(1, coinCountStepIntervalMs);

            while (displayedCoins > 0)
            {
                yield return new WaitForSeconds(stepIntervalMs / 1000f);
                displayedCoins--;
                coinText.text = displayedCoins.ToString();
            }

            coinCountLerping = false;
            ReloadLevel();
        }

        private void ReloadLevel()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
