using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Cinemachine;

namespace MicroUniverse {

    /// <summary>
    /// Logic:
    /// Find nearest activated pillar -> deactivate it -> find next
    /// </summary>
    public class BadBallController : MonoBehaviour {



        public Vector2 speedRange = new Vector2(1f, 3f);

        public RoadProp currRoadProp;
        public RoadProp targetRoadProp;
        public float targetReachDistance = 0.05f;

        CinemachineImpulseSource impulseSource;

        public enum Direction { Up, Left, Down, Right, Invalid }

        Direction currDirection = Direction.Up;
        bool running = false;
        float ballRadius;
        private float speed;

        public void Die() {
            // TODO: explode!
            impulseSource.GenerateImpulse();
            Destroy(gameObject);
        }

        private void Start() {
            currDirection = RandomDirection();
            ballRadius = transform.localScale.x / 2f;
            speed = Random.Range(speedRange.x, speedRange.y);
            impulseSource = GetComponent<CinemachineImpulseSource>();
        }

        private Direction RandomDirection() {
            Direction ret = Direction.Invalid;
            int dice = Random.Range(0, 4);
            switch (dice) {
                case 0:
                    ret = Direction.Up;
                    break;
                case 1:
                    ret = Direction.Left;
                    break;
                case 2:
                    ret = Direction.Down;
                    break;
                case 3:
                    ret = Direction.Right;
                    break;
            }
            return ret;
        }

        private RoadProp GetRoadOnDirection(Direction direction, RoadProp currProp) {
            RoadProp ret = null;
            switch (direction) {
                case Direction.Down:
                    ret = currProp.bottom;
                    break;
                case Direction.Left:
                    ret = currProp.left;
                    break;
                case Direction.Right:
                    ret = currProp.right;
                    break;
                case Direction.Up:
                    ret = currProp.top;
                    break;
                default:
                    break;
            }
            return ret;
        }

        private Direction GetDirection(RoadProp from, RoadProp to) {
            if (from.left == to) {
                return Direction.Left;
            } else if (from.right == to) {
                return Direction.Right;
            } else if (from.top == to) {
                return Direction.Up;
            } else if (from.bottom == to) {
                return Direction.Down;
            } else {
                return Direction.Invalid;
            }
        }

        private Direction GetInvDirection(Direction dir) {
            switch (dir) {
                case Direction.Down:
                    return Direction.Up;
                case Direction.Left:
                    return Direction.Right;
                case Direction.Right:
                    return Direction.Left;
                case Direction.Up:
                    return Direction.Down;
                default:
                    return Direction.Invalid;
            }
        }

        private List<RoadProp> GetPossibleTarget(Direction originalDirection, RoadProp currProp) {
            List<RoadProp> ret = new List<RoadProp>();
            if (originalDirection != Direction.Up && GetRoadOnDirection(Direction.Down, currProp) != null) {
                ret.Add(GetRoadOnDirection(Direction.Down, currProp));
            }
            if (originalDirection != Direction.Down && GetRoadOnDirection(Direction.Up, currProp) != null) {
                ret.Add(GetRoadOnDirection(Direction.Up, currProp));
            }
            if (originalDirection != Direction.Right && GetRoadOnDirection(Direction.Left, currProp) != null) {
                ret.Add(GetRoadOnDirection(Direction.Left, currProp));
            }
            if (originalDirection != Direction.Left && GetRoadOnDirection(Direction.Right, currProp) != null) {
                ret.Add(GetRoadOnDirection(Direction.Right, currProp));
            }
            return ret;
        }

        private void Update() {

            if (currRoadProp == null) {
                gameObject.SetActive(false);
                return;
            }
            if (targetRoadProp == null) {
                // select a new road prop to march.
                List<RoadProp> possible = GetPossibleTarget(currDirection, currRoadProp);

                if (possible.Count != 0) {
                    // has way to go
                    int dice = Random.Range(0, possible.Count);
                    targetRoadProp = possible[dice];
                    currDirection = GetDirection(currRoadProp, targetRoadProp);
                } else {
                    // dead end, return back
                    currDirection = GetInvDirection(currDirection);
                    targetRoadProp = GetRoadOnDirection(currDirection, currRoadProp);
                    if (targetRoadProp == null) {
                        running = false; // trapped??
                    }
                }
            } else {
                // marching
                Vector3 targetPos = targetRoadProp.transform.position;
                Vector3 selfPos = currRoadProp.transform.position;
                Vector3 toTargetDir = (targetPos - selfPos).normalized;

                float d = Vector3.Distance(transform.position, targetPos);
                if (d < targetReachDistance + ballRadius) {
                    // on target:
                    currRoadProp = targetRoadProp;
                    targetRoadProp = null;

                    PillarProp pillarProp = currRoadProp.GetComponent<PillarProp>();
                    if (pillarProp != null) {
                        pillarProp.Deactivate();
                    }

                } else {
                    Vector3 pos = transform.position;
                    Vector3 newPos = pos + toTargetDir * speed * Time.deltaTime;
                    newPos.y = ballRadius;
                    transform.position = newPos;
                }

            }
        }

    }

}