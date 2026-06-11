using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Platformer
{
    public class PlayerController : MonoBehaviour
    {
        public float movingSpeed;
        public float jumpForce;
        private float moveInput;

        private bool facingRight = false;
        [HideInInspector]
        public bool deathState = false;

        private bool isGrounded;
        public Transform groundCheck;

        private Rigidbody2D rb;
        private Animator animator;
        private GameManager gameManager;
        private bool oscInitialized;
        [SerializeField] private float footstepInterval = 0.35f;
        private float nextFootstepTime;
        private bool deathTriggerSent;
        private int lastSentCoinCount = int.MinValue;

        void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
            InitializeOsc();
            OSCHandler.Instance.SendMessageToClient("pd", "/oscmusicon", 1);
            SyncCoinCountOsc();
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

        private void SendCoinPickupOscTrigger()
        {
            if (!oscInitialized)
            {
                return;
            }

            OSCHandler.Instance.SendMessageToClient("pd", "/unity/cointrig", 1);
        }

        private void SendOscTrigger(string triggerName)
        {
            if (!oscInitialized)
            {
                return;
            }

            OSCHandler.Instance.SendMessageToClient("pd", "/unity/" + triggerName, 1);
        }

        private void SendOscInt(string eventName, int value)
        {
            if (!oscInitialized)
            {
                return;
            }

            OSCHandler.Instance.SendMessageToClient("pd", "/unity/" + eventName, value);
        }

        private void SendOscFloat(string eventName, float value)
        {
            if (!oscInitialized)
            {
                return;
            }

            OSCHandler.Instance.SendMessageToClient("pd", "/unity/" + eventName, value);
        }

        private void SyncCoinCountOsc()
        {
            if (gameManager == null)
            {
                return;
            }

            int currentCoinCount = gameManager.coinsCounter;
            if (currentCoinCount == lastSentCoinCount)
            {
                return;
            }

            SendOscInt("coincount", currentCoinCount);
            SendOscFloat("coincount", currentCoinCount);
            lastSentCoinCount = currentCoinCount;
        }

        private void HandleFootstepTriggers()
        {
            bool isMovingHorizontally = Mathf.Abs(moveInput) > 0.1f;
            if (!isGrounded || !isMovingHorizontally || deathState)
            {
                return;
            }

            if (Time.time >= nextFootstepTime)
            {
                SendOscTrigger("steptrig");
                nextFootstepTime = Time.time + Mathf.Max(0.05f, footstepInterval);
            }
        }

        private void FixedUpdate()
        {
            CheckGround();
        }

        void Update()
        {
            moveInput = ReadHorizontalInput();
            SyncCoinCountOsc();

            if (Mathf.Abs(moveInput) > 0.01f)
            {
                Vector3 direction = transform.right * moveInput;
                transform.position = Vector3.MoveTowards(transform.position, transform.position + direction, movingSpeed * Time.deltaTime);
                animator.SetInteger("playerState", 1); // Turn on run animation
            }
            else
            {
                if (isGrounded) animator.SetInteger("playerState", 0); // Turn on idle animation
            }

            if (IsJumpPressedThisFrame() && isGrounded)
            {
                SendOscTrigger("jumptrig");
                rb.AddForce(transform.up * jumpForce, ForceMode2D.Impulse);
            }

            if (!isGrounded)animator.SetInteger("playerState", 2); // Turn on jump animation

            if(facingRight == false && moveInput > 0)
            {
                Flip();
            }
            else if(facingRight == true && moveInput < 0)
            {
                Flip();
            }

            HandleFootstepTriggers();
        }

        private float ReadHorizontalInput()
        {
            float horizontal = 0f;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                {
                    horizontal -= 1f;
                }

                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                {
                    horizontal += 1f;
                }
            }

            if (Gamepad.current != null)
            {
                float stickX = Gamepad.current.leftStick.ReadValue().x;
                if (Mathf.Abs(stickX) > Mathf.Abs(horizontal))
                {
                    horizontal = stickX;
                }
            }

            return horizontal;
        }

        private bool IsJumpPressedThisFrame()
        {
            bool keyboardJump = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
            bool gamepadJump = Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
            return keyboardJump || gamepadJump;
        }

        private void Flip()
        {
            facingRight = !facingRight;
            Vector3 Scaler = transform.localScale;
            Scaler.x *= -1;
            transform.localScale = Scaler;
        }

        private void CheckGround()
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(groundCheck.transform.position, 0.2f);
            isGrounded = colliders.Length > 1;
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            if (other.gameObject.tag == "Enemy")
            {
                deathState = true; // Say to GameManager that player is dead
                if (!deathTriggerSent)
                {
                    SyncCoinCountOsc();
                    SendOscTrigger("deathtrig");
                    deathTriggerSent = true;
                }
            }
            else
            {
                deathState = false;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.tag == "Coin")
            {
                gameManager.coinsCounter += 1;
                SyncCoinCountOsc();
                SendCoinPickupOscTrigger();
                Destroy(other.gameObject);
            }
        }
    }
}
