using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapleCooldown
{
    public class Skill
    {
        public string iconPath { get; set; }
        public int cooldownSeconds { get; set; } // Maximum cooldown, start value
        public string keyboardKey { get; set; } // Key bind
        public float cooldownRemaining { get; set; } // Current value of the cooldown
        public bool isOnCooldown { get; set; } = false;
        public void ResetState()
        {
            cooldownRemaining = cooldownSeconds;
            isOnCooldown = false;
        }
        public override string ToString()
        {
            return string.Format("icon: {0}, cooldown: {1}, max cd: {3}, key: {2}", iconPath, Math.Round(cooldownRemaining, 1, MidpointRounding.AwayFromZero), keyboardKey, cooldownSeconds);
        }
    }
    public class Root
    {
        public List<Skill> skills { get; set; }
    }
}
