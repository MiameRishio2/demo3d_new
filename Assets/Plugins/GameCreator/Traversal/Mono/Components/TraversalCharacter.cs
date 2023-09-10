using System;
using System.Runtime.CompilerServices;
using GameCreator.Core;

namespace GameCreator.Traversal
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using GameCreator.Characters;

    [RequireComponent(typeof(Character))]
    public class TraversalCharacter : MonoBehaviour
    {
        private const float ANIM_SMOOTH_TIME = 0.1f;
        
        // ENUMS: ---------------------------------------------------------------------------------

        // public enum DetectMode
        // {
        //     None,
        //     CenterOfCamera,
        //     ClosestToPlayer
        // }

        // FIELDS & MEMBERS: ----------------------------------------------------------------------

        // TODO:
        // Need to find a way to select nearest TraversableComponent without iterating them all.
        // Maybe with a KD-Tree? Initial build is expensive. Maybe use Spatial-Hash array
        
        // public DetectMode detectionMode = DetectMode.None;
        // public float detectionRadius = 10f;
        // private TraversableComponent detected;
        
        private TraversableComponent activeTraversable = null;

        private bool isActivating = false;
        private bool isReaching = false;

        private float animationMovementVelocity = 0f;
        private Vector2 animationMovementDirectionVelocity = Vector2.zero;

        // PROPERTIES: ----------------------------------------------------------------------------

        public Character Character { get; private set; }
        public CharacterAnimator Animator { get; private set; }
        
        public float AnimationMovementTarget { set; private get; }
        public float AnimationMovement { get; private set; }
        
        public Vector2 AnimationMovementDirectionTarget { set; private get; }
        public Vector2 AnimationMovementDirection { get; private set; }

        // INITIALIZERS: --------------------------------------------------------------------------

        private void Awake()
        {
            this.Character = GetComponent<Character>();
            this.Animator = GetComponent<CharacterAnimator>();
        }
        
        // UPDATE METHODS: ------------------------------------------------------------------------

        private void Update()
        {
            this.AnimationMovement = Mathf.SmoothDamp(
                this.AnimationMovement, this.AnimationMovementTarget, 
                ref this.animationMovementVelocity, ANIM_SMOOTH_TIME 
            );
            
            this.AnimationMovementDirection = Vector2.SmoothDamp(
                this.AnimationMovementDirection, this.AnimationMovementDirectionTarget, 
                ref this.animationMovementDirectionVelocity, ANIM_SMOOTH_TIME 
            );
            
            if (this.activeTraversable == null) return;
            if (!this.isActivating && !this.isReaching)
            {
                this.activeTraversable.OnUpdate(this);
            }
        }

        // PUBLIC METHODS: ------------------------------------------------------------------------

        public IEnumerator SetActive(TraversableComponent traversable)
        {
            if (traversable == this.activeTraversable) yield break;
            if (this.activeTraversable is Climbable && this.isActivating) yield break;

            this.AnimationMovementTarget = 0f;
            Climbable fromClimbable = this.activeTraversable as Climbable;
            
            if (this.activeTraversable != null)
            {
                this.activeTraversable.OnDeactivate(this);
                this.activeTraversable = null;
            }

            this.isReaching = false;
            this.activeTraversable = traversable;
            
            if (this.activeTraversable != null)
            {
                this.isActivating = true;
                yield return this.activeTraversable.OnActivate(this, fromClimbable);
                this.isActivating = false;

                this.AnimationMovementDirection = Vector2.zero;
                this.AnimationMovementDirectionTarget = Vector2.zero;
                this.animationMovementDirectionVelocity = Vector2.zero;
            }
        }

        public void SetIsReaching()
        {
            this.isReaching = true;
        }

        public Vector3 GetHandlesOffset(Climbable climbable)
        {
            return climbable.transform.TransformDirection(
                0f,
                climbable.clip.offsetUpward,
                climbable.clip.offsetForward
            );
        }
        
        public Vector3 GetHandlesOffset(Obstacle obstacle)
        {
            // return transform.TransformDirection(
            return obstacle.transform.TransformDirection(
                0f,
                obstacle.clip.handlesOffsetUpward,
                obstacle.clip.handlesOffsetForward
            );
        }
        
        // GIZMOS: --------------------------------------------------------------------------------

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawCube(this.transform.position, Vector3.one * 0.1f);

            if (this.activeTraversable != null && this.activeTraversable is Climbable)
            {
                Gizmos.color = new Color(0,0,0, 0.5f);
                Gizmos.DrawCube(
                    this.transform.position - this.GetHandlesOffset((Climbable)this.activeTraversable), 
                    Vector3.one * 0.1f
                );
            }
        }
    }
}