using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using DebuffEffect;
using Unity.Netcode;





public enum IncapacitationType
{
    None,
    Movement,
    MovementSlow,
    MovementStop,
    SpellCasting,
    MovementAndSpellCasting,
    // Add more incapacitation types as needed
}

public enum IncapacitationName
{
    None,  
    Stun,
    Slow,
    Fear,
    Bind,
    Silence
}

public struct IncapacitationInfo
{
    public IncapacitationType Type { get; }
    public bool AffectsMovement { get; }
    public bool SlowsMovement { get; }
    public bool StopsMovement { get; }
    public bool AffectsSpellCasting { get; }

    public IncapacitationInfo(IncapacitationType type, bool affectsMovement, bool slowsMovementp, bool stopsMovementp, bool affectsSpellCasting)
    {
        Type = type;
        AffectsMovement = affectsMovement;
        SlowsMovement = slowsMovementp;
        StopsMovement = stopsMovementp;
        AffectsSpellCasting = affectsSpellCasting; // Currently only stops spell casting -- To rename
        // To add spell casting buffer slow
        // To add spell casting DR increase

    }
}

namespace IncapacitationEffect
{

    public class Incapacitation : DebuffController
    {
        protected static readonly bool isMovementAffected = true;
        protected static readonly bool isMovementSlowed = true;
        protected static readonly bool isStopsMovement = true;
        protected static readonly bool isSpellCastingAffected = true;

        public delegate void PlayerIncapacitation(ulong clientId, IncapacitationInfo info);
        public static event PlayerIncapacitation playerIncapacitation;

        public bool isActivated = false;

        public static readonly Dictionary<IncapacitationName, IncapacitationInfo> incapacitationDict = new Dictionary<IncapacitationName, IncapacitationInfo>
            {
                { IncapacitationName.Stun, new IncapacitationInfo(      // STUN
                    IncapacitationType.MovementAndSpellCasting, 
                    isMovementAffected, 
                    !isMovementSlowed, 
                    isStopsMovement, 
                    isSpellCastingAffected) },
                { IncapacitationName.Slow, new IncapacitationInfo(      // SLOW
                    IncapacitationType.MovementSlow,
                    !isMovementAffected, 
                    isMovementSlowed,
                    !isStopsMovement,
                    !isSpellCastingAffected) },
                { IncapacitationName.Bind, new IncapacitationInfo(      // BIND
                    IncapacitationType.MovementStop, 
                    !isMovementAffected,
                    !isMovementSlowed, 
                    isStopsMovement,
                    !isSpellCastingAffected) },
                { IncapacitationName.Fear, new IncapacitationInfo(      // FEAR
                    IncapacitationType.MovementAndSpellCasting,
                    isMovementAffected,
                    !isMovementSlowed,
                    !isStopsMovement, 
                    isSpellCastingAffected) },
                { IncapacitationName.Silence, new IncapacitationInfo(   // SILENCE
                    IncapacitationType.SpellCasting,
                    !isMovementAffected,
                    !isMovementSlowed,
                    !isStopsMovement, 
                    isSpellCastingAffected) },
                { IncapacitationName.None, new IncapacitationInfo(      // NONE
                    IncapacitationType.None, 
                    !isMovementAffected,
                    !isMovementSlowed,
                    !isStopsMovement,
                    !isSpellCastingAffected) }
            };

        float incapacitationDuration;
        ulong clientId;

        private IncapacitationName _currentIncapacitationName;

        public Incapacitation(IncapacitationName name, float duration, ulong clientId)
        {
            // This keyword needed here to access the variable in parent class
            this.IncapacitationDuration = duration;
            _currentIncapacitationName = name;
            this.clientId = clientId;
        }

        //private void Start()
        //{
        //    ActivateIncapacitation();
        //}

        public IncapacitationInfo GetIncapacitation()
        {
            IncapacitationName name = _currentIncapacitationName;

            //IncapacitationInfo theInfo = incapacitationDict.TryGetValue(name, out IncapacitationInfo info2) ? info2 : default;

            //print($"<color=red>{theInfo}</color>");

            // Accessing the IncapacitationInfo
            return incapacitationDict.TryGetValue(name, out IncapacitationInfo info) ? info : default;
        }

        public void ActivateIncapacitation()
        {
            //write("Incapacitation activated");
            IncapacitationName name = _currentIncapacitationName;

            if (playerIncapacitation != null) playerIncapacitation(clientId, incapacitationDict.TryGetValue(name, out IncapacitationInfo info2) ? info2 : default);

            // Accessing the IncapacitationInfo
            //return incapacitationDict.TryGetValue(name, out IncapacitationInfo info) ? info : default;
        }

                
        public void DeactivateIncapacitation()
        {
            IncapacitationName name = IncapacitationName.None;

            if (playerIncapacitation != null) playerIncapacitation(clientId, incapacitationDict.TryGetValue(name, out IncapacitationInfo info2) ? info2 : default);

            // Accessing the IncapacitationInfo
            //return incapacitationDict.TryGetValue(name, out IncapacitationInfo info) ? info : default;
        }

        public override bool Timer()
        {
            // if timer is active do nothing
            // if timer has ended emit new incapacitation bool values
            return base.Timer();
        }

    }
}

