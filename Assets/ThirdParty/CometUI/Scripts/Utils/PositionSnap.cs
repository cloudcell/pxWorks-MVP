using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CometUI
{
    /// <summary>Arttach the script to Content inside ScrollView</summary>
    public class PositionSnap : MonoBehaviour
    {
        [SerializeField] float StepX = 100;
        [SerializeField] float StepY = 100;
        [SerializeField] float MaxSpeed = 200;
        [SerializeField] float SnapSpeed = 300;
        [SerializeField] SnapDirection Direction = SnapDirection.ByMoving;
        [SerializeField] Vector2 Padding = new Vector2();

        ScrollRect sr;
        float decelerationRate;
        Vector3 prevPos;
        bool inWork;
        int frameCounter = 0;
        float delta = 0f;

        public enum SnapDirection
        {
            ToNearest, ToLower, ToHigher, ByMoving
        }

        void Start()
        {
            sr = GetComponentInParent<ScrollRect>();
            decelerationRate = sr.decelerationRate;
        }

        void LateUpdate()
        {
            if (!Input.GetMouseButton(0))
            {
                var speed = (transform.localPosition - prevPos).magnitude / Time.deltaTime;

                if (speed < MaxSpeed || inWork)
                {
                    frameCounter++;
                    if (frameCounter == 2)
                    {
                        switch (Direction)
                        {
                            case SnapDirection.ToLower: delta = -0.48f; break;
                            case SnapDirection.ToHigher: delta = +0.48f; break;
                            case SnapDirection.ToNearest: delta = 0; break;
                            case SnapDirection.ByMoving:
                            {
                                if (Mathf.Abs(speed) < 20)
                                    goto case SnapDirection.ToNearest;

                                var moving = transform.localPosition - prevPos;
                                var mov = Mathf.Abs(moving.x) > Mathf.Abs(moving.y) ? moving.x : moving.y;
                                if (mov < 0)
                                    goto case SnapDirection.ToLower;
                                else
                                    goto case SnapDirection.ToHigher;
                            }
                        }
                    }

                    if (frameCounter > 2)
                    {
                        inWork = true;

                        var pos = transform.localPosition - (Vector3)Padding;
                        var iY = Mathf.RoundToInt(pos.y / StepY + delta);
                        var iX = Mathf.RoundToInt(pos.x / StepX + delta);
                        var target = new Vector3(iX * StepX, iY * StepY, transform.localPosition.z) + (Vector3)Padding;
                        transform.localPosition = Vector3.MoveTowards(transform.localPosition, target, SnapSpeed * Time.deltaTime);
                        //transform.localPosition = Vector3.Lerp(transform.localPosition, new Vector3(iX * StepX, iY * StepY, transform.localPosition.z), SnapSpeed * Time.deltaTime);

                        if (sr)
                            sr.decelerationRate = 0;
                    }
                }
            }
            else
            {
                if (sr)
                    sr.decelerationRate = decelerationRate;
                inWork = false;
                frameCounter = 0;
            }

            prevPos = transform.localPosition;
        }
    }
}