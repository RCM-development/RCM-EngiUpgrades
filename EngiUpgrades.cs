using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using Shapes2D;
using TestMod;
using UnityEngine;
namespace RCM_CustomUnits{

    [BepInDependency(RCMManager.IDENTIFIER, BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(IDENTIFIER, "Engi Upgradaes Plugin", "1.0.0.0")]
    internal class EngiUpgrades : BaseUnityPlugin{
        const string IDENTIFIER = "RCM.plugins.engiupgrades";
        private void Awake(){
            new Harmony(IDENTIFIER).PatchAll();
            RCMManager.ConnectMod("Upgrades 4 Engi++").ContinueWith(t => {
                RCMModUI mod = t.Result;

                // begin mod UI construction here...
                mod.CreateCheckboxField("Show Engi", ToggleEngineer, engineer_enabled);
                mod.CreateCheckboxField("Show Spider", ToggleCreeper, creeper_enabled);
                mod.CreateCheckboxField("Show Zombo", ToggleZombo, zombo_enabled);

            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
        static bool engineer_enabled = false;
        static bool creeper_enabled = false;
        static bool zombo_enabled = false;
        void ToggleEngineer(bool value){ engineer_enabled = value;RefreshShownUpgrades();}
        void ToggleCreeper(bool value){creeper_enabled = value;RefreshShownUpgrades();}
        void ToggleZombo(bool value){zombo_enabled = value;RefreshShownUpgrades();}

        void RefreshShownUpgrades(){
            var deck = AssignUpgradeToCardView._instance;
            if (deck == null) return;

            int increment = 0;
            // clear deck
            while (deck._cards.Count > 0){
                RCMManager.Log($"destroying card: debug1: {increment}");
                GameObject.Destroy(deck._cards[0].gameObject);
                deck._cards.RemoveAt(0);
                increment++;
            }
            // leave nothing in our card UI slots
            for (int i = 0; i < deck.cardSlotTransforms.Count; i++){
                while (deck.cardSlotTransforms[i].childCount > 0){
                    var child = deck.cardSlotTransforms[i].GetChild(0);
                    child.SetParent(null);
                    GameObject.Destroy(child);
            }}
            // dont bother init'ing if not open, it'll do that for us next time
            if (!deck._isShowing) return;

            deck.InitCards();
            //deck.UpdateAllCards(); // may or may not be needed, p sure though that it does this when the cards are initialized (always, since we just deleted them)
            CardUpgradeScriptableObject cardUpgradeScriptableObject = UpgradeBalancingStore.ScriptableObject(deck._currentUpgradeId);
            foreach (CardNew cardNew in deck._cards){
                if (AssignUpgradeToCardView.Filter(cardNew.EntityId, deck._currentUpgradeId, cardUpgradeScriptableObject))
                     cardNew.SetNonViable();
                else cardNew.SetViable();
            }
        }

        [HarmonyPatch(typeof(AssignUpgradeToCardView), "InitCards")]
        private static class AssignUpgradeToCardView_Patch{
            private static bool Prefix(AssignUpgradeToCardView __instance){
                int i = __instance._cards.Count;

                bool engi_inserted = __instance._cards.Any(c => c.EntityId == Game.Engineer);
                bool spider_inserted = __instance._cards.Any(c => c.EntityId == "SpiderSpawn");
                bool zombo_inserted = __instance._cards.Any(c => c.EntityId == "RoboZombo");

                if (engineer_enabled && !engi_inserted && i < __instance.cardSlotTransforms.Count){
                    add_entity_id_to_deck(__instance, Game.Engineer, i);
                    engi_inserted = true;
                    i++;
                }
                if (creeper_enabled && !spider_inserted && i < __instance.cardSlotTransforms.Count){
                    add_entity_id_to_deck(__instance, "SpiderSpawn", i);
                    spider_inserted = true;
                    i++;
                }
                if (zombo_enabled && !zombo_inserted && i < __instance.cardSlotTransforms.Count){
                    add_entity_id_to_deck(__instance, "RoboZombo", i);
                    zombo_inserted = true;
                    i++;
                }

                int non_blueprint_count = (engi_inserted ? 1 : 0) + (spider_inserted ? 1 : 0) + (zombo_inserted ? 1 : 0);
                for (; i < __instance.cardSlotTransforms.Count && i - non_blueprint_count < Game.CardsInDeck.Count; i++){
                    string text = Game.CardsInDeck[i - non_blueprint_count];
                    string text2 = EntityBalancingStore.ProductEntityId(text);
                    if (text2 != null)
                         add_entity_id_to_deck(__instance, text2, i);
                    else add_entity_id_to_deck(__instance, text, i);
                }
                return false;
            }
            static void add_entity_id_to_deck(AssignUpgradeToCardView deck_obj, string entity_id, int slot_index){
                CardNew component = GameObject.Instantiate<GameObject>(deck_obj.cardPrefab, deck_obj.cardSlotTransforms[slot_index]).GetComponent<CardNew>();
                component.Init(entity_id, deck_obj, -1, false);
                List<string> list;
                if (Game.CardUpgrades.TryGetValue(entity_id, out list))
                {
                    foreach (string text3 in list)
                    {
                        component.AddUpgrade(text3, false);
                    }
                }
                deck_obj._cards.Add(component);
            }
        }
    }
}