using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace MistMod {

    /// <summary> Dialog for selecting which metal to burn. </summary>
    public class GuiDialogMetalSelector : GuiDialog
    {
        /// <summary> Index of the selected metal </summary>
        public int SelectedMetal = -1;
        /// <summary> Id of the key combination used to toggle the dialog </summary>
        public override string ToggleKeyCombinationCode => "guimetalselect";

        private ClientAllomancyHandler Chandler;

        string DisplayItemCode = "mistmod:vial-";
        double OriginX = 180 + 15;
        double OriginY = 180 + 40;
        double ExternalLen = 180;
        double InternalLen = 100;
        double SlotSize = GuiElementPassiveItemSlot.unscaledSlotSize 
            + GuiElementItemSlotGrid.unscaledSlotPadding;

        /// <summary> Create an instance of the dialog. </summary>
        /// <param name="capi"> A reference to the client api.static </param>
        public GuiDialogMetalSelector (ICoreClientAPI capi, ClientAllomancyHandler chandler) : base(capi) {
            Chandler = chandler;
            SetupDialog();
        }
        
        /// <summary> Draw all the elements that go in the dialog. </summary>
        private void SetupDialog() {

            // Make the dialog centered and resize automatically
            ElementBounds dialogBounds = ElementStdBounds
                .AutosizedMainDialog
                .WithAlignment(EnumDialogArea.CenterMiddle);

            // Background boundaries. Make it fit the metal slots.
            ElementBounds bgBounds = ElementBounds
                .Fill
                .WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;

            // Create the boundaries for each metal slot, then assign them as children of the background.
            ElementBounds[] metalBounds = new ElementBounds[MistModSystem.METALS.Length];
            for (int i = 0; i < MistModSystem.METALS.Length; i++) {
                metalBounds[i] = BoundsForIndex(i / 2, i % 2 == 1);
            }
            bgBounds.WithChildren(metalBounds);
            
            // Create the boundaries for the decoration image.
            ElementBounds decoBounds = ElementBounds.Fixed(0, 40, 420, 405);

            // Create the boundaries for the text indicating which metal is selected.
            ElementBounds metalText = ElementBounds.Fixed(
                OriginX - (200 / 2) + 18, 
                OriginY - (25 / 2), 
                200, 
                25);
            ElementBounds metalAmount = ElementBounds.Fixed(
                OriginX - (200 / 2) + 18, 
                OriginY - (25 / 2) + 20, 
                200, 
                25);

            // Lastly, create the dialog
            SingleComposer = capi.Gui.CreateCompo("metalSelector", dialogBounds)
                .AddShadedDialogBG(bgBounds) // Draw background 
                .AddDialogTitleBar("Select allomantic metal", OnTitleBarCloseClicked) // Draw title. 
                .AddImageBG(decoBounds, "gui/backgrounds/metalselector.png") // Draw decoration.
                .AddDynamicText(
                    "Metal", 
                    CairoFont.WhiteSmallText(), 
                    EnumTextOrientation.Center, 
                    metalText, 
                    "metalText") // Draw metal name.
                .AddDynamicText(
                    "Amount", 
                    CairoFont.WhiteSmallText(), 
                    EnumTextOrientation.Center, 
                    metalAmount, 
                    "metalAmount"); // Draw metal amount.

            // Iterate to create the slot for this metal.
            for (int i = 0; i < MistModSystem.METALS.Length; i++) {

                // Create a dummy slot of the item that will be displayed.
                AssetLocation itemLocation = AssetLocation.Create(
                    DisplayItemCode + MistModSystem.METALS[i]);
                Item item = capi.World.GetItem(itemLocation);
                ItemStack stack = new ItemStack(item,1);
                ItemSlot dummySlot = new DummySlot(stack);

                // Create a single-element dictionary to store the skill item.
                Dictionary<int,SkillItem> skillItems = new Dictionary<int, SkillItem>();
                skillItems.Add(0, new SkillItem(){
                    Code = itemLocation,
                    Name = MistModSystem.METALS[i],
                    Description = "Select " + MistModSystem.METALS[i],
                    RenderHandler = (AssetLocation code, float dt, double posX, double posY) => {
                        // No idea why the weird offset and size multiplier
                        double scsize = GuiElement.scaled(SlotSize - 5);
                        capi.Render.RenderItemstackToGui(
                            dummySlot, 
                            posX + scsize/2, 
                            posY + scsize / 2, 
                            100, 
                            (float)GuiElement.scaled(GuiElementPassiveItemSlot.unscaledItemSize), 
                            ColorUtil.WhiteArgb);
                    }
                });

                // Add the skill item to the UI.
                var j = i;
                SingleComposer = SingleComposer.AddSkillItemGrid(
                    skillItems, 
                    1, 
                    1,
                    delegate(int a) {SelectMetal(j);},
                    metalBounds[i],
                    "skill-"+MistModSystem.METALS[i]);

                // Add a text to easily tell which metal is being selected.
                SingleComposer = SingleComposer.AddHoverText(
                    MistModSystem.METALS[i],
                    CairoFont.WhiteSmallishText(), 
                    150, 
                    metalBounds[i]);
            }
            
            // Compose the dialog after all changes have been made. 
            SingleComposer = SingleComposer.Compose();
        }

        private void OnTitleBarCloseClicked()
        {
            TryClose();
        }
        
        /// <summary> Trigger an update of the UI </summary>
        public void UpdateUI (float dt) {
            if (SelectedMetal != -1) {
                string metalName = MistModSystem.METALS[SelectedMetal];
                float amount = Chandler.AllomancyHelper.GetMetalReserve(metalName);
                SetMetalAmount(amount);
            }
        }

        private void SetMetalAmount (float amount) {
            SingleComposer.GetDynamicText("metalAmount")
                .SetNewText("" + amount); // Change the amount of metal. 
        }

        /// <summary> Select a specific metal for burning </summary>
        public void SelectMetal (int index) {
            SingleComposer.GetDynamicText("metalText")
                .SetNewText(MistModSystem.METALS[index]); // Change the metal text accordingly.
            SelectedMetal = index;
            Chandler.Channel.SendPacket(new SelectedMetalMessage(index));
        }

        private ElementBounds BoundsForIndex (int index, bool external) {
            // Calculate the bounds
            double width = SlotSize;
            double height = SlotSize;
            ElementBounds result = ElementBounds.Fixed(
                RawPosX(index, external) - (width / 2), 
                RawPosY(index, external) - (height / 2), 
                width, 
                height);
            return result;
        }

        private double AngleForIndex (int index) {
            double degreesPerIndex = GameMath.TWOPI / 8;
            double padding = degreesPerIndex / 2;
            return (index * degreesPerIndex) + padding - GameMath.PIHALF;
        }

        private double RawPosY (int id, bool external) {
            return OriginY + PolarHelperY(
                AngleForIndex(id), 
                external ? ExternalLen : InternalLen);
        }

        private double RawPosX (int id, bool external) {
            return OriginX + PolarHelperX(
                AngleForIndex(id), 
                external ? ExternalLen : InternalLen);
        }

        private double PolarHelperX (double angle, double distance) {
            return GameMath.Cos(angle) * distance;
        }

        private double PolarHelperY (double angle, double distance) {
            return GameMath.Sin(angle) * distance;
        }
    }
}