using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using ProtoBuf;

namespace MistMod {

    /// <summary> Message for sending changes in the burn status.
    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    public class BurnMessage {
        public readonly int _metal_id;
        public readonly int _burn_strength;
        public BurnMessage(int id, int strength) {
            _metal_id = id;
            _burn_strength = strength;
        }
        public BurnMessage() {
            
        }
    }
}