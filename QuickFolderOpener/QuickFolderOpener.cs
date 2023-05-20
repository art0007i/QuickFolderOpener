using HarmonyLib;
using NeosModLoader;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using FrooxEngine;
using System.Reflection.Emit;
using BaseX;
using CloudX.Shared;

namespace QuickFolderOpener
{
    public class QuickFolderOpener : NeosMod
    {
        public override string Name => "QuickFolderOpener";
        public override string Author => "art0007i";
        public override string Version => "1.0.0";
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
                    Msg(code.ToString());
                    yield return code;
                    // too lazy to type out all the method args for proper reflection
                    if ((code.operand as MethodInfo)?.Name == "AddItem")
                    {
                        Msg("WE IN THERE!!!!");
                        foreach (var c in injects) yield return c;
                    }
                }
            }

            public static void InsertContextMenu(InventoryLink i, ContextMenu m)
            {
                var ruri = i.Target.Value;
                if (ruri.Scheme != "neosrec") return;
                m.AddItem("Open Folder", NeosAssets.Common.Icons.Folder, color.Orange).Button.LocalPressed += async (b,e) => { 
                    var dash = Userspace.UserspaceWorld.GetGloballyRegisteredComponent<UserspaceRadiantDash>();
                    SyncRef<NeosCanvasPanel> invPanel = Traverse.Create(dash).Field("_legacyInventoryPanel").GetValue() as SyncRef<NeosCanvasPanel>;
                    SyncRef<InventoryBrowser> inv = Traverse.Create(dash).Field("_legacyInventory").GetValue() as SyncRef<InventoryBrowser>;

                    // if it doesn't exist, or is inactive
                    if (invPanel.Target == null || !invPanel.Target.Slot.ActiveSelf)
                    {
                        dash.ToggleLegacyInventory();
                    }
                    CoroutineManager.Manager.Value = i.World.Coroutines;
                    await default(ToBackground);
                    var rec = (await Engine.Current.RecordManager.FetchRecord(ruri)).Entity;
                    await default(ToWorld);
                    Msg($"opening inventory {rec.OwnerId}, {rec.Path}, {i.TargetName.Value}");
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