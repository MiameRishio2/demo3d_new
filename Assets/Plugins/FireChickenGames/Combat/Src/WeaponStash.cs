namespace FireChickenGames.Combat
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using FireChickenGames.Combat.Core;
    using FireChickenGames.Combat.Core.Integrations;
    using GameCreator.Core;
    using UnityEngine;
    using UnityEngine.Events;

    [Serializable]
    public struct StashableWeapon
    {
        public int instanceId;
        public ScriptableObject weapon;
    }

    [AddComponentMenu("Fire Chicken Games/Combat/Weapon Stash")]
    public class WeaponStash : MonoBehaviour
    {
        [Tooltip("An object or variable with a Player/Character Shooter component.")]
        public TargetGameObject character = new TargetGameObject(TargetGameObject.Target.Player);
        [Tooltip("If the target is the player, the WeaponStashUi will be auto-wired if one is available in the scene.")]
        public WeaponStashUi weaponStashUi;

        public ScriptableObject _stashedWeapon;
        public ScriptableObject StashedWeapon
        {
            get { return _stashedWeapon; }
            set
            {
                _stashedWeapon = value;
                UpdateStashedWeaponUi(_stashedWeapon);
            }
        }
        public ScriptableObject stashedAmmo;
        public ScriptableObject StashedAmmo
        {
            get { return stashedAmmo; }
            set
            {
                stashedAmmo = value;
                if (weaponStashUi != null && characterShooter != null)
                    characterShooter.SetAmmoNameAndDescription(stashedAmmo);
            }
        }

        public List<StashableWeapon> weapons = new List<StashableWeapon>();
        public ICharacterShooter characterShooter;
        public ICharacterMelee characterMelee;

        public class WeaponStashEvent : UnityEvent<ScriptableObject, ScriptableObject> { }
        public WeaponStashEvent weaponChangedEvent = new WeaponStashEvent();

        private void Start()
        {
            if (character != null)
            {
                characterShooter = ShooterIntegrationManager.MakeCharacterShooter(character, gameObject, SetStashedWeapon, weaponStashUi);
                if (characterShooter != null)
                    characterShooter.AddEventChangeAmmoListener();

                characterMelee = MeleeIntegrationManager.MakeCharacterMelee(character, gameObject);

                if (weaponStashUi == null && character.GetGameObject(gameObject).CompareTag("Player"))
                    weaponStashUi = HookManager.GetWeaponStashUi();

                UpdateStashedWeaponUi(StashedWeapon);

                if (StashedWeapon != null && characterShooter != null)
                    characterShooter.ChangeAmmo(StashedWeapon);
            }

            if (weaponStashUi != null && characterShooter != null)
            {
                characterShooter.WeaponStashUi = weaponStashUi;
                characterShooter.AddEventChangeClipListener(weaponStashUi.SetAmmoInClip);
                characterShooter.AddEventChangeStorageListener(weaponStashUi.SetAmmoInStorage);
            }
        }

        void OnDestroy()
        {
            if (characterShooter == null)
                return;

            characterShooter.RemoveEventChangeAmmoListener();
            if (weaponStashUi != null)
            {
                characterShooter.RemoveEventChangeClipListener(weaponStashUi.SetAmmoInClip);
                characterShooter.RemoveEventChangeStorageListener(weaponStashUi.SetAmmoInStorage);
            }
        }

        ScriptableObject GetFirstOrLastWeapon(bool isReverseSelection = false)
        {
            return (isReverseSelection ? weapons.LastOrDefault() : weapons.FirstOrDefault()).weapon;
        }

        protected void SetStashedWeapon(ScriptableObject weapon)
        {
            if (weapon == null)
                return;

            StashedWeapon = weapon;

            if (characterShooter != null && characterShooter.IsShooterWeapon(weapon))
                StashedAmmo = characterShooter.GetAmmoUsedByWeapon(StashedWeapon);
        }

        protected void UpdateStashedWeaponUi(ScriptableObject stashedWeapon)
        {
            if (weaponStashUi == null)
                return;

            string weaponName;
            string weaponDescription;

            if (characterShooter != null && characterShooter.IsShooterWeapon(stashedWeapon))
            {
                characterShooter.SetWeaponNameAndDescription(stashedWeapon);
            }
            else if (characterMelee != null)
            {
                weaponName = characterMelee.GetWeaponName(stashedWeapon);
                weaponDescription = characterMelee.GetWeaponDescription(stashedWeapon);
                weaponStashUi.RemoveAmmoClipAndStorage();
                weaponStashUi.SetWeapon(weaponName, weaponDescription);
            }
        }

        /**
         * Public API
         */

        public bool HasWeaponDrawn()
        {
            return characterShooter?.CurrentWeapon != null || characterMelee?.CurrentWeapon != null;
        }

        public bool CanChangeWeapon()
        {
            if (characterShooter != null)
            { 
                if (characterShooter.IsDrawing || characterShooter.IsHolstering || characterShooter.IsReloading)
                    return false;
                if (!characterShooter.IsControllable)
                    return false;
                if (characterShooter.IsCharacterLocomotionBusy)
                    return false;
                if (!weapons.Any())
                    return false;
            }

            return true;
        }

        public IEnumerator CycleToNextWeapon(bool isReverseSelection = false)
        {
            if (!CanChangeWeapon())
                yield break;

            ScriptableObject nextWeapon = null;
            var weaponIndex = weapons.FindIndex(x => x.weapon == StashedWeapon);
            if (weaponIndex == -1)
                GetFirstOrLastWeapon(isReverseSelection);
            else if (isReverseSelection && weaponIndex == 0)
                nextWeapon = weapons.LastOrDefault().weapon;
            else if (isReverseSelection)
                nextWeapon = weapons[weaponIndex - 1].weapon;
            else if (!isReverseSelection && weaponIndex == weapons.Count - 1)
                nextWeapon = weapons.FirstOrDefault().weapon;
            else if (!isReverseSelection)
                nextWeapon = weapons[weaponIndex + 1].weapon;

            if (characterShooter != null && characterShooter.IsShooterWeapon(nextWeapon))
            {
                if (HasWeaponDrawn() && nextWeapon != null && characterShooter.CurrentWeapon != nextWeapon)
                {
                    /**
                     * If a weapon is drawn, draw a different weapon (and change ammo).
                     * The stashed weapon and ammo are changed after the weapon is drawn.
                     */
                    yield return characterMelee?.Sheathe();
                    yield return characterShooter.ChangeWeapon(nextWeapon);
                }
                else if (nextWeapon != null && characterShooter.IsAmmoUsedByWeapon(nextWeapon))
                {
                    /**
                     * If a weapon is not drawn, change the stashed weapon and ammo.
                     */
                    yield return characterMelee?.Sheathe();
                    SetStashedWeapon(nextWeapon);
                    characterShooter.SetAmmoNameAndDescriptionFromWeapon(nextWeapon);
                }
            }
            else
            {
                ///**
                // * Melee integration. 
                // */
                if (HasWeaponDrawn() && nextWeapon != null && characterMelee?.CurrentWeapon != nextWeapon)
                {
                    /**
                     * If a weapon is drawn, draw a different weapon (and change ammo).
                     * The stashed weapon is changed after the weapon is drawn.
                     */
                    yield return characterShooter?.ChangeWeapon(null);
                    SetStashedWeapon(nextWeapon);
                    yield return characterMelee?.Draw(nextWeapon);
                }
                else if (nextWeapon != null)
                {
                    /**
                     * If a weapon is not drawn, change the stashed weapon.
                     */
                    SetStashedWeapon(nextWeapon);
                    weaponStashUi?.RemoveAmmoClipAndStorage();
                }
            }
        }

        public IEnumerator GiveWeapon(ScriptableObject weapon, ScriptableObject ammo = null)
        {
            if (weapon == null)
                yield break;

            var indexId = weapons.FindIndex(x => x.instanceId == weapon.GetInstanceID());
            if (indexId != -1)
                yield break;

            weapons.Add(new StashableWeapon
            {
                instanceId = weapon.GetInstanceID(),
                weapon = weapon
            });

            if (StashedWeapon == null && StashedAmmo == null)
                StashedAmmo = ammo;

            if (StashedWeapon == null)
                StashedWeapon = weapon;
        }

        public IEnumerator TakeWeapon(ScriptableObject weapon)
        {
            if (weapons == null)
                yield break;

            var weaponsInStash = weapons.Count();
            if (weaponsInStash == 0)
                yield break;


            // Holster/sheathe currnent weapon.
            if (HasWeaponDrawn())
            {
                yield return characterShooter?.ChangeWeapon(null);
                yield return characterMelee?.Sheathe();
            }

            // Switch to next weapon.
            if (weaponsInStash > 1)
                yield return CycleToNextWeapon();
            else
            {
                StashedWeapon = null;
                StashedAmmo = null;
                if (weaponStashUi != null)
                {
                    weaponStashUi.SetWeapon("", "");
                    weaponStashUi.RemoveAmmoClipAndStorage();
                }
            }

            // Remove weapon.
            weapons.RemoveAll(x => x.instanceId == weapon.GetInstanceID());
            

            yield return 0;
        }

        public IEnumerator TakeCurrentWeapon()
        {
            yield return TakeWeapon(StashedWeapon);
        }

        public IEnumerator DrawShooter()
        {
            if (characterShooter != null && characterShooter.IsShooterWeapon(StashedWeapon))
            {
                if (StashedAmmo == null && StashedWeapon != null)
                    StashedAmmo = characterShooter.GetAmmoUsedByWeapon(StashedWeapon);

                weaponChangedEvent.Invoke(StashedWeapon, StashedAmmo);

                yield return characterShooter.ChangeWeapon(StashedWeapon, StashedAmmo);
            }
        }

        public IEnumerator DrawMelee()
        {
            yield return characterMelee?.Draw(StashedWeapon);
        }
    }
}
