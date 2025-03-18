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
    Bind
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
        AffectsSpellCasting = affectsSpellCasting;
    }
}

namespace IncapacitationEffect
{
    public class Incapacitation : DebuffController
    {
        public delegate void PlayerIncapacitation(ulong clientId, IncapacitationInfo info);
        public static event PlayerIncapacitation playerIncapacitation;

        public bool isActivated = false;

        public static readonly Dictionary<IncapacitationName, IncapacitationInfo> incapacitationDict = new Dictionary<IncapacitationName, IncapacitationInfo>
        {   // idk what i was thinking. to revise 
            { IncapacitationName.Stun, new IncapacitationInfo(IncapacitationType.MovementAndSpellCasting, true, false, false, true) },
            { IncapacitationName.Slow, new IncapacitationInfo(IncapacitationType.MovementSlow, false, true, false, false) },
            { IncapacitationName.Bind, new IncapacitationInfo(IncapacitationType.MovementStop, false, false, true, false) },
            { IncapacitationName.Fear, new IncapacitationInfo(IncapacitationType.MovementAndSpellCasting, true, false, false, true) },
            { IncapacitationName.None, new IncapacitationInfo(IncapacitationType.None, false, false, false, false) }
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

