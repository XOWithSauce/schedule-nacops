

using System.Collections;
using HarmonyLib;
using MelonLoader;

using static NACopsV1.BaseUtility;
using static NACopsV1.NACops;
using static NACopsV1.DebugModule;

#if MONO
using ScheduleOne.Economy;
using ScheduleOne.GameTime;
using ScheduleOne.PlayerScripts;
#else
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.PlayerScripts;
#endif


namespace NACopsV1
{

    [HarmonyPatch(typeof(Customer), "SampleOffered")]
    public static class Customer_SampleOffered_Patch
    {
        public static bool Prefix(Customer __instance)
        {
            coros.Add(MelonCoroutines.Start(PreSampleOffered(__instance)));
            return true;
        }

        private static IEnumerator PreSampleOffered(Customer customer)
        {
            if (!currentConfig.SnitchingSamples) yield break;
            yield return Wait5;
            if (!registered) yield break;

            Player closestPlayer = Player.GetClosestPlayer(customer.transform.position, out _);
            if (closestPlayer == null) yield break;
            (float min, float max) = ThresholdUtils.Evaluate(ThresholdMappings.SnitchProbability, TimeManager.Instance.ElapsedDays);
            if (UnityEngine.Random.Range(min, max) < 0.8f) yield break;
            Log("Snitching Samples");
            coros.Add(MelonCoroutines.Start(LateInvestigation(closestPlayer)));
            coros.Add(MelonCoroutines.Start(GiveFalseCharges(severity: 1, player: closestPlayer)));
        }
    }

}