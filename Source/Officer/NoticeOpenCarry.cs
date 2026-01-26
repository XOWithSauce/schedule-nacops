
using static NACopsV1.DebugModule;

#if MONO
using ScheduleOne.ItemFramework;
using ScheduleOne.DevUtilities;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Vision;
#else
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Vision;
#endif

namespace NACopsV1
{

    public static class NoticeOpenCarry
    {
        public static readonly List<string> weaponIDs = new()
        {
            "baseballbat", "fryingpan", "machete", "revolver", "m1911", "pumpshotgun"
        };
        public static bool HasSetBrandishing = false;
        public static bool IsCheckingSlot = false;

        #region Check for player equip slot is weapon and update visibility
        public static void CheckSlotItem()
        {
            int current = PlayerSingleton<PlayerInventory>.Instance._equippedSlotIndex;
            if (current == -1)
                current = PlayerSingleton<PlayerInventory>.Instance.PreviousEquippedSlotIndex;
            if (current >= 0 && current < 8)
            {
                ItemInstance item = Player.Local.Inventory[current].ItemInstance;
                if (item != null)
                {
                    if (weaponIDs.Contains(item.ID))
                        SetNoticable(true);
                    else
                        SetNoticable(false);
                }
                else
                {
                    SetNoticable(false);
                }
            }
            else
            {
                // Not itemslot
                SetNoticable(false);
            }
            IsCheckingSlot = false;
            return;
        }

        public static void OnSlotChanged(int _)
        {
            if (!IsCheckingSlot)
            {
                IsCheckingSlot = true;
                CheckSlotItem();
            }
        }
        public static void OnPlayerArrested()
        {
            //Log("RemoveState Brandishing");
            Player.Local.VisualState.RemoveState("Brandishing", 0f);
        }

        public static void SetNoticable(bool enabled)
        {
            if (enabled && !HasSetBrandishing)
            {
                HasSetBrandishing = true;
                Player.Local.VisualState.ApplyState("Brandishing", EVisualState.Brandishing, 0f);
                //Log("Player Brandishing");
            }
            else if (HasSetBrandishing)
            {
                HasSetBrandishing = false;
                //Log("RemoveState Brandishing");
                Player.Local.VisualState.RemoveState("Brandishing", 0f);
            }
        }
        #endregion

        #region Change Weapons to be illegal in inventory
        public static void SetWeaponsLegalStatus()
        {
            Func<string, ItemDefinition> GetItem;
#if MONO
            GetItem = ScheduleOne.Registry.GetItem;
#else
            GetItem = Il2CppScheduleOne.Registry.GetItem;
#endif
            foreach (string id in weaponIDs)
            {
                ItemDefinition defWeapon = GetItem(id);
                defWeapon.legalStatus = ELegalStatus.HighSeverityDrug; // or make later new enum value??
            }
        }
        #endregion

    }

}