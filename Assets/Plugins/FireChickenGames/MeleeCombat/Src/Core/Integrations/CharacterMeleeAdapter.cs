namespace FireChickenGames.MeleeCombat.Core.Integrations
{
    using FireChickenGames.Combat.Core.Integrations;
    using GameCreator.Melee;
    using System.Collections;
    using UnityEngine;

    public class CharacterMeleeAdapter : ICharacterMelee
    {
        public CharacterMelee _characterMelee;

        public ScriptableObject CurrentWeapon { get { return _characterMelee?.currentWeapon; } }
        public ScriptableObject CurrentShield { get { return _characterMelee?.currentShield; } }

        public void SetCharacterMelee(Component characterMelee)
        {
            _characterMelee = characterMelee as CharacterMelee;
        }

        public IEnumerator Draw(ScriptableObject meleeWeapon, ScriptableObject shield = null)
        {
            if (_characterMelee != null && meleeWeapon is MeleeWeapon && (shield is MeleeShield || shield == null))
                yield return _characterMelee.Draw(meleeWeapon as MeleeWeapon, shield as MeleeShield);
        }

        public void TakeWeapon()
        {
            if (_characterMelee != null)
                _characterMelee.currentWeapon = null;
        }

        public IEnumerator Sheathe()
        {
            if (_characterMelee != null)
                yield return _characterMelee.Sheathe();
        }

        public string GetWeaponName(ScriptableObject meleeWeapon)
        {
            return meleeWeapon is MeleeWeapon ? (meleeWeapon as MeleeWeapon).weaponName.GetText() : "";
        }

        public string GetWeaponDescription(ScriptableObject meleeWeapon)
        {
            return meleeWeapon is MeleeWeapon ? (meleeWeapon as MeleeWeapon).weaponDescription.GetText() : "";
        }
    }
}
