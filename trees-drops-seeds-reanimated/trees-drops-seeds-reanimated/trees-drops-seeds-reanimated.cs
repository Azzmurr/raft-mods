using UnityEngine;
using HarmonyLib;
using System.Reflection;

namespace azzmurr.TreesDropsSeedsReanimated
{
    public class TreesDropSeeds : Mod
    {
        public Harmony harmony;

        public void Start()
        {

            harmony = new Harmony("com.azzmurr.trees-drops-seeds-reanimated");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Debug.Log("TreesDropsSeedsReanimated loaded!");
        }

        public void OnModUnload()
        {
            harmony.UnpatchAll("com.azzmurr.trees-drops-seeds-reanimated");
            Destroy(gameObject);
            Debug.Log("TreesDropsSeedsReanimated unloaded!");
        }

        public static void ForceSeedDropRate(PickupItem pickupItem)
        {
            SO_RandomDropper dropper = Traverse.Create(pickupItem.dropper).Field("randomDropperAsset").GetValue() as SO_RandomDropper;
            Randomizer randomizer = dropper.randomizer;
            
            foreach (RandomItem item in randomizer.items)
            {
                if (item.obj != null && item.obj.GetType() == typeof(Item_Base))
                {
                    Item_Base item_Base = item.obj as Item_Base;
                    if (item_Base.UniqueName.ToLower().Contains("seed"))
                    {
                        item.weight = randomizer.TotalWeight * 0.3f;
                        item.spawnChance = item.weight / randomizer.TotalWeight * 100f + "%";
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(HarvestableTree)), HarmonyPatch("Harvest")]
    public class HarvestPatch
    {
        private static bool Prefix(ref PickupItem ___pickupItem)
        {
            TreesDropSeeds.ForceSeedDropRate(___pickupItem);
            return true;
        }
    }

}