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
        [SerializeField] private int coinCountDelayMs = 500;
        [SerializeField] private int coinCountStepIntervalMs = 100;

        private bool deathSequenceRunning;
        private bool coinCountLerping;
        private Coroutine deathSequenceRoutine;
        private bool oscInitialized;
        private Coroutine musicStartRoutine;

        void Start()
        {
            player = GameObject.Find("Player").GetComponent<PlayerController>();
            InitializeOsc();
            SendMusicTrigger(true);

            if (musicStartRoutine != null)
            {
                StopCoroutine(musicStartRoutine);
            }

            musicStartRoutine = StartCoroutine(ResendMusicTriggerAfterSceneStart());
        }

        private IEnumerator ResendMusicTriggerAfterSceneStart()
        {
            yield return null;
            SendMusicTrigger(true);
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

            if(player.deathState == true)
            {
                StartDeathSequence();
            }
        }

        private void StartDeathSequence()
        {
            if (deathSequenceRunning)
            {
                return;
            }

            deathSequenceRunning = true;
            SendMusicTrigger(false);
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
