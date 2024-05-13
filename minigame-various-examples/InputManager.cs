namespace Minigame.Common.Input
{
    using System;
    using UnityEngine;

    public enum SwipeDirection
    {
        None,
        Up,
        Down,
        Left,
        Right
    }

    public class InputManager : Singleton<InputManager>
    {
        public event Action<Vector3> OnTap;
        public event Action OnHold;
        public event Action OnRelease;
        public event Action<SwipeDirection> OnSwipe;
        public event Action<Vector2,Vector2> OnDrag;

        public event Action<Vector3> OnTouchDown;
        public event Action OnTouchUp;

        private bool isDragging = false;
        private bool isHolding = false;
        private Vector2 dragStartPos;
        private Vector2 previousMousePos;
        private float holdTime;

        public float swipeThreshold = 100f;
        public float holdDuration = 1f;
        public float swipeSpeedThreshold = 1000f;

        private void Update()
        {
            // Detect taps, holds, and swipes
            if (Input.GetMouseButtonDown(0))
            {
                previousMousePos = Input.mousePosition;
                isDragging = true;
                dragStartPos = Input.mousePosition;
                holdTime = 0f;

                OnTouchDown?.Invoke(Input.mousePosition);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                isDragging = false;

                if (holdTime < holdDuration)
                {
                    if (Vector2.Distance(dragStartPos, Input.mousePosition) < swipeThreshold)
                    {
                        // Tap detected
                        OnTap?.Invoke(Input.mousePosition);
                    }
                    else
                    {
                        // Swipe detected
                        Vector2 swipeDirection = ((Vector2)Input.mousePosition - dragStartPos).normalized;
                        float swipeSpeed = ((Vector2)Input.mousePosition - dragStartPos).magnitude / Time.deltaTime;
                        SwipeDirection direction = GetSwipeDirection(swipeDirection, swipeSpeed);
                        OnSwipe?.Invoke(direction);
                    }
                }

                if (isHolding)
                {
                    OnRelease?.Invoke();
                    isHolding = false;
                }
                OnTouchUp?.Invoke();
            }

            // Detect holds
            if (isDragging)
            {
                holdTime += Time.deltaTime;
                if (holdTime >= holdDuration && !isHolding)
                {
                    // Hold detected
                    isHolding = true;
                    OnHold?.Invoke();
                }

                // Calculate drag difference
                Vector2 currentMousePos = Input.mousePosition;
                Vector2 dragDifference = currentMousePos - previousMousePos;

                previousMousePos = currentMousePos;
                OnDrag?.Invoke(currentMousePos, dragDifference);
            }
        }

        private SwipeDirection GetSwipeDirection(Vector2 swipeVector, float swipeSpeed)
        {
            if (swipeSpeed < swipeSpeedThreshold)
            {
                return SwipeDirection.None;
            }

            float angle = Mathf.Atan2(swipeVector.y, swipeVector.x) * Mathf.Rad2Deg;

            if (angle < 0)
            {
                angle += 360f;
            }

            if (angle >= 45f && angle < 135f)
            {
                return SwipeDirection.Up;
            }
            else if (angle >= 135f && angle < 225f)
            {
                return SwipeDirection.Left;
            }
            else if (angle >= 225f && angle < 315f)
            {
                return SwipeDirection.Down;
            }
            else
            {
                return SwipeDirection.Right;
            }
        }
    }
}