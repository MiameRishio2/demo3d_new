namespace FireChickenGames.Combat.Core.Integrations
{
    using System.Collections;
    using UnityEngine;

    public interface ICharacterMelee
    {
        ScriptableObject CurrentWeapon { get; }
        ScriptableObject CurrentShield { get; }

        void SetCharacterMelee(Component characterMelee);

        IEnumerator Sheathe();
        IEnumerator Draw(ScriptableObject weapon, ScriptableObject shield = null);
        void TakeWeapon();
        string GetWeaponName(ScriptableObject stashedWeapon);
        string GetWeaponDescription(ScriptableObject stashedWeapon);
    }
}
