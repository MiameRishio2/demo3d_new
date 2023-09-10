namespace FireChickenGames.Combat.Core.Integrations
{
    using System.Collections;
    using UnityEngine;
    using UnityEngine.Events;

    public interface ICharacterShooter
    {
        bool IsControllable { get; }
        bool IsCharacterLocomotionBusy { get; }

        bool IsDrawing { get; }
        bool IsHolstering { get; }
        bool IsReloading { get; }

        bool IsAiming { get; }
        bool IsChargingShot { get; }
        ScriptableObject CurrentWeapon { get; }
        ScriptableObject CurrentAmmo { get; }

        WeaponStashUi WeaponStashUi { get; set; }
        UnityAction<ScriptableObject> SetStashedWeapon { get; set; }

        bool HasCharacterShooter();
        void SetCharacterShooter(Component characterShooter);
        Component GetCharacterShooter();

        IEnumerator ChangeWeapon(ScriptableObject weapon = null, ScriptableObject ammo = null);
        void SetWeaponNameAndDescription(ScriptableObject stashedWeapon);

        void ChangeAmmo(ScriptableObject weapon);
        void SetAmmoNameAndDescription(ScriptableObject ammo);
        bool IsShooterWeapon(ScriptableObject weapon);
        ScriptableObject GetAmmoUsedByWeapon(ScriptableObject weapon);
        void SetAmmoNameAndDescriptionFromWeapon(ScriptableObject weapon);
        bool IsAmmoUsedByWeapon(ScriptableObject weapon);

        void DestroyCrosshair();

        // Aiming
        void AddEventOnAimListener(UnityAction<bool> onAim);
        void RemoveEventOnAimListener(UnityAction<bool> onAim);
        void StartAiming(IAimingAtProximityTarget aimingAtTarget = null);
        GameObject GetAimAssistTarget();

        // Ammo
        void AddEventChangeAmmoListener();
        void RemoveEventChangeAmmoListener();

        // Clip
        void AddEventChangeClipListener(UnityAction<string, int> setAmmoInClip);
        void RemoveEventChangeClipListener(UnityAction<string, int> setAmmoInClip);

        // Storage
        void AddEventChangeStorageListener(UnityAction<string, int> setAmmoInStorage);
        void RemoveEventChangeStorageListener(UnityAction<string, int> setAmmoInStorage);
    }
}
