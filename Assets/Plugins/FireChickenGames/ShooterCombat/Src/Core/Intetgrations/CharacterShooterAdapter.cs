namespace FireChickenGames.ShooterCombat.Core.Integrations
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using FireChickenGames.Combat;
    using FireChickenGames.Combat.Core.Integrations;
    using FireChickenGames.ShooterCombat.Core.Aiming;
    using GameCreator.Characters;
    using GameCreator.Shooter;
    using UnityEngine;
    using UnityEngine.Events;

    public class CharacterShooterAdapter : ICharacterShooter
    {
        public Character Character { get { return characterShooter != null ? characterShooter.character : null; } }
        public CharacterShooter characterShooter;

        public bool IsControllable { get { return Character != null && Character.IsControllable(); } }
        public bool IsCharacterLocomotionBusy { get { return Character != null && Character.characterLocomotion.isBusy; } }

        public bool IsDrawing { get { return characterShooter != null && characterShooter.isDrawing; } }
        public bool IsHolstering { get { return characterShooter != null && characterShooter.isHolstering; } }
        public bool IsReloading { get { return characterShooter != null && characterShooter.isReloading; } }
        public bool IsAiming { get { return characterShooter != null && characterShooter.isAiming; } }
        public bool IsChargingShot { get { return characterShooter != null && characterShooter.isChargingShot; } }

        public ScriptableObject CurrentWeapon { get { return characterShooter.currentWeapon;  } }
        public ScriptableObject CurrentAmmo { get { return characterShooter.currentAmmo; } }

        public WeaponStashUi WeaponStashUi { get; set; }
        public UnityAction<ScriptableObject> SetStashedWeapon { get; set; }

        // Aim Assist
        private RaycastHit[] bufferCastHits = new RaycastHit[100];
        private class AimAssistRaycastComparer : IComparer<RaycastHit>
        {
            public int Compare(RaycastHit x, RaycastHit y)
            {
                return x.distance.CompareTo(y.distance);
            }
        }
        private static readonly AimAssistRaycastComparer AIM_ASSIST_RAYCAST_COMPARER = new AimAssistRaycastComparer();


        public void OnChangeAmmo(Ammo ammo)
        {
            SetStashedWeapon.Invoke(CurrentWeapon);

            if (WeaponStashUi == null || ammo == null)
                return;
            
            WeaponStashUi.SetAmmoInClip(ammo.ammoID, characterShooter.GetAmmoInClip(ammo.ammoID));
            WeaponStashUi.SetAmmoInStorage(ammo.ammoID, characterShooter.GetAmmoInStorage(ammo.ammoID));
            WeaponStashUi.SetAmmoMaxClipText(ammo.clipSize.ToString());
        }

        public void SetCharacterShooter(Component characterShooter)
        {
            this.characterShooter = characterShooter as CharacterShooter;
        }

        public bool HasCharacterShooter()
        {
            return characterShooter != null;
        }

        public Component GetCharacterShooter()
        {
            return characterShooter;
        }

        public void AddEventOnAimListener(UnityAction<bool> onAim)
        {
            if (characterShooter != null)
                characterShooter.eventOnAim.AddListener(onAim);
        }

        public void RemoveEventOnAimListener(UnityAction<bool> onAim)
        {
            characterShooter?.eventOnAim.RemoveListener(onAim);
        }

        public void StartAiming(IAimingAtProximityTarget aimingAtTarget = null)
        {
            if (aimingAtTarget != null)
            {
                var baseAimingAtTarget = (AimingAtProximityTarget)aimingAtTarget;
                characterShooter.StartAiming(baseAimingAtTarget);
            }
            else
                characterShooter.StartAiming(new AimingCameraDirection(characterShooter));
        }

        public void DestroyCrosshair()
        {
            WeaponCrosshair.Destroy();
        }

        public IEnumerator ChangeWeapon(ScriptableObject weapon = null, ScriptableObject ammo = null)
        {
            if (weapon == null)
                ammo = null;
            else if (ammo == null)
                ammo = (weapon as Weapon).defaultAmmo;
            yield return characterShooter.ChangeWeapon(weapon as Weapon, ammo as Ammo);
        }

        public void SetWeaponNameAndDescription(ScriptableObject weapon)
        {
            if (WeaponStashUi == null || !IsShooterWeapon(weapon))
                return;

            var shooterWeapon = weapon as Weapon;
            WeaponStashUi.SetWeapon(shooterWeapon.weaponName.GetText(), shooterWeapon.weaponDesc.GetText());
        }

        public void ChangeAmmo(ScriptableObject weapon)
        {
            if (!IsShooterWeapon(weapon))
                return;

            characterShooter.ChangeAmmo(GetAmmoUsedByWeapon(weapon) as Ammo);
        }

        public void SetAmmoNameAndDescription(ScriptableObject ammo)
        {
            if (WeaponStashUi == null || !(ammo is Ammo))
                return;

            var shooterAmmo = ammo as Ammo;
            WeaponStashUi.SetAmmo(shooterAmmo.ammoName.GetText(), shooterAmmo.ammoDesc.GetText());
        }

        public bool IsShooterWeapon(ScriptableObject weapon)
        {
            return weapon is Weapon;
        }

        public ScriptableObject GetAmmoUsedByWeapon(ScriptableObject weapon)
        {
            return (weapon as Weapon).defaultAmmo;
        }

        public void SetAmmoNameAndDescriptionFromWeapon(ScriptableObject weapon)
        {
            if (WeaponStashUi == null || !IsShooterWeapon(weapon))
                return;

            SetAmmoNameAndDescription((weapon as Weapon).defaultAmmo);
        }

        public bool IsAmmoUsedByWeapon(ScriptableObject weapon)
        {
            return IsShooterWeapon(weapon) && CurrentAmmo != (weapon as Weapon).defaultAmmo;
        }

        /**
         * Events
         */
        public void AddEventChangeAmmoListener()
        {
            characterShooter.eventChangeAmmo.AddListener(OnChangeAmmo);
        }

        public void RemoveEventChangeAmmoListener()
        {
            characterShooter.eventChangeAmmo.RemoveListener(OnChangeAmmo);
        }

        public void AddEventChangeClipListener(UnityAction<string, int> setAmmoInClip)
        {
            characterShooter.AddListenerClipChange(setAmmoInClip);
        }

        public void RemoveEventChangeClipListener(UnityAction<string, int> setAmmoInClip)
        {
            characterShooter.RmvListenerClipChange(setAmmoInClip);
        }

        public void AddEventChangeStorageListener(UnityAction<string, int> setAmmoInStorage)
        {
            characterShooter.AddListenerStorageChange(setAmmoInStorage);
        }

        public void RemoveEventChangeStorageListener(UnityAction<string, int> setAmmoInStorage)
        {
            characterShooter.RmvListenerStorageChange(setAmmoInStorage);
        }

        public GameObject GetAimAssistTarget()
        {
            if (characterShooter.currentAmmo == null || characterShooter.aiming == null)
                return null;

            var ammo = characterShooter.currentAmmo;

            if (ammo.shootType != Ammo.ShootType.SphereCast && ammo.shootType != Ammo.ShootType.SphereCastAll)
                return null;

            var shootPositionTarget = characterShooter.aiming.GetAimingPosition();
            var shootPositionRaycast = characterShooter.aiming.pointShootingRaycast;
            var direction = (shootPositionTarget - shootPositionRaycast).normalized;

            var hitCounter = Physics.SphereCastNonAlloc(
                shootPositionRaycast,
                ammo.radius,
                direction,
                bufferCastHits,
                ammo.distance,
                ammo.layerMask,
                ammo.triggersMask
            );

            if (hitCounter == 0)
                return null;

            var maxCount = Mathf.Min(hitCounter, this.bufferCastHits.Length);
            Array.Sort(bufferCastHits, 0, maxCount, AIM_ASSIST_RAYCAST_COMPARER);

            GameObject aimAssistTarget = null;
            if (bufferCastHits.Length > 0)
            {
                // Get all possible targets that aren't the shooter.
                var possibleTargets = bufferCastHits.ToList()
                    .Where(hit => {
                        return hit.collider != null &&
                               hit.collider.gameObject != null &&
                               hit.collider.gameObject != Character.gameObject &&
                               hit.collider.gameObject.TryGetComponent(out Targetable targetable);
                    })
                    .OrderBy(x => Vector3.Distance(shootPositionRaycast, x.collider.gameObject.transform.position));

                if (possibleTargets.Any())
                    aimAssistTarget = possibleTargets.FirstOrDefault().collider.gameObject;

            }
            Array.Clear(bufferCastHits, 0, bufferCastHits.Length);
            return aimAssistTarget;
        }
    }
}
