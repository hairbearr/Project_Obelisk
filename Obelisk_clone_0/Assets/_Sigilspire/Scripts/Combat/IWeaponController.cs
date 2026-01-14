using UnityEngine;

namespace Combat
{
    public interface IWeaponController
    {
        void RequestUseAbility(Vector2 inputDirection);

        bool CanUseAbility();
        float GetCooldownRemaining();
    }
}
