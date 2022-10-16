using _3dTerrainGeneration.util;

namespace _3dTerrainGeneration.entity
{
    internal class WeaponBase
    {
        public double AttackDamage;
        public double AttackCooldown;
        public double LastAttack;

        public WeaponBase(double AttackDamage, double AttackCooldown)
        {
            this.AttackDamage = AttackDamage;
            this.AttackCooldown = AttackCooldown;
        }

        public void Attack(EntityBase target)
        {
            if (LastAttack + AttackCooldown < TimeUtil.Unix()) return;

            target.Hurt(AttackDamage);
            LastAttack = TimeUtil.Unix();
        }
    }
}
