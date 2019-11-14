using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using ProtoBuf;

namespace MistMod {

    /// <summary> Message for sending changes in the selected metal.
    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    public class SelectedMetalMessage {
        public readonly int _metal_id;
        public SelectedMetalMessage(int id) {
            _metal_id = id;
        }
        public SelectedMetalMessage() {
            
        }
    }
}