using HarmonyLib;
using ResoniteModLoader;
using FrooxEngine.Store;
using System.Reflection;
using System.Collections.Generic;
using FrooxEngine;
using System.Reflection.Emit;
using Elements.Core;

namespace QuickFolderOpener
{
    public class QuickFolderOpener : ResoniteMod
    {
        public override string Name => "QuickFolderOpener";
        public override string Author => "art0007i";
        public override string Version => "2.0.1";
        public override string Link => "https://github.com/art0007i/QuickFolderOpener/";
        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony("me.art0007i.QuickFolderOpener");
            harmony.PatchAll();

        }
        [HarmonyPatch(typeof(InventoryLink), nameof(InventoryLink.GenerateMenuItems))]
        class QuickFolderOpenerPatch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes)
            {
                var injects = new CodeInstruction[] {
                    new(OpCodes.Ldarg_0),
                    new(OpCodes.Ldarg_1),
                    new(OpCodes.Call, typeof(QuickFolderOpenerPatch).GetMethod(nameof(InsertContextMenu))),
                };
                foreach (var code in codes)
                {
                    yield return code;
                    // too lazy to type out all the method args for proper reflection
                    if ((code.operand as MethodInfo)?.Name == "AddItem")
                    {
                        foreach (var c in injects) yield return c;
                    }
                }
            }

            public static void InsertContextMenu(InventoryLink i, ContextMenu m)
            {
                var ruri = i.Target.Value;
                if (ruri.Scheme != "resrec") return;
                m.AddItem("Open Folder", OfficialAssets.Common.Icons.Folder, colorX.Orange).Button.LocalPressed += async (b,e) => { 
                    var dash = Userspace.UserspaceWorld.GetGloballyRegisteredComponent<UserspaceRadiantDash>();
                    SyncRef<LegacyCanvasPanel> invPanel = Traverse.Create(dash).Field("_legacyInventoryPanel").GetValue() as SyncRef<LegacyCanvasPanel>;
                    SyncRef<InventoryBrowser> inv = Traverse.Create(dash).Field("_legacyInventory").GetValue() as SyncRef<InventoryBrowser>;

                    // if it doesn't exist, or is inactive
                    if (invPanel.Target == null || !invPanel.Target.Slot.ActiveSelf)
                    {
                        Debug("Toggling legacy inventory...");
                        dash.RunSynchronously(() =>
                        {
                            dash.ToggleLegacyInventory();
                        });
                    }
                    CoroutineManager.Manager.Value = i.World.Coroutines;
                    await default(ToBackground);
                    var rec = (await Engine.Current.RecordManager.FetchRecord(ruri)).Entity;
                    await default(ToWorld);
                    Debug($"opening inventory {rec.OwnerId}, {rec.Path}, {i.TargetName.Value}");
                    inv?.Target?.RunSynchronously(() => 
                    {
                        // yes, froox uses backslash for paths internally. it makes me cry
                        inv?.Target?.Open(new RecordDirectory(rec.OwnerId, rec.Path + "\\" + rec.Name, Engine.Current, i.TargetName.Value), FrooxEngine.UIX.SlideSwapRegion.Slide.Left);
                    });
                };
            }
        }
    }
}