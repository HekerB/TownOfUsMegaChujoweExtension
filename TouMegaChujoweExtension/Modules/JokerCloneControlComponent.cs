using System;
using System.Linq;
using Il2CppInterop.Runtime.Attributes;
using Reactor.Utilities.Attributes;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using UnityEngine;
using TownOfUs.Utilities;
using MiraAPI.GameOptions;

namespace TouMegaChujoweExtension.Modules
{
    [RegisterInIl2Cpp]
    public sealed class JokerCloneControlComponent : MonoBehaviour
    {
        public JokerCloneControlComponent(IntPtr ptr) : base(ptr) { }

        public byte OwnerId;
        public byte AppearanceId;

        private JokerDummy _dummy;
        private PetBehaviour _petBehaviour;

        private void Start()
        {
            var myCloneData = JokerCloneSystem.Clones.FirstOrDefault(c => c.Fake?.body == this.gameObject);
            if (myCloneData != null)
            {
                _dummy = myCloneData.Fake;
                _petBehaviour = _dummy?.Pet;
            }
        }
    }
}
