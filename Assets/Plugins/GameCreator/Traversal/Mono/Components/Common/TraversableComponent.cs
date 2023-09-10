using System;
using GameCreator.Core;

namespace GameCreator.Traversal
{
    using UnityEngine;
    using System.Collections;
    using System.Collections.Generic;

    public abstract class TraversableComponent : MonoBehaviour, ITraversableComponent
    {
        [SerializeField] private bool autoConnect = true; 
        
        // PROPERTIES: ----------------------------------------------------------------------------
        
        protected readonly Dictionary<TraversalCharacter, bool> Users = 
            new Dictionary<TraversalCharacter, bool>();
        
        // METHODS: -------------------------------------------------------------------------------

        protected void OnDisable()
        {
            if (SaveLoadManager.IS_EXITING) return;
            
            List<TraversalCharacter> characters = new List<TraversalCharacter>(this.Users.Keys);
            for (int i = characters.Count - 1; i >= 0; --i)
            {
                this.OnDeactivate(characters[i]);   
            }
        }

        // VIRTUAL & ABSTRACT METHODS: ------------------------------------------------------------

        public abstract IEnumerator OnActivate(TraversalCharacter traverser, Climbable from);

        public virtual void OnDeactivate(TraversalCharacter traverser)
        {
            this.Users.Remove(traverser);
        }
        
        public virtual void OnUpdate(TraversalCharacter traverser)
        { }
        
        public abstract Vector3 GetStartPoint(TraversalCharacter traverser, Climbable from);
        public abstract float GetReachDuration(TraversalCharacter traverser, Climbable from);

        public abstract List<TraversableComponent> GetAutoConnectionsElements();
    }   
}
